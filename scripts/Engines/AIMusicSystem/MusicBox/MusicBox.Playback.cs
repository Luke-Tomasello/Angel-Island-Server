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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.Playback.cs
 * Changelog
 *  11/6/26, Adam (2026 modernization, 10/29/26 to 11/6/26)
 *      Playing: rights, double-click, PlayMusic, play counts, the end of a song, continuing play and status checks.
 *      An unapproved track is announced as pending (or rejected) when it starts, and earns no play credit. A song's end
 *      deletes the box's temporary copy it played on; so do a new listener's context and the box's deletion.
 *      Split from MusicBox.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: staff messages, households (Angel Island: accounts shared by IP) and the old settings' placeholder
 *      owner.
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
        private PlayContext m_playContext = null;
        public void RefreshPlayContext(Mobile from)
        {
            try
            {
                // The speaker plays from their own profile (a hired minstrel relays its master's speech as its own,
                //  so the minstrel plays from its own: see MusicBox.Placement.cs).
                Mobile actor = from;

                // Robustness: cannot establish a context without a valid mobile.
                if (actor == null)
                    return;

                // Defensive: Profiles should exist, but fail-closed if it does not.
                if (Profiles == null)
                    return;

                // If there is no context yet, or the context is for a different mobile, rebuild it.
                if (m_playContext == null || m_playContext.Mobile != actor)
                {
                    // the old context's copy goes with it (now, or when its song ends)
                    m_playContext?.ReleaseNowPlaying();

                    // ** 1.0 musicboxes patch **
                    //
                    // Older 1.0 musicboxes had a shared notion of IgnoreList and PlayerNotificationList.
                    // That state has been moved into each user's Profiles entry.
                    //
                    // To migrate legacy saves:
                    // - On deserialize, the shared config was attached to World.GetSystemAcct() as a placeholder.
                    // - On first real use, we move that config from the system account to the first accessing player.
                    //
                    // This is a one-time opportunistic migration (best-effort).
                    Mobile? system = AimsHost.Current.SystemMobile;

                    if (system != null && Profiles.ContainsKey(system))
                    {
                        // One-time patch: move the placeholder config to the first real actor.
                        MusicBoxProfile mc = Profiles[system];

                        // Remove placeholder entry first.
                        Profiles.Remove(system);

                        // If an entry already exists for this actor, do not overwrite it; preserve the actor's existing config.
                        // Otherwise, attach the migrated config.
                        if (!Profiles.ContainsKey(actor))
                            Profiles.Add(actor, mc);
                        else
                            mc = Profiles[actor]; // prefer existing if present

                        // Build a new PlayContext using the actor's config lists (null-safe).
                        List<RolledUpSheetMusic> ignore = mc?.IgnoreList ?? new List<RolledUpSheetMusic>();
                        List<YouSoldMusic> notif = mc?.PlayerNotificationList ?? new List<YouSoldMusic>();

                        m_playContext = new PlayContext(actor, this, ignore, notif);
                    }
                    // Normal case: actor already has a Profiles entry.
                    else if (Profiles.TryGetValue(actor, out MusicBoxProfile? ctx) && ctx != null)
                    {
                        List<RolledUpSheetMusic> ignore = ctx.IgnoreList ?? new List<RolledUpSheetMusic>();
                        List<YouSoldMusic> notif = ctx.PlayerNotificationList ?? new List<YouSoldMusic>();

                        m_playContext = new PlayContext(actor, this, ignore, notif);
                    }
                    // First-time user: create a new per-user config and context.
                    else
                    {
                        List<RolledUpSheetMusic> ignore = new List<RolledUpSheetMusic>();
                        List<YouSoldMusic> notif = new List<YouSoldMusic>();

                        m_playContext = new PlayContext(actor, this, ignore, notif);

                        // Store the config so future accesses reuse the same lists.
                        // Do not assume Add will succeed if key exists; prefer assignment.
                        Profiles[actor] = new MusicBoxProfile(ignore, notif);
                    }
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            // the box is gone: so is its temporary copy (now, or when its song ends)
            m_playContext?.ReleaseNowPlaying();
        }
        public override void OnDoubleClick(Mobile from)
        {
            try
            {
                // Robustness: no mobile, no action.
                if (from == null)
                    return;

                // Establish a valid PlayContext for the resolved actor (PlayMobile or from).
                // NOTE: RefreshPlayContext() resolves actor internally and can update m_playContext.
                RefreshPlayContext(from);

                // Robustness: if context creation failed for any reason, fail closed.
                if (m_playContext == null)
                    return;

                // If the global repository changed since last check, refresh the play queue to reflect new/removed tracks.
                if (GlobalRepositoryChanged())
                {
                    // RefreshPlayQueue can throw if downstream lists/repo are inconsistent; keep it contained.
                    m_playContext.RefreshPlayQueue();
                }

                // Rights check (house locked-down or approved mobile-context rules).
                if (CheckRights(from, message: true) != Rights.okay)
                    return;

                // If the user's queue is empty and we are currently playing, stop playback.
                // This handles the case where the repository changed and removed all eligible tracks mid-play.
                if (m_playContext.Count == 0)
                {
                    if (IsThisDevicePlaying())
                    {
                        EndMusic(from);
                        return;
                    }

                    SendMessage(from, "Your music box is empty.");
                    return;
                }

                // Toggle behavior: if already playing, double-click stops playback.
                if (IsThisDevicePlaying())
                {
                    EndMusic(from);
                    return;
                }

                // Enforce "one active play context" per user.
                if (PlayingElsewhere(from))
                {
                    SendMessage(from, "You have initiated music in another context. You will need to cancel that or wait for it to finish.");

                    if (Core.Debug)
                        AimsHost.Current.SendSystemMessage(from, 5, "Debug message: music not started because this player has initiated music elsewhere.");

                    return;
                }

                // Start playback:
                // - Ensure autoplay is enabled (Prev/Next/manual song selection can disable it elsewhere).
                // - Rewind so playback begins from the start of the resolved queue.
                m_playContext.AutoPlay = true;
                m_playContext.Rewind();

                // Select the first track and begin playback.
                RolledUpSheetMusic next = m_playContext.Next();

                // Robustness: if Next() returned null despite Count > 0 (index skew, corrupted list),
                // fail gracefully rather than throwing inside PlayMusic().
                if (next == null)
                {
                    SendMessage(from, "Your music box is empty.");
                    return;
                }

                PlayMusic(from, next);
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        // detects changes to the global repository
        private int m_globalRepositoryHash = 0;
        public void PlayMusic(Mobile m, RolledUpSheetMusic rsm)
        {
            try
            {
                if (rsm == null)
                    return;

                // If the caller mobile is missing/deleted, do not attempt to play.
                if (m == null || m.Deleted)
                    return;

                // An unapproved track (its submitter's pending track, or staff listening) says so to everyone in earshot.
                if (!Approved(rsm))
                    Say(string.Format("{0} by {1} ({2}).", rsm, rsm.Author, Rejected(rsm) ? "rejected" : "pending approval"), hue: 54);

                // Try to animate, but do not let animation failure break playback.
                try
                {
                    Animate();
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }

                // Wire the status check (the performance keeps it). The end of the song is the performance's
                //  Completed (SongCompleted), raised once.
                rsm.CheckSourceStatus = new RazorInstrument.CheckSourceStatusCallback(CheckMobileStatus);

                // Default instrument is a harp (self-referential in this implementation).
                rsm.Instrument = rsm;

                // Item that sings (possibly obsolete)
                rsm.PlayItem = this;

                // The player plays it on their own music context (no other mobile owns it)
                rsm.PlayMobile = null;

                // Give the author credit for this play (non-critical; never crash).
                try
                {
                    IncPlayCount(m, rsm);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }

                // Execute playback (each song on its own temporary copy: a new performance).
                RazorInstrument.Performance before = rsm.CurrentPerformance;
                rsm.OnDoubleClick(m);
                RazorInstrument.Performance performance = rsm.CurrentPerformance;
                if (!ReferenceEquals(performance, before))
                {
                    m_performance = performance;
                    performance.Completed += SongCompleted;

                    // our temporary copy plays this one song: its end releases it (deleted)
                    PlayContext? ctx = m_playContext;
                    if (ctx != null && ctx.Owns(rsm))
                        performance.Completed += (p, reason) => ctx.Release(rsm);
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        private void IncPlayCount(Mobile m, RolledUpSheetMusic rsm)
        {
            try
            {
                if (rsm == null)
                    return;

                Mobile owner = rsm.Owner;

                // No owner, no credits to award (and nothing to compare against).
                if (owner == null)
                    return;

                // You don't get 'play' credits for playing your own music.
                if (m == owner)
                    return;

                // Nor for a track not yet approved (staff listening to a pending or rejected track).
                if (!Approved(rsm))
                    return;

                // Increment global play count (defensive against missing repo/entry).
                try
                {
                    if (MusicLibrary.Repository.TryGetValue(rsm, out var entry) && entry != null)
                        entry.Plays++;
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                    // Swallow; play count is non-critical.
                }

                // Award points for having your track played (only for Bard guild players).
                if (m == null)
                    return;

                if (owner is not PlayerMobile pm)
                    return;

                if (pm.NpcGuild != NpcGuild.BardsGuild)
                    return;

                bool family = false;

                try
                {
                    family = FamilyIP(owner, m);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                    // If FamilyIP fails, do not award (fail closed).
                    return;
                }

                if (!family)
                    AwardSkillFameKarma(pm, PlayFactor);
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                // swallow to prevent shard crash
            }
        }
        private bool FamilyIP(Mobile owner, Mobile other)
        {
            try
            {
                if (owner == null || other == null)
                    return false;

                if (owner.Deleted || other.Deleted)
                    return false;

                return AimsHost.Current.IsHousehold(owner, other);    // the shard's rule (Angel Island: shared accounts by IP)
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false; // fail closed: no family relationship assumed
            }
        }
        // the song now playing (null: none, or one we stopped ourselves)
        private RazorInstrument.Performance? m_performance = null;
        // a song's performance has ended (raised once; after its last notes have drained when it was cancelled or its
        //  status check failed)
        private void SongCompleted(RazorInstrument.Performance performance, RazorInstrument.PerformanceEnd reason)
        {
            if (!ReferenceEquals(performance, m_performance))
                return;     // a song we stopped ourselves (EndMusic(Mobile)), or one we have moved on from
            m_performance = null;
            EndMusic();
        }
        public void EndMusic()
        {   // the song has ended (SongCompleted)
            try
            {
                // Always stop animation; do not let follow-on logic prevent this.
                try
                {
                    StopAnimation();
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }

                var ctx = m_playContext;
                if (ctx == null)
                    return;

                Mobile mob = ctx.Mobile;

                // Must be valid/nearby and in autoplay mode to proceed.
                if (!ctx.AutoPlay)
                    return;

                if (!CheckMobile(mob, this))
                    return;

                // The library changed: bring the queue up to date first (it rewinds when tracks came or went).
                if (GlobalRepositoryChanged())
                    ctx.RefreshPlayQueue();

                // Autoplay: on to the next song (a library change used to stop it here).
                ContinueMusic(mob);
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        public void EndMusic(Mobile m)
        {   // player initiated cancel
            try
            {
                // NOTE: preserve existing behavior; just make it safe.
                if (IsThisDevicePlaying())
                {
                    var ctx = m_playContext;
                    if (ctx == null)
                        return;

                    var now = ctx.NowPlaying();
                    if (now == null)
                        return;

                    // We stop it ourselves: its completion is not ours to act on (SongCompleted ignores it).
                    m_performance = null;

                    if (m != null)
                        now.OnDoubleClick(m);
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
            finally
            {
                // Always attempt to stop animation, even if cancellation path faults.
                try
                {
                    StopAnimation();
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }
            }
        }
        private void ContinueMusic(Mobile m)
        {
            try
            {
                if (!CheckMobile(m, this))
                    return;

                var ctx = m_playContext;

                if (ctx == null)
                    return;

                if (ctx.QueueCount > 0)
                {
                    RolledUpSheetMusic next = ctx.Next();
                    if (next != null)
                        PlayMusic(m, next);

                    return;
                }

                if (ctx.QueueCount == 0 && ctx.Loop)
                {
                    ctx.Rewind();

                    RolledUpSheetMusic next = ctx.Next();
                    if (next != null)
                        PlayMusic(m, next);
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
    }
}