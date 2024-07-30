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

/* Scripts\Mobiles\Special\GOT\Items\GOTRewards.cs
 * ChangeLog
 *	5/19/2024, Adam
 *      First check in
 */

namespace Server.Items
{
    public class WhiteWalkerArmor : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 40; } }

        // public override int AosStrReq  { get { return 25; } }
        public override int StrReq { get { return 15; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 13; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        public override bool AllowMaleWearer { get { return false; } }

        public override Layer Layer { get { return Layer.Shirt; } }

        [Constructable]
        public WhiteWalkerArmor()
            : base(0x1C06)
        {
            Weight = 1.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker armor";
        }

        public WhiteWalkerArmor(Serial serial)
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

    public class WhiteWalkerArms : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 40; } }

        // public override int AosStrReq  { get { return 20; } }
        public override int StrReq { get { return 15; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 13; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        [Constructable]
        public WhiteWalkerArms()
            : base(0x13CD)
        {
            Weight = 2.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker arms";
        }

        public WhiteWalkerArms(Serial serial)
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

            if (Weight == 1.0)
                Weight = 2.0;
        }
    }

    public class WhiteWalkerTunic : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 40; } }

        // public override int AosStrReq  { get { return 25; } }
        public override int StrReq { get { return 15; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 13; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        [Constructable]
        public WhiteWalkerTunic()
            : base(0x13CC)
        {
            Weight = 6.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker tunic";
        }

        public WhiteWalkerTunic(Serial serial)
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

            if (Weight == 1.0)
                Weight = 6.0;
        }
    }

    public class WhiteWalkerGloves : BaseArmor
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
        public WhiteWalkerGloves()
            : base(0x13C6)
        {
            Weight = 1.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker gloves";
        }

        public WhiteWalkerGloves(Serial serial)
            : base(serial)
        {
        }

        public override bool ShowArmorAttributes { get { return false; } }

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

    public class WhiteWalkerGorget : BaseArmor
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
        public WhiteWalkerGorget()
            : base(0x13C7)
        {
            Weight = 1.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker gorget";
        }

        public WhiteWalkerGorget(Serial serial)
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

    public class WhiteWalkerLeggings : BaseArmor
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
        public WhiteWalkerLeggings()
            : base(0x13CB)
        {
            Weight = 4.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker leggings";
        }

        public WhiteWalkerLeggings(Serial serial)
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

    public class WhiteWalkerHelmet : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 40; } }

        // public override int AosStrReq  { get { return 20; } }
        public override int StrReq { get { return 15; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 13; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        [Constructable]
        public WhiteWalkerHelmet()
            : base(0x1DB9)
        {
            Weight = 2.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker helmet";
        }

        public WhiteWalkerHelmet(Serial serial)
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

            if (Weight == 1.0)
                Weight = 2.0;
        }
    }

    public class WhiteWalkerBustier : BaseArmor
    {

        public override int InitMinHits { get { return 30; } }
        public override int InitMaxHits { get { return 40; } }

        // public override int AosStrReq  { get { return 25; } }
        public override int StrReq { get { return 15; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 13; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Leather; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        public override bool AllowMaleWearer { get { return false; } }

        [Constructable]
        public WhiteWalkerBustier()
            : base(0x1C0A)
        {
            Weight = 1.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker bustier";
        }

        public WhiteWalkerBustier(Serial serial)
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

    public class WhiteWalkerShorts : BaseArmor
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

        public override bool AllowMaleWearer { get { return false; } }

        [Constructable]
        public WhiteWalkerShorts()
            : base(0x1C00)
        {
            Weight = 3.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker shorts";
        }

        public WhiteWalkerShorts(Serial serial)
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

    public class WhiteWalkerSkirt : BaseArmor
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

        public override bool AllowMaleWearer { get { return false; } }

        [Constructable]
        public WhiteWalkerSkirt()
            : base(0x1C08)
        {
            Weight = 1.0;
            Hue = 1429; // (0x595)
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            HideAttributes = true;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "wildling leather"; // "white walker skirt";
        }

        public WhiteWalkerSkirt(Serial serial)
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