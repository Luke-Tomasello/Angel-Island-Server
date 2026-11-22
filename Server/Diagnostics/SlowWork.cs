/***************************************************************************
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

/* Server/Diagnostics/SlowWork.cs
 * ChangeLog
 *  11/14/26, Adam
 *      Created. What held the main thread up: any piece of main-thread work that takes ThresholdMs or longer is kept
 *      here with when it ended and what it was (a timer callback by its name, as timer profiles name it; a step of
 *      the main loop; a packet, with its ID and who sent it). The last Capacity entries are kept. A hiccup can then be
 *      traced to its cause: the music engine's stall log names the work inside the stall, and [lag lists the last
 *      few minutes. Measuring costs two clock reads per piece of work; a name is only built when the work was slow.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Server.Diagnostics
{
    public static class SlowWork
    {
        /// <summary>Work this long or longer (ms) is recorded.</summary>
        public const double ThresholdMs = 50;

        /// <summary>How many entries are kept (the oldest goes first).</summary>
        public const int Capacity = 100;

        public readonly record struct Entry(DateTime EndedUtc, string What, double Ms);

        private static readonly Entry[] m_Ring = new Entry[Capacity];
        private static int m_Next, m_Count;
        private static readonly object m_Lock = new object();

        /// <summary>The start of a piece of work, for Finish.</summary>
        public static long Start() => Stopwatch.GetTimestamp();

        /// <summary>Milliseconds since Start.</summary>
        public static double Since(long start) => (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;

        /// <summary>Records the work begun at 'start' if it was slow. 'what' is only turned into text then (a timer: its ToString).</summary>
        public static void Finish(long start, object what)
        {
            double ms = Since(start);
            if (ms < ThresholdMs)
                return;
            string name;
            try { name = what?.ToString() ?? "(unnamed)"; }
            catch (Exception ex) { name = string.Format("{0} (name failed: {1})", what?.GetType().Name, ex.Message); }
            Record(name, ms);
        }

        public static void Record(string what, double ms)
        {
            lock (m_Lock)
            {
                m_Ring[m_Next] = new Entry(DateTime.UtcNow, what, ms);
                m_Next = (m_Next + 1) % Capacity;
                if (m_Count < Capacity)
                    ++m_Count;
            }
        }

        /// <summary>The entries that ended at or after 'utc', oldest first.</summary>
        public static List<Entry> EndedSince(DateTime utc)
        {
            List<Entry> list = new List<Entry>();
            lock (m_Lock)
            {
                for (int i = 0; i < m_Count; ++i)
                {
                    Entry e = m_Ring[(m_Next - m_Count + i + Capacity) % Capacity];
                    if (e.EndedUtc >= utc)
                        list.Add(e);
                }
            }
            return list;
        }

        /// <summary>"Timer X 1410 ms at 04:07:27Z, ..." (times UTC), or "none".</summary>
        public static string Describe(IEnumerable<Entry> entries)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Entry e in entries)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.AppendFormat("{0} {1:0} ms at {2:HH:mm:ss}Z", e.What, e.Ms, e.EndedUtc);
            }
            return sb.Length > 0 ? sb.ToString() : "none";
        }

        /// <summary>The slowest of the entries first, at most 'max' of them, for a short report.</summary>
        public static List<Entry> Slowest(IEnumerable<Entry> entries, int max) =>
            entries.OrderByDescending(e => e.Ms).Take(max).ToList();
    }
}
