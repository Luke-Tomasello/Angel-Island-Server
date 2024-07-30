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

/* Scripts/Mobiles/Monsters/Reptile/Melee/Kraken.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  11/10/04, Froste
 *      Removed PirateHat as loot, now restricted to "brethren only" drop
 *	7/21/04, mith
 *		Added PirateHat as loot, 5% drop.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	6/29/04, Pix
 *		Fixed MIB loot to spawn for the current facet.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a krakens corpse")]
    public class Kraken : BaseCreature
    {
        [Constructable]
        public Kraken()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a kraken";
            Body = 77;
            BaseSoundID = 353;

            SetStr(756, 780);
            SetDex(226, 245);
            SetInt(26, 40);

            SetHits(454, 468);
            SetMana(0);

            SetDamage(19, 33);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 60.0);

            Fame = 11000;
            Karma = -11000;

            VirtualArmor = 50;

            CanSwim = true;
            CantWalkLand = true;
        }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 3 : 0; } }

        public Kraken(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(200, 300);

                if (Utility.RandomDouble() <= 0.05)
                    PackItem(new Rope());

                // Adam: I think this is in error. See the comments below
                //	we may want to consider removing this
                if (Utility.RandomDouble() <= 0.05)
                    PackItem(new MessageInABottle(this.Map));
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020313114942/uo.stratics.com/hunters/kraken.shtml
                    // Messages in a Bottle, Treasure Maps, Rope, 300 - 600 Gold

                    if (Spawning)
                    {
                        PackGold(300, 600);
                    }
                    else
                    {
                        // No mib, SpecialFishingNet, or Map - these are added as part of the fishing system. 
                        //	See fishing.cs
                        // http://web.archive.org/web/20020313114942/uo.stratics.com/hunters/kraken.shtml
                        // There are two different types of Kraken, the one found in dungeons such as Shame and Wind Park and the one that 
                        //	spawns as a result of a fishing net. The difference between the two lies in the fact that the open sea Kraken can 
                        //	breathe fire, much like a Dragon, and can give a Message in a Bottle as loot. As a result of that, the open sea 
                        //	Kraken gives more Fame and Karma when killed. The damage done by the open sea Kraken breath attack diminishes 
                        //	as the Kraken runs out of mana.

                        if (Utility.RandomDouble() <= 0.05)
                            PackItem(new Rope());
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        Rope rope = new Rope();
                        rope.ItemID = 0x14F8;
                        PackItem(rope);

                        // No mib, SpecialFishingNet, or Map - these are added as part of the fishing system. 
                        //	See fishing.cs
                        // http://web.archive.org/web/20020313114942/uo.stratics.com/hunters/kraken.shtml
                        // There are two different types of Kraken, the one found in dungeons such as Shame and Wind Park and the one that 
                        //	spawns as a result of a fishing net. The difference between the two lies in the fact that the open sea Kraken can 
                        //	breathe fire, much like a Dragon, and can give a Message in a Bottle as loot. As a result of that, the open sea 
                        //	Kraken gives more Fame and Karma when killed. The damage done by the open sea Kraken breath attack diminishes 
                        //	as the Kraken runs out of mana.
                    }

                    AddLoot(LootPack.Rich);
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