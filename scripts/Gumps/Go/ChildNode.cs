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

using System.Xml;

namespace Server.Gumps
{
    public class ChildNode
    {
        private ParentNode m_Parent;

        private string m_Name;
        private Point3D m_Location;
        private Map m_Map;

        public ChildNode(XmlTextReader xml, ParentNode parent)
        {
            m_Parent = parent;

            Parse(xml);
        }

        private void Parse(XmlTextReader xml)
        {
            if (xml.MoveToAttribute("name"))
                m_Name = xml.Value;
            else
                m_Name = "empty";

            int x = 0, y = 0, z = 0;

            if (xml.MoveToAttribute("x"))
                x = Utility.ToInt32(xml.Value);

            if (xml.MoveToAttribute("y"))
                y = Utility.ToInt32(xml.Value);

            if (xml.MoveToAttribute("z"))
                z = Utility.ToInt32(xml.Value);

            m_Location = new Point3D(x, y, z);

            if (xml.MoveToAttribute("map"))
                m_Map = Map.Parse(xml.Value);
        }

        public ParentNode Parent
        {
            get
            {
                return m_Parent;
            }
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public Point3D Location
        {
            get
            {
                return m_Location;
            }
        }

        public Map Map
        {
            get
            {
                return m_Map;
            }
        }
    }
}