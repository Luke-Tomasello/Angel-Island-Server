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

/* Scripts/Accounting/ClientException.cs
 *	CHANGELOG
 *	11/18/26, Adam (account restrictions)
 *		Client exceptions are Engines/Restrictions (by account or address, scoped): IsException, Add/RemoveClientException
 *		ask it; the hard-coded account list is four rows now (PatchV276). ClientException.xml is read at world load only for
 *		PatchV276 and never written again.
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
            // ClientException.xml is read at world load only for PatchV276 and never written again (11/18/26)
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
                IPAddress? address = null;
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
                IPAddress? address = null;
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
                if (Server.Engines.Restrictions.Lift(Server.Engines.Restrictions.Kind.ClientException, sip, null, "") <= 0)
                {
                    status = string.Format("RemoveClientException: {0} is not whitelisted.", sip);
                    return false;
                }
                else
                {
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
                if (Server.Engines.Restrictions.ClientExempt(null, IPAddress.TryParse(sip, out IPAddress? parsed) ? parsed : null))
                {
                    status = string.Format("AddClientException: {0} is already white listed.", sip);
                    return false;
                }
                else
                {
                    Server.Engines.Restrictions.Add(Server.Engines.Restrictions.Kind.ClientException, sip, Server.Engines.Restrictions.All, 0, "", "", null);
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
            if (false)     // 11/18/26: these accounts are client exceptions in Engines/Restrictions (PatchV276)
            {
                if (ClientException.AddClientException(ns.Address.ToString(), ref status))
                    Utility.Monitor.WriteLine(status, ConsoleColor.Green);
                else
                    Utility.Monitor.WriteLine(status, ConsoleColor.Yellow);
            }

            return true;
        }
        public static bool IsException(IPAddress address) =>
            Server.Engines.Restrictions.ClientExempt(null, address);

        /// <summary>This player's account, or address, is excused from the code tests (Engines/Restrictions).</summary>
        public static bool IsException(Mobile m) =>
            Server.Engines.Restrictions.ClientExempt((m?.Account as Server.Accounting.Account)?.Username, m?.NetState?.Address);
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