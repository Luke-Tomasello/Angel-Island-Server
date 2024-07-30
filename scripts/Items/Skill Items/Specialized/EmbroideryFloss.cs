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

/* Scripts/Items/Skill Items/Specialized/EmbroideryFloss.cs
 * ChangeLog:
 * 5/1/08, Adam
 *		No longer reduces quality.  Replaced with HideAttributes to hide the [Exceptional] tag.
 *	05/01/06, weaver
 *		Normalized requirements to 90 primary skill / 80 secondary skill.
 *		Changed instances of 'erlein' to 'weaver' in code comments.
 *	09/11/05, weaver
 *		Changed char limit to 24.
 *		Altered application code to remove Exceptional tag.
 *	09/09/05, weaver
 *		Added a check for quality.
 *	09/08/05, weaver
 *		Added check to make sure item not previously embroidered.
 *		Added length check to preceed NameVerification (for reporting of length beyond maximum).
 *		Added chance of failure with ruination of piece.
 *	09/08/05, weaver
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

    // Graver target class

    public class EmbroideryTarget : Target
    {
        private Embroidery m_Embroidery;

        public EmbroideryTarget(Embroidery embroidery)
            : base(1, false, TargetFlags.None)
        {
            m_Embroidery = embroidery;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            // Check targetted thing is clothing

            if (target is BaseClothing)
            {
                // Is is clothing, so cast
                BaseClothing bc = (BaseClothing)target;

                // Check player crafted
                if (bc.PlayerCrafted == false)
                {
                    from.SendMessage("Embroidery can only be used on crafted clothing.");
                    return;
                }

                // Make sure it's in backpack too
                if (!bc.IsChildOf(from.Backpack))
                {
                    from.SendMessage("The clothing you wish to embroider must be in your backpack.");
                    return;
                }

                if (bc.Name != null)
                {
                    // Already embroidered
                    from.SendMessage("This piece has already been embroidered.");
                    return;
                }

                if (bc.Quality != ClothingQuality.Exceptional)
                {
                    // Must be exceptional
                    from.SendMessage("You feel that this piece is not worthy of your work.");
                    return;
                }


                from.SendMessage("Please enter the words you wish to embroider :");
                from.Prompt = new RenamePrompt(from, bc, m_Embroidery);
            }
            else
            {
                // Not clothing

                from.SendMessage("Embroidery can only be used on a clothing.");
            }

        }

        // Handles the renaming prompt and associated validation

        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            private BaseClothing m_clothing;
            private Embroidery m_embroidery;

            public RenamePrompt(Mobile from, BaseClothing clothing, Embroidery embroidery)
            {
                m_from = from;
                m_clothing = clothing;
                m_embroidery = embroidery;
            }

            public override void OnResponse(Mobile from, string text)
            {
                // Pattern match for invalid characters
                Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");

                if (InvalidPatt.IsMatch(text))
                {
                    // Invalid chars
                    from.SendMessage("You may only embroider numbers, letters, apostrophes and hyphens.");
                }
                else if (m_clothing.Name != null)
                {
                    // Already embroidered
                    from.SendMessage("This piece has already been embroidered.");
                }
                else if (text.Length > 24)
                {
                    // Invalid length
                    from.SendMessage("You may only embroider a maximum of 24 characters.");
                }
                else if (!NameVerification.Validate(text, 2, 24, true, true, true, 1, NameVerification.SpaceDashPeriodQuote))
                {
                    // Invalid for some other reason
                    from.SendMessage("You may not embroider it with this.");
                }
                else if (Utility.RandomDouble() < ((100 - ((from.Skills[SkillName.Tailoring].Base + from.Skills[SkillName.Inscribe].Base) / 2)) * 2) / 100)
                {
                    // Failed!!
                    from.SendMessage("You fail to embroider the piece, ruining it in the process!");
                    m_clothing.Delete();
                }
                else
                {
                    // Make the change
                    m_clothing.Name = text + "\n\n";
                    from.SendMessage("You successfully embroider the clothing.");

                    // Decrement UsesRemaining of graver
                    m_embroidery.UsesRemaining--;

                    // Check for 0 charges and delete if has none left
                    if (m_embroidery.UsesRemaining == 0)
                    {
                        m_embroidery.Delete();
                        from.SendMessage("You have used up your embroidery!");
                    }

                    // Consume single charge from scribe pen...

                    Item[] skits = ((Container)from.Backpack).FindItemsByType(typeof(SewingKit), true);

                    if (--((SewingKit)skits[0]).UsesRemaining == 0)
                    {
                        from.SendMessage("You have worn out your tool!");
                        skits[0].Delete();
                    }

                    // we want to hide the [Exceptional] attribute
                    m_clothing.HideAttributes = true;
                }
            }
        }
    }

    // Main type class, including Embroidering check on PlayerMobile

    public class Embroidery : Item
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
        public Embroidery()
            : base(0xFA0)
        {
            base.Weight = 1.0;
            base.Name = "embroidery floss";
            UsesRemaining = 10;
        }

        public Embroidery(Serial serial)
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

            // Confirm person using it has learn(t||ed) the embroidering skill!
            if (pm.Embroidering == false)
            {
                pm.SendMessage("You have not learned how to use embroidery floss.");
                return;
            }
            else if (pm.Skills[SkillName.Tailoring].Base < 90.0 || pm.Skills[SkillName.Inscribe].Base < 80.0)
            {
                pm.SendMessage("Only one who is a both a Master Tailor and an Expert Scribe can use this.");
                return;
            }

            // Confirm that there is a sewing kit in backpack
            Item[] skits;
            skits = ((Container)from.Backpack).FindItemsByType(typeof(SewingKit), true);

            bool found = false;

            foreach (SewingKit skit in skits)
            {
                if (skit != null)
                {
                    found = true;
                    break;
                }
            }

            if (found == false)
            {
                pm.SendMessage("You must have a sewing kit to embroider clothing.");
                return;

            }
            // Create target and call it
            pm.SendMessage("Choose the clothing you wish to embroider");
            pm.Target = new EmbroideryTarget(this);
        }
    }
}