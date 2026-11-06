/***************************************************************************
 *
 *  MidiToAims.Core - Converter: a MIDI file and its arrangement (.m2a) to an AIMS composition (.rzr).
 *
 *  A composer's workbench: the defaults make a good first draft (stage 1), and every choice they make can be changed
 *  in the song's .m2a file. Init writes a starter .m2a with the automatic choices shown as comments.
 *
 *  Defaults (stage 1): the track with the highest notes is the melody (lap harp); the others are the accompaniment
 *  (harp). Each voice moves by whole octaves to fit AIMS's two octaves (cl..ch, MIDI 48..72), the accompaniment
 *  below the melody; any note still outside is folded in by octaves. At most 3 notes at once: the melody, the bass,
 *  then inner notes from the top. Drums (channel 10) play as percussion (Drums: on, the default; each role a free
 *  percussion sound; Galois's kit by default: Arrangement.DefaultKit). Phrase blocks of 2 bars, each followed by a wait
 *  of exactly its length (Anchored, Prefetch, Tempo 0). Books of at most 95 pages (Books.DefaultPages).
 *  Repeated notes: alternate (the default; off turns it off) moves a note repeated on one instrument faster than the
 *  client replays it (it skips it) to the other harp; the report counts them either way. Inner notes: highest (the
 *  default) or loudest (the MIDI's velocity) chooses the notes after the melody and the bass when there are too many.
 *
 *  The entry points take and return strings only, so a caller can load this assembly fresh for every run:
 *      string Converter.Init(string midiPath)                         the starter .m2a
 *      string[] Converter.Convert(string midiPath, string? m2aText)   { .rzr, report, book 1, book 2, ... }
 *  The books are the composition laid out in music composition books (Books.Split); none when it is too long. A player
 *  posts each book with its Paste (MusicCompositionBook: the client's bulletin board gump), in pieces of at most 250
 *  lines (Books.Posts). The .m2a's Format (Arrangement.Format: post or whole) is for the command line, which writes the
 *  files; Convert's result is the same, and only the report's post count follows it.
 *
 ***************************************************************************/

using System.Globalization;
using System.Text;

namespace MidiToAims
{
    public static class Converter
    {
        // AIMS notes, low C (MIDI 48) to high C (MIDI 72); c is middle C
        private static readonly string[] Notes =
            { "cl", "csl", "d", "ds", "e", "f", "fs", "g", "gs", "a", "as", "b", "c", "cs", "dh", "dsh", "eh", "fh", "fsh", "gh", "gsh", "ah", "ash", "bh", "ch" };
        private const int Low = 48, High = 72;
        private const int MaxSound = 0x5CF;                 // CoreMusicPlayer: +NNN from 0 to 1487
        private static readonly string[] PitchNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        private static string PitchName(int p) => PitchNames[p % 12] + (p / 12 - 1);

        #region Arrangement (.m2a)
        public sealed class TrackSettings
        {
            public string Role = "auto";            // auto | melody | harmony | bass | off
            public string Instrument = "auto";      // auto | harp | lapharp | lute
            public int? Octave;                     // null: auto
        }

        public sealed class Arrangement
        {
            public string? Song, Composer, ArrangedBy;
            public string Timer = "Anchored";
            public int NotesAtOnce = 3;
            public int BarsPerBlock = 2;
            public int BookPages = Books.DefaultPages;
            public string Format = "post";          // the files written (the command line's): post (pieces for a book's Paste) | whole (one file)
            public bool Alternate = true;           // Repeated notes: alternate (the default) moves a note repeated too fast for the client (it skips it) to the other harp
            public bool Loudest;                    // Inner notes: loudest fills the slots after the melody and bass with the loudest notes (highest: from the top)
            public bool Drums = true;               // Drums: on (the default) plays the MIDI's drums (channel 10) as percussion sound effects
            public int DrumsAtOnce = 1;             // drum sounds at one moment (on top of Notes at once)
            public int?[] Kit = (int?[])DefaultKit.Clone();     // a sound effect per drum role (DrumRoles); null: left out

            // the drum roles, and the default kit: Galois's, from his 76 songs (328 opens 82% of his groups with the bass:
            //  a kick; 309 the backbeat: a snare; 305 every 0.41 s: a hi-hat; 943 his second downbeat: an accent, standing
            //  in for the cymbals). No toms yet. All are percussion (free of music karma: Percussion.Sounds).
            public static readonly string[] DrumRoles = { "Kick", "Snare", "Hi-hat", "Cymbal", "Toms" };
            public static readonly int?[] DefaultKit = { 328, 309, 305, 943, null };

