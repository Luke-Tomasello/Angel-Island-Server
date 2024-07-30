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

/* Scripts/Items/Armor/Helmets/PlateHelm.cs
* ChangeLog
*	7/26/05, erlein
*		Automated removal of AoS resistance related function calls. 15 lines removed.
*  9/11/04, Pigpen
*		Add Dread Steel Helm variant of this Helmet Piece.
*	9/08/04, Pigpen
*		Add Arctic Storm Helm variant of this Helmet piece.
*/

namespace Server.Items
{
    public class PlateHelm : BaseArmor
    {

        public override int InitMinHits { get { return 50; } }
        public override int InitMaxHits { get { return 65; } }

        // public override int AosStrReq  { get { return 80; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -1; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public PlateHelm()
            : base(0x1412)
        {
            Weight = 5.0;
        }

        public PlateHelm(Serial serial)
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

            if (Weight == 1.0)
                Weight = 5.0;
        }
    }
    public class ArcticStormHelm : BaseArmor
    {

        public override int InitMinHits { get { return 50; } }
        public override int InitMaxHits { get { return 65; } }

        // public override int AosStrReq  { get { return 80; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -1; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public ArcticStormHelm()
            : base(0x1412)
        {
            Weight = 5.0;
            Hue = 1364;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Arctic Storm Helm";
        }

        public ArcticStormHelm(Serial serial)
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
                Weight = 5.0;
        }

    }

    public class SpecialHelm : BaseArmor
    {

        public override int InitMinHits { get { return 50; } }
        public override int InitMaxHits { get { return 65; } }

        // public override int AosStrReq  { get { return 80; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -1; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public SpecialHelm()
            : base(0x1412)
        {
            Weight = 5.0;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
        }

        public SpecialHelm(Serial serial)
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
                Weight = 5.0;
        }
    }

    public class DreadSteelHelm : BaseArmor
    {

        public override int InitMinHits { get { return 50; } }
        public override int InitMaxHits { get { return 65; } }

        // public override int AosStrReq  { get { return 80; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -1; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public DreadSteelHelm()
            : base(0x1412)
        {
            Weight = 5.0;
            Hue = 1236;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Dread Steel Helm";
        }

        public DreadSteelHelm(Serial serial)
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
                Weight = 5.0;
        }
    }
}