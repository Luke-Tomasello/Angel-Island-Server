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

/* Scripts/Mobiles/Monsters/Plant/Melee/Quagmire.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("a quagmire corpse")]
    public class Quagmire : BaseCreature
    {
        [Constructable]
        public Quagmire()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a quagmire";
            Body = 789;
            BaseSoundID = 352;

            SetStr(101, 130);
            SetDex(66, 85);
            SetInt(31, 55);

            SetHits(91, 105);

            SetDamage(10, 14);

            SetSkill(SkillName.MagicResist, 65.1, 75.0);
            SetSkill(SkillName.Tactics, 50.1, 60.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 1500;
            Karma = -1500;

            VirtualArmor = 32;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override Poison HitPoison { get { return Poison.Lethal; } }
        public override double HitPoisonChance { get { return 0.1; } }

        public Quagmire(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(60, 90);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // 02 2002 no page
                    // http://web.archive.org/web/20020806154029/uo.stratics.com/hunters/quagmire.shtml
                    // unknown
                    // http://web.archive.org/web/20021014232959/uo.stratics.com/hunters/quagmire.shtml
                    // unknown
                    // http://web.archive.org/web/20021215191936/uo.stratics.com/hunters/quagmire.shtml
                    // unknown
                    // http://web.archive.org/web/20030210141933/uo.stratics.com/hunters/quagmire.shtml
                    // unknown
                    // http://web.archive.org/web/20030415180157/uo.stratics.com/database/view.php?db_content=hunters&id=281
                    // unknown
                    // http://uo.stratics.com/database/view.php?db_content=hunters&id=281
                    // 125 - 175 Gold.
                    if (Spawning)
                    {
                        PackGold(125, 175);
                    }
                    else
                    {
                    }
                }
                else
                {   // Standard RunUO
                    AddLoot(LootPack.Average);
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

            if (BaseSoundID == -1)
                BaseSoundID = 352;
        }
    }
}