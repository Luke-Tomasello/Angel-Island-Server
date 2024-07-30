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

/* Items/Triggers/Core/TriggerConditional.cs
 * CHANGELOG:
 *  7/12/23, Yoar
 *      Added "Else" link
 *  6/4/23, Yoar
 *      Now supporting Mobile targets
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using Server.Commands;
using System.Collections;

namespace Server.Items.Triggers
{
    public class TriggerConditional : Item, ITriggerable, ITrigger
    {
        public override string DefaultName { get { return "Trigger Conditional"; } }

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }

        private Item m_Link;
        private Item m_Else;

        private string m_ConditionStr;
        private ObjectConditional m_ConditionImpl;
        private IEntity m_Target;

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

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public string Condition
        {
            get { return m_ConditionStr; }
            set
            {
                if (value != null)
                {
                    try
                    {
                        string[] args = CommandSystem.Split(value);
                        m_ConditionImpl = ObjectConditional.Parse(null, ref args);
                        m_ConditionStr = value;
                    }
                    catch
                    {
                        m_ConditionImpl = null;
                        m_ConditionStr = null;
                    }
                }
                else
                {
                    m_ConditionImpl = null;
                    m_ConditionStr = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile TargetMobile
        {
            get { return m_Target as Mobile; }
            set { m_Target = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item TargetItem
        {
            get { return m_Target as Item; }
            set { m_Target = value; }
        }

        [Constructable]
        public TriggerConditional()
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

            bool result;
            return (CheckCondition(from, out result) && TriggerSystem.CanTrigger(from, result ? m_Link : m_Else));
        }

        public void OnTrigger(Mobile from)
        {
            bool result;

            if (CheckCondition(from, out result))
                TriggerSystem.OnTrigger(from, result ? m_Link : m_Else);
        }

        private bool CheckCondition(Mobile from, out bool result)
        {
            result = false;

            if (m_ConditionImpl == null)
                return false;

            object target = m_Target;

            if (target == null)
                target = from;

            if (target == null)
                return false;

            try
            {
                result = m_ConditionImpl.CheckCondition(target);
            }
            catch
            {
                return false;
            }

            return true;
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

        public TriggerConditional(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3
            writer.Write(m_Event);

            // version 2
            writer.Write((Item)m_Else);

            writer.Write((Item)m_Link);

            writer.Write((string)m_ConditionStr);

            if (m_Target is Item)
                writer.Write((Item)m_Target);
            else if (m_Target is Mobile)
                writer.Write((Mobile)m_Target);
            else
                writer.Write(Serial.MinusOne);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_Event = reader.ReadString();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Else = reader.ReadItem();

                        goto case 1;
                    }
                case 1:
                case 0:
                    {
                        m_Link = reader.ReadItem();

                        Condition = reader.ReadString();

                        if (version < 1)
                            m_Target = reader.ReadItem();
                        else
                            m_Target = World.FindEntity(reader.ReadInt());

                        break;
                    }
            }
        }
    }
}