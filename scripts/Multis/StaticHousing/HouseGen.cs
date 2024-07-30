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

/* Scripts/Commands/HouseGen.cs
 *  Changelog:
 *  6/7/22, Yoar
 *      - Removed "raw capture" mode. Instead, choose between target or bounds picking.
 *      - Added EastSteps option: Exclude east steps from the plot size.
 *  6/5/22, Yoar
 *      - Added SouthSteps option: Exclude front steps from the plot size.
 *      - Replaced RemoveFoundations option by ClearDirt option. Use in combination with ZMin
 *        to clear house foundations.
 *  6/5/22, Yoar
 *      Added Test mode: Capture house without exporting it to XML.
 *  6/5/22, Yoar
 *      Rewrote gump
 *  6/4/22, Yoar
 *      Added support for separate static housing files, each file container a single design.
 *      These files can be found in the "StaticHousingProd" and "StaticHousingTest" folders.
 *  6/4/22, Yoar
 *      Split PickerCallback into:
 *      1. PickerCallback: Handles the [housegen command.
 *      2. Capture: Attempts to make a HouseBlueprint of whatever it captures within the bounds.
 *      3. Export: Appends the HouseBlueprint to the static housing file.
 *      * Change from earlier revisions: The Capture method now makes a HouseBlueprint for the
 *        house that is being captured. This blueprint is finally exported to XML. Writing the
 *        blueprint to XML happens entirely in HouseBlueprint.ToXML.
 *  6/3/22, Yoar
 *      Refactored PickerCallback completely
 *      Added support for house doors
 *	5/9/22, Adam
 *		Globally refactor (rename) Owner ==> OriginalOwner 
 *  4/30/22, Yoar
 *      Added additional/optional data which includes hue and name.
 *      Tiles additional data are added as addon components in the FixerAddon.
 *  9/17/21, Yoar
 *      Static housing revamp
 *  12/28/07 Taran Kain
 *      Added doubled-tile filter and flag
 *	7/8/07, Adam
 *		Add foundation stripping. there is still a bug here when we drop the structure
 *		a full 7 Z (tiles at 0 get clipped)
 *  6/25/07, Adam
 *      Major changes, please SR/MR for full details
 *  6/22/07, Adam
 *      Add BasePrice calculation function
 *  06/02/2007, plasma
 *      Initial creation, modified version of AddonGen.cs
 */

using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Multis;
using Server.Multis.StaticHousing;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using DoorEntry = Server.Multis.StaticHousing.StaticHouseHelper.DoorEntry;
using HouseBlueprint = Server.Multis.StaticHousing.StaticHouseHelper.HouseBlueprint;
using TileEntry = Server.Multis.StaticHousing.StaticHouseHelper.TileEntry;
using TileType = Server.Multis.StaticHousing.StaticHouseHelper.TileType;

namespace Server.Commands
{
    /// <summary>
    /// Based upon [AddonGen, this command creates house blueprints and saves them to the StaticHousing file
    /// </summary>
    public class HouseGenerator
    {
        public static void Initialize()
        {
            CommandSystem.Register("HouseGen", AccessLevel.Administrator, new CommandEventHandler(HouseGen_OnCommand));
        }

        [Usage("HouseGen")]
        [Description("Brings up the house script generator gump.")]
        private static void HouseGen_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            // because we must keep rubish 'test' captures from ending up on Production, we have TC and PROD loading different XML files.
            // on Test Center we load/modify StaticHousingTC.xml, on production we load (read only) StaticHousingProd.xml.
            // we limit this tool to Test Center to help enforce this rule.
            if (TestCenter.Enabled == false || Core.UOBETA_CFG == true)
            {
                from.SendMessage(0x22, "Error: You may only capture houses on Test Center.");
                return;
            }

            from.CloseGump(typeof(InternalGump));
            from.SendGump(new InternalGump(new InternalState()));
        }

        private static Mobile m_Mobile;
        private static HouseBlueprint m_Captured;

