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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.Commands.cs
 * Changelog
 *  11/14/26, Adam
 *      Play, add, remove and buy name their song through PickSong: a name that matches nothing is "I don't know a song
 *      called '...'"; play, add and remove take the closest of several and say so; buy, which costs gold, considers only
 *      songs on sale and never guesses (several matches are listed, with serials).
 *  11/8/26, Adam (2026 modernization, 10/29/26 to 11/8/26)
 *      The speech commands as one table: each command's words, help line, gates and handler, so the help list and
 *      TextCommand() can't disagree with the commands. Command words match in any case. Play by title: staff (GM and
 *      up, was Administrator) hear any track, and the one who submitted a pending track hears it; What says "pending".
 *      Add alone is Submit (the target cursor); Add with a song name still puts a removed song back. Repeat plays the
 *      song from a fresh copy (PlayContext.Replay; the copy it played is deleted once its song has ended).
 *      Split from MusicBox.Speech.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: Test Center (staff may buy music there) goes through AimsHost.
 *      'who': the box speaks as always; whoever may use 'find' also gets find's line for the track as a system message
 *      (owner, serial, hash code, and the shard's note: AimsHost.TrackNote).
 *      'list <words>' lists the tracks whose titles have every word ("I couldn't find any tracks for ..." when none
 *      do); Help shows it with 'list by <composer>'.
 *      'status': the composer's songs (waiting for review, approved, remastered, or not approved with the reason and
 *      staff's note: MusicLibrary.ComposerSongs), then what 'sales' says. 'status pending|approved|rejected' (also
 *      waiting, published, denied) lists only those songs, without the sales lines.
 *      'new' (also "what's new", "whats new", "what is new", "anything new", "new releases", "new music", in any case):
 *      the latest releases, newest first, each with when it was approved or remastered: the last 7 days, at least the 5
 *      most recent, at most 10 lines ("...and N more"). "what's new" was the What command (the song playing); 'new' was
 *      every song of the last 8 days, unsorted, undated, and "There are no new releases" in a quiet week.
 */

using Server.Commands;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using static Server.Items.MusicLibrary;

namespace Server.Items
{
    public partial class MusicBox
    {
        #region Command table
        // checked before the handler, in this order; a failed gate ends the command
        [Flags]
        private enum Gate
        {
            None = 0x00,
            StartsPlayback = 0x01,  // refused while the speaker has music playing in another context
            NotEmbedded = 0x02,     // "You will need a music box for this functionality." (a minstrel's box)
            Alive = 0x04,           // CheckAlive (it tells the player)
            BoxAccess = 0x08,       // HasMusicBoxAccess (silent)
            Submission = NotEmbedded | Alive | BoxAccess,
        }

        private delegate void SpeechHandler(MusicBox box, SpeechEventArgs e, string[] tokens);

        private sealed class SpeechCommand
        {
            public readonly TextCommands Command;
            public readonly string[] Words;         // the second token, as spoken ("?" removed); any case
            public readonly string? Help;           // the Help line (null: not listed)
            public readonly Gate Gates;
            public readonly AccessLevel Access;     // below it: "Unauthorized."
            public readonly SpeechHandler Handler;
            public SpeechCommand(TextCommands command, string words, string? help, SpeechHandler handler, Gate gates, AccessLevel access)
            {
                Command = command;
                Words = words.Split(' ');
                Help = help;
                Handler = handler;
                Gates = gates;
                Access = access;
            }
        }

        private static SpeechCommand Cmd(TextCommands command, string words, string? help, SpeechHandler handler,
            Gate gates = Gate.None, AccessLevel access = AccessLevel.Player)
        {
            return new SpeechCommand(command, words, help, handler, gates, access);
        }

