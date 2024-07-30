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
    [FlipableAttribute(0xF4D, 0xF4E)]
    public class Bardiche : BasePoleArm
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.ParalyzingBlow; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Dismount; } }

        //		public override int AosStrengthReq{ get{ return 45; } }
        //		public override int AosMinDamage{ get{ return 17; } }
        //		public override int AosMaxDamage{ get{ return 18; } }
        //		public override int AosSpeed{ get{ return 28; } }
        //
        public override int OldMinDamage { get { return 5; } }
        public override int OldMaxDamage { get { return 43; } }
        public override int OldStrengthReq { get { return 40; } }
        public override int OldSpeed { get { return 26; } }

        public override int OldDieRolls { get { return 2; } }
        public override int OldDieMax { get { return 20; } }
        public override int OldAddConstant { get { return 3; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 100; } }

        [Constructable]
        public Bardiche()
            : base(0xF4D)
        {
            Weight = 7.0;
        }

        public Bardiche(Serial serial)
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