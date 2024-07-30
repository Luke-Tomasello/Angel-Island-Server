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

/* Scripts\Items\Weapons\Swords\GuardianKatana.cs
 * ChangeLog :
 *  11/9/08, Adam
 *      Replace old MaxHits and Hits with MaxHitPoints and HitPoints (RunUO 2.0 compatibility)
 */

namespace Server.Items
{
    [FlipableAttribute(0x13FF, 0x13FE)]
    public class GuardianKatana : BaseSword
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.DoubleStrike; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ArmorIgnore; } }

        //		public override int AosStrengthReq{ get{ return 25; } }
        //		public override int AosMinDamage{ get{ return 11; } }
        //		public override int AosMaxDamage{ get{ return 13; } }
        //		public override int AosSpeed{ get{ return 46; } }
        //
        public override int OldMinDamage { get { return 5; } }
        public override int OldMaxDamage { get { return 26; } }
        public override int OldStrengthReq { get { return 10; } }
        public override int OldSpeed { get { return 58; } }

        public override int OldDieRolls { get { return 3; } }
        public override int OldDieMax { get { return 8; } }
        public override int OldAddConstant { get { return 2; } }

        public override int DefHitSound { get { return 0x23B; } }
        public override int DefMissSound { get { return 0x23A; } }

        public override int InitMinHits { get { return 90; } }
        public override int InitMaxHits { get { return 90; } }

        [Constructable]
        public GuardianKatana()
            : base(0x13FF)
        {
            Weight = 6.0;
        }

        public GuardianKatana(Serial serial)
            : base(serial)
        {
        }

        public override void OnHit(Mobile attacker, Mobile defender)
        {
            base.OnHit(attacker, defender);

            if (!Core.RuleSets.AOSRules() && Poison != null && PoisonCharges > 0)
            {
                if (Utility.RandomDouble() >= 0.5) // 50% chance to poison
                    defender.ApplyPoison(attacker, Poison);
            }

            this.PoisonCharges = 30;
            this.HitPoints = this.MaxHitPoints;
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