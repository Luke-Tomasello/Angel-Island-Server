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

/* Misc\XmlUtility.cs
 * CHANGELOG:
 *	01/13/06, Pix
 *		Needed Utility class for XML.
 *		(TCCS and BountyKeeper systems should be changed to use this class)
 *		Any generic XML functions should be moved here.
 */

using System;
using System.Xml;

namespace Server
{
    /// <summary>
    /// Contains utility functions for working with Xml.
    /// </summary>
    public class XmlUtility
    {
        public XmlUtility()
        {
        }

        public static string GetText(XmlElement node, string defaultValue)
        {
            if (node == null)
                return defaultValue;

            return node.InnerText;
        }

        public static DateTime GetDateTime(string dateTimeString, DateTime defaultValue)
        {
            try
            {
                return XmlConvert.ToDateTime(dateTimeString, XmlDateTimeSerializationMode.Local);
            }
            catch
            {
                try
                {
                    return DateTime.Parse(dateTimeString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        public static int GetInt32(string intString, int defaultValue)
        {
            try
            {
                return XmlConvert.ToInt32(intString);
            }
            catch
            {
                try
                {
                    return Convert.ToInt32(intString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        public static double GetDouble(string dblString, double defaultValue)
        {
            if (dblString == null || dblString.Length == 0)
                return defaultValue;

            // 2/29/2024: Adam: we need to figure out why BountyKeeper is trying to load bad XML
            try
            {
                return XmlConvert.ToDouble(dblString);
            }
            catch
            {
                try
                {
                    double result;
                    if (double.TryParse(dblString, out result))
                        return result;
                    return defaultValue;
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

    }
}