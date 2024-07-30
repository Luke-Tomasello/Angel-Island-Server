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

/* Server/Engines/EventResources/Obsidian/Mobiles/BlackrockElemental.cs
 * CHANGELOG:
 *  10/18/23, Yoar
 *      Initial version.
 */

using Server.Items;

namespace Server.Mobiles
{
    [TypeAlias("Server.Engines.Obsidian.BlackrockElemental")]
    [CorpseName("a blackrock elemental corpse")]
    public class BlackrockElemental : BaseCreature
    {
        [Constructable]
        public BlackrockElemental()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a blackrock elemental";
            Body = 112;
            Hue = CraftResources.GetHue(CraftResource.Obsidian);
            BaseSoundID = 268;

            SetStr(350);
            SetDex(150);
            SetInt(100);

            SetHits(1200);

            SetDamage(9, 16);

            SetSkill(SkillName.MagicResist, 50.1, 95.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 23000;
            Karma = -23000;

            VirtualArmor = 50;
        }

        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        public override int TreasureMapLevel { get { return 5; } }

        public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
        {
            if (from is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)from;

                if (bc.Controlled || bc.BardTarget == this)
                    damage = 0; // Immune to pets and provoked creatures
            }
        }

        public override void CheckReflect(Mobile caster, ref bool reflect)
        {
            reflect = true; // Every spell is reflected back to the caster
        }

        public override void GenerateLoot()
        {
            // SP custom

            if (Spawning)
            {
                PackGold(1800, 2400);
            }
            else
            {
                PackMagicEquipment(1, 3);
                PackMagicEquipment(2, 3, 0.50, 0.50);
                PackItem(new Obsidian(25));
                PackGem(Utility.Random(3, 5));
                PackGem(Utility.Random(3, 5));
            }
        }

        public BlackrockElemental(Serial serial)
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