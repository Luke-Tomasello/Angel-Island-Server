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

/* ./Scripts/Items/Armor/Dragon/DragonPlateSet.cs
 *	ChangeLog:
 *	9/28/23, Yoar
 *	    Initial commit.
*/

namespace Server.Items
{
    [FlipableAttribute(0x1410, 0x1417)]
    public class DragonPlateArms : BaseArmor
    {
        public override string DefaultName { get { return "dragon plate arms"; } }

        public override int InitMinHits { get { return 55; } }
        public override int InitMaxHits { get { return 75; } }

        // public override int AosStrReq  { get { return 75; } }
        public override int StrReq { get { return 20; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -2; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }
        public override CraftResource DefaultResource { get { return CraftResource.RedScales; } }

        [Constructable]
        public DragonPlateArms()
            : base(0x1410)
        {
        }

        public DragonPlateArms(Serial serial)
            : base(serial)
        {
        }

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

    [FlipableAttribute(0x1415, 0x1416)]
    public class DragonPlateChest : BaseArmor
    {
        public override string DefaultName { get { return "dragon plate chest"; } }

        public override int InitMinHits { get { return 55; } }
        public override int InitMaxHits { get { return 75; } }

        // public override int AosStrReq  { get { return 75; } }
        public override int StrReq { get { return 60; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -8; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }
        public override CraftResource DefaultResource { get { return CraftResource.RedScales; } }

        [Constructable]
        public DragonPlateChest()
            : base(0x1415)
        {
        }

        public DragonPlateChest(Serial serial)
            : base(serial)
        {
        }

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

    [FlipableAttribute(0x1414, 0x1418)]
    public class DragonPlateGloves : BaseArmor
    {
        public override string DefaultName { get { return "dragon plate gloves"; } }

        public override int InitMinHits { get { return 55; } }
        public override int InitMaxHits { get { return 75; } }

        // public override int AosStrReq  { get { return 75; } }
        public override int StrReq { get { return 30; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -2; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }
        public override CraftResource DefaultResource { get { return CraftResource.RedScales; } }

        [Constructable]
        public DragonPlateGloves()
            : base(0x1414)
        {
        }

        public DragonPlateGloves(Serial serial)
            : base(serial)
        {
        }

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

    public class DragonPlateGorget : BaseArmor
    {
        public override string DefaultName { get { return "dragon plate gorget"; } }

        public override int InitMinHits { get { return 55; } }
        public override int InitMaxHits { get { return 75; } }

        // public override int AosStrReq  { get { return 50; } }
        public override int StrReq { get { return 30; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -2; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }
        public override CraftResource DefaultResource { get { return CraftResource.RedScales; } }

        [Constructable]
        public DragonPlateGorget()
            : base(0x1413)
        {
        }

        public DragonPlateGorget(Serial serial)
            : base(serial)
        {
        }

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

    public class DragonPlateHelm : BaseArmor
    {
        public override string DefaultName { get { return "dragon plate helm"; } }

        public override int InitMinHits { get { return 55; } }
        public override int InitMaxHits { get { return 75; } }

        // public override int AosStrReq  { get { return 75; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -1; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }
        public override CraftResource DefaultResource { get { return CraftResource.RedScales; } }

        [Constructable]
        public DragonPlateHelm()
            : base(0x1412)
        {
        }

        public DragonPlateHelm(Serial serial)
            : base(serial)
        {
        }

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

    [FlipableAttribute(0x1411, 0x141A)]
    public class DragonPlateLegs : BaseArmor
    {
        public override string DefaultName { get { return "dragon plate legs"; } }

        public override int InitMinHits { get { return 55; } }
        public override int InitMaxHits { get { return 75; } }

        // public override int AosStrReq  { get { return 75; } }
        public override int StrReq { get { return 60; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -6; } }

        public override int ArmorBase { get { return 40; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }
        public override CraftResource DefaultResource { get { return CraftResource.RedScales; } }

        [Constructable]
        public DragonPlateLegs()
            : base(0x1411)
        {
        }

        public DragonPlateLegs(Serial serial)
            : base(serial)
        {
        }

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