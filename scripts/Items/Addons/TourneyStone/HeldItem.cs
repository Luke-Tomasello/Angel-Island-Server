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

/* Scripts/Items/Addons/TourneyStone/HeldItem.cs
 * ChangeLog :
 * 	02/25/07, weaver
 *		Initial creation (code moved out of TournamentStoneAddon.cs)
 *
 */


using System;
using System.Collections;

namespace Server.Items
{
    public class HeldItem
    {
        // What is this type?
        public Type m_Type;

        // How many items of this type?
        public int m_Count;

        // Reference to the objects themselves
        private ArrayList m_Ref;
        public ArrayList Ref { get { return m_Ref; } set { m_Ref = value; } }

        public HeldItem(Type type)
        {
            m_Count = 1;
            m_Type = type;
            m_Ref = new ArrayList();
        }
    }
}