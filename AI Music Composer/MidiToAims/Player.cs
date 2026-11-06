/***************************************************************************
 *
 *  MidiToAims preview - hear a composition on this computer as the game plays it, with the player's own client sounds
 *  (ClientSounds), before pasting it into a book ([play preview in the game). MidiToAims play (Line) - a [play line
 *  typed on the command line, as in the game: notes, or a setting (instrument, tempo, timer, newtimer, prefetch) that
 *  is remembered for the next lines (MidiToAims.ini, as the game keeps the player's music context); config, reset.
 *
 *  The song: its .rzr pieces joined (as the books are), read as manuscript paper reads a book (blank lines dropped,
 *  'loop' removed, a line the book wrapped joined back: ManuscriptPaper.PreProcesss), and compiled (sections, plays,
 *  repeats: MusicCompiler, the shard's own source, linked). Its directives as the sheet music takes them
 *  (ManuscriptPaper.ProcessDirectives; '# Timer:' as RazorInstrument reads it).
 *
 *  Then the performance, worked out in advance on an exact clock:
 *  - the script (RazorInstrument): say '[play ...' queues its notes, another say is shown as text, wait N waits N ms;
 *  - the engine (SystemMusicPlayer.PlaySession, DynamicTimer and ClassicTimer): a [play line's first word may be a
 *    setting (instrument, tempo, prefetch, timer, newtimer); otherwise every word must be a note, a sound (+NNN), an
 *    instrument offset (-e, ++c), a volume (v15) or a pause, or the game ignores the line from that word on.
 *    Dynamic and Anchored: a note waits for its pause (Prefetch, or a negative pause) or else the tempo; a run starts
 *    a tempo after its line arrives, and an Anchored one continues where the last run ended when its line arrives
 *    within 100 ms of that (ReanchorMs). Classic: one element per 100 ms tick, a pause holding to the tick after it.
 *    (The shard's timers are late now and then; Dynamic about 10% slow. Here every timer is exactly on time.)
 *  - the client (ClassicUO Sound.Play): a sound played again within its lockout (about half its length: 0.2 s for the
 *    harps, 0.15 s for the lute) is skipped; played later while still sounding, it starts over.
 *  Sound effects (+NNN) play here whatever the composer's music karma (making sheet music from them needs 1000, except
 *  percussion: Percussion.Sounds).
 *  Volume (v0-v20) only matters in concert mode; here every sound plays at full volume.
 *
 *  Plays through Windows (winmm PlaySound) from memory; nothing is written. While it plays, a status line rewritten
 *  in place (\r): the time in the song of its length, and the line playing (book1.post2 line 140); Ctrl+C says where
 *  it stopped. --from / --to play part of the song: the whole performance is worked out (directives, tempo, the
 *  client's skips), then the part is cut out of it, so it sounds as it does in the song.
 *
 *  MidiToAims sound (Sounds): the client's sound effects one after another, each with its number and name, to choose
 *  one for a song (+81): by number, range, part of the name, or 'percussion' (the sounds free of music karma). Each
 *  says whether it is free or needs 1000 music karma.
 *
 ***************************************************************************/

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Server.Items;

namespace MidiToAims
{
    public static class Player
    {
        // CoreMusicPlayer.NoteSounds: each note's sound on the harp, lap harp and lute
        private static readonly string[] s_notes = { "cl", "csl", "d", "ds", "e", "f", "fs", "g", "gs", "a", "as", "b", "c", "cs", "dh", "dsh", "eh", "fh", "fsh", "gh", "gsh", "ah", "ash", "bh", "ch" };
        private static readonly int[][] s_noteSounds =
        {
            new[] { 1181, 976, 1028 }, new[] { 1184, 979, 1031 }, new[] { 1186, 981, 1033 }, new[] { 1188, 983, 1036 },
            new[] { 1190, 985, 1038 }, new[] { 1192, 987, 1040 }, new[] { 1194, 989, 1042 }, new[] { 1196, 991, 1044 },
            new[] { 1198, 993, 1046 }, new[] { 1175, 970, 1021 }, new[] { 1177, 972, 1023 }, new[] { 1179, 974, 1025 },
            new[] { 1182, 977, 1029 }, new[] { 1185, 980, 1032 }, new[] { 1187, 982, 1034 }, new[] { 1189, 984, 1037 },
            new[] { 1191, 986, 1039 }, new[] { 1193, 988, 1041 }, new[] { 1195, 990, 1043 }, new[] { 1197, 992, 1045 },
            new[] { 1199, 994, 1047 }, new[] { 1176, 971, 1022 }, new[] { 1178, 973, 1024 }, new[] { 1180, 975, 1026 },
            new[] { 1183, 978, 1030 },
        };
        private static readonly string[] s_instruments = { "harp", "lap harp", "lute" };
        private const int DefaultTempo = 100;           // SystemMusicPlayer.DefaultTempo
        private const double ReanchorMs = 100;          // SystemMusicPlayer.ReanchorMs
        private const int MaxSoundId = 0x5CF;           // CoreMusicPlayer: +NNN from 0 to 1487

        private enum Timer { Classic, Dynamic, Anchored }
        private sealed record Instrument(int Index);    // a queued instrument change ([play instrument lute)

        // a line of the compiled song, and where it is written (null: a song with sections, its lines rearranged)
        private sealed record Origin(string File, int Line)
        {
            public override string ToString() => string.Format("{0} line {1}", Path.GetFileName(File), Line);
        }

        private sealed record Sound(double Ms, int Id, string Name, Origin? Where);

        #region The song
        // the song's text, read as manuscript paper reads a book, with where each line came from
        private static (string Text, List<Origin> Origins) Read(IReadOnlyList<string> files)
        {
            List<(string Text, Origin Where)> lines = new();
            foreach (string file in files)
            {
                string[] raw = File.ReadAllText(file).Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < raw.Length; i++)
                    if (raw[i].Trim().Length > 0)                                                   // NormalizeString
                        lines.Add((Regex.Replace(raw[i], "#*loop", "", RegexOptions.IgnoreCase), new Origin(file, i + 1)));  // RemoveLoop
            }

            // TrueLineParser: a line that does not start with #, "wait " or "say " is the end of the one above it
            List<(StringBuilder Text, Origin Where)> joined = new();
            foreach (var (text, where) in lines)
            {
                string lower = text.ToLowerInvariant();
                if (joined.Count > 0 && !lower.StartsWith("#") && !lower.StartsWith("wait ") && !lower.StartsWith("say "))
                    joined[^1].Text.Append(text);
                else
                    joined.Add((new StringBuilder(text), where));
            }
            return (string.Join("\r\n", joined.Select(j => j.Text.ToString())) + "\r\n", joined.Select(j => j.Where).ToList());
        }

