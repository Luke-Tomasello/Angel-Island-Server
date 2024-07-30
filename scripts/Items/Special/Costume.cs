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

/* Items/Special/Costume.cs
 * CHANGELOG:
 * 	3/22/23, Yoar
 * 		Initial version.
 */

using System.Collections.Generic;

namespace Server.Items
{
    public abstract class BaseCostume : BaseOtherEquipable
    {
        public abstract Body Body { get; }

        public BaseCostume(int itemId)
            : base(itemId)
        {
            Layer = Layer.MiddleTorso;
        }

        private static Dictionary<Mobile, BaseCostume> m_Table = new Dictionary<Mobile, BaseCostume>();

        public static bool IsTransformed(Mobile m, out BaseCostume costume)
        {
            return m_Table.TryGetValue(m, out costume);
        }

        public virtual bool CanTransform(Mobile m)
        {
            return !m.IsBodyMod;
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile)
            {
                Mobile m = (Mobile)parent;

                if (CanTransform(m))
                    Transform(m);
            }
        }

        public void Transform(Mobile m)
        {
            if (m.Mount != null)
                m.Mount.Rider = null;

            m.BodyMod = Body;
            m.HueMod = 0;

            m.RevealingAction();

            m_Table[m] = this;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                Mobile m = (Mobile)parent;
                BaseCostume costume;

                if (IsTransformed(m, out costume) && costume == this)
                    Untransform(m);
            }
        }

        public void Untransform(Mobile m)
        {
            m.BodyMod = 0;
            m.HueMod = -1;

            m.RevealingAction();
            m.FixedParticles(0x3728, 1, 13, 0x13B2, EffectLayer.Waist);
            m.PlaySound(0xFA);

            m_Table.Remove(m);
        }

        public BaseCostume(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadInt();

            m_Queue.Add(this);
        }

        private static readonly List<BaseCostume> m_Queue = new List<BaseCostume>();

        public static void Initialize()
        {
            foreach (BaseCostume costume in m_Queue)
                costume.Validate();

            m_Queue.Clear();
        }

        public void Validate()
        {
            if (Parent is Mobile)
            {
                Mobile m = (Mobile)Parent;

                if (CanTransform(m) && !m.Mounted)
                {
                    m.BodyMod = Body;
                    m.HueMod = 0;
                    m_Table[m] = this;
                }
            }
        }
    }

    public class CustomCostume : BaseCostume
    {
        public override Body Body { get { return new Body(m_BodyValue); } }

        private int m_BodyValue;

        [CommandProperty(AccessLevel.GameMaster)]
        public int BodyValue
        {
            get { return m_BodyValue; }
            set { m_BodyValue = value; }
        }

        [Constructable]
        public CustomCostume()
            : this(0)
        {
        }

        [Constructable]
        public CustomCostume(int bodyValue)
            : base(0x1541)
        {
            m_BodyValue = bodyValue;
        }

        public CustomCostume(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_BodyValue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadInt();

            m_BodyValue = reader.ReadInt();
        }
    }
}