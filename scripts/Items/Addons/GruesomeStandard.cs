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

/* Scripts\Items\Addons\GruesomeStandard.cs
 * CHANGELOG:
 * 7/5/2024. Adam
 *   First time check in
 */

namespace Server.Items
{
    [Flipable(0x0420, 0x0429)]
    public class GruesomeStandard1Component : AddonComponent
    {
        public GruesomeStandard1Component() : base(0x0420)
        {
        }

        public GruesomeStandard1Component(Serial serial) : base(serial)
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
    public class GruesomeStandard1Addon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new GruesomeStandard1Deed(); } }
        public override bool Redeedable { get { return true; } }
        public GruesomeStandard1Addon() : base()
        {
            AddComponent(new GruesomeStandard1Component(), 0, 0, 0);
        }

        public GruesomeStandard1Addon(Serial serial) : base(serial)
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
    public class GruesomeStandard1Deed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new GruesomeStandard1Addon(); } }
        public override string DefaultName { get { return "gruesome standard deed"; } }
        [Constructable]
        public GruesomeStandard1Deed() : base()
        {
            LootType = LootType.Blessed;
        }

        public GruesomeStandard1Deed(Serial serial) : base(serial)
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

    [Flipable(0x041F, 0x0428)]
    public class GruesomeStandard2Component : AddonComponent
    {
        public GruesomeStandard2Component() : base(0x041F)
        {
        }

        public GruesomeStandard2Component(Serial serial) : base(serial)
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
    public class GruesomeStandard2Addon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new GruesomeStandard2Deed(); } }
        public override bool Redeedable { get { return true; } }
        public GruesomeStandard2Addon() : base()
        {
            AddComponent(new GruesomeStandard2Component(), 0, 0, 0);
        }

        public GruesomeStandard2Addon(Serial serial) : base(serial)
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
    public class GruesomeStandard2Deed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new GruesomeStandard2Addon(); } }
        public override string DefaultName { get { return "gruesome standard deed"; } }
        [Constructable]
        public GruesomeStandard2Deed() : base()
        {
            LootType = LootType.Blessed;
        }

        public GruesomeStandard2Deed(Serial serial) : base(serial)
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