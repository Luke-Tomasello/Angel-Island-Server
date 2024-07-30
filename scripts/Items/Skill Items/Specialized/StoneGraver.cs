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

/* Scripts/Items/Skill Items/Specialized/StoneGraver.cs
 * ChangeLog:
 *	04/07/05, Kitaras
 *		Initial creation
 */

using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
using System.Text.RegularExpressions;

namespace Server.Items
{
    public interface IStoneEngravable
    {
        bool IsEngravable { get; }

        bool OnEngrave(Mobile from, string text);
    }

    public class StoneGraverTarget : Target
    {
        private StoneGraver m_Tool;

        public StoneGraverTarget(StoneGraver tool)
            : base(2, false, TargetFlags.None)
        {
            m_Tool = tool;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Tool.Deleted || !m_Tool.IsChildOf(from.Backpack))
                return;

            Item item = targeted as Item;

            if (item == null || !(item is IStoneEngravable) || !((IStoneEngravable)item).IsEngravable)
            {
                from.SendMessage("You cannot engrave that!");
            }
            else
            {
                from.SendMessage("Enter the words you wish to engrave.");
                from.Prompt = new StoneGraverPrompt(m_Tool, item);
            }
        }

        private class StoneGraverPrompt : Prompt
        {
            private StoneGraver m_Tool;
            private Item m_Item;

            public StoneGraverPrompt(StoneGraver tool, Item item)
            {
                m_Tool = tool;
                m_Item = item;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Tool.Deleted || !m_Tool.IsChildOf(from.Backpack) || !from.InRange(m_Item.GetWorldLocation(), 2) || !m_Item.IsAccessibleTo(from) || !(m_Item is IStoneEngravable) || !((IStoneEngravable)m_Item).IsEngravable)
                    return;

                Regex InvalidPattern = new Regex("[^-a-zA-Z0-9' ]");

                if (InvalidPattern.IsMatch(text))
                {
                    from.SendMessage("You may only engrave numbers, letters, apostrophes and hyphens.");
                }
                else if (!NameVerification.Validate(text, 2, 30, true, true, true, 1, NameVerification.SpaceDashPeriodQuote))
                {
                    from.SendMessage("That name is invalid.");
                }
                else if (((IStoneEngravable)m_Item).OnEngrave(from, text))
                {
                    if (--m_Tool.UsesRemaining <= 0)
                    {
                        m_Tool.Delete();

                        from.SendMessage("You have worn out your tool!");
                    }
                }
            }
        }
    }

    public class StoneGraver : Item
    {
        public override string DefaultName { get { return "a stone graver"; } }

        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        [Constructable]
        public StoneGraver()
            : base(0x1027)
        {
            UsesRemaining = 10;
        }

        public StoneGraver(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060584, m_UsesRemaining.ToString()); // uses remaining: ~1_val~
        }

        public virtual void DisplayDurabilityTo(Mobile m)
        {
            LabelToAffix(m, 1017323, AffixType.Append, ": " + m_UsesRemaining.ToString()); // Durability
        }

        public override void OnSingleClick(Mobile from)
        {
            DisplayDurabilityTo(from);

            base.OnSingleClick(from);
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

            switch (version)
            {
                case 0:
                    {
                        m_UsesRemaining = reader.ReadInt();
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (!(from is PlayerMobile) || !((PlayerMobile)from).Masonry)
            {
                from.SendLocalizedMessage(1044633); // You haven't learned masonry.  Perhaps you need to study a book!
            }
            else
            {
                from.SendMessage("What do you wish to engrave?");
                from.Target = new StoneGraverTarget(this);
            }
        }
    }
}