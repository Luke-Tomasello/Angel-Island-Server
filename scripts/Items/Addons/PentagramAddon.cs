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

/* Scripts/Items/Addons/PentagramAddon.cs
 * ChangeLog
 *	06/06/06, Adam
 *		Add AddComponent override to allow the components being added to be invisible.
 */

namespace Server.Items
{
    public class PentagramAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new PentagramDeed(); } }
        private bool m_quiet;

        [Constructable]
        public PentagramAddon()
        {
            AddComponent(new AddonComponent(0xFE7), -1, -1, 0);
            AddComponent(new AddonComponent(0xFE8), 0, -1, 0);
            AddComponent(new AddonComponent(0xFEB), 1, -1, 0);
            AddComponent(new AddonComponent(0xFE6), -1, 0, 0);
            AddComponent(new AddonComponent(0xFEA), 0, 0, 0);
            AddComponent(new AddonComponent(0xFEE), 1, 0, 0);
            AddComponent(new AddonComponent(0xFE9), -1, 1, 0);
            AddComponent(new AddonComponent(0xFEC), 0, 1, 0);
            AddComponent(new AddonComponent(0xFED), 1, 1, 0);
        }

        [Constructable]
        public PentagramAddon(bool bQuiet)
        {
            m_quiet = bQuiet;

            AddComponent(new AddonComponent(0xFE7), -1, -1, 0);
            AddComponent(new AddonComponent(0xFE8), 0, -1, 0);
            AddComponent(new AddonComponent(0xFEB), 1, -1, 0);
            AddComponent(new AddonComponent(0xFE6), -1, 0, 0);
            AddComponent(new AddonComponent(0xFEA), 0, 0, 0);
            AddComponent(new AddonComponent(0xFEE), 1, 0, 0);
            AddComponent(new AddonComponent(0xFE9), -1, 1, 0);
            AddComponent(new AddonComponent(0xFEC), 0, 1, 0);
            AddComponent(new AddonComponent(0xFED), 1, 1, 0);
        }

        public PentagramAddon(Serial serial)
            : base(serial)
        {
        }

        public override void AddComponent(AddonComponent c, int x, int y, int z)
        {
            if (Deleted)
                return;

            base.AddComponent(c, x, y, z);

            c.Visible = (m_quiet == true) ? false : true;
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

    public class PentagramDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new PentagramAddon(); } }
        public override int LabelNumber { get { return 1044328; } } // pentagram

        [Constructable]
        public PentagramDeed()
        {
        }

        public PentagramDeed(Serial serial)
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