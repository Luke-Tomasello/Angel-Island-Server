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

/* Items/Deeds/TeleporterDeed.cs
 * ChangeLog:
 *	9/13/06, Pix.
 *		Added logic to convert this deed to new teleporter pair addon deed on doubleclick and in backpack
 *  8/27/04, Adam
 *		Created.
 *		Add message when double clicked.
 */

namespace Server.Items
{
    public class TeleporterDeed : Item
    {
        [Constructable]
        public TeleporterDeed()
            : base(0x14F0)
        {
            base.Weight = 1.0;
            base.Name = "a teleporter deed";
        }

        public TeleporterDeed(Serial serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            //from.SendMessage( "Please page a GM for Teleporter instalation." );
            if (this.IsChildOf(from.Backpack))
            {
                from.AddToBackpack(new TeleporterAddonDeed());
                this.Delete();
                from.SendMessage("Your old teleporter deed has been converted to a new teleporter deed.");
            }
            else
            {
                from.SendMessage("This must be in your backpack to use.");
            }
        }
    }
}