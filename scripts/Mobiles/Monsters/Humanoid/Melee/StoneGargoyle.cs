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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/StoneGargoyle.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a gargoyle corpse")]
    public class StoneGargoyle : BaseCreature
    {
        [Constructable]
        public StoneGargoyle()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a stone gargoyle";
            Body = 67;
            BaseSoundID = 0x174;

            SetStr(246, 275);
            SetDex(76, 95);
            SetInt(81, 105);

            SetHits(148, 165);

            SetDamage(11, 17);

            SetSkill(SkillName.MagicResist, 85.1, 100.0);
            SetSkill(SkillName.Tactics, 80.1, 100.0);
            SetSkill(SkillName.Wrestling, 60.1, 100.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 50;
        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 2 : 0; } }

        public StoneGargoyle(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGem();
                PackPotion();
                PackItem(new Arrow(10));
                PackGold(150, 250);
                PackScroll(1, 5);
                // TODO: Ore

                if (0.05 > Utility.RandomDouble())
                    PackItem(new GargoylesPickaxe());
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20021015002109/uo.stratics.com/hunters/stonegargoyle.shtml
                    // 150 to 250 Gold, Potions, Arrows, Gems, Scrolls, Ore, "a gargoyle's pickaxe"
                    if (Spawning)
                    {
                        PackGold(150, 250);
                    }
                    else
                    {
                        PackPotion();
                        PackPotion(0.3);
                        PackItem(new Arrow(Utility.RandomMinMax(1, 4)));
                        PackGem(Utility.RandomMinMax(1, 4));
                        PackScroll(1, 6);
                        PackScroll(1, 6, 0.5);
                        PackItem(new IronIngot(12));

                        if (0.05 > Utility.RandomDouble())
                            PackItem(new GargoylesPickaxe());
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackItem(new IronIngot(12));

                        if (0.05 > Utility.RandomDouble())
                            PackItem(new GargoylesPickaxe());
                    }

                    AddLoot(LootPack.Average, 2);
                    AddLoot(LootPack.Gems, 1);
                    AddLoot(LootPack.Potions);
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