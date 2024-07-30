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

/* scripts\Engines\Apiculture\BeeSwarm.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Engines.Apiculture
{
    public static class BeeSwarm
    {
        private static readonly Dictionary<Mobile, Timer> m_Registry = new Dictionary<Mobile, Timer>();

        public static Dictionary<Mobile, Timer> Registry { get { return m_Registry; } }

        public static void BeginEffect(Mobile m)
        {
            EndEffect(m, false);

            (m_Registry[m] = new EffectTimer(m)).Start();
        }

        public static void EndEffect(Mobile m, bool message)
        {
            Timer timer;

            if (m_Registry.TryGetValue(m, out timer))
            {
                if (message)
                    m.PublicOverheadMessage(MessageType.Emote, m.SpeechHue, true, "* The open flame begins to scatter the swarm of bees *");

                timer.Stop();
                m_Registry.Remove(m);
            }
        }

        public static bool UnderEffect(Mobile m)
        {
            return m_Registry.ContainsKey(m);
        }

        private static void DoEffect(Mobile m, int count)
        {
            if (!m.Alive)
            {
                EndEffect(m, false);
                return;
            }

            Torch torch = m.FindItemOnLayer(Layer.TwoHanded) as Torch;

            if (torch != null && torch.Burning)
            {
                EndEffect(m, true);
                return;
            }

            if ((count % 4) == 0)
            {
                if (count > 0 && Utility.RandomDouble() < 0.05 * count)
                {
                    EndEffect(m, false);
                    return;
                }

                m.LocalOverheadMessage(MessageType.Emote, m.SpeechHue, true, "* The swarm of bees bites and stings your flesh! *");
                m.NonlocalOverheadMessage(MessageType.Emote, m.SpeechHue, true, String.Format("* {0} is stung by a swarm of bees *", m.Name));
            }

            m.FixedParticles(0x91C, 10, 180, 9539, EffectLayer.Waist);
            m.PlaySound(0x00E);
            m.PlaySound(0x1BC);

            m.Damage(Utility.RandomMinMax(3, 6), null, null);

            if (!m.Alive)
                EndEffect(m, false);
        }

        private class EffectTimer : Timer
        {
            private Mobile m_From;
            private int m_Tick;

            public EffectTimer(Mobile from)
                : base(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(7.0))
            {
                m_From = from;
            }

            protected override void OnTick()
            {
                DoEffect(m_From, m_Tick++);
            }
        }
    }
}