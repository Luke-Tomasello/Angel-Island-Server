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

/*
 * 8/18/22, Adam(Utc)
 * Convert all read/write of times to handle UTC (the server format.)
 *  All DateTime objects are written as UTC.
 *  All DateTime objects are read as UTC and returned as LOCAL.
 *  Why: This insures that a Saves folder moved from production will run correctly on a local (developers) machine.
 *  Example: (old system) The AI server computer runs at UTC, but but time is saved in 'local' format. This works fine as long as the world files are saved and loaded
 *          on the server. But if you then move the world files to a local developer machine running, say Pacific Time, then when you read the world files all dateTime 
 *          objects will be off by 7-8 hours, depending on DST
 *  The above change fixes this problem by ensuring all DateTime objects are always written as UTC and converted to local on read.
 */
using System;
using System.Xml;

namespace Server.Accounting
{
    public class AccountComment
    {
        private string m_AddedBy;
        private string m_Content;
        private DateTime m_LastModified;

        /// <summary>
        /// A string representing who added this comment.
        /// </summary>
        public string AddedBy
        {
            get { return m_AddedBy; }
        }

        /// <summary>
        /// Gets or sets the body of this comment. Setting this value will reset LastModified.
        /// </summary>
        public string Content
        {
            get { return m_Content; }
            set { m_Content = value; m_LastModified = DateTime.UtcNow; }
        }

        /// <summary>
        /// The date and time when this account was last modified -or- the comment creation time, if never modified.
        /// </summary>
        public DateTime LastModified
        {
            get { return m_LastModified; }
        }

        /// <summary>
        /// Constructs a new AccountComment instance.
        /// </summary>
        /// <param name="addedBy">Initial AddedBy value.</param>
        /// <param name="content">Initial Content value.</param>
        public AccountComment(string addedBy, string content)
        {
            m_AddedBy = addedBy;
            m_Content = content;
            m_LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Deserializes an AccountComment instance from an xml element.
        /// </summary>
        /// <param name="node">The XmlElement instance from which to deserialize.</param>
        public AccountComment(XmlElement node)
        {
            m_AddedBy = Accounts.GetAttribute(node, "addedBy", "empty");
            m_LastModified = Accounts.GetDateTime(Accounts.GetAttribute(node, "lastModified"), DateTime.UtcNow);
            m_Content = Accounts.GetText(node, "");
        }

        /// <summary>
        /// Serializes this AccountComment instance to an XmlTextWriter.
        /// </summary>
        /// <param name="xml">The XmlTextWriter instance from which to serialize.</param>
        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("comment");

            xml.WriteAttributeString("addedBy", m_AddedBy);
            xml.WriteAttributeString("lastModified", XmlConvert.ToString(m_LastModified, XmlDateTimeSerializationMode.Utc));

            xml.WriteString(m_Content);

            xml.WriteEndElement();
        }
    }
}