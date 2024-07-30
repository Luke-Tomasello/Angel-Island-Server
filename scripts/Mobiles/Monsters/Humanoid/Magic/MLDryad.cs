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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/MLDryad.cs
 * ChangeLog
 *  12/9/23, Yoar
 *		Merge from RunUO
 */

using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("a dryad's corpse")]
    public class MLDryad : BaseCreature
    {
        public override bool InitialInnocent { get { return true; } }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.FeyAndUndead; }
        }

        [Constructable]
        public MLDryad() : base(AIType.AI_Mage, FightMode.Evil | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a dryad";
            Body = 266;
            BaseSoundID = 0x57B;

            SetStr(132, 149);
            SetDex(152, 168);
            SetInt(251, 280);

            SetHits(304, 321);

            SetDamage(11, 20);

            SetSkill(SkillName.Meditation, 80.0, 90.0);
            SetSkill(SkillName.EvalInt, 70.0, 80.0);
            SetSkill(SkillName.Magery, 70.0, 80.0);
            SetSkill(SkillName.Anatomy, 0);
            SetSkill(SkillName.MagicResist, 100.0, 120.0);
            SetSkill(SkillName.Tactics, 70.0, 80.0);
            SetSkill(SkillName.Wrestling, 70.0, 80.0);

            Fame = 5000;
            Karma = 5000;

            VirtualArmor = 28; // Don't know what it should be

            // TODO: Peculiar Seed

            // TODO: Arcane Scroll
        }

        public override void GenerateLoot()
        {
            // TODO
        }

        public override int Meat { get { return 1; } }

        public override void OnThink()
        {
            base.OnThink();

            AreaPeace();
            Undress(Combatant);
        }

        #region Area Peace
        private DateTime m_NextPeace;

        private static readonly Dictionary<Mobile, Timer> m_Timers = new Dictionary<Mobile, Timer>();

        public void AreaPeace()
        {
            if (Combatant == null || Deleted || !Alive || m_NextPeace > DateTime.UtcNow || 0.1 < Utility.RandomDouble())
                return;

            TimeSpan duration = TimeSpan.FromSeconds(Utility.RandomMinMax(20, 40));

            foreach (Mobile m in GetMobilesInRange(RangePerception))
            {
                if (m.Player && !UnderEffect(m) && !m.Hidden && CanBeHarmful(m))
                {
                    BeginEffect(m, duration);

                    m.SendLocalizedMessage(1072065); // You gaze upon the dryad's beauty, and forget to continue battling!
                    m.FixedParticles(0x376A, 1, 20, 0x7F5, EffectLayer.Waist);
                    m.Combatant = null;
                }
            }

            m_NextPeace = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            PlaySound(0x1D3);
        }

        public static bool UnderEffect(Mobile m)
        {
            return m_Timers.ContainsKey(m);
        }

        public static void BeginEffect(Mobile m, TimeSpan duration)
        {
            EndEffect(m);

            (m_Timers[m] = new InternalTimer(m, duration)).Start();
        }

        public static void EndEffect(Mobile m)
        {
            Timer timer;

            if (m_Timers.TryGetValue(m, out timer))
            {
                timer.Stop();
                m_Timers.Remove(m);
            }
        }

        private class InternalTimer : Timer
        {
            private Mobile m_Mobile;

            public InternalTimer(Mobile m, TimeSpan duration)
                : base(duration)
            {
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                EndEffect(m_Mobile);
            }
        }
        #endregion

        #region Undress
        private DateTime m_NextUndress;

        public void Undress(Mobile target)
        {
            if (target == null || Deleted || !Alive || m_NextUndress > DateTime.UtcNow || 0.005 < Utility.RandomDouble())
                return;

            if (target.Player && !target.Female && !target.Hidden && CanBeHarmful(target))
            {
                UndressItem(target, Layer.OuterTorso);
                UndressItem(target, Layer.InnerTorso);
                UndressItem(target, Layer.MiddleTorso);
                UndressItem(target, Layer.Pants);
                UndressItem(target, Layer.Shirt);

                target.SendLocalizedMessage(1072197); // The dryad's beauty makes your blood race. Your clothing is too confining.
            }

            m_NextUndress = DateTime.UtcNow + TimeSpan.FromMinutes(1);
        }

        public void UndressItem(Mobile m, Layer layer)
        {
            Item item = m.FindItemOnLayer(layer);

            if (item != null && item.Movable)
                m.PlaceInBackpack(item);
        }
        #endregion

        public MLDryad(Serial serial) : base(serial)
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