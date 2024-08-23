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

/* Scripts/Engines/NavStar/Navstar.cs
 * CHANGELOG
 * 3/26/2024, Adam,
 *    Complete rewrite. of the NavStar system. Smaller (works,) and easier to understand
 * 9/12/21, Adam:
 *  Replace the old slow and expensive map.GetItemsInRange(m.Location, 256) logic with a very fast registry
 *  The registry can be found in NavBeacon.cs and the beacons are registered during Deserialize() and
 *  the NavBeacon gump where it is configured.
 *   If we want to support other maps, this routine will need to be expanded as such.
 * 12/31/05, Kit
 *		Bug fix with random beacon selection.
 * 12/05/05, Kit
 *		Updated NavStar to set home area of creature to location of beacon returned.
 * 11/30/05, Kit
 *		Updated due to changes to NavBeacon, do not add beacons of inactive or -1 weight to beacon list,
 *		if multiple beacons same weight choose random beacon of said weight.
 * 11/18/05, Kit
 * 	Initial Creation
 */
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Engines
{
    public class NavStar
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }

        private static Dictionary<Mobile, Queue<NavBeacon>> NavContext = new();
        public static bool AnyBeacons(Mobile m, string nav)
        {
            return NavBeacon.Registry.ContainsKey(nav);
        }
        public static void CreateRequest(Mobile m, string nav)
        {
            // record defrag is done once per CreateRequest(). Queue defrag is done in each ProcessRequest()
            Defrag();

            if (!NavContext.ContainsKey(m) && NavBeacon.Registry.ContainsKey(nav))
            {
                NavContext.Add(m, new Queue<NavBeacon>());                                          // queue of beacons
                List<NavBeacon> list = NavBeacon.Registry[nav].ToList();                            // get the beacons
                Utility.Shuffle(list);                                                              // we want different ordering per mob
                list = list.GroupBy(elem => elem.Ring).Select(group => group.First()).ToList();     // one beacon from each ring (unordered)
                list = list.OrderBy(o => o.Ring).ToList();                                          // order the list, low to high

                foreach (var beacon in list)                                                        // only one of each Ring, last one is the goal
                {
                    NavContext[m].Enqueue(beacon);                                                  // add them to the queue
                    beacon.LinkCount++;                                                             // for debugging so we can look at distribution
                }
            }
            else
                System.Diagnostics.Debug.Assert(NavBeacon.Registry.ContainsKey(nav));
        }
        public static bool UnStuck(Mobile m)
        {
            if (NavContext.ContainsKey(m) && NavContext[m].Count > 0)
            {
                ProcessRequest(m);
                return true;
            }
            return false;
        }
        public static void RemoveRequest(Mobile m)
        {
            NavContext.Remove(m);
        }
        private static void Defrag()
        {   // remove records when the mobile is deleted or the queue is empty
            // defrag of the queue is handled in ProcessRequest()
            Dictionary<Mobile, Queue<NavBeacon>> temp = new();
            temp = NavContext.Where(kvp => kvp.Key.Deleted || kvp.Value.Count == 0)
                                 .ToDictionary(pair => pair.Key,
                                               pair => pair.Value);
            ;
            foreach (var kvp in temp)
                NavContext.Remove(kvp.Key);
            ;
        }
        public static NavBeacon ProcessRequest(Mobile m)
        {
            // defrag
            while (NavContext.ContainsKey(m) && NavContext[m].Count > 0 && NavContext[m].Peek().Deleted)
                NavContext[m].Dequeue();

            if (NavContext.ContainsKey(m) && NavContext[m].Count > 0)
            {
                NavBeacon nb = NavContext[m].Dequeue();
                ((BaseCreature)m).NavPoint = nb.Location;
                ((BaseCreature)m).Beacon = nb;
                return nb;
            }
            else
            {
                ((BaseCreature)m).NavDestination = null;
                ((BaseCreature)m).NavPoint = Point3D.Zero;
                ((BaseCreature)m).Beacon = null;
                return null;
            }
        }
        private const byte magic = 204;
        #region Serialization
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("NavStarContexts Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/NavStarContexts.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {

                    case 1: // private static Dictionary<Mobile, Queue<NavBeacon>> NavContext
                        {
                            int count = NavContext.Count;                   // number of records
                            writer.Write(count);
                            foreach (var kvp in NavContext)
                            {
                                writer.Write(kvp.Key);                      // write the mobile
                                writer.Write(kvp.Value.Count);              // number of points

                                foreach (var el in kvp.Value)
                                    writer.Write(el);

                                //while (kvp.Value.Count > 0)                 // and...
                                //writer.Write(kvp.Value.Dequeue());      // all the points

                                writer.Write(magic);                        // deserialization 'magic'
                            }

                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Saves/NavStarContexts.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        public static void Load()
        {
            if (File.Exists("Saves/NavStarContexts.bin"))
                Console.WriteLine("NavStar Contexts Loading...");
            else
            {
                Console.WriteLine("No NavStar Contexts to Load.");
                return;
            }
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/NavStarContexts.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {   // private static Dictionary<Mobile, Queue<NavBeacon>> NavContext

                            int record_count = reader.ReadInt();           // number of records
                            for (int ix = 0; ix < record_count; ix++)
                            {
                                Mobile m = reader.ReadMobile();             // the mobile
                                Queue<NavBeacon> temp = new();              // defrag buffer
                                int point_count = reader.ReadInt();
                                for (int jx = 0; jx < point_count; jx++)
                                {                                           // defrag buffer
                                    NavBeacon temp_beacon = (NavBeacon)reader.ReadItem();
                                    if (temp_beacon != null)
                                        temp.Enqueue(temp_beacon);
                                }
                                if (m != null && temp.Count > 0)            // if we have a mobile and at least some beacons
                                    NavContext.Add(m, temp);                // create the context 

                                byte magic_byte = reader.ReadByte();
                                System.Diagnostics.Debug.Assert(magic_byte == magic);
                            }

                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid NavStarContexts.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Saves/NavStarContexts.bin, using default values:", ConsoleColor.Red);
            }

            Defrag();
        }

        #endregion Serialization
    }
}