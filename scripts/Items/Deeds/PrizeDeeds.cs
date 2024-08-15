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

/* Items/Deeds/PrizeDeeds.cs
 * ChangeLog:
 * 11/02/06, Adam
 *		- many cosmetic updates to the way in which things are labeled.
 * 10/31/06, Adam
 *		- add HomeDecoPrize
 *		- Add Date stamp to all items to preserve 'rareness' of previous gifts
 */

using System;

namespace Server.Items
{
    public class HomeDecoPrize : Item
    {
        private string m_Signature;
        private int m_Place;
        private string m_PlaceText;

        [Constructable]
        public HomeDecoPrize(int place)
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = 53;
            LootType = LootType.Newbied;
            m_Place = place;

            if (place == 1)
                m_PlaceText = "1st place";
            else if (place == 2)
                m_PlaceText = "2nd place";
            else if (place == 3)
                m_PlaceText = "3rd place";
            else
            {
                place = 3;
                m_PlaceText = "3rd place";
            }

            m_Signature = string.Format("{2} Home Deco Prize: {0}/{1}", DateTime.UtcNow.Month, DateTime.UtcNow.Year, m_PlaceText);
            Name = string.Format("{2} Home Deco Prize Ticket: {0}/{1}", DateTime.UtcNow.Month, DateTime.UtcNow.Year, m_PlaceText);
        }

        public HomeDecoPrize(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            // Make sure is in pack
            if (from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);  // Must be in pack to use!!
                return;
            }

            //generate all the crap
            Item basket = new Basket();
            basket.Hue = Utility.RandomList(1345, 1146, 1253);
            basket.Name = "basket" + " - " + m_Signature;

            /*
			// blood
			Item blood = new Item(Utility.RandomList(4650,4651,4652,4653,4654));
			blood.Name = "blood" + " - " + m_Signature;
			blood.Weight = 1;
			blood.Movable = true;

			// bloody water
			Item bloodyWater = new Item(3619);
			bloodyWater.Name = "bloody water" + " - " + m_Signature;
			bloodyWater.Weight = 1;
			bloodyWater.Movable = true;
			*/

            // candelabrastand
            Item candelabrastand = new CandelabraStand();
            candelabrastand.Name = "candelabra" + " - " + m_Signature;
            candelabrastand.Movable = true;

            // Decorative armor 5402, 5384, 5394, 5404 
            Item DecorativeArmor = new Item(Utility.RandomList(5402, 5384, 5394, 5404));
            DecorativeArmor.Name = "decorative armor" + " - " + m_Signature;
            DecorativeArmor.Weight = 25;
            DecorativeArmor.Movable = true;

            // spitoon 4099
            Item spittoon = new Item(4099);
            spittoon.Name = "spittoon" + " - " + m_Signature;
            spittoon.Weight = 3;
            spittoon.Movable = true;

            /*
			 * no participation reward, period... just 3 winners, for house deco contest... 
			 * 3rd place: spitoon and candelabrastand, 
			 * 2nd place: candelabrastand and random tapestry deed (from all types), 
			 * 1st place: candelabrastand, random tapestry deed (from all types), and random decorative armor
			 **/

            if (m_Place == 1)
            {
                if (Utility.RandomBool())
                    basket.AddItem(new LightFlowerTapestryEastDeed());
                else
                    basket.AddItem(new LightFlowerTapestrySouthDeed());

                basket.AddItem(candelabrastand);
                basket.AddItem(DecorativeArmor);

                //basket.AddItem(blood);
                //basket.AddItem(bloodyWater);
            }
            else if (m_Place == 2)
            {
                if (Utility.RandomBool())
                    basket.AddItem(new DarkFlowerTapestryEastDeed());
                else
                    basket.AddItem(new DarkFlowerTapestrySouthDeed());

                basket.AddItem(candelabrastand);

                //basket.AddItem(blood);
                //basket.AddItem(bloodyWater);
            }
            else if (m_Place == 3)
            {
                basket.AddItem(candelabrastand);
                basket.AddItem(spittoon);

                //basket.AddItem(blood);
                //basket.AddItem(bloodyWater);
            }
            else
            {
                //basket.AddItem(blood);
                //basket.AddItem(bloodyWater);
            }

            // finish it up
            from.Backpack.AddItem(basket);
            this.Delete();
            from.SendMessage("Your Home Deco Prize has been placed into your backpack.");
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