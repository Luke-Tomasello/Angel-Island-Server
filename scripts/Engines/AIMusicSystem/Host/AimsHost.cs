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


/* Scripts\Engines\AIMusicSystem\Host\AimsHost.cs
 * Changelog
 *  11/14/26, Adam (11/11/26 to 11/14/26)
 *      OnTrackApproved: a submitted track was approved (first approval or remaster), for a shard's own news.
 *      MusicImportDirectory: where [MusicImport reads song files (default: Data\Music Import). ImportOwner: who owns the
 *      songs an admin imports, for the library's records only (default: that admin). An imported song pays no one: its
 *      sales and plays earn its owner nothing (no gold, credit, points or sale notice); OnImportedSale hears each sale,
 *      for a shard that sends the gold somewhere (default: nothing: it leaves the economy).
 *  11/9/26, Adam (review notice)
 *      ReviewNeeded: songs submitted here await review (every 5 minutes while they do). Default: Administrators are told
 *      "[Music System] New music, review required.", as the library's maintenance always did itself.
 *  11/7/26, Adam (track note, submission refusal)
 *      TrackNote: a note about a track in the line staff get ('find'; 'who' for those who may use 'find'). RefuseSubmission: the shard may
 *      refuse a submission or remaster, with its reason. Both do nothing by default.
 *  11/7/26, Adam (purchase event)
 *      OnMusicPurchased: every purchase, as it happens (buyer, song, record, price paid). A song owned by SystemMobile is
 *      one the shard holds for someone else: its sale queues no notice to a seller.
 *  11/7/26, Adam
 *      Deposit: paying a composer (a sale, or music box credit owed from sales their bank couldn't take). Default: the
 *      bank box (Banker.Deposit), as before.
 *  11/6/26, Adam (2026 modernization, 11/5/26 to 11/6/26)
 *      AimsHost: what the music system needs from the shard it runs on. AIMS provides the vehicle, the shard provides
 *      the destination: each member has a default that works on a stock RunUO server, and a shard overrides the ones
 *      it wants wired to its own systems by deriving a class from AimsHost (anywhere in its scripts; AIMS finds it).
 *      Angel Island's is AngelIslandAimsHost (Scripts\Engines\AIMusicHost), outside this folder.
 *      Members: logging (and log files: WriteLog), the Data and Logs folders, identity (account GUID), Music Karma and
 *      the music filter (default: kept by AIMS, Saves\AIMS\Players.bin), purchases, households, copies of sheets
 *      (staff copies, temporary playback copies), internal storage, the sale event, and the shard's settings (Test
 *      Center, [play access, whether a leaderboard shows music sales, whether MusicEngine.log is written).
 */

