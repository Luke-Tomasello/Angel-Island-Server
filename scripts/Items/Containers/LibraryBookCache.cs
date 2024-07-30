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

/* Scripts\Items\Containers\LibraryBookCache.cs
 *	ChangeLog:
 *	10/28/23, Yoar
 *		Initial version.
 *		
 *		Place books inside the library book cache to make them spawn randomly
 *		in library book cases throughout the world.
 */

using Server.SkillHandlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Items
{
    [Flipable(0xA97, 0xA99, 0xA98, 0xA9A, 0xA9B, 0xA9C)]
    public class LibraryBookCache : Container
    {
        public static readonly List<LibraryBookCache> Instances = new List<LibraryBookCache>();
        public static double LibraryDropRate;

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        #region Save/Load

        private const string FilePath = "Saves/LibraryBookCache.xml";

        public static void OnSave(WorldSaveEventArgs e)
        {
            try
            {
                Console.WriteLine("LibraryBookCache Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!String.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("LibraryBookCache");
                writer.WriteAttributeString("version", "0");

                try
                {
                    // version 0

                    writer.WriteElementString("LibraryDropRate", LibraryDropRate.ToString());
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

                Console.WriteLine("LibraryBookCache Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("LibraryBookCache");

                switch (version)
                {
                    case 0:
                        {
                            LibraryDropRate = Double.Parse(reader.ReadElementString("LibraryDropRate"));

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

        public static BaseBook Generate()
        {
            BaseBook source = GetRandom();

            if (source == null)
                return null;

            BaseBook target = Construct(source.GetType());

            if (target == null)
                return null;

            Inscribe.CopyBook(source, target);

            return target;
        }

        public static BaseBook GetRandom()
        {
            int count = 0;

            foreach (LibraryBookCache cache in Instances)
            {
                foreach (Item item in cache.Items)
                {
                    if (item is BaseBook)
                        count++;
                }
            }

            if (count <= 0)
                return null;

            int rnd = Utility.Random(count);

            foreach (LibraryBookCache cache in Instances)
            {
                foreach (Item item in cache.Items)
                {
                    if (item is BaseBook)
                    {
                        if (rnd == 0)
                            return (BaseBook)item;
                        else
                            rnd--;
                    }
                }
            }

            return null;
        }

        private static BaseBook Construct(Type bookType)
        {
            if (!typeof(BaseBook).IsAssignableFrom(bookType))
                return null;

            BaseBook book;

            try
            {
                book = (BaseBook)Activator.CreateInstance(bookType);
            }
            catch
            {
                book = null;
            }

            return book;
        }

        public override double DefaultWeight { get { return 1.0; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public double GlobalLibraryDropRate
        {
            get { return LibraryDropRate; }
            set { LibraryDropRate = value; }
        }

        [Constructable]
        public LibraryBookCache()
            : base(0xA97)
        {
            Movable = false;

            Instances.Add(this);
        }

        public override void OnAfterDelete()
        {
            Instances.Remove(this);
        }

        public LibraryBookCache(Serial serial)
            : base(serial)
        {
            Instances.Add(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt((int)0); // version    
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}