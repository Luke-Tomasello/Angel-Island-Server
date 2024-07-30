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

/* Scripts/Mobiles/Animals/Reptiles/SilverSerpent.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Factions;

namespace Server.Mobiles
{
    [CorpseName("a silver serpent corpse")]
    [TypeAlias("Server.Mobiles.Silverserpant")]
    public class SilverSerpent : BaseCreature
    {
        public override Faction FactionAllegiance { get { return TrueBritannians.Instance; } }
        public override Ethics.Ethic EthicAllegiance { get { return Ethics.Ethic.Hero; } }

        [Constructable]
        public SilverSerpent()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.175, 0.350)
        {
            Body = 92;
            Name = "a silver serpent";
            BaseSoundID = 219;

            SetStr(161, 360);
            SetDex(151, 300);
            SetInt(21, 40);

            SetHits(97, 216);

            SetDamage(5, 21);

            SetSkill(SkillName.Poisoning, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 95.1, 100.0);
            SetSkill(SkillName.Tactics, 80.1, 95.0);
            SetSkill(SkillName.Wrestling, 85.1, 100.0);

            Fame = 7000;
            Karma = -7000;

            VirtualArmor = 40;
        }

        public override int Meat { get { return 1; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override Poison HitPoison { get { return Poison.Lethal; } }

        public SilverSerpent(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
                PackGold(150, 200);
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20011205120613/uo.stratics.com/hunters/silverserpent.shtml
                    // 1 Raw Ribs (carved)
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
                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.Gems, 2);
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
                BaseSoundID = 219;
        }
    }
}