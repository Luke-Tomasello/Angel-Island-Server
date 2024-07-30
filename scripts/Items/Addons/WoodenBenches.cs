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

/* Scripts\Items\Addons\WoodenBenches.cs
 * ChangeLog:
 *	3/23/22, Yoar
 *	    Initial version.
 */

using Server.Township;

namespace Server.Items
{
    #region Medium Wooden Bench

    [TownshipAddon]
    public class MediumWoodenBenchEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumWoodenBenchEastDeed(); } }

        [Constructable]
        public MediumWoodenBenchEastAddon()
        {
            AddComponent(new AddonComponent(0x0B60), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B5F), 0, 1, 0);
        }

        public MediumWoodenBenchEastAddon(Serial serial)
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

    public class MediumWoodenBenchEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumWoodenBenchEastAddon(); } }

        [Constructable]
        public MediumWoodenBenchEastDeed()
        {
            Name = "Medium Wooden Bench [East]";
        }

        public MediumWoodenBenchEastDeed(Serial serial)
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
    public class MediumWoodenBenchSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumWoodenBenchSouthDeed(); } }

        [Constructable]
        public MediumWoodenBenchSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B66), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B65), 1, 0, 0);
        }

        public MediumWoodenBenchSouthAddon(Serial serial)
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

    public class MediumWoodenBenchSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumWoodenBenchSouthAddon(); } }

        [Constructable]
        public MediumWoodenBenchSouthDeed()
        {
            Name = "Medium Wooden Bench [South]";
        }

        public MediumWoodenBenchSouthDeed(Serial serial)
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

    #region Large Wooden Bench

    [TownshipAddon]
    public class LargeWoodenBenchEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargeWoodenBenchEastDeed(); } }

        [Constructable]
        public LargeWoodenBenchEastAddon()
        {
            AddComponent(new AddonComponent(0x0B60), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B61), 0, 1, 0);
            AddComponent(new AddonComponent(0x0B5F), 0, 2, 0);
        }

        public LargeWoodenBenchEastAddon(Serial serial)
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

    public class LargeWoodenBenchEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeWoodenBenchEastAddon(); } }

        [Constructable]
        public LargeWoodenBenchEastDeed()
        {
            Name = "Large Wooden Bench [East]";
        }

        public LargeWoodenBenchEastDeed(Serial serial)
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
    public class LargeWoodenBenchSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargeWoodenBenchSouthDeed(); } }

        [Constructable]
        public LargeWoodenBenchSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B66), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B67), 1, 0, 0);
            AddComponent(new AddonComponent(0x0B65), 2, 0, 0);
        }

        public LargeWoodenBenchSouthAddon(Serial serial)
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

    public class LargeWoodenBenchSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeWoodenBenchSouthAddon(); } }

        [Constructable]
        public LargeWoodenBenchSouthDeed()
        {
            Name = "Large Wooden Bench [South]";
        }

        public LargeWoodenBenchSouthDeed(Serial serial)
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

    #region Medium Elegant Wooden Bench

    [TownshipAddon]
    public class MediumElegantWoodenBenchEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumElegantWoodenBenchEastDeed(); } }

        [Constructable]
        public MediumElegantWoodenBenchEastAddon()
        {
            AddComponent(new AddonComponent(0x0B63), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B62), 0, 1, 0);
        }

        public MediumElegantWoodenBenchEastAddon(Serial serial)
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

    public class MediumElegantWoodenBenchEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumElegantWoodenBenchEastAddon(); } }

        [Constructable]
        public MediumElegantWoodenBenchEastDeed()
        {
            Name = "Medium Elegant Wooden Bench [East]";
        }

        public MediumElegantWoodenBenchEastDeed(Serial serial)
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
    public class MediumElegantWoodenBenchSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new MediumElegantWoodenBenchSouthDeed(); } }

        [Constructable]
        public MediumElegantWoodenBenchSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B69), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B68), 1, 0, 0);
        }

        public MediumElegantWoodenBenchSouthAddon(Serial serial)
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

    public class MediumElegantWoodenBenchSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MediumElegantWoodenBenchSouthAddon(); } }

        [Constructable]
        public MediumElegantWoodenBenchSouthDeed()
        {
            Name = "Medium Elegant Wooden Bench [South]";
        }

        public MediumElegantWoodenBenchSouthDeed(Serial serial)
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

    #region Large Elegant Wooden Bench

    [TownshipAddon]
    public class LargeElegantWoodenBenchEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargeElegantWoodenBenchEastDeed(); } }

        [Constructable]
        public LargeElegantWoodenBenchEastAddon()
        {
            AddComponent(new AddonComponent(0x0B63), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B64), 0, 1, 0);
            AddComponent(new AddonComponent(0x0B62), 0, 2, 0);
        }

        public LargeElegantWoodenBenchEastAddon(Serial serial)
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

    public class LargeElegantWoodenBenchEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeElegantWoodenBenchEastAddon(); } }

        [Constructable]
        public LargeElegantWoodenBenchEastDeed()
        {
            Name = "Large Elegant Wooden Bench [East]";
        }

        public LargeElegantWoodenBenchEastDeed(Serial serial)
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
    public class LargeElegantWoodenBenchSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return false; } }
        public override BaseAddonDeed Deed { get { return new LargeElegantWoodenBenchSouthDeed(); } }

        [Constructable]
        public LargeElegantWoodenBenchSouthAddon()
        {
            AddComponent(new AddonComponent(0x0B69), 0, 0, 0);
            AddComponent(new AddonComponent(0x0B6A), 1, 0, 0);
            AddComponent(new AddonComponent(0x0B68), 2, 0, 0);
        }

        public LargeElegantWoodenBenchSouthAddon(Serial serial)
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

    public class LargeElegantWoodenBenchSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeElegantWoodenBenchSouthAddon(); } }

        [Constructable]
        public LargeElegantWoodenBenchSouthDeed()
        {
            Name = "Large Elegant Wooden Bench [South]";
        }

        public LargeElegantWoodenBenchSouthDeed(Serial serial)
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