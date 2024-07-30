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

/* Mobiles/Monsters/Reptile/Magic/Leviathan.cs
 * CHANGELOG:
 *  7/7/2023, Adam
 *      Update loot to match 
 *      https://uo.stratics.com/database/view.php?db_content=hunters&id=1396
 *  7/7/23, Yoar
 *      Initial version
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a leviathan corpse")]
    public class Leviathan : BaseCreature
    {
        private Mobile m_Fisher;

        public Mobile Fisher
        {
            get { return m_Fisher; }
            set { m_Fisher = value; }
        }

        [Constructable]
        public Leviathan() : this(null)
        {
        }

        [Constructable]
        public Leviathan(Mobile fisher) : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            m_Fisher = fisher;

            // May not be OSI accurate; mostly copied from krakens
            Name = "a leviathan";
            Body = 77;
            BaseSoundID = 353;

            Hue = 0x6A7 /*bright white, eww 0x481*/;

            SetStr(1000);
            SetDex(501, 520);
            SetInt(501, 515);

            SetHits(1500);

            SetDamage(25, 33);

            SetSkill(SkillName.EvalInt, 97.6, 107.5);
            SetSkill(SkillName.Magery, 97.6, 107.5);
            SetSkill(SkillName.MagicResist, 97.6, 107.5);
            SetSkill(SkillName.Meditation, 97.6, 107.5);
            SetSkill(SkillName.Tactics, 97.6, 107.5);
            SetSkill(SkillName.Wrestling, 97.6, 107.5);

            Fame = 24000;
            Karma = -24000;

            VirtualArmor = 50;

            CanSwim = true;
            CantWalkLand = true;
        }

        public override bool HasBreath { get { return true; } }
        public override int BreathPhysicalDamage { get { return 70; } } // TODO: Verify damage type
        public override int BreathColdDamage { get { return 30; } }
        public override int BreathFireDamage { get { return 0; } }
        public override int BreathEffectHue { get { return 0x1ED; } }
        public override double BreathDamageScalar { get { return 0.05; } }
        public override double BreathMinDelay { get { return 5.0; } }
        public override double BreathMaxDelay { get { return 7.5; } }

        public override double TreasureMapChance { get { return 0.25; } }
        public override int TreasureMapLevel { get { return 5; } }

        // https://uo.stratics.com/database/view.php?db_content=hunters&id=1396
        // 2600 - 3000 Gold. Magic Items
        //  Special: 2 Ropes, Message in a Bottle
        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(2600, 3000);

                PackItem(new MessageInABottle());

                Rope rope = new Rope();
                rope.ItemID = 0x14F8;
                PackItem(rope);

                rope = new Rope();
                rope.ItemID = 0x14FA;
                PackItem(rope);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {
                    if (Spawning)
                    {
                        PackGold(2600, 3000);
                    }
                    else
                    {
                        PackItem(new MessageInABottle());

                        Rope rope = new Rope();
                        rope.ItemID = 0x14F8;
                        PackItem(rope);

                        rope = new Rope();
                        rope.ItemID = 0x14FA;
                        PackItem(rope);
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackGold(2600, 3000);
                    }
                    else
                    {
                        PackItem(new MessageInABottle());

                        Rope rope = new Rope();
                        rope.ItemID = 0x14F8;
                        PackItem(rope);

                        rope = new Rope();
                        rope.ItemID = 0x14FA;
                        PackItem(rope);
                    }
                }
            }
        }

        public Leviathan(Serial serial) : base(serial)
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
        }

        private static readonly Type[] m_Rares = new Type[]
            {
                typeof(AdmiralsHeartyRum),
                typeof(Anchor),
                typeof(Hook),
                typeof(Pulley),
                typeof(SeahorseStatuette),
                typeof(TallCandelabra),
            };

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            // 10/14/23, Yoar: Taking out leviathans is a huge ordeal
            // Let's always give an artifacts
            if (c != null /*&& Utility.Random(4) == 0*/)
            {
                Item rare = Loot.Construct(m_Rares);

                if (rare != null)
                {
                    rare.LootType = LootType.Rare;
                    c.DropItem(rare);
                }
            }

            m_Fisher = null;
        }
    }
}