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
using System.Xml;

namespace Server.Gumps
{
    public class ParentNode
    {
        private ParentNode m_Parent;
        private object[] m_Children;

        private string m_Name;

        public ParentNode(XmlTextReader xml, ParentNode parent)
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

            if (xml.IsEmptyElement)
            {
                m_Children = new object[0];
            }
            else
            {
                ArrayList children = new ArrayList();

                while (xml.Read() && xml.NodeType == XmlNodeType.Element)
                {
                    if (xml.Name == "child")
                    {
                        ChildNode n = new ChildNode(xml, this);

                        children.Add(n);
                    }
                    else
                    {
                        children.Add(new ParentNode(xml, this));
                    }
                }

                m_Children = children.ToArray();
            }
        }

        public ParentNode Parent
        {
            get
            {
                return m_Parent;
            }
        }

        public object[] Children
        {
            get
            {
                return m_Children;
            }
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
        }
    }
}