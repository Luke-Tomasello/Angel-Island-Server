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

/* Scripts/Mobiles/Monsters/LBR/Meers/MeerWarrior.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Spells;
using System;

namespace Server.Mobiles
{
    [CorpseName("a meer corpse")]
    public class MeerWarrior : BaseCreature
    {
        [Constructable]
        public MeerWarrior()
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a meer warrior";
            Body = 771;
            BardImmune = true;

            SetStr(86, 100);
            SetDex(186, 200);
            SetInt(86, 100);

            SetHits(52, 60);

            SetDamage(12, 19);

            SetSkill(SkillName.MagicResist, 91.0, 100.0);
            SetSkill(SkillName.Tactics, 91.0, 100.0);
            SetSkill(SkillName.Wrestling, 91.0, 100.0);

            VirtualArmor = 22;

            Fame = 2000;
            Karma = 5000;
        }

        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override bool InitialInnocent { get { return true; } }

        public override void OnDamage(int amount, Mobile from, bool willKill, DamageContext context)
        {
            if (from != null && !willKill && amount > 3 && from != null && !InRange(from, 7))
            {
                this.MovingEffect(from, 0xF51, 10, 0, false, false);
                SpellHelper.Damage(TimeSpan.FromSeconds(1.0), from, this, Utility.RandomMinMax(30, 40) - (Core.RuleSets.AOSRules() ? 0 : 10), new DamageContext { Source = this });
            }

            base.OnDamage(amount, from, willKill, context);
        }

        public override int GetHurtSound()
        {
            return 0x156;
        }

        public override int GetDeathSound()
        {
            return 0x15C;
        }

        public MeerWarrior(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            // https://www.uoguide.com/Meer_Warrior
            // Post-era creature (Mondain's Legacy). No Stratics-era entry.
            // 250 - 400 Gold
            // Special: 2-5 Gems, Magic Weapons/Armor
            // Note: Excludes ML systems (artifacts, quest items, etc.)

            if (Spawning)
            {
                PackGold(250, 400);
            }
            else
            {
                PackGem(2, 5);

                PackMagicEquipment(2, 3, 1.0, 1.0);
                PackMagicEquipment(2, 3, 0.50, 0.50);
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