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

/* Scripts/Engines/Pathing/Movement.cs
 * Changelog:
 *  1/4/2022, Yoar
 *      Fixed an issue which allowed players to move diagonally *through* objects.
 *      The client does not normally allow this kind of movement; when running into
 *      an obstacle diagonally, the client would force you to change facing and go
 *      around the obstacle.
 *      However, ClassicUO's "smooth doors" option allows players to pass through
 *      opened doors diagonally by negating the client's forced facing change.
 *      We fix this exploit by disallowing this type of movement from the server end.
 *      Exception: NPCs *are* able to do this type of movement (as per RunUO).
 *  12/24/05, Kit
 *		Added initial fly movement code for flying over impassable tiles 
 *		/static item type tiles, types flown over coded into mob and checked here.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Movement
{
    public class MovementImpl : IMovementImpl
    {
        private const int PersonHeight = 16;
        private const int StepHeight = 2;
        private int FlyZvalue = 0; //altered z value for mobs that fly
        private const TileFlag ImpassableSurface = TileFlag.Impassable | TileFlag.Surface;

        private static bool m_AlwaysIgnoreDoors;
        private static bool m_IgnoreMovableImpassables;

        public static bool AlwaysIgnoreDoors { get { return m_AlwaysIgnoreDoors; } set { m_AlwaysIgnoreDoors = value; } }
        public static bool IgnoreMovableImpassables { get { return m_IgnoreMovableImpassables; } set { m_IgnoreMovableImpassables = value; } }

        public static void Configure()
        {
            Movement.Impl = new MovementImpl();
        }

        private MovementImpl()
        {
        }

        private bool IsOk(MovementObject obj_start, bool ignoreDoors, int ourZ, int ourTop, StaticTile[] tiles, ArrayList items)
        {
            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile check = tiles[i];
                ItemData itemData = TileData.ItemTable[check.ID & 0x3FFF];

                if ((itemData.Flags & ImpassableSurface) != 0) // Impassable || Surface
                {
                    //we can fly test what we can fly over defined in mobile array
                    if (obj_start.CanFly && obj_start.FlyArray != null)
                    {
                        //look through are array of fly tiles
                        for (int x = 0; x < obj_start.FlyArray.Length; x++)
                        {
                            if (check.ID == obj_start.FlyArray[x])
                            {
                                FlyZvalue = check.Z + itemData.CalcHeight;
                                continue;
                            }
                        }
                        if (FlyZvalue != 0)
                            continue;

                    }

                    int checkZ = check.Z;
                    int checkTop = checkZ + itemData.CalcHeight;

                    if (checkTop > ourZ && ourTop > checkZ)
                        return false;
                }
            }

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = (Item)items[i];
                int itemID = item.ItemID & 0x3FFF;
                ItemData itemData = TileData.ItemTable[itemID];
                TileFlag flags = itemData.Flags;

                if ((flags & ImpassableSurface) != 0) // Impassable || Surface
                {
                    if (ignoreDoors && ((flags & TileFlag.Door) != 0 || itemID == 0x692 || itemID == 0x846 || itemID == 0x873 || (itemID >= 0x6F5 && itemID <= 0x6F6)))
                        continue;

                    int checkZ = item.Z;
                    int checkTop = checkZ + itemData.CalcHeight;

                    if (checkTop > ourZ && ourTop > checkZ)
                        return false;
                }
            }

            return true;
        }

        private ArrayList[] m_Pools = new ArrayList[5]
            {
                new ArrayList(), new ArrayList(),
                new ArrayList(), new ArrayList(),
                new ArrayList()
            };

        private bool Check(MovementObject obj_start, ArrayList items, int x, int y, int startTop, int startZ, out int newZ)
        {
            newZ = 0;

            StaticTile[] tiles = obj_start.Map.Tiles.GetStaticTiles(x, y, true);
            LandTile landTile = obj_start.Map.Tiles.GetLandTile(x, y);
#if false
            bool landBlocks = (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Impassable) != 0;
            bool considerLand = !landTile.Ignored;

            if (landBlocks && obj_start.CanSwim && (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Wet) != 0)
                landBlocks = false;
            else if (obj_start.CantWalkLand && (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Wet) == 0)
                landBlocks = true;
#else
            bool landBlocks = (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Impassable) != 0;
            bool wet = ((TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Wet) != 0);
            bool considerLand = !landTile.Ignored;

            if (landBlocks && obj_start.CanSwim && wet)
                landBlocks = false;
            else if (obj_start.CantWalkLand && !wet)
                landBlocks = true;
#endif
            int landZ = 0, landCenter = 0, landTop = 0;

            obj_start.Map.GetAverageZ(x, y, ref landZ, ref landCenter, ref landTop);

            bool moveIsOk = false;

            int stepTop = startTop + StepHeight;
            int checkTop = startZ + PersonHeight;

            bool ignoreDoors = (m_AlwaysIgnoreDoors || !obj_start.Alive || obj_start.BodyID == 0x3DB || obj_start.IsDeadBondedPet);

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile tile = tiles[i];
                ItemData itemData = TileData.ItemTable[tile.ID & 0x3FFF];
                TileFlag flags = itemData.Flags;

                if ((flags & ImpassableSurface) == TileFlag.Surface || (obj_start.CanSwim && (flags & TileFlag.Wet) != 0)) // Surface && !Impassable
                {
                    if (obj_start.CantWalkLand && (flags & TileFlag.Wet) == 0)
                        continue;

                    int itemZ = tile.Z;
                    int itemTop = itemZ;
                    int ourZ = itemZ + itemData.CalcHeight;
                    int ourTop = ourZ + PersonHeight;
                    int testTop = checkTop;

                    if (moveIsOk)
                    {
                        int cmp = Math.Abs(ourZ - obj_start.StartingLocation.Z) - Math.Abs(newZ - obj_start.StartingLocation.Z);

                        if (cmp > 0 || (cmp == 0 && ourZ > newZ))
                            continue;
                    }

                    if (ourZ + PersonHeight > testTop)
                        testTop = ourZ + PersonHeight;

                    if (!itemData.Bridge)
                        itemTop += itemData.Height;

                    if (stepTop >= itemTop)
                    {
                        int landCheck = itemZ;

                        if (itemData.Height >= StepHeight)
                            landCheck += StepHeight;
                        else
                            landCheck += itemData.Height;

                        if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ)
                            continue;

                        if (IsOk(obj_start, ignoreDoors, ourZ, testTop, tiles, items))
                        {
                            newZ = ourZ;
                            moveIsOk = true;
                        }
                    }
                }
            }

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = (Item)items[i];
                ItemData itemData = item.ItemData;//TileData.ItemTable[item.ItemID & 0x3FFF];
                TileFlag flags = itemData.Flags;

                if ((flags & ImpassableSurface) == TileFlag.Surface || (obj_start.CanSwim && (flags & TileFlag.Wet) != 0)) // Surface && !Impassable
                {
                    if (obj_start.CantWalkLand && (flags & TileFlag.Wet) == 0)
                        continue;

                    int itemZ = item.Z;
                    int itemTop = itemZ;
                    int ourZ = itemZ + itemData.CalcHeight;
                    int ourTop = ourZ + PersonHeight;
                    int testTop = checkTop;

                    if (moveIsOk)
                    {
                        int cmp = Math.Abs(ourZ - obj_start.StartingLocation.Z) - Math.Abs(newZ - obj_start.StartingLocation.Z);

                        if (cmp > 0 || (cmp == 0 && ourZ > newZ))
                            continue;
                    }

                    if (ourZ + PersonHeight > testTop)
                        testTop = ourZ + PersonHeight;

                    if (!itemData.Bridge)
                        itemTop += itemData.Height;

                    if (stepTop >= itemTop)
                    {
                        int landCheck = itemZ;

                        if (itemData.Height >= StepHeight)
                            landCheck += StepHeight;
                        else
                            landCheck += itemData.Height;

                        if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ)
                            continue;

                        if (IsOk(obj_start, ignoreDoors, ourZ, testTop, tiles, items))
                        {
                            newZ = ourZ;
                            moveIsOk = true;
                        }
                    }
                }
            }

            if (considerLand && !landBlocks && stepTop >= landZ)
            {
                int ourZ = landCenter;
                int ourTop = ourZ + PersonHeight;
                int testTop = checkTop;

                if (ourZ + PersonHeight > testTop)
                    testTop = ourZ + PersonHeight;

                bool shouldCheck = true;

                if (moveIsOk)
                {
                    int cmp = Math.Abs(ourZ - obj_start.StartingLocation.Z) - Math.Abs(newZ - obj_start.StartingLocation.Z);

                    if (cmp > 0 || (cmp == 0 && ourZ > newZ))
                        shouldCheck = false;
                }

                if (shouldCheck && IsOk(obj_start, ignoreDoors, ourZ, testTop, tiles, items))
                {
                    //if we fly assign adjusted z value and pass back to mobile.cs
                    if (FlyZvalue != 0)
                    {
                        newZ = FlyZvalue;
                        FlyZvalue = 0;
                    }
                    else
                        newZ = ourZ; //here is where fly z value needs modified

                    moveIsOk = true;
                }
            }

            return moveIsOk;
        }

        public bool CheckMovement(MovementObject obj_start, Point3D loc, Direction d, out int newZ)
        {
            if (obj_start.Map == null || obj_start.Map == Map.Internal)
            {
                newZ = 0;
                return false;
            }

            int xStart = loc.X;
            int yStart = loc.Y;
            int xForward = xStart, yForward = yStart;
            int xRight = xStart, yRight = yStart;
            int xLeft = xStart, yLeft = yStart;

            bool checkDiagonals = ((int)d & 0x1) == 0x1;

            Offset(d, ref xForward, ref yForward);
            Offset((Direction)(((int)d - 1) & 0x7), ref xLeft, ref yLeft);
            Offset((Direction)(((int)d + 1) & 0x7), ref xRight, ref yRight);

            if (xForward < 0 || yForward < 0 || xForward >= obj_start.Map.Width || yForward >= obj_start.Map.Height)
            {
                newZ = 0;
                return false;
            }

            int startZ, startTop;

            ArrayList itemsStart = m_Pools[0];
            ArrayList itemsForward = m_Pools[1];
            ArrayList itemsLeft = m_Pools[2];
            ArrayList itemsRight = m_Pools[3];
            //Dictionary<Server.Serial, object>.ValueCollection items;
            List<Item> items;

            bool ignoreMovableImpassables = m_IgnoreMovableImpassables;
            TileFlag reqFlags = ImpassableSurface;

            if (obj_start.CanSwim)
                reqFlags |= TileFlag.Wet;

            if (checkDiagonals)
            {
                Sector sectorStart = obj_start.Map.GetSector(xStart, yStart);
                Sector sectorForward = obj_start.Map.GetSector(xForward, yForward);
                Sector sectorLeft = obj_start.Map.GetSector(xLeft, yLeft);
                Sector sectorRight = obj_start.Map.GetSector(xRight, yRight);

                ArrayList sectors = m_Pools[4];

                sectors.Add(sectorStart);

                if (!sectors.Contains(sectorForward))
                    sectors.Add(sectorForward);

                if (!sectors.Contains(sectorLeft))
                    sectors.Add(sectorLeft);

                if (!sectors.Contains(sectorRight))
                    sectors.Add(sectorRight);

                for (int i = 0; i < sectors.Count; ++i)
                {
                    Sector sector = (Sector)sectors[i];

                    items = sector.Items;

                    foreach (Item item in items)
                    {
                        if (item == null)
                            continue;

                        if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                            continue;

                        if ((item.ItemData.Flags & reqFlags) == 0)
                            continue;

                        if (sector == sectorStart && item.AtWorldPoint(xStart, yStart) && item.ItemID < 0x4000)
                            itemsStart.Add(item);
                        else if (sector == sectorForward && item.AtWorldPoint(xForward, yForward) && item.ItemID < 0x4000)
                            itemsForward.Add(item);
                        else if (sector == sectorLeft && item.AtWorldPoint(xLeft, yLeft) && item.ItemID < 0x4000)
                            itemsLeft.Add(item);
                        else if (sector == sectorRight && item.AtWorldPoint(xRight, yRight) && item.ItemID < 0x4000)
                            itemsRight.Add(item);
                    }
                }

                if (m_Pools[4].Count > 0)
                    m_Pools[4].Clear();
            }
            else
            {
                Sector sectorStart = obj_start.Map.GetSector(xStart, yStart);
                Sector sectorForward = obj_start.Map.GetSector(xForward, yForward);

                if (sectorStart == sectorForward)
                {
                    items = sectorStart.Items;

                    foreach (Item item in items)
                    {
                        if (item == null)
                            continue;

                        if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                            continue;

                        if ((item.ItemData.Flags & reqFlags) == 0)
                            continue;

                        if (item.AtWorldPoint(xStart, yStart) && item.ItemID < 0x4000)
                            itemsStart.Add(item);
                        else if (item.AtWorldPoint(xForward, yForward) && item.ItemID < 0x4000)
                            itemsForward.Add(item);
                    }
                }
                else
                {
                    items = sectorForward.Items;

                    foreach (Item item in items)
                    {
                        if (item == null)
                            continue;

                        if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                            continue;

                        if ((item.ItemData.Flags & reqFlags) == 0)
                            continue;

                        if (item.AtWorldPoint(xForward, yForward) && item.ItemID < 0x4000)
                            itemsForward.Add(item);
                    }

                    items = sectorStart.Items;

                    foreach (Item item in items)
                    {
                        if (item == null)
                            continue;

                        if (ignoreMovableImpassables && item.Movable && item.ItemData.Impassable)
                            continue;

                        if ((item.ItemData.Flags & reqFlags) == 0)
                            continue;

                        if (item.AtWorldPoint(xStart, yStart) && item.ItemID < 0x4000)
                            itemsStart.Add(item);
                    }
                }
            }

            GetStartZ(obj_start, loc, itemsStart, out startZ, out startTop);

            bool moveIsOk = Check(obj_start, itemsForward, xForward, yForward, startTop, startZ, out newZ);


            if (moveIsOk && checkDiagonals)
            {
                int hold;

                if (obj_start.Player)
                {
                    // if we are a player, both the tile to our left AND the tile to our right must be clear
                    if (!Check(obj_start, itemsLeft, xLeft, yLeft, startTop, startZ, out hold) || !Check(obj_start, itemsRight, xRight, yRight, startTop, startZ, out hold))
                        moveIsOk = false;
                }
                else
                {
                    // if we are an NPC, either the tile to our left OR the tile to our right must be clear
                    if (!Check(obj_start, itemsLeft, xLeft, yLeft, startTop, startZ, out hold) && !Check(obj_start, itemsRight, xRight, yRight, startTop, startZ, out hold))
                        moveIsOk = false;
                }
            }

            for (int i = 0; i < (checkDiagonals ? 4 : 2); ++i)
            {
                if (m_Pools[i].Count > 0)
                    m_Pools[i].Clear();
            }

            if (!moveIsOk)
                newZ = startZ;

            return moveIsOk;
        }

        public bool CheckMovement(MovementObject obj_start, Direction d, out int newZ)
        {   // public bool CheckMovement(MovementObject obj_start,    Point3D loc, Direction d, out int newZ)
            return CheckMovement(obj_start, obj_start.StartingLocation, d, out newZ);
        }

        private void GetStartZ(MovementObject obj_start, Point3D loc, ArrayList itemList, out int zLow, out int zTop)
        {
            int xCheck = loc.X, yCheck = loc.Y;

            LandTile landTile = obj_start.Map.Tiles.GetLandTile(xCheck, yCheck);
            int landZ = 0, landCenter = 0, landTop = 0;
            bool landBlocks = (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Impassable) != 0;

            if (landBlocks && obj_start.CanSwim && (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Wet) != 0)
                landBlocks = false;
            else if (obj_start.CantWalkLand && (TileData.LandTable[landTile.ID & 0x3FFF].Flags & TileFlag.Wet) == 0)
                landBlocks = true;

            obj_start.Map.GetAverageZ(xCheck, yCheck, ref landZ, ref landCenter, ref landTop);

            bool considerLand = !landTile.Ignored;

            int zCenter = zLow = zTop = 0;
            bool isSet = false;

            if (considerLand && !landBlocks && loc.Z >= landCenter)
            {
                zLow = landZ;
                zCenter = landCenter;

                if (!isSet || landTop > zTop)
                    zTop = landTop;

                isSet = true;
            }

            StaticTile[] staticTiles = obj_start.Map.Tiles.GetStaticTiles(xCheck, yCheck, true);

            for (int i = 0; i < staticTiles.Length; ++i)
            {
                StaticTile tile = staticTiles[i];
                ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

                int calcTop = (tile.Z + id.CalcHeight);

                if ((!isSet || calcTop >= zCenter) && ((id.Flags & TileFlag.Surface) != 0 || (obj_start.CanSwim && (id.Flags & TileFlag.Wet) != 0)) && loc.Z >= calcTop)
                {
                    if ((obj_start.CantWalkLand) && (id.Flags & TileFlag.Wet) == 0)
                        continue;

                    zLow = tile.Z;
                    zCenter = calcTop;

                    int top = tile.Z + id.Height;

                    if (!isSet || top > zTop)
                        zTop = top;

                    isSet = true;
                }
            }

            for (int i = 0; i < itemList.Count; ++i)
            {
                Item item = (Item)itemList[i];

                ItemData id = item.ItemData;

                int calcTop = item.Z + id.CalcHeight;

                if ((!isSet || calcTop >= zCenter) && ((id.Flags & TileFlag.Surface) != 0 || (obj_start.CanSwim && (id.Flags & TileFlag.Wet) != 0)) && loc.Z >= calcTop)
                {
                    if (obj_start.CantWalkLand && (id.Flags & TileFlag.Wet) == 0)
                        continue;

                    zLow = item.Z;
                    zCenter = calcTop;

                    int top = item.Z + id.Height;

                    if (!isSet || top > zTop)
                        zTop = top;

                    isSet = true;
                }
            }

            if (!isSet)
                zLow = zTop = loc.Z;
            else if (loc.Z > zTop)
                zTop = loc.Z;
        }

        public void Offset(Direction d, ref int x, ref int y)
        {
            switch (d & Direction.Mask)
            {
                case Direction.North: --y; break;
                case Direction.South: ++y; break;
                case Direction.West: --x; break;
                case Direction.East: ++x; break;
                case Direction.Right: ++x; --y; break;
                case Direction.Left: --x; ++y; break;
                case Direction.Down: ++x; ++y; break;
                case Direction.Up: --x; --y; break;
            }
        }
    }
}