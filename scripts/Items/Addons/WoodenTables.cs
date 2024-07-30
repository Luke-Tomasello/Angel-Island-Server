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

/* Scripts\Items\Addons\WoodenTables.cs
 * ChangeLog:
 *	3/23/22, Yoar
 *	    Initial version.
 */

using Server.Township;

namespace Server.Items
{
    #region Medium Wooden Table

    [TownshipAddon]
    public class MediumWoodenTableEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumWoodenTableEastDeed(); } }

        [Constructable]
        public MediumWoodenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x0B76), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B75), 0, 1, 0);
        }

        public MediumWoodenTableEastAddon(Serial serial)
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

    public class MediumWoodenTableEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumWoodenTableEastAddon(); } }

        [Constructable]
        public MediumWoodenTableEastDeed()
        {
            Name = "Medium Wooden Table [East]";
        }

        public MediumWoodenTableEastDeed(Serial serial)
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
    public class MediumWoodenTableSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumWoodenTableSouthDeed(); } }

        [Constructable]
        public MediumWoodenTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B89), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B88), 1, 0, 0);
        }

        public MediumWoodenTableSouthAddon(Serial serial)
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

    public class MediumWoodenTableSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumWoodenTableSouthAddon(); } }

        [Constructable]
        public MediumWoodenTableSouthDeed()
        {
            Name = "Medium Wooden Table [South]";
        }

        public MediumWoodenTableSouthDeed(Serial serial)
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

    #endregion

    #region Large Wooden Table

    [TownshipAddon]
    public class LargeWoodenTableEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargeWoodenTableEastDeed(); } }

        [Constructable]
        public LargeWoodenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x0B7A), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B79), 1, 0, 0);
            AddComponent(new AddonComponent(0x0B7A), 0, 1, 0);
            AddComponent(new AddonComponent(0x0B7B), 1, 1, 0);
            AddComponent(new AddonComponent(0x0B77), 0, 2, 0);
            AddComponent(new AddonComponent(0x0B78), 1, 2, 0);
        }

        public LargeWoodenTableEastAddon(Serial serial)
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

    public class LargeWoodenTableEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeWoodenTableEastAddon(); } }

        [Constructable]
        public LargeWoodenTableEastDeed()
        {
            Name = "Large Wooden Table [East]";
        }

        public LargeWoodenTableEastDeed(Serial serial)
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
    public class LargeWoodenTableSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargeWoodenTableSouthDeed(); } }

        [Constructable]
        public LargeWoodenTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B8D), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B8D), 1, 0, 0);
            AddComponent(new AddonComponent(0x0B8A), 2, 0, 0);
            AddComponent(new AddonComponent(0x0B8C), 0, 1, 0);
            AddComponent(new AddonComponent(0x0B8E), 1, 1, 0);
            AddComponent(new AddonComponent(0x0B8B), 2, 1, 0);
        }

        public LargeWoodenTableSouthAddon(Serial serial)
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

    public class LargeWoodenTableSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeWoodenTableSouthAddon(); } }

        [Constructable]
        public LargeWoodenTableSouthDeed()
        {
            Name = "Large Wooden Table [South]";
        }

        public LargeWoodenTableSouthDeed(Serial serial)
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

    #endregion

    #region Medium Plain Wooden Table

    [TownshipAddon]
    public class MediumPlainWoodenTableEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumPlainWoodenTableEastDeed(); } }

        [Constructable]
        public MediumPlainWoodenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x0B6F), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B6E), 0, 1, 0);
        }

        public MediumPlainWoodenTableEastAddon(Serial serial)
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

    public class MediumPlainWoodenTableEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumPlainWoodenTableEastAddon(); } }

        [Constructable]
        public MediumPlainWoodenTableEastDeed()
        {
            Name = "Medium Plain Wooden Table [East]";
        }

        public MediumPlainWoodenTableEastDeed(Serial serial)
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
    public class MediumPlainWoodenTableSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumPlainWoodenTableSouthDeed(); } }

        [Constructable]
        public MediumPlainWoodenTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B82), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B81), 1, 0, 0);
        }

        public MediumPlainWoodenTableSouthAddon(Serial serial)
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

    public class MediumPlainWoodenTableSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumPlainWoodenTableSouthAddon(); } }

        [Constructable]
        public MediumPlainWoodenTableSouthDeed()
        {
            Name = "Medium Plain Wooden Table [South]";
        }

        public MediumPlainWoodenTableSouthDeed(Serial serial)
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

    #endregion

    #region Large Plain Wooden Table

    [TownshipAddon]
    public class LargePlainWoodenTableEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargePlainWoodenTableEastDeed(); } }

        [Constructable]
        public LargePlainWoodenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x0B73), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B72), 1, 0, 0);
            AddComponent(new AddonComponent(0x0B73), 0, 1, 0);
            AddComponent(new AddonComponent(0x0B73), 1, 1, 0);
            AddComponent(new AddonComponent(0x0B70), 0, 2, 0);
            AddComponent(new AddonComponent(0x0B71), 1, 2, 0);
        }

        public LargePlainWoodenTableEastAddon(Serial serial)
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

    public class LargePlainWoodenTableEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargePlainWoodenTableEastAddon(); } }

        [Constructable]
        public LargePlainWoodenTableEastDeed()
        {
            Name = "Large Plain Wooden Table [East]";
        }

        public LargePlainWoodenTableEastDeed(Serial serial)
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
    public class LargePlainWoodenTableSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargePlainWoodenTableSouthDeed(); } }

        [Constructable]
        public LargePlainWoodenTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B86), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B86), 1, 0, 0);
            AddComponent(new AddonComponent(0x0B83), 2, 0, 0);
            AddComponent(new AddonComponent(0x0B85), 0, 1, 0);
            AddComponent(new AddonComponent(0x0B86), 1, 1, 0);
            AddComponent(new AddonComponent(0x0B84), 2, 1, 0);
        }

        public LargePlainWoodenTableSouthAddon(Serial serial)
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

    public class LargePlainWoodenTableSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargePlainWoodenTableSouthAddon(); } }

        [Constructable]
        public LargePlainWoodenTableSouthDeed()
        {
            Name = "Large Plain Wooden Table [South]";
        }

        public LargePlainWoodenTableSouthDeed(Serial serial)
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

    #endregion

    #region Medium Elegant Wooden Table

    [TownshipAddon]
    public class MediumElegantWoodenTableEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumElegantWoodenTableEastDeed(); } }

        [Constructable]
        public MediumElegantWoodenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x0B6C), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B6B), 0, 1, 0);
        }

        public MediumElegantWoodenTableEastAddon(Serial serial)
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

    public class MediumElegantWoodenTableEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumElegantWoodenTableEastAddon(); } }

        [Constructable]
        public MediumElegantWoodenTableEastDeed()
        {
            Name = "Medium Elegant Wooden Table [East]";
        }

        public MediumElegantWoodenTableEastDeed(Serial serial)
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
    public class MediumElegantWoodenTableSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumElegantWoodenTableSouthDeed(); } }

        [Constructable]
        public MediumElegantWoodenTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B7F), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B7E), 1, 0, 0);
        }

        public MediumElegantWoodenTableSouthAddon(Serial serial)
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

    public class MediumElegantWoodenTableSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumElegantWoodenTableSouthAddon(); } }

        [Constructable]
        public MediumElegantWoodenTableSouthDeed()
        {
            Name = "Medium Elegant Wooden Table [South]";
        }

        public MediumElegantWoodenTableSouthDeed(Serial serial)
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

    #endregion

    #region Large Elegant Wooden Table

    [TownshipAddon]
    public class LargeElegantWoodenTableEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargeElegantWoodenTableEastDeed(); } }

        [Constructable]
        public LargeElegantWoodenTableEastAddon()
        {
            AddComponent(new AddonComponent(0x0B6C), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B6D), 0, 1, 0);
            AddComponent(new AddonComponent(0x0B6B), 0, 2, 0);
        }

        public LargeElegantWoodenTableEastAddon(Serial serial)
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

    public class LargeElegantWoodenTableEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeElegantWoodenTableEastAddon(); } }

        [Constructable]
        public LargeElegantWoodenTableEastDeed()
        {
            Name = "Large Elegant Wooden Table [East]";
        }

        public LargeElegantWoodenTableEastDeed(Serial serial)
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
    public class LargeElegantWoodenTableSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargeElegantWoodenTableSouthDeed(); } }

        [Constructable]
        public LargeElegantWoodenTableSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B7F), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B80), 1, 0, 0);
            AddComponent(new AddonComponent(0x0B7E), 2, 0, 0);
        }

        public LargeElegantWoodenTableSouthAddon(Serial serial)
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

    public class LargeElegantWoodenTableSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeElegantWoodenTableSouthAddon(); } }

        [Constructable]
        public LargeElegantWoodenTableSouthDeed()
        {
            Name = "Large Elegant Wooden Table [South]";
        }

        public LargeElegantWoodenTableSouthDeed(Serial serial)
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

    #endregion
}