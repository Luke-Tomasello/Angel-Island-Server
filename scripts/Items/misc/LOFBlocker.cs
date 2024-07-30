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

/* Scripts/Items/Misc/LOFBlocker.cs
 * ChangeLog :
 *	5/02/06, weaver
 *		Changed graphic displayed to Counselor+ access players to a silver orb.
 *	4/27/06, weaver
 *		Initial creation.
 */

using Server.Network;

namespace Server.Items
{
    public class LOFBlocker : Item
    {
        public static void Initialize()
        {
            TileData.ItemTable[0x3FFF].Flags = TileFlag.NoShoot;
            TileData.ItemTable[0x3FFF].Height = 20;
        }

        [Constructable]
        public LOFBlocker()
            : base(0x3FFF)
        {
            Movable = false;
            Name = "no line of fire";
            Hue = 989;
        }

        public LOFBlocker(Serial serial)
            : base(serial)
        {
        }

        public override void SendInfoTo(NetState state)
        {
            Mobile mob = state.Mobile;

            // Determines whether we sent the item's basic packet (<GM) or an indication
            // of the LoF blocker's existence (GM+)

            if (mob != null && mob.AccessLevel >= AccessLevel.GameMaster)
                state.Send(new GMItemPacket(this));
            else
                state.Send(WorldPacket);

            if (ObjectPropertyList.Enabled)
                state.Send(OPLPacket);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        // This replaces the WorldPacket (sent to <M level access)
        //
        // The only difference between GMItemPacket and Item.WorldPacket is the ItemID
        // which is forced to 0x36FF in the below packet (the orb thing you see 
        // to represent a LOF blocker)
        //	

        public sealed class GMItemPacket : Packet
        {
            public GMItemPacket(Item item)
                : base(0x1A)
            {
                this.EnsureCapacity(20);

                // 14 base length
                // +2 - Amount
                // +2 - Hue
                // +1 - Flags

                uint serial = (uint)item.Serial.Value;
                int itemID = 0x36FF;                    // glowing orb thingy
                int amount = item.Amount;
                Point3D loc = item.Location;
                int x = loc.X;
                int y = loc.Y;
                int hue = item.Hue;
                int flags = item.GetPacketFlags();
                int direction = (int)item.Direction;

                if (amount != 0)
                    serial |= 0x80000000;
                else
                    serial &= 0x7FFFFFFF;

                m_Stream.Write((uint)serial);
                m_Stream.Write((short)(itemID & 0x7FFF));

                if (amount != 0)
                    m_Stream.Write((short)amount);

                x &= 0x7FFF;

                if (direction != 0)
                    x |= 0x8000;

                m_Stream.Write((short)x);

                y &= 0x3FFF;

                if (hue != 0)
                    y |= 0x8000;

                if (flags != 0)
                    y |= 0x4000;

                m_Stream.Write((short)y);

                if (direction != 0)
                    m_Stream.Write((byte)direction);

                m_Stream.Write((sbyte)loc.Z);

                if (hue != 0)
                    m_Stream.Write((ushort)hue);

                if (flags != 0)
                    m_Stream.Write((byte)flags);
            }
        }
    }
}