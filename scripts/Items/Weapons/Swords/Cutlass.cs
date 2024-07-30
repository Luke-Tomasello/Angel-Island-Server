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

/* Scripts/Items/Weapons/Swords/Cutlass.cs
 * CHANGELOG
 *	01/10/05 - Pix
 *		Changed cutlass sound.
 */

namespace Server.Items
{
    [FlipableAttribute(0x1441, 0x1440)]
    public class Cutlass : BaseSword
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.BleedAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ShadowStrike; } }

        //		public override int AosStrengthReq{ get{ return 25; } }
        //		public override int AosMinDamage{ get{ return 11; } }
        //		public override int AosMaxDamage{ get{ return 13; } }
        //		public override int AosSpeed{ get{ return 44; } }
        //
        public override int OldMinDamage { get { return 6; } }
        public override int OldMaxDamage { get { return 28; } }
        public override int OldStrengthReq { get { return 10; } }
        public override int OldSpeed { get { return 45; } }

        public override int OldDieRolls { get { return 2; } }
        public override int OldDieMax { get { return 12; } }
        public override int OldAddConstant { get { return 4; } }

        public override int DefHitSound { get { return 0x23C; } }
        public override int DefMissSound { get { return 0x23A; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 70; } }

        [Constructable]
        public Cutlass()
            : base(0x1441)
        {
            Weight = 8.0;
        }

        public Cutlass(Serial serial)
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