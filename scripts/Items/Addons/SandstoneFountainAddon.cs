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

/* Scripts\Items\Addons\SandStoneFountainAddon.cs  
 * Changelog
 *  9/17/21, Yoar
 *      Added redeedable addons.
 *	11/17/04, Pix
 *		Made cancelling work (needed to add public override BaseAddonDeed Deed property)
 *  11/10/04,Froste
 *      Created deed for later sale.
 *
 *
 *
 */

namespace Server.Items
{
    public class SandstoneFountainAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SandstoneFountainDeed();
            }
        }

        [Constructable]
        public SandstoneFountainAddon()
        {
            int itemID = 0x19C3;

            AddComponent(new AddonComponent(itemID++), -2, +1, 0);
            AddComponent(new AddonComponent(itemID++), -1, +1, 0);
            AddComponent(new AddonComponent(itemID++), +0, +1, 0);
            AddComponent(new AddonComponent(itemID++), +1, +1, 0);

            AddComponent(new AddonComponent(itemID++), +1, +0, 0);
            AddComponent(new AddonComponent(itemID++), +1, -1, 0);
            AddComponent(new AddonComponent(itemID++), +1, -2, 0);

            AddComponent(new AddonComponent(itemID++), +0, -2, 0);
            AddComponent(new AddonComponent(itemID++), +0, -1, 0);
            AddComponent(new AddonComponent(itemID++), +0, +0, 0);

            AddComponent(new AddonComponent(itemID++), -1, +0, 0);
            AddComponent(new AddonComponent(itemID++), -2, +0, 0);

            AddComponent(new AddonComponent(itemID++), -2, -1, 0);
            AddComponent(new AddonComponent(itemID++), -1, -1, 0);

            AddComponent(new AddonComponent(itemID++), -1, -2, 0);
            AddComponent(new AddonComponent(++itemID), -2, -2, 0);
        }

        public SandstoneFountainAddon(Serial serial)
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

    public class SandstoneFountainDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new SandstoneFountainAddon(); } }

        [Constructable]
        public SandstoneFountainDeed()
        {
            Name = "Sandstone Fountain";
        }

        public SandstoneFountainDeed(Serial serial)
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