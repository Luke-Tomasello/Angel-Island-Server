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

/* Items/Misc/BankCheck.cs
 * ChangeLog:
 *  7/7/2023, Adam (CashiersCheck)
 *      Payment made in the form of a CashiersCheck - which can only be cashed by the owner.
 *  6/8/23, Yoar
 *      Added GetAmount, ConsumeUpTo static helper methods
 *	6/30/2021, Adam
 *		Add the new UnemploymentCheck for new player. Replaces gold as starter gold, as gold can be exploitatively farmed.
 *		-Cannot be cashed
 *		-Accepted by NPC merchants for goods and services.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using Server.Engines.Quests;
using Server.Mobiles;
using Server.Network;
using System;
using Haven = Server.Engines.Quests.Haven;
using Necro = Server.Engines.Quests.Necro;

namespace Server.Items
{
    public class BankCheck : Item
    {
        private int m_Worth;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Worth
        {
            get { return m_Worth; }
            set { m_Worth = value; InvalidateProperties(); }
        }

        public BankCheck(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Worth);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Worth = reader.ReadInt();
                        break;
                    }
            }
        }

        [Constructable]
        public BankCheck(int worth)
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = 0x34;
            LootType = LootType.Blessed;

            m_Worth = worth;
        }
        [Constructable]
        public BankCheck()
            : this(1)
        {
        }
        public override bool DisplayLootType { get { return false; } }

        public override int LabelNumber { get { return 1041361; } } // A bank check

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060738, m_Worth.ToString()); // value: ~1_val~
        }

        public override void OnSingleClick(Mobile from)
        {
            from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1041361, "", AffixType.Append, string.Concat(" ", m_Worth.ToString("N0") /*m_Worth.ToString()*/), "")); // A bank check:
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Deleted)
            {   // exploit! (they are trying to cash a deleted check.)
                LogHelper Logger = new LogHelper("BankCheckExploit.log", false);
                Logger.Log(LogType.Mobile, from, string.Format("exploit! they are trying to cash a deleted check."));
                Logger.Finish();
                // jail time
                Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(from as Mobiles.PlayerMobile, 3, "they are trying to cash a deleted check..", false);
                jt.GoToJail();

                // tell staff
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. they are trying to cash a deleted check..", from as Mobiles.PlayerMobile));
                return;
            }

            BankBox box = from.BankBox;
            if (box != null && IsChildOf(box))
            {
                Delete();

                int deposited = 0;

                int toAdd = m_Worth;

                Gold gold;

                while (toAdd > 60000)
                {
                    gold = new Gold(60000);

                    if (box.TryDropItem(from, gold, false))
                    {
                        toAdd -= 60000;
                        deposited += 60000;
                    }
                    else
                    {
                        gold.Delete();

                        from.AddToBackpack(new BankCheck(toAdd));
                        toAdd = 0;

                        break;
                    }
                }

                if (toAdd > 0)
                {
                    gold = new Gold(toAdd);

                    if (box.TryDropItem(from, gold, false))
                    {
                        deposited += toAdd;
                    }
                    else
                    {
                        gold.Delete();

                        from.AddToBackpack(new BankCheck(toAdd));
                    }
                }

                // Gold was deposited in your account:
                from.SendLocalizedMessage(1042672, true, " " + deposited.ToString());

                PlayerMobile? pm = from as PlayerMobile;

                if (pm != null)
                {
                    QuestSystem qs = pm.Quest;

                    if (qs is Necro.DarkTidesQuest)
                    {
                        QuestObjective? obj = qs.FindObjective(typeof(Necro.CashBankCheckObjective));

                        if (obj != null && !obj.Completed)
                            obj.Complete();
                    }

                    if (qs is Haven.UzeraanTurmoilQuest)
                    {
                        QuestObjective? obj = qs.FindObjective(typeof(Haven.CashBankCheckObjective));

                        if (obj != null && !obj.Completed)
                            obj.Complete();
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(1047026); // That must be in your bank box to use it.
            }
        }
    }
    public static class CivicBondKeys
    {
        public const string Generic = "Generic";

        // Legacy check-like instruments:
        public const string RefundCheck = "RefundCheck";
        public const string CashiersCheck = "CashiersCheck";

        // Legacy training / unemployment instruments:
        public const string UnemploymentCheck = "UnemploymentCheck";
        public const string TrainingVoucher = "TrainingVoucher";

        // Future examples:
        public const string ChampionCredit = "ChampionCredit";
        public const string RealmCredit = "RealmCredit";
    }
    public class CivicBond : Item
    {
        // --------------------------------------------------------------------
        // Sentinels / defaults
        // --------------------------------------------------------------------

        // OwnerSerial == UnboundOwner → anyone can use it
        public static readonly Serial UnboundOwner = Serial.MinusOne;

        // ValidDate == ValidAnyTime → no restriction
        public static readonly DateTime ValidAnyTime = DateTime.MinValue;

        // Default semantic tag when none is specified
        public const string DefaultBondKey = CivicBondKeys.Generic;

        // --------------------------------------------------------------------
        // Bond state
        // --------------------------------------------------------------------

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial OwnerSerial { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Worth { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string BondKey { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ValidDate { get; set; }

        public bool IsBound => OwnerSerial != UnboundOwner;

        public bool IsOwner(Mobile m)
        {
            if (!IsBound)
                return true;

            return m != null && m.Serial == OwnerSerial;
        }

        public bool IsValidNow =>
            ValidDate == ValidAnyTime || DateTime.UtcNow >= ValidDate;

        // --------------------------------------------------------------------
        // Constructors
        // --------------------------------------------------------------------

        public CivicBond(Serial serial) : base(serial)
        {
        }

        // Serial-based constructor
        public CivicBond(
            Serial ownerSerial,
            int worth = 0,
            string bondKey = DefaultBondKey,
            int hue = 0,
            string? name = null,
            DateTime? validDate = null)
            : base(0x14F0) // same graphic as checks
        {
            LootType = LootType.Blessed;
            Weight = 1.0;

            OwnerSerial = ownerSerial;
            Worth = worth;
            BondKey = string.IsNullOrEmpty(bondKey) ? DefaultBondKey : bondKey;
            ValidDate = validDate ?? ValidAnyTime;

            if (hue != 0)
                Hue = hue;

            if (!string.IsNullOrWhiteSpace(name))
                Name = name;
        }

        // Mobile-based convenience constructor
        [Constructable]
        public CivicBond(
            Mobile owner,
            int worth = 0,
            string bondKey = DefaultBondKey,
            int hue = 0,
            string? name = null,
            DateTime? validDate = null)
            : this(owner != null ? owner.Serial : UnboundOwner, worth, bondKey, hue, name, validDate)
        {
        }

        // Zero-argument convenience constructor (GM/spawn/testing use)
        [Constructable]
        public CivicBond()
            : this(UnboundOwner,            // not bound to any player
                0,                       // worth
                DefaultBondKey,          // "Generic"
                0,                       // hue
                null,                    // no custom name
                ValidAnyTime)            // usable immediately
        {
        }


        // --------------------------------------------------------------------
        // Presentation
        // --------------------------------------------------------------------

        public override bool DisplayLootType => false;

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            string displayName = !string.IsNullOrWhiteSpace(Name)
                ? Name
                : GetBondDisplayName();

            list.Add(displayName); // override top line

            list.Add(1060738, Worth.ToString()); // value: ~1_val~

            if (ValidDate != ValidAnyTime)
                list.Add("Valid from: {0}", ValidDate);
        }

        public override void OnSingleClick(Mobile from)
        {
            // Prefer explicit Name if the caller set one.
            string displayName;

            if (!string.IsNullOrWhiteSpace(Name))
            {
                displayName = Name;
            }
            else
            {
                // Fallback to camel-case → words (“training voucher”)
                displayName = GetBondDisplayName();
            }

            LabelTo(from, "{0} worth {1}", displayName, Worth.ToString("N0"));

            if (ValidDate != ValidAnyTime)
                LabelTo(from, "Valid from {0}", ValidDate);
        }

        // --------------------------------------------------------------------
        // Usage dispatcher
        // --------------------------------------------------------------------

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            if (Deleted)
            {
                HandleDeletedExploit(from);
                return;
            }

            switch (BondKey)
            {
                case CivicBondKeys.RefundCheck:
                case CivicBondKeys.CashiersCheck:
                    HandleBankRedemption(from);
                    break;

                case CivicBondKeys.TrainingVoucher:
                case CivicBondKeys.UnemploymentCheck:
                    HandleUsageInfo(from);
                    break;

                default:
                    base.OnDoubleClick(from);
                    break;
            }
        }

        // --------------------------------------------------------------------
        // Deleted exploit handling
        // --------------------------------------------------------------------

        private void HandleDeletedExploit(Mobile from)
        {
            try
            {
                LogHelper logger = new LogHelper("CivicBondExploit.log", false);
                logger.Log(LogType.Mobile, from,
                    string.Format("Attempted to use deleted CivicBond: BondKey={0}, Worth={1}, Owner={2}",
                        BondKey, Worth, OwnerSerial));
                logger.Finish();
            }
            catch
            {
            }

            // Brick it permanently
            Worth = 0;
            ValidDate = DateTime.UtcNow.AddYears(100);
        }

        // --------------------------------------------------------------------
        // Bank redemption (RefundCheck / CashiersCheck)
        // --------------------------------------------------------------------

        private void HandleBankRedemption(Mobile from)
        {
            if (!IsOwner(from))
            {
                from.SendMessage("This is not your {0}.", GetBondDisplayName());
                from.SendMessage("Guards!");
                return;
            }

            if (!IsValidNow)
            {
                from.SendMessage("That {0} will become valid {1}.", GetBondDisplayName(), ValidDate);
                return;
            }

            BankBox box = from.BankBox;

            if (box == null || !IsChildOf(box))
            {
                from.SendLocalizedMessage(1047026); // Must be in your bank box to use it.
                return;
            }

            int toAdd = Worth;
            if (toAdd <= 0)
                return;

            Delete();

            int deposited = DepositAsGoldOrChecks(from, box, toAdd);

            from.SendLocalizedMessage(1042672, true, " " + deposited.ToString());
        }

        private static int DepositAsGoldOrChecks(Mobile from, BankBox box, int amount)
        {
            int deposited = 0;
            int toAdd = amount;

            while (toAdd > 60000)
            {
                Gold gold = new Gold(60000);

                if (box.TryDropItem(from, gold, false))
                {
                    toAdd -= 60000;
                    deposited += 60000;
                }
                else
                {
                    gold.Delete();
                    from.AddToBackpack(new BankCheck(toAdd));
                    toAdd = 0;
                    break;
                }
            }

            if (toAdd > 0)
            {
                Gold gold = new Gold(toAdd);

                if (box.TryDropItem(from, gold, false))
                    deposited += toAdd;
                else
                {
                    gold.Delete();
                    from.AddToBackpack(new BankCheck(toAdd));
                }
            }

            return deposited;
        }

        // --------------------------------------------------------------------
        // Info-only usage (TrainingVoucher / UnemploymentCheck)
        // --------------------------------------------------------------------

        private void HandleUsageInfo(Mobile from)
        {
            string display = GetBondDisplayName();

            if (!IsOwner(from))
            {
                from.SendMessage("This is not your {0}.", display);
                from.SendMessage("Guards!");
                return;
            }

            BankBox box = from.BankBox;
            bool inBank = (box != null && IsChildOf(box));

            if (BondKey == CivicBondKeys.TrainingVoucher)
            {
                if (inBank)
                    from.SendMessage("We cannot cash this {0}. It will however be accepted in most shops and by trainers.", display);
                else
                    from.SendMessage("This {0} may be used for training and accepted in most shops.", display);
            }
            else if (BondKey == CivicBondKeys.UnemploymentCheck)
            {
                if (inBank)
                    from.SendMessage("We cannot cash this {0}. It will however be accepted in most shops.", display);
                else
                    from.SendMessage("This {0} may be used as payment and accepted in most shops.", display);
            }
        }

        // --------------------------------------------------------------------
        // CamelCase → display text
        // --------------------------------------------------------------------

        public string GetBondDisplayName()
        {
            string key = string.IsNullOrEmpty(BondKey) ? DefaultBondKey : BondKey;

            var sb = new System.Text.StringBuilder(key.Length + 4);

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];

                if (i > 0 && char.IsUpper(c) && char.IsLower(key[i - 1]))
                    sb.Append(' ');

                sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }

        // --------------------------------------------------------------------
        // Persistence
        // --------------------------------------------------------------------

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(OwnerSerial);
            writer.Write(Worth);
            writer.Write(BondKey);
            writer.Write(ValidDate);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            int version = reader.ReadInt();

            if (version == 0)
            {
                OwnerSerial = reader.ReadInt();
                Worth = reader.ReadInt();
                BondKey = reader.ReadString();
                ValidDate = reader.ReadDateTime();
            }

            if (string.IsNullOrEmpty(BondKey))
                BondKey = DefaultBondKey;
        }
    }
    [Obsolete("Legacy instrument type — no longer used. Do not create.")]
    public class RefundCheck : Item
    {
        private int m_Worth;
        [CommandProperty(AccessLevel.GameMaster)]
        public int Worth
        {
            get { return m_Worth; }
            set { m_Worth = value; InvalidateProperties(); }
        }

        private DateTime m_ValidDate = DateTime.MinValue; // now
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ValidDate
        {
            get { return m_ValidDate; }
            set { m_ValidDate = value; InvalidateProperties(); }
        }

        public RefundCheck(Serial serial)
            : base(serial)
        {
        }

        [Constructable]
        public RefundCheck(int worth)
            : base(0x14F0)
        {
            Name = "refund check";
            Weight = 1.0;
            Hue = 0x4CC;    // pink!
            LootType = LootType.Blessed;

            m_Worth = worth;
        }
        [Constructable]
        public RefundCheck(int worth, DateTime valid)
            : this(worth)
        {
            m_ValidDate = valid;
        }
        [Constructable]
        public RefundCheck(int worth, DateTime valid, int hue, string note)
            : this(worth)
        {
            m_ValidDate = valid;
            Hue = hue;
            Name = Name + note;
        }
        public override bool DisplayLootType { get { return false; } }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060738, m_Worth.ToString()); // value: ~1_val~
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, "{0} worth {1}", Name, m_Worth.ToString("N0"));
            LabelTo(from, "Valid {0}", m_ValidDate);
        }

        public override void OnDoubleClick(Mobile from)
        {


            if (Deleted)
            {   // exploit! (they are trying to cash a deleted check.)
                LogHelper Logger = new LogHelper("BankCheckExploit.log", false);
                Logger.Log(LogType.Mobile, from, string.Format("exploit! they are trying to cash a deleted check."));
                Logger.Finish();
                // jail time
                Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(from as Mobiles.PlayerMobile, 3, "they are trying to cash a deleted check..", false);
                jt.GoToJail();

                // tell staff
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. they are trying to cash a deleted check..", from as Mobiles.PlayerMobile));
                return;
            }
            if (DateTime.UtcNow > ValidDate)
            {
                BankBox box = from.BankBox;
                if (box != null && IsChildOf(box))
                {
                    Delete();

                    int deposited = 0;

                    int toAdd = m_Worth;

                    Gold gold;

                    while (toAdd > 60000)
                    {
                        gold = new Gold(60000);

                        if (box.TryDropItem(from, gold, false))
                        {
                            toAdd -= 60000;
                            deposited += 60000;
                        }
                        else
                        {
                            gold.Delete();

                            from.AddToBackpack(new BankCheck(toAdd));
                            toAdd = 0;

                            break;
                        }
                    }

                    if (toAdd > 0)
                    {
                        gold = new Gold(toAdd);

                        if (box.TryDropItem(from, gold, false))
                        {
                            deposited += toAdd;
                        }
                        else
                        {
                            gold.Delete();

                            from.AddToBackpack(new BankCheck(toAdd));
                        }
                    }

                    // Gold was deposited in your account:
                    from.SendLocalizedMessage(1042672, true, " " + deposited.ToString());
                }
                else
                {
                    from.SendLocalizedMessage(1047026); // That must be in your bank box to use it.
                }
            }
            else
                from.SendMessage("That refund check will become valid {0}", m_ValidDate);
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Worth);
            writer.Write(m_ValidDate);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Worth = reader.ReadInt();
                        m_ValidDate = reader.ReadDateTime();
                        break;
                    }
            }
        }
    }
    [Obsolete("Legacy instrument type — no longer used. Do not create.")]
    public class CashiersCheck : Item
    {
        private Serial m_ownerSerial = Serial.MinusOne;
        private int m_Worth;
        [CommandProperty(AccessLevel.GameMaster)]
        public int Worth
        {
            get { return m_Worth; }
            set { m_Worth = value; InvalidateProperties(); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int OwnerSerial
        {
            get
            {
                return m_ownerSerial;
            }
            set
            {
                m_ownerSerial = value;
                InvalidateProperties();
            }
        }

        public CashiersCheck(Serial serial)
            : base(serial)
        {
        }

        [Constructable]
        public CashiersCheck(int worth, Serial ownersSerial)
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = 0x4CC;    // pink!
            LootType = LootType.Newbied;
            Name = "cashier's check";
            Worth = worth;
            OwnerSerial = ownersSerial;
        }
        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, "A cashier's check worth {0}", m_Worth.ToString("N0"));
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (Deleted)
            {   // exploit! (they are trying to use a deleted check.)
                LogHelper Logger = new LogHelper("CashiersCheck.log", false);
                Logger.Log(LogType.Mobile, from, string.Format("exploit! they are trying to use a deleted cashier's check."));
                Logger.Finish();
                // jail time
                Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(from as Mobiles.PlayerMobile, 3, "they are trying to use a deleted cashier's check.", false);
                jt.GoToJail();

                // tell staff
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. they are trying to use a deleted cashier's check.", from as Mobiles.PlayerMobile));
                return;
            }

            BankBox box = from.BankBox;
            if (box != null && IsChildOf(box))
            {
                if (from != null && from.Serial != this.m_ownerSerial)
                {
                    from.SendMessage("This is not your check.");
                    from.SendMessage("Guards!");
                }
                else
                {
                    Delete();

                    int deposited = 0;

                    int toAdd = m_Worth;

                    Gold gold;

                    while (toAdd > 60000)
                    {
                        gold = new Gold(60000);

                        if (box.TryDropItem(from, gold, false))
                        {
                            toAdd -= 60000;
                            deposited += 60000;
                        }
                        else
                        {
                            gold.Delete();

                            from.AddToBackpack(new BankCheck(toAdd));
                            toAdd = 0;

                            break;
                        }
                    }

                    if (toAdd > 0)
                    {
                        gold = new Gold(toAdd);

                        if (box.TryDropItem(from, gold, false))
                        {
                            deposited += toAdd;
                        }
                        else
                        {
                            gold.Delete();

                            from.AddToBackpack(new BankCheck(toAdd));
                        }
                    }

                    // Gold was deposited in your account:
                    from.SendLocalizedMessage(1042672, true, " " + deposited.ToString());
                }
            }
            else
            {
                from.SendLocalizedMessage(1047026); // That must be in your bank box to use it.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);               // version

            writer.Write(m_Worth);
            writer.Write((int)m_ownerSerial);   // serial number of the owner of this unemployment check

        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Worth = reader.ReadInt();
                        m_ownerSerial = reader.ReadInt();
                        break;
                    }
            }
        }
    }
    [Obsolete("Legacy instrument type — no longer used. Do not create.")]
    public class UnemploymentCheck : BankCheck
    {
        private Serial m_ownerSerial = Serial.MinusOne;
        public UnemploymentCheck(Serial serial)
            : base(serial)
        {
        }

        [Constructable]
        public UnemploymentCheck(int worth)
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = 0x4CC;    // pink!
            LootType = LootType.Newbied;
            Name = "unemployment check";
            Worth = worth;
            if (Core.RuleSets.MortalisStyleRules())
                System.Diagnostics.Debug.Assert(false);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OwnerSerial
        {
            get
            {
                return m_ownerSerial;
            }
            set
            {
                m_ownerSerial = value;
                InvalidateProperties();
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            //from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1041361, "unemployment check", AffixType.Append, string.Concat(" ", Worth.ToString()), "")); // A bank check:

            LabelTo(from, "An unemployment check worth {0} gold", Worth);
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (Deleted)
            {   // exploit! (they are trying to use a deleted check.)
                LogHelper Logger = new LogHelper("UnemploymentCheckExploit.log", false);
                Logger.Log(LogType.Mobile, from, string.Format("exploit! they are trying to use a deleted unemployment check."));
                Logger.Finish();
                // jail time
                Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(from as Mobiles.PlayerMobile, 3, "they are trying to use a deleted unemployment check.", false);
                jt.GoToJail();

                // tell staff
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. they are trying to use a deleted unemployment check.", from as Mobiles.PlayerMobile));
                return;
            }

            BankBox box = from.BankBox;
            if (box != null && IsChildOf(box))
            {
                if (from != null && from.Serial != this.m_ownerSerial)
                {
                    from.SendMessage("This is not your check.");
                    from.SendMessage("Guards!");
                }
                else
                {
                    from.SendMessage("We cannot cash this check. It will however be accepted in most shops.");
                }
            }
            else
            {
                from.SendMessage("This check may be used for training and accepted in most shops.");
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);               // version

            writer.Write((int)m_ownerSerial);   // serial number of the owner of this unemployment check

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_ownerSerial = reader.ReadInt();
                        break;
                    }
            }
        }

        public static int GetAmount(Mobile from)
        {
            Container pack = from.Backpack;

            if (pack == null)
                return 0;

            int total = 0;

            foreach (UnemploymentCheck uc in pack.FindItemsByType<UnemploymentCheck>(true, check => check.OwnerSerial == from.Serial))
                total += uc.Worth;

            return total;
        }

        public static int ConsumeUpTo(Mobile from, int amount)
        {
            Container pack = from.Backpack;

            if (pack == null)
                return 0;

            int consumed = 0;

            foreach (UnemploymentCheck uc in pack.FindItemsByType<UnemploymentCheck>(true, check => check.OwnerSerial == from.Serial))
            {
                int need = amount - consumed;
                int theirAmount = uc.Worth;

                if (theirAmount <= need)
                {
                    uc.Delete();
                    consumed += theirAmount;
                }
                else
                {
                    uc.Worth -= need;
                    consumed += need;
                    break;
                }
            }

            return consumed;
        }
    }
    [Obsolete("Legacy instrument type — no longer used. Do not create.")]
    public class TrainingVoucher : BankCheck
    {
        private Serial m_ownerSerial = Serial.MinusOne;
        public TrainingVoucher(Serial serial)
            : base(serial)
        {
        }

        [Constructable]
        public TrainingVoucher(int worth)
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = 0x4CC;    // pink!
            LootType = LootType.Newbied;
            Name = "training voucher";
            Worth = worth;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OwnerSerial
        {
            get
            {
                return m_ownerSerial;
            }
            set
            {
                m_ownerSerial = value;
                InvalidateProperties();
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            //from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1041361, "unemployment check", AffixType.Append, string.Concat(" ", Worth.ToString()), "")); // A bank check:

            LabelTo(from, "A training voucher worth {0} gold", Worth);
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (Deleted)
            {   // exploit! (they are trying to use a deleted check.)
                LogHelper Logger = new LogHelper("TrainingVoucherExploit.log", false);
                Logger.Log(LogType.Mobile, from, string.Format("exploit! they are trying to use a deleted training voucher."));
                Logger.Finish();
                // jail time
                Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(from as Mobiles.PlayerMobile, 3, "they are trying to use a deleted training voucher.", false);
                jt.GoToJail();

                // tell staff
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. they are trying to use a deleted training voucher.", from as Mobiles.PlayerMobile));
                return;
            }

            BankBox box = from.BankBox;
            if (box != null && IsChildOf(box))
            {
                if (from != null && from.Serial != this.m_ownerSerial)
                {
                    from.SendMessage("This is not your voucher.");
                    from.SendMessage("Guards!");
                }
                else
                {
                    from.SendMessage("We cannot cash this voucher. It will however be accepted in most shops.");
                }
            }
            else
            {
                from.SendMessage("This voucher may be used for training and accepted in most shops.");
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);               // version

            writer.Write((int)m_ownerSerial);   // serial number of the owner of this unemployment check

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_ownerSerial = reader.ReadInt();
                        break;
                    }
            }
        }

        public static int GetAmount(Mobile from)
        {
            Container pack = from.Backpack;

            if (pack == null)
                return 0;

            int total = 0;

            foreach (TrainingVoucher uc in pack.FindItemsByType<TrainingVoucher>(true, check => check.OwnerSerial == from.Serial))
                total += uc.Worth;

            return total;
        }

        public static int ConsumeUpTo(Mobile from, int amount)
        {
            Container pack = from.Backpack;

            if (pack == null)
                return 0;

            int consumed = 0;

            foreach (TrainingVoucher uc in pack.FindItemsByType<TrainingVoucher>(true, check => check.OwnerSerial == from.Serial))
            {
                int need = amount - consumed;
                int theirAmount = uc.Worth;

                if (theirAmount <= need)
                {
                    uc.Delete();
                    consumed += theirAmount;
                }
                else
                {
                    uc.Worth -= need;
                    consumed += need;
                    break;
                }
            }

            return consumed;
        }
    }
}