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

namespace Server.Items
{
    [FlipableAttribute(0x26C3, 0x26CD)]
    public class RepeatingCrossbow : BaseRanged
    {
        public override int EffectID { get { return 0x1BFE; } }
        public override Type AmmoType { get { return typeof(Bolt); } }
        public override Item Ammo { get { return new Bolt(); } }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.DoubleStrike; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.MovingShot; } }

        //		public override int AosStrengthReq{ get{ return 30; } }
        //		public override int AosMinDamage{ get{ return 10; } }
        //		public override int AosMaxDamage{ get{ return 12; } }
        //		public override int AosSpeed{ get{ return 41; } }
        //
        public override int OldMinDamage { get { return 10; } }
        public override int OldMaxDamage { get { return 12; } }
        public override int OldStrengthReq { get { return 30; } }
        public override int OldSpeed { get { return 41; } }

        public override int OldDieRolls { get { return 1; } }
        public override int OldDieMax { get { return 26; } }
        public override int OldAddConstant { get { return 2; } }

        public override int DefMaxRange { get { return 7; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 80; } }

        [Constructable]
        public RepeatingCrossbow()
            : base(0x26C3)
        {
            Weight = 6.0;
        }

        public RepeatingCrossbow(Serial serial)
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

            int version = reader.ReadInt();
        }
    }
}