using Server.Accounting;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Server
{
    /// <summary>
    /// The music system's view of the shard. Derive from this class to wire AIMS to your shard's systems;
    /// override only what you need. AIMS uses the first class it finds that derives from AimsHost, or this one.
    /// </summary>
    public class AimsHost
    {
        private static AimsHost? m_Current;

        /// <summary>The host in use: the shard's class derived from AimsHost if there is one, else the defaults.</summary>
        public static AimsHost Current
        {
            get { return m_Current ??= Find(); }
            set { m_Current = value; }
        }

        private static AimsHost Find()
        {
            Type[] types;
            try
            {
                types = typeof(AimsHost).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (Type t in types)
            {
                if (t != typeof(AimsHost) && typeof(AimsHost).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                    return (AimsHost)Activator.CreateInstance(t)!;
            }

            return new AimsHost();
        }

        #region Logging
        /// <summary>Records an exception the music system caught. Default: the console.</summary>
        public virtual void LogException(Exception ex, string? message)
        {
            try
            {
                string text = message != null
                    ? string.Format("{0}\r\n{1}\r\n{2}", message, ex.Message, ex.StackTrace)
                    : string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace);

                WriteLine(text, ConsoleColor.Red);
            }
            catch
            {
                // never throw from the exception logger
            }
        }

        /// <summary>Writes a line to the server console in a color. Default: the console.</summary>
        public virtual void WriteLine(string text, ConsoleColor color)
        {
            lock (typeof(AimsHost))
            {
                ConsoleColor old = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ForegroundColor = old;
            }
        }

        /// <summary>
        /// Appends a record to one of the shard's log files (MusicRecorder.log: a runaway recording): who it is about, then
        /// one line per item. Default: the file in LogsDirectory, after a timestamped line.
        /// </summary>
        public virtual void WriteLog(string fileName, Mobile? about, IEnumerable<object> lines)
        {
            try
            {
                string path = Path.Combine(LogsDirectory, fileName);
                AimsUtil.EnsurePath(path);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Format("{0:u} {1}", DateTime.UtcNow, about?.ToString() ?? ""));
                foreach (object line in lines)
                    sb.AppendLine("    " + line);

                File.AppendAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                LogException(ex, "Error writing " + fileName);
            }
        }
        #endregion Logging

        #region Messages
        /// <summary>A status line from a music item to staff nearby (within 10 tiles, at or above an access level).</summary>
        public virtual void SendMessageNearby(Item item, string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            if (string.IsNullOrEmpty(text) || item.Map == null)
                return;

            IPooledEnumerable eable = item.Map.GetMobilesInRange(item.GetWorldLocation(), 10);

            foreach (Mobile m in eable)
            {
                if (m.AccessLevel >= accesslevel)
                    m.SendMessage(hue, text);
            }

            eable.Free();
        }

        /// <summary>
        /// A status line from a music item or a player to staff nearby, at most once per timeout for the same source and
        /// text (so a looping device doesn't flood them).
        /// </summary>
        public virtual void SendSystemMessage(IEntity source, double second_timeout, string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            if (source == null || string.IsNullOrEmpty(text) || !AimsUtil.AntiSpam(source.Serial.ToString() + text, second_timeout))
                return;

            if (source is Item item)
            {
                SendMessageNearby(item, text, accesslevel, hue);
            }
            else if (source is Mobile from && from.Map != null)
            {
                IPooledEnumerable eable = from.GetMobilesInRange(10);
                foreach (Mobile m in eable)
                {
                    if (m.AccessLevel >= accesslevel)
                        m.SendMessage(hue, text);
                }
                eable.Free();
            }
        }
        #endregion Messages

        #region Files
        /// <summary>The shard's Data folder (song files are in its "song files" folder). Default: Data beside the server.</summary>
        public virtual string DataDirectory
        {
            get { return Path.Combine(AppContext.BaseDirectory, "Data"); }
        }

        /// <summary>Where [MusicImport reads song files (.rzr). Default: the "Music Import" folder in the Data folder.</summary>
        public virtual string MusicImportDirectory
        {
            get { return Path.Combine(DataDirectory, "Music Import"); }
        }

        /// <summary>The shard's Logs folder (MusicEngine.log). Default: Logs beside the server.</summary>
        public virtual string LogsDirectory
        {
            get { return Path.Combine(AppContext.BaseDirectory, "Logs"); }
        }
        #endregion Files

        #region Identity
        /// <summary>
        /// The account a player belongs to, as a number that never changes: who composed a song and who owns it. Songs stay
        /// with their composer across characters and restarts by this number. Default: a stable hash of the account name.
        /// </summary>
        public virtual uint AccountGuid(IAccount? acct)
        {
#if ANGEL_ISLAND
            string? username = (acct as Account)?.Username;     // Angel Island's IAccount has no Username
#else
            string? username = acct?.Username;
#endif
            if (string.IsNullOrEmpty(username))
                return 0;

            uint guid = (uint)AimsUtil.GetStableHashCode(username.ToLowerInvariant());
            return guid != 0 ? guid : 1;
        }

        /// <summary>The account GUID of a player (0: none). Default: AccountGuid of the player's account.</summary>
        public virtual uint AccountGuid(Mobile? m)
        {
            return m != null ? AccountGuid(m.Account) : 0;
        }

        /// <summary>
        /// Are these two players one household (the same person or family, by the shard's rules)? A composer isn't paid for
        /// a household member's purchase. Default: the same account.
        /// </summary>
        public virtual bool IsHousehold(Mobile owner, Mobile other)
        {
            return owner != null && other != null && owner.Account != null && owner.Account == other.Account;
        }
        #endregion Identity

        #region Player data
        /// <summary>A player's Music Karma: points earned by composing, publishing, sales and plays. Default: kept by AIMS.</summary>
        public virtual double GetMusicKarma(Mobile m)
        {
            return AimsPlayerData.Get(m)?.Karma ?? 0.0;
        }

        public virtual void SetMusicKarma(Mobile m, double value)
        {
            AimsPlayerData.GetOrCreate(m).Karma = value;
        }

        /// <summary>Has the player turned off other players' music ([FilterMusic)? Default: kept by AIMS.</summary>
        public virtual bool GetFilterMusic(Mobile m)
        {
            return AimsPlayerData.Get(m)?.FilterMusic ?? false;
        }

        public virtual void SetFilterMusic(Mobile m, bool value)
        {
            AimsPlayerData.GetOrCreate(m).FilterMusic = value;
        }
        #endregion Player data

        #region Gold
        /// <summary>Takes a song's price from the buyer (false: they can't pay). Default: the buyer's bank box.</summary>
        public virtual bool Withdraw(Mobile buyer, int amount)
        {
#if ANGEL_ISLAND
            return Banker.CombinedWithdrawFromAllEnrolled(buyer, amount);   // Angel Island's Banker has no Withdraw
#else
            return Banker.Withdraw(buyer, amount);
#endif
        }

        /// <summary>
        /// Pays a composer: a sale's price, or credit owed from sales their bank couldn't take before (false: it can't take
        /// it now, and AIMS keeps it owed). Default: the composer's bank box (gold, or a check from 1,000).
        /// </summary>
        public virtual bool Deposit(Mobile composer, int amount)
        {
            return Banker.Deposit(composer, amount);
        }
        #endregion Gold

        #region Items
        /// <summary>
        /// A complete, independent copy of a music item (a sheet's staff copy, or a temporary playback copy), or null.
        /// Default: sheet music copies itself (RolledUpSheetMusic.CopySheet).
        /// </summary>
        public virtual Item? Dupe(Item item)
        {
            return item is Misc.RolledUpSheetMusic rsm ? rsm.CopySheet(rsm.Amount) : null;
        }

        /// <summary>A staff member made this copy (the shard may track staff copies). Default: nothing.</summary>
        public virtual void MarkStaffOwned(Mobile staff, Item copy)
        {
        }

        /// <summary>
        /// A temporary item the music system made for playback (a preview, a music box's playing copy). AIMS deletes its own
        /// once they can't play again; this is the shard's safety net: it may delete one that is no longer playing (Angel
        /// Island: cron, from 5 minutes after it was made), and its other cleanup rules should leave it alone. Default:
        /// nothing (see AimsUtil.MakeTempSheet).
        /// </summary>
        public virtual void MarkTemporary(Item item)
        {
        }

        /// <summary>Stores an item off the map (the music library keeps its sheets there). Default: Item.Internalize.</summary>
        public virtual void MoveToInternal(Item item)
        {
            item.Internalize();
        }

        /// <summary>
        /// The placeholder owner of music box settings read from saves made before each user had their own (old shards only).
        /// Default: none.
        /// </summary>
        public virtual Mobile? SystemMobile
        {
            get { return null; }
        }

        /// <summary>
        /// Who owns the songs an admin imports ([MusicImport), for the library's records only: an imported song pays no
        /// one (OnImportedSale). Its composer has no character here; the song keeps its author line. Default: the admin
        /// who imports them.
        /// </summary>
        public virtual Mobile? ImportOwner(Mobile importer)
        {
            return importer;
        }
        #endregion Items

        #region Settings
        /// <summary>Is this a test server (staff test commands such as flushall)? Default: no.</summary>
        public virtual bool IsTestCenter
        {
            get { return false; }
        }

        /// <summary>Who may use [play. Default: players.</summary>
        public virtual AccessLevel PlayAccessLevel
        {
            get { return AccessLevel.Player; }
        }

        /// <summary>
        /// Write the music engine's measurements to Logs\MusicEngine.log (a summary every 5 minutes while music plays, and
        /// [MusicMetrics report)? Measuring goes on either way. Default: yes.
        /// </summary>
        public virtual bool MusicEngineLog
        {
            get { return true; }
        }
        #endregion Settings

        #region Events
        /// <summary>
        /// A composer has been told of a sale of their song (a leaderboard's moment: gold earned, one more sale).
        /// Default: nothing.
        /// </summary>
        public virtual void OnMusicSold(Mobile composer, int price)
        {
        }

        /// <summary>
        /// A player has just bought a song from a music box: the buyer, the library's sheet and record, and the price paid
        /// (already taken from the buyer and paid through Deposit). Called for every purchase, at once, whoever the composer
        /// is (OnMusicSold waits for the composer to be told). Default: nothing.
        /// </summary>
        public virtual void OnMusicPurchased(Mobile buyer, Misc.RolledUpSheetMusic song, Items.MusicLibrary.MusicInfo info, int price)
        {
        }

        /// <summary>
        /// A purchase of an imported song ([MusicImport): it pays no one, since no one here composed it, so the gold leaves
        /// the economy. A shard that wants it to go somewhere (a town fund, say) sends it there. Called before
        /// OnMusicPurchased. Default: nothing.
        /// </summary>
        public virtual void OnImportedSale(Mobile buyer, Misc.RolledUpSheetMusic song, Items.MusicLibrary.MusicInfo info, int price)
        {
        }

        /// <summary>
        /// A submitted track has just been approved, on the shard that approved it: a first approval, or a remaster that
        /// replaced the approved track (remaster true; 'info' is the record it took over). For a shard's own news (an
        /// announcement, a feed). Default: nothing.
        /// </summary>
        public virtual void OnTrackApproved(Mobile approver, Misc.RolledUpSheetMusic song, Items.MusicLibrary.MusicInfo info, bool remaster)
        {
        }

        /// <summary>
        /// A note about a library track for this viewer, added to the line staff get about it (a music box's 'find', and
        /// 'who' for those who may use 'find'), such as where a shared song comes from. Default: none.
        /// </summary>
        public virtual string? TrackNote(Mobile viewer, Misc.RolledUpSheetMusic song, Items.MusicLibrary.MusicInfo? info)
        {
            return null;
        }

        /// <summary>
        /// May this sheet music be submitted to the library (or, remaster, replace its earlier version)? A reason refuses it,
        /// and the composer is told it. Called once the sheet itself is valid, before the library's own rules (a remaster
        /// needs an earlier version). Default: no refusal.
        /// </summary>
        public virtual string? RefuseSubmission(Mobile composer, Misc.RolledUpSheetMusic sheet, bool remaster)
        {
            return null;
        }

        /// <summary>
        /// Songs submitted on this shard are waiting for review ('count' of them). Called every 5 minutes while any are,
        /// from the library's maintenance. Default: Administrators and above who are online are told "[Music System] New
        /// music, review required." Override to tell whom you choose, or no one (a shard that reports review its own way).
        /// </summary>
        public virtual void ReviewNeeded(int count)
        {
            Commands.CommandHandlers.BroadcastMessage(AccessLevel.Administrator, 54, "[Music System] New music, review required.");
        }

        /// <summary>
        /// True when your OnMusicSold feeds a leaderboard players can see; the composer's sale notices then may say
        /// "Check the leaderboard!". Default: false (no leaderboard, so the notices never mention one).
        /// </summary>
        public virtual bool HasLeaderboard
        {
            get { return false; }
        }
        #endregion Events
    }

#if !ANGEL_ISLAND
    // Angel Island's core marks properties its dupe command must not copy; elsewhere the attribute is only a note.
    public enum CopyType { Copy, DoNotCopy, DoNotRelink }

    [AttributeUsage(AttributeTargets.Property)]
    public class CopyableAttribute : Attribute
    {
        public CopyType CopyType { get; }

        public CopyableAttribute(CopyType copyType)
        {
            CopyType = copyType;
        }
    }
#endif
}
