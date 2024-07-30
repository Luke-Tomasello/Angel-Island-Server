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

/* Scripts\Items\Special\Rares\Plows.cs
 * ChangeLog:
 *  8/4/2023, Adam
 *      Initial creation
 */

namespace Server.Items.Rares
{
    public class PlowSouth : PlowSouthAddon
    {
        [Constructable]
        public PlowSouth()
            : base()
        {
        }

        public PlowSouth(Serial serial)
            : base(serial)
        {
        }
        public override void OnAfterStolen(Mobile from)
        {
            base.OnAfterStolen(from);
            if (from != null && from.Backpack != null)
            {
                from.Backpack.AddItem(new PlowSouthAddonDeed());

                this.Delete();
            }
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
    public class PlowEast : PlowEastAddon
    {
        [Constructable]
        public PlowEast()
            : base()
        {
        }

        public PlowEast(Serial serial)
            : base(serial)
        {
        }
        public override void OnAfterStolen(Mobile from)
        {
            base.OnAfterStolen(from);
            if (from != null && from.Backpack != null)
            {
                from.Backpack.AddItem(new PlowEastAddonDeed());

                this.Delete();
            }
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