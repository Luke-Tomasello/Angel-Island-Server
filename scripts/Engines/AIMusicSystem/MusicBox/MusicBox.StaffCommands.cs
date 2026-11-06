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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.StaffCommands.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 10/29/26 to 11/5/26)
 *      The staff and Test Center speech commands (flushall, review, approve, deny, delete, find, dupe), each its old
 *      case of the OnSpeech switch; the command table checks the staff level first. Review announces a pending track
 *      once (PlayMusic says "pending approval"). Flushall empties the review queue too.
 *      Split from MusicBox.Speech.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: Test Center and staff copies go through AimsHost.
 */

using Server.Misc;
using System;
using System.Collections.Generic;
using static Server.Items.MusicLibrary;

namespace Server.Items
{
    public partial class MusicBox
    {
        #region Staff commands
        private void HandleFlushAll(SpeechEventArgs e)
        {
            if (e.Mobile.AccessLevel == AccessLevel.Owner && AimsHost.Current.IsTestCenter)
            {
                // Capture keys first to avoid modifying repository while iterating.
                List<RolledUpSheetMusic> list = new List<RolledUpSheetMusic>();
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
                {
                    if (kvp.Key != null && !kvp.Key.Deleted)
                        list.Add(kvp.Key);
                }

                // Clear local context.
                if (m_playContext != null)
                {
                    m_playContext.Playlist.Clear();
                    m_playContext.Rewind();
                    m_playContext.NowPlaying(null);
                    m_playContext.IgnoreList.Clear();
                }

                // Clear global repo and its review queue (else the review queue keeps the deleted sheets), then delete
                //  the captured items.
                MusicLibrary.Repository.Clear();
                MusicLibrary.UnpublishedNeedsReview.Clear();

                foreach (RolledUpSheetMusic rsm in list)
                {
                    if (rsm != null && !rsm.Deleted)
                        rsm.Delete();
                }

                SendMessage(e.Mobile, "Done.");
            }
            else
            {
                SendMessage(e.Mobile, "Unauthorized.");
            }
        }

        private void HandleReview(SpeechEventArgs e)
        {
            if (MusicLibrary.UnpublishedNeedsReview.Count == 0)
            {
                SendMessage(e.Mobile, "There are no songs to review.");
                return;
            }

            RolledUpSheetMusic rsm = MusicLibrary.UnpublishedNeedsReview[0];
            if (rsm == null || rsm.Deleted)
            {
                // Defensive: purge bad entry.
                MusicLibrary.UnpublishedNeedsReview.RemoveAt(0);
                SendMessage(e.Mobile, "There are no songs to review.");
                return;
            }

            if (rsm.Owner == null)
                SendMessage(e.Mobile, 33, string.Format("Warning: {0} by {1} has no Owner.", rsm.Title, rsm.Author));

            if (Approved(rsm))  // an unapproved track (the usual case here) is announced by PlayMusic, as pending
                Say(string.Format("{0} by {1}.", rsm.Title, rsm.Author), hue: 54);

            if (m_playContext == null)
                return;

            PlayMusic(e.Mobile, m_playContext.NowPlaying(rsm));

            // Ensure we do not start playing the user's playlist after the review track.
            m_playContext.SeekEnd();

            SendMessage(e.Mobile, "Please type Approve, Deny or Delete:");
            e.Mobile.Prompt = new ReviewMusicBoxPrompt(e.Mobile, rsm, this);
        }

        private void HandleApprove(SpeechEventArgs e, string[] tokens)
        {
            if (tokens != null && tokens.Length == 3 && tokens[2] == "*")
            {
                // name approve *
                //  (over a copy: approving a remaster removes the track it remasters from the library)
                foreach (RolledUpSheetMusic track in new List<RolledUpSheetMusic>(MusicLibrary.Repository.Keys))
                {
                    if (track == null || track.Deleted || !MusicLibrary.Repository.ContainsKey(track))
                        continue;

                    if (!Approved(track))
                        ApproveTrack(e.Mobile, track);
                }
            }
            else
            {
                if (!TryFindSong(tokens, out RolledUpSheetMusic rsm, out _))
                {
                    Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
                }
                else
                {
                    if (rsm == null || rsm.Deleted)
                        return;

                    if (!Approved(rsm))
                    {
                        ApproveTrack(e.Mobile, rsm);
                    }
                    else
                    {
                        SendMessage(e.Mobile, "That track has already been approved.");
                    }
                }
            }
        }

