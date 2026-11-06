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

/* Scripts/Accounting/Account.Mortalis.cs
 * CHANGELOG
 *  2/7/26, Adam
 *  Created
 */

using System;
using System.Collections.Generic;
using System.Xml;

namespace Server.Accounting
{
    // Account identity carry-over flags (Mortalis).
    // This is a generic bitfield intended to grow over time.
    // Today we only use it for hair/beard dye reimbursement tracking.
    [Flags]
    public enum IdentityFlags : uint
    {
        None = 0x00000000,

        // Dye reimbursement flags (set when dye is applied; cleared after reimbursement).
        HairDye = 0x00000001,
        BeardDye = 0x00000002,
        SpecialHairDye = 0x00000004,
        SpecialBeardDye = 0x00000008,

        // Future identity flags go here...
    }
    public partial class Account
    {
        public static void Initialize()
        {
            if (Core.RuleSets.MortalisRules())
            {
                Server.CommandSystem.Register("RestorePlayer", AccessLevel.Administrator, new CommandEventHandler(RestorePlayer));
            }
        }
        #region Obsolete - delete once all accounts have been through the new GUID/OwnerGUID flow and we no longer need to track bankbox or house serials at the account level.
        //Adam: Mortalis: your bankbox is saved and passed to the next character created on your account.
        private Serial m_BankBox = Serial.MinusOne;             // Available AFTER DEATH
        private Serial m_House = Serial.MinusOne;               // Available AFTER DEATH
        #endregion Obsolete

        private Serial m_GuildStone = Serial.MinusOne;          // Available AFTER DEATH
        private DateTime m_TimeOfDeath = DateTime.MinValue;     // Available AFTER DEATH
        private int m_Karma = 0;
        private int m_Fame = 0;
        private List<int> m_BannedHouseSerials = new();
        private bool m_IsImprisoned = false;
        private bool m_IsJailed = false;
        private int m_HairHue = 0;
        private int m_BeardHue = 0;
        private int m_BackpackHue = 0;

        // other bad boy stuff
        private int m_LongTermMurders = 0;
        private int m_ShortTermMurders = 0;
        private int m_HarmfulOffenses = 0;
        private DateTime m_LastOffenseTime = DateTime.UtcNow;

        public void DeleteMortalis()
        {
            // account owned house and bankbox
            uint guid = Utility.MakeGuid(Username, Username);

            if (guid != 0)
            {
                // new
                // When an account is deleted, remove any bankboxes bound to this account’s
                // identity. Bankboxes are StrongBackpack instances whose OwnerGUID matches
                // the account GUID. We no longer rely on Account.BankBox (Serial) here.
                if (guid != 0)
                {
                    var toDelete = new List<Items.StrongBackpack>();

                    // First pass: collect candidates
                    foreach (Item item in World.Items.Values)
                    {
                        if (item is not Items.StrongBackpack bankbox)
                            continue;

                        if (bankbox.Deleted)
                            continue;

                        if (bankbox.OwnerGUID == guid)
                            toDelete.Add(bankbox);
                    }

                    // Second pass: safely delete outside of the world enumeration
                    foreach (var bankbox in toDelete)
                    {
                        bankbox.Delete();
                    }
                }

                // When an account is deleted, any houses pending inheritance (or otherwise
                // bound to this account’s identity) must also be removed. We cannot assume
                // there are still characters on the account (they may not have reincarnated
                // yet), so we derive the account’s GUID directly from the username and match
                // on OwnerGUID.
                //
                // Normal directly-owned houses tied to live characters are already handled
                // when those mobiles are deleted. This pass specifically targets any
                // GUID-bound houses associated with this account identity.

                foreach (var kvp in Multis.BaseHouse.Instances)
                {
                    Multis.BaseHouse house = kvp.Key;
                    bool playerOwned = kvp.Value;

                    if (house == null || house.Deleted)
                        continue;

                    if (!playerOwned)
                        continue;

                    // Any player-owned house whose OwnerGUID matches this account’s GUID
                    // should be removed when the account is deleted.
                    if (house.OwnerGUID == guid)
                        house.Delete();
                }
            } // GUID owned properties
        }

        // offenses
        public DateTime HarmfulCriminalityEnds { get; set; } // murderous offenses
        public DateTime AFKCriminalityEnds { get; set; } // unattended resource gathering
        public DateTime ThievingCriminalityEnds { get; set; } // theft offenses
        // Guard-whack windows (guards may be called until these times)
        public DateTime HarmfulGuardWhackEnds { get; set; }
        public DateTime AFKGuardWhackEnds { get; set; }
        public DateTime ThievingGuardWhackEnds { get; set; }
        // Criminal travel suppression windows (recall/gate blocked until these times)
        public DateTime HarmfulTravelLockEnds { get; set; }
        public DateTime AFKTravelLockEnds { get; set; }
        public DateTime ThievingTravelLockEnds { get; set; }

