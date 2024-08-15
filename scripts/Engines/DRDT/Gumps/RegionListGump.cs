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

/* Scripts/Engines/DRDT/Gumps/RegionListGump.cs
 *  1/14/23, Yoar
 *      Initial commit.
 */

using Server.Items;
using Server.Network;
using Server.Regions;
using System;
using System.Collections.Generic;

namespace Server.Gumps
{
    public class RegionListGump : Gump
    {
        public static void Initialize()
        {
            CommandSystem.Register("StaticRegions", AccessLevel.GameMaster, new CommandEventHandler(StaticRegions_OnCommand));
            CommandSystem.Register("CustomRegions", AccessLevel.GameMaster, new CommandEventHandler(CustomRegions_OnCommand));
        }

        [Usage("StaticRegions")]
        [Description("Displays a list of all static regions in the Region database.")]
        private static void StaticRegions_OnCommand(CommandEventArgs e)
        {
            List<StaticRegion> list = new List<StaticRegion>();

            foreach (Region region in Region.Regions)
            {
                if (region.GetType() == typeof(StaticRegion))
                    list.Add((StaticRegion)region);
            }

            list.Sort(RegionComparer.Instance);

            e.Mobile.SendGump(new RegionListGump(list, "Static Regions"));
        }

        [Usage("CustomRegions")]
        [Description("Displays a list of all custom regions in the Region database.")]
        private static void CustomRegions_OnCommand(CommandEventArgs e)
        {
            List<StaticRegion> list = new List<StaticRegion>();

            foreach (Region region in Region.Regions)
            {
                if (region is CustomRegion)
                    list.Add((CustomRegion)region);
            }

            list.Sort(RegionComparer.Instance);

            e.Mobile.SendGump(new RegionListGump(list, "Custom Regions"));
        }

        private class RegionComparer : IComparer<Region>
        {
            public static readonly RegionComparer Instance = new RegionComparer();

            private RegionComparer()
            {
            }

            int IComparer<Region>.Compare(Region x, Region y)
            {
                if (x.Name == null && y.Name == null)
                    return 0;
                else if (x.Name == null && y.Name != null)
                    return -1;
                else if (x.Name != null && y.Name == null)
                    return +1;
                else
                    return x.Name.CompareTo(y.Name);
            }
        }

        private const int Lines = 10;
        private const int ButtonOffset = 100;

        private IList<StaticRegion> m_List;
        private string m_Title;
        private int m_ControlIndex;

        public RegionListGump(IList<StaticRegion> list)
            : this(list, "Region List", 0, -1)
        {
        }

        public RegionListGump(IList<StaticRegion> list, string title)
            : this(list, title, 0, -1)
        {
        }

