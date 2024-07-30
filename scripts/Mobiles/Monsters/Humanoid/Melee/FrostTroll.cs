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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/FrostTroll.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a frost troll corpse")]
    public class FrostTroll : BaseCreature
    {
        [Constructable]
        public FrostTroll()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a frost troll";
            Body = 55;
            BaseSoundID = 461;

            SetStr(227, 265);
            SetDex(66, 85);
            SetInt(46, 70);

            SetHits(140, 156);

            SetDamage(14, 20);

            SetSkill(SkillName.MagicResist, 65.1, 80.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 80.1, 100.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 50;
        }

        public override int Meat { get { return 2; } }
        // http://web.archive.org/web/20020301014847/uo.stratics.com/hunters/frosttroll.shtml
        // page not found
        // http://web.archive.org/web/20080627205222/uo.stratics.com/database/view.php?db_content=hunters&id=179
        // 25 - 175 Gold. 1 Gem, Weapon Carved: 2 Raw Ribs Special: Level 1 Treasure Map
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 1; } }

        public FrostTroll(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGem();
                PackPotion();
                PackItem(new Arrow(10));
                PackGold(50, 100);
                PackItem(new DoubleAxe()); // TODO: Weapon??

                // Category 2 MID
                PackMagicItem(1, 1, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020301014847/uo.stratics.com/hunters/frosttroll.shtml
                    // page not found
                    // http://web.archive.org/web/20080627205222/uo.stratics.com/database/view.php?db_content=hunters&id=179
                    // 25 - 175 Gold. 1 Gem, Weapon Carved: 2 Raw Ribs Special: Level 1 Treasure Map
                    if (Spawning)
                    {
                        PackGold(25, 175);
                    }
                    else
                    {
                        PackGem();
                        PackItem(new DoubleAxe());
                    }
                }
                else
                {
                    if (Spawning)
                        PackItem(new DoubleAxe()); // TODO: Weapon??

                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Gems);
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