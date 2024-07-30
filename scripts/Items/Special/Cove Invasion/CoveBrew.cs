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

/* Scripts\Items\Special\Cove Invasion\CoveBrew.cs
 * CHANGELOG
 *  4/10/2024, Adam
 *      Initial commit.
 */

using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class CoveBrew : Item
    {
        public override string DefaultName { get { return "cove brew"; } }

        [Constructable]
        public CoveBrew()
            : this(1)
        {
        }

        [Constructable]
        public CoveBrew(int amount)
            : base(0x99B)
        {
            Hue = 1165;
            Stackable = false;
            Amount = amount;
            Hue = Utility.RandomBool() ? 0x482 : 0x48C;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new CoveBrew(), amount);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            else if (UnderEffect(from))
                from.SendLocalizedMessage(502173); // You are already under a similar effect.
            else if (!BasePotion.HasFreeHand(from))
                from.SendLocalizedMessage(502172); // You must have a free hand to drink a potion.
            else
                Drink(from);
        }

        public void Drink(Mobile from)
        {
            BasePotion.PlayDrinkEffect(from);

            from.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
            from.PlaySound(0x1EC);

            BeginEffect(from, TimeSpan.FromMinutes(5.0));

            Consume();
        }

        private static readonly Dictionary<Mobile, Timer> m_Timers = new Dictionary<Mobile, Timer>();

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

        public static bool Slays(Mobile attacker, Mobile defender)
        {
            if (UnderEffect(attacker))
            {
                SlayerEntry entry = SlayerGroup.GetEntryByName(SlayerName.OrcSlaying);

                return (entry != null && entry.Slays(defender));
            }

            return false;
        }

        public static bool OppositionSuperSlays(Mobile attacker, Mobile defender)
        {
            if (UnderEffect(defender))
            {
                SlayerEntry entry = SlayerGroup.GetEntryByName(SlayerName.OrcSlaying);

                return (entry != null && entry.Group.Opposition.Super.Slays(attacker));
            }

            return false;
        }

        private class InternalTimer : Timer
        {
            private Mobile m_Mobile;

            public InternalTimer(Mobile m, TimeSpan delay)
                : base(delay)
            {
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                EndEffect(m_Mobile);
            }
        }

        public CoveBrew(Serial serial)
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