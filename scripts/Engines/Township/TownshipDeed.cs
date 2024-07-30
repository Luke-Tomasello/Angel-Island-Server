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

/* Scripts/Engines/Township/TownshipDeed.cs
 * CHANGELOG:
 *  7/2/23, Yoar
 *      New rule: You may only place township stones inside your guild's guildhouse
 *  7/2/23, Yoar
 *      Township placement fixes:
 *      1. Cannot place a township if your guild already owns one
 *      2. Cannot place a township if there are 0 houses in range
 *      3. Cannot place a township if the guild has no guildstone
 *      4. Can only place a township stone in the wilderness or inside a guild-owned house
 *  3/18/22, Adam
 *      Make GetHousesInRadius() public (used for calculating lockdowns)
 *	2/27/22, Yoar
 *		 Renamed + rewrote: GetPercentageOfGuildedHousesInArea -> CalculateHouseOwnership
 * 1/12/22, Yoar
 *		Township cleanups.
 * 7/23/08 Pix
 *		Guess I need more sleep still.  It'll only get worse. . . 
 * 7/23/08, Pix
 *		Added more null-checking in GetPercentageOfGuildedHousesInArea.
 *		Put in better logging of placement.
 * 7/23/08, Adam
 *		Don't assume that houses have an owner
 * 7/22/08, Pix
 *		Extended try/catch in GetPercentageOfGuildedHousesInArea method - need to track down what caused the nullreferenceexception
 * 7/20/08, Pix
 *		De-coupled township stones from houses.
 *	5/16/07, Pix
 *		Fixed overlap testing to include custom regions.
 *	4/22/07, Pix
 *		Now ignores Siege Tents in the ownership percentage check for placement.
 *  4/21/07, Adam
 *      Added time-to-place logging
 *	3/20/07, Pix
 *		Added InitialFunds dial.
 *	3/19/07, Pix
 *		Added confirmation gump.
 */

