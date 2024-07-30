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

using System;

namespace Server.Items
{
    [Flipable]
    public class WallSconce : BaseLight
    {
        public override int LitItemID
        {
            get
            {
                if (ItemID == 0x9FB)
                    return 0x9FD;
                else
                    return 0xA02;
            }
        }

        public override int UnlitItemID
        {
            get
            {
                if (ItemID == 0x9FD)
                    return 0x9FB;
                else
                    return 0xA00;
            }
        }

        [Constructable]
        public WallSconce()
            : base(0x9FB)
        {
            Movable = false;
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = false;
            Light = LightType.WestBig;
            Weight = 3.0;
        }

        public WallSconce(Serial serial)
            : base(serial)
        {
        }

        public void Flip()
        {
            if (Light == LightType.WestBig)
                Light = LightType.NorthBig;
            else if (Light == LightType.NorthBig)
                Light = LightType.WestBig;

            switch (ItemID)
            {
                case 0x9FB: ItemID = 0xA00; break;
                case 0x9FD: ItemID = 0xA02; break;

                case 0xA00: ItemID = 0x9FB; break;
                case 0xA02: ItemID = 0x9FD; break;
            }
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
}