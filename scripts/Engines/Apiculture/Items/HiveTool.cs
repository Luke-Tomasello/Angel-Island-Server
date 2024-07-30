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

/* scripts\Engines\Apiculture\Items\HiveTool.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Items;
using Server.Network;

namespace Server.Engines.Apiculture
{
    [Flipable(0x09F4, 0x09F5)]
    public class HiveTool : Item, IUsesRemaining
    {
        public override string DefaultName { get { return "Hive Tool"; } }

        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; }
        }

        public bool ShowUsesRemaining
        {
            get { return true; }
            set { }
        }

        [Constructable]
        public HiveTool()
            : this(50)
        {
        }

        [Constructable]
        public HiveTool(int uses)
            : base(0x09F5)
        {
            m_UsesRemaining = uses;
        }

        public override void OnSingleClick(Mobile from)
        {
            if (ShowUsesRemaining)
                LabelToAffix(from, 1017323, AffixType.Append, ": " + m_UsesRemaining.ToString()); // Durability

            base.OnSingleClick(from);
        }

        public HiveTool(Serial serial)
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
        }
    }
}