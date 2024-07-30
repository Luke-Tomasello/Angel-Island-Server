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

using System;

namespace Server
{
    public enum Notification
    {
        None,
        WeaponStatus,   // weapon is taking damage
        ArmorStatus,    // armor is taking damage
        ClothingStatus, // clothing is taking damage
        Destroyed,      // armor or weapon destroyed
        AmmoStatus,     // ammo being consumed
    }
    public interface IEntity : IPoint3D, IComparable, IComparable<IEntity>
    {
        Serial Serial { get; }
        Point3D Location { get; }
        Map Map { get; }
        bool Deleted { get; }
        public DateTime Created { get; }

        void Delete();
        void ProcessDelta();
        void Notify(Notification notification, params object[] args);
    }

    public class Entity : IEntity, IComparable<Entity>
    {
        public int CompareTo(IEntity other)
        {
            if (other == null)
                return -1;

            return m_Serial.CompareTo(other.Serial);
        }

        public int CompareTo(Entity other)
        {
            return this.CompareTo((IEntity)other);
        }

        public int CompareTo(object other)
        {
            if (other == null || other is IEntity)
                return this.CompareTo((IEntity)other);

            throw new ArgumentException();
        }

        private Serial m_Serial;
        private Point3D m_Location;
        private Map m_Map;
        private DateTime m_CreationTime;

        public Entity(Serial serial, Point3D loc, Map map)
        {
            m_Serial = serial;
            m_Location = loc;
            m_Map = map;
            m_CreationTime = DateTime.UtcNow;
        }

        public Serial Serial
        {
            get
            {
                return m_Serial;
            }
        }

        public Point3D Location
        {
            get
            {
                return m_Location;
            }
        }

        public int X
        {
            get
            {
                return m_Location.X;
            }
        }

        public int Y
        {
            get
            {
                return m_Location.Y;
            }
        }

        public int Z
        {
            get
            {
                return m_Location.Z;
            }
        }

        public Map Map
        {
            get
            {
                return m_Map;
            }
        }

        public bool Deleted
        {
            get
            {
                return false;
            }
        }
        public DateTime Created
        {
            get
            {
                return m_CreationTime;
            }
        }
        public void Delete()
        {
        }

        public void ProcessDelta()
        {
        }
        public void Notify(Notification notification, params object[] args)
        {

        }
    }
}