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

/* Scripts/Items/Skill Items/Harvest Tools/ResourceMap.cs
 * CHANGELOG:
 *  11/19/21, Yoar
 *      Added lumber resource maps.
 *  10/24/21, Yoar
 *      Rewrote resource maps to be more OSI accurate:
 *      1. While mining at a resource map location:
 *         a. The bank always yields the specified resource (and not a fallback resource),
 *         b. The bank never runs out of resources,
 *         until the map runs out of charges.
 *      2. The effect of resource maps no longer stacks with gargoyle's pickaxes.
 *      3. Increased the default number of charges.
 *  10/14/21, Yoar
 *      Added a constructor that takes only CraftResource.
 *  10/11/21, Yoar
 *      Resource maps can no longer point to T2A/dungeons.
 *  10/11/21, Yoar
 *      Added resource maps.
 */

using Server.Engines.Harvest;
using System;
using System.Collections;

namespace Server.Items
{
    public class ResourceMap : MapItem
    {
        public virtual HarvestDefinition HarvestDefinition
        {
            get
            {
                CraftResourceType type = CraftResources.GetType(m_Resource);

                if (type == CraftResourceType.Metal)
                    return Mining.System.OreAndStone;

                if (type == CraftResourceType.Wood)
                    return Lumberjacking.System.Definition;

                return null;
            }
        }

        public override string DefaultName { get { return "a resource map"; } }

        private CraftResource m_Resource;
        private int m_UsesMax;
        private int m_Uses;
        private Map m_BankMap;
        private Point2D m_BankLocation;

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; UpdateName(); UpdateHue(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesMax
        {
            get { return m_UsesMax; }
            set { m_UsesMax = value; UpdateName(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Uses
        {
            get { return m_Uses; }
            set { m_Uses = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map BankMap
        {
            get { return m_BankMap; }
            set { m_BankMap = value; UpdateMap(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D BankLocation
        {
            get { return m_BankLocation; }
            set { m_BankLocation = value; UpdateMap(); }
        }

        [Constructable]
        public ResourceMap()
            : this((CraftResource)Utility.RandomMinMax((int)CraftResource.DullCopper, (int)CraftResource.Valorite), 150, Map.Felucca)
        {
        }

        [Constructable]
        public ResourceMap(CraftResource res)
            : this(res, 150, Map.Felucca)
        {
        }

        [Constructable]
        public ResourceMap(CraftResource res, int uses)
            : this(res, uses, Map.Felucca)
        {
        }

        [Constructable]
        public ResourceMap(CraftResource res, int uses, Map bankMap)
            : base()
        {
            ReadOnly = true;
            m_Resource = res;
            m_UsesMax = m_Uses = uses;
            m_BankMap = bankMap;
            UpdateName();
            UpdateHue();
        }

        private void UpdateName()
        {
            string prefix;

            if (m_UsesMax >= 300)
                prefix = "a legendary";
            else if (m_UsesMax >= 200)
                prefix = "a fabled";
            else if (m_UsesMax >= 150)
                prefix = "a good";
            else if (m_UsesMax >= 100)
                prefix = "a decent";
            else
                prefix = "a fair";

            this.Name = string.Format("{0} {1} map", prefix, CraftResources.GetName(m_Resource).ToLower());
        }

        private void UpdateHue()
        {
            this.Hue = CraftResources.GetHue(m_Resource);
        }

        private void UpdateMap()
        {
            if ((m_BankMap != Map.Felucca && m_BankMap != Map.Trammel) || m_BankLocation == Point2D.Zero)
                return;

            CartographersSextant.CenterMap(this, m_BankLocation);

            Pins.Clear();

            if (this.Bounds.Contains(m_BankLocation))
                AddWorldPin(m_BankLocation.X, m_BankLocation.Y);
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_UsesMax > 0)
            {
                if (m_Uses <= 0)
                    LabelTo(from, "[completed]");
                else
                    LabelTo(from, "[{0}% completed]", 100 - (100 * m_Uses / m_UsesMax));
            }
        }

        public override void DisplayTo(Mobile from)
        {
            EnsureLocation();

            if (m_BankLocation == Point2D.Zero)
            {
                from.SendLocalizedMessage(500208); // It appears to be blank.
                return;
            }

            base.DisplayTo(from);
        }

        public void EnsureLocation()
        {
            Point2D oldLoc = m_BankLocation;

            if (m_Uses <= 0)
            {
                m_BankLocation = Point2D.Zero;
            }
            else if (m_BankLocation == Point2D.Zero)
            {
                Point2D loc = GetRandomLocation();

                HarvestDefinition def = HarvestDefinition;

                if (def != null)
                    m_BankLocation = new Point2D(loc.X * def.BankWidth + def.BankWidth / 2, loc.Y * def.BankHeight + def.BankHeight / 2);
                else
                    m_BankLocation = loc;
            }

            if (m_BankLocation != oldLoc)
                UpdateMap();
        }

        protected virtual Point2D GetRandomLocation()
        {
            if (m_BankMap == null)
                return Point2D.Zero;

            HarvestDefinition def = HarvestDefinition;

            if (def == null)
                return Point2D.Zero;

            Hashtable banks = (Hashtable)def.Banks[m_BankMap];

            if (banks == null)
                return Point2D.Zero;

            Point2D result = Point2D.Zero;

            int count = 0;

            // take a random key from the hash table without creating a new list
            foreach (Point2D key in banks.Keys)
            {
                if (Validate(key.X * def.BankWidth, key.Y * def.BankHeight, m_BankMap))
                {
                    if (Utility.Random(++count) == 0)
                        result = key;
                }
            }

            return result;
        }

        private static bool Validate(int x, int y, Map map)
        {
            if (map == Map.Felucca || map == Map.Trammel)
                return x < 5120;

            return false;
        }

        public static ResourceMap Find(Mobile from, Point3D loc, Map map, HarvestDefinition def)
        {
            if (def == null)
                return null;

            Container pack = from.Backpack;

            if (pack == null)
                return null;

            foreach (ResourceMap resourceMap in pack.FindItemsByType<ResourceMap>())
            {
                if (resourceMap.HarvestDefinition == def && resourceMap.BankMap == map && resourceMap.Uses > 0)
                {
                    resourceMap.EnsureLocation();

                    int dx = (loc.X - resourceMap.BankLocation.X) / def.BankWidth;
                    int dy = (loc.Y - resourceMap.BankLocation.Y) / def.BankHeight;

                    if (dx == 0 && dy == 0)
                        return resourceMap;
                }
            }

            return null;
        }

        public bool MutateResource(Mobile from, HarvestDefinition def, out HarvestResource resource)
        {
            foreach (HarvestResource res in def.Resources)
            {
                foreach (Type resType in res.Types)
                {
                    if (CraftResources.GetFromType(resType) == m_Resource)
                    {
                        if (--Uses <= 0)
                        {
                            from.SendLocalizedMessage(1115320); // You have used up the item.
                            //Delete();
                        }

                        resource = res;
                        return true;
                    }
                }
            }

            resource = null;
            return false;
        }

        public ResourceMap(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Resource);
            writer.Write((int)m_UsesMax);
            writer.Write((int)m_Uses);
            writer.Write((Map)m_BankMap);
            writer.Write((Point2D)m_BankLocation);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Resource = (CraftResource)reader.ReadInt();
            m_UsesMax = reader.ReadInt();
            m_Uses = reader.ReadInt();
            m_BankMap = reader.ReadMap();
            m_BankLocation = reader.ReadPoint2D();
        }
    }
}