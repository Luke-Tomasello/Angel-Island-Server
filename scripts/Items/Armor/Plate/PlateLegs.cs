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

/* Scripts/Items/Armor/Plate/PlateLegsChest.cs
* ChangeLog
*	7/26/05, erlein
*		Automated removal of AoS resistance related function calls. 10 lines removed.
*  9/11/04, Pigpen
*		Add Dread Steel Armor Variant to this Plate piece.
*/

using Server.Network;
using System.Collections;

namespace Server.Items
{
    [FlipableAttribute(0x1411, 0x141a)]
    public class PlateLegs : BaseArmor
    {

        public override int InitMinHits { get { return 50; } }
        public override int InitMaxHits { get { return 65; } }

        // public override int AosStrReq  { get { return 90; } }

        public override int StrReq { get { return 60; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -6; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public PlateLegs()
            : base(0x1411)
        {
            Weight = 7.0;
        }

        public PlateLegs(Serial serial)
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

    public class SpecialLeggings : BaseArmor
    {

        public override int InitMinHits { get { return 50; } }
        public override int InitMaxHits { get { return 65; } }

        // public override int AosStrReq  { get { return 90; } }

        public override int StrReq { get { return 60; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -6; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public SpecialLeggings()
            : base(0x1411)
        {
            Weight = 7.0;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
        }

        public SpecialLeggings(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        // Special version that DOES NOT show armor attributes and tags
        public override void OnSingleClick(Mobile from)
        {
            ArrayList attrs = new ArrayList();

            int number;

            if (Name == null)
            {
                number = LabelNumber;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
                return;

            EquipmentInfo eqInfo = new EquipmentInfo(number, Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));

            from.Send(new DisplayEquipmentInfo(this, eqInfo));

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class DreadSteelLeggings : BaseArmor
    {

        public override int InitMinHits { get { return 50; } }
        public override int InitMaxHits { get { return 65; } }

        // public override int AosStrReq  { get { return 90; } }

        public override int StrReq { get { return 60; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -6; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public DreadSteelLeggings()
            : base(0x1411)
        {
            Weight = 7.0;
            Hue = 1236;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Dread Steel Leggings";
        }

        public DreadSteelLeggings(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override bool ShowArmorAttributes { get { return false; } }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}