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

/* Scripts/Items/Addons/Plows.cs
 * ChangeLog
 *	8/3/2023, Adam
 *		Initial version
 */

namespace Server.Items
{
    public class PlowEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new PlowEastAddonDeed(); } }

        [Constructable]
        public PlowEastAddon()
        {
            AddComponent(new AddonComponent(0x1501), 0, 0, 0);
            AddComponent(new AddonComponent(0x1500), +1, 0, 0);
        }

        public PlowEastAddon(Serial serial)
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

    public class PlowEastAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new PlowEastAddon(); } }
        public override string DefaultName { get { return "plow (east)"; } }

        [Constructable]
        public PlowEastAddonDeed()
        {
        }

        public PlowEastAddonDeed(Serial serial)
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

    public class PlowSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new PlowSouthAddonDeed(); } }

        [Constructable]
        public PlowSouthAddon()
        {
            AddComponent(new AddonComponent(0x1502), 0, 0, 0);
            AddComponent(new AddonComponent(0x1503), 0, +1, 0);
        }

        public PlowSouthAddon(Serial serial)
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

    public class PlowSouthAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new PlowSouthAddon(); } }
        public override string DefaultName { get { return "plow (south)"; } }

        [Constructable]
        public PlowSouthAddonDeed()
        {
        }

        public PlowSouthAddonDeed(Serial serial)
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