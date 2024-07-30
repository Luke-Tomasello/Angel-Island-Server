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

/* scripts\Engines\Ethics\Core\EthicConfig.cs
 * CHANGELOG:
 *  1/10/23, Yoar
 *      Initial commit.
 */

using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Ethics
{
    public static class EthicConfig
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        public static int MaxLifeForce = 100;

        #region Save/Load

        private const string FilePath = "Saves/EthicConfig.xml";

        public static void OnSave(WorldSaveEventArgs e)
        {
            try
            {
                Console.WriteLine("EthicConfig Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!String.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("EthicConfig");
                writer.WriteAttributeString("version", "0");

                try
                {
                    // version 0

                    writer.WriteElementString("MaxLifeForce", MaxLifeForce.ToString());
                }
                finally
                {
                    writer.WriteEndDocument();
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        public static void OnLoad()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return;

                Console.WriteLine("EthicConfig Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("EthicConfig");

                switch (version)
                {
                    case 0:
                        {
                            MaxLifeForce = Int32.Parse(reader.ReadElementString("MaxLifeForce"));

                            break;
                        }
                }

                reader.ReadEndElement();

                reader.Close();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        #endregion
    }
}