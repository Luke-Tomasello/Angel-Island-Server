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

using Server.Multis;

namespace Server.Items
{
    public class GuildTeleporter : Item
    {
        private Item m_Stone;

        public override int LabelNumber { get { return 1041054; } } // guildstone teleporter

        [Constructable]
        public GuildTeleporter()
            : this(null)
        {
        }

        public GuildTeleporter(Item stone)
            : base(0x1869)
        {
            Weight = 1.0;
            LootType = LootType.Blessed;

            m_Stone = stone;
        }

        public GuildTeleporter(Serial serial)
            : base(serial)
        {
        }

        public override bool DisplayLootType { get { return false; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Stone);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LootType = LootType.Blessed;

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Stone = reader.ReadItem();

                        break;
                    }
            }

            if (Weight == 0.0)
                Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            Guildstone stone = m_Stone as Guildstone;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (stone == null || stone.Deleted || stone.Guild == null || stone.Guild.Teleporter != this)
            {
                from.SendLocalizedMessage(501197); // This teleporting object can not determine what guildstone to teleport
            }
            else
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house == null)
                {
                    from.SendLocalizedMessage(501138); // You can only place a guildstone in a house.
                }
                else if (!house.IsOwner(from))
                {
                    from.SendLocalizedMessage(501141); // You can only place a guildstone in a house you own!
                }
                else
                {
                    m_Stone.MoveToWorld(from.Location, from.Map);
                    Delete();
                    stone.Guild.Teleporter = null;
                }
            }
        }
    }
}