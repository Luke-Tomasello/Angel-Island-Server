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

/* Scripts\Engines\AIMusicShare\MusicShareDatabase.cs
 * ChangeLog
 *  11/18/26, Adam (one database folder)
 *      music.db lives in the one database folder (Server\DatabaseFolder.cs: AI.DBDIR, else <shard>\Databases); AI.MUSICDB
 *      is retired, and sharing is always on (-sharemusic is retired).
 *  11/16/26, Adam (-uomc)
 *      IsReleased / SetReleased: a held shard's writes (the Music Conservatory) are released by staff, the mark kept in
 *      Meta ("released:AIMC"); unreadable is reported as null, which the caller treats as held.
 *  11/16/26, Adam (About and Tags)
 *      A rename may set a song's About and Tags too (Renames.newAbout, newTags; NULL: as they are, '': none), added to an
 *      existing database when it's opened.
 *  11/16/26, Adam (song renames)
 *      Renames: staff renames of library songs ([MusicShare titles apply), recorded with the names before and after, for
 *      each song's home to apply at its next sync (MusicLibrary.Rename), as review decisions are (table Renames, created
 *      in an existing database when it's opened). GetRenameHistory: a song's names before its applied renames, for a
 *      home song rebuilt from the database.
 *  11/14/26, Adam (a synced decision is final)
 *      GetDenialNotes: the note of a song's latest applied denial, so a home world that lost the denial to a crash or
 *      rollback can restore it (MusicShare: ActKind.Adopt).
 *  11/14/26, Adam (composed on)
 *      Songs know the shard they were composed on (column origin, added to an existing database when it's opened; ""
 *      until set): a new song's is the shard that first writes it (its home, where it was submitted), and no sync
 *      changes it after. SetOrigins sets it for songs already there ([MusicShare origins apply). Home is untouched: it
 *      still decides a song's status and pays its composer; origin is for people (the website's music catalog).
 *  11/9/26, Adam (music reviewers)
 *      Reviewers: the accounts that may review music ([MusicReview), one list for every shard (table Reviewers, added
 *      to an existing database when it's opened). RecordDecision: a decision is recorded only if the song is still
 *      awaiting review and has no decision waiting to be applied (one transaction, so two reviewers can't both decide
 *      the same song); GetWaitingDecisions: those waiting, for the pending list.
 *  11/8/26, Adam (rejection note)
 *      Decisions carry staff's note to the composer (column note, added to an existing database when it's opened), so
 *      a denial made on another shard reaches the composer at home with its note.
 *  11/7/26, Adam
 *      Created. The shared music database of the Angel Island shards on one machine (MusicShare.cs uses it): the songs
 *      themselves (each shard builds its own library copies from them) and the sales of songs on shards other than
 *      their home. SQLite at AI.MUSICDB (default C:\AIDB\music.db), created on first use, as the accounts database is;
 *      WAL and a busy timeout, since every shard on the machine reads and writes it. Every call is safe to run off the
 *      game thread and returns what it could do (exceptions are logged, never thrown).
 *      Seeding: the Candidates staging table ([MusicShare collect), and the 'seeded' mark in Meta that seeding sets: no
 *      shard syncs with a database without it.
 *      Decisions: staff review decisions on pending songs, made on any shard, applied by each song's home.
 */

