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
    #region SosariaTapestryEast

    #region deeds

    public class SosariaTapestryEastAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SosariaTapestryEastAddon();
            }
        }

        [Constructable]
        public SosariaTapestryEastAddonDeed()
        {
            Name = "a sosaria tapestry (east)";
        }

        public SosariaTapestryEastAddonDeed(Serial serial)
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
    public class SosariaTapestrySouthAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SosariaTapestrySouthAddon();
            }
        }

        [Constructable]
        public SosariaTapestrySouthAddonDeed()
        {
            Name = "a sosaria tapestry (South)";
        }

        public SosariaTapestrySouthAddonDeed(Serial serial)
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
    public class SosariaTapestryEastAddon : BaseAddon
    {
        [Constructable]
        public SosariaTapestryEastAddon()
        {
            AddComponent(new SosariaTapestryEast(), 0, 0, 0);
        }

        public SosariaTapestryEastAddon(Serial serial)
            : base(serial)
        {
        }

        public override bool Redeedable { get { return true; } }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SosariaTapestryEastAddonDeed();
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
    public class SosariaTapestrySouthAddon : BaseAddon
    {
        [Constructable]
        public SosariaTapestrySouthAddon()
        {
            AddComponent(new SosariaTapestrySouth(), 0, 0, 0);
        }

        public SosariaTapestrySouthAddon(Serial serial)
            : base(serial)
        {
        }

        public override bool Redeedable { get { return true; } }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SosariaTapestrySouthAddonDeed();
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
    public class SosariaTapestryEast : AddonComponent
    {
        public override bool NeedsWall { get { return true; } }
        [Constructable]
        public SosariaTapestryEast()
            : this(0x234E)
        {
        }

        public SosariaTapestryEast(int itemID)
            : base(itemID)
        {
        }

        public SosariaTapestryEast(Serial serial)
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
    public class SosariaTapestrySouth : AddonComponent
    {
        public override bool NeedsWall { get { return true; } }
        [Constructable]
        public SosariaTapestrySouth()
            : this(0x234F)
        {
        }

        public SosariaTapestrySouth(int itemID)
            : base(itemID)
        {
        }

        public SosariaTapestrySouth(Serial serial)
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