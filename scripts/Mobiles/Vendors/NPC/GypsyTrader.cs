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

/* Scripts/Mobiles/Vendors/NPC/GypsyTrader.cs
 *  11/14/04, Froste
 *      Created from Carpenter.cs
 */

using System.Collections;

namespace Server.Mobiles
{
    public class GypsyTrader : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }


        [Constructable]
        public GypsyTrader()
            : base("the gypsy trader")
        {
        }

        public override void InitSBInfo()
        {

            m_SBInfos.Add(new SBGypsyTrader());

        }

        public override int GetShoeHue()
        {
            return 0;
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            Item item = FindItemOnLayer(Layer.Pants);

            if (item != null)
                item.Hue = RandomBrightHue();

            item = FindItemOnLayer(Layer.OuterLegs);

            if (item != null)
                item.Hue = RandomBrightHue();

            item = FindItemOnLayer(Layer.InnerLegs);

            if (item != null)
                item.Hue = RandomBrightHue();

            item = FindItemOnLayer(Layer.OuterTorso);

            if (item != null)
                item.Hue = RandomBrightHue();

            item = FindItemOnLayer(Layer.InnerTorso);

            if (item != null)
                item.Hue = RandomBrightHue();

            item = FindItemOnLayer(Layer.Shirt);

            if (item != null)
                item.Hue = RandomBrightHue();
        }

        public GypsyTrader(Serial serial)
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