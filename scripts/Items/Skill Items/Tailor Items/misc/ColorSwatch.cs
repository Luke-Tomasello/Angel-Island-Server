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

/* Scripts/Items/Skill Items/Tailor Items/Misc/ColorSwatch.cs
 * ChangeLog:
 *	10/16/05, erlein
 *		Added Dupe() function override so swatch stacks work correctly.
 *	10/15/05, erlein
 *		Moved most of functional code to SpecialDyeTub for new dye tub based craft model.
 *	10/15/05, erlein
 *		Initial creation.
 */

using System;

namespace Server.Items
{
    public class ColorSwatch : Item
    {
        private string m_StoredColorName;

        [CommandProperty(AccessLevel.GameMaster)]
        public string StoredColorName
        {
            get
            {
                return m_StoredColorName;
            }
            set
            {
                m_StoredColorName = value;
            }
        }

        [Constructable]
        public ColorSwatch()
            : this(1)
        {
        }

        [Constructable]
        public ColorSwatch(int amount)
            : base(0x175D)
        {
            Stackable = true;
            Weight = 0.1;
            Amount = amount;
            Name = "a color swatch";
        }

        public ColorSwatch(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new ColorSwatch(), amount);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_StoredColorName);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_StoredColorName = reader.ReadString();
        }

        public override void OnSingleClick(Mobile from)
        {
            // Say what colour it is

            if (String.IsNullOrEmpty(m_StoredColorName))
                from.SendMessage("This swatch has not yet been soaked in any dye.");
            else
                from.SendMessage("You examine your swatch and note it is " + m_StoredColorName.ToLower() + ".");

            base.OnSingleClick(from);
        }
    }
}