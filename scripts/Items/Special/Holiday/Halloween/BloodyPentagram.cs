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

/* Scripts/Items/Special/Holiday/Halloween/BloodyPentagram.cs
 * CHANGELOG
 *  10/5/23, Yoar
 *      Initial commit.
 */

namespace Server.Items
{
    public class BloodyPentagramAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new BloodyPentagramDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public BloodyPentagramAddon()
            : base()
        {
            AddComponent(new AddonComponent(0x1CF9), -2, -1, 0);
            AddComponent(new AddonComponent(0x1CF8), -2, 0, 0);
            AddComponent(new AddonComponent(0x1CF7), -2, 1, 0);
            AddComponent(new AddonComponent(0x1CF6), -2, 2, 0);
            AddComponent(new AddonComponent(0x1CF5), -2, 3, 0);

            AddComponent(new AddonComponent(0x1CFB), -1, -2, 0);
            AddComponent(new AddonComponent(0x1CFA), -1, -1, 0);
            AddComponent(new AddonComponent(0x1D09), -1, 0, 0);
            AddComponent(new AddonComponent(0x1D08), -1, 1, 0);
            AddComponent(new AddonComponent(0x1D07), -1, 2, 0);
            AddComponent(new AddonComponent(0x1CF4), -1, 3, 0);

            AddComponent(new AddonComponent(0x1CFC), 0, -2, 0);
            AddComponent(new AddonComponent(0x1D0A), 0, -1, 0);
            AddComponent(new AddonComponent(0x1D11), 0, 0, 0);
            AddComponent(new AddonComponent(0x1D10), 0, 1, 0);
            AddComponent(new AddonComponent(0x1D06), 0, 2, 0);
            AddComponent(new AddonComponent(0x1CF3), 0, 3, 0);

            AddComponent(new AddonComponent(0x1CFD), 1, -2, 0);
            AddComponent(new AddonComponent(0x1D0B), 1, -1, 0);
            AddComponent(new AddonComponent(0x1D12), 1, 0, 0);
            AddComponent(new AddonComponent(0x1D0F), 1, 1, 0);
            AddComponent(new AddonComponent(0x1D05), 1, 2, 0);
            AddComponent(new AddonComponent(0x1CF2), 1, 3, 0);

            AddComponent(new AddonComponent(0x1CFE), 2, -2, 0);
            AddComponent(new AddonComponent(0x1D0C), 2, -1, 0);
            AddComponent(new AddonComponent(0x1D0D), 2, 0, 0);
            AddComponent(new AddonComponent(0x1D0E), 2, 1, 0);
            AddComponent(new AddonComponent(0x1D04), 2, 2, 0);
            AddComponent(new AddonComponent(0x1CF1), 2, 3, 0);

            AddComponent(new AddonComponent(0x1CFF), 3, -2, 0);
            AddComponent(new AddonComponent(0x1D00), 3, -1, 0);
            AddComponent(new AddonComponent(0x1D01), 3, 0, 0);
            AddComponent(new AddonComponent(0x1D02), 3, 1, 0);
            AddComponent(new AddonComponent(0x1D03), 3, 2, 0);
        }

        public BloodyPentagramAddon(Serial serial)
            : base(serial)
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

    public class BloodyPentagramDeed : BaseAddonDeed
    {
        public override string DefaultName { get { return "a bloody pentagram deed"; } }
        public override BaseAddon Addon { get { return new BloodyPentagramAddon(); } }

        [Constructable]
        public BloodyPentagramDeed()
            : base()
        {
        }

        public BloodyPentagramDeed(Serial serial)
            : base(serial)
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