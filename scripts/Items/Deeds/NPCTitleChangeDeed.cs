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

/* Items/Deeds/NPCTitleChangeDeed.cs
 * CHANGELOG:
 *  11/3/2021, Adam
 *      Add support for HouseSitters
 *  04/05/06 Taran Kain
 *		Made to work on PlayerBarkeepers as well.
 *  3/29/06 Taran Kain
 *		Cleaned up code, fixed typos.
 *	3/27/05, Kitaras	
 *		 Fixed problem with Item Class declaration not exsisting in 
 *		 server.items namespace.
 *	3/26/05, Adam
 *		Weave in the new NameVerification.BuildList() function to construct
 *		A custom list of allowed names. We disallow all the usual, but allow 
 *		the standard shopkeeper type titles.
 *  3/24/05, Kit
 *		updated messages to reflect title vs name
 *		changed to updateing vendor.title vs name property
 *  3/23/05, Kitaras
 *		Initial Creation
 */

using Server.Misc;
using Server.Mobiles;
using Server.Prompts;
using Server.Targeting;
using System.Text.RegularExpressions;

namespace Server.Items
{
    public class NpcTitleChangeDeed : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public NpcTitleChangeDeed()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "an npc title change deed";
        }

        public NpcTitleChangeDeed(Serial serial)
            : base(serial)
        {
        }

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
            // Make sure deed is in pack
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            // Create target and call it
            from.SendMessage("Whose title dost thou wish to change?");
            from.Target = new NpcTitleChangeDeedTarget(this);
        }

    }
    public class NpcTitleChangeDeedTarget : Target // Create our targeting class (which we derive from the base target class)
    {
        private NpcTitleChangeDeed m_Deed;

        public NpcTitleChangeDeedTarget(NpcTitleChangeDeed deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target)
        {

            if (target is PlayerVendor)
            {
                PlayerVendor vendor = (PlayerVendor)target;
                if (vendor.IsOwner(from))
                {
                    from.SendMessage("How dost thou wish to title thy servant?");
                    from.Prompt = new RenamePrompt(from, vendor, m_Deed);
                }

                else
                {
                    vendor.SayTo(from, "I do not work for thee! Only my master may change my title.");
                }
            }
            else if (target is HouseSitter)
            {
                HouseSitter vendor = (HouseSitter)target;
                if (vendor.IsOwner(from))
                {
                    from.SendMessage("How dost thou wish to title thy servant?");
                    from.Prompt = new RenamePrompt(from, vendor, m_Deed);
                }

                else
                {
                    vendor.SayTo(from, "I do not work for thee! Only my master may change my title.");
                }
            }
            else if (target is PlayerBarkeeper)
            {
                PlayerBarkeeper barkeep = (PlayerBarkeeper)target;
                if (barkeep.IsOwner(from))
                {
                    from.SendMessage("How dost thou wish to title thy servant?");
                    from.Prompt = new RenamePrompt(from, barkeep, m_Deed);
                }
                else
                {
                    barkeep.SayTo(from, "I do not work for thee! Only my master may change my title.");
                }
            }
            else if (target is ITownshipNPC tsnpc)
            {
                Mobile m = target as Mobile;
                if (TownshipNPCHelper.IsOwner(m, from))
                {
                    from.SendMessage("How dost thou wish to title thy servant?");
                    from.Prompt = new RenamePrompt(from, m, m_Deed);
                }
                else
                {
                    m.SayTo(from, "I do not work for thee! Only my master may change my title.");
                }
            }
            else
            {
                from.SendMessage("Thou canst only change the titles of thy servants.");
            }
        }


        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            private NpcTitleChangeDeed m_deed;
            private Mobile vendor;

            public RenamePrompt(Mobile from, Mobile target, NpcTitleChangeDeed deed)
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
                    from.SendMessage("That title is not allowed here.");
                }
                else
                {
                    vendor.Title = text;
                    from.SendMessage("Thou hast successfully changed thy servant's title.");
                    m_deed.Delete();
                }
            }
        }
    }
}