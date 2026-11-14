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

/* Server/Diagnostics/Herald.cs
 * ChangeLog:
 *  11/11/2026, Adam
 *      First version. One place that hears "something worth telling" and decides who is told. Code says what
 *      happened (Interest) and who may hear it (Audience); subscribers (staff alerts now, Discord next) say whether
 *      they want it. The code that publishes never knows who listens.
 *      Audience keeps private text private by design: World = a broadcast everyone in game already sees (safe for
 *      a public Discord channel), Player = one player's private message (never leaves the game), Staff = staff
 *      only (exceptions, console trouble).
 *      Publish is safe from any thread and never throws or blocks: it queues the event. Pump(), called on the
 *      game thread by the subscribers' timer (Scripts/Misc/StaffAlerts.cs), delivers them, so subscribers may use
 *      the game freely. A subscriber that throws is skipped, never thrown back into the publisher.
 *      Flood guard: the same interest and first line again within a minute is counted, not delivered; when the
 *      minute is up one "(repeated N more times in the last minute)" event follows. A broken timer that throws
 *      thousands of times a minute costs one line a minute.
 *      Describe gives an exception's place as file and line, "(Spawner.cs:812)", not the full source path (the
 *      first test's lines ran past 300 characters); Exception.log keeps the full stack.
 *      Interests Announcement and Test, for Discord (Scripts/Misc/DiscordHerald.cs). Death: the realm's death news.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Server.Diagnostics
{
    // what kind of thing happened (add as they're needed)
    public enum Interest
    {
        None,
        Exception,      // LogHelper.LogException
        Conquest,       // a town challenged, fallen, reclaimed (TerritoryMessenger)
        Announcement,   // [announce, and events that announce themselves (DiscordHerald.Announce)
        Test,           // [herald test
        Death,          // "Word reaches the realm: ..." (OnDeathDoppelganger)
        Music,          // a song approved (AngelIslandAimsHost.OnTrackApproved)
    }

    // who may hear it
    public enum Audience
    {
        World,          // a broadcast everyone in game sees
        Player,         // one player's private message: never leaves the game
        Staff,          // staff only
    }

    public sealed class HeraldEvent
    {
        public Interest Interest { get; }
        public Audience Audience { get; }
        public string Text { get; }
        public DateTime When { get; }
        public object? Data { get; }        // the facts behind the text (a town, a mobile), for subscribers that format their own

        public HeraldEvent(Interest interest, Audience audience, string text, object? data)
        {
            Interest = interest;
            Audience = audience;
            Text = text;
            When = DateTime.UtcNow;
            Data = data;
        }
    }

    public interface IHeraldSubscriber
    {
        // called on the game thread
        void Deliver(HeraldEvent e);
    }

    public static class Herald
    {
        public static readonly TimeSpan FloodWindow = TimeSpan.FromMinutes(1);
        private const int MaxQueued = 1000;                 // nobody pumping: drop rather than grow forever

        private static readonly ConcurrentQueue<HeraldEvent> s_Queue = new();
        private static readonly List<IHeraldSubscriber> s_Subscribers = new();
        private static readonly object s_FloodLock = new();
        private static readonly Dictionary<string, FloodEntry> s_Flood = new();

        private sealed class FloodEntry
        {
            public DateTime WindowEnds;
            public int Repeats;
            public Interest Interest;
            public Audience Audience;
            public string Text = "";
        }

        public static void Subscribe(IHeraldSubscriber subscriber)
        {
            lock (s_Subscribers)
            {
                if (!s_Subscribers.Contains(subscriber))
                    s_Subscribers.Add(subscriber);
            }
        }

        // safe from any thread; never throws
        public static void Publish(Interest interest, Audience audience, string text, object? data = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return;

                string key = interest + "|" + FirstLine(text);
                DateTime now = DateTime.UtcNow;
                lock (s_FloodLock)
                {
                    if (s_Flood.TryGetValue(key, out FloodEntry? entry) && now < entry.WindowEnds)
                    {
                        entry.Repeats++;
                        return;
                    }
                    s_Flood[key] = new FloodEntry { WindowEnds = now + FloodWindow, Interest = interest, Audience = audience, Text = FirstLine(text) };
                }

                Enqueue(new HeraldEvent(interest, audience, text, data));
            }
            catch
            {
                // telling must never break the teller
            }
        }

        // game thread: deliver what's queued, and the repeat counts of windows that have closed
        public static void Pump()
        {
            DateTime now = DateTime.UtcNow;
            List<HeraldEvent>? summaries = null;
            lock (s_FloodLock)
            {
                List<string>? expired = null;
                foreach (KeyValuePair<string, FloodEntry> kv in s_Flood)
                {
                    if (now < kv.Value.WindowEnds)
                        continue;
                    (expired ??= new()).Add(kv.Key);
                    if (kv.Value.Repeats > 0)
                        (summaries ??= new()).Add(new HeraldEvent(kv.Value.Interest, kv.Value.Audience,
                            string.Format("{0} (repeated {1} more time{2} in the last minute)", kv.Value.Text, kv.Value.Repeats, kv.Value.Repeats == 1 ? "" : "s"), null));
                }
                if (expired != null)
                    foreach (string key in expired)
                        s_Flood.Remove(key);
            }
            if (summaries != null)
                foreach (HeraldEvent e in summaries)
                    Enqueue(e);

            IHeraldSubscriber[] subscribers;
            lock (s_Subscribers)
                subscribers = s_Subscribers.ToArray();

            while (s_Queue.TryDequeue(out HeraldEvent? e))
            {
                foreach (IHeraldSubscriber s in subscribers)
                {
                    try
                    {
                        s.Deliver(e);
                    }
                    catch (Exception ex)
                    {
                        // not LogException: that would publish again
                        Console.WriteLine("Herald: {0} failed: {1}", s.GetType().Name, ex.Message);
                    }
                }
            }
        }

        // one line for an exception: "Exception: NullReferenceException: <message> at <first frame>"
        public static string Describe(Exception ex, string? context = null)
        {
            string frame = FirstLine(ex.StackTrace ?? "").Trim();
            if (frame.StartsWith("at ", StringComparison.Ordinal))
                frame = frame.Substring(3);

            // " in C:\...\Spawner.cs:line 812" -> " (Spawner.cs:812)": the file and line say where; the full path is in Exception.log
            int at = frame.LastIndexOf(" in ", StringComparison.Ordinal);
            int line = at < 0 ? -1 : frame.LastIndexOf(":line ", StringComparison.Ordinal);
            if (at >= 0 && line > at)
            {
                string file = System.IO.Path.GetFileName(frame.Substring(at + 4, line - at - 4));
                frame = string.Format("{0} ({1}:{2})", frame.Substring(0, at), file, frame.Substring(line + 6));
            }
            string text = string.Format("Exception: {0}{1}: {2}{3}",
                string.IsNullOrWhiteSpace(context) ? "" : FirstLine(context!).Trim() + ": ",
                ex.GetType().Name, FirstLine(ex.Message), frame.Length > 0 ? " at " + frame : "");
            return text.Length > 300 ? text.Substring(0, 297) + "..." : text;
        }

        private static void Enqueue(HeraldEvent e)
        {
            if (s_Queue.Count >= MaxQueued)
                s_Queue.TryDequeue(out _);
            s_Queue.Enqueue(e);
        }

        private static string FirstLine(string text)
        {
            int end = text.IndexOfAny(new[] { '\r', '\n' });
            return end < 0 ? text : text.Substring(0, end);
        }
    }
}
