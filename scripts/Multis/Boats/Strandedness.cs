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

/* Scripts/Multis/Boats/Strandedness.cs
 * CHANGELOG 
 *  9/4/22, Yoar (WorldZone)
 *      Added support for world zone strandedness locations.
 *	4/20/10, adam
 *		Add ProcessStranded changes to allow ProcessStranded calls across maps, i.e., booting players from other maps like CTF
 *		1. Added a ProcessStranded call that takes and explicit map
 *		2. Have ProcessStranded MoveToWorld if the mobiles current map is not the internal map 
 *	06/28/09, plasma
 *		Prevent staff being kicked from capture area :) 
 *	06/07/09, plasma
 *		Added check to boot non iob chars from capture zones
 *  4/4/07, Adam
 *      Remove CheckMobiles from the CanFit flags in strandness as 'you' are there during the check, and the test will fail.
 *      This was probably working before, but the recent redesign of the CanFit() interface borked it up.
 *  03/25/07, plasma
 *      - Seperated main logic from the eventsink into its own method
 *      - Added bool to new method that allows a new location to be found reagrdless of if the mobile is stranded or not (auto kick for pirating)
 *  02/10/07, Pix
 *		Now checks for houses/houseregions in CheckLegalBounce.
 *	02/28/06, weaver
 *		Added CheckLegalBounce() routine to handle bounce exclusion zones.
 *	11/17/04, lego eater
 *		commited out coords that sent stranded to AI coords are  (232, 752)
 *
 */

using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections.Generic;

namespace Server.Misc
{
    public class Strandedness
    {
        // wea: added area exclusion definition
        private static Rectangle2D[] m_Exclusion = new Rectangle2D[]
            {
                new Rectangle2D(new Point2D(4880,   0), new Point2D(6501, 4201)), // T2A, dungeons, green acres
                new Rectangle2D(new Point2D( 150, 700), new Point2D( 451,  901)), // Angel Island
            };

        private static Point2D[] m_Felucca = new Point2D[]
            {
                new Point2D( 2528, 3568 ), new Point2D( 2376, 3400 ), new Point2D( 2528, 3896 ),
                new Point2D( 2168, 3904 ), new Point2D( 1136, 3416 ), new Point2D( 1432, 3648 ),
                new Point2D( 1416, 4000 ), new Point2D( 4512, 3936 ), new Point2D( 4440, 3120 ),
                new Point2D( 4192, 3672 ), new Point2D( 4720, 3472 ), new Point2D( 3744, 2768 ),
                new Point2D( 3480, 2432 ), new Point2D( 3560, 2136 ), new Point2D( 3792, 2112 ),
                new Point2D( 2800, 2296 ), new Point2D( 2736, 2016 ), new Point2D( 4576, 1456 ),
                new Point2D( 4680, 1152 ), new Point2D( 4304, 1104 ), new Point2D( 4496, 984 ),
                new Point2D( 4248, 696 ), new Point2D( 4040, 616 ), new Point2D( 3896, 248 ),
                new Point2D( 4176, 384 ), new Point2D( 3672, 1104 ), new Point2D( 3520, 1152 ),
                new Point2D( 3720, 1360 ), new Point2D( 2184, 2152 ), new Point2D( 1952, 2088 ),
                new Point2D( 2056, 1936 ), new Point2D( 1720, 1992 ), new Point2D( 472, 2064 ),
                new Point2D( 656, 2096 ), new Point2D( 3008, 3592 ), new Point2D( 2784, 3472 ),
                new Point2D( 5456, 2400 ), new Point2D( 5976, 2424 ), new Point2D( 5328, 3112 ),
                new Point2D( 5792, 3152 ), new Point2D( 2120, 3616 ), new Point2D( 2136, 3128 ),
                new Point2D( 1632, 3528 ), new Point2D( 1328, 3160 ), new Point2D( 1072, 3136 ),
                new Point2D( 1128, 2976 ), new Point2D( 960, 2576 ), new Point2D( 752, 1832 ),
                new Point2D( 184, 1488 ), new Point2D( 592, 1440 ), new Point2D( 368, 1216 ),
                new Point2D( 232, 752 ), new Point2D( 696, 744 ), new Point2D( 304, 1000 ),
                new Point2D( 840, 376 ), new Point2D( 1192, 624 ), new Point2D( 1200, 192 ),
                new Point2D( 1512, 240 ), new Point2D( 1336, 456 ), new Point2D( 1536, 648 ),
                new Point2D( 1104, 952 ), new Point2D( 1864, 264 ), new Point2D( 2136, 200 ),
                new Point2D( 2160, 528 ), new Point2D( 1904, 512 ), new Point2D( 2240, 784 ),
                new Point2D( 2536, 776 ), new Point2D( 2488, 216 ), new Point2D( 2336, 72 ),
                new Point2D( 2648, 288 ), new Point2D( 2680, 576 ), new Point2D( 2896, 88 ),
                new Point2D( 2840, 344 ), new Point2D( 3136, 72 ), new Point2D( 2968, 520 ),
                new Point2D( 3192, 328 ), new Point2D( 3448, 208 ), new Point2D( 3432, 608 ),
                new Point2D( 3184, 752 ), new Point2D( 2800, 704 ), new Point2D( 2768, 1016 ),
                new Point2D( 2448, 1232 ), new Point2D( 2272, 920 ), new Point2D( 2072, 1080 ),
                new Point2D( 2048, 1264 ), new Point2D( 1808, 1528 ), new Point2D( 1496, 1880 ),
                new Point2D( 1656, 2168 ), new Point2D( 2096, 2320 ), new Point2D( 1816, 2528 ),
                new Point2D( 1840, 2640 ), new Point2D( 1928, 2952 ), new Point2D( 2120, 2712 )
            };

