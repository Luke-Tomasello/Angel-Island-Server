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

/* Server/Engines/EventResources/Stahlrim/Items/FrozenForge.cs
 * CHANGELOG:
 *  12/11/23, Yoar
 *      Initial version.
 */

namespace Server.Items
{
    public class FrozenForgeAddon : BaseAddon
    {
        [Constructable]
        public FrozenForgeAddon()
            : this(0)
        {
        }

        [Constructable]
        public FrozenForgeAddon(int type)
            : base()
        {
            switch (type)
            {
                case 0: // south
                    {
                        AddComponent(new FrozenForgeComponent(0x197A), 0, 0, 0);
                        AddComponent(new FrozenForgeComponent(0x197E), 1, 0, 0);
                        AddComponent(new FrozenForgeComponent(0x19A2), 2, 0, 0);
                        AddComponent(new FrozenForgeComponent(0x199E), 3, 0, 0);
                        break;
                    }
                case 1: // east
                    {
                        AddComponent(new FrozenForgeComponent(0x1986), 0, 0, 0);
                        AddComponent(new FrozenForgeComponent(0x198A), 0, 1, 0);
                        AddComponent(new FrozenForgeComponent(0x1996), 0, 2, 0);
                        AddComponent(new FrozenForgeComponent(0x1992), 0, 3, 0);
                        break;
                    }
            }
        }

        public FrozenForgeAddon(Serial serial)
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

        private class FrozenForgeComponent : AddonComponent
        {
            public override string DefaultName { get { return "frozen forge"; } }

            public FrozenForgeComponent(int itemID)
                : base(itemID)
            {
                Hue = CraftResources.GetHue(CraftResource.Stahlrim);
            }

            public FrozenForgeComponent(Serial serial)
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

    public class FrozenForgeDeed : BaseChoiceAddonDeed
    {
        public override string DefaultName { get { return "frozen forge"; } }
        public override BaseAddon Addon { get { return new FrozenForgeAddon(m_Type); } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Frozen Forge (South)",
                "Frozen Forge (East)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public FrozenForgeDeed()
            : base()
        {
        }

        public FrozenForgeDeed(Serial serial)
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