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

/* Scripts/Items/Addons/FallenLogs.cs
 * ChangeLog
 *	7/27/23, Yoar
 *		Initial version
 */

using Server.Township;

namespace Server.Items
{
    [TownshipAddon]
    public class FallenLogEastAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new FallenLogEastAddonDeed(); } }

        [Constructable]
        public FallenLogEastAddon()
        {
            AddComponent(new AddonComponent(0x0CF3), 0, 0, 0);
            AddComponent(new AddonComponent(0x0CF4), 0, 1, 0);
        }

        public FallenLogEastAddon(Serial serial)
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

    public class FallenLogEastAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new FallenLogEastAddon(); } }
        public override string DefaultName { get { return "fallen log (east)"; } }

        [Constructable]
        public FallenLogEastAddonDeed()
        {
        }

        public FallenLogEastAddonDeed(Serial serial)
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
    public class FallenLogSouthAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new FallenLogSouthAddonDeed(); } }

        [Constructable]
        public FallenLogSouthAddon()
        {
            AddComponent(new AddonComponent(0x0CF5), -1, 0, 0);
            AddComponent(new AddonComponent(0x0CF6), 0, 0, 0);
            AddComponent(new AddonComponent(0x0CF7), +1, 0, 0);
        }

        public FallenLogSouthAddon(Serial serial)
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

    public class FallenLogSouthAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new FallenLogSouthAddon(); } }
        public override string DefaultName { get { return "fallen log (south)"; } }

        [Constructable]
        public FallenLogSouthAddonDeed()
        {
        }

        public FallenLogSouthAddonDeed(Serial serial)
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