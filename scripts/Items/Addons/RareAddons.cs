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

/* Scripts\Items\Addons\RareAddons.cs
 * ChangeLog:
 *  11/13/21, Adam
 *      Rename X to WallPlaqueAddon/deed
 *      Rename GenericAddon.cs to RareAddons.cs
 *      Add TatteredBanner addons
 *	11/8/21, Adam
 *	    Initial checkin
 */
namespace Server.Items
{
    public class WallPlaqueAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new WallPlaqueAddonDeed(ComponentID, Facing); } }
        [Constructable]
        public WallPlaqueAddon(int graphicID, Direction direction)
            : base(graphicID, direction, true, true)
        {
            AddComponent(new AddonComponent(graphicID, true), 0, 0, 0);
        }

        public WallPlaqueAddon(Serial serial)
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

    public class WallPlaqueAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new WallPlaqueAddon(ComponentID, Facing); } }

        [Constructable]
        public WallPlaqueAddonDeed(int GraphicID, Direction direction)
            : base(GraphicID, direction, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for wall plaque {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for wall plaque {0} addon", rootName);
        }
        public WallPlaqueAddonDeed(Serial serial)
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

    public class TatteredBanner1EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new TatteredBanner1EastAddonDeed(); } }
        [Constructable]
        public TatteredBanner1EastAddon()
            : base(0x42a, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0x42a, true), 0, 0, 0);
            AddComponent(new AddonComponent(0x42b, true), 0, 1, 0);
        }

        public TatteredBanner1EastAddon(Serial serial)
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

    public class TatteredBanner1EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new TatteredBanner1EastAddon(); } }

        [Constructable]
        public TatteredBanner1EastAddonDeed()
            : base(0x42a, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public TatteredBanner1EastAddonDeed(Serial serial)
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
    public class TatteredBanner2EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new TatteredBanner2EastAddonDeed(); } }
        [Constructable]
        public TatteredBanner2EastAddon()
            : base(0x42f, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0x42e, true), 0, 1, 0);
            AddComponent(new AddonComponent(0x42f, true), 0, 0, 0);
        }

        public TatteredBanner2EastAddon(Serial serial)
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

    public class TatteredBanner2EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new TatteredBanner2EastAddon(); } }

        [Constructable]
        public TatteredBanner2EastAddonDeed()
            : base(0x42f, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public TatteredBanner2EastAddonDeed(Serial serial)
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

    public class Tapestry1EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Tapestry1EastAddonDeed(); } }
        [Constructable]
        public Tapestry1EastAddon()
            : base(0xEAE, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0xEAE, true), 0, 1, 0);
            AddComponent(new AddonComponent(0xEAF, true), 0, 0, 0);
        }

        public Tapestry1EastAddon(Serial serial)
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

    public class Tapestry1EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Tapestry1EastAddon(); } }

        [Constructable]
        public Tapestry1EastAddonDeed()
            : base(0xEAE, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Tapestry1EastAddonDeed(Serial serial)
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

    public class BloodSplatter1EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new BloodSplatter1EastAddonDeed(); } }
        [Constructable]
        public BloodSplatter1EastAddon()
            : base(0x1d94, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0x1d94, true), 0, 0, 0);
        }

        public BloodSplatter1EastAddon(Serial serial)
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

    public class BloodSplatter1EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new BloodSplatter1EastAddon(); } }

        [Constructable]
        public BloodSplatter1EastAddonDeed()
            : base(0x1d94, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public BloodSplatter1EastAddonDeed(Serial serial)
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

    public class BloodSplatter2EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new BloodSplatter2EastAddonDeed(); } }
        [Constructable]
        public BloodSplatter2EastAddon()
            : base(0x1d96, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0x1d96, true), 0, 0, 0);
        }

        public BloodSplatter2EastAddon(Serial serial)
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

    public class BloodSplatter2EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new BloodSplatter2EastAddon(); } }

        [Constructable]
        public BloodSplatter2EastAddonDeed()
            : base(0x1d96, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public BloodSplatter2EastAddonDeed(Serial serial)
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

    public class Vines1SouthAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Vines1SouthAddonDeed(); } }
        [Constructable]
        public Vines1SouthAddon()
            : base(0xceb, Direction.South, true, true)
        {
            AddComponent(new AddonComponent(0xceb, true), 0, 0, 0);
        }

        public Vines1SouthAddon(Serial serial)
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

    public class Vines1SouthAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Vines1SouthAddon(); } }

        [Constructable]
        public Vines1SouthAddonDeed()
            : base(0xceb, Direction.South, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Vines1SouthAddonDeed(Serial serial)
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

    public class Vines2SouthAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Vines2SouthAddonDeed(); } }
        [Constructable]
        public Vines2SouthAddon()
            : base(0xcec, Direction.South, true, true)
        {
            AddComponent(new AddonComponent(0xcec, true), 0, 0, 0);
        }

        public Vines2SouthAddon(Serial serial)
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

    public class Vines2SouthAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Vines2SouthAddon(); } }

        [Constructable]
        public Vines2SouthAddonDeed()
            : base(0xcec, Direction.South, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Vines2SouthAddonDeed(Serial serial)
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
    public class Vines1EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Vines1EastAddonDeed(); } }
        [Constructable]
        public Vines1EastAddon()
            : base(0xced, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0xced, true), 0, 0, 0);
        }

        public Vines1EastAddon(Serial serial)
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

    public class Vines1EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Vines1EastAddon(); } }

        [Constructable]
        public Vines1EastAddonDeed()
            : base(0xced, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Vines1EastAddonDeed(Serial serial)
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

    public class Vines2EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Vines2EastAddonDeed(); } }
        [Constructable]
        public Vines2EastAddon()
            : base(0xcee, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0xcee, true), 0, 0, 0);
        }

        public Vines2EastAddon(Serial serial)
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

    public class Vines2EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Vines2EastAddon(); } }

        [Constructable]
        public Vines2EastAddonDeed()
            : base(0xcee, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Vines2EastAddonDeed(Serial serial)
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

    public class Vines3EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Vines3EastAddonDeed(); } }
        [Constructable]
        public Vines3EastAddon()
            : base(0xcef, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0xcef, true), 0, 0, 0);
        }

        public Vines3EastAddon(Serial serial)
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

    public class Vines3EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Vines3EastAddon(); } }

        [Constructable]
        public Vines3EastAddonDeed()
            : base(0xcef, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Vines3EastAddonDeed(Serial serial)
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
    public class Vines4EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Vines4EastAddonDeed(); } }
        [Constructable]
        public Vines4EastAddon()
            : base(0xcf1, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0xcf1, true), 0, 0, 0);
        }

        public Vines4EastAddon(Serial serial)
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

    public class Vines4EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Vines4EastAddon(); } }

        [Constructable]
        public Vines4EastAddonDeed()
            : base(0xcf1, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Vines4EastAddonDeed(Serial serial)
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
    public class Vines5EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Vines5EastAddonDeed(); } }
        [Constructable]
        public Vines5EastAddon()
            : base(0xcf2, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0xcf2, true), 0, 0, 0);
        }

        public Vines5EastAddon(Serial serial)
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

    public class Vines5EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Vines5EastAddon(); } }

        [Constructable]
        public Vines5EastAddonDeed()
            : base(0xcf2, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Vines5EastAddonDeed(Serial serial)
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
    public class SkeletonWithMeat1SouthAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new SkeletonWithMeat1SouthAddonDeed(); } }
        [Constructable]
        public SkeletonWithMeat1SouthAddon()
            : base(0x1b1e, Direction.South, true, true)
        {
            AddComponent(new AddonComponent(0x1b1e, true), 0, 0, 0);
        }

        public SkeletonWithMeat1SouthAddon(Serial serial)
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

    public class SkeletonWithMeat1SouthAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new SkeletonWithMeat1SouthAddon(); } }

        [Constructable]
        public SkeletonWithMeat1SouthAddonDeed()
            : base(0x1b1e, Direction.South, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public SkeletonWithMeat1SouthAddonDeed(Serial serial)
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
    public class SkeletonWithMeat2EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new SkeletonWithMeat2EastAddonDeed(); } }
        [Constructable]
        public SkeletonWithMeat2EastAddon()
            : base(0x1b1d, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0x1b1d, true), 0, 0, 0);
        }

        public SkeletonWithMeat2EastAddon(Serial serial)
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

    public class SkeletonWithMeat2EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new SkeletonWithMeat2EastAddon(); } }

        [Constructable]
        public SkeletonWithMeat2EastAddonDeed()
            : base(0x1b1d, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public SkeletonWithMeat2EastAddonDeed(Serial serial)
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
    public class Skeleton1SouthAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Skeleton1SouthAddonDeed(); } }
        [Constructable]
        public Skeleton1SouthAddon()
            : base(0x1A02, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0x1A02, true), 0, 0, 0);
        }

        public Skeleton1SouthAddon(Serial serial)
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

    public class Skeleton1SouthAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Skeleton1SouthAddon(); } }

        [Constructable]
        public Skeleton1SouthAddonDeed()
            : base(0x1A02, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Skeleton1SouthAddonDeed(Serial serial)
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
    public class Chains1SouthAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Chains1SouthAddonDeed(); } }
        [Constructable]
        public Chains1SouthAddon()
            : base(0x1A08, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0x1A08, true), 0, 0, 0);
        }

        public Chains1SouthAddon(Serial serial)
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

    public class Chains1SouthAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Chains1SouthAddon(); } }

        [Constructable]
        public Chains1SouthAddonDeed()
            : base(0x1A08, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Chains1SouthAddonDeed(Serial serial)
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
    public class Chains1EastAddon : Addon
    {
        public override BaseAddonDeed Deed { get { return new Chains1EastAddonDeed(); } }
        [Constructable]
        public Chains1EastAddon()
            : base(0x1A07, Direction.East, true, true)
        {
            AddComponent(new AddonComponent(0x1A07, true), 0, 0, 0);
        }

        public Chains1EastAddon(Serial serial)
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

    public class Chains1EastAddonDeed : AddonDeed
    {
        public override BaseAddon Addon { get { return new Chains1EastAddon(); } }

        [Constructable]
        public Chains1EastAddonDeed()
            : base(0x1A07, Direction.East, true)
        {
            UpdateName();
        }
        public override void UpdateName()
        {
            string rootName = Utility.GetRootName(ComponentID);

            if (Facing >= Direction.North && Facing < Direction.Mask)
                this.Name = string.Format("a deed for {0} addon ({1})", rootName, Facing.ToString().ToLower());
            else
                this.Name = string.Format("a deed for {0} addon", rootName);
        }
        public Chains1EastAddonDeed(Serial serial)
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