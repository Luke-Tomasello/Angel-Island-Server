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

/* Scripts\Items\Addons\Signs.cs
 * CHANGELOG
 *  4/10/2024, Adam
 *      Initial version.
 */

namespace Server.Items
{
    public class BowyerSignAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new BowyerSignDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public BowyerSignAddon()
            : this(1)
        {
        }

        [Constructable]
        public BowyerSignAddon(int type)
        {
            switch (type)
            {
                case 0: // east
                    {
                        AddComponent(new AddonComponent(0xBCE), 0, 0, 0);
                        break;
                    }
                case 1: // south
                    {
                        AddComponent(new AddonComponent(0x0BCD), 0, 0, 0);
                        break;
                    }
            }
        }

        public BowyerSignAddon(Serial serial)
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

    public class BowyerSignDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a bowyer sign deed"; } }
        public override BaseAddon Addon { get { return new BowyerSignAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Bowyer Sign (East)",
                "Bowyer Sign (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public BowyerSignDeed()
            : base()
        {
        }

        public BowyerSignDeed(Serial serial)
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

    public class TailorSignAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new TailorSignDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public TailorSignAddon()
            : this(1)
        {
        }

        [Constructable]
        public TailorSignAddon(int type)
        {
            switch (type)
            {
                case 0: // east
                    {
                        AddComponent(new AddonComponent(0x0BA5), 0, 0, 0);
                        break;
                    }
                case 1: // south
                    {
                        AddComponent(new AddonComponent(0x0BA6), 0, 0, 0);
                        break;
                    }
            }
        }

        public TailorSignAddon(Serial serial)
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

    public class TailorSignDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a tailor sign deed"; } }
        public override BaseAddon Addon { get { return new TailorSignAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Tailor Sign (East)",
                "Tailor Sign (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public TailorSignDeed()
            : base()
        {
        }

        public TailorSignDeed(Serial serial)
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

    public class BlacksmithSignAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new BlacksmithSignDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public BlacksmithSignAddon()
            : this(1)
        {
        }

        [Constructable]
        public BlacksmithSignAddon(int type)
        {
            switch (type)
            {
                case 0: // east
                    {
                        AddComponent(new AddonComponent(0x0BC7), 0, 0, 0);
                        break;
                    }
                case 1: // south
                    {
                        AddComponent(new AddonComponent(0x0BC8), 0, 0, 0);
                        break;
                    }
            }
        }

        public BlacksmithSignAddon(Serial serial)
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

    public class BlacksmithSignDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "a blacksmith sign deed"; } }
        public override BaseAddon Addon { get { return new BlacksmithSignAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Blacksmith Sign (East)",
                "Blacksmith Sign (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public BlacksmithSignDeed()
            : base()
        {
        }

        public BlacksmithSignDeed(Serial serial)
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