        // Help lists the commands in this order.
        //  square brackets [optional option], angle brackets <required argument>, curly braces {default values},
        //  parenthesis (miscellaneous info)
        private static readonly SpeechCommand[] SpeechCommands =
        {
            // player commands
            Cmd(TextCommands.Play, "play", "play [song]", (b, e, t) => b.HandlePlay(e, t), Gate.StartsPlayback),
            Cmd(TextCommands.Cancel, "cancel stop", "Cancel, stops playback", (b, e, t) => b.HandleCancel(e)),
            Cmd(TextCommands.Pause, "pause", "Pause, pauses playback", (b, e, t) => b.HandlePause()),
            Cmd(TextCommands.Continue, "continue resume", "Continue, resume playback", (b, e, t) => b.HandleContinue(), Gate.StartsPlayback),
            Cmd(TextCommands.What, "who what who's what's", "What, composer and song name", (b, e, t) => b.HandleWhat(e.Mobile)),
            Cmd(TextCommands.Previous, "prev previous", "Previous, previous song", (b, e, t) => b.HandlePrevious(e), Gate.StartsPlayback),
            Cmd(TextCommands.Next, "next", "Next, next song", (b, e, t) => b.HandleNext(e), Gate.StartsPlayback),
            Cmd(TextCommands.Repeat, "repeat", "Repeat, repeat last song", (b, e, t) => b.HandleRepeat(e), Gate.StartsPlayback),
            Cmd(TextCommands.Loop, "loop", "Loop, loop playlist", (b, e, t) => b.HandleLoop()),
            Cmd(TextCommands.List, "list", "List [words | by <composer>], list all tracks, the tracks whose titles have those words, or a composer's tracks", (b, e, t) => b.HandleList(e, t)),
            Cmd(TextCommands.Add, "add", "Add [song], add track (previously removed); Add alone, add music to the music box", (b, e, t) => b.HandleAdd(e, t)),
            Cmd(TextCommands.Remove, "remove ignore", "Remove, add a global track to the ignore list", (b, e, t) => b.HandleRemove(e, t)),
            Cmd(TextCommands.Refresh, "refresh", "Refresh, adds any new global (free) tracks to your library", (b, e, t) => b.HandleRefresh()),
            Cmd(TextCommands.Buy, "buy", "Buy, user wishes to buy a track", (b, e, t) => b.HandleBuy(e, t)),
            Cmd(TextCommands.Shuffle, "shuffle", "Shuffle, shuffle users playlist", (b, e, t) => b.HandleShuffle()),
            Cmd(TextCommands.New, "new newreleases", "New (or what's new), the latest releases", (b, e, t) => b.HandleNew(e, t)),
            Cmd(TextCommands.Remaster, "remaster", "Remaster, request to remaster a previously approved song", (b, e, t) => b.HandleRemaster(e), Gate.Submission),

            // composer/purchaser
            Cmd(TextCommands.Sales, "sales", "Sales, your sales as a composer", (b, e, t) => b.HandleSales(e), Gate.NotEmbedded),
            Cmd(TextCommands.Purchases, "purchases", "Purchases, the music you have bought", (b, e, t) => b.HandlePurchases(e), Gate.NotEmbedded),
            Cmd(TextCommands.Status, "status", "Status [pending | approved | rejected], your songs (waiting, approved, or why not) and your sales", (b, e, t) => b.HandleStatus(e, t), Gate.NotEmbedded),

            // siege has no context menus, so the next two commands are verbal replacements
            Cmd(TextCommands.Submit, "submit", "Submit, add music to the music box", (b, e, t) => b.HandleSubmit(e), Gate.Submission),
            Cmd(TextCommands.Rename, "rename", "Rename, rename the music box", (b, e, t) => b.HandleRename(e), Gate.Submission),

            Cmd(TextCommands.Help, "help commands", "Help, list of commands", (b, e, t) => b.HandleHelp(e)),

            // staff and TC commands (MusicBox.StaffCommands.cs)
            Cmd(TextCommands.FlushAll, "flushall", null, (b, e, t) => b.HandleFlushAll(e)),
            Cmd(TextCommands.Review, "review", null, (b, e, t) => b.HandleReview(e), access: AccessLevel.Owner),
            Cmd(TextCommands.Approve, "approve", null, (b, e, t) => b.HandleApprove(e, t), access: AccessLevel.Owner),
            Cmd(TextCommands.Deny, "deny", null, (b, e, t) => b.HandleDeny(e, t), access: AccessLevel.Owner),
            Cmd(TextCommands.Delete, "delete", null, (b, e, t) => b.HandleDelete(e, t), access: AccessLevel.Owner),
            Cmd(TextCommands.Find, "find", null, (b, e, t) => b.HandleFind(e, t), Gate.NotEmbedded, AccessLevel.Owner),
            Cmd(TextCommands.Dupe, "dupe", null, (b, e, t) => b.HandleDupe(e, t), Gate.NotEmbedded, AccessLevel.Seer),
        };

