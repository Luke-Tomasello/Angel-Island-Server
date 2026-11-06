/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\Spawner\Spawner.Utils.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using Server.Diagnostics;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        private static void ValidateFollowerSpec(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec))
                return;

            foreach (string choice in spec.Split('|'))
            {
                foreach (string followerName in choice.Split(':'))
                {
                    string name = followerName.Trim();

                    if (name.Length == 0)
                        continue;

                    if (name.Equals("null", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Type? type = ScriptCompiler.FindTypeByName(name);

                    if (type == null)
                        throw new ArgumentException($"Unknown follower type '{name}'.");

                    if (!typeof(BaseCreature).IsAssignableFrom(type))
                        throw new ArgumentException(
                            $"Follower '{name}' is not derived from BaseCreature.");

                    BaseCreature? bc = null;

                    try
                    {
                        bc = Activator.CreateInstance(type) as BaseCreature;

                        if (bc == null)
                            throw new ArgumentException(
                                $"Unable to construct follower '{name}'.");

                        if (!bc.Tamable)
                            throw new ArgumentException(
                                $"Follower '{name}' is not tamable.");
                    }
                    finally
                    {
                        bc?.Delete();
                    }
                }
            }
        }
        private List<BaseCreature> ComposeFollowerList()
        {
            List<BaseCreature> followers = new List<BaseCreature>();

            if (string.IsNullOrWhiteSpace(m_Followers))
                return followers;

            // Pick one alternation branch first.
            string[] choices = m_Followers.Split('|');

            string choice = choices[Utility.Random(choices.Length)].Trim();

            if (IsNullFollower(choice))
                return followers;

            // Then spawn all followers listed in that branch.
            foreach (string rawName in choice.Split(':'))
            {
                string name = rawName.Trim();

                if (IsNullFollower(name))
                    continue;

                BaseCreature? follower = ConstructFollower(name);

                if (follower != null)
                    followers.Add(follower);
            }

            return followers;
        }

        private static bool IsNullFollower(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                   || value.Equals("null", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("none", StringComparison.OrdinalIgnoreCase);
        }

        private static BaseCreature? ConstructFollower(string name)
        {
            Type? type = ScriptCompiler.FindTypeByName(name);

            if (type == null || !typeof(BaseCreature).IsAssignableFrom(type))
                return null; // should already have been caught by setter validation

            return Activator.CreateInstance(type) as BaseCreature;
        }
        public static void Check()
        {
            if (Spawner.Instances.Count > 0)
            {
                Console.WriteLine("Spawner maintenance running on {0} spawners...", Instances.Count.ToString());
                // Look for broken template spawners
                int bcount = Commands.FindSpawner.LogBrokenTemplateSpawners();
                if (bcount > 0)
                {
                    Utility.Monitor.WriteLine("Warning: {0} Broken Template Spawners detected. Please see: {1}", ConsoleColor.Red, bcount, "FindBrokenTemplateSpawners.log");
                }
                // now see if there are any spawners that are trying to spawn something that doesn't exist
                LogHelper logger = new LogHelper("badspawn.log", false);
                int kcount = 0;
                foreach (Spawner sx in Spawner.Instances)
                {
                    if (sx == null) continue;
                    if (sx.ObjectNames == null) continue;
                    if (sx.Running == false) continue;

                    foreach (string typeName in sx.ObjectNames)
                    {
                        if (SpawnerType.GetType(typeName) == null)
                        {
                            kcount++;
                            logger.Log(LogType.Text, string.Format("Spawner at location {0}, on map {1}, was trying to spawn {2} (which is not a valid type)", sx.Location, sx.Map, typeName));
                        }
                    }
                }
                logger.Finish();

                if (kcount > 0)
                {
                    Utility.Monitor.WriteLine("Warning: {0} Bad Spawners detected. Please see: {1}", ConsoleColor.Red, kcount, "badspawn.log");
                }

            }
        }
    }
}