            // a drum role's index (a track's Role: kick ... a drum written as a pitched track), or null
            public static int? DrumRole(string role)
            {
                int r = Array.FindIndex(DrumRoles, d => d.Equals(role, StringComparison.OrdinalIgnoreCase));
                return r >= 0 ? r : null;
            }

            // the drum a track's name says it is (BassDrum, Snare, Hi-Hat ...), for the starter .m2a's hint; or null
            public static int? DrumByName(string name)
            {
                string n = name.ToLowerInvariant().Replace(" ", "").Replace("-", "");
                return n.Contains("bassdrum") || n.Contains("kick") ? 0 : n.Contains("snare") ? 1 : n.Contains("hihat") || n.Contains("hat") ? 2
                    : n.Contains("cymbal") || n.Contains("crash") || n.Contains("ride") ? 3 : n.Contains("tom") ? 4 : null;
            }
            public Dictionary<int, TrackSettings> Tracks = new();
            public List<string> Warnings = new();

            public TrackSettings For(int track) => Tracks.TryGetValue(track, out TrackSettings? s) ? s : new TrackSettings();

            public static Arrangement Parse(string? text)
            {
                Arrangement a = new();
                TrackSettings? current = null;
                int number = 0;
                foreach (string raw in (text ?? "").Split('\n'))
                {
                    number++;
                    int hash = CommentAt(raw);
                    string line = (hash >= 0 ? raw.Substring(0, hash) : raw).Trim();
                    if (line.Length == 0)
                        continue;
                    int colon = line.IndexOf(':');
                    if (colon < 0)
                    {
                        a.Warnings.Add(string.Format("line {0}: '{1}' is not 'Key: value'", number, line));
                        continue;
                    }
                    string key = line.Substring(0, colon).Trim().ToLowerInvariant();
                    string value = line.Substring(colon + 1).Trim();

                    if (key.StartsWith("track "))
                    {
                        if (int.TryParse(key.Substring(6), out int t))
                            a.Tracks[t] = current = new TrackSettings();
                        else
                            a.Warnings.Add(string.Format("line {0}: '{1}' needs a track number", number, line));
                        continue;
                    }
                    if (value.Length == 0)
                        continue;       // a blank setting keeps its default
                    switch (key)
                    {
                        case "song": a.Song = value; break;
                        case "composer": a.Composer = value; break;
                        case "arranged by": a.ArrangedBy = value; break;
                        case "midi": break;     // the MIDI the arrangement is for (informational)
                        case "timer": a.Timer = value; break;
                        case "notes at once": if (int.TryParse(value, out int n) && n >= 1 && n <= 6) a.NotesAtOnce = n; else a.Warnings.Add(string.Format("line {0}: Notes at once is 1 to 6", number)); break;
                        case "bars per block": if (int.TryParse(value, out int b) && b >= 1 && b <= 8) a.BarsPerBlock = b; else a.Warnings.Add(string.Format("line {0}: Bars per block is 1 to 8", number)); break;
                        case "book pages": if (int.TryParse(value, out int p) && p >= 1 && p <= Books.MaxPages) a.BookPages = p; else a.Warnings.Add(string.Format("line {0}: Book pages is 1 to {1}", number, Books.MaxPages)); break;
                        case "format":
                            if (value.ToLowerInvariant() is "post" or "whole") a.Format = value.ToLowerInvariant();
                            else a.Warnings.Add(string.Format("line {0}: Format is post or whole", number));
                            break;
                        case "drums":
                            if (value.ToLowerInvariant() is "on" or "off") a.Drums = value.ToLowerInvariant() == "on";
                            else a.Warnings.Add(string.Format("line {0}: Drums is on or off", number));
                            break;
                        case "drums at once":
                            if (int.TryParse(value, out int da) && da >= 1 && da <= 4) a.DrumsAtOnce = da;
                            else a.Warnings.Add(string.Format("line {0}: Drums at once is 1 to 4", number));
                            break;
                        case "kick":
                        case "snare":
                        case "hi-hat":
                        case "cymbal":
                        case "toms":
                        {
                            int role = Array.FindIndex(DrumRoles, r => r.Equals(key, StringComparison.OrdinalIgnoreCase));
                            if (value.ToLowerInvariant() is "off" or "none")
                                a.Kit[role] = null;
                            else if (int.TryParse(value.TrimStart('+'), out int sound) && sound >= 0 && sound <= MaxSound)
                            {
                                a.Kit[role] = sound;
                                if (!Percussion.IsPercussion(sound))
                                    a.Warnings.Add(string.Format("line {0}: {1}: +{2} isn't percussion: making sheet music with it needs 1000 music karma (MidiToAims sound percussion lists the free ones)", number, DrumRoles[role], sound));
                            }
                            else
                                a.Warnings.Add(string.Format("line {0}: {1} is a sound effect, +0 to +{2}, or off", number, DrumRoles[role], MaxSound));
                            break;
                        }
                        case "inner notes":
                            if (value.ToLowerInvariant() is "highest" or "loudest") a.Loudest = value.ToLowerInvariant() == "loudest";
                            else a.Warnings.Add(string.Format("line {0}: Inner notes is highest or loudest", number));
                            break;
                        case "repeated notes":
                            if (value.ToLowerInvariant() is "off" or "alternate") a.Alternate = value.ToLowerInvariant() == "alternate";
                            else a.Warnings.Add(string.Format("line {0}: Repeated notes is off or alternate", number));
                            break;
                        case "role":
                        case "instrument":
                        case "octave":
                            if (current == null) { a.Warnings.Add(string.Format("line {0}: {1} belongs under a 'Track N:' line", number, key)); break; }
                            string v = value.ToLowerInvariant();
                            if (key == "role")
                            {
                                if (v is "auto" or "melody" or "harmony" or "bass" or "off" || DrumRole(v) != null) current.Role = v;
                                else a.Warnings.Add(string.Format("line {0}: Role is auto, melody, harmony, bass, off, or a drum: kick, snare, hi-hat, cymbal, toms", number));
                            }
                            else if (key == "instrument")
                            {
                                if (v is "auto" or "harp" or "lapharp" or "lute") current.Instrument = v;
                                else a.Warnings.Add(string.Format("line {0}: Instrument is auto, harp, lapharp or lute", number));
                            }
                            else if (v == "auto") current.Octave = null;
                            else if (int.TryParse(v, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int o) && o >= -4 && o <= 4) current.Octave = o;
                            else a.Warnings.Add(string.Format("line {0}: Octave is auto or -4 to +4", number));
                            break;
                        default:
                            a.Warnings.Add(string.Format("line {0}: unknown setting '{1}'", number, line.Substring(0, colon).Trim()));
                            break;
                    }
                }
                return a;
            }