        // spoken word => command (declared after SpeechCommands: static fields initialize in textual order)
        private static readonly Dictionary<string, SpeechCommand> SpeechWords =
            SpeechCommands.SelectMany(c => c.Words.Select(w => (w, c))).ToDictionary(x => x.w, x => x.c, StringComparer.OrdinalIgnoreCase);

        private static IEnumerable<string> HelpLines => SpeechCommands.Where(c => c.Help != null).Select(c => c.Help!);

        // the command named by the second token ("name command ..."); null: unknown
        private SpeechCommand? FindCommand(string[] tokens)
        {
            if (AsksWhatsNew(tokens))
                return SpeechWords["new"];
            if (tokens.Length > 1)
            {
                tokens = PreprocessTokens(tokens);
                if (SpeechWords.TryGetValue(tokens[1].Replace("?", "").Trim(), out SpeechCommand? command))
                    return command;
            }
            return null;
        }

        // "what's new", "whats new", "what is new", "anything new", "new releases", "new music": the New command (any case,
        //  either apostrophe, a trailing "?"); "what's" alone, or followed by anything else, stays the What command
        private static bool AsksWhatsNew(string[] tokens)
        {
            string Word(int i) => i < tokens.Length ? tokens[i].Replace("'", "").Replace("’", "").Replace("?", "").Trim().ToLowerInvariant() : "";
            if (tokens.Length == 3 && (Word(1) == "whats" || Word(1) == "anything") && Word(2) == "new")
                return true;
            if (tokens.Length == 4 && Word(1) == "what" && Word(2) == "is" && Word(3) == "new")
                return true;
            return tokens.Length == 3 && Word(1) == "new" && (Word(2) == "releases" || Word(2) == "music");
        }

        private TextCommands TextCommand(string[] tokens)
        {
            return FindCommand(tokens)?.Command ?? TextCommands.Unknown;
        }

        // the gates after "playing elsewhere" (OnSpeech checks that one first, for every command that starts playback)
        private bool PassesGates(SpeechCommand command, Mobile m)
        {
            if ((command.Gates & Gate.NotEmbedded) != 0 && CheckEmbedded(m, message: true))
                return false;

            if (m.AccessLevel < command.Access)
            {
                SendMessage(m, "Unauthorized.");
                return false;
            }

            if ((command.Gates & Gate.Alive) != 0 && !m.CheckAlive())
                return false;

            if ((command.Gates & Gate.BoxAccess) != 0 && !HasMusicBoxAccess(m))
                return false;

            return true;
        }
        #endregion Command table

        #region Player commands
        // before this box starts another song: if it is playing, stop it (StopPlaying ends the song; ForceStop clears what
        //  the engine still has queued for the player). preservePlaylist: keep the play queue's position (next, previous,
        //  repeat); false starts it over (play).
        private void StopForNewSong(bool preservePlaylist = true)
        {
            if (IsThisDevicePlaying())
            {
                StopPlaying(preservePlaylist);
                SystemMusicPlayer.ForceStop(m_playContext.Mobile);
            }
        }

