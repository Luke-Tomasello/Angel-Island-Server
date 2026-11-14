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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.Speech.cs
 * Changelog
 *  11/8/26, Adam (2026 modernization, 10/29/26 to 11/8/26)
 *      The speech interface: OnSpeech dispatches through the command table (MusicBox.Commands.cs; staff commands in
 *      MusicBox.StaffCommands.cs).
 *      Split from MusicBox.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: staff messages.
 *      A composer speaking to a music box collects music box credit owed to them (MusicLibrary.CollectCredits).
 *      ComposerVisit (was CollectCredits): the composer is also told review decisions on their songs (TellComposer).
 *      TextCommands.Status ('status').
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
        public enum TextCommands
        {
            #region player issued commands
            Play,           // request to play
            Cancel,         // stops playback (RazorInstrument stops)
            Pause,          // pauses playback (RazorInstrument still looping/waiting)
            Continue,       // resume playback
            What,           // who is the composer
            Previous,       // previous song
            Next,           // next song
            Repeat,         // repeat last song
            Loop,           // loop playlist
            List,           // list all tracks
            Add,            // add track (previously removed)
            Remove,         // add a global track to the ignore list
            Refresh,        // adds any new global (free) tracks to your library
            Buy,            // user wishes to buy a track
            Shuffle,        // shuffle users playlist
            New,            // list new releases
            Help,           // list of commands
            Remaster,       // request to remaster a previously approved song

            // composer/purchaser
            Sales,          // total composers sales
            Purchases,      // player purchases
            Status,         // a composer's songs (review results) and sales
            #endregion  player issued commands

            Unknown = 100,  // unknown command

            // siege has no context menus, so the next two commands are verbal replacements
            Submit,         // add music to the music box
            Rename,         // rename the music box

            // staff and TC commands
            FlushAll = 200,         // flush all tracks - everywhere
            Review,                 // review submitted tracks
            Approve,                // approve a track
            Deny,                   // deny (reject) a track
            Delete,                 // delete a track from the library
            Find,                   // find a track and display: serial number, owner, name, etc.
            Dupe,                   // makes a copy of the specified track and puts it in your backpack
        }
        string[] Tokenizer(string text, string name)
        {
            try
            {
                // Robustness: normalize null inputs.
                if (string.IsNullOrWhiteSpace(text))
                    return Array.Empty<string>();

                // Normalize text for consistent processing.
                string working = text.Trim();

                // If no name provided, fall back to simple tokenization.
                if (string.IsNullOrWhiteSpace(name))
                {
                    return working
                        .ToLowerInvariant()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }

                // Case-insensitive match at the beginning of the string.
                // We do NOT lower-case the original string here because we want to preserve
                // the original casing of "name" when returning it as a distinct first token.
                if (working.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    // Ensure we are matching a proper token boundary:
                    // Either exact match OR followed by whitespace.
                    if (working.Length == name.Length || char.IsWhiteSpace(working[name.Length]))
                    {
                        List<string> tokens = new List<string> { name };

                        // Remove the matched name portion.
                        string remainder = working.Substring(name.Length).TrimStart();

                        if (!string.IsNullOrWhiteSpace(remainder))
                        {
                            tokens.AddRange(
                                remainder
                                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        }

                        return tokens.ToArray();
                    }
                }

                // Default behavior: lowercase everything and split.
                return working
                    .ToLowerInvariant()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return Array.Empty<string>(); // Fail closed.
            }
        }
        public bool YouTalkingToMe(SpeechEventArgs e, out bool wasNamed)
        {
            wasNamed = false;

            try
            {
                // Robustness: validate event args.
                if (e == null)
                    return false;

                // If someone else already handled this speech, do not process it again.
                if (e.Handled)
                    return false;

                // Robustness: mobile and speech are required.
                if (e.Mobile == null || string.IsNullOrWhiteSpace(e.Speech))
                    return false;

                // Normalize once for consistent comparison.
                // NOTE: Use invariant casing for predictable behavior across cultures.
                string itemName = (Name ?? string.Empty).Trim();
                if (itemName.Length == 0)
                    return false;

                string itemNameLower = itemName.ToLowerInvariant();

                // Tokenize: if the player starts the speech with our name, the first token will be the (original) name.
                // Otherwise tokens are lower-cased by Tokenizer() by default.
                string[] tokens = Tokenizer(e.Speech, itemName);

                // Must have at least one token and must be addressing us by name.
                if (tokens == null || tokens.Length == 0)
                    return false;

                // Compare first token to our name case-insensitively (do not assume Tokenizer lower-cased it).
                if (!string.Equals(tokens[0], itemName, StringComparison.OrdinalIgnoreCase))
                    return false;

                wasNamed = true;

                // The speaker must have rights here (where the box is: MusicBox.Placement.cs; a box in a container
                //  has none).
                if (CheckRights(e.Mobile, message: false) != Rights.okay)
                    return false;

                // Validate that the remainder of the speech resolves to a known text command.
                if (TextCommand(tokens) == TextCommands.Unknown)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                wasNamed = false;
                return false;
            }
        }
        public bool PlayingElsewhere(Mobile m)
        {
            if (m == null || m.Deleted)
                return true;
            return (SystemMusicPlayer.IsUserBlocked(m) || RazorInstrument.PerformanceOf(m) != null) && !IsThisDevicePlaying();
        }

        private Queue<SpeechEventArgs> SpamQueue = new Queue<SpeechEventArgs>();
        public override void OnSpeech(SpeechEventArgs e)
        {
            try
            {
                // Robustness: validate event args.
                if (e == null)
                    return;

                if (e.Handled)
                    return;

                if (e.Mobile == null || string.IsNullOrWhiteSpace(e.Speech))
                    return;

                // Robustness: item name is required for addressing-by-name semantics.
                string myName = (Name ?? string.Empty).Trim();
                if (myName.Length == 0)
                    return;

                // Tokenize once. Tokenizer() already handles case rules and name-first token behavior.
                string[] tokens = Tokenizer(e.Speech, myName);

                bool wasNamed = false;

                // Decide if this speech is intended for us and also validate the command.
                // NOTE: YouTalkingToMe() performs:
                // - name addressing check
                // - rights check for supported contexts
                // - unknown-command rejection
                if (YouTalkingToMe(e, out wasNamed))
                {
                    // Mark handled so other items do not also respond.
                    e.Handled = true;
                }
                else
                {
                    // If they addressed us by name and have rights, but the command is not recognized,
                    // give a gentle "unknown" response.
                    if (wasNamed && CheckRights(e.Mobile, message: false) == Rights.okay)
                        Say("Hmm, I don't know that.");

                    return;
                }

                // Within speaking range (a carried box hears through its carrier: MusicBox.Placement.cs).
                if (!InSpeechRange(e.Mobile))
                    return;

                // A composer using a music box collects credit owed from earlier sales (before "sales" reports it).
                MusicLibrary.ComposerVisit(e.Mobile);

                // Ensure we have a valid PlayContext for this user before processing commands.
                RefreshPlayContext(e.Mobile);

                // Robustness: context creation can fail; fail closed.
                if (m_playContext == null)
                    return;

                // If the global repository changed since last check, refresh the user's queue.
                if (GlobalRepositoryChanged())
                    m_playContext.RefreshPlayQueue();

                // Resolve the command once (MusicBox.Commands.cs).
                SpeechCommand? command = FindCommand(tokens);

                // Enforce "one active play context" per user for commands that would start/advance playback.
                bool blocked = PlayingElsewhere(e.Mobile);
                if (command != null && (command.Gates & Gate.StartsPlayback) != 0 && blocked)
                {
                    SendMessage(e.Mobile,
                        "You have initiated music in another context. You will need to cancel that or wait for it to finish.");

                    if (Core.Debug)
                        AimsHost.Current.SendSystemMessage(e.Mobile, 5,
                            "Debug message: music not started because this player has initiated music elsewhere.");

                    return;
                }

                if (command == null)
                {
                    Say("Hmm, I don't know that.", hue: 54);
                    return;
                }

                // The command's gates, then its handler.
                if (PassesGates(command, e.Mobile))
                    command.Handler(this, e, tokens);
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        #endregion Text Interface
    }
}