        /// <summary>
        /// Available AFTER DEATH:
        /// Mortalis: BankBox carryover for players killed on Mortalis
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public Serial BankBox
        {
            get
            {
                // Normalize: treat Zero as MinusOne for safety
                if (m_BankBox == Serial.Zero)
                    return Serial.MinusOne;

                return m_BankBox;
            }
            set
            {
                // Defensive: flag if someone tries to assign Serial.Zero
                System.Diagnostics.Debug.Assert(value != Serial.Zero, "Attempted to assign Serial.Zero to BankBox.");

                // Normalize: convert Zero to MinusOne on assignment
                m_BankBox = (value == Serial.Zero) ? Serial.MinusOne : value;
            }
        }

        /// <summary>
        /// Mortalis: Long-term murder counts carried forward after death
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int LongTermMurders
        {
            get { return m_LongTermMurders; }
            set { m_LongTermMurders = value; }
        }

        /// <summary>
        /// Mortalis: Short-term murder counts carried forward after death
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int ShortTermMurders
        {
            get { return m_ShortTermMurders; }
            set { m_ShortTermMurders = value; }
        }

        /// <summary>
        /// Mortalis: Karma value carried forward after death
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int Karma
        {
            get { return m_Karma; }
            set { m_Karma = value; }
        }

        /// <summary>
        /// Mortalis: Fame value carried forward after death
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int Fame
        {
            get { return m_Fame; }
            set { m_Fame = value; }
        }
        /// <summary>
        /// Available AFTER DEATH:
        /// Mortalis: remember the house oned OnBeforeDeath. We will need to search this for assets we may use to reequip the player
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public Serial House
        {
            get
            {
                // Normalize: treat Zero as MinusOne for safety
                if (m_House == Serial.Zero)
                    return Serial.MinusOne;

                return m_House;
            }
            set
            {
                // Defensive: flag if someone tries to assign Serial.Zero
                System.Diagnostics.Debug.Assert(value != Serial.Zero, "Attempted to assign Serial.Zero to House.");

                // Normalize: convert Zero to MinusOne on assignment
                m_House = (value == Serial.Zero) ? Serial.MinusOne : value;
            }
        }
        /// <summary>
        /// Available AFTER DEATH:
        /// Mortalis: remember the house oned OnBeforeDeath. We will need to search this for assets we may use to reequip the player
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public Serial GuildStone
        {
            get
            {
                // Normalize: treat Zero as MinusOne for safety
                if (m_GuildStone == Serial.Zero)
                    return Serial.MinusOne;

                return m_GuildStone;
            }
            set
            {
                // Defensive: flag if someone tries to assign Serial.Zero
                System.Diagnostics.Debug.Assert(value != Serial.Zero, "Attempted to assign Serial.Zero to GuildStone.");

                // Normalize: convert Zero to MinusOne on assignment
                m_GuildStone = (value == Serial.Zero) ? Serial.MinusOne : value;
            }
        }

        /// <summary>
        /// Available AFTER DEATH:
        /// Date detailing when the (one-and-only) died.
        /// Used AccountHandler to override IPLimiter default functionally.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public DateTime TimeOfDeath
        {
            get { return m_TimeOfDeath; }
            set { m_TimeOfDeath = value; }
        }



        /// <summary>
        /// Mortalis: tracks the number of murderous offenses committed in guarded towns.
        /// These are incidents where a player attacks an innocent and is killed by guards.
        /// Used to drive the escalation system for repeated griefing.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public int HarmfulOffenses
        {
            get
            {
                // Perform cleanup on every read.
                if (m_HarmfulOffenses > 0)
                {
                    TimeSpan delta = DateTime.UtcNow - m_LastOffenseTime;

                    // One offense decays per full day.
                    int days = (int)delta.TotalDays;

                    if (days > 0)
                    {
                        m_HarmfulOffenses -= days;

                        if (m_HarmfulOffenses < 0)
                            m_HarmfulOffenses = 0;

                        // Reset decay reference point
                        m_LastOffenseTime = DateTime.UtcNow;
                    }
                }

                return m_HarmfulOffenses;
            }

            set
            {
                // Value may have been cleaned up by getter if caller used +=.
                m_HarmfulOffenses = value;

                if (value > 0)
                {
                    // Reset decay timer whenever the count is incremented or set positive.
                    m_LastOffenseTime = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Mortalis: time of last murderous offenses committed in guarded towns.
        /// Used for decay: 24 hours per count
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Administrator)]
        public DateTime LastOffenseTime
        {
            get { return m_LastOffenseTime; }
            set => m_LastOffenseTime = value;
        }

        // Houses this account is banned from (stored by house Serial).
        // Used to persist house bans across character death and reincarnation.
        public List<int> BannedHouseSerials
        {
            get { return m_BannedHouseSerials; }
        }
        /// <summary>
        /// Gets or sets whether this account is currently imprisoned.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Administrator)]
        public bool IsImprisoned
        {
            get { return m_IsImprisoned; }
            set
            {
                m_IsImprisoned = value;

                if (value)
                    m_IsJailed = false; // mutually exclusive
            }
        }

        /// <summary>
        /// Gets or sets whether this account is currently jailed.
        /// </summary>
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Administrator)]
        public bool IsJailed
        {
            get { return m_IsJailed; }
            set
            {
                m_IsJailed = value;

                if (value)
                    m_IsImprisoned = false; // mutually exclusive
            }
        }