            // where a line's comment starts: a # that begins the line, or a # on its own (whitespace on both sides), so
            //  'Song: Prelude in C# minor' and 'Song: #1 Hit' keep their #; -1 when there is none
            private static int CommentAt(string raw)
            {
                for (int i = 0; i < raw.Length; i++)
                    if (raw[i] == '#' && (raw.Substring(0, i).Trim().Length == 0
                        || char.IsWhiteSpace(raw[i - 1]) && (i + 1 == raw.Length || char.IsWhiteSpace(raw[i + 1]))))
                        return i;
                return -1;
            }

            // the settings a conversion used, for the report: what the composer set, and what was left at its default
            public string Summary(string song) =>
                string.Format("Song: {0}; Composer: {1}; Arranged by: {2}; Timer: {3}; Notes at once: {4}; Bars per block: {5}; Book pages: {6}; Format: {7}; Repeated notes: {8}; Inner notes: {9}; Drums: {10}",
                    song, Composer ?? "(none)", ArrangedBy ?? "MidiToAims (the default: set 'Arranged by' in the .m2a)", Timer, NotesAtOnce, BarsPerBlock, BookPages, Format,
                    Alternate ? "alternate" : "off", Loudest ? "loudest" : "highest", Drums ? "on" : "off");
        }
        #endregion Arrangement (.m2a)

