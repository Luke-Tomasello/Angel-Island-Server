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

/* Scripts/Commands/CheckLOS.cs
 * ChangeLog
 *  1/3/22, Adam
 *      added CanSee to the LOS check.
 *      Now you get messages for each test individually.
 *	4/28/08, Adam
 *		First time checkin
 */


using Server.Diagnostics;
using Server.Items;
using Server.Misc;                      // TestCenter
using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.Commands
{
    public class CheckLOSCommand
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("CheckLOS", AccessLevel.Player, new CommandEventHandler(CheckLOS_OnCommand));
        }

        public static void CheckLOS_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel == AccessLevel.Player && TestCenter.Enabled == false)
            {   // Players can only test this on Test Center
                e.Mobile.SendMessage("Not available here.");
                return;
            }

            //if (e.Mobile.AccessLevel > AccessLevel.Player)
            //{   // you will not get good results if you test this with AccessLevel > Player
            //    e.Mobile.SendMessage("You should test this with AccessLevel.Player.");
            //    return;
            //}

            try
            {
                e.Mobile.Target = new LOSTarget();
                e.Mobile.SendMessage("Check LOS to which object?");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

        }

        private class LOSTarget : Target
        {
            public LOSTarget()
                : base(15, false, TargetFlags.None)
            {
                CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                LogHelper logger = new LogHelper("teleport.log", true, true, true);
                bool los = LineOfSight(logger, from, from.Map, from.Map.GetPoint(from, true), from.Map.GetPoint(targ, false));
                logger.Finish();
                from.SendMessage("You {0} see that(LOS).", los ? "can" : "cannot");
                from.SendMessage("You {0} see that(CanSee).", from.CanSee(targ) ? "can" : "cannot");
                return;
            }
            // Copy of LineOfSight from map.cs for testing purposes.
            private bool LineOfSight(LogHelper logger, Mobile from, Map map, Point3D org, Point3D dest)
            {
                if (!Utility.InRange(org, dest, Map.MaxLOSDistance))
                    return false;

                // DEBUG
                logger.Log(string.Format("Origin: {0}, Destination: {1}", org.ToString(), dest.ToString()));

                Point3D start = org;
                Point3D end = dest;

                if (org.X > dest.X || (org.X == dest.X && org.Y > dest.Y) || (org.X == dest.X && org.Y == dest.Y && org.Z > dest.Z))
                {
                    Point3D swap = org;
                    org = dest;
                    dest = swap;

                    // DEBUG
                    //logger.Log(string.Format("Swap: {0}, Destination: {1}", org.ToString(), dest.ToString()));
                }

                double rise, run, zslp;
                double sq3d;
                double x, y, z;
                int xd, yd, zd;
                int ix, iy, iz;
                int height;
                bool found;
                Point3D p;
                Point3DList path = new Point3DList();
                TileFlag flags;

                if (org == dest)
                    return true;

                if (path.Count > 0)
                    path.Clear();

                xd = dest.m_X - org.m_X;
                yd = dest.m_Y - org.m_Y;
                zd = dest.m_Z - org.m_Z;
                zslp = Math.Sqrt(xd * xd + yd * yd);
                if (zd != 0)
                    sq3d = Math.Sqrt(zslp * zslp + zd * zd);
                else
                    sq3d = zslp;

                rise = ((float)yd) / sq3d;
                run = ((float)xd) / sq3d;
                zslp = ((float)zd) / sq3d;

                y = org.m_Y;
                z = org.m_Z;
                x = org.m_X;
                while (Utility.NumberBetween(x, dest.m_X, org.m_X, 0.5) && Utility.NumberBetween(y, dest.m_Y, org.m_Y, 0.5) && Utility.NumberBetween(z, dest.m_Z, org.m_Z, 0.5))
                {
                    ix = (int)Math.Round(x);
                    iy = (int)Math.Round(y);
                    iz = (int)Math.Round(z);
                    if (path.Count > 0)
                    {
                        p = path.Last;

                        if (p.m_X != ix || p.m_Y != iy || p.m_Z != iz)
                            path.Add(ix, iy, iz);
                    }
                    else
                    {
                        path.Add(ix, iy, iz);
                    }
                    x += run;
                    y += rise;
                    z += zslp;
                }

                if (path.Count == 0)
                    return true;//<--should never happen, but to be safe.

                p = path.Last;

                if (p != dest)
                    path.Add(dest);

                #region DEBUG
                PlayerMobile pm = from as PlayerMobile;
                if (pm != null)
                {
                    pm.JumpIndex = 0;
                    pm.JumpList = new System.Collections.ArrayList(0);
                    for (int jx = 0; jx < path.Count; jx++)
                    {
                        logger.Log(path[jx].ToString());
                        pm.JumpList.Add(path[jx]);
                    }
                }
                #endregion DEBUG

                Point3D pTop = org, pBottom = dest;
                Utility.FixPoints(ref pTop, ref pBottom);

                int pathCount = path.Count;
                int endTop = end.m_Z + 1;

                for (int i = 0; i < pathCount; ++i)
                {
                    Point3D point = path[i];
                    int pointTop = point.m_Z + 1;

                    LandTile landTile = map.Tiles.GetLandTile(point.X, point.Y);
                    int landZ = 0, landAvg = 0, landTop = 0;
                    map.GetAverageZ(point.m_X, point.m_Y, ref landZ, ref landAvg, ref landTop);

                    if (landZ <= pointTop && landTop >= point.m_Z && (point.m_X != end.m_X || point.m_Y != end.m_Y || landZ > endTop || landTop < end.m_Z) && !landTile.Ignored)
                        return false;

                    /* --Do land tiles need to be checked?  There is never land between two people, always statics.--
                    LandTile landTile = Tiles.GetLandTile( point.X, point.Y );
                    if ( landTile.Z-1 >= point.Z && landTile.Z+1 <= point.Z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Impassable) != 0 )
                        return false;
                    */

                    StaticTile[] statics = map.Tiles.GetStaticTiles(point.m_X, point.m_Y, true);

                    bool contains = false;
                    int ltID = landTile.ID;

                    for (int j = 0; !contains && j < Map.InvalidLandTiles.Length; ++j)
                        contains = (ltID == Map.InvalidLandTiles[j]);

                    if (contains && statics.Length == 0)
                    {
                        IPooledEnumerable<Item> eable = map.GetItemsInRange(point, 0);

                        foreach (Item item in eable)
                        {
                            if (item.Visible)
                                contains = false;

                            if (!contains)
                                break;
                        }

                        eable.Free();

                        if (contains)
                            return false;
                    }

                    for (int j = 0; j < statics.Length; ++j)
                    {
                        StaticTile t = statics[j];

                        ItemData id = TileData.ItemTable[t.ID & TileData.MaxItemValue];

                        flags = id.Flags;
                        height = id.CalcHeight;

                        if (t.Z <= pointTop && t.Z + height >= point.Z && (flags & (TileFlag.Window | TileFlag.NoShoot)) != 0)
                        {
                            if (point.m_X == end.m_X && point.m_Y == end.m_Y && t.Z <= endTop && t.Z + height >= end.m_Z)
                                continue;

                            return false;
                        }

                        /*if ( t.Z <= point.Z && t.Z+height >= point.Z && (flags&TileFlag.Window)==0 && (flags&TileFlag.NoShoot)!=0
                            && ( (flags&TileFlag.Wall)!=0 || (flags&TileFlag.Roof)!=0 || (((flags&TileFlag.Surface)!=0 && zd != 0)) ) )*/
                        /*{
                            //Console.WriteLine( "LoS: Blocked by Static \"{0}\" Z:{1} T:{3} P:{2} F:x{4:X}", TileData.ItemTable[t.ID&TileData.MaxItemValue].Name, t.Z, point, t.Z+height, flags );
                            //Console.WriteLine( "if ( {0} && {1} && {2} && ( {3} || {4} || {5} || ({6} && {7} && {8}) ) )", t.Z <= point.Z, t.Z+height >= point.Z, (flags&TileFlag.Window)==0, (flags&TileFlag.Impassable)!=0, (flags&TileFlag.Wall)!=0, (flags&TileFlag.Roof)!=0, (flags&TileFlag.Surface)!=0, t.Z != dest.Z, zd != 0 ) ;
                            return false;
                        }*/
                    }
                }

                Rectangle2D rect = new Rectangle2D(pTop.m_X, pTop.m_Y, (pBottom.m_X - pTop.m_X) + 1, (pBottom.m_Y - pTop.m_Y) + 1);

                IPooledEnumerable<Item> area = map.GetItemsInBounds(rect);

                foreach (Item i in area)
                {
                    if (!i.Visible)
                        continue;

                    if (i is BaseMulti || i.ItemID > TileData.MaxItemValue)
                        continue;

                    ItemData id = i.ItemData;
                    flags = id.Flags;

                    if ((flags & (TileFlag.Window | TileFlag.NoShoot)) == 0)
                        continue;

                    height = id.CalcHeight;

                    found = false;

                    int count = path.Count;

                    for (int j = 0; j < count; ++j)
                    {
                        Point3D point = path[j];
                        int pointTop = point.m_Z + 1;
                        Point3D loc = i.Location;

                        //if ( t.Z <= point.Z && t.Z+height >= point.Z && ( height != 0 || ( t.Z == dest.Z && zd != 0 ) ) )
                        if (loc.m_X == point.m_X && loc.m_Y == point.m_Y &&
                            loc.m_Z <= pointTop && loc.m_Z + height >= point.m_Z)
                        {
                            if (loc.m_X == end.m_X && loc.m_Y == end.m_Y && loc.m_Z <= endTop && loc.m_Z + height >= end.m_Z)
                                continue;

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        continue;

                    area.Free();
                    return false;

                    /*if ( (flags & (TileFlag.Impassable | TileFlag.Surface | TileFlag.Roof)) != 0 )

                    //flags = TileData.ItemTable[i.ItemID&TileData.MaxItemValue].Flags;
                    //if ( (flags&TileFlag.Window)==0 && (flags&TileFlag.NoShoot)!=0 && ( (flags&TileFlag.Wall)!=0 || (flags&TileFlag.Roof)!=0 || (((flags&TileFlag.Surface)!=0 && zd != 0)) ) )
                    {
                        //height = TileData.ItemTable[i.ItemID&TileData.MaxItemValue].Height;
                        //Console.WriteLine( "LoS: Blocked by ITEM \"{0}\" P:{1} T:{2} F:x{3:X}", TileData.ItemTable[i.ItemID&TileData.MaxItemValue].Name, i.Location, i.Location.Z+height, flags );
                        area.Free();
                        return false;
                    }*/
                }

                area.Free();

                return true;
            }
        }

    }
}