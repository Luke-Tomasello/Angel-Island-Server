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

/* Scripts/items/misc/TreeTrimmer.cs
 * ChangeLog
 *	10/20/23, Yoar
 *		Initial version.
 */

using Server.Engines.Plants;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class TreeTrimmer : Item, IUsesRemaining
    {
        public override string DefaultName { get { return "Tree Trimmer"; } }

        public virtual bool BreakOnDepletion { get { return true; } }
        public virtual bool ShowUsesRemaining { get { return true; } set { } }

        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; }
        }

        [Constructable]
        public TreeTrimmer()
            : this(50)
        {
        }

        [Constructable]
        public TreeTrimmer(int uses)
            : base(0x0DFC)
        {
            Hue = 71;
            m_UsesRemaining = uses;
        }

        public override void OnSingleClick(Mobile from)
        {
            DisplayDurabilityTo(from);

            base.OnSingleClick(from);
        }

        public virtual void DisplayDurabilityTo(Mobile m)
        {
            LabelToAffix(m, 1017323, AffixType.Append, ": " + m_UsesRemaining.ToString()); // Durability
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendMessage("Target the tree you wish to trim.");
                from.Target = new InternalTarget(this);
            }
        }

        private void OnTarget(Mobile from, object targeted)
        {
            if (targeted is AddonComponent)
                targeted = ((AddonComponent)targeted).Addon;

            PlantAddon plantAddon = targeted as PlantAddon;

            if (plantAddon == null || plantAddon.PlantItem == null || plantAddon.PlantItem.Deleted || !TreeSeed.IsTree(plantAddon.PlantItem.PlantType))
            {
                from.SendMessage("You cannot trim that!");
                return;
            }

            StaticPlantItem plantItem = plantAddon.PlantItem;

            if (!plantItem.IsUsableBy(from))
            {
                from.SendMessage("That is not your tree!");
                return;
            }

            string[] addonIDs = StaticPlantItem.GetAddonIDs(TreeSeed.GetTreeType(plantItem.PlantType));

            if (addonIDs.Length <= 1)
            {
                from.SendMessage("The tree trimmer has no use for this type of tree.");
                return;
            }

            int index = -1;

            for (int i = 0; index == -1 && i < addonIDs.Length; i++)
            {
                if (XmlAddon.MatchByName(plantAddon, PlantAddon.DataFolder, addonIDs[i]))
                    index = i;
            }

            index++;

            if (index >= addonIDs.Length)
                index = 0;

            plantItem.RebuildAddon(addonIDs[index]);

            from.SendMessage("You trim the tree.");
            from.PlaySound(0x248);

            if (--UsesRemaining <= 0)
            {
                from.SendLocalizedMessage(1044038); // You have worn out your tool!
                Delete();
            }
        }

        private class InternalTarget : Target
        {
            private TreeTrimmer m_Item;

            public InternalTarget(TreeTrimmer item)
                : base(2, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted || !m_Item.IsChildOf(from.Backpack))
                    return;

                m_Item.OnTarget(from, targeted);
            }
        }

        public TreeTrimmer(Serial serial)
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