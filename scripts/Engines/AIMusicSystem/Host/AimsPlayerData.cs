/***************************************************************************
 *
 *                   The Angel Island Music System (AIMS)
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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


/* Scripts\Engines\AIMusicSystem\Host\AimsPlayerData.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 11/5/26)
 *      AimsPlayerData: the per-player music data AimsHost keeps by default (Music Karma, the [FilterMusic setting), for a
 *      shard that doesn't keep them itself. Saved with the world in Saves\AIMS\Players.bin; nothing is written while it
 *      is empty (a shard that keeps them itself, like Angel Island, never fills it).
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace Server
{
    public static class AimsPlayerData
    {
        public class Entry
        {
            public double Karma;
            public bool FilterMusic;
        }

        private static readonly Dictionary<Mobile, Entry> m_Table = new Dictionary<Mobile, Entry>();
        private static readonly string FilePath = Path.Combine("Saves", "AIMS", "Players.bin");

        public static Entry? Get(Mobile m)
        {
            return m != null && m_Table.TryGetValue(m, out Entry? e) ? e : null;
        }

        public static Entry GetOrCreate(Mobile m)
        {
            if (!m_Table.TryGetValue(m, out Entry? e))
                m_Table[m] = e = new Entry();

            return e;
        }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }

        private static void Save(WorldSaveEventArgs e)
        {
            try
            {
                if (m_Table.Count == 0 && !File.Exists(FilePath))
                    return;

                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

                BinaryFileWriter writer = new BinaryFileWriter(FilePath, true);
                writer.Write(0);    // version

                List<KeyValuePair<Mobile, Entry>> live = new List<KeyValuePair<Mobile, Entry>>();
                foreach (KeyValuePair<Mobile, Entry> kvp in m_Table)
                {
                    if (kvp.Key != null && !kvp.Key.Deleted)
                        live.Add(kvp);
                }

                writer.Write(live.Count);
                foreach (KeyValuePair<Mobile, Entry> kvp in live)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value.Karma);
                    writer.Write(kvp.Value.FilterMusic);
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex, "Error writing " + FilePath);
            }
        }

        private static void Load()
        {
            if (!File.Exists(FilePath))
                return;

            try
            {
                using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                {
                    BinaryFileReader reader = new BinaryFileReader(new BinaryReader(fs));
                    int version = reader.ReadInt();

                    switch (version)
                    {
                        case 0:
                            {
                                int count = reader.ReadInt();
                                for (int i = 0; i < count; i++)
                                {
                                    Mobile m = reader.ReadMobile();
                                    Entry e = new Entry { Karma = reader.ReadDouble(), FilterMusic = reader.ReadBool() };
                                    if (m != null && !m.Deleted)
                                        m_Table[m] = e;
                                }
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex, "Error reading " + FilePath);
            }
        }
    }
}
