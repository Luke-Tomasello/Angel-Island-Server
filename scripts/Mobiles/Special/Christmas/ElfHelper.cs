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

/* Scripts\Mobiles\Special\ElfHelper.cs
 * ChangeLog
 * 12/12/23, Yoar
 *      Moved all but ChristmasElf to separate source files.
 *      Renamed to ElfHelper
 * 12/4/21, Yoar
 *      Added elf peasants - will have snowball fights with each other!
 * 12/3/21, Yoar
 *      Removed old code
 *      Adjusted outfit
 *      Reverted ChristmasElf to invulnerable vendor
 *      Added blue elves: ElfMace, ElfMage
 * 11/30/21, Yoar
 *      Added snowball throwing.
 * 11/28/21, Yoar
 *      Added a new outfit! Disabled old outfit.
 *      May now spawn as both male/female.
 *      Changed title to "the elf helper"
 *      Moved gift-giving code to Items/Special/Holiday/HolidayDeed.cs
 *      Added EvilElfMage, EvilElfMace
 * 12/20/07, Adam
 *      Replace the Christmas tree with a roast pig so that we can get the trees our well before christmas
 * 12/18/06 Adam
 *      Update gifts to have "Christmas <year>" tag
 * 12/03/06 Taran Kain
 *      Set Female to true - no tranny elves!
 * 12/17/05, Adam
 *		This method fails if the bank is full!
 *		--> box.TryDropItem( from, Giftbox, false );
 *		this one won't 
 *		--> box.AddItem( Giftbox );
 * 12/13/05, Kit
 *		Added cookies to be dropped as well
 * 12/11/05 Adam
 *		Add the 'elf look'
 * 12/11/05 Kit,
 *		Added light source
 * 12/11/05 Kit, 
 *		Initial Creation
 */

using Server.Items;

namespace Server.Mobiles
{
    [TypeAlias("Server.Mobiles.ChristmasElf")]
    [CorpseName("an elf corpse")]
    public class ElfHelper : BaseCreature
    {
        [Constructable]
        public ElfHelper()
            : base(AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2)
        {
            Title = "the helper elf";

            InitStats(100, 100, 25);

            InitBody(this);
            InitOutfit(this);

            Candle candle = new Candle();
            candle.Ignite();
            ForceAddItem(this, candle);

            NameHue = CalcInvulNameHue();
            // 1/9/2024: Adam, no blessed. Use Invulnerable instead
            //Blessed = true;
        }

        #region The Elf Look

        public static void InitBody(Mobile m)
        {
            m.SpeechHue = Utility.RandomSpeechHue();

            m.Hue = RandomSkinHue();

            if (m.Female = Utility.RandomBool())
            {
                m.Body = 0x191;
                m.Name = NameList.RandomName("female");
            }
            else
            {
                m.Body = 0x190;
                m.Name = NameList.RandomName("male");
            }

            Item hair = new Item(RandomHairItemID());
            hair.Hue = RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            ForceAddItem(m, hair);
        }

        public static void InitOutfit(Mobile m)
        {
            ForceAddItem(m, new LightSource());

            bool primary = Utility.RandomBool();

            switch (Utility.Random(3))
            {
                case 0: ForceAddItem(m, new FeatheredHat(RandomClothingHue(primary))); break;
                case 1: ForceAddItem(m, new JesterHat(RandomClothingHue(primary))); break;
                case 2: ForceAddItem(m, new WideBrimHat(RandomClothingHue(primary))); break;
            }

            ForceAddItem(m, new FancyShirt(RandomClothingHue(primary)));

            ForceAddItem(m, new Doublet(RandomClothingHue(!primary)));

            if (m.Female)
                ForceAddItem(m, new Kilt(RandomClothingHue(primary)));
            else
                ForceAddItem(m, new ShortPants(RandomClothingHue(primary)));

            ForceAddItem(m, new ThighBoots(RandomClothingHue(!primary))); // stockings

            // seems to be a client crash causer
#if false
            ForceAddItem(m, SetMovable(false, SetLayer(Layer.OuterLegs, new Shoes(RandomClothingHue(primary))))); // shoes over stockings
#endif
        }

        public static int RandomSkinHue()
        {
            return Utility.RandomList(
                1002, 1003, 1008,
                1009, 1010, 1015,
                1016, 1017, 1022) | 0x8000;
        }

        public static int RandomHairItemID()
        {
            return Utility.RandomList(0x203B, 0x203D, 0x2049, 0x204A);
        }

        public static int RandomHairHue()
        {
            return Utility.RandomList(
                1810, 1816, 1818, // blond
                1846, 1852, 1854, // brunette
                1855, 1861, 1863, // ginger
                1900, 1906, 1908); // dark
        }

        public static int RandomClothingHue(bool primary)
        {
            if (primary)
                return 37 + Utility.Random(4) + 100 * Utility.Random(6); // red
            else
                return 67 + Utility.Random(4) + 100 * Utility.Random(6); // green
        }

        public static Item SetHue(int hue, Item item)
        {
            item.Hue = hue;

            return item;
        }

        public static Item SetMovable(bool movable, Item item)
        {
            item.Movable = movable;

            return item;
        }

        public static Item SetLayer(Layer layer, Item item)
        {
            item.Layer = layer;

            return item;
        }

        public static void ForceAddItem(Mobile m, Item item)
        {
            Item existing = m.FindItemOnLayer(item.Layer);

            if (existing != null)
                existing.Delete();

            if (item.Layer == Layer.TwoHanded)
            {
                existing = m.FindItemOnLayer(Layer.OneHanded);

                if (existing != null)
                    existing.Delete();
            }

            m.AddItem(item);
        }

        #endregion

        public override bool ClickTitle { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        public ElfHelper(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        if (version < 1)
                            reader.ReadString(); // message

                        break;
                    }
            }

            NameHue = CalcInvulNameHue();
        }
    }
}