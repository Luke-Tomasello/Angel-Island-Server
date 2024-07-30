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

/* Scripts\Items\Special\Holiday\Winter\Mistletoe.cs
 * Changelog:
 *	11/27/21, Yoar
 *		Initial version
 */

using Server.Multis;

namespace Server.Items
{
    // note: RunUO's version derives from BaseWallAddon
    public class MistletoeAddon : Item, IDyable, IHolidayItem
    {
        private int m_Year;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Year
        {
            get { return m_Year; }
            set { m_Year = value; InvalidateProperties(); }
        }

        [Constructable]
        public MistletoeAddon()
            : this(Utility.RandomDyedHue(), 2004)
        {
        }

        [Constructable]
        public MistletoeAddon(int hue, int year)
            : base(0x2375)
        {
            Movable = false;
            Hue = hue;
            m_Year = year;
        }

        public MistletoeAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteEncodedInt(m_Year);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Year = reader.ReadEncodedInt();
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Year == 2004)
                LabelTo(from, 1070880); // Winter 2004
            else
                LabelTo(from, "Winter {0}", m_Year);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Year == 2004)
                list.Add(1070880); // Winter 2004
            else
                list.Add("Winter {0}", m_Year);
        }

        public override void OnDoubleClick(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsCoOwner(from))
            {
                if (from.InRange(GetWorldLocation(), 1))
                {
                    from.AddToBackpack(new MistletoeDeed(this.Hue, m_Year));
                    Delete();
                }
                else
                {
                    from.SendLocalizedMessage(500295); // You are too far away to do that.
                }
            }
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house != null && house.IsCoOwner(from))
            {
                if (from.InRange(GetWorldLocation(), 1))
                {
                    Hue = sender.DyedHue;
                    return true;
                }
                else
                {
                    from.SendLocalizedMessage(500295); // You are too far away to do that.
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }

    // note: RunUO's version derives from BaseWallAddonDeed
    [Flipable(0x14F0, 0x14EF)]
    public class MistletoeDeed : Item, IHolidayItem
    {
        public override int LabelNumber { get { return 1070882; } } // Mistletoe Deed

        private int m_Year;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Year
        {
            get { return m_Year; }
            set { m_Year = value; InvalidateProperties(); }
        }

        [Constructable]
        public MistletoeDeed()
            : this(0, 2004)
        {
        }

        [Constructable]
        public MistletoeDeed(int hue, int year)
            : base(0x14F0)
        {
            Hue = hue;
            m_Year = year;
        }

        public MistletoeDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteEncodedInt(m_Year);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Year = reader.ReadEncodedInt();
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Year == 2004)
                LabelTo(from, 1070880); // Winter 2004
            else
                LabelTo(from, "Winter {0}", m_Year);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Year == 2004)
                list.Add(1070880); // Winter 2004
            else
                list.Add("Winter {0}", m_Year);
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

                    int itemID = 0;

                    if (northWall)
                        itemID = 0x2375;
                    else if (westWall)
                        itemID = 0x2374;
                    else
                        from.SendLocalizedMessage(1062840); // The decoration must be placed next to a wall.

                    if (itemID > 0)
                    {
                        Item addon = new MistletoeAddon(this.Hue, m_Year);

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