        // the starter .m2a for a MIDI: every setting at its default, the automatic choices shown as comments
        public static string Init(string midiPath)
        {
            Midi midi = Midi.Read(midiPath);
            List<Midi.Note> notes = midi.Notes.Where(n => n.Channel != 9).ToList();
            var tracks = notes.GroupBy(n => n.Track).OrderBy(g => g.Key).ToList();
            int melodyTrack = tracks.Count > 1 ? tracks.OrderByDescending(g => g.Average(n => n.Pitch)).First().Key : -1;

            StringBuilder s = new();
            s.AppendLine("# MidiToAims arrangement for " + Path.GetFileName(midiPath));
            s.AppendLine("# Change any setting and convert again. 'auto' keeps the converter's choice (shown in the comment).");
            s.AppendLine("# Write a value right after its colon. A # starts a comment (a # in a value, like C#, is kept).");
            s.AppendLine();
            s.AppendLine("MIDI: " + Path.GetFileName(midiPath));
            s.AppendLine("Song: " + Path.GetFileNameWithoutExtension(midiPath));
            s.AppendLine("# Composer: who wrote the music (blank: not shown)");
            s.AppendLine("Composer:");
            s.AppendLine("# Arranged by: you, the book's author - your character's name (blank: MidiToAims, or whoever runs [m2a)");
            s.AppendLine("Arranged by:");
            s.AppendLine("Timer: Anchored");
            s.AppendLine("Notes at once: 3         # melody, bass, then inner notes from the top");
            s.AppendLine("Inner notes: highest     # the inner notes kept when there are too many: highest | loudest (the MIDI's velocity)");
            s.AppendLine("Bars per block: 2");
            s.AppendLine(string.Format("Book pages: {0}           # pages filled per book; a book holds {1}, the rest is room for your own lines", Books.DefaultPages, Books.MaxPages));
            s.AppendLine("Format: post             # post: files for the book's Paste (right-click the book, Paste, Post Message) | whole: the song in one file");
            s.AppendLine("Repeated notes: alternate   # a note repeated faster than the game replays it (0.2 s; it skips it) moves to the other harp | off");
            s.AppendLine();
            s.AppendLine("# Roles: melody (lap harp) | harmony, bass (harp) | off | a drum written as notes: kick, snare, hi-hat, cymbal, toms");
            s.AppendLine("#  (it plays as that drum with Drums: on, below). Instruments: harp | lapharp | lute. Octave: auto or -4..+4");
            foreach (var g in tracks)
            {
                string name = midi.TrackNames.TryGetValue(g.Key, out string? n) && n.Length > 0 ? n : "(no name)";
                string auto = melodyTrack < 0 ? "melody: the top note at each moment" : g.Key == melodyTrack ? "melody (the highest track)" : "harmony";
                if (Arrangement.DrumByName(name) is int drum)
                    auto += string.Format("; its name says it's a drum: Role: {0} plays it as one", Arrangement.DrumRoles[drum].ToLowerInvariant());
                s.AppendLine();
                s.AppendLine(string.Format("Track {0}:                # {1}; {2} notes, {3} to {4}", g.Key, name, g.Count(), PitchName(g.Min(x => x.Pitch)), PitchName(g.Max(x => x.Pitch))));
                s.AppendLine("  Role: auto             # " + auto);
                s.AppendLine("  Instrument: auto");
                s.AppendLine("  Octave: auto");
            }
            // the drums: Galois's kit (Arrangement.DefaultKit), all of it free of music karma
            int drums = midi.Notes.Count(n => n.Channel == 9);
            s.AppendLine();
            s.AppendLine(drums > 0 ? string.Format("# Drums: this MIDI has {0} drum hits (MIDI channel 10).", drums) : "# Drums: this MIDI has none.");
            s.AppendLine("Drums: on               # the drums, as percussion sound effects (free for everyone: MidiToAims sound percussion) | off");
            s.AppendLine("Drums at once: 1        # drum sounds at the same moment (they don't count against Notes at once)");
            s.AppendLine("Kick: +328              # hit20, Galois's kick (bass drum)");
            s.AppendLine("Snare: +309             # hit01, Galois's snare (also side stick and clap)");
            s.AppendLine("Hi-hat: +305            # feet15a, Galois's hi-hat (closed, pedal and open)");
            s.AppendLine("Cymbal: +943            # hit21, Galois's accent, standing in for crash and ride: still looking for a cymbal (suggestions to Adam)");
            s.AppendLine("Toms: off               # none yet: still looking for toms (suggestions to Adam); off leaves them out");
            return s.ToString();
        }

