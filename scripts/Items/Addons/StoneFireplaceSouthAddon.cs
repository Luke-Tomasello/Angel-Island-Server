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

/* Scripts/Items/Addons/StoneFireplaceSouthAddon.cs
 * ChangeLog
 *  9/17/21, Yoar
 *      Added redeedable addons.
 */

namespace Server.Items
{
    public class StoneFireplaceSouthAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new StoneFireplaceSouthDeed(); } }

        [Constructable]
        public StoneFireplaceSouthAddon()
        {
            AddComponent(new AddonComponent(0x967), -1, 0, 0);
            AddComponent(new AddonComponent(0x961), 0, 0, 0);
        }

        public StoneFireplaceSouthAddon(Serial serial)
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

    public class StoneFireplaceSouthDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new StoneFireplaceSouthAddon(); } }
        public override int LabelNumber { get { return 1061849; } } // stone fireplace (south)

        [Constructable]
        public StoneFireplaceSouthDeed()
        {
        }

        public StoneFireplaceSouthDeed(Serial serial)
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