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

/* Scripts/Items/Addons/MarketStalls.cs
 * ChangeLog
 *  2/3/2024, Yoar
 *      Added placable market stall vendors
 *	1/13/2024, Yoar
 *		Initial version
 */

using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Township;
using System.Collections.Generic;

namespace Server.Items
{
    #region Small Vesper

    [TownshipAddon]
    public class SmallVesperMarketStallAddon : BaseMarketStallAddon
    {
        public override BaseAddonDeed Deed { get { return new SmallVesperMarketStallDeed(); } }
        public override bool Redeedable { get { return true; } }
        public override bool ShareHue { get { return false; } }

        [Constructable]
        public SmallVesperMarketStallAddon()
            : this(0)
        {
        }

        [Constructable]
        public SmallVesperMarketStallAddon(int type)
            : base((type % 2) == 1 ? -1 : 0)
        {
            switch (type)
            {
                case 0: // darkwood, south
                    {
                        // side panel (west)
                        AddComponent(new AddonComponent(0x09), -1, -1, 0);
                        AddComponent(new AddonComponent(0x0D), -1, 0, 0);

                        // side panel (east)
                        AddComponent(new AddonComponent(0x09), 1, -1, 0);
                        AddComponent(new AddonComponent(0x0D), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x15), -1, 1, 0);
                        AddComponent(new AddonComponent(0x16), 0, 1, 0);
                        AddComponent(new AddonComponent(0x14), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3E), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x616, false), 0, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, true), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, false), 2, 0, 25);
                        AddComponent(new MarketStallComponent(0x615, false), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, true), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, false), 2, 1, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 1, 2, 0);

                        break;
                    }
                case 1: // darkwood, east
                    {
                        // side panel (north)
                        AddComponent(new AddonComponent(0x09), -1, -1, 0);
                        AddComponent(new AddonComponent(0x0C), 0, -1, 0);

                        // side panel (south)
                        AddComponent(new AddonComponent(0x09), -1, 1, 0);
                        AddComponent(new AddonComponent(0x0C), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x16), 1, -1, 0);
                        AddComponent(new AddonComponent(0x15), 1, 0, 0);
                        AddComponent(new AddonComponent(0x14), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3D), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x634, false), 0, 0, 25);
                        AddComponent(new MarketStallComponent(0x635, false), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x634, true), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x635, true), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x634, false), 0, 2, 25);
                        AddComponent(new MarketStallComponent(0x635, false), 1, 2, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 1, 0);

                        break;
                    }
                case 2: // lightwood, south
                    {
                        // side panel (west)
                        AddComponent(new AddonComponent(0x09), -1, -1, 0);
                        AddComponent(new AddonComponent(0xAC), -1, 0, 0);

                        // side panel (east)
                        AddComponent(new AddonComponent(0x09), 1, -1, 0);
                        AddComponent(new AddonComponent(0xAC), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0xBE), -1, 1, 0);
                        AddComponent(new AddonComponent(0xBF), 0, 1, 0);
                        AddComponent(new AddonComponent(0xBD), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3E), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x616, false), 0, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, true), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, false), 2, 0, 25);
                        AddComponent(new MarketStallComponent(0x615, false), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, true), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, false), 2, 1, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 1, 2, 0);

                        break;
                    }
                case 3: // lightwood, east
                    {
                        // side panel (north)
                        AddComponent(new AddonComponent(0x09), -1, -1, 0);
                        AddComponent(new AddonComponent(0xAD), 0, -1, 0);

                        // side panel (south)
                        AddComponent(new AddonComponent(0x09), -1, 1, 0);
                        AddComponent(new AddonComponent(0xAD), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0xBF), 1, -1, 0);
                        AddComponent(new AddonComponent(0xBE), 1, 0, 0);
                        AddComponent(new AddonComponent(0xBD), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3D), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x634, false), 0, 0, 25);
                        AddComponent(new MarketStallComponent(0x635, false), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x634, true), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x635, true), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x634, false), 0, 2, 25);
                        AddComponent(new MarketStallComponent(0x635, false), 1, 2, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 1, 0);

                        break;
                    }
            }
        }

        public SmallVesperMarketStallAddon(Serial serial)
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

    public class SmallVesperMarketStallDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new SmallVesperMarketStallAddon(m_Type); } }
        public override string DefaultName { get { return "Small Vesper Market Stall"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Darkwood, South",
                "Darkwood, East",
                "Lightwood, South",
                "Lightwood, East",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public SmallVesperMarketStallDeed()
            : base()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public SmallVesperMarketStallDeed(Serial serial)
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

    #endregion

    #region Large Vesper

    [TownshipAddon]
    public class LargeVesperMarketStallAddon : BaseMarketStallAddon
    {
        public override BaseAddonDeed Deed { get { return new LargeVesperMarketStallDeed(); } }
        public override bool Redeedable { get { return true; } }
        public override bool ShareHue { get { return false; } }

        [Constructable]
        public LargeVesperMarketStallAddon()
            : this(0)
        {
        }

        [Constructable]
        public LargeVesperMarketStallAddon(int type)
            : base((type % 2) == 1 ? -1 : 0)
        {
            switch (type)
            {
                case 0: // darkwood, south
                    {
                        // side panel (west)
                        AddComponent(new AddonComponent(0x09), -2, -1, 0);
                        AddComponent(new AddonComponent(0x0D), -2, 0, 0);

                        // side panel (east)
                        AddComponent(new AddonComponent(0x09), 1, -1, 0);
                        AddComponent(new AddonComponent(0x0D), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x15), -2, 1, 0);
                        AddComponent(new AddonComponent(0x16), -1, 1, 0);
                        AddComponent(new AddonComponent(0x16), 0, 1, 0);
                        AddComponent(new AddonComponent(0x14), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3E), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x616, false), -1, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, true), 0, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, false), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, true), 2, 0, 25);
                        AddComponent(new MarketStallComponent(0x615, false), -1, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, true), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, true), 2, 1, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 0);

                        break;
                    }
                case 1: // darkwood, east
                    {
                        // side panel (north)
                        AddComponent(new AddonComponent(0x09), -1, -2, 0);
                        AddComponent(new AddonComponent(0x0C), 0, -2, 0);

                        // side panel (south)
                        AddComponent(new AddonComponent(0x09), -1, 1, 0);
                        AddComponent(new AddonComponent(0x0C), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x16), 1, -2, 0);
                        AddComponent(new AddonComponent(0x15), 1, -1, 0);
                        AddComponent(new AddonComponent(0x15), 1, 0, 0);
                        AddComponent(new AddonComponent(0x14), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3D), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x634, false), 0, -1, 25);
                        AddComponent(new MarketStallComponent(0x635, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x634, true), 0, 0, 25);
                        AddComponent(new MarketStallComponent(0x635, true), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x634, false), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x635, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x634, true), 0, 2, 25);
                        AddComponent(new MarketStallComponent(0x635, true), 1, 2, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 0);

                        break;
                    }
                case 2: // lightwood, south
                    {
                        // side panel (west)
                        AddComponent(new AddonComponent(0x09), -2, -1, 0);
                        AddComponent(new AddonComponent(0xAC), -2, 0, 0);

                        // side panel (east)
                        AddComponent(new AddonComponent(0x09), 1, -1, 0);
                        AddComponent(new AddonComponent(0xAC), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0xBE), -2, 1, 0);
                        AddComponent(new AddonComponent(0xBF), -1, 1, 0);
                        AddComponent(new AddonComponent(0xBF), 0, 1, 0);
                        AddComponent(new AddonComponent(0xBD), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3E), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x616, false), -1, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, true), 0, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, false), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x616, true), 2, 0, 25);
                        AddComponent(new MarketStallComponent(0x615, false), -1, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, true), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x615, true), 2, 1, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 0);

                        break;
                    }
                case 3: // lightwood, east
                    {
                        // side panel (north)
                        AddComponent(new AddonComponent(0x09), -1, -2, 0);
                        AddComponent(new AddonComponent(0xAD), 0, -2, 0);

                        // side panel (south)
                        AddComponent(new AddonComponent(0x09), -1, 1, 0);
                        AddComponent(new AddonComponent(0xAD), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0xBF), 1, -2, 0);
                        AddComponent(new AddonComponent(0xBE), 1, -1, 0);
                        AddComponent(new AddonComponent(0xBE), 1, 0, 0);
                        AddComponent(new AddonComponent(0xBD), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3D), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x634, false), 0, -1, 25);
                        AddComponent(new MarketStallComponent(0x635, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x634, true), 0, 0, 25);
                        AddComponent(new MarketStallComponent(0x635, true), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x634, false), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x635, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x634, true), 0, 2, 25);
                        AddComponent(new MarketStallComponent(0x635, true), 1, 2, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 0);

                        break;
                    }
            }
        }

        public LargeVesperMarketStallAddon(Serial serial)
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

    public class LargeVesperMarketStallDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeVesperMarketStallAddon(m_Type); } }
        public override string DefaultName { get { return "Large Vesper Market Stall"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Darkwood, South",
                "Darkwood, East",
                "Lightwood, South",
                "Lightwood, East",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public LargeVesperMarketStallDeed()
            : base()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public LargeVesperMarketStallDeed(Serial serial)
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

    #endregion

    #region Small Nujel'm

    [TownshipAddon]
    public class SmallNujelmMarketStallAddon : BaseMarketStallAddon
    {
        public override BaseAddonDeed Deed { get { return new SmallNujelmMarketStallDeed(); } }
        public override bool Redeedable { get { return true; } }
        public override bool ShareHue { get { return false; } }

        [Constructable]
        public SmallNujelmMarketStallAddon()
            : this(0)
        {
        }

        [Constructable]
        public SmallNujelmMarketStallAddon(int type)
            : base((type % 2) == 1 ? -1 : 0)
        {
            switch (type)
            {
                case 0: // darkwood, south
                    {
                        // side panel (west)
                        AddComponent(new AddonComponent(0x0B), -1, -1, 0);
                        AddComponent(new AddonComponent(0x09), -1, -1, 1);
                        AddComponent(new AddonComponent(0x09), -1, 0, 0);

                        // side panel (east)
                        AddComponent(new AddonComponent(0x0B), 1, -1, 0);
                        AddComponent(new AddonComponent(0x09), 1, -1, 1);
                        AddComponent(new AddonComponent(0x09), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x15), -1, 1, 0);
                        AddComponent(new AddonComponent(0x16), 0, 1, 0);
                        AddComponent(new AddonComponent(0x14), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3E), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), 0, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, true), 1, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, false), 2, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, false), 0, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, true), 1, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, false), 2, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, false), 0, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, true), 1, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, false), 2, 1, 23);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 1, 2, 0);

                        break;
                    }
                case 1: // darkwood, east
                    {
                        // side panel (north)
                        AddComponent(new AddonComponent(0x0A), -1, -1, 0);
                        AddComponent(new AddonComponent(0x09), -1, -1, 1);
                        AddComponent(new AddonComponent(0x09), 0, -1, 0);

                        // side panel (south)
                        AddComponent(new AddonComponent(0x0A), -1, 1, 0);
                        AddComponent(new AddonComponent(0x09), -1, 1, 1);
                        AddComponent(new AddonComponent(0x09), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x16), 1, -1, 0);
                        AddComponent(new AddonComponent(0x15), 1, 0, 0);
                        AddComponent(new AddonComponent(0x14), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3D), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 0, 17);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 0, 20);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 0, 23);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 1, 17);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 1, 20);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 1, 23);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 2, 17);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 2, 20);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 2, 23);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 1, 0);

                        break;
                    }
                case 2: // lightwood, south
                    {
                        // side panel (west)
                        AddComponent(new AddonComponent(0xAA), -1, -1, 0);
                        AddComponent(new AddonComponent(0x09), -1, -1, 1);
                        AddComponent(new AddonComponent(0x09), -1, 0, 0);

                        // side panel (east)
                        AddComponent(new AddonComponent(0xAA), 1, -1, 0);
                        AddComponent(new AddonComponent(0x09), 1, -1, 1);
                        AddComponent(new AddonComponent(0x09), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0xBE), -1, 1, 0);
                        AddComponent(new AddonComponent(0xBF), 0, 1, 0);
                        AddComponent(new AddonComponent(0xBD), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3E), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), 0, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, true), 1, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, false), 2, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, false), 0, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, true), 1, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, false), 2, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, false), 0, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, true), 1, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, false), 2, 1, 23);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 1, 2, 0);

                        break;
                    }
                case 3: // lightwood, east
                    {
                        // side panel (north)
                        AddComponent(new AddonComponent(0xAB), -1, -1, 0);
                        AddComponent(new AddonComponent(0x09), -1, -1, 1);
                        AddComponent(new AddonComponent(0x09), 0, -1, 0);

                        // side panel (south)
                        AddComponent(new AddonComponent(0xAB), -1, 1, 0);
                        AddComponent(new AddonComponent(0x09), -1, 1, 1);
                        AddComponent(new AddonComponent(0x09), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0xBF), 1, -1, 0);
                        AddComponent(new AddonComponent(0xBE), 1, 0, 0);
                        AddComponent(new AddonComponent(0xBD), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3D), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 0, 17);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 0, 20);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 0, 23);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 1, 17);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 1, 20);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 1, 23);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 2, 17);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 2, 20);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 2, 23);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 1, 0);

                        break;
                    }
            }
        }

        public SmallNujelmMarketStallAddon(Serial serial)
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

    public class SmallNujelmMarketStallDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new SmallNujelmMarketStallAddon(m_Type); } }
        public override string DefaultName { get { return "Small Nujel'm Market Stall"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Darkwood, South",
                "Darkwood, East",
                "Lightwood, South",
                "Lightwood, East",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public SmallNujelmMarketStallDeed()
            : base()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public SmallNujelmMarketStallDeed(Serial serial)
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

    #endregion

    #region Large Nujel'm

    [TownshipAddon]
    public class LargeNujelmMarketStallAddon : BaseMarketStallAddon
    {
        public override BaseAddonDeed Deed { get { return new LargeNujelmMarketStallDeed(); } }
        public override bool Redeedable { get { return true; } }
        public override bool ShareHue { get { return false; } }

        [Constructable]
        public LargeNujelmMarketStallAddon()
            : this(0)
        {
        }

        [Constructable]
        public LargeNujelmMarketStallAddon(int type)
            : base((type % 2) == 1 ? -1 : 0)
        {
            switch (type)
            {
                case 0: // darkwood, south
                    {
                        // side panel (west)
                        AddComponent(new AddonComponent(0x0B), -2, -1, 0);
                        AddComponent(new AddonComponent(0x09), -2, -1, 1);
                        AddComponent(new AddonComponent(0x09), -2, 0, 0);

                        // side panel (east)
                        AddComponent(new AddonComponent(0x0B), 1, -1, 0);
                        AddComponent(new AddonComponent(0x09), 1, -1, 1);
                        AddComponent(new AddonComponent(0x09), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x15), -2, 1, 0);
                        AddComponent(new AddonComponent(0x16), -1, 1, 0);
                        AddComponent(new AddonComponent(0x16), 0, 1, 0);
                        AddComponent(new AddonComponent(0x14), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3E), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 1, 23);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 0);

                        break;
                    }
                case 1: // darkwood, east
                    {
                        // side panel (north)
                        AddComponent(new AddonComponent(0x0A), -1, -2, 0);
                        AddComponent(new AddonComponent(0x09), -1, -2, 1);
                        AddComponent(new AddonComponent(0x09), 0, -2, 0);

                        // side panel (south)
                        AddComponent(new AddonComponent(0x0A), -1, 1, 0);
                        AddComponent(new AddonComponent(0x09), -1, 1, 1);
                        AddComponent(new AddonComponent(0x09), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x16), 1, -2, 0);
                        AddComponent(new AddonComponent(0x15), 1, -1, 0);
                        AddComponent(new AddonComponent(0x15), 1, 0, 0);
                        AddComponent(new AddonComponent(0x14), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3D), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 17);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, -1, 20);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, -1, 23);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 17);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 0, 20);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 0, 23);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 17);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 1, 20);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 1, 23);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 17);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 2, 20);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 2, 23);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 0);

                        break;
                    }
                case 2: // lightwood, south
                    {
                        // side panel (west)
                        AddComponent(new AddonComponent(0xAA), -2, -1, 0);
                        AddComponent(new AddonComponent(0x09), -2, -1, 1);
                        AddComponent(new AddonComponent(0x09), -2, 0, 0);

                        // side panel (east)
                        AddComponent(new AddonComponent(0xAA), 1, -1, 0);
                        AddComponent(new AddonComponent(0x09), 1, -1, 1);
                        AddComponent(new AddonComponent(0x09), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0xBE), -2, 1, 0);
                        AddComponent(new AddonComponent(0xBF), -1, 1, 0);
                        AddComponent(new AddonComponent(0xBF), 0, 1, 0);
                        AddComponent(new AddonComponent(0xBD), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3E), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB3E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 17);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 0, 20);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 1, 23);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 1, 23);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 0);

                        break;
                    }
                case 3: // lightwood, east
                    {
                        // side panel (north)
                        AddComponent(new AddonComponent(0xAB), -1, -2, 0);
                        AddComponent(new AddonComponent(0x09), -1, -2, 1);
                        AddComponent(new AddonComponent(0x09), 0, -2, 0);

                        // side panel (south)
                        AddComponent(new AddonComponent(0xAB), -1, 1, 0);
                        AddComponent(new AddonComponent(0x09), -1, 1, 1);
                        AddComponent(new AddonComponent(0x09), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0xBF), 1, -2, 0);
                        AddComponent(new AddonComponent(0xBE), 1, -1, 0);
                        AddComponent(new AddonComponent(0xBE), 1, 0, 0);
                        AddComponent(new AddonComponent(0xBD), 1, 1, 0);

                        // counter
                        AddComponent(new AddonComponent(0xB3D), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB3D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 17);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, -1, 20);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, -1, 23);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 17);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 0, 20);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 0, 23);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 17);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 1, 20);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 1, 23);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 17);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 2, 20);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 2, 23);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 0);

                        break;
                    }
            }
        }

        public LargeNujelmMarketStallAddon(Serial serial)
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

    public class LargeNujelmMarketStallDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeNujelmMarketStallAddon(m_Type); } }
        public override string DefaultName { get { return "Large Nujel'm Market Stall"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Darkwood, South",
                "Darkwood, East",
                "Lightwood, South",
                "Lightwood, East",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public LargeNujelmMarketStallDeed()
            : base()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public LargeNujelmMarketStallDeed(Serial serial)
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

    #endregion

    #region Small Britain

    [TownshipAddon]
    public class SmallBritainMarketStallAddon : BaseMarketStallAddon
    {
        public override BaseAddonDeed Deed { get { return new SmallBritainMarketStallDeed(); } }
        public override bool Redeedable { get { return true; } }
        public override bool ShareHue { get { return false; } }

        [Constructable]
        public SmallBritainMarketStallAddon()
            : this(0)
        {
        }

        [Constructable]
        public SmallBritainMarketStallAddon(int type)
            : base((type % 2) == 1 ? -1 : 0)
        {
            switch (type)
            {
                case 0: // fieldstone, south
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x1D), -2, -2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, -2, 0);
                        AddComponent(new AddonComponent(0x29), 1, -2, 0);
                        AddComponent(new AddonComponent(0x2B), -2, -1, 0);
                        AddComponent(new AddonComponent(0x2B), 1, -1, 0);
                        AddComponent(new AddonComponent(0x2A), -2, 0, 0);
                        AddComponent(new AddonComponent(0x2C), -1, 0, 0);
                        AddComponent(new AddonComponent(0x28), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x2E), -2, 1, 0);
                        AddComponent(new AddonComponent(0x2F), -1, 1, 0);
                        AddComponent(new AddonComponent(0x2F), 0, 1, 0);
                        AddComponent(new AddonComponent(0x2D), 1, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0D), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 25);
                        AddComponent(new MarketStallComponent(0x603, false), -1, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, true), 0, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, false), 1, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, true), 2, 0, 28);
                        AddComponent(new MarketStallComponent(0x619, false), -1, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, true), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, true), 2, 1, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 0);

                        // curtain
                        AddComponent(new MarketStallComponent(0x3D9E, false), 2, -1, 0);

                        break;
                    }
                case 1: // fieldstone, east
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x1D), -2, -2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, -2, 0);
                        AddComponent(new AddonComponent(0x29), 0, -2, 0);
                        AddComponent(new AddonComponent(0x2B), -2, -1, 0);
                        AddComponent(new AddonComponent(0x2B), 0, -1, 0);
                        AddComponent(new AddonComponent(0x2A), -2, 1, 0);
                        AddComponent(new AddonComponent(0x2C), -1, 1, 0);
                        AddComponent(new AddonComponent(0x28), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x2F), 1, -2, 0);
                        AddComponent(new AddonComponent(0x2E), 1, -1, 0);
                        AddComponent(new AddonComponent(0x2E), 1, 0, 0);
                        AddComponent(new AddonComponent(0x2D), 1, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0E), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, -1, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 24);
                        AddComponent(new MarketStallComponent(0x64C, true), 0, 0, 28);
                        AddComponent(new MarketStallComponent(0x602, true), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, 1, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 24);
                        AddComponent(new MarketStallComponent(0x64C, true), 0, 2, 28);
                        AddComponent(new MarketStallComponent(0x602, true), 1, 2, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 0);

                        // curtain
                        AddComponent(new MarketStallComponent(0x12E7, false), 0, 2, 0);

                        break;
                    }
                case 2: // brick, south
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x36), -2, -2, 0);
                        AddComponent(new AddonComponent(0x47), -1, -2, 0);
                        AddComponent(new AddonComponent(0x48), 1, -2, 0);
                        AddComponent(new AddonComponent(0x49), -2, -1, 0);
                        AddComponent(new AddonComponent(0x49), 1, -1, 0);
                        AddComponent(new AddonComponent(0x46), -2, 0, 0);
                        AddComponent(new AddonComponent(0x47), -1, 0, 0);
                        AddComponent(new AddonComponent(0x45), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x56), -2, 1, 0);
                        AddComponent(new AddonComponent(0x55), -1, 1, 0);
                        AddComponent(new AddonComponent(0x55), 0, 1, 0);
                        AddComponent(new AddonComponent(0x55), 1, 1, 0);
                        AddComponent(new AddonComponent(0x56), 1, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0D), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 25);
                        AddComponent(new MarketStallComponent(0x603, false), -1, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, true), 0, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, false), 1, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, true), 2, 0, 28);
                        AddComponent(new MarketStallComponent(0x619, false), -1, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, true), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, true), 2, 1, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 0);

                        // curtain
                        AddComponent(new MarketStallComponent(0x3D9E, false), 2, -1, 0);

                        break;
                    }
                case 3: // brick, east
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x36), -2, -2, 0);
                        AddComponent(new AddonComponent(0x47), -1, -2, 0);
                        AddComponent(new AddonComponent(0x48), 0, -2, 0);
                        AddComponent(new AddonComponent(0x49), -2, -1, 0);
                        AddComponent(new AddonComponent(0x49), 0, -1, 0);
                        AddComponent(new AddonComponent(0x46), -2, 1, 0);
                        AddComponent(new AddonComponent(0x47), -1, 1, 0);
                        AddComponent(new AddonComponent(0x45), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x55), 1, -2, 0);
                        AddComponent(new AddonComponent(0x56), 1, -1, 0);
                        AddComponent(new AddonComponent(0x56), 1, 0, 0);
                        AddComponent(new AddonComponent(0x56), 1, 1, 0);
                        AddComponent(new AddonComponent(0x55), 1, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0E), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, -1, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 24);
                        AddComponent(new MarketStallComponent(0x64C, true), 0, 0, 28);
                        AddComponent(new MarketStallComponent(0x602, true), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, 1, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 24);
                        AddComponent(new MarketStallComponent(0x64C, true), 0, 2, 28);
                        AddComponent(new MarketStallComponent(0x602, true), 1, 2, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 0);

                        // curtain
                        AddComponent(new MarketStallComponent(0x12E7, false), 0, 2, 0);

                        break;
                    }
            }
        }

        public SmallBritainMarketStallAddon(Serial serial)
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

    public class SmallBritainMarketStallDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new SmallBritainMarketStallAddon(m_Type); } }
        public override string DefaultName { get { return "Small Britain Market Stall"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Fieldstone, South",
                "Fieldstone, East",
                "Brick, South",
                "Brick, East",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public SmallBritainMarketStallDeed()
            : base()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public SmallBritainMarketStallDeed(Serial serial)
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

    #endregion

    #region Large Britain

    [TownshipAddon]
    public class LargeBritainMarketStallAddon : BaseMarketStallAddon
    {
        public override BaseAddonDeed Deed { get { return new LargeBritainMarketStallDeed(); } }
        public override bool Redeedable { get { return true; } }
        public override bool ShareHue { get { return false; } }

        [Constructable]
        public LargeBritainMarketStallAddon()
            : this(0)
        {
        }

        [Constructable]
        public LargeBritainMarketStallAddon(int type)
            : base((type % 2) == 1 ? -1 : 0)
        {
            switch (type)
            {
                case 0: // fieldstone, south
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x1D), -2, -2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, -2, 0);
                        AddComponent(new AddonComponent(0x29), 2, -2, 0);
                        AddComponent(new AddonComponent(0x2B), -2, -1, 0);
                        AddComponent(new AddonComponent(0x2B), 2, -1, 0);
                        AddComponent(new AddonComponent(0x2A), -2, 0, 0);
                        AddComponent(new AddonComponent(0x2C), -1, 0, 0);
                        AddComponent(new AddonComponent(0x28), 2, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x2E), -2, 1, 0);
                        AddComponent(new AddonComponent(0x2F), -1, 1, 0);
                        AddComponent(new AddonComponent(0x2F), 0, 1, 0);
                        AddComponent(new AddonComponent(0x2F), 1, 1, 0);
                        AddComponent(new AddonComponent(0x2D), 2, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0D), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 2, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, false), 3, -1, 25);
                        AddComponent(new MarketStallComponent(0x603, true), -1, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, false), 0, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, true), 1, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, false), 2, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, true), 3, 0, 28);
                        AddComponent(new MarketStallComponent(0x619, false), -1, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, true), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, true), 2, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, false), 3, 1, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 2, 2, 0);

                        // curtain
                        AddComponent(new MarketStallComponent(0x3D9E, true), 3, -1, 0);

                        break;
                    }
                case 1: // fieldstone, east
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x1D), -2, -2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, -2, 0);
                        AddComponent(new AddonComponent(0x29), 0, -2, 0);
                        AddComponent(new AddonComponent(0x2B), -2, -1, 0);
                        AddComponent(new AddonComponent(0x2B), 0, -1, 0);
                        AddComponent(new AddonComponent(0x2A), -2, 2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, 2, 0);
                        AddComponent(new AddonComponent(0x28), 0, 2, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x2F), 1, -2, 0);
                        AddComponent(new AddonComponent(0x2E), 1, -1, 0);
                        AddComponent(new AddonComponent(0x2E), 1, 0, 0);
                        AddComponent(new AddonComponent(0x2E), 1, 1, 0);
                        AddComponent(new AddonComponent(0x2D), 1, 2, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0E), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 2, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, -1, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 24);
                        AddComponent(new MarketStallComponent(0x64C, true), 0, 0, 28);
                        AddComponent(new MarketStallComponent(0x602, true), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, 1, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 24);
                        AddComponent(new MarketStallComponent(0x64C, true), 0, 2, 28);
                        AddComponent(new MarketStallComponent(0x602, true), 1, 2, 25);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 3, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, 3, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, 3, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 2, 0);

                        // curtain
                        AddComponent(new MarketStallComponent(0x12E7, true), 0, 3, 0);

                        break;
                    }
                case 2: // brick, south
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x36), -2, -2, 0);
                        AddComponent(new AddonComponent(0x47), -1, -2, 0);
                        AddComponent(new AddonComponent(0x48), 2, -2, 0);
                        AddComponent(new AddonComponent(0x49), -2, -1, 0);
                        AddComponent(new AddonComponent(0x49), 2, -1, 0);
                        AddComponent(new AddonComponent(0x46), -2, 0, 0);
                        AddComponent(new AddonComponent(0x47), -1, 0, 0);
                        AddComponent(new AddonComponent(0x45), 2, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x56), -2, 1, 0);
                        AddComponent(new AddonComponent(0x55), -1, 1, 0);
                        AddComponent(new AddonComponent(0x55), 0, 1, 0);
                        AddComponent(new AddonComponent(0x55), 1, 1, 0);
                        AddComponent(new AddonComponent(0x55), 2, 1, 0);
                        AddComponent(new AddonComponent(0x56), 2, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0D), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 2, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 25);
                        AddComponent(new MarketStallComponent(0x600, false), 3, -1, 25);
                        AddComponent(new MarketStallComponent(0x603, true), -1, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, false), 0, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, true), 1, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, false), 2, 0, 28);
                        AddComponent(new MarketStallComponent(0x603, true), 3, 0, 28);
                        AddComponent(new MarketStallComponent(0x619, false), -1, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, true), 0, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, true), 2, 1, 25);
                        AddComponent(new MarketStallComponent(0x619, false), 3, 1, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 0);
                        AddComponent(new MarketStallComponent(0x378, false), 2, 2, 0);

                        // curtain
                        AddComponent(new MarketStallComponent(0x3D9E, true), 3, -1, 0);

                        break;
                    }
                case 3: // brick, east
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x36), -2, -2, 0);
                        AddComponent(new AddonComponent(0x47), -1, -2, 0);
                        AddComponent(new AddonComponent(0x48), 0, -2, 0);
                        AddComponent(new AddonComponent(0x49), -2, -1, 0);
                        AddComponent(new AddonComponent(0x49), 0, -1, 0);
                        AddComponent(new AddonComponent(0x46), -2, 2, 0);
                        AddComponent(new AddonComponent(0x47), -1, 2, 0);
                        AddComponent(new AddonComponent(0x45), 0, 2, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x55), 1, -2, 0);
                        AddComponent(new AddonComponent(0x56), 1, -1, 0);
                        AddComponent(new AddonComponent(0x56), 1, 0, 0);
                        AddComponent(new AddonComponent(0x56), 1, 1, 0);
                        AddComponent(new AddonComponent(0x56), 1, 2, 0);
                        AddComponent(new AddonComponent(0x55), 1, 2, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0E), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 2, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, -1, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, -1, 25);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 24);
                        AddComponent(new MarketStallComponent(0x64C, true), 0, 0, 28);
                        AddComponent(new MarketStallComponent(0x602, true), 1, 0, 25);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, 1, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, 1, 25);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 24);
                        AddComponent(new MarketStallComponent(0x64C, true), 0, 2, 28);
                        AddComponent(new MarketStallComponent(0x602, true), 1, 2, 25);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 3, 24);
                        AddComponent(new MarketStallComponent(0x64C, false), 0, 3, 28);
                        AddComponent(new MarketStallComponent(0x602, false), 1, 3, 25);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 0);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 0);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 2, 0);

                        // curtain
                        AddComponent(new MarketStallComponent(0x12E7, true), 0, 3, 0);

                        break;
                    }
            }
        }

        public LargeBritainMarketStallAddon(Serial serial)
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

    public class LargeBritainMarketStallDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeBritainMarketStallAddon(m_Type); } }
        public override string DefaultName { get { return "Large Britain Market Stall"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Fieldstone, South",
                "Fieldstone, East",
                "Brick, South",
                "Brick, East",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public LargeBritainMarketStallDeed()
            : base()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public LargeBritainMarketStallDeed(Serial serial)
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

    #endregion

    #region Small Minoc

    [TownshipAddon]
    public class SmallMinocMarketStallAddon : BaseMarketStallAddon
    {
        public override BaseAddonDeed Deed { get { return new SmallMinocMarketStallDeed(); } }
        public override bool Redeedable { get { return true; } }
        public override bool ShareHue { get { return false; } }

        [Constructable]
        public SmallMinocMarketStallAddon()
            : this(0)
        {
        }

        [Constructable]
        public SmallMinocMarketStallAddon(int type)
            : base((type % 2) == 1 ? -1 : 0)
        {
            switch (type)
            {
                case 0: // fieldstone, south
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x1D), -2, -2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, -2, 0);
                        AddComponent(new AddonComponent(0x29), 1, -2, 0);
                        AddComponent(new AddonComponent(0x2B), -2, -1, 0);
                        AddComponent(new AddonComponent(0x2B), 1, -1, 0);
                        AddComponent(new AddonComponent(0x2A), -2, 0, 0);
                        AddComponent(new AddonComponent(0x2C), -1, 0, 0);
                        AddComponent(new AddonComponent(0x28), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x2E), -2, 1, 0);
                        AddComponent(new AddonComponent(0x2F), -1, 1, 0);
                        AddComponent(new AddonComponent(0x2F), 0, 1, 0);
                        AddComponent(new AddonComponent(0x2D), 1, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0D), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 1, 24);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 1);

                        // curtain
                        AddComponent(new MarketStallComponent(0x3D9E, false), 2, -1, 0);

                        break;
                    }
                case 1: // fieldstone, east
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x1D), -2, -2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, -2, 0);
                        AddComponent(new AddonComponent(0x29), 0, -2, 0);
                        AddComponent(new AddonComponent(0x2B), -2, -1, 0);
                        AddComponent(new AddonComponent(0x2B), 0, -1, 0);
                        AddComponent(new AddonComponent(0x2A), -2, 1, 0);
                        AddComponent(new AddonComponent(0x2C), -1, 1, 0);
                        AddComponent(new AddonComponent(0x28), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x2F), 1, -2, 0);
                        AddComponent(new AddonComponent(0x2E), 1, -1, 0);
                        AddComponent(new AddonComponent(0x2E), 1, 0, 0);
                        AddComponent(new AddonComponent(0x2D), 1, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0E), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, -1, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, -1, 24);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 18);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 0, 21);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 0, 24);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 1, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 1, 24);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 18);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 2, 21);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 2, 24);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 1);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 1);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 1);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 1);

                        // curtain
                        AddComponent(new MarketStallComponent(0x12E7, false), 0, 2, 0);

                        break;
                    }
                case 2: // brick, south
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x36), -2, -2, 0);
                        AddComponent(new AddonComponent(0x47), -1, -2, 0);
                        AddComponent(new AddonComponent(0x48), 1, -2, 0);
                        AddComponent(new AddonComponent(0x49), -2, -1, 0);
                        AddComponent(new AddonComponent(0x49), 1, -1, 0);
                        AddComponent(new AddonComponent(0x46), -2, 0, 0);
                        AddComponent(new AddonComponent(0x47), -1, 0, 0);
                        AddComponent(new AddonComponent(0x45), 1, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x56), -2, 1, 0);
                        AddComponent(new AddonComponent(0x55), -1, 1, 0);
                        AddComponent(new AddonComponent(0x55), 0, 1, 0);
                        AddComponent(new AddonComponent(0x55), 1, 1, 0);
                        AddComponent(new AddonComponent(0x56), 1, 1, 0);

                        // dislay case
                        AddComponent(new AddonComponent(0xB0D), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 1, 24);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 1);

                        // curtain
                        AddComponent(new MarketStallComponent(0x3D9E, false), 2, -1, 0);

                        break;
                    }
                case 3: // brick, east
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x36), -2, -2, 0);
                        AddComponent(new AddonComponent(0x47), -1, -2, 0);
                        AddComponent(new AddonComponent(0x48), 0, -2, 0);
                        AddComponent(new AddonComponent(0x49), -2, -1, 0);
                        AddComponent(new AddonComponent(0x49), 0, -1, 0);
                        AddComponent(new AddonComponent(0x46), -2, 1, 0);
                        AddComponent(new AddonComponent(0x47), -1, 1, 0);
                        AddComponent(new AddonComponent(0x45), 0, 1, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x55), 1, -2, 0);
                        AddComponent(new AddonComponent(0x56), 1, -1, 0);
                        AddComponent(new AddonComponent(0x56), 1, 0, 0);
                        AddComponent(new AddonComponent(0x56), 1, 1, 0);
                        AddComponent(new AddonComponent(0x55), 1, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0E), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, -1, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, -1, 24);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 18);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 0, 21);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 0, 24);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 1, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 1, 24);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 18);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 2, 21);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 2, 24);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 1);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 1);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 1);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 1);

                        // curtain
                        AddComponent(new MarketStallComponent(0x12E7, false), 0, 2, 0);

                        break;
                    }
            }
        }

        public SmallMinocMarketStallAddon(Serial serial)
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

    public class SmallMinocMarketStallDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new SmallMinocMarketStallAddon(m_Type); } }
        public override string DefaultName { get { return "Small Minoc Market Stall"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Fieldstone, South",
                "Fieldstone, East",
                "Brick, South",
                "Brick, East",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public SmallMinocMarketStallDeed()
            : base()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public SmallMinocMarketStallDeed(Serial serial)
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

    #endregion

    #region Large Minoc

    [TownshipAddon]
    public class LargeMinocMarketStallAddon : BaseMarketStallAddon
    {
        public override BaseAddonDeed Deed { get { return new LargeMinocMarketStallDeed(); } }
        public override bool Redeedable { get { return true; } }
        public override bool ShareHue { get { return false; } }

        [Constructable]
        public LargeMinocMarketStallAddon()
            : this(0)
        {
        }

        [Constructable]
        public LargeMinocMarketStallAddon(int type)
            : base((type % 2) == 1 ? -1 : 0)
        {
            switch (type)
            {
                case 0: // fieldstone, south
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x1D), -2, -2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, -2, 0);
                        AddComponent(new AddonComponent(0x29), 2, -2, 0);
                        AddComponent(new AddonComponent(0x2B), -2, -1, 0);
                        AddComponent(new AddonComponent(0x2B), 2, -1, 0);
                        AddComponent(new AddonComponent(0x2A), -2, 0, 0);
                        AddComponent(new AddonComponent(0x2C), -1, 0, 0);
                        AddComponent(new AddonComponent(0x28), 2, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x2E), -2, 1, 0);
                        AddComponent(new AddonComponent(0x2F), -1, 1, 0);
                        AddComponent(new AddonComponent(0x2F), 0, 1, 0);
                        AddComponent(new AddonComponent(0x2F), 1, 1, 0);
                        AddComponent(new AddonComponent(0x2D), 2, 1, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0D), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 2, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, false), 3, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), -1, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), 0, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, true), 1, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), 2, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, true), 3, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, false), 3, 1, 24);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, false), 2, 2, 1);

                        // curtain
                        AddComponent(new MarketStallComponent(0x3D9E, true), 3, -1, 0);

                        break;
                    }
                case 1: // fieldstone, east
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x1D), -2, -2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, -2, 0);
                        AddComponent(new AddonComponent(0x29), 0, -2, 0);
                        AddComponent(new AddonComponent(0x2B), -2, -1, 0);
                        AddComponent(new AddonComponent(0x2B), 0, -1, 0);
                        AddComponent(new AddonComponent(0x2A), -2, 2, 0);
                        AddComponent(new AddonComponent(0x2C), -1, 2, 0);
                        AddComponent(new AddonComponent(0x28), 0, 2, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x2F), 1, -2, 0);
                        AddComponent(new AddonComponent(0x2E), 1, -1, 0);
                        AddComponent(new AddonComponent(0x2E), 1, 0, 0);
                        AddComponent(new AddonComponent(0x2E), 1, 1, 0);
                        AddComponent(new AddonComponent(0x2D), 1, 2, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0E), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 2, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, -1, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, -1, 24);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 18);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 0, 21);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 0, 24);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 1, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 1, 24);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 18);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 2, 21);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 2, 24);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 3, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 3, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 3, 24);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 1);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 1);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 1);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 1);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 2, 1);

                        // curtain
                        AddComponent(new MarketStallComponent(0x12E7, false), 0, 3, 0);

                        break;
                    }
                case 2: // brick, south
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x36), -2, -2, 0);
                        AddComponent(new AddonComponent(0x47), -1, -2, 0);
                        AddComponent(new AddonComponent(0x48), 2, -2, 0);
                        AddComponent(new AddonComponent(0x49), -2, -1, 0);
                        AddComponent(new AddonComponent(0x49), 2, -1, 0);
                        AddComponent(new AddonComponent(0x46), -2, 0, 0);
                        AddComponent(new AddonComponent(0x47), -1, 0, 0);
                        AddComponent(new AddonComponent(0x45), 2, 0, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x56), -2, 1, 0);
                        AddComponent(new AddonComponent(0x55), -1, 1, 0);
                        AddComponent(new AddonComponent(0x55), 0, 1, 0);
                        AddComponent(new AddonComponent(0x55), 1, 1, 0);
                        AddComponent(new AddonComponent(0x55), 2, 1, 0);
                        AddComponent(new AddonComponent(0x56), 2, 1, 0);

                        // dislay case
                        AddComponent(new AddonComponent(0xB0D), -1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 0, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 1, 1, -1);
                        AddComponent(new AddonComponent(0xB0D), 2, 1, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x600, false), -1, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), 0, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, false), 1, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), 2, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, false), 3, -1, 18);
                        AddComponent(new MarketStallComponent(0x600, true), -1, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), 0, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, true), 1, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), 2, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, true), 3, 0, 21);
                        AddComponent(new MarketStallComponent(0x600, false), -1, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, true), 0, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, false), 1, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, true), 2, 1, 24);
                        AddComponent(new MarketStallComponent(0x600, false), 3, 1, 24);

                        // dagges
                        AddComponent(new MarketStallComponent(0x378, false), -2, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, true), -1, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, false), 0, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, true), 1, 2, 1);
                        AddComponent(new MarketStallComponent(0x378, false), 2, 2, 1);

                        // curtain
                        AddComponent(new MarketStallComponent(0x3D9E, true), 3, -1, 0);

                        break;
                    }
                case 3: // brick, east
                    {
                        // main frame
                        AddComponent(new AddonComponent(0x36), -2, -2, 0);
                        AddComponent(new AddonComponent(0x47), -1, -2, 0);
                        AddComponent(new AddonComponent(0x48), 0, -2, 0);
                        AddComponent(new AddonComponent(0x49), -2, -1, 0);
                        AddComponent(new AddonComponent(0x49), 0, -1, 0);
                        AddComponent(new AddonComponent(0x46), -2, 2, 0);
                        AddComponent(new AddonComponent(0x47), -1, 2, 0);
                        AddComponent(new AddonComponent(0x45), 0, 2, 0);

                        // display frame
                        AddComponent(new AddonComponent(0x55), 1, -2, 0);
                        AddComponent(new AddonComponent(0x56), 1, -1, 0);
                        AddComponent(new AddonComponent(0x56), 1, 0, 0);
                        AddComponent(new AddonComponent(0x56), 1, 1, 0);
                        AddComponent(new AddonComponent(0x56), 1, 2, 0);
                        AddComponent(new AddonComponent(0x55), 1, 2, 0);

                        // display case
                        AddComponent(new AddonComponent(0xB0E), 1, -1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 0, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 1, -1);
                        AddComponent(new AddonComponent(0xB0E), 1, 2, -1);

                        // roof
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, -1, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, -1, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, -1, 24);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 0, 18);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 0, 21);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 0, 24);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 1, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 1, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 1, 24);
                        AddComponent(new MarketStallComponent(0x5FF, true), -1, 2, 18);
                        AddComponent(new MarketStallComponent(0x5FF, true), 0, 2, 21);
                        AddComponent(new MarketStallComponent(0x5FF, true), 1, 2, 24);
                        AddComponent(new MarketStallComponent(0x5FF, false), -1, 3, 18);
                        AddComponent(new MarketStallComponent(0x5FF, false), 0, 3, 21);
                        AddComponent(new MarketStallComponent(0x5FF, false), 1, 3, 24);

                        // dagges
                        AddComponent(new MarketStallComponent(0x379, false), 2, -2, 1);
                        AddComponent(new MarketStallComponent(0x379, true), 2, -1, 1);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 0, 1);
                        AddComponent(new MarketStallComponent(0x379, true), 2, 1, 1);
                        AddComponent(new MarketStallComponent(0x379, false), 2, 2, 1);

                        // curtain
                        AddComponent(new MarketStallComponent(0x12E7, true), 0, 3, 0);

                        break;
                    }
            }
        }

        public LargeMinocMarketStallAddon(Serial serial)
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

    public class LargeMinocMarketStallDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new LargeMinocMarketStallAddon(m_Type); } }
        public override string DefaultName { get { return "Large Minoc Market Stall"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Fieldstone, South",
                "Fieldstone, East",
                "Brick, South",
                "Brick, East",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public LargeMinocMarketStallDeed()
            : base()
        {
        }

        protected override void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
            base.AddBuildFlags(ref buildFlags);

            buildFlags |= TownshipBuilder.BuildFlag.NeedsGround;
        }

        public LargeMinocMarketStallDeed(Serial serial)
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

    #endregion

    public abstract class BaseMarketStallAddon : BaseAddon
    {
        private static readonly Point3D[] m_DefaultOffsets = new Point3D[]
            {
                new Point3D(0, 0, 0),
            };

        public virtual Point3D[] VendorOffsets { get { return m_DefaultOffsets; } }

        private List<Item> m_Contracts;
        private List<Mobile> m_Vendors;

        public List<Item> Contracts
        {
            get { Defrag(m_Contracts); return m_Contracts; }
        }

        public List<Mobile> Vendors
        {
            get { Defrag(m_Vendors); return m_Vendors; }
        }

        public BaseMarketStallAddon(int rotation)
        {
            m_Contracts = new List<Item>();
            m_Vendors = new List<Mobile>();

            Point3D[] offsets = VendorOffsets;

            for (int i = 0; i < offsets.Length; i++)
            {
                Point3D offset = offsets[i];

                MarketStallContract contract = new MarketStallContract(this);

                contract.MoveToWorld(Location + Rotate(offset, rotation), Map);

                m_Contracts.Add(contract);
            }
        }

        private static Point3D Rotate(Point3D offset, int rotation)
        {
            int x = offset.X;
            int y = offset.Y;

            for (int i = 0; i < Modulo(rotation, 4); i++)
            {
                int temp = x;
                x = -y;
                y = temp;
            }

            return new Point3D(x, y, offset.Z);
        }

        private static int Modulo(int v, int k)
        {
            return ((v % k) + k) % k;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            Point3D offset = Location - oldLocation;

            foreach (Item contract in Contracts)
                contract.Location += offset;

            foreach (Mobile vendor in Vendors)
                vendor.Location += offset;
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            foreach (Item contract in Contracts)
                contract.Map = Map;

            foreach (Mobile vendor in Vendors)
                vendor.Map = Map;
        }

        // TODO: On chop, show confirmation gump to confirm the deletion of all player vendors

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            for (int i = m_Contracts.Count - 1; i >= 0; i--)
            {
                if (i >= m_Contracts.Count)
                    continue;

                m_Contracts[i].Delete();
            }

            m_Contracts.Clear();

            // TODO: Delete player vendors, drop items
        }

        private static void Defrag<T>(List<T> list) where T : IEntity
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count)
                    continue;

                if (list[i].Deleted)
                    list.RemoveAt(i);
            }
        }

        public BaseMarketStallAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteItemList(m_Contracts);
            writer.WriteMobileList(m_Vendors);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Contracts = reader.ReadStrongItemList();
                        m_Vendors = reader.ReadStrongMobileList();

                        break;
                    }
            }
        }
    }

    public class MarketStallComponent : AddonComponent, IDyable
    {
        private bool m_Secondary;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Secondary
        {
            get { return m_Secondary; }
            set { m_Secondary = value; }
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get { return base.Hue; }
            set
            {
                if (Hue != value)
                {
                    base.Hue = value;

                    if (Addon != null)
                        SetMarketStallHue(Addon, m_Secondary, value);
                }
            }
        }

        public static void SetMarketStallHue(BaseAddon addon, bool secondary, int hue)
        {
            foreach (AddonComponent ac in addon.Components)
            {
                if (ac is MarketStallComponent && ((MarketStallComponent)ac).Secondary == secondary)
                    ac.Hue = hue;
            }
        }

        [Constructable]
        public MarketStallComponent(int itemID, bool secondary)
            : base(itemID)
        {
            m_Secondary = secondary;
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            Hue = sender.DyedHue;

            return true;
        }

        public MarketStallComponent(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write((bool)m_Secondary);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            m_Secondary = reader.ReadBool();
        }
    }
}

