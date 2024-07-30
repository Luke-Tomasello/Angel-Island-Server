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

using Server.Diagnostics;
using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Server
{
    public class NameList
    {
        private string m_Type;
        private string[] m_List;

        public string Type { get { return m_Type; } }
        public string[] List { get { return m_List; } }

        public NameList(string type, XmlElement xml)
        {
            m_Type = type;
            m_List = xml.InnerText.Split(',');
        }

        public string GetRandomName()
        {
            if (m_List.Length > 0)
                return m_List[Utility.Random(m_List.Length)].Trim();

            return "";
        }

        public static NameList GetNameList(string type)
        {
            return (NameList)m_Table[type];
        }

        public static string RandomName(string type)
        {
            NameList list = GetNameList(type);

            if (list != null)
                return list.GetRandomName();

            return "";
        }

        private static Hashtable m_Table;

        static NameList()
        {
            m_Table = new Hashtable(CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);

            string filePath = Path.Combine(Core.DataDirectory, "names.xml");

            if (!File.Exists(filePath))
                return;

            try
            {
                Load(filePath);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Warning: Exception caught loading name lists:");
                Console.WriteLine(e);
            }
        }

        private static void Load(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlElement root = doc["names"];

            foreach (XmlElement element in root.GetElementsByTagName("namelist"))
            {
                string type = element.GetAttribute("type");

                if (type == null || type == String.Empty)
                    continue;

                try
                {
                    NameList list = new NameList(type, element);

                    m_Table[type] = list;
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }
        }
    }
}