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

/* Items/Triggers/WallSwitch.cs
 * CHANGELOG:
 * 	3/7/23, Yoar
 * 		Initial version.
 */

namespace Server.Items.Triggers
{
    [TypeAlias("Server.Items.WallSwitch")]
    [Flipable(
        0x108F, 0x1092, 0x108F,
        0x1090, 0x1091, 0x1090)]
    public class WallSwitch : Lever
    {
        protected override int DefaultUnswitchedID { get { return ((ItemID == 0x108F || ItemID == 0x1090) ? 0x108F : 0x1092); } }
        protected override int DefaultSwitchedID { get { return ((ItemID == 0x108F || ItemID == 0x1090) ? 0x1090 : 0x1091); } }

        [Constructable]
        public WallSwitch()
            : base(0x1092)
        {
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public WallSwitch(Serial serial)
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