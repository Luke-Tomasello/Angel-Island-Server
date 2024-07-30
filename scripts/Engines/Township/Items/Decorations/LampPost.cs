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

/* Engines/Township/Items/Decorations/LampPost.cs
 * CHANGELOG:
 * 11/23/21, Yoar
 *      Initial version.
*/

using System;

namespace Server.Township
{
    public class TownshipLampPost : TownshipLight
    {
        public override int LitItemID
        {
            get
            {
                switch (this.ItemID)
                {
                    case 0xB21: return 0xB20;
                    case 0xB23: return 0xB22;
                    case 0xB25: return 0xB24;
                }

                return this.ItemID;
            }
        }

        public override int UnlitItemID
        {
            get
            {
                switch (this.ItemID)
                {
                    case 0xB20: return 0xB21;
                    case 0xB22: return 0xB23;
                    case 0xB24: return 0xB25;
                }

                return this.ItemID;
            }
        }

        [Constructable]
        public TownshipLampPost(int itemID)
            : base(itemID)
        {
            SetHits(500);
            Duration = TimeSpan.Zero;
            Light = LightType.Circle300;
            Weight = 40.0;
        }

        public TownshipLampPost(Serial serial)
            : base(serial)
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