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

/* Scripts/Engines/Craft/Core/CraftRes.cs
 * CHANGELOG
 *  1/8/22, Yoar
 *      Simplified input structure of CraftRes, CraftGroup constructors using TextEntry.
 *      Added CraftRes.Predicate: Check additional property requirements for the resource.
 */

using System;

namespace Server.Engines.Craft
{
    public class CraftRes
    {
        private Type m_Type;
        private int m_Amount;

        private TextDefinition m_Message;
        private TextDefinition m_Name;

        private Predicate<Item> m_Predicate;

        public CraftRes(Type type, TextDefinition name, int amount, TextDefinition message, Predicate<Item> predicate)
        {
            m_Type = type;
            m_Amount = amount;

            m_Message = message;
            m_Name = name;

            m_Predicate = predicate;
        }

        public Type ItemType
        {
            get { return m_Type; }
        }

        public TextDefinition Message
        {
            get { return m_Message; }
        }

        public TextDefinition Name
        {
            get { return m_Name; }
        }

        public int Amount
        {
            get { return m_Amount; }
        }

        public Predicate<Item> Predicate
        {
            get { return m_Predicate; }
        }
    }
}