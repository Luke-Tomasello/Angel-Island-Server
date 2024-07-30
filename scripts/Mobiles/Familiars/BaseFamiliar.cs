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

using Server.ContextMenus;
using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public abstract class BaseFamiliar : BaseCreature
    {
        public BaseFamiliar()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            BardImmune = true;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override bool Commandable { get { return false; } }
        protected virtual Mobile Focus
        {
            get
            {
                if (ControlMaster == null || ControlMaster.Deleted || ControlMaster.Map != this.Map || !InRange(ControlMaster.Location, 20))
                    return null;
                return ControlMaster.Combatant;
            }
        }

        private bool m_LastHidden;

        public override void OnThink()
        {
            base.OnThink();

            Mobile master = ControlMaster;

            if (master == null)
                return;

            if (master.Deleted || master.Map != this.Map || !InRange(master.Location, 20))
            {
                DropPackContents();
                EndRelease(null);
                return;
            }

            if (m_LastHidden != master.Hidden)
                Hidden = m_LastHidden = master.Hidden;

            Mobile toAttack = null;

            if (!Hidden)
            {
                toAttack = Focus;

                if (toAttack == this)
                    toAttack = master;
                else if (toAttack == null)
                    toAttack = this.Combatant;
            }

            if (Combatant != toAttack)
                Combatant = null;

            if (toAttack == null)
            {
                if (ControlTarget != master || ControlOrder != OrderType.Follow)
                {
                    ControlTarget = master;
                    ControlOrder = OrderType.Follow;
                }
            }
            else if (ControlTarget != toAttack || ControlOrder != OrderType.Attack)
            {
                ControlTarget = toAttack;
                ControlOrder = OrderType.Attack;
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive && Controlled && from == ControlMaster && from.InRange(this, 14))
                list.Add(new ReleaseEntry(from, this));
        }

        public virtual void BeginRelease(Mobile from)
        {
            if (!Deleted && Controlled && from == ControlMaster && from.CheckAlive())
                EndRelease(from);
        }

        public virtual void EndRelease(Mobile from)
        {
            if (from == null || (!Deleted && Controlled && from == ControlMaster && from.CheckAlive()))
            {
                Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 1, 13, 2100, 3, 5042, 0);
                PlaySound(0x201);
                OnBeforeDispel(from);
                Delete();
            }
        }

        public virtual void DropPackContents()
        {
            Map map = this.Map;
            Container pack = this.Backpack;

            if (map != null && map != Map.Internal && pack != null)
            {
                ArrayList list = new ArrayList(pack.Items);

                for (int i = 0; i < list.Count; ++i)
                    ((Item)list[i]).MoveToWorld(Location, map);
            }
        }

        public BaseFamiliar(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            DropPackContents();
            Delete();
        }

        private class ReleaseEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private BaseFamiliar m_Familiar;

            public ReleaseEntry(Mobile from, BaseFamiliar familiar)
                : base(6118, 14)
            {
                m_From = from;
                m_Familiar = familiar;
            }

            public override void OnClick()
            {
                if (!m_Familiar.Deleted && m_Familiar.Controlled && m_From == m_Familiar.ControlMaster && m_From.CheckAlive())
                    m_Familiar.BeginRelease(m_From);
            }
        }
    }
}