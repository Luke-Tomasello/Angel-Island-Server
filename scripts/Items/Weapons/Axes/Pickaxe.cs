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

using Server.Engines.Harvest;

namespace Server.Items
{
    [FlipableAttribute(0xE86, 0xE85)]
    public class Pickaxe : BaseAxe
    {
        public override HarvestSystem HarvestSystem { get { return Mining.System; } }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.DoubleStrike; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Disarm; } }

        //		public override int AosStrengthReq{ get{ return 50; } }
        //		public override int AosMinDamage{ get{ return 13; } }
        //		public override int AosMaxDamage{ get{ return 15; } }
        //		public override int AosSpeed{ get{ return 35; } }
        //
        public override int OldMinDamage { get { return 1; } }
        public override int OldMaxDamage { get { return 15; } }
        public override int OldStrengthReq { get { return 25; } }
        public override int OldSpeed { get { return 35; } }

        public override int OldDieRolls { get { return 1; } }
        public override int OldDieMax { get { return 15; } }
        public override int OldAddConstant { get { return 0; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 60; } }

        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Slash1H; } }

        [Constructable]
        public Pickaxe()
            : base(0xE86)
        {
            Weight = 11.0;
            UsesRemaining = 50;
            ShowUsesRemaining = true;
        }

        public Pickaxe(Serial serial)
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
            ShowUsesRemaining = true;
        }
    }
}