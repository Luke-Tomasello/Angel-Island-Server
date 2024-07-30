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

/* Scripts/Items/Special/Holiday/Halloween/BoneTable.cs
 * CHANGELOG
 *  10/26/23, Yoar
 *      Initial version.
 */

namespace Server.Items
{
    public class BoneTableAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new BoneTableDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public BoneTableAddon()
        {
            AddComponent(new AddonComponent(0x2A5C), 0, 0, 0);
        }

        public BoneTableAddon(Serial serial)
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

    public class BoneTableDeed : BaseAddonDeed
    {
        public override string DefaultName { get { return "a bone table deed"; } }
        public override BaseAddon Addon { get { return new BoneTableAddon(); } }

        [Constructable]
        public BoneTableDeed()
            : base()
        {
        }

        public BoneTableDeed(Serial serial)
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