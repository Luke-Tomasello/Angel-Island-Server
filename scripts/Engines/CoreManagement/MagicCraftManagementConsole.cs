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

/* Engines/CoreManagement/MagicCraftManagementConsole.cs
 * ChangeLog
 *	10/26/21, Adam
 *		Created.
 *		Controls the success of creating magic gear via the Magic Craft system
 */

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class MagicCraftManagementConsole : Item
    {
        [Constructable]
        public MagicCraftManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 0x33;
            Name = "Magic Craft Management Console";
        }

        public MagicCraftManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Owner)]
        public double ValoriteSuccess
        {
            get
            {
                return CoreAI.ValoriteSuccess;
            }
            set
            {
                CoreAI.ValoriteSuccess = value;
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public double VeriteSuccess
        {
            get
            {
                return CoreAI.VeriteSuccess;
            }
            set
            {
                CoreAI.VeriteSuccess = value;
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public double AgapiteSuccess
        {
            get
            {
                return CoreAI.AgapiteSuccess;
            }
            set
            {
                CoreAI.AgapiteSuccess = value;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
            {
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

}