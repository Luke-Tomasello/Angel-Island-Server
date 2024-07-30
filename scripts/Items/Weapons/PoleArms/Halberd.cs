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
    [FlipableAttribute(0x143E, 0x143F)]
    public class Halberd : BasePoleArm
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.WhirlwindAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ConcussionBlow; } }

        //		public override int AosStrengthReq{ get{ return 95; } }
        //		public override int AosMinDamage{ get{ return 18; } }
        //		public override int AosMaxDamage{ get{ return 19; } }
        //		public override int AosSpeed{ get{ return 25; } }
        //
        public override int OldMinDamage { get { return 5; } }
        public override int OldMaxDamage { get { return 49; } }
        public override int OldStrengthReq { get { return 45; } }
        public override int OldSpeed { get { return 25; } }

        public override int OldDieRolls { get { return 2; } }
        public override int OldDieMax { get { return 23; } }
        public override int OldAddConstant { get { return 3; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 80; } }

        [Constructable]
        public Halberd()
            : base(0x143E)
        {
            Weight = 16.0;
        }

        public Halberd(Serial serial)
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