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

/* Scripts/Items/Skill Items/Tailor Items/Misc/SpecialDyeTub.cs
 * ChangeLog:
 *  9/19/21, Yoar
 *      Moved the SpecialDyeTub class from "\SpecialDyeTub.cs" to "\Charged\SpecialDyeTubCharged.cs".
 *  2/4/07, Adam
 *      Add back old style SpecialDyeTub as SpecialDyeTubClassic
 *  01/04/07, plasma
 *      Added two read only properties that indicate if a tub can be lightened/darkened
 *	10/16/05, erlein
 *		Altered use of "Prepped" to define whether tub has been darkened or lightened already.
 *		Added appropriate deserialization to handle old tubs.
 *	10/15/05, erlein
 *		Added checks to ensure dye tub and targetted clothing is in backpack.
 *		Added stack handling for dying of multiple color swatches.
 *		Added check to ensure only clothing is targetted in dying process.
 *	10/15/05, erlein
 *		Initial creation (complete re-write).
 */

namespace Server.Items
{
    public class SpecialDyeTubClassic : DyeTub, Engines.VeteranRewards.IRewardItem
    {
        public override CustomHuePicker CustomHuePicker { get { return CustomHuePicker.SpecialDyeTub; } }
        public override int LabelNumber { get { return 1041285; } } // Special Dye Tub

        private bool m_IsRewardItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get { return m_IsRewardItem; }
            set { m_IsRewardItem = value; }
        }

        [Constructable]
        public SpecialDyeTubClassic()
        {
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_IsRewardItem && !Engines.VeteranRewards.RewardSystem.CheckIsUsableBy(from, this, null))
                return;

            base.OnDoubleClick(from);
        }

        public SpecialDyeTubClassic(Serial serial)
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