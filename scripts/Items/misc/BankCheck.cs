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
            from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1041361, "", AffixType.Append, String.Concat(" ", m_Worth.ToString("N0") /*m_Worth.ToString()*/), "")); // A bank check:
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

                PlayerMobile pm = from as PlayerMobile;

                if (pm != null)
                {
                    QuestSystem qs = pm.Quest;

                    if (qs is Necro.DarkTidesQuest)
                    {
                        QuestObjective obj = qs.FindObjective(typeof(Necro.CashBankCheckObjective));

                        if (obj != null && !obj.Completed)
                            obj.Complete();
                    }

                    if (qs is Haven.UzeraanTurmoilQuest)
                    {
                        QuestObjective obj = qs.FindObjective(typeof(Haven.CashBankCheckObjective));

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

                    PlayerMobile pm = from as PlayerMobile;

                    if (pm != null)
                    {
                        QuestSystem qs = pm.Quest;

                        if (qs is Necro.DarkTidesQuest)
                        {
                            QuestObjective obj = qs.FindObjective(typeof(Necro.CashBankCheckObjective));

                            if (obj != null && !obj.Completed)
                                obj.Complete();
                        }

                        if (qs is Haven.UzeraanTurmoilQuest)
                        {
                            QuestObjective obj = qs.FindObjective(typeof(Haven.CashBankCheckObjective));

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
            //from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1041361, "unemployment check", AffixType.Append, String.Concat(" ", Worth.ToString()), "")); // A bank check:

            LabelTo(from, "An unemployment check worth {0}", Worth);
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


}