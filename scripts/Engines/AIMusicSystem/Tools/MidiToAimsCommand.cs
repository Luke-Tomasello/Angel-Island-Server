/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
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


/* Scripts\Engines\AIMusicSystem\Tools\MidiToAimsCommand.cs
 * Changelog
 *  11/2/26, Adam (2026 modernization, 11/1/26 to 11/2/26)
 *      Initial creation. [m2a <song>: a MIDI in Development\Midi to composition books in a bag in the backpack, for
 *      trying arrangements quickly, with MidiToAims.Core (read fresh from disk each run, so it can be rebuilt while the
 *      shard runs). Checks that Core's copies of MaxPages, MaxBooks and the percussion sounds match the shard's.
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Engines
{
    public static class MidiToAimsCommand
    {
        private static string Folder(string name) => Path.Combine(Core.ExeDirectory, "Development", name);
        private const string BagPrefix = "M2A: ";

        public static void Initialize()
        {
            CommandSystem.Register("m2a", AccessLevel.Administrator, new CommandEventHandler(OnCommand));
        }

        [Usage("m2a [song] [0xSerial]")]
        [Description("Arranges a MIDI from Development\\Midi into composition books (MidiToAims): best match on the song's file name.")]
        private static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            try
            {
                string midiFolder = Folder("Midi");
                List<string> midis = Directory.Exists(midiFolder) ? Directory.GetFiles(midiFolder, "*.mid").OrderBy(f => f).ToList() : new List<string>();

                // the target: the last argument when it is a serial, else the caller
                List<string> words = e.Arguments.ToList();
                Mobile target = from;
                if (words.Count > 0 && words[^1].StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(words[^1].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int serial) || World.FindMobile(serial) is not Mobile m)
                    {
                        from.SendMessage("m2a: no mobile {0}.", words[^1]);
                        return;
                    }
                    target = m;
                    words.RemoveAt(words.Count - 1);
                }

                if (words.Count == 0)
                {
                    from.SendMessage(midis.Count == 0 ? "m2a: there are no MIDI files in Development\\Midi." : "m2a: the MIDI files in Development\\Midi:");
                    foreach (string f in midis.Take(25))
                        from.SendMessage("  " + Path.GetFileNameWithoutExtension(f));
                    return;
                }

                string? midi = BestMatch(from, midis, string.Join(" ", words));
                if (midi == null)
                    return;     // BestMatch said why
                if (target.Backpack == null)
                {
                    from.SendMessage("m2a: {0} has no backpack.", target.Name);
                    return;
                }

                Arrange(from, target, midi);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                from.SendMessage(33, "m2a: {0}", ex.Message);
            }
        }

        #region Finding the song
        private static string Words(string s) => Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();

        // the MIDI whose file name best matches what was typed: the name itself, then a name containing the words as
        //  typed, then one containing all of them; several equally good matches are listed rather than guessed
        private static string? BestMatch(Mobile from, List<string> midis, string query)
        {
            string q = Words(query);
            string[] qWords = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var scored = midis.Select(f =>
            {
                string name = Words(Path.GetFileNameWithoutExtension(f));
                string[] nameWords = name.Split(' ');
                int score = name == q ? 3 : (" " + name + " ").Contains(" " + q + " ") ? 2 : qWords.All(w => nameWords.Contains(w)) ? 1 : 0;
                return (File: f, Score: score);
            }).Where(s => s.Score > 0).OrderByDescending(s => s.Score).ToList();

            if (scored.Count == 0)
            {
                from.SendMessage("m2a: no MIDI in Development\\Midi matches '{0}'. [m2a alone lists them.", query);
                return null;
            }
            var best = scored.Where(s => s.Score == scored[0].Score).ToList();
            if (best.Count > 1)
            {
                from.SendMessage("m2a: '{0}' matches {1} files; say more of the name:", query, best.Count);
                foreach (var s in best.Take(10))
                    from.SendMessage("  " + Path.GetFileNameWithoutExtension(s.File));
                return null;
            }
            return best[0].File;
        }
        #endregion Finding the song

        #region Arranging
        private static void Arrange(Mobile from, Mobile target, string midi)
        {
            string name = Path.GetFileNameWithoutExtension(midi);
            string m2aPath = Path.ChangeExtension(midi, ".m2a");
            StringBuilder log = new StringBuilder();
            log.AppendLine(string.Format("{0} [m2a {1} by {2} for {3}", DateTime.Now, name, from.Name, target.Name));

            string? dll = ConverterPath();
            if (dll == null)
            {
                from.SendMessage(33, "m2a: MidiToAims.Core.dll was not found (next to the server, or built in AI Music Composer\\MidiToAims.Core).");
                return;
            }

            // the converter, read fresh from disk into a context that is unloaded afterwards (the file is never locked)
            string rzr, report;
            List<string[]> books = new List<string[]>();
            AssemblyLoadContext context = new AssemblyLoadContext("MidiToAims", isCollectible: true);
            try
            {
                Assembly assembly = context.LoadFromStream(new MemoryStream(File.ReadAllBytes(dll)));
                Type converter = assembly.GetType("MidiToAims.Converter", throwOnError: true)!;
                Type bookLayout = assembly.GetType("MidiToAims.Books", throwOnError: true)!;

                // Core's copies of the book limits (it has no shard dependencies) must match the shard's
                int corePages = (int)bookLayout.GetField("MaxPages")!.GetValue(null)!;
                int coreBooks = (int)bookLayout.GetField("MaxBooks")!.GetValue(null)!;
                if (corePages != MusicCompositionBook.MaxPages || coreBooks != ManuscriptPaper.LocationTarget.MaxBooks)
                {
                    from.SendMessage(33, "m2a: MidiToAims.Core's book limits ({0} pages, {1} books) differ from the shard's ({2} pages, {3} books): change them together (MidiToAims.Core\\Books.cs).",
                        corePages, coreBooks, MusicCompositionBook.MaxPages, ManuscriptPaper.LocationTarget.MaxBooks);
                    return;
                }

                // and its copy of the percussion (the sound effects free of music karma)
                Type percussion = assembly.GetType("MidiToAims.Percussion", throwOnError: true)!;
                int[] coreSounds = (int[])percussion.GetField("Sounds")!.GetValue(null)!;
                if (!SystemMusicPlayer.PercussionSounds.SetEquals(coreSounds))
                {
                    from.SendMessage(33, "m2a: MidiToAims.Core's percussion sounds differ from the shard's (SystemMusicPlayer.PercussionSounds): change them together (MidiToAims.Core\\Percussion.cs).");
                    return;
                }

                if (!File.Exists(m2aPath))
                {
                    File.WriteAllText(m2aPath, (string)converter.GetMethod("Init")!.Invoke(null, new object[] { midi })!);
                    from.SendMessage("m2a: wrote {0}.m2a (the arrangement settings) beside the MIDI.", name);
                }
                // the arranger defaults to whoever runs [m2a: first, so an 'Arranged by' in the .m2a wins
                string m2aText = "Arranged by: " + from.Name + Environment.NewLine + File.ReadAllText(m2aPath);
                string[] result = (string[])converter.GetMethod("Convert")!.Invoke(null, new object?[] { midi, m2aText })!;
                rzr = result[0];
                report = result[1];
                // { .rzr, report, book 1, book 2, ... }: each book's lines as the client wraps them
                MethodInfo bookLines = bookLayout.GetMethod("Lines")!;
                foreach (string book in result.Skip(2))
                    books.Add((string[])bookLines.Invoke(null, new object[] { book })!);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                from.SendMessage(33, "m2a: the converter failed: {0}", ex.InnerException.Message);
                return;
            }
            finally
            {
                context.Unload();
            }
            log.AppendLine("converter: " + dll + " (" + File.GetLastWriteTime(dll) + ")");
            log.Append(report);

            string[] reportLines = report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in reportLines.Where(l => l.StartsWith("warning:")))
                from.SendMessage(33, "m2a: {0}.m2a {1}", name, line);
            if (rzr.Length == 0)
            {
                from.SendMessage(33, "m2a: {0}", reportLines.LastOrDefault() ?? "nothing to play.");
                WriteLog(log);
                return;
            }

            // the music compiler's rules, as the manuscript paper applies them
            MusicCompiler.Result compiled = MusicCompiler.Compile(rzr);
            if (!compiled.Success)
            {
                from.SendMessage(33, "m2a: the music compiler refused it:");
                foreach (string error in compiled.Errors)
                    from.SendMessage(33, "  " + error);
                log.AppendLine("compiler: " + string.Join(" / ", compiled.Errors));
                WriteLog(log);
                return;
            }

            string songs = Folder("Songs");
            Directory.CreateDirectory(songs);
            File.WriteAllText(Path.Combine(songs, name + ".rzr"), rzr);

            string song = Regex.Match(rzr, @"^# Song: (.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
            string author = Regex.Match(rzr, @"^# Author: (.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
            if (books.Count == 0)
            {
                from.SendMessage(33, "m2a: '{0}' needs more than {1} books.", song, ManuscriptPaper.LocationTarget.MaxBooks);
                WriteLog(log);
                return;
            }

            // a bag for the song's books, replacing the bag of the last run
            foreach (Item old in target.Backpack!.Items.Where(i => i is Bag && i.Name == BagPrefix + song).ToList())
                old.Delete();
            Bag bag = new Bag { Name = BagPrefix + song };
            for (int b = 0; b < books.Count; b++)
            {
                MusicCompositionBook book = new MusicCompositionBook();
                string[] lines = books[b];
                for (int p = 0; p < book.PagesCount; p++)
                    book.Pages[p].Lines = lines.Skip(p * 8).Take(8).ToArray();
                book.Title = books.Count == 1 ? song : string.Format("{0} ({1}/{2})", song, b + 1, books.Count);
                book.Author = author.Length > 0 ? author : from.Name;
                bag.DropItem(book);
            }
            target.Backpack.DropItem(bag);

            from.SendMessage("m2a: {0}", reportLines.FirstOrDefault(l => !l.StartsWith("warning:")) ?? song);
            int changedBars = reportLines.Count(l => l.StartsWith("bar ") || l.StartsWith("..."));
            from.SendMessage("m2a: '{0}' is in {1} book{2} in a bag in {3}'s backpack.{4}", song, books.Count, books.Count == 1 ? "" : "s", target.Name,
                changedBars > 0 ? " Bars changed to fit: Development\\Logs\\M2A.log." : "");
            log.AppendLine(string.Format("books: {0}", books.Count));
            WriteLog(log);
        }

        // the newest MidiToAims.Core.dll: beside the server, or built in AI Music Composer
        private static string? ConverterPath()
        {
            List<string> candidates = new List<string> { Path.Combine(Core.ExeDirectory, "MidiToAims.Core.dll") };
            string built = Path.Combine(Core.ExeDirectory, "AI Music Composer", "MidiToAims.Core", "bin");
            if (Directory.Exists(built))
                candidates.AddRange(Directory.GetFiles(built, "MidiToAims.Core.dll", SearchOption.AllDirectories));
            return candidates.Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        }

        private static void WriteLog(StringBuilder log)
        {
            string logs = Folder("Logs");
            Directory.CreateDirectory(logs);
            File.AppendAllText(Path.Combine(logs, "M2A.log"), log.ToString() + Environment.NewLine);
        }
        #endregion Arranging
    }
}
