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

/* Scripts\Engines\ChampionSpawn\Champs\Special\AbominableSnowman.cs
 * ChangeLog
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("an abominable snowman corpse")]
    public class AbominableSnowman : BaseCreature
    {
        [Constructable]
        public AbominableSnowman()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Bumble";
            Body = 161;         // ice elemental
            Hue = 1150;         // white
            BaseSoundID = 263;  // snow elemental

            Utility.CopyStats(typeof(ArcticOgreLord), this);
        }

        public override int TreasureMapLevel { get { return Utility.RandomList(2, 3); } }

        public override AuraType MyAura { get { return AuraType.Ice; } }

        public AbominableSnowman(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AllShards)
            {
                if (Spawning)
                {
                    // nothing
                }
                else
                {
                    // Gold
                    List<Item> list = Utility.CopyLoot(typeof(ArcticOgreLord));
                    foreach (Item item in list)
                        if (item is Gold)
                            PackItem(item);
                        else
                            item.Delete();

                    // magics loot pack: weapons/armor, regs, etc..
                    list = new List<Item>(Loot.StandardMagicLoot(level: Utility.RandomList(5, 6), scroll_count: 1, item_count: 1, reagent_count: 20, gem_count: 10));
                    foreach (Item item in list)
                    {
                        item.SetItemBool(Item.ItemBoolTable.NoScroll, true);
                        PackItem(item);
                    }

                    // guarantee 1/10
                    if (Utility.Random(10) == 0)
                    {
                        Item reward = null;
                        while (reward == null)
                        {
                            switch (Utility.Random(5))
                            {
                                case 0:
                                    {
                                        // snow pile
                                        int sp_count = Utility.RandomList(0, 3);
                                        for (int ix = 0; ix < sp_count; ix++)
                                        {
                                            reward = new SnowPile();
                                            reward.LootType = LootType.Regular;   // or it won't drop
                                            PackItem(reward);
                                        }
                                        break;
                                    }
                                case 1:
                                    {
                                        // heart of the abominable snowman
                                        if (Utility.Chance(0.10))
                                        {
                                            string frozen_heart = "heart of the abominable snowman";
                                            reward = new Item(7405);
                                            reward.Hue = 1154;
                                            reward.Name = frozen_heart;
                                            reward.Weight = 1;
                                            PackItem(reward);
                                        }
                                        break;
                                    }
                                case 2:
                                    {
                                        // misfit toy
                                        if (Utility.Chance(0.08))
                                        {
                                            int[] interesting_hues = new int[] { 0xB85, 0xB87, 0xB89, 0xB8c, 0xB8e, 0xB8f, 0xB93, 0xB97, 0xB98, 0xB9A, 0x482 };
                                            int[] interesting_bodies = new int[] { 0x2101 /*boar*/, 0x2611 /*horde demon*/, 0x20DF/*Ogre*/, 0x2103/*Cow*/, 0x20F6/*Llama*/, 0x211E /*Grizzly Bear*/, 0x20D0/*GiantRat*/ };
                                            string misfit = "a misfit toy";
                                            reward = new Item(Utility.RandomList(interesting_bodies));
                                            reward.Hue = Utility.RandomList(interesting_hues);
                                            reward.Name = misfit;
                                            reward.Weight = 1;
                                            PackItem(reward);
                                        }
                                        break;
                                    }
                                case 3:
                                    {
                                        // mask of the abomination
                                        if (Utility.Chance(0.08))
                                        {
                                            string abomination = "mask of the abomination";
                                            if (Utility.RandomBool())
                                                reward = new BearMask(1154);
                                            else
                                                reward = new OrcishMask(1154);
                                            reward.Name = abomination;
                                            PackItem(reward);
                                        }
                                        break;
                                    }
                                case 4:
                                    {
                                        // mulled wine
                                        if (Utility.Chance(0.05))
                                        {
                                            reward = new MulledWineCauldronDeed();
                                            PackItem(reward);
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}