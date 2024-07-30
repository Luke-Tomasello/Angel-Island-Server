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

/* Misc/StatGain.cs
 * CHANGELOG:
 *  12/46/22, Adam (CheckStatGain)
 *      Added a new function CanGainStat(), called from CheckStatGain() to ensure
 *      we don't lose a chance to gain a stat if one of the stats is at 100, yet marked UP.
 *      The old code would accept this as a valid scenario, just to failure later because this 
 *      stat is already at it's cap
 *  11/28/22 Yoar (added by Adam) CheckStatGain
 *  What I tried to do here is reduce the stat gain rate in order to
 *  account for the old stat gain rates.
 * 
 *  It is based on the assumption that the player receives one stat gain
 *  check every 10 seconds. However, this is a bad assumption. Stat gain
 *  checks are only performed on skill gain. Therefore, in order to
 *  obtain the old AI stat gain rate, you need one skill gain (+0.1)
 *  every 10 seconds. This is not realistic for skills that require
 *  extended waiting periods between skill attempts (e.g. magery).
 * 
 *  There simply is no accurate estimate for the number of stat gains
 *  within a certain time span. Therefore we can't factor in the old
 *  AI stat gain waits.      
 *  2/22/22, Yoar
 *		Initial version.
 */

using Server.Gumps;
using Server.Mobiles;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Misc
{
    public static class StatGainSystem
    {
        public static bool Enabled { get { return Core.RuleSets.AngelIslandRules(); } } // exclusive to AI

        public static bool PointSystem = true; // enable/disable stat gain point system
        public static double GainFactor = 1.0;

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
            Load();
        }

        private static readonly StatType[] m_StatArray = new StatType[]
            {
                StatType.Str, StatType.Dex, StatType.Int
            };
        public static bool CanGainStat(Mobile m, StatType stat)
        {
            switch (stat)
            {
                case StatType.Str: return m.RawStr < m.StrMax;
                case StatType.Dex: return m.RawDex < m.DexMax;
                case StatType.Int: return m.RawInt < m.IntMax;
            }
            // error - unknown stat
            return false;
        }
        public static bool CheckStatGain(Mobile m, Skill skill, out StatType toGain)
        {
            Utility.Shuffle(m_StatArray);

            for (int i = 0; i < 3; i++)
            {
                StatType stat = m_StatArray[i];

                bool up = false;
                double gc = 0.0;

                switch (stat)
                {
                    case StatType.Str: up = (m.StrLock == StatLockType.Up); gc = m.StatGainChance(skill, Stat.Str); break;
                    case StatType.Dex: up = (m.DexLock == StatLockType.Up); gc = m.StatGainChance(skill, Stat.Dex); break;
                    case StatType.Int: up = (m.IntLock == StatLockType.Up); gc = m.StatGainChance(skill, Stat.Int); break;
                }

                if (!up || gc <= 0.0)
                    continue;

                // do we want to waste a roll of the dice on a stat that can't possibly raise?
                if (CanGainStat(m, stat) == false)
                    continue;

                /* Yoar: The following code is bad!
                 * 
                 * What I tried to do here is reduce the stat gain rate in order to
                 * account for the old stat gain rates.
                 * 
                 * It is based on the assumption that the player receives one stat gain
                 * check every 10 seconds. However, this is a bad assumption. Stat gain
                 * checks are only performed on skill gain. Therefore, in order to
                 * obtain the old AI stat gain rate, you need one skill gain (+0.1)
                 * every 10 seconds. This is not realistic for skills that require
                 * extended waiting periods between skill attempts (e.g. magery).
                 * 
                 * There simply is no accurate estimate for the number of stat gains
                 * within a certain time span. Therefore we can't factor in the old
                 * AI stat gain waits.
                 */
#if false
                // factor in gain waits, assuming a 10s stat check delay
                gc /= (Mobile.StatGainDelay.TotalSeconds / 10.0);
#endif

                int val = 0, cap = 0;

                switch (stat)
                {
                    case StatType.Str: val = m.RawStr; cap = m.StrMax; break;
                    case StatType.Dex: val = m.RawDex; cap = m.DexMax; break;
                    case StatType.Int: val = m.RawInt; cap = m.IntMax; break;
                }

                // slow down stat gain near the stat cap
                gc *= (cap - val + 25.0) / cap;


                gc *= GainFactor;

                if (gc < 0.01)
                    gc = 0.01;

                if (m is PlayerMobile)
                {
                    PlayerMobile pm = (PlayerMobile)m;

                    if (PointSystem)
                        AwardPoints(pm, stat, ref gc);

                    //m.SendMessage(0x37, "Stat gain ({0}): {1:F3}", stat, gc);
                }

                if (Utility.RandomDouble() < gc)
                {
                    toGain = stat;
                    return true;
                }

                break; // try to gain only one stat
            }

            toGain = (StatType)0;
            return false;
        }

        public static void OnAfterGain(Mobile m, StatType stat, int oldStatRaw)
        {
            if (PointSystem && m is PlayerMobile)
                ResetPoints((PlayerMobile)m, stat); // we've gained, reset stat gain points
        }

        public static void AwardPoints(PlayerMobile pm, StatType stat, ref double gc)
        {
            if (gc < 0.01 || gc >= 0.30)
                return;

            double exponent;

            if (gc < 0.10)
                exponent = 1.700;
            else if (gc < 0.20)
                exponent = 1.450;
            else
                exponent = 1.275;

            double pts = 0.5 * Math.Pow(gc, exponent);

            /* Keep incrementing our stat gain points until we gain. The stat gain chance
             * equals our accumulated stat gain points. Since we're incrementing our points,
             * we're guaranteed to gain if we keep trying.
             * 
             * This procedure reduces the variance in stat gain times while approximately
             * preserving the expected stat gain time.
             */
            switch (stat)
            {
                case StatType.Str: gc = (pm.StrGainPoints += (float)pts); break;
                case StatType.Dex: gc = (pm.DexGainPoints += (float)pts); break;
                case StatType.Int: gc = (pm.IntGainPoints += (float)pts); break;
            }
#if DEBUG && false
            Utility.ConsoleOut(string.Format("Points gained: {0}", pts), ConsoleColor.Green);
            switch (stat)
            {
                case StatType.Str: Utility.ConsoleOut(string.Format("Str Accumulated Points: {0}", pm.StrGainPoints), ConsoleColor.Green); ; break;
                case StatType.Dex: Utility.ConsoleOut(string.Format("Dex Accumulated Points: {0}", pm.DexGainPoints), ConsoleColor.Green); ; break;
                case StatType.Int: Utility.ConsoleOut(string.Format("Int Accumulated Points: {0}", pm.IntGainPoints), ConsoleColor.Green); ; break;
            }
#endif
        }

        public static void ResetPoints(PlayerMobile pm, StatType stat)
        {
            switch (stat)
            {
                case StatType.Str: pm.StrGainPoints = 0.0f; break;
                case StatType.Dex: pm.DexGainPoints = 0.0f; break;
                case StatType.Int: pm.IntGainPoints = 0.0f; break;
            }
        }

        #region Data Management

        private const string CfgFileName = @"Saves\StatGainConfig.xml";

        private static void Save()
        {
            Console.WriteLine("Stat Gain Config Saving...");

            try
            {
                XmlTextWriter writer = new XmlTextWriter(CfgFileName, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("StatGainConfig");
                writer.WriteAttributeString("version", "0");

                try
                {
                    writer.WriteElementString("PointSystem", PointSystem.ToString());
                    writer.WriteElementString("GainFactor", GainFactor.ToString());
                }
                finally { writer.WriteEndDocument(); }

                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("StatGainSystem Error: {0}", e);
            }
        }

        private static bool Load()
        {
            Console.WriteLine("Stat Gain Config Loading...");

            try
            {
                if (!File.Exists(CfgFileName))
                    return false; // nothing to read

                XmlTextReader reader = new XmlTextReader(CfgFileName);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int verison = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("StatGainConfig");

                switch (verison)
                {
                    case 0:
                        {
                            PointSystem = Boolean.Parse(reader.ReadElementString("PointSystem"));
                            GainFactor = Double.Parse(reader.ReadElementString("GainFactor"));

                            break;
                        }
                }

                reader.ReadEndElement();

                reader.Close();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("StatGainSystem Error: {0}", e);
                return false;
            }
        }

        #endregion
    }

    [NoSort]
    public class StatGainControl : Item
    {
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool PointSystem
        {
            get { return StatGainSystem.PointSystem; }
            set { StatGainSystem.PointSystem = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public double GainFactor
        {
            get { return StatGainSystem.GainFactor; }
            set { StatGainSystem.GainFactor = value; }
        }

        [Constructable]
        public StatGainControl()
            : base(0x1F14)
        {
            Name = "Stat Gain Control";
            Weight = 1.0;
            Hue = 0x3A;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Counselor)
                from.SendGump(new PropertiesGump(from, this));
        }

        public StatGainControl(Serial serial)
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