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

/* Scripts\Commands\SignParser.cs
 * ChangeLog:
 * 9/29/22, Adam
 *  Add Map(s) and rectangle(s) to update. This allows the targeted application of signs. For instance, one building.
 *  9/20/22, Adam 
 *      Update to  RunUO 2.6
 */

using Server.Items;
using Server.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Commands
{
    public class SignParser
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

        public static void Initialize()
        {
            CommandSystem.Register("SignGen", AccessLevel.Owner, new CommandEventHandler(SignGen_OnCommand));
        }

        [Usage("SignGen")]
        [Description("Generates world/shop signs on all facets.")]
        public static void SignGen_OnCommand(CommandEventArgs c)
        {
            Parse(c.Mobile);
        }

        public static void Parse(Mobile from, List<Rectangle2D>? boundingRects = null, Map[]? includeMaps = null)
        {
            string cfg = Path.Combine(Core.ExeDirectory, Path.Combine(Core.DataDirectory, "signs.cfg"));
            if (includeMaps == null)
                includeMaps = new Map[] { Map.Felucca, Map.Trammel, Map.Ilshenar, Map.Malas, Map.Tokuno };

            if (File.Exists(cfg))
            {
                List<SignEntry> list = new List<SignEntry>();
                from.SendMessage("Generating signs, please wait.");

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
                Map[] tokuno = new Map[] { Map.Tokuno };

                for (int i = 0; i < list.Count; ++i)
                {
                    SignEntry e = list[i];
                    Map[]? maps = null;

                    switch (e.m_Map)
                    {
                        case 0: maps = brit; break; // Trammel and Felucca
                        case 1: maps = fel; break;  // Felucca
                        case 2: maps = tram; break; // Trammel
                        case 3: maps = ilsh; break; // Ilshenar
                        case 4: maps = malas; break; // Malas
                        case 5: maps = tokuno; break; // Tokuno Islands
                    }

                    for (int j = 0; maps != null && j < maps.Length; ++j)
                        if (maps[j] != null)
                            if (Array.Exists(includeMaps, element => element == maps[j]))
                                if (boundingRects == null || R2dListContains(boundingRects, e.m_Location))
                                    Add_Static(e.m_ItemID, e.m_Location, maps[j], e.m_Text);
                }

                from.SendMessage("Sign generating complete.");
            }
            else
            {
                from.SendMessage("{0} not found!", cfg);
            }
        }

        private static bool R2dListContains(List<Rectangle2D> boundingRects, Point3D px)
        {
            foreach (Rectangle2D rect in boundingRects)
                if (rect.Contains(px))
                    return true;

            return false;
        }

        private static Queue<Item> m_ToDelete = new Queue<Item>();

        private static bool OldSignFilter(Item oldItem, int newItemID, Point3D newItemLoc)
        {   // old style sign .. a static (not a Sign) with the same ItemID
            if (oldItem.ItemID == newItemID)
                return true;

            return false;
        }
        private static bool HasException(Item item)
        {   // The cross roads, To Britain, To Trinsic .. leave this alone .. RunUO 2.6 gets it wrong
            if (item.Location.X == 1355 && item.Location.Y == 1835)
                return true;

            return false;
        }
        public static void Add_Static(int itemID, Point3D location, Map map, string name)
        {
            IPooledEnumerable eable = map.GetItemsInRange(location, 0);

            foreach (Item item in eable)
            {
                if (item == null || item.Deleted)
                    continue;

                if (HasException(item))
                {
                    eable.Free();
                    return;                                       // The cross roads, To Britain, To Trinsic .. leave this alone .. RunUO 2.6 gets it wrong
                }
                else if (item.Z == location.Z)
                {
                    if (item is Sign && item.ItemID == itemID || OldSignFilter(item, itemID, location))
                        m_ToDelete.Enqueue(item);
                }
                else if (Math.Abs(item.Z - location.Z) <= 5) // old sign, bad placement
                {
                    if (item is Sign && item.ItemID == itemID || OldSignFilter(item, itemID, location))
                        m_ToDelete.Enqueue(item);
                }
            }

            eable.Free();

            while (m_ToDelete.Count > 0)
                m_ToDelete.Dequeue().Delete();

            Item sign;

            if (name.StartsWith("#"))
            {
                sign = new LocalizedSign(itemID, Utility.ToInt32(name.Substring(1)));
            }
            else
            {
                sign = new Sign(itemID);
                sign.Name = name;
            }

            if (map == Map.Malas)
            {
                if (location.X >= 965 && location.Y >= 502 && location.X <= 1012 && location.Y <= 537)
                    sign.Hue = 0x47E;
                else if (location.X >= 1960 && location.Y >= 1278 && location.X < 2106 && location.Y < 1413)
                    sign.Hue = 0x44E;
            }

            sign.MoveToWorld(location, map);
        }

        #region Runtime use of a sign's name => location lookup
        public sealed class SignLocation
        {
            public string Name { get; set; }
            public string Key { get; set; }
            public Point3D Location { get; set; }
            public Map? Map { get; set; }
        }

        public static void Configure()
        {
            EventSink.WorldLoad += OnWorldLoad;
        }
        private static readonly Dictionary<string, List<SignLocation>> m_SignLocations =
            new Dictionary<string, List<SignLocation>>(StringComparer.OrdinalIgnoreCase);

        private static void OnWorldLoad() => LoadSignLocations();
        public static void LoadSignLocations()
        {
            m_SignLocations.Clear();

            string path = Path.Combine(Core.DataDirectory, "signs.cfg");

            if (!File.Exists(path))
                return;

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                string[] split = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length < 6)
                    continue;

                if (!int.TryParse(split[0], out int mapIndex))
                    continue;

                if (!int.TryParse(split[2], out int x))
                    continue;

                if (!int.TryParse(split[3], out int y))
                    continue;

                if (!int.TryParse(split[4], out int z))
                    continue;

                string clilocToken = split[5].TrimStart('#');

                if (!int.TryParse(clilocToken, out int clilocNumber))
                    continue;

                if (!Cliloc.Lookup.TryGetValue(clilocNumber, out string? name))
                    continue;

                Map map = Map.Maps[mapIndex];

                if (map == null || map == Map.Internal)
                    continue;

                AddSignLocation(name, new Point3D(x, y, z), map);
            }
        }
        private static void AddSignLocation(string name, Point3D loc, Map map)
        {
            string? key = NormalizeSignName(name);

            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!m_SignLocations.TryGetValue(key, out List<SignLocation>? list))
                m_SignLocations[key] = list = new List<SignLocation>();

            list.Add(new SignLocation
            {
                Name = name,
                Key = key,
                Location = loc,
                Map = map
            });
        }
        private static string? NormalizeSignName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim().ToLowerInvariant();

            if (text.StartsWith("the "))
                text = text.Substring(4);

            return text;
        }
        public static SignLocation? FindSignLocation(Mobile from, string raw)
        {
            if (from == null || string.IsNullOrWhiteSpace(raw))
                return null;

            string? key = NormalizeSignName(raw);

            if (key == null)
                return null;

            if (!m_SignLocations.TryGetValue(key, out List<SignLocation>? list) || list.Count == 0)
                return null;

            // Prefer exact/current-map match.
            SignLocation? match = list.FirstOrDefault(s => s.Map == from.Map);

            if (match != null)
                return new SignLocation
                {
                    Name = match.Name,
                    Key = match.Key,
                    Location = match.Location,
                    Map = match.Map
                };

            // Player convenience: allow familiar sign names from another facet,
            // but project the destination onto the player's current map.
            // This prevents map-hopping while allowing Felucca names to work in TestCenter/Trammel.
            if (from.AccessLevel == AccessLevel.Player && from.Map != null && from.Map != Map.Internal)
            {
                SignLocation? fallback = list.FirstOrDefault(s => s.Map != null && s.Map != Map.Internal);

                if (fallback != null)
                    return new SignLocation
                    {
                        Name = fallback.Name,
                        Key = fallback.Key,
                        Location = fallback.Location,
                        Map = from.Map
                    };
            }

            // Staff fallback: if no current-map match, use first valid sign location.
            if (from.AccessLevel > AccessLevel.Player)
            {
                SignLocation? fallback = list.FirstOrDefault(s => s.Map != null && s.Map != Map.Internal);

                if (fallback != null)
                    return new SignLocation
                    {
                        Name = fallback.Name,
                        Key = fallback.Key,
                        Location = fallback.Location,
                        Map = fallback.Map
                    };
            }

            return null;
        }

        #endregion Runtime use of a sign's name => location lookup
    }
}