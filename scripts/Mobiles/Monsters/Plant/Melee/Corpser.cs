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

/* Scripts/Mobiles/Monsters/Plant/Melee/Corpser.cs
 * ChangeLog
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a corpser corpse")]
    public class Corpser : BaseCreature
    {
        [Constructable]
        public Corpser()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a corpser";
            Body = 8;
            BaseSoundID = 684;

            SetStr(156, 180);
            SetDex(26, 45);
            SetInt(26, 40);

            SetHits(94, 108);
            SetMana(0);

            SetDamage(10, 23);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 45.1, 60.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 18;
        }

        public override Poison PoisonImmune { get { return Poison.Lesser; } }
        public override bool DisallowAllMoves { get { return true; } }

        public Corpser(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                if (0.25 > Utility.RandomDouble())
                    PackItem(new Board(10));
                else
                    PackItem(new Log(10));

                PackReg(3);
                PackGold(25, 50);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020207054322/uo.stratics.com/hunters/corpser.shtml
                    // 0 to 50 Gold, Logs or Boards, Executioner's Cap reagent

                    if (Spawning)
                    {
                        PackGold(0, 50);
                    }
                    else
                    {
                        if (0.25 > Utility.RandomDouble())
                            PackItem(new Board(10));
                        else
                            PackItem(new Log(10));

                        //	http://uo.stratics.com/content/basics/reagenttome.shtml
                        if (0.2 >= Utility.RandomDouble())
                            PackItem(new ExecutionersCap());
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        if (0.25 > Utility.RandomDouble())
                            PackItem(new Board(10));
                        else
                            PackItem(new Log(10));

                        PackItem(new MandrakeRoot(3));
                    }

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

            if (BaseSoundID == 352)
                BaseSoundID = 684;
        }
    }
}