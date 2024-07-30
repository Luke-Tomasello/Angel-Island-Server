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

/* Scripts/Engines/Pathing/SectorPathAlgorithm.cs
 * CHANGELOG
 *  7/1/23, Yoar
 *      Removed old location generator
 *      Reworked OnSingleClick
 *  1/3/22, Adam
 *      Add exception handling around OnSingleClick() (see note below)
 *      If pins == zero (which is a bug if level > 0) call the base class OnSingleClick (emergency patch, needs review)
 *      Note: base class (item) OnSingleClick IS wrapped in a try/catch block. So how how did it crash the server?
 *  12/21/21, Yoar
 *      Revamped sea chart location generation completely.
 *      - Sea chart locations are now generated in "Scrips/Misc/SeaTreasure.cs."
 *      - The new algorithm is *much* faster!
 *      Cleanups in SeaChart.cs.
 *  12/6/21, Adam
 *      Rewrite all I/O to obsolete BinaryFormatter
 *      https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2300
 */

using Server.Misc;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    [Flags]
    public enum MapPinFlag : byte
    {
        None = 0x00,

        Pin1 = 0x01,
        Pin2 = 0x02,
        Pin3 = 0x04,
        Pin4 = 0x08,
        Pin5 = 0x10,
        Pin6 = 0x20,
        Pin7 = 0x40,
        Pin8 = 0x80,
    }

    public class SeaChart : MapItem
    {
        public override int LabelNumber { get { return 1015232; } } // sea chart
        public override bool ReadOnly { get { return m_level > 0; } }

        private int m_level = 0; // player crafted is level 0
        private MapPinFlag m_Completed;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get { return m_level; }
            set { m_level = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MapPinFlag Completed
        {
            get { return m_Completed; }
            set { m_Completed = value; InvalidateProperties(); }
        }

        public bool HasCompleted(int index)
        {
            if (index < 0 || index >= 8)
                return false;

            MapPinFlag flag = (MapPinFlag)(0x1 << index);

            return (m_Completed & flag) != 0;
        }

        public void SetCompleted(int index, bool value)
        {
            if (index < 0 || index >= 8)
                return;

            MapPinFlag flag = (MapPinFlag)(0x1 << index);

            if (value)
                Completed |= flag;
            else
                Completed &= ~flag;
        }

        private static readonly Point2D[] m_EmptyPins = new Point2D[0];

        public Point2D[] GetWorldPins()
        {
            ArrayList pins = this.Pins;

            if (pins.Count == 0)
                return m_EmptyPins;

            List<Point2D> list = null;

            for (int i = 0; i < pins.Count; i++)
            {
                Point2D pin = (Point2D)pins[i];

                int worldX, worldY;
                ConvertToWorld(pin.X, pin.Y, out worldX, out worldY);

                if (list == null)
                    list = new List<Point2D>();

                list.Add(new Point2D(worldX, worldY));
            }

            if (list == null)
                return m_EmptyPins;

            return list.ToArray();
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D WorldPin1 { get { return GetWorldPin(0); } set { SetWorldPin(0, value); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D WorldPin2 { get { return GetWorldPin(1); } set { SetWorldPin(1, value); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D WorldPin3 { get { return GetWorldPin(2); } set { SetWorldPin(2, value); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D WorldPin4 { get { return GetWorldPin(3); } set { SetWorldPin(3, value); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D WorldPin5 { get { return GetWorldPin(4); } set { SetWorldPin(4, value); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D WorldPin6 { get { return GetWorldPin(5); } set { SetWorldPin(5, value); } }

        private Point2D GetWorldPin(int index)
        {
            ArrayList pins = this.Pins;

            if (index >= 0 && index < pins.Count)
            {
                Point2D pin = (Point2D)pins[index];

                int worldX, worldY;
                ConvertToWorld(pin.X, pin.Y, out worldX, out worldY);

                return new Point2D(worldX, worldY);
            }

            return Point2D.Zero;
        }

        private void SetWorldPin(int index, Point2D loc)
        {
            ArrayList pins = this.Pins;

            if (index < 0 || index > 5)
                return;

            while (index >= pins.Count)
                pins.Add(new Point2D(0, 0));

            int mapX, mapY;
            ConvertToMap(loc.X, loc.Y, out mapX, out mapY);

            pins[index] = new Point2D(mapX, mapY);
        }

        [Constructable]
        public SeaChart()
            : this(0)
        {
        }

        [Constructable]
        public SeaChart(int level)
            : base()
        {
            SetLevel(level);
        }

        public override void CraftInit(Mobile from)
        {
            double skillValue = from.Skills[SkillName.Cartography].Value;
            int dist = 64 + (int)(skillValue * 10);

            if (dist < 200)
                dist = 200;

            int size = 24 + (int)(skillValue * 3.3);

            if (size < 200)
                size = 200;
            else if (size > 400)
                size = 400;

            SetDisplay(from.X - dist, from.Y - dist, from.X + dist, from.Y + dist, size, size);
        }

        public void SetLevel(int level)
        {
            m_level = level;

            this.Pins.Clear();

            if (m_level == 0)
            {
                SetDisplay(0, 0, 5119, 4095, 400, 400); // reset display
                return;
            }

            switch (m_level)
            {
                case 1: Name = "a fair fishing spot"; break;
                case 2: Name = "a decent fishing spot"; break;
                case 3: Name = "a good fishing spot"; break;
                case 4: Name = "a fabled fishing spot"; break;
                case 5: Name = "a legendary fishing spot"; break;
            }

            int dist = GetDist(m_level);
            int size = GetSize(m_level);

            // init. width, height and bounds
            SetDisplay(0, 0, 2 * dist, 2 * dist, size, size);

            Point2D[] pins = SeaTreasure.GetRandomPins(level);

            if (pins.Length != 0)
            {
                // center the map on the first pin
                CartographersSextant.CenterMap(this, pins[0]);

                // place pins
                foreach (Point2D pin in pins)
                {
                    if (Bounds.Contains(pin))
                        AddWorldPin(pin.X, pin.Y);
                }
            }
        }

        public static int GetDist(int level)
        {
            return Math.Max(200, 64 + (int)(10.0 * GetSkillValue(level)));
        }

        public static int GetSize(int level)
        {
            return Math.Max(200, Math.Min(400, 24 + (int)(3.3 * GetSkillValue(level))));
        }

        private static double GetSkillValue(int level)
        {
            switch (level)
            {
                default:
                case 1: return 20.0;
                case 2: return 30.0;
                case 3: return 50.0;
                case 4: return 90.0;
                case 5: return 100.0;
            }
        }

        public SeaChart(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            int pins = this.Pins.Count;

            if (m_level > 0 && pins > 0)
            {
                int done = 0;

                for (int i = 0; i < 8 && i < pins; i++)
                {
                    if (HasCompleted(i))
                        done++;
                }

                int perc = 100 * done / pins;

                LabelTo(from, "[{0}% complete]", perc);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2);

            writer.Write((int)m_level);
            writer.Write((byte)m_Completed);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                case 1:
                    {
                        m_level = reader.ReadInt();

                        if (version >= 2)
                        {
                            m_Completed = (MapPinFlag)reader.ReadByte();
                        }
                        else if (m_level > 0)
                        {
                            for (int i = 0; i < 6; i++)
                                SetCompleted(i, reader.ReadBool());
                        }

                        break;
                    }
            }
        }
    }
}