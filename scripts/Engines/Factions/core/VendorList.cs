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
    public class VendorList
    {
        private VendorDefinition m_Definition;
        private List<BaseFactionVendor> m_Vendors;

        public VendorDefinition Definition { get { return m_Definition; } }
        public List<BaseFactionVendor> Vendors { get { return m_Vendors; } }

        public BaseFactionVendor Construct(Town town, Faction faction)
        {
            try { return Activator.CreateInstance(m_Definition.Type, new object[] { town, faction }) as BaseFactionVendor; }
            catch { return null; }
        }

        public VendorList(VendorDefinition definition)
        {
            m_Definition = definition;
            m_Vendors = new List<BaseFactionVendor>();
        }
    }
}