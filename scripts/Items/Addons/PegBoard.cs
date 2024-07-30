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

/* Scripts\Items\Addons\PegBoard.cs
 * CHANGELOG:
 * 7/5/2024. Adam
 *   First time check in
 */

namespace Server.Items
{
    [Flipable(0x0C39, 0x0C40)]
    public class PegBoardComponent : AddonComponent
    {
        public PegBoardComponent() : base(0x0C39)
        {
        }

        public PegBoardComponent(Serial serial) : base(serial)
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
    public class PegBoardAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new PegBoardDeed(); } }
        public override bool Redeedable { get { return true; } }
        public PegBoardAddon() : base()
        {
            AddComponent(new PegBoardComponent(), 0, 0, 0);
        }

        public PegBoardAddon(Serial serial) : base(serial)
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
    public class PegBoardDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new PegBoardAddon(); } }
        public override string DefaultName { get { return "peg board deed"; } }
        [Constructable]
        public PegBoardDeed() : base()
        {
            LootType = LootType.Blessed;
        }

        public PegBoardDeed(Serial serial) : base(serial)
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