using Server.Diagnostics;			// log helper
using Server.Guilds;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Regions;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class TownshipDeed : Item
    {
        [Constructable]
        public TownshipDeed()
            : base(0x14F0)
        {
            Weight = 12.0;
            LootType = LootType.Blessed;
            Name = "a township deed";
            this.Hue = Township.TownshipSettings.Hue;
        }

        public TownshipDeed(Serial serial)
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
            UseDeed(from, from.Location, from.Map, false);
        }

        public void UseDeed(Mobile from, Point3D loc, Map map, bool hasConfirmed)
        {
            try
            {
                UseDeedInternal(from, loc, map, hasConfirmed);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private void UseDeedInternal(Mobile from, Point3D loc, Map map, bool hasConfirmed)
        {
            Guild guild = from.Guild as Guild;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (guild == null)
            {
                from.SendMessage("You must be a member of a guild to use this.");
            }
            else if (guild.TownshipStone != null && !guild.TownshipStone.Deleted)
            {
                from.SendMessage("Your guild already owns a township.");
            }
            else
            {
                Utility.TimeCheck tc = new Utility.TimeCheck();

                tc.Start();

                PlacementResult result = CheckPlacement(loc, map, guild, TownshipStone.INITIAL_RADIUS);

                tc.End();

                //from.SendMessage(String.Format("Stone placement check took {0}", tc.TimeTaken));

                LogHelper logger = new LogHelper("TownshipPlacementTime.log", false);
                logger.Log(LogType.Mobile, from, String.Format("Stone placement check at {0} ({1}) took {2}", loc, map, tc.TimeTaken));
                logger.Finish();

                if (result != PlacementResult.Success)
                {
                    ProcessResult(from, result);
                }
                else if (!hasConfirmed)
                {
                    from.CloseGump(typeof(ConfirmPlacementGump));
                    from.SendGump(new ConfirmPlacementGump(this, loc, map));
                }
                else
                {
                    tc = new Utility.TimeCheck();

                    tc.Start();

                    TownshipStone ts = new TownshipStone(guild);

                    ts.GoldHeld = Township.TownshipSettings.InitialFunds; //initial gold :-)
                    ts.MoveToWorld(loc, map);
                    ts.TownshipCenter = loc;
                    ts.CustomRegion.Registered = true;

                    from.SendMessage("The township has been created.");

                    Delete();

                    tc.End();

                    //from.SendMessage(String.Format("Stone placement took {0}", tc.TimeTaken));

                    logger = new LogHelper("TownshipPlacementTime.log", false);
                    logger.Log(LogType.Mobile, from, String.Format("Stone placement ACTUAL at {0} ({1}) took {2}", loc, map, tc.TimeTaken));
                    logger.Finish();
                }
            }
        }

        public enum PlacementResult
        {
            InvalidLocation,
            InvalidMap,
            InvalidGuild,
            NotInsideGuildhouse,
            NotInsideOwnedHouse,
            InsideUnownedHouse,
            RegionConflict,
            NotEnoughHouseOwnership,
            Success,
        }

        public enum HouseRequirementType
        {
            Guildhouse,
            OwnedHouse,
            OwnedHouseOrOutside,
        }

        public static PlacementResult CheckPlacement(Point3D loc, Map map, Guild guild, int radius, HouseRequirementType houseRequirement = HouseRequirementType.Guildhouse)
        {
            if (loc == Point3D.Zero)
                return PlacementResult.InvalidLocation;

            if (map == null || map == Map.Internal)
                return PlacementResult.InvalidMap;

            if (guild == null)
                return PlacementResult.InvalidGuild;

            switch (houseRequirement)
            {
                case HouseRequirementType.Guildhouse:
                    {
                        BaseHouse house = BaseHouse.FindHouseAt(loc, map, 16);

                        if (house == null || !house.Contains(guild.Guildstone))
                            return PlacementResult.NotInsideGuildhouse;

                        break;
                    }
                case HouseRequirementType.OwnedHouse:
                    {
                        BaseHouse house = BaseHouse.FindHouseAt(loc, map, 16);

                        if (house == null || house.Owner == null || !guild.IsMember(house.Owner))
                            return PlacementResult.NotInsideOwnedHouse;

                        break;
                    }
                case HouseRequirementType.OwnedHouseOrOutside:
                    {
                        BaseHouse house = BaseHouse.FindHouseAt(loc, map, 16);

                        if (house != null && (house.Owner == null || !guild.IsMember(house.Owner)))
                            return PlacementResult.InsideUnownedHouse;

                        break;
                    }
            }

            Region ignore = null;

            // if we already have a township, ignore our current township region
            if (guild.TownshipStone != null && !guild.TownshipStone.Deleted && guild.TownshipStone is TownshipStone)
                ignore = ((TownshipStone)guild.TownshipStone).CustomRegion;

            if (HasConflictingRegion(loc, map, radius, ignore))
                return PlacementResult.RegionConflict;

            if (CalculateHouseOwnership(loc, map, radius, guild, true) < Township.TownshipSettings.GuildHousePercentage)
                return PlacementResult.NotEnoughHouseOwnership;

            return PlacementResult.Success;
        }

        public static void ProcessResult(Mobile from, PlacementResult result)
        {
            switch (result)
            {
                case PlacementResult.NotInsideGuildhouse:
                    from.SendMessage("You can only build this inside your guildhouse.");
                    break;
                case PlacementResult.NotInsideOwnedHouse:
                    from.SendMessage("You can only build this inside a house owned by your guild.");
                    break;
                case PlacementResult.InsideUnownedHouse:
                    from.SendMessage("You can only build this outdoors or inside a house owned by your guild.");
                    break;
                case PlacementResult.RegionConflict:
                    from.SendMessage("You can't create a township that conflicts with another township, guardzone, or other special area.");
                    break;
                case PlacementResult.NotEnoughHouseOwnership:
                    from.SendMessage("You can't create a township without owning most of the houses in the area.");
                    break;
                case PlacementResult.Success:
                    break; // no message
                default:
                    from.SendMessage("Something went wrong!");
                    break;
            }
        }

        public static double CalculateHouseOwnership(Point3D loc, Map map, int radius, Guild guild, bool allied)
        {
            try
            {
                if (loc == Point3D.Zero || map == null || map == Map.Internal || guild == null)
                    return 0.0;

                int ownedCount = 0;
                int totalCount = 0;

                foreach (BaseHouse h in GetHousesInRadius(loc, map, radius))
                {
                    if (h is SiegeTent || h.Owner == null)
                        continue; // ignore completely

                    Guild houseGuild = h.Owner.Guild as Guild;

                    if (houseGuild != null && (guild == houseGuild || (allied && guild.IsAlly(houseGuild))))
                        ownedCount++;

                    totalCount++;
                }

                if (totalCount == 0)
                    return 0.0;

                return (double)ownedCount / totalCount;
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            return 0.0;
        }

        public static bool HasConflictingRegion(Point3D loc, Map map, int radius, Region ignore)
        {
            try
            {
                if (loc == Point3D.Zero || map == null || map == Map.Internal)
                    return true;

                foreach (Region r in GetRegionsInRadius(loc, map, radius))
                {
                    if ((ignore == null || r != ignore) && r is GuardedRegion)
                        return true;
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            return false;
        }

        public static List<BaseHouse> GetHousesInRadius(Point3D loc, Map map, int radius)
        {
            List<BaseHouse> list = new List<BaseHouse>();

            Rectangle2D rect = new Rectangle2D(new Point2D(loc.X - radius, loc.Y - radius), new Point2D(loc.X + radius, loc.Y + radius));

            for (int x = rect.Start.X; x < rect.End.X; x++)
            {
                for (int y = rect.Start.Y; y < rect.End.Y; y++)
                {
                    HouseRegion r = Region.Find(new Point3D(x, y, loc.Z), map) as HouseRegion;

                    if (r != null && r.House != null && !list.Contains(r.House))
                        list.Add(r.House);
                }
            }

            return list;
        }

        private static List<Region> GetRegionsInRadius(Point3D loc, Map map, int radius)
        {
            List<Region> list = new List<Region>();

            Rectangle2D rect = new Rectangle2D(new Point2D(loc.X - radius, loc.Y - radius), new Point2D(loc.X + radius, loc.Y + radius));

            for (int x = rect.Start.X; x < rect.End.X; x++)
            {
                for (int y = rect.Start.Y; y < rect.End.Y; y++)
                {
                    Region r = Region.Find(new Point3D(x, y, loc.Z), map);

                    if (r != null && !list.Contains(r))
                        list.Add(r);
                }
            }

            return list;
        }

        private class ConfirmPlacementGump : Gump
        {
            private TownshipDeed m_Deed;
            private Point3D m_Location;
            private Map m_Map;

            public ConfirmPlacementGump(TownshipDeed deed, Point3D loc, Map map)
                : base(50, 50)
            {
                m_Deed = deed;
                m_Location = loc;
                m_Map = map;

                AddPage(0);

                AddBackground(10, 10, 190, 140, 0x242C);

                AddHtml(30, 30, 150, 75, String.Format("<div align=CENTER>{0}</div>", "The township can be placed here.  Continue?"), false, false);

                AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (info.ButtonID == 1)
                    m_Deed.UseDeed(state.Mobile, m_Location, m_Map, true);
            }
        }
    }
}