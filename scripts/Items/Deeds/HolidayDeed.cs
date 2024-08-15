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

/* Items/Deeds/HolidayDeed.cs
 * ChangeLog:
 * 11/28/21, Yoar
 *     Deprecated. Use HolidayDeed in Items/Special/Holiday/HolidayDeed.cs.
 * 9/21/08, Adam
 *      - Serialize m_Signature, and m_Place.
 *      - Create Nuke to patch all old lables to today's date
 *      - Upgrade all tickets that have not been cashed in to 1st place.
 *      - add random placement of items in halloween basket
 * 12/18/06 Adam
 *      Update holiday deed to have "Christmas <year>" tag
 * 11/02/06, Adam
 *		- many cosmetic updates to the way in which things are labeled.
 * 10/30/06, Adam
 *		- Add Date stamp to all items to preserve 'rareness' of previous gifts
 * 10/29/06, Adam
 *		- Change deed hue
 *		- Make sure deed is in players backpack
 *		- fix item weights
 *		- name the deeds based on 'place' (1st, 2nd, 3rd)
 *		- change chance to get a poisoned apple from 75% to 25%
 *		- change the random number selector to be 4 and not 3 so as to allow
 *			for all 4 prizes.
 *		- remove weight from MonsterStatuette
 *		- add success message
 * 10/29/06, Kit
 *		Added halloween deed type
 * 12/12/05, Kit
 *		Added msg on doubltr click
 * 12/11/05, Kit
 *		Set loot type to newbied
 * 12/11/05, Kit
 *		Initial Creation for use with ChristmasElf Christmas Holiday 2005	
 */

using System;

namespace Server.Items
{
#if old
    public class HolidayDeed : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public HolidayDeed()
            : base(0x14F0)
        {
            string year = DateTime.UtcNow.Year.ToString();
            Name = "Christmas " + year;
            Weight = 1.0;
            Hue = 0x47;
            LootType = LootType.Newbied;
        }

        public HolidayDeed(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage("Give this ticket to the elf at Britan Bank");
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
#endif

    [Obsolete("Use HolidayDeed instead")]
    public class HalloweenDeed : Item
    {
        private string m_Signature;
        private int m_Place;

        [Constructable]
        public HalloweenDeed()
            : this(0)
        {
        }

        [Constructable]
        public HalloweenDeed(int place)
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = 44;
            LootType = LootType.Newbied;
            m_Place = place;
            string PlaceText = null;

            if (place == 1)
                PlaceText = "1st place";
            else if (place == 2)
                PlaceText = "2nd place";
            else if (place == 3)
                PlaceText = "3rd place";

            if (place >= 1 && place <= 3)
            {
                m_Signature = string.Format("{2} Halloween Prize: {0}/{1}", DateTime.UtcNow.Month, DateTime.UtcNow.Year, PlaceText);
                Name = string.Format("a {2} Halloween Prize Ticket: {0}/{1}", DateTime.UtcNow.Month, DateTime.UtcNow.Year, PlaceText);
            }
            else
            {
                m_Signature = string.Format("Halloween Prize: {0}/{1}", DateTime.UtcNow.Month, DateTime.UtcNow.Year);
                Name = string.Format("Halloween Prize Ticket: {0}/{1}", DateTime.UtcNow.Month, DateTime.UtcNow.Year);
            }
        }

        public HalloweenDeed(Serial serial)
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
            Basket basket = new Basket();
            basket.Hue = Utility.RandomList(44, 167);
            basket.Name = "basket" + " - " + m_Signature;

            // skull
            CandleSkull skull = new CandleSkull();
            skull.Name = "skull candle" + " - " + m_Signature;

            // apple 1
            Item apple1 = new Apple();
            apple1.Hue = 134;
            apple1.Name = "candied apple" + " - " + m_Signature;

            // apple 2
            Item apple2 = new Apple();
            apple2.Hue = 47;
            apple2.Name = "caramel apple" + " - " + m_Signature;

            // apple 3
            if (Utility.RandomDouble() < 0.25)
            {
                Item apple3 = new Apple();
                ((Apple)apple3).Poison = Poison.Lesser;
                apple3.Hue = 63;
                apple3.Name = "poisoned apple" + " - " + m_Signature;
                basket.DropItem(apple3);
            }

            // add them
            basket.DropItem(skull);
            basket.DropItem(apple1);
            basket.DropItem(apple2);

            if (m_Place == 1)
            {
                Item HangingSkeleton = new Item(Utility.RandomList(6657, 6658, 6659, 6660));
                HangingSkeleton.Hue = 0;
                HangingSkeleton.Light = LightType.Circle225;
                HangingSkeleton.Weight = 6;
                HangingSkeleton.Name = "hanging skeleton" + " - " + m_Signature;
                basket.DropItem(HangingSkeleton);
            }

            if (m_Place == 2)
            {
                Item web = new Item(Utility.RandomList(4306, 4307, 4308, 4309));
                web.Hue = 0;
                web.Light = LightType.Circle225;
                web.Weight = 1;
                web.Name = "spider web" + " - " + m_Signature;
                basket.DropItem(web);
            }

            if (m_Place == 3)
            {
                Item skeleton = new MonsterStatuette(MonsterStatuetteType.Skeleton);
                skeleton.Hue = 44;
                skeleton.Light = LightType.Circle225;
                skeleton.Name = "Happy Halloween" + " - " + m_Signature;
                basket.DropItem(skeleton);
            }

            // finish it up
            from.Backpack.DropItem(basket);
            this.Delete();
            from.SendMessage("Your Halloween gift has been placed into your backpack.");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            // version 1
            writer.Write(m_Signature);
            writer.Write((int)m_Place);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Signature = reader.ReadString();
                        m_Place = reader.ReadInt();
                        goto case 0;
                    }

                case 0:
                    {   // our bad, everyone's a winner!
                        if (version == 0)
                        {
                            m_Place = 1;
                            string PlaceText = "1st place";
                            m_Signature = string.Format("{2} Halloween Prize: {0}/{1}", DateTime.UtcNow.Month, DateTime.UtcNow.Year, PlaceText);
                        }
                        goto default;
                    }

                default:
                    break;
            }
        }
    }
}