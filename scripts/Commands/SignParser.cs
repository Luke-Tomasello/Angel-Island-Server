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

/* Scripts\Commands\SignParser.cs
 * ChangeLog:
 * 9/29/22, Adam
 *  Add Map(s) and rectangle(s) to update. This allows the targeted application of signs. For instance, one building.
 *  9/20/22, Adam 
 *      Update to  RunUO 2.6
 */

using Server.Items;
using System;
using System.Collections.Generic;
using System.IO;

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

        public static void Parse(Mobile from, List<Rectangle2D> boundingRects = null, Map[] includeMaps = null)
        {
            string cfg = Path.Combine(Core.BaseDirectory, Path.Combine(Core.DataDirectory, "signs.cfg"));
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
                    Map[] maps = null;

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
    }
}