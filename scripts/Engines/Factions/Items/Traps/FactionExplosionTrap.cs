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

using System;

namespace Server.Factions
{
    public class FactionExplosionTrap : BaseFactionTrap
    {
        public override int LabelNumber { get { return 1044599; } } // faction explosion trap

        public override int AttackMessage { get { return 1010543; } } // You are enveloped in an explosion of fire!
        public override int DisarmMessage { get { return 1010539; } } // You carefully remove the pressure trigger and disable the trap.
        public override int EffectSound { get { return 0x307; } }
        public override int MessageHue { get { return 0x78; } }

        public override AllowedPlacing AllowedPlacing { get { return AllowedPlacing.AnyFactionTown; } }

        public override void DoVisibleEffect()
        {
            Effects.SendLocationEffect(GetWorldLocation(), Map, 0x36BD, 15, 10);
        }

        public override void DoAttackEffect(Mobile m)
        {
            m.Damage(Utility.Dice(6, 10, 40), m, this);
        }

        [Constructable]
        public FactionExplosionTrap() : this(null)
        {
        }

        public FactionExplosionTrap(Faction f) : this(f, null)
        {
        }

        public FactionExplosionTrap(Faction f, Mobile m) : base(f, m, 0x11C1)
        {
        }

        public FactionExplosionTrap(Serial serial) : base(serial)
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

    public class FactionExplosionTrapDeed : BaseFactionTrapDeed
    {
        public override Type TrapType { get { return typeof(FactionExplosionTrap); } }
        public override int LabelNumber { get { return 1044603; } } // faction explosion trap deed

        public FactionExplosionTrapDeed() : base(0x36D2)
        {
        }

        public FactionExplosionTrapDeed(Serial serial) : base(serial)
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