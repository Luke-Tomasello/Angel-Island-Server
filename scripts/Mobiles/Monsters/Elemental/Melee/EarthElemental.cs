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

/* Scripts/Mobiles/Monsters/Elemental/Melee/EarthElemental.cs
 * ChangeLog
 *	7/16/10, adam
 *		o decrease average dex
 *		o decrease average int
 *		o increase average hp
 *		o decrease average damage
 *		o increase virtual armor
 *		o increase average wrestling
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	4/27/05, Kit
 *		Adjusted dispell difficulty
 *  10/3/04, Jade
 *      Added fertile dirt as loot.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an earth elemental corpse")]
    public class EarthElemental : BaseCreature
    {
        public override double DispelDifficulty { get { return 56; } }
        public override double DispelFocus { get { return 45.0; } }

        [Constructable]
        public EarthElemental()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "an earth elemental";
            Body = 14;
            BaseSoundID = 268;

            SetStr(126, 155);
            SetDex(66, 85);
            SetInt(71, 92);
            SetHits(76, 93);
            SetDamage(9, 16);

            SetSkill(SkillName.MagicResist, 50.1, 95.0);
            SetSkill(SkillName.Tactics, 60.1, 100.0);
            SetSkill(SkillName.Wrestling, 60.1, 100.0);

            Fame = 3500;
            Karma = -3500;

            VirtualArmor = 50;
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=745
            // (unchanged)
            ControlSlots = 2;
        }

        public EarthElemental(bool summoned)
            : this()
        {
            if (summoned == true)
            {
                SetStr(126, 155);
                SetDex(50 - 10, 50 + 10);
                SetInt(50 - 10, 50 + 10);
                SetHits(150 - 10, 150 + 10);
                SetDamage(12 - 1, 12 + 1);

                SetSkill(SkillName.MagicResist, 85 - 10, 85 + 10);
                SetSkill(SkillName.Tactics, 60.1, 100.0);
                SetSkill(SkillName.Wrestling, 90 - 10, 90 + 10);

                VirtualArmor = 50;
            }
        }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }

        public EarthElemental(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGem();
                // Jade: Add FertileDirt
                PackItem(new FertileDirt(Utility.RandomMinMax(15, 30)));
                PackItem(new IronOre(5)); // TODO: Five small iron ore
                PackGold(100, 150);
                PackItem(new MandrakeRoot());
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020202092911/uo.stratics.com/hunters/earthelemental.shtml
                    // 200 - 350 Gold, Gems, Ore, Fertile Dirt reagent

                    if (Spawning)
                    {
                        PackGold(200, 350);
                    }
                    else
                    {
                        PackItem(new FertileDirt(Utility.RandomMinMax(1, 4)));
                        PackItem(new IronOre(3));
                    }
                }
                else
                {   // standard runuo
                    if (Spawning)
                    {
                        PackItem(new FertileDirt(Utility.RandomMinMax(1, 4)));
                        PackItem(new IronOre(3)); // TODO: Five small iron ore
                        PackItem(new MandrakeRoot());
                    }

                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Meager);
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