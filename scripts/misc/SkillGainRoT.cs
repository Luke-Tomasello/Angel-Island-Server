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

/* Misc/SkillGainRoT.cs
 * CHANGELOG:
 *  11/30/22, Adam
 *		Initial version.
 */

using Server.Mobiles;
using System;
using static Server.Mobiles.PlayerMobile;

namespace Server.Misc
{
    public static class RoTSkillGainSystem
    {
        public static void GetGainInfo(PlayerMobile pm, Skill skill, ref TimeSpan timebetween, ref double maxgain)
        {
            if (PublishInfo.Publish >= 13.6)
            {
                /*
                 * Stat/ROT Changes
                 * Stats (Strength, Dexterity, and Intelligence) will gain a maximum of 6 points per day, total. (For example, a character may gain 3 points in Strength, 2 points in Dexterity, and 1 point in Intelligence, equaling 6 total points gained).
                 * A variation of the “Rate Over Time” (ROT) system will be returning to Siege Perilous, and will apply to all skills:
                 * Skill points for skills under 70 points will gain as normal shards (including “power hour” point gain capability).
                 * Skill points for skills between 70 and 79.9 points will gain a maximum of 3.6 points total per day, with a minimum of 20 minutes between point gained.
                 * Skill points for skills between 80 and 98.9 points will gain a maximum of 2 points total per day, with a minimum of 40 minutes between point gained.
                 * Skill points for skills 99.0 points and up will gain a maximum of 2 points total per day, with a minimum of 60 minutes between point gained.
                 * https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                 */
                if (pm.GetPlayerBool(PlayerBoolTable.DebugRot))
                {
                    timebetween = TimeSpan.FromMinutes(1.0);
                    maxgain = 5;
                }
                else if (skill.Base >= 70 && skill.Base <= 79.9)
                {
                    timebetween = TimeSpan.FromMinutes(20.0);
                    maxgain = 3.6;
                }
                else if (skill.Base >= 80 && skill.Base <= 98.9)
                {
                    timebetween = TimeSpan.FromMinutes(40.0);
                    maxgain = 2.0;
                }
                else if (skill.Base >= 99.0)
                {
                    timebetween = TimeSpan.FromMinutes(60.0);
                    maxgain = 2.0;
                }
            }
            else
            {
                /*
                 * Skills & Stats
                 * Rate Over Time skill gain
                 * Skill points for skills less than 70 points will gain as normal shards.
                 * Skill points for skills between 70 and 79.9 points will have a delay of 5 minutes between points gained.
                 * Skill points for skills between 80 and 89.9 will have a delay of 8 minutes between points gained.
                 * Skill points for skills between 90 and 99.9 will have a delay of 12 minutes between points gained.
                 * Skill points for skills between 100 and 109.9 will have a delay of 15 minutes between points gained.
                 * Skill points for skills between 110 and 120 will have a delay of 15 minutes between points gained.
                 * https://www.uoguide.com/Siege_Perilous
                 * -- uothief.com adds this --
                 * Skills between 70.0 – 79.9 can gain 0.1 every 5 minutes, for a total of 5.0 per day.
                 * Skills between 80.0 – 89.9 can gain 0.1 every 8 minutes, for a total of 4.0 per day.
                 * Skills between 90.0 – 99.9 can gain 0.1 every 12 minutes, for a total of 3.0 per day.
                 * Skills between 100.0 – 109.9 can gain 0.1 every 15 minutes, for a total of 3.0 per day.
                 * Skills between 110.0 – 120.0 can gain 0.1 every 15 minutes, for a total of 2.0 per day.
                 * http://uothief.com/faq/7pt5.html
                 */

                if (pm.GetPlayerBool(PlayerBoolTable.DebugRot))
                {
                    timebetween = TimeSpan.FromMinutes(2.0);
                    maxgain = 5;
                }
                else if (skill.Base >= 70 && skill.Base <= 79.9)
                {
                    timebetween = TimeSpan.FromMinutes(5.0);
                    maxgain = 5.0;
                }
                else if (skill.Base >= 80 && skill.Base <= 89.9)
                {
                    timebetween = TimeSpan.FromMinutes(8.0);
                    maxgain = 4.0;
                }
                else if (skill.Base >= 90 && skill.Base <= 99.9)
                {
                    timebetween = TimeSpan.FromMinutes(12.0);
                    maxgain = 3.0;
                }
                else if (skill.Base >= 100 && skill.Base <= 109.9)
                {
                    timebetween = TimeSpan.FromMinutes(15.0);
                    maxgain = 3.0;
                }
                else if (skill.Base >= 110 && skill.Base <= 120.0)
                {
                    timebetween = TimeSpan.FromMinutes(15.0);
                    maxgain = 2.0;
                }
            }

            return;
        }
        public static bool AllowGain(PlayerMobile pm, Skill skill, object[] contextObj)
        {
            #region Siege Skill Gain

            // on siege, use anti-macro code if < 70
            if (skill.Base < 70.0 && !SkillGainSystem.AntiMacroCheck(pm, skill, contextObj))
            {
                pm.LogRoT(RoTLogType.AntiMacro, skill, null);
                return false;
            }

            if (skill.Base < 70.0)
            {   //under 70, use normal gain
                pm.LogRoT(RoTLogType.NormalSkillGain, skill, null);
                return true;
            }
            else
            {
                TimeSpan timebetween = TimeSpan.Zero;
                double maxgain = 0.0;

                //calc time till next gain for skill and maxgain for that level
                GetGainInfo(pm, skill, ref timebetween, ref maxgain);
                bool maxGainReached = pm.GetDailyRoTSkillGain(skill.SkillName) >= maxgain;
                bool failWait = pm.GetNextSkillGainTime(skill.SkillName) > DateTime.UtcNow;
                bool failAny = false;  // under some circumstances, we still allow RotStatGainChance() on fail

                //make sure we've not hit max gain for the day
                if (!failAny && maxGainReached)
                {
                    pm.LogRoT(RoTLogType.MaxSkillGain, skill, null);
                    failAny = true;
                }

                //make sure we're passed the allotted time
                if (!failAny && failWait)
                {
                    pm.LogRoT(RoTLogType.InsufficientWait, skill,
                        new object[] { (pm.GetNextSkillGainTime(skill.SkillName) - DateTime.UtcNow).TotalMinutes });
                    failAny = true;
                }

                string reason = null;
                if (WillFailSkillGain(pm, skill, ref reason))
                {
                    pm.LogRoT(RoTLogType.WillFailGain, skill,
                        new object[] { reason });
                    failAny = true;
                }

                // still allow a stat gain chance even if the skill gain is blocked due to dailyRoTSkillGain >= maxgain.
                //  but they have waited the allotted time for a gain
                if (maxGainReached && DateTime.UtcNow > pm.SpecialRoTStatGainTime)
                {
                    pm.RotStatGainChance(skill);
                    pm.SpecialRoTStatGainTime = DateTime.UtcNow + timebetween;
                }

                if (failAny)
                    return false;

                pm.LogRoT(RoTLogType.ReadyToGain, skill, null);

                //we've already checked (in Mobile.CheckSkill) that we are working on something that isn't too easy or too hard, so gain it.
                return true;
            }
            #endregion
        }

