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

/* Items/misc/WeatherController.cs
 * CHANGELOG:
 * 	3/7/23, Yoar
 * 		Initial version. Used to spawn dynamic Weather objects.
 */

using Server.Misc;
using System;

namespace Server.Items
{
    public class WeatherController : Item
    {
        public override string DefaultName { get { return "Weather Controller"; } }

        private int m_Range;
        private int m_Temperature;
        private int m_ChanceOfPercipitation;
        private int m_ChanceOfExtremeTemperature;
        private TimeSpan m_Interval;
        private Weather m_Weather;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Temperature
        {
            get { return m_Temperature; }
            set { m_Temperature = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ChanceOfPercipitation
        {
            get { return m_ChanceOfPercipitation; }
            set { m_ChanceOfPercipitation = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ChanceOfExtremeTemperature
        {
            get { return m_ChanceOfExtremeTemperature; }
            set { m_ChanceOfExtremeTemperature = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Interval
        {
            get { return m_Interval; }
            set { m_Interval = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Registered
        {
            get { return (m_Weather != null); }
            set
            {
                if (Registered != value)
                {
                    if (value)
                        Register();
                    else
                        Unregister();

                    InvalidateProperties();
                }
            }
        }

        [Constructable]
        public WeatherController()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Range = 200;
            m_Temperature = +15;
            m_ChanceOfPercipitation = 50;
            m_ChanceOfExtremeTemperature = 5;
            m_Interval = TimeSpan.FromSeconds(30.0);
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        private void Unregister()
        {
            if (m_Weather != null)
            {
                m_Weather.Remove();
                m_Weather = null;
            }
        }

        private void Register()
        {
            Unregister();

            if (Map == null || Map == Map.Internal || m_Range <= 0)
                return;

            Rectangle2D[] area = new Rectangle2D[1];

            area[0] = new Rectangle2D(X - m_Range, Y - m_Range, 2 * m_Range + 1, 2 * m_Range + 1);

            m_Weather = new Weather(Map, area, m_Temperature, m_ChanceOfPercipitation, m_ChanceOfExtremeTemperature, m_Interval);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_Weather != null)
                Register();
        }

        public override void OnMapChange()
        {
            if (m_Weather != null)
                Register();
        }

        public override void OnAfterDelete()
        {
            Unregister();
        }

        public WeatherController(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Range);
            writer.Write((int)m_Temperature);
            writer.Write((int)m_ChanceOfPercipitation);
            writer.Write((int)m_ChanceOfExtremeTemperature);
            writer.Write((TimeSpan)m_Interval);
            writer.Write((bool)Registered);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Range = reader.ReadInt();
                        m_Temperature = reader.ReadInt();
                        m_ChanceOfPercipitation = reader.ReadInt();
                        m_ChanceOfExtremeTemperature = reader.ReadInt();
                        m_Interval = reader.ReadTimeSpan();
                        Registered = reader.ReadBool();

                        break;
                    }
            }
        }
    }
}