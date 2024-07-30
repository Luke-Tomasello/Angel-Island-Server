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

/* Scripts/Items/Addons/FlowerTapestries.cs
 * ChangeLog
 *  9/17/21, Yoar
 *      Added redeedable addons.
 */

namespace Server.Items
{
    public class LightFlowerTapestryEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new LightFlowerTapestryEastDeed(); } }

        [Constructable]
        public LightFlowerTapestryEastAddon()
        {
            AddComponent(new AddonComponent(0xFDC), 0, 0, 0);
            AddComponent(new AddonComponent(0xFDB), 0, 1, 0);
        }

        public LightFlowerTapestryEastAddon(Serial serial)
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

    public class LightFlowerTapestryEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LightFlowerTapestryEastAddon(); } }
        public override int LabelNumber { get { return 1049393; } } // a flower tapestry deed facing east

        [Constructable]
        public LightFlowerTapestryEastDeed()
        {
        }

        public LightFlowerTapestryEastDeed(Serial serial)
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

    public class LightFlowerTapestrySouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new LightFlowerTapestrySouthDeed(); } }

        [Constructable]
        public LightFlowerTapestrySouthAddon()
        {
            AddComponent(new AddonComponent(0xFD9), 0, 0, 0);
            AddComponent(new AddonComponent(0xFDA), 1, 0, 0);
        }

        public LightFlowerTapestrySouthAddon(Serial serial)
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

    public class LightFlowerTapestrySouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LightFlowerTapestrySouthAddon(); } }
        public override int LabelNumber { get { return 1049394; } } // a flower tapestry deed facing south

        [Constructable]
        public LightFlowerTapestrySouthDeed()
        {
        }

        public LightFlowerTapestrySouthDeed(Serial serial)
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

    public class DarkFlowerTapestryEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new DarkFlowerTapestryEastDeed(); } }

        [Constructable]
        public DarkFlowerTapestryEastAddon()
        {
            AddComponent(new AddonComponent(0xFE0), 0, 0, 0);
            AddComponent(new AddonComponent(0xFDF), 0, 1, 0);
        }

        public DarkFlowerTapestryEastAddon(Serial serial)
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

    public class DarkFlowerTapestryEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new DarkFlowerTapestryEastAddon(); } }
        public override int LabelNumber { get { return 1049396; } } // a dark flower tapestry deed facing south

        [Constructable]
        public DarkFlowerTapestryEastDeed()
        {
        }

        public DarkFlowerTapestryEastDeed(Serial serial)
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

    public class DarkFlowerTapestrySouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new DarkFlowerTapestrySouthDeed(); } }

        [Constructable]
        public DarkFlowerTapestrySouthAddon()
        {
            AddComponent(new AddonComponent(0xFDD), 0, 0, 0);
            AddComponent(new AddonComponent(0xFDE), 1, 0, 0);
        }

        public DarkFlowerTapestrySouthAddon(Serial serial)
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

    public class DarkFlowerTapestrySouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new DarkFlowerTapestrySouthAddon(); } }
        public override int LabelNumber { get { return 1049396; } } // a dark flower tapestry deed facing south

        [Constructable]
        public DarkFlowerTapestrySouthDeed()
        {
        }

        public DarkFlowerTapestrySouthDeed(Serial serial)
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

    // begin new Tapestry addons converted from the items in 
    //	Scripts\Items\Construction\Decorative\Tapestry.cs
    public class DarkTapestrySouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new DarkTapestrySouthDeed(); } }

        [Constructable]
        public DarkTapestrySouthAddon()
        {
            AddComponent(new AddonComponent(0xFD5), 0, 0, 0);
            AddComponent(new AddonComponent(0xFD6), 2, 0, 0);
        }

        public DarkTapestrySouthAddon(Serial serial)
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

    public class DarkTapestrySouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new DarkTapestrySouthAddon(); } }
        //public override int LabelNumber { get { return; } } // a dark flower tapestry deed facing south

        [Constructable]
        public DarkTapestrySouthDeed()
        {
            Name = "a dark tapestry deed facing south";
        }

        public DarkTapestrySouthDeed(Serial serial)
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

    public class DarkTapestryEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new DarkTapestryEastDeed(); } }

        [Constructable]
        public DarkTapestryEastAddon()
        {
            AddComponent(new AddonComponent(0xFD8), 0, 0, 0);
            AddComponent(new AddonComponent(0xFD7), 0, 2, 0);
        }

        public DarkTapestryEastAddon(Serial serial)
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

    public class DarkTapestryEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new DarkTapestryEastAddon(); } }
        //public override int LabelNumber { get { return; } } // a flower tapestry deed facing east

        [Constructable]
        public DarkTapestryEastDeed()
        {
            Name = "a dark tapestry deed facing east";
        }

        public DarkTapestryEastDeed(Serial serial)
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

    public class LightTapestrySouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new LightTapestrySouthDeed(); } }

        [Constructable]
        public LightTapestrySouthAddon()
        {
            AddComponent(new AddonComponent(0xFE1), 0, 0, 0);
            AddComponent(new AddonComponent(0xFE2), 1, 0, 0);
        }

        public LightTapestrySouthAddon(Serial serial)
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

    public class LightTapestrySouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LightTapestrySouthAddon(); } }
        //public override int LabelNumber { get { return; } } // a dark flower tapestry deed facing south

        [Constructable]
        public LightTapestrySouthDeed()
        {
            Name = "a light tapestry deed facing south";
        }

        public LightTapestrySouthDeed(Serial serial)
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

    public class LightTapestryEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new LightTapestryEastDeed(); } }

        [Constructable]
        public LightTapestryEastAddon()
        {
            AddComponent(new AddonComponent(0xFE4), 0, 0, 0);
            AddComponent(new AddonComponent(0xFE3), 0, 1, 0);
        }

        public LightTapestryEastAddon(Serial serial)
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

    public class LightTapestryEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LightTapestryEastAddon(); } }
        //public override int LabelNumber { get { return; } } // a flower tapestry deed facing east

        [Constructable]
        public LightTapestryEastDeed()
        {
            Name = "a light tapestry deed facing east";
        }

        public LightTapestryEastDeed(Serial serial)
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