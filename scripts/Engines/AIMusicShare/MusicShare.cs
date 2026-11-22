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

/* Scripts\Engines\AIMusicShare\MusicShare.cs
 * ChangeLog
 *  11/18/26, Adam (sales acknowledged on save)
 *      A sale elsewhere of this shard's song (a Music Sales point, Music Karma, a notice) was marked recognized in the
 *      database as soon as it was given, before the world saved: a crash or rollback lost the points while the database
 *      said done. Now it's acknowledged on save: the sale ids given are kept in Saves/MusicShare/Recognized.txt with the
 *      world, the database is told only after a world save has finished, and a sale already given is never given again.
 *      A world that never saved them finds them still open and gives them again. (Server/DatabaseFolder.cs: the rule.)
 *      Sharing is always on: -sharemusic is retired.
 *  11/18/26, Adam (Development)
 *      flush asks Core.Development (a Debug build or -nosaves) instead of NoSaves.
 *  11/17/26, Adam (flush)
 *      [MusicShare flush [yes] (Owners; the Music Conservatory on a development machine only: -uomc with -nosaves, which
 *      production never has): before the world built here is copied to production, the test and demo songs made on it
 *      go. flush lists them (songs homed here in the database, and library songs the database doesn't know); flush yes
 *      deletes them from the library with their sheet music, marks their database rows deleted (the other shards drop
 *      their copies at their next sync), clears the export list (Exported.txt) and saves the world. The rows are marked,
 *      not just the library cleared: a sync rebuilds an approved home song its world doesn't have.
 *  11/17/26, Adam (decisions written at once)
 *      A sync that applies decisions or renames at a song's home starts a follow-up sync once they are marked applied,
 *      so the database has the song's new status (or title) at once. It was a sync late (about 5 minutes): a sync
 *      writes its rows before it reads the decisions, so a song approved elsewhere was written Pending once more.
 *  11/16/26, Adam (-uomc)
 *      The Music Conservatory (-uomc) is its own shard in the database, AIMC (ConservatoryShard), never Upheaval, whose
 *      rules it runs on, named "Angel Island Music Conservatory" (ShardName: "composed on ..."). It starts held: it reads
 *      only until an Owner releases its writes ([MusicShare release; the mark in the database, read at every sync;
 *      missing or unreadable is held, and a restart is held until the first sync reads it; [MusicShare hold takes it
 *      back). The release asks for a dry run first ([MusicShare sync dryrun: Logs\AIMC-dryrun.csv, nothing written:
 *      known songs, new approved, pending, rejected, dropped, and suspect test material: staff-owned, ownerless, a
 *      title another song has), and clears the export list a copied world brings (another shard's; harmless, since only
 *      songs homed here are acted on). Its purchases record nothing for composers elsewhere (its players have free
 *      money). A song whose composer's character is gone keeps the last name the database knew, not the Author.
 *  11/16/26, Adam (About and Tags)
 *      The titles file sets songs' About and Tags too: [MusicShare titles writes each song's About and Tags, with empty
 *      NewAbout and NewTags columns; titles apply records them with the renames (an About of 60 characters at most, 10
 *      tags of 100; '-' clears one; the file's About and Tags must still be the song's), and each home applies them at its
 *      sync (MusicLibrary.Describe: the notes' # About: and # Tags: lines; not news for the composer). Its notes then
 *      reach every shard as a rename's do. "N changes recorded" (was "N renames").
 *  11/16/26, Adam (song renames)
 *      Library songs are renamed from a reviewed file, for the library's 'Title (Artist)' names: [MusicShare titles
 *      writes Logs\titles-review.csv (every song: its home, composer, author and title, with empty NewTitle, NewAuthor and
 *      Note columns); staff fill in the renames; [MusicShare titles apply [file] records them (all or nothing: every line
 *      a known song whose title is still the file's, a new title ending in its attribution in brackets, an author of 40
 *      characters at most, a note of 100, no rename already waiting). Nothing changes yet: each song's home applies its
 *      renames at its next sync, as review decisions are (MusicLibrary.Rename: the title everywhere the sheet keeps it,
 *      the names it had kept for its composer's remasters, and the composer told once), and writes the new name at the
 *      sync after; a database title is never set directly, or the home's next sync would write the old one back. A copy
 *      on another shard follows its home's title, author and notes (Mirror; the notes only when they are the same song).
 *      A home song rebuilt from the database keeps the names it had (Renames, GetRenameHistory).
 *  11/14/26, Adam (arranged by)
 *      Remember keeps each song's composer too (ComposerOf), and the names each account has composed under (ComposedAs),
 *      for the arranger of a song held for its composer (AngelIslandAimsHost.Arranger).
 *  11/14/26, Adam (submitted date)
 *      A library copy built from the database (an import, or a home song the world lost) keeps the song's submitted date
 *      (MusicInfo.DateSubmitted can be set now); it was the day it was built, which the home's next sync then wrote over
 *      the database's (seen 11/14/26: Creep and Sail, rebuilt by a local Upheaval from an older world).
 *  11/14/26, Adam (a synced decision is final)
 *      "The database wins" (11/7) covers review decisions too. A home world that crashed or rolled back after a sync
 *      wrote its decision came back with the song still pending, and its next sync wrote Pending over the decision; one
 *      made on another shard was already marked applied, so it was never applied again (the song just reappeared for
 *      review). Only a song's home writes its status, and a status only moves on from Pending, so a home song the
 *      database has decided (approved or rejected) and the world says otherwise is a decision the world lost: the plan
 *      adopts it (ActKind.Adopt, first in the plan) and doesn't write that song this time. Approved: through the music
 *      box's own approval (a remaster takes over the song it remasters; points and the composer's news come back, as
 *      the world lost them too), quietly (AngelIslandAimsHost.QuietApprovals: no second "New music" post), with the
 *      database's dates. Rejected: its reason, and the note of its latest applied decision if it has one (a note typed
 *      at the home music box lived only in the world). A database still Pending: the world writes as before (a
 *      decision made at the home music box). Seen 11/14/26: a local Upheaval started from an older world rewrote three
 *      of its songs (Golden back to pending, Bombshell and Thriller to rejected).
 *  11/14/26, Adam (composed on)
 *      Where each song was composed (Songs.origin, for the website's music catalog; home is untouched). [MusicShare
 *      origins writes Logs\origins-review.csv: each song's origin by the rule (ChooseOrigins: the oldest shard in the
 *      lineage, Angel Island -> Siege Perilous -> Mortalis -> Seasons -> Upheaval, whose collected library held it, since
 *      each shard's world began as a copy of the one before; a song collected nowhere was composed at its home), with the
 *      reason, for staff to check and edit. [MusicShare origins apply [file] writes it, all or nothing. A song submitted
 *      later gets its home as its origin when first written.
 *  11/13/26, Adam (music catalog)
 *      [MusicShare export publishes the music catalog for the website now (MusicCatalog.cs; a server started with
 *      -publishmusic=<folder>, the production login server); status says where it goes and what it last did.
 *  11/9/26, Adam (review notices, deny prompts)
 *      One review notice, every sync (5 minutes): Administrators and up, and reviewers, online are told how many songs
 *      await review on every shard, this one's among them ("[Music System] 4 songs awaiting review: Angel Island 1,
 *      Mortalis 1, Siege Perilous 2 ([MusicReview list; [MusicReview pause quiets these)."); a song with a decision
 *      waiting isn't counted. It replaces both notices: this one's of other shards only, and AIMS's of this shard's
 *      (off while music is shared: AngelIslandAimsHost.ReviewNeeded). Test Center and event shards send none.
 *      [MusicReview pause [minutes] quiets them for an hour (or that many minutes, up to 24 hours), resume turns them
 *      back on; per character, until the shard restarts.
 *      deny asks, as the music box does: deny <n> alone asks for the reason (TooLong, TooShort, Inappropriate,
 *      Duplicate or Other; any other word is Other), and Other asks for a note to the composer, which it needs (Esc:
 *      not denied). Every denial has a reason; the decision is recorded only when it's complete. The typed forms still
 *      work (deny <n> Duplicate, deny <n> Other <note>, deny <n> <note>); deny <n> Other alone asks for the note.
 *  11/9/26, Adam (music reviewers)
 *      [MusicReview: trusted players review music without staff privileges (Player access; refused unless their
 *      account is on the reviewer list in the shared database, or they are an Owner). list (or pending) numbers every
 *      shard's songs awaiting review, to take in any order; listen, approve and deny work as [MusicShare's (the song's
 *      home applies the decision at its next sync); help. Owners: reviewers, add <account>, remove <account>. Composers
 *      aren't told who reviewed; the decision and the log name the reviewer.
 *      Both commands: a song with a decision waiting to be applied shows it in the list ("approved by Adam Ant, applies
 *      within 5 minutes"), and a second decision on it is refused, as is one on a song no longer awaiting review
 *      (MusicShareDatabase.RecordDecision; before, the home set the second aside and only the log said so). A note
 *      longer than 100 characters is refused (the music box's note prompt holds 100). A listen copy is a review copy
 *      (IsReviewCopy): it can't be added to a music box (AngelIslandAimsHost.RefuseSubmission), so a reviewer can't
 *      submit someone else's song as theirs.
 *  11/8/26, Adam (rejection note)
 *      [MusicShare deny <n> [reason] [note]: words after the reason (or in place of it: reason Other) are a note to the
 *      composer, carried in the decision; the song's home sets it on the record (MusicInfo.Note) and the composer hears
 *      it at their next music box visit (TellComposer). A library copy built from the database counts as told (its
 *      approval is no news to its composer).
 *  11/7/26, Adam (-sharemusic)
 *      Sharing is on when the shard starts with -sharemusic (Core.ShareMusic), so Main knows it at startup and says where
 *      music is shared from; the MusicSharing feature bit and PatchV266 are retired, and [MusicShare on|off is gone. The
 *      sync every 5 minutes is a cron task (CronTasks: CMusicShare), no longer its own timer. Each song's home is read
 *      from the database when the world loads, so staff notes and refusals know it before the first sync.
 *  11/7/26, Adam
 *      Created. Music shared between the Angel Island shards on one machine, through the shared music database
 *      (MusicShareDatabase.cs). Angel Island's, outside the music system (AIMS knows nothing of it). The rules (decided
 *      11/5-11/7/26):
 *      - Every song has a home shard. Its status follows its home: approved, rejected, remastered (a new song, the old
 *        one deleted) or deleted at home, it is so everywhere. Home staff approve it once, for every shard.
 *      - Elsewhere a song is held for its composer by the system account (AimsHost.SystemMobile): it keeps its price, and
 *        a purchase pays the shard's LB fund (AngelIslandAimsHost.Deposit), not the composer. No gold crosses shards: the
 *        composer is recognized at home instead (a Music Sales leaderboard point, Music Karma, a notice). A Napster song
 *        is free elsewhere.
 *      - The database wins: a home song this shard's world no longer has (a rollback, a new season) is rebuilt. A home song
 *        whose composer has no character here is held by the system account, priced, until a character of the composer's
 *        account appears; then it is theirs again (songs follow their composer, as into a new season).
 *      - Test Center and event shards only read (their shard is "<home>-TC" / "-EV": every song is foreign there).
 *      Every 5 minutes, when the MusicSharing feature is on: this shard's songs go to the database, the others' come into
 *      its library, and sales elsewhere of its songs are recognized. Database work runs off the game thread; the
 *      library changes on it. Everything is written to MusicShare.log. [MusicShare status|sync|on|off.
 *      First-time seeding, central so nothing depends on which shard goes first: [MusicShare collect on each live shard
 *      (its library into a staging table, with each composer account's last login there), [MusicShare review
 *      (Logs\seed-review.csv: each song's home by the rule, a song in one library stays there, else the shard where its
 *      composer last logged in; staff check and edit it), [MusicShare seed [file] (all or nothing, into an empty database;
 *      no file: this shard's Logs\seed-review.csv, a bare name: in Logs, since a full path may not fit a typed command).
 *      Seed marks the database seeded; a shard never syncs with one that isn't (the first to start would claim every song
 *      in its library as its own). PatchV266 turns sharing on at a shard's first start with a seeded database.
 *      Review across shards: [MusicShare pending lists every shard's songs awaiting review; listen <n> plays one from a
 *      temporary sheet; approve/deny <n> [reason] (Owners) records a decision that the song's home applies at its next
 *      sync, through the music box's own approval. Every sync tells Administrators online how many songs await review
 *      on other shards. Each song's home is remembered from the last sync, for staff notes ('who', 'find') and for
 *      refusing a remaster or resubmission away from home (AngelIslandAimsHost).
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Engines.DataRecorder;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Engines.MusicShare
{
    public static class MusicShare
    {
        public const string LogFile = "MusicShare.log";
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);   // cron's schedule (CronTasks: CMusicShare)
        private static readonly string ExportedFile = Path.Combine("Saves", "MusicShare", "Exported.txt");
        private const int Approved = (int)MusicLibrary.SubmissionStatus.Approved;
        private const int PendingStatus = (int)MusicLibrary.SubmissionStatus.Pending;

        // the hashes of this shard's songs it has written to the database (a world save keeps it: a rollback restores it)
        private static HashSet<int> m_Exported = new HashSet<int>();

        // sales elsewhere this world has recognized (points and karma given) that the database hasn't been told of yet:
        //  saved with the world (Recognized.txt), and the database is told only after a world save completes (acknowledged
        //  on save, never on apply: a world that never saved them finds them still open, and gives them again)
        private static readonly string RecognizedFile = Path.Combine("Saves", "MusicShare", "Recognized.txt");
        private static HashSet<long> m_Applied = new HashSet<long>();
        private static bool m_Busy;
        private static string? m_LastFailure;

        #region Identity and switches
        /// <summary>This shard in the database: its ServerID; a Test Center or event instance is its own (read-only) shard;
        /// the Music Conservatory (-uomc, on the Upheaval rules) is AIMC, never Upheaval.</summary>
        public static string ThisShard =>
            Core.GMNMC_CFG ? ConservatoryShard : Core.ServerID.ToString() + (Core.GMNTC_CFG ? "-TC" : Core.GMNEV_CFG ? "-EV" : "");

        /// <summary>The Music Conservatory's key in the database (-uomc).</summary>
        public const string ConservatoryShard = Core.MusicConservatoryShortName;

        /// <summary>Test Center and event shards only read the database. The Music Conservatory reads only until staff
        /// release its writes ([MusicShare release, after its dry run): the release is read from the database at every
        /// sync, and a missing or unreadable one means held (fail closed); a restart starts held until the first sync reads
        /// it.</summary>
        public static bool CanWrite => !Core.GMNTC_CFG && !Core.GMNEV_CFG && (!Core.GMNMC_CFG || m_Released);

        /// <summary>A shard that starts held (the Music Conservatory) until staff release its writes.</summary>
        public static bool StartsHeld => Core.GMNMC_CFG;

        private static bool m_Released;         // a held shard's release, as the last sync read it (game thread)
        private static bool m_DryRunDone;       // [MusicShare sync dryrun has run since startup (release asks for it)

        /// <summary>Sharing is on: always, but never on the login server (Core.ShareMusic is always true: -sharemusic is retired).</summary>
        public static bool Enabled =>
            Core.ShareMusic && !Core.RuleSets.LoginServerRules();
        #endregion Identity and switches

        #region Startup
        public static void Configure()
        {
            EventSink.WorldLoad += LoadExported;
            EventSink.WorldLoad += LoadHomes;
            EventSink.WorldSave += SaveExported;
            EventSink.WorldLoad += LoadRecognized;
            EventSink.WorldSave += SaveRecognized;
        }

        // the sync every 5 minutes is a cron task (CronTasks: CMusicShare)
        public static void Initialize()
        {
            CommandSystem.Register("MusicShare", AccessLevel.Administrator, new CommandEventHandler(MusicShare_OnCommand));
            CommandSystem.Register("MusicReview", AccessLevel.Player, new CommandEventHandler(MusicReview_OnCommand));     // checks the reviewer list itself
        }

        // each song's home, read from the database at startup (off the game thread), so staff notes and refusals know it
        //  before the first sync
        private static void LoadHomes()
        {
            if (!Enabled)
                return;
            Task.Run(() =>
            {
                try
                {
                    List<SongRow>? rows = MusicShareDatabase.IsSeeded() == true ? MusicShareDatabase.GetSongs() : null;
                    if (rows != null)
                        Timer.DelayCall(TimeSpan.Zero, () => Remember(rows));
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "MusicShare.LoadHomes");
                }
            });
        }

        private static void LoadExported()
        {
            try
            {
                if (File.Exists(ExportedFile))
                    m_Exported = new HashSet<int>(File.ReadAllLines(ExportedFile).Where(l => int.TryParse(l, out _)).Select(int.Parse));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShare: reading " + ExportedFile);
            }
        }

        private static void LoadRecognized()
        {
            try
            {
                if (File.Exists(RecognizedFile))
                    m_Applied = new HashSet<long>(File.ReadAllLines(RecognizedFile).Where(l => long.TryParse(l, out _)).Select(long.Parse));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShare: reading " + RecognizedFile);
            }
        }

        // the world save keeps the recognized sales; once it has finished, the database is told (then they're forgotten here)
        private static void SaveRecognized(WorldSaveEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RecognizedFile)!);
                File.WriteAllLines(RecognizedFile, m_Applied.Select(id => id.ToString()));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShare: writing " + RecognizedFile);
                return;     // not saved: the database isn't told
            }
            if (m_Applied.Count == 0)
                return;
            List<long> saved = m_Applied.ToList();
            Timer.DelayCall(TimeSpan.Zero, () =>       // after the save has finished
                Task.Run(() =>
                {
                    if (MusicShareDatabase.MarkRecognized(saved))
                        Timer.DelayCall(TimeSpan.Zero, () => { foreach (long id in saved) m_Applied.Remove(id); });
                    // a failed mark: they stay here (and in the file), are never given twice, and are told after the next save
                }));
        }

        private static void SaveExported(WorldSaveEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ExportedFile)!);
                File.WriteAllLines(ExportedFile, m_Exported.Select(h => h.ToString()));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShare: writing " + ExportedFile);
            }
        }
        #endregion Startup

        #region The plan
        public enum Holder { Player, Gone, System }

        /// <summary>A library record of this shard, as the database would see it (taken on the game thread).</summary>
        public sealed class LocalSong
        {
            public RolledUpSheetMusic Sheet = null!;
            public MusicLibrary.MusicInfo Info = null!;
            public SongRow Row = null!;
            public Holder Holder;
        }

        public enum ActKind { Import, Rebuild, Remove, Convert, Mirror, Relink, Adopt }

        public sealed class Act
        {
            public ActKind Kind;
            public LocalSong? Local;
            public SongRow? Row;
            public string Why = "";
            public string Note = "";            // Adopt, a denial: its note to the composer (the latest applied decision's)
            public List<(string Title, string Author)>? FormerNames;    // Rebuild: the names it had (its applied renames)
        }

        public sealed class SyncPlan
        {
            public readonly List<SongRow> Save = new List<SongRow>();
            public readonly List<Act> Acts = new List<Act>();
            public readonly List<int> ExportedAdd = new List<int>();
            public readonly List<int> ExportedRemove = new List<int>();
        }

        /// <summary>
        /// What a sync does, from this shard's library (locals), the database (rows) and the songs this shard has written
        /// before (exported). Touches nothing: safe off the game thread.
        /// </summary>
        public static SyncPlan Plan(IList<LocalSong> locals, IList<SongRow> rows, string shard, bool canWrite, ISet<int> exported)
        {
            SyncPlan plan = new SyncPlan();
            List<Act> adopts = new List<Act>();
            Dictionary<int, SongRow> byHash = new Dictionary<int, SongRow>();
            foreach (SongRow r in rows)
                byHash[r.Hash] = r;
            HashSet<int> localHashes = new HashSet<int>();

            foreach (LocalSong l in locals)
            {
                localHashes.Add(l.Row.Hash);
                if (!byHash.TryGetValue(l.Row.Hash, out SongRow? r))
                {
                    if (l.Holder == Holder.System)
                        plan.Acts.Add(new Act { Kind = ActKind.Remove, Local = l, Why = "no longer in the shared database" });
                    else if (canWrite)
                    {
                        l.Row.HomeShard = shard;
                        plan.Save.Add(l.Row);
                        plan.ExportedAdd.Add(l.Row.Hash);
                    }
                }
                else if (r.HomeShard == shard)
                {
                    if (r.Deleted)
                    {   // deleted at home (the database wins over a world that still has it)
                        plan.Acts.Add(new Act { Kind = ActKind.Remove, Local = l, Row = r, Why = "deleted at home" });
                        continue;
                    }
                    if (r.Status != PendingStatus && l.Row.Status != r.Status)
                    {   // a synced decision is final: only this shard writes its songs' status, and a song's status only
                        //  moves on from Pending, so the world lost a decision it had synced (a crash or rollback), or
                        //  is another history (a test copy). The world takes the database's, and isn't written this time.
                        adopts.Add(new Act { Kind = ActKind.Adopt, Local = l, Row = r, Why = string.Format("the world said {0}, the database {1}",
                            (MusicLibrary.SubmissionStatus)l.Row.Status, (MusicLibrary.SubmissionStatus)r.Status) });
                        plan.ExportedAdd.Add(r.Hash);
                    }
                    else if (canWrite)
                    {
                        SongRow update = l.Row;
                        update.HomeShard = shard;
                        if (l.Holder != Holder.Player)
                        {   // held for its composer: the database keeps who composed it, and its price
                            update.Price = r.Price;
                            update.ComposerGuid = r.ComposerGuid;
                            update.ComposerName = r.ComposerName;
                        }
                        plan.Save.Add(update);
                        plan.ExportedAdd.Add(r.Hash);
                    }
                    if (l.Holder != Holder.Player)
                        plan.Acts.Add(new Act { Kind = ActKind.Relink, Local = l, Row = r, Why = "home song without its composer" });
                }
                else
                {   // another shard's song
                    if (l.Holder != Holder.System)
                        plan.Acts.Add(new Act { Kind = ActKind.Convert, Local = l, Row = r, Why = "its home is " + r.HomeShard });
                    if (r.Deleted || r.Status != Approved)
                        plan.Acts.Add(new Act { Kind = ActKind.Remove, Local = l, Row = r, Why = r.Deleted ? "deleted at home" : "not approved at home" });
                    else
                        plan.Acts.Add(new Act { Kind = ActKind.Mirror, Local = l, Row = r });
                }
            }

            foreach (SongRow r in rows)
            {
                if (localHashes.Contains(r.Hash) || r.Deleted)
                    continue;
                if (r.HomeShard == shard)
                {
                    if (exported.Contains(r.Hash) && canWrite)
                    {   // written by this shard and gone from its library since: deleted (or remastered) here
                        r.Deleted = true;
                        plan.Save.Add(r);
                        plan.ExportedRemove.Add(r.Hash);
                    }
                    else if (r.Status == Approved)
                        plan.Acts.Add(new Act { Kind = ActKind.Rebuild, Row = r, Why = "a home song this world doesn't have" });
                }
                else if (r.Status == Approved)
                    plan.Acts.Add(new Act { Kind = ActKind.Import, Row = r });
            }
            // decisions first: a re-approved remaster takes over the song it remasters (MusicBox.ApproveTrack), which
            //  must still be in the library, before "deleted at home" removes it
            plan.Acts.InsertRange(0, adopts);
            return plan;
        }
        #endregion The plan

        #region Sync
        /// <summary>One sync: now (staff: [MusicShare sync), or every 5 minutes. Reports to 'from' if given.</summary>
        public static void Sync(Mobile? from)
        {
            if (!Enabled || m_Busy)
            {
                from?.SendMessage(m_Busy ? "MusicShare: a sync is already running." : "MusicShare: sharing is off here (the login server holds no music).");
                return;
            }

            m_Busy = true;
            string shard = ThisShard;
            bool canWrite = CanWrite;
            bool held = StartsHeld;
            bool released = false;
            List<LocalSong> locals = Snapshot();
            HashSet<int> exported = new HashSet<int>(m_Exported);

            Task.Run(() =>
            {
                SyncPlan? plan = null;
                List<SaleRow>? sales = null;
                List<DecisionRow>? decisions = null;
                List<RenameRow>? renames = null;
                Dictionary<int, DecisionRow>? waiting = null;
                List<ReviewerRow>? reviewers = null;
                List<SongRow>? rows = null;
                string? failure = null;
                try
                {
                    bool? seeded = MusicShareDatabase.IsSeeded();
                    rows = seeded == true ? MusicShareDatabase.GetSongs() : null;
                    if (seeded != true)
                        failure = seeded == null ? "no shared music database at " + MusicShareDatabase.DatabasePath
                            : "the shared music database isn't seeded yet ([MusicShare collect, review, seed): nothing synced";
                    else if (rows == null)
                        failure = "the shared music database couldn't be read";
                    else
                    {
                        if (held)
                        {   // a held shard writes only once released, as the database says now (unreadable: held)
                            released = MusicShareDatabase.IsReleased(shard) == true;
                            canWrite = released;
                        }
                        plan = Plan(locals, rows, shard, canWrite, exported);
                        AddDenialNotes(plan);
                        AddFormerNames(plan);
                        if (plan.Save.Count > 0 && MusicShareDatabase.SaveSongs(plan.Save, shard) < 0)
                            failure = "the shared music database couldn't be written";
                        else if (canWrite)
                        {
                            sales = MusicShareDatabase.GetOpenSales(shard);
                            decisions = MusicShareDatabase.GetOpenDecisions(shard);
                            renames = MusicShareDatabase.GetOpenRenames(shard);
                            waiting = MusicShareDatabase.GetWaitingDecisions();     // the review notice
                            reviewers = MusicShareDatabase.GetReviewers();
                        }
                    }
                }
                catch (Exception ex)
                {
                    failure = ex.Message;
                    LogHelper.LogException(ex, "MusicShare.Sync");
                }

                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    try
                    {
                        if (failure != null || plan == null)
                        {
                            if (failure != m_LastFailure)       // once, not every 5 minutes
                                Log("sync failed: " + failure);
                            m_LastFailure = failure;
                            from?.SendMessage("MusicShare: sync failed: " + failure);
                            return;
                        }
                        if (m_LastFailure != null)
                            Log("syncing again");
                        m_LastFailure = null;
                        if (held && m_Released != released)
                            Log(released ? "writes released: this shard writes its songs now" : "held: this shard reads only");
                        if (held)
                            m_Released = released;
                        foreach (int h in plan.ExportedAdd) m_Exported.Add(h);
                        foreach (int h in plan.ExportedRemove) m_Exported.Remove(h);
                        List<string> lines = Apply(plan);
                        foreach (SongRow r in plan.Save.Where(r => r.Deleted))
                            lines.Add(string.Format("deleted here, so everywhere: '{0}' (hash {1})", r.Title, r.Hash));
                        // sales this world already gave (saved, the database not yet told) are never given twice; the
                        //  rest are given now, and the database is told after the next world save (SaveRecognized)
                        lines.AddRange(Recognize(sales?.Where(sale => !m_Applied.Contains(sale.Id)).ToList(), rows!, out List<long> recognized));
                        foreach (long id in recognized)
                            m_Applied.Add(id);
                        lines.AddRange(ApplyDecisions(decisions, rows!, out List<long> decided));
                        lines.AddRange(ApplyRenames(renames, out List<long> renamed));
                        if (decided.Count > 0 || renamed.Count > 0)
                            Task.Run(() =>
                            {   // marked applied, then a follow-up sync writes what they changed (a decision's status, a
                                //  rename's title) now: this sync wrote its rows before it read them, so the database would
                                //  otherwise wait for the next sync. Not when the marks fail (no loop).
                                bool marked = (decided.Count == 0 || MusicShareDatabase.MarkDecisionsApplied(decided))
                                    & (renamed.Count == 0 || MusicShareDatabase.MarkRenamesApplied(renamed));
                                if (marked)
                                    Timer.DelayCall(TimeSpan.Zero, () => Sync(null));
                            });
                        Remember(rows!);
                        NotifyPending(rows!, plan.Save, waiting, reviewers);
                        if (lines.Count > 0)
                            Log(lines.ToArray());
                        from?.SendMessage(string.Format("MusicShare: synced ({0} written, {1} library changes, {2} notes in {3}).",
                            plan.Save.Count, plan.Acts.Count, lines.Count, LogFile));
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex, "MusicShare.Apply");
                    }
                    finally
                    {
                        m_Busy = false;
                    }
                });
            });
        }

        /// <summary>Adopted denials get their note to the composer from the database (off the game thread).</summary>
        public static void AddDenialNotes(SyncPlan plan)
        {
            List<Act> denials = plan.Acts.Where(a => a.Kind == ActKind.Adopt && a.Row!.Status == (int)MusicLibrary.SubmissionStatus.Rejected).ToList();
            if (denials.Count == 0)
                return;
            Dictionary<int, string>? notes = MusicShareDatabase.GetDenialNotes(denials.Select(a => a.Row!.Hash));
            foreach (Act a in denials)
                if (notes != null && notes.TryGetValue(a.Row!.Hash, out string? note))
                    a.Note = note;
        }

        /// <summary>A home song rebuilt from the database gets the names it had from its applied renames (off the game thread).</summary>
        public static void AddFormerNames(SyncPlan plan)
        {
            List<Act> rebuilds = plan.Acts.Where(a => a.Kind == ActKind.Rebuild).ToList();
            if (rebuilds.Count == 0)
                return;
            Dictionary<int, List<(string Title, string Author)>>? history = MusicShareDatabase.GetRenameHistory(rebuilds.Select(a => a.Row!.Hash));
            foreach (Act a in rebuilds)
                if (history != null && history.TryGetValue(a.Row!.Hash, out List<(string Title, string Author)>? names))
                    a.FormerNames = names;
        }

        // this shard's library, as rows (game thread)
        public static List<LocalSong> Snapshot()
        {
            Mobile? system = AimsHost.Current.SystemMobile;
            List<LocalSong> list = new List<LocalSong>();
            foreach (KeyValuePair<RolledUpSheetMusic, MusicLibrary.MusicInfo> kvp in MusicLibrary.Repository)
            {
                RolledUpSheetMusic rsm = kvp.Key;
                MusicLibrary.MusicInfo info = kvp.Value;
                if (rsm == null || rsm.Deleted || info == null || rsm.HashCode == 0)
                    continue;
                Holder holder = info.Owner == null || info.Owner.Deleted ? Holder.Gone
                    : info.Owner == system ? Holder.System : Holder.Player;
                list.Add(new LocalSong { Sheet = rsm, Info = info, Holder = holder, Row = RowOf(rsm, info) });
            }
            return list;
        }

        public static SongRow RowOf(RolledUpSheetMusic rsm, MusicLibrary.MusicInfo info)
        {
            int sound = rsm.Instrument?.SuccessSound ?? 0;
            return new SongRow
            {
                Hash = rsm.HashCode,
                Composition = rsm.Composition ?? "",
                Title = rsm.Title ?? "",
                Author = rsm.Author ?? "",
                Instrument = sound == (int)RazorInstrument.InstrumentSoundType.LapHarp ? "lapharp"
                    : sound == (int)RazorInstrument.InstrumentSoundType.Lute ? "lute" : "harp",
                NewTimer = rsm.NewTimer,
                Prefetch = rsm.Prefetch,
                ConcertMode = rsm.ConcertMode,
                Tempo = rsm.Tempo,
                Napster = info.Napster || rsm.Napster,
                Price = info.Price,
                Status = (int)info.Status,
                Reason = (int)info.Reason,
                ComposerGuid = info.OwnerGUID != 0 ? info.OwnerGUID : AimsHost.Current.AccountGuid(info.Owner),
                // the composer's name; when the character is gone, the last name the database knew (an archive keeps who
                //  made a song), else the Author
                ComposerName = info.Owner != null && !info.Owner.Deleted ? info.Owner.Name
                    : ComposerOf(rsm.HashCode) is (string known, uint _) && known.Length > 0 ? known : rsm.Author ?? "",
                DateSubmitted = info.DateSubmitted.Ticks,
                DateApproved = info.DateApproved.Ticks,
                DateRemastered = info.DateRemastered.Ticks,
            };
        }

        // the plan's library changes (game thread); returns its log lines
        public static List<string> Apply(SyncPlan plan)
        {
            List<string> lines = new List<string>();
            Mobile? system = AimsHost.Current.SystemMobile;
            foreach (Act a in plan.Acts)
            {
                try
                {
                    switch (a.Kind)
                    {
                        case ActKind.Import:
                            if (FindLocal(a.Row!.Hash) == null)
                            {
                                Build(a.Row, system, foreign: true);
                                lines.Add(string.Format("imported '{0}' by {1} (home {2}), {3}", a.Row.Title, a.Row.ComposerName, a.Row.HomeShard, PriceText(a.Row, foreign: true)));
                            }
                            break;
                        case ActKind.Rebuild:
                            if (FindLocal(a.Row!.Hash) == null)
                            {
                                Mobile? composer = FindCharacter(a.Row.ComposerGuid);
                                Build(a.Row, composer ?? system, foreign: false, a.FormerNames);
                                lines.Add(string.Format("rebuilt home song '{0}' ({1}), held by {2}", a.Row.Title, a.Why, composer?.Name ?? "the system account"));
                            }
                            break;
                        case ActKind.Remove:
                            if (a.Local != null && !a.Local.Sheet.Deleted && MusicLibrary.Repository.Remove(a.Local.Sheet))
                            {
                                MusicLibrary.Unqueue(a.Local.Sheet);
                                a.Local.Sheet.Delete();
                                lines.Add(string.Format("removed '{0}' ({1})", a.Local.Row.Title, a.Why));
                            }
                            break;
                        case ActKind.Convert:
                            if (a.Local != null && system != null)
                            {
                                string was = a.Local.Info.Owner?.Name ?? "no one";
                                Hold(a.Local.Sheet, a.Local.Info, system, a.Row!.ComposerGuid);
                                lines.Add(string.Format("'{0}' was {1}'s here; now held for its composer ({2})", a.Local.Row.Title, was, a.Why));
                            }
                            break;
                        case ActKind.Mirror:
                            if (a.Local != null)
                            {
                                int price = a.Row!.Napster ? 0 : a.Row.Price;
                                if (a.Local.Info.Price != price || a.Local.Info.Status != MusicLibrary.SubmissionStatus.Approved)
                                {
                                    lines.Add(string.Format("'{0}': price {1} -> {2}, status {3} -> Approved (as at home)", a.Local.Row.Title,
                                        a.Local.Info.Price, price, a.Local.Info.Status));
                                    a.Local.Info.Price = price;
                                    a.Local.Info.Status = MusicLibrary.SubmissionStatus.Approved;
                                }
                                a.Local.Info.Napster = false;
                                a.Local.Sheet.Napster = false;
                                string? names = FollowNames(a.Local.Sheet, a.Row);
                                if (names != null)
                                    lines.Add(names);
                            }
                            break;
                        case ActKind.Adopt:
                            if (a.Local != null && !a.Local.Sheet.Deleted && MusicLibrary.Repository.TryGetValue(a.Local.Sheet, out MusicLibrary.MusicInfo? info))
                            {
                                SongRow r = a.Row!;
                                if (r.Status == Approved)
                                {
                                    if (system == null)
                                        break;      // next sync: there is a system account to approve as
                                    // the music box's own approval (a remaster takes over the song it remasters; the
                                    //  Bards Guild points and the composer's news come back with it, as the world lost
                                    //  them too), but no "New music" post: that went out the first time
                                    AngelIslandAimsHost.QuietApprovals = true;
                                    try
                                    {
                                        MusicBox.ApproveTrack(system, a.Local.Sheet);
                                    }
                                    finally
                                    {
                                        AngelIslandAimsHost.QuietApprovals = false;
                                    }
                                    // the database's dates, not today's
                                    if (MusicLibrary.Repository.TryGetValue(a.Local.Sheet, out MusicLibrary.MusicInfo? approved))
                                    {
                                        if (r.DateApproved > 0) approved.DateApproved = new DateTime(r.DateApproved, DateTimeKind.Utc);
                                        if (r.DateRemastered > 0) approved.DateRemastered = new DateTime(r.DateRemastered, DateTimeKind.Utc);
                                    }
                                }
                                else
                                {
                                    info.Status = (MusicLibrary.SubmissionStatus)r.Status;
                                    info.Reason = (MusicLibrary.RejectionReason)r.Reason;
                                    info.Note = a.Note;
                                    MusicLibrary.Unqueue(a.Local.Sheet);
                                }
                                lines.Add(string.Format("'{0}': {1}; the database's decision stands (a crash or rollback lost it here){2}",
                                    r.Title, a.Why, a.Note.Length > 0 ? ", note: " + a.Note : ""));
                            }
                            break;
                        case ActKind.Relink:
                            if (a.Local != null)
                            {
                                Mobile? composer = FindCharacter(a.Row!.ComposerGuid);
                                if (composer != null)
                                {
                                    a.Local.Info.Owner = composer;
                                    a.Local.Sheet.Owner = composer;
                                    RestorePrice(a.Local.Info, a.Row);
                                    lines.Add(string.Format("'{0}' is {1}'s again (their account's character on this shard)", a.Row.Title, composer.Name));
                                }
                                else if (a.Local.Holder == Holder.Gone && system != null)
                                {
                                    Hold(a.Local.Sheet, a.Local.Info, system, a.Row.ComposerGuid);
                                    RestorePrice(a.Local.Info, a.Row);
                                    lines.Add(string.Format("'{0}': its composer has no character here; held for them, priced", a.Row.Title));
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "MusicShare: " + a.Kind);
                }
            }
            return lines;
        }

        private static string PriceText(SongRow r, bool foreign) =>
            foreign && r.Napster ? "free (Napster)" : r.Price > 0 ? r.Price + " gold" : "free";

        private static void RestorePrice(MusicLibrary.MusicInfo info, SongRow r)
        {   // the library's maintenance frees a song whose composer is gone; held for them, it keeps its price
            if (info.Price == 0 && r.Price > 0 && !r.Napster)
                info.Price = r.Price;
        }

        private static void Hold(RolledUpSheetMusic sheet, MusicLibrary.MusicInfo info, Mobile system, uint composerGuid)
        {   // the owner setters stamp the owner's GUID: the composer's goes back on after
            info.Owner = system;
            info.OwnerGUID = composerGuid;
            sheet.Owner = system;
            sheet.OwnerGUID = composerGuid;
        }

        // sheet music of a database song, as transcribing it would make it
        private static RolledUpSheetMusic MakeSheet(SongRow r, Mobile? owner, int price, bool napster)
        {
            RolledUpSheetMusic rsm = new RolledUpSheetMusic(owner!, RazorInstrument.GetSoundType(r.Instrument), r.Author, r.Title, price, r.Composition);
            rsm.HashCode = r.Hash;
            rsm.SongBytes = null;
            rsm.SongName = r.Title;
            rsm.Author = r.Author;
            rsm.ConfigureInstrument(r.Instrument);
            rsm.NewTimer = r.NewTimer;
            rsm.Tempo = r.Tempo;
            rsm.Prefetch = r.Prefetch;
            rsm.Napster = napster;
            rsm.ConcertMode = r.ConcertMode;
            rsm.OwnerGUID = r.ComposerGuid;
            return rsm;
        }

        // a copy of another shard's song takes its home's names (a rename there): title, author and the notes, whose
        //  '# Song:' line the instrument reads as it plays (the notes only when they are the same song: they differ in #
        //  lines alone). Its log line, or null: nothing changed.
        public static string? FollowNames(RolledUpSheetMusic sheet, SongRow row)
        {
            string title = sheet.Title ?? "", author = sheet.Author ?? "", notes = sheet.Composition ?? "";
            bool sameNotes = notes == row.Composition || row.Composition.Length == 0;
            if (title == row.Title && author == row.Author && sameNotes)
                return null;
            sheet.Title = row.Title;
            sheet.SongName = row.Title;
            sheet.Author = row.Author;
            if (!sameNotes)
            {
                ManuscriptPaper.LocationTarget hasher = new ManuscriptPaper.LocationTarget(null!);
                if (hasher.CompileHashCode(row.Composition) == hasher.CompileHashCode(notes))
                    sheet.Composition = row.Composition;
                else
                    return string.Format("'{0}': named as at home; its notes differ from home's in more than names, kept", row.Title);
            }
            return title == row.Title ? string.Format("'{0}': author or notes as at home", row.Title)
                : string.Format("'{0}' is '{1}' now (renamed at home)", title, row.Title);
        }

        // a library copy of a database song: the sheet (in internal storage, as a submitted one) and its record; a home
        //  song keeps the names it had (formerNames: its applied renames)
        private static void Build(SongRow r, Mobile? owner, bool foreign, List<(string Title, string Author)>? formerNames = null)
        {
            bool napster = r.Napster && !foreign;               // a Napster song is free elsewhere, not passed hand to hand
            int price = r.Napster && foreign ? 0 : r.Price;
            RolledUpSheetMusic rsm = MakeSheet(r, owner, price, napster);
            AimsHost.Current.MoveToInternal(rsm);

            MusicLibrary.MusicInfo info = new MusicLibrary.MusicInfo(owner!, price);
            info.OwnerGUID = r.ComposerGuid;
            info.Napster = napster;
            info.Status = MusicLibrary.SubmissionStatus.Approved;
            info.ComposerTold = true;           // a copy built from the database is no news to its composer
            if (r.DateSubmitted > 0) info.DateSubmitted = new DateTime(r.DateSubmitted, DateTimeKind.Utc);  // not today's
            if (r.DateApproved > 0) info.DateApproved = new DateTime(r.DateApproved, DateTimeKind.Utc);
            if (r.DateRemastered > 0) info.DateRemastered = new DateTime(r.DateRemastered, DateTimeKind.Utc);
            if (formerNames != null)
                foreach ((string title, string author) in formerNames)
                    info.AddFormerName(title, author);
            MusicLibrary.Repository.Add(rsm, info);
        }

        private static RolledUpSheetMusic? FindLocal(int hash) =>
            MusicLibrary.Repository.Keys.FirstOrDefault(k => k != null && !k.Deleted && k.HashCode == hash);

        /// <summary>A character on this shard of the account with this GUID (null: none).</summary>
        public static PlayerMobile? FindCharacter(uint guid) =>
            guid == 0 ? null : PlayerMobile.Instances.FirstOrDefault(pm => pm != null && !pm.Deleted && pm.AcctGUID == guid);
        #endregion Sync

        #region Sales elsewhere
        /// <summary>
        /// A purchase on this shard (AngelIslandAimsHost.OnMusicPurchased). A song held for its composer (owned by the
        /// system account: its home is elsewhere) is recorded in the database, for its composer's recognition at home.
        /// </summary>
        public static void OnPurchase(Mobile buyer, RolledUpSheetMusic song, MusicLibrary.MusicInfo info, int price)
        {
            if (!Enabled || !CanWrite || buyer == null || song == null || info == null || info.Owner != AimsHost.Current.SystemMobile)
                return;
            // the Music Conservatory's players have free money: its purchases are its own, and earn composers nothing
            //  elsewhere (no Music Sales points, no Music Karma)
            if (Core.GMNMC_CFG)
                return;

            SaleRow sale = new SaleRow
            {
                SongHash = song.HashCode,
                SoldOnShard = ThisShard,
                BuyerName = buyer.Name ?? "someone",
                BuyerGuid = AimsHost.Current.AccountGuid(buyer),
                Price = price,
                Utc = DateTime.UtcNow.Ticks,
            };
            string line = string.Format("sold '{0}' to {1} for {2} gold (the LB fund's); recorded for its composer", song.Title, sale.BuyerName, price);
            Task.Run(() =>
            {
                bool ok = MusicShareDatabase.AddSale(sale);
                Timer.DelayCall(TimeSpan.Zero, () => Log(ok ? line : line + ": NOT recorded (database error)"));
            });
        }

        /// <summary>
        /// Sales elsewhere of this shard's songs: a Music Sales point, Music Karma and a notice for each composer online
        /// (the others wait until they are). Returns the log lines; 'done' gets the sales recognized, for the database.
        /// </summary>
        public static List<string> Recognize(List<SaleRow>? sales, List<SongRow> rows, out List<long> done)
        {
            List<string> lines = new List<string>();
            done = new List<long>();
            if (sales == null || sales.Count == 0)
                return lines;

            Dictionary<int, SongRow> byHash = rows.GroupBy(r => r.Hash).ToDictionary(g => g.Key, g => g.First());
            foreach (SaleRow s in sales)
            {
                if (!byHash.TryGetValue(s.SongHash, out SongRow? song))
                    continue;
                RolledUpSheetMusic? local = FindLocal(s.SongHash);
                Mobile? owner = local != null && MusicLibrary.Repository.TryGetValue(local, out MusicLibrary.MusicInfo? info) ? info.Owner : null;
                PlayerMobile? composer = owner is PlayerMobile pm && !pm.Deleted ? pm : FindCharacter(song.ComposerGuid);
                if (composer == null || composer.NetState == null)
                    continue;       // told when they're next online here

                DataRecorder.DataRecorder.Add(composer, RecorderDomain.MusicPoints, 1);
                if (composer.NpcGuild == NpcGuild.BardsGuild)
                    AimsHost.Current.SetMusicKarma(composer, AimsHost.Current.GetMusicKarma(composer) + MusicLibrary.SalesFactor);
                composer.SendMessage(54, string.Format("{0} bought your song '{1}' on {2}.", s.BuyerName, song.Title, s.SoldOnShard));
                done.Add(s.Id);
                lines.Add(string.Format("recognized: {0} bought '{1}' on {2}; {3} told", s.BuyerName, song.Title, s.SoldOnShard, composer.Name));
            }
            return lines;
        }
        #endregion Sales elsewhere

        #region Homes and review across shards
        // each song's home, from the last sync (for the host's notes and refusals: AngelIslandAimsHost)
        private static Dictionary<int, string> m_Homes = new Dictionary<int, string>();
        // the pending list each staff member or reviewer last asked for ([MusicShare / [MusicReview listen/approve/deny <n>)
        private static readonly Dictionary<Mobile, List<SongRow>> m_PendingLists = new Dictionary<Mobile, List<SongRow>>();
        private static readonly TimeSpan ListenTime = TimeSpan.FromMinutes(10);
        // listen copies not yet gone: heard, never submitted (AngelIslandAimsHost.RefuseSubmission asks IsReviewCopy)
        private static readonly HashSet<Serial> m_ReviewCopies = new HashSet<Serial>();
        // a note to the composer: as the music box's note prompt (MusicBox RejectionNotePrompt.MaxLength, ClassicUO's chat box)
        private const int NoteMaxLength = 100;

        // each song's composer (by hash), and the composer names each account has had (for the arranger of a song held
        //  for its composer: AngelIslandAimsHost.Arranger)
        private static Dictionary<int, (string Name, uint Guid)> m_Composers = new Dictionary<int, (string Name, uint Guid)>();
        private static Dictionary<uint, HashSet<string>> m_AccountComposers = new Dictionary<uint, HashSet<string>>();

        /// <summary>Remembers each song's home and composer (the database's rows: at startup, then each sync's).</summary>
        public static void Remember(IEnumerable<SongRow> rows)
        {
            Dictionary<int, string> homes = new Dictionary<int, string>();
            Dictionary<int, (string Name, uint Guid)> composers = new Dictionary<int, (string Name, uint Guid)>();
            Dictionary<uint, HashSet<string>> accounts = new Dictionary<uint, HashSet<string>>();
            foreach (SongRow r in rows)
                if (!r.Deleted)
                {
                    homes[r.Hash] = r.HomeShard;
                    string name = AimsHost.PlainName(r.ComposerName);
                    if (name.Length == 0)
                        continue;
                    composers[r.Hash] = (name, r.ComposerGuid);
                    if (!accounts.TryGetValue(r.ComposerGuid, out HashSet<string>? names))
                        accounts[r.ComposerGuid] = names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    names.Add(name);
                }
            m_Homes = homes;
            m_Composers = composers;
            m_AccountComposers = accounts;
        }

        /// <summary>The home shard of the song with this hash, as of the last sync (null: unknown).</summary>
        public static string? HomeOf(int hash) => m_Homes.TryGetValue(hash, out string? home) ? home : null;

        /// <summary>The composer of the song with this hash (their character's name, and account), as of the last sync.</summary>
        public static (string Name, uint Guid)? ComposerOf(int hash) => m_Composers.TryGetValue(hash, out var c) ? c : null;

        /// <summary>Is this one of the names this account has composed under, on any shard (as of the last sync)?</summary>
        public static bool ComposedAs(uint account, string? name) =>
            account != 0 && m_AccountComposers.TryGetValue(account, out HashSet<string>? names) && names.Contains(AimsHost.PlainName(name));

        /// <summary>A shard as players know it: "SiegePerilous" -> "Siege Perilous", "Upheaval-TC" -> "Upheaval Test Center".</summary>
        public static string ShardName(string shard)
        {
            if (shard == ConservatoryShard)
                return Core.MusicConservatoryName;      // "composed on Angel Island Music Conservatory"
            string name = shard.EndsWith("-TC") ? shard[..^3] + " Test Center" : shard.EndsWith("-EV") ? shard[..^3] + " event shard" : shard;
            return System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        }

        /// <summary>The songs awaiting review on any shard, by home, oldest first.</summary>
        public static List<SongRow> PendingSongs(IEnumerable<SongRow> rows) =>
            rows.Where(r => !r.Deleted && r.Status == (int)MusicLibrary.SubmissionStatus.Pending)
                .OrderBy(r => r.HomeShard, StringComparer.Ordinal).ThenBy(r => r.DateSubmitted).ToList();

        // every sync: Administrators and up, and reviewers, online hear how many songs await review on every shard, this
        //  one's among them (AIMS's own notice is off while music is shared: AngelIslandAimsHost.ReviewNeeded). The rows
        //  are the database's as read, with what this sync wrote over them (a song submitted here counts at once). A paused
        //  reviewer ([MusicReview pause) hears nothing. Test Center and event shards say nothing.
        private static void NotifyPending(List<SongRow> rows, IEnumerable<SongRow> written, Dictionary<int, DecisionRow>? waiting, List<ReviewerRow>? reviewers)
        {
            if (!CanWrite)
                return;
            Dictionary<int, SongRow> current = new Dictionary<int, SongRow>();
            foreach (SongRow r in rows.Concat(written))
                current[r.Hash] = r;
            string? notice = ReviewNotice(current.Values, waiting);
            if (notice == null)
                return;
            HashSet<string> accounts = new HashSet<string>((reviewers ?? new List<ReviewerRow>()).Select(r => r.Account), StringComparer.OrdinalIgnoreCase);
            foreach (NetState ns in NetState.Instances.ToList())
            {
                Mobile? m = ns.Mobile;
                if (m == null || m.Deleted || IsPaused(m))
                    continue;
                if (m.AccessLevel >= AccessLevel.Administrator || accounts.Contains(AccountName(m)))
                    m.SendMessage(54, notice);
            }
        }

        /// <summary>
        /// The review notice: how many songs await review on every shard, by home, leaving out those with a decision
        /// waiting to be applied (null: none await review).
        /// </summary>
        public static string? ReviewNotice(IEnumerable<SongRow> rows, IReadOnlyDictionary<int, DecisionRow>? waiting)
        {
            List<IGrouping<string, SongRow>> byHome = PendingSongs(rows).Where(r => waiting == null || !waiting.ContainsKey(r.Hash))
                .GroupBy(r => r.HomeShard).ToList();
            int n = byHome.Sum(g => g.Count());
            if (n == 0)
                return null;
            return string.Format("[Music System] {0} song{1} awaiting review: {2} ([MusicReview list; [MusicReview pause quiets these).",
                n, n == 1 ? "" : "s", string.Join(", ", byHome.Select(g => ShardName(g.Key) + " " + g.Count())));
        }

        /// <summary>
        /// Review decisions made on any shard about this shard's pending songs: approved through the music box's own
        /// approval (MusicBox.ApproveTrack), or rejected. A song no longer pending (or no longer here) sets its decision
        /// aside. Returns the log lines; 'done' gets every decision handled.
        /// </summary>
        public static List<string> ApplyDecisions(List<DecisionRow>? decisions, List<SongRow> rows, out List<long> done)
        {
            List<string> lines = new List<string>();
            done = new List<long>();
            if (decisions == null || decisions.Count == 0)
                return lines;

            Mobile? system = AimsHost.Current.SystemMobile;
            foreach (DecisionRow d in decisions)
            {
                done.Add(d.Id);
                string title = rows.FirstOrDefault(r => r.Hash == d.SongHash)?.Title ?? ("hash " + d.SongHash);
                string by = string.Format("{0} on {1}", d.Staff, ShardName(d.OnShard));
                RolledUpSheetMusic? local = FindLocal(d.SongHash);
                if (local == null || !MusicLibrary.Repository.TryGetValue(local, out MusicLibrary.MusicInfo? info))
                {
                    lines.Add(string.Format("decision by {0} on '{1}' set aside: the song isn't in this library", by, title));
                    continue;
                }
                if (info.Status != MusicLibrary.SubmissionStatus.Pending)
                {
                    lines.Add(string.Format("decision by {0} on '{1}' set aside: it's already {2}", by, title, info.Status));
                    continue;
                }
                if (d.Approve)
                {
                    if (system == null)
                    {
                        done.Remove(d.Id);      // try again when there is a system account to approve as
                        continue;
                    }
                    MusicBox.ApproveTrack(system, local);
                    lines.Add(string.Format("approved '{0}' (decided by {1})", title, by));
                }
                else
                {
                    info.Status = MusicLibrary.SubmissionStatus.Rejected;
                    info.Reason = (MusicLibrary.RejectionReason)d.Reason;
                    info.Note = d.Note;             // the composer is told at their next music box visit (TellComposer)
                    MusicLibrary.Unqueue(local);
                    lines.Add(string.Format("rejected '{0}', {1}{2} (decided by {3})", title, info.Reason,
                        string.IsNullOrEmpty(info.Note) ? "" : ": " + info.Note, by));
                }
            }
            return lines;
        }

        /// <summary>
        /// Staff renames of this shard's songs ([MusicShare titles apply), applied in the order made (game thread): the
        /// library's own rename (MusicLibrary.Rename: the composer hears of it), and its About and Tags when the rename sets
        /// them (MusicLibrary.Describe: not news). The next sync writes the new names and notes. A song
        /// not in this library sets its rename aside. Returns its log lines; 'done' the renames to mark applied.
        /// </summary>
        public static List<string> ApplyRenames(List<RenameRow>? renames, out List<long> done)
        {
            List<string> lines = new List<string>();
            done = new List<long>();
            if (renames == null || renames.Count == 0)
                return lines;

            foreach (RenameRow n in renames)
            {
                done.Add(n.Id);
                RolledUpSheetMusic? local = FindLocal(n.SongHash);
                if (local == null)
                {
                    lines.Add(string.Format("rename by {0} of '{1}' set aside: the song isn't in this library", n.Staff, n.OldTitle));
                    continue;
                }
                string was = local.Title ?? "";
                bool renamed = MusicLibrary.Rename(local, n.NewTitle, n.NewAuthor, n.Note);
                if (renamed)
                    lines.Add(string.Format("renamed '{0}' to '{1}'{2} (by {3})", was, local.Title,
                        n.NewAuthor.Length > 0 ? ", author " + local.Author : "", n.Staff));
                // its About and Tags (the notes' lines; not news for the composer)
                bool described = (n.NewAbout != null || n.NewTags != null) && MusicLibrary.Describe(local, n.NewAbout, n.NewTags);
                if (described)
                    lines.Add(string.Format("'{0}': about '{1}', tags '{2}' (by {3})", local.Title, local.About, local.Tags, n.Staff));
                if (!renamed && !described)
                    lines.Add(string.Format("rename by {0} of '{1}': it already has that name{2}", n.Staff, was,
                        n.NewAbout != null || n.NewTags != null ? ", about and tags" : ""));
            }
            return lines;
        }

        // the commands that review ([MusicShare for staff, [MusicReview for reviewers): replies start with the command's name
        private const string ShareCommand = "MusicShare", ReviewCommand = "MusicReview";
        private static string ListVerb(string cmd) => cmd == ReviewCommand ? "list" : "pending";

        private static void Pending(Mobile from, string cmd)
        {
            Task.Run(() =>
            {
                List<SongRow>? rows = MusicShareDatabase.GetSongs();
                Dictionary<int, DecisionRow>? waiting = rows == null ? null : MusicShareDatabase.GetWaitingDecisions();
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    if (rows == null)
                    {
                        from.SendMessage("{0}: the shared music database couldn't be read.", cmd);
                        return;
                    }
                    List<SongRow> pending = PendingSongs(rows);
                    m_PendingLists[from] = pending;
                    if (pending.Count == 0)
                    {
                        from.SendMessage("{0}: no songs are awaiting review on any shard.", cmd);
                        return;
                    }
                    from.SendMessage("{0}: {1} song{2} awaiting review ([{0} listen, approve or deny <n>):",
                        cmd, pending.Count, pending.Count == 1 ? "" : "s");
                    for (int i = 0; i < pending.Count; i++)
                        from.SendMessage(PendingLine(i + 1, pending[i], waiting != null && waiting.TryGetValue(pending[i].Hash, out DecisionRow? d) ? d : null));
                });
            });
        }

        /// <summary>One line of the pending list: number, title, composer, home and date; then a decision waiting to be applied.</summary>
        public static string PendingLine(int n, SongRow r, DecisionRow? waiting) =>
            string.Format("{0}. '{1}' by {2}, {3}, submitted {4:yyyy-MM-dd}{5}", n, r.Title, r.ComposerName, ShardName(r.HomeShard),
                new DateTime(r.DateSubmitted, DateTimeKind.Utc), waiting == null ? "" : " - " + Decided(waiting));

        // "approved by Adam Ant, applies within 5 minutes" / "denied (Other) by ..."
        private static string Decided(DecisionRow d) =>
            string.Format("{0} by {1}, applies within {2} minutes", d.Approve ? "approved" : "denied (" + (MusicLibrary.RejectionReason)d.Reason + ")",
                d.Staff, (int)Interval.TotalMinutes);

        // the n-th song of the staff member's or reviewer's last pending list (null: they are told why)
        private static SongRow? Pick(Mobile from, string[] args, string cmd)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int n))
            {
                from.SendMessage("{0}: which song? Its number in [{0} {1}.", cmd, ListVerb(cmd));
                return null;
            }
            if (!m_PendingLists.TryGetValue(from, out List<SongRow>? list) || n < 1 || n > list.Count)
            {
                from.SendMessage("{0}: there's no song {1} in your last [{0} {2}.", cmd, n, ListVerb(cmd));
                return null;
            }
            return list[n - 1];
        }

        // a review copy of a pending song in the backpack, played; it disappears after ListenTime
        private static void Listen(Mobile from, SongRow row, string cmd)
        {
            if (from.Backpack == null)
                return;
            RolledUpSheetMusic sheet = MakeReviewCopy(row, from);
            from.Backpack.DropItem(sheet);
            from.SendMessage("{0}: '{1}' ({2}) is in your backpack for {3} minutes: double-click it to play it.",
                cmd, row.Title, ShardName(row.HomeShard), (int)ListenTime.TotalMinutes);
            sheet.OnDoubleClick(from);
        }

        /// <summary>
        /// A review copy of a song awaiting review ('listen'): temporary, gone after ListenTime, and never accepted by a music
        /// box (IsReviewCopy; AngelIslandAimsHost.RefuseSubmission), so whoever holds it can't submit the song as theirs.
        /// </summary>
        public static RolledUpSheetMusic MakeReviewCopy(SongRow row, Mobile owner)
        {
            RolledUpSheetMusic sheet = MakeSheet(row, owner, 0, napster: false);
            AimsHost.Current.MarkTemporary(sheet);
            Serial serial = sheet.Serial;
            m_ReviewCopies.Add(serial);
            Timer.DelayCall(ListenTime, () =>
            {
                m_ReviewCopies.Remove(serial);
                if (!sheet.Deleted)
                    sheet.Delete();
            });
            return sheet;
        }

        /// <summary>Is this a review copy ('listen') of a song awaiting review?</summary>
        public static bool IsReviewCopy(Item? item) => item != null && m_ReviewCopies.Contains(item.Serial);

        private static void Decide(Mobile from, SongRow row, bool approve, string[] args, string cmd)
        {
            if (!CanWrite)
            {
                from.SendMessage("{0}: a Test Center or event shard only reads the shared music: decide on a live shard.", cmd);
                return;
            }
            string decider = from.Name ?? "staff";
            Task.Run(() =>
            {
                string reply = RecordReviewDecision(decider, row, approve, args, cmd, out string? logged);
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    from.SendMessage(reply);
                    if (logged != null)
                        Log(logged);
                });
            });
        }

        // deny, as at a music box: deny <n> alone asks for the reason, and Other asks for the note it needs; the typed forms
        //  (deny <n> Duplicate, deny <n> Other <note>, deny <n> <note>) skip the questions. Recorded only once complete.
        private static void Deny(Mobile from, SongRow row, string[] args, string cmd)
        {
            if (!CanWrite)
            {
                from.SendMessage("{0}: a Test Center or event shard only reads the shared music: decide on a live shard.", cmd);
                return;
            }
            switch (DenyAsks(args))
            {
                case DenyQuestion.Reason:
                    from.SendMessage("{0}: why isn't '{1}' approved? One of: {2} (Esc: not denied)", cmd, row.Title, string.Join(", ", DenyReasons));
                    from.Prompt = new DenyReasonPrompt(row, cmd);
                    break;
                case DenyQuestion.Note:
                    AskNote(from, row, cmd);
                    break;
                default:
                    Decide(from, row, false, args, cmd);
                    break;
            }
        }

        /// <summary>What deny still has to ask (args: "deny", n, then the typed reason and note, if any).</summary>
        public enum DenyQuestion { None, Reason, Note }

        /// <summary>What a deny with these arguments asks: the reason (none typed), the note (Other typed alone), or nothing.</summary>
        public static DenyQuestion DenyAsks(string[] args)
        {
            if (args.Length < 3 || string.IsNullOrWhiteSpace(args[2]))
                return DenyQuestion.Reason;
            if (args.Length == 3 && string.Equals(args[2].Trim(), MusicLibrary.RejectionReason.Other.ToString(), StringComparison.OrdinalIgnoreCase))
                return DenyQuestion.Note;
            return DenyQuestion.None;
        }

        /// <summary>The reasons a denial gives (every RejectionReason but None).</summary>
        public static IEnumerable<string> DenyReasons =>
            Enum.GetNames(typeof(MusicLibrary.RejectionReason)).Where(n => n != MusicLibrary.RejectionReason.None.ToString());

        /// <summary>A reason from the reason prompt: a listed reason in any letter case; any other answer is Other, as at a music box.</summary>
        public static MusicLibrary.RejectionReason ReasonOf(string text) =>
            Enum.TryParse((text ?? "").Trim(), true, out MusicLibrary.RejectionReason r) && Enum.IsDefined(r) && r != MusicLibrary.RejectionReason.None
                ? r : MusicLibrary.RejectionReason.Other;

        private static void AskNote(Mobile from, SongRow row, string cmd)
        {
            from.SendMessage("{0}: tell the composer why, in one line (up to {1} characters; Esc: not denied, since Other needs a note):", cmd, NoteMaxLength);
            from.Prompt = new DenyNotePrompt(row, cmd);
        }

        // deny's reason
        private sealed class DenyReasonPrompt : Prompt
        {
            private readonly SongRow m_Row;
            private readonly string m_Cmd;

            public DenyReasonPrompt(SongRow row, string cmd)
            {
                m_Row = row;
                m_Cmd = cmd;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    OnCancel(from);
                    return;
                }
                MusicLibrary.RejectionReason reason = ReasonOf(text);
                if (reason == MusicLibrary.RejectionReason.Other)
                    AskNote(from, m_Row, m_Cmd);
                else
                    Decide(from, m_Row, false, new[] { "deny", "", reason.ToString() }, m_Cmd);
            }

            public override void OnCancel(Mobile from)
            {
                from.SendMessage("{0}: not denied: '{1}'.", m_Cmd, m_Row.Title);
            }
        }

        // deny's note to the composer, for Other (it needs one)
        private sealed class DenyNotePrompt : Prompt
        {
            private readonly SongRow m_Row;
            private readonly string m_Cmd;

            public DenyNotePrompt(SongRow row, string cmd)
            {
                m_Row = row;
                m_Cmd = cmd;
            }

            public override void OnResponse(Mobile from, string text)
            {
                string note = (text ?? "").Trim();
                if (note.Length == 0)
                {
                    OnCancel(from);
                    return;
                }
                if (note.Length > NoteMaxLength)        // as at a music box: longer text from another client is cut
                    note = note.Substring(0, NoteMaxLength);
                Decide(from, m_Row, false, new[] { "deny", "", MusicLibrary.RejectionReason.Other.ToString(), note }, m_Cmd);
            }

            public override void OnCancel(Mobile from)
            {
                from.SendMessage("{0}: not denied: Other needs a note to the composer. Pick a listed reason, or deny it again and write one.", m_Cmd);
            }
        }

        /// <summary>
        /// Records an approve or deny of this song by 'decider' and returns the reply; 'logged' is the log line when it was
        /// recorded. deny <n> [reason] [note...]: a reason word (TooLong, TooShort, Inappropriate, Duplicate, Other), then the
        /// note to the composer; words that aren't a reason are the note (reason Other). A second decision on a song that
        /// has one waiting, or one on a song no longer awaiting review, is refused. Off the game thread (the shared database).
        /// </summary>
        public static string RecordReviewDecision(string decider, SongRow row, bool approve, string[] args, string cmd, out string? logged)
        {
            logged = null;
            MusicLibrary.RejectionReason reason = MusicLibrary.RejectionReason.Other;
            int noteFrom = 2;
            if (!approve && args.Length > 2 && Enum.TryParse(args[2], true, out MusicLibrary.RejectionReason named)
                && Enum.IsDefined(named) && named != MusicLibrary.RejectionReason.None)
            {
                reason = named;
                noteFrom = 3;
            }
            string note = approve ? "" : string.Join(" ", args.Skip(noteFrom)).Trim();
            if (note.Length > NoteMaxLength)
                return string.Format("{0}: that note is {1} characters; a note to the composer holds up to {2}.", cmd, note.Length, NoteMaxLength);
            if (!approve && reason == MusicLibrary.RejectionReason.Other && note.Length == 0)
                return string.Format("{0}: Other needs a note to the composer: [{0} deny <n> asks for it.", cmd);

            DecisionRow d = new DecisionRow
            {
                SongHash = row.Hash,
                Approve = approve,
                Reason = (int)(approve ? MusicLibrary.RejectionReason.None : reason),
                Note = note,
                Staff = decider,
                OnShard = ThisShard,
                Utc = DateTime.UtcNow.Ticks,
            };
            string text = string.Format("{0} '{1}'{2}: {3} applies it at its next sync (within {4} minutes)", approve ? "approve" : "deny",
                row.Title, approve ? "" : " (" + reason + (note.Length > 0 ? ": " + note : "") + ")", ShardName(row.HomeShard), (int)Interval.TotalMinutes);
            switch (MusicShareDatabase.RecordDecision(d, out DecisionRow? waiting))
            {
                case DecisionOutcome.Recorded:
                    logged = "decision recorded: " + text + " (by " + decider + ")";
                    return cmd + ": recorded: " + text + ".";
                case DecisionOutcome.AlreadyDecided:
                    return string.Format("{0}: '{1}' is already decided: {2}.", cmd, row.Title, Decided(waiting!));
                case DecisionOutcome.NotPending:
                    return string.Format("{0}: '{1}' isn't awaiting review any more ([{0} {2} for the current list).", cmd, row.Title, ListVerb(cmd));
                default:
                    return cmd + ": the decision couldn't be recorded (database error).";
            }
        }
        #endregion Homes and review across shards

        #region Seeding
        // First time only, before sharing is turned on anywhere: 'collect' on each live shard (its library into the staging
        //  table), 'review' once they all have (seed-review.csv: each song's home, by the rule, for staff to check and edit),
        //  then 'seed <file>' (the songs, with the homes in the file). Nothing depends on which shard collects first.

        /// <summary>One song's seeding choice: its candidates (shard and the composer's last login there) and its home.</summary>
        public sealed class SeedChoice
        {
            public int Hash;
            public string Home = "";
            public string Title = "", Composer = "";
            public int Status;
            public List<(string Shard, long LastLogin)> Candidates = new List<(string, long)>();
        }

        /// <summary>
        /// The rule: a song in one library belongs to that shard; a song in several belongs to the shard where its composer's
        /// account last logged in (the shard they play on today). Same notes, same song (by hash).
        /// </summary>
        public static List<SeedChoice> ChooseHomes(IEnumerable<(string Shard, SongRow Row, long LastLogin)> candidates)
        {
            List<SeedChoice> choices = new List<SeedChoice>();
            foreach (IGrouping<int, (string Shard, SongRow Row, long LastLogin)> g in candidates.GroupBy(c => c.Row.Hash))
            {
                var ordered = g.OrderByDescending(c => c.LastLogin).ThenBy(c => c.Shard, StringComparer.Ordinal).ToList();
                var home = ordered[0];
                choices.Add(new SeedChoice
                {
                    Hash = g.Key,
                    Home = home.Shard,
                    Title = home.Row.Title,
                    Composer = home.Row.ComposerName,
                    Status = home.Row.Status,
                    Candidates = ordered.Select(c => (c.Shard, c.LastLogin)).ToList(),
                });
            }
            return choices.OrderBy(c => c.Composer, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void Collect(Mobile from)
        {
            if (!CanWrite || Core.RuleSets.LoginServerRules())
            {
                from.SendMessage("MusicShare: a Test Center, event or login server has no songs of its own to collect.");
                return;
            }
            Dictionary<uint, long> lastLogin = new Dictionary<uint, long>();
            foreach (object o in Accounting.Accounts.Table.Values)
                if (o is Accounting.Account a && a.GUID != 0)
                    lastLogin[a.GUID] = Math.Max(lastLogin.TryGetValue(a.GUID, out long t) ? t : 0, a.LastLogin.Ticks);

            List<(SongRow, long)> rows = Snapshot().Where(l => l.Holder != Holder.System)
                .Select(l => { l.Row.HomeShard = ThisShard; return (l.Row, lastLogin.TryGetValue(l.Row.ComposerGuid, out long t) ? t : 0L); }).ToList();
            string shard = ThisShard;
            Task.Run(() =>
            {
                int n = MusicShareDatabase.SaveCandidates(shard, rows);
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    string text = n < 0 ? "collect failed (database error)" : string.Format("collected {0} songs of {1} for seeding", n, shard);
                    Log(text + " (by " + from.Name + ")");
                    from.SendMessage("MusicShare: " + text + ".");
                });
            });
        }

        private static void Review(Mobile from)
        {
            Task.Run(() =>
            {
                List<(string, SongRow, long)>? candidates = MusicShareDatabase.GetCandidates();
                string path = Path.Combine(Core.LogsDirectory, "seed-review.csv");
                string text;
                if (candidates == null)
                    text = "review failed (database error)";
                else
                {
                    List<SeedChoice> choices = ChooseHomes(candidates);
                    List<string> lines = new List<string> { "Hash,Home,Title,Composer,Status,Candidates (shard: composer's last login, newest first)" };
                    foreach (SeedChoice c in choices)
                        lines.Add(string.Join(",", c.Hash.ToString(), Csv(c.Home), Csv(c.Title), Csv(c.Composer),
                            ((MusicLibrary.SubmissionStatus)c.Status).ToString(),
                            Csv(string.Join(" | ", c.Candidates.Select(x => x.Shard + ": " + (x.LastLogin > 0 ? new DateTime(x.LastLogin, DateTimeKind.Utc).ToString("yyyy-MM-dd") : "never"))))));
                    File.WriteAllLines(path, lines);
                    text = string.Format("{0} songs from {1} shards: {2}", choices.Count, candidates.Select(c => c.Item1).Distinct().Count(), path);
                }
                Timer.DelayCall(TimeSpan.Zero, () => from.SendMessage("MusicShare: review: " + text));
            });
        }

        private static void Seed(Mobile from, string path)
        {
            Task.Run(() =>
            {
                string text;
                try
                {
                    text = SeedFrom(path);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "MusicShare.Seed");
                    text = "seed failed: " + ex.Message;
                }
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    Log(text + " (by " + from.Name + ")");
                    from.SendMessage("MusicShare: " + text);
                });
            });
        }

        /// <summary>The reviewed file's homes -> Songs; all or nothing, and only into an empty database. Returns what it did.</summary>
        public static string SeedFrom(string path)
        {
            if (!File.Exists(path))
                return "no such file: " + path;
            int existing = MusicShareDatabase.CountSongs();
            if (existing != 0)
                return existing < 0 ? "the database couldn't be read" : string.Format("the database already has {0} songs: seeding is for an empty one", existing);
            List<(string Shard, SongRow Row, long LastLogin)>? candidates = MusicShareDatabase.GetCandidates();
            if (candidates == null)
                return "the candidates couldn't be read";

            Dictionary<(string, int), SongRow> byKey = candidates.ToDictionary(c => (c.Shard, c.Row.Hash), c => c.Row);
            List<SongRow> songs = new List<SongRow>();
            List<string> problems = new List<string>();
            using (Microsoft.VisualBasic.FileIO.TextFieldParser csv = new Microsoft.VisualBasic.FileIO.TextFieldParser(path))
            {
                csv.SetDelimiters(",");
                csv.HasFieldsEnclosedInQuotes = true;
                csv.ReadFields();       // the header
                while (!csv.EndOfData)
                {
                    string[]? f = csv.ReadFields();
                    if (f == null || f.Length < 2)
                        continue;
                    if (!int.TryParse(f[0], out int hash))
                    {
                        problems.Add("line " + csv.LineNumber + ": no hash");
                        continue;
                    }
                    string home = f[1].Trim();
                    if (!byKey.TryGetValue((home, hash), out SongRow? row))
                    {
                        problems.Add(string.Format("'{0}' (hash {1}): {2} didn't collect it", f.Length > 2 ? f[2] : "?", hash, home));
                        continue;
                    }
                    row.HomeShard = home;
                    songs.Add(row);
                }
            }
            if (problems.Count > 0)
                return string.Format("nothing seeded; {0} problems: {1}", problems.Count, string.Join("; ", problems.Take(5)));

            int written = 0;
            foreach (IGrouping<string, SongRow> g in songs.GroupBy(s => s.HomeShard))
            {
                int n = MusicShareDatabase.SaveSongs(g, g.Key);
                if (n < 0)
                    return string.Format("seed failed writing {0}'s songs ({1} written before it)", g.Key, written);
                written += n;
            }
            if (!MusicShareDatabase.MarkSeeded())
                return string.Format("seeded {0} songs, but couldn't mark the database seeded: shards won't sync with it", written);
            return string.Format("seeded {0} songs ({1})", written,
                string.Join(", ", songs.GroupBy(s => s.HomeShard).Select(g => g.Key + " " + g.Count())));
        }

        private static string Csv(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
        #endregion Seeding

        #region Origins
        // The shards' lineage, oldest first: a new shard's world (its music library among it) began as a copy of an older
        //  one's, so a song can be in several shards' libraries with the same dates. It was composed on the oldest of them.
        //  Angel Island (2004) -> Siege Perilous (copied about 5/26/23) -> Mortalis (copied between 8/24 and 12/24) ->
        //  Seasons (copied from Mortalis after 3/26) -> Upheaval (10/26). Confirmed by the collected libraries (Candidates)
        //  11/14/26, Adam.
        private static readonly string[] Lineage = { "AngelIsland", "SiegePerilous", "Mortalis", "Seasons", "Upheaval" };

        /// <summary>
        /// Where each song was composed, by the rule: the oldest shard in the lineage whose collected library held it; a
        /// song collected nowhere (submitted since seeding) was composed at its home. Each with the reason.
        /// </summary>
        public static List<(SongRow Row, string Origin, string Why)> ChooseOrigins(List<SongRow> songs, List<(string Shard, SongRow Row, long LastLogin)> candidates)
        {
            Dictionary<int, HashSet<string>> held = new Dictionary<int, HashSet<string>>();
            foreach ((string shard, SongRow row, long _) in candidates)
            {
                if (!held.TryGetValue(row.Hash, out HashSet<string>? shards))
                    held[row.Hash] = shards = new HashSet<string>();
                shards.Add(shard);
            }

            List<(SongRow, string, string)> list = new List<(SongRow, string, string)>();
            foreach (SongRow r in songs.Where(r => !r.Deleted).OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase))
            {
                if (!held.TryGetValue(r.Hash, out HashSet<string>? shards) || shards.Count == 0)
                {
                    list.Add((r, r.HomeShard, "collected nowhere: submitted since seeding, at its home"));
                    continue;
                }
                string heldBy = string.Join(" ", shards.OrderBy(s => Array.IndexOf(Lineage, s) is int i && i >= 0 ? i : int.MaxValue).ThenBy(s => s, StringComparer.Ordinal));
                string? oldest = Lineage.FirstOrDefault(shards.Contains);
                if (oldest != null)
                    list.Add((r, oldest, shards.Count == 1 ? "only " + oldest + " held it" : "the oldest of " + heldBy));
                else
                    list.Add((r, shards.OrderBy(s => s, StringComparer.Ordinal).First(), "no shard in the lineage held it: " + heldBy));
            }
            return list;
        }

        // [MusicShare origins: Logs\origins-review.csv, each song's origin by the rule, for staff to check (and edit)
        private static void OriginsReview(Mobile from)
        {
            Task.Run(() =>
            {
                string text;
                List<SongRow>? songs = MusicShareDatabase.GetSongs();
                List<(string, SongRow, long)>? candidates = MusicShareDatabase.GetCandidates();
                if (songs == null || candidates == null)
                    text = "failed (database error)";
                else
                {
                    string path = Path.Combine(Core.LogsDirectory, "origins-review.csv");
                    List<(SongRow Row, string Origin, string Why)> choices = ChooseOrigins(songs, candidates);
                    List<string> lines = new List<string> { "Hash,Origin,Title,Composer,Status,Submitted,Home,Now,Why" };
                    foreach ((SongRow r, string origin, string why) in choices)
                        lines.Add(string.Join(",", r.Hash.ToString(), Csv(origin), Csv(r.Title), Csv(r.ComposerName),
                            ((MusicLibrary.SubmissionStatus)r.Status).ToString(),
                            r.DateSubmitted > 0 ? new DateTime(r.DateSubmitted, DateTimeKind.Utc).ToString("yyyy-MM-dd") : "",
                            Csv(r.HomeShard), Csv(r.Origin), Csv(why)));
                    File.WriteAllLines(path, lines);
                    text = string.Format("{0} songs ({1}): {2}; check it, then [MusicShare origins apply", choices.Count,
                        string.Join(", ", choices.GroupBy(c => c.Origin).OrderBy(g => Array.IndexOf(Lineage, g.Key)).Select(g => ShardName(g.Key) + " " + g.Count())), path);
                }
                Timer.DelayCall(TimeSpan.Zero, () => from.SendMessage("MusicShare: origins: " + text));
            });
        }

        // [MusicShare origins apply [file]: the reviewed file's origins -> Songs, all or nothing
        private static void OriginsApply(Mobile from, string path)
        {
            Task.Run(() =>
            {
                string text;
                try
                {
                    text = OriginsFrom(path);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "MusicShare.OriginsApply");
                    text = "origins failed: " + ex.Message;
                }
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    Log(text + " (by " + from.Name + ")");
                    from.SendMessage("MusicShare: " + text);
                });
            });
        }

        /// <summary>The reviewed file's origins -> Songs; all or nothing (every line a known song and a shard). Returns what it did.</summary>
        public static string OriginsFrom(string path)
        {
            if (!File.Exists(path))
                return "no such file: " + path;
            List<SongRow>? songs = MusicShareDatabase.GetSongs();
            if (songs == null)
                return "the database couldn't be read";
            HashSet<int> known = new HashSet<int>(songs.Select(s => s.Hash));

            List<(int, string)> origins = new List<(int, string)>();
            List<string> problems = new List<string>();
            using (Microsoft.VisualBasic.FileIO.TextFieldParser csv = new Microsoft.VisualBasic.FileIO.TextFieldParser(path))
            {
                csv.SetDelimiters(",");
                csv.HasFieldsEnclosedInQuotes = true;
                csv.ReadFields();       // the header
                while (!csv.EndOfData)
                {
                    string[]? f = csv.ReadFields();
                    if (f == null || f.Length < 2)
                        continue;
                    if (!int.TryParse(f[0], out int hash) || !known.Contains(hash))
                    {
                        problems.Add(string.Format("'{0}': not a song in the database", f.Length > 2 ? f[2] : f[0]));
                        continue;
                    }
                    string origin = f[1].Trim();
                    if (origin.Length == 0 || origin.Contains(' '))
                    {
                        problems.Add(string.Format("'{0}': '{1}' isn't a shard (as the database names them: SiegePerilous)", f.Length > 2 ? f[2] : f[0], origin));
                        continue;
                    }
                    origins.Add((hash, origin));
                }
            }
            if (problems.Count > 0)
                return string.Format("origins: nothing written; {0} problems: {1}", problems.Count, string.Join("; ", problems.Take(5)));

            int changed = MusicShareDatabase.SetOrigins(origins);
            if (changed < 0)
                return "origins failed (database error): nothing written";
            return string.Format("origins: {0} songs in the file, {1} changed ({2})", origins.Count, changed,
                string.Join(", ", origins.GroupBy(o => o.Item2).Select(g => g.Key + " " + g.Count())));
        }
        #endregion Origins

        #region Release (a shard that starts held: the Music Conservatory)
        /// <summary>A song of this shard's library, as a held shard's dry run sees it.</summary>
        public sealed class DryRunLine
        {
            public string Category = "";    // known, new, pending, rejected, dropped
            public string Flags = "";       // staff-owned, no owner, duplicate title (; between)
            public SongRow Row = null!;
            public string Owner = "";
            public string Home = "";        // a known song's home in the database
        }

        /// <summary>
        /// What this shard's first writes would do, touching nothing: every song in its library, sorted into known (the
        /// database has it: it stays a copy, never written), new (approved, a player's: would be written as this shard's),
        /// pending and rejected (would be written with that status), and dropped (held for a composer elsewhere, but the
        /// database doesn't have it). Suspect test material is flagged: owned by a staff character, no owner, or a title
        /// another song already has under another hash.
        /// </summary>
        public static List<DryRunLine> DryRun(IList<LocalSong> locals, IList<SongRow> rows)
        {
            Dictionary<int, SongRow> byHash = rows.GroupBy(r => r.Hash).ToDictionary(g => g.Key, g => g.First());
            Dictionary<string, HashSet<int>> titles = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in rows.Where(r => !r.Deleted).Select(r => (r.Title, r.Hash)).Concat(locals.Select(l => (l.Row.Title, l.Row.Hash))))
            {
                if (!titles.TryGetValue(t.Title ?? "", out HashSet<int>? set))
                    titles[t.Title ?? ""] = set = new HashSet<int>();
                set.Add(t.Hash);
            }

            List<DryRunLine> lines = new List<DryRunLine>();
            foreach (LocalSong l in locals)
            {
                DryRunLine d = new DryRunLine { Row = l.Row, Owner = l.Info.Owner == null || l.Info.Owner.Deleted ? "" : l.Info.Owner.Name ?? "" };
                if (byHash.TryGetValue(l.Row.Hash, out SongRow? r))
                {
                    d.Category = "known";
                    d.Home = r.HomeShard;
                }
                else if (l.Holder == Holder.System)
                    d.Category = "dropped";
                else
                    d.Category = l.Row.Status == Approved ? "new" : l.Row.Status == PendingStatus ? "pending" : "rejected";

                List<string> flags = new List<string>();
                if (d.Category != "known" && d.Category != "dropped")
                {
                    if (l.Holder == Holder.Gone)
                        flags.Add("no owner");
                    else if (l.Info.Owner != null && l.Info.Owner.AccessLevel > AccessLevel.Player)
                        flags.Add("staff-owned");
                    if (titles.TryGetValue(l.Row.Title ?? "", out HashSet<int>? same) && same.Count > 1)
                        flags.Add("duplicate title");
                }
                d.Flags = string.Join("; ", flags);
                lines.Add(d);
            }
            return lines.OrderBy(d => Array.IndexOf(new[] { "new", "pending", "rejected", "dropped", "known" }, d.Category))
                .ThenBy(d => d.Row.Title, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // [MusicShare sync dryrun: Logs\<shard>-dryrun.csv, and nothing written
        private static void DryRunReport(Mobile from)
        {
            if (!Enabled)
            {
                from.SendMessage("MusicShare: sharing is off here (the login server holds no music).");
                return;
            }
            List<LocalSong> locals = Snapshot();
            string shard = ThisShard;
            int inherited = m_Exported.Count;
            Task.Run(() =>
            {
                string text;
                List<SongRow>? rows = MusicShareDatabase.GetSongs();
                if (rows == null)
                    text = "dry run failed: the shared music database couldn't be read";
                else
                {
                    List<DryRunLine> lines = DryRun(locals, rows);
                    string path = Path.Combine(Core.LogsDirectory, shard + "-dryrun.csv");
                    List<string> csv = new List<string> { "Category,Flags,Hash,Title,Author,Composer,Status,Owner,Home" };
                    foreach (DryRunLine d in lines)
                        csv.Add(string.Join(",", d.Category, Csv(d.Flags), d.Row.Hash.ToString(), Csv(d.Row.Title), Csv(d.Row.Author), Csv(d.Row.ComposerName),
                            ((MusicLibrary.SubmissionStatus)d.Row.Status).ToString(), Csv(d.Owner), Csv(d.Home)));
                    File.WriteAllLines(path, csv);
                    int Count(string c) => lines.Count(d => d.Category == c);
                    // the export list (songs this shard has written): a held shard has written none; one copied from
                    //  another shard's world carries that shard's (harmless: only songs homed here are acted on), and
                    //  the release clears it
                    text = string.Format("dry run ({0}, nothing written): {1} songs: {2} known, {3} new approved, {4} pending, {5} rejected, {6} dropped; {7} suspect; export list {8} (the release clears it): {9}",
                        shard, lines.Count, Count("known"), Count("new"), Count("pending"), Count("rejected"), Count("dropped"),
                        lines.Count(d => d.Flags.Length > 0), inherited, path);
                }
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    m_DryRunDone = rows != null;
                    Log(text);
                    from.SendMessage("MusicShare: " + text);
                });
            });
        }

        // [MusicShare release | hold (Owners, a held shard): release its writes after its dry run (the inherited export
        //  list is cleared), or hold them again
        private static void Release(Mobile from, bool release)
        {
            if (from.AccessLevel < AccessLevel.Owner)
            {
                from.SendMessage("MusicShare: releasing a shard's writes is for Owners.");
                return;
            }
            if (!StartsHeld)
            {
                from.SendMessage("MusicShare: this shard isn't held: only the Music Conservatory starts held.");
                return;
            }
            if (release && !m_DryRunDone)
            {
                from.SendMessage("MusicShare: run [MusicShare sync dryrun first, and review its report.");
                return;
            }
            string shard = ThisShard;
            string by = string.Format("{0} {1:yyyy-MM-dd HH:mm}Z", from.Name, DateTime.UtcNow);
            Task.Run(() =>
            {
                bool ok = MusicShareDatabase.SetReleased(shard, release ? by : null);
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    if (!ok)
                    {
                        from.SendMessage("MusicShare: the shared music database couldn't be written: still held.");
                        return;
                    }
                    if (release)
                    {   // it has written nothing yet: the export list it carries is another shard's (a copied world)
                        m_Exported.Clear();
                        SaveExported(null!);
                    }
                    m_Released = release;
                    string text = release ? string.Format("{0}'s writes released by {1}: its songs are written at the next sync", shard, by)
                        : string.Format("{0} held again by {1}: it reads only", shard, from.Name);
                    Log(text);
                    from.SendMessage("MusicShare: " + text);
                });
            });
        }
        #endregion Release

        #region Flush (development)
        // [MusicShare flush [yes] (Owners, the Music Conservatory on a development machine): before its world is copied to
        //  production, the songs made on it while it was built and tested are deleted. flush lists them; flush yes deletes
        //  them from this library (with their sheet music), marks their database rows deleted (each other shard drops its
        //  copy at its next sync: "deleted at home"), clears the export list, and saves the world (-nosaves keeps nothing
        //  until a save). The database rows are marked, not just the library cleared: a sync would otherwise rebuild an
        //  approved home song the world no longer has.
        private static void Flush(Mobile from, bool yes)
        {
            if (from.AccessLevel < AccessLevel.Owner)
            {
                from.SendMessage("MusicShare: flushing songs is for Owners.");
                return;
            }
            if (!Core.GMNMC_CFG || !Core.Development)
            {
                from.SendMessage("MusicShare: flush is only for the Music Conservatory on a development machine: never production.");
                return;
            }
            if (!Enabled || m_Busy)
            {
                from.SendMessage(m_Busy ? "MusicShare: a sync is running; try again in a moment." : "MusicShare: sharing is off here (the login server holds no music).");
                return;
            }

            string shard = ThisShard;
            List<LocalSong> locals = Snapshot();
            m_Busy = true;
            Task.Run(() =>
            {
                List<SongRow>? rows = MusicShareDatabase.GetSongs();
                List<SongRow> home = rows?.Where(r => r.HomeShard == shard && !r.Deleted).ToList() ?? new List<SongRow>();
                int written = 0;
                if (rows != null && yes && home.Count > 0)
                {
                    foreach (SongRow r in home)
                        r.Deleted = true;
                    written = MusicShareDatabase.SaveSongs(home, shard);
                }

                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    try
                    {
                        if (rows == null)
                        {
                            from.SendMessage("MusicShare: flush: the shared music database couldn't be read: nothing changed.");
                            return;
                        }
                        // this library's songs made here: homed here in the database, or not in it at all (a song made here
                        //  before the release; this world's other songs all came from the database)
                        HashSet<int> homed = new HashSet<int>(home.Select(r => r.Hash));
                        HashSet<int> known = new HashSet<int>(rows.Select(r => r.Hash));
                        List<LocalSong> mine = locals.Where(l => homed.Contains(l.Row.Hash) || !known.Contains(l.Row.Hash)).ToList();

                        if (!yes)
                        {
                            from.SendMessage("MusicShare: flush would delete {0} song{1} made here ({2} in this library, {3} homed here in the database) and clear the export list ({4}):",
                                Math.Max(mine.Count, home.Count), Math.Max(mine.Count, home.Count) == 1 ? "" : "s", mine.Count, home.Count, m_Exported.Count);
                            foreach (string title in mine.Select(l => l.Row.Title).Concat(home.Where(r => !mine.Any(l => l.Row.Hash == r.Hash)).Select(r => r.Title)))
                                from.SendMessage("  '{0}'", title);
                            from.SendMessage("MusicShare: [MusicShare flush yes to delete them (and save the world).");
                            return;
                        }
                        if (written < 0)
                        {
                            from.SendMessage("MusicShare: flush: the shared music database couldn't be written: nothing changed.");
                            return;
                        }

                        List<string> lines = new List<string>();
                        foreach (LocalSong l in mine)
                        {
                            RolledUpSheetMusic rsm = l.Sheet;
                            MusicLibrary.Unqueue(rsm);
                            MusicLibrary.Repository.Remove(rsm);
                            if (!rsm.Deleted)
                                rsm.Delete();
                            lines.Add(string.Format("flushed (development): '{0}' (hash {1})", l.Row.Title, l.Row.Hash));
                        }
                        foreach (SongRow r in home.Where(r => !mine.Any(l => l.Row.Hash == r.Hash)))
                            lines.Add(string.Format("flushed (development): '{0}' (hash {1}), database only", r.Title, r.Hash));
                        m_Exported.Clear();
                        lines.Add(string.Format("flush by {0}: {1} song(s) deleted here and marked deleted in the database; export list cleared", from.Name, lines.Count));
                        Log(lines.ToArray());
                        foreach (string line in lines)
                            from.SendMessage("MusicShare: " + line);

                        World.Save();       // writes the export list too (SaveExported)
                    }
                    finally
                    {
                        m_Busy = false;
                    }
                });
            });
        }
        #endregion Flush (development)

        #region Titles
        public const int RenameAuthorMax = 40;      // as the Author rule (AngelIslandAimsHost.RefuseSubmission)
        public const int RenameNoteMax = 100;       // as a denial's note

        // a library title: the song's name, then its attribution (an artist or composer, Original, Traditional or Unknown)
        //  in brackets at the end, the last bracketed part (a title may have its own: "(Sittin' On) The Dock of the Bay")
        private static readonly System.Text.RegularExpressions.Regex AttributedTitle =
            new System.Text.RegularExpressions.Regex(@"^(?<title>.*\S)\s*\((?<by>[^()]*\S[^()]*)\)$");

        /// <summary>Why this isn't a library title ("Title (Attribution)"), or null if it is.</summary>
        public static string? TitleProblem(string title)
        {
            title = title.Trim();
            if (title.Length == 0)
                return "no title";
            if (!AttributedTitle.IsMatch(title))
                return "doesn't end with its attribution in brackets: Title (Artist), (Original), (Traditional) or (Unknown)";
            return null;
        }

        // [MusicShare titles: Logs\titles-review.csv, every song, for staff to fill in the renames
        private static void TitlesReview(Mobile from)
        {
            Task.Run(() =>
            {
                string text;
                List<SongRow>? songs = MusicShareDatabase.GetSongs();
                if (songs == null)
                    text = "failed (database error)";
                else
                {
                    string path = Path.Combine(Core.LogsDirectory, "titles-review.csv");
                    List<SongRow> live = songs.Where(r => !r.Deleted).OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList();
                    List<string> lines = new List<string> { "Hash,Home,Composer,Status,Author,Title,About,Tags,NewTitle,NewAuthor,NewAbout,NewTags,Note" };
                    foreach (SongRow r in live)
                        lines.Add(string.Join(",", r.Hash.ToString(), Csv(r.HomeShard), Csv(r.ComposerName),
                            ((MusicLibrary.SubmissionStatus)r.Status).ToString(), Csv(r.Author), Csv(r.Title),
                            Csv(RolledUpSheetMusic.NotesLine(r.Composition, "about")), Csv(RolledUpSheetMusic.NotesLine(r.Composition, "tags")),
                            "", "", "", "", ""));
                    File.WriteAllLines(path, lines);
                    text = string.Format("{0} songs, {1} not named Title (Attribution), {2} without tags: {3}; fill in NewTitle (NewAuthor, Note), NewAbout or NewTags ('-' clears one) for each change, then [MusicShare titles apply",
                        live.Count, live.Count(r => TitleProblem(r.Title) != null), live.Count(r => RolledUpSheetMusic.NotesLine(r.Composition, "tags").Length == 0), path);
                }
                Timer.DelayCall(TimeSpan.Zero, () => from.SendMessage("MusicShare: titles: " + text));
            });
        }

        // [MusicShare titles apply [file]: the reviewed file's renames -> Renames, all or nothing; each home applies its own
        private static void TitlesApply(Mobile from, string path)
        {
            string staff = from.Name;
            Task.Run(() =>
            {
                string text;
                try
                {
                    text = TitlesFrom(path, staff);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "MusicShare.TitlesApply");
                    text = "titles failed: " + ex.Message;
                }
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    Log(text + " (by " + staff + ")");
                    from.SendMessage("MusicShare: " + text);
                });
            });
        }

        /// <summary>
        /// The reviewed file's renames -> Renames, all or nothing: every line a song in the database, its Title (and About
        /// and Tags) still the database's (else the file is out of date), a NewTitle that is a library title, a NewAuthor
        /// of 40 characters at most, a Note of 100, a NewAbout of 60 and NewTags of 10 tags (100 characters; '-' clears
        /// either), and no rename of it already waiting. A line with none of NewTitle, NewAuthor, NewAbout and NewTags is no
        /// change. Nothing is renamed here: each song's home applies its renames at its next sync. Returns what it did.
        /// </summary>
        public static string TitlesFrom(string path, string staff)
        {
            if (!File.Exists(path))
                return "no such file: " + path;
            List<SongRow>? songs = MusicShareDatabase.GetSongs();
            List<RenameRow>? waiting = MusicShareDatabase.GetOpenRenames(null);
            if (songs == null || waiting == null)
                return "the database couldn't be read";
            Dictionary<int, SongRow> byHash = songs.Where(s => !s.Deleted).ToDictionary(s => s.Hash);
            HashSet<int> pending = new HashSet<int>(waiting.Select(w => w.SongHash));

            List<RenameRow> renames = new List<RenameRow>();
            List<string> problems = new List<string>();
            HashSet<int> seen = new HashSet<int>();
            using (Microsoft.VisualBasic.FileIO.TextFieldParser csv = new Microsoft.VisualBasic.FileIO.TextFieldParser(path))
            {
                csv.SetDelimiters(",");
                csv.HasFieldsEnclosedInQuotes = true;
                csv.TrimWhiteSpace = false;
                string[]? header = csv.ReadFields();
                int Col(string name) => header == null ? -1 : Array.FindIndex(header, h => h.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
                int cHash = Col("Hash"), cTitle = Col("Title"), cNewTitle = Col("NewTitle"), cNewAuthor = Col("NewAuthor"), cNote = Col("Note");
                int cAbout = Col("About"), cTags = Col("Tags"), cNewAbout = Col("NewAbout"), cNewTags = Col("NewTags");     // a file before 11/16/26 has none
                if (cHash < 0 || cTitle < 0 || cNewTitle < 0)
                    return "titles: nothing recorded; the file needs the columns Hash, Title and NewTitle ([MusicShare titles writes them)";
                while (!csv.EndOfData)
                {
                    string[]? f = csv.ReadFields();
                    if (f == null || f.All(string.IsNullOrWhiteSpace))
                        continue;
                    string Field(int c) => c >= 0 && c < f.Length ? f[c].Trim() : "";
                    string title = Field(cTitle), newTitle = Field(cNewTitle), newAuthor = Field(cNewAuthor), note = Field(cNote);
                    string newAbout = Field(cNewAbout), newTags = Field(cNewTags);
                    if (newTitle.Length == 0 && newAuthor.Length == 0 && newAbout.Length == 0 && newTags.Length == 0)
                        continue;   // no change
                    string what = "'" + (title.Length > 0 ? title : Field(cHash)) + "'";
                    if (!int.TryParse(Field(cHash), out int hash) || !byHash.TryGetValue(hash, out SongRow? song))
                    {
                        problems.Add(what + ": not a song in the database");
                        continue;
                    }
                    if (!seen.Add(hash))
                    {
                        problems.Add(what + ": in the file twice");
                        continue;
                    }
                    if (song.Title != title)
                    {
                        problems.Add(string.Format("{0}: the database calls it '{1}' now (the file is out of date: write it again)", what, song.Title));
                        continue;
                    }
                    if (pending.Contains(hash))
                    {
                        problems.Add(what + ": a rename of it is already waiting for its home to apply");
                        continue;
                    }
                    // About and Tags: the file's (its About and Tags columns) must still be the song's; '-' clears one
                    string about = RolledUpSheetMusic.NotesLine(song.Composition, "about"), tags = RolledUpSheetMusic.NotesLine(song.Composition, "tags");
                    if (cAbout >= 0 && cTags >= 0 && (newAbout.Length > 0 || newTags.Length > 0) && (Field(cAbout) != about || Field(cTags) != tags))
                    {
                        problems.Add(string.Format("{0}: its About or Tags changed since the file was written (the file is out of date: write it again)", what));
                        continue;
                    }
                    string? setAbout = newAbout.Length == 0 ? null : newAbout == "-" ? "" : newAbout;
                    string? setTags = newTags.Length == 0 ? null : newTags == "-" ? "" : RolledUpSheetMusic.TidyTags(newTags);
                    if (RolledUpSheetMusic.DescriptionProblem(setAbout ?? "", setTags ?? "") is string described)
                    {
                        problems.Add(string.Format("{0}: {1}", what, described.Replace("The # About: line", "the About").Replace("The # Tags: line", "the Tags")));
                        continue;
                    }
                    if (setAbout == about)
                        setAbout = null;
                    if (setTags == tags)
                        setTags = null;
                    string finalTitle = newTitle.Length > 0 ? newTitle : song.Title;
                    if (newTitle.Length > 0 && TitleProblem(newTitle) is string why)
                    {
                        problems.Add(string.Format("{0}: '{1}' {2}", what, newTitle, why));
                        continue;
                    }
                    if (newAuthor.Length > RenameAuthorMax)
                    {
                        problems.Add(string.Format("{0}: the author is {1} characters ({2} at most)", what, newAuthor.Length, RenameAuthorMax));
                        continue;
                    }
                    if (note.Length > RenameNoteMax)
                    {
                        problems.Add(string.Format("{0}: the note is {1} characters ({2} at most)", what, note.Length, RenameNoteMax));
                        continue;
                    }
                    if (finalTitle == song.Title && (newAuthor.Length == 0 || newAuthor == song.Author) && setAbout == null && setTags == null)
                        continue;   // already so
                    renames.Add(new RenameRow
                    {
                        SongHash = hash, OldTitle = song.Title, OldAuthor = song.Author, NewTitle = finalTitle,
                        NewAuthor = newAuthor == song.Author ? "" : newAuthor, Note = note, Staff = staff, Utc = DateTime.UtcNow.Ticks,
                        NewAbout = setAbout, NewTags = setTags,
                    });
                }
            }
            if (problems.Count > 0)
                return string.Format("titles: nothing recorded; {0} problems: {1}{2}", problems.Count, string.Join("; ", problems.Take(5)),
                    problems.Count > 5 ? " (and more)" : "");
            if (renames.Count == 0)
                return "titles: no changes in the file";

            if (MusicShareDatabase.AddRenames(renames) < 0)
                return "titles failed (database error): nothing recorded";
            return string.Format("titles: {0} changes recorded; each song's home applies them at its next sync ({1})", renames.Count,
                string.Join(", ", renames.GroupBy(n => byHash[n.SongHash].HomeShard).OrderBy(g => g.Key).Select(g => ShardName(g.Key) + " " + g.Count())));
        }
        #endregion Titles

        #region Command and log
        [Usage("MusicShare status|sync [dryrun]|release|hold|pending|listen <n>|approve <n>|deny <n> [reason] [note]|collect|review|seed [file]|origins [apply [file]]|titles [apply [file]]|export|flush [yes]")]
        [Description("The music shared between shards (always on; not on the login server): its state, a sync now; every shard's songs awaiting review (listen, approve, deny: the song's home applies the decision); first-time seeding: collect, review, seed; origins: where each song was composed (write Logs\\origins-review.csv, check it, then origins apply); sync dryrun / release / hold: a shard that starts held (the Music Conservatory) writes nothing until an Owner reviews its dry run (Logs\\AIMC-dryrun.csv) and releases it; titles: rename library songs and set their About and Tags (write Logs\\titles-review.csv, fill in NewTitle, NewAbout or NewTags, then titles apply: each song's home applies it at its next sync); export: publish the music catalog for the website now (a server started with -publishmusic=<folder>); flush [yes]: the Music Conservatory on a development machine only, before its world is copied to production: list (yes: delete) the songs made on it, everywhere, clear its export list and save.")]
        private static void MusicShare_OnCommand(CommandEventArgs e)
        {
            string arg = e.Arguments.Length > 0 ? e.Arguments[0].ToLowerInvariant() : "status";
            if (arg is "pending" or "listen" or "approve" or "deny" && !Enabled)
            {
                e.Mobile.SendMessage("MusicShare: sharing is off here (the login server holds no music).");
                return;
            }
            switch (arg)
            {
                case "pending":
                    Pending(e.Mobile, ShareCommand);
                    break;
                case "listen":
                    if (Pick(e.Mobile, e.Arguments, ShareCommand) is SongRow listen)
                        Listen(e.Mobile, listen, ShareCommand);
                    break;
                case "approve":
                case "deny":
                    if (e.Mobile.AccessLevel < AccessLevel.Owner)
                    {
                        e.Mobile.SendMessage("MusicShare: approving and denying music is for Owners, as at a music box.");
                        break;
                    }
                    if (Pick(e.Mobile, e.Arguments, ShareCommand) is SongRow decide)
                    {
                        if (arg == "approve")
                            Decide(e.Mobile, decide, true, e.Arguments, ShareCommand);
                        else
                            Deny(e.Mobile, decide, e.Arguments, ShareCommand);
                    }
                    break;
                case "collect":
                    Collect(e.Mobile);
                    break;
                case "review":
                    Review(e.Mobile);
                    break;
                case "seed":
                    {   // the reviewed file: this shard's Logs\seed-review.csv (where review writes it), a file name in
                        //  Logs, or a full path (a long path may not fit a typed command)
                        string file = e.Arguments.Length < 2 ? "seed-review.csv" : string.Join(" ", e.Arguments.Skip(1));
                        Seed(e.Mobile, Path.IsPathRooted(file) ? file : Path.Combine(Core.LogsDirectory, file));
                        break;
                    }
                case "sync":
                    if (e.Arguments.Length > 1 && e.Arguments[1].ToLowerInvariant() == "dryrun")
                        DryRunReport(e.Mobile);
                    else
                        Sync(e.Mobile);
                    break;
                case "release":
                case "hold":
                    Release(e.Mobile, arg == "release");
                    break;
                case "export":
                    MusicCatalog.Publish(e.Mobile);
                    break;
                case "flush":
                    Flush(e.Mobile, e.Arguments.Length > 1 && e.Arguments[1].ToLowerInvariant() == "yes");
                    break;
                case "origins":
                    if (e.Arguments.Length < 2)
                        OriginsReview(e.Mobile);
                    else if (e.Arguments[1].ToLowerInvariant() != "apply")
                        e.Mobile.SendMessage("MusicShare: origins (write Logs\\origins-review.csv) or origins apply [file].");
                    else
                    {   // as seed: Logs\origins-review.csv, a file name in Logs, or a full path
                        string file = e.Arguments.Length < 3 ? "origins-review.csv" : string.Join(" ", e.Arguments.Skip(2));
                        OriginsApply(e.Mobile, Path.IsPathRooted(file) ? file : Path.Combine(Core.LogsDirectory, file));
                    }
                    break;
                case "titles":
                    if (e.Arguments.Length < 2)
                        TitlesReview(e.Mobile);
                    else if (e.Arguments[1].ToLowerInvariant() != "apply")
                        e.Mobile.SendMessage("MusicShare: titles (write Logs\\titles-review.csv) or titles apply [file].");
                    else if (e.Mobile.AccessLevel < AccessLevel.Owner)
                        e.Mobile.SendMessage("MusicShare: renaming music is for Owners, as approving it is.");
                    else
                    {   // as origins: Logs\titles-review.csv, a file name in Logs, or a full path
                        string file = e.Arguments.Length < 3 ? "titles-review.csv" : string.Join(" ", e.Arguments.Skip(2));
                        TitlesApply(e.Mobile, Path.IsPathRooted(file) ? file : Path.Combine(Core.LogsDirectory, file));
                    }
                    break;
                default:
                    int held = MusicLibrary.Repository.Values.Count(i => i != null && i.Owner == AimsHost.Current.SystemMobile);
                    e.Mobile.SendMessage("MusicShare: {0} on {1} ({2}); database {3}; library {4} songs, {5} held for composers elsewhere; {6} written by this shard.",
                        Enabled ? "on" : "off", ThisShard, CanWrite ? "reads and writes" : "reads only", MusicShareDatabase.DatabasePath,
                        MusicLibrary.Repository.Count, held, m_Exported.Count);
                    if (MusicCatalog.Enabled)
                        e.Mobile.SendMessage("MusicShare: catalog to {0}: {1}.", MusicCatalog.Folder, MusicCatalog.LastResult);
                    break;
            }
        }

        private static void Log(params string[] lines) =>
            AimsHost.Current.WriteLog(LogFile, null, lines.Select(l => (object)("[" + ThisShard + "] " + l)));
        #endregion Command and log

        #region Reviewers ([MusicReview)
        /// <summary>The name reviewers are listed by: their account's (one list for every shard and every character).</summary>
        public static string AccountName(Mobile? m) => (m?.Account as Server.Accounting.Account)?.Username ?? "";

        /// <summary>May review music: an Owner, or an account on the reviewer list. Null: the database couldn't be read. Off the game thread.</summary>
        public static bool? IsReviewer(Mobile? m)
        {
            if (m == null || m.Deleted)
                return false;
            if (m.AccessLevel >= AccessLevel.Owner)
                return true;
            return MusicShareDatabase.IsReviewer(AccountName(m));
        }

        // [MusicReview pause: whose review notices are quiet, and until when (UTC). Not saved: a pause is "not right now".
        private static readonly Dictionary<Mobile, DateTime> m_Paused = new Dictionary<Mobile, DateTime>();
        public const int PauseDefaultMinutes = 60, PauseMaxMinutes = 24 * 60;

        /// <summary>Are this character's review notices paused?</summary>
        public static bool IsPaused(Mobile m)
        {
            if (!m_Paused.TryGetValue(m, out DateTime until))
                return false;
            if (until > DateTime.UtcNow)
                return true;
            m_Paused.Remove(m);
            return false;
        }

        /// <summary>Pauses this character's review notices for 'minutes' (at most 24 hours); returns when they resume (UTC).</summary>
        public static DateTime PauseNotices(Mobile m, int minutes)
        {
            DateTime until = DateTime.UtcNow.AddMinutes(Math.Clamp(minutes, 1, PauseMaxMinutes));
            m_Paused[m] = until;
            return until;
        }

        /// <summary>Turns this character's review notices back on (false: they weren't paused).</summary>
        public static bool ResumeNotices(Mobile m) => IsPaused(m) && m_Paused.Remove(m);

        /// <summary>[MusicReview help (an Owner also gets the reviewer list's commands).</summary>
        public static List<string> ReviewHelp(bool owner)
        {
            List<string> lines = new List<string>
            {
                string.Format("MusicReview: songs awaiting review on every shard; a decision applies at the song's home within {0} minutes.", (int)Interval.TotalMinutes),
                "[MusicReview list - the songs awaiting review, numbered (pending works too)",
                string.Format("[MusicReview listen <n> - hear song n (a review copy in your backpack for {0} minutes)", (int)ListenTime.TotalMinutes),
                "[MusicReview approve <n> - approve song n",
                string.Format("[MusicReview deny <n> - asks why: TooLong, TooShort, Inappropriate, Duplicate or Other (Other: a note to the composer, up to {0} characters). Or type it: deny <n> Duplicate, deny <n> Other <note>", NoteMaxLength),
                "[MusicReview pause [minutes] - quiet the review notices for an hour, or that many minutes (resume: hear them again)",
                "Numbers come from your last list. A song already decided shows who decided it until it applies.",
            };
            if (owner)
                lines.Add("Owners: [MusicReview reviewers - the reviewers; add <account>, remove <account>");
            return lines;
        }

        [Usage("MusicReview [list|pending|listen <n>|approve <n>|deny <n> [reason] [note]|pause [minutes]|resume|help|reviewers|add <account>|remove <account>]")]
        [Description("Music reviewers (and Owners): every shard's songs awaiting review, numbered, in any order; hear one; approve or deny it (the song's home applies the decision within 5 minutes). Owners: reviewers, add <account>, remove <account>.")]
        private static void MusicReview_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            string[] args = e.Arguments;
            string arg = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
            if (!Enabled)
            {
                from.SendMessage("MusicReview: music isn't shared on this shard, so there's nothing to review here.");
                return;
            }
            if (arg is "reviewers" or "add" or "remove")
            {
                if (from.AccessLevel < AccessLevel.Owner)
                    from.SendMessage("MusicReview: only Owners manage the reviewers.");
                else
                    ManageReviewers(from, arg, args);
                return;
            }

            // the reviewer list is in the shared database: read it off the game thread, then act on it
            Task.Run(() =>
            {
                bool? reviewer = IsReviewer(from);
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    if (reviewer == null)
                        from.SendMessage("MusicReview: the shared music database couldn't be read.");
                    else if (reviewer == false)
                        from.SendMessage("MusicReview: you aren't a music reviewer.");
                    else
                        Review(from, arg, args);
                });
            });
        }

        private static void Review(Mobile from, string arg, string[] args)
        {
            switch (arg)
            {
                case "list":
                case "pending":
                    Pending(from, ReviewCommand);
                    break;
                case "listen":
                    if (Pick(from, args, ReviewCommand) is SongRow listen)
                        Listen(from, listen, ReviewCommand);
                    break;
                case "approve":
                    if (Pick(from, args, ReviewCommand) is SongRow approve)
                        Decide(from, approve, true, args, ReviewCommand);
                    break;
                case "deny":
                    if (Pick(from, args, ReviewCommand) is SongRow deny)
                        Deny(from, deny, args, ReviewCommand);
                    break;
                case "pause":
                    {
                        int minutes = PauseDefaultMinutes;
                        if (args.Length > 1 && (!int.TryParse(args[1], out minutes) || minutes < 1))
                        {
                            from.SendMessage("MusicReview: pause for how many minutes? [MusicReview pause [minutes] (no number: {0}; at most {1}).", PauseDefaultMinutes, PauseMaxMinutes);
                            break;
                        }
                        DateTime until = PauseNotices(from, minutes);
                        from.SendMessage("MusicReview: review notices paused{0} until {1:h:mm tt} shard time ([MusicReview resume to hear them again).",
                            minutes > PauseMaxMinutes ? " for the most there is, 24 hours," : "", ShardClock.ToShard(until));
                        break;
                    }
                case "resume":
                    from.SendMessage(ResumeNotices(from) ? "MusicReview: review notices are on again." : "MusicReview: review notices weren't paused.");
                    break;
                default:
                    foreach (string line in ReviewHelp(from.AccessLevel >= AccessLevel.Owner))
                        from.SendMessage(line);
                    break;
            }
        }

        // Owners: reviewers | add <account> | remove <account>
        private static void ManageReviewers(Mobile from, string arg, string[] args)
        {
            if (arg == "reviewers")
            {
                Task.Run(() =>
                {
                    List<ReviewerRow>? list = MusicShareDatabase.GetReviewers();
                    Timer.DelayCall(TimeSpan.Zero, () =>
                    {
                        if (list == null)
                            from.SendMessage("MusicReview: the shared music database couldn't be read.");
                        else if (list.Count == 0)
                            from.SendMessage("MusicReview: no reviewers yet (Owners can always review).");
                        else
                            from.SendMessage("MusicReview: {0} reviewer{1}: {2}", list.Count, list.Count == 1 ? "" : "s", string.Join(", ",
                                list.Select(r => string.Format("{0} (added by {1}, {2:yyyy-MM-dd})", r.Account, r.AddedBy, new DateTime(r.Utc, DateTimeKind.Utc)))));
                    });
                });
                return;
            }

            if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                from.SendMessage("MusicReview: which account? [MusicReview {0} <account>", arg);
                return;
            }
            // the account as this shard knows it (its own spelling); one this shard doesn't have may be on another
            Server.Accounting.Account? known = Server.Accounting.Accounts.GetAccount(args[1]);
            string account = known?.Username ?? args[1];
            string by = from.Name ?? "staff";
            bool add = arg == "add";
            Task.Run(() =>
            {
                int done = add ? MusicShareDatabase.AddReviewer(account, by) : MusicShareDatabase.RemoveReviewer(account);
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    if (done < 0)
                        from.SendMessage("MusicReview: the shared music database couldn't be changed.");
                    else if (done == 0)
                        from.SendMessage(add ? "MusicReview: {0} is already a reviewer." : "MusicReview: {0} isn't a reviewer.", account);
                    else
                    {
                        from.SendMessage(add ? "MusicReview: {0} can now review music, on every shard ([MusicReview help)." : "MusicReview: {0} is no longer a reviewer.", account);
                        Log(string.Format("reviewer {0}: {1} (by {2})", add ? "added" : "removed", account, by));
                    }
                    if (known == null)
                        from.SendMessage("MusicReview: (this shard has no account named {0}: check the spelling; it may be on another shard.)", account);
                });
            });
        }
        #endregion Reviewers ([MusicReview)
    }
}
