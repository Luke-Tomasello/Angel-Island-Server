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

using Server.Engines.Craft;
using System;

namespace Server.Factions
{
    [CraftItemID(4523)]
    public class FactionGasTrap : BaseFactionTrap
    {
        public override int LabelNumber { get { return 1044598; } } // faction gas trap

        public override int AttackMessage { get { return 1010542; } } // A noxious green cloud of poison gas envelops you!
        public override int DisarmMessage { get { return 502376; } } // The poison leaks harmlessly away due to your deft touch.
        public override int EffectSound { get { return 0x230; } }
        public override int MessageHue { get { return 0x44; } }

        public override AllowedPlacing AllowedPlacing { get { return AllowedPlacing.FactionStronghold; } }

        public override void DoVisibleEffect()
        {
            Effects.SendLocationEffect(this.Location, this.Map, 0x3709, 28, 10, 0x1D3, 5);
        }

        public override void DoAttackEffect(Mobile m)
        {
            m.ApplyPoison(m, Poison.Lethal);
        }

        [Constructable]
        public FactionGasTrap() : this(null)
        {
        }

        public FactionGasTrap(Faction f) : this(f, null)
        {
        }

        public FactionGasTrap(Faction f, Mobile m) : base(f, m, 0x113C)
        {
        }

        public FactionGasTrap(Serial serial) : base(serial)
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

            int version = reader.ReadInt();
        }
    }

    public class FactionGasTrapDeed : BaseFactionTrapDeed
    {
        public override Type TrapType { get { return typeof(FactionGasTrap); } }
        public override int LabelNumber { get { return 1044602; } } // faction gas trap deed

        public FactionGasTrapDeed() : base(0x11AB)
        {
        }

        public FactionGasTrapDeed(Serial serial) : base(serial)
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
            int version = reader.ReadInt();
        }
    }
}