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

/* Scripts/Engines/DRDT/RegionCommands.cs
 *  9/25/22, Yoar
 *      Rewrote [RegionBounds such that it displays the rectangle perimeters as walls,
 *      how it used to be
 *  9/22/22, Yoar
 *      Fixed ExportCustomRegions: Now loops over custom region controllers
 *  9/19/22, Yoar
 *      Renamed to "RegionCommands"
 *  9/15/22, Yoar
 *      Added [ExportCustomRegions command
 *  9/14/22, Yoar
 *      Initial version.
 *      Moved [GetRegion and [RegionBounds commands to this file.
 */

using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Regions;
using System;
using System.Collections.Generic;

namespace Server.Commands
{
    public static class RegionCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("GetRegion", AccessLevel.GameMaster, new CommandEventHandler(GetRegion_OnCommand));
            CommandSystem.Register("RegionBounds", AccessLevel.GameMaster, new CommandEventHandler(RegionBounds_OnCommand));
            CommandSystem.Register("ExportCustomRegions", AccessLevel.Administrator, new CommandEventHandler(ExportCustomRegions_OnCommand));
        }

        [Usage("GetRegion (<name>)")]
        [Description("Displays information about the specified region (or current region, if none specified).")]
        private static void GetRegion_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            Region region = from.Region;

            if (e.Arguments.Length >= 2)
            {
                from.SendMessage("Usage: RegionBounds (<name>)");
                return;
            }

            if (e.Arguments.Length == 1)
            {
                region = FindRegion(from.Map, e.GetString(0));

                if (region == null)
                {
                    from.SendMessage("That region does not exist.");
                    return;
                }
            }

            DisplayRegion(from, region);

            if (region is StaticRegion)
                from.SendGump(new RegionControlGump((StaticRegion)region));
        }

        [Usage("RegionBounds (<name>)")]
        [Description("Flashes the area and bounds of the specified region (or current region, if none specified).")]
        private static void RegionBounds_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            Region region = from.Region;

            if (e.Arguments.Length >= 2)
            {
                from.SendMessage("Usage: RegionBounds (<name>)");
                return;
            }

            if (e.Arguments.Length == 1)
            {
                region = FindRegion(from.Map, e.GetString(0));

                if (region == null)
                {
                    from.SendMessage("That region does not exist.");
                    return;
                }
            }

            DisplayRegion(from, region);

            FlashBounds(from, region.Coords, region.Map);
        }

        public static void FlashBounds(Mobile from, List<Rectangle3D> area, Map map)
        {
            if (map == null || map == Map.Internal || map != from.Map)
                return;

            foreach (Rectangle3D rect3D in area)
            {
                Rectangle2D rect2D = new Rectangle2D(new Point2D(rect3D.Start), new Point2D(rect3D.End));

                FlashBounds(from, rect2D, map);
            }
        }

        public static void FlashBounds(Mobile from, Rectangle2D rect, Map map)
        {
            if (map == null || map == Map.Internal || map != from.Map || from.NetState == null)
                return;

            from.ProcessDelta();

            int x1 = rect.Start.X - 1, x2 = rect.End.X;
            int y1 = rect.Start.Y - 1, y2 = rect.End.Y;

            Point3D pNW = new Point3D(x1, y1, map.GetAverageZ(x1, y1));

            if (from.InRange(pNW, 18))
                from.NetState.Send(new LocationEffect(pNW, 251, 1, 75, 1152, 3));

            Point3D pSW = new Point3D(x1, y2, map.GetAverageZ(x1, y2));

            if (from.InRange(pSW, 18))
                from.NetState.Send(new LocationEffect(pSW, 250, 1, 75, 1152, 3));

            Point3D pNE = new Point3D(x2, y1, map.GetAverageZ(x1, y2));

            if (from.InRange(pNE, 18))
                from.NetState.Send(new LocationEffect(pNE, 249, 1, 75, 1152, 3));

            Point3D pSE = new Point3D(x2, y2, map.GetAverageZ(x1, y2));

            if (from.InRange(pSE, 18))
                from.NetState.Send(new LocationEffect(pSE, 248, 1, 75, 1152, 3));

            for (int x = x1 + 1; x <= x2 - 1; x++)
            {
                Point3D pN = new Point3D(x, y1, map.GetAverageZ(x, y1));

                if (from.InRange(pN, 18))
                    from.NetState.Send(new LocationEffect(pN, 249, 1, 75, 1152, 3));

                Point3D pS = new Point3D(x, y2, map.GetAverageZ(x, y2));

                if (from.InRange(pS, 18))
                    from.NetState.Send(new LocationEffect(pS, 249, 1, 75, 1152, 3));
            }

            for (int y = y1 + 1; y <= y2 - 1; y++)
            {
                Point3D pW = new Point3D(x1, y, map.GetAverageZ(x1, y));

                if (from.InRange(pW, 18))
                    from.NetState.Send(new LocationEffect(pW, 250, 1, 75, 1152, 3));

                Point3D pE = new Point3D(x2, y, map.GetAverageZ(x2, y));

                if (from.InRange(pE, 18))
                    from.NetState.Send(new LocationEffect(pE, 250, 1, 75, 1152, 3));
            }
        }

        private static Region FindRegion(Map map, string name)
        {
            if (map == null)
                return null;

            foreach (Region reg in Region.Regions)
            {
                if (reg.Map == map && reg.Name == name)
                    return reg;
            }

            return null;
        }

        private static void DisplayRegion(Mobile m, Region region)
        {
            string regionName;

            if (region.Map != null && region == region.Map.DefaultRegion)
                regionName = string.Format("({0} default region)", region.Map.Name);
            else if (region.Name == null)
                regionName = string.Format("(unnamed region)", region.Name);
            else
                regionName = region.Name;

            m.SendMessage("Your region: {0} (Type={1}, Priority={2}).", regionName, region.GetType().Name, region.Priority);
        }

        [Usage("ExportCustomRegions")]
        [Description("Exports all custom regions to XML.")]
        private static void ExportCustomRegions_OnCommand(CommandEventArgs e)
        {
            List<StaticRegion> toSave = new List<StaticRegion>();

            foreach (CustomRegionControl rc in CustomRegionControl.Instances)
            {
                if (rc.GetType() == typeof(CustomRegionControl))
                    toSave.Add(rc.CustomRegion);
            }

            StaticRegion.SaveRegions(toSave, "CustomRegions.xml");

            e.Mobile.SendMessage("Exported {0} custom region(s) to XML.", toSave.Count);
        }
    }
}