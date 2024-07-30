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

/* Items/Skill Items/Camping/CampHelper.cs
 * ChangeLog:
 *  11/21/06, plasma
 *      Initial creation      
 */

using Server.Diagnostics;
using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Server.Items
{
    public class CampHelper
    {
        // static array
        private static ArrayList m_RestrictedAreas;

        // allow global list to load / save with world
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        public static void OnLoad()
        {
            // LOAD, GLORIOUS LOAD!
            System.Console.WriteLine("CampHelper Loading...");

            string filePath = Path.Combine("Saves/AngelIsland", "CampHelper.xml");

            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                if (m_RestrictedAreas == null)
                    m_RestrictedAreas = new ArrayList();

                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                XmlElement root = doc["CampHelper"];
                foreach (XmlElement entry in doc.GetElementsByTagName("RestrictedRect"))
                {
                    try
                    {
                        // load in entry!
                        Rectangle2D rect = new Rectangle2D(
                             XmlUtility.GetInt32(XmlUtility.GetText(entry["X"], "0"), 0),
                             XmlUtility.GetInt32(XmlUtility.GetText(entry["Y"], "0"), 0),
                             XmlUtility.GetInt32(XmlUtility.GetText(entry["Width"], "0"), 0),
                             XmlUtility.GetInt32(XmlUtility.GetText(entry["Height"], "0"), 0)
                        );

                        // add to the global restricted rect list
                        m_RestrictedAreas.Add(rect);
                    }
                    catch
                    {
                        Console.WriteLine("Warning: A CampHelper rect entry load failed");
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Exception caught loading CampHelper.xml");
                Console.WriteLine(e.StackTrace);

            }

        }

        public static void OnSave(WorldSaveEventArgs e)
        {
            System.Console.WriteLine("CampHelper Saving...");
            if (m_RestrictedAreas == null)
                return;

            try
            {
                if (!Directory.Exists("Saves/AngelIsland"))
                    Directory.CreateDirectory("Saves/AngelIsland");

                string filePath = Path.Combine("Saves/AngelIsland", "CampHelper.xml");

                using (StreamWriter op = new StreamWriter(filePath))
                {
                    XmlTextWriter xml = new XmlTextWriter(op);

                    xml.Formatting = Formatting.Indented;
                    xml.IndentChar = '\t';
                    xml.Indentation = 1;

                    xml.WriteStartDocument(true);
                    xml.WriteStartElement("CampHelp");
                    // just to be complete..
                    xml.WriteAttributeString("Count", m_RestrictedAreas.Count.ToString());
                    // Now write each entry
                    foreach (Rectangle2D rect in m_RestrictedAreas)
                    {
                        xml.WriteStartElement("RestrictedRect");
                        xml.WriteStartElement("X");
                        xml.WriteString(rect.X.ToString());
                        xml.WriteEndElement();
                        xml.WriteStartElement("Y");
                        xml.WriteString(rect.Y.ToString());
                        xml.WriteEndElement();
                        xml.WriteStartElement("Width");
                        xml.WriteString(rect.Width.ToString());
                        xml.WriteEndElement();
                        xml.WriteStartElement("Height");
                        xml.WriteString(rect.Height.ToString());
                        xml.WriteEndElement();
                        xml.WriteEndElement();
                    }
                    //and end doc
                    xml.WriteEndElement();

                    xml.Close();
                }

            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                System.Console.WriteLine("Error in CampHelper.OnSave(): " + ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Adds a Rectangle2D into the no camping zone array
        /// </summary>        
        public static bool AddRestrictedArea(Rectangle2D rect)
        {
            // prevention rather than crash!
            if (m_RestrictedAreas == null)
                m_RestrictedAreas = new ArrayList();

            // is this already in the list?
            if (IsRestrictedArea(rect))
                return false;

            // add to the list
            m_RestrictedAreas.Add(rect);

            return true;

        }
        /// <summary>
        /// Removes a Rectangle2D from the no camping zone array
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static bool RemoveRestrictedArea(Rectangle2D rect)
        {
            if (m_RestrictedAreas == null)
                return false;

            // is this NOT in the list?
            if (!IsRestrictedArea(rect))
                return false;

            // remove from the list
            m_RestrictedAreas.Remove(rect);

            return true;
        }

        /// <summary>
        /// Checks if a given Mobile is within a no camp xone
        /// </summary>        
        public static bool InRestrictedArea(Mobile from)
        {
            return InRestrictedArea(new Point2D(from.X, from.Y));
        }

        /// <summary>
        /// Checks if a given Point2D is within a no camp xone
        /// </summary>        
        public static bool InRestrictedArea(Point2D point)
        {
            if (m_RestrictedAreas == null)
                return false;

            foreach (Rectangle2D rect in m_RestrictedAreas)
            {
                // cycle through each rect and compare 
                if ((point.X >= rect.X) && (point.X <= rect.X + rect.Width) && (point.Y >= rect.Y) && (point.Y <= rect.Y + rect.Height))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a given Rectangle2D is a no camp zone
        /// </summary>        
        public static bool IsRestrictedArea(Rectangle2D rect)
        {
            // check if the given rectangle is in the list
            if (m_RestrictedAreas == null)
                m_RestrictedAreas = new ArrayList();

            for (int i = 0; i < m_RestrictedAreas.Count; ++i)
            {
                // cycle through each rect and compare 
                if (rect.Equals((Rectangle2D)m_RestrictedAreas[i]))
                    return true;
            }

            return false;
        }

    }
}