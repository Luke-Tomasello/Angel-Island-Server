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

using Server.Items;
using Server.Targeting;
using System;

namespace Server.Commands
{
    public class SplashGold
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("SplashGold", AccessLevel.Seer, new CommandEventHandler(SplashGold_OnCommand));
        }

        [Usage("SplashGold [amount]")]
        [Description("Creates champion-type gold splash.")]
        private static void SplashGold_OnCommand(CommandEventArgs e)
        {
            int amount = 1;
            if (e.Length >= 1)
                amount = e.GetInt32(0);

            if (amount < 100)
            {
                e.Mobile.SendMessage("Splash at least 100 gold.");
            }
            else if (amount > 2800000)
            {
                e.Mobile.SendMessage("Amount exceeded.  Use an amount less than 2800000.");
            }
            else
            {
                e.Mobile.Target = new SplashTarget(amount > 0 ? amount : 1);
                e.Mobile.SendMessage("Where do you want the center of the gold splash to be?");
            }
        }

        private class SplashTarget : Target
        {
            private int m_Amount;
            private string m_Location;

            public SplashTarget(int amount)
                : base(15, true, TargetFlags.None)
            {
                m_Amount = amount;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                DoSplashGold(from.Map, targ, m_Amount);
                CommandLogging.WriteLine(from, "{0} {1} splashed {2} gold at {3} )", from.AccessLevel, CommandLogging.Format(from), m_Amount, m_Location);
            }
        }

        public static void DoSplashGold(Map map, object targ, int amount)
        {
            IPoint3D center = targ as IPoint3D;
            if (center != null)
            {
                Point3D p = new Point3D(center);

                if (map != null)
                {
                    for (int x = -3; x <= 3; ++x)
                    {
                        for (int y = -3; y <= 3; ++y)
                        {
                            double dist = Math.Sqrt(x * x + y * y);

                            if (dist <= 12)
                                new GoodiesTimer(map, p.X + x, p.Y + y, amount).Start();
                        }
                    }
                }
            }
        }
        private class GoodiesTimer : Timer
        {
            private Map m_Map;
            private int m_X, m_Y;
            private int m_Amount;

            public GoodiesTimer(Map map, int x, int y, int amount)
                : base(TimeSpan.FromSeconds(Utility.RandomDouble() * 10.0))
            {
                m_Amount = amount;
                m_Map = map;
                m_X = x;
                m_Y = y;
            }

            protected override void OnTick()
            {
                int z = m_Map.GetAverageZ(m_X, m_Y);
                bool canFit = Utility.CanFit(m_Map, m_X, m_Y, z, 6, Utility.CanFitFlags.requireSurface);

                for (int i = -3; !canFit && i <= 3; ++i)
                {
                    canFit = Utility.CanFit(m_Map, m_X, m_Y, z + i, 6, Utility.CanFitFlags.requireSurface);

                    if (canFit)
                        z += i;
                }

                if (!canFit)
                    return;

                Gold g = new Gold(m_Amount / 49 + Utility.Random(m_Amount / 10000, m_Amount / 1000 - m_Amount / 10000));

                g.MoveToWorld(new Point3D(m_X, m_Y, z), m_Map);

                if (0.5 >= Utility.RandomDouble())
                {
                    switch (Utility.Random(3))
                    {
                        case 0: // Fire column
                            {
                                Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                                Effects.PlaySound(g, g.Map, 0x208);

                                break;
                            }
                        case 1: // Explosion
                            {
                                Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x36BD, 20, 10, 5044);
                                Effects.PlaySound(g, g.Map, 0x307);

                                break;
                            }
                        case 2: // Ball of fire
                            {
                                Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x36FE, 10, 10, 5052);

                                break;
                            }
                    }
                }
            }
        }
    }
}