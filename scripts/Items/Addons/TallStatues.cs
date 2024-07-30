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

/* Scripts/Items/Addons/TallStatues.cs
 * ChangeLog
 *	7/29/23, Yoar
 *		Initial version
 */

using Server.Township;

namespace Server.Items
{
    [TownshipAddon]
    public class LordStatueAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new LordStatueAddonDeed(); } }

        [Constructable]
        public LordStatueAddon()
        {
            AddComponent(new AddonComponent(0x21A3), -1, -1, 0);
            AddComponent(new AddonComponent(0x12A0), 0, -1, 0);
            AddComponent(new AddonComponent(0x12A1), -1, 0, 0);
            AddComponent(new AddonComponent(0x129F), 0, 0, 0);
        }

        public LordStatueAddon(Serial serial)
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

    public class LordStatueAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new LordStatueAddon(); } }
        public override string DefaultName { get { return "lord statue"; } }

        [Constructable]
        public LordStatueAddonDeed()
        {
        }

        public LordStatueAddonDeed(Serial serial)
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

    [TownshipAddon]
    public class PeasantStatueAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new PeasantStatueAddonDeed(); } }

        [Constructable]
        public PeasantStatueAddon()
        {
            AddComponent(new AddonComponent(0x21A3), -1, -1, 0);
            AddComponent(new AddonComponent(0x12A3), 0, -1, 0);
            AddComponent(new AddonComponent(0x12A4), -1, 0, 0);
            AddComponent(new AddonComponent(0x12A2), 0, 0, 0);
        }

        public PeasantStatueAddon(Serial serial)
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

    public class PeasantStatueAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new PeasantStatueAddon(); } }
        public override string DefaultName { get { return "peasant statue"; } }

        [Constructable]
        public PeasantStatueAddonDeed()
        {
        }

        public PeasantStatueAddonDeed(Serial serial)
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

    [TownshipAddon]
    public class UnfinishedStatueAddon : BaseAddon
    {
        public override bool Redeedable { get { return true; } }
        public override BaseAddonDeed Deed { get { return new UnfinishedStatueAddonDeed(); } }

        [Constructable]
        public UnfinishedStatueAddon()
        {
            AddComponent(new AddonComponent(0x21A3), -1, -1, 0);
            AddComponent(new AddonComponent(0x12AF), 0, -1, 0);
            AddComponent(new AddonComponent(0x12B0), -1, 0, 0);
            AddComponent(new AddonComponent(0x12AE), 0, 0, 0);
        }

        public UnfinishedStatueAddon(Serial serial)
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

    public class UnfinishedStatueAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new UnfinishedStatueAddon(); } }
        public override string DefaultName { get { return "unfinished statue"; } }

        [Constructable]
        public UnfinishedStatueAddonDeed()
        {
        }

        public UnfinishedStatueAddonDeed(Serial serial)
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