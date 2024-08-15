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

/* Scripts/Accounting/ClientException.cs
 *	CHANGELOG
 *	9/30/2023, Adam
 *		Initial Version.
 */

using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;

namespace Server.Accounting
{
    public static class ClientException
    {
        private static List<string> m_Table = new();
        public static List<string> Table { get { return m_Table; } }
        public static void Configure()
        {
            EventSink.WorldLoad += EventSink_OnWorldLoad;
            EventSink.WorldSave += EventSink_OnWorldSave;
        }
        public static void Initialize()
        {
            Server.CommandSystem.Register("AddClientException", AccessLevel.Administrator, new CommandEventHandler(AddClientException));
            Server.CommandSystem.Register("RemoveClientException", AccessLevel.Administrator, new CommandEventHandler(RemoveClientException));
        }
        [Usage("AddClientException <ip address>")]
        [Description("Established a client exception based on this ip address.")]
        public static void AddClientException(CommandEventArgs e)
        {
            try
            {
                IPAddress address = null;
                if (e.GetString(0) == null || !IPAddress.TryParse(e.GetString(0), out address))
                {
                    e.Mobile.SendMessage("Usage: AddClientException <ip address>");
                    return;
                }
                string sip = address.ToString();
                string status = string.Empty;
                AddClientException(sip, ref status);
                e.Mobile.SendMessage(status);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
        [Usage("RemoveClientException <ip address>")]
        [Description("Established a client exception based on this ip address.")]
        public static void RemoveClientException(CommandEventArgs e)
        {
            try
            {
                IPAddress address = null;
                if (e.GetString(0) == null || !IPAddress.TryParse(e.GetString(0), out address))
                {
                    e.Mobile.SendMessage("Usage: RemoveClientException <ip address>");
                    return;
                }
                string sip = address.ToString();
                string status = string.Empty;
                RemoveClientException(sip, ref status);
                e.Mobile.SendMessage(status);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
        public static bool RemoveClientException(string sip, ref string status)
        {
            try
            {
                if (!m_Table.Contains(sip, StringComparer.OrdinalIgnoreCase))
                {
                    status = string.Format("RemoveClientException: {0} is not whitelisted.", sip);
                    return false;
                }
                else
                {
                    m_Table.Remove(sip);
                    status = string.Format("{0} removed from whitelisted.", sip);
                    return true;
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            return false;
        }
        public static bool AddClientException(string sip, ref string status)
        {
            try
            {
                if (m_Table.Contains(sip, StringComparer.OrdinalIgnoreCase))
                {
                    status = string.Format("AddClientException: {0} is already white listed.", sip);
                    return false;
                }
                else
                {
                    m_Table.Add(sip);
                    status = string.Format("{0} white listed.", sip);
                    return true;
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            return false;
        }
        public static bool ClientExceptionWhiteList(string userName, NetState ns)
        {
            if (string.IsNullOrEmpty(userName) || ns == null)
                return false;
            /* White List
             * These accounts, for whatever reason (using a Mac or for instance,) are
             * exempt from the bad client, bad razor check.
             * Note: Other multi-accounting checks are still active for these accounts.
             */
            string status = string.Empty;
            List<string> exemptAccounts = new()
                    {
                        // Kevin_ � 08/29/2023 12:40 PM (Mac)
                        // "90.187.112.29",
                        // Character: Weirdbeard
                        "databreaker",

                        // DemonSlayer � Yesterday at 1:02 PM (Mac)
                        // "70.115.53.101",
                        // Character: Demon Slayer
                        "Demonous",

                        // KarmaKarma � Yesterday at 3:07 PM
                        // ???
                        // Character: Karma
                        "alupton", // (Mac)

                        // cooper144
                        // ??
                        // character: Colloid
                        "cooper144",

                        //"adam ant"
                    };
            if (exemptAccounts.Contains(userName, StringComparer.OrdinalIgnoreCase))
            {
                if (ClientException.AddClientException(ns.Address.ToString(), ref status))
                    Utility.ConsoleWriteLine(status, ConsoleColor.Green);
                else
                    Utility.ConsoleWriteLine(status, ConsoleColor.Yellow);
            }

            return true;
        }
        public static bool IsException(IPAddress address)
        {
            foreach (string sip in m_Table)
                if (Utility.IPMatch(sip, address))
                    return true;
            return false;
        }
        private static void EventSink_OnWorldLoad()
        {
            LoadXml();
        }
        public static void EventSink_OnWorldSave(WorldSaveEventArgs e)
        {
            SaveXml();
        }
        #region Save/Load XML
        private const string Folder = "Saves/Accounts";
        private const string FileName = "ClientException.xml";
        // TODO: Rewrite to use XmlDoc instead of XmlTextWriter
        private static void SaveXml()
        {
            try
            {
                if (!Directory.Exists(Folder))
                    Directory.CreateDirectory(Folder);

                string filePath = Path.Combine(Folder, FileName);

                using (StreamWriter sw = new StreamWriter(filePath))
                using (XmlTextWriter writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.IndentChar = '\t';
                    writer.Indentation = 1;

                    writer.WriteStartDocument(true);

                    writer.WriteStartElement("root");

                    writer.WriteAttributeString("count", m_Table.Count.ToString());

                    foreach (string clientIP in m_Table)
                    {
                        writer.WriteStartElement("ClientException");
                        writer.WriteStartElement("ip");
                        writer.WriteString(clientIP);
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();

                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }
        private static void LoadXml()
        {
            try
            {
                Console.Write("Loading Client IP exceptions from '{0}'...", FileName);

                string filePath = Path.Combine(Folder, FileName);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine(" Aborting, file doesn't exist.");
                    return;
                }

                int count = 0;

                XmlDocument doc = new XmlDocument();

                doc.Load(filePath);

                foreach (XmlElement xmlElem in doc["root"].GetElementsByTagName("ClientException"))
                {
                    string ip = xmlElem["ip"].InnerText;

                    if (!string.IsNullOrEmpty(ip))
                    {
                        m_Table.Add(ip);
                        count++;
                    }
                }

                Console.WriteLine(" Done, loaded {0} Client IP exceptions.", count);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }
        #endregion
    }
}