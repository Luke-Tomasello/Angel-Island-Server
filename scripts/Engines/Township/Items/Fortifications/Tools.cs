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

/* Engines/Township/Items/Fortifications/Tools.cs
 * CHANGELOG:
 * 11/23/21, Yoar
 *	    Disabled tool functionality:
 *	    - Township fortifications are now constructed using TownshipBuilderTool.
 *	    - Fortifications are now destroyed using regular weapons.
 * 11/20/21, Yoar
 *	    Refactored township walls/tools.
 * 5/11/10, Pix
 *      Changed the check for wall placement only to count houses that are actually in the township.  Now
 *      if there's a house right up to the township border, you can place the wall.  Note that the 1-tile
 *      restriction around the house does still apply.
 * 4/23/10, adam
 *		Add a DemolitionAx and Sledgehammer for knocking down walls
 *		(currently not craftable, and sold on carpenters.)
 *		Consideration: cost and durability should be factored into the ease-of-wall-destruction equation.
 * 11/16/08, Pix
 *		Refactored, rebalanced, and fixed stuff
 *	10/19/08, Pix
 *		Reduced logs for spear wall from 200 to 150, so people can carry stuff.
 *		Changed message to say walls need iron ingots (as opposed to just ingots).
 * 10/15/08, Pix.
 *		Added 100% home ownership check, under protest.
 * 10/14/08, Pix.
 *		Some code cleanup/consolidation.
 *		Change restrictions on wall placement - not within 1 of a township-owned house, not within 5 of a non-township-owned house.
 *		Fixed ingotsRequired >= comparison!
 * 10/10/08, Pix
 *		Initial.
*/

using Server.Items;
using System;

namespace Server.Township
{
    [FlipableAttribute(0x1439, 0x1438)]
    [Obsolete]
    public class Sledgehammer : WarHammer
    {
        public override string OldName { get { return "sledgehammer"; } }
        public override Article OldArticle { get { return Article.A; } }

        [Constructable]
        public Sledgehammer()
            : base()
        {
            Weight = 10.0;
            Layer = Layer.TwoHanded;
            Name = "sledgehammer";
        }

        #region Serialization

        public Sledgehammer(Serial serial)
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

        #endregion
    }

    [FlipableAttribute(0x13FB, 0x13FA)]
    [Obsolete]
    public class DemolitionAx : LargeBattleAxe
    {
        public override string OldName { get { return "demolition ax"; } }
        public override Article OldArticle { get { return Article.A; } }

        [Constructable]
        public DemolitionAx()
            : base()
        {
            Weight = 6.0;
            Name = "demolition ax";
        }

        #region Serialization

        public DemolitionAx(Serial serial)
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

        #endregion
    }

    [Obsolete]
    public class StoneWallCreationTool : Item
    {
        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        [Constructable]
        public StoneWallCreationTool()
            : base(0x13E3)
        {
            Name = "Township Stone Wall Tool";
            m_UsesRemaining = 10;
            Hue = 819;
            Weight = 10;
        }

        #region Serialization

        public StoneWallCreationTool(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_UsesRemaining = reader.ReadInt();

            Weight = 10;
        }

        #endregion
    }

    [Obsolete]
    public class WoodenWallCreationTool : Item
    {
        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        [Constructable]
        public WoodenWallCreationTool()
            : base(0x1031)
        {
            Name = "Township Wooden Wall Tool";
            m_UsesRemaining = 10;
            Hue = 802;
            Weight = 10;
        }

        #region Serialization

        public WoodenWallCreationTool(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_UsesRemaining = reader.ReadInt();

            Weight = 10;
        }

        #endregion
    }

    [Obsolete]
    public class WallCustomizationTool : Item
    {
        [Constructable]
        public WallCustomizationTool()
            : base(0xFC1)
        {
            Name = "Township Wall Customization Tool";
            Hue = 803;
            Weight = 10;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!this.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                TownshipBuilderTool tool = new TownshipBuilderTool();

                tool.Hue = 803;

                ReplaceWith(tool);

                from.SendMessage("The item was converted into a township builder tool.");
            }
        }

        #region Serialization

        public WallCustomizationTool(Serial serial)
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

            Weight = 10;
        }

        #endregion
    }
}