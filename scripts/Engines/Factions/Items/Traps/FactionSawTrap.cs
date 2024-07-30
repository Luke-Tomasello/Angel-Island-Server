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
    [CraftItemID(4359)]
    public class FactionSawTrap : BaseFactionTrap
    {
        public override int LabelNumber { get { return 1041047; } } // faction saw trap

        public override int AttackMessage { get { return 1010544; } } // The blade cuts deep into your skin!
        public override int DisarmMessage { get { return 1010540; } } // You carefully dismantle the saw mechanism and disable the trap.
        public override int EffectSound { get { return 0x218; } }
        public override int MessageHue { get { return 0x5A; } }

        public override AllowedPlacing AllowedPlacing { get { return AllowedPlacing.ControlledFactionTown; } }

        public override void DoVisibleEffect()
        {
            Effects.SendLocationEffect(this.Location, this.Map, 0x11AD, 25, 10);
        }

        public override void DoAttackEffect(Mobile m)
        {
            m.Damage(Utility.Dice(6, 10, 40), m, this);
        }

        [Constructable]
        public FactionSawTrap() : this(null)
        {
        }

        public FactionSawTrap(Serial serial) : base(serial)
        {
        }

        public FactionSawTrap(Faction f) : this(f, null)
        {
        }

        public FactionSawTrap(Faction f, Mobile m) : base(f, m, 0x11AC)
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

    public class FactionSawTrapDeed : BaseFactionTrapDeed
    {
        public override Type TrapType { get { return typeof(FactionSawTrap); } }
        public override int LabelNumber { get { return 1044604; } } // faction saw trap deed

        public FactionSawTrapDeed() : base(0x1107)
        {
        }

        public FactionSawTrapDeed(Serial serial) : base(serial)
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