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

/* Scripts/Items/Armor/Bone/BoneArms.cs
* ChangeLog
*	7/26/05, erlein
*		Automated removal of AoS resistance related function calls. 15 lines removed.
*  1/23/05, Froste
*  Add Bone Magi variant of this Bone piece. Meddable, AR like leather, Exceptional.
*	9/10/04, Pigpen
*	Add Unholy Bone variant of this Bone piece.
*/

namespace Server.Items
{
    [FlipableAttribute(0x144e, 0x1453)]
    public class BoneArms : BaseArmor
    {

        public override int InitMinHits { get { return 25; } }
        public override int InitMaxHits { get { return 30; } }

        //// public override int AosStrReq  { get { return 55; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return BoneChest.DexPenalties ? -2 : 0; } }

        public override int ArmorBase { get { return 30; } }
        public override int RevertArmorBase { get { return 4; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Bone; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        [Constructable]
        public BoneArms()
            : base(0x144E)
        {
            Weight = 2.0;
        }

        public BoneArms(Serial serial)
            : base(serial)
        {
        }

        // old name removed, see base class

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            if (Weight == 1.0)
                Weight = 2.0;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class UnholyBoneArms : BaseArmor
    {

        public override int InitMinHits { get { return 25; } }
        public override int InitMaxHits { get { return 30; } }

        //// public override int AosStrReq  { get { return 55; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -2; } }

        public override int ArmorBase { get { return 30; } }
        public override int RevertArmorBase { get { return 4; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Bone; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        [Constructable]
        public UnholyBoneArms()
            : base(0x144E)
        {
            Weight = 2.0;
            Hue = 1109;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Unholy Bone Arms";
        }

        public UnholyBoneArms(Serial serial)
            : base(serial)
        {
        }

        public override bool ShowArmorAttributes { get { return false; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            if (Weight == 1.0)
                Weight = 2.0;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class BoneMagiArms : BaseArmor
    {

        public override int InitMinHits { get { return 25; } }
        public override int InitMaxHits { get { return 30; } }

        //// public override int AosStrReq  { get { return 55; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int EquipDexBonus { get { return -2; } }

        public override int ArmorBase { get { return 13; } }
        public override int RevertArmorBase { get { return 4; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Bone; } }
        public override CraftResource DefaultResource { get { return CraftResource.RegularLeather; } }

        [Constructable]
        public BoneMagiArms()
            : base(0x144E)
        {
            Weight = 2.0;
            Quality = ArmorQuality.Exceptional;
            Name = "Arms of the Bone Magi";
        }

        public BoneMagiArms(Serial serial)
            : base(serial)
        {
        }

        public override bool ShowArmorAttributes { get { return false; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            if (Weight == 1.0)
                Weight = 2.0;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}