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

/* Scripts/Items/Skill Items/Fishing/FishingPole.cs
/* CHANGELOG
 *  9/22/23, Yoar
 *      Fishing poles are now one-handed (enabled for all shards)
 * 11/14/22, Adam (IHasUsesRemaining)
 *  Now implements IHasUsesRemaining to consume uses on Siege, but not on other shards.
 *      Like IUsesRemaining, IHasUsesRemaining consumes uses, but is dynamic where 
 *      IUsesRemaining is not.
 * 11/14/21, Yoar
 *		- Now derives from BaseOtherEquipable.
 *		- Changed layer from OneHanded to TwoHanded.
 *		- Added range check in OnDoubleClick.
 */

using Server.Engines.Harvest;
using Server.Network;
using System.Collections;

namespace Server.Items
{
    public class FishingPole : BaseOtherEquipable, IHasUsesRemaining
    {
        public override int AosStrReq { get { return 10; } }

        [Constructable]
        public FishingPole()
            : base(0x0DC0)
        {
            Layer = (Core.RuleSets.AllShards ? Layer.OneHanded : Layer.TwoHanded);
            Weight = 8.0;
            m_usesRemaining = 200;
        }
        #region UsesRemaining
        public bool WearsOut { get { return Core.RuleSets.SiegeStyleRules(); } }
        public int ToolBrokeMessage => 503174; // You broke your fishing pole.
        int m_usesRemaining;
        // staff don't need to see this
        [CommandProperty(AccessLevel.Owner)]
        public int UsesRemaining { get { return m_usesRemaining; } set { m_usesRemaining = value; } }
        public override void OnActionComplete(Mobile from, Item tool)
        {
            if (this == tool && Utility.Inventory(from).Contains(this))
                // fishing poles only wear out on Siege
                if (WearsOut)
                    ConsumeUse(from);
        }
        public void ConsumeUse(Mobile from)
        {
            // diminish uses

            if (UsesRemaining > 0)
                UsesRemaining--;

            if (UsesRemaining < 1)
            {
                Delete();
                from.SendLocalizedMessage(ToolBrokeMessage);
            }
        }
        #endregion UsesRemaining
        public override void OnDoubleClick(Mobile from)
        {
            Point3D loc = GetWorldLocation();
            if (!from.InLOS(loc) || !from.InRange(loc, 2))
                from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1019045); // I can't reach that
            else
                Fishing.System.BeginHarvesting(from, this);
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            BaseHarvestTool.AddContextMenuEntries(from, this, list, Fishing.System);
        }

        public FishingPole(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(m_usesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_usesRemaining = reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        Layer = Layer.TwoHanded;
                        break;
                    }
                case 0:
                    {
                        Layer = Layer.OneHanded;
                        break;
                    }
            }

            if (Layer == Layer.TwoHanded && Core.RuleSets.AllShards)
                Layer = Layer.OneHanded;
        }
    }
}