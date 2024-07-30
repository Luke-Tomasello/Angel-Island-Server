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

/* Scripts/Items/Skill Items/Fishing/Misc/Sextant.cs
 * CHANGELOG:
 *  7/6/2023, Adam (Parse)
 *      Add a Parse command for Sextant.
 *      Parses a sextant string like 93� 20'S, 135� 21'E (and other formats), and returns a Point3D
 *  12/22/21, Yoar
 *      Added CalculateMapBounds method to calculate centered map bounds without a MapItem ref.
 *  11/10/21, Yoar
 *      Added support for ResourceMap.
 *  11/10/21, Yoar
 *      Rewrote the cartographer's sextant:
 *      - Added "public static" methods UpdateMap, CenterMap.
 *      - Removed to need of timers while updating a map. This is achieved by
 *        1. Internalizing the map to close all map displays.
 *        2. Calling mapItem.ProcessDelta before sending the new map display.
 *      - Rewrote the method GiveDirectionalHint as "public static" as well.
 *      - Rewrote the hints in GiveDirectionalHint so that they also support short-range hints.
 *	02/11/06, Adam
 *		Make common the formatting of sextant coords.
 */

using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using TextCopy;

namespace Server.Items
{
    public class Sextant : Item
    {
        [Constructable]
        public Sextant()
            : base(0x1058)
        {
            Weight = 2.0;
        }

