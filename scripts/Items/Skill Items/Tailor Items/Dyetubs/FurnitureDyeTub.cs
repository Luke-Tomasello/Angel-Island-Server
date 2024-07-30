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

namespace Server.Items
{
    public class FurnitureDyeTub : DyeTub, Engines.VeteranRewards.IRewardItem
    {
        public override bool AllowDyables { get { return false; } }
        public override bool AllowFurniture { get { return true; } }
        public override int TargetMessage { get { return 501019; } } // Select the furniture to dye.
        public override int FailMessage { get { return 501021; } } // That is not a piece of furniture.
        public override int LabelNumber { get { return 1041246; } } // Furniture Dye Tub

        // 12/1/23, Yoar: Let's use some of the leather dye tub colors instead of the ugly regular hues
        private static readonly CustomHuePicker m_InternalHuePicker = new CustomHuePicker(new CustomHueGroup[]
            {
				/* Reds */
				new CustomHueGroup( 1018340, new int[]{ 2113, 2114, 2115, 2116, 2117, 2118 } ),
				/* Blues */
				new CustomHueGroup( 1018341, new int[]{ 2119, 2120, 2121, 2122, 2123, 2124 } ),
				/* Greens */
				new CustomHueGroup( 1018342, new int[]{ 2126, 2127, 2128, 2129, 2130 } ),
				/* Yellows */
				new CustomHueGroup( 1018343, new int[]{ 2213, 2214, 2215, 2216, 2217, 2218 } )
            }, true);

        public override CustomHuePicker CustomHuePicker { get { return m_InternalHuePicker; } }

        private bool m_IsRewardItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get { return m_IsRewardItem; }
            set { m_IsRewardItem = value; }
        }

        [Constructable]
        public FurnitureDyeTub()
        {
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_IsRewardItem && !Engines.VeteranRewards.RewardSystem.CheckIsUsableBy(from, this, null))
                return;

            base.OnDoubleClick(from);
        }

        public FurnitureDyeTub(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_IsRewardItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_IsRewardItem = reader.ReadBool();
                        break;
                    }
            }

            if (LootType == LootType.Regular)
                LootType = LootType.Blessed;
        }
    }
}