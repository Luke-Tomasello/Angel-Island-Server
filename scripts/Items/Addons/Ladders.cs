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

/* Scripts/Items/Addons/HouseLadder.cs
 * ChangeLog
 *	8/8/23, Yoar
 *		Based on RunUO's Scripts\Items\Special\Heritage Items\HouseLadder.cs
 */

using Server.Township;

namespace Server.Items
{
    public class HouseLadderAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new HouseLadderDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public HouseLadderAddon(int type)
            : base()
        {
            switch (type)
            {
                case 0: // castle south
                    AddComponent(new LocalizedAddonComponent(0x3DB2, 1076791), 0, 0, 0);
                    AddComponent(new LocalizedAddonComponent(0x3F28, 1076791), 0, 1, 28);
                    AddComponent(new LocalizedAddonComponent(0x3DB4, 1076791), 0, 2, 20);
                    break;
                case 1: // castle east
                    AddComponent(new LocalizedAddonComponent(0x3DB3, 1076791), 0, 0, 0);
                    AddComponent(new LocalizedAddonComponent(0x3F28, 1076791), 1, 0, 28);
                    AddComponent(new LocalizedAddonComponent(0x3DB5, 1076791), 2, 0, 20);
                    break;
                case 2: // castle north
                    AddComponent(new LocalizedAddonComponent(0x2FDF, 1076791), 0, 0, 0);
                    AddComponent(new LocalizedAddonComponent(0x3F28, 1076791), 0, -1, 28);
                    AddComponent(new LocalizedAddonComponent(0x3DB6, 1076791), 0, -2, 20);
                    break;
                case 3: // castle west
                    AddComponent(new LocalizedAddonComponent(0x2FDE, 1076791), 0, 0, 0);
                    AddComponent(new LocalizedAddonComponent(0x3F28, 1076791), -1, 0, 28);
                    AddComponent(new LocalizedAddonComponent(0x3DB7, 1076791), -2, 0, 20);
                    break;
                case 4: // south
                    AddComponent(new LocalizedAddonComponent(0x3DB2, 1076287), 0, 0, 0);
                    break;
                case 5: // east
                    AddComponent(new LocalizedAddonComponent(0x3DB3, 1076287), 0, 0, 0);
                    break;
                case 6: // north
                    AddComponent(new LocalizedAddonComponent(0x2FDF, 1076287), 0, 0, 0);
                    break;
                case 7: // west
                    AddComponent(new LocalizedAddonComponent(0x2FDE, 1076287), 0, 0, 0);
                    break;
            }
        }

        public HouseLadderAddon(Serial serial)
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

    public class HouseLadderDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new HouseLadderAddon(m_Type); } }
        public override int LabelNumber { get { return 1076287; } } // Ladder

        public override TextEntry ChoiceText { get { return 1076780; } } // Please select your ladder position.  <br>Use the ladders marked (castle) <br> for accessing the tops of keeps <br> and castles.

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                1076794, // South (Castle)
                1076795, // East (Castle)
                1076792, // North (Castle)
                1076793, // West (Castle)
                1075386, // South
                1075387, // East
                1075389, // North
                1075390, // West
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public HouseLadderDeed()
            : base()
        {
            LootType = LootType.Blessed;
        }

        public HouseLadderDeed(Serial serial)
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

    [TownshipAddon]
    public class FortLadderAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new FortLadderDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public FortLadderAddon(int type)
            : base()
        {
            switch (type)
            {
                case 0: // fort south
                    AddComponent(NamedComponent(0x3DB2, "Fort Ladder"), 0, 0, 0);
                    AddComponent(NamedComponent(0x3F28, "Fort Ladder"), 0, 1, 19);
                    break;
                case 1: // fort east
                    AddComponent(NamedComponent(0x3DB3, "Fort Ladder"), 0, 0, 0);
                    AddComponent(NamedComponent(0x3F28, "Fort Ladder"), 1, 0, 19);
                    break;
                case 2: // fort north
                    AddComponent(NamedComponent(0x2FDF, "Fort Ladder"), 0, 0, 0);
                    AddComponent(NamedComponent(0x3F28, "Fort Ladder"), 0, -1, 19);
                    break;
                case 3: // fort west
                    AddComponent(NamedComponent(0x2FDE, "Fort Ladder"), 0, 0, 0);
                    AddComponent(NamedComponent(0x3F28, "Fort Ladder"), -1, 0, 19);
                    break;
                case 4: // south
                    AddComponent(new LocalizedAddonComponent(0x3DB2, 1076287), 0, 0, 0);
                    break;
                case 5: // east
                    AddComponent(new LocalizedAddonComponent(0x3DB3, 1076287), 0, 0, 0);
                    break;
                case 6: // north
                    AddComponent(new LocalizedAddonComponent(0x2FDF, 1076287), 0, 0, 0);
                    break;
                case 7: // west
                    AddComponent(new LocalizedAddonComponent(0x2FDE, 1076287), 0, 0, 0);
                    break;
            }
        }

        private AddonComponent NamedComponent(int itemID, string name)
        {
            AddonComponent ac = new AddonComponent(itemID);

            ac.Name = name;

            return ac;
        }

        public FortLadderAddon(Serial serial)
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

    public class FortLadderDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new FortLadderAddon(m_Type); } }
        public override string DefaultName { get { return "Fort Ladder"; } }

        public override TextEntry ChoiceText { get { return "Please select your ladder position."; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "South (Fort)",
                "East (Fort)",
                "North (Fort)",
                "West (Fort)",
                1075386, // South
                1075387, // East
                1075389, // North
                1075390, // West
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public FortLadderDeed()
            : base()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public FortLadderDeed(Serial serial)
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