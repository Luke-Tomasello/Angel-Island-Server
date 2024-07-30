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

/* Scripts/Items/Special/Holiday/Winter/LightOfTheWinterSolstice.cs
 * Changelog:
 *	11/27/21, Yoar
 *		Initial version.
 */

using System;

namespace Server.Items
{
    [FlipableAttribute(0x236E, 0x2371)]
    public class LightOfTheWinterSolstice : Item, IHolidayItem
    {
        public override int LabelNumber { get { return 1070875; } } // Light of the Winter Solstice

        private int m_Year;
        private string m_Dipper;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Year { get { return m_Year; } set { m_Year = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Dipper { get { return m_Dipper; } set { m_Dipper = value; InvalidateProperties(); } }

        [Constructable]
        public LightOfTheWinterSolstice() : this(2004, NameList.RandomName("staff"))
        {
        }

        [Constructable]
        public LightOfTheWinterSolstice(int year, string dipper) : base(0x236E)
        {
            m_Year = year;
            m_Dipper = dipper;

            Weight = 1.0;
            Light = LightType.Circle300;
            Hue = Utility.RandomDyedHue();
        }

        public LightOfTheWinterSolstice(Serial serial) : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, 1070881, m_Dipper); // Hand Dipped by ~1_name~

            if (m_Year == 2004)
                LabelTo(from, 1070880); // Winter 2004
            else
                LabelTo(from, "Winter {0}", m_Year);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1070881, m_Dipper); // Hand Dipped by ~1_name~

            if (m_Year == 2004)
                list.Add(1070880); // Winter 2004
            else
                list.Add("Winter {0}", m_Year);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.WriteEncodedInt(m_Year);
            writer.Write((string)m_Dipper);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Year = reader.ReadEncodedInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Dipper = reader.ReadString();
                        break;
                    }
                case 0:
                    {
                        m_Dipper = NameList.RandomName("staff");
                        break;
                    }
            }

            if (version < 2)
                m_Year = 2004;

            if (m_Dipper != null)
                m_Dipper = String.Intern(m_Dipper);
        }
    }
}