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

/* Scripts/Items/Special/Holiday/Halloween/SacrificialAltar.cs
 * CHANGELOG
 *  12/14/23, Yoar
 *      Initial version.
 */

namespace Server.Items
{
    public class SacrificialAltarAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new SacrificialAltarDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public SacrificialAltarAddon()
            : this(1)
        {
        }

        [Constructable]
        public SacrificialAltarAddon(int type)
        {
            switch (type)
            {
                case 0: // east
                    {
                        AddComponent(new AddonComponent(0x2A9D), 0, 0, 0);
                        AddComponent(new AddonComponent(0x2A9C), 0, 1, 0);
                        break;
                    }
                case 1: // south
                    {
                        AddComponent(new AddonComponent(0x2A9B), 0, 0, 0);
                        AddComponent(new AddonComponent(0x2A9A), 1, 0, 0);
                        break;
                    }
            }
        }

        public SacrificialAltarAddon(Serial serial)
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

    public class SacrificialAltarDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a sacrificial altar deed"; } }
        public override BaseAddon Addon { get { return new SacrificialAltarAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Sacrificial Altar (East)",
                "Sacrificial Altar (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public SacrificialAltarDeed()
            : base()
        {
        }

        public SacrificialAltarDeed(Serial serial)
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