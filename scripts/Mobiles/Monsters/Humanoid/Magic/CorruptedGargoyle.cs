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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/CorruptedGargoyle.cs
 * ChangeLog
 *	10/19/23, Yoar
 *		Initial Version.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an undead gargoyle corpse")]
    public class CorruptedGargoyle : BaseCreature
    {
        [Constructable]
        public CorruptedGargoyle()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a corrupted gargoyle";
            Body = 4;
            Hue = 2101;
            BaseSoundID = 372;

            SetStr(416, 505);
            SetDex(146, 165);
            SetInt(566, 655);

            SetHits(451, 500);

            SetDamage(13, 24);

            SetSkill(SkillName.EvalInt, 100.1, 110.0);
            SetSkill(SkillName.Magery, 100.1, 110.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 150.5, 200.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 75.1, 100.0);

            Fame = 20000;
            Karma = -20000;

            VirtualArmor = 60;
        }

        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override bool CanRummageCorpses { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override int TreasureMapLevel { get { return 5; } }

        public override void GenerateLoot()
        {
            // SP custom

            if (Spawning)
            {
                PackGold(1200, 1500);
            }
            else
            {
                PackPotion();
                PackMagicEquipment(1, 3);
                PackMagicEquipment(2, 3, 0.33, 0.33);
                PackGem(Utility.Random(3, 5));
                PackGem(Utility.Random(3, 5), 0.50);
                PackScroll(4, 7);
                PackScroll(4, 7, 0.50);
                PackItem(typeof(GargoylesPickaxe), 0.15);
            }
        }

        public CorruptedGargoyle(Serial serial)
            : base(serial)
        {
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