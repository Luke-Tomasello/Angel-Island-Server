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

/* Scripts\Misc\XmlTextWriterExtensions.cs
 * ChangeLog :
 *	8/29/21, Liberation
 *		Initial creation.
 */

using System.Xml;

namespace Scripts.Misc
{
    public static class XmlTextWriterExtensions
    {
        public static void WriteCoreXml(this XmlTextWriter xml, string elementName, object property)
        {
            xml.WriteStartElement(elementName);

            if (property is bool boolean)
            {
                xml.WriteString(boolean ? "1" : "0");
            }
            else
            {
                xml.WriteString(property.ToString());
            }

            xml.WriteEndElement();
        }
    }
}