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

/* Scripts/Items/Skill Items/Specialized/SquareGraver.cs
 * ChangeLog:
 *	05/01/06, weaver
 *		Normalized requirements to 90 primary skill / 80 secondary skill.
 *		Changed instances of 'erlein' to 'weaver' in code comments.
 *	10/18/05, weaver
 *		Added skill check for Inscription and Carpentry on use.
 *	09/11/05, weaver
 *		Changed char limit to 24.
 *	09/08/05, weaver
 *		Added chance of failure.
 *		Added check to make sure item not previously engraved.
 *		Added length check to preceed NameVerification (for reporting of length beyond maximum).
 *	03/11/05, weaver
 *		Made the square graver get deleted when last charge is used.
 *		w/message to mobile using it indicating it broken.
 *	03/10/05, weaver
 *		Fixed spelling error, changed naming error message, added pack check for
 *		container we're trying to engrave, changed graver graphic.
 *	03/09/05, weaver
 *		Initial creation.
 */

using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
using System.Text.RegularExpressions;

namespace Server.Items
{

    // Graver target class

    public class SquareGraverTarget : Target
    {
        private SquareGraver m_Graver;

        public SquareGraverTarget(SquareGraver graver)
            : base(1, false, TargetFlags.None)
        {
            m_Graver = graver;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            // Check targetted thing is a container

            if (target is BaseContainer)
            {

                // Is a container, so cast

                BaseContainer bc = (BaseContainer)target;

                // Check player crafted

                if (bc.PlayerCrafted == false)
                {
                    from.SendMessage("This tool can only be used on crafted containers.");
                    return;
                }

                // Make sure it's in backpack too

                if (!bc.IsChildOf(from.Backpack))
                {
                    from.SendMessage("The container you wish to engrave must be in your backpack.");
                    return;
                }

                if (bc.Name != null)
                {
                    // Already engraved
                    from.SendMessage("This piece has already been engraved.");
                    return;
                }

                from.SendMessage("Please enter the words you wish to engrave :");
                from.Prompt = new RenamePrompt(from, bc, m_Graver);
            }
            else
            {
                // Not a container

                from.SendMessage("This tool can only be used on a container.");
            }

        }

        // Handles the renaming prompt and associated validation

        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            private BaseContainer m_container;
            private SquareGraver m_graver;

            public RenamePrompt(Mobile from, BaseContainer container, SquareGraver graver)
            {
                m_from = from;
                m_container = container;
                m_graver = graver;
            }

            public override void OnResponse(Mobile from, string text)
            {

                // Pattern match for invalid characters
                Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");

                if (InvalidPatt.IsMatch(text))
                {
                    // Invalid chars
                    from.SendMessage("You may only engrave numbers, letters, apostrophes and hyphens.");
                }
                else if (text.Length > 24)
                {
                    // Invalid length
                    from.SendMessage("You may only engrave a maximum of 24 characters.");
                }
                else if (!NameVerification.Validate(text, 2, 24, true, true, true, 1, NameVerification.SpaceDashPeriodQuote))
                {
                    // Invalid for some other reason
                    from.SendMessage("You may not name it this here.");
                }
                else if (Utility.RandomDouble() < (100 - from.Skills[SkillName.Carpentry].Base) / 100)
                {
                    // Failed!!
                    from.SendMessage("You fail to engrave the piece, ruining it in the process!");
                    m_container.Delete();
                }
                else
                {
                    // Make the change
                    m_container.Name = text;
                    from.SendMessage("You successfully engrave the container.");

                    // Decrement UsesRemaining of graver
                    m_graver.UsesRemaining--;

                    // Check for 0 charges and delete if has none left
                    if (m_graver.UsesRemaining == 0)
                    {
                        m_graver.Delete();
                        from.SendMessage("You have worn out your tool!");
                    }
                }
            }
        }
    }

    // Main type class, including WoodEngraving check on PlayerMobile

    public class SquareGraver : Item
    {

        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set
            {
                m_UsesRemaining = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public SquareGraver()
            : base(0x10E7)
        {
            base.Weight = 1.0;
            base.Name = "a square graver";
            UsesRemaining = 10;
        }

        public SquareGraver(Serial serial)
            : base(serial)
        {
        }

        // UsesRemaining property handling

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
            writer.Write((int)m_UsesRemaining); // Uses remaining
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_UsesRemaining = reader.ReadInt(); // Uses remaining
                        break;
                    }

            }

        }

        public override void OnDoubleClick(Mobile from)
        {
            // Make sure is in pack
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            PlayerMobile pm = (PlayerMobile)from;

            // Confirm person using it has learn(t||ed) he engraving skill!
            if (pm.WoodEngraving == false)
            {
                pm.SendMessage("You have not learned how to use this tool.");
                return;
            }

            if (pm.Skills[SkillName.Inscribe].Value < 80.0 || pm.Skills[SkillName.Carpentry].Value < 90.0)
            {
                pm.SendMessage("Only one who is a both a Master Carpenter and an Expert Scribe can use this.");
                return;
            }

            // Create target and call it
            pm.SendMessage("Choose the container you wish to engrave");
            pm.Target = new SquareGraverTarget(this);

        }

    }

}