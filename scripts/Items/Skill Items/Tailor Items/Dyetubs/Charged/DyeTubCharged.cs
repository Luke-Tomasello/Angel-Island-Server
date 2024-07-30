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

/* Scripts/Items/Skill Items/Tailor Items/Misc/Charged/DyeTubCharged.cs
 * ChangeLog:
 *  11/27/23, Yoar
 *      Moved functionality to DyeTub.
 *  9/19/21, Yoar
 *      Initial Version. Serves as a base class for all limited-use dye tubs.
 */

namespace Server.Items
{
    public abstract class DyeTubCharged : DyeTub
    {
        public DyeTubCharged(int hue, int uses)
            : base()
        {
            SetDyedHue(hue);
            UsesRemaining = uses;
            Redyable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            // do nothing - special behavior must be defined in the child classes
        }

        public DyeTubCharged(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x81); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version;

            if ((Utility.PeekByte(reader) & 0x80) == 0)
            {
                version = 0; // old version
            }
            else
            {
                version = reader.ReadByte();

                switch (version)
                {
                    case 0x81:
                    case 0x80:
                        {
                            if (version < 0x81)
                                UsesRemaining = reader.ReadInt();

                            break;
                        }
                }
            }
        }
    }
}