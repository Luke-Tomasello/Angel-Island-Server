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

/* Server/Engines/EventResources/Obsidian/Items/ObsidianForge.cs
 * CHANGELOG:
 *  10/18/23, Yoar
 *      Initial version.
 */

namespace Server.Items
{
    [TypeAlias("Server.Engines.Obsidian.ObsidianForgeAddon")]
    public class ObsidianForgeAddon : BaseAddon
    {
        [Constructable]
        public ObsidianForgeAddon()
            : this(0)
        {
        }

        [Constructable]
        public ObsidianForgeAddon(int type)
            : base()
        {
            switch (type)
            {
                case 0: // south
                    {
                        AddComponent(new ObsidianForgeComponent(0x30), -2, -1, 0);
                        AddComponent(new ObsidianForgeComponent(0x2F), -1, -1, 0);
                        AddComponent(new ObsidianForgeComponent(0x2F), 0, -1, 0);
                        AddComponent(new ObsidianForgeComponent(0x2F), 1, -1, 0);
                        AddComponent(new ObsidianForgeComponent(0x2E), -2, 0, 0);
                        AddComponent(new ObsidianForgeComponent(0x2F), -1, 0, 0);
                        AddComponent(new ObsidianForgeComponent(0x2F), 0, 0, 0);
                        AddComponent(new ObsidianForgeComponent(0x2D), 1, 0, 0);

                        AddComponent(new ObsidianForgeComponent(0x30), -2, -1, 3);
                        AddComponent(new ObsidianForgeComponent(0x2F), -1, -1, 3);
                        AddComponent(new ObsidianForgeComponent(0x2F), 0, -1, 3);
                        AddComponent(new ObsidianForgeComponent(0x2F), 1, -1, 3);
                        AddComponent(new ObsidianForgeComponent(0x2E), -2, 0, 3);
                        AddComponent(new ObsidianForgeComponent(0x2F), -1, 0, 3);
                        AddComponent(new ObsidianForgeComponent(0x2F), 0, 0, 3);
                        AddComponent(new ObsidianForgeComponent(0x2D), 1, 0, 3);

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
                        AddComponent(new ObsidianForgeComponent(0x30), -1, -2, 0);
                        AddComponent(new ObsidianForgeComponent(0x2F), 0, -2, 0);
                        AddComponent(new ObsidianForgeComponent(0x2E), -1, -1, 0);
                        AddComponent(new ObsidianForgeComponent(0x2E), 0, -1, 0);
                        AddComponent(new ObsidianForgeComponent(0x2E), -1, 0, 0);
                        AddComponent(new ObsidianForgeComponent(0x2E), 0, 0, 0);
                        AddComponent(new ObsidianForgeComponent(0x2E), -1, 1, 0);
                        AddComponent(new ObsidianForgeComponent(0x2D), 0, 1, 0);

                        AddComponent(new ObsidianForgeComponent(0x30), -1, -2, 3);
                        AddComponent(new ObsidianForgeComponent(0x2F), 0, -2, 3);
                        AddComponent(new ObsidianForgeComponent(0x2E), -1, -1, 3);
                        AddComponent(new ObsidianForgeComponent(0x2E), 0, -1, 3);
                        AddComponent(new ObsidianForgeComponent(0x2E), -1, 0, 3);
                        AddComponent(new ObsidianForgeComponent(0x2E), 0, 0, 3);
                        AddComponent(new ObsidianForgeComponent(0x2E), -1, 1, 3);
                        AddComponent(new ObsidianForgeComponent(0x2D), 0, 1, 3);

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

        public ObsidianForgeAddon(Serial serial)
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

        [TypeAlias("Server.Engines.Obsidian.ObsidianForgeAddon+ObsidianForgeComponent")]
        private class ObsidianForgeComponent : AddonComponent
        {
            public override string DefaultName { get { return "obsidian forge"; } }

            public ObsidianForgeComponent(int itemID)
                : base(itemID)
            {
                Hue = CraftResources.GetHue(CraftResource.Obsidian);
            }

            public ObsidianForgeComponent(Serial serial)
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

    [TypeAlias("Server.Engines.Obsidian.ObsidianForgeDeed")]
    public class ObsidianForgeDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "obsidian forge"; } }
        public override BaseAddon Addon { get { return new ObsidianForgeAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Obsidian Forge (South)",
                "Obsidian Forge (East)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public ObsidianForgeDeed()
            : base()
        {
        }

        public ObsidianForgeDeed(Serial serial)
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