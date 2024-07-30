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

/* Scripts/Items/Special/Holiday/Halloween/BoneThrone.cs
 * CHANGELOG
 *  10/26/23, Yoar
 *      Initial version.
 */

namespace Server.Items
{
    public class BoneThroneAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new BoneThroneDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public BoneThroneAddon()
            : this(1)
        {
        }

        [Constructable]
        public BoneThroneAddon(int type)
        {
            switch (type)
            {
                case 0: AddComponent(new AddonComponent(0x2A59), 0, 0, 0); break; // east
                case 1: AddComponent(new AddonComponent(0x2A58), 0, 0, 0); break; // south
            }
        }

        public BoneThroneAddon(Serial serial)
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

    public class BoneThroneDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a bone throne deed"; } }
        public override BaseAddon Addon { get { return new BoneThroneAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Bone Throne (East)",
                "Bone Throne (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public BoneThroneDeed()
            : base()
        {
        }

        public BoneThroneDeed(Serial serial)
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