        // ManuscriptPaper.ProcessDirectives (each line: the first of these words it contains), and '# Timer:'
        private sealed class Directives
        {
            public string Song = "", Author = "", Instrument = "";
            public bool NewTimer, Prefetch, ConcertMode;
            public int Tempo = DefaultTempo;
            public Timer? TimerHeader;
            public Timer Timer => TimerHeader ?? (NewTimer ? Timer.Dynamic : Timer.Classic);

            public Directives(string source)
            {
                foreach (string line in source.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
                {
                    string lower = line.ToLowerInvariant();
                    string Value(string key) => Regex.Replace(line.Replace("#", ""), Regex.Escape(key), "", RegexOptions.IgnoreCase).Trim();
                    bool True(string key) => Regex.IsMatch(Value(key), "true", RegexOptions.IgnoreCase);

                    string directive = lower.TrimStart('#', ' ', '\t');
                    if (directive.StartsWith("timer:"))
                    {
                        string v = directive.Substring("timer:".Length).Trim();
                        if (!int.TryParse(v, out _) && Enum.TryParse(v, true, out Timer t) && Enum.IsDefined(t))
                            TimerHeader = t;
                    }

                    if (lower.Contains("author:")) Author = Value("author:");
                    else if (lower.Contains("napster:")) { }
                    else if (lower.Contains("prefetch:")) Prefetch = True("prefetch:");
                    else if (lower.Contains("concertmode:")) ConcertMode = True("concertmode:");
                    else if (lower.Contains("newtimer:")) NewTimer = True("newtimer:");
                    else if (lower.Contains("tempo:")) Tempo = int.TryParse(Regex.Match(Value("tempo:"), @"\d+").Value, out int n) ? n : DefaultTempo;
                    else if (lower.Contains("instrument:")) Instrument = Value("instrument:");
                    else if (lower.Contains("price:")) { }
                    else if (lower.Contains("song:")) Song = Value("song:");
                }
            }
        }
        #endregion The song

        #region The performance
        private static bool IsNote(string t) => Array.IndexOf(s_notes, t) >= 0;

        private static bool IsSound(string t) => t.Length >= 2 && t[0] == '+' && t.Skip(1).All(char.IsDigit);

        private static bool IsVolume(string t) => (t.Length == 2 || t.Length == 3) && t[0] == 'v' && t.Skip(1).All(char.IsDigit);

        // SystemMusicPlayer.IsSelector: + or - (up to two) then a note; the offset from the main instrument
        private static bool IsSelector(string t, out int offset)
        {
            offset = 0;
            if (t.Length < 2 || (t[0] != '+' && t[0] != '-'))
                return false;
            int plus = 0, minus = 0, letters = 0;
            foreach (char c in t)
            {
                if (char.IsLower(c)) letters++;
                else if (c == '+' && letters == 0 && minus == 0) plus++;
                else if (c == '-' && letters == 0 && plus == 0) minus++;
                else return false;
            }
            offset = plus - minus;
            return plus <= 2 && minus <= 2 && letters <= 3 && offset != 0;
        }

        private static int InstrumentIndex(string? name) => (name ?? "").Trim().ToLowerInvariant() switch
        {
            "lapharp" => 1,
            "lute" => 2,
            _ => 0,
        };

        private sealed class Performance
        {
            public readonly List<Sound> Sounds = new();
            public readonly List<(double Ms, string Text)> Says = new();
            public readonly List<string> Problems = new();
            public double End;
        }

        // the performance, on an exact clock (see the header)
        private static Performance Perform(string text, List<Origin>? origins, Directives d, string unnamed = "the song")
        {
            Performance p = new();
            string[] lines = text.Split("\r\n", StringSplitOptions.None);

            // the session (MusicContext)
            Timer timer = d.Timer;
            int tempo = d.Tempo, selector = InstrumentIndex(d.Instrument);
            bool prefetch = d.Prefetch;
            Queue<(object Item, Origin? Where)> queue = new();

            // the engine: a run of the timer, its tempo and prefetch fixed when it starts
            bool playing = false, classic = false;
            double next = 0, anchorNext = double.NaN, pauseUntil = 0;
            int runTempo = 0;
            bool runPrefetch = false;

            void Start(double now)
            {
                if (playing || queue.Count == 0)
                    return;
                playing = true;
                runTempo = tempo;
                runPrefetch = prefetch;
                classic = timer == Timer.Classic;
                if (classic)
                {
                    next = now;
                    pauseUntil = now;
                }
                else if (timer == Timer.Anchored && !double.IsNaN(anchorNext) && now - anchorNext <= ReanchorMs)
                    next = Math.Max(anchorNext, now);
                else
                    next = now + runTempo;
            }

            void Play(double now, string token, int instrument, Origin? where)
            {
                if (IsSound(token))
                {
                    if (int.TryParse(token.Substring(1), out int id) && id >= 0 && id <= MaxSoundId)
                        p.Sounds.Add(new Sound(now, id, token, where));
                    else
                        p.Problems.Add(string.Format("{0}: '{1}' is not a sound (0 to {2})", where?.ToString() ?? unnamed, token, MaxSoundId));
                    return;
                }
                int n = Array.IndexOf(s_notes, token);
                p.Sounds.Add(new Sound(now, s_noteSounds[Math.Max(0, n)][instrument], token + " on the " + s_instruments[instrument], where));
            }

            // one element of the queue at the engine's time; the delay to the next
            void Step()
            {
                double now = next;
                if (classic && now < pauseUntil)
                {
                    next = now + 100;
                    return;
                }
                while (true)
                {
                    if (queue.Count == 0)
                    {   // the run is over (Anchored: the next may continue it)
                        playing = false;
                        anchorNext = now;
                        p.End = Math.Max(p.End, now);
                        return;
                    }
                    var (item, where) = queue.Dequeue();
                    int instrument = selector;
                    double delay = classic ? 100 : runTempo;

                    if (item is Instrument change)
                    {
                        selector = change.Index;
                        continue;
                    }
                    if (item is string s && IsVolume(s))
                        continue;
                    if (item is string sel && IsSelector(sel, out int offset))
                    {
                        int target = selector + offset;
                        if (target < 0 || target > 2)
                        {
                            p.Problems.Add(string.Format("{0}: '{1}' - there is no instrument there (harp, lap harp, lute, from the {2})", where?.ToString() ?? unnamed, sel, s_instruments[selector]));
                            next = now + delay;
                            return;
                        }
                        instrument = target;
                        item = sel.Replace("+", "").Replace("-", "");
                        if (!IsNote((string)item))
                        {
                            p.Problems.Add(string.Format("{0}: '{1}' is not a note", where?.ToString() ?? unnamed, sel));
                            next = now + delay;
                            return;
                        }
                    }

                    if (item is string token)
                    {
                        Play(now, token, instrument, where);
                        if (!classic && queue.Count > 0 && queue.Peek().Item is double pause && (runPrefetch || pause < 0))
                        {
                            queue.Dequeue();
                            delay = Math.Abs(pause) * 1000;
                        }
                    }
                    else if (item is double pause)
                    {
                        if (classic)
                            pauseUntil = now + pause * 1000;
                        else
                            delay = Math.Max(0, pause * 1000);
                    }
                    next = now + delay;
                    return;
                }
            }

            // a [play line arrives (SystemMusicPlayer.PlaySession)
            void PlayLine(double now, string line, Origin? where)
            {
                string song = Regex.Replace(Regex.Replace(line, Regex.Escape("[play"), "", RegexOptions.IgnoreCase), "say", "", RegexOptions.IgnoreCase).Replace("'", "").Trim();
                string[] words = song.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                    return;

                // a setting (DoConfigure): the whole line is the setting
                string command = words[0].ToLowerInvariant();
                string? arg = words.Length == 2 ? words[1] : null;
                switch (command)
                {
                    case "instrument":
                        if (arg != null && (arg.ToLowerInvariant() is "harp" or "lapharp" or "lute"))
                            queue.Enqueue((new Instrument(InstrumentIndex(arg)), where));
                        return;
                    case "tempo":
                        if (int.TryParse(arg, out int t)) tempo = t;
                        return;
                    case "prefetch":
                        if (bool.TryParse(arg, out bool pf)) prefetch = pf;
                        return;
                    case "newtimer":
                        if (bool.TryParse(arg, out bool nt)) timer = nt ? Timer.Dynamic : Timer.Classic;
                        return;
                    case "timer":
                        if (arg != null && !int.TryParse(arg, out _) && Enum.TryParse(arg, true, out Timer tm) && Enum.IsDefined(tm)) timer = tm;
                        return;
                    case "chatter": case "volume": case "concertmode": case "reset": case "config": case "inspect":
                    case "macro": case "list": case "musickarma": case "test":
                        return;
                }

                foreach (string word in words)
                {
                    string w = word.ToLowerInvariant();
                    if (IsNote(w) || IsSound(w) || IsSelector(w, out _) || IsVolume(w))
                        queue.Enqueue((w, where));
                    else if (double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out double pause))
                    {
                        if (timer == Timer.Classic && !(pause >= 0 && pause <= 1))
                        {
                            p.Problems.Add(string.Format("{0}: the pause {1} is more than the Classic timer takes (0 to 1): the game ignores the line from there", where?.ToString() ?? unnamed, word));
                            return;
                        }
                        queue.Enqueue((pause, where));
                    }
                    else
                    {
                        p.Problems.Add(string.Format("{0}: '{1}' is not a note, a pause or a setting: the game ignores the line from there", where?.ToString() ?? unnamed, word));
                        return;
                    }
                }
                Start(now);
            }

