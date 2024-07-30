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

/* Scripts/Items/Skill Items/Tailor Items/Misc/RunebookDyeTub.cs
 * ChangeLog:
 *  9/20/21, Yoar
 *      Renamed to RunebookDyeTubClassic. Existing runebook dye tubs are
 *      converted to RunebookDyeTub, which is the AI customized version
 *      of runebook dye tubs.
 */

namespace Server.Items
{
    public class RunebookDyeTubClassic : DyeTub, Engines.VeteranRewards.IRewardItem
    {
        public override bool AllowDyables { get { return false; } }
        public override bool AllowRunebooks { get { return true; } }
        public override int TargetMessage { get { return 1049774; } } // Target the runebook or runestone to dye
        public override int FailMessage { get { return 1049775; } } // You can only dye runestones or runebooks with this tub.
        public override int LabelNumber { get { return 1049740; } } // Runebook Dye Tub
        public override CustomHuePicker CustomHuePicker { get { return CustomHuePicker.LeatherDyeTub; } }

        private bool m_IsRewardItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get { return m_IsRewardItem; }
            set { m_IsRewardItem = value; }
        }

        [Constructable]
        public RunebookDyeTubClassic()
        {
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_IsRewardItem && !Engines.VeteranRewards.RewardSystem.CheckIsUsableBy(from, this, null))
                return;

            base.OnDoubleClick(from);
        }

        public RunebookDyeTubClassic(Serial serial)
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
        }
    }
}