namespace Server.Mobiles
{
    public class MarketStallVendor : PlayerVendor
    {
        private BaseMarketStallAddon m_MarketStall;

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseMarketStallAddon MarketStall
        {
            get { return m_MarketStall; }
            set { m_MarketStall = value; }
        }

        public MarketStallVendor(Mobile owner, BaseMarketStallAddon marketStall)
            : base(owner, null)
        {
            m_MarketStall = marketStall;
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if ((m_MarketStall == null || m_MarketStall.Deleted) && IsOwner(from))
            {
                SayTo(from, "Excuse me, without my market stall I can no longer continue operations.");
                return false;
            }

            return base.OnDragDrop(from, item);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_MarketStall != null && !m_MarketStall.Deleted)
            {
                MarketStallContract contract = new MarketStallContract(m_MarketStall);

                contract.MoveToWorld(Location, Map);

                m_MarketStall.Contracts.Add(contract);
            }
        }

        public MarketStallVendor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((Item)m_MarketStall);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_MarketStall = reader.ReadItem() as BaseMarketStallAddon;

                        break;
                    }
            }
        }
    }
}

namespace Server.Items
{
    public class MarketStallContract : ContractOfEmployment
    {
        public override string DefaultName { get { return "Market Stall Contract"; } }

        private BaseMarketStallAddon m_MarketStall;

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseMarketStallAddon MarketStall
        {
            get { return m_MarketStall; }
            set { m_MarketStall = value; }
        }

