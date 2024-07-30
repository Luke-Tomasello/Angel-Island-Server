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
    public class EthicDefinition
    {
        private int m_PrimaryHue;

        private TextDefinition m_Title;
        private TextDefinition m_Adjunct;

        private TextDefinition m_JoinPhrase;

        private TextDefinition m_InvokePhrase;

        private Power[] m_Powers;

        public int PrimaryHue { get { return m_PrimaryHue; } }

        public TextDefinition Title { get { return m_Title; } }
        public TextDefinition Adjunct { get { return m_Adjunct; } }

        public TextDefinition JoinPhrase { get { return m_JoinPhrase; } }

        public TextDefinition InvokePhrase { get { return m_InvokePhrase; } }

        public Power[] Powers { get { return m_Powers; } }

        public EthicDefinition(int primaryHue, TextDefinition title, TextDefinition adjunct, TextDefinition joinPhrase, TextDefinition invokePhrase, Power[] powers)
        {
            m_PrimaryHue = primaryHue;

            m_Title = title;
            m_Adjunct = adjunct;

            m_JoinPhrase = joinPhrase;

            m_InvokePhrase = invokePhrase;

            m_Powers = powers;
        }
    }
}