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

/* Scripts/Mobiles/Monsters/Plant/Melee/Bogling.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  6/9/04, OldSalty
 * 		Added Seeds to creatures pack consistent with RunUO 1.0
 *  6/5/04, Pix
 * 		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a plant corpse")]
    public class Bogling : BaseCreature
    {
        [Constructable]
        public Bogling()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a bogling";
            Body = 779;
            BaseSoundID = 422;

            SetStr(96, 120);
            SetDex(91, 115);
            SetInt(21, 45);

            SetHits(58, 72);

            SetDamage(5, 7);

            SetSkill(SkillName.MagicResist, 75.1, 100.0);
            SetSkill(SkillName.Tactics, 55.1, 80.0);
            SetSkill(SkillName.Wrestling, 55.1, 75.0);

            Fame = 450;
            Karma = -450;

            VirtualArmor = 28;

        }

        public override int Hides { get { return 6; } }
        public override int Meat { get { return 1; } }

        public Bogling(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(25, 50);
                PackItem(new Log(4));
                PackItem(new Engines.Plants.Seed());
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020207055120/http://uo.stratics.com/hunters/ore_elementals.shtml
                    // unknown
                    // http://web.archive.org/web/20020806163656/uo.stratics.com/hunters/bogling.shtml
                    // 40 - 60 Gold, 4 Logs, 1 Seed, 1 Rib, 6 Leather
                    if (Spawning)
                    {
                        PackGold(40, 60);
                    }
                    else
                    {
                        PackItem(new Log(4));
                        if (Engines.Plants.PlantSystem.Enabled)
                            PackItem(new Engines.Plants.Seed());
                    }
                }
                else
                {   // Standard RunUO
                    if (Spawning)
                    {
                        PackItem(new Log(4));
                        if (Engines.Plants.PlantSystem.Enabled)
                            PackItem(new Engines.Plants.Seed());
                    }

                    AddLoot(LootPack.Meager);
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