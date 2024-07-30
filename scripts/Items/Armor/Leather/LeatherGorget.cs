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

/* Scripts/Items/Armor/Leather/LeatherGorget.cs
 * ChangeLog
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 15 lines removed.
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *  9/11/04
 *		Add Hellish variant of this leather piece.
 *	7/16/04, Adam
 *		Add Corpse Skin variant of this leather piece.
 */

namespace Server.Items
{
    public class LeatherGorget : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 40; } }

        // public override int AosStrReq  { get { return 20; } }
        public override int StrReq { get { return 10; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 13; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        [Constructable]
        public LeatherGorget()
            : base(0x13C7)
        {
            Weight = 1.0;
        }

        public LeatherGorget(Serial serial)
            : base(serial)
        {
        }

        // old name removed, see base class

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class CorpseSkinGorget : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 40; } }

        // public override int AosStrReq  { get { return 20; } }
        public override int StrReq { get { return 10; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 13; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        [Constructable]
        public CorpseSkinGorget()
            : base(0x13C7)
        {
            Weight = 1.0;
            Hue = 2101;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Corpse Skin Gorget";
        }

        public CorpseSkinGorget(Serial serial)
            : base(serial)
        {
        }

        public override bool ShowArmorAttributes { get { return false; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class HellishGorget : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 40; } }

        // public override int AosStrReq  { get { return 20; } }
        public override int StrReq { get { return 10; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 13; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        [Constructable]
        public HellishGorget()
            : base(0x13C7)
        {
            Weight = 1.0;
            Hue = 1645;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Hellish Gorget";
        }

        public HellishGorget(Serial serial)
            : base(serial)
        {
        }

        public override bool ShowArmorAttributes { get { return false; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}