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

/* Scripts\Items\Addons\MountedPixie.cs
 * CHANGELOG:
 * 7/5/2024. Adam
 *   First time check in
 */

using Server.Network;

namespace Server.Items
{
    [Flipable(0x2A79, 0x2A7A)]
    public class MountedPixieWhiteComponent : AddonComponent
    {
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie

        public MountedPixieWhiteComponent() : base(0x2A79)
        {
        }

        public MountedPixieWhiteComponent(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Utility.InRange(Location, from.Location, 2))
                Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x562, 0x564));
            else
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
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

    public class MountedPixieWhiteAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new MountedPixieWhiteDeed(); } }
        public override bool Redeedable { get { return true; } }
        public MountedPixieWhiteAddon() : base()
        {
            AddComponent(new MountedPixieWhiteComponent(), 0, 0, 0);
        }

        public MountedPixieWhiteAddon(Serial serial) : base(serial)
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

    public class MountedPixieWhiteDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MountedPixieWhiteAddon(); } }
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie
        public override string DefaultName { get { return "white mounted pixie deed"; } }
        [Constructable]
        public MountedPixieWhiteDeed() : base()
        {
            LootType = LootType.Regular;
        }

        public MountedPixieWhiteDeed(Serial serial) : base(serial)
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
    [Flipable(0x2A73, 0x2A74)]
    public class MountedPixieOrangeComponent : AddonComponent
    {
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie

        public MountedPixieOrangeComponent() : base(0x2A73)
        {
        }

        public MountedPixieOrangeComponent(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Utility.InRange(Location, from.Location, 2))
                Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x558, 0x55B));
            else
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
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

    public class MountedPixieOrangeAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new MountedPixieOrangeDeed(); } }
        public override bool Redeedable { get { return true; } }
        public MountedPixieOrangeAddon() : base()
        {
            AddComponent(new MountedPixieOrangeComponent(), 0, 0, 0);
        }

        public MountedPixieOrangeAddon(Serial serial) : base(serial)
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

    public class MountedPixieOrangeDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MountedPixieOrangeAddon(); } }
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie
        public override string DefaultName { get { return "orange mounted pixie deed"; } }

        [Constructable]
        public MountedPixieOrangeDeed() : base()
        {
            LootType = LootType.Regular;
        }

        public MountedPixieOrangeDeed(Serial serial) : base(serial)
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
    [Flipable(0x2A77, 0x2A78)]
    public class MountedPixieLimeComponent : AddonComponent
    {
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie

        public MountedPixieLimeComponent() : base(0x2A77)
        {
        }

        public MountedPixieLimeComponent(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Utility.InRange(Location, from.Location, 2))
                Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x55F, 0x561));
            else
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
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

    public class MountedPixieLimeAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new MountedPixieLimeDeed(); } }
        public override bool Redeedable { get { return true; } }
        public MountedPixieLimeAddon() : base()
        {
            AddComponent(new MountedPixieLimeComponent(), 0, 0, 0);
        }

        public MountedPixieLimeAddon(Serial serial) : base(serial)
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

    public class MountedPixieLimeDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MountedPixieLimeAddon(); } }
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie
        public override string DefaultName { get { return "lime mounted pixie deed"; } }

        [Constructable]
        public MountedPixieLimeDeed() : base()
        {
            LootType = LootType.Regular;
        }

        public MountedPixieLimeDeed(Serial serial) : base(serial)
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
    [Flipable(0x2A75, 0x2A76)]
    public class MountedPixieBlueComponent : AddonComponent
    {
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie

        public MountedPixieBlueComponent() : base(0x2A75)
        {
        }

        public MountedPixieBlueComponent(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Utility.InRange(Location, from.Location, 2))
                Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x55C, 0x55E));
            else
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
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

    public class MountedPixieBlueAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new MountedPixieBlueDeed(); } }
        public override bool Redeedable { get { return true; } }
        public MountedPixieBlueAddon() : base()
        {
            AddComponent(new MountedPixieBlueComponent(), 0, 0, 0);
        }

        public MountedPixieBlueAddon(Serial serial) : base(serial)
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

    public class MountedPixieBlueDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MountedPixieBlueAddon(); } }
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie
        public override string DefaultName { get { return "blue mounted pixie deed"; } }

        [Constructable]
        public MountedPixieBlueDeed() : base()
        {
            LootType = LootType.Regular;
        }

        public MountedPixieBlueDeed(Serial serial) : base(serial)
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

    [Flipable(0x2A71, 0x2A72)]
    public class MountedPixieGreenComponent : AddonComponent
    {
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie

        public MountedPixieGreenComponent() : base(0x2A71)
        {
        }

        public MountedPixieGreenComponent(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Utility.InRange(Location, from.Location, 2))
                Effects.PlaySound(Location, Map, Utility.RandomMinMax(0x554, 0x557));
            else
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
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

    public class MountedPixieGreenAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new MountedPixieGreenDeed(); } }
        public override bool Redeedable { get { return true; } }
        public MountedPixieGreenAddon() : base()
        {
            AddComponent(new MountedPixieGreenComponent(), 0, 0, 0);
        }

        public MountedPixieGreenAddon(Serial serial) : base(serial)
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

    public class MountedPixieGreenDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new MountedPixieGreenAddon(); } }
        public override int LabelNumber { get { return 1074482; } } // Mounted pixie
        public override string DefaultName { get { return "green mounted pixie deed"; } }

        [Constructable]
        public MountedPixieGreenDeed() : base()
        {
            LootType = LootType.Regular;
        }

        public MountedPixieGreenDeed(Serial serial) : base(serial)
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