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

/* Scripts/Items/Skill Items/Specialized/EtchingKit.cs
 * ChangeLog:
 * 5/1/08, Adam
 *		Use HideAttributes filtering to hide the [Exceptional] tag.
 *	05/01/06, weaver
 *		Normalized requirements to 90 primary skill / 80 secondary skill.
 *		Changed instances of 'erlein' to 'weaver' in code comments.
 *	09/11/05, weaver
 *		Changed char limit to 24.
 *		Altered application code to remove Exceptional tag.
 *	09/09/05, weaver
 *		Made it so exceptional required.
 *		Fixed "must be in backpack" message so doesn't talk about clothing.
 *	09/08/05, weaver
 *		Added check to make sure item not previously etched.
 *		Added sound effect for etch!
 *		Added length check to preceed NameVerification (for reporting of length beyond maximum).
 *		Added chance of failure with ruination of piece.
 *	09/08/05, weaver
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

    public class EtchingKitTarget : Target
    {
        private EtchingKit m_EtchingKit;

        public EtchingKitTarget(EtchingKit etchingkit)
            : base(1, false, TargetFlags.None)
        {
            m_EtchingKit = etchingkit;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            // Check targetted thing is clothing

            if (target is BaseJewel)
            {
                // Is is Jewelry, so cast
                BaseJewel bj = (BaseJewel)target;

                // Check player crafted
                if (bj.PlayerCrafted == false)
                {
                    from.SendMessage("Etching kits can only be used on jewelry.");
                    return;
                }

                // Make sure it's in backpack too
                if (!bj.IsChildOf(from.Backpack))
                {
                    from.SendMessage("The jewelry you wish to etch must be in your backpack.");
                    return;
                }

                if (bj.Name != null)
                {
                    // Already etched
                    from.SendMessage("This piece has already been etched.");
                    return;
                }

                if (bj.Quality != JewelQuality.Exceptional)
                {
                    // Must be exceptional
                    from.SendMessage("You feel that this piece is not worthy of your work.");
                    return;
                }

                from.SendMessage("Please enter the words you wish to etch :");
                from.Prompt = new RenamePrompt(from, bj, m_EtchingKit);
            }
            else
            {
                // Not jewelry
                from.SendMessage("Etching kits can only be used on jewelry.");
            }
        }

        // Handles the renaming prompt and associated validation

        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            private BaseJewel m_jewel;
            private EtchingKit m_etchingkit;

            public RenamePrompt(Mobile from, BaseJewel jewel, EtchingKit etchingkit)
            {
                m_from = from;
                m_jewel = jewel;
                m_etchingkit = etchingkit;
            }

            public override void OnResponse(Mobile from, string text)
            {

                // Pattern match for invalid characters
                Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");

                if (InvalidPatt.IsMatch(text))
                {
                    // Invalid chars
                    from.SendMessage("You may only etch numbers, letters, apostrophes and hyphens.");
                }
                else if (text.Length > 24)
                {
                    // Invalid length
                    from.SendMessage("You may only etch a maximum of 24 characters.");
                }
                else if (!NameVerification.Validate(text, 2, 24, true, true, true, 1, NameVerification.SpaceDashPeriodQuote))
                {
                    // Invalid for some other reason
                    from.SendMessage("You may not etch it with this.");
                }
                else if (Utility.RandomDouble() < ((100 - ((from.Skills[SkillName.Tinkering].Base + from.Skills[SkillName.Inscribe].Base) / 2)) * 2) / 100)
                {
                    // Failed!!
                    from.SendMessage("You fail to etch the piece, ruining it in the process!");
                    m_jewel.Delete();
                }
                else
                {
                    // Make the change
                    m_jewel.Name = text + "\n\n";
                    from.SendMessage("You successfully etch your message.");

                    // Play a little sound
                    from.PlaySound(0x241);

                    // Decrement UsesRemaining of graver
                    m_etchingkit.UsesRemaining--;

                    // Check for 0 charges and delete if has none left
                    if (m_etchingkit.UsesRemaining == 0)
                    {
                        m_etchingkit.Delete();
                        from.SendMessage("You have used up your etching kit!");
                    }

                    // we want to hide the [Exceptional] attribute
                    m_jewel.HideAttributes = true;
                }
            }
        }
    }

    // Main type class, including Embroidering check on PlayerMobile
    [Flipable(0x1EB8, 0x1EB9)]
    public class EtchingKit : Item
    {

        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get
            {
                return m_UsesRemaining;
            }
            set
            {
                m_UsesRemaining = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public EtchingKit()
            : base(0x1EB8)
        {
            base.Weight = 1.0;
            base.Name = "metal etching kit";
            UsesRemaining = 10;
            Hue = 0x973;
        }

        public EtchingKit(Serial serial)
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

            // Confirm person using it has learn(t||ed) the etching skill!
            if (pm.Etching == false)
            {
                pm.SendMessage("You have not learned how to etch jewelry.");
                return;
            }
            else if (pm.Skills[SkillName.Tinkering].Base < 90.0 || pm.Skills[SkillName.Inscribe].Base < 80.0)
            {
                pm.SendMessage("Only one who is a both Master Tinker and Expert Scribe can use this.");
                return;
            }

            // Create target and call it
            pm.SendMessage("Choose the jewelry you wish to etch");
            pm.Target = new EtchingKitTarget(this);
        }
    }
}