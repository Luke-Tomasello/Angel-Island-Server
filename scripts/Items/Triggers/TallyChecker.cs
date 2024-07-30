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

/* Items/Triggers/TallyChecker.cs
 * CHANGELOG:
 * 	3/22/24, Yoar
 * 		Initial version.
 */

using System.Collections;

namespace Server.Items.Triggers
{
    public class TallyChecker : Item, ITriggerable, ITrigger
    {
        public enum _Comparison : byte
        {
            Equal,
            NotEqual,
            Greater,
            GreaterEqual,
            Lesser,
            LesserEqual,
        }

        public override string DefaultName { get { return "Tally Checker"; } }

        private Item m_Link;
        private Item m_Else;

        private TallyCounter m_Tally;
        private _Comparison m_Comparison;
        private int m_CompareValue;

        private bool m_WipeOnSuccess;

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
        public TallyCounter Tally
        {
            get { return m_Tally; }
            set { m_Tally = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public _Comparison Comparison
        {
            get { return m_Comparison; }
            set { m_Comparison = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CompareValue
        {
            get { return m_CompareValue; }
            set { m_CompareValue = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool WipeOnSuccess
        {
            get { return m_WipeOnSuccess; }
            set { m_WipeOnSuccess = value; }
        }

        private string m_Event;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }

        [Constructable]
        public TallyChecker()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Comparison = _Comparison.GreaterEqual;
            m_CompareValue = 1;
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

            bool result;
            return (DoComparison(from, out result) && TriggerSystem.CanTrigger(from, result ? m_Link : m_Else));
        }

        public void OnTrigger(Mobile from)
        {
            bool result;

            if (DoComparison(from, out result))
            {
                TriggerSystem.OnTrigger(from, result ? m_Link : m_Else);

                if (result && m_WipeOnSuccess)
                    WipeValue(m_Tally, from);
            }
        }

        private bool DoComparison(Mobile from, out bool result)
        {
            if (m_Tally == null || m_Tally.Deleted)
            {
                result = false;
                return false;
            }

            int value = GetValue(m_Tally, from);

            switch (m_Comparison)
            {
                case _Comparison.Equal:
                    {
                        result = (value == m_CompareValue);
                        break;
                    }
                case _Comparison.NotEqual:
                    {
                        result = (value != m_CompareValue);
                        break;
                    }
                case _Comparison.Greater:
                    {
                        result = (value > m_CompareValue);
                        break;
                    }
                case _Comparison.GreaterEqual:
                    {
                        result = (value >= m_CompareValue);
                        break;
                    }
                case _Comparison.Lesser:
                    {
                        result = (value < m_CompareValue);
                        break;
                    }
                case _Comparison.LesserEqual:
                    {
                        result = (value <= m_CompareValue);
                        break;
                    }
                default:
                    {
                        result = false;
                        return false;
                    }
            }

            return true;
        }

        private static int GetValue(TallyCounter tally, Mobile from)
        {
            switch (tally.TallyMode)
            {
                case TallyCounter._TallyMode.This:
                    {
                        return tally.Value;
                    }
                case TallyCounter._TallyMode.From:
                    {
                        if (from == null)
                            return 0;

                        return tally.GetValue(from);
                    }
                default:
                    {
                        return 0;
                    }
            }
        }

        private static void WipeValue(TallyCounter tally, Mobile from)
        {
            switch (tally.TallyMode)
            {
                case TallyCounter._TallyMode.This:
                    {
                        tally.Value = 0;

                        break;
                    }
                case TallyCounter._TallyMode.From:
                    {
                        if (from == null)
                            return;

                        tally.MobValues.Remove(from);

                        break;
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

        #region ITriggerable

        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return true;
        }

        void ITriggerable.OnTrigger(Mobile from)
        {
            OnTrigger(from);
        }

        #endregion

        public TallyChecker(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((string)m_Event);

            writer.Write((bool)m_WipeOnSuccess);

            writer.Write((Item)m_Link);
            writer.Write((Item)m_Else);

            writer.Write((Item)m_Tally);
            writer.Write((byte)m_Comparison);
            writer.Write((int)m_CompareValue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Event = reader.ReadString();

                        goto case 1;
                    }
                case 1:
                    {
                        m_WipeOnSuccess = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Link = reader.ReadItem();
                        m_Else = reader.ReadItem();

                        m_Tally = reader.ReadItem() as TallyCounter;
                        m_Comparison = (_Comparison)reader.ReadByte();
                        m_CompareValue = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}