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


/* Scripts\Engines\AIMusicSystem\Host\AimsUtil.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 11/5/26)
 *      AimsUtil: the general helpers the music system used from Angel Island's Utility, copied so the AIMS folder
 *      builds on any RunUO server: the stable hash (GetStableHashCode V1 and V2, unchanged: every composition hash and
 *      composer key depends on them), Shuffle, LocalTimer, and the Data folder (AimsHost.DataDirectory). For stock
 *      RunUO: LongestCommonSubstring (title matching), AntiSpam, GetDistanceToSqrt, MakeTempSheet, IsUOMusic and
 *      EnsurePath.
 *      GetStableHashCode history (1/27/22, Adam): string.GetHashCode() differs on every run of a .NET Core program, and
 *      the music box needs a deterministic hash.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server
{
    /// <summary>General helpers for the music system.</summary>
    public static class AimsUtil
    {
        #region Hashing
        /// <summary>A hash that is the same on every run. Version 1: compositions; version 2 (default): the rest.</summary>
        public static int GetStableHashCode(string str, int version = 2)
        {
            switch (version)
            {
                case 1: return GetStableHashCodeV1(str);
                case 2: return GetStableHashCodeV2(str);
                default: throw new ArgumentException(string.Format("Version {0} not a supported hash generator", version));
            }
        }

        /*
         * From Stackoverflow: https://stackoverflow.com/questions/36845430/persistent-hashcode-for-strings/36845864#36845864
         */
        public static int GetStableHashCodeV1(string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        /*
         * Based on the Jenkins One At a Time Hash (as implemented and exhaustively tested by Bret Mulvey).
         * https://stackoverflow.com/questions/548158/fixed-length-numeric-hash-code-from-variable-length-string-in-c-sharp
         */
        public static int GetStableHashCodeV2(string s)
        {
            const int MUST_BE_LESS_THAN = 100000000; // 8 decimal digits
            uint hash = 0;
            foreach (byte b in System.Text.Encoding.Unicode.GetBytes(s))
            {
                hash += b;
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }
            // final avalanche
            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);
            // only a positive integer < MUST_BE_LESS_THAN
            return (int)(hash % MUST_BE_LESS_THAN);
        }
        #endregion Hashing

        /// <summary>Fisher-Yates shuffle, in place.</summary>
        public static void Shuffle(IList list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Utility.Random(n + 1);
                object? value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        #region Matching
        /// <summary>
        /// The length of the longest run of characters two strings share: how well spoken words match a song's title
        /// (MusicLibrary.FindEntry). Angel Island used its Intelligent Dialogue System's rolling hash for this.
        /// </summary>
        public static int LongestCommonSubstring(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return 0;

            int best = 0;
            int[] prev = new int[b.Length + 1];
            int[] curr = new int[b.Length + 1];

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    curr[j] = a[i - 1] == b[j - 1] ? prev[j - 1] + 1 : 0;
                    if (curr[j] > best)
                        best = curr[j];
                }

                int[] swap = prev; prev = curr; curr = swap;
                Array.Clear(curr, 0, curr.Length);
            }

            return best;
        }
        #endregion Matching

        #region Messages
        private static readonly Dictionary<string, long> m_AntiSpam = new Dictionary<string, long>();

        /// <summary>True if this key wasn't used in the last <paramref name="seconds"/> (and remembers it now).</summary>
        public static bool AntiSpam(string key, double seconds)
        {
            long now = Core.TickCount;

            if (m_AntiSpam.Count > 1000)
            {
                foreach (string stale in m_AntiSpam.Where(kvp => kvp.Value <= now).Select(kvp => kvp.Key).ToList())
                    m_AntiSpam.Remove(stale);
            }

            if (m_AntiSpam.TryGetValue(key, out long until) && until > now)
                return false;

            m_AntiSpam[key] = now + (long)(seconds * 1000);
            return true;
        }
        #endregion Messages

        #region Items
        /// <summary>The distance from an item (its Location) to a point, as Angel Island's Item.GetDistanceToSqrt.</summary>
        public static double GetDistanceToSqrt(Item item, IPoint2D p)
        {
            int xDelta = item.Location.X - p.X;
            int yDelta = item.Location.Y - p.Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        /// <summary>
        /// A temporary copy of a sheet for playback (a music box plays a copy, never the library's sheet): it plays anywhere,
        /// and the shard is told it's temporary (AimsHost.MarkTemporary). The box deletes it once it can't play again.
        /// </summary>
        public static Misc.RolledUpSheetMusic? MakeTempSheet(Misc.RolledUpSheetMusic rsm)
        {
            if (rsm == null)
                return null;

            if (AimsHost.Current.Dupe(rsm) is not Misc.RolledUpSheetMusic copy)
                return null;

            copy.RequiresBackpack = false;
            AimsHost.Current.MarkTemporary(copy);
            return copy;
        }
        #endregion Items

        /// <summary>Is this the name of one of the game's own music tracks (MusicName)?</summary>
        public static bool IsUOMusic(string enum_name)
        {
            foreach (string name in Enum.GetNames(typeof(MusicName)))
                if (enum_name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        /// <summary>Creates the folder a file goes in, if needed.</summary>
        public static void EnsurePath(string path)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>The shard's Data folder; song files are in its "song files" folder.</summary>
        public static string DataDirectory
        {
            get { return AimsHost.Current.DataDirectory; }
        }

        /// <summary>A millisecond countdown on Core.TickCount: Start, then Triggered once the time is up.</summary>
        public class LocalTimer
        {
            private long m_Timer = long.MaxValue;
            private long m_Timeout = 0;
            public bool Running { get { return m_Timer != long.MaxValue; } }
            public void Start(long millisecond_timeout) { m_Timer = Core.TickCount; m_Timeout = millisecond_timeout; }
            public void Start(double millisecond_timeout) { Start((long)millisecond_timeout); }
            public void Stop() { m_Timer = long.MaxValue; m_Timeout = 0; }
            public bool Triggered { get { return Core.TickCount > m_Timer + m_Timeout; } }
            public long Remaining => (m_Timer + m_Timeout) - Core.TickCount;
            public LocalTimer(long millisecond_timeout)
            {
                Start(millisecond_timeout: millisecond_timeout);
            }
            public LocalTimer(double millisecond_timeout)
            {
                Start((long)millisecond_timeout);
            }
            public LocalTimer()
            {
            }
        }
    }
}
