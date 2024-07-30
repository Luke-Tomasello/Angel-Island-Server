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

/* Scripts\Engines\Spawner\Commands\Commands.cs
 * Changelog
 *  6/24/2023, Adam 
 *      Created
 */
using Server.Targeting;
using System;

namespace Server.Mobiles
{
    public partial class Spawner : Item
    {
        [Usage("AdjustSpawn <percentage amount>")]
        [Description("Increase overall spawn of this spawner by N%.")]
        public static void AdjustSpawn_OnCommand(CommandEventArgs e)
        {
            try
            {
                double.Parse(e.ArgString);
                e.Mobile.SendMessage("Target the spawner you wish to adjust.");
                e.Mobile.Target = new SpawnerTarget(e);
            }
            catch
            {
                e.Mobile.SendMessage("Usage: AdjustSpawn <double amount>");
            }
        }

        public class SpawnerTarget : Target
        {
            CommandEventArgs m_args;
            double m_amount;
            public SpawnerTarget(CommandEventArgs e)
                : base(12, false, TargetFlags.None)
            {
                m_args = e;
                m_amount = double.Parse(e.ArgString);
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Spawner spawner)
                {
                    try
                    {
                        int[] counts = Utility.IntParser(spawner.Counts);
                        if (counts == null || counts.Length == 0 || spawner.ObjectNamesRaw == null || spawner.ObjectNamesRaw.Count == 0)
                        {
                            from.SendMessage("Nothing to do.");
                            return;
                        }

                        double[] new_counts = new double[counts.Length];
                        for (int ix = 0; ix < counts.Length; ix++)
                        {
                            if (m_amount < 1.0)
                                new_counts[ix] = Math.Ceiling(counts[ix] * m_amount);
                            else
                                new_counts[ix] = Math.Floor(counts[ix] * m_amount);
                        }

                        for (int ix = 0; ix < new_counts.Length; ix++)
                            counts[ix] = (int)new_counts[ix];

                        var result = string.Join(",", counts);

                        spawner.Counts = result;
                        spawner.RemoveObjects();
                        spawner.Respawn();
                    }
                    catch
                    {
                        from.SendMessage("Error processing spawner.");
                    }
                }
                else
                    from.SendMessage("That is not a spawner.");
            }
        }
    }
}