        public Sextant(Serial serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            int xLong = 0, yLat = 0;
            int xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            if (Sextant.Format(from.Location, from.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
            {
                string location = Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                from.LocalOverheadMessage(MessageType.Regular, from.SpeechHue, false, location);
                if (from.AccessLevel >= AccessLevel.Administrator)
                    ClipboardService.SetText(location);
            }
        }

        public static bool ComputeMapDetails(Map map, int x, int y, out int xCenter, out int yCenter, out int xWidth, out int yHeight)
        {
            xWidth = 5120; yHeight = 4096;

            if (map == Map.Trammel || map == Map.Felucca)
            {
                if (x >= 0 && y >= 0 && x < 5120 && y < 4096)
                {
                    xCenter = 1323; yCenter = 1624;
                }
                else if (x >= 5120 && y >= 2304 && x < 6144 && y < 4096)
                {
                    xCenter = 5936; yCenter = 3112;
                }
                else
                {
                    xCenter = 0; yCenter = 0;
                    return false;
                }
            }
            else if (x >= 0 && y >= 0 && x < map.Width && y < map.Height)
            {
                xCenter = 1323; yCenter = 1624;
            }
            else
            {
                xCenter = 0; yCenter = 0;
                return false;
            }

            return true;
        }

        public static Point3D ReverseLookup(Map map, int xLong, int yLat, int xMins, int yMins, bool xEast, bool ySouth)
        {
            if (map == null || map == Map.Internal)
                return Point3D.Zero;

            int xCenter, yCenter;
            int xWidth, yHeight;

            if (!ComputeMapDetails(map, 0, 0, out xCenter, out yCenter, out xWidth, out yHeight))
                return Point3D.Zero;

            double absLong = xLong + ((double)xMins / 60);
            double absLat = yLat + ((double)yMins / 60);

            if (!xEast)
                absLong = 360.0 - absLong;

            if (!ySouth)
                absLat = 360.0 - absLat;

            int x, y, z;

            x = xCenter + (int)Math.Round(((absLong * xWidth) / 360));
            y = yCenter + (int)Math.Round(((absLat * yHeight) / 360));

            if (x < 0)
                x += xWidth;
            else if (x >= xWidth)
                x -= xWidth;

            if (y < 0)
                y += yHeight;
            else if (y >= yHeight)
                y -= yHeight;

            z = map.GetAverageZ(x, y);

            return new Point3D(x, y, z);
        }

        public static bool Format(Point3D p, Map map, ref int xLong, ref int yLat, ref int xMins, ref int yMins, ref bool xEast, ref bool ySouth)
        {
            if (map == null || map == Map.Internal)
                return false;

            int x = p.X, y = p.Y;
            int xCenter, yCenter;
            int xWidth, yHeight;

            if (!ComputeMapDetails(map, x, y, out xCenter, out yCenter, out xWidth, out yHeight))
                return false;

            double absLong = (double)((x - xCenter) * 360) / xWidth;
            double absLat = (double)((y - yCenter) * 360) / yHeight;

            if (absLong > 180.0)
                absLong = -180.0 + (absLong % 180.0);

            if (absLat > 180.0)
                absLat = -180.0 + (absLat % 180.0);

            bool east = (absLong >= 0), south = (absLat >= 0);

            if (absLong < 0.0)
                absLong = -absLong;

            if (absLat < 0.0)
                absLat = -absLat;

            xLong = (int)absLong;
            yLat = (int)absLat;

            xMins = (int)((absLong % 1.0) * 60);
            yMins = (int)((absLat % 1.0) * 60);

            xEast = east;
            ySouth = south;

            return true;
        }

        public static string Format(int xLong, int yLat, int xMins, int yMins, bool xEast, bool ySouth)
        {
            return String.Format("{0}� {1}'{2}, {3}� {4}'{5}", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W");
        }
        public static string Where(IEntity ent)
        {
            int xLong = 0, yLat = 0;
            int xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            if (Sextant.Format(ent.Location, ent.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
            {
                string location = Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                return location;
            }

            return "(unknown)";
        }
        public static string Normalize(string desc)
        {
            // length == 4 is old - style sextant: [go 55 54 N 72 54 W
            // length == 6 is new-style sextant: [go 55� 54'N 72� 54'W
            // if (e.Length == 6) patch coords to old-style
            String[] args = desc.Split(new char[] { ' ', '\'', '�' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                int.Parse(args[0]);
                return string.Join(" ", args); ;
            }
            catch
            {
                List<String> list = new(args);
                list.Remove(args[0]);
                return string.Join(" ", list.ToArray());
            }
        }
        public static Point3D Parse(Map map, string desc)
        {
            String clean = Normalize(desc).ToUpper();
            String[] args = clean.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            Point3D px = new Point3D();

            try
            {
                int xLong = Int32.Parse(args[3]);
                int yLat = Int32.Parse(args[0]);
                int xMins = Int32.Parse(args[4]);
                int yMins = Int32.Parse(args[1]);
                bool xEast = Insensitive.Equals(args[5], "E");
                bool ySouth = Insensitive.Equals(args[2], "S");
                //string DebugTestString_ShouldMatchInput = Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                px = Sextant.ReverseLookup(map, xLong, yLat, xMins, yMins, xEast, ySouth);

                // reverse test to see if we got same values
                int xLong_out = 0;
                int yLat_out = 0;
                int xMins_out = 0;
                int yMins_out = 0;
                bool xEast_out = false;
                bool ySouth_out = false;
                Format(px, map, ref xLong_out, ref yLat_out, ref xMins_out, ref yMins_out, ref xEast_out, ref ySouth_out);
                return px;
            }
            catch
            {
                return px;
            }
        }
    }

    public class CartographersSextant : Item
    {
        [Constructable]
        public CartographersSextant()
            : base(0x1058)
        {
            Weight = 2.0;
            Name = "a cartographer's sextant";
        }

        public CartographersSextant(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1 && Name == "a cartographers sextant")
                Name = "a cartographer's sextant";
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage("Target the map you wish to update."); // TODO: Better message?
            from.Target = new UpdateMapTarget();
        }

        public class UpdateMapTarget : Target
        {
            public UpdateMapTarget()
                : base(2, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.Alive)
                    return;

                if (targeted is TreasureMap)
                {
                    TreasureMap tmap = (TreasureMap)targeted;

                    if (tmap.Completed)
                        from.SendMessage("That treasure map has already been completed.");
                    else if (tmap.Decoder == null)
                        from.SendMessage("This map has not yet been decoded.");
                    else if (tmap.Decoder != from)
                        from.SendMessage("You did not decode this map.");
                    else if (tmap.ChestMap != from.Map)
                        from.SendMessage("You are in the wrong facet!");
                    else if (!tmap.WorldPointInMap(from.Location))
                        GiveDirectionalHint(from, tmap.ChestLocation);
                    else
                        UpdateMap(from, tmap, tmap.ChestMap, false, tmap.ChestLocation, new Point2D(from.Location));
                }
                else if (targeted is ResourceMap)
                {
                    ResourceMap rmap = (ResourceMap)targeted;

                    rmap.EnsureLocation();

                    if (rmap.Uses <= 0)
                        from.SendMessage("That resource map has already been completed.");
                    else if (rmap.BankMap != from.Map)
                        from.SendMessage("You are in the wrong facet!");
                    else if (!rmap.WorldPointInMap(from.Location))
                        GiveDirectionalHint(from, rmap.BankLocation);
                    else
                        UpdateMap(from, rmap, rmap.BankMap, true, rmap.BankLocation, new Point2D(from.Location));
                }
                else
                {
                    from.SendMessage("That is not a treasure map nor a resource map."); // TODO: Better message?
                }
            }
        }

        public static bool UpdateMap(Mobile m, MapItem mapItem, Map facet, bool centering, params Point2D[] pins)
        {
            return UpdateMap(m, mapItem, facet, centering, (IList<Point2D>)pins);
        }

        /* The problem when updating maps is that, while we can easily add new pins and
         * send a new map display, the old map display remains on-screen. In order to
         * remove the old map display, we internalize the map before making our updates.
         * Finally, before sending the new map display, we must ensure that the item
         * update packets are sent the beholder of the map.
         */
        public static bool UpdateMap(Mobile m, MapItem mapItem, Map facet, bool centering, IList<Point2D> pins)
        {
            if ((facet != Map.Felucca && facet != Map.Trammel))
                return false; // invalid facet

            object parent = mapItem.Parent;

            if (parent != null && !(parent is Item))
                return false; // invalid parent (shouldn't happen)

            // remember the position of the map item
            Point3D itemLoc = mapItem.Location;
            Map itemMap = mapItem.Map;

            // close all displays for this map
            mapItem.Internalize();

            mapItem.Pins.Clear();

            // center the map on the first pin
            if (centering && pins.Count != 0)
                CenterMap(mapItem, pins[0]);

            for (int i = 0; i < pins.Count; i++)
            {
                Point2D pin = pins[i];

                if (mapItem.Bounds.Contains(pin))
                    mapItem.AddWorldPin(pin.X, pin.Y);
            }

            // move the map item back to its original position
            if (parent == null)
            {
                mapItem.MoveToWorld(itemLoc, itemMap);
            }
            else if (parent is Item)
            {
                ((Item)parent).AddItem(mapItem);

                mapItem.Location = itemLoc;
            }

            // send the necessary item update packets
            mapItem.ProcessDelta();

            if (m.InRange(mapItem.GetWorldLocation(), 2))
                mapItem.DisplayTo(m);

            return true;
        }

        public static void CenterMap(MapItem mapItem, IPoint2D loc)
        {
            int width = mapItem.Bounds.Width;
            int height = mapItem.Bounds.Height;

            if (width == 0)
                width = 2 * mapItem.Width;

            if (height == 0)
                height = 2 * mapItem.Height;

            mapItem.Bounds = CalculateMapBounds(loc, width, height);
        }

        public static Rectangle2D CalculateMapBounds(IPoint2D loc, int width, int height)
        {
            int x1 = Math.Max(0, loc.X - width / 2);
            int y1 = Math.Max(0, loc.Y - height / 2);

            int x2 = Math.Min(5119, x1 + width);
            int y2 = Math.Min(4095, y1 + height);

            // ensure the width and height don't change
            x1 = Math.Max(0, x2 - width);
            y1 = Math.Max(0, y2 - height);

            return new Rectangle2D(x1, y1, x2 - x1, y2 - y1);
        }

        public static void GiveDirectionalHint(Mobile from, Point2D loc)
        {
            Direction dir = from.GetDirectionTo(loc);

            string direction;

            switch (dir)
            {
                case Direction.Left: direction = "south-west"; break;
                case Direction.Right: direction = "north-east"; break;
                case Direction.Up: direction = "north-west"; break;
                case Direction.Down: direction = "south-east"; break;
                default: direction = dir.ToString().ToLower(); break;
            }

            double dist = from.GetDistanceToSqrt(loc);

#if old
            if (dist > 200)
                from.SendMessage(string.Format("Tis a long journey {0} from here.", direction));
            else if (dist > 150)
                from.SendMessage(string.Format("Tis quite a long {0} distance from here.", direction));
            else if (dist > 100)
                from.SendMessage(string.Format("Tis a long way {0} from here.", direction));
            else if (dist > 75)
                from.SendMessage(string.Format("Tis a fair distance {0} from there.", direction));
            else if (dist > 50)
                from.SendMessage(string.Format("Tis quite a ways {0} from here.", direction));
            else if (dist > 40)
                from.SendMessage(string.Format("Tis just a short way {0} from here.", direction));
            else if (dist >= 32)
                from.SendMessage(string.Format("Tis just a few steps {0} from here.", direction));
#else
            if (dist > 200)
                from.SendMessage(string.Format("Tis a long journey {0} from here.", direction));
            else if (dist > 100)
                from.SendMessage(string.Format("Tis quite a long distance {0} from here.", direction));
            else if (dist > 50)
                from.SendMessage(string.Format("Tis a fair distance {0} from here.", direction));
            else if (dist > 25)
                from.SendMessage(string.Format("Tis quite a way {0} from here.", direction));
            else if (dist > 10)
                from.SendMessage(string.Format("Tis just a short way {0} from here.", direction));
            else
                from.SendMessage(string.Format("Tis just a few steps {0} from here.", direction));
#endif
        }
    }
}