        public static string[] Convert(string midiPath, string? m2aText)
        {
            Arrangement arr = Arrangement.Parse(m2aText);
            Midi midi = Midi.Read(midiPath);
            StringBuilder report = new();
            foreach (string w in arr.Warnings)
                report.AppendLine("warning: " + w);

            // a header line the book wraps where a word begins with # (or is say, wait) reads as a new line to the shard
            //  (ManuscriptPaper.CheckBookWrap), which splits the name; the client wraps at spaces, so any such word may
            foreach (var (key, value) in new[] { ("Song", arr.Song), ("Composer", arr.Composer), ("Arranged by", arr.ArrangedBy) })
                if (value != null && value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1)
                    .Any(w => w.StartsWith("#") || w.Equals("say", StringComparison.OrdinalIgnoreCase) || w.Equals("wait", StringComparison.OrdinalIgnoreCase)))
                    report.AppendLine(string.Format("warning: {0}: '{1}' has a word the book may begin a line with (#..., say, wait); the shard would read it as a new line and cut the name there. Check the name after transcribing, or reword it.", key, value));

            // the notes: not the drums (channel 10, and tracks with a drum role: Drums), nor tracks that are off
            List<Midi.Note> notes = midi.Notes.Where(n => n.Channel != 9 && arr.For(n.Track).Role != "off" && Arrangement.DrumRole(arr.For(n.Track).Role) == null).ToList();
            if (notes.Count == 0)
                return new[] { "", report.AppendLine("No notes to play.").ToString() };

            // the voices: tracks marked melody; else (stage 1) the highest track, or with one track each moment's top note
            var tracks = notes.GroupBy(n => n.Track).ToList();
            HashSet<int> melodyTracks = tracks.Where(g => arr.For(g.Key).Role == "melody").Select(g => g.Key).ToHashSet();
            bool topNoteMode = false;
            if (melodyTracks.Count == 0)
            {
                var autos = tracks.Where(g => arr.For(g.Key).Role == "auto").ToList();
                if (tracks.Count > 1 && autos.Count > 0)
                    melodyTracks.Add(autos.OrderByDescending(g => g.Average(n => n.Pitch)).First().Key);
                else if (tracks.Count == 1)
                    topNoteMode = true;
            }
            bool IsMelody(Midi.Note n, int top) => topNoteMode ? n.Pitch == top : melodyTracks.Contains(n.Track);

            // moments: notes that start together (same tick); a pitch sounding in two tracks counts once (the first track)
            var moments = notes.GroupBy(n => n.Tick).OrderBy(g => g.Key).Select(g =>
            {
                int top = g.Max(n => n.Pitch);
                List<Midi.Note> mel = g.Where(n => IsMelody(n, top)).GroupBy(n => n.Pitch).Select(p => p.OrderBy(n => n.Track).First()).OrderByDescending(n => n.Pitch).ToList();
                List<Midi.Note> acc = g.Where(n => !IsMelody(n, top)).GroupBy(n => n.Pitch).Select(p => p.OrderBy(n => n.Track).First()).OrderByDescending(n => n.Pitch).ToList();
                return (Tick: g.Key, Melody: mel, Accomp: acc);
            }).ToList();

            // octaves: tracks with Octave set move by exactly that; the rest move together (melody, accompaniment), the
            //  accompaniment kept below the melody
            List<int> melodyPitches = moments.SelectMany(m => m.Melody.Take(1)).Where(n => arr.For(n.Track).Octave == null).Select(n => n.Pitch).ToList();
            List<int> accompPitches = moments.SelectMany(m => m.Accomp).Where(n => arr.For(n.Track).Octave == null).Select(n => n.Pitch).ToList();
            IEnumerable<int> Shifts() => Enumerable.Range(-4, 9).Select(o => o * 12);
            int Fitting(List<int> pitches, int s) => pitches.Count(p => p + s >= Low && p + s <= High);
            int melodyShift = Shifts().OrderByDescending(s => Fitting(melodyPitches, s)).ThenBy(Math.Abs).First();
            double melodyAverage = melodyPitches.Count > 0 ? melodyPitches.Average() + melodyShift : High;
            int accompShift = accompPitches.Count == 0 ? 0 : Shifts()
                .Where(s => accompPitches.Average() + s < melodyAverage)
                .OrderByDescending(s => Fitting(accompPitches, s)).ThenBy(Math.Abs).DefaultIfEmpty(melodyShift - 12).First();

            string Prefix(Midi.Note n, bool melody)
            {
                string instrument = arr.For(n.Track).Instrument;
                if (instrument == "auto")
                    instrument = melody ? "lapharp" : "harp";
                return instrument == "harp" ? "-" : instrument == "lute" ? "+" : "";      // relative to the lap harp
            }

