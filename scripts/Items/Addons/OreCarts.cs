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

/* Scripts/Items/Addons/OreCart.cs
 * ChangeLog
 *	7/27/23, Yoar
 *		Initial version
 */

using Server.Township;

namespace Server.Items
{
    [TownshipAddon]
    public class OreCartEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new OreCartEastAddonDeed(); } }

        [Constructable]
        public OreCartEastAddon()
        {
            AddComponent(new AddonComponent(0x1A8B), -1, 0, 0);
            AddComponent(new AddonComponent(0x1A88), 0, 0, 0);
            AddComponent(new AddonComponent(0x1A87), +1, 0, 0);
        }

        public OreCartEastAddon(Serial serial)
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

    public class OreCartEastAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new OreCartEastAddon(); } }
        public override string DefaultName { get { return "ore cart (east)"; } }

        [Constructable]
        public OreCartEastAddonDeed()
        {
        }

        public OreCartEastAddonDeed(Serial serial)
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

    [TownshipAddon]
    public class OreCartSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new OreCartSouthAddonDeed(); } }

        [Constructable]
        public OreCartSouthAddon()
        {
            AddComponent(new AddonComponent(0x1A86), 0, -1, 0);
            AddComponent(new AddonComponent(0x1A83), 0, 0, 0);
            AddComponent(new AddonComponent(0x1A82), 0, +1, 0);
        }

        public OreCartSouthAddon(Serial serial)
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

    public class OreCartSouthAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new OreCartSouthAddon(); } }
        public override string DefaultName { get { return "ore cart (south)"; } }

        [Constructable]
        public OreCartSouthAddonDeed()
        {
        }

        public OreCartSouthAddonDeed(Serial serial)
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