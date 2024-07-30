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

/* Scripts\Items\Special\Holiday\Winter\Wreath.cs
 * Changelog:
 *	11/27/21, Yoar
 *		Setting LootType to HolidayGifts.LootType
 */

using Server.Multis;

namespace Server.Items
{
    public class WreathAddon : Item
    {
        [Constructable]
        public WreathAddon()
            : this(Utility.RandomDyedHue())
        {
        }

        [Constructable]
        public WreathAddon(int hue)
            : base(0x232C)
        {
            Hue = hue;
            Movable = false;
        }

        public WreathAddon(Serial serial)
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
            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsCoOwner(from))
            {
                if (from.InRange(GetWorldLocation(), 1))
                {
                    from.AddToBackpack(new WreathDeed(this.Hue));
                    Delete();
                }
                else
                {
                    from.SendLocalizedMessage(500295); // You are too far away to do that.
                }
            }
        }
    }

    [Flipable(0x14F0, 0x14EF)]
    public class WreathDeed : Item
    {
        public override int LabelNumber { get { return 1062837; } } // holiday wreath deed

        [Constructable]
        public WreathDeed()
            : this(Utility.RandomDyedHue())
        {
        }

        [Constructable]
        public WreathDeed(int hue)
            : base(0x14F0)
        {
            Weight = 1.0;
            Hue = hue;
        }

        public WreathDeed(Serial serial)
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

        public static bool IsWall(int x, int y, int z, Map map)
        {
            if (map == null)
                return false;

            StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile t = tiles[i];
                ItemData id = TileData.ItemTable[t.ID & 0x3FFF];

                if ((id.Flags & TileFlag.Wall) != 0 && (z + 16) > t.Z && (t.Z + t.Height) > z)
                    return true;
            }

            return false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house != null && house.IsCoOwner(from))
                {
                    bool northWall = IsWall(from.X, from.Y - 1, from.Z, from.Map);
                    bool westWall = IsWall(from.X - 1, from.Y, from.Z, from.Map);

                    if (northWall && westWall)
                    {
                        switch (from.Direction & Direction.Mask)
                        {
                            case Direction.North:
                            case Direction.South: northWall = true; westWall = false; break;

                            case Direction.East:
                            case Direction.West: northWall = false; westWall = true; break;

                            default: from.SendMessage("Turn to face the wall on which to hang this decoration."); return;
                        }
                    }

                    int itemID = 0;

                    if (northWall)
                        itemID = 0x232C;
                    else if (westWall)
                        itemID = 0x232D;
                    else
                        from.SendLocalizedMessage(1062840); // The decoration must be placed next to a wall.

                    if (itemID > 0)
                    {
                        Item addon = new WreathAddon(this.Hue);

                        addon.ItemID = itemID;
                        addon.MoveToWorld(from.Location, from.Map);

                        house.Addons.Add(addon);
                        Delete();
                    }
                }
                else
                {
                    from.SendLocalizedMessage(502092); // You must be in your house to do this.
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }
    }
}