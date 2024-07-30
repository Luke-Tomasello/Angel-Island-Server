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

/* Scripts/Mobiles/Monsters/Mammal/Melee/VorpalBunny.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 1 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a vorpal bunny corpse")]
    public class VorpalBunny : BaseCreature
    {
        [Constructable]
        public VorpalBunny()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.175, 0.350)
        {
            Name = "a vorpal bunny";
            Body = 205;
            Hue = 0x480;
            BardImmune = true;

            SetStr(15);
            SetDex(2000);
            SetInt(1000);

            SetHits(2000);
            SetStam(500);
            SetMana(0);

            SetDamage(1);


            SetSkill(SkillName.MagicResist, 200.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 1000;
            Karma = 0;

            VirtualArmor = 4;

            DelayBeginTunnel();
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(250, 350);
                PackItem(new Carrot());
                // TODO: statue, eggs
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20021215144938/uo.stratics.com/hunters/vorpalbunny.shtml
                    // 600-700 Gold, Carrots, Gems, Scrolls, Magic Items, Statue, Brightly Colored Eggs
                    if (Spawning)
                    {
                        PackGold(600, 700);
                    }
                    else
                    {
                        PackItem(new Carrot(Utility.RandomMinMax(5, 10)));

                        PackGem(1, .9);
                        PackGem(1, .05);

                        PackScroll(1, 7);
                        PackScroll(1, 7, 0.2);

                        if (Utility.RandomBool())
                            PackMagicEquipment(1, 2);
                        else
                            PackMagicItem(1, 2, 0.30);

                        PackStatue(0.02);

                        if (Utility.Random(5) == 0)
                            PackItem(new BrightlyColoredEggs());
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        int carrots = Utility.RandomMinMax(5, 10);
                        PackItem(new Carrot(carrots));

                        if (Utility.Random(5) == 0)
                            PackItem(new BrightlyColoredEggs());

                        PackStatue(0.02);
                    }

                    AddLoot(LootPack.FilthyRich);
                    AddLoot(LootPack.Rich, 2);
                }
            }
        }

        public class BunnyHole : Item
        {
            public BunnyHole()
                : base(0x913)
            {
                Movable = false;
                Hue = 1;
                Name = "a mysterious rabbit hole";

                Timer.DelayCall(TimeSpan.FromSeconds(40.0), new TimerCallback(Delete));
            }

            public BunnyHole(Serial serial)
                : base(serial)
            {
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

                Delete();
            }
        }

        public virtual void DelayBeginTunnel()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(3.0), new TimerCallback(BeginTunnel));
        }

        public virtual void BeginTunnel()
        {
            if (Deleted)
                return;

            new BunnyHole().MoveToWorld(Location, Map);

            Frozen = true;
            Say("* The bunny begins to dig a tunnel back to its underground lair *");
            PlaySound(0x247);

            Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(Delete));
        }

        public override int Meat { get { return 1; } }
        public override int Hides { get { return 1; } }

        public VorpalBunny(Serial serial)
            : base(serial)
        {
        }


        public override int GetAttackSound()
        {
            return 0xC9;
        }

        public override int GetHurtSound()
        {
            return 0xCA;
        }

        public override int GetDeathSound()
        {
            return 0xCB;
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

            DelayBeginTunnel();
        }
    }
}