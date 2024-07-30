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

/* Scripts/Items/Armor/Studded/StuddedLegs.cs
* ChangeLog
*	7/26/05, erlein
*		Automated removal of AoS resistance related function calls. 10 lines removed.
*	9/11/04, Pigpen
*	Add Wyrm Skin variant of this Studded piece.
*/

namespace Server.Items
{
    [FlipableAttribute(0x13da, 0x13e1)]
    public class StuddedLegs : BaseArmor
    {

        public override int InitMinHits { get { return 35; } }
        public override int InitMaxHits { get { return 45; } }

        // public override int AosStrReq  { get { return 30; } }
        public override int StrReq { get { return 35; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 16; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Studded; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        [Constructable]
        public StuddedLegs()
            : base(0x13DA)
        {
            Weight = 5.0;
        }

        public StuddedLegs(Serial serial)
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

            if (Weight == 3.0)
                Weight = 5.0;
        }
    }
    public class WyrmSkinLeggings : BaseArmor
    {

        public override int InitMinHits { get { return 35; } }
        public override int InitMaxHits { get { return 45; } }

        // public override int AosStrReq  { get { return 30; } }
        public override int StrReq { get { return 35; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 16; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Studded; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        [Constructable]
        public WyrmSkinLeggings()
            : base(0x13DA)
        {
            Weight = 5.0;
            Hue = 1367;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Wyrm Skin Leggings";
        }

        public WyrmSkinLeggings(Serial serial)
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

            if (Weight == 3.0)
                Weight = 5.0;
        }
    }
}