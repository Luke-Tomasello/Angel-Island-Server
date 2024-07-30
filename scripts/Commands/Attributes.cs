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

using System;

namespace Server
{
    public class UsageAttribute : Attribute
    {
        private string m_Usage;

        public string Usage { get { return m_Usage; } }

        public UsageAttribute(string usage)
        {
            m_Usage = usage;
        }
    }

    public class DescriptionAttribute : Attribute
    {
        private string m_Description;

        public string Description { get { return m_Description; } }

        public DescriptionAttribute(string description)
        {
            m_Description = description;
        }
    }

    public class AliasesAttribute : Attribute
    {
        private string[] m_Aliases;

        public string[] Aliases { get { return m_Aliases; } }

        public AliasesAttribute(params string[] aliases)
        {
            m_Aliases = aliases;
        }
    }
}