        [Constructable]
        public MarketStallContract()
            : this(null)
        {
        }

        public MarketStallContract(BaseMarketStallAddon marketStall)
            : base()
        {
            Movable = false;

            m_MarketStall = marketStall;
        }

        public override void OnDoubleClick(Mobile from)
        {
            UseDeed(from, false);
        }

        private void UseDeed(Mobile from, bool confirm)
        {
            if (Deleted || Movable || Parent != null || m_MarketStall == null || m_MarketStall.Deleted)
                return;

            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (!confirm)
            {
                from.CloseGump(typeof(ConfirmClaimGump));
                from.SendGump(new ConfirmClaimGump(this));
            }
            else
            {
                Mobile v = new MarketStallVendor(from, m_MarketStall);
                v.Direction = from.Direction & Direction.Mask;
                v.MoveToWorld(Location, Map);

                v.SayTo(from, 503246); // Ah! it feels good to be working again.

                m_MarketStall.Vendors.Add(v);

                Delete();
            }
        }

        public MarketStallContract(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((Item)m_MarketStall);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_MarketStall = reader.ReadItem() as BaseMarketStallAddon;

                        break;
                    }
            }
        }

        public class ConfirmClaimGump : Gump
        {
            private MarketStallContract m_Contract;

            public ConfirmClaimGump(MarketStallContract contract)
                : base(50, 50)
            {
                m_Contract = contract;

                AddPage(0);

                AddBackground(10, 10, 190, 140, 0x242C);

                AddHtml(25, 30, 160, 40, "<center>Do you wish to place a vendor here?</center>", false, false);

                AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (info.ButtonID == 1)
                    m_Contract.UseDeed(state.Mobile, true);
            }
        }
    }
}