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

/* Scripts/Engines/DRDT/Gumps/EditAreaGump.cs
 * ChangeLog:
 *  3/6/2024, Adam: (Flash Area)
 *      this was fine most of the time, until we are in a cave, at which point the graphic paints out of view (on the mountain top)
 *      Point3D p = new Point3D(x, y, map.GetAverageZ(x, y));
 *      now we will just use the staff's Z
 *  9/24/22, Yoar
 *      - Rewrote - now carries a ref to the region
 *      - Readded "Add (X/Y)" button
 *      - FlashArea:
 *        * Now looping over the client range rather than the entire rectangle
 *        * Now Using a different graphic to display boundaries
 *  9/13/22, Yoar
 *      Removed calls to CloseGump so that we may view multiple region controllers
 *      at the same time
 *  9/11/22, Yoar
 *      Removed Add X/Y button
 *      Added Flash Area button
 *  9/11/22, Yoar (Custom Region Overhaul)
 *      Completely overhauled custom region system
 */

using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Gumps
{
    public class EditAreaGump : Gump
    {
        private Region m_Region;
        private bool m_Inns;

        public EditAreaGump(Region region, bool inns)
            : base(50, 50)
        {
            m_Region = region;
            m_Inns = inns;

            List<Rectangle3D> area = (m_Inns ? m_Region.InnBounds : m_Region.Coords);

            AddPage(0);

            AddBackground(0, 0, 590, 300, 9270);
            AddAlphaRegion(0, 0, 590, 300);

            if (m_Inns)
                AddHtml(30, 20, 530, 20, String.Format("<BASEFONT COLOR=#FFFFFF><CENTER>Edit Inns of {0}</CENTER></BASEFONT>", m_Region.Name), false, false);
            else
                AddHtml(30, 20, 530, 20, String.Format("<BASEFONT COLOR=#FFFFFF><CENTER>Edit Area of {0}</CENTER></BASEFONT>", m_Region.Name), false, false);

            AddButton(50, 60, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddLabel(85, 60, 1152, "Add (Target)");

            AddLabel(210, 60, 1152, "Start (X, Y):");
            AddBackground(300, 58, 70, 24, 3000);
            AddTextEntry(305, 60, 60, 20, 0xFA5, 0, "0");

            AddLabel(370, 60, 1152, ",");
            AddBackground(380, 58, 70, 24, 3000);
            AddTextEntry(385, 60, 60, 20, 0xFA5, 1, "0");

            AddLabel(210, 85, 1152, "End (X, Y):");
            AddBackground(300, 83, 70, 24, 3000);
            AddTextEntry(305, 85, 60, 20, 0xFA5, 2, "0");

            AddLabel(370, 85, 1152, ",");
            AddBackground(380, 83, 70, 24, 3000);
            AddTextEntry(385, 85, 60, 20, 0xFA5, 3, "0");

            AddButton(455, 60, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddLabel(490, 60, 1152, "Add (Input)");

            AddButton(50, 85, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddLabel(85, 85, 1152, "Flash Area");

            AddPage(1);

            const int y0 = 120;
            int y = y0;
            int page = 1;

            for (int i = 0; i < area.Count; i++, y += 25)
            {
                Rectangle3D rect = area[i];

                if (i != 0 && (i % 5) == 0)
                {
                    y = y0;

                    AddButton(510, 255, 4007, 4009, 0, GumpButtonType.Page, page + 1);
                    AddPage(++page);
                    AddButton(50, 255, 4014, 4016, 1, GumpButtonType.Page, page - 1);
                }

                AddButton(50, y, 4017, 4019, 100 + i, GumpButtonType.Reply, 0);
                AddLabel(85, y, 1152, rect.Start.ToString());
                AddLabel(270, y, 1152, "<-->");
                AddLabel(370, y, 1152, rect.End.ToString());
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            List<Rectangle3D> area = (m_Inns ? m_Region.InnBounds : m_Region.Coords);

            if (info.ButtonID == 1) // Add (Target)
            {
                BoundingBoxPicker.Begin(from, ChooseArea_Callback, ChooseArea_Cancel, new InternalState(m_Region, m_Inns));
            }
            else if (info.ButtonID == 2) // Add (X/Y)
            {
                int x1 = 0, y1 = 0, x2 = 0, y2 = 0;

                if (!int.TryParse(info.GetTextEntry(0).Text, out x1) ||
                    !int.TryParse(info.GetTextEntry(1).Text, out y1) ||
                    !int.TryParse(info.GetTextEntry(2).Text, out x2) ||
                    !int.TryParse(info.GetTextEntry(3).Text, out y2))
                {
                    from.SendMessage("Error: Bad format.");
                }
                else
                {
                    Point2D start = new Point2D(x1, y1);
                    Point2D end = new Point2D(x2, y2);

                    AddRect(m_Region, m_Inns, new Rectangle3D(new Point3D(start, Region.DefaultMinZ), new Point3D(end, Region.DefaultMaxZ)));
                }

                from.SendGump(new EditAreaGump(m_Region, m_Inns));
            }
            else if (info.ButtonID == 3) // Flash Area
            {
                FlashArea(from, area, m_Region.Map);

                from.SendGump(new EditAreaGump(m_Region, m_Inns));
            }
            else if (info.ButtonID >= 100 && info.ButtonID < 100 + area.Count) // Remove
            {
                RemoveRect(m_Region, m_Inns, info.ButtonID - 100);

                from.SendGump(new EditAreaGump(m_Region, m_Inns));
            }
        }

        private static void ChooseArea_Callback(Mobile from, Map map, Point3D start, Point3D end, object obj)
        {
            InternalState state = (InternalState)obj;

            AddRect(state.Region, state.Inns, new Rectangle3D(new Point3D(start, Region.DefaultMinZ), new Point3D(end, Region.DefaultMaxZ)));

            from.SendGump(new EditAreaGump(state.Region, state.Inns));
        }

        private static void ChooseArea_Cancel(Mobile from, object obj)
        {
            InternalState state = (InternalState)obj;

            from.SendGump(new EditAreaGump(state.Region, state.Inns));
        }

        public static void AddRect(Region region, bool inns, Rectangle3D rect)
        {
            List<Rectangle3D> area = (inns ? region.InnBounds : region.Coords);

            bool registered = Region.Regions.Contains(region);

            if (registered)
                Region.RemoveRegion(region);

            area.Add(rect);

            if (registered)
                Region.AddRegion(region);
        }

        public static void RemoveRect(Region region, bool inns, int index)
        {
            List<Rectangle3D> area = (inns ? region.InnBounds : region.Coords);

            bool registered = Region.Regions.Contains(region);

            if (registered)
                Region.RemoveRegion(region);

            area.RemoveAt(index);

            if (registered)
                Region.AddRegion(region);
        }

        public static void FlashArea(Mobile from, List<Rectangle3D> area, Map map)
        {
            if (map == null || map == Map.Internal || map != from.Map)
                return;

            foreach (Rectangle3D rect3D in area)
            {
                Rectangle2D rect2D = new Rectangle2D(new Point2D(rect3D.Start), new Point2D(rect3D.End));

                FlashArea(from, rect2D, map);
            }
        }

        public static void FlashArea(Mobile from, Rectangle2D rect, Map map)
        {
            if (map == null || map == Map.Internal || map != from.Map || from.NetState == null)
                return;

            const int flashRange = 18;

            from.ProcessDelta();

            for (int y = from.Y - flashRange; y <= from.Y + flashRange; y++)
            {
                if (y < rect.Start.Y || y > rect.End.Y)
                    continue;

                for (int x = from.X - flashRange; x <= from.X + flashRange; x++)
                {
                    if (x < rect.Start.X || x > rect.End.X)
                        continue;

                    // 3/6/2024, Adam: this was fine most of the time, until we are in a cave, at which point the graphic paints out of view (on the mountain top)
                    // Point3D p = new Point3D(x, y, map.GetAverageZ(x, y));
                    // now we will just use the staff's Z
                    Point3D p = new Point3D(x, y, from.Z);

                    if (x < rect.End.X && y < rect.End.Y)
                        from.NetState.Send(new LocationEffect(new Point3D(p, p.Z + 1), 0x1ECD, 1, 75, 1152, 3)); // interior

                    if (x == rect.Start.X || y == rect.Start.Y || x == rect.End.X || y == rect.End.Y)
                        from.NetState.Send(new LocationEffect(p, 0x1124, 1, 75, 1152, 3)); // boundary
                }
            }
        }

        private class InternalState
        {
            public readonly Region Region;
            public readonly bool Inns;

            public InternalState(Region region, bool inns)
            {
                Region = region;
                Inns = inns;
            }
        }
    }
}