        public int HairHue
        {
            get { return m_HairHue; }
            set { m_HairHue = value; }
        }
        public int BeardHue
        {
            get { return m_BeardHue; }
            set { m_BeardHue = value; }
        }
        public int BackpackHue
        {
            get { return m_BackpackHue; }
            set { m_BackpackHue = value; }
        }

        public void LoadMortalis(XmlElement node)
        {
            if (node == null)
                return;

            // load a snapshot of players stats and skills
            LoadSnapshot(node);

            m_BackpackHue = Accounts.GetInt32(Accounts.GetText(node["backpackHue"], "0"), m_BackpackHue);
            m_HairHue = Accounts.GetInt32(Accounts.GetText(node["hairHue"], "0"), m_HairHue);
            m_BeardHue = Accounts.GetInt32(Accounts.GetText(node["beardHue"], "0"), m_BeardHue);
            m_BankBox = ReadSerialOrMinusOne(node, "bankBox"); // verify obsolete
            m_House = ReadSerialOrMinusOne(node, "house"); // verify obsolete
            m_GuildStone = ReadSerialOrMinusOne(node, "guildStone");
            m_TimeOfDeath = Accounts.GetDateTime(Accounts.GetText(node["timeOfDeath"], null), DateTime.MinValue);
            m_LongTermMurders = Accounts.GetInt32(Accounts.GetText(node["longterm_murders"], "0"), 0);
            m_ShortTermMurders = Accounts.GetInt32(Accounts.GetText(node["shortterm_murders"], "0"), 0);
            m_Karma = Accounts.GetInt32(Accounts.GetText(node["karma"], "0"), 0);
            m_Fame = Accounts.GetInt32(Accounts.GetText(node["fame"], "0"), 0);
            m_StarterFamilyIds = LoadStarterFamilyIds(node);
            m_HarmfulOffenses = Accounts.GetInt32(Accounts.GetText(node["harmfulOffense"], "0"), 0);
            m_LastOffenseTime = Accounts.GetDateTime(Accounts.GetText(node["lastOffenseTime"], null), DateTime.UtcNow);
            m_BannedHouseSerials = LoadIntList(node, "bannedHouseSerials");
            m_IsImprisoned = Accounts.GetBool(node["isImprisoned"], false);
            m_IsJailed = Accounts.GetBool(node["isJailed"], false);
            // offenses
            HarmfulCriminalityEnds = Accounts.GetDateTime(Accounts.GetText(node["harmfulCriminalityEnds"], null), DateTime.UtcNow);
            AFKCriminalityEnds = Accounts.GetDateTime(Accounts.GetText(node["afkCriminalityEnds"], null), DateTime.UtcNow);
            ThievingCriminalityEnds = Accounts.GetDateTime(Accounts.GetText(node["thievingCriminalityEnds"], null), DateTime.UtcNow);
            // Guard-whack windows (guards may be called until these times)
            HarmfulGuardWhackEnds = Accounts.GetDateTime(Accounts.GetText(node["harmfulGuardWhackEnds"], null), DateTime.UtcNow);
            AFKGuardWhackEnds = Accounts.GetDateTime(Accounts.GetText(node["afkGuardWhackEnds"], null), DateTime.UtcNow);
            ThievingGuardWhackEnds = Accounts.GetDateTime(Accounts.GetText(node["thievingGuardWhackEnds"], null), DateTime.UtcNow);
            // Criminal travel suppression windows (recall/gate blocked until these times)
            HarmfulTravelLockEnds = Accounts.GetDateTime(Accounts.GetText(node["harmfulTravelLockEnds"], null), DateTime.UtcNow);
            AFKTravelLockEnds = Accounts.GetDateTime(Accounts.GetText(node["afkTravelLockEnds"], null), DateTime.UtcNow);
            ThievingTravelLockEnds = Accounts.GetDateTime(Accounts.GetText(node["thievingTravelLockEnds"], null), DateTime.UtcNow);
        }