            // the script, with the engine running beside it
            double script = 0;
            int at = 0;
            Regex waitNumber = new(@"^[0-9]*(?:\.[0-9]*)?$");
            while (at < lines.Length || playing)
            {
                if (playing && (at >= lines.Length || next < script))
                {
                    Step();
                    continue;
                }

                string line = lines[at].Trim();
                Origin? where = origins != null && at < origins.Count ? origins[at] : null;
                at++;
                string lower = line.ToLowerInvariant();
                if (lower.StartsWith("#"))
                    continue;
                if (lower.StartsWith("say '[play"))
                    PlayLine(script, line, where);
                else if (lower.StartsWith("say"))
                {
                    string said = Regex.Replace(line, "say ", "", RegexOptions.IgnoreCase);
                    if (said.Length >= 2 && said[0] == '\'' && said[^1] == '\'')
                        said = said.Substring(1, said.Length - 2);
                    p.Says.Add((script, said));
                }
                else if (lower.StartsWith("wait"))
                {
                    string w = Regex.Replace(line, "wait", "", RegexOptions.IgnoreCase).Trim();
                    if (waitNumber.IsMatch(w) && double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out double ms) && ms > 0)
                        script += ms;
                }
            }
            p.End = Math.Max(p.End, script);
            return p;
        }
        #endregion The performance

        #region The waits
        // a wait that doesn't match its block: Written (the wait's ms) and Plays (how long the block's notes take)
        private sealed record WaitMismatch(Origin Where, double Written, double Plays);

        // the waits that don't match their blocks (the [play lines since the wait before), by more than WaitSlackMs.
        //  A block plays as long as the engine takes over its queue (Cost); a wait shorter than that queues the next
        //  block's notes behind it (and its lyrics come early), a longer one leaves a silence. Blocks without notes (a
        //  wait for an intro, between lyrics) are rests, not checked. Only on the Anchored timer, which keeps exact
        //  time: the Dynamic timer runs about 10% slow in the game, and its composers set their waits by ear for that
        //  (Galois's 76 songs: 57 have waits that don't match the written length, all right in the game); waits fitted
        //  to the written length would be short there, and the lyrics would drift ahead. text, origins: the song as
        //  read (Read), not compiled: the waits as written, where they are written.
        private const double WaitSlackMs = 5;
        private static List<WaitMismatch> CheckWaits(string text, List<Origin> origins, Directives d)
        {
            List<WaitMismatch> found = new();
            string[] lines = text.Split("\r\n", StringSplitOptions.None);
            Timer timer = d.Timer;
            int tempo = d.Tempo;
            bool prefetch = d.Prefetch;
            List<(object Item, Timer Timer, int Tempo, bool Prefetch)> block = new();
            Regex waitNumber = new(@"^[0-9]*(?:\.[0-9]*)?$");
            for (int at = 0; at < lines.Length; at++)
            {
                string line = lines[at].Trim(), lower = line.ToLowerInvariant();
                if (lower.StartsWith("say '[play"))
                {
                    string song = Regex.Replace(Regex.Replace(line, Regex.Escape("[play"), "", RegexOptions.IgnoreCase), "say", "", RegexOptions.IgnoreCase).Replace("'", "").Trim();
                    string[] words = song.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length == 0)
                        continue;
                    string? arg = words.Length == 2 ? words[1] : null;
                    switch (words[0].ToLowerInvariant())
                    {   // a setting (PlayLine): the whole line is the setting
                        case "tempo":
                            if (int.TryParse(arg, out int t)) tempo = t;
                            continue;
                        case "prefetch":
                            if (bool.TryParse(arg, out bool pf)) prefetch = pf;
                            continue;
                        case "newtimer":
                            if (bool.TryParse(arg, out bool nt)) timer = nt ? Timer.Dynamic : Timer.Classic;
                            continue;
                        case "timer":
                            if (arg != null && !int.TryParse(arg, out _) && Enum.TryParse(arg, true, out Timer tm) && Enum.IsDefined(tm)) timer = tm;
                            continue;
                        case "instrument": case "chatter": case "volume": case "concertmode": case "reset": case "config": case "inspect":
                        case "macro": case "list": case "musickarma": case "test":
                            continue;
                    }
                    foreach (string word in words)
                    {
                        string w = word.ToLowerInvariant();
                        if (IsNote(w) || IsSound(w) || IsSelector(w, out _))
                            block.Add((w, timer, tempo, prefetch));
                        else if (IsVolume(w))
                            continue;
                        else if (double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out double pause)
                            && !(timer == Timer.Classic && !(pause >= 0 && pause <= 1)))
                            block.Add((pause, timer, tempo, prefetch));
                        else
                            break;                                          // the game ignores the line from here
                    }
                }
                else if (lower.StartsWith("wait"))
                {
                    string w = Regex.Replace(line, "wait", "", RegexOptions.IgnoreCase).Trim();
                    if (timer == Timer.Anchored && waitNumber.IsMatch(w) && double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out double ms) && ms > 0
                        && block.Any(b => b.Item is string) && at < origins.Count)
                    {
                        double plays = Cost(block);
                        if (Math.Abs(plays - ms) > WaitSlackMs)
                            found.Add(new WaitMismatch(origins[at], ms, plays));
                    }
                    block.Clear();
                }
            }
            return found;
        }

        // how long the engine takes over a block's queue (Step): Dynamic and Anchored, a note waits for the pause after
        //  it (Prefetch, or a negative pause) or else the tempo, a pause its length; Classic, a 100 ms tick each, a pause
        //  holding to the first tick at or after its end. Ms, rounded to a µs (0.3 * 1000 is a hair over 300)
        private static double Cost(List<(object Item, Timer Timer, int Tempo, bool Prefetch)> block)
        {
            double ms = 0;
            for (int i = 0; i < block.Count; i++)
            {
                var (item, timer, tempo, prefetch) = block[i];
                if (timer == Timer.Classic)
                    ms += item is double pause ? 100 * Math.Max(1, Math.Ceiling(Math.Round(pause * 1000, 3) / 100)) : 100;
                else if (item is double pause)
                    ms += Math.Max(0, Math.Round(pause * 1000, 3));
                else if (i + 1 < block.Count && block[i + 1].Item is double next && (prefetch || next < 0))
                {
                    ms += Math.Round(Math.Abs(next) * 1000, 3);
                    i++;
                }
                else
                    ms += tempo;
            }
            return ms;
        }

        // the waits' report: one line, or (details) each wait
        private static void ReportWaits(List<WaitMismatch> waits, bool details)
        {
            if (waits.Count == 0)
                return;
            if (!details)
            {
                Console.WriteLine("{0} match how long {1} block{2} play{3}: waits <song> fixes {4}; --check lists {4}.",
                    waits.Count == 1 ? "1 wait doesn't" : waits.Count + " waits don't", waits.Count == 1 ? "its" : "their", waits.Count == 1 ? "" : "s", waits.Count == 1 ? "s" : "", waits.Count == 1 ? "it" : "them");
                return;
            }
            Console.WriteLine("{0} match how long {1} block{2} play{3} (waits <song> fixes {4}):",
                waits.Count == 1 ? "1 wait doesn't" : waits.Count + " waits don't", waits.Count == 1 ? "its" : "their", waits.Count == 1 ? "" : "s", waits.Count == 1 ? "s" : "", waits.Count == 1 ? "it" : "them");
            foreach (WaitMismatch m in waits.Take(12))
                Console.WriteLine("  {0}: {1}", Short(m.Where), Mismatch(m));
            if (waits.Count > 12)
                Console.WriteLine("  ... and {0} more.", waits.Count - 12);
        }

        private static string Mismatch(WaitMismatch m) => string.Format("wait {0}, but its block plays {1:0.000} s: {2}", m.Written, m.Plays / 1000,
            m.Written < m.Plays ? "the next block's notes wait in line, and its lyrics come early"
            : string.Format("{0:0.000} s of silence after it", (m.Written - m.Plays) / 1000));

        // MidiToAims waits: sets each wait that doesn't match its block to the block's length, in the song's .rzr files
        //  (each changed file kept first as .rzr.bak); nothing else in them changes
        public static int FixWaits(IReadOnlyList<string> files)
        {
            var (text, origins) = Read(files);
            MusicCompiler.Result compiled = MusicCompiler.Compile(text, origins.Select(o => o.ToString()).ToList());
            Directives d = new(compiled.Success ? compiled.Source : text);
            if (d.Timer != Timer.Anchored)
            {
                Console.WriteLine("This song uses the {0} timer, whose waits are set by ear (the game plays it a little slower than written). waits fits them to the Anchored timer: put # Timer: Anchored in the song to use it.", d.Timer);
                return 1;
            }
            List<WaitMismatch> waits = CheckWaits(text, origins, d);
            if (waits.Count == 0)
            {
                Console.WriteLine("Every wait matches how long its block plays: nothing to fix.");
                return 0;
            }
            foreach (var group in waits.GroupBy(w => w.Where.File))
            {
                string file = group.Key;
                byte[] bytes = File.ReadAllBytes(file);
                bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                string[] raw = File.ReadAllText(file).Split('\n');     // each line keeps its \r, if it has one
                foreach (WaitMismatch m in group)
                    raw[m.Where.Line - 1] = new Regex(@"(wait\s+)[0-9]*(?:\.[0-9]*)?", RegexOptions.IgnoreCase)
                        .Replace(raw[m.Where.Line - 1], "${1}" + (int)Math.Round(m.Plays), 1);
                File.Copy(file, file + ".bak", true);
                File.WriteAllText(file, string.Join("\n", raw), new UTF8Encoding(bom));
            }
            Console.WriteLine("Fixed {0}:", waits.Count == 1 ? "1 wait" : waits.Count + " waits");
            foreach (WaitMismatch m in waits)
                Console.WriteLine("  {0}: wait {1} is now wait {2}{3}", Short(m.Where), m.Written, (int)Math.Round(m.Plays),
                    m.Written > m.Plays ? string.Format(" (the {0:0.000} s of silence after it is gone; to keep a rest, end the [play line with a pause)", (m.Written - m.Plays) / 1000) : "");
            Console.WriteLine("The files as they were are kept as .rzr.bak.");
            return 0;
        }
        #endregion The waits

        #region Clips
        // MidiToAims preview <song> --from --to --clip: the part of the song, as a song of its own: <song>.clipN.rzr
        //  beside it (the next free N). Cut on whole blocks (the lines up to and including a wait), so it holds real
        //  phrases and the song's own text, line for line, with its waits: from the block playing at --from to the one
        //  playing at --to. Its header is the song's (the lines before the first block), # Song: saying it's a clip;
        //  the [play settings of the blocks before it (instrument, tempo, timer ...) come first, so it plays as in the
        //  song. A later book's # Song: / # Book: lines inside it are left out. Written, not played.
        public static int Clip(IReadOnlyList<string> files, double? from, double? to)
        {
            var (text, origins) = Read(files);
            MusicCompiler.Result compiled = MusicCompiler.Compile(text, origins.Select(o => o.ToString()).ToList());
            if (!compiled.Success)
            {
                Console.WriteLine("The song has {0}: fix {1} first (preview lists {1}).", compiled.ErrorCount == 1 ? "a mistake" : compiled.ErrorCount + " mistakes", compiled.ErrorCount == 1 ? "it" : "them");
                return 1;
            }
            string[] lines = compiled.Text.Split("\r\n", StringSplitOptions.None);
            List<Origin>? where = compiled.Structured ? null : origins;
            Directives d = new(compiled.Source);
            double total = Perform(compiled.Text, where, d).End;

            // the blocks: (first line, last line, start, length); the header is the lines before the first
            static bool Starts(string line, string word) => line.TrimStart().StartsWith(word, StringComparison.OrdinalIgnoreCase);
            int header = Array.FindIndex(lines, l => Starts(l, "say") || Starts(l, "wait"));
            if (header < 0)
            {
                Console.WriteLine("The song has nothing to play.");
                return 1;
            }
            List<(int First, int Last, double Start, double Length)> blocks = new();
            Regex waitNumber = new(@"^[0-9]*(?:\.[0-9]*)?$");
            double script = 0;
            int first = header;
            for (int i = header; i < lines.Length; i++)
            {
                if (!Starts(lines[i], "wait"))
                    continue;
                string w = Regex.Replace(lines[i], "wait", "", RegexOptions.IgnoreCase).Trim();
                double ms = waitNumber.IsMatch(w) && double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) && n > 0 ? n : 0;
                blocks.Add((first, i, script, ms));
                script += ms;
                first = i + 1;
            }
            if (lines.Skip(first).Any(l => Starts(l, "say")))
                blocks.Add((first, lines.Length - 1, script, Math.Max(0, total - script)));

            double start = from ?? 0, stop = to ?? double.MaxValue;
            if (start >= total)
            {
                Console.WriteLine("The song is {0} long: --from {1} is after its end.", Time(total), Time(start));
                return 1;
            }
            if (stop <= start)
            {
                Console.WriteLine("--to {0} is not after --from {1}.", Time(stop), Time(start));
                return 1;
            }
            int s = Math.Max(0, blocks.FindLastIndex(b => b.Start <= start));
            int e = Math.Max(s, blocks.FindLastIndex(b => b.Start < stop));
            double clipStart = blocks[s].Start, clipEnd = blocks[e].Start + blocks[e].Length;
            string span = string.Format("{0} to {1}", Time(clipStart), Time(clipEnd));

            // the clip: the header (# Song: saying it's a clip), the settings so far, the blocks
            string song = d.Song.Length > 0 ? d.Song : Path.GetFileNameWithoutExtension(files[0]);
            List<string> clip = new();
            bool titled = false;
            foreach (string line in lines.Take(header))
            {
                if (Regex.IsMatch(line, @"^\s*#\s*song:", RegexOptions.IgnoreCase) && !titled)
                {
                    clip.Add(string.Format("# Song: {0} (clip, {1})", song, span));
                    titled = true;
                }
                else if (!Regex.IsMatch(line, @"^\s*#\s*book:", RegexOptions.IgnoreCase))
                    clip.Add(line);
            }
            if (!titled)
                clip.Insert(0, string.Format("# Song: {0} (clip, {1})", song, span));
            string[] settings = { "instrument", "tempo", "prefetch", "newtimer", "timer" };
            foreach (string line in lines.Take(blocks[s].First).Skip(header))
            {
                Match m = Regex.Match(line, @"^\s*say\s+'\[play\s+(\w+)", RegexOptions.IgnoreCase);
                if (m.Success && settings.Contains(m.Groups[1].Value.ToLowerInvariant()))
                    clip.Add(line);
            }
            for (int i = blocks[s].First; i <= blocks[e].Last; i++)
                if (!Regex.IsMatch(lines[i], @"^\s*#\s*(song|book):", RegexOptions.IgnoreCase))
                    clip.Add(lines[i]);
            string clipText = string.Join("\r\n", clip).TrimEnd() + "\r\n";

            // <song>.clipN.rzr, beside the song
            string dir = Path.GetDirectoryName(Path.GetFullPath(files[0]))!;
            string name = Regex.Replace(Path.GetFileNameWithoutExtension(files[0]), @"(\.book\d+)?(\.(?:paste|post)\d+)?$", "", RegexOptions.IgnoreCase);
            int number = 1;
            while (File.Exists(Path.Combine(dir, string.Format("{0}.clip{1}.rzr", name, number))))
                number++;
            string path = Path.Combine(dir, string.Format("{0}.clip{1}.rzr", name, number));
            File.WriteAllText(path, clipText);

            string lineSpan = where != null && blocks[s].First < where.Count && blocks[e].Last < where.Count
                ? string.Format(", {0} to {1}", Short(where[blocks[s].First]), Short(where[blocks[e].Last])) : "";
            Console.WriteLine("Wrote {0}: {1} of {2}{3}.", Path.GetFileName(path), span, Time(total), lineSpan);
            int posts = Books.Posts(clipText).Length;
            if (posts > 1)
                Console.WriteLine("It's too long for one post ({0} posts): a shorter clip pastes in one.", posts);
            Console.WriteLine("Hear it: [play preview <song> clip{0}", number);
            return 0;
        }
        #endregion Clips

        #region The client
        private sealed record Heard(Sound Sound, double StopMs);

        // what the client plays: a sound again within its lockout is skipped, later while it still sounds starts over
        private static (List<Heard> Heard, List<Sound> Skipped, List<Sound> Restarted) Client(List<Sound> sounds, ClientSounds client)
        {
            List<Heard> heard = new();
            List<Sound> skipped = new();
            List<Sound> restarted = new();     // the notes cut short
            Dictionary<int, int> playing = new();       // sound id: its instance in heard
            foreach (Sound s in sounds.OrderBy(s => s.Ms))
            {
                short[]? samples = client.Samples(s.Id);
                if (samples == null)
                    continue;
                double length = samples.Length * 1000.0 / ClientSounds.SampleRate;
                if (playing.TryGetValue(s.Id, out int last))
                {
                    Heard before = heard[last];
                    if (s.Ms - before.Sound.Ms < client.LockoutMs(s.Id))
                    {
                        skipped.Add(s);
                        continue;
                    }
                    if (s.Ms < before.StopMs)
                    {
                        heard[last] = before with { StopMs = s.Ms };
                        restarted.Add(before.Sound);
                    }
                }
                playing[s.Id] = heard.Count;
                heard.Add(new Heard(s, s.Ms + length));
            }
            return (heard, skipped, restarted);
        }

        // the performance as a 16-bit mono WAV at the client's rate (scaled down only if it would clip)
        private static byte[] Render(List<Heard> heard, ClientSounds client, double endMs)
        {
            double last = heard.Count == 0 ? endMs : Math.Max(endMs, heard.Max(h => h.StopMs));
            int total = (int)Math.Ceiling(last / 1000.0 * ClientSounds.SampleRate) + 1;
            int[] mix = new int[total];
            foreach (Heard h in heard)
            {
                short[] samples = client.Samples(h.Sound.Id)!;
                int start = (int)Math.Round(h.Sound.Ms / 1000.0 * ClientSounds.SampleRate);
                int count = Math.Min(samples.Length, (int)Math.Round((h.StopMs - h.Sound.Ms) / 1000.0 * ClientSounds.SampleRate));
                for (int i = 0; i < count && start + i < total; i++)
                    mix[start + i] += samples[i];
            }
            int peak = mix.Length == 0 ? 0 : mix.Max(Math.Abs);
            double scale = peak > short.MaxValue ? (double)short.MaxValue / peak : 1.0;

            using MemoryStream wav = new();
            using BinaryWriter w = new(wav);
            int bytes = total * 2;
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes); w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(ClientSounds.SampleRate); w.Write(ClientSounds.SampleRate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (int v in mix)
                w.Write((short)Math.Round(v * scale));
            w.Flush();
            return wav.ToArray();
        }
        #endregion The client

        #region Playback
        [DllImport("winmm.dll", EntryPoint = "PlaySoundW")]
        private static extern bool PlaySound(IntPtr sound, IntPtr module, uint flags);
        private const uint SND_ASYNC = 0x0001, SND_NODEFAULT = 0x0002, SND_MEMORY = 0x0004;

        private static string Time(double ms) => string.Format("{0}:{1:00.0}", (int)(ms / 60000), ms % 60000 / 1000);

        // a time as typed (--from 1:10, --to 1:40.5, --from 70): milliseconds, or null when it isn't one
        public static double? ParseTime(string text)
        {
            Match m = Regex.Match(text.Trim(), @"^(?:(\d+):)?(\d+(?:\.\d+)?)$");
            if (!m.Success)
                return null;
            double seconds = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            if (m.Groups[1].Success && seconds >= 60)
                return null;
            return ((m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 0) * 60 + seconds) * 1000;
        }

        // where a line is, short: the piece and line (book1.post2 line 140), or line 140 for a song in one file
        private static string Short(Origin? where)
        {
            if (where == null)
                return "";
            string piece = Regex.Match(Path.GetFileNameWithoutExtension(where.File), @"(\.book\d+)?(\.(?:paste|post)\d+)?$", RegexOptions.IgnoreCase).Value.TrimStart('.');
            return (piece.Length > 0 ? piece + " " : "") + "line " + where.Line;
        }

        // the client's sounds, or null (and why, said to the player); kept for the next command (the m2a> prompt)
        private static ClientSounds? s_client;
        private static ClientSounds? FindClient(string? uo, string example)
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("MidiToAims needs Windows to play sounds.");
                return null;
            }
            if (uo == null && s_client != null)
                return s_client;
            List<string> tried = new();
            ClientSounds? client = ClientSounds.Find(uo, tried);
            if (client != null)
                return s_client = client;
            string? uop = tried.Select(t => t.Replace(" (--uo)", "")).FirstOrDefault(t => Directory.Exists(t) && ClientSounds.HasUop(t));
            Console.WriteLine(uop != null
                ? string.Format("Your Ultima Online client in {0} keeps its sounds in a .uop file, which MidiToAims can't read yet: it needs sound.mul and soundidx.mul.", uop)
                : "MidiToAims couldn't find your Ultima Online client's sounds (sound.mul). It looked in:");
            if (uop == null)
                foreach (string t in tried)
                    Console.WriteLine("  " + t);
            Console.WriteLine("Tell it where they are (it remembers): MidiToAims {0} --uo \"C:\\...\\UO Angel Island\"", example);
            return null;
        }

        // plays a WAV made by Render, showing each caption at its time; Ctrl+C stops. positions: where each sound is
        //  written (Short), for the status line (the time in the song, of total, and the line playing, rewritten in
        //  place with \r; not when the output goes to a file) and for where it stopped; null: no status line. start:
        //  the song time the WAV begins at (--from); end: a part's end (--to), said with where it ended (Done at ...)
        private static int Playback(byte[] wav, IReadOnlyList<(double Ms, string Text)> captions, IReadOnlyList<(double Ms, string Where)>? positions = null, double start = 0, double total = 0, double? end = null)
        {
            double length = (wav.Length - 44) / 2 * 1000.0 / ClientSounds.SampleRate;
            bool stop = false;
            ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop = true; };
            Console.CancelKeyPress += cancel;
            IntPtr memory = Marshal.AllocHGlobal(wav.Length);
            bool status = positions != null && !Console.IsOutputRedirected;
            string shownStatus = "";
            void Clear()
            {
                if (shownStatus.Length > 0)
                    Console.Write("\r" + new string(' ', shownStatus.Length) + "\r");
                shownStatus = "";
            }
            try
            {
                Marshal.Copy(wav, 0, memory, wav.Length);
                if (!PlaySound(memory, IntPtr.Zero, SND_MEMORY | SND_ASYNC | SND_NODEFAULT))
                {
                    Console.WriteLine("Windows couldn't play the sound (is there a sound device?).");
                    return 1;
                }
                Stopwatch clock = Stopwatch.StartNew();
                int shown = 0, at = -1;
                double now = 0;
                while (!stop && (now = clock.Elapsed.TotalMilliseconds) < length + 100)
                {
                    for (; shown < captions.Count && captions[shown].Ms <= now; shown++)
                    {
                        Clear();
                        Console.WriteLine("  [{0}] {1}", Time(start + captions[shown].Ms), captions[shown].Text);
                    }
                    while (positions != null && at + 1 < positions.Count && positions[at + 1].Ms <= now)
                        at++;
                    if (status)
                    {
                        string line = string.Format("  {0} / {1}{2}", Time(start + Math.Min(now, length)), Time(total), at >= 0 && positions![at].Where.Length > 0 ? "   " + positions[at].Where : "");
                        if (line.TrimEnd() != shownStatus.TrimEnd())
                        {
                            line = line.PadRight(shownStatus.Length);
                            Console.Write("\r" + line);
                            shownStatus = line;
                        }
                    }
                    Thread.Sleep(50);
                }
                Clear();
                string where = positions != null && at >= 0 && positions[at].Where.Length > 0 ? " (" + positions[at].Where + ")" : "";
                Console.WriteLine(stop ? (positions != null ? string.Format("Stopped at {0}{1}.", Time(start + now), where) : "Stopped.")
                    : end != null ? string.Format("Done at {0}{1}.", Time(end.Value), where) : "Done.");
            }
            finally
            {
                PlaySound(IntPtr.Zero, IntPtr.Zero, 0);
                Marshal.FreeHGlobal(memory);
                Console.CancelKeyPress -= cancel;
            }
            return 0;
        }

        // plays the song (its .rzr pieces, in order); uo: --uo, or null to find the client files; check: report only;
        //  from, to: the part of the song to play (--from 1:10 --to 1:40, ms; null: its start, its end)
        public static int Run(IReadOnlyList<string> files, string? uo, bool check = false, double? from = null, double? to = null)
        {
            ClientSounds? client = FindClient(uo, "preview \"song.rzr\"");
            if (client == null)
                return 1;

            // the song, compiled as for sheet music
            var (text, origins) = Read(files);
            MusicCompiler.Result compiled = MusicCompiler.Compile(text, origins.Select(o => o.ToString()).ToList());
            if (!compiled.Success)
            {
                Console.WriteLine("The song has {0}:", compiled.ErrorCount == 1 ? "a mistake" : compiled.ErrorCount + " mistakes");
                foreach (string e in compiled.Errors)
                    Console.WriteLine("  " + e);
                if (compiled.ErrorCount > compiled.Errors.Count)
                    Console.WriteLine("  ... and {0} more.", compiled.ErrorCount - compiled.Errors.Count);
                return 1;
            }
            Directives d = new(compiled.Source);
            ReportWaits(CheckWaits(text, origins, d), check || Program.Debug);
            Performance perf = Perform(compiled.Text, compiled.Structured ? null : origins, d);
            return Hear(perf, d, client, d.Song.Length > 0 ? d.Song : Path.GetFileNameWithoutExtension(files[0]), "song", check, from, to);
        }

        // what the game will do differently from what is written, then the performance played (check: a summary).
        //  from, to: the part to play (ms; null: the start, the end); the client's skips are worked out over the whole
        //  performance (a note just before the part can skip one in it), then the part is cut out. The details (each
        //  line's skips, notes cut short, the files) only with --check or debug (Program.Debug); else a line.
        private static int Hear(Performance perf, Directives d, ClientSounds client, string song, string what, bool check, double? from = null, double? to = null)
        {
            var (allHeard, allSkipped, restarted) = Client(perf.Sounds, client);
            double total = allHeard.Count == 0 ? perf.End : Math.Max(perf.End, allHeard.Max(h => h.StopMs));
            double start = from ?? 0, stop = to ?? double.MaxValue;
            bool part = from != null || to != null;
            if (from != null && start >= total)
            {
                Console.WriteLine("The {0} is {1} long: --from {2} is after its end.", what, Time(total), Time(start));
                return 1;
            }
            if (part && stop <= start)
            {
                Console.WriteLine("--to {0} is not after --from {1}.", Time(stop), Time(start));
                return 1;
            }
            bool In(Sound s) => s.Ms >= start && s.Ms < stop;
            List<Heard> heard = allHeard.Where(h => In(h.Sound)).Select(h => new Heard(h.Sound with { Ms = h.Sound.Ms - start }, h.StopMs - start)).ToList();
            List<Sound> skipped = allSkipped.Where(In).ToList();
            int notes = perf.Sounds.Count(In);
            byte[] wav = Render(heard, client, Math.Min(perf.End, stop) - start);
            double length = (wav.Length - 44) / 2 * 1000.0 / ClientSounds.SampleRate;
            bool details = check || Program.Debug;

            // what the game will do differently from what is written
            foreach (string problem in perf.Problems.Take(10))
                Console.WriteLine("Warning: " + problem);
            if (perf.Problems.Count > 10)
                Console.WriteLine("... and {0} more warnings.", perf.Problems.Count - 10);
            List<string> karma = perf.Sounds.Where(s => IsSound(s.Name) && !Percussion.IsPercussion(s.Id)).Select(s => s.Name).Distinct().ToList();
            if (karma.Count > 0)
                Console.WriteLine("This {0} has sound effects that need 1000 music karma ({1}): they play here, but in the game they need that karma. (Percussion is free: MidiToAims sound percussion.)",
                    what, string.Join(" ", karma.Take(8)) + (karma.Count > 8 ? " ..." : ""));
            if (skipped.Count > 0 && !details)
                Console.WriteLine("The game will skip {0} of {1} notes, as you'll hear (the same note on the same instrument again within about 0.2 s): --check lists them.",
                    skipped.Count, notes);
            else if (skipped.Count > 0)
            {
                Console.WriteLine("The game client will skip {0} of {1} notes: the same note on the same instrument again within about 0.2 s (lute: 0.15 s) doesn't play. You'll hear them missing here too.",
                    skipped.Count, notes);
                var lines = skipped.GroupBy(s => s.Where).ToList();
                foreach (var g in lines.Take(12))
                    Console.WriteLine("  {0}: {1} ({2})", g.Key?.ToString() ?? Time(g.First().Ms), g.Count() == 1 ? "1 note" : g.Count() + " notes",
                        string.Join(", ", g.Select(s => s.Name).Distinct()));
                if (lines.Count > 12)
                    Console.WriteLine("  ... and {0} more lines.", lines.Count - 12);
            }
            int cut = restarted.Count(In);
            if (cut > 0 && details)
                Console.WriteLine("{0} cut short by the same note starting again (as in the game).", cut == 1 ? "1 note is" : cut + " notes are");

            // where the sounds come from: said once (the m2a> prompt plays many lines from the same client)
            string folder = s_saidFolder == client.Folder ? "" : "; sounds from " + client.Folder;
            s_saidFolder = client.Folder;
            string span = part ? string.Format("{0} to {1} of {2}", Time(start), Time(Math.Min(stop, total)), Time(total)) : Time(length);
            if (check)
            {
                Console.WriteLine("{0}: {1}, {2} timer, {3} notes and sounds, {4} heard{5}.", char.ToUpper(song[0]) + song.Substring(1), span, d.Timer, notes, heard.Count, folder);
                return 0;
            }
            Console.WriteLine("Playing {0} ({1}, {2} timer{3}). Ctrl+C stops.", song, span, d.Timer, folder);
            List<(double Ms, string Where)> positions = perf.Sounds.Where(In).OrderBy(s => s.Ms).Select(s => (s.Ms - start, Short(s.Where))).ToList();
            List<(double Ms, string Text)> captions = perf.Says.Where(c => c.Ms >= start && c.Ms < stop).Select(c => (c.Ms - start, c.Text)).ToList();
            return Playback(wav, captions, positions, start, total, part ? Math.Min(stop, total) : null);
        }
        private static string? s_saidFolder;

        // MidiToAims play: a [play line (the words after [play), as SystemMusicPlayer.DoConfigure and PlaySession take it.
        //  A setting is remembered (MidiToAims.ini) as the game keeps it in the player's music context.
        private const string KeyInstrument = "Play instrument:", KeyTempo = "Play tempo:", KeyTimer = "Play timer:", KeyPrefetch = "Play prefetch:";

        public static int Line(IReadOnlyList<string> words, string? uo, bool check)
        {
            string command = words[0].ToLowerInvariant();
            string? arg = words.Count == 2 ? words[1] : null;
            switch (command)
            {
                case "instrument":
                    if (arg == null || !(arg.ToLowerInvariant() is "harp" or "lapharp" or "lute"))
                        return Say("Error in Instrument format: harp, lapharp or lute.");
                    Ini.Set(KeyInstrument, arg.ToLowerInvariant());
                    return Say("Instrument set to {0}.", arg.ToLowerInvariant());
                case "tempo":
                    if (!int.TryParse(arg, out int tempo))
                        return Say("Error in Tempo format: milliseconds, e.g. [play tempo 150.");
                    Ini.Set(KeyTempo, tempo.ToString());
                    return Say("Tempo set to {0} milliseconds.", tempo);
                case "prefetch":
                    if (!bool.TryParse(arg, out bool prefetch))
                        return Say("Error in Prefetch format: true or false.");
                    Ini.Set(KeyPrefetch, prefetch.ToString());
                    return Say("Prefetch set to {0}.", prefetch);
                case "newtimer":
                    if (!bool.TryParse(arg, out bool newTimer))
                        return Say("Error in NewTimer format: true or false.");
                    Ini.Set(KeyTimer, (newTimer ? Timer.Dynamic : Timer.Classic).ToString());
                    return Say("NewTimer set to {0} (Timer set to {1}).", newTimer, newTimer ? Timer.Dynamic : Timer.Classic);
                case "timer":
                    if (arg == null || int.TryParse(arg, out _) || !Enum.TryParse(arg, true, out Timer timer) || !Enum.IsDefined(timer))
                        return Say("Usage: [play timer classic|dynamic|anchored");
                    Ini.Set(KeyTimer, timer.ToString());
                    return Say("Timer set to {0}.", timer);
                case "config":
                {
                    Directives d = Settings();
                    Console.WriteLine("Instrument set to {0}.", d.Instrument);
                    Console.WriteLine("Timer set to {0}.", d.Timer);
                    Console.WriteLine("Prefetch set to {0}.", d.Prefetch);
                    Console.WriteLine("Tempo set to {0} milliseconds.", d.Tempo);
                    return 0;
                }
                case "reset":
                    foreach (string key in new[] { KeyInstrument, KeyTempo, KeyTimer, KeyPrefetch })
                        Ini.Set(key, null);
                    return Say("Music settings reset: harp, Classic timer, tempo {0}, prefetch False.", DefaultTempo);
                case "musickarma":
                    return Say("Only the game knows your music karma: type [play musickarma there. (Sound effects play here whatever it is.)");
                case "volume": case "concertmode": case "chatter":
                    return Say("'{0}' changes nothing here: MidiToAims plays every sound at full volume, without chatter.", command);
                case "macro": case "list": case "inspect": case "test":
                    return Say("'{0}' is a game-only [play command: MidiToAims doesn't have it.", command);
            }

            ClientSounds? client = FindClient(uo, "play c e g");
            if (client == null)
                return 1;
            Directives settings = Settings();
            Performance perf = Perform("say '[play " + string.Join(" ", words) + "'", null, settings, "your line");

            // [play dance: words, not notes - most likely a song's name, which [play preview plays
            string first = words[0].ToLowerInvariant();
            if (!IsNote(first) && !IsSound(first) && !IsSelector(first, out _) && !IsVolume(first)
                && !double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                Console.WriteLine("'{0}' is not a note, a pause or a setting. To hear a song, type [play preview {1}", words[0], string.Join(" ", words));
                return 1;
            }
            return Hear(perf, settings, client, "your line", "line", check);
        }

        // the remembered settings, as a song's directives
        private static Directives Settings()
        {
            Directives d = new("");
            d.Instrument = Ini.Get(KeyInstrument) is string i && i.ToLowerInvariant() is "harp" or "lapharp" or "lute" ? i.ToLowerInvariant() : "harp";
            if (int.TryParse(Ini.Get(KeyTempo), out int tempo)) d.Tempo = tempo;
            if (Enum.TryParse(Ini.Get(KeyTimer), true, out Timer timer) && Enum.IsDefined(timer)) d.TimerHeader = timer;
            d.Prefetch = bool.TryParse(Ini.Get(KeyPrefetch), out bool prefetch) && prefetch;
            return d;
        }

        private static int Say(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            return format.StartsWith("Error") || format.StartsWith("Usage") ? 1 : 0;
        }

        // MidiToAims sound: plays the client's sounds one after another, each with its number and name. specs: a number
        //  (81), a range (300-330) or part of a name (wood); list: show them without playing
        public static int Sounds(IReadOnlyList<string> specs, string? uo, bool list)
        {
            ClientSounds? client = FindClient(uo, "sound 81");
            if (client == null)
                return 1;

            List<int> ids = new();
            foreach (string spec in specs)
            {
                Match range = Regex.Match(spec, @"^\+?(\d+)(?:-(\d+))?$");
                if (spec.Equals("percussion", StringComparison.OrdinalIgnoreCase))
                    ids.AddRange(Percussion.Sounds.Where(id => client.Samples(id) != null));
                else if (range.Success)
                {
                    int from = int.Parse(range.Groups[1].Value), to = range.Groups[2].Success ? int.Parse(range.Groups[2].Value) : from;
                    ids.AddRange(client.Ids.Where(id => id >= Math.Min(from, to) && id <= Math.Max(from, to)));
                }
                else
                    ids.AddRange(client.Ids.Where(id => client.Name(id).Contains(spec, StringComparison.OrdinalIgnoreCase)));
            }
            ids = ids.Distinct().ToList();
            if (ids.Count == 0)
            {
                Console.WriteLine("No sounds match {0}. Try a number (81), a range (300-330), part of a name (wood, drum, hit) or percussion.", string.Join(" ", specs));
                return 1;
            }

            // one after another, half a second apart
            const double gap = 500;
            List<Heard> heard = new();
            List<(double Ms, string Text)> captions = new();
            double at = 0;
            foreach (int id in ids)
            {
                double length = client.Samples(id)!.Length * 1000.0 / ClientSounds.SampleRate;
                string caption = string.Format("+{0}  {1}  ({2:0.00} s)  {3}", id, client.Name(id), length / 1000,
                    id > MaxSoundId ? string.Format("not playable in AIMS (only +0 to +{0})", MaxSoundId)
                    : Percussion.IsPercussion(id) ? "percussion: free for everyone" : "needs 1000 music karma");
                if (list)
                    Console.WriteLine(caption);
                heard.Add(new Heard(new Sound(at, id, client.Name(id), null), at + length));
                captions.Add((at, caption));
                at += length + gap;
            }
            if (list)
                return 0;
            Console.WriteLine("Playing {0}: write one in a song as +number, e.g. +{1}. Ctrl+C stops.", ids.Count == 1 ? "1 sound" : ids.Count + " sounds", ids[0]);
            return Playback(Render(heard, client, at), captions);
        }
        #endregion Playback
    }
}
