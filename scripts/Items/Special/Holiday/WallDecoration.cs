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

/* Scripts\Items\Special\Holiday\WallDecoration.cs
 * Changelog:
 *	11/27/21, Yoar
 *		Initial version
 */

using Server.Multis;

namespace Server.Items
{
    public abstract class BaseWallDecoration : Item, IAddon
    {
        public abstract Item Deed { get; }

        public BaseWallDecoration(int itemID)
            : base(itemID)
        {
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsCoOwner(from))
            {
                if (from.InRange(GetWorldLocation(), 1))
                {
                    Item deed = Deed;

                    if (deed != null)
                    {
                        from.AddToBackpack(Deed);
                        Delete();
                    }
                }
                else
                {
                    from.SendLocalizedMessage(500295); // You are too far away to do that.
                }
            }
        }

        public BaseWallDecoration(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x80); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version;

            if ((Utility.PeekByte(reader) & 0x80) == 0)
            {
                version = 0; // old version
            }
            else
            {
                version = reader.ReadByte();

                switch (version)
                {
                    case 0x80:
                        {
                            break;
                        }
                }
            }
        }
    }

    public abstract class BaseWallDecorationDeed : Item
    {
        public override double DefaultWeight { get { return 1.0; } }

        public BaseWallDecorationDeed()
            : base(0x14F0)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house != null && house.IsCoOwner(from))
                {
                    bool northWall = WreathDeed.IsWall(from.X, from.Y - 1, from.Z, from.Map);
                    bool westWall = WreathDeed.IsWall(from.X - 1, from.Y, from.Z, from.Map);

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

                    Item addon = null;

                    if (northWall)
                        addon = GetAddon(false);
                    else if (westWall)
                        addon = GetAddon(true);
                    else
                        from.SendLocalizedMessage(1062840); // The decoration must be placed next to a wall.

                    if (addon != null)
                    {
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

        public abstract Item GetAddon(bool east);

        public BaseWallDecorationDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x80); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version;

            if ((Utility.PeekByte(reader) & 0x80) == 0)
            {
                version = 0; // old version
            }
            else
            {
                version = reader.ReadByte();

                switch (version)
                {
                    case 0x80:
                        {
                            break;
                        }
                }
            }
        }
    }
}