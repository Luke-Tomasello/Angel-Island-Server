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

/* Scripts/Mobiles/Monsters/AOS/FleshGolem.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  7/30/04, Adam
 *		return gold to 300-550 from 200-300
 *		NOTE: This is too much gold for this creature, but this change is being done
 *		specifically to generate PvP competition with in Flesh Golem room in Wrong.
 *  7/21/04, Adam
 *		reduce from 300, 550 ==> 200, 300
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a flesh golem corpse")]
    public class FleshGolem : BaseCreature
    {
        public override WeaponAbility GetWeaponAbility()
        {
            return WeaponAbility.BleedAttack;
        }

        [Constructable]
        public FleshGolem()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a flesh golem";
            Body = 304;
            BaseSoundID = 684;

            SetStr(176, 200);
            SetDex(51, 75);
            SetInt(46, 70);

            SetHits(106, 120);

            SetDamage(18, 22);

            SetSkill(SkillName.MagicResist, 50.1, 75.0);
            SetSkill(SkillName.Tactics, 55.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 70.0);

            Fame = 1000;
            Karma = -1800;

            VirtualArmor = 34;
        }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }

        public FleshGolem(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            PackGem();

            // adam: see comments at the top of the file
            PackGold(300, 550);
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