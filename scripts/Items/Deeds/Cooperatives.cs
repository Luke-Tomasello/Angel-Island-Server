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

/* ChangeLog
 * Scripts/Items/Deeds/BaseCooperativeDeed.cs
 * 
 *  10/19/21, Adam
 *		Initial version.
 */

using Server.Misc;
using Server.Multis;
using Server.Prompts;
using Server.Targeting;
using System;
using System.Text.RegularExpressions;

namespace Server.Items
{
    public class BaseCooperativeDeedTarget : Target
    {
        private BaseCooperativeDeed m_Deed;

        public BaseCooperativeDeedTarget(BaseCooperativeDeed deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target)
        {
            if (m_Deed == null || m_Deed.Deleted)
                return;

            if (target is HouseSign hs)
            {
                if (hs.MembershipOnly && hs.CooperativeType == m_Deed.CooperativeType)
                {
                    Item[] items = hs.Structure.FindAllItems(typeof(MultiUserStrongBox));
                    bool added = false;
                    foreach (Item item in items)
                        if (item is MultiUserStrongBox musb)
                        {   // currently we only support one MultiUserMemberStorage per house.
                            if (musb.AddMemberStorage(from, hs.Structure))
                                added = true;
                            else
                                added = false;

                            // now add them to the list of members for the house
                            if (hs.Structure.IsMember(from) == false)
                                hs.Structure.AddMember(from, TimeSpan.FromDays(90));
                            else
                                hs.Structure.ExtendMembership(from, TimeSpan.FromDays(90));

                            if (added)
                                from.SendAsciiMessage("Welcome to the {0}!", hs.Name);

                            from.SendAsciiMessage("Your membership will expire on {0}.", hs.Structure.GetMembershipExpiration(from));
                            m_Deed.Delete();
                            return;
                        }
                }

                else
                {
                    if (hs.MembershipOnly == false)
                        from.SendAsciiMessage("That is not a members only establishment.");
                    else
                        from.SendAsciiMessage("Your membership will only work on a {0} cooperative.", m_Deed.CooperativeType.ToString());
                }
            }
            else
            {
                from.SendMessage("That is not a house sign.");
            }
        }


        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            private BaseCooperativeDeed m_deed;
            private Mobile vendor;

            public RenamePrompt(Mobile from, Mobile target, BaseCooperativeDeed deed)
            {
                m_from = from;
                m_deed = deed;
                vendor = target;
            }

            public override void OnResponse(Mobile from, string text)
            {
                Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");
                if (InvalidPatt.IsMatch(text))
                {
                    // Invalid chars
                    from.SendMessage("You may only use numbers, letters, apostrophes, hyphens and spaces in the name to be changed.");

                }
                else if (!NameVerification.Validate(text, 2, 16, true, true, true, 1, NameVerification.SpaceDashPeriodQuote, NameVerification.BuildList(true, true, false, true)))
                {
                    // Invalid for some other reason
                    from.SendMessage("That name is not allowed here.");
                }
                else
                {
                    vendor.Name = text;
                    from.SendMessage("Thou hast successfully changed thy servant's name.");
                    m_deed.Delete();
                }
            }
        }
    }

    public abstract class BaseCooperativeDeed : Item
    {
        public BaseCooperativeDeed()
            : base(0x14F0)
        {
            Weight = 1.0;
        }

        public BaseCooperativeDeed(Serial serial)
            : base(serial)
        {
        }

        public abstract BaseHouse.CooperativeType CooperativeType { get; }

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

        public override void OnDoubleClick(Mobile from)
        {
            if (this.Deleted)
                return;

            // Make sure deed is in pack
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            // Create target and call it
            from.SendMessage("Target the house sign of the Cooperative you wish to join");
            from.Target = new BaseCooperativeDeedTarget(this);
        }
    }

    public class BlacksmithCooperativeDeed : BaseCooperativeDeed
    {
        [Constructable]
        public BlacksmithCooperativeDeed()
            : base()
        {
            Name = "deed to a blacksmith cooperative";
        }

        public BlacksmithCooperativeDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse.CooperativeType CooperativeType { get { return BaseHouse.CooperativeType.Blacksmith; } }

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
    }
}