            int kept = 0, dropped = 0, folded = 0;
            SortedDictionary<long, (int Dropped, int Folded)> byBar = new();
            void Note(long tick, int d, int f)
            {
                long bar = tick / midi.BarTicks + 1;
                byBar.TryGetValue(bar, out var v);
                byBar[bar] = (v.Dropped + d, v.Folded + f);
            }

            // the client skips a sound started again within about half its length (harp and lap harp 0.2 s, lute
            //  0.15 s: ClassicUO's Sound.Play), so a note repeated that fast on one instrument is lost. Repeated notes:
            //  alternate moves it to the other harp (from the lute: the lap harp) when that one is free. Measured on the
            //  times written (rounded as the pauses are), with 20 ms for the timer, the network and the client's frames;
            //  a skipped note doesn't start its sound's window again.
            string[] voicePrefix = { "-", "", "+" };                    // harp, lap harp, lute (relative to the lap harp)
            double[] window = { 220, 220, 170 };
            int[] partner = { 1, 0, 1 };
            Dictionary<(int Voice, int Note), double> started = new();
            int moved = 0, skipped = 0;
            int differs = 0;                                            // moments where the loudest inner notes aren't the highest
            bool Locked(int voice, int note, double at) => started.TryGetValue((voice, note), out double t) && at - t < window[voice];

            List<(long Tick, List<string> Tokens)> plays = new();
            foreach (var (tick, mel, acc) in moments)
            {
                double at = Math.Round(midi.Seconds(tick), 2) * 1000;
                // the melody's top note; then the bass (the lowest bass-role note, else the lowest), then inner notes from the top
                List<(Midi.Note Note, bool Melody)> chosen = new();
                if (mel.Count > 0)
                    chosen.Add((mel[0], true));
                Midi.Note? bass = acc.Where(n => arr.For(n.Track).Role == "bass").Cast<Midi.Note?>().LastOrDefault() ?? (acc.Count > 0 ? acc[^1] : null);
                if (bass != null && chosen.Count < arr.NotesAtOnce)
                    chosen.Add((bass.Value, false));
                // the inner notes: from the top, or (Inner notes: loudest) the loudest first (the MIDI's velocity), ties
                //  from the top; written from the top either way
                List<Midi.Note> inner = acc.Where(n => !(bass != null && n.Equals(bass.Value))).ToList();
                int room = Math.Max(0, arr.NotesAtOnce - chosen.Count);
                List<Midi.Note> highest = inner.Take(room).ToList();
                List<Midi.Note> loudest = inner.OrderByDescending(n => n.Velocity).ThenByDescending(n => n.Pitch).Take(room).ToList();
                if (!highest.ToHashSet().SetEquals(loudest))
                    differs++;
                chosen.AddRange((arr.Loudest ? loudest : highest).OrderByDescending(n => n.Pitch).Select(n => (n, false)));
                int droppedHere = mel.Count + acc.Count - chosen.Count, foldedHere = 0;

                List<string> tokens = new();
                HashSet<string> used = new();
                foreach (var (n, isMelody) in chosen)
                {
                    int? octave = arr.For(n.Track).Octave;
                    int p = n.Pitch + (octave != null ? octave.Value * 12 : isMelody ? melodyShift : accompShift);
                    int f = p;
                    while (f < Low) f += 12;
                    while (f > High) f -= 12;
                    if (f != p) foldedHere++;
                    int voice = Array.IndexOf(voicePrefix, Prefix(n, isMelody)), note = f - Low;
                    if (!used.Add(voicePrefix[voice] + Notes[note]))
                    {
                        droppedHere++;      // folding made it the same note as another
                        continue;
                    }
                    bool locked = Locked(voice, note, at), moving = false;
                    if (locked && arr.Alternate && !Locked(partner[voice], note, at) && !used.Contains(voicePrefix[partner[voice]] + Notes[note]))
                    {
                        voice = partner[voice];
                        locked = false;
                        moving = true;
                    }
                    string token = voicePrefix[voice] + Notes[note];
                    used.Add(token);
                    tokens.Add(token);
                    kept++;
                    if (moving)
                        moved++;
                    if (locked)
                        skipped++;      // the client will skip it
                    else
                        started[(voice, note)] = at;
                }
                tokens.Reverse();       // accompaniment first, the melody note last
                plays.Add((tick, tokens));
                dropped += droppedHere;
                folded += foldedHere;
                if (droppedHere > 0 || foldedHere > 0)
                    Note(tick, droppedHere, foldedHere);
            }

