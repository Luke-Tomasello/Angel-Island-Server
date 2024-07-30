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

namespace Server.Ethics
{
    public class PowerDefinition
    {
        private int m_Power;    // lifeforce in old ethics system
        private int m_Sphere;   // basically the circle to cast

        private TextDefinition m_Name;
        private TextDefinition m_Phrase;
        private TextDefinition m_Description;

        public int Power { get { return m_Power; } }    // lifeforce in old ethics system
        public int Sphere { get { return m_Sphere; } }

        public TextDefinition Name { get { return m_Name; } }
        public TextDefinition Phrase { get { return m_Phrase; } }
        public TextDefinition Description { get { return m_Description; } }

        public PowerDefinition(int power, int sphere, TextDefinition name, TextDefinition phrase, TextDefinition description)
        {
            m_Power = power;
            m_Sphere = sphere;
            m_Name = name;
            m_Phrase = phrase;
            m_Description = description;
        }
    }
}