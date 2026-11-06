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

/* Scripts/Accounting/Account.Utils.cs
 * CHANGELOG
 *  6/9/26, Adam
 *  Created
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Xml;

namespace Server.Accounting
{
    public partial class Account : IAccount
    {
        #region Utility & Helper Methods

        /// <summary>
        /// Adds a new tag to this account. This method does not check for duplicate names.
        /// </summary>
        /// <param name="name">New tag name.</param>
        /// <param name="value">New tag value.</param>
        public void AddTag(string name, string value)
        {
            m_Tags.Add(new AccountTag(name, value));
        }

        /// <summary>
        /// Removes all tags with the specified name from this account.
        /// </summary>
        /// <param name="name">Tag name to remove.</param>
        public void RemoveTag(string name)
        {
            for (int i = m_Tags.Count - 1; i >= 0; --i)
            {
                if (i >= m_Tags.Count)
                    continue;

                AccountTag? tag = (AccountTag?)m_Tags[i];

                if (tag.Name == name)
                    m_Tags.RemoveAt(i);
            }
        }

        /// <summary>
        /// Modifies an existing tag or adds a new tag if no tag exists.
        /// </summary>
        /// <param name="name">Tag name.</param>
        /// <param name="value">Tag value.</param>
        public void SetTag(string name, string value)
        {
            for (int i = 0; i < m_Tags.Count; ++i)
            {
                AccountTag? tag = (AccountTag?)m_Tags[i];

                if (tag.Name == name)
                {
                    tag.Value = value;
                    return;
                }
            }

            AddTag(name, value);
        }

        /// <summary>
        /// Gets the value of a tag -or- null if there are no tags with the specified name.
        /// </summary>
        /// <param name="name">Name of the desired tag value.</param>
        public string? GetTag(string name)
        {
            for (int i = 0; i < m_Tags.Count; ++i)
            {
                AccountTag? tag = (AccountTag?)m_Tags[i];

                if (tag.Name == name)
                    return tag.Value;
            }

            return null;
        }

        public void SetUnspecifiedBan(Mobile? from)
        {
            SetBanTags(from, DateTime.MinValue, TimeSpan.Zero);
        }

        public void SetBanTags(Mobile? from, DateTime banTime, TimeSpan banDuration)
        {
            if (from == null)
                RemoveTag("BanDealer");
            else
                SetTag("BanDealer", from.ToString());

            if (banTime == DateTime.MinValue)
                RemoveTag("BanTime");
            else
                SetTag("BanTime", XmlConvert.ToString(banTime, XmlDateTimeSerializationMode.Utc));

            if (banDuration == TimeSpan.Zero)
                RemoveTag("BanDuration");
            else
                SetTag("BanDuration", banDuration.ToString());
        }

        public bool GetBanTags(out DateTime banTime, out TimeSpan banDuration)
        {
            string? tagTime = GetTag("BanTime");
            string? tagDuration = GetTag("BanDuration");

            if (tagTime != null)
                banTime = Accounts.GetDateTime(tagTime, DateTime.MinValue);
            else
                banTime = DateTime.MinValue;

            if (tagDuration == "Infinite")
            {
                banDuration = TimeSpan.MaxValue;
            }
            else if (tagDuration != null)
            {
                try { banDuration = TimeSpan.Parse(tagDuration); }
                catch { banDuration = TimeSpan.Zero; }
            }
            else
            {
                banDuration = TimeSpan.Zero;
            }

            return (banTime != DateTime.MinValue && banDuration != TimeSpan.Zero);
        }

        public AccessLevel GetAccessLevel()
        {
            bool online;
            AccessLevel accessLevel;
            return GetAccountInfo(out accessLevel, out online);
        }

        public AccessLevel GetAccountInfo(out AccessLevel accessLevel, out bool online)
        {
            accessLevel = this.AccessLevel;
            online = false;

            for (int j = 0; j < 5; ++j)
            {
                Mobile check = this[j];

                if (check == null)
                    continue;

                if (check.AccessLevel > accessLevel)
                    accessLevel = check.AccessLevel;

                if (check.NetState != null)
                    online = true;
            }

            return accessLevel;
        }

        /// <summary>
        /// Returns the set of active character names on this account.
        ///
        /// This is a general-purpose helper used for name reservation and validation.
        /// Names are derived directly from existing character slots:
        /// - Only non-null, non-deleted mobiles are considered (via indexer)
        /// - Blank or whitespace names are ignored
        /// - Names are returned as-is (trimmed), with no normalization applied
        ///
        /// Design intent:
        /// - Represents the names currently "owned" by this account
        /// - Avoids separate reserved-name bookkeeping for multi-character shards (e.g., Seasons)
        /// - Names are automatically freed when characters are deleted
        /// </summary>
        public List<string> GetReservedNames()
        {
            List<string> names = new List<string>();

            for (int i = 0; i < this.Length; i++)
            {
                Mobile m = this[i];
                if (m == null) continue;

                string name = m.Name;
                if (string.IsNullOrWhiteSpace(name)) continue;

                names.Add(name.Trim());
            }

            return names;
        }

        public override string ToString()
        {
            return m_Username;
        }

        #endregion Utility & Helper Methods

        #region Serialization Helpers (Moved from core)

        private static Serial ReadSerialOrMinusOne(XmlElement node, string name)
        {
            int raw = Accounts.GetInt32(
                Accounts.GetText(node[name], ((int)Serial.MinusOne).ToString()),
                (int)Serial.MinusOne
            );
            if (raw == (int)Serial.Zero)
                raw = (int)Serial.MinusOne;

            return (Serial)raw;
        }

        /// <summary>
        /// Deserializes a list of string values from an xml element. Null values are not added to the list.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>String list. Value will never be null.</returns>
        public static string[] LoadAccessCheck(XmlElement node)
        {
            string[] stringList;
            XmlElement? accessCheck = node["accessCheck"];

            if (accessCheck != null)
            {
                ArrayList list = new ArrayList();

                foreach (XmlElement ip in accessCheck.GetElementsByTagName("ip"))
                {
                    string text = Accounts.GetText(ip, null);

                    if (text != null)
                        list.Add(text);
                }

                stringList = (string[])list.ToArray(typeof(string));
            }
            else
            {
                stringList = new string[0];
            }

            return stringList;
        }

        public static string[] LoadEmailHistory(XmlElement node)
        {
            string[] stringList;
            XmlElement? emailHistory = node["emailHistory"];

            if (emailHistory != null)
            {
                ArrayList list = new ArrayList();

                foreach (XmlElement address in emailHistory.GetElementsByTagName("addr"))
                {
                    string text = Accounts.GetText(address, null);

                    if (text != null)
                        list.Add(text);
                }

                stringList = (string[])list.ToArray(typeof(string));
            }
            else
            {
                stringList = new string[0];
            }

            return stringList;
        }

        /// <summary>
        /// Deserializes a list of IPAddress values from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Address list. Value will never be null.</returns>
        public static IPAddress[] LoadAddressList(XmlElement node)
        {
            IPAddress[] list;
            XmlElement? addressList = node["addressList"];

            if (addressList != null)
            {
                int count = Accounts.GetInt32(Accounts.GetAttribute(addressList, "count", "0"), 0);

                list = new IPAddress[count];

                count = 0;

                foreach (XmlElement ip in addressList.GetElementsByTagName("ip"))
                {
                    try
                    {
                        if (count < list.Length)
                        {
                            list[count] = IPAddress.Parse(Accounts.GetText(ip, null));
                            count++;
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }

                if (count != list.Length)
                {
                    IPAddress[] old = list;
                    list = new IPAddress[count];

                    for (int i = 0; i < count && i < old.Length; ++i)
                        list[i] = old[i];
                }
            }
            else
            {
                list = new IPAddress[0];
            }

            return list;
        }

        public static List<int> LoadMachineList(XmlElement node)
        {
            List<int> list = new();
            XmlElement? machineList = node["machineList"];

            if (machineList != null)
            {
                foreach (XmlElement hash in machineList.GetElementsByTagName("hash"))
                {
                    try
                    {
                        list.Add(int.Parse(Accounts.GetText(hash, null)));
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        /// <summary>
        /// Deserializes a list of AccountTag instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Tag list. Value will never be null.</returns>
        public static ArrayList LoadTags(XmlElement node)
        {
            ArrayList list = new ArrayList();
            XmlElement? tags = node["tags"];

            if (tags != null)
            {
                foreach (XmlElement tag in tags.GetElementsByTagName("tag"))
                {
                    try { list.Add(new AccountTag(tag)); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        /// <summary>
        /// Deserializes a list of AccountComment instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement from which to deserialize.</param>
        /// <returns>Comment list. Value will never be null.</returns>
        public static ArrayList LoadComments(XmlElement node)
        {
            ArrayList list = new ArrayList();
            XmlElement? comments = node["comments"];

            if (comments != null)
            {
                foreach (XmlElement comment in comments.GetElementsByTagName("comment"))
                {
                    try { list.Add(new AccountComment(comment)); }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }
        public static List<int> LoadStarterFamilyIds(XmlElement node)
        {
            return LoadIntList(node, "starterFamilyIds");
        }

        public static List<int> LoadIntList(XmlElement node, string containerName)
        {
            List<int> list = new();

            XmlElement? container = node[containerName];
            if (container != null)
            {
                foreach (XmlElement ele in container.GetElementsByTagName("id"))
                {
                    try
                    {
                        int val = Accounts.GetInt32(Accounts.GetText(ele, "0"), 0);

                        // Ignore invalid or zero entries
                        if (val != 0)
                            list.Add(val);
                    }
                    catch (Exception ex)
                    {
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Deserializes a list of Mobile instances from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement instance from which to deserialize.</param>
        /// <returns>Mobile list. Value will never be null.</returns>
        public static Mobile[] LoadMobiles(XmlElement node)
        {
            Mobile[] list;
            XmlElement? chars = node["chars"];

            if (chars == null)
            {
                list = new Mobile[5];
            }
            else
            {
                int length = Accounts.GetInt32(Accounts.GetAttribute(chars, "length", "5"), 5);

                list = new Mobile[length];

                foreach (XmlElement ele in chars.GetElementsByTagName("char"))
                {
                    try
                    {
                        int index = Accounts.GetInt32(Accounts.GetAttribute(ele, "index", "0"), 0);
                        int serial = Accounts.GetInt32(Accounts.GetText(ele, "0"), 0);

                        if (index >= 0 && index < list.Length)
                            list[index] = World.FindMobile(serial);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return list;
        }

        private static void WriteDate(XmlWriter xml, string name, DateTime value)
        {
            if (value == DateTime.MinValue) return;
            xml.WriteElementString(name, XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc));
        }

        #endregion Serialization Helpers
    }
}