            // the drums (Drums: on): each General MIDI drum hit (channel 10) as its role's sound effect, at most Drums at
            //  once at a moment (by role: kick, snare, cymbal, hi-hat, toms), written first in its moment as Galois
            //  wrote them. A role without a sound, and drums without a role, are left out (counted). A track with a drum
            //  role (a drum written as notes, as notation programs do: Role: kick) is that drum, whatever its notes.
            List<Midi.Note> drumNotes = midi.Notes.Where(n => arr.For(n.Track).Role != "off"
                && (n.Channel == 9 || Arrangement.DrumRole(arr.For(n.Track).Role) != null)).ToList();
            int[] drumHits = new int[Arrangement.DrumRoles.Length];
            int drumsUnassigned = 0, drumsCrowded = 0;
            if (arr.Drums)
            {
                int? RoleOf(int key) => key is 35 or 36 ? 0 : key is >= 37 and <= 40 ? 1 : key is 42 or 44 or 46 ? 2
                    : key is 49 or 51 or 52 or 53 or 55 or 57 or 59 ? 3 : key is 41 or 43 or 45 or 47 or 48 or 50 ? 4 : null;
                int[] priority = { 0, 1, 3, 2, 4 };         // kick, snare, cymbal, hi-hat, toms
                foreach (var hits in drumNotes.GroupBy(n => n.Tick).OrderBy(g => g.Key))
                {
                    List<int> roles = new();
                    foreach (Midi.Note n in hits)
                        if ((Arrangement.DrumRole(arr.For(n.Track).Role) ?? RoleOf(n.Pitch)) is int r && arr.Kit[r] != null)
                        {
                            if (!roles.Contains(r))
                                roles.Add(r);
                        }
                        else
                            drumsUnassigned++;
                    List<int> playing = roles.OrderBy(r => Array.IndexOf(priority, r)).Take(arr.DrumsAtOnce).ToList();
                    drumsCrowded += roles.Count - playing.Count;
                    if (playing.Count == 0)
                        continue;
                    List<string> sounds = playing.Select(r => "+" + arr.Kit[r]).Distinct().ToList();
                    foreach (int r in playing)
                        drumHits[r]++;
                    int at = plays.FindIndex(p => p.Tick == hits.Key);
                    if (at >= 0)
                        plays[at].Tokens.InsertRange(0, sounds);
                    else
                        plays.Add((hits.Key, sounds));
                }
                plays.Sort((x, y) => x.Tick.CompareTo(y.Tick));
            }

            // the composition
            string song = arr.Song ?? Path.GetFileNameWithoutExtension(midiPath);
            StringBuilder rzr = new();
            rzr.AppendLine("# Song: " + song);
            rzr.AppendLine("# Author: " + (arr.ArrangedBy ?? "MidiToAims"));
            if (!string.IsNullOrWhiteSpace(arr.Composer))
                rzr.AppendLine("# Composer: " + arr.Composer);
            rzr.AppendLine("# Instrument: LapHarp");
            rzr.AppendLine("# Timer: " + arr.Timer);
            rzr.AppendLine("# Prefetch: True");
            rzr.AppendLine("# Tempo: 0");
            rzr.AppendLine("# ConcertMode: False");
            rzr.AppendLine("# Source: MIDI " + Path.GetFileName(midiPath) + " (arranged with MidiToAims)");

            long blockTicks = midi.BarTicks * arr.BarsPerBlock;
            double total = 0;
            int lines = 0;
            for (int i = 0; i < plays.Count;)
            {
                long block = plays[i].Tick / blockTicks;
                List<string> tokens = new();
                double blockSeconds = 0;
                for (; i < plays.Count && plays[i].Tick / blockTicks == block; i++)
                {
                    // from rounded absolute times, so rounding never accumulates
                    long next = i + 1 < plays.Count ? plays[i + 1].Tick : plays[i].Tick + midi.Division;
                    double pause = Math.Round(Math.Round(midi.Seconds(next), 2) - Math.Round(midi.Seconds(plays[i].Tick), 2), 2);
                    tokens.AddRange(plays[i].Tokens);
                    tokens.Add(pause.ToString("0.##", CultureInfo.InvariantCulture));
                    blockSeconds += pause;
                }
                // a [play line of at most 16 tokens, split after a pause
                List<string> line = new();
                foreach (string t in tokens)
                {
                    line.Add(t);
                    if (line.Count >= 16 && char.IsDigit(t[0]))
                    {
                        rzr.AppendLine("say '[play " + string.Join(" ", line) + "'");
                        lines++;
                        line.Clear();
                    }
                }
                if (line.Count > 0)
                {
                    rzr.AppendLine("say '[play " + string.Join(" ", line) + "'");
                    lines++;
                }
                rzr.AppendLine("wait " + (int)Math.Round(blockSeconds * 1000));
                total += blockSeconds;
            }