        private static void HouseGen_OnTarget(Mobile from, object targeted, object obj)
        {
            InternalState state = (InternalState)obj;

            bool success = false;

            HouseSign sign = targeted as HouseSign;

            if (sign == null)
            {
                if (targeted is StaticHouseSign)
                    from.SendMessage("That is not a valid house sign. It must be a real sign attached to a house.");
                else
                    from.SendMessage("That is not a house sign.");
            }
            else if (sign.Structure == null)
            {
                from.SendMessage("That sign is not attached to any house.");
            }
            else
            {
                BaseHouse house = sign.Structure;

                MultiComponentList mcl = house.Components;
                Rectangle2D bounds = new Rectangle2D(house.X + mcl.Min.X, house.Y + mcl.Min.Y, mcl.Width, mcl.Height);

                success = DoCapture(from, from.Map, bounds, house, state);
            }

            if (!success || state.Test)
            {
                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump(state));
            }
        }

        private static void HouseGen_OnBoundPicked(Mobile from, Map map, Point3D start, Point3D end, object obj)
        {
            InternalState state = (InternalState)obj;

            Rectangle2D bounds = new Rectangle2D(start, end);

            bool success = DoCapture(from, map, bounds, null, state);

            if (!success || state.Test)
            {
                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump(state));
            }
        }

        private static bool DoCapture(Mobile from, Map map, Rectangle2D bounds, BaseHouse house, InternalState state)
        {
            m_Mobile = from;

            bool success = Capture(map, bounds, null, state);

            m_Mobile = null;

            HouseBlueprint blueprint = m_Captured;

            if (success && blueprint != null)
            {
                if (state.Update)
                {
                    HouseBlueprint existing = StaticHouseHelper.GetBlueprint(blueprint.ID);

                    if (existing != null)
                    {
                        blueprint.Version = Math.Floor(existing.Version + 1.0);

                        if (String.IsNullOrEmpty(blueprint.Description))
                            blueprint.Description = existing.Description;

                        blueprint.OriginalOwnerName = existing.OriginalOwnerName;
                        blueprint.OriginalOwnerSerial = existing.OriginalOwnerSerial;
                        blueprint.OriginalOwnerAccount = existing.OriginalOwnerAccount;
                    }
                }

                if (state.Test)
                {
                    // give the GM a temp deed
                    from.SendMessage("A temporary static house deed has been placed in your backpack.");
                    from.AddToBackpack(new TempStaticDeed(blueprint));
                }
                else
                {
                    Export(blueprint);

                    // give the GM a legit deed
                    from.SendMessage("A house deed has been placed in your backpack.");
                    from.AddToBackpack(new StaticDeed(blueprint.ID));
                }
            }

            return success;
        }

        private static bool Capture(Map map, Rectangle2D bounds, BaseHouse house, InternalState state)
        {
            const bool statics = true; // we alwaye capture statics
            const bool doors = true; // we always capture doors

            if (map == null || map == Map.Internal)
            {
                Report(ReportType.Error, "Invalid map.");
                return false;
            }

            // ************ VALIDATE CAPTURE BOUNDS ************

            // we're in raw capture mode, help the user by snapping to the next valid plot size
            //	if the rect they selected is not a valid size
            if (house == null && !StaticHouseHelper.IsValidFoundationSize(bounds.Width, bounds.Height))
            {
                for (int i = 0; i < 18; i++)
                {
                    Rectangle2D snapX = new Rectangle2D(bounds.Start, new Point2D(bounds.End.X + i, bounds.End.Y));
                    Rectangle2D snapY = new Rectangle2D(bounds.Start, new Point2D(bounds.End.X, bounds.End.Y + i));

                    if (StaticHouseHelper.IsValidFoundationSize(snapX.Width, snapX.Height))
                    {
                        bounds = snapX;
                        Report(ReportType.Info, "Snapping to next legal X plot size: {0}x{1}.", snapX.Width, snapX.Height);
                        break;
                    }

                    if (StaticHouseHelper.IsValidFoundationSize(snapY.Width, snapY.Height))
                    {
                        bounds = snapY;
                        Report(ReportType.Info, "Snapping to next legal Y plot size: {0}x{1}.", snapX.Width, snapX.Height);
                        break;
                    }
                }
            }

            int plotWidth = bounds.Width;
            int plotHeight = bounds.Height;

            if (state.SouthSteps)
                plotHeight--;

            if (!StaticHouseHelper.IsValidFoundationSize(plotWidth, plotHeight))
            {
                Report(ReportType.Error, "Invalid house size: {0}x{1}.", plotWidth, plotHeight);
                return false;
            }

            // we now have the 'perfect' rect for BaseHouse being captured.
            Report(ReportType.Info, "Selected plot size {0}x{1}.", plotWidth, plotHeight);

            // define our inclusive bounds to also capture items along the southern and eastern edges
            Rectangle2D inclusiveBounds = new Rectangle2D(bounds.X, bounds.Y, bounds.Width + (state.EastSteps ? 1 : 0), bounds.Height + (state.SouthSteps ? 1 : 0));

            // ************ FIND HOUSE SIGN ************

            Item houseSign = null;

            if (house != null)
                houseSign = house.Sign;

            // find a house sign
            foreach (Item item in map.GetItemsInBounds(inclusiveBounds))
            {
                if (!CheckRange(item.Z, state.ZMin, state.ZMax))
                    continue;

                if (item is HouseSign || item is StaticHouseSign)
                {
                    houseSign = item;
                    break;
                }
            }

            if (houseSign == null)
                Report(ReportType.Warning, "No house sign found. Add a static house sign via \"[add StaticHouseSign\" and set its props.");

            // ************ CAPTURE STATICS/ITEMS ************

            List<TileEntry> tileList = new List<TileEntry>();
            List<DoorEntry> doorList = new List<DoorEntry>();

            if (statics)
            {
                // process statics
                for (int x = inclusiveBounds.X; x < inclusiveBounds.End.X; x++)
                {
                    for (int y = inclusiveBounds.Y; y < inclusiveBounds.End.Y; y++)
                    {
                        foreach (object o in map.GetTilesAt(new Point2D(x, y), false, false, true))
                        {
                            if (o == null || o is not StaticTile)
                                continue;

                            StaticTile tile = (StaticTile)o;

                            if (!CheckRange(tile.Z, state.ZMin, state.ZMax))
                                continue;

                            int id = tile.ID & 0x3FFF;

                            TileType flags = TileType.Normal;

                            if (state.SouthSteps && y == inclusiveBounds.End.Y - 1)
                                flags |= TileType.Outside;

                            if (state.EastSteps && x == inclusiveBounds.End.X - 1)
                            {
                                flags |= TileType.Outside;

                                // east steps must be added via the fixer addon
                                if (state.FixerAddon)
                                    flags |= TileType.Patch;
                            }

                            bool isFloor = IsFloor(id);

                            bool add = true;

                            foreach (TileEntry existing in tileList)
                            {
                                if (existing.m_xOffset == x &&
                                    existing.m_yOffset == y &&
                                    existing.m_zOffset == tile.Z)
                                {
                                    if (existing.m_id == id)
                                    {
                                        Report(ReportType.Info, "Ignoring duplicate static.");
                                        add = false;
                                        continue;
                                    }

                                    // one of the tiles is a floor, the other isn't
                                    if (isFloor != IsFloor(existing.m_id))
                                        continue;

                                    if (!state.FixerAddon)
                                    {
                                        Report(ReportType.Info, "Ignoring overlapping static.");
                                        add = false;
                                        continue;
                                    }

                                    flags |= TileType.Overlap;
                                }
                            }

                            if (add)
                                tileList.Add(new TileEntry((short)x, (short)y, (short)tile.Z, (ushort)id, 0, null, flags));
                        }
                    }
                }
            }

            // process items
            foreach (Item item in map.GetItemsInBounds(inclusiveBounds))
            {
                if (!CheckRange(item.Z, state.ZMin, state.ZMax))
                    continue;

                if (item is BaseMulti || item is BaseAddon || item is HouseSign || item is StaticHouseSign)
                    continue;

                if (item is BaseDoor)
                {
                    if (!doors)
                    {
                        Report(ReportType.Info, "Ignoring door.");
                        continue;
                    }

                    BaseDoor door = (BaseDoor)item;

                    doorList.Add(new DoorEntry((short)item.X, (short)item.Y, (short)item.Z, DoorHelper.Identify(door), door.Facing));
                }
                else
                {
                    int id = item.ItemID;

                    int hue = 0;
                    string itemName = null;

                    TileType flags = TileType.Normal;

                    // hued/named items must be added via the fixer addon
                    if (state.FixerAddon)
                    {
                        if (item.Hue != 0)
                        {
                            hue = item.Hue;
                            flags |= TileType.Patch;
                        }

                        if (item.Name != item.DefaultName)
                        {
                            itemName = item.Name;
                            flags |= TileType.Patch;
                        }
                    }

                    if (state.SouthSteps && item.Y == inclusiveBounds.End.Y - 1)
                        flags |= TileType.Outside;

                    if (state.EastSteps && item.X == inclusiveBounds.End.X - 1)
                    {
                        flags |= TileType.Outside;

                        // east steps must be added via the fixer addon
                        if (state.FixerAddon)
                            flags |= TileType.Patch;
                    }

                    bool isFloor = IsFloor(id);

                    bool add = true;

                    foreach (TileEntry existing in tileList)
                    {
                        if (existing.m_xOffset == item.X &&
                            existing.m_yOffset == item.Y &&
                            existing.m_zOffset == item.Z)
                        {
                            if (existing.m_id == id)
                            {
                                Report(ReportType.Info, "Ignoring duplicate item.");
                                add = false;
                                continue;
                            }

                            // one of the tiles is a floor, the other isn't
                            if (isFloor != IsFloor(existing.m_id))
                                continue;

                            if (!state.FixerAddon)
                            {
                                Report(ReportType.Info, "Ignoring overlapping item.");
                                add = false;
                                continue;
                            }

                            flags |= TileType.Overlap;
                        }
                    }

                    if (add)
                        tileList.Add(new TileEntry((short)item.X, (short)item.Y, (short)item.Z, (ushort)id, (ushort)hue, itemName, flags));
                }
            }

            // ************ POST-PROCESSING OF CAPTURED TILES ************

            if (state.ClearDirt)
            {
                for (int i = tileList.Count - 1; i >= 0; i--)
                {
                    TileEntry te = tileList[i];

                    if (te.m_zOffset == Math.Min(state.ZMin, state.ZMax) && te.m_id == 0x31F4)
                        tileList.RemoveAt(i);
                }
            }

            int signHangerGraphic = 0;
            int signpostGraphic = 0;

            if (houseSign != null)
            {
                // remove house sign hangers/posts
                for (int i = tileList.Count - 1; i >= 0; i--)
                {
                    TileEntry te = tileList[i];

                    if (te.m_xOffset == houseSign.X && te.m_yOffset == houseSign.Y && te.m_zOffset == houseSign.Z && IsSignHanger(te.m_id))
                    {
                        signHangerGraphic = te.m_id;

                        tileList.RemoveAt(i);
                    }
                    else if (te.m_xOffset == houseSign.X && te.m_yOffset == houseSign.Y - 1 && te.m_zOffset == houseSign.Z && IsSignpost(te.m_id))
                    {
                        signpostGraphic = te.m_id;

                        tileList.RemoveAt(i);
                    }
                }
            }

            if (tileList.Count == 0 && doorList.Count == 0)
            {
                Report(ReportType.Error, "Nothing was captured!");
                return false;
            }

            // ************ CALCULATE CENTER ************

            // tightest bounds around the captured tiles/doors
            int xMin = int.MaxValue;
            int yMin = int.MaxValue;
            int xMax = 0;
            int yMax = 0;
            int zMin = sbyte.MaxValue;

            foreach (TileEntry te in tileList)
            {
                if (!te.m_flags.HasFlag(TileType.Outside))
                {
                    xMin = Math.Min(xMin, te.m_xOffset);
                    yMin = Math.Min(yMin, te.m_yOffset);
                    xMax = Math.Max(xMax, te.m_xOffset);
                    yMax = Math.Max(yMax, te.m_yOffset);
                }

                zMin = Math.Min(zMin, te.m_zOffset);
            }

            foreach (DoorEntry de in doorList)
            {
                xMin = Math.Min(xMin, de.m_xOffset);
                yMin = Math.Min(yMin, de.m_yOffset);
                xMax = Math.Max(xMax, de.m_xOffset);
                yMax = Math.Max(yMax, de.m_yOffset);
                zMin = Math.Min(zMin, de.m_zOffset);
            }

            int dx = xMax - xMin;
            int dy = yMax - yMin;

            if ((plotWidth % 2) == 0)
                dx--;
            if ((plotHeight % 2) == 0)
                dy--;

            // center of the house relative to the north-west corner
            Point3D center = new Point3D(xMin + dx / 2, yMin + dy / 2, zMin);

            // ************ MAKE BLUEPRINT ************

            HouseBlueprint blueprint = m_Captured = new HouseBlueprint();

            blueprint.ID = state.HouseID;
            blueprint.Description = state.Description;
            blueprint.Capture = DateTime.UtcNow;
            blueprint.Width = plotWidth;
            blueprint.Height = plotHeight;
            blueprint.Price = StaticHouseHelper.GetFoundationPrice(plotWidth, plotHeight);

            // Design price (unused)
            //	price is a base price based on the plot size + a per tile cost (PTC).
            //	PTC is greater as the plot gets bigger .. this will encourage smaller houses, and 'tax' the big land hogs.
            //	the numbers were heuristically derived by looking at a very large and complex 18x18 and deciding that we wanted to add
            //	about 1,000,000 to the price, then dividing that by the number of tiles on this house (1130) and came up with a cost of approx
            //	885 per tile. We then use the logic 18x18 = 36 and 885/32 == 24 which gives us the base PTC. We then multiply
            //	24 * (width + height) to get the actual PTC for this house.
            //	so using this system above, a small 8x8 house has a PTC of 384 where a large 18x18 has a pertile cost of 864
            //int designPrice = (tileList.Count * (24 * (plotWidth + plotHeight)));

            if (houseSign != null)
            {
                if (houseSign is HouseSign)
                {
                    Report(ReportType.Info, "Processing house sign.");
                    HouseSign sign = (HouseSign)houseSign;
                    if (sign.Structure != null)
                        blueprint.BuiltOn = sign.Structure.BuiltOn;
                    if (sign.OriginalOwner != null)
                    {
                        Mobile owner = sign.OriginalOwner;
                        blueprint.OriginalOwnerName = owner.Name;
                        blueprint.OriginalOwnerSerial = owner.Serial;
                        if (owner.Account != null)
                            blueprint.OriginalOwnerAccount = owner.Account.ToString();
                    }
                }
                else if (houseSign is StaticHouseSign)
                {
                    Report(ReportType.Info, "Processing static house sign.");
                    StaticHouseSign sign = (StaticHouseSign)houseSign;
                    blueprint.BuiltOn = sign.BuiltOn;
                    if (sign.Owner != null)
                    {
                        Mobile owner = sign.Owner;
                        blueprint.OriginalOwnerName = owner.Name;
                        blueprint.OriginalOwnerSerial = owner.Serial;
                        if (owner.Account != null)
                            blueprint.OriginalOwnerAccount = owner.Account.ToString();
                    }
                }

                blueprint.UseSignLocation = true;

                blueprint.SignLocation = new Point3D(
                    houseSign.X - center.X,
                    houseSign.Y - center.Y,
                    houseSign.Z - center.Z);

                blueprint.SignGraphic = houseSign.ItemID;
            }

            blueprint.SignHangerGraphic = signHangerGraphic;
            blueprint.SignpostGraphic = signpostGraphic;

            foreach (TileEntry te in tileList)
            {
                blueprint.TileList.Add(new TileEntry(
                    (short)(te.m_xOffset - center.X),
                    (short)(te.m_yOffset - center.Y),
                    (short)(te.m_zOffset - center.Z),
                    te.m_id,
                    te.m_hue,
                    te.m_name,
                    te.m_flags));
            }

            foreach (DoorEntry de in doorList)
            {
                blueprint.DoorList.Add(new DoorEntry(
                    (short)(de.m_xOffset - center.X),
                    (short)(de.m_yOffset - center.Y),
                    (short)(de.m_zOffset - center.Z),
                    de.m_doorType,
                    de.m_facing));
            }

            // Record region info
            //	we will also capture the (HouseRegion) region info. We don't like the dumb
            //	complete-plot-is-the-region system introduced with Custom Housing, but prefer
            //	the old-school OSI bodle where the region is an array of well defined rects.
            //	we use the rect editing tools on copy1 of the Custom House, then recapture to 
            //	record the region info
            if (house != null && house.Region != null)
            {
                foreach (Rectangle3D rect3D in house.Region.Coords)
                {
                    Rectangle2D rect = new Rectangle2D(rect3D.Start, rect3D.End);

                    // NOTE: Region coordinates are offset by "bounds.Start", NOT by "center"
                    blueprint.Area.Add(new Rectangle2D(
                        rect.Start.X - bounds.X,
                        rect.Start.Y - bounds.Y,
                        rect.Width,
                        rect.Height));
                }
            }

            blueprint.Version = 1.0;

            Report(ReportType.Info, "Blueprint creation successful.");

            return true;
        }

        private enum ReportType : byte
        {
            Info,
            Warning,
            Error,
        }

        private static void Report(ReportType type, string format, params object[] args)
        {
            if (m_Mobile != null)
            {
                int color = (type == ReportType.Warning || type == ReportType.Error) ? 0x22 : 0x40;

                m_Mobile.SendMessage(color, String.Concat(type.ToString(), ": ", String.Format(format, args)));
            }
        }

        private static bool CheckRange(int value, int min, int max)
        {
            if (min > max)
            {
                int temp = min;
                min = max;
                max = temp;
            }

            return (value >= min && value <= max);
        }

        private static bool IsFloor(int itemID)
        {
            return (TileData.ItemTable[itemID & 0x3FFF].CalcHeight == 0);
        }

        private static bool IsSignHanger(int itemID)
        {
            return (Array.IndexOf(m_SignHangerIDs, itemID) != -1);
        }

        private static readonly int[] m_SignHangerIDs = new int[]
            {
                2968,
                2970,
                2972,
                2974,
                2976,
                2978,
            };

        private static bool IsSignpost(int itemID)
        {
            return (Array.IndexOf(m_SignpostIDs, itemID) != -1);
        }

        private static readonly int[] m_SignpostIDs = new int[]
            {
                9,
                29,
                54,
                90,
                147,
                169,
                177,
                204,
                251,
                257,
                263,
                298,
                347,
                424,
                441,
                466,
            };

        private static void Export(HouseBlueprint blueprint)
        {
            const double fileVersion = 1.0; // version of the blueprints file

            if (!Directory.Exists(Core.DataDirectory))
                Directory.CreateDirectory("Data");

            // old method: append XML element to the static housing file
            // new methos: make one XML file per static house design
#if old
            string fileName = StaticHouseHelper.ExportFile;
#else
            if (!Directory.Exists(StaticHouseHelper.ExportFolder))
                Directory.CreateDirectory(StaticHouseHelper.ExportFolder);

            string fileName = Path.Combine(StaticHouseHelper.ExportFolder, String.Format("{0}-{1}.xml", blueprint.ID, blueprint.Description));
#endif

            XmlDocument xmlDoc = new XmlDocument();

            if (System.IO.File.Exists(fileName))
                xmlDoc.Load(fileName);

            if (!xmlDoc.HasChildNodes)
                xmlDoc.AppendChild(xmlDoc.CreateElement("StaticHousing"));

            XmlElement xmlRoot = (XmlElement)xmlDoc.FirstChild;

            if (!xmlRoot.HasAttribute("Version"))
                xmlRoot.SetAttribute("Version", fileVersion.ToString());

            XmlElement xmlHouseElem = xmlDoc.CreateElement("HouseID");
            blueprint.ToXml(xmlHouseElem);
            xmlRoot.AppendChild(xmlHouseElem);

            xmlDoc.Save(fileName);
        }

        private class InternalGump : Gump
        {
            private const int LabelHue = 0x480;
            private const int GreenHue = 0x40;

            private InternalState m_State;

            public InternalGump(InternalState state)
                : base(100, 50)
            {
                m_State = state;

                AddBackground(0, 0, 280, 335, 9270);
                AddAlphaRegion(10, 10, 260, 315);

                AddLabel(20, 15, GreenHue, @"Static House Blueprint Generator");

                int y;

                y = 40;
                AddLabel(20, y, LabelHue, @"House ID");
                AddTextEntry(95, y - 5, 165, 20, LabelHue, 0, m_State.HouseID);
                AddImageTiled(95, y + 15, 165, 1, 9304);

                y += 25;
                AddLabel(20, y, LabelHue, @"Description");
                AddTextEntry(95, y - 5, 165, 20, LabelHue, 1, m_State.Description);
                AddImageTiled(95, y + 15, 165, 1, 9304);

                y += 25;
                AddLabel(47, y, LabelHue, @"Z Min");
                AddTextEntry(95, y - 5, 50, 20, LabelHue, 2, m_State.ZMin.ToString());
                AddImageTiled(95, y + 15, 50, 1, 9304);

                AddLabel(159, y, LabelHue, @"Z Max");
                AddTextEntry(210, y - 5, 50, 20, LabelHue, 3, m_State.ZMax.ToString());
                AddImageTiled(210, y + 15, 50, 1, 9304);

                y += 25;
                AddCheck(20, y, 2510, 2511, m_State.SouthSteps, 0);
                AddLabel(40, y, LabelHue, @"South Steps");

                y += 25;
                AddCheck(20, y, 2510, 2511, m_State.EastSteps, 1);
                AddLabel(40, y, LabelHue, @"East Steps");

                y += 25;
                AddCheck(20, y, 2510, 2511, m_State.ClearDirt, 2);
                AddLabel(40, y, LabelHue, @"Clear Dirt");

                y += 25;
                AddCheck(20, y, 2510, 2511, m_State.FixerAddon, 3);
                AddLabel(40, y, LabelHue, @"Fixer Addon");

                y += 25;
                AddCheck(20, y, 2510, 2511, m_State.Update, 4);
                AddLabel(40, y, LabelHue, @"Update Version");

                y += 25;
                AddCheck(20, y, 2510, 2511, m_State.Test, 5);
                AddLabel(40, y, LabelHue, @"Test Capture");

                y += 25;
                AddButton(20, y, 4005, 4006, 1, GumpButtonType.Reply, 0);
                AddLabel(55, y, LabelHue, @"Target Sign");

                AddButton(155, y, 4005, 4006, 2, GumpButtonType.Reply, 0);
                AddLabel(190, y, LabelHue, @"Pick Bounds");

                y += 25;
                AddButton(20, y, 4020, 4021, 0, GumpButtonType.Reply, 0);
                AddLabel(55, y, LabelHue, @"Cancel");
            }

            private enum GenMode : byte
            {
                Invalid,

                Target,
                Bounds,
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                GenMode mode;

                switch (info.ButtonID)
                {
                    case 1: mode = GenMode.Target; break;
                    case 2: mode = GenMode.Bounds; break;
                    default: return;
                }

                string zMin = String.Empty;
                string zMax = String.Empty;

                // process text entries
                foreach (TextRelay text in info.TextEntries)
                {
                    switch (text.EntryID)
                    {
                        case 0:
                            m_State.HouseID = text.Text;
                            break;
                        case 1:
                            m_State.Description = text.Text;
                            break;
                        case 2:
                            zMin = text.Text;
                            break;
                        case 3:
                            zMax = text.Text;
                            break;
                    }
                }

                // clear switches
                m_State.SouthSteps = false;
                m_State.EastSteps = false;
                m_State.ClearDirt = false;
                m_State.FixerAddon = false;
                m_State.Update = false;
                m_State.Test = false;

                // process switches
                foreach (int switchID in info.Switches)
                {
                    switch (switchID)
                    {
                        case 0:
                            m_State.SouthSteps = true;
                            break;
                        case 1:
                            m_State.EastSteps = true;
                            break;
                        case 2:
                            m_State.ClearDirt = true;
                            break;
                        case 3:
                            m_State.FixerAddon = true;
                            break;
                        case 4:
                            m_State.Update = true;
                            break;
                        case 5:
                            m_State.Test = true;
                            break;
                    }
                }

                if (Verify(sender.Mobile, m_State, zMin, zMax))
                {
                    switch (mode)
                    {
                        case GenMode.Target:
                            sender.Mobile.BeginTarget(-1, false, TargetFlags.None, HouseGen_OnTarget, m_State);
                            break;
                        case GenMode.Bounds:
                            BoundingBoxPicker.Begin(sender.Mobile, new BoundingBoxCallback(HouseGen_OnBoundPicked), m_State);
                            break;
                    }
                }
                else
                {
                    sender.Mobile.CloseGump(typeof(InternalGump));
                    sender.Mobile.SendGump(new InternalGump(m_State));
                }
            }

            private static bool Verify(Mobile from, InternalState state, string zMin, string zMax)
            {
                if (String.IsNullOrEmpty(state.HouseID))
                {
                    from.SendMessage(0x22, "Error: You must specify a HID for your blueprint.");
                    return false;
                }

                bool exists = StaticHouseHelper.BlueprintExists(state.HouseID);

                if (state.Update)
                {
                    if (!exists)
                    {
                        from.SendMessage(0x22, "Error: A blueprint with that HID does not exist.");
                        return false;
                    }
                }
                else if (exists)
                {
                    from.SendMessage(0x22, "Error: A blueprint with that HID already exists.");
                    return false;
                }

                if (!state.Update && String.IsNullOrEmpty(state.Description))
                    from.SendMessage(0x22, "Warning: You should specify a description for this house.");

                try
                {
                    state.ZMin = Convert.ToSByte(zMin);
                }
                catch
                {
                    from.SendMessage(0x22, "Error: Bad Z Min specified.");
                    return false;
                }

                try
                {
                    state.ZMax = Convert.ToSByte(zMax);
                }
                catch
                {
                    from.SendMessage(0x22, "Error: Bad Z Max specified.");
                    return false;
                }

                if (state.Update)
                    from.SendMessage(0x40, "Info: Updating the revision of the existing blueprint.");

                if (state.Test)
                    from.SendMessage(0x40, "Info: Test capture; not exporting to file.");

                return true;
            }
        }

        internal class InternalState
        {
            public string HouseID;
            public string Description;
            public sbyte ZMin;
            public sbyte ZMax;
            public bool SouthSteps;
            public bool EastSteps;
            public bool ClearDirt;
            public bool FixerAddon;
            public bool Update;
            public bool Test;

            public InternalState()
            {
                HouseID = StaticHouseHelper.GetNewHouseID();
                ZMin = sbyte.MinValue;
                ZMax = sbyte.MaxValue;
                FixerAddon = true; // Yoar: let's enable this by default
            }
        }
    }
}