        public static bool WillFailSkillGain(PlayerMobile pm, Skill skill, ref string reason)
        {
            bool willFailGain = true;
            bool foundToLower = false;
            bool needToLower = false;
            bool totalSkillCap = false;
            if (skill.Base < skill.Cap && skill.Lock == SkillLock.Up)
            {
                int toGain = 1;

                if (skill.Base <= 10.0)
                    toGain = Utility.Random(4) + 1;

                if ((pm.Skills.Total / pm.Skills.Cap) >= Utility.RandomDouble())
                {
                    needToLower = true;
                    for (int i = 0; i < pm.Skills.Length; ++i)
                    {
                        Skill toLower = pm.Skills[i];

                        if (toLower != skill && toLower.Lock == SkillLock.Down && toLower.BaseFixedPoint >= toGain)
                        {
                            foundToLower = true;
                            break;
                        }
                    }
                }

                if (!((pm.Skills.Total + toGain) <= pm.Skills.Cap))
                {
                    reason = "at total skill cap";
                    totalSkillCap = true;
                }

                if (!totalSkillCap)
                    willFailGain = false;
                else if (!needToLower || (needToLower && foundToLower))
                    willFailGain = false;
            }
            else
            {
                if (skill.Base >= skill.Cap)
                    reason = "at skill cap";

                if (skill.Lock != SkillLock.Up)
                    reason = "skill lock needs to be up";
            }

            return willFailGain;
        }
    }
}