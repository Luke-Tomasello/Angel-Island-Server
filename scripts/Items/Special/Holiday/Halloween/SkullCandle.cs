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

/* Scripts/Items/Special/Holiday/Halloween/SkullCandle.cs
 * CHANGELOG
 *  10/5/23, Yoar
 *      Initial commit.
 */

namespace Server.Items
{
    [Flipable(
        0x1853, 0x1857, 0x1853,
        0x1854, 0x1858, 0x1854)]
    public class SkullCandle : BaseLight
    {
        public override double DefaultWeight { get { return 1.0; } }

        public override int LitItemID
        {
            get
            {
                if (ItemID == 0x1853)
                    return 0x1854;
                else
                    return 0x1858;
            }
        }

        public override int UnlitItemID
        {
            get
            {
                if (ItemID == 0x1854)
                    return 0x1853;
                else
                    return 0x1857;
            }
        }

        [Constructable]
        public SkullCandle()
            : base(0x1857)
        {
            Light = LightType.Circle150;
        }

        public SkullCandle(Serial serial)
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