        private void HandleDeny(SpeechEventArgs e, string[] tokens)
        {
            if (tokens != null && tokens.Length == 3 && tokens[2] == "*")
            {
                // name deny *
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
                {
                    if (kvp.Key == null)
                        continue;

                    SetFileStatus(e.Mobile, kvp.Key, SubmissionStatus.Rejected);

                    MusicLibrary.Unqueue(kvp.Key);
                }
            }
            else
            {
                if (!TryFindSong(tokens, out RolledUpSheetMusic rsm, out _))
                {
                    Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
                    return;
                }

                if (rsm == null)
                    return;

                SetFileStatus(e.Mobile, rsm, SubmissionStatus.Rejected);

                MusicLibrary.Unqueue(rsm);
            }
        }

        private void HandleDelete(SpeechEventArgs e, string[] tokens)
        {
            List<RolledUpSheetMusic> list = new List<RolledUpSheetMusic>();

            if (tokens != null && tokens.Length == 3 && tokens[2] == "*")
            {
                // name delete *
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
                {
                    if (kvp.Key != null)
                        list.Add(kvp.Key);
                }
            }
            else if (tokens != null &&
                     tokens.Length == 3 &&
                     tokens[2].Length > 2 &&
                     tokens[2].StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
            {
                int serial;
                if (Int32.TryParse(tokens[2].Substring(2), System.Globalization.NumberStyles.HexNumber, null, out serial))
                {
                    Item? item = World.FindItem(serial);
                    if (item is RolledUpSheetMusic rsm)
                        list.Add(rsm);
                    else
                        SendMessage(e.Mobile, string.Format("{0} is not the serial of any RolledUpSheetMusic.", tokens[2]));
                }
                else
                {
                    SendMessage(e.Mobile, string.Format("Unable to parse serial '{0}'.", tokens[2]));
                }
            }
            else
            {
                if (!TryFindSong(tokens, out RolledUpSheetMusic rsm, out _))
                {
                    Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
                    return;
                }

                if (rsm != null)
                    list.Add(rsm);
            }

            // Perform deletions.
            foreach (RolledUpSheetMusic rsm in list)
            {
                if (rsm == null)
                    continue;

                MusicLibrary.Unqueue(rsm);

                if (m_playContext != null && m_playContext.Playlist.Contains(rsm))
                    m_playContext.Playlist.Remove(rsm);

                if (MusicLibrary.Repository.ContainsKey(rsm))
                    MusicLibrary.Repository.Remove(rsm);

                if (!rsm.Deleted)
                    rsm.Delete();

                SendMessage(e.Mobile, string.Format("{0} deleted.", rsm));
            }
        }

        private void HandleFind(SpeechEventArgs e, string[] tokens)
        {
            string query = SongName(tokens);
            if (string.IsNullOrEmpty(query))
            {
                SendMessage(e.Mobile, "No song specified.");
                return;
            }

            string qLower = query.ToLowerInvariant();

            int count = 0;
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
            {
                RolledUpSheetMusic track = kvp.Key;
                if (track == null)
                    continue;

                string song = track.SongName ?? string.Empty;
                if (song.ToLowerInvariant().Contains(qLower))
                {
                    SendMessage(e.Mobile, string.Format(
                        "{0}, Owner: {1}, Serial: {2:X}, Hash Code: {3:X}",
                        track.Name,                                  // 0
                        track.Owner != null ? track.Owner : "(null)",// 1
                        track.Serial,                                // 2
                        track.HashCode));                            // 3

                    count++;
                }
            }

            if (count == 0)
                Say(string.Format("I couldn't find '{0}' in the music library", query), hue: 54);
        }

        private void HandleDupe(SpeechEventArgs e, string[] tokens)
        {
            string query = SongName(tokens);
            if (string.IsNullOrEmpty(query))
            {
                SendMessage(e.Mobile, "No song specified.");
                return;
            }

            string qLower = query.ToLowerInvariant();

            int count = 0;
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
            {
                RolledUpSheetMusic track = kvp.Key;
                if (track == null)
                    continue;

                string song = track.SongName ?? string.Empty;
                if (song.ToLowerInvariant().Contains(qLower))
                {
                    SendMessage(e.Mobile, string.Format(
                        "Duping {0}, Owner: {1}, Serial: {2:X}, Hash Code: {3:X}",
                        track.Name,                                  // 0
                        track.Owner != null ? track.Owner : "(null)",// 1
                        track.Serial,                                // 2
                        track.HashCode));                            // 3

                    Item? dupe = AimsHost.Current.Dupe(track);
                    if (dupe != null)
                    {
                        e.Mobile.AddToBackpack(dupe);

                        SendMessage(e.Mobile,
                            string.Format("a copy of {0} has been placed in your backpack. Do not redistribute.", dupe));

                        AimsHost.Current.MarkStaffOwned(e.Mobile, dupe);  // staff owned (the shard may track staff copies)
                    }

                    count++;
                }
            }

            if (count == 0)
                Say(string.Format("I couldn't find '{0}' in the music library", query), hue: 54);
        }
        #endregion Staff commands
    }
}