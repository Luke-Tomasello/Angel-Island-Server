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

/* Scripts/Accounting/Account.Extended.cs
 * CHANGELOG
 *  6/9/26, Adam
 *  Created
 */

using System;
using System.Collections.Generic;
using System.Xml;

namespace Server.Accounting
{
    public partial class Account
    {
        private int m_VendorDailyGold;
        private DateTime m_VendorWindowStart; // UTC
        private List<int> m_StarterFamilyIds = new();           // track starter-loot family ids bound to this account
        private int m_DisconnectProtectionsInWindow;
        private DateTime m_DisconnectProtectionWindowStartUtc;

        #region Disconnect Protection
        public DateTime LastDisconnect { get; set; } = DateTime.UtcNow;
        public ConnectionStatus SeqConnectionStatus { get; set; } = ConnectionStatus.None;

        [Flags]
        public enum ConnectionStatus
        {
            None,
            LogedOut,
            Disconnected
        }
        #endregion Disconnect Protection

        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public List<int> StarterFamilyIds
        {
            get { return m_StarterFamilyIds; }          // returns the live list (matches Machines pattern)
            set { m_StarterFamilyIds = value ?? new(); }
        }

        // convenience helpers
        public void AddStarterFamilyId(int id) { if (id != 0 && !m_StarterFamilyIds.Contains(id)) m_StarterFamilyIds.Add(id); }
        public bool HasStarterFamilyId(int id) => m_StarterFamilyIds != null && m_StarterFamilyIds.Contains(id);
        public void ClearStarterFamilyIds() => m_StarterFamilyIds = new();

        /// <summary>
        /// Mortalis: Total gold this account has received from NPC vendors
        /// within the current 24-hour window. Resets when the window rolls over.
        /// BODs are tracked, but not goverened. We may consider this.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int VendorDailyGold
        {
            get { return m_VendorDailyGold; }
            set { m_VendorDailyGold = value; }
        }

        /// <summary>
        /// Mortalis: UTC timestamp marking the beginning of this account’s
        /// current vendor-gold 24-hour window.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public DateTime VendorWindowStart
        {
            get { return m_VendorWindowStart; }
            set { m_VendorWindowStart = value; }
        }

        #region Disconnect Protection Throttle (persistent)

        // Policy: 2 disconnect protections per rolling 1-hour window (persistent)
        public int DisconnectProtectionsInWindow { get { return m_DisconnectProtectionsInWindow; } }
        public DateTime DisconnectProtectionWindowStartUtc { get { return m_DisconnectProtectionWindowStartUtc; } }

        public bool CanUseDisconnectProtection()
        {
            return CanUseDisconnectProtection(DateTime.UtcNow);
        }

        public bool CanUseDisconnectProtection(DateTime nowUtc)
        {
            NormalizeDisconnectProtectionWindow(nowUtc);
            return (m_DisconnectProtectionsInWindow < CoreAI.DCPMaxUses);
        }

        public void RecordDisconnectProtectionUse()
        {
            RecordDisconnectProtectionUse(DateTime.UtcNow);
        }

        public void RecordDisconnectProtectionUse(DateTime nowUtc)
        {
            NormalizeDisconnectProtectionWindow(nowUtc);

            if (m_DisconnectProtectionsInWindow < int.MaxValue)
                m_DisconnectProtectionsInWindow++;
        }

        private void NormalizeDisconnectProtectionWindow(DateTime nowUtc)
        {
            if (nowUtc.Kind != DateTimeKind.Utc)
                nowUtc = nowUtc.ToUniversalTime();

            if (m_DisconnectProtectionWindowStartUtc == DateTime.MinValue)
            {
                m_DisconnectProtectionWindowStartUtc = nowUtc;
                m_DisconnectProtectionsInWindow = 0;
                return;
            }

            TimeSpan elapsed = nowUtc - m_DisconnectProtectionWindowStartUtc;

            if (elapsed >= CoreAI.DCPWindow || elapsed < TimeSpan.Zero)
            {
                m_DisconnectProtectionWindowStartUtc = nowUtc;
                m_DisconnectProtectionsInWindow = 0;
            }
        }

        #endregion Disconnect Protection Throttle (persistent)

        public void LoadExtended(XmlElement node)
        {
            if (node == null)
                return;

            m_StarterFamilyIds = LoadStarterFamilyIds(node);
            m_VendorDailyGold = Accounts.GetInt32(Accounts.GetText(node["vendorDailyGold"], "0"), 0);
            m_VendorWindowStart = Accounts.GetDateTime(Accounts.GetText(node["vendorWindowStart"], null), DateTime.MinValue);
            m_DisconnectProtectionsInWindow = Accounts.GetInt32(Accounts.GetText(node["disconnectProtectionsInWindow"], "0"), 0);
            m_DisconnectProtectionWindowStartUtc = Accounts.GetDateTime(Accounts.GetText(node["disconnectProtectionWindowStartUtc"], null), DateTime.MinValue);

            // Normalize after load to avoid stale windows persisting forever
            NormalizeDisconnectProtectionWindow(DateTime.UtcNow);
        }

        public void SaveExtended(XmlTextWriter xml)
        {
            if (xml == null)
                return;

            if (m_StarterFamilyIds.Count > 0)
            {
                xml.WriteStartElement("starterFamilyIds");
                foreach (int id in m_StarterFamilyIds)
                {
                    xml.WriteStartElement("id");
                    xml.WriteString(XmlConvert.ToString(id));
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }

            xml.WriteStartElement("vendorDailyGold");
            xml.WriteString(XmlConvert.ToString(m_VendorDailyGold));
            xml.WriteEndElement();

            xml.WriteStartElement("vendorWindowStart");
            xml.WriteString(XmlConvert.ToString(m_VendorWindowStart, XmlDateTimeSerializationMode.Utc));
            xml.WriteEndElement();

            #region DisconnectProtection
            NormalizeDisconnectProtectionWindow(DateTime.UtcNow);

            xml.WriteStartElement("disconnectProtectionsInWindow");
            xml.WriteString(m_DisconnectProtectionsInWindow.ToString());
            xml.WriteEndElement();

            if (m_DisconnectProtectionWindowStartUtc != DateTime.MinValue)
            {
                xml.WriteStartElement("disconnectProtectionWindowStartUtc");
                xml.WriteString(m_DisconnectProtectionWindowStartUtc.ToString("o"));
                xml.WriteEndElement();
            }
            #endregion DisconnectProtection
        }
    }
}