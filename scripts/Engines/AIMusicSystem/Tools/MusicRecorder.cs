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

/* Scripts\Engines\Music\MusicRecorder.cs
 * Changelog
 *  11/6/26, Adam (2026 modernization, 10/27/26 to 11/6/26)
 *      Recordings carry '# Timer: <mode>' beside '# NewTimer:' (kept for older builds). Recording into a book keeps its
 *      size (at least MaxPages); a recording longer than the book is cut off with a message.
 *      Portable AIMS: the recorder hears speech itself (EventSink.Speech; it was a line in PlayerMobile.OnSaid), keeps
 *      its own copy of the book font widths (Angel Island's FontHelper, which began here), and logs a runaway recording
 *      through AimsHost.WriteLog. On stock RunUO it fills the book's own pages (a stock book can't grow); Angel Island's
 *      books still grow to MaxPages (#if ANGEL_ISLAND). Recordings are unchanged. Targeting anything but a book in reach
 *      asks again (it lost the recording); cancel discards it.
 *  2/25/22, Adam
 *      Add support for ConcertMode
 *  2/20/22, Adam
 *		Initial creation.
 */

using Server.Commands;
using Server.Items;
using Server.Misc;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Server.Engines
{
    public class MusicRecorderObject
    {
        public List<KeyValuePair<string, double>> Music;
        public Stopwatch Timer;
        public TimeSpan Last;
        public MusicRecorderObject(Mobile m)
        {
            Music = new List<KeyValuePair<string, double>>();
            if (!SystemMusicPlayer.MusicConfig.ContainsKey(m))   // use player configuration
                SystemMusicPlayer.ConfigurePlayer(m);            // if no configuration, use defaults

            // initialize our recorded music with default header info. this will get updated should the user change anything during the recording
            Music.Add(new KeyValuePair<string, double>(string.Format("# Instrument: {0}", SystemMusicPlayer.MusicConfig[m].Instrument.ToString()), 0));
            Music.Add(new KeyValuePair<string, double>(string.Format("# Prefetch: {0}", SystemMusicPlayer.MusicConfig[m].Prefetch.ToString()), 0));
            Music.Add(new KeyValuePair<string, double>(string.Format("# NewTimer: {0}", SystemMusicPlayer.MusicConfig[m].NewTimer.ToString()), 0));
            Music.Add(new KeyValuePair<string, double>(string.Format("# Timer: {0}", SystemMusicPlayer.MusicConfig[m].TimerMode.ToString()), 0));
            Music.Add(new KeyValuePair<string, double>(string.Format("# Tempo: {0}", SystemMusicPlayer.MusicConfig[m].Tempo.ToString()), 0));
            Music.Add(new KeyValuePair<string, double>(string.Format("# ConcertMode: {0}", SystemMusicPlayer.MusicConfig[m].ConcertMode.ToString()), 0));
            // add Author
            // add Song

            Timer = new Stopwatch();
            Last = TimeSpan.Zero;
        }
    }
    public static class MusicRecorder
    {
        // Mobile, text, elapsed time
        private static readonly Dictionary<Mobile, MusicRecorderObject> MusicRecorderBuffer = new Dictionary<Mobile, MusicRecorderObject>();

        public static void Start(Mobile m)
        {
            if (MusicRecorderBuffer.ContainsKey(m))
            {
                if (MusicRecorderBuffer[m].Timer.IsRunning)
                    MusicRecorderBuffer[m].Timer.Stop();
                MusicRecorderBuffer[m].Timer = null;
                MusicRecorderBuffer.Remove(m);
            }

            // create a new record and start the timer.
            MusicRecorderBuffer.Add(m, new MusicRecorderObject(m));

        }
        public static void Stop(Mobile m)
        {
            if (MusicRecorderBuffer.ContainsKey(m))
            {
                m.SendMessage("Target the music composition book to copy to...");
                m.Target = new LocationTarget(MusicRecorderBuffer[m].Music);

                // debug
                /*foreach (KeyValuePair<string, double> kvp in MusicRecorderBuffer[m].Music)
                    Console.WriteLine(kvp.Key + ": " + kvp.Value.ToString());*/

                if (MusicRecorderBuffer[m].Timer.IsRunning)
                    MusicRecorderBuffer[m].Timer.Stop();
                MusicRecorderBuffer[m].Timer = null;
                MusicRecorderBuffer.Remove(m);
            }
        }
        public static void Record(Mobile m, string text)
        {
            if (MusicRecorderBuffer.ContainsKey(m))
            {
                TimeSpan delta = NewTime(m);
                MusicRecorderBuffer[m].Music.Add(new KeyValuePair<string, double>(text, delta.TotalMilliseconds));

                // exploit check
                if (MusicRecorderBuffer[m].Music.Count > 1024)
                //if (MusicRecorderBuffer[m].Music.Count > 100)
                {   // log it: who, and the last ten entries
                    List<KeyValuePair<string, double>> music = MusicRecorderBuffer[m].Music;
                    AimsHost.Current.WriteLog("MusicRecorder.log", m, music.Skip(music.Count - 10).Cast<object>());

                    // kill it
                    MusicRecorderBuffer[m].Music.Clear();
                    if (MusicRecorderBuffer[m].Timer.IsRunning)
                        MusicRecorderBuffer[m].Timer.Stop();
                    MusicRecorderBuffer.Remove(m);
                }
            }
        }
        public static TimeSpan NewTime(Mobile m)
        {
            if (!MusicRecorderBuffer.ContainsKey(m))
                return TimeSpan.Zero;

            if (MusicRecorderBuffer[m].Timer.IsRunning == false)
            {   // initialize and start timer
                MusicRecorderBuffer[m].Timer.Start();
            }

            TimeSpan delta = MusicRecorderBuffer[m].Timer.Elapsed - MusicRecorderBuffer[m].Last;
            MusicRecorderBuffer[m].Last = MusicRecorderBuffer[m].Timer.Elapsed;

            return delta;
        }
        public static void Record(Mobile m)
        {   // we are recording AND the user has a music context, and it's being updated
            if (MusicRecorderBuffer.ContainsKey(m) && SystemMusicPlayer.MusicConfig.ContainsKey(m))
            {
                List<KeyValuePair<string, double>> list = new List<KeyValuePair<string, double>>();
                foreach (KeyValuePair<string, double> kvp in MusicRecorderBuffer[m].Music)
                {
                    if (
                        kvp.Key.ToLower().StartsWith("# Instrument:".ToLower()) ||
                        kvp.Key.ToLower().StartsWith("# Prefetch:".ToLower()) ||
                        kvp.Key.ToLower().StartsWith("# NewTimer:".ToLower()) ||
                        kvp.Key.ToLower().StartsWith("# Timer:".ToLower()) ||
                        kvp.Key.ToLower().StartsWith("# Tempo:".ToLower()) ||
                        kvp.Key.ToLower().StartsWith("# ConcertMode:".ToLower())
                        )
                        list.Add(new KeyValuePair<string, double>(kvp.Key, kvp.Value));
                }
                // now update for the new player config
                foreach (KeyValuePair<string, double> kvp in list)
                {
                    int index = MusicRecorderBuffer[m].Music.FindIndex(s => s.Key == kvp.Key);
                    if (index != -1)
                    {
                        if (kvp.Key.ToLower().StartsWith("# Instrument:".ToLower()))
                            MusicRecorderBuffer[m].Music[index] = new KeyValuePair<string, double>(string.Format("# Instrument: {0}", SystemMusicPlayer.MusicConfig[m].Instrument.ToString()), 0);
                        if (kvp.Key.ToLower().StartsWith("# Prefetch:".ToLower()))
                            MusicRecorderBuffer[m].Music[index] = new KeyValuePair<string, double>(string.Format("# Prefetch: {0}", SystemMusicPlayer.MusicConfig[m].Prefetch.ToString()), 0);
                        if (kvp.Key.ToLower().StartsWith("# NewTimer:".ToLower()))
                            MusicRecorderBuffer[m].Music[index] = new KeyValuePair<string, double>(string.Format("# NewTimer: {0}", SystemMusicPlayer.MusicConfig[m].NewTimer.ToString()), 0);
                        if (kvp.Key.ToLower().StartsWith("# Timer:".ToLower()))
                            MusicRecorderBuffer[m].Music[index] = new KeyValuePair<string, double>(string.Format("# Timer: {0}", SystemMusicPlayer.MusicConfig[m].TimerMode.ToString()), 0);
                        if (kvp.Key.ToLower().StartsWith("# Tempo:".ToLower()))
                            MusicRecorderBuffer[m].Music[index] = new KeyValuePair<string, double>(string.Format("# Tempo: {0}", SystemMusicPlayer.MusicConfig[m].Tempo.ToString()), 0);
                        if (kvp.Key.ToLower().StartsWith("# ConcertMode:".ToLower()))
                            MusicRecorderBuffer[m].Music[index] = new KeyValuePair<string, double>(string.Format("# ConcertMode: {0}", SystemMusicPlayer.MusicConfig[m].ConcertMode.ToString()), 0);
                        // add Author
                        // add Song
                    }
                }
            }
        }
        public class LocationTarget : Target
        {
            private List<KeyValuePair<string, double>> m_music;
            private bool m_done;                    // the recording was written into a book, or discarded (cancel)
            public LocationTarget(List<KeyValuePair<string, double>> music)
                : base(2, false, TargetFlags.None)
            {
                m_music = music;
            }
            // anything but a book in reach (another item, out of range, ...) asks again: the recording is kept until it is
            //  written or the player cancels
            protected override void OnTargetFinish(Mobile from)
            {
                if (m_done || from.Deleted)
                    return;

                from.SendMessage("Target a music composition book to copy the recording to, or cancel to discard it.");
                from.Target = new LocationTarget(m_music);
            }
            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                m_done = true;
                from.SendMessage("The recording was discarded.");
            }
            protected override void OnTarget(Mobile from, object target)
            {
                if (target is not MusicCompositionBook)
                    from.SendMessage("That is not a music composition book.");

                if (target is MusicCompositionBook mcb)
                {
                    m_done = true;
                    {
                        // the book keeps its size (at least a full MaxPages book), and the recording fills it no further:
                        //  a bigger book would crash the client (MusicCompositionBook.MaxPages)
#if ANGEL_ISLAND
                        int pages = Math.Max(mcb.PagesCount, MusicCompositionBook.MaxPages);
#else
                        int pages = mcb.PagesCount;                     // a stock book can't grow (no AddPage)
#endif
                        int room = pages * 8;
                        List<string> book = new List<string>();
                        bool cut = false;
                        void Add(string line)
                        {
                            if (book.Count < room) book.Add(line);
                            else cut = true;
                        }

                        #region fixup pauses
                        // in the following example:
                        //  [play a b c d
                        //  wait 300
                        //  [play f a c e
                        // the 'wait 300' is recorded and attached to '[play f a c e', since that's when the recorder recognized the delay.
                        // this is correct, however we need to place the pause *before*  '[play f a c e' and not after it.
                        // we therefore, shift all pauses *up* to the previous statement
                        for (int ix = 0; ix < m_music.Count - 1; ix++)
                            m_music[ix] = new KeyValuePair<string, double>(m_music[ix].Key, m_music[ix + 1].Value);

                        m_music[m_music.Count - 1] = new KeyValuePair<string, double>(m_music[m_music.Count - 1].Key, 0);
                        #endregion fixup pauses

                        foreach (KeyValuePair<string, double> kvp in m_music)
                        {
                            object[] lines = FormatLines(kvp.Key, kvp.Value);
                            string[] chunks = (lines[0] as string).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            List<string> list = BookLines(chunks);
                            foreach (string line in list)
                                // f'ing CUO (0.1.9.4) blows chunks if you add these ("♪") to a book
                                // Try pasting this into a CUO book
                                // ♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪♪
                                // Then right click to close the book. *crash* .. at least my version of CUO
                                // Note: You can add one, example: " say '♪ I haven't seen the' ", but the text will be munged: " say '♪ I haven't seethe' "
                                // Note: CUO 0.1.10.168 seems to fix this bug
                                Add(line.Replace("♪", "!"));

                            // now add the 'wait' if there is one
                            if (lines[1] != null)
                                // output the 'wait'
                                Add(lines[1] as string);
                        }

                        // the rest of the book stays blank, for edits (it used to shrink to the recording and two pages)
#if ANGEL_ISLAND
                        mcb.ClearPages();
                        foreach (string line in book)
                            mcb.AddLine(line);
                        while (mcb.PagesCount < pages)
                            mcb.AddPage(new BookPageInfo());
#else
                        for (int p = 0; p < pages; p++)
                            mcb.Pages[p].Lines = book.Skip(p * 8).Take(8).ToArray();
#endif

                        if (cut)
                            from.SendMessage(33, "The recording is longer than the book ({0} pages): the rest was not written.", pages);
                    }
                }
            }
            private List<string> BookLines(string[] chunks)
            {   // smallest character is 3 pixels. You can fit 52 on a line
                //  52*3 = 156
                const int BookLinePixels = 156;
                List<string> bookLines = new List<string>();

                // truncate comments, don't wrap them
                if (chunks[0][0] == '#')
                {   // rebuild the comment
                    string temp = string.Empty;
                    foreach (string line in chunks)
                        temp += line + " ";

                    // start trimming comment until it fits
                    while (RecorderFont.Width(temp) > BookLinePixels)
                        temp = temp.TrimEnd(temp[temp.Length - 1]);

                    bookLines.Add(temp.Trim()); // <== trim here ok (truncated)
                    return bookLines;
                }
                // wrap music and text
                else
                {
                    string temp = string.Empty;
                    foreach (string chunk in chunks)
                    {
                        // see if the next chunk will fit on the line
                        if (RecorderFont.Width(temp) + RecorderFont.Width(chunk) + RecorderFont.Width(' ') >= BookLinePixels)
                        {
                            bookLines.Add(temp);
                            temp = chunk + " ";
                        }
                        else
                            temp += chunk + " ";
                    }
                    if (temp.Length > 0)
                        bookLines.Add(temp); // <== don't trim here (wrapped)

                    return bookLines;
                }
            }
            private object[] FormatLines(string text, double pause)
            {   // need the Environment.NewLine appended for inconsistent CUO line break weirdness
                object[] lines = new object[2];
                lines[1] = null;
                if (text.StartsWith('#'))                                                   // comment
                {
                    lines[0] = string.Format("{0}", text.Trim() + Environment.NewLine);
                    return lines;                                                           // no pauses after a comment
                }
                else if (LooksLikeMusic(text))
                    lines[0] = string.Format("say '[play {0}'", text.Trim() + Environment.NewLine);               // music
                else
                    lines[0] = string.Format("say '{0}'", text.Trim() + Environment.NewLine);                     // lyrics

                // now output the wait
                //  for now, just don't output <100ms waits, because I would otherwise always add them, and the likelihood of
                //  a user using them is small (and they can always add them after the book is created.)
                //if (pause > 0)
                if (pause >= 100)
                {
                    if (pause > 100)
                    {
                        // if the razor script said something like 1500 ms pause, we likely have something like 1586.1234
                        // we will round down to the nearest 100 and trim off the fraction.
                        pause = ((int)pause / 100) * 100;
                    }
                    else
                        pause = (int)pause;

                    lines[1] = string.Format("wait {0}", pause.ToString() + Environment.NewLine);                            // Wait
                }

                return lines;
            }
            private bool LooksLikeMusic(string musicString)
            {
                musicString = musicString.Replace("+", "").Replace("-", "").Trim();
                string[] notes = musicString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (string note in notes)
                {
                    double dmy;
                    // if not a note and not a pause, it's not music.
                    if (!SystemMusicPlayer.validNotes.Contains(note) && !double.TryParse(note, out dmy) && !SystemMusicPlayer.IsVolume(note))
                        return false;
                }

                return true;
            }
        }
        public static void Initialize()
        {
            CommandSystem.Register("MusicRecorder", AccessLevel.Player, new CommandEventHandler(MusicRecorder_OnCommand));
            EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
        }

        // what a recording player says (a Razor script's lyrics, or typed): commands never get here (Mobile.DoSpeech
        //  handles them first), and [play is recorded where it plays (SystemMusicPlayer). Lyrics a sheet or instrument
        //  speaks are recorded in RazorInstrument.
        private static void EventSink_Speech(SpeechEventArgs e)
        {
            if (e.Mobile != null)
                Record(e.Mobile, e.Speech);
        }

        // the book font's character widths, in pixels, for wrapping a recording's lines. Began in this file; Yoar moved it to
        //  its own (Scripts\Misc\FontHelper.cs, 8/5/23), which other Angel Island code uses. This copy keeps the recorder
        //  portable and its books unchanged.
        private static class RecorderFont
        {
            private static readonly Dictionary<char, int> m_Widths = new Dictionary<char, int>
            {
                ['i'] = 3, ['l'] = 3, ['m'] = 9, ['w'] = 9, ['E'] = 7, ['F'] = 7, ['I'] = 3, ['L'] = 7, ['M'] = 10, ['Q'] = 9,
                ['T'] = 7, ['W'] = 12, ['Y'] = 9, [' '] = 8, ['.'] = 3, [','] = 3, ['\''] = 3, ['!'] = 3, ['?'] = 7, [':'] = 3,
                [';'] = 3, ['/'] = 9, ['1'] = 4,
            };

            public static int Width(char c)
            {
                if (m_Widths.TryGetValue(c, out int width))
                    return width;
                else if (c >= 'A' && c <= 'Z')
                    return 8;
                else if (c >= '0' && c <= '9')
                    return 8;
                else if ((int)c > 127)
                    return 16; // example '♪' is 15 pixels. We'll play it safe with 16.
                else
                    return 6;
            }

            public static int Width(string str)
            {
                if (str == null)
                    return 0;

                int width = 0;
                for (int i = 0; i < str.Length; i++)
                    width += Width(str[i]);

                return width;
            }
        }
        public static void MusicRecorder_OnCommand(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
            {
                e.Mobile.SendMessage("Usage [MusicRecorder start|stop");
                return;
            }
            if (e.ArgString.ToLower() == "start")
            {
                MusicRecorder.Start(e.Mobile);
                e.Mobile.SendMessage("MusicRecorder started.");
            }
            else if (e.ArgString.ToLower() == "stop")
            {
                MusicRecorder.Stop(e.Mobile);
                e.Mobile.SendMessage("MusicRecorder stopped.");
            }
        }
    }
}