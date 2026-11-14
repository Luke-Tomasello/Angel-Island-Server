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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.ContextMenus.cs
 * Changelog
 *  11/8/26, Adam (2026 modernization, 10/29/26 to 11/8/26)
 *      The context menu: Add Sheet Music (submit or remaster) and Name Music Box, shown only where the box works
 *      (CheckRights, as double-click and speech). A sheet that is playing stops when the box takes it (stored or deleted).
 *      Split from MusicBox.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: internal storage. Angel Island only (#if ANGEL_ISLAND): the ArrayList context menu (else
 *      List<ContextMenuEntry>).
 *      Adding or remastering sheet music asks the shard first (AimsHost.RefuseSubmission): a reason refuses it.
 *      A composer opening a music box's menu collects music box credit owed to them (MusicLibrary.CollectCredits).
 *      ComposerVisit (was CollectCredits): the composer is also told review decisions on their songs (TellComposer).
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
        #region Context Menus
#if ANGEL_ISLAND
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
#else
        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
#endif
        {
            base.GetContextMenuEntries(from, list);

            // A composer using a music box collects credit owed from earlier sales.
            MusicLibrary.ComposerVisit(from);

            // only where the box works (placed and locked down, for someone with access): see MusicBox.Placement.cs
            if (from.CheckAlive() && CheckRights(from, message: false) == Rights.okay)
            {
                list.Add(new AddSheetMusicEntry(from, this, remaster: false));
                list.Add(new NameMusicBoxEntry(from, this));
            }

            SetSecureLevelEntry.AddTo(from, this, list);
        }
        /*
		m_map[3000075] = "Name";
		m_map[3000154] = "Clear";
		m_map[3000163] = "Remove";
		m_map[3000175] = "Add";
		m_map[3000176] = "Delete";
		m_map[3000186] = "Continue";
		m_map[3000090] = "Apply";
        m_map[3000091] = "Cancel";
		m_map[3000094] = "Default";
		m_map[3000098] = "Information";
		m_map[3000123] = "Credits";
		m_map[3000165] = "Title";
		m_map[3000170] = "Sort by";
		m_map[3000191] = "Quit";
		m_map[3000362] = "Open";
        m_map[3000363] = "Close";
		m_map[3000390] = "Sound";
		m_map[3000395] = "Interface";
		m_map[3000410] = "Off";
        m_map[3000411] = "On";
		m_map[3000431] = "Inventory";
		m_map[3000548] = "Done";
        m_map[3000549] = "Close";
		m_map[3000564] = "Volume";
		m_map[3005113] = "Start";
        m_map[3005114] = "Stop";
		m_map[3010002] = "Back";
        m_map[3010003] = "Index";
        m_map[3010004] = "History";
        m_map[3010005] = "Search";
		m_map[3010041] = "Inventory";
		*/
        private class AddSheetMusicEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private MusicBox m_musicBox;
            bool m_remaster;

            public AddSheetMusicEntry(Mobile from, MusicBox musicbox, bool remaster)
                : base(0175/*add*/)
            {
                m_From = from;
                m_musicBox = musicbox;
                m_remaster = remaster;
            }

            public override void OnClick()
            {
                if (m_From.CheckAlive() && m_musicBox.CheckRights(m_From, message: true) == Rights.okay)
                {
                    m_From.SendMessage("Target the music you would like to add...");
                    m_From.Target = new AddMusicTarget(m_musicBox, m_remaster); // Call our target
                }
            }
        }
        public class AddMusicTarget : Target // Target handler for adding (or remastering) RolledUpSheetMusic into the global library
        {
            private readonly MusicBox m_musicBox;
            private readonly bool m_remaster;

            public AddMusicTarget(MusicBox musicbox, bool remaster)
                : base(5, true, TargetFlags.None)
            {
                // Robustness: allow construction even if caller passes null; OnTarget will fail-closed.
                m_musicBox = musicbox;
                m_remaster = remaster;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                // Robustness: targeting callbacks can arrive with null/invalid mobile in some edge cases.
                if (from == null)
                    return;

                try
                {
                    // Robustness: ensure we have a valid music box to operate on.
                    if (m_musicBox == null || m_musicBox.Deleted)
                    {
                        from.SendMessage("That music box is no longer available.");
                        return;
                    }

                    // Establish/refresh the play context up front so subsequent operations
                    // (like queue refresh or Say()) have a coherent actor/context.
                    m_musicBox.RefreshPlayContext(from);

                    // For clarity in this handler: "buyer" is the player performing the add/purchase action.
                    Mobile buyer = from;

                    // Validate target type early and return on mismatch.
                    if (target is not RolledUpSheetMusic rsm)
                    {
                        buyer.SendMessage("That is not rolled up sheet music.");
                        return;
                    }

                    // Deleted items should be treated as invalid targets.
                    if (rsm.Deleted)
                    {
                        buyer.SendMessage("That item is no longer available.");
                        return;
                    }

                    // A songless sheet has no identity of its own: its hash is shared (empty composition) or meaningless.
                    if (!rsm.HasComposition)
                    {
                        buyer.SendMessage("There is no music written on that sheet music.");
                        return;
                    }

                    // Only movable music can be submitted (locked down items cannot be transferred).
                    if (!rsm.Movable)
                    {
                        buyer.SendMessage("That is locked down.");
                        return;
                    }

                    // The shard may refuse it, with its reason (AimsHost.RefuseSubmission: e.g. a song whose home is another
                    //  shard is remastered there).
                    string? refusal = AimsHost.Current.RefuseSubmission(buyer, rsm, m_remaster);
                    if (refusal != null)
                    {
                        buyer.SendMessage(refusal);
                        return;
                    }

                    // Defensive: repository references must exist.
                    // Remaster gating:
                    // - Remaster requires an existing TrackID match in the global library.
                    // - New submission requires the TrackID not already exist (or else you should remaster).
                    bool trackExists = m_musicBox.GMLHasTrackID(rsm);

                    if (m_remaster && !trackExists)
                    {
                        buyer.SendMessage("Unable to locate {0} in the global music library for remastering.", rsm.Title);
                        return;
                    }
                    else if (!m_remaster && trackExists)
                    {
                        buyer.SendMessage("The global music library already contains {0}.", rsm.Title);
                        buyer.SendMessage("You will need to 'remaster' the track to submit it.");
                        return;
                    }

                    // Napster behavior:
                    // - If already in MusicLibrary.Repository, this handler behaves like "purchase/add to my library"
                    //   and deletes the submitted copy.
                    // - If not present, this handler adds it to the global library (pending review) and records purchaser.
                    if (rsm.Napster)
                    {
                        if (MusicLibrary.Repository.ContainsKey(rsm))
                        {
                            // Already in the global library: this is a purchase/access grant flow.

                            // Robustness: Ensure the repository entry exists and is valid before dereferencing.
                            if (!MusicLibrary.Repository.TryGetValue(rsm, out MusicInfo? info) || info == null)
                            {
                                buyer.SendMessage("The global music library entry is invalid.");
                                return;
                            }

                            // If the player already has rights or is the owner, they already have access.
                            if (m_musicBox.HasRights(buyer, rsm) || info.Owner == buyer)
                            {
                                buyer.SendMessage("You already have access to {0}.", rsm.Title);
                                return;
                            }

                            // Do not grant access to an unapproved track.
                            if (!m_musicBox.Approved(rsm))
                            {
                                buyer.SendMessage("{0} is pending approval.", rsm.Title);
                                return;
                            }

                            // Update the user's library (acquiring through the context menu costs nothing).
                            buyer.SendMessage("You acquired {0} for {1} gold.", rsm, 0);

                            // Use music box speech for consistent UX.
                            m_musicBox.Say(string.Format("'{0}', added to your library.", rsm), hue: 54);

                            // Record purchaser. Guard Purchasers list in case of unexpected null.
                            if (info.Purchasers != null)
                                info.Purchasers.Add(new PurchaserInfo(buyer, info.Price));

                            // Refresh play queue so newly granted access is immediately reflected.
                            if (m_musicBox.m_playContext != null)
                                m_musicBox.m_playContext.RefreshPlayQueue();

                            // Delete the submitted copy (stopping it first if it is playing).
                            StopIfPlaying(buyer, rsm);
                            rsm.Delete();
                            return;
                        }
                        else
                        {
                            // New Napster submission: add to global library and queue for review.

                            // Legacy compatibility guard: older RSMs may have no owner.
                            if (rsm.Owner == null)
                            {
                                buyer.SendMessage("This old version of {0} is not compatible with the musicbox.", rsm.Title);
                                buyer.SendMessage("Please recreate using Manuscript Paper.");
                                return;
                            }

                            // A sheet that is playing stops: it is leaving the player for the library.
                            StopIfPlaying(buyer, rsm);

                            // Prepare the RSM for music box/global library requirements: not tied to backpack-only usage.
                            rsm.RequiresBackpack = false;

                            // Detach from any container before moving into internal storage.
                            if (rsm.Parent is Container parentContainer)
                                parentContainer.RemoveItem(rsm);

                            // Move into internal map/storage for persistent repository ownership.
                            AimsHost.Current.MoveToInternal(rsm);

                            // Create repository info and mark Napster submission.
                            MusicInfo info = new MusicInfo(rsm.Owner, rsm.Price);
                            info.Napster = true;

                            // Add to repository.
                            MusicLibrary.Repository.Add(rsm, info);

                            // Record purchaser (submitter gets access).
                            if (info.Purchasers != null)
                                info.Purchasers.Add(new PurchaserInfo(buyer, info.Price));

                            // Add to review queue (defensive: ensure list exists).
                            MusicLibrary.UnpublishedNeedsReview.Add(rsm);

                            buyer.SendMessage("Added {0} (Pending review.)", rsm.Title);
                            return;
                        }
                    }

                    // Non-Napster behavior:
                    // - Only the owner may submit into the global library.
                    // - Duplicate entries (by object key) are disallowed.
                    if (MusicLibrary.Repository.ContainsKey(rsm))
                    {
                        buyer.SendMessage("The global music library already contains {0}.", rsm.Title);
                        return;
                    }

                    // Legacy compatibility guard: older RSMs may have no owner.
                    if (rsm.Owner == null)
                    {
                        buyer.SendMessage("This old version of {0} is not compatible with the musicbox.", rsm.Title);
                        buyer.SendMessage("Please recreate using Manuscript Paper.");
                        return;
                    }

                    // Only the owner may add non-Napster music. Checked before the sheet is touched: a refused sheet
                    //  stays with the player.
                    if (buyer != rsm.Owner)
                    {
                        buyer.SendMessage("You do not own {0}.", rsm.Title);
                        return;
                    }

                    // A sheet that is playing stops: it is leaving the player for the library.
                    StopIfPlaying(buyer, rsm);

                    // Prepare the RSM for repository storage.
                    rsm.RequiresBackpack = false;

                    // Detach from any container before moving into internal storage.
                    if (rsm.Parent is Container c)
                        c.RemoveItem(rsm);

                    // Move into internal storage.
                    AimsHost.Current.MoveToInternal(rsm);

                    MusicLibrary.Repository.Add(rsm, new MusicInfo(rsm.Owner, rsm.Price));

                    MusicLibrary.UnpublishedNeedsReview.Add(rsm);

                    buyer.SendMessage("Added {0} (Pending review.)", rsm.Title);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }
            }
            // The sheet is about to go into the library (internal storage) or be deleted: a performance in progress would
            //  go on playing from there. Cancelled as the player would cancel it (the same emote).
            private static void StopIfPlaying(Mobile from, RolledUpSheetMusic rsm)
            {
                if (rsm.IsInstancePlaying)
                    rsm.CancelPlayback(from);
            }
        }
        private class NameMusicBoxEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private MusicBox m_musicBox;

            public NameMusicBoxEntry(Mobile from, MusicBox musicbox)
                : base(0075/*name*/)
            {
                m_From = from;
                m_musicBox = musicbox;
            }

            public override void OnClick()
            {
                if (m_From.CheckAlive() && m_musicBox.CheckRights(m_From, message: true) == Rights.okay)
                {
                    m_From.Prompt = new NameMusicBoxPrompt(m_musicBox);
                    m_From.SendMessage("Type in the new name of the music box.");
                }
            }
        }
        private class NameMusicBoxPrompt : Prompt
        {
            private MusicBox m_musicBox;

            public NameMusicBoxPrompt(MusicBox musicbox)
            {
                m_musicBox = musicbox;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                    text = text.Substring(0, 40);

                if (from.CheckAlive() && m_musicBox.CheckRights(from, message: true) == Rights.okay)
                {
                    m_musicBox.Name = Utility.FixHtml(text.Trim());

                    from.SendMessage("The music box has been renamed.");
                }
            }

            public override void OnCancel(Mobile from)
            {
            }
        }
        #endregion Context Menus
    }
}