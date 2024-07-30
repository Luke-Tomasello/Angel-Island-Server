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

/* Scripts/Items/Addons/ShieldAddons.cs
 * CHANGELOG:
 *  9/17/21, Yoar
 *      Added redeedable addons.
 *	15/12/07, plasma
 *		Initial creation
 */

namespace Server.Items
{

    #region silver sepent shield

    #region deeds

    public class SilverSerpentShieldSouthAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SilverSerpentShieldSouthAddon();
            }
        }

        [Constructable]
        public SilverSerpentShieldSouthAddonDeed()
        {
            Name = "a silver serpent shield (south)";
        }

        public SilverSerpentShieldSouthAddonDeed(Serial serial)
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

    public class SilverSerpentShieldEastAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SilverSerpentShieldEastAddon();
            }
        }

        [Constructable]
        public SilverSerpentShieldEastAddonDeed()
        {
            Name = "a silver serpent shield (east)";
        }

        public SilverSerpentShieldEastAddonDeed(Serial serial)
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

    #endregion

    #region addons

    public class SilverSerpentShieldSouthAddon : BaseAddon
    {
        [Constructable]
        public SilverSerpentShieldSouthAddon()
        {
            AddComponent(new SilverSerpentShieldSouth(), 0, 0, 0);
        }

        public SilverSerpentShieldSouthAddon(Serial serial)
            : base(serial)
        {
        }

        public override bool Redeedable { get { return true; } }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SilverSerpentShieldSouthAddonDeed();
            }
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

    public class SilverSerpentShieldEastAddon : BaseAddon
    {
        [Constructable]
        public SilverSerpentShieldEastAddon()
        {
            AddComponent(new SilverSerpentShieldEast(), 0, 1, 0);
        }

        public SilverSerpentShieldEastAddon(Serial serial)
            : base(serial)
        {
        }

        public override bool Redeedable { get { return true; } }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SilverSerpentShieldEastAddonDeed();
            }
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

    #region addon components

    public class SilverSerpentShieldEast : AddonComponent
    {
        [Constructable]
        public SilverSerpentShieldEast()
            : this(0x1577)
        {
        }

        public SilverSerpentShieldEast(int itemID)
            : base(itemID)
        {
        }

        public SilverSerpentShieldEast(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SilverSerpentShieldSouth : AddonComponent
    {
        [Constructable]
        public SilverSerpentShieldSouth()
            : this(0x1576)
        {
        }

        public SilverSerpentShieldSouth(int itemID)
            : base(itemID)
        {
        }

        public SilverSerpentShieldSouth(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #endregion

    #region Golden serpent shield

    #region deeds

    public class GoldenSerpentShieldSouthAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new GoldenSerpentShieldSouthAddon();
            }
        }

        [Constructable]
        public GoldenSerpentShieldSouthAddonDeed()
        {
            Name = "a golden serpent shield (south)";
        }

        public GoldenSerpentShieldSouthAddonDeed(Serial serial)
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

    public class GoldenSerpentShieldEastAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new GoldenSerpentShieldEastAddon();
            }
        }

        [Constructable]
        public GoldenSerpentShieldEastAddonDeed()
        {
            Name = "a golden serpent shield (east)";
        }

        public GoldenSerpentShieldEastAddonDeed(Serial serial)
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

    #endregion

    #region addons

    public class GoldenSerpentShieldSouthAddon : BaseAddon
    {
        [Constructable]
        public GoldenSerpentShieldSouthAddon()
        {
            AddComponent(new GoldenSerpentShieldSouth(), 0, 0, 0);
        }

        public GoldenSerpentShieldSouthAddon(Serial serial)
            : base(serial)
        {
        }

        public override bool Redeedable { get { return true; } }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new GoldenSerpentShieldSouthAddonDeed();
            }
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

    public class GoldenSerpentShieldEastAddon : BaseAddon
    {
        [Constructable]
        public GoldenSerpentShieldEastAddon()
        {
            AddComponent(new GoldenSerpentShieldEast(), 0, 1, 0);
        }

        public GoldenSerpentShieldEastAddon(Serial serial)
            : base(serial)
        {
        }

        public override bool Redeedable { get { return true; } }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new GoldenSerpentShieldEastAddonDeed();
            }
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

    #region addon components

    public class GoldenSerpentShieldEast : AddonComponent
    {
        [Constructable]
        public GoldenSerpentShieldEast()
            : this(0x1579)
        {
        }

        public GoldenSerpentShieldEast(int itemID)
            : base(itemID)
        {
        }

        public GoldenSerpentShieldEast(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class GoldenSerpentShieldSouth : AddonComponent
    {
        [Constructable]
        public GoldenSerpentShieldSouth()
            : this(0x1578)
        {
        }

        public GoldenSerpentShieldSouth(int itemID)
            : base(itemID)
        {
        }

        public GoldenSerpentShieldSouth(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #endregion

}