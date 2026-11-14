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
        }

        /// <summary>Writes the catalog into 'folder' if anything changed; returns what it did. Off the game thread.</summary>
        public static string Write(string folder)
        {
            if (MusicShareDatabase.IsSeeded() != true)
                return "not published: no shared music database at " + MusicShareDatabase.DatabasePath;
            List<SongRow>? rows = MusicShareDatabase.GetSongs();
            if (rows == null)
                return "not published: the shared music database couldn't be read";

            List<Entry> entries = Build(rows);
            string version = Version(entries);
            string rzrFolder = Path.Combine(folder, "rzr");
            string catalogFile = Path.Combine(folder, "catalog.json");
            if (PublishedVersion(catalogFile) == version && File.Exists(Path.Combine(folder, ZipFile)))
                return string.Format("unchanged ({0} songs)", entries.Count);

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

            return string.Format("published: {0} songs ({1} written, {2} removed)", entries.Count, written, removed);
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

        // the directive names a music composition book reads anywhere in a line (ManuscriptPaper.ProcessDirectives)
        private static readonly string[] Directives =
            { "song:", "author:", "instrument:", "tempo:", "newtimer:", "prefetch:", "concertmode:", "price:", "napster:" };

        private static bool IsDirective(string line) =>
            Directives.Any(d => line.IndexOf(d, StringComparison.OrdinalIgnoreCase) >= 0);

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
            // no directive names here (see Directives): a book would read them
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
                    .Append(e.Row.Napster).Append(e.Row.Price).Append('\0');
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
                    file = "rzr/" + e.File,
                    instrument = e.Row.Instrument == "lapharp" ? "lap harp" : e.Row.Instrument,
                    composedOn = e.Row.Origin.Length > 0 ? MusicShare.ShardName(e.Row.Origin) : null,
                    added = Added(e.Row),
                    remastered = Day(e.Row.DateRemastered),
                    price = e.Row.Napster ? 0 : e.Row.Price,       // Angel Island's: a suggestion
                    napster = e.Row.Napster,
                }),
            };
            return JsonSerializer.Serialize(catalog, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,     // titles readable as written (é, not é)
            });
        }

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
