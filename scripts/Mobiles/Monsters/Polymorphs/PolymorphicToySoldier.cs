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

/* Scripts\Mobiles\Monsters\Polymorphs\PolymorphicToySoldier.cs
 * ChangeLog
 *  12/11/23, Adam
 *      Created.
 */

using Server.Items;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class PolymorphicToySoldier : BasePolymorphic
    {
        [Constructable]
        public PolymorphicToySoldier()
            : base()
        {
            SpeechHue = Utility.RandomSpeechHue();

            Fame = 1000;
            Karma = -1000;

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
        }
        public PolymorphicToySoldier(Serial serial)
            : base(serial)
        {
        }
        public override bool CanRummageCorpses { get { return false; } }
        public override bool AlwaysMurderer { get { return false; } }
        public override void InitStats()
        {
            base.InitStats();
        }
        public enum ToyHues
        {
            Gold = 0x8A5,
            Green = 0x89F
        }
        public override void InitBody()
        {
            if (this.Female = Utility.RandomBool())
                Body = 0x191;
            else
                Body = 0x190;

            Hue = (int)Utility.RandomEnumValue<ToyHues>();
            Name = "a toy soldier";
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            Item item = new FancyShirt();
            AddItem(item);
            item = new PlateChest();
            AddItem(item);
            item = new Helmet();
            AddItem(item);
            item = new Boots();
            AddItem(item);
            item = new LongPants();
            AddItem(item);


            if (AI == AIType.AI_Melee)
                switch (Utility.Random(3))
                {
                    case 0: AddItem(new Club()); break;
                    case 1: AddItem(new Dagger()); break;
                    case 2: AddItem(new Spear()); break;
                }
            else if (AI == AIType.AI_Archer)
                switch (Utility.Random(6))
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        AddItem(new Bow());
                        break;
                    case 4:
                        AddItem(new Crossbow());
                        break;
                    case 5:
                        AddItem(new HeavyCrossbow());
                        break;
                }
            else if (AI == AIType.AI_BaseHybrid)
            {
                PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
                PackStrongPotions(6, 12);
                PackItem(new Pouch(), lootType: LootType.UnStealable);
            }

            HueAllLayers(Hue);
            RemoveHueOnLiftAllLayers();
        }

        public override void GenerateLoot()
        {
            base.GenerateLoot();
            if (Utility.Chance(0.2))
            {
                /* Equal chance to get either a weapon or armor piece (for soldiers that have both)
                 * Since items are always added in the same order, we need to shuffle to ensure a 50/50 split
                 *  between weapons and armor.
                 * Mages for instance don't have a weapon, that's handled.
                 */
                Utility.Shuffle(Items);                     // mix up ordering so we don't always get the same armor piece or weapon
                List<Item> list = new List<Item>();         // since soldiers have more armor than weapons, only select one of each group        
                bool have_weapon = false;
                bool have_armor = false;
                foreach (Item equip in Items)
                {
                    if (have_weapon && have_armor)
                        break;

                    if (have_weapon == false && equip is BaseWeapon)
                    {
                        list.Add(equip);
                        have_weapon = true;
                    }
                    else if (have_armor == false && equip is BaseArmor)
                    {
                        list.Add(equip);
                        have_armor = true;
                    }
                }
                Utility.Shuffle(list);                                          // shuffle again since armor has a greater chance to be on top
                // should be at most one armor and one weapon
                foreach (Item equip in list)
                {
                    equip.SetItemBool(Item.ItemBoolTable.NoScroll, true);               // don't make scroll
                    equip.SetItemBool(Item.ItemBoolTable.RemoveHueOnLift, false);       // don't remove hue on lift
                    equip.Hue = 0;                                              // remove any existing hue
                    equip.HideAttributes = true;                                // don't display attributes
                    equip.Name = string.Format("toy soldier's {0}", equip.GetType().Name.ToLower());

                    if (equip is BaseWeapon bw)
                    {   // drop our weapon!
                        bw = (BaseWeapon)Loot.ImbueWeaponOrArmor(bw, 2);        // add low magics
                        bw.Quality = WeaponQuality.Exceptional;                 // addition buff
                        bw.Identified = true;
                        bw.Resource = (Hue == (int)ToyHues.Green) ? CraftResource.Verite : CraftResource.Gold;
                        break;
                    }
                    else if (equip is BaseArmor ba)
                    {   // drop our armor!
                        ba = (BaseArmor)Loot.ImbueWeaponOrArmor(ba, 2);        // add low magics
                        ba.Quality = ArmorQuality.Exceptional;                 // addition buff
                        ba.Identified = true;
                        ba.Resource = (Hue == (int)ToyHues.Green) ? CraftResource.Verite : CraftResource.Gold;
                        break;
                    }
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    goto case 0;
                case 0:
                    break;
            }

        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    goto case 0;
                case 0:
                    break;
            }
        }
    }
}