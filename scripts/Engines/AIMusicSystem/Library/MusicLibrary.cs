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


/* Scripts\Engines\AIMusicSystem\Library\MusicLibrary.cs
 * Changelog
 *  11/14/26, Adam
 *      A record knows it was imported ([MusicImport: the Imported flag, saved with the others, no format change), and
 *      its submission date can be set, so a record rebuilt from elsewhere keeps the day the song was submitted.
 *      Finding a song by name (FindMatches) means what was said: a serial, an exact title, or every title holding all
 *      the words said (case and punctuation aside); titles that only share some letters no longer match (they did:
 *      "delete clip", with the clip gone, deleted Axel F), though the closest may be suggested. FindSong and FindEntry
 *      give the closest of the matches; [DupeMusic says when several match.
 *  11/8/26, Adam (2026 modernization, 10/29/26 to 11/8/26)
 *      The global music library, moved out of MusicBox unchanged but for the names (GlobalMusicRepository is
 *      Repository): every approved player-composed track and the review queue, the database records, Saves/Music.bin
 *      (its format unchanged), the 5-minute maintenance, FindSong and [DupeMusic. Anything can use it without a music
 *      box. Moved from MusicBox (MusicBox.Library.cs); its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: account GUIDs and staff copies ([DupeMusic). Title matching uses AimsUtil.LongestCommonSubstring (it
 *      used the Intelligent Dialogue System's rolling hash; the same scores).
 *      Maintenance: a record whose composer is gone (no character on this shard, or deleted) forfeits its credit
 *      (ForfeitCredit, MusicCredits.log) as its music becomes free.
 *      Composers hear review decisions: MusicInfo version 5 saves Note (staff's note on a rejection) and the
 *      ComposerTold flag (a record from an older save counts as told); a status changed to Approved or Rejected is news
 *      until TellComposer (MusicLibrary.Composers.cs) or 'status' has told them.
 *      The staff notice of songs awaiting review goes through the host (AimsHost.ReviewNeeded; its default is the
 *      notice as it was), so a shard can tell others, or report review its own way.
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Engines;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.Items
{
    public static partial class MusicLibrary
    {
        #region The library
        // we never want these tracks to disappear forever. Angel Island will keep a global repository of
        //	player created music. (Could be used elsewhere.)
        public static readonly Dictionary<RolledUpSheetMusic, MusicInfo> Repository = new Dictionary<RolledUpSheetMusic, MusicInfo>();

        // submitted tracks waiting for staff review (the music box reviews, approves and denies them)
        internal static readonly List<RolledUpSheetMusic> UnpublishedNeedsReview = new List<RolledUpSheetMusic>();
        public static IReadOnlyList<RolledUpSheetMusic> ReviewQueue => UnpublishedNeedsReview;

        // takes a track off the review queue (approved, denied or deleted); nothing if it is not waiting there
        internal static void Unqueue(RolledUpSheetMusic track) => UnpublishedNeedsReview.Remove(track);
        #endregion The library

        #region Music Database Format
        public enum SubmissionStatus
        {
            Pending,
            Approved,
            Rejected
        }
        public enum RejectionReason
        {
            None,
            TooLong,
            TooShort,
            Inappropriate,
            Duplicate,
            Other
        }
        public record PurchaserInfo
        {
            Mobile m_purchaser = null;          // who bought it
            DateTime m_PurchaseDate;            // when purchased
            int m_Price;                        // price paid
            SubmissionStatus m_status;          // submission status
            RejectionReason m_rejectionReason;  // why rejected
            public SubmissionStatus Status { get { return m_status; } set { m_status = value; } }
            public RejectionReason Reason { get { return m_rejectionReason; } set { m_rejectionReason = value; } }
            public Mobile Purchaser => m_purchaser;
            public DateTime PurchaseDate => m_PurchaseDate;
            public int Price => m_Price;
            public PurchaserInfo(Mobile m, int price)
            {
                m_purchaser = m;
                m_PurchaseDate = DateTime.UtcNow;
                m_Price = price;
            }
            public PurchaserInfo(GenericReader reader)
            {
                int version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 0:
                        {
                            m_purchaser = reader.ReadMobile();
                            m_PurchaseDate = reader.ReadDateTime();
                            m_Price = reader.ReadEncodedInt();
                            break;
                        }
                }
            }
            public void Serialize(GenericWriter writer)
            {
                int version = 0;
                writer.WriteEncodedInt(version);
                switch (version)
                {
                    case 0:
                        {
                            writer.Write(m_purchaser);
                            writer.Write(m_PurchaseDate);
                            writer.WriteEncodedInt(m_Price);
                            break;
                        }
                }
            }
        }
        [Flags]
        public enum MusicInfoFlags
        {
            None,
            Napster = 0x01,
            Remastered = 0x02,
            ComposerTold = 0x04,    // the composer has heard the latest review decision (TellComposer, status)
            Imported = 0x08,        // added from a song file by [MusicImport (MusicImport.cs), not submitted at a music box
        }
        public record MusicInfo
        {
            List<PurchaserInfo> m_purchasers = new List<PurchaserInfo>();
            Mobile m_Owner = null;             // who authored it
            DateTime m_DateSubmitted;           // when submitted
            DateTime m_DateApproved;            // when approved
            DateTime m_DateRemastered;          // when remastered
            int m_Price;                        // cost

            int m_totalSales = 0;               // total track sales in gold
            int m_credit = 0;                   // couldn't fit in bank box: owed, paid at the composer's next music box visit (CollectCredits)
            int m_sold = 0;                     // units sold
            int m_plays = 0;                    // number of times this track has been played
            SubmissionStatus m_status;          // submission status
            RejectionReason m_rejectionReason;  // why rejected
            MusicInfoFlags m_flags = MusicInfoFlags.None;
            string? m_note;                     // staff's note to the composer on a rejection (version 5)
            public List<PurchaserInfo> Purchasers => m_purchasers;
            // a new decision (approved or rejected) is news for the composer: TellComposer tells them once
            public SubmissionStatus Status
            {
                get { return m_status; }
                set
                {
                    if (value != m_status && value != SubmissionStatus.Pending)
                        ComposerTold = false;
                    if (value == SubmissionStatus.Approved)
                        m_note = null;
                    m_status = value;
                }
            }
            public string? Note { get { return m_note; } set { m_note = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); } }
            public bool ComposerTold { get { return m_flags.HasFlag(MusicInfoFlags.ComposerTold); } set { m_flags = value ? m_flags | MusicInfoFlags.ComposerTold : m_flags & ~MusicInfoFlags.ComposerTold; } }
            public RejectionReason Reason { get { return m_rejectionReason; } set { m_rejectionReason = value; } }
            public int TotalSales { get { return m_totalSales; } set { m_totalSales = value; } }
            public int Credit { get { return m_credit; } set { m_credit = value; } }
            public int Sold { get { return m_sold; } set { m_sold = value; } }
            public int Plays { get { return m_plays; } set { m_plays = value; } }
            public bool Napster { get { return m_flags.HasFlag(MusicInfoFlags.Napster); } set { m_flags = (value == true) ? m_flags | MusicInfoFlags.Napster : m_flags & ~MusicInfoFlags.Napster; } }
            public bool Remastered { get { return m_flags.HasFlag(MusicInfoFlags.Remastered); } set { m_flags = (value == true) ? m_flags | MusicInfoFlags.Remastered : m_flags & ~MusicInfoFlags.Remastered; } }
            public bool Imported { get { return m_flags.HasFlag(MusicInfoFlags.Imported); } set { m_flags = value ? m_flags | MusicInfoFlags.Imported : m_flags & ~MusicInfoFlags.Imported; } }
            public Mobile Owner
            {
                get { return m_Owner; }
                set
                {
                    m_Owner = value;

                    // Identity is account-rooted; PlayerMobile.GUID is the portable identity handle.
                    // Keep MusicInfo.OwnerGUID synchronized with assigned owner.
                    m_OwnerGUID = value is PlayerMobile && !value.Deleted ? AimsHost.Current.AccountGuid(value) : 0;
                }
            }
            private uint m_OwnerGUID;
            public uint OwnerGUID
            {
                get { return m_OwnerGUID; }
                set { m_OwnerGUID = value; }
            }

            // set when a record is rebuilt from elsewhere (a shard's own copy of a song kept outside its world), so it keeps
            //  the day the song was submitted, not the day it was rebuilt
            public DateTime DateSubmitted { get { return m_DateSubmitted; } set { m_DateSubmitted = value; } }
            public DateTime DateApproved { get { return m_DateApproved; } set { m_DateApproved = value; } }

            public DateTime DateRemastered { get { return m_DateRemastered; } set { m_DateRemastered = value; } }
            public int Price { get { return m_Price; } set { m_Price = value; } }
            public MusicInfo(Mobile m, int price)
            {
                Owner = m;
                m_DateSubmitted = DateTime.UtcNow;
                m_Price = price;
                m_status = SubmissionStatus.Pending;
                m_rejectionReason = RejectionReason.None;
            }
            public MusicInfo(GenericReader reader)
            {
                int version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 5:
                        {
                            m_note = reader.ReadString();
                            goto case 4;
                        }
                    case 4:
                        {
                            m_OwnerGUID = reader.ReadUInt();
                            goto case 3;
                        }
                    case 3:
                        {
                            m_DateRemastered = reader.ReadDateTime();
                            goto case 2;
                        }
                    case 2:
                        {
                            m_DateApproved = reader.ReadDateTime();
                            goto case 1;
                        }
                    case 1:
                        {
                            m_flags = (MusicInfoFlags)reader.ReadEncodedInt();
                            goto case 0;
                        }
                    case 0:
                        {
                            int count = reader.ReadEncodedInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                PurchaserInfo lml = new PurchaserInfo(reader);
                                m_purchasers.Add(lml);
                            }
                            m_status = (SubmissionStatus)reader.ReadEncodedInt();
                            m_rejectionReason = (RejectionReason)reader.ReadEncodedInt();
                            m_totalSales = reader.ReadEncodedInt();
                            m_credit = reader.ReadEncodedInt();
                            m_sold = reader.ReadEncodedInt();
                            m_plays = reader.ReadEncodedInt();
                            // Don't be tempted to sync or clear OwnerGUID here.
                            // Mobile resolution may fail on load, but OwnerGUID is persistent identity metadata.
                            m_Owner = reader.ReadMobile();
                            m_DateSubmitted = reader.ReadDateTime();
                            m_Price = reader.ReadEncodedInt();
                            break;
                        }
                    default:
                        throw new Exception("MusicInfo: invalid version");
                }

                // saved before composers were told of decisions: what's decided is old news, not told again
                if (version < 5)
                    m_flags |= MusicInfoFlags.ComposerTold;
            }
            public void Serialize(GenericWriter writer)
            {
                int version = 5;
                writer.WriteEncodedInt(version);
                switch (version)
                {
                    case 5:
                        {
                            writer.Write(m_note);
                            goto case 4;
                        }
                    case 4:
                        {
                            writer.Write(m_OwnerGUID);
                            goto case 3;
                        }
                    case 3:
                        {
                            writer.Write(m_DateRemastered);
                            goto case 2;
                        }
                    case 2:
                        {
                            writer.Write(m_DateApproved);
                            goto case 1;
                        }
                    case 1:
                        {
                            writer.WriteEncodedInt((int)m_flags);
                            goto case 0;
                        }
                    case 0:
                        {
                            writer.WriteEncodedInt(m_purchasers.Count);
                            for (int ix = 0; ix < m_purchasers.Count; ix++)
                                m_purchasers[ix].Serialize(writer);
                            writer.WriteEncodedInt((int)m_status);
                            writer.WriteEncodedInt((int)m_rejectionReason);
                            writer.WriteEncodedInt(m_totalSales);
                            writer.WriteEncodedInt(m_credit);
                            writer.WriteEncodedInt(m_sold);
                            writer.WriteEncodedInt(m_plays);
                            writer.Write(m_Owner);
                            writer.Write(m_DateSubmitted);
                            writer.WriteEncodedInt(m_Price);
                            break;
                        }
                }
            }
        }
        #endregion Music Database Format

        #region Global Music Repository Serialization
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }
        public static void Load()
        {
            if (!File.Exists("Saves/Music.bin"))
                return;

            Console.WriteLine("Global music library Loading...");
            BinaryFileReader? reader = null;
            try
            {
                reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/Music.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadEncodedInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Item? item = reader.ReadItem();
                                if (item is RolledUpSheetMusic rsm && rsm != null)
                                    UnpublishedNeedsReview.Add(rsm);
                            }

                            count = reader.ReadEncodedInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                RolledUpSheetMusic? rsm = (RolledUpSheetMusic?)reader.ReadItem();
                                if (rsm != null)
                                    Repository.Add(rsm, new MusicInfo(reader));
                                else
                                    // throw it away
                                    new MusicInfo(reader);
                            }
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid Music.bin savefile version.");
                        }
                }
            }
            catch
            {
                AimsLog.WriteLine("Error reading Saves/Music.bin, using default values...", ConsoleColor.Red);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Global music library Saving...");
            BinaryFileWriter? writer = null;
            try
            {
                writer = new BinaryFileWriter("Saves/Music.bin", true);
                int version = 0;
                writer.WriteEncodedInt(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.WriteEncodedInt(UnpublishedNeedsReview.Count);
                            foreach (RolledUpSheetMusic rsp in UnpublishedNeedsReview)
                                writer.Write(rsp);

                            writer.WriteEncodedInt(Repository.Count);
                            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                            {
                                writer.Write(kvp.Key);
                                kvp.Value.Serialize(writer);
                            }

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                AimsLog.WriteLine("Error writing Saves/Music.bin", ConsoleColor.Red);
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        #endregion Global Music Repository Serialization

        #region Maintenance
        // Every 5 minutes, after the music boxes' sale notices (MusicBox.MaintenanceTimer).
        public static void Maintain()
        {
            // notify staff some unpublished work needs review
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                if (UnpublishedNeedsReview.Contains(kvp.Key) && kvp.Value.Status == SubmissionStatus.Rejected)
                    UnpublishedNeedsReview.Remove(kvp.Key);

            if (UnpublishedNeedsReview.Count > 0)
                ReviewNotify();

            // flush all deleted tracks
            RemoveDeletedTracks();
        }

        /// <summary>
        /// Performs periodic cleanup of the Repository.
        ///
        /// Responsibilities:
        /// - Remove deleted RolledUpSheetMusic entries.
        /// - If the composer no longer exists, mark their music as free (Price = 0) and forfeit any credit owed (ForfeitCredit).
        ///
        /// Defensive rules:
        /// - Avoid modifying the dictionary while enumerating.
        /// - Fail closed on exception.
        /// </summary>
        private static void RemoveDeletedTracks()
        {
            try
            {
                if (Repository.Count == 0)
                    return;

                List<RolledUpSheetMusic> killList = new List<RolledUpSheetMusic>();

                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                {
                    RolledUpSheetMusic rsm = kvp.Key;
                    MusicInfo info = kvp.Value;

                    if (rsm == null)
                        continue;

                    // Schedule deleted tracks for removal.
                    if (rsm.Deleted)
                    {
                        killList.Add(rsm);
                        continue;
                    }

                    if (info == null)
                        continue;

                    // If composer leaves shard or deletes character,
                    // music becomes free, and credit owed to them is forfeited (MusicLibrary.Composers.cs, Credit).
                    if (info.Owner == null || info.Owner.Deleted)
                    {
                        info.Price = 0;
                        ForfeitCredit(rsm, info, info.Owner == null ? "the composer has no character on this shard" : "the composer's character was deleted");
                    }
                }

                // Remove after enumeration to avoid modifying collection mid-loop.
                foreach (RolledUpSheetMusic rsm in killList)
                {
                    Repository.Remove(rsm);
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        // the shard decides who hears of it (default: Administrators, "[Music System] New music, review required.")
        private static void ReviewNotify()
        {
            AimsHost.Current.ReviewNeeded(UnpublishedNeedsReview.Count);
        }
        #endregion Maintenance

        #region Finding a song
        /// <summary>
        /// What a spoken (or typed) song name matched (FindMatches): the songs, closest first, and how.
        /// </summary>
        public sealed class SongMatches
        {
            /// <summary>What was said.</summary>
            public string Text = "";
            /// <summary>The songs it names: the exact title (or serial), else every title holding all its words, shortest first.</summary>
            public readonly List<KeyValuePair<RolledUpSheetMusic, MusicInfo>> Songs = new List<KeyValuePair<RolledUpSheetMusic, MusicInfo>>();
            /// <summary>The title is exactly what was said, or the serial was given.</summary>
            public bool Exact;
            /// <summary>Nothing matched: the closest title, if close enough to suggest ("Did you mean ...?"). Never acted on.</summary>
            public RolledUpSheetMusic? Suggestion;
        }

        /// <summary>
        /// The songs a name names, among those 'which' allows (null: every song). In order: a song's serial ("0x4003F1D0");
        /// an exact title; else every title that holds all the words said, in any order ("kpdh golden" is "Golden
        /// (KPDH)"), shortest title first. Case, punctuation and extra spaces don't count. Titles that merely share some
        /// letters never match (they did: "delete clip", with the clip gone, deleted "Axel F"); the closest of them may be
        /// offered as a suggestion.
        /// </summary>
        public static SongMatches FindMatches(string? text, Func<RolledUpSheetMusic, MusicInfo, bool>? which = null)
        {
            SongMatches result = new SongMatches { Text = (text ?? string.Empty).Trim() };
            try
            {
                string said = Plain(result.Text);
                if (said.Length == 0)
                    return result;

                List<KeyValuePair<RolledUpSheetMusic, MusicInfo>> songs = new List<KeyValuePair<RolledUpSheetMusic, MusicInfo>>();
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in Repository)
                    if (kvp.Key != null && !kvp.Key.Deleted && kvp.Value != null && (which == null || which(kvp.Key, kvp.Value)))
                        songs.Add(kvp);

                // a serial: that song
                string trimmed = result.Text.Trim();
                if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(trimmed.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int serial))
                {
                    result.Songs.AddRange(songs.Where(s => s.Key.Serial.Value == serial));
                    result.Exact = result.Songs.Count > 0;
                    return result;
                }

                // an exact title (two songs can share one)
                result.Songs.AddRange(songs.Where(s => Plain(s.Key.ToString()) == said));
                if (result.Songs.Count > 0)
                {
                    result.Exact = true;
                    return result;
                }

                // every title holding all the words said
                string[] words = said.Split(' ');
                result.Songs.AddRange(songs.Where(s => { string title = Plain(s.Key.ToString()); return words.All(w => title.Contains(w)); })
                    .OrderBy(s => (s.Key.ToString() ?? string.Empty).Length).ThenBy(s => s.Key.ToString(), StringComparer.OrdinalIgnoreCase));
                if (result.Songs.Count > 0)
                    return result;

                // nothing: the closest title, if it shares a run of at least half of what was said (at least 3 letters)
                int best = 0;
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> s in songs)
                {
                    int score = AimsUtil.LongestCommonSubstring(Plain(s.Key.ToString()), said);
                    if (score > best)
                    {
                        best = score;
                        result.Suggestion = s.Key;
                    }
                }
                if (best < Math.Max(3, (said.Length + 1) / 2))
                    result.Suggestion = null;
                return result;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return result;
            }
        }

        // a title as it's compared: lower case, letters and digits, one space between words ("Golden (KPDH)" -> "golden kpdh")
        private static string Plain(string? text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in (text ?? string.Empty).ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            return string.Join(" ", sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// The library track a name names (FindMatches): its exact title, or the closest of the titles holding all its
        /// words. null: none.
        /// </summary>
        public static RolledUpSheetMusic? FindSong(string? title)
        {
            return FindEntry(title, out KeyValuePair<RolledUpSheetMusic, MusicInfo> entry) ? entry.Key : null;
        }

        /// <summary>
        /// FindSong with the track's library record. Returns true if any track matches, with entry set to the closest.
        /// </summary>
        public static bool FindEntry(string? title, out KeyValuePair<RolledUpSheetMusic, MusicInfo> entry)
        {
            SongMatches matches = FindMatches(title);
            entry = matches.Songs.Count > 0 ? matches.Songs[0] : default;
            return matches.Songs.Count > 0;
        }
        #endregion Finding a song

        #region Staff
        public static void Initialize()
        {
            // get staff 'get a copy' of rolled up sheet music
            CommandSystem.Register("DupeMusic", AccessLevel.Seer, new CommandEventHandler(DupeMusic_OnCommand));
        }

        [Usage("[DupeMusic <(partial) song text>")]
        [Description("Creates a copy of music box music. (not for sale or redistribution!)")]
        public static void DupeMusic_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e == null)
                    return;

                Mobile from = e.Mobile;
                if (from == null || from.Deleted)
                    return;

                string query = (e.ArgString ?? string.Empty).Trim();

                if (string.IsNullOrEmpty(query))
                {
                    from.SendMessage("Usage: [DupeMusic <(partial) song text>");
                    return;
                }

                SongMatches matches = FindMatches(query);
                RolledUpSheetMusic? rsm = matches.Songs.Count > 0 ? matches.Songs[0].Key : null;

                if (rsm == null || rsm.Deleted)
                {
                    from.SendMessage(string.Format("No songs matching '{0}' found.{1}", query,
                        matches.Suggestion != null ? string.Format(" Did you mean {0}?", matches.Suggestion) : ""));
                    return;
                }
                if (matches.Songs.Count > 1)
                    from.SendMessage(string.Format("{0} songs match '{1}': copying the closest. Say more of the title (or its serial) for another.",
                        matches.Songs.Count, query));

                Item? dupe = AimsHost.Current.Dupe(rsm);
                if (dupe == null || dupe.Deleted)
                {
                    from.SendMessage("Dupe failed.");
                    return;
                }

                // Mark staff owned (AimsHost: the shard may track staff copies) and place in backpack.
                from.AddToBackpack(dupe);
                from.SendMessage(string.Format("A copy of {0} has been placed in your backpack. Do not redistribute.", dupe));
                AimsHost.Current.MarkStaffOwned(from, dupe);
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        #endregion Staff
    }
}