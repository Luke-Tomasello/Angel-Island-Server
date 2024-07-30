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

/* Scripts\Engines\Alignment\AlignmentConfig.cs
 * Changelog:
 *  4/14/23, Yoar
 *      Initial version.
 */

using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Engines.Alignment
{
    public class AlignmentConfig
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        public static TimeSpan ChangeAlignmentDelay = TimeSpan.FromDays(7.0);
        public static bool StrongholdNotoriety = false;
        public static TimeSpan TraitorCooldown = TimeSpan.FromMinutes(5.0);
        public static TitleDisplay TitleDisplay = (TitleDisplay.Paperdoll | TitleDisplay.GuildSuffix);
        public static bool KillPointsEnabled = false;
        public static TimeSpan KillAwardCooldown = TimeSpan.FromMinutes(2.0);
        public static CreatureAllegiance CreatureAllegiance = CreatureAllegiance.Default;

        #region Save/Load

        private const string FilePath = "Saves/AlignmentConfig.xml";

        public static void OnSave(WorldSaveEventArgs e)
        {
            try
            {
                Console.WriteLine("AlignmentConfig Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!String.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("AlignmentConfig");
                writer.WriteAttributeString("version", "3");

                try
                {
                    // version 3

                    writer.WriteElementString("CreatureAllegiance", CreatureAllegiance.ToString());

                    // version 2

                    writer.WriteElementString("KillPointsEnabled", KillPointsEnabled.ToString());
                    writer.WriteElementString("KillAwardCooldown", KillAwardCooldown.ToString());

                    // version 1
                    // version 0

                    writer.WriteElementString("ChangeAlignmentDelay", ChangeAlignmentDelay.ToString());
                    writer.WriteElementString("StrongholdNotoriety", StrongholdNotoriety.ToString());
                    writer.WriteElementString("TraitorCooldown", TraitorCooldown.ToString());
                    writer.WriteElementString("TitleDisplay", TitleDisplay.ToString());
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

                Console.WriteLine("AlignmentConfig Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("AlignmentConfig");

                switch (version)
                {
                    case 3:
                        {
                            CreatureAllegiance = Enum.Parse<CreatureAllegiance>(reader.ReadElementString("CreatureAllegiance"));

                            goto case 2;
                        }
                    case 2:
                        {
                            KillPointsEnabled = Boolean.Parse(reader.ReadElementString("KillPointsEnabled"));
                            KillAwardCooldown = TimeSpan.Parse(reader.ReadElementString("KillAwardCooldown"));

                            goto case 1;
                        }
                    case 1:
                    case 0:
                        {
                            ChangeAlignmentDelay = TimeSpan.Parse(reader.ReadElementString("ChangeAlignmentDelay"));
                            StrongholdNotoriety = Boolean.Parse(reader.ReadElementString("StrongholdNotoriety"));
                            TraitorCooldown = TimeSpan.Parse(reader.ReadElementString("TraitorCooldown"));
                            TitleDisplay = Enum.Parse<TitleDisplay>(reader.ReadElementString("TitleDisplay"));

                            if (version < 1)
                                TitleDisplay |= TitleDisplay.Paperdoll;

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