        private RegionListGump(IList<StaticRegion> list, string title, int lastIndex, int controlIndex)
            : base(50, 50)
        {
            m_List = list;
            m_Title = title;
            m_ControlIndex = controlIndex;

            int pages = (m_List.Count + Lines - 1) / Lines;

            int lastPage = 1 + lastIndex / Lines;
            int pageIndex = 1 - lastPage;
            int page = 1 + Modulo(pageIndex, pages);

            AddPage(0);

            AddBackground(0, 0, 600, 20 + (Lines + 2) * 25, 9270);
            AddAlphaRegion(0, 0, 600, 20 + (Lines + 2) * 25);

            AddHtml(10, 10, 580, 20, string.Format("<BASEFONT COLOR=#FFFFFF><CENTER>{0}</CENTER></BASEFONT>", title), false, false);

            AddHtml(10, 10, 50, 20, "<BASEFONT COLOR=#FFFFFF>Region</BASEFONT>", false, false);
            AddHtml(60, 10, 50, 20, "<BASEFONT COLOR=#FFFFFF>Control</BASEFONT>", false, false);
            AddHtml(110, 10, 50, 20, "<BASEFONT COLOR=#FFFFFF>Props</BASEFONT>", false, false);

            AddPage(page);

            string html = string.Format("<BASEFONT COLOR=#FFFFFF><CENTER>Showing {0}-{1} of {2}, Page {3} of {4}</CENTER></BASEFONT>",
                1,
                Math.Min(m_List.Count, Lines),
                m_List.Count,
                1 + Modulo(lastPage + page - 2, pages),
                pages);
            AddHtml(10, 10 + (Lines + 1) * 25, 580, 20, html, false, false);

            const int y0 = 35;
            int y = y0;

            for (int i = 0; i < list.Count; i++, y += 25)
            {
                if (i != 0 && (i % Lines) == 0)
                {
                    y = y0;

                    int nextPage = 1 + Modulo(++pageIndex, pages);

                    AddButton(560, 10 + (Lines + 1) * 25, 4005, 4007, 0, GumpButtonType.Page, nextPage);
                    AddPage(nextPage);
                    AddButton(10, 10 + (Lines + 1) * 25, 4014, 4016, 1, GumpButtonType.Page, page);

                    page = nextPage;

                    html = string.Format("<BASEFONT COLOR=#FFFFFF><CENTER>Showing {0}-{1} of {2}, Page {3} of {4}</CENTER></BASEFONT>",
                        i + 1,
                        Math.Min(m_List.Count, i + Lines),
                        m_List.Count,
                        1 + Modulo(lastPage + page - 2, pages),
                        pages);
                    AddHtml(10, 10 + (Lines + 1) * 25, 580, 20, html, false, false);
                }

                StaticRegion sr = m_List[i];

                int buttonOffset = (i + 1) * ButtonOffset;

                AddButton(10, y, 4005, 4007, buttonOffset + 0, GumpButtonType.Reply, 0);
                AddButton(60, y, 4005, 4007, buttonOffset + 1, GumpButtonType.Reply, 0);
                AddButton(110, y, 4011, 4013, buttonOffset + 2, GumpButtonType.Reply, 0);
                AddHtml(160, y, 430, 20, string.Format("<BASEFONT COLOR=#FFFFFF>{0} ({1})</BASEFONT>", sr.Name, sr.Map), false, false);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            int index = (info.ButtonID / ButtonOffset) - 1;
            int type = (info.ButtonID % ButtonOffset);

            if (index >= 0 && index < m_List.Count)
            {
                StaticRegion sr = m_List[index];

                switch (type)
                {
                    case 0: // Go to region
                        {
                            Point3D loc;
                            Map map;

                            if (GetRegionLoc(sr, out loc, out map))
                                sender.Mobile.MoveToWorld(loc, map);

                            sender.Mobile.SendGump(new RegionListGump(m_List, m_Title, index, -1));

                            break;
                        }
                    case 1: // Go to controller
                        {
                            Point3D loc;
                            Map map;
                            int count;

                            if (GetControlLoc(sr, ref m_ControlIndex, out count, out loc, out map))
                            {
                                if (count > 1)
                                    sender.Mobile.SendMessage("Controller {0}/{1}", m_ControlIndex + 1, count);

                                sender.Mobile.MoveToWorld(loc, map);
                            }

                            sender.Mobile.SendGump(new RegionListGump(m_List, m_Title, index, m_ControlIndex));

                            break;
                        }
                    case 2: // View props
                        {
                            sender.Mobile.SendGump(new RegionListGump(m_List, m_Title, index, -1));

                            sender.Mobile.SendGump(new PropertiesGump(sender.Mobile, sr));

                            break;
                        }
                }
            }
        }

        private static bool GetRegionLoc(Region region, out Point3D loc, out Map map)
        {
            loc = Point3D.Zero;
            map = region.Map;

            if (map == null)
                return false;

            if (region.GoLocation != Point3D.Zero)
            {
                loc = region.GoLocation;

                return true;
            }

            if (region.Coords.Count != 0)
            {
                loc = region.Coords[0].Start;

                loc.Z = Math.Max(loc.Z, map.GetAverageZ(loc.X, loc.Y));

                return true;
            }

            return false;
        }

        private static bool GetControlLoc(StaticRegion sr, ref int index, out int count, out Point3D loc, out Map map)
        {
            loc = Point3D.Zero;
            map = sr.Map;
            count = 1;

            if (sr is CustomRegion)
            {
                CustomRegionControl crc = ((CustomRegion)sr).Controller;

                if (crc.Map == null)
                    return false;

                loc = crc.GetWorldLocation();
                map = crc.Map;

                return true;
            }

            List<StaticRegionControl> list = new List<StaticRegionControl>();

            foreach (StaticRegionControl instance in StaticRegionControl.Instances)
            {
                if (instance.StaticRegion == sr)
                    list.Add(instance);
            }

            count = list.Count;

            if (count == 0)
                return false;

            index = Modulo(index + 1, list.Count);

            StaticRegionControl src = list[index];

            loc = src.GetWorldLocation();
            map = src.Map;

            return true;
        }

        private static int Modulo(int a, int b)
        {
            return ((a % b) + b) % b;
        }
    }
}