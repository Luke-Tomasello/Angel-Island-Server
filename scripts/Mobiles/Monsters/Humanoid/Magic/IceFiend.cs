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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/IceFiend.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	7/2/04
 *		Change chance to drop a magic item to 20% 
 *		add a 5% chance for a bonus drop at next intensity level
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("an ice fiend corpse")]
    public class IceFiend : BaseCreature
    {
        [Constructable]
        public IceFiend()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "an ice fiend";
            Body = 43;
            BaseSoundID = 357;

            SetStr(376, 405);
            SetDex(176, 195);
            SetInt(201, 225);

            SetHits(226, 243);

            SetDamage(8, 19);

            SetSkill(SkillName.EvalInt, 80.1, 90.0);
            SetSkill(SkillName.Magery, 80.1, 90.0);
            SetSkill(SkillName.MagicResist, 75.1, 85.0);
            SetSkill(SkillName.Tactics, 80.1, 90.0);
            SetSkill(SkillName.Wrestling, 80.1, 100.0);


            Fame = 18000;
            Karma = -18000;

            VirtualArmor = 60;
        }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 4 : 0; } }
        public override int Meat { get { return 1; } }

        public IceFiend(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(300, 400);
                PackScroll(2, 6);
                PackScroll(2, 6);
                PackMagicEquipment(1, 2, 0.25, 0.25);
                PackMagicEquipment(1, 2, 0.05, 0.05);

                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020202091622/uo.stratics.com/hunters/icefiend.shtml
                    // 500 to 800 Gold, Scrolls (circles 4-6), Magic items
                    if (Spawning)
                    {
                        PackGold(500, 800);
                    }
                    else
                    {
                        PackScroll(4, 6);
                        PackScroll(4, 6, .8);
                        PackScroll(4, 6, .5);

                        if (Utility.RandomBool())
                            PackMagicEquipment(1, 2);
                        else
                            PackMagicItem(1, 2, 0.10);
                    }
                }
                else
                {
                    AddLoot(LootPack.FilthyRich);
                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.MedScrolls, 2);
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