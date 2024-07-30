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

/* Items/Triggers/StatusController.cs
 * CHANGELOG:
 *  7/11/23, Yoar
 *      Added Despawn
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.Seventh;
using System.Collections;

namespace Server.Items.Triggers
{
    public class StatusController : Item, ITriggerable
    {
        public enum SCStatusType : byte
        {
            None,

            Hits,
            Stam,
            Mana,

            Damage,
            Heal,

            Kill,
            Resurrect,

            Poison,
            Cure,

            Reveal,
            Demorph,

            Despawn,
        }

        public override string DefaultName { get { return "Status Controller"; } }

        private SCStatusType m_StatusType;
        private int m_ValueMin;
        private int m_ValueMax;

        [CommandProperty(AccessLevel.GameMaster)]
        public SCStatusType StatusType
        {
            get { return m_StatusType; }
            set { m_StatusType = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ValueMin
        {
            get { return m_ValueMin; }
            set { m_ValueMin = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ValueMax
        {
            get { return m_ValueMax; }
            set { m_ValueMax = value; }
        }

        [Constructable]
        public StatusController()
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
        public void OnTrigger(Mobile from)
        {
            if (from != null)
                DoEffect(from);
        }

        private void DoEffect(Mobile m)
        {
            switch (m_StatusType)
            {
                case SCStatusType.Hits: m.Hits += Utility.RandomMinMax(m_ValueMin, m_ValueMax); break;
                case SCStatusType.Stam: m.Stam += Utility.RandomMinMax(m_ValueMin, m_ValueMax); break;
                case SCStatusType.Mana: m.Mana += Utility.RandomMinMax(m_ValueMin, m_ValueMax); break;

                case SCStatusType.Damage:
                    {
                        m.Damage(Utility.RandomMinMax(m_ValueMin, m_ValueMax), m, null);

                        break;
                    }
                case SCStatusType.Heal:
                    {
                        m.Heal(Utility.RandomMinMax(m_ValueMin, m_ValueMax));

                        break;
                    }

                case SCStatusType.Kill: m.Kill(); break;
                case SCStatusType.Resurrect: m.Resurrect(); break;

                case SCStatusType.Poison:
                    {
                        Poison poison = Poison.GetPoison(Utility.RandomMinMax(m_ValueMin, m_ValueMax));

                        if (poison == null)
                            return;

                        m.ApplyPoison(m, poison);

                        break;
                    }
                case SCStatusType.Cure:
                    {
                        m.CurePoison(m);

                        break;
                    }

                case SCStatusType.Reveal:
                    {
                        m.RevealingAction();

                        break;
                    }
                case SCStatusType.Demorph:
                    {
                        m.BodyMod = 0;
                        m.HueMod = -1;
                        m.NameMod = null;

                        PolymorphSpell.StopTimer(m);
                        IncognitoSpell.StopTimer(m);

                        if (DisguiseGump.IsDisguised(m))
                            DisguiseGump.StopTimer(m);

                        m.EndAction(typeof(PolymorphSpell));
                        m.EndAction(typeof(IncognitoSpell));

                        TransformationSpellHelper.RemoveContext(m, true);

                        break;
                    }

                case SCStatusType.Despawn:
                    {
                        if (m is BaseCreature && ((BaseCreature)m).Spawner != null)
                            m.Delete();

                        break;
                    }
            }
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

        public StatusController(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((byte)m_StatusType);
            writer.Write((int)m_ValueMin);
            writer.Write((int)m_ValueMax);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_StatusType = (SCStatusType)reader.ReadByte();
                        m_ValueMin = reader.ReadInt();
                        m_ValueMax = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}