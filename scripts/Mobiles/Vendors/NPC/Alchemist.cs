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

/* Scripts/Mobiles/Vendors/NPC/Alchemist.cs
 * ChangeLog
 *  10/18/04, Froste
 *      Modified Restock to use OnRestock() because it's fixed now
 *	4/29/04, mith
 *		Modified Restock to use OnRestockReagents() to restock 100 of each item instead of only 20.
 */

using System;
using System.Collections;

namespace Server.Mobiles
{
    public class Alchemist : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override NpcGuild NpcGuild { get { return NpcGuild.MagesGuild; } }

        [Constructable]
        public Alchemist()
            : base("the alchemist")
        {
            SetSkill(SkillName.Alchemy, 85.0, 100.0);
            SetSkill(SkillName.TasteID, 65.0, 88.0);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBAlchemist());
        }

        public override VendorShoeType ShoeType
        {
            get { return Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals; }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Server.Items.Robe(Utility.RandomPinkHue()));
        }

        public override void Restock()
        {
            base.LastRestock = DateTime.UtcNow;

            IBuyItemInfo[] buyInfo = this.GetBuyInfo();

            foreach (IBuyItemInfo bii in buyInfo)
                bii.OnRestock(); // change bii.OnRestockReagents() to OnRestock()
        }

        public Alchemist(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}