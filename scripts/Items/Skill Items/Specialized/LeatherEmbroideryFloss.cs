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

/* Scripts/Items/Skill Items/Specialized/LeatherEmbroideryFloss.cs
 * ChangeLog:
 * 4/30/08, Pix
 *		No longer reduces quality.  Replaced with HideAttributes to hide the [Exceptional] tag.
 *	4/2/8,Pix
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
    public class LeatherEmbroideryFloss : Item
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
        public LeatherEmbroideryFloss()
            : base(0x1420)
        {
            base.Weight = 2.0;
            base.Name = "heavy embroidery floss";
            UsesRemaining = 10;
        }

        public LeatherEmbroideryFloss(Serial serial)
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
            if (pm.LeatherEmbroidering == false)
            {
                pm.SendMessage("You have not learned how to use heavy embroidery floss.");
                return;
            }
            else if (pm.Skills[SkillName.Tailoring].Base < LeatherEmbroideryBook.c_RequiredTailoring || pm.Skills[SkillName.Inscribe].Base < LeatherEmbroideryBook.c_RequiredInscribe)
            {
                pm.SendMessage("Only one who is a both a Master Tailor and an Expert Scribe can use this.");
                return;
            }

            Item[] sewingkits;
            sewingkits = ((Container)from.Backpack).FindItemsByType(typeof(SewingKit), true);

            bool found = false;

            foreach (SewingKit sewingkit in sewingkits)
            {
                if (sewingkit != null)
                {
                    found = true;
                    break;
                }
            }

            if (found == false)
            {
                pm.SendMessage("You must have a sewing kit to embroider leather.");
                return;
            }

            pm.SendMessage("Choose the leather you wish to embroider");
            pm.Target = new LeatherEmbroideryTarget(this);
        }
    }

    public class LeatherEmbroideryTarget : Target
    {
        private LeatherEmbroideryFloss m_Embroidery;

        public LeatherEmbroideryTarget(LeatherEmbroideryFloss embroidery)
            : base(1, false, TargetFlags.None)
        {
            m_Embroidery = embroidery;
        }

        private bool IsMadeOfLeather(Item item)
        {
            if (IsLeatherArmor(item))
            {
                return true;
            }
            return false;
        }
        private bool IsExceptional(Item item)
        {
            if (item is BaseArmor)
            {
                BaseArmor ba = item as BaseArmor;
                if (ba != null && ba.Quality == ArmorQuality.Exceptional)
                {
                    return true;
                }
            }
            return false;
        }
        private bool IsPlayerCrafted(Item item)
        {
            if (item != null)
            {
                return item.PlayerCrafted;
            }
            return false;
        }


        //This was taken from LeatherArmorDyeTub.cs
        private bool IsLeatherArmor(Item item)
        {
            if (item is BaseArmor)
            {
                BaseArmor ba = item as BaseArmor;
                if (ba == null)
                    return false;

                // Unfortunately certain 'bone' armor is using leather as the resource.
                //  therefore we make an explicit type check.
                if (ba.GetType().ToString().Contains("LeatherCap") ||
                    ba.GetType().ToString().Contains("FemaleLeatherChest") || ba.GetType().ToString().Contains("FemaleStuddedChest") ||
                    ba.GetType().ToString().Contains("LeatherArms") || ba.GetType().ToString().Contains("StuddedArms") ||
                    ba.GetType().ToString().Contains("LeatherBustierArms") || ba.GetType().ToString().Contains("StuddedBustierArms") ||
                    ba.GetType().ToString().Contains("LeatherChest") || ba.GetType().ToString().Contains("StuddedChest") ||
                    ba.GetType().ToString().Contains("LeatherGloves") || ba.GetType().ToString().Contains("StuddedGloves") ||
                    ba.GetType().ToString().Contains("LeatherGorget") || ba.GetType().ToString().Contains("StuddedGorget") ||
                    ba.GetType().ToString().Contains("LeatherLegs") || ba.GetType().ToString().Contains("StuddedLegs") ||
                    ba.GetType().ToString().Contains("LeatherShorts") ||
                    ba.GetType().ToString().Contains("LeatherSkirt"))
                    if (ba.Resource == CraftResource.RegularLeather ||
                        ba.Resource == CraftResource.SpinedLeather ||
                        ba.Resource == CraftResource.HornedLeather ||
                        ba.Resource == CraftResource.BarbedLeather)
                        return true;
            }

            return false;
        }


        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is Item)
            {
                Item item = target as Item;
                if (item == null)
                {
                    from.SendMessage("Bad item.");
                }

                if (IsMadeOfLeather(item))
                {
                    if (IsPlayerCrafted(item) == false)
                    {
                        from.SendMessage("Heavy embroidery floss can only be used on crafted items made of leather.");
                        return;
                    }
                    if (!item.IsChildOf(from.Backpack))
                    {
                        from.SendMessage("The leather item you wish to embroider must be in your backpack.");
                        return;
                    }
                    if (item.Name != null)
                    {
                        // Already embroidered
                        from.SendMessage("This has already been embroidered.");
                        return;
                    }
                    if (IsExceptional(item) == false)
                    {
                        // Must be exceptional
                        from.SendMessage("You feel that this piece is not worthy of your work.");
                        return;
                    }
                    from.SendMessage("Please enter the words you wish to embroider :");
                    from.Prompt = new RenamePrompt(from, item, m_Embroidery);
                }
                else
                {
                    from.SendMessage("Heavy embroidery floss can only be used on leather.");
                }
            }
            else
            {
                from.SendMessage("You can only use that on leather items.");
            }
        }

        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            private Item m_item;
            private LeatherEmbroideryFloss m_embroidery;

            public RenamePrompt(Mobile from, Item item, LeatherEmbroideryFloss embroidery)
            {
                m_from = from;
                m_item = item;
                m_embroidery = embroidery;
            }

            private void ReduceQuality(Item item)
            {
                if (item is BaseArmor)
                {
                    BaseArmor ba = item as BaseArmor;
                    if (ba != null && ba.Quality == ArmorQuality.Exceptional)
                    {
                        ba.Quality = ArmorQuality.Regular;
                    }
                }
            }

            private bool HasSpecialKeyword(string text)
            {
                string lowertext = text.ToLower();
                if (
                    lowertext.Contains("magic")
                    || lowertext.Contains("crafted by")
                    || lowertext.Contains("poisoned")
                    || lowertext.Contains("charges")
                    || lowertext.Contains("exceptional")
                    //ArmorDurability && WeaponDurabilityLevel
                    || lowertext.Contains("durable")
                    || lowertext.Contains("substantial")
                    || lowertext.Contains("massive")
                    || lowertext.Contains("fortified")
                    || lowertext.Contains("indestructible")
                    //ArmorProtectionLevel
                    || lowertext.Contains("defense")
                    || lowertext.Contains("guarding")
                    || lowertext.Contains("hardening")
                    || lowertext.Contains("fortification")
                    || lowertext.Contains("invulnerability")
                    //WeaponDamageLevel
                    || lowertext.Contains("ruin")
                    || lowertext.Contains("might")
                    || lowertext.Contains("force")
                    || lowertext.Contains("power")
                    || lowertext.Contains("vanquishing")
                    //WeaponAccuracyLevel
                    || lowertext.Contains("accurate")
                    || lowertext.Contains("surpassingly")
                    || lowertext.Contains("eminently")
                    || lowertext.Contains("exceedingly")
                    || lowertext.Contains("supremely")
                    )
                {
                    return true;
                }
                return false;
            }

            public override void OnResponse(Mobile from, string text)
            {
                Regex InvalidPattern = new Regex("[^-a-zA-Z0-9' ]");

                if (InvalidPattern.IsMatch(text))
                {
                    from.SendMessage("You may only embroider numbers, letters, apostrophes and hyphens.");
                }
                else if (m_item.Name != null)
                {
                    from.SendMessage("This piece has already been embroidered.");
                }
                else if (text.Length > 24)
                {
                    from.SendMessage("You may only embroider a maximum of 24 characters.");
                }
                else if (!NameVerification.Validate(text, 2, 24, true, true, true, 1, NameVerification.SpaceDashPeriodQuote))
                {
                    from.SendMessage("You may not embroider it with this.");
                }
                else if (HasSpecialKeyword(text))
                {
                    from.SendMessage("You cannot embroider it with that.");
                }
                else if (Utility.RandomDouble() < ((100 - ((from.Skills[SkillName.Tailoring].Base + from.Skills[SkillName.Inscribe].Base) / 2)) * 2) / 100)
                {
                    from.SendMessage("You fail to embroider the piece, ruining it in the process!");
                    m_item.Delete();
                }
                else
                {
                    m_item.Name = text + "\n\n";
                    from.SendMessage("You successfully embroider the clothing.");

                    m_embroidery.UsesRemaining--;

                    if (m_embroidery.UsesRemaining <= 0)
                    {
                        m_embroidery.Delete();
                        from.SendMessage("You have used up your embroidery!");
                    }

                    Item[] sewingkits = ((Container)from.Backpack).FindItemsByType(typeof(SewingKit), true);

                    if (--((SewingKit)sewingkits[0]).UsesRemaining <= 0)
                    {
                        from.SendMessage("You have worn out your tool!");
                        sewingkits[0].Delete();
                    }

                    //Pix: 4/30/08 - we do want to hide the [Exceptional] attribute :D
                    m_item.HideAttributes = true;

                }
            }
        }
    }

}