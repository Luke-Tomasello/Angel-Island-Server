/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/ChaosDaemon.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("a chaos daemon corpse")]
    public class ChaosDaemon : BaseCreature
    {
        [Constructable]
        public ChaosDaemon()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a chaos daemon";
            Body = 792;
            BaseSoundID = 0x3E9;

            SetStr(106, 130);
            SetDex(171, 200);
            SetInt(56, 80);

            SetHits(91, 110);

            SetDamage(12, 17);

            SetSkill(SkillName.MagicResist, 85.1, 95.0);
            SetSkill(SkillName.Tactics, 70.1, 80.0);
            SetSkill(SkillName.Wrestling, 95.1, 100.0);

            Fame = 3000;
            Karma = -4000;

            VirtualArmor = 15;
        }

        public ChaosDaemon(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            // LBR: we'll not need to worry about this guys loot unless we decide to add LBR
            // http://web.archive.org/web/20020414105125/uo.stratics.com/hunters/chaosdaemon.shtml
            // unknown
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=46
            // 500 - 600 Gold
            // Special: 4-8 Gems, Magic Weapons/Armor
            // Cut Up: None

            if (Spawning)
            {
                PackGold(500, 600);
            }
            else
            {
                PackGem(4, 8);

                PackMagicEquipment(3, 4, 1.0, 1.0);
                PackMagicEquipment(3, 4, 0.50, 0.50);
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