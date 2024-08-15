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

/* Scripts/Engines/Reward System/RewardSystem.cs
 * Created 5/23/04 by mith
 * ChangeLog
 * 3/10/10, Adam
 *		Turned off Rewards system
 * 6/19/04, adam
 *		1. fixed typo "enterred." to "entered."
 *	5/26/04, mith
 *		Removed AccessLevel check in Reward_OnCommand.
 *	5/26/04, mith
 *		Modified ReadXml to read from Data/Rewards.xml if Saves/Rewards/Rewards.xml is missing.
 *	5/25/04, mith
 *		Modified WriteXml to remove exception when file not found. 
 *		Added checking to make sure local array is not empty before writing.
 *	5/24/04, mith
 *		Added more robust exception-handling to prevent server load/save crashes.
 *		Fixed a potential crash bug in WriteXml if the Rewards.xml file doesn't exist.
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Server.Engines.RewardSystem
{
    public class RewardSystem
    {
        private static RewardCategory[] m_Categories;
        private static RewardList[] m_Lists;
        private static ArrayList m_Codes;
        private static string m_RewardCode, m_RewardType, m_RewardUsed;

        public static bool Enabled = false; // change to true to enable vet rewards

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        private static void OnLoad()
        {
            Console.WriteLine("Rewards Loading...");

            try
            {
                string filePath = Path.Combine("Saves/Rewards", "Rewards.xml");
                if (File.Exists(filePath) == false && File.Exists(Path.Combine(Core.DataDirectory, "Rewards.xml")) == false)
                {
                    Core.LoggerShortcuts.BootError(string.Format("Error while reading Rewards from \"{0}\".", Path.Combine(Core.DataDirectory, "Rewards.xml")));
                    Enabled = false;
                    return;
                }
                ReadXml(filePath);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("Error Loading Rewards.");
                Console.WriteLine("Exception Message :  {0}", ex.ToString());
                Console.WriteLine("Rewards system has been disabled.");
                Enabled = false;
            }
        }

        private static void OnSave(WorldSaveEventArgs e)
        {
            Console.WriteLine("Rewards Saving...");

            try
            {
                WriteXml(e);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("Error Saving Rewards.");
                Console.WriteLine("Exception Message :  {0}", ex.ToString());
                Console.WriteLine("Rewards system has been disabled.");
                Enabled = false;
            }
        }

        private static void ReadXml(string filePath)
        {
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(Core.DataDirectory, "Rewards.xml");

                if (!File.Exists(filePath))
                    throw new FileNotFoundException();
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement root = doc["RewardCodes"];

            int count = Accounts.GetInt32(Accounts.GetAttribute(root, "count", "0"), 0);
            m_Codes = new ArrayList();

            foreach (XmlElement node in root.GetElementsByTagName("Reward"))
            {
                try
                {
                    DBObject entry = new DBObject();
                    entry.Code = Accounts.GetText(node["RewardCode"], "");
                    entry.Type = Accounts.GetText(node["RewardType"], "");
                    entry.Used = Accounts.GetText(node["RewardUsed"], "");

                    m_Codes.Add(entry);
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }
        }

        private static void WriteXml(WorldSaveEventArgs e)
        {
            if (!Directory.Exists("Saves/Rewards"))
                Directory.CreateDirectory("Saves/Rewards");

            string filePath = Path.Combine("Saves/Rewards", "Rewards.xml");

            if (m_Codes.Count > 0)
            {
                using (StreamWriter op = new StreamWriter(filePath))
                {
                    XmlTextWriter xml = new XmlTextWriter(op);

                    xml.Formatting = Formatting.Indented;
                    xml.IndentChar = '\t';
                    xml.Indentation = 1;

                    xml.WriteStartDocument(true);

                    xml.WriteStartElement("RewardCodes");

                    xml.WriteAttributeString("count", m_Codes.Count.ToString());

                    for (int i = 0; i < m_Codes.Count; ++i)
                    {
                        xml.WriteStartElement("Reward");

                        xml.WriteStartElement("RewardCode");
                        xml.WriteString(((DBObject)m_Codes[i]).Code.ToString());
                        xml.WriteEndElement();
                        xml.WriteStartElement("RewardType");
                        xml.WriteString(((DBObject)m_Codes[i]).Type.ToString());
                        xml.WriteEndElement();
                        xml.WriteStartElement("RewardUsed");
                        xml.WriteString(((DBObject)m_Codes[i]).Used.ToString());
                        xml.WriteEndElement();

                        xml.WriteEndElement();
                    }

                    xml.WriteEndElement();

                    xml.Close();
                }
            }
        }

        [Usage("Reward [RewardCode]")]
        [Description("Display rewards selection gump.")]
        public static void Reward_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!Enabled)
                return;

            if (!from.Alive)
                return;

            if (e.Length == 1)
            {
                foreach (DBObject entry in m_Codes)
                {
                    m_RewardCode = m_RewardType = m_RewardUsed = "";
                    if (entry.Code == e.GetString(0) && entry.Used == "False")
                    {
                        m_RewardCode = entry.Code;
                        m_RewardType = entry.Type;
                        m_RewardUsed = entry.Used;
                        break;
                    }
                }

                if (m_RewardCode != "")
                    if (((Account)from.Account).GetTag("RewardsClaimed") == m_RewardType)
                        from.SendMessage("You have already claimed a reward.");
                    else if (m_RewardUsed == "False")
                        from.SendGump(new RewardChoiceGump(from, m_RewardType));
                    else
                        from.SendMessage("This RewardCode has already been used.");
                else
                    from.SendMessage("Invalid RewardCode entered.");
            }
            else
                from.SendMessage("Format : Reward <RewardCode>");
        }

        public static RewardCategory[] Categories
        {
            get
            {
                if (m_Categories == null)
                    SetupRewardTables();

                return m_Categories;
            }
        }

        public static RewardList[] Lists
        {
            get
            {
                if (m_Lists == null)
                    SetupRewardTables();

                return m_Lists;
            }
        }


        public static bool HasAccess(Mobile mob, RewardCategory list)
        {
            if (list == null)
                return false;

            Account acct = mob.Account as Account;

            if (acct == null)
                return false;

            if (list.XmlString != null && list.XmlString == m_RewardType)
                return true;

            return false;
        }

        public static void SetupRewardTables()
        {
            RewardCategory betaTest = new RewardCategory("Beta Tester Rewards", "BetaTester");
            // Misc rewards should be modified to whatever additional rewards program we want to add.
            RewardCategory miscReward = new RewardCategory("Miscellaneous Rewards", "Miscellaneous");

            m_Categories = new RewardCategory[]
                {
                    betaTest,
                    miscReward
                };

            m_Lists = new RewardList[]
            {
                new RewardList( 1, new RewardEntry[]
                {
                    new RewardEntry( betaTest, "Decorative Statue (East)", typeof( DecorativeStatue1E ) ),
                    new RewardEntry( betaTest, "Decorative Statue (South)", typeof( DecorativeStatue1S ) ),
                    new RewardEntry( betaTest, "Decorative Statue (East)", typeof( DecorativeStatue2E ) ),
                    new RewardEntry( betaTest, "Decorative Statue (South)", typeof( DecorativeStatue2S ) )
                } ),
                new RewardList( 2, new RewardEntry[]
                {
					// These are only for testing, whenever new rewards are added, these will be modified.
					new RewardEntry( miscReward, "Item #1", typeof( Blocker ) ),
                    new RewardEntry( miscReward, "Item #2", typeof( Blocker ) ),
                    new RewardEntry( miscReward, "Item #3", typeof( Blocker ) ),
                    new RewardEntry( miscReward, "Item #4", typeof( Blocker ) )
                } ),
            };
        }

        public static bool UpdateRewardCodes(Mobile from)
        {
            for (int i = 0; i < m_Codes.Count; i++)
                if (((DBObject)m_Codes[i]).Code == m_RewardCode)
                {
                    ((DBObject)m_Codes[i]).Used = "True";
                    ((Account)from.Account).SetTag("RewardsClaimed", m_RewardType);
                    return true;
                }

            return false;
        }

    }

    public interface IRewardItem
    {
        bool IsRewardItem { get; set; }
    }

    public class DBObject
    {
        private string m_Code, m_Type, m_Used;

        public string Code
        {
            get { return m_Code; }
            set { m_Code = value; }
        }

        public string Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        public string Used
        {
            get { return m_Used; }
            set { m_Used = value; }
        }
    }
}