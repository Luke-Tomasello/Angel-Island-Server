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

/* Scripts/Mobiles/Monsters/Plant/Magic/Reaper.cs
 * ChangeLog
 *  3/25/22, Adam (RandomTreeSeed())
 *      Add a Random Tree Seed to loot.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a reapers corpse")]
    public class Reaper : BaseCreature
    {
        [Constructable]
        public Reaper()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a reaper";
            Body = 47;
            BaseSoundID = 442;

            SetStr(66, 215);
            SetDex(66, 75);
            SetInt(101, 250);

            SetHits(40, 129);
            SetStam(0);

            SetDamage(9, 11);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 100.1, 125.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 50.1, 60.0);

            Fame = 3500;
            Karma = -3500;

            VirtualArmor = 40;
        }

        public override Poison PoisonImmune { get { return Poison.Greater; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 2 : 0; } }
        public override bool DisallowAllMoves { get { return true; } }

        public Reaper(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackItem(new Log(10));
                PackGold(0, 50);
                PackItem(new Engines.Plants.TreeSeed());
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20021014235628/uo.stratics.com/hunters/reaper.shtml
                    // 0 to 150 Gold, 10 Logs

                    if (Spawning)
                    {
                        PackGold(0, 150);
                    }
                    else
                    {
                        PackItem(new Log(10));
                        if (Core.RuleSets.PlantSystem())
                            PackItem(new Engines.Plants.TreeSeed());
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackItem(new Log(10));
                        PackItem(new MandrakeRoot(5));
                    }

                    AddLoot(LootPack.Average);
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