using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Server.Engines.MusicShare
{
    /// <summary>A song in the shared database: everything a shard needs to build its own library copy.</summary>
    public sealed class SongRow
    {
        public long Id;
        public int Hash;                    // the library key (the composition's hash): the same on every shard
        public string Composition = "";
        public string Title = "";
        public string Author = "";          // the song's Author line, as the composer wrote it
        public string Instrument = "harp";  // harp, lapharp or lute
        public bool NewTimer, Prefetch, ConcertMode, Napster;
        public int Tempo, Price, Status, Reason;
        public uint ComposerGuid;           // the composer's account (the same on every shard)
        public string ComposerName = "";
        public string HomeShard = "";       // decides the song's status, and is the only shard that pays its composer
        public string Origin = "";          // the shard it was composed on ("": not known); never changed by a sync
        public long DateSubmitted, DateApproved, DateRemastered;   // UTC ticks
        public bool Deleted;
        public long UpdatedUtc;
    }

    /// <summary>A purchase, on one shard, of a song whose home is another (recognition for its composer at home).</summary>
    public sealed class SaleRow
    {
        public long Id;
        public int SongHash;
        public string SoldOnShard = "";
        public string BuyerName = "";
        public uint BuyerGuid;
        public int Price;
        public long Utc;
    }

    /// <summary>A staff review decision on a pending song, made on any shard, for the song's home to apply.</summary>
    public sealed class DecisionRow
    {
        public long Id;
        public int SongHash;
        public bool Approve;
        public int Reason;                  // MusicLibrary.RejectionReason, for a denial
        public string Note = "";            // staff's note to the composer, for a denial (MusicInfo.Note)
        public string Staff = "";
        public string OnShard = "";
        public long Utc;
    }

    /// <summary>A staff rename of a library song ([MusicShare titles apply), for the song's home to apply.</summary>
    public sealed class RenameRow
    {
        public long Id;
        public int SongHash;
        public string OldTitle = "", OldAuthor = "";
        public string NewTitle = "", NewAuthor = "";    // NewAuthor "": the author stays
        public string? NewAbout, NewTags;               // null: as they are; "": none (the song's # About: / # Tags: lines)
        public string Note = "";            // staff's note to the composer
        public string Staff = "";
        public long Utc;
    }

    /// <summary>An account that may review music on every shard ([MusicReview).</summary>
    public sealed class ReviewerRow
    {
        public string Account = "";
        public string AddedBy = "";
        public long Utc;
    }

    /// <summary>What became of a review decision (MusicShareDatabase.RecordDecision).</summary>
    public enum DecisionOutcome
    {
        Recorded,
        AlreadyDecided,                     // the song has a decision waiting to be applied at its home
        NotPending,                         // the song isn't awaiting review any more (decided, deleted, or gone)
        Failed,                             // a database error (logged)
    }

    public static class MusicShareDatabase
    {
        private const int SchemaVersion = 1;
        private static readonly object m_SchemaLock = new object();
        private static string? m_SchemaReadyFor;

        /// <summary>For tests: a database file other than AI.MUSICDB / the default.</summary>
        public static string? PathOverride { get; set; }

        public static string DatabasePath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PathOverride))
                    return PathOverride!;
                // the one database folder (AI.DBDIR, else <shard>\Databases): AI.MUSICDB is retired
                return DatabaseFolder.PathOf("music.db");
            }
        }

        private static SQLiteConnection Open()
        {
            string path = DatabasePath;
            if (!File.Exists(path))
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                SQLiteConnection.CreateFile(path);
            }

            SQLiteConnection conn = new SQLiteConnection(string.Format(
                "Data Source={0};Pooling=True;Max Pool Size=100;Journal Mode=WAL;BusyTimeout=5000;", path));
            conn.Open();
            EnsureSchema(conn, path);
            return conn;
        }

        private static void EnsureSchema(SQLiteConnection conn, string path)
        {
            lock (m_SchemaLock)
            {
                if (m_SchemaReadyFor == path)
                    return;

                Execute(conn, null, @"
CREATE TABLE IF NOT EXISTS Meta (key TEXT PRIMARY KEY, value TEXT);
CREATE TABLE IF NOT EXISTS Songs (
    id INTEGER PRIMARY KEY AUTOINCREMENT, hash INTEGER NOT NULL UNIQUE, composition TEXT NOT NULL, title TEXT NOT NULL,
    author TEXT NOT NULL, instrument TEXT NOT NULL, newTimer INTEGER NOT NULL, prefetch INTEGER NOT NULL,
    concertMode INTEGER NOT NULL, napster INTEGER NOT NULL, tempo INTEGER NOT NULL, price INTEGER NOT NULL,
    status INTEGER NOT NULL, reason INTEGER NOT NULL, composerGuid INTEGER NOT NULL, composerName TEXT NOT NULL,
    homeShard TEXT NOT NULL, dateSubmitted INTEGER NOT NULL, dateApproved INTEGER NOT NULL, dateRemastered INTEGER NOT NULL,
    deleted INTEGER NOT NULL, updatedUtc INTEGER NOT NULL, origin TEXT NOT NULL DEFAULT '');
CREATE TABLE IF NOT EXISTS Sales (
    id INTEGER PRIMARY KEY AUTOINCREMENT, songHash INTEGER NOT NULL, soldOnShard TEXT NOT NULL, buyerName TEXT NOT NULL,
    buyerGuid INTEGER NOT NULL, price INTEGER NOT NULL, utc INTEGER NOT NULL, recognized INTEGER NOT NULL);
CREATE INDEX IF NOT EXISTS SalesOpen ON Sales (recognized, songHash);
CREATE TABLE IF NOT EXISTS Renames (
    id INTEGER PRIMARY KEY AUTOINCREMENT, songHash INTEGER NOT NULL, oldTitle TEXT NOT NULL, oldAuthor TEXT NOT NULL,
    newTitle TEXT NOT NULL, newAuthor TEXT NOT NULL, note TEXT NOT NULL, staff TEXT NOT NULL, utc INTEGER NOT NULL,
    applied INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS Decisions (
    id INTEGER PRIMARY KEY AUTOINCREMENT, songHash INTEGER NOT NULL, approve INTEGER NOT NULL, reason INTEGER NOT NULL,
    staff TEXT NOT NULL, onShard TEXT NOT NULL, utc INTEGER NOT NULL, applied INTEGER NOT NULL,
    note TEXT NOT NULL DEFAULT '');
CREATE TABLE IF NOT EXISTS Candidates (
    shard TEXT NOT NULL, hash INTEGER NOT NULL, composition TEXT NOT NULL, title TEXT NOT NULL, author TEXT NOT NULL,
    instrument TEXT NOT NULL, newTimer INTEGER NOT NULL, prefetch INTEGER NOT NULL, concertMode INTEGER NOT NULL,
    napster INTEGER NOT NULL, tempo INTEGER NOT NULL, price INTEGER NOT NULL, status INTEGER NOT NULL, reason INTEGER NOT NULL,
    composerGuid INTEGER NOT NULL, composerName TEXT NOT NULL, dateSubmitted INTEGER NOT NULL, dateApproved INTEGER NOT NULL,
    dateRemastered INTEGER NOT NULL, lastLogin INTEGER NOT NULL, collectedUtc INTEGER NOT NULL, PRIMARY KEY (shard, hash));
CREATE TABLE IF NOT EXISTS Reviewers (account TEXT PRIMARY KEY COLLATE NOCASE, addedBy TEXT NOT NULL, utc INTEGER NOT NULL);
INSERT OR IGNORE INTO Meta (key, value) VALUES ('schema', '" + SchemaVersion + "');");

                // a database made before decisions carried a note (11/8/26): add the column
                if (!HasColumn(conn, "Decisions", "note"))
                    Execute(conn, null, "ALTER TABLE Decisions ADD COLUMN note TEXT NOT NULL DEFAULT ''");
                // a database made before songs knew where they were composed (11/14/26): add the column ('': not known
                //  until [MusicShare origins apply)
                if (!HasColumn(conn, "Songs", "origin"))
                    Execute(conn, null, "ALTER TABLE Songs ADD COLUMN origin TEXT NOT NULL DEFAULT ''");
                // a database made before renames could set a song's About and Tags (11/16/26): add the columns (NULL: as
                //  they are)
                if (!HasColumn(conn, "Renames", "newAbout"))
                    Execute(conn, null, "ALTER TABLE Renames ADD COLUMN newAbout TEXT");
                if (!HasColumn(conn, "Renames", "newTags"))
                    Execute(conn, null, "ALTER TABLE Renames ADD COLUMN newTags TEXT");
                m_SchemaReadyFor = path;
            }
        }

        private static void Execute(SQLiteConnection conn, SQLiteTransaction? tx, string sql)
        {
            using SQLiteCommand cmd = new SQLiteCommand(sql, conn, tx);
            cmd.ExecuteNonQuery();
        }

        private static bool HasColumn(SQLiteConnection conn, string table, string column)
        {
            using SQLiteCommand cmd = new SQLiteCommand("PRAGMA table_info(" + table + ")", conn);
            using SQLiteDataReader r = cmd.ExecuteReader();
            while (r.Read())
                if (string.Equals(Convert.ToString(r["name"]), column, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>Every song row (null: the database couldn't be read).</summary>
        public static List<SongRow>? GetSongs()
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Songs", conn);
                using SQLiteDataReader r = cmd.ExecuteReader();
                List<SongRow> list = new List<SongRow>();
                while (r.Read())
                {
                    list.Add(new SongRow
                    {
                        Id = Convert.ToInt64(r["id"]),
                        Hash = Convert.ToInt32(r["hash"]),
                        Composition = Convert.ToString(r["composition"]) ?? "",
                        Title = Convert.ToString(r["title"]) ?? "",
                        Author = Convert.ToString(r["author"]) ?? "",
                        Instrument = Convert.ToString(r["instrument"]) ?? "harp",
                        NewTimer = Convert.ToInt64(r["newTimer"]) != 0,
                        Prefetch = Convert.ToInt64(r["prefetch"]) != 0,
                        ConcertMode = Convert.ToInt64(r["concertMode"]) != 0,
                        Napster = Convert.ToInt64(r["napster"]) != 0,
                        Tempo = Convert.ToInt32(r["tempo"]),
                        Price = Convert.ToInt32(r["price"]),
                        Status = Convert.ToInt32(r["status"]),
                        Reason = Convert.ToInt32(r["reason"]),
                        ComposerGuid = (uint)Convert.ToInt64(r["composerGuid"]),
                        ComposerName = Convert.ToString(r["composerName"]) ?? "",
                        HomeShard = Convert.ToString(r["homeShard"]) ?? "",
                        Origin = Convert.ToString(r["origin"]) ?? "",
                        DateSubmitted = Convert.ToInt64(r["dateSubmitted"]),
                        DateApproved = Convert.ToInt64(r["dateApproved"]),
                        DateRemastered = Convert.ToInt64(r["dateRemastered"]),
                        Deleted = Convert.ToInt64(r["deleted"]) != 0,
                        UpdatedUtc = Convert.ToInt64(r["updatedUtc"]),
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.GetSongs");
                return null;
            }
        }

        /// <summary>
        /// Writes these rows (by hash: a new song, or an update of one), in one transaction. A row whose hash is already the
        /// song of another home shard is not changed (only its home writes it). A new song's origin is its own (else its
        /// home: where it was submitted); an update never changes it. Returns the number written, -1 on failure.
        /// </summary>
        public static int SaveSongs(IEnumerable<SongRow> rows, string writerShard)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                int written = 0;
                foreach (SongRow s in rows)
                {
                    using SQLiteCommand cmd = new SQLiteCommand(@"
INSERT INTO Songs (hash, composition, title, author, instrument, newTimer, prefetch, concertMode, napster, tempo, price,
    status, reason, composerGuid, composerName, homeShard, dateSubmitted, dateApproved, dateRemastered, deleted, updatedUtc, origin)
VALUES (@hash, @composition, @title, @author, @instrument, @newTimer, @prefetch, @concertMode, @napster, @tempo, @price,
    @status, @reason, @composerGuid, @composerName, @homeShard, @dateSubmitted, @dateApproved, @dateRemastered, @deleted, @updatedUtc,
    @origin)
ON CONFLICT(hash) DO UPDATE SET composition = excluded.composition, title = excluded.title, author = excluded.author,
    instrument = excluded.instrument, newTimer = excluded.newTimer, prefetch = excluded.prefetch,
    concertMode = excluded.concertMode, napster = excluded.napster, tempo = excluded.tempo, price = excluded.price,
    status = excluded.status, reason = excluded.reason, composerGuid = excluded.composerGuid,
    composerName = excluded.composerName, dateSubmitted = excluded.dateSubmitted, dateApproved = excluded.dateApproved,
    dateRemastered = excluded.dateRemastered, deleted = excluded.deleted, updatedUtc = excluded.updatedUtc
WHERE Songs.homeShard = @writer", conn, tx);
                    cmd.Parameters.AddWithValue("@hash", s.Hash);
                    cmd.Parameters.AddWithValue("@composition", s.Composition);
                    cmd.Parameters.AddWithValue("@title", s.Title);
                    cmd.Parameters.AddWithValue("@author", s.Author);
                    cmd.Parameters.AddWithValue("@instrument", s.Instrument);
                    cmd.Parameters.AddWithValue("@newTimer", s.NewTimer ? 1 : 0);
                    cmd.Parameters.AddWithValue("@prefetch", s.Prefetch ? 1 : 0);
                    cmd.Parameters.AddWithValue("@concertMode", s.ConcertMode ? 1 : 0);
                    cmd.Parameters.AddWithValue("@napster", s.Napster ? 1 : 0);
                    cmd.Parameters.AddWithValue("@tempo", s.Tempo);
                    cmd.Parameters.AddWithValue("@price", s.Price);
                    cmd.Parameters.AddWithValue("@status", s.Status);
                    cmd.Parameters.AddWithValue("@reason", s.Reason);
                    cmd.Parameters.AddWithValue("@composerGuid", (long)s.ComposerGuid);
                    cmd.Parameters.AddWithValue("@composerName", s.ComposerName);
                    cmd.Parameters.AddWithValue("@homeShard", s.HomeShard);
                    cmd.Parameters.AddWithValue("@dateSubmitted", s.DateSubmitted);
                    cmd.Parameters.AddWithValue("@dateApproved", s.DateApproved);
                    cmd.Parameters.AddWithValue("@dateRemastered", s.DateRemastered);
                    cmd.Parameters.AddWithValue("@deleted", s.Deleted ? 1 : 0);
                    cmd.Parameters.AddWithValue("@updatedUtc", DateTime.UtcNow.Ticks);
                    cmd.Parameters.AddWithValue("@origin", s.Origin.Length > 0 ? s.Origin : s.HomeShard);
                    cmd.Parameters.AddWithValue("@writer", writerShard);
                    written += cmd.ExecuteNonQuery();
                }
                tx.Commit();
                return written;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.SaveSongs");
                return -1;
            }
        }

        /// <summary>
        /// Sets where these songs were composed ([MusicShare origins apply: once, from the reviewed file), in one transaction.
        /// Returns the number of songs changed, -1 on failure.
        /// </summary>
        public static int SetOrigins(IEnumerable<(int Hash, string Origin)> origins)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                int changed = 0;
                foreach ((int hash, string origin) in origins)
                {
                    using SQLiteCommand cmd = new SQLiteCommand("UPDATE Songs SET origin = @origin WHERE hash = @hash AND origin <> @origin", conn, tx);
                    cmd.Parameters.AddWithValue("@origin", origin);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    changed += cmd.ExecuteNonQuery();
                }
                tx.Commit();
                return changed;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.SetOrigins");
                return -1;
            }
        }

        /// <summary>Records a sale of a song on a shard other than its home. False on failure.</summary>
        public static bool AddSale(SaleRow sale)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand(@"
INSERT INTO Sales (songHash, soldOnShard, buyerName, buyerGuid, price, utc, recognized)
VALUES (@songHash, @soldOnShard, @buyerName, @buyerGuid, @price, @utc, 0)", conn);
                cmd.Parameters.AddWithValue("@songHash", sale.SongHash);
                cmd.Parameters.AddWithValue("@soldOnShard", sale.SoldOnShard);
                cmd.Parameters.AddWithValue("@buyerName", sale.BuyerName);
                cmd.Parameters.AddWithValue("@buyerGuid", (long)sale.BuyerGuid);
                cmd.Parameters.AddWithValue("@price", sale.Price);
                cmd.Parameters.AddWithValue("@utc", sale.Utc);
                return cmd.ExecuteNonQuery() == 1;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.AddSale");
                return false;
            }
        }

        /// <summary>Sales not yet recognized of songs whose home is this shard (null on failure).</summary>
        public static List<SaleRow>? GetOpenSales(string homeShard)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand(@"
SELECT Sales.* FROM Sales JOIN Songs ON Songs.hash = Sales.songHash
WHERE Sales.recognized = 0 AND Songs.homeShard = @home ORDER BY Sales.id", conn);
                cmd.Parameters.AddWithValue("@home", homeShard);
                using SQLiteDataReader r = cmd.ExecuteReader();
                List<SaleRow> list = new List<SaleRow>();
                while (r.Read())
                    list.Add(new SaleRow
                    {
                        Id = Convert.ToInt64(r["id"]),
                        SongHash = Convert.ToInt32(r["songHash"]),
                        SoldOnShard = Convert.ToString(r["soldOnShard"]) ?? "",
                        BuyerName = Convert.ToString(r["buyerName"]) ?? "",
                        BuyerGuid = (uint)Convert.ToInt64(r["buyerGuid"]),
                        Price = Convert.ToInt32(r["price"]),
                        Utc = Convert.ToInt64(r["utc"]),
                    });
                return list;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.GetOpenSales");
                return null;
            }
        }

        #region Review decisions
        /// <summary>Records a review decision for the song's home to apply. False on failure.</summary>
        public static bool AddDecision(DecisionRow d)
        {
            try
            {
                using SQLiteConnection conn = Open();
                return InsertDecision(conn, null, d);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.AddDecision");
                return false;
            }
        }

        /// <summary>
        /// Records a review decision only if the song is still awaiting review and has no decision waiting to be applied
        /// ('waiting': that decision). One transaction (System.Data.SQLite begins it IMMEDIATE: one writer at a time, on
        /// every shard), so two reviewers deciding the same song at once can't both record.
        /// </summary>
        public static DecisionOutcome RecordDecision(DecisionRow d, out DecisionRow? waiting)
        {
            waiting = null;
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                using (SQLiteCommand q = new SQLiteCommand("SELECT * FROM Decisions WHERE songHash = @hash AND applied = 0 ORDER BY id LIMIT 1", conn, tx))
                {
                    q.Parameters.AddWithValue("@hash", d.SongHash);
                    using SQLiteDataReader r = q.ExecuteReader();
                    if (r.Read())
                        waiting = ReadDecision(r);
                }
                if (waiting != null)
                    return DecisionOutcome.AlreadyDecided;

                using (SQLiteCommand q = new SQLiteCommand("SELECT COUNT(*) FROM Songs WHERE hash = @hash AND status = @pending AND deleted = 0", conn, tx))
                {
                    q.Parameters.AddWithValue("@hash", d.SongHash);
                    q.Parameters.AddWithValue("@pending", (int)Server.Items.MusicLibrary.SubmissionStatus.Pending);
                    if (Convert.ToInt64(q.ExecuteScalar()) == 0)
                        return DecisionOutcome.NotPending;
                }

                if (!InsertDecision(conn, tx, d))
                    return DecisionOutcome.Failed;
                tx.Commit();
                return DecisionOutcome.Recorded;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.RecordDecision");
                return DecisionOutcome.Failed;
            }
        }

        /// <summary>Decisions recorded but not yet applied, the first for each song (null on failure): the pending list shows them.</summary>
        public static Dictionary<int, DecisionRow>? GetWaitingDecisions()
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Decisions WHERE applied = 0 ORDER BY id", conn);
                using SQLiteDataReader r = cmd.ExecuteReader();
                Dictionary<int, DecisionRow> waiting = new Dictionary<int, DecisionRow>();
                while (r.Read())
                {
                    DecisionRow d = ReadDecision(r);
                    if (!waiting.ContainsKey(d.SongHash))
                        waiting[d.SongHash] = d;
                }
                return waiting;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.GetWaitingDecisions");
                return null;
            }
        }

        private static bool InsertDecision(SQLiteConnection conn, SQLiteTransaction? tx, DecisionRow d)
        {
            using SQLiteCommand cmd = new SQLiteCommand(@"
INSERT INTO Decisions (songHash, approve, reason, note, staff, onShard, utc, applied)
VALUES (@songHash, @approve, @reason, @note, @staff, @onShard, @utc, 0)", conn, tx);
            cmd.Parameters.AddWithValue("@songHash", d.SongHash);
            cmd.Parameters.AddWithValue("@approve", d.Approve ? 1 : 0);
            cmd.Parameters.AddWithValue("@reason", d.Reason);
            cmd.Parameters.AddWithValue("@note", d.Note ?? "");
            cmd.Parameters.AddWithValue("@staff", d.Staff);
            cmd.Parameters.AddWithValue("@onShard", d.OnShard);
            cmd.Parameters.AddWithValue("@utc", d.Utc);
            return cmd.ExecuteNonQuery() == 1;
        }

        private static DecisionRow ReadDecision(SQLiteDataReader r) => new DecisionRow
        {
            Id = Convert.ToInt64(r["id"]),
            SongHash = Convert.ToInt32(r["songHash"]),
            Approve = Convert.ToInt64(r["approve"]) != 0,
            Reason = Convert.ToInt32(r["reason"]),
            Note = Convert.ToString(r["note"]) ?? "",
            Staff = Convert.ToString(r["staff"]) ?? "",
            OnShard = Convert.ToString(r["onShard"]) ?? "",
            Utc = Convert.ToInt64(r["utc"]),
        };

        /// <summary>Decisions not yet applied on songs whose home is this shard, oldest first (null on failure).</summary>
        public static List<DecisionRow>? GetOpenDecisions(string homeShard)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand(@"
SELECT Decisions.* FROM Decisions JOIN Songs ON Songs.hash = Decisions.songHash
WHERE Decisions.applied = 0 AND Songs.homeShard = @home ORDER BY Decisions.id", conn);
                cmd.Parameters.AddWithValue("@home", homeShard);
                using SQLiteDataReader r = cmd.ExecuteReader();
                List<DecisionRow> list = new List<DecisionRow>();
                while (r.Read())
                    list.Add(ReadDecision(r));
                return list;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.GetOpenDecisions");
                return null;
            }
        }

        /// <summary>
        /// The note of the latest applied denial of each of these songs (a song without one isn't in it; null on failure):
        /// for a home world that lost the denial (a crash or rollback) to tell its composer again.
        /// </summary>
        public static Dictionary<int, string>? GetDenialNotes(IEnumerable<int> hashes)
        {
            try
            {
                using SQLiteConnection conn = Open();
                Dictionary<int, string> notes = new Dictionary<int, string>();
                foreach (int hash in hashes)
                {
                    using SQLiteCommand cmd = new SQLiteCommand(@"
SELECT note FROM Decisions WHERE songHash = @hash AND applied = 1 AND approve = 0 ORDER BY id DESC LIMIT 1", conn);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    if (cmd.ExecuteScalar() is string note && note.Length > 0)
                        notes[hash] = note;
                }
                return notes;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.GetDenialNotes");
                return null;
            }
        }

        /// <summary>Marks these decisions applied (or set aside: the song was no longer pending). False on failure.</summary>
        public static bool MarkDecisionsApplied(IEnumerable<long> ids)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                foreach (long id in ids)
                {
                    using SQLiteCommand cmd = new SQLiteCommand("UPDATE Decisions SET applied = 1 WHERE id = @id", conn, tx);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.MarkDecisionsApplied");
                return false;
            }
        }
        #endregion Review decisions

        #region Renames
        /// <summary>Records these renames, all or none (one transaction). The number recorded; -1 on failure.</summary>
        public static int AddRenames(IList<RenameRow> renames)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                foreach (RenameRow n in renames)
                {
                    using SQLiteCommand cmd = new SQLiteCommand(@"
INSERT INTO Renames (songHash, oldTitle, oldAuthor, newTitle, newAuthor, note, staff, utc, applied, newAbout, newTags)
VALUES (@hash, @oldTitle, @oldAuthor, @newTitle, @newAuthor, @note, @staff, @utc, 0, @newAbout, @newTags)", conn, tx);
                    cmd.Parameters.AddWithValue("@hash", n.SongHash);
                    cmd.Parameters.AddWithValue("@oldTitle", n.OldTitle);
                    cmd.Parameters.AddWithValue("@oldAuthor", n.OldAuthor);
                    cmd.Parameters.AddWithValue("@newTitle", n.NewTitle);
                    cmd.Parameters.AddWithValue("@newAuthor", n.NewAuthor);
                    cmd.Parameters.AddWithValue("@note", n.Note);
                    cmd.Parameters.AddWithValue("@staff", n.Staff);
                    cmd.Parameters.AddWithValue("@utc", n.Utc);
                    cmd.Parameters.AddWithValue("@newAbout", (object?)n.NewAbout ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@newTags", (object?)n.NewTags ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
                return renames.Count;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.AddRenames");
                return -1;
            }
        }

        private static RenameRow ReadRename(SQLiteDataReader r) => new RenameRow
        {
            Id = Convert.ToInt64(r["id"]),
            SongHash = Convert.ToInt32(r["songHash"]),
            OldTitle = Convert.ToString(r["oldTitle"]) ?? "",
            OldAuthor = Convert.ToString(r["oldAuthor"]) ?? "",
            NewTitle = Convert.ToString(r["newTitle"]) ?? "",
            NewAuthor = Convert.ToString(r["newAuthor"]) ?? "",
            Note = Convert.ToString(r["note"]) ?? "",
            Staff = Convert.ToString(r["staff"]) ?? "",
            Utc = Convert.ToInt64(r["utc"]),
            NewAbout = r["newAbout"] is DBNull ? null : Convert.ToString(r["newAbout"]),
            NewTags = r["newTags"] is DBNull ? null : Convert.ToString(r["newTags"]),
        };

        /// <summary>Renames not yet applied: of songs whose home is this shard, or (homeShard null) every one; oldest first. Null on failure.</summary>
        public static List<RenameRow>? GetOpenRenames(string? homeShard)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand(homeShard == null
                    ? "SELECT * FROM Renames WHERE applied = 0 ORDER BY id"
                    : @"SELECT Renames.* FROM Renames JOIN Songs ON Songs.hash = Renames.songHash
WHERE Renames.applied = 0 AND Songs.homeShard = @home ORDER BY Renames.id", conn);
                if (homeShard != null)
                    cmd.Parameters.AddWithValue("@home", homeShard);
                using SQLiteDataReader r = cmd.ExecuteReader();
                List<RenameRow> list = new List<RenameRow>();
                while (r.Read())
                    list.Add(ReadRename(r));
                return list;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.GetOpenRenames");
                return null;
            }
        }

        /// <summary>
        /// The names each of these songs had before its applied renames, oldest first (a song never renamed isn't in it;
        /// null on failure): a home song rebuilt from the database keeps them (MusicInfo.FormerNames).
        /// </summary>
        public static Dictionary<int, List<(string Title, string Author)>>? GetRenameHistory(IEnumerable<int> hashes)
        {
            try
            {
                using SQLiteConnection conn = Open();
                Dictionary<int, List<(string, string)>> history = new Dictionary<int, List<(string, string)>>();
                foreach (int hash in hashes)
                {
                    using SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Renames WHERE songHash = @hash AND applied = 1 ORDER BY id", conn);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    using SQLiteDataReader r = cmd.ExecuteReader();
                    List<(string, string)> names = new List<(string, string)>();
                    while (r.Read())
                    {
                        RenameRow n = ReadRename(r);
                        names.Add((n.OldTitle, n.OldAuthor));
                    }
                    if (names.Count > 0)
                        history[hash] = names;
                }
                return history;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.GetRenameHistory");
                return null;
            }
        }

        /// <summary>Marks these renames applied (or set aside: the song isn't in the home's library). False on failure.</summary>
        public static bool MarkRenamesApplied(IEnumerable<long> ids)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                foreach (long id in ids)
                {
                    using SQLiteCommand cmd = new SQLiteCommand("UPDATE Renames SET applied = 1 WHERE id = @id", conn, tx);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.MarkRenamesApplied");
                return false;
            }
        }
        #endregion Renames

        #region Reviewers
        /// <summary>Is this account a music reviewer (any letter case)? Null: the database couldn't be read.</summary>
        public static bool? IsReviewer(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return false;
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Reviewers WHERE account = @account", conn);
                cmd.Parameters.AddWithValue("@account", account.Trim());
                return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.IsReviewer");
                return null;
            }
        }

        /// <summary>Every reviewer, by account (null on failure).</summary>
        public static List<ReviewerRow>? GetReviewers()
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Reviewers ORDER BY account", conn);
                using SQLiteDataReader r = cmd.ExecuteReader();
                List<ReviewerRow> list = new List<ReviewerRow>();
                while (r.Read())
                    list.Add(new ReviewerRow
                    {
                        Account = Convert.ToString(r["account"]) ?? "",
                        AddedBy = Convert.ToString(r["addedBy"]) ?? "",
                        Utc = Convert.ToInt64(r["utc"]),
                    });
                return list;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.GetReviewers");
                return null;
            }
        }

        /// <summary>Adds a reviewer: 1 added, 0 already one, -1 failure.</summary>
        public static int AddReviewer(string account, string addedBy)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Reviewers (account, addedBy, utc) VALUES (@account, @addedBy, @utc)", conn);
                cmd.Parameters.AddWithValue("@account", account.Trim());
                cmd.Parameters.AddWithValue("@addedBy", addedBy ?? "");
                cmd.Parameters.AddWithValue("@utc", DateTime.UtcNow.Ticks);
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.AddReviewer");
                return -1;
            }
        }

        /// <summary>Removes a reviewer: 1 removed, 0 wasn't one, -1 failure.</summary>
        public static int RemoveReviewer(string account)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Reviewers WHERE account = @account", conn);
                cmd.Parameters.AddWithValue("@account", account.Trim());
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.RemoveReviewer");
                return -1;
            }
        }
        #endregion Reviewers

        #region Seeding
        // Seeding (first time only): each shard collects its library into Candidates ([MusicShare collect); the review
        //  chooses each song's home ([MusicShare review); the reviewed choices become Songs ([MusicShare seed).

        /// <summary>Replaces this shard's candidates with these (song rows with HomeShard = this shard; LastLogin per row). -1 on failure.</summary>
        public static int SaveCandidates(string shard, IList<(SongRow Row, long LastLogin)> rows)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                using (SQLiteCommand del = new SQLiteCommand("DELETE FROM Candidates WHERE shard = @shard", conn, tx))
                {
                    del.Parameters.AddWithValue("@shard", shard);
                    del.ExecuteNonQuery();
                }
                foreach ((SongRow s, long lastLogin) in rows)
                {
                    using SQLiteCommand cmd = new SQLiteCommand(@"
INSERT OR REPLACE INTO Candidates (shard, hash, composition, title, author, instrument, newTimer, prefetch, concertMode,
    napster, tempo, price, status, reason, composerGuid, composerName, dateSubmitted, dateApproved, dateRemastered,
    lastLogin, collectedUtc)
VALUES (@shard, @hash, @composition, @title, @author, @instrument, @newTimer, @prefetch, @concertMode, @napster, @tempo,
    @price, @status, @reason, @composerGuid, @composerName, @dateSubmitted, @dateApproved, @dateRemastered, @lastLogin, @now)", conn, tx);
                    cmd.Parameters.AddWithValue("@shard", shard);
                    cmd.Parameters.AddWithValue("@hash", s.Hash);
                    cmd.Parameters.AddWithValue("@composition", s.Composition);
                    cmd.Parameters.AddWithValue("@title", s.Title);
                    cmd.Parameters.AddWithValue("@author", s.Author);
                    cmd.Parameters.AddWithValue("@instrument", s.Instrument);
                    cmd.Parameters.AddWithValue("@newTimer", s.NewTimer ? 1 : 0);
                    cmd.Parameters.AddWithValue("@prefetch", s.Prefetch ? 1 : 0);
                    cmd.Parameters.AddWithValue("@concertMode", s.ConcertMode ? 1 : 0);
                    cmd.Parameters.AddWithValue("@napster", s.Napster ? 1 : 0);
                    cmd.Parameters.AddWithValue("@tempo", s.Tempo);
                    cmd.Parameters.AddWithValue("@price", s.Price);
                    cmd.Parameters.AddWithValue("@status", s.Status);
                    cmd.Parameters.AddWithValue("@reason", s.Reason);
                    cmd.Parameters.AddWithValue("@composerGuid", (long)s.ComposerGuid);
                    cmd.Parameters.AddWithValue("@composerName", s.ComposerName);
                    cmd.Parameters.AddWithValue("@dateSubmitted", s.DateSubmitted);
                    cmd.Parameters.AddWithValue("@dateApproved", s.DateApproved);
                    cmd.Parameters.AddWithValue("@dateRemastered", s.DateRemastered);
                    cmd.Parameters.AddWithValue("@lastLogin", lastLogin);
                    cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.Ticks);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
                return rows.Count;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.SaveCandidates");
                return -1;
            }
        }

        /// <summary>Every shard's candidates: (shard, song row, the composer account's last login there). Null on failure.</summary>
        public static List<(string Shard, SongRow Row, long LastLogin)>? GetCandidates()
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Candidates", conn);
                using SQLiteDataReader r = cmd.ExecuteReader();
                List<(string, SongRow, long)> list = new List<(string, SongRow, long)>();
                while (r.Read())
                {
                    string shard = Convert.ToString(r["shard"]) ?? "";
                    list.Add((shard, new SongRow
                    {
                        Hash = Convert.ToInt32(r["hash"]),
                        Composition = Convert.ToString(r["composition"]) ?? "",
                        Title = Convert.ToString(r["title"]) ?? "",
                        Author = Convert.ToString(r["author"]) ?? "",
                        Instrument = Convert.ToString(r["instrument"]) ?? "harp",
                        NewTimer = Convert.ToInt64(r["newTimer"]) != 0,
                        Prefetch = Convert.ToInt64(r["prefetch"]) != 0,
                        ConcertMode = Convert.ToInt64(r["concertMode"]) != 0,
                        Napster = Convert.ToInt64(r["napster"]) != 0,
                        Tempo = Convert.ToInt32(r["tempo"]),
                        Price = Convert.ToInt32(r["price"]),
                        Status = Convert.ToInt32(r["status"]),
                        Reason = Convert.ToInt32(r["reason"]),
                        ComposerGuid = (uint)Convert.ToInt64(r["composerGuid"]),
                        ComposerName = Convert.ToString(r["composerName"]) ?? "",
                        HomeShard = shard,
                        DateSubmitted = Convert.ToInt64(r["dateSubmitted"]),
                        DateApproved = Convert.ToInt64(r["dateApproved"]),
                        DateRemastered = Convert.ToInt64(r["dateRemastered"]),
                    }, Convert.ToInt64(r["lastLogin"])));
                }
                return list;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.GetCandidates");
                return null;
            }
        }

        /// <summary>
        /// Has the database been seeded ([MusicShare seed)? A shard never syncs with one that hasn't: the first to start
        /// would claim every song in its library as its own. Null: the database couldn't be read (or isn't there).
        /// </summary>
        public static bool? IsSeeded()
        {
            try
            {
                if (!File.Exists(DatabasePath))
                    return null;
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT value FROM Meta WHERE key = 'seeded'", conn);
                return Convert.ToString(cmd.ExecuteScalar()) == "1";
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.IsSeeded");
                return null;
            }
        }

        /// <summary>
        /// May this shard write to the database (its writes released by staff: [MusicShare release)? Only a shard that
        /// starts held asks (the Music Conservatory). False when it isn't released; null when that can't be read: the
        /// caller treats both as held (fail closed).
        /// </summary>
        public static bool? IsReleased(string shard)
        {
            try
            {
                if (!File.Exists(DatabasePath))
                    return null;
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT value FROM Meta WHERE key = @key", conn);
                cmd.Parameters.AddWithValue("@key", "released:" + shard);
                object? value = cmd.ExecuteScalar();
                return value != null && value != DBNull.Value && Convert.ToString(value)!.Length > 0;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.IsReleased");
                return null;
            }
        }

        /// <summary>Releases (who and when: "Adam Ant 2026-11-16 21:50Z") or holds again (null) this shard's writes. False on failure.</summary>
        public static bool SetReleased(string shard, string? byWhom)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand(byWhom == null ? "DELETE FROM Meta WHERE key = @key"
                    : "INSERT OR REPLACE INTO Meta (key, value) VALUES (@key, @value)", conn);
                cmd.Parameters.AddWithValue("@key", "released:" + shard);
                if (byWhom != null)
                    cmd.Parameters.AddWithValue("@value", byWhom);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.SetReleased");
                return false;
            }
        }

        /// <summary>Marks the database seeded (the end of [MusicShare seed). False on failure.</summary>
        public static bool MarkSeeded()
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Meta (key, value) VALUES ('seeded', '1')", conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.MarkSeeded");
                return false;
            }
        }

        /// <summary>How many songs the database holds (-1 on failure).</summary>
        public static int CountSongs()
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Songs", conn);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.CountSongs");
                return -1;
            }
        }
        #endregion Seeding

        /// <summary>Marks these sales recognized (the composer was credited and told). False on failure.</summary>
        public static bool MarkRecognized(IEnumerable<long> saleIds)
        {
            try
            {
                using SQLiteConnection conn = Open();
                using SQLiteTransaction tx = conn.BeginTransaction();
                foreach (long id in saleIds)
                {
                    using SQLiteCommand cmd = new SQLiteCommand("UPDATE Sales SET recognized = 1 WHERE id = @id", conn, tx);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "MusicShareDatabase.MarkRecognized");
                return false;
            }
        }
    }
}
