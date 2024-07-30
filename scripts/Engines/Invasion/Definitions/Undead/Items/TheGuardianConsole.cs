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

/* Scripts/Engines/Invasion/Undead/Items/TheGuardianConsole.cs
 * ChangeLog
 *  10/28/23, Yoar
 *		Initial Version.
 */

using Server.Items;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Engines.Invasion
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class TheGuardianConsole : Item
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        #region Save/Load

        private const string FilePath = "Saves/TheGuardian.xml";

        public static void OnSave(WorldSaveEventArgs e)
        {
            try
            {
                Console.WriteLine("TheGuardian Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!String.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("TheGuardian");
                writer.WriteAttributeString("version", "3");

                try
                {
                    // version 3

                    writer.WriteElementString("UnholyBoneCount", TheGuardianTentacles.UnholyBoneCount.ToString());

                    // version 2

                    writer.WriteElementString("ArtifactsCount", TheGuardian.ArtifactsCount.ToString());

                    // version 0

                    writer.WriteElementString("TentaclesCount", TheGuardian.TentaclesCount.ToString());
                    writer.WriteElementString("ArcSpacing", TheGuardian.ArcSpacing.ToString());
                    writer.WriteElementString("CircleSpacing", TheGuardian.CircleSpacing.ToString());
                    writer.WriteElementString("TeleportRadius", TheGuardian.TeleportRadius.ToString());
                    writer.WriteElementString("TeleportCooldown", TheGuardian.TeleportCooldown.ToString());
                    writer.WriteElementString("GoodiesRadius", TheGuardian.GoodiesRadius.ToString());
                    writer.WriteElementString("GoodiesTotalMin", TheGuardian.GoodiesTotalMin.ToString());
                    writer.WriteElementString("GoodiesTotalMax", TheGuardian.GoodiesTotalMax.ToString());
                    writer.WriteElementString("DemorphDelay", TheGuardian.DemorphDelay.ToString());
                    writer.WriteElementString("DrainCooldown", TheGuardianTentacles.DrainCooldown.ToString());
                    writer.WriteElementString("DrainRadius", TheGuardianTentacles.DrainRadius.ToString());
                    writer.WriteElementString("DrainDamageMin", TheGuardianTentacles.DrainDamageMin.ToString());
                    writer.WriteElementString("DrainDamageMax", TheGuardianTentacles.DrainDamageMax.ToString());
                    writer.WriteElementString("DrainHealScalar", TheGuardianTentacles.DrainHealScalar.ToString());
                    writer.WriteElementString("DamageDistMin", TheGuardianTentacles.DamageDistMin.ToString());
                    writer.WriteElementString("DamageDistMax", TheGuardianTentacles.DamageDistMax.ToString());
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

                Console.WriteLine("TheGuardian Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("TheGuardian");

                switch (version)
                {
                    case 3:
                        {
                            TheGuardianTentacles.UnholyBoneCount = Int32.Parse(reader.ReadElementString("UnholyBoneCount"));

                            goto case 2;
                        }
                    case 2:
                        {
                            TheGuardian.ArtifactsCount = Int32.Parse(reader.ReadElementString("ArtifactsCount"));

                            goto case 1;
                        }
                    case 1:
                    case 0:
                        {
                            if (version < 1)
                                break; // reset defaults

                            TheGuardian.TentaclesCount = Int32.Parse(reader.ReadElementString("TentaclesCount"));
                            TheGuardian.ArcSpacing = Double.Parse(reader.ReadElementString("ArcSpacing"));
                            TheGuardian.CircleSpacing = Int32.Parse(reader.ReadElementString("CircleSpacing"));
                            TheGuardian.TeleportRadius = Int32.Parse(reader.ReadElementString("TeleportRadius"));
                            TheGuardian.TeleportCooldown = Int32.Parse(reader.ReadElementString("TeleportCooldown"));
                            TheGuardian.GoodiesRadius = Int32.Parse(reader.ReadElementString("GoodiesRadius"));
                            TheGuardian.GoodiesTotalMin = Int32.Parse(reader.ReadElementString("GoodiesTotalMin"));
                            TheGuardian.GoodiesTotalMax = Int32.Parse(reader.ReadElementString("GoodiesTotalMax"));
                            TheGuardian.DemorphDelay = TimeSpan.Parse(reader.ReadElementString("DemorphDelay"));
                            TheGuardianTentacles.DrainCooldown = TimeSpan.Parse(reader.ReadElementString("DrainCooldown"));
                            TheGuardianTentacles.DrainRadius = Int32.Parse(reader.ReadElementString("DrainRadius"));
                            TheGuardianTentacles.DrainDamageMin = Int32.Parse(reader.ReadElementString("DrainDamageMin"));
                            TheGuardianTentacles.DrainDamageMax = Int32.Parse(reader.ReadElementString("DrainDamageMax"));
                            TheGuardianTentacles.DrainHealScalar = Int32.Parse(reader.ReadElementString("DrainHealScalar"));
                            TheGuardianTentacles.DamageDistMin = Int32.Parse(reader.ReadElementString("DamageDistMin"));
                            TheGuardianTentacles.DamageDistMax = Int32.Parse(reader.ReadElementString("DamageDistMax"));

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

        [CommandProperty(AccessLevel.GameMaster)]
        public int TentaclesCount
        {
            get { return TheGuardian.TentaclesCount; }
            set { TheGuardian.TentaclesCount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ArcSpacing
        {
            get { return TheGuardian.ArcSpacing; }
            set { TheGuardian.ArcSpacing = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CircleSpacing
        {
            get { return TheGuardian.CircleSpacing; }
            set { TheGuardian.CircleSpacing = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TeleportRadius
        {
            get { return TheGuardian.TeleportRadius; }
            set { TheGuardian.TeleportRadius = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TeleportCooldown
        {
            get { return TheGuardian.TeleportCooldown; }
            set { TheGuardian.TeleportCooldown = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GoodiesRadius
        {
            get { return TheGuardian.GoodiesRadius; }
            set { TheGuardian.GoodiesRadius = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GoodiesTotalMin
        {
            get { return TheGuardian.GoodiesTotalMin; }
            set { TheGuardian.GoodiesTotalMin = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GoodiesTotalMax
        {
            get { return TheGuardian.GoodiesTotalMax; }
            set { TheGuardian.GoodiesTotalMax = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan DemorphDelay
        {
            get { return TheGuardian.DemorphDelay; }
            set { TheGuardian.DemorphDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ArtifactsCount
        {
            get { return TheGuardian.ArtifactsCount; }
            set { TheGuardian.ArtifactsCount = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan DrainCooldown
        {
            get { return TheGuardianTentacles.DrainCooldown; }
            set { TheGuardianTentacles.DrainCooldown = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DrainRadius
        {
            get { return TheGuardianTentacles.DrainRadius; }
            set { TheGuardianTentacles.DrainRadius = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DrainDamageMin
        {
            get { return TheGuardianTentacles.DrainDamageMin; }
            set { TheGuardianTentacles.DrainDamageMin = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DrainDamageMax
        {
            get { return TheGuardianTentacles.DrainDamageMax; }
            set { TheGuardianTentacles.DrainDamageMax = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DrainHealScalar
        {
            get { return TheGuardianTentacles.DrainHealScalar; }
            set { TheGuardianTentacles.DrainHealScalar = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DamageDistMin
        {
            get { return TheGuardianTentacles.DamageDistMin; }
            set { TheGuardianTentacles.DamageDistMin = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DamageDistMax
        {
            get { return TheGuardianTentacles.DamageDistMax; }
            set { TheGuardianTentacles.DamageDistMax = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UnholyBoneCount
        {
            get { return TheGuardianTentacles.UnholyBoneCount; }
            set { TheGuardianTentacles.UnholyBoneCount = value; }
        }

        [Constructable]
        public TheGuardianConsole()
            : base(0x1F14)
        {
            Hue = 1157;
            Name = "The Guardian Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public TheGuardianConsole(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadEncodedInt();
        }
    }
}