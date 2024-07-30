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

/* Scripts\Engines\ChampionSpawn\Champs\Harrower\Mobiles\HarrowerTentacles.cs
 * CHANGELOG
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 10 lines removed.
 *  9/26/04, Jade
 *      Decreased gold drop from (900, 1000) to (400, 650)
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using System;
using System.Collections;

namespace Server.Mobiles
{
    [CorpseName("lifeless tentacles")] // TODO: Corpse name?
    public class HarrowerTentacles : BaseCreature
    {
        private Mobile m_Harrower;

        private DrainTimer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Harrower
        {
            get
            {
                return m_Harrower;
            }
            set
            {
                m_Harrower = value;
            }
        }

        [Constructable]
        public HarrowerTentacles()
            : this(null)
        {
        }

        public HarrowerTentacles(Mobile harrower)
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            m_Harrower = harrower;

            Name = "tentacles of the harrower";
            Body = 129;
            BaseSoundID = 352;

            SetStr(901, 1000);
            SetDex(126, 140);
            SetInt(1001, 1200);

            SetHits(541, 600);

            SetDamage(13, 20);

            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.MagicResist, 120.1, 140.0);
            SetSkill(SkillName.Swords, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 60;

            m_Timer = new DrainTimer(this);
            m_Timer.Start();
        }

        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override bool DisallowAllMoves { get { return true; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }

        public HarrowerTentacles(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(400, 650);
                PackMagicEquipment(1, 3, 1.0, 1.0);
                PackMagicEquipment(1, 3, 0.75, 0.75);
                PackReg(25);
                PackReg(25);
            }
            else
            {
                AddLoot(LootPack.FilthyRich, 2);
                AddLoot(LootPack.MedScrolls, 3);
                AddLoot(LootPack.HighScrolls, 2);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Harrower);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Harrower = reader.ReadMobile();

                        m_Timer = new DrainTimer(this);
                        m_Timer.Start();

                        break;
                    }
            }
        }

        public override void OnAfterDelete()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;

            base.OnAfterDelete();
        }

        private class DrainTimer : Timer
        {
            private HarrowerTentacles m_Owner;

            public DrainTimer(HarrowerTentacles owner)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Owner = owner;
                Priority = TimerPriority.TwoFiftyMS;
            }

            private static ArrayList m_ToDrain = new ArrayList();

            protected override void OnTick()
            {
                if (m_Owner.Deleted)
                {
                    Stop();
                    return;
                }

                if (0.2 < Utility.RandomDouble())
                    return;

                IPooledEnumerable eable = m_Owner.GetMobilesInRange(8);
                foreach (Mobile m in eable)
                {
                    if (m != m_Owner && m.Player && m_Owner.CanBeHarmful(m))
                        m_ToDrain.Add(m);
                }
                eable.Free();

                foreach (Mobile m in m_ToDrain)
                {
                    m_Owner.DoHarmful(m);

                    m.FixedParticles(0x374A, 10, 15, 5013, 0x455, 0, EffectLayer.Waist);
                    m.PlaySound(0x231);

                    m.SendAsciiMessage("You feel the life drain out of you!");

                    m_Owner.Hits += 20;

                    if (m_Owner.Harrower != null)
                        m_Owner.Harrower.Hits += 20;

                    m.Damage(20, m_Owner, this);
                }

                m_ToDrain.Clear();
            }
        }
    }
}