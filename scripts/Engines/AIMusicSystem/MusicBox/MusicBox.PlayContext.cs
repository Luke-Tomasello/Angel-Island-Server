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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.PlayContext.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 10/29/26 to 11/5/26)
 *      PlayContext: the play list of a music box and its position. NowPlaying selects the track before it syncs the
 *      position, so Next and Previous continue from it. A pending track is in the play list of the one who submitted it.
 *      Split from MusicBox.cs; its history before 10/29/26 is in MusicBox.cs.
 */

using Server.Accounting;
using Server.Commands;
using Server.ContextMenus;
using Server.Diagnostics;
using Server.Engines;
using Server.Engines.DataRecorder;
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
        public class PlayContext
        {
            // Index into m_playList representing the next item to be played.
            // After Next() returns a track, _index points to the subsequent track.
            private int _index = 0;

            // The resolved play queue for this context (filtered from the global repository).
            private readonly List<RolledUpSheetMusic> m_playList = new List<RolledUpSheetMusic>();
            public List<RolledUpSheetMusic> Playlist => m_playList;

            // Tracks the user chose to ignore (excluded from the play queue).
            private List<RolledUpSheetMusic> m_ignoreList = new List<RolledUpSheetMusic>();
            public List<RolledUpSheetMusic> IgnoreList
            {
                get => m_ignoreList;
                set => m_ignoreList = value ?? new List<RolledUpSheetMusic>();
            }

            // Per-player notification queue (eg, "you sold music" events).
            private List<YouSoldMusic> m_playerNotificationList = new List<YouSoldMusic>();
            public List<YouSoldMusic> PlayerNotificationList
            {
                get => m_playerNotificationList;
                set => m_playerNotificationList = value ?? new List<YouSoldMusic>();
            }

            // Total items in the current play queue.
            public int Count => m_playList.Count;

            // Remaining items to be played (based on _index).
            public int QueueCount => Math.Max(0, m_playList.Count - _index);

            // A temporary copy of the currently playing track (so playback does not mutate the repo item).
            private RolledUpSheetMusic? m_nowPlaying = null;

            // If rsm is provided, selects that as now playing and syncs _index accordingly.
            public RolledUpSheetMusic NowPlaying(RolledUpSheetMusic? rsm = null)
            {
                if (rsm != null)
                {
                    // the selected track first: Sync() finds the track now playing
                    m_nowPlaying = SetNowPlaying(rsm);

                    int temp = Sync();
                    if (temp > -1)
                    {
                        // Sync our index to the selected track. _index is "next to play",
                        // so selected track is considered already consumed.
                        _index = temp + 1;
                    }
                }

                return m_nowPlaying;
            }

            private bool m_loop = false;
            public bool Loop
            {
                get => m_loop;
                set => m_loop = value;
            }

            // The player (or controlling mobile) for this play context.
            private readonly Mobile m_from;
            public Mobile Mobile => m_from;

            // Prev/Next and explicit song selection can disable autoplay.
            private bool m_autoPlay = true;
            public bool AutoPlay
            {
                get => m_autoPlay;
                set => m_autoPlay = value;
            }

            // The owning music box for policy checks (approved, access, etc).
            private readonly MusicBox? m_musicbox;

            public PlayContext(
                Mobile from,
                MusicBox musicbox,
                List<RolledUpSheetMusic> ignoreList,
                List<YouSoldMusic> playerNotificationList)
            {
                m_from = from;
                m_musicbox = musicbox;

                // Robustness: normalize null lists to empty lists.
                m_ignoreList = ignoreList ?? new List<RolledUpSheetMusic>();
                m_playerNotificationList = playerNotificationList ?? new List<YouSoldMusic>();

                LoadPlayQueue();
            }

            private RolledUpSheetMusic? SetNowPlaying(RolledUpSheetMusic? rsm)
            {
                try
                {
                    if (rsm != null)
                    {
                        // Make a temporary instance for playback (do not play directly off the repository item).
                        m_nowPlaying = (RolledUpSheetMusic)Utility.MakeTempItem(rsm);
                    }
                    else
                    {
                        m_nowPlaying = null;
                    }

                    return m_nowPlaying;
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    m_nowPlaying = null;
                    return null;
                }
            }

            public RolledUpSheetMusic Next()
            {
                try
                {
                    if (_index < 0)
                        _index = 0;

                    if (_index < m_playList.Count)
                        m_nowPlaying = SetNowPlaying(m_playList[_index++]);
                    else
                        m_nowPlaying = SetNowPlaying(null);

                    return m_nowPlaying;
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    return SetNowPlaying(null);
                }
            }

            public RolledUpSheetMusic? Prev()
            {
                try
                {
                    // _index points to "next item to play".
                    // To go back one item, we need to step back 2, then consume 1.
                    if (_index > 1 && m_playList.Count > 0)
                    {
                        int backIndex = _index - 2;
                        if (backIndex < 0)
                            backIndex = 0;

                        _index = backIndex + 1;
                        m_nowPlaying = SetNowPlaying(m_playList[backIndex]);
                    }
                    else
                    {
                        m_nowPlaying = SetNowPlaying(null);
                    }

                    return m_nowPlaying;
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    return SetNowPlaying(null);
                }
            }

            public RolledUpSheetMusic? PeekPrev()
            {
                try
                {
                    if (_index > 1)
                        return m_playList[_index - 2];

                    return null;
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    return null;
                }
            }

            public void Shuffle()
            {
                Utility.Shuffle(m_playList);

                int temp = Sync();
                if (temp > -1)
                {
                    // Keep _index consistent with whatever we consider "now playing" after shuffle.
                    _index = temp + 1;
                }
                else
                {
                    Rewind();
                }
            }

            // Returns the index of the "now playing" track within the playlist, or -1 if no match.
            private int Sync()
            {
                if (m_nowPlaying == null)
                    return -1;

                // CompareTo() == 0 is the canonical "same track" (the same song hash).
                return Playlist.FindIndex(a => a != null && a.CompareTo(m_nowPlaying) == 0);
            }

            public void Rewind()
            {
                _index = 0;
            }

            public void SeekEnd()
            {
                _index = Playlist.Count;
            }

            public void ResetPlayQueue()
            {
                Playlist.Clear();
                LoadPlayQueue();
                Rewind();
            }

            // Returns delta count (after - before). Rewinds if new items were added/removed.
            public int RefreshPlayQueue()
            {
                int before = Count;
                LoadPlayQueue();

                int delta = Count - before;
                if (delta != 0)
                    Rewind();

                return delta;
            }

            private void LoadPlayQueue()
            {
                try
                {
                    // Robustness: if critical dependencies are missing, do not attempt to build a queue.
                    if (m_musicbox == null || m_from == null)
                        return;

                    List<RolledUpSheetMusic> killList = new List<RolledUpSheetMusic>();
                    bool needsReset = false;

                    // First, clean up any invalid items already in the user's playlist.
                    if (Playlist.Count > 0)
                    {
                        foreach (RolledUpSheetMusic rsm in Playlist)
                        {
                            if (rsm == null)
                            {
                                killList.Add(rsm);
                                continue;
                            }

                            // Remove tracks that are deleted, missing from repo, not playable (unapproved, unless
                            //  pending and submitted by this player), or not accessible.
                            if (rsm.Deleted ||
                                !MusicLibrary.Repository.ContainsKey(rsm) ||
                                !m_musicbox.Playable(m_from, rsm) ||
                                (rsm.Napster && !m_musicbox.HasNapsterAccess(m_from, rsm)))
                            {
                                killList.Add(rsm);
                            }
                        }

                        foreach (RolledUpSheetMusic rsm in killList)
                        {
                            Playlist.Remove(rsm);
                            needsReset = true;

                            if (rsm != null && m_ignoreList != null && m_ignoreList.Contains(rsm))
                                m_ignoreList.Remove(rsm);
                        }
                    }

                    // Now reconcile against the global library: remove deleted/unapproved and add eligible tracks.
                    foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
                    {
                        RolledUpSheetMusic track = kvp.Key;

                        if (track == null)
                            continue;

                        // Remove deleted/unplayable tracks from local play/ignore lists.
                        if (track.Deleted || !m_musicbox.Playable(m_from, track))
                        {
                            if (m_playList.Contains(track))
                            {
                                m_playList.Remove(track);
                                needsReset = true;
                            }

                            if (m_ignoreList != null && m_ignoreList.Contains(track))
                                m_ignoreList.Remove(track);

                            continue;
                        }

                        // Add tracks the user can play:
                        // - Not deleted
                        // - Free OR purchased/owned
                        // - Approved (or pending and submitted by this player)
                        // - Not ignored
                        // - Napster requires Napster access
                        if (!track.Deleted &&
                            (IsFree(track) || m_musicbox.PurchasedOrOwned(m_from, track)) &&
                            m_musicbox.Playable(m_from, track))
                        {
                            if (!m_playList.Contains(track))
                            {
                                if (m_ignoreList == null || !m_ignoreList.Contains(track))
                                {
                                    if (!track.Napster || (track.Napster && m_musicbox.HasNapsterAccess(m_from, track)))
                                        m_playList.Add(track);
                                }
                            }
                        }
                    }

                    if (needsReset)
                        Rewind();
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }
        }
    }
}