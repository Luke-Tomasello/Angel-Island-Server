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
    public interface ILoom
    {
        int Phase { get; set; }
    }

    public class LoomEastAddon : BaseAddon, ILoom
    {
        public override BaseAddonDeed Deed { get { return new LoomEastDeed(); } }

        private int m_Phase;

        public int Phase { get { return m_Phase; } set { m_Phase = value; } }

        [Constructable]
        public LoomEastAddon()
        {
            AddComponent(new AddonComponent(0x1060), 0, 0, 0);
            AddComponent(new AddonComponent(0x105F), 0, 1, 0);
        }

        public LoomEastAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_Phase);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Phase = reader.ReadInt();
                        break;
                    }
            }
        }
    }

    public class LoomEastDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LoomEastAddon(); } }
        public override int LabelNumber { get { return 1044343; } } // loom (east)

        [Constructable]
        public LoomEastDeed()
        {
        }

        public LoomEastDeed(Serial serial)
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