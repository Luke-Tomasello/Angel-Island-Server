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

/* Scripts\Items\Addons\SwordDisplay.cs
 * CHANGELOG:
 * 7/5/2024. Adam
 *   First time check in
 */

namespace Server.Items
{
    [Flipable(0x2A47, 0x2A48)]
    public class SwordDisplayBlackComponent : AddonComponent
    {
        public SwordDisplayBlackComponent() : base(0x2A47)
        {
        }

        public SwordDisplayBlackComponent(Serial serial) : base(serial)
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
    public class SwordDisplayBlackAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new SwordDisplayBlackDeed(); } }
        public override bool Redeedable { get { return true; } }
        public SwordDisplayBlackAddon() : base()
        {
            AddComponent(new SwordDisplayBlackComponent(), 0, 0, 0);
        }

        public SwordDisplayBlackAddon(Serial serial) : base(serial)
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
    public class SwordDisplayBlackDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new SwordDisplayBlackAddon(); } }
        public override string DefaultName { get { return "black sword display deed"; } }
        [Constructable]
        public SwordDisplayBlackDeed() : base()
        {
            LootType = LootType.Regular;
        }

        public SwordDisplayBlackDeed(Serial serial) : base(serial)
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
    [Flipable(0x2A45, 0x2A46)]
    public class SwordDisplayRedComponent : AddonComponent
    {
        public SwordDisplayRedComponent() : base(0x2A45)
        {
        }

        public SwordDisplayRedComponent(Serial serial) : base(serial)
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
    public class SwordDisplayRedAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new SwordDisplayRedDeed(); } }
        public override bool Redeedable { get { return true; } }
        public SwordDisplayRedAddon() : base()
        {
            AddComponent(new SwordDisplayRedComponent(), 0, 0, 0);
        }

        public SwordDisplayRedAddon(Serial serial) : base(serial)
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
    public class SwordDisplayRedDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new SwordDisplayRedAddon(); } }
        public override string DefaultName { get { return "red sword display deed"; } }

        [Constructable]
        public SwordDisplayRedDeed() : base()
        {
            LootType = LootType.Regular;
        }

        public SwordDisplayRedDeed(Serial serial) : base(serial)
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
    [Flipable(0x2A49, 0x2A4A)]
    public class SwordDisplayVioletComponent : AddonComponent
    {
        public SwordDisplayVioletComponent() : base(0x2A49)
        {
        }

        public SwordDisplayVioletComponent(Serial serial) : base(serial)
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
    public class SwordDisplayVioletAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new SwordDisplayVioletDeed(); } }
        public override bool Redeedable { get { return true; } }
        public SwordDisplayVioletAddon() : base()
        {
            AddComponent(new SwordDisplayVioletComponent(), 0, 0, 0);
        }

        public SwordDisplayVioletAddon(Serial serial) : base(serial)
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
    public class SwordDisplayVioletDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new SwordDisplayVioletAddon(); } }
        public override string DefaultName { get { return "violet sword display deed"; } }

        [Constructable]
        public SwordDisplayVioletDeed() : base()
        {
            LootType = LootType.Regular;
        }

        public SwordDisplayVioletDeed(Serial serial) : base(serial)
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
    [Flipable(0x2A4B, 0x2A4C)]
    public class SwordDisplayGreenComponent : AddonComponent
    {
        public SwordDisplayGreenComponent() : base(0x2A4B)
        {
        }

        public SwordDisplayGreenComponent(Serial serial) : base(serial)
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
    public class SwordDisplayGreenAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new SwordDisplayGreenDeed(); } }
        public override bool Redeedable { get { return true; } }
        public SwordDisplayGreenAddon() : base()
        {
            AddComponent(new SwordDisplayGreenComponent(), 0, 0, 0);
        }

        public SwordDisplayGreenAddon(Serial serial) : base(serial)
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
    public class SwordDisplayGreenDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new SwordDisplayGreenAddon(); } }
        public override string DefaultName { get { return "green sword display deed"; } }

        [Constructable]
        public SwordDisplayGreenDeed() : base()
        {
            LootType = LootType.Regular;
        }

        public SwordDisplayGreenDeed(Serial serial) : base(serial)
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