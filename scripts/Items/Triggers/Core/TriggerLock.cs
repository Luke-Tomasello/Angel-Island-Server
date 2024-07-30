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

/* Items/Triggers/Core/TriggerLock.cs
 * CHANGELOG:
 *  3/22/2024, Adam (CanTrigger/OnTrigger)
 *      Revert some recent changes that were preventing all/most existing controller chains from functioning.
 *  6/4/23, Yoar
 *      Added Manual_Clear property
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using System;
using System.Collections;

namespace Server.Items.Triggers
{
    public class TriggerLock : Item, ITriggerable, ITrigger
    {
        public enum _LockType : byte
        {
            This,
            From,
        }

        public override string DefaultName { get { return "Trigger Lock"; } }

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }

        private Item m_Link;
        private Item m_Else;

        private _LockType m_LockType;
        private TimeSpan m_LockDelay;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Else
        {
            get { return m_Else; }
            set { m_Else = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public _LockType LockType
        {
            get { return m_LockType; }
            set { m_LockType = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan LockDelay
        {
            get { return m_LockDelay; }
            set { m_LockDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Clear
        {
            get { return false; }
            set
            {
                if (value)
                    m_Memory.WipeMemory();
            }
        }

        [Constructable]
        public TriggerLock()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;
#if false
            return TriggerSystem.CanTrigger(from, !IsLocked(from) ? m_Link : m_Else);
#else
            return (!IsLocked(from) && TriggerSystem.CanTrigger(from, m_Link));
#endif
        }

        public void OnTrigger(Mobile from)
        {
            BeginLock(from);
#if false
            TriggerSystem.OnTrigger(from, !IsLocked(from) ? m_Link : m_Else);
#else
            TriggerSystem.OnTrigger(from, m_Link);
#endif
        }

        private Memory m_Memory = new Memory();

        private void BeginLock(Mobile m)
        {
            if (m_LockDelay == TimeSpan.Zero)
                return;

            m_Memory.Remember(GetLock(m), m_LockDelay.TotalSeconds);
        }

        private bool IsLocked(Mobile m)
        {
            return (m_Memory.Recall(GetLock(m)) != null);
        }

        private object GetLock(Mobile m)
        {
            switch (m_LockType)
            {
                default:
                    {
                        return this;
                    }
                case _LockType.From:
                    {
                        return m;
                    }
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        public TriggerLock(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write((Item)m_Else);

            // version 1
            writer.Write(m_Event);

            // version 0
            writer.Write((Item)m_Link);

            writer.Write((byte)m_LockType);
            writer.Write((TimeSpan)m_LockDelay);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Else = reader.ReadItem();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Event = reader.ReadString();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Link = reader.ReadItem();

                        m_LockType = (_LockType)reader.ReadByte();
                        m_LockDelay = reader.ReadTimeSpan();

                        break;
                    }
            }
        }
    }
}