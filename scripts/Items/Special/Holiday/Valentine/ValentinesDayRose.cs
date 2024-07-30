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

/* Scripts\Items\Special\Holiday\Valentine\ValentinesDayRose.cs
 * ChangeLog:
 * 12/18/06 Adam
 *		Initial Creation 	
 */

using Server.Misc;
using Server.Prompts;
using System.Text.RegularExpressions;

namespace Server.Items
{
    public class ValentinesDayRose : Item // Create the item class which is derived from the base item class
    {
        private bool m_personalized = false;
        public bool Personalized
        {
            get { return m_personalized; }
            set { m_personalized = value; }
        }

        [Constructable]
        public ValentinesDayRose()
            // ugly, but an easy way to code 'you get the cheap red roses way more often than the neat-o purple ones'
            : base(Utility.RandomList(9035, 9036, 9037, 6377, 6378, 6377, 6378, 6377, 6378, 6377, 6378, 6377, 6378, 6377, 6378))
        {
            Name = "a rose";
            Weight = 1.0;
            Hue = 0;
            LootType = LootType.Newbied;
        }

        public ValentinesDayRose(Serial serial)
            : base(serial)
        {
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
            if (Personalized == true)
                from.SendMessage("That rose has already been inscribed.");
            else
            {
                from.SendMessage("What dost thou wish to inscribe?");
                from.Prompt = new RenamePrompt(from, this);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(m_personalized);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_personalized = reader.ReadBool();
        }

        private class RenamePrompt : Prompt
        {
            private Mobile m_from;
            ValentinesDayRose m_rose;

            public RenamePrompt(Mobile from, ValentinesDayRose rose)
            {
                m_from = from;
                m_rose = rose;
            }

            public override void OnResponse(Mobile from, string text)
            {
                char[] exceptions = new char[] { ' ', '-', '.', '\'', ':', ',' };
                Regex InvalidPatt = new Regex("[^-a-zA-Z0-9':, ]");
                if (InvalidPatt.IsMatch(text))
                {
                    // Invalid chars
                    from.SendMessage("You may only use numbers, letters, apostrophes, hyphens, colons, commas, and spaces in the inscription.");

                }
                else if (!NameVerification.Validate(text, 2, 32, true, true, true, 4, exceptions, NameVerification.BuildList(true, true, false, true)))
                {
                    // Invalid for some other reason
                    from.SendMessage("That inscription is not allowed here.");
                }
                else
                {
                    m_rose.Name = text;
                    m_rose.Personalized = true;
                    from.SendMessage("Thou hast successfully inscribed thy rose.");
                }
            }
        }
    }
}