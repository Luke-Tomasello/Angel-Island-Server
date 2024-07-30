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

/* Scripts\Engines\EventResources\Dragonglass\Items\DragonglassForge.cs
 * CHANGELOG:
 *  4/26/2024, Adam
 *      Initial version.
 */

namespace Server.Items
{
    public class DragonglassForgeAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new DragonglassForgeDeed(); } }

        [Constructable]
        public DragonglassForgeAddon()
            : this(0)
        {
        }

        [Constructable]
        public DragonglassForgeAddon(int type)
            : base()
        {
            switch (type)
            {
                case 0: // south
                    {
                        AddComponent(new DragonglassForgeComponent(0x30), -2, -1, 0);
                        AddComponent(new DragonglassForgeComponent(0x2F), -1, -1, 0);
                        AddComponent(new DragonglassForgeComponent(0x2F), 0, -1, 0);
                        AddComponent(new DragonglassForgeComponent(0x2F), 1, -1, 0);
                        AddComponent(new DragonglassForgeComponent(0x2E), -2, 0, 0);
                        AddComponent(new DragonglassForgeComponent(0x2F), -1, 0, 0);
                        AddComponent(new DragonglassForgeComponent(0x2F), 0, 0, 0);
                        AddComponent(new DragonglassForgeComponent(0x2D), 1, 0, 0);

                        AddComponent(new DragonglassForgeComponent(0x30), -2, -1, 3);
                        AddComponent(new DragonglassForgeComponent(0x2F), -1, -1, 3);
                        AddComponent(new DragonglassForgeComponent(0x2F), 0, -1, 3);
                        AddComponent(new DragonglassForgeComponent(0x2F), 1, -1, 3);
                        AddComponent(new DragonglassForgeComponent(0x2E), -2, 0, 3);
                        AddComponent(new DragonglassForgeComponent(0x2F), -1, 0, 3);
                        AddComponent(new DragonglassForgeComponent(0x2F), 0, 0, 3);
                        AddComponent(new DragonglassForgeComponent(0x2D), 1, 0, 3);

                        AddComponent(new AddonComponent(0x12EE), -1, 0, 4);
                        AddComponent(new AddonComponent(0x12EE), 0, 0, 4);
                        AddComponent(new AddonComponent(0x12EE), 1, 0, 4);

                        AddComponent(new AddonComponent(0x1A75), -1, 0, 6);
                        AddComponent(new AddonComponent(0x1A75), 0, 0, 6);
                        AddComponent(new AddonComponent(0x1A75), 1, 0, 6);

                        break;
                    }
                case 1: // east
                    {
                        AddComponent(new DragonglassForgeComponent(0x30), -1, -2, 0);
                        AddComponent(new DragonglassForgeComponent(0x2F), 0, -2, 0);
                        AddComponent(new DragonglassForgeComponent(0x2E), -1, -1, 0);
                        AddComponent(new DragonglassForgeComponent(0x2E), 0, -1, 0);
                        AddComponent(new DragonglassForgeComponent(0x2E), -1, 0, 0);
                        AddComponent(new DragonglassForgeComponent(0x2E), 0, 0, 0);
                        AddComponent(new DragonglassForgeComponent(0x2E), -1, 1, 0);
                        AddComponent(new DragonglassForgeComponent(0x2D), 0, 1, 0);

                        AddComponent(new DragonglassForgeComponent(0x30), -1, -2, 3);
                        AddComponent(new DragonglassForgeComponent(0x2F), 0, -2, 3);
                        AddComponent(new DragonglassForgeComponent(0x2E), -1, -1, 3);
                        AddComponent(new DragonglassForgeComponent(0x2E), 0, -1, 3);
                        AddComponent(new DragonglassForgeComponent(0x2E), -1, 0, 3);
                        AddComponent(new DragonglassForgeComponent(0x2E), 0, 0, 3);
                        AddComponent(new DragonglassForgeComponent(0x2E), -1, 1, 3);
                        AddComponent(new DragonglassForgeComponent(0x2D), 0, 1, 3);

                        AddComponent(new AddonComponent(0x12EE), 0, -1, 4);
                        AddComponent(new AddonComponent(0x12EE), 0, 0, 4);
                        AddComponent(new AddonComponent(0x12EE), 0, 1, 4);

                        AddComponent(new AddonComponent(0x1A75), 0, -1, 6);
                        AddComponent(new AddonComponent(0x1A75), 0, 0, 6);
                        AddComponent(new AddonComponent(0x1A75), 0, 1, 6);

                        break;
                    }
            }
        }

        public DragonglassForgeAddon(Serial serial)
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

        private class DragonglassForgeComponent : AddonComponent
        {
            public override string DefaultName { get { return "Dragonglass forge"; } }

            public DragonglassForgeComponent(int itemID)
                : base(itemID)
            {
                Hue = CraftResources.GetHue(CraftResource.Dragonglass);
            }

            public DragonglassForgeComponent(Serial serial)
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

    public class DragonglassForgeDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "Dragonglass forge"; } }
        public override BaseAddon Addon { get { return new DragonglassForgeAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Dragonglass Forge (South)",
                "Dragonglass Forge (East)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public DragonglassForgeDeed()
            : base()
        {
        }

        public DragonglassForgeDeed(Serial serial)
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