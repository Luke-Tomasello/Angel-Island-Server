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

/* Scripts\Items\Addons\HitchingPost.cs
 * CHANGELOG:
 * 7/5/2024. Adam
 *   First time check in
 */

namespace Server.Items
{
    [Flipable(0x14E7, 0x14E8)]
    public class HitchingPostComponent : AddonComponent
    {
        public HitchingPostComponent() : base(0x14E7)
        {
        }

        public HitchingPostComponent(Serial serial) : base(serial)
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
    public class HitchingPostAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new HitchingPostDeed(); } }
        public override bool Redeedable { get { return true; } }
        public HitchingPostAddon() : base()
        {
            AddComponent(new HitchingPostComponent(), 0, 0, 0);
        }

        public HitchingPostAddon(Serial serial) : base(serial)
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
    public class HitchingPostDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new HitchingPostAddon(); } }
        public override string DefaultName { get { return "hitching post deed"; } }
        [Constructable]
        public HitchingPostDeed() : base()
        {
            LootType = LootType.Blessed;
        }

        public HitchingPostDeed(Serial serial) : base(serial)
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