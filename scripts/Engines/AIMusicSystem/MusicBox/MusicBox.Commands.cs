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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.Commands.cs
 * Changelog
 *  11/6/26, Adam (2026 modernization, 10/29/26 to 11/6/26)
 *      The speech commands as one table: each command's words, help line, gates and handler, so the help list and
 *      TextCommand() can't disagree with the commands. Command words match in any case. Play by title: staff (GM and
 *      up, was Administrator) hear any track, and the one who submitted a pending track hears it; What says "pending".
 *      Add alone is Submit (the target cursor); Add with a song name still puts a removed song back. Repeat plays the
 *      song from a fresh copy (PlayContext.Replay; the copy it played is deleted once its song has ended).
 *      Split from MusicBox.Speech.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: Test Center (staff may buy music there) goes through AimsHost.
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
            Cmd(TextCommands.What, "who what who's what's", "What, composer and song name", (b, e, t) => b.HandleWhat()),
            Cmd(TextCommands.Previous, "prev previous", "Previous, previous song", (b, e, t) => b.HandlePrevious(e), Gate.StartsPlayback),
            Cmd(TextCommands.Next, "next", "Next, next song", (b, e, t) => b.HandleNext(e), Gate.StartsPlayback),
            Cmd(TextCommands.Repeat, "repeat", "Repeat, repeat last song", (b, e, t) => b.HandleRepeat(e), Gate.StartsPlayback),
            Cmd(TextCommands.Loop, "loop", "Loop, loop playlist", (b, e, t) => b.HandleLoop()),
            Cmd(TextCommands.List, "list", "List, list all tracks", (b, e, t) => b.HandleList(e, t)),
            Cmd(TextCommands.Add, "add", "Add [song], add track (previously removed); Add alone, add music to the music box", (b, e, t) => b.HandleAdd(e, t)),
            Cmd(TextCommands.Remove, "remove ignore", "Remove, add a global track to the ignore list", (b, e, t) => b.HandleRemove(t)),
            Cmd(TextCommands.Refresh, "refresh", "Refresh, adds any new global (free) tracks to your library", (b, e, t) => b.HandleRefresh()),
            Cmd(TextCommands.Buy, "buy", "Buy, user wishes to buy a track", (b, e, t) => b.HandleBuy(e, t)),
            Cmd(TextCommands.Shuffle, "shuffle", "Shuffle, shuffle users playlist", (b, e, t) => b.HandleShuffle()),
            Cmd(TextCommands.New, "new newreleases", "New, list new releases", (b, e, t) => b.HandleNew(e, t)),
            Cmd(TextCommands.Remaster, "remaster", "Remaster, request to remaster a previously approved song", (b, e, t) => b.HandleRemaster(e), Gate.Submission),

            // composer/purchaser
            Cmd(TextCommands.Sales, "sales", "Sales, your sales as a composer", (b, e, t) => b.HandleSales(e), Gate.NotEmbedded),
            Cmd(TextCommands.Purchases, "purchases", "Purchases, the music you have bought", (b, e, t) => b.HandlePurchases(e), Gate.NotEmbedded),

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
            if (tokens.Length > 1)
            {
                tokens = PreprocessTokens(tokens);
                if (SpeechWords.TryGetValue(tokens[1].Replace("?", "").Trim(), out SpeechCommand? command))
                    return command;
            }
            return null;
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
            else if (TryFindSong(tokens, out RolledUpSheetMusic track, out _))
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
            else
            {
                Say(string.Format("I couldn't find '{0}' in your music library", SongName(tokens)), hue: 54);
            }
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

        private void HandleWhat()
        {
            if (IsThisDevicePlaying())
            {
                RolledUpSheetMusic now = m_playContext.NowPlaying();
                if (now != null)
                    Say(string.Format("This is {0} by {1}{2}.", now, now.Author, Approved(now) ? "" : " (pending approval)"), hue: 54);
                else
                    Say("Hmm, I don't know that.", hue: 54);
            }
            else
            {
                RolledUpSheetMusic? prev = m_playContext.PeekPrev();
                if (prev != null)
                    Say(string.Format("That was {0} by {1}.", prev, prev.Author), hue: 54);
                else
                    Say("Hmm, I don't know that.", hue: 54);
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

            if (TryFindSong(tokens, out RolledUpSheetMusic track, out _))
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
            else
            {
                Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
            }
        }

        // Remove = "hide from my library" via IgnoreList + rebuild queue.
        private void HandleRemove(string[] tokens)
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

            if (!TryFindSong(tokens, out RolledUpSheetMusic rsm, out _))
            {
                Say(string.Format("I could not find {0} in your library.", SongName(tokens)), hue: 54);
                return;
            }

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

            if (!TryFindSong(tokens, out RolledUpSheetMusic rsm, out MusicInfo mi))
            {
                Say(string.Format("I couldn't find '{0}' in the global music repository.", SongName(tokens)), hue: 54);
                return;
            }

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

        private void HandleNew(SpeechEventArgs e, string[] tokens)
        {
            List<KeyValuePair<int, string>> list = CompositeList(e.Mobile, tokens, SetFilter(Filter.New | Filter.Remastered));

            if (list == null || list.Count == 0)
                SendMessage(e.Mobile, 54, "There are no new releases.");
            else
            {
                foreach (KeyValuePair<int, string> kvp in list)
                    SendMessage(e.Mobile, kvp.Key, kvp.Value);
            }

            if (MusicLibrary.Repository.Count == 0)
                SendMessage(e.Mobile, 54, "There are no music tracks available.");
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