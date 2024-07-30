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

/* Scripts/Items/Weapons/SpearsAndForks/WarFork.cs
 * ChangeLog :
 *  7/28/23, Yoar
 *      Removed poison check. This is handled in BaseSpear
 *	10/16/05, Pix
 *		Streamlined applied poison code.
 *	09/13/05, erlein
 *		Reverted poisoning rules, applied same system as archery for determining
 *		poison level achieved.
 *	09/12/05, erlein
 *		Changed OnHit() code to utilise new poisoning rules.
 *	4/23/04, Pulse
 *		Added OnHit() function to call Base.OnHit() and then perform poison check
 *		prior to this change, war forks were poisonable, but would never apply poison
 *		to victim or consume charges, it will now do both of those things properly.
 */

namespace Server.Items
{
    [FlipableAttribute(0x1405, 0x1404)]
    public class WarFork : BaseSpear
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.BleedAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Disarm; } }

        //		public override int AosStrengthReq{ get{ return 45; } }
        //		public override int AosMinDamage{ get{ return 12; } }
        //		public override int AosMaxDamage{ get{ return 13; } }
        //		public override int AosSpeed{ get{ return 43; } }
        //
        public override int OldMinDamage { get { return 4; } }
        public override int OldMaxDamage { get { return 32; } }
        public override int OldStrengthReq { get { return 35; } }
        public override int OldSpeed { get { return 45; } }

        public override int OldDieRolls { get { return 1; } }
        public override int OldDieMax { get { return 29; } }
        public override int OldAddConstant { get { return 3; } }

        public override int DefHitSound { get { return 0x236; } }
        public override int DefMissSound { get { return 0x238; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 110; } }

        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Pierce1H; } }

        [Constructable]
        public WarFork()
            : base(0x1405)
        {
            Weight = 9.0;
        }

        public WarFork(Serial serial)
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