        private static Point2D[] m_Trammel = m_Felucca;

        private static Point2D[] m_Ilshenar = new Point2D[]
            {
                new Point2D( 1252, 1180 ), new Point2D( 1562, 1090 ), new Point2D( 1444, 1016 ),
                new Point2D( 1324, 968 ), new Point2D( 1418, 806 ), new Point2D( 1722, 874 ),
                new Point2D( 1456, 684 ), new Point2D( 1036, 866 ), new Point2D( 612, 476 ),
                new Point2D( 1476, 372 ), new Point2D( 762, 472 ), new Point2D( 812, 1162 ),
                new Point2D( 1422, 1144 ), new Point2D( 1254, 1066 ), new Point2D( 1598, 870 ),
                new Point2D( 1358, 866 ), new Point2D( 510, 302 ), new Point2D( 510, 392 )
            };

        public static Point2D[] GetLocations(Mobile m)
        {
            #region World Zone
            if (WorldZone.IsInside(m.Location, m.Map))
                return WorldZone.ActiveZone.StrandednessLocations;
            #endregion

            if (m.Map == Map.Felucca)
                return m_Felucca;
            else if (m.Map == Map.Trammel)
                return m_Trammel;
            else if (m.Map == Map.Ilshenar)
                return m_Ilshenar;
            else
                return new Point2D[0];
        }

        public static void Initialize()
        {
            EventSink.Login += new LoginEventHandler(EventSink_Login);
        }

