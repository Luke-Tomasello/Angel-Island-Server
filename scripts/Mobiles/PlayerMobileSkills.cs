/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

/* Scripts/Mobiles/PlayerMobileSkills.cs
 * ChangeLog:
 *  11/2/2023, Adam (RotInfo)
 *      Add a Memory object to prevent players from spamming [RotInfo
 *  10/9/2023, Adam
 *      For Siege II we bump up the daily stat gains from 6 to 10
 *  8/6/2023, Adam
 *      Add LogStatus for rollover credits issued, and applied.
 *  8/4/2023, Adam (CalcRoTSkillRolloverCredits)
 *      Design the RoT Smooth Gain System (RoTSGS)
 *      RoTSGS saves the time invested in a skill before the RoT session rolls over. These saved units are 'credits'.
 *      The next time we check to see if you are due for a gain, we apply the credits. (subtracting previous session time invested from the current time requirement.)
 *          If you are due for a gain, and you gain, the credit for this skill is zeroed. 
 *      It is noteworthy that this credits array, as well as the 'time to next gain' array are only byte arrays. Very small, very efficient.
 *      Furthermore, only the 'time to next gain' array is serialized. 
 *      System overview: The entire system only requires one DateTime per Player, the 'epoch'. All calculations are based on byte offsets from this 'epoch'.
 *  7/31/2023, Adam
 *      Move massive skill gain systems here
 */

/* Gaining Stats (normal shards)
 * You can gain an unlimited amount of stat points per day, with no fixed intervals between gains.
 * Skills each have primary and secondary stats associated with them. If both stats associated with a skill are set to raise on your character, 
 * the primary stat will increase 75% of the time, while secondary stats will increase 25% of the time. 
 * If the primary stat associated with a skill is locked, however, the secondary stat will be chosen to raise.
 * There is a 1 in 20 chance that you will receive a stat gain whenever a skill is advanced.
 * https://www.uoguide.com/Stats
 *  --
 * Gaining Stats - Siege/Rot
 *  There is no good explanation on how stats should rise under RoT, so we will make one up here:
 *  Since stats only gain when Mobile.Gain(skill) is called, whether or not you actually get the skill gain, 
 *  you have a chance at a stat gain. But since RoT blocks all gains (over skill 70)if you are outside your 'wait' period,
 *  (for example 5 minutes between 70-79.9) or have reached your daily gain maximum (5 skill points for the day,) you cannot gain stats.
 *  Siege: "Players will be allowed to gain 15 stat points a day". https://www.uoguide.com/Siege_Perilous
 *  Given the fact that "There is a 1 in 20 chance that you will receive a stat gain whenever a skill is advanced" it would be 
 *  near impossible to gain your alloted 15 stat points under RoT with the hard limits placed on skill gain.
 *  Our Solution: When Mobile.Gain(skill) is blocked on one of the above limiters, we will call RotStatGainChance() which will
 *  emulate the chance resolution of CheckSkill(). If RotStatGainChance() succeeds, then CheckStatGain(skill) will be called as per usual.
 *  --
 *  As the wait timer is on a "per skill" basis, it's far easier to train multiple skills at once. For example, 
 *  if you cast a spell, you'll be exercising Magery, Evaluating Intelligence, then Meditation and Focus to replace the lost mana.
 *  Note that skills in the ROT ranges that are eligible for a gain WILL gain when you attempt to use them, 
 *  regardless of whether you succeed that attempt or not. This means that skills which normally require "grinding" large amounts of resources 
 *  to train can be done relatively cheaply, as you can potentially gain off every single attempt through good timing.
 *  https://www.uoguide.com/Rate_Over_Time
 */

using Server.Diagnostics;
using Server.Misc;
using Server.Targeting;
using System;
using System.Collections;
using System.IO;

namespace Server.Mobiles
{
    public partial class PlayerMobile : Mobile
    {
        #region Stat Gain
        private byte m_DailyRoTTotalStatIncrease = 0;
        private int GetDailyRotTotalStatIncrease()
        {
            CheckDailyRoTSysGainLimitReset();
            return (int)m_DailyRoTTotalStatIncrease;
        }
        private void IncDailyRotTotalStatGain()
        {
            CheckDailyRoTSysGainLimitReset();
            m_DailyRoTTotalStatIncrease++;
        }
        protected override void GainStat(Stat stat)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                #region Siege Stat Gain
                // We will use the delay specified in RunUO which is two minutes
                if ((LastStatGain + StatGainDelay) >= DateTime.UtcNow)
                    return;

