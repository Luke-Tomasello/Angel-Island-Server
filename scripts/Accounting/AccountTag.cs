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

namespace Server.Accounting
{
    public class AccountTag
    {
        private string m_Name, m_Value;

        /// <summary>
        /// Gets or sets the name of this tag.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>
        /// Gets or sets the value of this tag.
        /// </summary>
        public string Value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        /// <summary>
        /// Constructs a new AccountTag instance with a specific name and value.
        /// </summary>
        /// <param name="name">Initial name.</param>
        /// <param name="value">Initial value.</param>
        public AccountTag(string name, string value)
        {
            m_Name = name;
            m_Value = value;
        }

        /// <summary>
        /// Deserializes an AccountTag instance from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement instance from which to deserialize.</param>
        public AccountTag(XmlElement node)
        {
            m_Name = Accounts.GetAttribute(node, "name", "empty");
            m_Value = Accounts.GetText(node, "");
        }

        /// <summary>
        /// Serializes this AccountTag instance to an XmlTextWriter.
        /// </summary>
        /// <param name="xml">The XmlTextWriter instance from which to serialize.</param>
        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("tag");
            xml.WriteAttributeString("name", m_Name);
            xml.WriteString(m_Value);
            xml.WriteEndElement();
        }
    }
}