        // wea: added CheckLegalBounce() functions to handle destination check for secure location exclusion
        public static bool CheckLegalBounce(Point2D loc)
        {
            bool bLegal = true;

            try
            {
                foreach (Rectangle2D rect in m_Exclusion)
                {
                    if (rect.Contains(loc))
                    {
                        bLegal = false;
                        break;
                    }
                }

                //now make sure that it's not in a house
                if (bLegal)
                {
                    BaseHouse house = BaseHouse.FindHouseAt(new Point3D(loc, 0), Map.Felucca, 40);
                    if (house != null)
                    {
                        bLegal = false;
                    }
                }

                //also check for castle courtyards (yes the above check is contained in this one - I don't care)
                if (bLegal)
                {
                    Sector sector = Map.Felucca.GetSector(loc);
                    List<Region> list = sector.Regions;
                    for (int i = 0; i < list.Count; ++i)
                    {
                        Region region = (Region)list[i];

                        if (region.Contains(new Point3D(loc, 0)))
                        {
                            if (region is Regions.HouseRegion)
                            {
                                bLegal = false;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Server.Diagnostics.LogHelper.LogException(exception, "Exception with strandedness checks!");
                bLegal = false;
            }

            return bLegal;
        }

        /// <summary>
        /// Moves a stranded mobile.  If bCheckStranded == false then a new location is found regardless of the current location
        /// </summary>
        /// <param name="from"></param>
        /// <param name="bCheckCurrentLoc"></param>
        public static void ProcessStranded(Mobile from, bool bCheckStranded)
        {
            ProcessStranded(from, bCheckStranded, from.Map);
        }
        public static void ProcessStranded(Mobile from, bool bCheckStranded, Map map)
        {
            if (map == null)
                return;

            //pla: Change this to allow the code to get a new location even if the current location is good.
            //This is used to auto-kick ghosts from pirated ships!
            if (bCheckStranded && Utility.CanFit(map, from.X, from.Y, from.Z, 16, Utility.CanFitFlags.requireSurface))
                return;

            bool isStranded = false;

            LandTile landTile = map.Tiles.GetLandTile(from.X, from.Y);

            if ((landTile.ID >= 168 && landTile.ID <= 171) || (landTile.ID >= 310 && landTile.ID <= 311))
            {
                isStranded = true;
            }
            else
            {
                StaticTile[] tiles = map.Tiles.GetStaticTiles(from.X, from.Y);

                for (int i = 0; !isStranded && i < tiles.Length; ++i)
                {
                    StaticTile tile = tiles[i];

                    if (tile.ID >= 0x5797 && tile.ID <= 0x57B2)
                        isStranded = true;
                }
            }

            //pla: Force stranded to true for auto-kicks
            if (bCheckStranded == false)
                isStranded = true;

            if (!isStranded)
                return;

            Point2D[] list = GetLocations(from);

            if (list.Length == 0)
                return;

            Point2D p = Point2D.Zero;
            double pdist = double.MaxValue;

            for (int i = 0; i < list.Length; ++i)
            {
                // wea: added legal bounce check
                if (!CheckLegalBounce(list[i]))
                    continue;

                double dist = from.GetDistanceToSqrt(list[i]);

                if (dist < pdist)
                {
                    p = list[i];
                    pdist = dist;
                }
            }

            int x = p.X, y = p.Y;
            int z;
            bool canFit = false;

            z = map.GetAverageZ(x, y);
            canFit = map.CanSpawnLandMobile(x, y, z);

            for (int i = 1; !canFit && i <= 40; i += 2)
            {
                for (int xo = -1; !canFit && xo <= 1; ++xo)
                {
                    for (int yo = -1; !canFit && yo <= 1; ++yo)
                    {
                        if (xo == 0 && yo == 0)
                            continue;

                        x = p.X + (xo * i);
                        y = p.Y + (yo * i);
                        z = map.GetAverageZ(x, y);
                        canFit = map.CanSpawnLandMobile(x, y, z);
                    }
                }
            }

            if (canFit)
                if (from.Map == Map.Internal)
                    from.Location = new Point3D(x, y, z);
                else
                    from.MoveToWorld(new Point3D(x, y, z), map);

        }

        public static void EventSink_Login(LoginEventArgs e)
        {
            Mobile from = e.Mobile;

            #region IOB
            //pla: if this is a capture zone and they are not IOB then boot them
            if (from is PlayerMobile && ((PlayerMobile)from).IOBRealAlignment == IOBAlignment.None && ((PlayerMobile)from).AccessLevel == AccessLevel.Player)
            {
                Regions.StaticRegion sr = Regions.StaticRegion.FindStaticRegion(from);
                if (sr != null && sr.CaptureArea)
                {
                    ProcessStranded(from, false);
                    return;
                }
            }
            #endregion

            //pla: Call new function, and check current location as well
            ProcessStranded(from, true);

            return;

        }
    }
}