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

/* Scripts\Engines\AIMusicShare\MusicCatalog.cs
 * ChangeLog
 *  11/16/26, Adam (About and Tags)
 *      catalog.json gives each song's about (its # About: line) and tags (its # Tags:, as a list), or null, for the page
 *      to show or search. The .rzr already carries both lines: they are in the composition.
 *  11/16/26, Adam (audio code)
 *      A song's audio code counts what it plays (Sound): its notes and the directives that change how they play
 *      (# Timer:, # Tempo:, # Instrument: ...), not its other # lines (# Song:, # Author:, # Price:, About, Tags,
 *      comments). Renaming a song rewrites its # Song: and # Author: lines, which made the catalog render its audio again
 *      (the 76 renames of 11/16/26 did); now it keeps its audio. Every code changes once with this, so every song is
 *      heard once more at the next publish (AudioRender stays 2: the code itself changed).
 *  11/16/26, Adam (directives)
 *      A book names a directive only at the start of its line now (MusicCompiler.Directive), so the .rzr drops only the
 *      composition's own header lines the database replaces (# Song:, # Author: ...), never a line that just holds such
 *      a word (a # About: that says "song:"). The rule below, that a comment line must not contain a directive name, is
 *      only for AIMS before 11/16/26.
 *  11/15/26, Adam (game timing)
 *      AudioRender (2) is part of each song's audio code: MidiToAims now plays Classic and Dynamic songs as slowly as
 *      the game does (they rushed on the page), so every song is heard again, once, under a new name; the old audio
 *      is deleted after. Raise it whenever MidiToAims hears songs differently.
 *  11/15/26, Adam (audio)
 *      Each song can be heard on the page: before catalog.json is written, every song without audio is rendered by
 *      MidiToAims (preview --wav, the client's own sounds) and made an MP3 by ffmpeg (96 kbps mono) into
 *      <folder>\audio\<code>.mp3; catalog.json's audio names it. The code is taken from what the song sounds like
 *      (composition, instrument, tempo, timer settings), so a song is rendered once and again only when its sound
 *      changes; audio no song uses is deleted after. Tools (server utilities, not part of the install): MidiToAims.exe in
 *      Tools\MidiToAims beside the server or on the PATH (C:\bin), ffmpeg in Tools\ffmpeg, the PATH or winget's
 *      install. No ffmpeg: the WAV is kept and played. No MidiToAims: no audio. A song
 *      that fails is logged ([catalog audio] in the music share log) and not tried again until a restart. A publish
 *      renders for at most 5 minutes; the rest wait for the next (the first run: about 5 seconds a song).
 *  11/14/26, Adam (arranged by)
 *      catalog.json names a song's arranger (arranger: its composer, when the Author names someone else and isn't a
 *      name the composer's account composed under), for the page to show under the composer.
 *  11/14/26, Adam (music import)
 *      For other shards to import the library ([MusicImport, AIMS): a .rzr carries every setting a book reads, its price
 *      too (# Price:, # Napster:, Angel Island's: a suggestion), and the price comment names no setting ("Suggested price
 *      from the Angel Island shards; your shard may change it."). aims-library.zip: every .rzr and a README (put them in
 *      Data\Music Import, type [MusicImport), written with the catalog when it changes; catalog.json's zip gives its
 *      file and size for the page's "Download all".
 *  11/14/26, Adam (composed on)
 *      The shard a song was composed on (Songs.origin) replaces its home shard: catalog.json's composedOn (home is gone:
 *      readers want who wrote what, when and where) and the .rzr comment ("composed on Siege Perilous, added
 *      2023-07-15"). A song whose origin isn't set yet shows none.
 *  11/13/26, Adam
 *      Created. The music catalog for the website: every approved song in the shared music database, free to download
 *      as a .rzr file (the composers agreed), with catalog.json listing them for the page to show. Angel Island's,
 *      beside music sharing, not part of AIMS.
 *      On when the server starts with -publishmusic=<folder> (Core.PublishMusic): the production login server, whose
 *      cron publishes every 15 minutes and once at startup; [MusicShare export publishes now. The folder belongs to the
 *      catalog: <folder>\catalog.json and <folder>\rzr\*.rzr. Nothing is written when nothing changed; a .rzr is
 *      written before catalog.json names it, and one no longer listed is deleted after, so the page never links a
 *      missing file. Each file is written whole and then moved into place. File names are the title, with what Windows
 *      or a web link can't take (+ % # ? & ; too: IIS refuses '+' and '%' in a path) made spaces. A date before 2000
 *      is "never recorded" (older songs hold DateTime.MinValue, a few hours past zero) and isn't shown. "Added" is the
 *      approval date, or the submission date for songs approved before approval dates were kept (2022-2024 songs).
 *      A .rzr is the song as the shards play it: a header from the database (# Song:, # Author:, # Instrument:,
 *      # Tempo:, # NewTimer:, # Prefetch:, # ConcertMode:), a comment naming its home shard, approval and Angel Island's
 *      price (a suggestion: each shard prices its own), then the composition, less the header lines the database
 *      replaces. A comment line must not contain a directive name (song:, author:, price: ...): music composition
 *      books read them anywhere in a line.
 *      Reads the database only, and never creates it (MusicShareDatabase.IsSeeded first). Off the game thread.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.Engines.MusicShare
{
    public static class MusicCatalog
    {
        /// <summary>The folder the catalog is published to (-publishmusic=folder), or null: not published here.</summary>
        public static string? Folder => string.IsNullOrWhiteSpace(Core.PublishMusic) ? null : Core.PublishMusic;

        public static bool Enabled => Folder != null;

        private static volatile bool m_Busy;
        private static string m_Last = "not yet published since the server started";

        /// <summary>What the last publish did, for [MusicShare status.</summary>
        public static string LastResult => m_Last;

        /// <summary>Publish now (cron, or [MusicShare export). Reports to 'from' if given.</summary>
        public static void Publish(Mobile? from)
        {
            string? folder = Folder;
            if (folder == null)
            {
                from?.SendMessage("MusicShare: this server doesn't publish the music catalog (start it with -publishmusic=<folder>).");
                return;
            }
            if (m_Busy)
            {
                from?.SendMessage("MusicShare: the catalog is being published already.");
                return;
            }
            m_Busy = true;
            from?.SendMessage("MusicShare: publishing the music catalog to {0}...", folder);
            Task.Run(() =>
            {
                string result;
                try
                {
                    result = Write(folder);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "MusicCatalog: publishing to " + folder);
                    result = "failed: " + ex.Message;
                }
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    m_Busy = false;
                    m_Last = string.Format("{0:yyyy-MM-dd HH:mm} UTC: {1}", DateTime.UtcNow, result);
                    foreach (string line in TakeAudioLog())         // what went wrong making a song's audio, if anything
                        AimsHost.Current.WriteLog(MusicShare.LogFile, null, new object[] { "[catalog audio] " + line });
                    AimsHost.Current.WriteLog(MusicShare.LogFile, null, new object[] { "[catalog] " + result });
                    from?.SendMessage("MusicShare: catalog {0}", result);
                });
            });
        }

        #region Writing
        private sealed class Entry
        {
            public SongRow Row = null!;
            public string File = "";
            public string Text = "";
            public string? Audio;       // "audio/<code>.mp3" (or .wav), when the song has been heard; null: not yet
        }

        /// <summary>Writes the catalog into 'folder' if anything changed; returns what it did. Off the game thread.</summary>
        public static string Write(string folder)
        {
            if (MusicShareDatabase.IsSeeded() != true)
                return "not published: no shared music database at " + MusicShareDatabase.DatabasePath;
            List<SongRow>? rows = MusicShareDatabase.GetSongs();
            if (rows == null)
                return "not published: the shared music database couldn't be read";
            return Write(folder, rows);
        }

        /// <summary>Writes the catalog of these songs (the database's, or a harness's few) into 'folder' if anything changed.</summary>
        public static string Write(string folder, List<SongRow> rows)
        {
            List<Entry> entries = Build(rows);
            string audio = Audio(folder, entries);      // the songs heard first: catalog.json names only audio that's there
            string version = Version(entries);
            string rzrFolder = Path.Combine(folder, "rzr");
            string catalogFile = Path.Combine(folder, "catalog.json");
            if (PublishedVersion(catalogFile) == version && File.Exists(Path.Combine(folder, ZipFile)))
                return string.Format("unchanged ({0} songs); {1}", entries.Count, audio);

            Directory.CreateDirectory(rzrFolder);

            // the songs first: catalog.json never names a file that isn't there
            int written = 0;
            foreach (Entry e in entries)
                if (WriteIfChanged(Path.Combine(rzrFolder, e.File), e.Text))
                    ++written;
            long zipBytes = WriteZip(Path.Combine(folder, ZipFile), entries);

            WriteIfChanged(catalogFile, Catalog(entries, version, zipBytes));

            // then what the catalog no longer lists
            HashSet<string> keep = new HashSet<string>(entries.Select(e => e.File), StringComparer.OrdinalIgnoreCase);
            int removed = 0;
            foreach (string file in Directory.GetFiles(rzrFolder, "*.rzr"))
            {
                if (keep.Contains(Path.GetFileName(file)))
                    continue;
                File.Delete(file);
                ++removed;
            }
            removed += RemoveStaleAudio(folder, entries);

            return string.Format("published: {0} songs ({1} written, {2} removed); {3}", entries.Count, written, removed, audio);
        }

        // the approved songs, each with its file name (unique, stable: by title, then the earliest approved keeps the plain name)
        private static List<Entry> Build(List<SongRow> rows)
        {
            const int approved = (int)MusicLibrary.SubmissionStatus.Approved;
            List<Entry> entries = new List<Entry>();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SongRow r in rows.Where(r => !r.Deleted && r.Status == approved && r.Composition.Trim().Length > 0)
                .OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.DateApproved).ThenBy(r => r.Hash))
            {
                string name = FileName(r.Title);
                if (names.Contains(name + ".rzr"))
                    name = FileName(r.Title + " - " + r.Author);
                string unique = name;
                for (int n = 2; names.Contains(unique + ".rzr"); ++n)
                    unique = string.Format("{0} ({1})", name, n);
                names.Add(unique + ".rzr");
                entries.Add(new Entry { Row = r, File = unique + ".rzr", Text = Rzr(r) });
            }
            return entries;
        }

        private static string FileName(string title)
        {
            // and what a web link trips on: IIS refuses '+' and '%' in a path (404.11), '#' and '?' end it, '&' and ';' confuse it
            HashSet<char> bad = new HashSet<char>(Path.GetInvalidFileNameChars().Concat("+%#?&;"));
            string name = new string(title.Select(c => bad.Contains(c) ? ' ' : c).ToArray());
            name = string.Join(" ", name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Trim('.', ' ');
            if (name.Length > 80)
                name = name.Substring(0, 80).TrimEnd('.', ' ');
            return name.Length > 0 ? name : "Untitled";
        }

        // the directives the header above replaces (a book names a directive at the start of its line: MusicCompiler.Directive)
        private static readonly string[] Directives =
            { "song", "author", "instrument", "tempo", "newtimer", "prefetch", "concertmode", "price", "napster" };

        private static bool IsDirective(string line) =>
            MusicCompiler.Directive(line, out string name, out _) && Directives.Contains(name);

        /// <summary>The song as a .rzr file: the database's header, a comment about it, then the composition.</summary>
        public static string Rzr(SongRow r)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("# Song: ").Append(r.Title).Append("\r\n");
            sb.Append("# Author: ").Append(r.Author).Append("\r\n");
            sb.Append("# Instrument: ").Append(r.Instrument == "lapharp" ? "LapHarp" : r.Instrument == "lute" ? "Lute" : "Harp").Append("\r\n");
            sb.Append("# Tempo: ").Append(r.Tempo).Append("\r\n");
            sb.Append("# NewTimer: ").Append(r.NewTimer ? "True" : "False").Append("\r\n");
            sb.Append("# Prefetch: ").Append(r.Prefetch ? "True" : "False").Append("\r\n");
            sb.Append("# ConcertMode: ").Append(r.ConcertMode ? "True" : "False").Append("\r\n");
            // Angel Island's price, a suggestion (a book, or [MusicImport, sets it; a Napster song can't be priced)
            sb.Append("# Price: ").Append(r.Napster ? 0 : r.Price).Append("\r\n");
            sb.Append("# Napster: ").Append(r.Napster ? "True" : "False").Append("\r\n");
            // comment lines: a book reads a directive only at the start of a line, so these name none
            List<string> facts = new List<string>();
            if (r.Origin.Length > 0)
                facts.Add("composed on " + MusicShare.ShardName(r.Origin));
            if (Added(r) is string added)
                facts.Add("added " + added);
            if (Day(r.DateRemastered) is string remastered)
                facts.Add("remastered " + remastered);
            sb.Append("# From the Angel Island shards").Append(facts.Count > 0 ? ": " + string.Join(", ", facts) : "").Append(".\r\n");
            sb.Append("# Suggested price from the Angel Island shards; your shard may change it.\r\n");

            // the composition, less the header lines above it that the database's replace (a later one would win)
            bool header = true;
            foreach (string line in r.Composition.Replace("\r\n", "\n").Split('\n'))
            {
                string t = line.Trim();
                if (header && t.Length > 0 && !t.StartsWith("#"))
                    header = false;
                if (header && t.StartsWith("#") && IsDirective(t))
                    continue;
                sb.Append(line.TrimEnd('\r')).Append("\r\n");
            }
            return sb.ToString();
        }

        // what the catalog holds, for "has anything changed": every song's file name and text
        private static string Version(List<Entry> entries)
        {
            using SHA256 sha = SHA256.Create();
            StringBuilder all = new StringBuilder();
            foreach (Entry e in entries)
                all.Append(e.File).Append('\0').Append(e.Text).Append('\0')
                    .Append(e.Row.Napster).Append(e.Row.Price).Append('\0').Append(e.Audio).Append('\0');
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(all.ToString()))).Substring(0, 16).ToLowerInvariant();
        }

        private static string? PublishedVersion(string catalogFile)
        {
            try
            {
                if (!File.Exists(catalogFile))
                    return null;
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(catalogFile));
                return doc.RootElement.TryGetProperty("version", out JsonElement v) ? v.GetString() : null;
            }
            catch (Exception ex) when (ex is IOException || ex is JsonException || ex is UnauthorizedAccessException)
            {
                return null;    // unreadable: write it again
            }
        }

        // a database date (UTC ticks) as yyyy-MM-dd, or null: never recorded. Songs from before the shards kept the date
        //  hold DateTime.MinValue, which arrives a few hours past zero (a time zone moved it), so not "> 0": "after 2000".
        private static string? Day(long ticks)
        {
            DateTime day = new DateTime(Math.Max(0, ticks), DateTimeKind.Utc);
            return day.Year >= 2000 ? day.ToString("yyyy-MM-dd") : null;
        }

        // when the song joined the library: approved, or submitted for songs approved before the shards kept that date
        private static string? Added(SongRow r)
        {
            return Day(r.DateApproved) ?? Day(r.DateSubmitted);
        }

        /// <summary>Every song in one file, for a shard to import ([MusicImport): the .rzr files and a README.</summary>
        public const string ZipFile = "aims-library.zip";

        // the zip: written whole beside it, then moved into place; returns its size
        private static long WriteZip(string path, List<Entry> entries)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (System.IO.Compression.ZipArchive zip = new System.IO.Compression.ZipArchive(memory, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
                {
                    void Add(string name, string text)
                    {
                        System.IO.Compression.ZipArchiveEntry entry = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.Optimal);
                        using StreamWriter writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                        writer.Write(text);
                    }
                    Add("README.txt", ZipReadme(entries.Count));
                    foreach (Entry e in entries)
                        Add(e.File, e.Text);
                }
                string temp = path + ".new";
                File.WriteAllBytes(temp, memory.ToArray());
                File.Move(temp, path, overwrite: true);
                return memory.Length;
            }
        }

        private static string ZipReadme(int count)
        {
            return string.Join("\r\n",
                "The Angel Island Music Library (https://game-master.net/AimsMusic.html)",
                string.Format("{0} songs composed by the players of the Angel Island shards, published {1:yyyy-MM-dd}.", count, DateTime.UtcNow),
                "Every song is free to download; the composers agreed to share their music.",
                "",
                "For a shard running AIMS, the Angel Island Music System",
                "(https://github.com/Luke-Tomasello/Angel-Island-Music-System):",
                "  1. Put these .rzr files in your server's Data\\Music Import folder.",
                "  2. In game, as an Administrator, type: [MusicImport",
                "New songs join your music library, approved. Songs you already have are left as they are, and a new",
                "version of a song you imported before replaces it, so you can import a newer download the same way.",
                "Each song carries the price Angel Island suggests (# Price:); your shard sets its own: change any",
                "afterwards, or type [MusicImport free to add them all free. Imported songs pay no one (no one on your",
                "shard composed them): the gold a player spends on one leaves your economy.",
                "",
                "To play one yourself: open it in a text editor, copy all of it, and paste it into a blank music",
                "composition book in game (single-click the book, Paste). One post holds 250 lines: paste a longer song",
                "in pieces, in order.",
                "");
        }

        private static string Catalog(List<Entry> entries, string version, long zipBytes)
        {
            // the names each composer's account has composed under: an Author that is one of them names no arranger
            Dictionary<uint, HashSet<string>> composedAs = new Dictionary<uint, HashSet<string>>();
            foreach (Entry e in entries)
            {
                if (!composedAs.TryGetValue(e.Row.ComposerGuid, out HashSet<string>? names))
                    composedAs[e.Row.ComposerGuid] = names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                names.Add(AimsHost.PlainName(e.Row.ComposerName));
            }

            var catalog = new
            {
                version,
                published = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                count = entries.Count,
                zip = new { file = ZipFile, bytes = zipBytes },
                songs = entries.Select(e => new
                {
                    title = e.Row.Title,
                    author = e.Row.Author,
                    arranger = Arranger(e.Row, composedAs),
                    file = "rzr/" + e.File,
                    audio = e.Audio,                                // the song as the game plays it, or null: not yet
                    instrument = e.Row.Instrument == "lapharp" ? "lap harp" : e.Row.Instrument,
                    composedOn = e.Row.Origin.Length > 0 ? MusicShare.ShardName(e.Row.Origin) : null,
                    added = Added(e.Row),
                    remastered = Day(e.Row.DateRemastered),
                    price = e.Row.Napster ? 0 : e.Row.Price,       // Angel Island's: a suggestion
                    napster = e.Row.Napster,
                    // the song's # About: and # Tags: lines (RolledUpSheetMusic.About, Tags), or null: for the page to show
                    //  or search
                    about = NullIfEmpty(RolledUpSheetMusic.NotesLine(e.Row.Composition, "about")),
                    tags = TagsOf(e.Row.Composition),
                }),
            };
            return JsonSerializer.Serialize(catalog, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,     // titles readable as written (é, not é)
            });
        }

        private static string? NullIfEmpty(string text) => text.Length > 0 ? text : null;

        // a song's tags, tidied, or null: none
        private static string[]? TagsOf(string composition)
        {
            string tags = RolledUpSheetMusic.TidyTags(RolledUpSheetMusic.NotesLine(composition, "tags"));
            return tags.Length > 0 ? tags.Split(new[] { ", " }, StringSplitOptions.None) : null;
        }

        // who arranged a song, as the music box says it (AimsHost.Arranger): its composer, when the Author names someone
        //  else. Here only the database's names are known: an Author that is a name the composer's account composed under
        //  names no one (another account of theirs, or a household, isn't known here). Null: no arranger.
        public static string? Arranger(SongRow r, Dictionary<uint, HashSet<string>> composedAs)
        {
            string author = AimsHost.PlainName(r.Author), composer = AimsHost.PlainName(r.ComposerName);
            if (author.Length == 0 || composer.Length == 0 || AimsHost.SameName(author, composer)
                || (composedAs.TryGetValue(r.ComposerGuid, out HashSet<string>? names) && names.Contains(author)))
                return null;
            return composer;
        }

        #region Audio
        // Each song heard as the game plays it, for the page's play button: MidiToAims renders the .rzr to a WAV (the
        //  client's own sounds: sound.mul, found as the server finds its client files), and ffmpeg makes an MP3 of it.
        //  <folder>\audio\<code>.mp3, the code taken from what the song sounds like (AudioCode), so a song is heard once
        //  and again only when its sound changes. No ffmpeg: the WAV is kept (and made an MP3 once ffmpeg is there). No
        //  MidiToAims: no audio (the page shows no play button). A song that fails is logged and not tried again until
        //  the server restarts. Rendering stops for this publish after AudioBudget; the rest wait for the next one.

        public const string AudioFolder = "audio";
        public static readonly TimeSpan AudioBudget = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan ToolLimit = TimeSpan.FromMinutes(3);    // one song, one program
        public const string Bitrate = "96k";                                    // mono: far more than the client's sounds need

        /// <summary>MidiToAims.exe, if set (harnesses); else Tools\MidiToAims beside the server, or the PATH (C:\bin).</summary>
        public static string? MidiToAimsPath;

        /// <summary>ffmpeg.exe, if set (harnesses); else Tools\ffmpeg, the PATH, or winget's install. "" : none.</summary>
        public static string? FfmpegPath;

        private static readonly HashSet<string> m_AudioFailed = new HashSet<string>();     // codes, until a restart
        private static readonly List<string> m_AudioLog = new List<string>();

        private static void AudioLog(string format, params object[] args)
        {
            lock (m_AudioLog)
                m_AudioLog.Add(string.Format(format, args));
        }

        private static List<string> TakeAudioLog()
        {
            lock (m_AudioLog)
            {
                List<string> lines = new List<string>(m_AudioLog);
                m_AudioLog.Clear();
                return lines;
            }
        }

        // the way MidiToAims hears songs: raised when that changes, so every song is heard again (its audio is named anew,
        //  the old deleted). 2: Classic and Dynamic songs as slow as the game plays them (11/15/26; the first audio's
        //  codes had no number).
        //  The new MidiToAims must be on the server before the catalog publishes, or the songs are heard the old way
        //  under the new names (then delete <folder>\audio to hear them again).
        //  (11/16/26: AudioCode stopped counting the # lines that don't change the sound; every code changed once, and
        //  every song was heard once more, with no need to raise this.)
        public const int AudioRender = 2;

        /// <summary>
        /// The song's sound as a code: what it plays (Sound: its notes and the directives that change how they play) and the
        /// settings it plays with, and AudioRender. Not its title, author, price, About, Tags or comments: renaming a song
        /// rewrites its # Song: and # Author: lines, and must not make it heard again.
        /// </summary>
        public static string AudioCode(SongRow r)
        {
            string sound = string.Join("\0", Sound(r.Composition), r.Instrument, r.Tempo, r.NewTimer, r.Prefetch, r.ConcertMode,
                AudioRender);
            using SHA256 sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sound))).Substring(0, 16).ToLowerInvariant();
        }

        // the directives that change how a song plays (RazorInstrument.ParseDirectives): the rest of a song's # lines don't
        private static readonly HashSet<string> SoundDirectives =
            new HashSet<string> { "timer", "instrument", "tempo", "newtimer", "prefetch", "concertmode" };

        /// <summary>A composition as it sounds: every line but the # lines that don't change how it plays (names, price,
        /// About, Tags, comments). '# Timer: Anchored', and a # Tempo: or # Instrument: anywhere in it, stay.</summary>
        public static string Sound(string composition)
        {
            return string.Join("\n", (composition ?? "").Replace("\r\n", "\n").Split('\n')
                .Where(l => !l.TrimStart().StartsWith("#") || MusicCompiler.Directive(l, out string name, out _) && SoundDirectives.Contains(name)));
        }

        public static string? FindMidiToAims()
        {
            if (MidiToAimsPath != null)
                return File.Exists(MidiToAimsPath) ? MidiToAimsPath : null;
            string tools = Path.Combine(AppContext.BaseDirectory, "Tools");
            List<string> places = new List<string> { Path.Combine(tools, "MidiToAims", "MidiToAims.exe"), Path.Combine(tools, "MidiToAims.exe") };
            // a server utility like ffmpeg: anywhere on the PATH (C:\bin on Angel Island's machines)
            foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
                places.Add(Path.Combine(dir.Trim().Trim('"'), "MidiToAims.exe"));
            return places.FirstOrDefault(p => { try { return File.Exists(p); } catch { return false; } });
        }

        public static string? FindFfmpeg()
        {
            if (FfmpegPath != null)
                return FfmpegPath.Length > 0 && File.Exists(FfmpegPath) ? FfmpegPath : null;
            string tools = Path.Combine(AppContext.BaseDirectory, "Tools", "ffmpeg");
            List<string> places = new List<string> { Path.Combine(tools, "ffmpeg.exe"), Path.Combine(tools, "bin", "ffmpeg.exe") };
            foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
                places.Add(Path.Combine(dir.Trim().Trim('"'), "ffmpeg.exe"));
            // winget (Gyan.FFmpeg...): a server started before the install doesn't have the new PATH
            string winget = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet");
            places.Add(Path.Combine(winget, "Links", "ffmpeg.exe"));
            try
            {
                string packages = Path.Combine(winget, "Packages");
                if (Directory.Exists(packages))
                    foreach (string package in Directory.GetDirectories(packages, "Gyan.FFmpeg*"))
                        places.AddRange(Directory.GetFiles(package, "ffmpeg.exe", SearchOption.AllDirectories));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
            return places.FirstOrDefault(p => { try { return File.Exists(p); } catch { return false; } });
        }

        // where the client's sounds are: the folder the server reads its client files from (null: MidiToAims looks itself)
        private static string? ClientFolder()
        {
            try
            {
                string path = Core.FindDataFile("sound.mul");
                return File.Exists(path) ? Path.GetDirectoryName(path) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // every song's audio: made if missing (within AudioBudget), then named on its entry; what happened, for the result
        private static string Audio(string folder, List<Entry> entries)
        {
            string audioFolder = Path.Combine(folder, AudioFolder);
            string? m2a = FindMidiToAims(), ffmpeg = FindFfmpeg(), client = ClientFolder();
            Directory.CreateDirectory(audioFolder);
            int rendered = 0, failed = 0, waiting = 0;
            System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
            foreach (Entry e in entries)
            {
                string code = AudioCode(e.Row);
                string mp3 = Path.Combine(audioFolder, code + ".mp3"), wav = Path.Combine(audioFolder, code + ".wav");
                bool needed = !File.Exists(mp3) && !(File.Exists(wav) && ffmpeg == null);
                if (needed && m2a != null && !m_AudioFailed.Contains(code))
                {
                    if (clock.Elapsed > AudioBudget)
                        ++waiting;
                    else if (Hear(e, code, audioFolder, m2a, ffmpeg, client))
                        ++rendered;
                    else
                    {
                        ++failed;
                        m_AudioFailed.Add(code);
                    }
                }
                e.Audio = File.Exists(mp3) ? AudioFolder + "/" + code + ".mp3" : File.Exists(wav) ? AudioFolder + "/" + code + ".wav" : null;
            }

            int heard = entries.Count(e => e.Audio != null);
            string text = string.Format("audio {0} of {1}", heard, entries.Count);
            if (rendered + failed + waiting > 0)
                text += string.Format(" ({0} made, {1} failed, {2} for the next publish)", rendered, failed, waiting);
            if (m2a == null)
                text += " (no MidiToAims.exe: put it on the PATH, e.g. C:\\bin)";
            else if (ffmpeg == null)
                text += " (no ffmpeg: kept as WAV)";
            if (m_AudioFailed.Count > 0)
                text += string.Format(" ({0} not tried again until a restart)", m_AudioFailed.Count);
            return text;
        }

        // one song: MidiToAims renders it (or the kept WAV is used), ffmpeg makes the MP3; true: its audio is there
        private static bool Hear(Entry e, string code, string audioFolder, string m2a, string? ffmpeg, string? client)
        {
            string wav = Path.Combine(audioFolder, code + ".wav"), mp3 = Path.Combine(audioFolder, code + ".mp3");
            string temp = Path.Combine(Path.GetTempPath(), "AimsCatalogAudio", code);
            try
            {
                Directory.CreateDirectory(temp);
                string source = wav;
                if (!File.Exists(wav))
                {   // the .rzr in a folder of its own: MidiToAims reads a song's pieces (.post1 ...) from beside it
                    string rzr = Path.Combine(temp, "song.rzr");
                    source = Path.Combine(temp, "song.wav");
                    File.WriteAllText(rzr, e.Text, new UTF8Encoding(false));
                    List<string> args = new List<string> { "preview", rzr, "--wav", source };
                    if (client != null)
                        args.AddRange(new[] { "--uo", client });
                    var (exit, output) = Run(m2a, args);
                    if (exit != 0 || !File.Exists(source))
                    {
                        AudioLog("{0} ({1}): MidiToAims failed ({2}): {3}", e.Row.Title, code, exit == -1 ? "took too long" : "exit " + exit, Tail(output));
                        return false;
                    }
                    if (ffmpeg == null)
                    {
                        File.Move(source, wav, overwrite: true);
                        return true;
                    }
                }

                string made = mp3 + ".new";
                var (fexit, foutput) = Run(ffmpeg!, new List<string> { "-hide_banner", "-loglevel", "error", "-y", "-i", source,
                    "-codec:a", "libmp3lame", "-b:a", Bitrate, "-ac", "1", "-f", "mp3", made });
                if (fexit != 0 || !File.Exists(made))
                {   // keep the WAV: the page plays that instead
                    AudioLog("{0} ({1}): ffmpeg failed ({2}): {3}; kept as WAV", e.Row.Title, code, fexit == -1 ? "took too long" : "exit " + fexit, Tail(foutput));
                    if (source != wav)
                        File.Move(source, wav, overwrite: true);
                    return false;                           // failed: not tried again until a restart (the WAV plays)
                }
                File.Move(made, mp3, overwrite: true);
                if (File.Exists(wav))
                    File.Delete(wav);                       // the MP3 replaces a kept WAV
                return true;
            }
            catch (Exception ex)
            {
                AudioLog("{0} ({1}): {2}", e.Row.Title, code, ex.Message);
                return false;
            }
            finally
            {
                try { Directory.Delete(temp, recursive: true); }
                catch (Exception) { }
            }
        }

        // runs a program and waits (ToolLimit); its exit code and output (-1: it took too long and was stopped)
        private static (int Exit, string Output) Run(string exe, List<string> args)
        {
            System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory,
            };
            foreach (string a in args)
                start.ArgumentList.Add(a);
            using System.Diagnostics.Process p = System.Diagnostics.Process.Start(start)!;
            Task<string> stdout = p.StandardOutput.ReadToEndAsync(), stderr = p.StandardError.ReadToEndAsync();
            if (!p.WaitForExit((int)ToolLimit.TotalMilliseconds))
            {
                try { p.Kill(entireProcessTree: true); }
                catch (Exception) { }
                return (-1, "");
            }
            p.WaitForExit();
            return (p.ExitCode, stdout.Result + stderr.Result);
        }

        private static string Tail(string output)
        {
            string text = string.Join(" ", output.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(3));
            return text.Length > 300 ? text.Substring(text.Length - 300) : text.Length > 0 ? text : "(no output)";
        }

        // the audio no catalog song uses any more (and leftovers of a write that stopped); how many were deleted
        private static int RemoveStaleAudio(string folder, List<Entry> entries)
        {
            string audioFolder = Path.Combine(folder, AudioFolder);
            if (!Directory.Exists(audioFolder))
                return 0;
            HashSet<string> keep = new HashSet<string>(entries.Where(e => e.Audio != null).Select(e => Path.GetFileName(e.Audio!)), StringComparer.OrdinalIgnoreCase);
            int removed = 0;
            foreach (string file in Directory.GetFiles(audioFolder))
            {
                if (keep.Contains(Path.GetFileName(file)))
                    continue;
                try
                {
                    File.Delete(file);
                    ++removed;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    AudioLog("couldn't remove {0}: {1}", Path.GetFileName(file), ex.Message);
                }
            }
            return removed;
        }
        #endregion Audio

        // written whole beside it, then moved into place; false: the file already says this
        private static bool WriteIfChanged(string path, string text)
        {
            if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) == text)
                return false;
            string temp = path + ".new";
            File.WriteAllText(temp, text, new UTF8Encoding(false));
            File.Move(temp, path, overwrite: true);
            return true;
        }
        #endregion Writing
    }
}
