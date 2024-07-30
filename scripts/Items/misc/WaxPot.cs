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

/* Items/Misc/WaxPot.cs
 * CHANGELOG:
 *	11/21/21, Yoar
 *	    Fixed typo (dye->wax) in wax craft
 *	    Explicitly set names of wax pots
 *	11/20/21, Yoar
 *		Newly instanced wax pots are now empty.
 *		Wax craft now only works on actual wax pots.
 *	11/19/21, Yoar
 *		Initial version.
 */

using Server.Engines.Craft;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.Items
{
    public abstract class BaseWaxPot : Item
    {
        public abstract int MaxQuantity { get; }
        public abstract int EmptyItemID { get; }
        public abstract int FilledItemID { get; }

        private int m_Quantity;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Quantity
        {
            get { return m_Quantity; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > MaxQuantity)
                    value = MaxQuantity;

                if (m_Quantity != value)
                {
                    m_Quantity = value;
                    UpdateItemID();
                    InvalidateProperties();
                }
            }
        }

        public BaseWaxPot(int itemID)
            : this(itemID, 0)
        {
        }

        public BaseWaxPot(int itemID, int quantity)
            : base(itemID)
        {
            m_Quantity = quantity;
            UpdateItemID();
        }

        protected void UpdateItemID()
        {
            if (m_Quantity == 0)
                ItemID = this.EmptyItemID;
            else
                ItemID = this.FilledItemID;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Quantity > 0)
                LabelToAffix(from, 1077826, AffixType.Append, String.Format(": {0}", m_Quantity)); // Quantity: ~1_QUANTITY~
        }

        private static object m_Targeted;

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Targeted != null)
            {
                OnTarget(from, m_Targeted);
            }
            else
            {
                if (Quantity == 0)
                    from.SendLocalizedMessage(1019073);         // This item is out of charges.
                else
                {
                    from.SendLocalizedMessage(1010086);         // What do you want to use this on?
                    from.Target = new InternalTarget(this);
                }
            }
        }

        private class InternalTarget : Target
        {
            private BaseWaxPot m_Pot;

            public InternalTarget(BaseWaxPot pot)
                : base(2, false, TargetFlags.None)
            {
                m_Pot = pot;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                m_Targeted = targeted;

                from.Use(m_Pot);

                m_Targeted = null;
            }
        }

        private void OnTarget(Mobile from, object targeted)
        {
            if (targeted is BaseWeapon bw)
            {
                bool isValidResource =
                    bw.Resource == CraftResource.RegularWood || bw.Resource == CraftResource.OakWood ||
                    bw.Resource == CraftResource.AshWood || bw.Resource == CraftResource.YewWood ||
                    bw.Resource == CraftResource.Heartwood || bw.Resource == CraftResource.Bloodwood ||
                    bw.Resource == CraftResource.Frostwood;
                bool isNpcCrafted =
                    bw.Resource == CraftResource.Iron && (bw is BaseRanged || bw is BaseBashing || bw is GnarledStaff ||
                    bw is QuarterStaff || bw is ShepherdsCrook /* || fishingpole? lol */);

                if (isValidResource || isNpcCrafted)
                {
                    bw.WaxCharges += 20;
                    Quantity -= 1;
                    from.SendMessage("You add 20 wax coats to your weapon.");
                }
                else
                    from.SendMessage("That weapon is not eligible for waxing.");
            }
            else
                from.SendMessage("That is not a weapon.");

        }

        public BaseWaxPot(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Quantity);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Quantity = reader.ReadInt();
        }
    }

    public class WaxPot : BaseWaxPot
    {
        public override int MaxQuantity { get { return 5; } }
        public override int EmptyItemID { get { return 0x9E4; } } // TODO: Use dirty ID?
        public override int FilledItemID { get { return 0x142B; } }

        public override string OldName { get { return "wax pot"; } }
        public override Article OldArticle { get { return Article.A; } }

        [Constructable]
        public WaxPot()
            : this(0)
        {
        }

        [Constructable]
        public WaxPot(int quantity)
            : base(quantity)
        {
            Name = "wax pot";
        }

        public WaxPot(Serial serial)
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

            reader.ReadEncodedInt();
        }
    }

    public class WaxKettle : BaseWaxPot
    {
        public override int MaxQuantity { get { return 15; } }
        public override int EmptyItemID { get { return 0x9ED; } } // TODO: Use dirty ID?
        public override int FilledItemID { get { return 0x142A; } }

        public override string OldName { get { return "wax kettle"; } }
        public override Article OldArticle { get { return Article.A; } }

        [Constructable]
        public WaxKettle()
            : this(0)
        {
        }

        [Constructable]
        public WaxKettle(int quantity)
            : base(quantity)
        {
            Name = "wax kettle";
        }

        public WaxKettle(Serial serial)
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

            reader.ReadEncodedInt();
        }
    }

    // based on Server.Engines.Craft.TrapCraft
    [CraftItemIDAttribute(0x142B)]
    public class WaxCraft : CustomCraft
    {
        private Item m_Pot;

        public WaxCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
        }

        private TextDefinition Verify(Item pot)
        {
            if (pot != null)
            {
                // TODO: If this is a dirty pot, clean it first? Chance to break the pot on clean.

                if (!pot.IsChildOf(From.Backpack))
                    return 1042001; // That must be in your pack for you to use it.

                if (pot is BaseWaxPot)
                {
                    BaseWaxPot waxPot = (BaseWaxPot)pot;

                    if (waxPot.Quantity < waxPot.MaxQuantity)
                        return null; // valid

                    return "That wax pot is full.";
                }
            }

            return "You can only pour this into wax pots.";
        }

        private bool Acquire(object targeted, out TextDefinition message)
        {
            Item pot = targeted as Item;

            message = Verify(pot);

            if (message != null)
            {
                return false;
            }
            else
            {
                m_Pot = pot;
                return true;
            }
        }

        public override void EndCraftAction()
        {
            Container pack = From.Backpack;

            Item found = null;

            if (pack != null)
            {
                foreach (Item item in pack.Items)
                {
                    if (item is BaseWaxPot)
                    {
                        BaseWaxPot waxPot = (BaseWaxPot)item;

                        if (waxPot.Quantity < waxPot.MaxQuantity)
                        {
                            found = waxPot;
                            break;
                        }
                    }
                }
            }

            if (found != null)
            {
                m_Pot = found;
                CraftItem.CompleteCraft(Quality, false, From, CraftSystem, TypeRes, Tool, this);
            }
            else
            {
                From.SendMessage("Select the wax pot to pour this wax into.");
                From.Target = new WaxPotTarget(this);
            }
        }

        private class WaxPotTarget : Target
        {
            private WaxCraft m_Craft;

            public WaxPotTarget(WaxCraft craft)
                : base(-1, false, TargetFlags.None)
            {
                m_Craft = craft;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                TextDefinition message;

                if (m_Craft.Acquire(targeted, out message))
                    m_Craft.CraftItem.CompleteCraft(m_Craft.Quality, false, m_Craft.From, m_Craft.CraftSystem, m_Craft.TypeRes, m_Craft.Tool, m_Craft);
                else
                    Failure(message);
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (cancelType == TargetCancelType.Canceled)
                    Failure(null);
            }

            private void Failure(TextDefinition message)
            {
                Mobile from = m_Craft.From;
                BaseTool tool = m_Craft.Tool;

                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    from.SendGump(new CraftGump(from, m_Craft.CraftSystem, tool, message));
                else
                    TextDefinition.SendMessageTo(from, message);
            }
        }

        public override Item CompleteCraft(out TextDefinition message)
        {
            message = Verify(m_Pot);

            if (message == null)
            {
                ((BaseWaxPot)m_Pot).Quantity++;

                From.PlaySound(0x20); // bubbles

                message = "You melt the wax in the wax pot.";
            }

            return null;
        }
    }
}