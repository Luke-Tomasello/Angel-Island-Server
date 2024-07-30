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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/Betrayer.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    public class Betrayer : BaseCreature
    {
        [Constructable]
        public Betrayer()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = NameList.RandomName("male");
            Title = "the betrayer";
            Body = 767;
            BardImmune = true;


            SetStr(401, 500);
            SetDex(81, 100);
            SetInt(151, 200);

            SetHits(241, 300);

            SetDamage(16, 22);

            SetSkill(SkillName.Anatomy, 90.1, 100.0);
            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 50.1, 100.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 120.1, 130.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 65;
            SpeechHue = Utility.RandomSpeechHue();
        }

        public override bool AlwaysMurderer { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override int Meat { get { return 1; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }

        public Betrayer(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGem();

                switch (Utility.Random(4))
                {
                    case 0: PackItem(new Katana()); break;
                    case 1: PackItem(new BodySash()); break;
                    case 2: PackItem(new Halberd()); break;
                    case 3: PackItem(new LapHarp()); break;
                }

                PackGold(200, 300);

                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // no LBR
                    //http://web.archive.org/web/20020403051743/uo.stratics.com/hunters/betrayer.shtml
                    // 200-300 Gold, Gem, Power Crystal, Level 5 Treasue Map, Blackthorne's "A welcome"
                    if (Spawning)
                    {
                        PackGold(200, 300);
                    }
                    else
                    {
                    }
                }
                else
                {
                    if (Spawning)
                    {   // probably not for siege
                        //PackItem(new PowerCrystal());

                        //if (0.02 > Utility.RandomDouble())
                        //PackItem(new BlackthornWelcomeBook());
                    }

                    AddLoot(LootPack.FilthyRich);
                    AddLoot(LootPack.Rich);
                    AddLoot(LootPack.Gems, 1);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}