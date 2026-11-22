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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.StaffCommands.cs
 * Changelog
 *  11/16/26, Adam (find)
 *      'find' shows a track's About and Tags, when it has them (staff only).
 *  11/16/26, Adam (About and Tags)
 *      Review shows the reviewer a track's About and Tags, when it has them.
 *  11/14/26, Adam (arranged by)
 *      Review names a track's arranger, when it has one (By).
 *  11/14/26, Adam
 *      Approve, deny and delete name their song through PickSong and never guess: several matches are listed, with
 *      serials, and nothing is done; a name that matches nothing is said so (it matched any title sharing a letter or two:
 *      "delete clip", with the clip already gone, deleted Axel F). Approve and deny consider only songs waiting for
 *      review (deny rejected approved songs too). Deleting one song, by name or serial, asks first, naming it ("Type
 *      Delete"), as 'delete *' does (ConfirmAll's single).
 *  11/8/26, Adam (2026 modernization, 10/29/26 to 11/8/26)
 *      The staff and Test Center speech commands (flushall, review, approve, deny, delete, find, dupe), each its old
 *      case of the OnSpeech switch; the command table checks the staff level first. Review announces a pending track
 *      once (PlayMusic says "pending approval"). Flushall empties the review queue too.
 *      Split from MusicBox.Speech.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: Test Center and staff copies go through AimsHost.
 *      'find' adds the shard's note about each track (AimsHost.TrackNote; none by default). Its line is TrackLine, which
 *      'who' also sends to those who may use 'find'. The line names the song by its title (was "rolled up sheet music").
 *      'find' and 'dupe' search a track's title (the name list, play, who and announcements show), not SongName (the
 *      instrument's song name, from the manuscript book's title or a "song:" line in the notes), which can differ: then
 *      'find <the title you see>' missed the track. Dupe's line names the track by its title too.
 *      'approve *' and 'deny *' act only on the songs waiting for review ('deny *' rejected every song in the library,
 *      approved ones too; 'approve *' approved rejected songs too); 'delete *' is for Test Center only (as flushall).
 *      Each says how many songs and acts only when staff type Approve, Deny or Delete (Esc cancels): ConfirmAll.
 */

