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

/* Scripts\Engines\AIMusicSystem\Library\MusicImport.cs
 * Changelog
 *  11/14/26, Adam
 *      Created. [MusicImport [free] (Administrators): songs from song files (.rzr) into the music library, approved.
 *      For a shard taking in another's music, such as the Angel Island Music Library (game-master.net, "Download all"):
 *      put the files in Data\Music Import (AimsHost.MusicImportDirectory) and run it. Each file is made into sheet
 *      music exactly as the same text pasted into a composition book would be (ManuscriptPaper.SheetFromText: compiled,
 *      its # Song:, # Author:, # Instrument:, # Price: ... settings, its hash from the notes alone), then:
 *      - already in the library (the same notes): nothing changes; running it again is safe.
 *      - updated: an earlier import with the same title and author, but other notes, is replaced by the new version,
 *        which takes over its record (sales, buyers, price) as a remaster does; the old version leaves the library.
 *      - added: a new song, approved now (the admin choosing the file is the review), at the file's # Price: ([MusicImport
 *        free: every song added free). The shard's prices are its own: change any afterwards.
 *      - refused, with the reason: it doesn't compile, is too long, has no title, names no author (a file is never
 *        credited to the one importing it), or isn't text.
 *      An imported song is owned by AimsHost.ImportOwner (default: the admin importing it) for the library's records
 *      only: it pays no one (its sales and plays earn its owner no gold, credit or points: no one here composed it; the
 *      price leaves the economy unless the shard sends it somewhere, AimsHost.OnImportedSale). It is no news to anyone,
 *      and keeps its author and its comments (where it came from). The record knows it was imported
 *      (MusicInfo.Imported). The files stay where they are. Each file's outcome is in Logs\MusicImport.log.
 */

