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

/* Scripts/Items/Special/Holiday/Halloween/CreepyBust.cs
 * CHANGELOG
 *  10/4/23, Yoar
 *      Initial commit.
 */

using System;

namespace Server.Items
{
    [Flipable(0x12CA, 0x12CB)]
    public class CreepyBust : BaseStatue
    {
        private static readonly string[] m_MaleNames = new string[]
            {
                "Allan", "Ambrose", "Auguste", "Burton", "Edgar", "Gillian",
                "Igor", "Poe", "Roderick", "Romero", "Rue", "William",
                "Wilson",
            };

        private static readonly string[] m_FemaleNames = new string[]
            {
                "Agatha", "Camille", "Carrie", "Clarice", "Elvira", "Lilith",
                "Luna", "Onyx", "Raven", "Rosemary", "Rowena", "Rune",
                "Samara",
            };

        private static readonly string[] m_Titles = new string[]
            {
                "archivist", "barber", "butcher", "cobbler", "cook", "hatter",
                "jongleur", "mourner", "potter", "servant", "tanner", "taxidermist",
                "weaver",
            };

        private static string RandomName(bool female)
        {
            return RandomList(female ? m_FemaleNames : m_MaleNames);
        }

        private static string RandomTitle()
        {
            return RandomList(m_Titles);
        }

        private static string RandomList(string[] array)
        {
            if (array.Length == 0)
                return null;

            return array[Utility.Random(array.Length)];
        }

        public override string DefaultName
        {
            get
            {
                if (m_CreepyName != null)
                {
                    if (m_CreepyTitle != null)
                        return String.Format("the creepy bust of {0} the {1}", m_CreepyName, m_CreepyTitle);
                    else
                        return String.Format("the creepy bust of {0}", m_CreepyName);
                }
                else
                {
                    return base.DefaultName;
                }
            }
        }

        private string m_CreepyName;
        private string m_CreepyTitle;

        [CommandProperty(AccessLevel.GameMaster)]
        public string CreepyName
        {
            get { return m_CreepyName; }
            set { m_CreepyName = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string CreepyTitle
        {
            get { return m_CreepyTitle; }
            set { m_CreepyTitle = value; }
        }

        [Constructable]
        public CreepyBust()
            : base(0x12CB)
        {
            Hue = 2419 + Utility.Random(6);
            m_CreepyName = RandomName(Utility.RandomBool());
            m_CreepyTitle = RandomTitle();
        }

        public CreepyBust(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((string)m_CreepyName);
            writer.Write((string)m_CreepyTitle);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_CreepyName = Utility.Intern(reader.ReadString());
            m_CreepyTitle = Utility.Intern(reader.ReadString());
        }
    }
}