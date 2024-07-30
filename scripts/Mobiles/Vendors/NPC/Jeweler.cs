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

/* scripts\Mobiles\Vendors\NPC\Jeweler.cs
 * CHANGELOG:
 * 3/26/23. Adam: (Black Sandals)
 *   Old UO had a special black sandals on this NPC once in a while.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class Jeweler : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public Jeweler()
            : base("the jeweler")
        {
            SetSkill(SkillName.ItemID, 64.0, 100.0);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBJewel());
        }
        public override void InitOutfit()
        {
            base.InitOutfit();

            // 3/26/23. Adam: Old UO had a special black sandals on this NPC once in a while.
            if (0.18 >= Utility.RandomDouble())
            {
                Item old = this.FindItemOnLayer(Layer.Shoes);
                if (old != null)
                {
                    RemoveItem(old);
                    old.Delete();
                }
                Sandals item = new Sandals();
                item.Hue = 0x01;
                AddItem(item);
            }
        }

        public Jeweler(Serial serial)
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