            // the books
            string[] books = Books.Split(rzr.ToString(), song, arr.BookPages);
            // the posts per book (Format: post); none for whole
            int[] posts = books.Select(b => Books.Posts(b).Length).ToArray();
            string bookSummary = books.Length == 0
                ? string.Format("too long for {0} books of {1} pages", Books.MaxBooks, arr.BookPages)
                : arr.Format == "whole"
                ? string.Format("{0} book{1} ({2} pages)", books.Length, books.Length == 1 ? "" : "s", string.Join(", ", books.Select(Books.Pages)))
                : string.Format("{0} book{1} ({2} pages; {3} post{4})", books.Length, books.Length == 1 ? "" : "s",
                    string.Join(", ", books.Select(Books.Pages)), string.Join(", ", posts), posts.Length == 1 && posts[0] == 1 ? "" : "s");

            // the report: the summary, then where the music was changed to fit
            string voices = topNoteMode ? "melody: the top note at each moment" : "melody: track " + string.Join(", ", melodyTracks.OrderBy(t => t));
            report.Insert(0, string.Format("{0}: {1} notes ({2} kept, {3} dropped, {4} folded by octave); {5}; melody octave {6:+0;-0;0}, accompaniment {7:+0;-0;0}; {8} [play lines, {9:F0} s, {10:F1} KB; {11}{12}",
                song, notes.Count, kept, dropped, folded, voices, melodyShift / 12, accompShift / 12, lines, total, rzr.Length / 1024.0, bookSummary, Environment.NewLine)
                + arr.Summary(song) + Environment.NewLine
                + (arr.Alternate && (moved > 0 || skipped > 0)
                    ? string.Format("Repeated notes: {0} moved to the other harp, so the game plays them{1}{2}", moved,
                        skipped > 0 ? string.Format("; {0} still too fast (the other harp was busy too): the game skips them", skipped) : "", Environment.NewLine)
                    : !arr.Alternate && skipped > 0
                    ? string.Format("Repeated notes: {0} repeat too fast on one instrument, and the game skips them. 'Repeated notes: alternate' in the .m2a moves them to the other harp.{1}", skipped, Environment.NewLine)
                    : "")
                + (drumNotes.Count == 0 ? ""
                    : !arr.Drums ? string.Format("Drums: the MIDI has {0} drum hits, left out; 'Drums: on' in the .m2a plays them (free percussion sounds).{1}", drumNotes.Count, Environment.NewLine)
                    : string.Format("Drums: {0} hits ({1}){2}{3}{4}", drumHits.Sum(),
                        string.Join(", ", Enumerable.Range(0, drumHits.Length).Where(r => drumHits[r] > 0).Select(r => Arrangement.DrumRoles[r].ToLowerInvariant() + " " + drumHits[r])),
                        drumsUnassigned > 0 ? string.Format("; {0} left out (toms, or a drum without a sound)", drumsUnassigned) : "",
                        drumsCrowded > 0 ? string.Format("; {0} at the same moment as another (Drums at once: {1})", drumsCrowded, arr.DrumsAtOnce) : "",
                        Environment.NewLine))
                + (differs == 0 ? ""
                    : arr.Loudest ? string.Format("Inner notes: in {0} moments the loudest notes were kept, not the highest.{1}", differs, Environment.NewLine)
                    : string.Format("Inner notes: in {0} moments louder notes were dropped for higher ones; 'Inner notes: loudest' in the .m2a keeps the loudest.{1}", differs, Environment.NewLine)));
            foreach (var (bar, (d, f)) in byBar.Take(60))
                report.AppendLine(string.Format("bar {0}: {1}{2}{3}", bar, d > 0 ? d + " dropped" : "", d > 0 && f > 0 ? ", " : "", f > 0 ? f + " folded by octave" : ""));
            if (byBar.Count > 60)
                report.AppendLine(string.Format("... and {0} more bars", byBar.Count - 60));
            return new[] { rzr.ToString(), report.ToString() }.Concat(books).ToArray();
        }
    }
}