        private void HandlePlay(SpeechEventArgs e, string[] tokens)
        {
            // Explicit play disables autoplay (caller intent is direct control).
            m_playContext.AutoPlay = false;

            // If we are already playing on this device, stop (and start the play queue over).
            StopForNewSong(preservePlaylist: false);

            // Start a new play:
            // - "name play" with only one argument behaves like double-click (play user's queue).
            // - Otherwise attempt to locate a specific song.
            if (tokens != null && tokens.Length == 2)
            {
                // Play/restart the user's playlist and free global tracks.
                OnDoubleClick(e.Mobile);
            }
            else if (PickSong(e.Mobile, tokens, "play", mustBeOne: false, null, out RolledUpSheetMusic track, out _))
            {
                // Re-check play context (some flows can change who "owns" the context).
                RefreshPlayContext(e.Mobile);

                if (m_playContext == null)
                    return;

                // Robustness: track can be null if repository is inconsistent.
                if (track == null)
                {
                    Say("Hmm, I don't know that.", hue: 54);
                    return;
                }

                bool approved = Approved(track);
                // staff (GM and up) hear any track by title; the one who submitted a pending track hears it too
                bool staff = e.Mobile.AccessLevel >= AccessLevel.GameMaster;

                if (staff || Playable(e.Mobile, track))
                {
                    // Ignore list is not consulted here since the user explicitly requested a song.
                    if (IsFree(track) ||
                        m_playContext.Playlist.Contains(track) ||
                        PurchasedOrOwned(e.Mobile, track) ||
                        staff)
                    {
                        // Napster restrictions: must have Napster access to play full track.
                        if (!track.Napster || (track.Napster && HasNapsterAccess(e.Mobile, track)))
                        {
                            if (approved)   // an unapproved track is announced by PlayMusic, as pending
                                Say(string.Format("{0} by {1}.", track, track.Author), hue: 54);
                            PlayMusic(e.Mobile, m_playContext.NowPlaying(track));
                        }
                        else
                        {
                            // Napster without access: play a short sample.
                            SendMessage(e.Mobile,
                                string.Format(
                                    "'{0}' is a Napster track. You will need to get a copy directly from {1}.",
                                    track, track.Owner != null ? track.Owner.Name : "(unknown)"));

                            Say(string.Format("Here is a sample of {0} by {1}.", track, track.Author), hue: 54);
                            PlayMusic(e.Mobile, m_playContext.NowPlaying(track));

                            Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerStateCallback(EndSample),
                                new object[] { this, e.Mobile });
                        }
                    }
                    else
                    {
                        // Not owned: play a sample.
                        Say(string.Format("Here is a sample of {0} by {1}.", track, track.Author), hue: 54);
                        PlayMusic(e.Mobile, m_playContext.NowPlaying(track));

                        Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerStateCallback(EndSample),
                            new object[] { this, e.Mobile });
                    }
                }
                else
                {
                    Say(string.Format("'{0}' is still pending approval", track), hue: 54);
                }
            }
            // else PickSong said why
        }

        private void HandleCancel(SpeechEventArgs e)
        {
            if (IsThisDevicePlaying())
                OnDoubleClick(e.Mobile);
        }

        private void HandlePause()
        {
            if (IsThisDevicePlaying())
            {
                RolledUpSheetMusic now = m_playContext.NowPlaying();
                if (now != null)
                    now.Pause(m_playContext.Mobile);
            }
        }

        private void HandleContinue()
        {
            if (IsThisDevicePlaying())
            {
                RolledUpSheetMusic now = m_playContext.NowPlaying();
                if (now != null)
                    now.Resume(m_playContext.Mobile);
            }
        }

        private void HandleWhat(Mobile asker)
        {
            if (IsThisDevicePlaying())
            {
                RolledUpSheetMusic now = m_playContext.NowPlaying();
                if (now != null)
                {
                    Say(string.Format("This is {0} by {1}{2}.", now, now.Author, Approved(now) ? "" : " (pending approval)"), hue: 54);
                    TellFinder(asker, now);
                }
                else
                    Say("Hmm, I don't know that.", hue: 54);
            }
            else
            {
                RolledUpSheetMusic? prev = m_playContext.PeekPrev();
                if (prev != null)
                {
                    Say(string.Format("That was {0} by {1}.", prev, prev.Author), hue: 54);
                    TellFinder(asker, prev);
                }
                else
                    Say("Hmm, I don't know that.", hue: 54);
            }
        }

        // whoever may use 'find' also gets its line for the track (a system message): the box speaks as always. A box plays a
        //  copy of the library's sheet: the line is about the library's own sheet and record of the same song.
        private void TellFinder(Mobile asker, RolledUpSheetMusic track)
        {
            SpeechCommand? find = SpeechCommands.FirstOrDefault(c => c.Command == TextCommands.Find);
            if (asker == null || find == null || asker.AccessLevel < find.Access)
                return;
            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
                if (kvp.Key != null && kvp.Key.Equals(track))
                {
                    SendMessage(asker, TrackLine(asker, kvp.Key, kvp.Value));
                    return;
                }
        }

        private void HandlePrevious(SpeechEventArgs e)
        {
            if (m_playContext.PeekPrev() != null)
            {
                StopForNewSong();
                RolledUpSheetMusic? prev = m_playContext.Prev();
                PlayMusic(e.Mobile, m_playContext.NowPlaying(prev));
            }
            else
            {
                Say("I can't go back any further.", hue: 54);
            }
        }

        private void HandleNext(SpeechEventArgs e)
        {
            if (m_playContext.QueueCount > 0)
            {
                StopForNewSong();
                PlayMusic(e.Mobile, m_playContext.Next());
            }
            else
            {
                Say("You're at the end of your playlist.", hue: 54);
            }
        }

        private void HandleRepeat(SpeechEventArgs e)
        {
            m_playContext.AutoPlay = false;

            RolledUpSheetMusic now = m_playContext.NowPlaying();
            RolledUpSheetMusic? prev = m_playContext.PeekPrev();

            if (now != null)
            {
                Say("I'll repeat the song.", hue: 54);
                StopForNewSong();
                PlayMusic(e.Mobile, m_playContext.Replay());     // a fresh copy: the old one is deleted when its song ends
            }
            else if (prev != null)
            {
                Say("I'll repeat the song.", hue: 54);
                StopForNewSong();
                PlayMusic(e.Mobile, m_playContext.NowPlaying(m_playContext.Prev()));
            }
            else
            {
                Say("Hmm, I have nothing in the queue.", hue: 54);
            }
        }

        private void HandleLoop()
        {
            if (!m_playContext.Loop)
            {
                Say("Okay, I will loop your playlist.", hue: 54);
                m_playContext.Loop = true;
            }
            else
            {
                Say("Okay, I will stop looping your playlist.", hue: 54);
                m_playContext.Loop = false;
            }
        }

        private void HandleList(SpeechEventArgs e, string[] tokens)
        {
            List<KeyValuePair<int, string>> list = CompositeList(e.Mobile, tokens, SetFilter());

            foreach (KeyValuePair<int, string> kvp in list)
                SendMessage(e.Mobile, kvp.Key, kvp.Value);

            if (MusicLibrary.Repository.Count == 0)
                SendMessage(e.Mobile, 54, "There are no music tracks available.");
            else if (list.Count == 0 && tokens.Length >= 3)
                Say(string.Format("I couldn't find any tracks for '{0}'.", string.Join(" ", tokens.Skip(2))), hue: 54);
        }

        private void HandleAdd(SpeechEventArgs e, string[] tokens)
        {
            string title = SongName(tokens);

            // "add" alone is Submit (the context menu's Add Sheet Music), with Submit's gates; "add <song>" puts a removed
            //  song back in the play list
            if (string.IsNullOrEmpty(title))
            {
                if (PassesGates(SpeechWords["submit"], e.Mobile))
                    HandleSubmit(e);
                return;
            }

            if (PickSong(e.Mobile, tokens, "add", mustBeOne: false, null, out RolledUpSheetMusic track, out _))
            {
                if (track == null || !Approved(track))
                {
                    Say(string.Format("Sorry, '{0}' is currently unavailable.", track), hue: 54);
                    return;
                }

                if (IsFree(track))
                {
                    if (!m_playContext.Playlist.Contains(track))
                    {
                        Say(string.Format("Added {0} by {1} to your library.", track, track.Author), hue: 54);

                        if (m_playContext.IgnoreList.Contains(track))
                        {
                            m_playContext.IgnoreList.Remove(track);
                            m_playContext.RefreshPlayQueue();
                        }
                    }
                    else
                    {
                        Say(string.Format("Your library already contains {0} by {1}.", track, track.Author), hue: 54);
                    }
                }
                else
                {
                    if (!m_playContext.Playlist.Contains(track))
                    {
                        Say(string.Format("{0} by {1} is a premium track. You will need to buy it.",
                            track, track.Author), hue: 54);

                        if (m_playContext.IgnoreList.Contains(track))
                            m_playContext.IgnoreList.Remove(track);
                    }
                    else
                    {
                        Say(string.Format("Your library already contains {0} by {1}.", track, track.Author), hue: 54);
                    }
                }
            }
            // else PickSong said why
        }

        // Remove = "hide from my library" via IgnoreList + rebuild queue.
        private void HandleRemove(SpeechEventArgs e, string[] tokens)
        {
            string title = SongName(tokens);
            if (string.IsNullOrEmpty(title))
            {
                Say("Tell me what you would like to remove.", hue: 54);
                return;
            }

            // Robustness: the play context must exist.
            if (m_playContext == null)
            {
                Say("Hmm, I don't know that.", hue: 54);
                return;
            }

            if (!PickSong(e.Mobile, tokens, "remove", mustBeOne: false, null, out RolledUpSheetMusic rsm, out _))
                return;     // PickSong said why

            if (rsm == null || rsm.Deleted)
            {
                Say("Hmm, I don't know that.", hue: 54);
                return;
            }

            // Only allow remove if the track is something the user could plausibly have seen:
            // - in their playlist OR present in global repository.
            if (m_playContext.Playlist.Contains(rsm) || MusicLibrary.Repository.ContainsKey(rsm))
            {
                if (!m_playContext.IgnoreList.Contains(rsm))
                {
                    Say(string.Format("Okay, I'll remove {0} from your library.", rsm), hue: 54);

                    m_playContext.IgnoreList.Add(rsm);

                    // Reset queue so the removal takes effect immediately (and rewinds).
                    m_playContext.ResetPlayQueue();
                }
                else
                {
                    Say(string.Format("Hmm, you've already removed {0} from your library.", rsm), hue: 54);
                }
            }
            else
            {
                Say(string.Format("I could not find {0} in your library.", rsm), hue: 54);
            }
        }

        // Refresh = rebuild queue from repository (delta message to user).
        private void HandleRefresh()
        {
            if (m_playContext == null)
                return;

            int delta = m_playContext.RefreshPlayQueue();

            if (delta == 0)
                Say("Your library is up to date.", hue: 54);
            else
                Say(string.Format("{1} {0} tracks to your library.",
                    Math.Abs(delta),
                    delta > 0 ? "Added" : "Removed"), hue: 54);
        }

        // Buy = purchase premium track (or add free track to playlist).
        private void HandleBuy(SpeechEventArgs e, string[] tokens)
        {
            // Staff restriction on production shard.
            if (e.Mobile.AccessLevel > AccessLevel.Player && !AimsHost.Current.IsTestCenter)
            {
                SendMessage(e.Mobile, "Staff cannot buy music on the production shard.");
                return;
            }

            // A carried box (a minstrel's) has no shop.
            if (CheckEmbedded(e.Mobile, message: true))
                return;

            string title = SongName(tokens);
            if (string.IsNullOrEmpty(title))
            {
                Say("Tell me what you would like to buy.", hue: 54);
                return;
            }

            if (m_playContext == null)
            {
                Say("Hmm, I don't know that.", hue: 54);
                return;
            }

            // only songs on sale (approved), and never a guess: buying costs gold
            if (!PickSong(e.Mobile, tokens, "buy", mustBeOne: true, (s, i) => i.Status == SubmissionStatus.Approved, out RolledUpSheetMusic rsm, out MusicInfo mi))
                return;     // PickSong said why

            // Robustness: validate repository entry.
            if (rsm == null || rsm.Deleted || mi == null)
            {
                Say("Hmm, I don't know that.", hue: 54);
                return;
            }

            if (!Approved(rsm))
            {
                Say(string.Format("Sorry, '{0}' is currently unavailable.", rsm), hue: 54);
                return;
            }

            // You cannot buy Napster tracks; must acquire from composer.
            if (rsm.Napster)
            {
                string ownerName = (rsm.Owner != null) ? rsm.Owner.Name : "(unknown)";
                Say(string.Format("'{0}' is a Napster track. You will need to get a copy directly from {1}.", rsm, ownerName), hue: 54);
                return;
            }

            // If they previously removed it, re-allow by removing from ignore list.
            if (m_playContext.IgnoreList.Contains(rsm))
                m_playContext.IgnoreList.Remove(rsm);

            int price = mi.Price;

            // Free track: behave like "add to playlist if not present".
            if (price == 0)
            {
                if (m_playContext.Playlist.Contains(rsm))
                {
                    Say(string.Format("'{0}', is already in your playlist.", rsm), hue: 54);
                    return;
                }

                m_playContext.RefreshPlayQueue();
                Say(string.Format("'{0}', added to your playlist.", rsm), hue: 54);
                return;
            }

            // Premium track: if already owned or has rights, do not prompt.
            // NOTE: Use TryGetValue to avoid ContainsKey+index races.
            if (HasRights(e.Mobile, rsm) ||
                (MusicLibrary.Repository.TryGetValue(rsm, out MusicInfo? repoInfo) && repoInfo != null && repoInfo.Owner == e.Mobile))
            {
                SendMessage(e.Mobile, string.Format("You already have access to {0}.", rsm.Title));
                return;
            }

            // Prompt for purchase confirmation.
            e.Mobile.Prompt = new BuyMusicBoxPrompt(e.Mobile, rsm, this, price);
            SendMessage(e.Mobile,
                string.Format("The cost to buy '{0}' is {1} gold. Are you sure you wish to buy this track?", rsm, price));
        }

        private void HandleShuffle()
        {
            Say("Okay, I'll shuffle your library.", hue: 54);

            if (m_playContext != null)
                m_playContext.Shuffle();
        }

        // the latest releases: approved songs by when they were approved or remastered, newest first; the last
        //  NewReleaseDays days, at least NewReleaseMin songs, at most NewReleaseMax lines
        private const int NewReleaseDays = 7, NewReleaseMin = 5, NewReleaseMax = 10;

        private void HandleNew(SpeechEventArgs e, string[] tokens)
        {
            Mobile m = e.Mobile;
            if (MusicLibrary.Repository.Count == 0)
            {
                SendMessage(m, 54, "There are no music tracks available.");
                return;
            }

            DateTime now = DateTime.UtcNow;
            var releases = MusicLibrary.Repository
                .Where(kvp => Approved(kvp.Key))
                .Select(kvp => (Track: kvp.Key, Info: kvp.Value,
                    Remastered: kvp.Value.Remastered && kvp.Value.DateRemastered > kvp.Value.DateApproved,
                    When: kvp.Value.Remastered && kvp.Value.DateRemastered > kvp.Value.DateApproved ? kvp.Value.DateRemastered : kvp.Value.DateApproved))
                .Where(r => r.When > DateTime.MinValue)
                .OrderByDescending(r => r.When)
                .ToList();

            if (releases.Count == 0)
            {
                SendMessage(m, 54, "There are no new releases.");
                return;
            }

            int recent = releases.Count(r => (now - r.When).TotalDays < NewReleaseDays);
            int show = Math.Min(Math.Max(recent, NewReleaseMin), releases.Count);
            if (recent == 0)
                SendMessage(m, 54, "Nothing new this week. The latest releases:");

            foreach (var r in releases.Take(Math.Min(show, NewReleaseMax)))
            {
                bool locked = (!PurchasedOrOwned(m, r.Track) && r.Info.Price > 0) || (r.Track.Napster && !HasNapsterAccess(m, r.Track));
                string tags = (m_playContext.IgnoreList.Contains(r.Track) ? " (ignored)" : "")
                    + (r.Track.Napster ? " (Napster)" : "") + (r.Info.Price > 0 ? " (Premium)" : "");
                SendMessage(m, locked ? 33 : 54, string.Format("{0} by {1}{2}, {3} {4}",
                    r.Track, r.Track.Author, tags, r.Remastered ? "remastered" : "approved", WhenSaid(now, r.When)));
            }

            if (show > NewReleaseMax)
                SendMessage(m, 54, string.Format("...and {0} more: say 'list' for everything.", show - NewReleaseMax));
        }

        // "today", "yesterday", "3 days ago", then the date
        private static string WhenSaid(DateTime now, DateTime when)
        {
            int days = (int)(now.Date - when.Date).TotalDays;
            return days <= 0 ? "today" : days == 1 ? "yesterday" : days < 60 ? days + " days ago" : "on " + when.ToString("M/d/yy");
        }

        private void HandleHelp(SpeechEventArgs e)
        {
            foreach (string line in HelpLines)
                SendMessage(e.Mobile, line);
        }

        // composer/purchaser
        private void HandleSales(SpeechEventArgs e)
        {
            int totalSales, sold, credit;
            MusicLibrary.CalculateSellerData(e.Mobile, out totalSales, out sold, out credit);

            SendMessage(e.Mobile, string.Format("You have sold {0} tracks.", sold));
            SendMessage(e.Mobile, string.Format("For a total of {0} gold", totalSales));
            SendMessage(e.Mobile, string.Format("You have {0} credit due you.", credit));
        }

        // a composer's songs, with what review decided, then the 'sales' figures; 'status pending|approved|rejected' lists
        //  only those songs (no sales lines): a long list scrolls off the screen
        private void HandleStatus(SpeechEventArgs e, string[] tokens)
        {
            string word = tokens != null && tokens.Length > 2 ? tokens[2].ToLowerInvariant() : "";
            SubmissionStatus? only = null;
            switch (word)
            {
                case "":
                    break;
                case "pending": case "waiting":
                    only = SubmissionStatus.Pending;
                    break;
                case "approved": case "published":
                    only = SubmissionStatus.Approved;
                    break;
                case "rejected": case "denied":
                    only = SubmissionStatus.Rejected;
                    break;
                default:
                    SendMessage(e.Mobile, "Say status, or status pending, status approved or status rejected.");
                    return;
            }

            List<string> songs = MusicLibrary.ComposerSongs(e.Mobile, only);
            string which = only switch
            {
                SubmissionStatus.Pending => " waiting for review",
                SubmissionStatus.Approved => " approved",
                SubmissionStatus.Rejected => " not approved",
                _ => "",
            };
            if (songs.Count == 0)
                SendMessage(e.Mobile, only == null ? "You haven't submitted any songs to the music library." :
                    string.Format("You have no songs{0}.", which));
            else
            {
                SendMessage(e.Mobile, string.Format("Your songs{0} ({1}):", which, songs.Count));
                foreach (string line in songs)
                    SendMessage(e.Mobile, line);
            }
            if (only == null)
                HandleSales(e);
        }

        private void HandlePurchases(SpeechEventArgs e)
        {
            int total = 0;

            foreach (KeyValuePair<RolledUpSheetMusic, MusicInfo> kvp in MusicLibrary.Repository)
            {
                RolledUpSheetMusic track = kvp.Key;
                MusicInfo info = kvp.Value;

                if (track == null || info == null || info.Purchasers == null)
                    continue;

                foreach (PurchaserInfo lml in info.Purchasers)
                {
                    if (lml == null)
                        continue;

                    if (lml.Purchaser == e.Mobile)
                    {
                        SendMessage(e.Mobile,
                            string.Format("On {0}, you purchased {1} by {2} for {3} gold.",
                                lml.PurchaseDate, track, track.Author, lml.Price));

                        if (info.Price != lml.Price)
                        {
                            // This can happen when the owner's character is deleted - all music gets repriced to zero.
                            SendMessage(e.Mobile,
                                string.Format("Note: {0} is currently priced at {1} gold.", track, info.Price));
                        }

                        total += lml.Price;
                    }
                }
            }

            SendMessage(e.Mobile, string.Format("You have spent a total of {0} gold on music.", total));
        }

        // Remaster, Submit and Rename: not embedded, alive and with music box access (Gate.Submission)
        private void HandleRemaster(SpeechEventArgs e)
        {
            SendMessage(e.Mobile, "Target the remastered music...");
            e.Mobile.Target = new AddMusicTarget(this, remaster: true);
        }

        // siege has no context menus, so the next two commands are verbal replacements
        private void HandleSubmit(SpeechEventArgs e)
        {
            SendMessage(e.Mobile, "Target the music you would like to add...");
            e.Mobile.Target = new AddMusicTarget(this, remaster: false);
        }

        private void HandleRename(SpeechEventArgs e)
        {
            e.Mobile.Prompt = new NameMusicBoxPrompt(this);
            SendMessage(e.Mobile, "Type in the new name of the music box.");
        }
        #endregion Player commands
    }
}