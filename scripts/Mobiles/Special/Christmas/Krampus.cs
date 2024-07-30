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

/* Scripts\Mobiles\Special\Krampus.cs
 * ChangeLog
 *  12/13/23, Yoar
 *      Initial version.
 */

using Server.Items;
using Server.Network;
using Server.SkillHandlers;
using System;

namespace Server.Mobiles
{
    [CorpseName("a Krampus corpse")]
    public class Krampus : BaseCreature
    {
        [Constructable]
        public Krampus()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Krampus";
            Body = 259;

            SetStr(451, 500);
            SetDex(201, 250);
            SetInt(551, 600);

            SetHits(1200);

            SetDamage(13, 24);

            SetSkill(SkillName.EvalInt, 100.1, 110.0);
            SetSkill(SkillName.Magery, 100.1, 110.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 150.5, 200.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 75.1, 100.0);
            SetSkill(SkillName.Discordance, 120.0);

            Fame = 30000;
            Karma = -30000;

            VirtualArmor = 60;
        }

        public override int GetDeathSound()
        {
            return 0x57F;
        }

        public override int GetAttackSound()
        {
            return 0x580;
        }

        public override int GetIdleSound()
        {
            return 0x581;
        }

        public override int GetAngerSound()
        {
            return 0x582;
        }

        public override int GetHurtSound()
        {
            return 0x583;
        }

        public override Characteristics MyCharacteristics { get { return Characteristics.Run; } }
        public override bool CanRummageCorpses { get { return true; } }
        public override int TreasureMapLevel { get { return 5; } }

        public override void GenerateLoot()
        {
            // SP custom

            if (Spawning)
            {
                PackGold(2000, 2500);
                PackItem(typeof(NaughtyList), 1.00);
            }
            else
            {
                PackMagicEquipment(1, 3);
                PackMagicEquipment(2, 3, 0.33, 0.33);
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            ThrowSack(attacker);
        }

        public override void OnDamagedBySpell(Mobile from)
        {
            base.OnDamagedBySpell(from);

            ThrowSack(from);
        }

        public override void OnThink()
        {
            base.OnThink();

            Suppress(Combatant);
            Taunt();
        }

        #region Throw Sack

        public static TimeSpan SackDelay = TimeSpan.FromSeconds(12.0);

        private DateTime m_NextSack;

        private void ThrowSack(Mobile target)
        {
            if (target == null || target.Hidden || target.Frozen || !CanBeHarmful(target) || m_NextSack > DateTime.UtcNow)
                return;

            m_NextSack = DateTime.UtcNow + SackDelay;

            Effects.SendMovingParticles(this, target, 0x1045, 1, 0, false, false, 0, 0, 0, 0, 0, 0);

            TimeSpan delay = TimeSpan.FromSeconds(Utility.GetDistanceToSqrt(this.Location, target.Location) / 5);

            Timer.DelayCall(delay, OnSackThrown, target);

            m_NextSack = DateTime.UtcNow + SackDelay;
        }

        private void OnSackThrown(Mobile target)
        {
            KrampusSack sack = new KrampusSack();

            sack.MoveToWorld(target.Location, target.Map);
            sack.Caught = target;

            target.Frozen = true;
            target.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You were caught by Krampus!");
        }

        private class KrampusSack : Item, ICarvable, IScissorable
        {
            private Mobile m_Caught;
            private Timer m_Timer;

            public Mobile Caught
            {
                get { return m_Caught; }
                set { m_Caught = value; }
            }

            public KrampusSack()
                : base(0x1045)
            {
                Movable = false;

                m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(15.0), Delete);
            }

            public void Carve(Mobile from, Item item)
            {
                Release(from);
                Delete();
            }

            public bool Scissor(Mobile from, Scissors scissors)
            {
                Release(from);
                Delete();
                from.PlaySound(0x248);
                return true;
            }

            private void Release(Mobile by)
            {
                if (m_Caught != null)
                {
                    if (m_Caught == by)
                        m_Caught.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You free yourself from Krampus' sack!");
                    else
                        m_Caught.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You were released from Krampus' sack!");
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Caught != null)
                    m_Caught.Frozen = false;

                if (m_Timer != null)
                    m_Timer.Stop();
            }

            public KrampusSack(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.WriteEncodedInt(0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                reader.ReadEncodedInt();

                Delete();
            }
        }

        #endregion

        #region Suppress

        public static TimeSpan SuppressDelay = TimeSpan.FromSeconds(5.0);

        private DateTime m_NextSuppress;

        public void Suppress(Mobile target)
        {
            if (target == null || target.Hidden || !CanBeHarmful(target) || Discordance.Table.ContainsKey(target) || m_NextSuppress > DateTime.UtcNow || 0.1 < Utility.RandomDouble())
                return;

            if (!target.Hidden && CanBeHarmful(target))
            {
                PlaySound(0x58C);

                double diff;

                if (target is BaseCreature)
                    diff = BaseInstrument.GetCreatureDifficulty((BaseCreature)target);
                else
                    diff = 115.0;

                diff -= 20.0;

                if (CheckSkill(SkillName.Discordance, diff - 25.0, diff + 25.0, contextObj: new object[2]))
                    Discordance.ApplyDiscordance(this, target);
                else
                    target.SendLocalizedMessage(1072064); // You hear jarring music, but it fails to disrupt you.
            }

            m_NextSuppress = DateTime.UtcNow + SuppressDelay;
        }

        #endregion

        #region Taunts

        public static TimeSpan TauntDelay = TimeSpan.FromSeconds(24.0);

        private DateTime m_NextTaunt;

        private void Taunt()
        {
            if (100 * Hits / HitsMax < 15 || m_NextTaunt > DateTime.UtcNow || 0.05 < Utility.RandomDouble())
                return;

            if (Combatant != null)
                Say(RandomTaunt(m_CombatTaunts));
            else
                Say(RandomTaunt(m_WanderTaunts));

            m_NextTaunt = DateTime.UtcNow + TauntDelay;
        }

        private static readonly string[] m_WanderTaunts = new string[]
            {
                "Who has been unruly this year?",
                "Who has been particularly naughty this season?",
                "*sniff sniff* Do I smell naughty children?",
                "I am in search of children undeserving of a lump of coal..."
            };

        private static readonly string[] m_CombatTaunts = new string[]
            {
                "Your misdeeds have not gone unnoticed, child.",
                "Santa told me that you have been naughty this year!",
                "I have a stick with your name on it.",
                "I will catch all troublemakers in my sack!",
            };

        private static string RandomTaunt(string[] taunts)
        {
            if (taunts.Length == 0)
                return null;

            return taunts[Utility.Random(taunts.Length)];
        }

        #endregion

        public Krampus(Serial serial)
            : base(serial)
        {
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