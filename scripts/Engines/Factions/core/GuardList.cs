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
using System.Collections.Generic;

namespace Server.Factions
{
    public class GuardList
    {
        private GuardDefinition m_Definition;
        private List<BaseFactionGuard> m_Guards;

        public GuardDefinition Definition { get { return m_Definition; } }
        public List<BaseFactionGuard> Guards { get { return m_Guards; } }

        public BaseFactionGuard Construct()
        {
            try { return Activator.CreateInstance(m_Definition.Type) as BaseFactionGuard; }
            catch { return null; }
        }

        public GuardList(GuardDefinition definition)
        {
            m_Definition = definition;
            m_Guards = new List<BaseFactionGuard>();
        }
    }
}