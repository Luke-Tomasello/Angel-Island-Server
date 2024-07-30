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

/* Scripts/Items/Armor/Helmets/ChainCoif.cs
* ChangeLog
*	7/26/05, erlein
*		Automated removal of AoS resistance related function calls. 10 lines removed.
*	9/11/04, Pigpen
*	Add Wyrm Skin variant of this Helmet piece.
*/

namespace Server.Items
{
    [FlipableAttribute(0x13BB, 0x13C0)]
    public class ChainCoif : BaseArmor
    {

        public override int InitMinHits { get { return 35; } }
        public override int InitMaxHits { get { return 60; } }

        // public override int AosStrReq  { get { return 60; } }
        public override int StrReq { get { return 20; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 28; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Chainmail; } }

        [Constructable]
        public ChainCoif()
            : base(0x13BB)
        {
            Weight = 1.0;
        }

        public ChainCoif(Serial serial)
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

    public class WyrmSkinHelmet : BaseArmor
    {

        public override int InitMinHits { get { return 35; } }
        public override int InitMaxHits { get { return 60; } }

        // public override int AosStrReq  { get { return 60; } }
        public override int StrReq { get { return 20; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 28; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Chainmail; } }

        [Constructable]
        public WyrmSkinHelmet()
            : base(0x13BB)
        {
            Weight = 1.0;
            Hue = 1367;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Wyrm Skin Helmet";
        }

        public WyrmSkinHelmet(Serial serial)
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