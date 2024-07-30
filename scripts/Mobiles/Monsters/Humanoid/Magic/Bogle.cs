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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/Bogle.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a ghostly corpse")]
    public class Bogle : BaseCreature
    {
        [Constructable]
        public Bogle()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a bogle";
            Body = 153;
            BaseSoundID = 0x482;

            SetStr(76, 100);
            SetDex(76, 95);
            SetInt(36, 60);

            SetHits(46, 60);

            SetDamage(7, 11);

            SetSkill(SkillName.EvalInt, 55.1, 70.0);
            SetSkill(SkillName.Magery, 35.1, 40.0);
            SetSkill(SkillName.MagicResist, 35.1, 40.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 55.0);

            Fame = 4000;
            Karma = -4000;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public Bogle(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(10, 50);
                PackItem(Loot.RandomWeapon());
                PackItem(new Bone());
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // none circa 2002
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackItem(Loot.RandomWeapon());
                        PackItem(new Bone());
                    }

                    AddLoot(LootPack.Meager);
                }
            }
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.FeyAndUndead; }
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