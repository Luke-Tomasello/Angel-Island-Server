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

/* Scripts\misc\Tickets.cs
 * ChangeLog:
 *	6/24/2023, Adam
 *		Created
 *		Sold on bard NPCs, these tickets allow players, with minimal staff help, to
 *		host their own player-run event. 
 *		By double clicking, players add an event name, event duration, passcode, and set the tickets color.
 *		Staff will take this passcode and plug it into a moongate, sungate, event sungate, etc.
 *		This limits users of the moongate to only those holding a valid ticket.
 */

using Server.Prompts;
using System;

namespace Server.Items
{
    public class Ticket : Item
    {
        private iFlags m_Flags = iFlags.None;
        private DateTime m_ExpirationDate = DateTime.MinValue;
        private string m_Passcode;

        [Flags]
        public enum iFlags
        {
            None = 0x00,
            Named = 0x01,
            Scheduled = 0x02 | Named,
            Expired = 0x04,
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public iFlags Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ExpirationDate
        {
            get { return m_ExpirationDate; }
            set { m_ExpirationDate = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Passcode
        {
            get { return m_Passcode; }
            set { m_Passcode = value; }
        }

        public enum TicketColor
        {
            White = 0,
            Green = 0x89F,
            Blue = 0x8AB,
            Gold = 0x8A5,
        }

        [Constructable]
        public Ticket()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "a blank ticket";
            LootType = LootType.Newbied;
        }

        public Ticket(Serial serial)
            : base(serial)
        {
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);// That must be in your pack for you to use it.
            }
            else if ((m_Flags & iFlags.Scheduled) != iFlags.Scheduled && (m_Flags & iFlags.Expired) != iFlags.Expired)
            {
                from.SendMessage("What will you name this event?");
                from.Prompt = new ActivateTicketPrompt(this);
            }
            else
            {
                if ((m_Flags & iFlags.Expired) == iFlags.Expired)
                    from.SendMessage("Sorry, this ticket has expired.");
                else
                    from.SendMessage("This event has already been scheduled and may not be changed.");
            }
        }
        public bool Expired { get { return m_ExpirationDate != DateTime.MinValue && DateTime.UtcNow > m_ExpirationDate; } }
        public override void OnSingleClick(Mobile from)
        {
            try
            {
                if (Deleted || !from.CanSee(this))
                    return;

                if (Expired)
                    m_Flags = iFlags.Expired;

                string text = Name;
                if (m_Flags == iFlags.None)
                    text = Name;
                else if (m_Flags == iFlags.Named)
                    text = String.Format("A ticket to {0}, not yet scheduled.", Name);
                else if (Expired)
                    text = String.Format("A ticket to {0}, expired.", Name);
                else
                    text = String.Format("A ticket to {0}", Name);

                LabelTo(from, text);

                return;
            }
            catch { }
        }
        private class ActivateTicketPrompt : Prompt
        {
            private Ticket m_ticket;

            public ActivateTicketPrompt(Ticket ticket)
            {
                m_ticket = ticket;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40);

                if (from.CheckAlive())
                {
                    m_ticket.Name = Utility.FixHtml(text.Trim());

                    from.SendMessage("Event name set.");
                    m_ticket.Flags |= iFlags.Named;

                    from.SendMessage("When will this ticket expire in hours, minutes, seconds?");
                    from.SendMessage("Example: HH:MM:SS");
                    from.Prompt = new ScheduleTicketPrompt(m_ticket);
                }
                else
                    from.SendMessage("You are dead and cannot do that.");
            }

            public override void OnCancel(Mobile from)
            {
            }
        }

        private class ScheduleTicketPrompt : Prompt
        {
            private Ticket m_ticket;

            public ScheduleTicketPrompt(Ticket ticket)
            {
                m_ticket = ticket;
            }
            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40);


                TimeSpan ts;
                if (Utility.TryParseTimeSpan(text, out ts) == false)
                {
                    from.SendMessage("Bad format.");
                    return;
                }
                else
                {
                    m_ticket.ExpirationDate = DateTime.UtcNow + ts;
                    m_ticket.Flags = iFlags.Scheduled;
                    from.SendMessage("Scheduled. This ticket will expire on {0} UTC", m_ticket.ExpirationDate);

                    from.SendMessage("Enter a passcode for this ticket.");
                    from.SendMessage("This passcode is used by the teleporter or moongate to identify the ticket.");
                    from.Prompt = new TicketPasscodePrompt(m_ticket);
                }
            }

            public override void OnCancel(Mobile from)
            {
            }
        }

        private class TicketPasscodePrompt : Prompt
        {
            private Ticket m_ticket;

            public TicketPasscodePrompt(Ticket ticket)
            {
                m_ticket = ticket;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40);

                if (from.CheckAlive())
                {
                    m_ticket.Passcode = Utility.FixHtml(text.Trim());
                    from.SendMessage("Passcode set.");

                    from.SendMessage("Enter color for this ticket.");
                    from.SendMessage("Valid colors are White, Green, Blue, and Gold.");
                    from.Prompt = new TicketColorPrompt(m_ticket);
                }
                else
                    from.SendMessage("You are dead and cannot do that.");
            }

            public override void OnCancel(Mobile from)
            {
            }
        }

        private class TicketColorPrompt : Prompt
        {
            private Ticket m_ticket;

            public TicketColorPrompt(Ticket ticket)
            {
                m_ticket = ticket;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40);

                if (from.CheckAlive())
                {
                    try
                    {
                        m_ticket.Hue = (int)Enum.Parse(typeof(TicketColor), text, true);
                        from.SendMessage("Color set.");
                    }
                    catch
                    { from.SendMessage("Invalid Color."); }
                }
                else
                    from.SendMessage("You are dead and cannot do that.");
            }

            public override void OnCancel(Mobile from)
            {
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write((int)m_Flags);
            writer.Write(m_ExpirationDate);
            writer.Write(m_Passcode);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    m_Flags = (iFlags)reader.ReadInt();
                    m_ExpirationDate = reader.ReadDateTime();
                    m_Passcode = reader.ReadString();
                    goto default;

                default:
                    break;
            }

            if (Expired)
                m_Flags = iFlags.Expired;
        }
    }
}