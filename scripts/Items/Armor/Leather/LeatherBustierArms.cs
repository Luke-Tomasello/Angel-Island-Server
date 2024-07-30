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

/* ./Scripts/Items/Armor/Leather/LeatherBustierArms.cs
 *	ChangeLog :
 *  7/08/06, Kit
 *		Added Hellish/Corpse skin versions
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
*/

namespace Server.Items
{
    [FlipableAttribute(0x1c0a, 0x1c0b)]
    public class LeatherBustierArms : BaseArmor
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

        public override bool AllowMaleWearer { get { return false; } }

        [Constructable]
        public LeatherBustierArms()
            : base(0x1C0A)
        {
            Weight = 1.0;
        }

        public LeatherBustierArms(Serial serial)
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

    public class CorpseSkinBustier : BaseArmor
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
        public CorpseSkinBustier()
            : base(0x1C0A)
        {
            Weight = 1.0;
            Hue = 2101;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Corpse Skin Bustier";
        }

        public CorpseSkinBustier(Serial serial)
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

    public class HellishBustier : BaseArmor
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
        public HellishBustier()
            : base(0x1C0A)
        {
            Weight = 1.0;
            Hue = 1645;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Hellish Bustier";
        }

        public HellishBustier(Serial serial)
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