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

namespace Server.Engines.Craft
{
    public class CraftSubRes
    {
        private Type m_Type;
        private double m_ReqSkill;
        private TextDefinition m_Name;
        private TextDefinition m_GenericName;
        private TextDefinition m_Message;

        public CraftSubRes(Type type, TextDefinition name, double reqSkill, TextDefinition message)
            : this(type, name, reqSkill, null, message)
        {
        }

        public CraftSubRes(Type type, TextDefinition name, double reqSkill, TextDefinition genericName, TextDefinition message)
        {
            m_Type = type;
            m_Name = name;
            m_ReqSkill = reqSkill;
            m_GenericName = genericName;
            m_Message = message;
        }

        public Type ItemType
        {
            get { return m_Type; }
        }

        public TextDefinition Name
        {
            get { return m_Name; }
        }

        public TextDefinition GenericName
        {
            get { return m_GenericName; }
        }

        public TextDefinition Message
        {
            get { return m_Message; }
        }

        public double RequiredSkill
        {
            get { return m_ReqSkill; }
        }
    }
}