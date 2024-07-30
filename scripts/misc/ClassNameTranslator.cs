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

/* Misc/ClassNameTranslator.cs
 * ChangeLog :
 *	7/20/05, erlein
 *		Initial creation.
 */

using Server.Accounting;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace Server.Misc
{
    // Class that handles object type -> string conversions via
    // dynamically loaded and disposed of part and label strings
    // (stored in xml file Data/ClassNameTranslator)

    public class ClassNameTranslator
    {
        private string[] m_NamePart;
        private string[] m_LabelPart;

        private int m_Count;

        public ClassNameTranslator()
        {
            LoadTranslator();
        }

        public string TranslateClass(Type ObjType)
        {
            string sType = ObjType.Name;
            string sApproxLabel = "";

            if (sType.Length > 1)
            {
                Regex UCMatch = new Regex("[A-Z]");
                sApproxLabel = sType[0].ToString();

                for (int napos = 1; napos < ObjType.Name.Length; napos++)
                    sApproxLabel += (UCMatch.IsMatch(sType[napos].ToString()) ? " " : "") + sType[napos];

                sApproxLabel = sApproxLabel.ToLower();
            }

            for (int i = 0; i < m_Count; i++)
                sApproxLabel = sApproxLabel.Replace(m_NamePart[i], m_LabelPart[i]);

            return sApproxLabel;
        }

        public void LoadTranslator()
        {
            XmlDocument xdoc = new XmlDocument();

            string filePath = Path.Combine(Core.DataDirectory, "ClassTranslator.xml");
            if (!File.Exists(filePath))
            {
                Console.WriteLine("ClassTranslator::LoadTranslator() - ClassTranslator.xml not found");
                throw (new FileNotFoundException());
            }

            xdoc.Load(filePath);
            XmlElement root = xdoc["ClassTranslator"];

            int count = Convert.ToInt32(Accounts.GetAttribute(root, "count", "0"));

            m_NamePart = new string[count];
            m_LabelPart = new string[count];

            int cur_trans = 0;

            foreach (XmlElement node in root.GetElementsByTagName("Translate"))
            {
                try
                {
                    m_NamePart[cur_trans] = Accounts.GetText(node["NamePart"], "");
                    m_LabelPart[cur_trans] = Accounts.GetText(node["LabelPart"], "");
                }
                catch (Exception e)
                {
                    Console.WriteLine("ClassNameTranslator::LoadTranslator : Exception reading XML - {0}", e);
                }

                cur_trans++;
            }

            m_Count = cur_trans;
        }
    }
}