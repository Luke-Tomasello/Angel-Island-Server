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

/* Scripts/Items/Weapons/Ranged/HeavyCrossbow.cs
 * CHANGELOG:
 *	11/18/21, Yoar
 *		Wax sealed bows now retain their waxy hue on craft.
 *	4/23/07, Pix
 *		Fixed for oldschool labelling.
 *  1/30/07, Adam
 *      Give the sealed bows a better 'waxy' hue.
 *	01/02/07, Pix
 *		Made sealed variant constructable
 *	01/02/07, Pix
 *		Added SealedHeavyCrossbow.
 */

using Server.Engines.Craft;
using System;

namespace Server.Items
{
    public class SealedHeavyCrossbow : HeavyCrossbow
    {
        [Constructable]
        public SealedHeavyCrossbow()
            : base()
        {
            Hue = 0x33;
        }

        public SealedHeavyCrossbow(Serial s)
            : base(s)
        {
        }

        public override string OldName
        {
            get { return "sealed heavy crossbow"; }
        }

        public override Article OldArticle
        {
            get { return Article.A; }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1)
                Name = null;

            if (version < 2 && Hue == 0)
                Hue = 0x33;
        }

        public override int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            int result = base.OnCraft(quality, makersMark, from, craftSystem, typeRes, tool, craftItem, resHue);

            Hue = 0x33;

            return result;
        }
    }

    [FlipableAttribute(0x13FD, 0x13FC)]
    public class HeavyCrossbow : BaseRanged
    {
        public override int EffectID { get { return 0x1BFE; } }
        public override Type AmmoType { get { return typeof(Bolt); } }
        public override Item Ammo { get { return new Bolt(); } }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.MovingShot; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Dismount; } }

        //		public override int AosStrengthReq{ get{ return 80; } }
        //		public override int AosMinDamage{ get{ return 19; } }
        //		public override int AosMaxDamage{ get{ return 20; } }
        //		public override int AosSpeed{ get{ return 22; } }
        //
        public override int OldMinDamage { get { return 11; } }
        public override int OldMaxDamage { get { return 56; } }
        public override int OldStrengthReq { get { return 40; } }
        public override int OldSpeed { get { return 10; } }

        public override int OldDieRolls { get { return 5; } }
        public override int OldDieMax { get { return 10; } }
        public override int OldAddConstant { get { return 6; } }

        public override int DefMaxRange { get { return 8; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 100; } }

        [Constructable]
        public HeavyCrossbow()
            : base(0x13FD)
        {
            Weight = 9.0;
            Layer = Layer.TwoHanded;
        }

        public HeavyCrossbow(Serial serial)
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