using Server.Misc;
using Server.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
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

            // its About and Tags, for the reviewer to check (players see the About; the tags are only searched)
            if (rsm.About.Length > 0 || rsm.Tags.Length > 0)
                SendMessage(e.Mobile, 54, string.Format("About: {0}. Tags: {1}.", rsm.About.Length > 0 ? rsm.About : "none",
                    rsm.Tags.Length > 0 ? rsm.Tags : "none"));

            if (Approved(rsm))  // an unapproved track (the usual case here) is announced by PlayMusic, as pending
                Say(string.Format("{0} by {1}.", rsm.Title, By(rsm)), hue: 54);

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
                // name approve *: every song waiting for review, once staff type Approve
                //  (over a copy: approving a remaster removes the track it remasters from the library)
                ConfirmAll(e.Mobile, "Approve", "approved", PendingTracks(), (from, tracks) =>
                {
                    foreach (RolledUpSheetMusic track in tracks)
                        if (IsPending(track))
                            ApproveTrack(from, track);
                });
            }
            else
            {
                // only songs waiting for review, and never a guess
                if (!PickSong(e.Mobile, tokens, "approve", mustBeOne: true, (s, i) => i.Status == SubmissionStatus.Pending,
                    out RolledUpSheetMusic rsm, out _, none: "No song waiting for review is called '{0}'."))
                    return;     // PickSong said why

                if (rsm == null || rsm.Deleted)
                    return;
                ApproveTrack(e.Mobile, rsm);
            }
        }

        private void HandleDeny(SpeechEventArgs e, string[] tokens)
        {
            if (tokens != null && tokens.Length == 3 && tokens[2] == "*")
            {
                // name deny *: every song waiting for review, once staff type Deny (it rejected every song in the
                //  library, approved ones too)
                ConfirmAll(e.Mobile, "Deny", "denied", PendingTracks(), (from, tracks) =>
                {
                    foreach (RolledUpSheetMusic track in tracks)
                        if (IsPending(track))
                        {
                            SetFileStatus(from, track, SubmissionStatus.Rejected);
                            MusicLibrary.Unqueue(track);
                        }
                });
            }
            else
            {
                // only songs waiting for review (it rejected approved songs too), and never a guess
                if (!PickSong(e.Mobile, tokens, "deny", mustBeOne: true, (s, i) => i.Status == SubmissionStatus.Pending,
                    out RolledUpSheetMusic rsm, out _, none: "No song waiting for review is called '{0}'."))
                    return;     // PickSong said why

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
                // name delete *: the whole library, on Test Center only (as flushall), once staff type Delete
                if (!AimsHost.Current.IsTestCenter)
                {
                    SendMessage(e.Mobile, "'delete *' is for Test Center only. Here, delete songs one at a time.");
                    return;
                }
                foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
                {
                    if (kvp.Key != null)
                        list.Add(kvp.Key);
                }
                ConfirmAll(e.Mobile, "Delete", "deleted", list, (from, tracks) =>
                {
                    if (AimsHost.Current.IsTestCenter)
                        DeleteTracks(from, tracks);
                });
                return;
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
                // never a guess
                if (!PickSong(e.Mobile, tokens, "delete", mustBeOne: true, null, out RolledUpSheetMusic rsm, out _))
                    return;     // PickSong said why

                if (rsm != null)
                    list.Add(rsm);
            }

            // and only once staff confirm, naming the song (Esc, or anything but Delete, cancels)
            if (list.Count == 0)
                return;
            RolledUpSheetMusic one = list[0];
            ConfirmAll(e.Mobile, "Delete", "deleted", list, (from, tracks) => DeleteTracks(from, tracks.Where(t => t != null && !t.Deleted).ToList()),
                single: string.Format("'{0}' by {1} (0x{2:X})", one, one.Author, one.Serial.Value));
        }

        private void DeleteTracks(Mobile from, List<RolledUpSheetMusic> list)
        {
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

                SendMessage(from, string.Format("{0} deleted.", rsm));
            }
        }

        // the songs waiting for review (approve *, deny *)
        private static List<RolledUpSheetMusic> PendingTracks()
        {
            List<RolledUpSheetMusic> list = new List<RolledUpSheetMusic>();
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
                if (IsPending(kvp.Key))
                    list.Add(kvp.Key);
            return list;
        }

        private static bool IsPending(RolledUpSheetMusic? track)
        {
            return track != null && !track.Deleted && MusicLibrary.Repository.TryGetValue(track, out MusicInfo? info)
                && info != null && info.Status == SubmissionStatus.Pending;
        }

        // 'approve *', 'deny *', 'delete *' (and deleting one song: 'single' names it): says how many songs, and acts only
        //  when staff type the command's word (Esc, or anything else, cancels). The act checks each song again: things
        //  may change while staff type.
        private void ConfirmAll(Mobile from, string verb, string done, List<RolledUpSheetMusic> tracks,
            Action<Mobile, List<RolledUpSheetMusic>> act, string? single = null)
        {
            bool all = verb == "Delete";
            if (tracks.Count == 0)
            {
                SendMessage(from, all ? "There are no songs in the music library." : "There are no songs waiting for review.");
                return;
            }
            if (single != null)
                SendMessage(from, string.Format("You are about to {0} {1}. Type {2} to continue, or Esc to cancel.",
                    verb.ToLowerInvariant(), single, verb));
            else
                SendMessage(from, string.Format("You are about to {0} {1} song{2}{3}. Type {4} to continue, or Esc to cancel.",
                    verb.ToLowerInvariant(), tracks.Count, tracks.Count == 1 ? "" : "s",
                    all ? " (every song in the music library)" : " waiting for review", verb));
            from.Prompt = new ConfirmAllPrompt(this, verb, done, tracks, act);
        }

        private class ConfirmAllPrompt : Prompt
        {
            private readonly MusicBox m_box;
            private readonly string m_verb, m_done;
            private readonly List<RolledUpSheetMusic> m_tracks;
            private readonly Action<Mobile, List<RolledUpSheetMusic>> m_act;

            public ConfirmAllPrompt(MusicBox box, string verb, string done, List<RolledUpSheetMusic> tracks,
                Action<Mobile, List<RolledUpSheetMusic>> act)
            {
                m_box = box;
                m_verb = verb;
                m_done = done;
                m_tracks = tracks;
                m_act = act;
            }

            public override void OnResponse(Mobile from, string text)
            {
                try
                {
                    if (from == null || m_box == null || m_box.Deleted)
                        return;
                    if (!string.Equals((text ?? "").Trim(), m_verb, StringComparison.OrdinalIgnoreCase))
                    {
                        from.SendMessage("Cancelled: nothing was {0}.", m_done);
                        return;
                    }
                    m_act(from, m_tracks);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }
            }

            public override void OnCancel(Mobile from)
            {
                from?.SendMessage("Cancelled: nothing was {0}.", m_done);
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

                if (ShownTitle(track).ToLowerInvariant().Contains(qLower))
                {
                    SendMessage(e.Mobile, TrackLine(e.Mobile, track, kvp.Value));
                    count++;
                }
            }

            if (count == 0)
                Say(string.Format("I couldn't find '{0}' in the music library", query), hue: 54);
        }

        // a library track as 'find' shows it (also 'who', to those who may use 'find'): title, owner, serial, hash code,
        //  and the shard's note (AimsHost.TrackNote)
        private static string TrackLine(Mobile viewer, RolledUpSheetMusic track, MusicInfo? info)
        {
            string? note = AimsHost.Current.TrackNote(viewer, track, info);
            return string.Format("{0}, Owner: {1}, Serial: {2:X}, Hash Code: {3:X}{4}{5}{6}",
                ShownTitle(track),                                  // 0
                track.Owner != null ? track.Owner : "(null)",       // 1
                track.Serial,                                       // 2
                track.HashCode,                                     // 3
                string.IsNullOrEmpty(note) ? "" : ", " + note,      // 4
                track.About.Length > 0 ? ", About: " + track.About : "",    // 5 (staff only: players see the About on the sheet)
                track.Tags.Length > 0 ? ", Tags: " + track.Tags : "");      // 6 (staff only: players never see tags)
        }

        // the name a track goes by everywhere (list, play, who, announcements): its title, else the item's name. find and
        //  dupe search it, so staff find a track by the name they see (they searched SongName, which can differ).
        private static string ShownTitle(RolledUpSheetMusic track)
        {
            return !string.IsNullOrEmpty(track.Title) ? track.Title : track.Name ?? string.Empty;
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

                if (ShownTitle(track).ToLowerInvariant().Contains(qLower))
                {
                    SendMessage(e.Mobile, string.Format(
                        "Duping {0}, Owner: {1}, Serial: {2:X}, Hash Code: {3:X}",
                        ShownTitle(track),                           // 0
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