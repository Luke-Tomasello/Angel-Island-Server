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

/* Scripts/Items/Deeds/ClaimTicket.cs
 * ChangeLog
 *  11/18/26, Adam (bank claim)
 *      Created. A claim ticket for a crate held in internal storage (IsIntMapStorage, so the cron's cleanup leaves it
 *      alone). The Music Conservatory's opening (PatchV273) moves each character's bank into a crate and puts one of
 *      these in the backpack, so every bank has room for the composer's kit. Double-clicked by its owner, inside a
 *      house they own or co-own, the crate is delivered at their feet and the ticket is used up. Logged
 *      (Bank Claims.log). A ticket deleted before its crate is delivered deletes the crate too (nothing else could
 *      claim it); a delivered crate (out of internal storage) is the player's and stays.
 */

using Server.Diagnostics;
using System;

namespace Server.Items
{
    public class ClaimTicket : Item
    {
        private Mobile? m_Owner;
        private Item? m_Crate;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile? Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item? Crate
        {
            get { return m_Crate; }
            set { m_Crate = value; }
        }

        public const string LogName = "Bank Claims.log";

        [Constructable]
        public ClaimTicket()
            : base(0x14F0)
        {
            Name = "a claim ticket";
            Hue = (int)Ticket.TicketColor.Gold;
            Weight = 1.0;
            LootType = LootType.Newbied;
        }

        public ClaimTicket(Mobile owner, Item crate)
            : this()
        {
            m_Owner = owner;
            m_Crate = crate;
        }

        public ClaimTicket(Serial serial)
            : base(serial)
        {
        }

        public override bool DisplayLootType { get { return false; } }

        private int CrateItems { get { return m_Crate is Container c && !c.Deleted ? c.TotalItems : 0; } }

        public override void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
                return;

            LabelTo(from, string.Format("a claim ticket: your bank's contents, {0} items, held in a crate", CrateItems));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }
            if (from != m_Owner && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("This claim ticket isn't yours.");
                return;
            }
            if (m_Crate == null || m_Crate.Deleted)
            {
                from.SendMessage("The crate this ticket claims can't be found. Please page staff, and keep this ticket.");
                Log(from, "crate not found");
                return;
            }
            Multis.BaseHouse? house = Multis.BaseHouse.FindHouseAt(from);
            if (house == null || !house.IsCoOwner(from))
            {
                from.SendMessage("Use this ticket inside a house you own or co-own, and your crate will be delivered at your feet.");
                return;
            }

            int items = CrateItems;
            m_Crate.RetrieveItemFromIntStorage(from.Location, from.Map);
            from.SendMessage("Your crate has been delivered: {0} items from your bank.", items);
            Log(from, string.Format("delivered {0} items at {1} {2} ({3})", items, from.Location, from.Map, house.Sign?.Name ?? "house"));
            Delete();
        }

        // a ticket deleted before its crate was delivered takes the crate with it (nothing else could ever claim it)
        public override void OnDelete()
        {
            if (m_Crate != null && !m_Crate.Deleted && m_Crate.IsIntMapStorage)
            {
                Log(m_Owner, string.Format("ticket deleted: undelivered crate deleted with it ({0} items)", CrateItems));
                m_Crate.IsIntMapStorage = false;
                m_Crate.Delete();
            }
            base.OnDelete();
        }

        private void Log(Mobile? from, string what)
        {
            using (LogHelper logger = new LogHelper(LogName, false, true))
            {
                logger.Log(LogType.Text, string.Format("claim ticket {0} for {1} (crate {2}): {3}",
                    Serial, from?.ToString() ?? "no owner", m_Crate?.Serial.ToString() ?? "none", what));
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Owner);
            writer.Write(m_Crate);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Owner = reader.ReadMobile();
                        m_Crate = reader.ReadItem();
                        break;
                    }
            }
        }
    }
}