using Server.Commands;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Items
{
    public static class MusicImport
    {
        public const string LogFile = "MusicImport.log";

        public enum Outcome { Added, AlreadyInLibrary, Updated, Refused }

        /// <summary>What became of one song file.</summary>
        public sealed class FileResult
        {
            public string File = "";
            public Outcome Outcome;
            public string Title = "";
            public string Detail = "";      // the reason (refused), or what it matched (already in the library, updated)
            public RolledUpSheetMusic? Sheet;

            public override string ToString()
            {
                string what = Outcome switch
                {
                    Outcome.Added => "added",
                    Outcome.AlreadyInLibrary => "already in the library",
                    Outcome.Updated => "updated",
                    _ => "refused",
                };
                return string.Format("{0}: {1}{2}{3}", File, what, Title.Length > 0 ? " '" + Title + "'" : "", Detail.Length > 0 ? " (" + Detail + ")" : "");
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("MusicImport", AccessLevel.Administrator, new CommandEventHandler(MusicImport_OnCommand));
        }

        [Usage("MusicImport [free]")]
        [Description("Adds the songs in the song files (.rzr) in Data\\Music Import to the music library, approved, at each file's price (free: every song free). A song already in the library is left as it is; a new version of one imported before replaces it. Each file's outcome is in Logs\\MusicImport.log.")]
        private static void MusicImport_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            bool free = e.Arguments.Length == 1 && e.Arguments[0].Equals("free", StringComparison.OrdinalIgnoreCase);
            if (e.Arguments.Length > 0 && !free)
            {
                from.SendMessage("Usage: [MusicImport [free]  (free: every song added free; otherwise each file's price).");
                return;
            }

            string folder = AimsHost.Current.MusicImportDirectory;
            if (!Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    from.SendMessage("MusicImport: the folder {0} couldn't be made: {1}", folder, ex.Message);
                    return;
                }
            }
            string[] files = Directory.GetFiles(folder, "*.rzr").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
            if (files.Length == 0)
            {
                from.SendMessage("MusicImport: no song files (.rzr) in {0}. Put them there, then [MusicImport again.", folder);
                return;
            }

            List<FileResult> results = Import(from, files, free);
            List<string> lines = new List<string> { string.Format("[MusicImport{0} by {1}: {2}", free ? " free" : "", from.Name, Summary(results)) };
            lines.AddRange(results.Select(r => r.ToString()));
            AimsHost.Current.WriteLog(LogFile, from, lines);

            from.SendMessage("MusicImport: {0}.", Summary(results));
            foreach (FileResult r in results.Where(r => r.Outcome == Outcome.Refused).Take(5))
                from.SendMessage(33, r.ToString());
            int more = results.Count(r => r.Outcome == Outcome.Refused) - 5;
            from.SendMessage("{0}Every file's outcome is in {1}.", more > 0 ? string.Format("... and {0} more refused. ", more) : "",
                Path.Combine(AimsHost.Current.LogsDirectory, LogFile));
        }

        /// <summary>"12 added, 117 already in the library, 1 updated, 0 refused"</summary>
        public static string Summary(List<FileResult> results)
        {
            return string.Format("{0} added, {1} already in the library, {2} updated, {3} refused",
                results.Count(r => r.Outcome == Outcome.Added), results.Count(r => r.Outcome == Outcome.AlreadyInLibrary),
                results.Count(r => r.Outcome == Outcome.Updated), results.Count(r => r.Outcome == Outcome.Refused));
        }

        /// <summary>Imports these song files, in order (game thread). 'free': every song added free.</summary>
        public static List<FileResult> Import(Mobile importer, IEnumerable<string> files, bool free)
        {
            List<FileResult> results = new List<FileResult>();
            foreach (string file in files)
            {
                FileResult result;
                try
                {
                    result = ImportFile(importer, file, free);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex, "MusicImport: " + file);
                    result = new FileResult { File = Path.GetFileName(file), Outcome = Outcome.Refused, Detail = "an error: " + ex.Message };
                }
                results.Add(result);
            }
            return results;
        }

        /// <summary>One song file into the library (game thread).</summary>
        public static FileResult ImportFile(Mobile importer, string path, bool free)
        {
            FileResult result = new FileResult { File = Path.GetFileName(path) };
            Mobile? owner = AimsHost.Current.ImportOwner(importer);
            if (owner == null)
                return Refuse(result, "this shard names no owner for imported songs (AimsHost.ImportOwner)");

            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                return Refuse(result, "it couldn't be read: " + ex.Message);
            }
            if (text.IndexOf('\0') >= 0)
                return Refuse(result, "it isn't a text file");

            List<string> said = new List<string>();
            RolledUpSheetMusic? rsm = ManuscriptPaper.SheetFromText(owner, text, said);
            if (rsm == null)
                return Refuse(result, said.Count > 0 ? string.Join(" ", said) : "it can't be made into music");
            result.Title = rsm.Title ?? "";
            if (free)
                rsm.Price = 0;

            // the same notes: this song is here already (the library knows a song by its notes, as a music box does)
            RolledUpSheetMusic? same = MusicLibrary.Repository.Keys.FirstOrDefault(k => k != null && !k.Deleted && k.HashCode == rsm.HashCode);
            if (same != null)
            {
                rsm.Delete();
                result.Outcome = Outcome.AlreadyInLibrary;
                if (!string.Equals(same.Title, result.Title, StringComparison.OrdinalIgnoreCase))
                    result.Detail = "as '" + same.Title + "'";
                return result;
            }

            // an earlier import of this song (its title and author) with other notes: this is its new version
            KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo> earlier = MusicLibrary.Repository.FirstOrDefault(kvp =>
                kvp.Key != null && !kvp.Key.Deleted && kvp.Value != null && kvp.Value.Imported
                && kvp.Value.Status == MusicLibrary.SubmissionStatus.Approved
                && string.Equals(kvp.Key.Title, rsm.Title, StringComparison.OrdinalIgnoreCase)
                && string.Equals(kvp.Key.Author, rsm.Author, StringComparison.OrdinalIgnoreCase));

            // stored as a submitted song is: off the map, playing from the library
            rsm.RequiresBackpack = false;
            AimsHost.Current.MoveToInternal(rsm);

            if (earlier.Key != null)
            {   // a remaster, as a music box approves one: the new version takes over the record (sales, buyers, price,
                //  approval), and the old version leaves the library
                RolledUpSheetMusic old = earlier.Key;
                MusicLibrary.MusicInfo info = earlier.Value;
                MusicLibrary.Repository.Remove(old);
                MusicLibrary.Unqueue(old);
                if (!old.Deleted && old.Map == Map.Internal && old.Parent == null)
                    old.Delete();
                if (info.Owner != null)
                    rsm.Owner = info.Owner;
                rsm.Price = info.Price;
                MusicLibrary.Repository[rsm] = info;
                info.DateRemastered = DateTime.UtcNow;
                info.Remastered = true;
                info.ComposerTold = true;
                result.Outcome = Outcome.Updated;
                result.Sheet = rsm;
                return result;
            }

            MusicLibrary.MusicInfo added = new MusicLibrary.MusicInfo(owner, rsm.Price);
            added.Napster = rsm.Napster;
            added.Imported = true;
            added.Status = MusicLibrary.SubmissionStatus.Approved;
            added.DateApproved = DateTime.UtcNow;
            added.ComposerTold = true;      // no news to anyone: its composer has no character here
            MusicLibrary.Repository.Add(rsm, added);
            result.Outcome = Outcome.Added;
            result.Sheet = rsm;
            return result;
        }

        private static FileResult Refuse(FileResult result, string why)
        {
            result.Outcome = Outcome.Refused;
            result.Detail = why;
            return result;
        }
    }
}
