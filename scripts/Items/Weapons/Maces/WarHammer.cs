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
    [FlipableAttribute(0x1439, 0x1438)]
    public class WarHammer : BaseBashing
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.WhirlwindAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.CrushingBlow; } }

        //		public override int AosStrengthReq{ get{ return 95; } }
        //		public override int AosMinDamage{ get{ return 17; } }
        //		public override int AosMaxDamage{ get{ return 18; } }
        //		public override int AosSpeed{ get{ return 28; } }
        //
        public override int OldMinDamage { get { return 8; } }
        public override int OldMaxDamage { get { return 36; } }
        public override int OldStrengthReq { get { return 40; } }
        public override int OldSpeed { get { return 31; } }

        public override int OldDieRolls { get { return 7; } }
        public override int OldDieMax { get { return 5; } }
        public override int OldAddConstant { get { return 1; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 110; } }

        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Bash2H; } }

        [Constructable]
        public WarHammer()
            : base(0x1439)
        {
            Weight = 10.0;
            Layer = Layer.TwoHanded;
        }

        public WarHammer(Serial serial)
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