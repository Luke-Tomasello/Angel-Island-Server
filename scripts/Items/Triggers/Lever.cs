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

/* Items/Triggers/Lever.cs
 * CHANGELOG:
 * 	3/17/23, Yoar
 * 		Added AllowDead
 * 	3/8/23, Yoar
 * 		Added Telekinesisable + cleanups
 * 	2/11/22, Yoar
 * 		Initial version.
 */

using Server.Network;
using System.Collections;

namespace Server.Items.Triggers
{
    [TypeAlias("Server.Items.Lever")]
    [Flipable(
        0x108C, 0x1093, 0x108C,
        0x108E, 0x1095, 0x108E)]
    public class Lever : Item, ITriggerable, ITrigger, ITelekinesisable
    {
        protected virtual int DefaultUnswitchedID { get { return ((ItemID == 0x108C || ItemID == 0x108E) ? 0x108C : 0x1093); } }
        protected virtual int DefaultSwitchedID { get { return ((ItemID == 0x108C || ItemID == 0x108E) ? 0x108E : 0x1095); } }

        private Item m_Link;

        private int m_UnswitchedID;
        private int m_SwitchedID;
        private int m_SwitchSound;

        private string m_UseMessage;
        private string m_NoUseMessage;
        private int m_MessageHue;
        private bool m_MessageAscii;

        private bool m_Telekinesisable;

        private bool m_AllowDead;
        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UnswitchedID
        {
            get { return m_UnswitchedID; }
            set { m_UnswitchedID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SwitchedID
        {
            get { return m_SwitchedID; }
            set { m_SwitchedID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SwitchSound
        {
            get { return m_SwitchSound; }
            set { m_SwitchSound = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string UseMessage
        {
            get { return m_UseMessage; }
            set { m_UseMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string NoUseMessage
        {
            get { return m_NoUseMessage; }
            set { m_NoUseMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster), Hue]
        public int MessageHue
        {
            get { return m_MessageHue; }
            set { m_MessageHue = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool MessageAscii
        {
            get { return m_MessageAscii; }
            set { m_MessageAscii = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Telekinesisable
        {
            get { return m_Telekinesisable; }
            set { m_Telekinesisable = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowDead
        {
            get { return m_AllowDead; }
            set { m_AllowDead = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsSwitched
        {
            get { return (ItemID == GetSwitchedID()); }
            set
            {
                if (IsSwitched != value)
                    OnSwitch(null);
            }
        }

        [Constructable]
        public Lever()
            : this(0x1093)
        {
        }

        [Constructable]
        public Lever(int itemID)
            : base(itemID)
        {
            Movable = false;

            m_UnswitchedID = m_SwitchedID = -1;
            m_SwitchSound = 0x3E8;

            m_Telekinesisable = true;
        }

        protected int GetUnswitchedID()
        {
            return (m_UnswitchedID >= 0 ? m_UnswitchedID : DefaultUnswitchedID);
        }

        protected int GetSwitchedID()
        {
            return (m_SwitchedID >= 0 ? m_SwitchedID : DefaultSwitchedID);
        }

        public override void OnDoubleClickDead(Mobile from)
        {
            if (m_AllowDead)
                OnDoubleClick(from);
            else
                base.OnDoubleClickDead(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this, 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            OnSwitch(from);
        }

        public void OnTelekinesis(Mobile from)
        {
            Effects.SendLocationParticles(EffectItem.Create(GetWorldLocation(), Map, EffectItem.DefaultDuration), 0x376A, 9, 32, 5022);
            Effects.PlaySound(GetWorldLocation(), Map, 0x1F5);

            if (!m_Telekinesisable)
            {
                from.SendLocalizedMessage(501855); // Your magic seems to have no effect.
                return;
            }

            OnSwitch(from);
        }

        public void OnSwitch(Mobile from)
        {
            if (Visible)
                Effects.PlaySound(Location, Map, m_SwitchSound);

            if (IsSwitched)
                ItemID = GetUnswitchedID();
            else
                ItemID = GetSwitchedID();

            if (TriggerSystem.CanTrigger(from, this)) // make sure WE can trigger
            {
                if (TriggerSystem.CheckTrigger(from, m_Link))
                    SendMessageTo(from, m_UseMessage);
                else
                    SendMessageTo(from, m_NoUseMessage);
            }
            else
                SendMessageTo(from, m_NoUseMessage);
        }

        private void SendMessageTo(Mobile from, string text)
        {
            if (from == null || text == null)
                return;

            from.LocalOverheadMessage(MessageType.Regular, m_MessageHue, m_MessageAscii, text);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(((ITriggerable)this).CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        #region ITriggerable

        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return true;
        }

        void ITriggerable.OnTrigger(Mobile from)
        {
            OnSwitch(from);
        }

        #endregion

        public Lever(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)4); // version

            // version 4
            writer.Write(m_Event);

            // version 3
            writer.Write((bool)m_AllowDead);

            writer.Write((bool)m_Telekinesisable);

            writer.Write((int)m_UnswitchedID);
            writer.Write((int)m_SwitchedID);

            writer.Write((Item)m_Link);
            writer.Write((string)m_UseMessage);
            writer.Write((string)m_NoUseMessage);
            writer.Write((bool)m_MessageAscii);
            writer.Write((int)m_MessageHue);
            writer.Write((int)m_SwitchSound);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        m_Event = reader.ReadString();

                        goto case 3;
                    }
                case 3:
                    {
                        m_AllowDead = reader.ReadBool();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Telekinesisable = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        m_UnswitchedID = reader.ReadInt();
                        m_SwitchedID = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Link = reader.ReadItem();

                        if (version < 2)
                            m_UseMessage = new TextEntry(reader).String;
                        else
                            m_UseMessage = reader.ReadString();

                        if (version < 2)
                            m_NoUseMessage = new TextEntry(reader).String;
                        else
                            m_NoUseMessage = reader.ReadString();

                        m_MessageAscii = reader.ReadBool();
                        m_MessageHue = reader.ReadInt();
                        m_SwitchSound = reader.ReadInt();

                        break;
                    }
            }

            if (version < 2)
            {
                if (m_UnswitchedID == 0)
                    m_UnswitchedID = -1;

                if (m_SwitchedID == 0)
                    m_SwitchedID = -1;

                m_Telekinesisable = true;
            }
        }
    }
}