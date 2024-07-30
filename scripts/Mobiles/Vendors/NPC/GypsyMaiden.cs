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

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class GypsyMaiden : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public GypsyMaiden()
            : base("the gypsy maiden")
        {
        }

        public override bool GetGender()
        {
            return true; // always female
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBProvisioner());
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            switch (Utility.Random(4))
            {
                case 0: AddItem(new JesterHat(RandomBrightHue())); break;
                case 1: AddItem(new Bandana(RandomBrightHue())); break;
                case 2: AddItem(new SkullCap(RandomBrightHue())); break;
            }

            if (Utility.RandomBool())
                AddItem(new HalfApron(RandomBrightHue()));

            Item item = FindItemOnLayer(Layer.Pants);

            if (item != null)
                item.Hue = RandomBrightHue();

            item = FindItemOnLayer(Layer.OuterLegs);

            if (item != null)
                item.Hue = RandomBrightHue();

            item = FindItemOnLayer(Layer.InnerLegs);

            if (item != null)
                item.Hue = RandomBrightHue();
        }

        public GypsyMaiden(Serial serial)
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