                LastStatGain = DateTime.UtcNow;
                IncreaseStat(stat, false);
                #endregion Siege Stat Gain
            }
            else
            {
                base.GainStat(stat);
            }
        }

        public override double StatGainChance(Skill skill, Stat stat)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                #region Siege Stat Gain

                // Stats (Strength, Dexterity, and Intelligence) will gain a maximum of 6 points per day, total. (For example, a character may gain 3 points in Strength, 2 points in Dexterity, and 1 point in Intelligence, equaling 6 total points gained).
                //  https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                if (GetDailyRotTotalStatIncrease() < MaxSiegeDailyStatGains)
                {
                    bool gain = false;

                    switch (stat)
                    {
                        case Stat.Str: gain = (skill.Info.StrGain > 0.0); break;
                        case Stat.Dex: gain = (skill.Info.DexGain > 0.0); break;
                        case Stat.Int: gain = (skill.Info.IntGain > 0.0); break;
                    }

                    return (gain ? 1.0 : 0.0);
                }
                else
                {
                    // already at their maximum
                    return 0;
                }
                #endregion
            }
            else
            {
                return base.StatGainChance(skill, stat);
            }
        }

        private float m_StrGainPoints;
        private float m_DexGainPoints;
        private float m_IntGainPoints;

        [CommandProperty(AccessLevel.GameMaster)]
        public float StrGainPoints
        {
            get { return m_StrGainPoints; }
            set { m_StrGainPoints = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float DexGainPoints
        {
            get { return m_DexGainPoints; }
            set { m_DexGainPoints = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float IntGainPoints
        {
            get { return m_IntGainPoints; }
            set { m_IntGainPoints = value; }
        }

        protected override void CheckStatGain(Skill skill)
        {
            if (StatGainSystem.Enabled)
            {
                #region Angel Island Stat Gain

                StatType toGain;

                if (StatGainSystem.CheckStatGain(this, skill, out toGain))
                {
                    switch (toGain)
                    {
                        case StatType.Str: GainStat(Stat.Str); break;
                        case StatType.Dex: GainStat(Stat.Dex); break;
                        case StatType.Int: GainStat(Stat.Int); break;
                    }
                }

                #endregion
            }
            else
            {
                base.CheckStatGain(skill);
            }
        }

        protected override void OnAfterStatGain(StatType stat, int oldStatRaw)
        {
            StatGainSystem.OnAfterGain(this, stat, oldStatRaw);
            if (Core.RuleSets.SiegeStyleRules())
                IncDailyRotTotalStatGain();
        }

        #endregion Stat Gain

        #region Skill Gain

        private Hashtable m_AntiMacroTable;

        public Hashtable AntiMacroTable
        {
            get { return m_AntiMacroTable; }
            set { m_AntiMacroTable = value; }
        }

        private SkillNameFlags m_GMHistoryTable;
        public SkillNameFlags GMHistoryTable { get { return m_GMHistoryTable; } set { m_GMHistoryTable = value; } }

        #region Last RoT Skill Gain Times

        private ushort[] m_LastRoTSkillGainTime = new ushort[SkillInfo.Table.Length];    // individual skill daily gain delays
        private DateTime m_SkillGainEpoch;
        public DateTime SkillGainEpoch
        {
            get
            {
                if (m_SkillGainEpoch == DateTime.MinValue)
                    m_SkillGainEpoch = DateTime.UtcNow;
                return m_SkillGainEpoch;
            }
        }
        public DateTime SkillGainEpochRaw { get { return m_SkillGainEpoch; } }

        public DateTime GetLastSkillGain(SkillName skill)
        {
            CheckDailyRoTSysGainLimitReset();   // will allocate if need be
            return GetLastSkillGainRaw(skill);
        }
        public DateTime GetLastSkillGainRaw(SkillName skill)
        {
            int index = (int)skill;

            if (index < 0 || index >= m_LastRoTSkillGainTime.Length)
                return DateTime.MinValue;   // error

            if (m_RoTSkillRolloverCredits[index] != 0)
            {
                Utility.DebugOut(string.Format("Last Gain           :{0}", SkillGainEpoch + TimeSpan.FromMinutes(m_LastRoTSkillGainTime[index])), ConsoleColor.Red);
                Utility.DebugOut(string.Format("Last Gain + Credits :{0}", SkillGainEpoch + TimeSpan.FromMinutes(m_LastRoTSkillGainTime[index] - m_RoTSkillRolloverCredits[index])), ConsoleColor.Red);
            }

            // rather than storing 49 DateTime structs (392 bytes) for each player,
            // we store 49 shorts (98 bytes)
            //  the short can represent 65535 minutes, or 45.5 days
            //  we don't actually need all 45.5 days since we reset daily
            // We use the player's First-Gain-Query as the 'beginning of time'
            return SkillGainEpoch + TimeSpan.FromMinutes(m_LastRoTSkillGainTime[index] - m_RoTSkillRolloverCredits[index]);
        }
        public TimeSpan GetNextSkillGainWait(SkillName skillName)
        {
            Skill skill = Skills[skillName];

            TimeSpan timebetween = TimeSpan.Zero;
            double dontCare = 0.0;
            RoTSkillGainSystem.GetGainInfo(this, skill, ref timebetween, ref dontCare);

            return timebetween;
        }
        public TimeSpan GetNextSkillGain(SkillName skill)
        {
            TimeSpan ts = ((GetLastSkillGain(skill) + GetNextSkillGainWait(skill)) - DateTime.UtcNow);
            if (ts < TimeSpan.Zero)
                return TimeSpan.Zero;
            return ts;
        }
        public TimeSpan GetNextSkillGainRaw(SkillName skill)
        {
            TimeSpan ts = ((GetLastSkillGainRaw(skill) + GetNextSkillGainWait(skill)) - DateTime.UtcNow);
            if (ts < TimeSpan.Zero)
                return TimeSpan.Zero;
            return ts;
        }
        public DateTime GetNextSkillGainTime(SkillName skill)
        {   // timeUntilGain can be negative here
            TimeSpan timeUntilGain = ((GetLastSkillGain(skill) + GetNextSkillGainWait(skill)) - DateTime.UtcNow);
            return DateTime.UtcNow + timeUntilGain;
        }
        public void SetLastSkillGain(SkillName skill)
        {
            CheckDailyRoTSysGainLimitReset();   // will allocate if need be

            int index = (int)skill;

            if (index < 0 || index >= m_LastRoTSkillGainTime.Length)
                return; // error

            // We store the LastSkillGainTime as a (ushort) offset from their First-Gain-Query
            //  we can do this for about 45.5 days.
            //  we don't actually need all 45.5 days since we reset daily
            TimeSpan ts = DateTime.UtcNow - SkillGainEpoch;
            m_LastRoTSkillGainTime[index] = (ushort)ts.TotalMinutes;

            if (m_RoTSkillRolloverCredits[index] != 0)
            {   // we only want to notify the user after the credit was applied.
                //  If we issue this message each time it is checked, it is way too spammy
                LogRoT(RoTLogType.CreditsApplied, this.Skills[skill], new object[] { m_RoTSkillRolloverCredits[index] });

                // clear the rollover credits for this skill
                m_RoTSkillRolloverCredits[index] = 0;
            }
        }

        #endregion Last RoT Skill Gain Times
        #region Special RoT Stat Gain Time
        private DateTime m_specialRoTStatGainTime;      // 'special' waiting period to next gain
        public DateTime SpecialRoTStatGainTime { get { return m_specialRoTStatGainTime; } set { m_specialRoTStatGainTime = value; } }
        #endregion Special RoT Stat Gain Time
        #region Daily RoT Skill Gain Limits

        private byte[] m_DailyRoTSkillGainLimits = new byte[SkillInfo.Table.Length];   // individual skill daily gain caps

        public double GetDailyRoTSkillGain(SkillName skill)
        {
            CheckDailyRoTSysGainLimitReset();
            return GetDailyRoTSkillGainRaw(skill);
        }

        public double GetDailyRoTSkillGainRaw(SkillName skill)
        {
            int index = (int)skill;

            if (index < 0 || index >= m_DailyRoTSkillGainLimits.Length)
                return byte.MinValue;   // error

            return m_DailyRoTSkillGainLimits[index] * 0.1;
        }

        public void SetDailyRoTSkillGain(SkillName skill, byte value)
        {
            CheckDailyRoTSysGainLimitReset();

            int index = (int)skill;

            if (index < 0 || index >= m_DailyRoTSkillGainLimits.Length)
                return;

            m_DailyRoTSkillGainLimits[index] += value;
        }

        private void CheckDailyRoTSysGainLimitReset()
        {   // see if it's time to reset daily limits
            if (DateTime.UtcNow > m_DailyRoTSysGainLimitTime || m_DailyRoTSysGainLimitTime == DateTime.MinValue)
                //reset daily values
                DoDailyRoTSysGainLimitReset();
        }
        private void DoDailyRoTSysGainLimitReset()
        {   // capture 'time spent' credits to be applied to next RoT cycle
            CalcRoTSkillRolloverCredits();
            // reset daily limits
            m_DailyRoTSkillGainLimits = new byte[SkillInfo.Table.Length];   // individual skill daily gain caps
            m_LastRoTSkillGainTime = new ushort[SkillInfo.Table.Length];    // individual skill daily gain delays
            m_DailyRoTTotalStatIncrease = 0;    // total stat daily gain cap
            m_SkillGainEpoch = DateTime.UtcNow;    // base time on which we base our LastRoTSkillGainTime
            // master reset for all RoT skills and stats
            if (Core.Debug && false)
                m_DailyRoTSysGainLimitTime = DateTime.UtcNow.AddMinutes(5.0);
            else
                m_DailyRoTSysGainLimitTime = DateTime.UtcNow.AddDays(1.0);
        }

        private byte[] m_RoTSkillRolloverCredits = new byte[SkillInfo.Table.Length];
        private void CalcRoTSkillRolloverCredits()
        {
            m_RoTSkillRolloverCredits = new byte[SkillInfo.Table.Length];

            foreach (SkillName skill in (SkillName[])Enum.GetValues(typeof(SkillName)))
                if (this.GetDailyRoTSkillGainRaw(skill) != 0.0)
                {   // Ceiling here to give them max credit. Note, with this 'round up', they can gain a full 59 seconds early.
                    double credit = Math.Ceiling((this.GetNextSkillGainWait(skill) - this.GetNextSkillGainRaw(skill)).TotalMinutes);
                    Utility.DebugOut(string.Format("Applying Credit     :{0}", (byte)credit), ConsoleColor.DarkRed);
                    m_RoTSkillRolloverCredits[(int)skill] = (byte)credit;
                    LogRoT(RoTLogType.CreditsIssued, this.Skills[skill], new object[] { (byte)credit });
                }
        }
        public byte GetRoTSkillRolloverCredits(SkillName skill)
        {
            int index = (int)skill;

            if (index < 0 || index >= m_RoTSkillRolloverCredits.Length)
                return 0;   // error

            return m_RoTSkillRolloverCredits[index];
        }
        public TimeSpan DailyRoTSkillGainLimitTimeRemaining()
        {
            return m_DailyRoTSysGainLimitTime - DateTime.UtcNow;
        }
        #endregion Daily RoT Skill Gain Limits
        #region RoTDebug
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool RoTDebug
        {
            get { return ShowRoT; }
            set
            {
                ShowRoT = value;
                if (value)
                    RotFilter = Skills.Swords;
            }
        }
        #endregion RoTDebug

        private SkillGainPoints m_SkillGainPoints;

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillGainPoints SkillGainPoints
        {
            get { return m_SkillGainPoints; }
            set { m_SkillGainPoints = value; }
        }

        protected override bool CheckSkill(Skill skill, double chance, object[] contextObj)
        {
            if (skill == null)
                return false;

            LastSkillUsed = skill.SkillName;
            LastSkillTime = DateTime.UtcNow;

            bool result = base.CheckSkill(skill, chance, contextObj);
            if (result)
            {
                if (chance >= 1.0)
                    LogRoT(RoTLogType.TooEasy, skill, new object[] { chance });
            }
            else
            {
                if (chance < 0.0)
                    LogRoT(RoTLogType.TooHard, skill, new object[] { chance });
            }

            return result;
        }
        private DateTime m_DailyRoTSysGainLimitTime;
        // Stats (Strength, Dexterity, and Intelligence) will gain a maximum of 6 points per day, total.
        // (For example, a character may gain 3 points in Strength, 2 points in Dexterity, and 1 point in Intelligence, equaling 6 total points gained).
        //  https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
        private static int MaxSiegeDailyStatGains { get => Core.SiegeII_CFG ? 10 : 6; }
        protected override bool AllowGain(Skill skill, object[] contextObj)
        {
            if (Core.RuleSets.UsingRoT(m: this, skill: skill))
                // siege perilous skill gain
                return base.AllowGain(skill, contextObj) && RoTSkillGainSystem.AllowGain(this, skill, contextObj);
            else
                // angel island skill gain
                return base.AllowGain(skill, contextObj) && SkillGainSystem.AllowGain(this, skill, contextObj);
        }

        public bool RotStatGainChance(Skill skill)
        {
            if (skill == null)
                return false;
            if (Skills.Cap == 0)
                return false;

            double minSkill = 0.0;
            double maxSkill = 100.0;

            double chance = (skill.Value - minSkill) / (maxSkill - minSkill);

            if (chance < 0.0)
                return false; // Too difficult
            else if (chance >= 1.0)
                return true; // No challenge

            bool success = (chance >= Utility.RandomDouble());
            double gc = GainChance(skill, chance, success);

            if (Alive)
            {
                OnBeforeGain(skill, ref gc);

                if (skill.Base < 10.0 || Utility.RandomDouble() < gc)
                {
                    if (skill.Lock == SkillLock.Up)
                        CheckStatGain(skill);
                }
                else
                {
                }
            }

            return success;
        }
        protected override double GainChance(Skill skill, double chance, bool success)
        {
            if (Core.RuleSets.UsingRoT(m: this, skill: skill))
                return base.GainChance(skill, chance, success);
            else
                return SkillGainSystem.GainChance(this, skill, chance, success);
        }

        protected override void OnBeforeGain(Skill skill, ref double gc)
        {
            if (Core.RuleSets.UsingRoT(m: this, skill: skill) && this.Player)
                LogRoT(RoTLogType.ChanceToGain0, skill, new object[] { gc });
            else
                SkillGainSystem.OnBeforeGain(this, skill, ref gc);
        }

        protected override void OnAfterGain(Skill skill, double oldBase)
        {
            if (Core.RuleSets.UsingRoT(m: this, skill: skill) && this.Player)
            {
                if (skill.Base >= 70)
                {
                    SetDailyRoTSkillGain(skill.SkillName, (byte)(skill.BaseFixedPoint - oldBase));
                    SetLastSkillGain(skill.SkillName);
                }

                LogRoT(RoTLogType.ChanceToGain1, skill, new object[] { skill.BaseFixedPoint != oldBase });
            }
            else
                SkillGainSystem.OnAfterGain(this, skill, oldBase);
        }

        protected override void OnAfterFailedGain(Skill skill, double gc)
        {
            if (Core.RuleSets.UsingRoT(m: this, skill: skill) && this.Player)
                LogRoT(RoTLogType.ChanceToGain2, skill, new object[] { gc });
        }

        #endregion Skill Gain

        #region Skill Gain Return-Over-Time (RoT) Logging System
        private bool m_showRoT = false;
        private Skill m_RotFilter;// = Skills.Swords;
        public Skill RotFilter { get { return m_RotFilter; } set { m_RotFilter = value; } }
        public bool ShowRoT { get { return m_showRoT; } set { m_showRoT = value; } }
        public enum RoTLogType
        {
            None = 0,
            ChanceToGain0,
            ChanceToGain1,
            ChanceToGain2,
            MaxSkillGain,
            NormalSkillGain,
            AntiMacro,
            InsufficientWait,
            ReadyToGain,
            TooEasy,
            TooHard,
            WillFailGain,
            CreditsIssued,
            CreditsApplied,
        }
        public void LogRotOut(string text)
        {
            LogRotOut(0x3B2, text);
        }
        public void LogRotOut(int hue, string text)
        {
            if (m_showRoT)
                this.SendMessage(hue, text);
            // We write this to Saves since it will get very big and we rotate logs out of existence after time.
            LogHelper logger = new LogHelper(Path.Combine(Core.BaseDirectory, "Saves/Logs", "RotSkillGain.log"), false, true, true);
            logger.Log(LogType.Mobile, this, text);
            logger.Finish();
            return;
        }
        public void LogRoT(RoTLogType logType, Skill skill, object[] args)
        {
            if (!Core.RuleSets.SiegeStyleRules()) return;
            if (!this.Player) return;
            if (skill == m_RotFilter)
            {
                switch (logType)
                {
                    case RoTLogType.None: break;
                    case RoTLogType.AntiMacro:
                        {
                            LogRotOut(string.Format("ROT: skill/value: {0}/{1} - using normal skillgain (<70.0) - antimacro code hit", skill.Name, skill.Base));
                            break;
                        }
                    case RoTLogType.NormalSkillGain:
                        {
                            LogRotOut(string.Format("ROT: skill/value: {0}/{1} - using normal skillgain (<70.0)", skill.Name, skill.Base));
                            break;
                        }
                    case RoTLogType.InsufficientWait:
                        {
                            LogRotOut(string.Format("ROT: Skill {0} failed to gain due to insufficient wait. {1:0.00} minutes remaining", skill.Name, (double)args[0]));
                            break;
                        }
                    case RoTLogType.ReadyToGain:
                        {
                            LogRotOut(0x35, string.Format("ROT: Skill {0} ready to gain", skill.Name));
                            break;
                        }
                    case RoTLogType.ChanceToGain0:
                        {
                            LogRotOut(0x35, string.Format("ROT: Skill {0} chance to gain {1:0.00}",
                                skill.Name, (double)args[0]));
                            break;
                        }
                    case RoTLogType.ChanceToGain1:
                        {
                            if (args[0] is bool gained && gained == true)
                                LogRotOut(0x40, string.Format("ROT: Skill {0} gain ok", skill.Name));
                            else
                                LogRotOut(0x35, string.Format("ROT: Skill {0} failed to gain", skill.Name));
                            break;
                        }
                    case RoTLogType.ChanceToGain2:
                        {
                            LogRotOut(0x35, string.Format("ROT: Skill {0} failed {1:0.00}% chance to gain",
                                skill.Name, (double)args[0]));
                            break;
                        }
                    case RoTLogType.MaxSkillGain:
                        {
                            TimeSpan ts = DailyRoTSkillGainLimitTimeRemaining();
                            LogRotOut(0x22, string.Format
                                ("ROT: Skill {0} failed to gain due to hitting max daily skillgain. {1} hours {2} minutes remaining",
                                skill.Name, ts.Hours, ts.Minutes));
                            break;
                        }
                    case RoTLogType.TooEasy:
                        {
                            LogRotOut(0x35, string.Format("ROT: Skill {0} chance to gain {1:0.00}, too easy",
                                skill.Name, (double)args[0]));
                            break;
                        }
                    case RoTLogType.TooHard:
                        {
                            LogRotOut(0x35, string.Format("ROT: Skill {0} chance to gain {1:0.00}, too hard",
                                skill.Name, (double)args[0]));
                            break;
                        }
                    case RoTLogType.WillFailGain:
                        {
                            LogRotOut(0x22, string.Format("ROT: Fail to gain in {0}, reason: {1}",
                                skill.Name, (string)args[0]));
                            break;
                        }
                    case RoTLogType.CreditsIssued:
                        {
                            LogRotOut(0x40, string.Format("ROT: {1} minutes of credits issued for skill: {0}",
                                skill.Name, (byte)args[0]));
                            break;
                        }
                    case RoTLogType.CreditsApplied:
                        {
                            LogRotOut(0x40, string.Format("ROT: {1} minutes of credits applied to skill: {0}",
                                skill.Name, (byte)args[0]));
                            break;
                        }
                }

            }
        }
        #endregion Skill Gain Return-Over-Time (RoT) Logging System

        #region Commands

        #region RoTInfo Command
        private static Memory m_RotInfoSpam = new Memory();
        public static void RoTInfo(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from.AccessLevel > AccessLevel.Player)
            {
                e.Mobile.SendMessage("Target player...");
                e.Mobile.Target = new RoTInfoTarget(e.Arguments); // 
            }
            else
            {
                if (m_RotInfoSpam.Recall(e.Mobile))
                    e.Mobile.SendMessage("You can call RotInfo once every 7 seconds.");
                else
                {
                    m_RotInfoSpam.Remember(e.Mobile, 7);
                    RotInfo(from, null, e.Arguments);
                }
            }
        }
        private static void RotInfo(Mobile from, Mobile to, string[] arguments)
        {
            if (to == null)
                to = from;
            try
            {
                #region Process Arguments
                bool startEnd = false;
                bool skills = false;
                bool reset = false;
                bool debugRot = false;
                bool nextSkillGain = false;
                bool razorFormat = false;
                bool credits = false;
                bool skillCaps = false;
                bool statCaps = false;
                if (arguments != null)
                {
                    foreach (string arg in arguments)
                        if (arg.Equals("se", StringComparison.OrdinalIgnoreCase))
                            startEnd = true;
                        else if (arg.Equals("sk", StringComparison.OrdinalIgnoreCase))
                            skills = true;
                        else if (arg.Equals("nsg", StringComparison.OrdinalIgnoreCase))
                            nextSkillGain = true;
                        else if (arg.Equals("reset", StringComparison.OrdinalIgnoreCase) && to.AccessLevel >= AccessLevel.Administrator)
                            reset = true;
                        else if (arg.Equals("debug", StringComparison.OrdinalIgnoreCase) && to.AccessLevel >= AccessLevel.Administrator)
                            debugRot = true;
                        else if (arg.Equals("credits", StringComparison.OrdinalIgnoreCase) && to.AccessLevel >= AccessLevel.Administrator)
                            credits = true;
                        else if (arg.Equals("skillCaps", StringComparison.OrdinalIgnoreCase) && to.AccessLevel >= AccessLevel.Administrator)
                            skillCaps = true;
                        else if (arg.Equals("statCaps", StringComparison.OrdinalIgnoreCase) && to.AccessLevel >= AccessLevel.Administrator)
                            statCaps = true;
                }

                if (startEnd || skills || nextSkillGain)
                    razorFormat = true;

                #endregion Process Arguments
                if (from is PlayerMobile pm)
                {
                    if (reset)
                    {   // admin only functionality
                        pm.DoDailyRoTSysGainLimitReset();
                        to.SendMessage("Reset okay.");
                        return;
                    }

                    if (debugRot)
                    {   // admin only functionality
                        pm.SetPlayerBool(PlayerBoolTable.DebugRot, !pm.GetPlayerBool(PlayerBoolTable.DebugRot));
                        to.SendMessage("Debug Rot set to {0}.", pm.GetPlayerBool(PlayerBoolTable.DebugRot));
                        return;
                    }

                    if (credits)
                    {   // admin only functionality
                        foreach (SkillName skill in (SkillName[])Enum.GetValues(typeof(SkillName)))
                            if (pm.GetRoTSkillRolloverCredits(skill) != 0)
                                // how much credit you've earned for this skill (in minutes)
                                to.SendMessage("{0} : {1}", SkillInfo.Table[(int)skill].Name, pm.GetRoTSkillRolloverCredits(skill));
                        return;
                    }

                    if (skillCaps)
                    {
                        // admin only functionality
                        string reason = null;
                        foreach (SkillName skill in (SkillName[])Enum.GetValues(typeof(SkillName)))
                            if (RoTSkillGainSystem.WillFailSkillGain(pm, pm.Skills[(int)skill], ref reason) == true)
                                // how much credit you've earned for this skill (in minutes)
                                to.SendMessage("{0} : {1}", SkillInfo.Table[(int)skill].Name, reason);
                        return;
                    }

                    if (statCaps)
                    {
                        // admin only functionality
                        foreach (Stat stat in (Stat[])Enum.GetValues(typeof(Stat)))
                            to.SendMessage("{0} : CanRaise {1}, CanLower {2}", stat, pm.CanRaise(stat), pm.CanLower(stat));
                        return;
                    }

                    pm.CheckDailyRoTSysGainLimitReset();

                    if (razorFormat)
                    {
                        if (startEnd)
                        {
                            if (pm.SkillGainEpochRaw == DateTime.MinValue)
                                to.SendMessage("0 : 0");
                            else
                            {   // when your rot cycle started, time when your rot cycle ends
                                to.SendMessage("{0} : {1}", pm.SkillGainEpochRaw, pm.DailyRoTSysGainLimitTime);
                            }
                        }

                        if (skills)
                        {
                            foreach (SkillName skill in (SkillName[])Enum.GetValues(typeof(SkillName)))
                                if (pm.GetDailyRoTSkillGain(skill) != 0.0)
                                    // how much you've gained in this skill, the last gain time
                                    to.SendMessage("{0} : {1:#,0.000} : {2}", SkillInfo.Table[(int)skill].Name, pm.GetDailyRoTSkillGain(skill), pm.GetLastSkillGain(skill));
                        }

                        if (nextSkillGain)
                        {
                            foreach (SkillName skill in (SkillName[])Enum.GetValues(typeof(SkillName)))
                                if (pm.GetDailyRoTSkillGain(skill) != 0.0)
                                    // how long until the next skill gain
                                    to.SendMessage("{0} : {1:#,0.00}", SkillInfo.Table[(int)skill].Name, pm.GetNextSkillGain(skill).TotalMinutes);
                        }
                    }
                    else if (pm.SkillGainEpochRaw == DateTime.MinValue)
                    {
                        to.SendMessage("You've not yet begun a RoT cycle.");
                        to.SendMessage("(try using a skill.)");
                    }
                    else
                    {
                        to.SendMessage("RoT begin: {0} (GMT)", pm.SkillGainEpochRaw);                           // when your rot cycle started
                        to.SendMessage("RoT end: {0} (GMT)", pm.DailyRoTSysGainLimitTime);                      // time when your rot cycle ends
                        to.SendMessage("RoT time remaining: {0}", pm.DailyRoTSkillGainLimitTimeRemaining());    // how much time is left in your rot cycle
                        to.SendMessage("RoT stat gains this period: {0} ({1} max)", pm.GetDailyRotTotalStatIncrease(), MaxSiegeDailyStatGains);
                        foreach (SkillName skill in (SkillName[])Enum.GetValues(typeof(SkillName)))             // how much you've gained in this skill, the last gain time
                            if (pm.GetDailyRoTSkillGain(skill) != 0.0)
                                to.SendMessage("Last skill gain snapshot for: {0}: {1:#,0.000}: {2} (GMT)", SkillInfo.Table[(int)skill].Name, pm.GetDailyRoTSkillGain(skill), pm.GetLastSkillGain(skill));

                        foreach (SkillName skill in (SkillName[])Enum.GetValues(typeof(SkillName)))
                            if (pm.GetDailyRoTSkillGain(skill) != 0.0)                                          // how long until the next skill gain
                                to.SendMessage("Next skill gain for {0} in {1:#,0.00} minutes", SkillInfo.Table[(int)skill].Name, pm.GetNextSkillGain(skill).TotalMinutes);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public class RoTInfoTarget : Target
        {
            string[] m_Arguments = null;
            public RoTInfoTarget(string[] arguments)
                : base(17, true, TargetFlags.None)
            {
                m_Arguments = arguments;
            }

            protected override void OnTarget(Mobile to, object target)
            {
                if (target is PlayerMobile from)
                {
                    RotInfo(from, to, m_Arguments);
                }
                else
                {
                    to.SendMessage("That is not a PlayerMobile.");
                    return;
                }
            }
        }
        public DateTime DailyRoTSysGainLimitTime { get { return m_DailyRoTSysGainLimitTime; } }
        #endregion RoTInfo Command
        #region RoT Status Command
        public static void RoTStatus(CommandEventArgs e)
        {
            PlayerMobile from = e.Mobile as PlayerMobile;
            bool sp = Core.RuleSets.SiegeStyleRules();
            bool prod = Core.ReleasePhase == ReleasePhase.Production;
            bool green_acres = Utility.TeleporterRuleHelpers.InRegion(e.Mobile.Location, e.Mobile.Map, "Green Acres");
            bool britain_graveyard = Utility.TeleporterRuleHelpers.InRegion(e.Mobile.Location, e.Mobile.Map, "Britain Graveyard");
            bool TC = Core.RuleSets.TestCenterRules();
            bool okay = (sp && !prod) || (sp && green_acres) || (sp && TC) || (sp && britain_graveyard);
            if (!okay)
            { from.SendMessage("You cannot enable/disable RoT Status on this shard."); return; }
            if (string.IsNullOrEmpty(e.ArgString)) { from.SendMessage("RoT Status disabled."); from.RoTDebug = false; return; }

            SkillName index;
            try
            {
                int score;
                string real_name = Engines.IntelligentDialogue.Levenshtein.BestEnumMatch(typeof(SkillName), e.ArgString, out score);
                if (score > 5) // a 'distance' of > 5 is likely bad input (5 is a heuristically derived value)
                {
                    RoTStatusUsage(from);
                    return;
                }
                index = (SkillName)Enum.Parse(typeof(SkillName), real_name, true);
            }
            catch
            {

                RoTStatusUsage(from);
                return;
            }

            Skill skill = from.Skills[index];

            if (skill != null)
            {
                from.RoTDebug = true;
                from.RotFilter = skill;
                from.SendMessage("RoT Status enabled for {0}.", skill.SkillName);
            }
            else
            {
                RoTStatusUsage(from);
            }
        }
        private static void RoTStatusUsage(Mobile from)
        {
            from.SendMessage("You have specified an invalid skill to track.");
            from.SendMessage("Usage [RotStatus <skill to track>.");
            string names = string.Join(", ", Enum.GetNames(typeof(SkillName)));
            from.SendMessage(names);
            return;
        }
        #endregion RoT Status Command

        #endregion Commands
    }
}