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

/* Items/Triggers/TallyCounter.cs
 * CHANGELOG:
 *  3/22/24, Yoar
 *      Added mob-bound tallies.
 * 	7/12/23, Yoar
 * 		Initial version.
 */

using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    public class TallyCounter : Item, ITriggerable
    {
        public enum _TallyMode : byte
        {
            This,
            From,
        }

        public override string DefaultName { get { return "Tally Counter"; } }

        private _TallyMode m_TallyMode;
        private int m_Value;
        private Dictionary<Mobile, int> m_MobValues;
        private DiceEntry m_Increment;

        [CommandProperty(AccessLevel.GameMaster)]
        public _TallyMode TallyMode
        {
            get { return m_TallyMode; }
            set { m_TallyMode = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value
        {
            get { return m_Value; }
            set { m_Value = value; InvalidateProperties(); }
        }

        public Dictionary<Mobile, int> MobValues
        {
            get { return m_MobValues; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DiceEntry Increment
        {
            get { return m_Increment; }
            set { m_Increment = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Wipe
        {
            get { return false; }
            set
            {
                if (value)
                {
                    m_Value = 0;

                    m_MobValues.Clear();
                }
            }
        }

        [Constructable]
        public TallyCounter()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_MobValues = new Dictionary<Mobile, int>();
            m_Increment = new DiceEntry(0, 0, 1);
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public void OnTrigger(Mobile from)
        {
            switch (m_TallyMode)
            {
                case _TallyMode.This:
                    {
                        m_Value += m_Increment.Roll();

                        break;
                    }
                case _TallyMode.From:
                    {
                        if (from == null)
                            break;

                        int add = m_Increment.Roll();

                        if (m_MobValues.ContainsKey(from))
                            m_MobValues[from] += add;
                        else
                            m_MobValues[from] = add;

                        break;
                    }
            }
        }

        public int GetValue(Mobile from)
        {
            int value;

            if (m_MobValues.TryGetValue(from, out value))
                return value;

            return 0;
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
                list.Add(new TriggerSystem.ActivateCME(((ITriggerable)this).CanTrigger(from)));
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

        public TallyCounter(Serial serial)
            : base(serial)
        {
            m_MobValues = new Dictionary<Mobile, int>();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((byte)m_TallyMode);

            writer.Write((int)m_MobValues.Count);

            foreach (KeyValuePair<Mobile, int> kvp in m_MobValues)
            {
                writer.Write((Mobile)kvp.Key);
                writer.Write((int)kvp.Value);
            }

            writer.Write((int)m_Value);
            m_Increment.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_TallyMode = (_TallyMode)reader.ReadByte();

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            Mobile mob = reader.ReadMobile();
                            int value = reader.ReadInt();

                            if (mob != null)
                                m_MobValues[mob] = value;
                        }

                        goto case 0;
                    }
                case 0:
                    {
                        m_Value = reader.ReadInt();
                        m_Increment = new DiceEntry(reader);

                        break;
                    }
            }
        }
    }
}