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

/* Scripts\Engines\CacheFactory\CacheFactory.cs
 * CHANGELOG
 *  8/30/22, Adam
 *      spelling
 *  6/18/22, Adam (UpdateQuickTables)
 *      Don't bother UpdateQuickTables if we are Core.RuleSets.LoginServerRules() (waste of time)
 *  6/15/22, Adam 
 *      Don't bother loading these databases if we are Core.RuleSets.LoginServerRules() (waste of memory)
 *  2/8/22, Adam (InitializeSpawners)
 *      Filter spawners on: null, Running, and Map.Felucca
 *  12/21/21, Adam
 *      Add a Regions quick table.
 *      Because Angel Island renamed several regions, like Ocllo ==> Island Siege, 
 *      we load all Angel Island regions and classic UO regions to avoid player confusion.
 *  12/16/21, Adam
 *      First time checkin.
 *      The Cache Factory creates and manages various caches or Quick Tables.
 *      These quick tables are used heavily by the Intelligent Dialog system
 */


using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace Server.Engines
{
    public static partial class CacheFactory
    {
        private static Dictionary<string, List<Point3D>> m_placesQuickTable = new Dictionary<string, List<Point3D>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<Point3D>> m_vendorsQuickTable = new Dictionary<string, List<Point3D>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<Mobile, Point3D> m_mobilesQuickTable = new Dictionary<Mobile, Point3D>();
        private static Dictionary<Mobile, Point3D> m_playersQuickTable = new Dictionary<Mobile, Point3D>();
        private static Dictionary<string, List<Spawner>> m_spawnedObjectsQuickTable = new Dictionary<string, List<Spawner>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<Spawner>> m_spawnedItemsQuickTable = new Dictionary<string, List<Spawner>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<Spawner>> m_spawnedMobilesQuickTable = new Dictionary<string, List<Spawner>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Point3D> m_moongatesQuickTable = new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Point3D> m_docksQuickTable = new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<Region>> m_regionQuickTable = new Dictionary<string, List<Region>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, List<Point3D>> m_inventoryQuickTable = new Dictionary<string, List<Point3D>>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<Serial, List<string>> m_vendorInventoryIndex = new Dictionary<Serial, List<string>>();
        private static Dictionary<string, Type> m_OldNameToTypeQuickTable = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        private static List<int> m_signIds = new List<int>();
        // ======================================================================= //
        public static Dictionary<string, List<Point3D>> PlacesQuickTable { get { return m_placesQuickTable; } }
        public static Dictionary<string, List<Point3D>> VendorsQuickTable { get { return m_vendorsQuickTable; } }
        public static Dictionary<Mobile, Point3D> MobilesQuickTable { get { return m_mobilesQuickTable; } }
        public static Dictionary<Mobile, Point3D> PlayersQuickTable { get { return m_playersQuickTable; } }
        public static Dictionary<string, List<Spawner>> SpawnedObjectsQuickTable { get { return m_spawnedObjectsQuickTable; } }
        public static Dictionary<string, List<Spawner>> SpawnedItemsQuickTable { get { return m_spawnedItemsQuickTable; } }
        public static Dictionary<string, List<Spawner>> SpawnedMobilesQuickTable { get { return m_spawnedMobilesQuickTable; } }
        public static Dictionary<string, Point3D> MoongatesQuickTable { get { return m_moongatesQuickTable; } }
        public static Dictionary<string, Point3D> DocksQuickTable { get { return m_docksQuickTable; } }
        public static Dictionary<string, Type> OldNameToTypeQuickTable { get { return m_OldNameToTypeQuickTable; } }
        public static Dictionary<string, List<Region>> RegionQuickTable { get { return m_regionQuickTable; } }
        public static Dictionary<string, List<Point3D>> InventoryQuickTable { get { return m_inventoryQuickTable; } }
        public static Dictionary<string, Point3D>.KeyCollection TownsList { get { return m_moongatesQuickTable.Keys; } }
        public static Dictionary<string, List<Point3D>>.KeyCollection PlacesList { get { return m_placesQuickTable.Keys; } }
        public static Dictionary<string, List<Point3D>>.KeyCollection InventoryList { get { return m_inventoryQuickTable.Keys; } }
        public static Dictionary<string, List<Point3D>>.KeyCollection VendorTitleList { get { return m_vendorsQuickTable.Keys; } }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
        }

        public static void OnLoad()
        {
            try
            {
                if (!Core.RuleSets.LoginServerRules())
                {
                    InitializePlaces();
                    InitializeVendors();
                    InitializeSpawners();
                    InitializeMoongates();
                    InitializeRegions();
                    // We need to rethink this.
                    //  this routine causes all vendors to declare all their GenericBuyInfo resulting
                    //  in like 14K items getting created. They will be deleted on the next server restart
                    //  but still...
                    //  Note: in normal operation, individual vendors only declare their GenericBuyInfo when
                    //  a player says "vendor buy"
                    InitializeInventories();    // we cannot comment this out or IDS will fail
                    InitializeOldNames();
                    FixUpPlaces();
                }
            }
            catch (Exception ex)
            {
                Utility.Monitor.WriteLine(ex.Message, ConsoleColor.Red);
                Diagnostics.LogHelper.LogException(ex);
            }
            finally
            {
            }
        }

        private static void FixUpPlaces()
        {   // some places, like "Oc'Nivelle" don't have legit points based on our 'sign' approach.
            //  There is one sign for "Oc'Nivelle", but it's in a dungeon.
            //  we'll give it some real points now
            if (PlacesQuickTable.ContainsKey("Oc'Nivelle"))
            {   // We'll use the center point of the rect(s) that defines the region.
                //List<object> Regions = Region.Regions;
                foreach (Region rx in Region.Regions)
                {
                    if (rx == null || string.IsNullOrEmpty(rx.Name)) continue;
                    if (rx.Name.ToLower() == "Oc'Nivelle".ToLower())
                    {
                        foreach (Rectangle3D area in rx.Coords)
                        {
                            Point3D center = new Point3D(area.Start.X + area.Width / 2, area.Start.Y + area.Height / 2, 0);
                            if (area.Contains(center))
                                PlacesQuickTable["Oc'Nivelle"].Add(center);
                        }
                    }

                }
            }
        }
        #region InitializeInventories
        private static void InitializeInventories()
        {
            Console.WriteLine("Initializing inventory quick table...");

            m_inventoryQuickTable.Clear();
            m_vendorInventoryIndex.Clear();

            Console.WriteLine("Inventory quick table initialized.");
        }

        public static void RegisterVendorInventory(BaseVendor vendor)
        {
            if (vendor == null || vendor.Deleted)
                return;

            if (!IsVendorReadyForRegistration(vendor))
            {
                QueueVendorRegistrationRetry(vendor);
                return;
            }

            RegisterVendorInventoryCore(vendor);
        }
        private static HashSet<Serial> m_pendingVendorRegistration = new HashSet<Serial>();
        private static void QueueVendorRegistrationRetry(BaseVendor vendor)
        {
            if (vendor == null || vendor.Deleted)
                return;

            Serial serial = vendor.Serial;

            // Avoid scheduling multiple retries
            if (m_pendingVendorRegistration.Contains(serial))
                return;

            m_pendingVendorRegistration.Add(serial);

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
            {
                m_pendingVendorRegistration.Remove(serial);

                if (vendor == null || vendor.Deleted)
                    return;

                // Try registration again
                RegisterVendorInventory(vendor);
            });
        }
        private static bool IsVendorReadyForRegistration(BaseVendor vendor)
        {
            return vendor != null
                   && !vendor.Deleted
                   && vendor.Map != null
                   && vendor.Map != Map.Internal
                   && !string.IsNullOrWhiteSpace(vendor.Name)
                   && vendor.Inventory != null;
        }
        public static void RegisterVendorInventoryCore(BaseVendor vendor)
        {
            if (vendor == null || vendor.Deleted)
                return;

            if (vendor.Map != Map.Felucca || vendor.Title == null)
                return;

            ArrayList infos = vendor.Inventory;

            if (infos == null || infos.Count == 0)
                return;

            // Make registration idempotent.
            UnregisterVendorInventory(vendor);

            List<string> registeredNames = new List<string>();
            Point3D loc = vendor.Location;

            foreach (SBInfo info in infos)
            {
                if (info == null || info.BuyInfo == null)
                    continue;

                foreach (GenericBuyInfo binfo in info.BuyInfo)
                {
                    if (binfo == null || binfo.Type == null)
                        continue;

                    // Exclude pooled resource placeholders / BBS-style zero-price entries.
                    if (ResourcePool.ResourcePool.IsPooledResource(binfo.Type, true) && binfo.Price == 0)
                        continue;

                    string? name = GetBuyInfoDisplayName(binfo);

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    List<Point3D>? points;

                    if (!m_inventoryQuickTable.TryGetValue(name, out points))
                    {
                        points = new List<Point3D>();
                        m_inventoryQuickTable[name] = points;
                    }

                    // Avoid duplicate locations for the same item name from the same vendor.
                    if (!points.Contains(loc))
                        points.Add(loc);

                    if (!registeredNames.Contains(name))
                        registeredNames.Add(name);
                }
            }

            if (registeredNames.Count > 0)
                m_vendorInventoryIndex[vendor.Serial] = registeredNames;
        }
        public static void UnregisterVendorInventory(BaseVendor vendor)
        {
            if (vendor == null)
                return;

            List<string>? names;

            if (!m_vendorInventoryIndex.TryGetValue(vendor.Serial, out names) || names == null || names.Count == 0)
                return;

            Point3D loc = vendor.Location;

            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                List<Point3D>? points;

                if (!m_inventoryQuickTable.TryGetValue(name, out points) || points == null)
                    continue;

                points.RemoveAll(p => p == loc);

                if (points.Count == 0)
                    m_inventoryQuickTable.Remove(name);
            }

            m_vendorInventoryIndex.Remove(vendor.Serial);
        }
        private static string? GetBuyInfoDisplayName(GenericBuyInfo binfo)
        {
            if (binfo == null)
                return null;

            string name = binfo.Name;

            // 1. Prefer an explicit meaningful string name.
            // If it is not just a numeric cliloc id, use it as-is.
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();

                int number;
                if (!int.TryParse(name, out number))
                    return name;
            }

            // 2. Prefer the buy type name over generic cliloc results like "deed".
            if (binfo.Type != null)
                return Utility.SplitCamelCase(binfo.Type.Name);

            // 3. Last resort: resolve numeric cliloc name if present.
            if (!string.IsNullOrWhiteSpace(name))
            {
                int number;
                if (int.TryParse(name, out number))
                {
                    string? clilocName;
                    if (Cliloc.Lookup.TryGetValue(number, out clilocName) && !string.IsNullOrWhiteSpace(clilocName))
                        return clilocName;
                }
            }

            return null;
        }
        #endregion InitializeInventories
        private static void InitializeOldNames()
        {
            Console.WriteLine("Generating OldNames quick table, please wait...");

            foreach (Item item in World.Items.Values)
            {
                /* After Yoar's patch
                if (m_OldNameToTypeQuickTable.ContainsKey(item.GetRawOldName()))
                    continue;
                else
                    m_OldNameToTypeQuickTable.Add(item.GetRawOldName(), item.GetType());
                */

                /* Before Yoar's patch
                 if (m_OldNameToTypeQuickTable.ContainsKey(item.OldName))
                    continue;
                else
                    m_OldNameToTypeQuickTable.Add(item.OldName, item.GetType());
                 */
                // new test implementation
                string old_name = item.GetRawOldName() != null ? item.GetRawOldName() : item.GetType().Name;
                if (m_OldNameToTypeQuickTable.ContainsKey(old_name))
                    continue;
                else
                    m_OldNameToTypeQuickTable.Add(old_name, item.GetType());
            }


            Console.WriteLine("OldNames quick table generation complete with {0} items registered.", m_OldNameToTypeQuickTable.Count);
        }
        private static void InitializeRegions()
        {
            Console.WriteLine("Generating Region quick table, please wait...");
            List<Region> regions = new List<Region>();
            string outputMessage = string.Empty;
            regions.AddRange(CreateCacheFromXML("Regions.xml", ref outputMessage, string.Empty));
            regions.AddRange(CreateCacheFromXML("RunUORegions.xml", ref outputMessage, string.Empty));
            foreach (Region region in regions)
            {
                if (string.IsNullOrEmpty(region.Name))
                    continue;
                if (m_regionQuickTable.ContainsKey(region.Name))
                {
                    if (!m_regionQuickTable[region.Name].Contains(region))
                        m_regionQuickTable[region.Name].Add(region);
                }
                else
                {
                    m_regionQuickTable.Add(region.Name, new List<Region> { region });
                }
            }

            // okay, now in case there are any differences from the regions on disk and our item-based regions
            // (region controllers) We load all the live regions as well
            foreach (Region region in Region.Regions)
            {
                if (string.IsNullOrEmpty(region.Name))
                    continue;
                if (m_regionQuickTable.ContainsKey(region.Name))
                {
                    if (!m_regionQuickTable[region.Name].Contains(region))
                        m_regionQuickTable[region.Name].Add(region);
                }
                else
                {
                    m_regionQuickTable.Add(region.Name, new List<Region> { region });
                }
            }
            Console.WriteLine("\nRegion quick table generation complete with {0} regions registered.", m_regionQuickTable.Count);
            return;
        }
        private static void InitializeMoongates()
        {
            Console.WriteLine("Generating Moongate quick table, please wait...");
            m_moongatesQuickTable.TryAdd("britain", new Point3D(1330, 1991, 0));
            m_moongatesQuickTable.TryAdd("jhelom", new Point3D(1494, 3767, 0));
            m_moongatesQuickTable.TryAdd("minoc", new Point3D(2694, 685, 0));
            m_moongatesQuickTable.TryAdd("trinsic", new Point3D(1823, 2943, 0));
            m_moongatesQuickTable.TryAdd("yew", new Point3D(761, 741, 0));
            m_moongatesQuickTable.TryAdd("skara brae", new Point3D(638, 2062, 0));
            m_moongatesQuickTable.TryAdd("moonglow", new Point3D(4459, 1276, 0));
            m_moongatesQuickTable.TryAdd("magincia", new Point3D(3554, 2132, 0));
            Console.WriteLine("Moongate quick table generation complete with {0} moongates registered.", m_moongatesQuickTable.Count);
            return;
        }
        private static void InitializeVendors()
        {
            Console.WriteLine("Generating Vendors quick table, please wait...");
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob == null || mob is RentedVendor)
                    continue;

                if (mob.Map != Map.Felucca)
                    continue;

                if ((mob is BaseVendor || mob is BaseEscortable) && mob.Title != null)
                {
                    if (m_vendorsQuickTable.ContainsKey(mob.Title))
                        m_vendorsQuickTable[mob.Title].Add(mob.Location);
                    else
                        m_vendorsQuickTable.Add(mob.Title, new List<Point3D> { mob.Location });
                }
            }
            Console.WriteLine("Vendor quick table generation complete with {0} vendors registered.", VendorsQuickTable.Count);
            return;
        }
        private static void InitializePlaces()
        {
            // Classic OSI locations - based on signs
            SignParser.Parse();

            // custom AI / or later OSI locations - based on signs.
            foreach (Item item in World.Items.Values)
            {
                if (m_signIds.Contains(item.ItemID))
                {
                    // if not house sign or static house sign...
                    if (item is HouseSign || item is StaticHouseSign || item is CustomRegionControl)
                        continue;

                    Map map = item.Map;
                    string name = item.Name;
                    int labelNumber = item.LabelNumber;
                    Point3D location = item.Location;

                    // LocalizedSign overrides LabelNumber
                    if (item is LocalizedSign)
                        labelNumber = (item as LocalizedSign).LabelNumber;

                    if (map == Map.Felucca)
                    {
                        // get the text from the sign (localized style) and see if we already know about it
                        if (name == null)
                        {
                            if (Server.Text.Cliloc.Lookup.ContainsKey(labelNumber))
                                name = Server.Text.Cliloc.Lookup[labelNumber];
                        }

                        // here we exclude the custom houses in the custom housing area
                        //Region rx = Region.Find(item.Location, item.Map);
                        //if (rx != null && rx.Name != null && rx.Name == "Restricted Area" && name.ToLower().Contains("house"))
                        //continue;

                        if (name != null)
                        {
                            if (m_placesQuickTable.ContainsKey(name))
                            {
                                if (!m_placesQuickTable[name].Contains(location))
                                    // add a location
                                    m_placesQuickTable[name].Add(location);
                            }
                            else
                                m_placesQuickTable.Add(name, new List<Point3D> { location });
                        }
                    }
                }
            }

            // locations based on Locations/felucca.xml
            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(Core.DataDirectory, "Locations", "felucca.xml"));
            XmlElement? root = doc["places"];
            foreach (XmlElement places in root.GetElementsByTagName("parent"))
            {
                string placeName = places.GetAttribute("name");
                //Console.WriteLine("place name: {0}", placeName);
                if (string.IsNullOrEmpty(placeName))
                    continue;

                // skip all the sub-parents
                if (places.FirstChild == null || string.IsNullOrEmpty(places.FirstChild.Name) || places.FirstChild.Name == "parent")
                    continue;

                // grab all the locations
                foreach (XmlElement child in places)
                {
                    //Console.WriteLine("location name: {0}", child.GetAttribute("name"));
                    //Console.WriteLine("location data: {0}", child.OuterXml);
                    // parse out the coords
                    string[] chunks = child.OuterXml.Split(new char[] { ' ' });
                    int X, Y, Z; X = Y = Z = 0;
                    foreach (string chunk in chunks)
                    {
                        if (chunk.StartsWith("x="))
                        {
                            string temp = chunk.Replace("x=", "").Replace("\"", "").Trim();
                            int.TryParse(temp, out X);
                        }
                        else if (chunk.StartsWith("y="))
                        {
                            string temp = chunk.Replace("y=", "").Replace("\"", "").Trim();
                            int.TryParse(temp, out Y);
                        }
                        else if (chunk.StartsWith("z="))
                        {
                            string temp = chunk.Replace("z=", "").Replace("\"", "").Trim();
                            int.TryParse(temp, out Z);
                        }
                    }
                    Point3D point = new Point3D(X, Y, Z);
                    if (m_placesQuickTable.ContainsKey(placeName))
                    {
                        if (!m_placesQuickTable[placeName].Contains(point))
                            // add a location
                            m_placesQuickTable[placeName].Add(point);
                    }
                    else
                        m_placesQuickTable.Add(placeName, new List<Point3D> { point });
                }
            }

            // let's setup the 'docks'
            Dictionary<string, Point3D> specificDocks = new Dictionary<string, Point3D>()
            {
                { "Buccaneers Den docks", new Point3D(2736, 2166, 0) },
                { "Jhelom docks", new Point3D(1492, 3696, -3) },
                { "Magincia docks", new Point3D(3675, 2259, 20) },
                { "Moonglow docks", new Point3D(4406, 1045, -2) },
                { "Nujel'm docks", new Point3D(3803, 1279, 5) },
                { "Ocllo docks", new Point3D(3650, 2653, 0) },
                { "Skara Brae East docks",new Point3D(716, 2233, -3) },
                { "Skara Brae West docks", new Point3D(639, 2236, -3) },
                { "Trinsic docks", new Point3D(2071, 2855, -3) },
                { "Vesper docks", new Point3D(3013, 828, -3) }
            };

            foreach (KeyValuePair<string, Point3D> kvp in specificDocks)
            {   // we'll add three entries, one for the specific dock, one for the generic dock, and on to a list exclusively of docks
                // first the generic docks
                string docks = "docks";
                if (m_placesQuickTable.ContainsKey(docks))
                {
                    if (!m_placesQuickTable[docks].Contains(kvp.Value))
                        // add a location
                        m_placesQuickTable[docks].Add(kvp.Value);
                }
                else
                    m_placesQuickTable.Add(docks, new List<Point3D> { kvp.Value });
                // okay, now for the specific docks
                docks = kvp.Key;
                if (m_placesQuickTable.ContainsKey(docks))
                {
                    if (!m_placesQuickTable[docks].Contains(kvp.Value))
                        // add a location
                        m_placesQuickTable[docks].Add(kvp.Value);
                }
                else
                    m_placesQuickTable.Add(docks, new List<Point3D> { kvp.Value });

                // finally, add these docks to a quicktable of docks
                //  We do this because we need to differentiate between docks and other 'in town' places since
                //  docks are often outside the town limits
                docks = kvp.Key;
                if (!m_docksQuickTable.ContainsKey(docks))
                    m_docksQuickTable.Add(docks, kvp.Value);

            }

            return;
        }
        public static int InitializeSpawners()
        {
            // empty the list in case we need to reload the entire set (SpawnerManagement SpawnerCapture.Load)
            m_spawnedObjectsQuickTable.Clear();
            m_spawnedItemsQuickTable.Clear();
            m_spawnedMobilesQuickTable.Clear();

            // let's begin
            Console.WriteLine("Generating Spawner quick table, please wait...");
            foreach (Spawner spawner in Spawner.Instances)
            {
                if (spawner == null || spawner.Running == false || spawner.Map != Map.Felucca)
                    continue;

                if (spawner.ObjectNames != null)
                    foreach (string sx in spawner.ObjectNames)
                    {   // our general table of all spawned objects
                        if (m_spawnedObjectsQuickTable.ContainsKey(sx.ToLower()))
                            m_spawnedObjectsQuickTable[sx.ToLower()].Add(spawner);
                        else
                            m_spawnedObjectsQuickTable.Add(sx.ToLower(), new List<Spawner> { spawner });

                        // now create two more tables, one for items and one for mobiles
                        Type? type = ScriptCompiler.FindTypeByName(sx);
                        if (type != null)
                        {
                            // if a mobile, cache here
                            if (typeof(Mobile).IsAssignableFrom(type))
                            {
                                if (m_spawnedMobilesQuickTable.ContainsKey(sx.ToLower()))
                                    m_spawnedMobilesQuickTable[sx.ToLower()].Add(spawner);
                                else
                                    m_spawnedMobilesQuickTable.Add(sx.ToLower(), new List<Spawner> { spawner });

                            }
                            // if an item, cache here
                            if (typeof(Item).IsAssignableFrom(type))
                            {
                                if (m_spawnedItemsQuickTable.ContainsKey(sx.ToLower()))
                                    m_spawnedItemsQuickTable[sx.ToLower()].Add(spawner);
                                else
                                    m_spawnedItemsQuickTable.Add(sx.ToLower(), new List<Spawner> { spawner });

                            }
                        }
                    }
            }
            Console.WriteLine("Spawner quick table generation complete with {0} Spawners registered.", m_spawnedObjectsQuickTable.Count);
            // how many did we get?
            return m_spawnedObjectsQuickTable.Count;
        }
        public enum QuickTableUpdate
        {
            None,
            Changed,
            Deleted
        }
        public static void UpdateQuickTables(Spawner spawner, QuickTableUpdate mode)
        {
            if (Core.RuleSets.LoginServerRules())
                return;

            //System.Console.WriteLine("Updating quick tables: ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            UpdateQuickTables(spawner, mode, m_spawnedObjectsQuickTable);
            UpdateQuickTables(spawner, mode, m_spawnedItemsQuickTable);
            UpdateQuickTables(spawner, mode, m_spawnedMobilesQuickTable);
            tc.End();
            //System.Console.WriteLine("Updated quick tables in {0}", tc.TimeTaken);
        }
        public static void UpdateQuickTables(Spawner spawner, QuickTableUpdate mode, Dictionary<string, List<Spawner>> quickTable)
        {
            if (spawner == null)
                return;

            // get a list of spawner objects that we can do StringComparer.OrdinalIgnoreCase
            List<string> spawnerObjects = spawner.ObjectNames.Cast<string>().ToList();

            if (mode == QuickTableUpdate.Deleted)
            {   // find the quick table record that holds this spawner
                List<string> recordsToDelete = new List<string>();
                foreach (KeyValuePair<string, List<Spawner>> kvp in quickTable)
                {   // if this record contains the deleted spawner, remove the spawner
                    if (kvp.Value.Contains(spawner))
                    {   // remove this spawner from the list of spawners
                        kvp.Value.Remove(spawner);
                        // if the spawner list is now empty, remove the table record
                        if (kvp.Value.Count == 0)
                            recordsToDelete.Add(kvp.Key);
                    }
                }

                // remove records that have no associated spawner lists
                foreach (string record in recordsToDelete)
                    quickTable.Remove(record);

            }
            else if (mode == QuickTableUpdate.Changed)
            {
                // do processing for when the record key exists
                List<string> recordsToDelete = new List<string>();
                foreach (KeyValuePair<string, List<Spawner>> kvp in quickTable)
                {
                    // process remove spawner when record key exists
                    if (kvp.Value.Contains(spawner))
                    {
                        // does this spawner contain the object associated with the record's key?
                        if (spawnerObjects.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                            // yes
                            continue;

                        // the spawner associated with this key no longer contains this object
                        kvp.Value.Remove(spawner);

                        // if this is the only spawner associated with the record, delete the record
                        if (kvp.Value.Count == 0)
                            recordsToDelete.Add(kvp.Key);
                    }
                    // process add spawner if the key exists
                    else if (spawnerObjects.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                    {   // Does this record contain this spawner?
                        if (kvp.Value.Contains(spawner))
                            continue;
                        kvp.Value.Add(spawner);
                    }
                }

                // process add record if the key does not exist
                foreach (string sx in spawnerObjects)
                    if (!quickTable.ContainsKey(sx.ToLower()))
                        quickTable.Add(sx, new List<Spawner> { spawner });


                // remove records that have no associated spawner lists
                foreach (string record in recordsToDelete)
                    quickTable.Remove(record);
            }
        }
        #region Sign Parser
        private static class SignParser
        {
            private class SignEntry
            {
                public string m_Text;
                public Point3D m_Location;
                public int m_ItemID;
                public int m_Map;

                public SignEntry(string text, Point3D pt, int itemID, int mapLoc)
                {
                    m_Text = text;
                    m_Location = pt;
                    m_ItemID = itemID;
                    m_Map = mapLoc;
                }
            }

            public static void Parse()
            {
                string cfg = Path.Combine(Core.DataDirectory, "signs.cfg");

                if (File.Exists(cfg))
                {
                    ArrayList list = new ArrayList();
                    Console.WriteLine("Generating Places quick table, please wait...");

                    using (StreamReader ip = new StreamReader(cfg))
                    {
                        string line;

                        while ((line = ip.ReadLine()) != null)
                        {
                            string[] split = line.Split(' ');

                            SignEntry e = new SignEntry(
                                line.Substring(split[0].Length + 1 + split[1].Length + 1 + split[2].Length + 1 + split[3].Length + 1 + split[4].Length + 1),
                                new Point3D(Utility.ToInt32(split[2]), Utility.ToInt32(split[3]), Utility.ToInt32(split[4])),
                                Utility.ToInt32(split[1]), Utility.ToInt32(split[0]));

                            list.Add(e);
                        }
                    }

                    Map[] brit = new Map[] { Map.Felucca, Map.Trammel };
                    Map[] fel = new Map[] { Map.Felucca };
                    Map[] tram = new Map[] { Map.Trammel };
                    Map[] ilsh = new Map[] { Map.Ilshenar };
                    Map[] malas = new Map[] { Map.Malas };

                    for (int i = 0; i < list.Count; ++i)
                    {
                        SignEntry? e = (SignEntry?)list[i];
                        Map[]? maps = null;

                        switch (e.m_Map)
                        {
                            case 0: maps = brit; break; // Trammel and Felucca
                            case 1: maps = fel; break;  // Felucca
                            case 2: maps = tram; break; // Trammel
                            case 3: maps = ilsh; break; // Ilshenar
                            case 4: maps = malas; break; // Malas
                        }

                        for (int j = 0; maps != null && j < maps.Length; ++j)
                            if (maps[j] == Map.Felucca)
                                AddRecord(e.m_ItemID, e.m_Location, maps[j], e.m_Text);
                    }

                    Console.WriteLine("Places generation complete with {0} places registered.", PlacesQuickTable.Count);
                }
                else
                {
                    Console.WriteLine("{0} not found!", cfg);
                }
            }

            public static void AddRecord(int itemID, Point3D location, Map map, string name)
            {
                if (name.StartsWith("#"))
                    name = Text.Cliloc.Lookup[Utility.ToInt32(name.Substring(1))];

                if (m_placesQuickTable.ContainsKey(name))
                    // add a location
                    m_placesQuickTable[name].Add(location);
                else
                {
                    m_placesQuickTable.Add(name, new List<Point3D> { location });
                    m_signIds.Add(itemID);
                }
            }
        }
        #endregion Sign Parser
        #region CacheLookup Command
        public static void Initialize()
        {
            Server.CommandSystem.Register("CacheLookup", AccessLevel.Administrator, new CommandEventHandler(CacheLookup_OnCommand));
        }
        [Usage("CacheLookup")]
        [Description("Looks up a text string in all of our databases.")]
        public static void CacheLookup_OnCommand(CommandEventArgs c)
        {
            string? needle = c.ArgString?.Trim();

            if (string.IsNullOrWhiteSpace(needle))
            {
                c.Mobile.SendMessage("Usage: CacheLookup <text>");
                return;
            }

            // remove surrounding quotes
            if (needle.Length >= 2 && needle[0] == '"' && needle[needle.Length - 1] == '"')
                needle = needle.Substring(1, needle.Length - 2);

            bool found = SearchCaches(c, needle);

            if (!found && Utility.LooksCamelCase(needle))
            {
                string camel = Utility.SplitCamelCase(needle);

                if (!camel.Equals(needle, StringComparison.OrdinalIgnoreCase))
                {
                    c.Mobile.SendMessage("No match for '{0}', retrying as '{1}'...", needle, camel);
                    found = SearchCaches(c, camel);
                }
            }

            if (!found)
                c.Mobile.SendMessage("'{0}' was not found in any cache list.", needle);

            c.Mobile.SendMessage("done.");
        }

        public static bool SearchCaches(CommandEventArgs c, string needle)
        {
            bool found = false;

            List<Dictionary<string, List<Point3D>>> dic3DLists = new List<Dictionary<string, List<Point3D>>>()
    { m_placesQuickTable, m_vendorsQuickTable };

            List<Dictionary<string, List<Spawner>>> dicSpawnerLists = new List<Dictionary<string, List<Spawner>>>()
    { m_spawnedObjectsQuickTable, m_spawnedItemsQuickTable, m_spawnedMobilesQuickTable };

            List<Dictionary<string, Point3D>> dicMoongateLists = new List<Dictionary<string, Point3D>>()
    { m_moongatesQuickTable };

            foreach (string s in PlacesList)
            {
                if (s != null && s.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.Mobile.SendMessage("'{0}' was found in PlacesList", s);
                    found = true;
                }
            }

            foreach (string s in InventoryList)
            {
                if (s != null && s.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.Mobile.SendMessage("'{0}' was found in InventoryList", s);
                    found = true;
                }
            }

            foreach (string s in VendorTitleList)
            {
                if (s != null && s.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.Mobile.SendMessage("'{0}' was found in VendorTitleList", s);
                    found = true;
                }
            }

            foreach (string s in TownsList)
            {
                if (s != null && s.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c.Mobile.SendMessage("'{0}' was found in TownsList", s);
                    found = true;
                }
            }

            foreach (Dictionary<string, List<Point3D>> dict in dic3DLists)
            {
                foreach (string s in dict.Keys)
                {
                    if (s != null && s.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        c.Mobile.SendMessage("'{0}' was found in dic3DLists", s);
                        found = true;
                    }
                }
            }

            foreach (Dictionary<string, Point3D> dict in dicMoongateLists)
            {
                foreach (string s in dict.Keys)
                {
                    if (s != null && s.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        c.Mobile.SendMessage("'{0}' was found in dicMoongateLists", s);
                        found = true;
                    }
                }
            }

            return found;
        }
        #endregion CacheLookup Command
    }
}