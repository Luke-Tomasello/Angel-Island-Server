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

/* Scripts/Skills/Stealing.cs
 * ChangeLog:
 *  7/14/2023, Adam
 *      Initial Creation
 */

namespace Server.Items
{
    public class Scarecrow : AddonComponent
    {
        public bool East { get { return this.ItemID == 0x1E35; } }

        [Constructable]
        public Scarecrow() : this(true)
        {
        }

        [Constructable]
        public Scarecrow(bool east) : base(east ? 0x1E35 : 0x1E34)
        {
        }

        public Scarecrow(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class ScarecrowEastAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new ScarecrowEastDeed(); } }

        public ScarecrowEastAddon()
        {
            AddComponent(new Scarecrow(true), 0, 0, 0);
        }

        public ScarecrowEastAddon(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class ScarecrowEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new ScarecrowEastAddon(); } }

        [Constructable]
        public ScarecrowEastDeed()
        {
            Name = "scarecrow (east)";
        }

        public ScarecrowEastDeed(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class ScarecrowSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new ScarecrowSouthDeed(); } }

        public ScarecrowSouthAddon()
        {
            AddComponent(new Scarecrow(false), 0, 0, 0);
        }

        public ScarecrowSouthAddon(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class ScarecrowSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new ScarecrowSouthAddon(); } }

        [Constructable]
        public ScarecrowSouthDeed()
        {
            Name = "scarecrow (south)";
        }

        public ScarecrowSouthDeed(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}