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
    [FlipableAttribute(0x26C0, 0x26CA)]
    public class Lance : BaseSword
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.Dismount; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ConcussionBlow; } }

        //		public override int AosStrengthReq{ get{ return 95; } }
        //		public override int AosMinDamage{ get{ return 17; } }
        //		public override int AosMaxDamage{ get{ return 18; } }
        //		public override int AosSpeed{ get{ return 24; } }

        public override int OldMinDamage { get { return 17; } }
        public override int OldMaxDamage { get { return 18; } }
        public override int OldStrengthReq { get { return 95; } }
        public override int OldSpeed { get { return 24; } }

        public override int OldDieRolls { get { return 2; } }
        public override int OldDieMax { get { return 20; } }
        public override int OldAddConstant { get { return 3; } }

        public override int DefHitSound { get { return 0x23C; } }
        public override int DefMissSound { get { return 0x238; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 110; } }

        public override SkillName DefSkill { get { return SkillName.Fencing; } }
        public override WeaponType DefType { get { return WeaponType.Piercing; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Pierce1H; } }

        [Constructable]
        public Lance()
            : base(0x26C0)
        {
            Weight = 12.0;
        }

        public Lance(Serial serial)
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