        public void SaveMortalis(XmlTextWriter xml)
        {
            if (xml == null)
                return;

            // save a snapshot of players stats and skills
            SaveSnapshot(xml);

            if (m_BackpackHue != 0)
            {
                xml.WriteStartElement("backpackHue");
                xml.WriteString(XmlConvert.ToString(m_BackpackHue));
                xml.WriteEndElement();
            }

            if (m_HairHue != 0)
            {
                xml.WriteStartElement("hairHue");
                xml.WriteString(XmlConvert.ToString(m_HairHue));
                xml.WriteEndElement();
            }

            if (m_BeardHue != 0)
            {
                xml.WriteStartElement("beardHue");
                xml.WriteString(XmlConvert.ToString(m_BeardHue));
                xml.WriteEndElement();
            }

            if (m_LongTermMurders != 0)
            {
                xml.WriteStartElement("longterm_murders");
                xml.WriteString(XmlConvert.ToString(m_LongTermMurders));
                xml.WriteEndElement();
            }

            if (m_ShortTermMurders != 0)
            {
                xml.WriteStartElement("shortterm_murders");
                xml.WriteString(XmlConvert.ToString(m_ShortTermMurders));
                xml.WriteEndElement();
            }

            xml.WriteStartElement("karma");
            xml.WriteString(XmlConvert.ToString(m_Karma));
            xml.WriteEndElement();

            xml.WriteStartElement("fame");
            xml.WriteString(XmlConvert.ToString(m_Fame));
            xml.WriteEndElement();

            if (m_GuildStone != Serial.MinusOne && m_GuildStone != Serial.Zero)
            {
                xml.WriteStartElement("guildStone");
                xml.WriteString(XmlConvert.ToString(m_GuildStone));
                xml.WriteEndElement();
            }

            if (m_BankBox != Serial.MinusOne && m_BankBox != Serial.Zero)
            {
                xml.WriteStartElement("bankBox");
                xml.WriteString(XmlConvert.ToString(m_BankBox));
                xml.WriteEndElement();
            }

            if (m_TimeOfDeath != DateTime.MinValue)
            {
                xml.WriteStartElement("timeOfDeath");
                xml.WriteString(XmlConvert.ToString(m_TimeOfDeath, XmlDateTimeSerializationMode.Utc));
                xml.WriteEndElement();
            }

            if (m_BannedHouseSerials.Count > 0)
            {
                xml.WriteStartElement("bannedHouseSerials");
                foreach (int id in m_BannedHouseSerials)
                {
                    xml.WriteStartElement("id");
                    xml.WriteString(XmlConvert.ToString(id));
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }

            if (m_HarmfulOffenses != 0)
            {
                xml.WriteStartElement("harmfulOffense");
                xml.WriteString(XmlConvert.ToString(m_HarmfulOffenses));
                xml.WriteEndElement();

                xml.WriteStartElement("lastOffenseTime");
                xml.WriteString(XmlConvert.ToString(m_LastOffenseTime, XmlDateTimeSerializationMode.Utc));
                xml.WriteEndElement();
            }

            if (m_IsImprisoned != false)
            {
                xml.WriteStartElement("isImprisoned");
                xml.WriteString((m_IsImprisoned ? "true" : "false"));
                xml.WriteEndElement();
            }

            if (m_IsJailed != false)
            {
                xml.WriteStartElement("isJailed");
                xml.WriteString((m_IsJailed ? "true" : "false"));
                xml.WriteEndElement();
            }
            // offenses
            WriteDate(xml, "harmfulCriminalityEnds", HarmfulCriminalityEnds);
            WriteDate(xml, "afkCriminalityEnds", AFKCriminalityEnds);
            WriteDate(xml, "thievingCriminalityEnds", ThievingCriminalityEnds);
            // Guard-whack windows (guards may be called until these times)
            WriteDate(xml, "harmfulGuardWhackEnds", HarmfulGuardWhackEnds);
            WriteDate(xml, "afkGuardWhackEnds", AFKGuardWhackEnds);
            WriteDate(xml, "thievingGuardWhackEnds", ThievingGuardWhackEnds);
            // Criminal travel suppression windows (recall/gate blocked until these times)
            WriteDate(xml, "harmfulTravelLockEnds", HarmfulTravelLockEnds);
            WriteDate(xml, "afkTravelLockEnds", AFKTravelLockEnds);
            WriteDate(xml, "thievingTravelLockEnds", ThievingTravelLockEnds);
        }
    }
}

