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

namespace Server.Items
{
    public class LargeBedEastAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new LargeBedEastDeed(); } }

        [Constructable]
        public LargeBedEastAddon()
        {
            AddComponent(new AddonComponent(0xA7D), 0, 0, 0);
            AddComponent(new AddonComponent(0xA7C), 0, 1, 0);
            AddComponent(new AddonComponent(0xA79), 1, 0, 0);
            AddComponent(new AddonComponent(0xA78), 1, 1, 0);
        }

        public LargeBedEastAddon(Serial serial)
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

    public class LargeBedEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeBedEastAddon(); } }
        public override int LabelNumber { get { return 1044324; } } // large bed (east)

        [Constructable]
        public LargeBedEastDeed()
        {
        }

        public LargeBedEastDeed(Serial serial)
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