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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.CommandHelpers.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 10/29/26 to 11/5/26)
 *      Helpers for the speech commands: filters, CompositeList, rights and approval, stopping and samples, submission
 *      status, SongName. Approving a remaster deletes the track it replaces (PatchV262 deletes the ones left before);
 *      a Napster track's composer has access to it. Playable: a pending track is playable by the one who submitted it.
 *      The library hash includes each sheet's serial: a new sheet of the same song (flushall, then added again) is a change.
 *      Split from MusicBox.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: Music Karma.
 */

using Server.Accounting;
using Server.Commands;
using Server.ContextMenus;
using Server.Diagnostics;
using Server.Engines;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Prompts;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static Server.Items.MusicBox;
using static Server.Items.MusicLibrary;

namespace Server.Items
{
    public partial class MusicBox
    {
        #region Text Interface
        [Flags]
        protected enum Filter
        {
            None,
            New,
            Remastered,
        }
        private Filter m_Filter = Filter.None;  // not serialized
        private Filter SetFilter(Filter flags = Filter.None, bool value = true)
        {
            m_Filter = Filter.None;
            if (value)
                m_Filter |= flags;
            else
                m_Filter &= ~flags;

            return m_Filter;
        }
        private bool GetFilter(Filter flag)
        {
            return ((m_Filter & flag) != 0);
        }
        private List<KeyValuePair<int, string>> CompositeList(Mobile from, string[] tokens, Filter filter = Filter.None)
        {
            string author = string.Empty;
            if (tokens.Length >= 4)
            {
                if (tokens[2].ToLower() == "by")
                {
                    string temp = string.Empty;
                    for (int ix = 3; ix < tokens.Length; ix++)
                    {
                        temp += tokens[ix] + " ";
                    }
                    author = temp.Trim();
                }
            }
            bool admin = from.AccessLevel > AccessLevel.Player;
            List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
            {
                int color =
                    PendingReview(kvp.Key) ||
                    (!PurchasedOrOwned(from, kvp.Key) && (kvp.Value.Price > 0)) ||
                    kvp.Key.Napster && !HasNapsterAccess(from, kvp.Key)
                    ? 33 : 54;

                string text = string.Format("{0} by {1}{2}{3}{4}{5}{6}{7}",
                        kvp.Key,                                                            // 0
                        kvp.Key.Author,                                                     // 1
                        m_playContext.IgnoreList.Contains(kvp.Key) ? " (ignored)" : "",     // 2
                        Rejected(kvp.Key) ? " (Rejected)" : "",                             // 3
                        PendingReview(kvp.Key) ? " (Pending)" : "",                         // 4
                        kvp.Key.Napster ? " (Napster)" : "",                                // 5
                        (kvp.Value.Price > 0) ? " (Premium)" : "",                          // 6
                        kvp.Value.Remastered ? " (Remastered)" : ""                          // 7
                    );

                // filter by date approved for new tracks
                bool ok = !GetFilter(Filter.New) || (DateTime.UtcNow - kvp.Value.DateApproved).TotalDays < 8;
                // filter by date remastered for remastered tracks
                if (!ok) ok = !GetFilter(Filter.Remastered) || kvp.Value.Remastered && (DateTime.UtcNow - kvp.Value.DateRemastered).TotalDays < 8;
                if (ok)
                {
                    // only list 'rejected' tracks for staff
                    if (admin && Rejected(kvp.Key) || !Rejected(kvp.Key))
                        if (author == string.Empty)
                            list.Add(new KeyValuePair<int, string>(color, text));
                        else if (kvp.Key.Author.ToLower().Contains(author))
                            list.Add(new KeyValuePair<int, string>(color, text));
                }
            }

            // order the list so that 'owned' (color 54) are at the top
            list.Sort((e1, e2) =>
            {
                return e2.Key.CompareTo(e1.Key);
            });

            return list;
        }
        private static void AwardSkillFameKarma(PlayerMobile pm, double skill_points)
        {
            int fameBump = 0;
            int karmaBump = 0;
            int fameOld = pm.Fame;
            if (skill_points == PublishFactor)
            {   // got a track approved. Good job!
                fameBump = 14000;                               // Nightmare level
                karmaBump = -14000;                             //
            }
            else if (skill_points == SalesFactor)
            {   // award points for selling a track
                fameBump = 8000;                                // lich level
                karmaBump = -8000;                              //
            }
            else if (skill_points == PlayFactor)
            {   // award points for having your track played
                fameBump = 5000;                                // BronzeElemental level
                karmaBump = -5000;                              //
            }
            else
            {
                AimsLog.WriteLine("Error: invalid skill points in CoreMusicPlayer Box.", ConsoleColor.Red);
                return;
            }

            int totalFame = fameBump / 100;
            int totalKarma = -karmaBump / 100;
            totalFame += ((totalFame / 10) * 3);
            totalKarma += ((totalKarma / 10) * 3);

            Titles.AwardFame(pm, totalFame, true);
            Titles.AwardKarma(pm, totalKarma, true);
            // skill gain
            AimsHost.Current.SetMusicKarma(pm, AimsHost.Current.GetMusicKarma(pm) + skill_points);              // skill points
            pm.OnFameChange(fameOld);                       // old fame
            Commands.SystemMusicPlayer.OnSkillChange(pm, skill_points);
        }
        private string[] PreprocessTokens(string[] tokens)
        {
            List<string> list = new();
            if (tokens.Length >= 3)
            {   // collapse to a single command
                if (tokens[1] == "new" && (tokens[2] == "releases" || tokens[2] == "music"))
                {
                    list.Add(tokens[0]);                        // music box name
                    list.Add("newreleases");                    // new releases => newreleases
                    for (int ix = 3; ix < tokens.Length; ix++)  // add the rest of the command line
                        list.Add(tokens[ix]);
                }
                else list = new List<string>(tokens);
            }
            else list = new List<string>(tokens);
            return list.ToArray();
        }
        public bool PurchasedOrOwned(Mobile m, RolledUpSheetMusic rsm)
        {
            if (CheckRights(m, message: false) != Rights.okay)
                return false;
            else if (CheckEmbedded())
                return true;

            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
                if (kvp.Key == rsm)
                    if (kvp.Key.Owner == m)
                        return true;            // owner/composer
                    else
                        foreach (PurchaserInfo lml in kvp.Value.Purchasers)
                            if (lml.Purchaser == m)
                                return true;    // bought it
            return false;
        }
        public bool HasNapsterAccess(Mobile m, RolledUpSheetMusic rsm)
        {
            if (MusicLibrary.Repository.ContainsKey(rsm))
            {
                if (rsm.Napster)
                {
                    if (CheckRights(m, message: false) != Rights.okay)
                        return false;
                    else if (CheckEmbedded())
                        return true;
                    else if (rsm.Owner == m)
                        return true;    // the composer

                    foreach (PurchaserInfo lml in MusicLibrary.Repository[rsm].Purchasers)
                        if (lml.Purchaser == m)
                            return true;    // was given this Napster track
                }
            }

            return false;
        }
        private void SetFileStatus(Mobile m, RolledUpSheetMusic rsm, SubmissionStatus status)
        {
            if (MusicLibrary.Repository.ContainsKey(rsm) && MusicLibrary.Repository[rsm].Status != status)
            {
                MusicLibrary.Repository[rsm].Status = status;
                MusicLibrary.Repository[rsm].Reason = RejectionReason.None;
            }

            if (rsm != null)
                m.SendMessage("{0} {1}.", rsm, status.ToString());
        }
        private int GetGlobalRepositoryHash()
        {   // detect changes in the MusicLibrary.Repository
            // We currently detect:
            //  tracks added or removed - kvp.Key.Serial (a new sheet of the same song has the same GetHashCode)
            //  the song itself changed - kvp.Key.GetHashCode() (the composition's hash)
            //  status change on a track - kvp.Value.Status.GetHashCode()
            //  and if the track has been deleted - kvp.Key.Deleted.GetHashCode()
            int hash = MusicLibrary.Repository.GetHashCode();
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
            {
                // if any of these things change, then the library has changed and the user's music box must resync
                hash = hash * 23 + kvp.Key.Serial.Value;
                hash = hash * 23 + kvp.Key.GetHashCode();
                hash = hash * 23 + kvp.Value.Status.GetHashCode();
                hash = hash * 23 + kvp.Key.Deleted.GetHashCode();
            }
            // unreliable
            // hash_code ^= hash_code ^ kvp.Key.GetHashCode() ^ kvp.Value.Status.GetHashCode() ^ kvp.Key.Deleted.GetHashCode();
            // https://stackoverflow.com/questions/19139240/why-is-the-xor-operator-used-in-computing-hash-code?lq=1
            // prefer
            // https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode/263416#263416

            return hash;
        }
        // Approves a submitted track (the review prompt, 'approve <song>' and 'approve *'): either a remaster of an
        //  approved track with the same TrackID, which takes over that track's MusicInfo (sales, purchasers, approval
        //  date), or a first approval (dated for "new releases", publishing points for the Bards Guild).
        private static void ApproveTrack(Mobile from, RolledUpSheetMusic rsm)
        {
            int trackID = rsm.TrackID;

            RolledUpSheetMusic? remasterTarget = null;
            MusicInfo? remasterInfo = null;

            // Find an approved track with the same TrackID.
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
            {
                RolledUpSheetMusic existing = kvp.Key;
                MusicInfo info = kvp.Value;

                if (existing == null || info == null || ReferenceEquals(existing, rsm))
                    continue;

                if (info.Status == SubmissionStatus.Approved && existing.TrackID == trackID)
                {
                    remasterTarget = existing;
                    remasterInfo = info;
                    break;
                }
            }

            if (remasterTarget != null && remasterInfo != null)
            {
                // Replace old approved copy with the remaster while preserving MusicInfo.
                // Note: Remove old key before assigning new key to avoid duplicate TrackID entry.
                if (MusicLibrary.Repository.ContainsKey(remasterTarget))
                    MusicLibrary.Repository.Remove(remasterTarget);

                // The replaced track is deleted (it was left in internal storage). Only from internal storage, where
                //  submitted tracks are kept: anything elsewhere is not ours to delete.
                if (!remasterTarget.Deleted && remasterTarget.Map == Map.Internal && remasterTarget.Parent == null)
                    remasterTarget.Delete();

                MusicLibrary.Repository[rsm] = remasterInfo;

                // Mark remaster metadata.
                MusicLibrary.Repository[rsm].DateRemastered = DateTime.UtcNow;
                MusicLibrary.Repository[rsm].Remastered = true;

                MusicLibrary.Unqueue(rsm);

                // No points and do not alter original publish date for remasters (per original intent).
                from.SendMessage("You approve the remastered track {0}.", rsm);
            }
            else
            {
                // First-time approval: ensure the track exists in the global repo and mark approved.
                if (!MusicLibrary.Repository.ContainsKey(rsm))
                    MusicLibrary.Repository.Add(rsm, new MusicInfo(rsm.Owner, rsm.Price));

                MusicLibrary.Repository[rsm].Status = SubmissionStatus.Approved;
                MusicLibrary.Repository[rsm].Reason = RejectionReason.None;

                MusicLibrary.Unqueue(rsm);

                // Award points for getting a track approved (Bards Guild only).
                if (rsm.Owner is PlayerMobile pm && pm.NpcGuild == NpcGuild.BardsGuild)
                    AwardSkillFameKarma(pm, PublishFactor);

                // Used for "new releases" queries.
                MusicLibrary.Repository[rsm].DateApproved = DateTime.UtcNow;

                from.SendMessage("You approve the track {0}.", rsm);
            }
        }
        /// <summary>
        /// Determines whether this MusicBox device is currently playing a track
        /// in its active PlayContext.
        /// 
        /// Defensive rules:
        /// - m_playContext must exist
        /// - PlayContext.Mobile must exist
        /// - NowPlaying() must return a non-null track
        /// - Track must report that it is playing for this specific mobile
        /// 
        /// This method does NOT assume global playback state — it is strictly
        /// device/context scoped.
        /// </summary>
        private bool IsThisDevicePlaying()
        {
            try
            {
                // No context means nothing can be playing.
                if (m_playContext == null)
                    return false;

                // No associated mobile means playback cannot be validated.
                if (m_playContext.Mobile == null)
                    return false;

                // Capture NowPlaying() once to avoid double invocation and
                // potential state mutation or race inconsistencies.
                RolledUpSheetMusic current = m_playContext.NowPlaying();

                if (current == null || current.Deleted)
                    return false;

                // Delegate to the track-level playback state check.
                return current.IsThisDevicePlaying(m_playContext.Mobile);
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false; // Fail closed.
            }
        }
        /// <summary>
        /// Stops playback on this device.
        /// 
        /// Behavior:
        /// - If something is currently playing in this PlayContext, stop it.
        /// - Optionally reset the playlist (default: true).
        /// 
        /// preservePlaylist = false:
        ///     Fully resets the queue so a subsequent Play starts clean.
        /// preservePlaylist = true:
        ///     Leaves the queue intact (used for Next/Previous/Repeat flows).
        /// </summary>
        private void StopPlaying(bool preservePlaylist = false)
        {
            try
            {
                // No context means nothing to stop.
                if (m_playContext == null)
                    return;

                // Capture mobile and track once to avoid repeated calls and race inconsistencies.
                Mobile mobile = m_playContext.Mobile;
                RolledUpSheetMusic current = m_playContext.NowPlaying();

                // Only attempt to stop if this device is actually playing.
                if (mobile != null && current != null && IsThisDevicePlaying())
                {
                    // Resume first: a paused song is unpaused before it is ended, so it ends in a consistent state.
                    if (!current.Deleted)
                        current.Resume(mobile);

                    // EndMusic handles actual teardown of playback state.
                    this.EndMusic(mobile);
                }

                // Reset the play queue unless explicitly preserving it.
                // This is necessary when issuing a fresh Play command.
                if (!preservePlaylist)
                {
                    m_playContext.ResetPlayQueue();
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        /// <summary>
        /// Timer callback used to terminate a preview/sample playback.
        /// 
        /// Expected state payload:
        ///     object[] { MusicBox musicbox, Mobile from }
        /// 
        /// Defensive rules:
        /// - Validate state object and shape
        /// - Validate types
        /// - Fail closed if malformed
        /// </summary>
        private void EndSample(object state)
        {
            try
            {
                // Validate state container.
                if (state is not object[] aState || aState.Length < 2)
                    return;

                // Extract and validate typed entries.
                MusicBox? musicbox = aState[0] as MusicBox;
                Mobile? from = aState[1] as Mobile;

                if (musicbox == null || from == null)
                    return;

                // If device is already deleted or mobile invalid, do nothing.
                if (musicbox.Deleted || from.Deleted)
                    return;

                // Only terminate if this device is actually playing for this mobile.
                if (musicbox.IsThisDevicePlaying())
                {
                    musicbox.EndMusic(from);
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        // A track is free when its library price is 0 (maintenance sets it to 0 when the composer is gone); the sheet's
        //  own Price is only what it was submitted with.
        private static bool IsFree(RolledUpSheetMusic track)
        {
            return MusicLibrary.Repository.TryGetValue(track, out MusicInfo? info) && info != null ? info.Price == 0 : track.Price == 0;
        }
        private bool Approved(RolledUpSheetMusic rsm)
        {
            return Status(rsm, SubmissionStatus.Approved);
        }
        private bool Rejected(RolledUpSheetMusic rsm)
        {
            return Status(rsm, SubmissionStatus.Rejected);
        }
        private bool PendingReview(RolledUpSheetMusic rsm)
        {
            return Status(rsm, SubmissionStatus.Pending);
        }
        // A track m may have in their play list: approved, or pending and m submitted it (the sheet's Owner, the character
        //  who made it, not the Author text). A rejected track is not. Staff hear any track by title (HandlePlay).
        private bool Playable(Mobile m, RolledUpSheetMusic rsm)
        {
            return Approved(rsm) || (PendingReview(rsm) && rsm.Owner != null && rsm.Owner == m);
        }
        /// <summary>
        /// Determines whether the specified RolledUpSheetMusic exists in the
        /// MusicLibrary.Repository and matches the supplied SubmissionStatus.
        /// 
        /// Defensive rules:
        /// - rsm must not be null or deleted
        /// - MusicInfo entry must exist
        /// - Fail closed on any inconsistency
        /// </summary>
        private bool Status(RolledUpSheetMusic rsm, SubmissionStatus status)
        {
            try
            {
                // Basic validation.
                if (rsm == null || rsm.Deleted)
                    return false;

                // Use TryGetValue to avoid double dictionary lookup and potential race issues.
                if (!MusicLibrary.Repository.TryGetValue(rsm, out MusicInfo? info) || info == null)
                    return false;

                return info.Status == status;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false; // Fail closed.
            }
        }
        /// <summary>
        /// Extracts the song title portion from tokenized speech.
        /// 
        /// Expected format:
        ///     [name] [command] [song words...]
        /// 
        /// Returns:
        ///     Empty string if insufficient tokens.
        /// 
        /// Defensive rules:
        /// - tokens must not be null
        /// - Ignore null/empty token entries
        /// - Avoid repeated string concatenation (use StringBuilder)
        /// </summary>
        private static string SongName(string[]? tokens)
        {
            try
            {
                if (tokens == null || tokens.Length < 3)
                    return string.Empty;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                for (int ix = 2; ix < tokens.Length; ix++)
                {
                    if (!string.IsNullOrWhiteSpace(tokens[ix]))
                    {
                        sb.Append(tokens[ix]);
                        sb.Append(' ');
                    }
                }

                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return string.Empty; // Fail closed.
            }
        }
        #endregion Text Interface
    }
}