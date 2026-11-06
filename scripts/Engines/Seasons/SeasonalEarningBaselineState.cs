/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\Seasons\SeasonalEarningBaselineState.cs
 * ChangeLog
 *  3/27/2026, Adam
 *      First time checkin.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Engines.Seasons
{
    public class SeasonalEarningBaselineEntry
    {
        public uint AccountGUID { get; private set; }

        public int MusicGoldCredited { get; set; }
        public int SmithBODGoldCredited { get; set; }
        public int TailorBODGoldCredited { get; set; }
        public int CarpenterBODGoldCredited { get; set; }

        public int PvMLootGoldCredited { get; set; }
        public int ChestGoldCredited { get; set; }
        public int TheftGoldCredited { get; set; }

        public SeasonalEarningBaselineEntry(uint guid)
        {
            AccountGUID = guid;
        }

        public SeasonalEarningBaselineEntry(XmlElement node)
        {
            AccountGUID = Utility.ToUInt32(node.GetAttribute("guid"));

            MusicGoldCredited = Utility.ToInt32(node.GetAttribute("musicCredited"));
            SmithBODGoldCredited = Utility.ToInt32(node.GetAttribute("smithBodCredited"));
            TailorBODGoldCredited = Utility.ToInt32(node.GetAttribute("tailorBodCredited"));
            CarpenterBODGoldCredited = Utility.ToInt32(node.GetAttribute("carpenterBod"));

            PvMLootGoldCredited = Utility.ToInt32(node.GetAttribute("PvMCredited"));
            ChestGoldCredited = Utility.ToInt32(node.GetAttribute("chestCredited"));
            TheftGoldCredited = Utility.ToInt32(node.GetAttribute("theftCredited"));
        }

        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("account");

            xml.WriteAttributeString("guid", AccountGUID.ToString());

            xml.WriteAttributeString("musicCredited", MusicGoldCredited.ToString());
            xml.WriteAttributeString("smithBodCredited", SmithBODGoldCredited.ToString());
            xml.WriteAttributeString("tailorBodCredited", TailorBODGoldCredited.ToString());
            xml.WriteAttributeString("carpenterBod", CarpenterBODGoldCredited.ToString());

            xml.WriteAttributeString("PvMCredited", PvMLootGoldCredited.ToString());
            xml.WriteAttributeString("chestCredited", ChestGoldCredited.ToString());
            xml.WriteAttributeString("theftCredited", TheftGoldCredited.ToString());

            xml.WriteEndElement();
        }
    }

    public static class SeasonalEarningBaselineState
    {
        private static Dictionary<uint, SeasonalEarningBaselineEntry> m_Table = new Dictionary<uint, SeasonalEarningBaselineEntry>();

        public static SeasonalEarningBaselineEntry GetEntry(uint guid)
        {
            if (!m_Table.TryGetValue(guid, out SeasonalEarningBaselineEntry? entry))
            {
                entry = new SeasonalEarningBaselineEntry(guid);
                m_Table[guid] = entry;
            }

            return entry;
        }

        public static void Configure()
        {
            if (!Core.RuleSets.SeasonsRules())
                return;

            EventSink.WorldLoad += Load;
            EventSink.WorldSave += Save;
        }

        private const string Folder = "Saves/Seasons";
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Seasonal Progress Saving...");

            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            string filePath = Path.Combine(Folder, "SeasonalProgress.xml");

            using (StreamWriter op = new StreamWriter(filePath))
            using (XmlTextWriter xml = new XmlTextWriter(op))
            {
                xml.Formatting = Formatting.Indented;
                xml.IndentChar = '\t';
                xml.Indentation = 1;

                xml.WriteStartDocument(true);
                xml.WriteStartElement("seasonalProgress");
                xml.WriteAttributeString("count", m_Table.Count.ToString());

                foreach (SeasonalEarningBaselineEntry entry in m_Table.Values)
                    entry.Save(xml);

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }
        }

        public static void Load()
        {
            string filePath = Path.Combine(Folder, "SeasonalProgress.xml");

            Console.WriteLine("Seasonal Progress Loading...");

            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Folder);

                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    sw.WriteLine("<seasonalProgress count=\"0\"></seasonalProgress>");
                }
            }

            m_Table.Clear();

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement? root = doc["seasonalProgress"];

            if (root == null)
                return;

            foreach (XmlElement node in root.GetElementsByTagName("account"))
            {
                try
                {
                    SeasonalEarningBaselineEntry entry = new SeasonalEarningBaselineEntry(node);
                    m_Table[entry.AccountGUID] = entry;
                }
                catch
                {
                    Console.WriteLine("Warning: SeasonalProgressEntry load failed");
                }
            }
        }

        public static void Reset(uint guid)
        {
            if (m_Table.TryGetValue(guid, out var entry))
                ResetEntry(entry);
        }

        public static void ResetAll()
        {
            foreach (var entry in m_Table.Values)
                ResetEntry(entry);
        }

        private static void ResetEntry(SeasonalEarningBaselineEntry entry)
        {
            if (entry == null)
                return;

            entry.MusicGoldCredited = 0;
            entry.SmithBODGoldCredited = 0;
            entry.TailorBODGoldCredited = 0;
            entry.CarpenterBODGoldCredited = 0;

            entry.PvMLootGoldCredited = 0;
            entry.ChestGoldCredited = 0;   // NEW
            entry.TheftGoldCredited = 0;   // NEW
        }
        public static int ClearAll()
        {
            int count = m_Table.Count;
            m_Table.Clear();
            return count;
        }
    }
}