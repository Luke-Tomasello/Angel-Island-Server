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

/* Scripts/Misc/Poison.cs
 * ChangeLog
 *  06/03/06, Kit
 *		Added check for posion damage reduction for creatures via CheckSpellImmunity call
 *  11/07/05, Kit
 *		Restored Poison tick rates to former values.
 *	10/16/05, Pix
 *		Poison tick tweaks.
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;

namespace Server
{
    public class PoisonImpl : Poison
    {

        [CallPriority(10)]
        public static void Configure()
        {
            // Things to note:
            // Cure's cast time is: 0.75 seconds
            // GCure's cast time is: 1.25 secons

            //Updated poison tick rates
            //                          name    level  min  max  percent  delay  interval  count messageInterval
            //Register( new PoisonImpl( "Lesser",		0,   4,  26,   2.500,   1.5,      2.5,    10,    2 ) );
            //Register( new PoisonImpl( "Regular",	1,   5,  26,   3.125,   2.0,      2.0,    10,    2 ) );
            //Register( new PoisonImpl( "Greater",	2,   6,  26,   6.250,   2.0,      3.5,    10,    2 ) );
            //Register( new PoisonImpl( "Deadly",		3,   7,  26,  12.500,   2.0,      5.0,    10,    2 ) );
            //Register( new PoisonImpl( "Lethal",		4,   9,  26,  25.000,   3.0,      5.0,    10,    2 ) );

            //                          name    level  min  max  percent  delay  interval  count messageInterval
            Register(new PoisonImpl("Lesser", 0, 4, 26, 2.500, 3.5, 3.0, 10, 2));
            Register(new PoisonImpl("Regular", 1, 5, 26, 3.125, 3.5, 3.0, 10, 2));
            Register(new PoisonImpl("Greater", 2, 6, 26, 6.250, 3.5, 3.0, 10, 2));
            Register(new PoisonImpl("Deadly", 3, 7, 26, 12.500, 3.5, 4.0, 10, 2));
            Register(new PoisonImpl("Lethal", 4, 9, 26, 25.000, 3.5, 5.0, 10, 2));
        }

        public static Poison IncreaseLevel(Poison oldPoison)
        {
            Poison newPoison = (oldPoison == null ? null : GetPoison(oldPoison.Level + 1));

            return (newPoison == null ? oldPoison : newPoison);
        }

        // Info
        private string m_Name;
        private int m_Level;

        // Damage
        private int m_Minimum, m_Maximum;
        private double m_Scalar;

        // Timers
        private TimeSpan m_Delay;
        private TimeSpan m_Interval;
        private int m_Count, m_MessageInterval;

        public PoisonImpl(string name, int level, int min, int max, double percent, double delay, double interval, int count, int messageInterval)
        {
            m_Name = name;
            m_Level = level;
            m_Minimum = min;
            m_Maximum = max;
            m_Scalar = percent * 0.01;
            m_Delay = TimeSpan.FromSeconds(delay);
            m_Interval = TimeSpan.FromSeconds(interval);
            m_Count = count;
            m_MessageInterval = messageInterval;
        }

        public override string Name { get { return m_Name; } }
        public override int Level { get { return m_Level; } }

        private class PoisonTimer : Timer
        {
            private PoisonImpl m_Poison;
            private Mobile m_Mobile;
            private int m_LastDamage;
            private int m_Index;

            public PoisonTimer(Mobile m, PoisonImpl p)
                : base(p.m_Delay, p.m_Interval)
            {
                m_Mobile = m;
                m_Poison = p;
            }

            protected override void OnTick()
            {
                if ((m_Poison.Level < 3 && OrangePetals.UnderEffect(m_Mobile)) ||
                    (m_Poison.Level < 5 && NecromancersCauldron.GetEffect(m_Mobile) == NecromancersCauldron.EffectType.PoisonResistance))
                {
                    if (m_Mobile.CurePoison(m_Mobile))
                    {
                        m_Mobile.LocalOverheadMessage(MessageType.Emote, 0x3F, true,
                            "* You feel yourself resisting the effects of the poison *");

                        m_Mobile.NonlocalOverheadMessage(MessageType.Emote, 0x3F, true,
                            String.Format("* {0} seems resistant to the poison *", m_Mobile.Name));

                        Stop();
                        return;
                    }
                }

                if (m_Index++ == m_Poison.m_Count)
                {
                    m_Mobile.SendLocalizedMessage(502136); // The poison seems to have worn off.
                    m_Mobile.Poison = null;

                    Stop();
                    return;
                }

                int damage;

                if (m_LastDamage != 0 && Utility.RandomBool())
                {
                    damage = m_LastDamage;
                }
                else
                {
                    damage = 1 + (int)(m_Mobile.Hits * m_Poison.m_Scalar);

                    if (damage < m_Poison.m_Minimum)
                    {
                        damage = m_Poison.m_Minimum;
                    }
                    else if (damage > m_Poison.m_Maximum)
                    {
                        damage = m_Poison.m_Maximum;
                    }

                    m_LastDamage = damage;
                }

                double Moddamage = damage;

                if (m_Mobile is BaseCreature)
                {
                    ((BaseCreature)m_Mobile).CheckSpellImmunity(Server.Spells.SpellDamageType.Posion, (double)damage, out Moddamage);
                    //Console.WriteLine("Old Damage {0}, new Damage {1}",damage,Moddamage);
                }

                AOS.Damage(m_Mobile, (int)Moddamage, 0, 0, 0, 100, 0, this);

                if ((m_Index % m_Poison.m_MessageInterval) == 0)
                {
                    m_Mobile.OnPoisoned(m_Mobile, m_Poison, m_Poison);
                }
            }
        }

        public override Timer ConstructTimer(Mobile m)
        {
            return new PoisonTimer(m, this);
        }
    }
}