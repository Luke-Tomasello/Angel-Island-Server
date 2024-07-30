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

namespace Server.Items
{
    [FlipableAttribute(0x13B6, 0x13B5)]
    public class Scimitar : BaseSword
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.DoubleStrike; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ParalyzingBlow; } }

        //		public override int AosStrengthReq{ get{ return 25; } }
        //		public override int AosMinDamage{ get{ return 13; } }
        //		public override int AosMaxDamage{ get{ return 15; } }
        //		public override int AosSpeed{ get{ return 37; } }
        //
        public override int OldMinDamage { get { return 4; } }
        public override int OldMaxDamage { get { return 30; } }
        public override int OldStrengthReq { get { return 10; } }
        public override int OldSpeed { get { return 43; } }

        public override int OldDieRolls { get { return 2; } }
        public override int OldDieMax { get { return 14; } }
        public override int OldAddConstant { get { return 2; } }

        public override int DefHitSound { get { return 0x23B; } }
        public override int DefMissSound { get { return 0x23A; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 90; } }

        [Constructable]
        public Scimitar()
            : base(0x13B6)
        {
            Weight = 5.0;
        }

        public Scimitar(Serial serial)
            : base(serial)
        {
        }

        // old name removed, see base class

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