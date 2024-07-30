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

/* Scripts/Mobiles/Monsters/Arachnid/Melee/TerathanDrone.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a terathan drone corpse")]
    public class TerathanDrone : BaseCreature
    {
        [Constructable]
        public TerathanDrone()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a terathan drone";
            Body = 71;
            BaseSoundID = 594;

            SetStr(36, 65);
            SetDex(96, 145);
            SetInt(21, 45);

            SetHits(22, 39);
            SetMana(0);

            SetDamage(6, 12);

            SetSkill(SkillName.Poisoning, 40.1, 60.0);
            SetSkill(SkillName.MagicResist, 30.1, 45.0);
            SetSkill(SkillName.Tactics, 30.1, 50.0);
            SetSkill(SkillName.Wrestling, 40.1, 50.0);

            Fame = 2000;
            Karma = -2000;

            VirtualArmor = 24;
        }

        public override int Meat { get { return 4; } }

        public TerathanDrone(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackItem(new SpidersSilk(2));
                PackGold(25, 50);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020207055638/uo.stratics.com/hunters/terdrone.shtml
                    // 	0 to 50 Gold, 2 Spiders Silk
                    if (Spawning)
                    {
                        PackGold(0, 50);
                    }
                    else
                    {
                        PackItem(new SpidersSilk(2));
                    }
                }
                else
                {
                    if (Spawning)
                        PackItem(new SpidersSilk(2));

                    AddLoot(LootPack.Meager);
                    // TODO: weapon?
                }
            }
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.TerathansAndOphidians; }
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

            if (BaseSoundID == 589)
                BaseSoundID = 594;
        }
    }
}