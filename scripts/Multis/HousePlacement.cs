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

/* Scripts/Multis/HousePlacement.cs
 *	ChangeLog: 
 *  7/1/2023, Adam
 *      In AREA CHECK, ignore orc camps, brigand camps etc.
 *	5/28/23, Yoar
 *	    Now disallowing house placement in (a) actively guarded regions OR (b) regions with the Town ruleset
 *	4/2/2023, Adam
 *	    any flavor of guard region (actively guarded or not,) blocks placement
 *  9/2/22, Yoar (WorldZone)
 *      Added WorldZone check in order to contain players within the world zone.
 *	5/8/2022, Yoar
 *	    Added a rule to prevent people from placing houses *near* townships that they are not a part of.
 *	    This rule is consistent with the township "neutral house clearance" rule.
 *	7/26/2021, Adam
 *	    Add LenientZ(landAvgZ, addTileZ) handling for placing things like brigand camps.
 *	    The wheels on the brigand's covered waggon are at average z -1 which fails under normal house placement rules.
 *	    We turn on lenientZ processing for these types of structures. Valid LenientZ returns are (Abs) 0 or 1
*	6/28/2021, Adam
*	    Turning on placement over tents again (What could go wrong?)
 *	3/17/16, Adam
 *		Tent Annex() by implementing rule #7 until better understood.
 *	6/18/10, adam
 *		Remove test for NoHousingRegion since we've moved to custom regions.
 *	2/27/10, Adam
 *		Tents don't block houses.
 *		If a tent exists where a house will be placed, the tent is 'deeded' and the deed placed in the owners bank.
 *		In addition, and to prevent tent griefing we limit the house placements to once per 7 days if a tent was annexed.
 *	5/8/08, Adam
 *		Move the NoHousingRegion check into the new HousePlacementRegionCheck() function,
 *		The old version of NoHousingRegion only checked the center of the house which meant 1/2 of your house could be in the NoHousingRegion.
 *		The HousePlacementRegionCheck() function makes all region checks on ALL regions at theat point and against ALL house tiles.
 *  5/1/07, Adam
 *      Make custom house placement based on TestCenter.Enabled == true
 *	10/16/06, Kit
 *		Made placement invalid if house layers overlap.
 *	8/01/06, weaver
 *		Removed idiotic check before HouseRegion cast.
 *	7/19/06, weaver	
 *		Added check before HouseRegion cast. 
 *	5/03/06, weaver	
 *		Disallowed placement on top of tents (they're special).
 *	9/20/05, Adam
 *		Update Placement to check ALL regions that contain the area in question.
 *		We can define NoHousing regions in Regions.xml.
 *		The old code only checked the FIRST region found.
 *  10/27/04, Lego
 *        added two grass tiles to the list 879 and 881
 *	10/7/04, Adam
 *		Remove Debugging info
 *	10/6/04, Adam
 *		ExceptionTiles()
 *		Fix issues, and add another tileID to the tile exception list for placement (122)
 *		Add debugging info (needs to come out)
 *	10/4/04, Adam
 *		Revert last fix as it's not quite right
 *	10/04/2004 - LegoEater
 *		fixed so can place over dirt tiles
 */

