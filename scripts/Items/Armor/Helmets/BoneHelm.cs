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

/* Scripts/Items/Armor/Helmets/BoneHelm.cs
 * ChangeLog
 *  6/21/06, Kit
 *		Changed to material type bone not plate to allow correct Sdrop creation.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 15 lines removed.
 *  1/23/05, Froste
 *  Add Bone Magi variant of this Bone piece. Meddable, AR like leather, Exceptional.
 *	9/10/04, Pigpen
 *	Add Unholy Bone variant of this Helmet piece.
 */

namespace Server.Items
{
    [FlipableAttribute(0x1451, 0x1456)]
    public class BoneHelm : BaseArmor
    {

        public override int InitMinHits { get { return 25; } }
        public override int InitMaxHits { get { return 30; } }

        // public override int AosStrReq  { get { return 20; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 30; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Bone; } }

        [Constructable]
        public BoneHelm()
            : base(0x1451)
        {
            Weight = 3.0;
        }

        public BoneHelm(Serial serial)
            : base(serial)
        {
        }

        // old name removed, see base class

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            if (Weight == 1.0)
                Weight = 3.0;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class UnholyBoneHelm : BaseArmor
    {

        public override int InitMinHits { get { return 25; } }
        public override int InitMaxHits { get { return 30; } }

        // public override int AosStrReq  { get { return 20; } }
        public override int StrReq { get { return 40; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return 30; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public UnholyBoneHelm()
            : base(0x1451)
        {
            Weight = 3.0;
            Hue = 1109;
            ProtectionLevel = ArmorProtectionLevel.Guarding;
            DurabilityLevel = ArmorDurabilityLevel.Massive;
            if (Utility.RandomDouble() < 0.20)
                Quality = ArmorQuality.Exceptional;
            Name = "Unholy Bone Helm";
        }

        public UnholyBoneHelm(Serial serial)
            : base(serial)
        {
        }

        public override bool ShowArmorAttributes { get { return false; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            if (Weight == 1.0)
                Weight = 3.0;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class BoneMagiHelm : BaseArmor
    {
        private int m_ArmorBase = 0;
        private int m_StrReq = -1;
        public override int InitMinHits { get { return 25; } }
        public override int InitMaxHits { get { return 30; } }

        // if m_ArmorBase is defined, this is just an RP helm
        public override int StrReq { get { return m_StrReq == -1 ? 40 : m_StrReq; } }

        public override int DexReq { get { return 0; } }
        public override int IntReq { get { return 0; } }
        public override int ArmorBase { get { return m_ArmorBase == 0 ? 13 : m_ArmorBase; } }

        public override ArmorMeditationAllowance MedAllowance { get { return ArmorMeditationAllowance.All; } }

        public override ArmorMaterialType MaterialType { get { return ArmorMaterialType.Plate; } }

        [Constructable]
        public BoneMagiHelm()
            : base(0x1451)
        {
            Weight = 3.0;
            Quality = ArmorQuality.Exceptional;
            Name = "Helm of the Bone Magi";
        }

        public BoneMagiHelm(int armorBase, int strReq)
            : this()
        {
            m_ArmorBase = armorBase;
            m_StrReq = strReq;
            Weight = 3.0;
            Quality = ArmorQuality.Regular;
            Name = "Bone Magi Helmet";
        }

        public BoneMagiHelm(Serial serial)
            : base(serial)
        {
        }

        public override bool ShowArmorAttributes { get { return false; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);

            // version 1
            writer.Write(m_ArmorBase);
            writer.Write(m_StrReq);

            if (Weight == 1.0)
                Weight = 3.0;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    m_ArmorBase = reader.ReadInt();
                    m_StrReq = reader.ReadInt();
                    goto case 0;
                case 0:
                    break;
            }
        }
    }
}