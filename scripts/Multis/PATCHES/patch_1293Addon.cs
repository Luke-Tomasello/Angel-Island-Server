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

/* Scripts\Multis\Patches\patch_1293Addon.cs
 * ChangeLog
 *	8/12/07, Adam
 *		single tile patch for houses with marble floors. Static ID 1293
 */

using Server.Items;

namespace Server.Multis
{
    public class patch_1293Addon : BaseAddon
    {
        public override BaseAddonDeed Deed
        {
            get
            {
                return new patch_1293AddonDeed();
            }
        }

        [Constructable]
        public patch_1293Addon()
        {
            AddonComponent ac = null;
            ac = new AddonComponent(1293);
            AddComponent(ac, 0, 0, 0);

        }

        public patch_1293Addon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class patch_1293AddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new patch_1293Addon();
            }
        }

        [Constructable]
        public patch_1293AddonDeed()
        {
            Name = "patch_1293";
        }

        public patch_1293AddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}