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

/* Misc/SkillGain.cs
 * CHANGELOG:
 *  8/28/22, Yoar
 *      Cannot gain skills while in faction stat loss.
 *  10/28/21, Yoar
 *		Initial version.
 */

using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Misc
{
    public static class SkillGainSystem
    {
        public static double Bonus = 1.0 / 8.0;
        public static double CapWeight = 1.0 / 3.0;
        public static double SuccessWeight = 2.0 / 3.0;
        public static double GlobalFactor = 1.0;

        public static bool GainWaits = false; // enable/disable gain waits
        public static double GSGG = 4.0; // total gain wait per skill range (AI only)

        public static bool PointSystem = true; // enable/disable skill gain point system

        public static bool AntiMacroCode = false; // enable/disable anti-macro code
        public static SkillNameFlags UseAntiMacro = SkillNameFlags._None; // which skills use anti-macro code?
        public static TimeSpan AntiMacroExpire = TimeSpan.FromMinutes(5.0); // how long do we remember targets/locations?
        public static int AntiMacroAllowance = 3; // how many times may we use the same location/target for gain
        public static int AntiMacroLocationSize = 5; // the size of each location, make this smaller so players dont have to move as far

        private static readonly List<SkillGainModifier> m_Modifiers = new List<SkillGainModifier>();

        public static List<SkillGainModifier> Modifiers { get { return m_Modifiers; } }

        public static void Configure()
        {
            EventSink.WorldSave += new WorldSaveEventHandler(EventSink_WorldSave);
            EventSink.WorldLoad += new WorldLoadEventHandler(EventSink_WorldLoad);
        }

        private static void EventSink_WorldSave(WorldSaveEventArgs e)
        {
            Save();
        }

        private static void EventSink_WorldLoad()
        {
            if (!Load())
                InitModifiers();
        }

        private static void InitModifiers()
        {
            // the following modifiers emulate the old gain waits, assuming 10s skill delay
            m_Modifiers.Add(new SkillGainModifier(SkillNameFlags._Mask, 900, 949, 0.55));
            m_Modifiers.Add(new SkillGainModifier(SkillNameFlags._Mask, 950, 989, 0.45));
            m_Modifiers.Add(new SkillGainModifier(SkillNameFlags._Mask, 990, 999, 0.12));

            // let's add 9 more to play with
            for (int i = 0; i < 9; i++)
                m_Modifiers.Add(new SkillGainModifier());
        }

        public static bool AllowGain(PlayerMobile pm, Skill skill, object[] contextObj)
        {
            if (/*Core.RuleSets.AOSRules() &&*/ Faction.InSkillLoss(pm))  //Changed some time between the introduction of AoS and SE.
                return false;

            if (!AntiMacroCheck(pm, skill, contextObj))
                return false;

            if (GainWaits)
            {
                double totalDesiredWait = 3600.0 * GSGG;

                TimeSpan wait;

                if (skill.Base > 80.0 && skill.Base < 90.0)
                    wait = TimeSpan.FromSeconds((totalDesiredWait / 4) / 100);
                else if (skill.Base >= 90.0 && skill.Base < 95.0)
                    wait = TimeSpan.FromSeconds((totalDesiredWait / 4) / 50);
                else if (skill.Base >= 95.0 && skill.Base < 99.0)
                    wait = TimeSpan.FromSeconds((totalDesiredWait / 4) / 40);
                else if (skill.Base >= 99.0)
                    wait = TimeSpan.FromSeconds((totalDesiredWait / 4) / 10);
                else //skill is <= 80.0, ignore it
                    wait = TimeSpan.FromSeconds(0.1);

                if ((pm.GetLastSkillGain(skill.SkillName) + wait) > DateTime.UtcNow)
                    return false;

                // looks like we can gain, set last gain
                pm.SetLastSkillGain(skill.SkillName);
            }

            return true;
        }

        public static double GainChance(PlayerMobile pm, Skill skill, double chance, bool success)
        {
            double gc = Bonus;

            gc += CapWeight * 0.75 * (skill.Cap - skill.Base) / skill.Cap;

            gc += SuccessWeight * 0.75 * (1.0 - chance) * (success ? 0.5 : 0.2);

            gc *= GlobalFactor;

            gc *= skill.Info.GainFactor;

            foreach (SkillGainModifier mod in m_Modifiers)
            {
                if ((mod.MinFixed != 0 && skill.BaseFixedPoint < mod.MinFixed) ||
                    (mod.MaxFixed != 0 && skill.BaseFixedPoint >= mod.MaxFixed) ||
                    (mod.Flags & skill.SkillName.GetFlag()) == 0)
                    continue;

                gc *= mod.Scalar;
            }

            if (gc < 0.01)
                gc = 0.01;

            return gc;
        }

        public static void OnBeforeGain(PlayerMobile pm, Skill skill, ref double gc)
        {
            if (skill.BaseFixedPoint < skill.CapFixedPoint && skill.Lock == SkillLock.Up)
            {
                if (PointSystem && skill.BaseFixedPoint >= 800)
                    AwardPoints(pm, skill.SkillName, ref gc);

#if DEBUG
                if (pm.AccessLevel >= AccessLevel.GameMaster)
                    pm.SendMessage("Skill gain ({0}): {1:F3}", skill.Name, gc);
#endif
            }
        }

        public static void OnAfterGain(PlayerMobile pm, Skill skill, double oldBase)
        {
            if (PointSystem)
                ResetPoints(pm, skill.SkillName); // we've gained, reset skill gain points
        }

        public static void AwardPoints(PlayerMobile pm, SkillName skill, ref double gc)
        {
            if (gc < 0.01 || gc >= 0.30)
                return;

            if (pm.SkillGainPoints == null)
                pm.SkillGainPoints = new SkillGainPoints();

            double exponent;

            if (gc < 0.10)
                exponent = 1.700;
            else if (gc < 0.20)
                exponent = 1.450;
            else
                exponent = 1.275;

            double pts = 0.5 * Math.Pow(gc, exponent);

            /* Keep incrementing our skill gain points until we gain. The skill gain chance
			 * equals our accumulated skill gain points. Since we're incrementing our points,
			 * we're guaranteed to gain if we keep trying.
			 * 
			 * This procedure reduces the variance in skill gain times while approximately
			 * preserving the expected skill gain time.
			 */
            gc = (pm.SkillGainPoints[skill] += (float)pts);
        }

        public static void ResetPoints(PlayerMobile pm, SkillName skill)
        {
            if (pm.SkillGainPoints != null)
                pm.SkillGainPoints[skill] = 0.0f;
        }

        #region Anti-Macro

        public static bool AntiMacroCheck(PlayerMobile pm, Skill skill, object[] contextObj)
        {
            if (!AntiMacroCode || (UseAntiMacro & skill.SkillName.GetFlag()) == 0)
                return true;

            if (contextObj == null || contextObj[1] == null || pm.AntiMacroTable == null /*|| pm.AccessLevel != AccessLevel.Player*/)
                return true;

            Hashtable t = (Hashtable)pm.AntiMacroTable[skill];

            if (t == null)
                pm.AntiMacroTable[skill] = t = new Hashtable();

            CountAndTimeStamp count = (CountAndTimeStamp)t[contextObj[1]];

            if (count != null)
            {
                if (count.TimeStamp + AntiMacroExpire <= DateTime.UtcNow)
                {
                    count.Count = 1;
                    return true;
                }

                return ++count.Count <= AntiMacroAllowance;
            }

            t[contextObj[1]] = count = new CountAndTimeStamp();

            count.Count = 1;
            return true;
        }

        public static void CleanupAntiMacroTable(PlayerMobile pm)
        {
            if (pm.AntiMacroTable == null)
                return;

            foreach (Hashtable t in pm.AntiMacroTable.Values)
            {
                ArrayList toRemove = new ArrayList();

                foreach (CountAndTimeStamp time in t.Values)
                {
                    if (time.TimeStamp + AntiMacroExpire <= DateTime.UtcNow)
                        toRemove.Add(time);
                }

                for (int i = 0; i < toRemove.Count; ++i)
                    t.Remove(toRemove[i]);
            }
        }

        private class CountAndTimeStamp
        {
            private int m_Count;
            private DateTime m_Stamp;

            public DateTime TimeStamp { get { return m_Stamp; } }

            public int Count
            {
                get { return m_Count; }
                set { m_Count = value; m_Stamp = DateTime.UtcNow; }
            }

            public CountAndTimeStamp()
            {
            }
        }

        #endregion

        #region Data Management

        private const string CfgFileName = @"Saves\SkillGainConfig.xml";

        private static void Save()
        {
            Console.WriteLine("Skill Gain Config Saving...");

            try
            {
                XmlTextWriter writer = new XmlTextWriter(CfgFileName, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("SkillGainConfig");
                writer.WriteAttributeString("version", "0");

                try
                {
                    writer.WriteElementString("Bonus", Bonus.ToString());
                    writer.WriteElementString("CapWeight", CapWeight.ToString());
                    writer.WriteElementString("SuccessWeight", SuccessWeight.ToString());
                    writer.WriteElementString("GlobalFactor", GlobalFactor.ToString());

                    writer.WriteElementString("GainWaits", GainWaits.ToString());
                    writer.WriteElementString("GSGG", GSGG.ToString());

                    writer.WriteElementString("PointSystem", PointSystem.ToString());

                    writer.WriteElementString("AntiMacroCode", AntiMacroCode.ToString());
                    writer.WriteElementString("UseAntiMacro", UseAntiMacro.ToString());
                    writer.WriteElementString("AntiMacroExpire", AntiMacroExpire.ToString());
                    writer.WriteElementString("AntiMacroAllowance", AntiMacroAllowance.ToString());
                    writer.WriteElementString("AntiMacroLocationSize", AntiMacroLocationSize.ToString());

                    foreach (SkillGainModifier mod in m_Modifiers)
                        mod.Save(writer);
                }
                finally { writer.WriteEndDocument(); }

                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("SkillGainSystem Error: {0}", e);
            }
        }

        private static bool Load()
        {
            Console.WriteLine("Skill Gain Config Loading...");

            try
            {
                if (!File.Exists(CfgFileName))
                    return false; // nothing to read

                XmlTextReader reader = new XmlTextReader(CfgFileName);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int verison = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("SkillGainConfig");

                switch (verison)
                {
                    case 0:
                        {
                            Bonus = Double.Parse(reader.ReadElementString("Bonus"));
                            CapWeight = Double.Parse(reader.ReadElementString("CapWeight"));
                            SuccessWeight = Double.Parse(reader.ReadElementString("SuccessWeight"));
                            GlobalFactor = Double.Parse(reader.ReadElementString("GlobalFactor"));

                            GainWaits = Boolean.Parse(reader.ReadElementString("GainWaits"));
                            GSGG = Double.Parse(reader.ReadElementString("GSGG"));

                            PointSystem = Boolean.Parse(reader.ReadElementString("PointSystem"));

                            AntiMacroCode = Boolean.Parse(reader.ReadElementString("AntiMacroCode"));
                            UseAntiMacro = (SkillNameFlags)Enum.Parse(typeof(SkillNameFlags), reader.ReadElementString("UseAntiMacro"));
                            AntiMacroExpire = TimeSpan.Parse(reader.ReadElementString("AntiMacroExpire"));
                            AntiMacroAllowance = Int32.Parse(reader.ReadElementString("AntiMacroAllowance"));
                            AntiMacroLocationSize = Int32.Parse(reader.ReadElementString("AntiMacroLocationSize"));

                            while (reader.LocalName == "SkillGainModifier")
                                m_Modifiers.Add(new SkillGainModifier(reader));

                            break;
                        }
                }

                reader.ReadEndElement();

                reader.Close();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("SkillGainSystem Error: {0}", e);
                return false;
            }
        }

        #endregion
    }

    [NoSort]
    [PropertyObject]
    public class SkillGainModifier
    {
        private SkillNameFlags m_Flags;
        private int m_MinFixed;
        private int m_MaxFixed;
        private double m_Scalar;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillNameFlags Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MinFixed
        {
            get { return m_MinFixed; }
            set { m_MinFixed = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int MaxFixed
        {
            get { return m_MaxFixed; }
            set { m_MaxFixed = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double Scalar
        {
            get { return m_Scalar; }
            set { m_Scalar = value; }
        }

        public SkillGainModifier()
            : this(SkillNameFlags._None, 0, 1000, 1.0)
        {
        }

        public SkillGainModifier(SkillNameFlags flags, int minFixed, int maxFixed, double scalar)
        {
            m_Flags = flags;
            m_MinFixed = minFixed;
            m_MaxFixed = maxFixed;
            m_Scalar = scalar;
        }

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("SkillGainModifier");
            writer.WriteAttributeString("version", "0");

            try
            {
                writer.WriteElementString("Flags", Flags.ToString());

                writer.WriteElementString("MinFixed", MinFixed.ToString());
                writer.WriteElementString("MaxFixed", MaxFixed.ToString());

                writer.WriteElementString("Scalar", Scalar.ToString());
            }
            finally { writer.WriteEndElement(); }
        }

        public SkillGainModifier(XmlTextReader reader)
        {
            int version = Int32.Parse(reader.GetAttribute("version"));
            reader.ReadStartElement("SkillGainModifier");

            switch (version)
            {
                case 0:
                    {
                        Flags = (SkillNameFlags)Enum.Parse(typeof(SkillNameFlags), reader.ReadElementString("Flags"));

                        MinFixed = Int32.Parse(reader.ReadElementString("MinFixed"));
                        MaxFixed = Int32.Parse(reader.ReadElementString("MaxFixed"));

                        Scalar = Double.Parse(reader.ReadElementString("Scalar"));

                        break;
                    }
            }

            reader.ReadEndElement();
        }

        public override string ToString()
        {
            if (m_MinFixed != 0 && m_MaxFixed != 0)
                return String.Format("x{0:F2} [{1:F1}-{2:F1}] : {3}", m_Scalar, m_MinFixed / 10.0, m_MaxFixed / 10.0, m_Flags);
            else if (m_MinFixed != 0)
                return String.Format("x{0:F2} >={1:F1} : {2}", m_Scalar, m_MinFixed / 10.0, m_Flags);
            else if (m_MaxFixed != 0)
                return String.Format("x{0:F2} <{1:F1} : {2}", m_Scalar, m_MaxFixed / 10.0, m_Flags);
            else
                return String.Format("x{0:F2} : {1}", m_Scalar, m_Flags);
        }
    }

    [PropertyObject]
    public class SkillGainPoints : CompactArray64
    {
        public float this[SkillName skill]
        {
            get { return Utility.IntToFloatBits(this[(ulong)skill.GetFlag()]); }
            set { this[(ulong)skill.GetFlag()] = Utility.FloatToIntBits(value); }
        }

        #region Accessors

        [CommandProperty(AccessLevel.GameMaster)]
        public float Alchemy
        {
            get { return this[SkillName.Alchemy]; }
            set { this[SkillName.Alchemy] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Anatomy
        {
            get { return this[SkillName.Anatomy]; }
            set { this[SkillName.Anatomy] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float AnimalLore
        {
            get { return this[SkillName.AnimalLore]; }
            set { this[SkillName.AnimalLore] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float ItemID
        {
            get { return this[SkillName.ItemID]; }
            set { this[SkillName.ItemID] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float ArmsLore
        {
            get { return this[SkillName.ArmsLore]; }
            set { this[SkillName.ArmsLore] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Parry
        {
            get { return this[SkillName.Parry]; }
            set { this[SkillName.Parry] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Begging
        {
            get { return this[SkillName.Begging]; }
            set { this[SkillName.Begging] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Blacksmith
        {
            get { return this[SkillName.Blacksmith]; }
            set { this[SkillName.Blacksmith] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Fletching
        {
            get { return this[SkillName.Fletching]; }
            set { this[SkillName.Fletching] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Peacemaking
        {
            get { return this[SkillName.Peacemaking]; }
            set { this[SkillName.Peacemaking] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Camping
        {
            get { return this[SkillName.Camping]; }
            set { this[SkillName.Camping] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Carpentry
        {
            get { return this[SkillName.Carpentry]; }
            set { this[SkillName.Carpentry] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Cartography
        {
            get { return this[SkillName.Cartography]; }
            set { this[SkillName.Cartography] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Cooking
        {
            get { return this[SkillName.Cooking]; }
            set { this[SkillName.Cooking] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float DetectHidden
        {
            get { return this[SkillName.DetectHidden]; }
            set { this[SkillName.DetectHidden] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Discordance
        {
            get { return this[SkillName.Discordance]; }
            set { this[SkillName.Discordance] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float EvalInt
        {
            get { return this[SkillName.EvalInt]; }
            set { this[SkillName.EvalInt] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Healing
        {
            get { return this[SkillName.Healing]; }
            set { this[SkillName.Healing] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Fishing
        {
            get { return this[SkillName.Fishing]; }
            set { this[SkillName.Fishing] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Forensics
        {
            get { return this[SkillName.Forensics]; }
            set { this[SkillName.Forensics] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Herding
        {
            get { return this[SkillName.Herding]; }
            set { this[SkillName.Herding] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Hiding
        {
            get { return this[SkillName.Hiding]; }
            set { this[SkillName.Hiding] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Provocation
        {
            get { return this[SkillName.Provocation]; }
            set { this[SkillName.Provocation] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Inscribe
        {
            get { return this[SkillName.Inscribe]; }
            set { this[SkillName.Inscribe] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Lockpicking
        {
            get { return this[SkillName.Lockpicking]; }
            set { this[SkillName.Lockpicking] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Magery
        {
            get { return this[SkillName.Magery]; }
            set { this[SkillName.Magery] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float MagicResist
        {
            get { return this[SkillName.MagicResist]; }
            set { this[SkillName.MagicResist] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Tactics
        {
            get { return this[SkillName.Tactics]; }
            set { this[SkillName.Tactics] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Snooping
        {
            get { return this[SkillName.Snooping]; }
            set { this[SkillName.Snooping] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Musicianship
        {
            get { return this[SkillName.Musicianship]; }
            set { this[SkillName.Musicianship] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Poisoning
        {
            get { return this[SkillName.Poisoning]; }
            set { this[SkillName.Poisoning] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Archery
        {
            get { return this[SkillName.Archery]; }
            set { this[SkillName.Archery] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float SpiritSpeak
        {
            get { return this[SkillName.SpiritSpeak]; }
            set { this[SkillName.SpiritSpeak] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Stealing
        {
            get { return this[SkillName.Stealing]; }
            set { this[SkillName.Stealing] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Tailoring
        {
            get { return this[SkillName.Tailoring]; }
            set { this[SkillName.Tailoring] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float AnimalTaming
        {
            get { return this[SkillName.AnimalTaming]; }
            set { this[SkillName.AnimalTaming] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float TasteID
        {
            get { return this[SkillName.TasteID]; }
            set { this[SkillName.TasteID] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Tinkering
        {
            get { return this[SkillName.Tinkering]; }
            set { this[SkillName.Tinkering] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Tracking
        {
            get { return this[SkillName.Tracking]; }
            set { this[SkillName.Tracking] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Veterinary
        {
            get { return this[SkillName.Veterinary]; }
            set { this[SkillName.Veterinary] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Swords
        {
            get { return this[SkillName.Swords]; }
            set { this[SkillName.Swords] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Macing
        {
            get { return this[SkillName.Macing]; }
            set { this[SkillName.Macing] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Fencing
        {
            get { return this[SkillName.Fencing]; }
            set { this[SkillName.Fencing] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Wrestling
        {
            get { return this[SkillName.Wrestling]; }
            set { this[SkillName.Wrestling] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Lumberjacking
        {
            get { return this[SkillName.Lumberjacking]; }
            set { this[SkillName.Lumberjacking] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Mining
        {
            get { return this[SkillName.Mining]; }
            set { this[SkillName.Mining] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Meditation
        {
            get { return this[SkillName.Meditation]; }
            set { this[SkillName.Meditation] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float Stealth
        {
            get { return this[SkillName.Stealth]; }
            set { this[SkillName.Stealth] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public float RemoveTrap
        {
            get { return this[SkillName.RemoveTrap]; }
            set { this[SkillName.RemoveTrap] = value; }
        }

        //[CommandProperty(AccessLevel.GameMaster)]
        //public float Necromancy
        //{
        //	get { return this[SkillName.Necromancy]; }
        //	set { this[SkillName.Necromancy] = value; }
        //}

        //[CommandProperty(AccessLevel.GameMaster)]
        //public float Focus
        //{
        //	get { return this[SkillName.Focus]; }
        //	set { this[SkillName.Focus] = value; }
        //}

        //[CommandProperty(AccessLevel.GameMaster)]
        //public float Chivalry
        //{
        //	get { return this[SkillName.Chivalry]; }
        //	set { this[SkillName.Chivalry] = value; }
        //}

        //[CommandProperty(AccessLevel.GameMaster)]
        //public float Bushido
        //{
        //	get { return this[SkillName.Bushido]; }
        //	set { this[SkillName.Bushido] = value; }
        //}

        //[CommandProperty(AccessLevel.GameMaster)]
        //public float Ninjitsu
        //{
        //	get { return this[SkillName.Ninjitsu]; }
        //	set { this[SkillName.Ninjitsu] = value; }
        //}

        //[CommandProperty(AccessLevel.GameMaster)]
        //public float Spellweaving
        //{
        //	get { return this[SkillName.Spellweaving]; }
        //	set { this[SkillName.Spellweaving] = value; }
        //}

        #endregion

        public SkillGainPoints()
            : base()
        {
        }

        public override string ToString()
        {
            return "...";
        }
    }

    [NoSort]
    public class SkillGainControl : Item
    {
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double Bonus
        {
            get { return SkillGainSystem.Bonus; }
            set { SkillGainSystem.Bonus = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double CapWeight
        {
            get { return SkillGainSystem.CapWeight; }
            set { SkillGainSystem.CapWeight = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double SuccessWeight
        {
            get { return SkillGainSystem.SuccessWeight; }
            set { SkillGainSystem.SuccessWeight = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double GlobalFactor
        {
            get { return SkillGainSystem.GlobalFactor; }
            set { SkillGainSystem.GlobalFactor = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool GainWaits
        {
            get { return SkillGainSystem.GainWaits; }
            set { SkillGainSystem.GainWaits = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double GSGG
        {
            get { return SkillGainSystem.GSGG; }
            set { SkillGainSystem.GSGG = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool PointSystem
        {
            get { return SkillGainSystem.PointSystem; }
            set { SkillGainSystem.PointSystem = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool AntiMacroCode
        {
            get { return SkillGainSystem.AntiMacroCode; }
            set { SkillGainSystem.AntiMacroCode = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillNameFlags UseAntiMacro
        {
            get { return SkillGainSystem.UseAntiMacro; }
            set { SkillGainSystem.UseAntiMacro = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public TimeSpan AntiMacroExpire
        {
            get { return SkillGainSystem.AntiMacroExpire; }
            set { SkillGainSystem.AntiMacroExpire = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AntiMacroAllowance
        {
            get { return SkillGainSystem.AntiMacroAllowance; }
            set { SkillGainSystem.AntiMacroAllowance = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int AntiMacroLocationSize
        {
            get { return SkillGainSystem.AntiMacroLocationSize; }
            set { SkillGainSystem.AntiMacroLocationSize = value; }
        }

        private static SkillGainModifier GetModifier(int index)
        {
            if (index >= 0 && index < SkillGainSystem.Modifiers.Count)
                return SkillGainSystem.Modifiers[index];

            return null;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier1
        {
            get { return GetModifier(0); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier2
        {
            get { return GetModifier(1); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier3
        {
            get { return GetModifier(2); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier4
        {
            get { return GetModifier(3); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier5
        {
            get { return GetModifier(4); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier6
        {
            get { return GetModifier(5); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier7
        {
            get { return GetModifier(6); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier8
        {
            get { return GetModifier(7); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier9
        {
            get { return GetModifier(8); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier10
        {
            get { return GetModifier(9); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier11
        {
            get { return GetModifier(10); }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public SkillGainModifier Modifier12
        {
            get { return GetModifier(11); }
            set { }
        }

        [Constructable]
        public SkillGainControl()
            : base(0x1F14)
        {
            Name = "Skill Gain Control";
            Weight = 1.0;
            Hue = 0x26;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Counselor)
                from.SendGump(new PropertiesGump(from, this));
        }

        public SkillGainControl(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}