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

/////////////////////////////////////////////////
//
// Automatically generated by the
// AddonGenerator script by Arya
//
/////////////////////////////////////////////////

namespace Server.Items
{
    public class ArchwaySouthAddon : BaseAddon
    {
        public override BaseAddonDeed Deed
        {
            get
            {
                return new ArchwaySouthAddonDeed();
            }
        }

        [Constructable]
        public ArchwaySouthAddon()
        {
            AddonComponent ac = null;
            ac = new AddonComponent(1955);
            AddComponent(ac, 1, 0, 0);
            ac = new AddonComponent(1955);
            AddComponent(ac, 0, 0, 0);
            ac = new AddonComponent(1955);
            AddComponent(ac, -1, 0, 0);
            ac = new AddonComponent(1955);
            AddComponent(ac, -2, 0, 0);
            ac = new AddonComponent(1955);
            AddComponent(ac, -2, 0, 5);
            ac = new AddonComponent(1955);
            AddComponent(ac, -2, 0, 10);
            ac = new AddonComponent(1955);
            AddComponent(ac, -2, 0, 15);
            ac = new AddonComponent(1955);
            AddComponent(ac, -2, 0, 20);
            ac = new AddonComponent(1956);
            AddComponent(ac, 1, 1, 0);
            ac = new AddonComponent(1956);
            AddComponent(ac, 0, 1, 0);
            ac = new AddonComponent(1956);
            AddComponent(ac, -1, 1, 0);
            ac = new AddonComponent(1963);
            AddComponent(ac, -2, 1, 0);
            ac = new AddonComponent(1958);
            AddComponent(ac, 1, -1, 0);
            ac = new AddonComponent(1958);
            AddComponent(ac, 0, -1, 0);
            ac = new AddonComponent(1958);
            AddComponent(ac, -1, -1, 0);
            ac = new AddonComponent(1960);
            AddComponent(ac, -2, -1, 0);
            ac = new AddonComponent(1959);
            AddComponent(ac, -2, 0, 25);
            ac = new AddonComponent(1959);
            AddComponent(ac, -1, 0, 31);
            ac = new AddonComponent(1957);
            AddComponent(ac, 1, 0, 31);
            ac = new AddonComponent(1955);
            AddComponent(ac, 0, 0, 31);
            ac = new AddonComponent(1955);
            AddComponent(ac, -1, 0, 26);
            ac = new AddonComponent(1955);
            AddComponent(ac, 1, 0, 26);
            ac = new AddonComponent(1955);
            AddComponent(ac, 0, 0, 26);
            ac = new AddonComponent(1955);
            AddComponent(ac, 2, 0, 0);
            ac = new AddonComponent(1955);
            AddComponent(ac, 2, 0, 5);
            ac = new AddonComponent(1955);
            AddComponent(ac, 2, 0, 10);
            ac = new AddonComponent(1955);
            AddComponent(ac, 2, 0, 15);
            ac = new AddonComponent(1955);
            AddComponent(ac, 2, 0, 20);
            ac = new AddonComponent(1961);
            AddComponent(ac, 2, 1, 0);
            ac = new AddonComponent(1962);
            AddComponent(ac, 2, -1, 0);
            ac = new AddonComponent(1957);
            AddComponent(ac, 2, 0, 25);

        }

        public ArchwaySouthAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class ArchwaySouthAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new ArchwaySouthAddon();
            }
        }

        [Constructable]
        public ArchwaySouthAddonDeed()
        {
            Name = "ArchwaySouth";
        }

        public ArchwaySouthAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}