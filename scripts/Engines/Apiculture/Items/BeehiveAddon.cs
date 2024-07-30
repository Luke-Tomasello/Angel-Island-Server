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

/* scripts\Engines\Apiculture\Items\BeehiveAddon.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Items;
using Server.Multis;

namespace Server.Engines.Apiculture
{
    public class BeehiveAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new BeehiveAddonDeed(); } }

        private Beehive m_Hive;

        [CommandProperty(AccessLevel.GameMaster)]
        public Beehive Hive
        {
            get { return m_Hive; }
            set { m_Hive = value; }
        }

        [Constructable]
        public BeehiveAddon()
            : base()
        {
            AddComponent(new AddonComponent(0xB34), 0, 0, 0);

            m_Hive = new Beehive();
            m_Hive.Z = 6;

            m_Hive.Addon = this;
        }

        public override void OnAfterPlace(Mobile placer, BaseHouse house)
        {
            // update resource indicators
            if (m_Hive != null)
                m_Hive.Colony.FindResources();
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            base.OnLocationChange(oldLoc);

            if (m_Hive != null)
                m_Hive.Location += (Location - oldLoc);
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (m_Hive != null)
                m_Hive.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Hive != null)
                m_Hive.Delete();
        }

        public BeehiveAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((Item)m_Hive);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Hive = reader.ReadItem() as Beehive;
        }
    }

    public class BeehiveAddonDeed : BaseAddonDeed
    {
        public override string DefaultName { get { return "a beehive deed"; } }
        public override BaseAddon Addon { get { return new BeehiveAddon(); } }

        [Constructable]
        public BeehiveAddonDeed()
            : base()
        {
        }

        public BeehiveAddonDeed(Serial serial)
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
    }
}