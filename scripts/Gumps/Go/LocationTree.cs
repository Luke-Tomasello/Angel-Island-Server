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

using System.Collections;
using System.IO;
using System.Xml;

namespace Server.Gumps
{
    public class LocationTree
    {
        private Map m_Map;
        private ParentNode m_Root;
        private Hashtable m_LastBranch;

        public LocationTree(string fileName, Map map)
        {
            m_LastBranch = new Hashtable();
            m_Map = map;

            string path = Path.Combine(Core.DataDirectory, "Locations", fileName);

            if (File.Exists(path))
            {
                XmlTextReader xml = new XmlTextReader(new StreamReader(path));

                xml.WhitespaceHandling = WhitespaceHandling.None;

                m_Root = Parse(xml);

                xml.Close();
            }
        }

        public Hashtable LastBranch
        {
            get
            {
                return m_LastBranch;
            }
        }

        public Map Map
        {
            get
            {
                return m_Map;
            }
        }

        public ParentNode Root
        {
            get
            {
                return m_Root;
            }
        }

        private ParentNode Parse(XmlTextReader xml)
        {
            xml.Read();
            xml.Read();
            xml.Read();

            return new ParentNode(xml, null);
        }
    }
}