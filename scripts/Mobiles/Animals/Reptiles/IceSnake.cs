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

/* Scripts/Mobiles/Animals/Reptiles/IceSnake.cs
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
    [CorpseName("an ice snake corpse")]
    [TypeAlias("Server.Mobiles.Icesnake")]
    public class IceSnake : BaseCreature
    {
        [Constructable]
        public IceSnake()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "an ice snake";
            Body = 52;
            Hue = 0x480;
            BaseSoundID = 0xDB;

            SetStr(42, 54);
            SetDex(36, 45);
            SetInt(26, 30);

            SetMana(0);

            SetDamage(4, 12);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 39.3, 54.0);
            SetSkill(SkillName.Wrestling, 39.3, 54.0);

            Fame = 900;
            Karma = -900;

            VirtualArmor = 30;
        }

        public override int Meat { get { return 1; } }

        public IceSnake(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
                PackGold(25, 50);
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020607033340/uo.stratics.com/hunters/icesnake.shtml
                    // loot - none
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
                    AddLoot(LootPack.Meager);
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