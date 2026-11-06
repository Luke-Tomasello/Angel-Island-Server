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

/* Scripts/Mobiles/Monsters/AOS/TreeFellow.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a treefellow corpse")]
    public class Treefellow : BaseCreature
    {
        public override WeaponAbility GetWeaponAbility()
        {
            return WeaponAbility.Dismount;
        }

        [Constructable]
        public Treefellow()
            : base(AIType.AI_Melee, FightMode.Aggressor | FightMode.Evil, 10, 1, 0.25, 0.5)
        {
            Name = "a treefellow";
            Body = 301;

            SetStr(196, 220);
            SetDex(31, 55);
            SetInt(66, 90);

            SetHits(118, 132);

            SetDamage(12, 16);

            SetSkill(SkillName.MagicResist, 40.1, 55.0);
            SetSkill(SkillName.Tactics, 65.1, 90.0);
            SetSkill(SkillName.Wrestling, 65.1, 85.0);

            Fame = 500;
            Karma = 1500;

            VirtualArmor = 24;
        }

        public override int GetIdleSound()
        {
            return 443;
        }

        public override int GetDeathSound()
        {
            return 31;
        }

        public override int GetAttackSound()
        {
            return 672;
        }

        public Treefellow(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=749
            // 200 - 300 Gold
            // Special: 2-5 Gems, Magic Weapons/Armor
            // Cut Up: None

            if (Spawning)
            {
                PackGold(200, 300);
            }
            else
            {
                PackGem(2, 5);

                PackMagicEquipment(2, 3, 0.75, 0.75);
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

            if (BaseSoundID == 442)
                BaseSoundID = -1;
        }
    }
}