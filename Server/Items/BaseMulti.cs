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

/* Server/Items/BaseMulti.cs
 * CHANGELOG:
 *  8/13/2023, Adam
 *      Add FindAt(Point3D p) to find camps that may not CONTAIN p.
 *  3/25/07, Adam
 *      Add FindAll() function to locate all multis at a given point.
 */

using System.Collections;

namespace Server.Items
{
    public class BaseMulti : Item
    {
        [Constructable]
        public BaseMulti(int itemID)
            : base(itemID)
        {
            Movable = false;
        }

        public BaseMulti(Serial serial)
            : base(serial)
        {
        }

        public virtual void RefreshComponents()
        {
            if (Parent != null)
                return;

            Map map = Map;

            if (map != null)
            {
                map.OnLeave(this);
                map.OnEnter(this);
            }
        }

        public override int LabelNumber
        {
            get
            {
                MultiComponentList mcl = this.Components;

                if (mcl.List.Length > 0)
                    return 1020000 + (mcl.List[0].m_ItemID & 0x3FFF);

                return base.LabelNumber;
            }
        }
        public virtual bool AllowsRelativeDrop
        {
            get { return false; }
        }
        public override int GetMaxUpdateRange()
        {
            return 22;
        }

        public override int GetUpdateRange(Mobile m)
        {
            return 22;
        }

        public virtual MultiComponentList Components
        {
            get
            {
                return MultiData.GetComponents(ItemID);
            }
        }

        public virtual bool Contains(Point2D p)
        {
            return Contains(p.m_X, p.m_Y);
        }

        public virtual bool Contains(Point3D p)
        {
            return Contains(p.m_X, p.m_Y);
        }

        public virtual bool Contains(IPoint3D p)
        {
            return Contains(p.X, p.Y);
        }

        public virtual bool Contains(int x, int y)
        {
            MultiComponentList mcl = this.Components;

            x -= this.X + mcl.Min.m_X;
            y -= this.Y + mcl.Min.m_Y;

            return x >= 0
                && x < mcl.Width
                && y >= 0
                && y < mcl.Height
                && mcl.Tiles[x][y].Length > 0;
        }

        public bool Contains(Mobile m)
        {
            if (m.Map == this.Map)
                return Contains(m.X, m.Y);
            else
                return false;
        }

        public bool Contains(Item item)
        {
            if (item.Map == this.Map)
                return Contains(item.X, item.Y);
            else
                return false;
        }

        public virtual bool Inside(int x, int y)
        {
            MultiComponentList mcl = this.Components;

            x -= this.X + mcl.Min.m_X;
            y -= this.Y + mcl.Min.m_Y;

            return x >= 0
                && x < mcl.Width
                && y >= 0
                && y < mcl.Height;
        }

        public static BaseMulti Find(Point3D p, Map map)
        {
            if (map == null)
                return null;

            Sector sector = map.GetSector(p);

            foreach (BaseMulti mult in sector.Multis)
            {
                if (mult == null)
                    continue;

                if (mult.Contains(p) || mult.Inside(p.X, p.Y))
                    return mult;
            }

            return null;
        }
        public static BaseMulti FindAt(Point3D p, Map map)
        {   // Useful for finding camps - the camp is AT p, but does not CONTAIN p
            if (map == null)
                return null;

            Sector sector = map.GetSector(p);

            foreach (BaseMulti mult in sector.Multis)
            {
                if (mult == null)
                    continue;

                if (mult.Contains(p) || mult.Inside(p.X, p.Y) || mult.Location == p)
                    return mult;
            }

            return null;
        }
        public static ArrayList FindAll(Point3D p, Map map)
        {
            ArrayList all = new ArrayList();

            if (map == null)
                return all;

            Sector sector = map.GetSector(p);

            foreach (BaseMulti mult in sector.Multis)
            {
                if (mult == null)
                    continue;

                if (mult.Contains(p) || mult.Inside(p.X, p.Y))
                    all.Add(mult);
            }

            return all;
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
    }
}