using Server.Items;
using Server.Regions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Server.Multis
{
    public enum HousePlacementResult
    {
        Valid,
        BadRegion,
        BadLand,
        BadStatic,
        BadItem,
        NoSurface,
        BadRegionHidden,
        BadRegionTownship,
        TentRegion,
        NearTownship,
        BadArea,            // farm plots qualify as a 'bad area'
    }

    public class HousePlacement
    {
        private const int YardSize = 5;

        // Any land tile which matches one of these ID numbers is considered a road and cannot be placed over.
        private static int[] m_RoadIDs = new int[]
            {
                0x071, 0x08C,
                0x0E8, 0x0EB,
                0x14C, 0x14F,
                0x161, 0x174,
                0x1F0, 0x1F3,
                0x26E, 0x279,
                0x27E, 0x281,
                0x324, 0x3AC,
                0x547, 0x556,
                0x597, 0x5A6,
                0x637, 0x63A,
                0x7AE, 0x7B1,
                0x442, 0x479, // Sand stones
				0x501, 0x510, // Sand stones
				0x009, 0x015, // Furrows
				0x150, 0x15C  // Furrows 
			};

        public static int[] RoadIDs
        {
            get { return m_RoadIDs; }
        }

        public static bool ExceptionTiles(int TileID)
        {
            switch (TileID)
            {
                case 0x79: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 121
                case 0x7a: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 122
                case 0x7b: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 123
                case 0x7c: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 124
                case 0x7d: /*Console.WriteLine(TileID.ToString());*/ return true;//grass tile 125
                case 0x7e: /*Console.WriteLine(TileID.ToString());*/ return true;//grass tile 126
                case 0x82: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 130
                case 0x83: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 131
                case 0x84: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 132
                case 0x85: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 133
                case 0x86: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 134
                case 0x87: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 135
                case 0x88: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 136
                case 0x89: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 137
                case 0x8a: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 138
                case 0x8b: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 139
                case 0x8c: /*Console.WriteLine(TileID.ToString());*/ return true;//dirt tile 140
                case 0x36f: /*Console.WriteLine(TileID.ToString());*/ return true;//grass tile 879
                case 0x371: /*Console.WriteLine(TileID.ToString());*/ return true;//grass tile 881
            }
            return false;
        }

        private static HousePlacementResult HousePlacementRegionCheck(Mobile? from, Map map, Point3D testPoint, out Region region)
        {
            Sector sector = map.GetSector(testPoint);
            List<Region> list = sector.Regions;
            region = null;

            // Placement may not cross the Test Center containment boundary in either direction.
            bool fromInTestCenter = from != null && from.AllRegions.Any(r => r is TestCenterRegion);
            bool pointInTestCenter = list.Any(r => r is TestCenterRegion && r.Contains(testPoint));

            if (fromInTestCenter != pointInTestCenter)
                return BlockHousePlacement(from, HousePlacementResult.BadRegion, "Test Center boundary violation");

            for (int i = 0; i < list.Count; ++i)
            {
                region = (Region)list[i];

                if (region.Contains(testPoint))
                {
                    if (IsProtectedPlacementRegion(region))
                        return BlockHousePlacement(from, HousePlacementResult.BadRegion, "Guarded region or town rules");

                    if (region is HouseRegion)
                    {
                        // Adam: tents only block other tents, not houses.
                        BaseHouse house = (region as HouseRegion).House;

                        if (house is Tent)
                            return HousePlacementResult.TentRegion;

                        return BlockHousePlacement(from, HousePlacementResult.BadRegion, "Existing house region");
                    }

                    if (!region.AllowHousing(testPoint)) // Cannot place houses in dungeons, towns, treasure map areas etc
                    {
                        return BlockHousePlacement(from, HousePlacementResult.BadRegion, "Region does not allow housing");
                    }

                    if (region is TownshipRegion)
                    {
                        TownshipRegion tsr = (TownshipRegion)region;

                        if (from == null || tsr.TStone == null || !tsr.TStone.IsMember(from))
                            return BlockHousePlacement(from, HousePlacementResult.BadRegionTownship, "Township region - non-member");
                    }
                }
            }

            return HousePlacementResult.Valid;
        }
        private static bool IsProtectedPlacementRegion(Region region)
        {
            if (region is TownshipRegion)
                return false;

            if (region is GuardedRegion gr && gr.IsGuarded)
                return true;

            return region.IsTownRules;
        }
        private static HousePlacementResult HousePlacementBlockerCheck(
            Mobile? from,
            Map map,
            Point3D testPoint,
            Dictionary<Sector, HashSet<Point3D>> blockerCache)
        {
            Sector sector = map.GetSector(testPoint);

            if (sector == null)
                return HousePlacementResult.Valid;

            if (!blockerCache.TryGetValue(sector, out HashSet<Point3D>? blockerPoints))
            {
                blockerPoints = new HashSet<Point3D>();

                if (sector.Items != null)
                {
                    foreach (Item item in sector.Items)
                    {
                        if (item != null && !item.Deleted && item.ItemID == 0x2199)
                            blockerPoints.Add(item.Location);
                    }
                }

                blockerCache[sector] = blockerPoints;
            }

            if (blockerPoints.Contains(testPoint))
                return HousePlacementResult.BadItem;

            return HousePlacementResult.Valid;
        }

        public static HousePlacementResult Check(Mobile from, int multiID, Point3D center, out ArrayList toMove)
        {
            return Check(from, multiID, center, out toMove, false);
        }
        public static HousePlacementResult Check(Mobile? from /* may be null */, int multiID, Point3D center, out ArrayList toMove, bool tent)
        {
            // Policy:
            // This method validates whether a house/tent may be placed at the requested location.
            // It also collects blocking mobiles/items that may be relocated if placement succeeds.
            //
            // Embedded policy currently includes:
            // - staff bypass
            // - map/event exclusions
            // - tent annexation
            // - world-zone restrictions
            // - township clearance
            // - road restrictions
            //
            // The goal of this pass is to preserve those policies while improving structure and lookup cost.

            HashSet<object> moveSet = new HashSet<object>();
            Map map = (from != null) ? from.Map : Map.Felucca;
            bool isPlayer = from != null && from.AccessLevel == AccessLevel.Player;
            bool isStaff = from != null && from.AccessLevel >= AccessLevel.GameMaster;
            bool allowTentAnnexation = Core.RuleSets.TentAnnexation();

            HousePlacementResult earlyResult = ValidatePlacementContext(from, map, center, isStaff);
            if (earlyResult != HousePlacementResult.Valid)
            {
                toMove = new ArrayList();
                return earlyResult;
            }

            if (isStaff)
            {
                toMove = new ArrayList();
                return HousePlacementResult.Valid;
            }

            MultiComponentList mcl = MultiData.GetComponents(multiID);
            if (multiID >= 0x13EC && multiID < 0x1D00)
                HouseFoundation.AddStairsTo(ref mcl);

            Point3D start = new Point3D(center.X + mcl.Min.X, center.Y + mcl.Min.Y, center.Z);

            PlacementScratch scratch = new PlacementScratch();

            HousePlacementResult footprintResult = ScanFootprint(
                from,
                map,
                center,
                start,
                mcl,
                tent,
                isPlayer,
                allowTentAnnexation,
                scratch,
                moveSet);

            if (footprintResult != HousePlacementResult.Valid)
            {
                toMove = new ArrayList();
                return footprintResult;
            }

            HousePlacementResult borderResult = ValidateBorders(from, map, center, scratch.Border, scratch.TentComponents);
            if (borderResult != HousePlacementResult.Valid)
            {
                toMove = new ArrayList();
                return borderResult;
            }

            HousePlacementResult yardResult = ValidateYard(from, map, scratch.Yard);
            if (yardResult != HousePlacementResult.Valid)
            {
                toMove = new ArrayList();
                return yardResult;
            }

            toMove = new ArrayList();
            toMove.AddRange(moveSet.ToArray());

            return HousePlacementResult.Valid;
        }
        private sealed class PlacementScratch
        {
            // Per-tile working sets.
            public readonly List<Item> Items = new List<Item>();
            public readonly List<Mobile> Mobiles = new List<Mobile>();

            // Aggregate sets built while scanning the footprint.
            public readonly HashSet<Point2D> Yard = new HashSet<Point2D>();
            public readonly HashSet<Point2D> Border = new HashSet<Point2D>();

            // Tent annexation exclusions.
            public readonly HashSet<Item> TentComponents = new HashSet<Item>();
        }
        private static HousePlacementResult ValidatePlacementContext(Mobile? from, Map map, Point3D center, bool isStaff)
        {
            // Policy:
            // Global placement exclusions that apply before any footprint scanning begins.
            // This is where shard-wide "you may not place here at all" rules live.

            if (map == null || map == Map.Internal)
                return BlockHousePlacement(from, HousePlacementResult.BadLand, "Invalid map (null or Internal)");

            if (isStaff)
                return HousePlacementResult.Valid;

            if (map == Map.Ilshenar || Misc.WinterEventSystem.Contains(center, map))
                return BlockHousePlacement(from, HousePlacementResult.BadRegion, "Ilshenar or Winter Event - placement blocked");

            return HousePlacementResult.Valid;
        }
        private static HousePlacementResult ScanFootprint(
    Mobile? from,
    Map map,
    Point3D center,
    Point3D start,
    MultiComponentList mcl,
    bool tent,
    bool isPlayer,
    bool allowTentAnnexation,
    PlacementScratch scratch,
    HashSet<object> toMove)
        {
            // Policy:
            // This is the core structural placement validator.
            // It enforces:
            // - collision rules
            // - region rules
            // - road restrictions
            // - world-zone restrictions
            // - township clearance
            // It also collects yard/border points and relocation targets.

            Dictionary<Sector, HashSet<Point3D>> blockerCache = new Dictionary<Sector, HashSet<Point3D>>();

            for (int x = 0; x < mcl.Width; ++x)
            {
                for (int y = 0; y < mcl.Height; ++y)
                {
                    int tileX = start.X + x;
                    int tileY = start.Y + y;
                    StaticTile[] foundationTiles = mcl.Tiles[x][y];

                    if (foundationTiles.Length == 0)
                        continue;

                    Point3D testPoint = new Point3D(tileX, tileY, center.Z);

                    HousePlacementResult blockerResult = HousePlacementBlockerCheck(from, map, testPoint, blockerCache);
                    if (blockerResult != HousePlacementResult.Valid)
                        return BlockHousePlacement(from, blockerResult, "LOS Blocker (0x2199) detected in footprint");

                    Region region;
                    HousePlacementResult regionResult = ValidatePlacementRegion(
                        from,
                        map,
                        testPoint,
                        tent,
                        allowTentAnnexation,
                        scratch.TentComponents,
                        toMove,
                        out region);

                    if (regionResult != HousePlacementResult.Valid)
                        return BlockHousePlacement(from, regionResult, "Region validation failed");

                    HousePlacementResult areaResult = ValidateSurroundingArea(from, map, testPoint);
                    if (areaResult != HousePlacementResult.Valid)
                        return BlockHousePlacement(from, areaResult, "Surrounding area validation failed");

                    if (isPlayer && WorldZone.IsOutside(testPoint, map))
                        return BlockHousePlacement(from, HousePlacementResult.BadRegion, "WorldZone - Outside placement blocked");

                    // Reject placement inside any configured forbidden placement area
                    if (IsInForbiddenPlacementArea(testPoint, map))
                        return BlockHousePlacement(from, HousePlacementResult.BadRegion, "Forbidden placement area");

                    LandTile landTile = map.Tiles.GetLandTile(tileX, tileY);
                    int landID = landTile.ID & 0x3FFF;
                    StaticTile[] staticTiles = map.Tiles.GetStaticTiles(tileX, tileY, true);
                    Sector sector = map.GetSector(tileX, tileY);

                    CollectTileItemsAndMobiles(from, tileX, tileY, sector, scratch, scratch.TentComponents);

                    bool hasFoundation;
                    HousePlacementResult tileResult = ValidateFootprintTile(
                        from,
                        map,
                        center,
                        tileX,
                        tileY,
                        landTile,
                        staticTiles,
                        foundationTiles,
                        scratch.Items,
                        scratch.Mobiles,
                        toMove,
                        out hasFoundation);

                    if (tileResult != HousePlacementResult.Valid)
                        return BlockHousePlacement(from, tileResult, "Footprint tile validation failed");

                    if (IsRoadTile(landID))
                        return BlockHousePlacement(from, HousePlacementResult.BadLand, "Road tile detected in foundation");

                    if (hasFoundation)
                    {
                        CollectYardPoints(tileX, tileY, scratch.Yard);
                        CollectBorderPoints(x, y, tileX, tileY, mcl, scratch.Border);
                    }

                    HousePlacementResult townshipResult = ValidateTownshipClearance(from, map, center.Z, tileX, tileY, hasFoundation);
                    if (townshipResult != HousePlacementResult.Valid)
                        return BlockHousePlacement(from, townshipResult, "Township clearance violation");
                }
            }

            return HousePlacementResult.Valid;
        }
        private sealed class ForbiddenPlacementArea
        {
            public string Name { get; }
            public Map Map { get; }
            public Rectangle2D Rect { get; }

            public ForbiddenPlacementArea(string name, Map map, Rectangle2D rect)
            {
                Name = name;
                Map = map;
                Rect = rect;
            }
        }
        private static readonly ForbiddenPlacementArea[] m_ForbiddenPlacementAreas =
        {
            new ForbiddenPlacementArea(
                "Ice Island",
                Map.Felucca,
                new Rectangle2D(3623, 213, 705, 546)),
        };
        private static bool IsInForbiddenPlacementArea(Point3D p, Map map)
        {
            foreach (ForbiddenPlacementArea area in m_ForbiddenPlacementAreas)
            {
                if (!ShouldApply(area.Name))
                    continue;

                if (area.Map == map && area.Rect.Contains(p))
                    return true;
            }

            return false;
        }
        private static bool ShouldApply(string name)
        {
            switch (name)
            {
                case "Ice Island":
                    return (Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules());

                default:
                    return true;
            }
        }
        private static HousePlacementResult ValidatePlacementRegion(
            Mobile? from,
            Map map,
            Point3D testPoint,
            bool tent,
            bool allowTentAnnexation,
            HashSet<Item> tentComponents,
            HashSet<object> toMove,
            out Region region)
        {
            // Policy:
            // Region placement rules are layered:
            // - tents require a fully valid region
            // - non-tents may overlap a tent region when tent annexation is enabled
            // - annexed tent components are excluded from later collision tests

            HousePlacementResult regionResult = HousePlacementRegionCheck(from, map, testPoint, out region);

            if (tent)
            {
                if (regionResult != HousePlacementResult.Valid)
                    return BlockHousePlacement(from, regionResult, "Tent placement failed region check");
            }
            else
            {
                if (regionResult != HousePlacementResult.Valid && regionResult != HousePlacementResult.TentRegion)
                    return BlockHousePlacement(from, regionResult, "House placement failed region check");
            }

            if (allowTentAnnexation && regionResult == HousePlacementResult.TentRegion && region != null)
                CaptureTentAnnexation(region, tentComponents, toMove);

            return HousePlacementResult.Valid;
        }

        private static void CaptureTentAnnexation(Region region, HashSet<Item> tentComponents, HashSet<object> toMove)
        {
            // Policy:
            // When a house is allowed to annex a tent region, tent-owned components are treated
            // as exempt from collision checks and are scheduled for post-placement movement.

            BaseHouse house = (region as HouseRegion).House;

            if (house is not Tent tentHouse)
                return;

            toMove.Add(house);

            AddTentComponent(tentComponents, house);
            AddTentComponent(tentComponents, tentHouse.TentPack);
            AddTentComponent(tentComponents, tentHouse.TentBed);

            if (house.Addons == null)
                return;

            for (int addonIndex = 0; addonIndex < house.Addons.Count; addonIndex++)
            {
                BaseAddon? addon = house.Addons[addonIndex] as BaseAddon;
                if (addon == null)
                    continue;

                for (int componentIndex = 0; componentIndex < addon.Components.Count; componentIndex++)
                    AddTentComponent(tentComponents, addon.Components[componentIndex] as Item);
            }
        }

        private static void AddTentComponent(HashSet<Item> tentComponents, Item? item)
        {
            if (item != null)
                tentComponents.Add(item);
        }
        private static HousePlacementResult ValidateSurroundingArea(Mobile? from, Map map, Point3D testPoint)
        {
            // Policy:
            // Special-case environmental blockers that are not part of the core footprint collision rules.
            // This currently includes farm-pen style gates and camp exceptions.

            IPooledEnumerable nearbyItems = map.GetItemsInRange(testPoint, 30);

            foreach (Item item in nearbyItems)
            {
                if (item == null)
                    continue;

                if (item.CampComponent)
                    continue;

                if (item is BaseDoor && item.GetType().Name.Contains("Gate") && Region.Find(item.Location, map) is not HouseRegion)
                {
                    nearbyItems.Free();
                    return BlockHousePlacement(from, HousePlacementResult.BadArea, "Farm gate / pen gate blocking placement");
                }
            }

            nearbyItems.Free();
            return HousePlacementResult.Valid;
        }
        private static void CollectTileItemsAndMobiles(Mobile? from, int tileX, int tileY, Sector sector, PlacementScratch scratch, HashSet<Item> tentComponents)
        {
            // Policy:
            // Gather only the items/mobiles that physically occupy this tile and are relevant
            // to placement. Special exclusions are applied here so later checks can stay simpler.

            scratch.Items.Clear();

            foreach (Item item in sector.Items)
            {
                if (item == null)
                    continue;

                if (item.X != tileX || item.Y != tileY)
                    continue;

                if (SpecialItemExclusion(item))
                    continue;

                if (tentComponents.Contains(item))
                    continue;

                scratch.Items.Add(item);
            }

            scratch.Mobiles.Clear();

            foreach (Mobile mobile in sector.Mobiles)
            {
                if (mobile == null)
                    continue;

                if (mobile.X != tileX || mobile.Y != tileY)
                    continue;

                if (SpecialMobileExclusion(mobile))
                    continue;

                scratch.Mobiles.Add(mobile);
            }
        }
        private static HousePlacementResult ValidateFootprintTile(Mobile? from,
    Map map,
    Point3D center,
    int tileX,
    int tileY,
    LandTile landTile,
    StaticTile[] staticTiles,
    StaticTile[] foundationTiles,
    List<Item> items,
    List<Mobile> mobiles,
    HashSet<object> toMove,
    out bool hasFoundation)
        {
            // Policy:
            // This is the strict collision/terrain validator for a single occupied house tile.
            // It enforces:
            // - no terrain or static overlap
            // - no item overlap unless movable
            // - foundation support requirements
            // It also collects items/mobiles that may be relocated.

            hasFoundation = false;
            int landStartZ = 0;
            int landAvgZ = 0;
            int landTopZ = 0;
            map.GetAverageZ(tileX, tileY, ref landStartZ, ref landAvgZ, ref landTopZ);

            for (int i = 0; i < foundationTiles.Length; ++i)
            {
                StaticTile foundationTile = foundationTiles[i];

                if (foundationTile.ID == 0x4001)
                    continue;

                TileFlag foundationFlags = TileData.ItemTable[foundationTile.ID & 0x3FFF].Flags;
                bool isFoundation = (foundationTile.Z == 0 && (foundationFlags & TileFlag.Wall) != 0);
                bool hasSurface = false;

                if (isFoundation)
                    hasFoundation = true;

                int structureZ = center.Z + foundationTile.Z;
                int structureTop = structureZ + foundationTile.Height;

                if ((foundationFlags & TileFlag.Surface) != 0)
                    structureTop += 16;

                if (structureTop > landStartZ && landAvgZ > structureZ)
                    return BlockHousePlacement(from, HousePlacementResult.BadLand, "BadLand - terrain overlap with foundation");

                if (isFoundation &&
                    (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Impassable) == 0 &&
                    landAvgZ == center.Z)
                {
                    hasSurface = true;
                }

                for (int j = 0; j < staticTiles.Length; ++j)
                {
                    StaticTile staticTile = staticTiles[j];
                    ItemData staticData = TileData.ItemTable[staticTile.ID & 0x3FFF];

                    if ((staticData.Impassable || (staticData.Surface && (staticData.Flags & TileFlag.Background) == 0)) &&
                        structureTop > staticTile.Z &&
                        (staticTile.Z + staticData.CalcHeight) > structureZ)
                    {
                        return BlockHousePlacement(from, HousePlacementResult.BadStatic, "BadStatic - static tile overlap");
                    }

                    if (isFoundation &&
                        !hasSurface &&
                        (staticData.Flags & TileFlag.Surface) != 0 &&
                        (staticTile.Z + staticData.CalcHeight) == center.Z)
                    {
                        hasSurface = true;
                    }
                }

                for (int j = 0; j < items.Count; ++j)
                {
                    Item item = items[j];
                    ItemData itemData = item.ItemData;

                    if (structureTop > item.Z && (item.Z + itemData.CalcHeight) > structureZ)
                    {
                        if (item.Movable)
                        {
                            toMove.Add(item);
                        }
                        else if (itemData.Impassable || (itemData.Surface && (itemData.Flags & TileFlag.Background) == 0))
                        {
                            return BlockHousePlacement(from, HousePlacementResult.BadItem, "BadItem - impassable item overlap");
                        }
                    }
                    else if (isFoundation &&
                             !hasSurface &&
                             (itemData.Flags & TileFlag.Surface) != 0 &&
                             (item.Z + itemData.CalcHeight) == center.Z)
                    {
                        hasSurface = true;
                    }
                }

                if (isFoundation && !hasSurface)
                    return BlockHousePlacement(from, HousePlacementResult.NoSurface, "NoSurface - foundation has no support");

                for (int j = 0; j < mobiles.Count; ++j)
                {
                    Mobile mobile = mobiles[j];

                    if (structureTop > mobile.Z && (mobile.Z + 16) > structureZ)
                        toMove.Add(mobile);
                }
            }

            return HousePlacementResult.Valid;
        }
        private static void CollectYardPoints(int tileX, int tileY, HashSet<Point2D> yard)
        {
            // Policy:
            // The yard set represents the clearance area later checked for obstructing multi tiles.

            for (int xOffset = -1; xOffset <= 1; ++xOffset)
            {
                for (int yOffset = -YardSize; yOffset <= YardSize; ++yOffset)
                    yard.Add(new Point2D(tileX + xOffset, tileY + yOffset));
            }
        }

        private static void CollectBorderPoints(int houseX, int houseY, int tileX, int tileY, MultiComponentList mcl, HashSet<Point2D> border)
        {
            // Policy:
            // Border points represent the immediate outer perimeter around the footprint.
            // Border collection is softened when neighboring structure tiles already provide base-floor coverage.

            for (int xOffset = -1; xOffset <= 1; ++xOffset)
            {
                for (int yOffset = -1; yOffset <= 1; ++yOffset)
                {
                    if (xOffset == 0 && yOffset == 0)
                        continue;

                    int neighborX = houseX + xOffset;
                    int neighborY = houseY + yOffset;

                    if (neighborX >= 0 && neighborX < mcl.Width && neighborY >= 0 && neighborY < mcl.Height)
                    {
                        StaticTile[] neighboringTiles = mcl.Tiles[neighborX][neighborY];
                        bool suppressBorder = false;

                        for (int i = 0; !suppressBorder && i < neighboringTiles.Length; ++i)
                        {
                            StaticTile neighboringTile = neighboringTiles[i];

                            if (neighboringTile.Height == 0 &&
                                neighboringTile.Z <= 8 &&
                                TileData.ItemTable[neighboringTile.ID & 0x3FFF].Surface)
                            {
                                suppressBorder = true;
                            }
                        }

                        if (suppressBorder)
                            continue;
                    }

                    border.Add(new Point2D(tileX + xOffset, tileY + yOffset));
                }
            }
        }
        private static HousePlacementResult ValidateTownshipClearance(Mobile? from, Map map, int centerZ, int tileX, int tileY, bool hasFoundation)
        {
            // Policy:
            // Township clearance is enforced only around true foundation tiles and may be bypassed
            // for township members.

            if (!hasFoundation)
                return HousePlacementResult.Valid;

            int clearance = Township.TownshipSettings.NeutralHouseClearance;

            for (int xOffset = -clearance; xOffset <= clearance; ++xOffset)
            {
                for (int yOffset = -clearance; yOffset <= clearance; ++yOffset)
                {
                    Point3D point = new Point3D(tileX + xOffset, tileY + yOffset, centerZ);

                    TownshipRegion? township = Region.Find(point, map) as TownshipRegion;

                    if (township != null && (from == null || township.TStone == null || !township.TStone.IsMember(from)))
                        return BlockHousePlacement(from, HousePlacementResult.NearTownship, "Near Township (clearance violation)");
                }
            }

            return HousePlacementResult.Valid;
        }
        private static HousePlacementResult ValidateBorders(Mobile? from, Map map, Point3D center, HashSet<Point2D> borders, HashSet<Item> tentComponents)
        {
            // Policy:
            // Border validation enforces the "clear around the outside" rule.
            // Tent-annexed components are excluded from item obstruction checks.

            foreach (Point2D borderPoint in borders)
            {
                LandTile landTile = map.Tiles.GetLandTile(borderPoint.X, borderPoint.Y);
                int landID = landTile.ID & 0x3FFF;

                if ((TileData.LandTable[landID].Flags & TileFlag.Impassable) != 0)
                    return BlockHousePlacement(from, HousePlacementResult.BadLand, "BadLand - impassable land on border");

                if (IsRoadTile(landID))
                    return BlockHousePlacement(from, HousePlacementResult.BadLand, "BadLand - road tile on border");

                StaticTile[] staticTiles = map.Tiles.GetStaticTiles(borderPoint.X, borderPoint.Y, true);

                for (int j = 0; j < staticTiles.Length; ++j)
                {
                    StaticTile staticTile = staticTiles[j];
                    ItemData staticData = TileData.ItemTable[staticTile.ID & 0x3FFF];

                    if (staticData.Impassable ||
                        (staticData.Surface && (staticData.Flags & TileFlag.Background) == 0 && (staticTile.Z + staticData.CalcHeight) > (center.Z + 2)))
                    {
                        return BlockHousePlacement(from, HousePlacementResult.BadStatic, "BadStatic - static obstruction on border");
                    }
                }

                Sector sector = map.GetSector(borderPoint.X, borderPoint.Y);

                foreach (Item item in sector.Items)
                {
                    if (item == null)
                        continue;

                    if (item.X != borderPoint.X || item.Y != borderPoint.Y || item.Movable)
                        continue;

                    if (tentComponents.Contains(item))
                        continue;

                    ItemData itemData = item.ItemData;

                    if (itemData.Impassable ||
                        (itemData.Surface && (itemData.Flags & TileFlag.Background) == 0 && (item.Z + itemData.CalcHeight) > (center.Z + 2)))
                    {
                        return BlockHousePlacement(from, HousePlacementResult.BadItem, "BadItem - impassable item on border");
                    }
                }
            }

            return HousePlacementResult.Valid;
        }
        private static HousePlacementResult ValidateYard(Mobile? from, Map map, HashSet<Point2D> yard)
        {
            // Policy:
            // Yard validation enforces clearance in the front/back placement buffer by checking
            // for blocking multi tiles.

            foreach (Point2D yardPoint in yard)
            {
                IPooledEnumerable multiTiles = map.GetMultiTilesAt(yardPoint.X, yardPoint.Y);

                foreach (StaticTile[] tileSet in multiTiles)
                {
                    for (int j = 0; j < tileSet.Length; ++j)
                    {
                        if ((TileData.ItemTable[tileSet[j].ID & 0x3FFF].Flags & (TileFlag.Impassable | TileFlag.Surface)) != 0)
                        {
                            multiTiles.Free();
                            return BlockHousePlacement(from, HousePlacementResult.BadStatic, "BadStatic - multi tile blocking yard clearance");
                        }
                    }
                }

                multiTiles.Free();
            }

            return HousePlacementResult.Valid;
        }
        private static bool IsRoadTile(int landID)
        {
            for (int i = 0; i < m_RoadIDs.Length; i += 2)
            {
                if (landID >= m_RoadIDs[i] && landID <= m_RoadIDs[i + 1] && !ExceptionTiles(landID))
                    return true;
            }

            return false;
        }
        // ===================================================================
        // House Placement Debug Helper
        // ===================================================================

        private static HousePlacementResult BlockHousePlacement(
            Mobile? from,
            HousePlacementResult result,
            string reason,
            [CallerLineNumber] int lineNumber = 0)
        {
#if DEBUG
            if (from != null /*&& from.AccessLevel >= AccessLevel.GameMaster*/)
            {
                from.SendMessage($"[HousePlacement Debug] Blocked at line {lineNumber}: {reason}");
            }
#endif
            return result;
        }
        private static bool SpecialItemExclusion(Item item)
        {
            // inVisible items are excluded from placement restrictions
            if (item.Visible == false)
                return true;

            // no restriction on this item
            return false;
        }

        private static bool SpecialMobileExclusion(Mobile m)
        {
            // no restriction on this mobile
            return false;
        }

    }
}