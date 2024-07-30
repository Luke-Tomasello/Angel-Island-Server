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

/* Scripts/Items/Special/Holiday/Halloween/BoneCouch.cs
 * CHANGELOG
 *  10/26/23, Yoar
 *      Initial version.
 */

namespace Server.Items
{
    public class BoneCouchAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new BoneCouchDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public BoneCouchAddon()
            : this(1)
        {
        }

        [Constructable]
        public BoneCouchAddon(int type)
        {
            switch (type)
            {
                case 0: // east
                    {
                        AddComponent(new AddonComponent(0x2A80), 0, 0, 0);
                        AddComponent(new AddonComponent(0x2A7F), 0, 1, 0);
                        break;
                    }
                case 1: // south
                    {
                        AddComponent(new AddonComponent(0x2A5B), 0, 0, 0);
                        AddComponent(new AddonComponent(0x2A5A), 1, 0, 0);
                        break;
                    }
            }
        }

        public BoneCouchAddon(Serial serial)
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

    public class BoneCouchDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a bone couch deed"; } }
        public override BaseAddon Addon { get { return new BoneCouchAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Bone Couch (East)",
                "Bone Couch (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public BoneCouchDeed()
            : base()
        {
        }

        public BoneCouchDeed(Serial serial)
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