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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.Prompts.cs
 * Changelog
 *  11/15/26, Adam (the library)
 *      A purchase redraws the buyer's library window, if it is open, so the song shows Owned at once (RefreshLibrary).
 *  11/14/26, Adam
 *      A sale is recorded by RecordSale (the buy prompt's bookkeeping, unchanged, so it can be tested alone). An
 *      imported song ([MusicImport) pays no one: no one here composed it, so its owner gets no gold, credit, sale notice
 *      or sales points, and the price leaves the economy unless the shard sends it somewhere (AimsHost.OnImportedSale).
 *  11/8/26, Adam (2026 modernization, 10/29/26 to 11/8/26)
 *      The review, rejection reason and buy prompts.
 *      Split from MusicBox.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: paying for a track (Angel Island: all enrolled bank accounts).
 *      Every purchase calls AimsHost.OnMusicPurchased (buyer, song, record, price paid). A song the shard holds for
 *      someone else (owned by AimsHost.SystemMobile) queues no "you sold music" notice: there is no seller here to tell.
 *      The composer is paid through AimsHost.Deposit (it was Banker.Deposit), so a shard's own banking can take it. A sale
 *      their bank box can't take is still kept as credit; it's now paid at their next music box visit (CollectCredits).
 *      A rejection for 'Other' asks staff for a note to the composer (RejectionNotePrompt, one line, up to 100
 *      characters: what ClassicUO's chat box sends), kept as MusicInfo.Note and told to the composer.
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
        private class ReviewMusicBoxPrompt : Prompt
        {
            private readonly Mobile m_mobile;
            private readonly RolledUpSheetMusic m_rsm;
            private readonly MusicBox? m_musicbox;

            public ReviewMusicBoxPrompt(Mobile m, RolledUpSheetMusic rsm, MusicBox musicbox)
            {
                m_mobile = m;
                m_rsm = rsm;
                m_musicbox = musicbox;
            }

            public override void OnResponse(Mobile from, string text)
            {
                try
                {
                    // Robustness: normalize input.
                    if (string.IsNullOrWhiteSpace(text))
                        return;

                    // Cap length to keep prompts cheap and predictable.
                    if (text.Length > 40)
                        text = text.Substring(0, 40);

                    text = text.ToLowerInvariant().Trim();

                    // Robustness: validate critical refs.
                    if (from == null || m_musicbox == null || m_rsm == null)
                        return;

                    // Security: ensure the responding mobile is the expected mobile and has access.
                    if (!m_musicbox.CheckMobile(from, m_musicbox) || !m_musicbox.HasMusicBoxAccess(from))
                        return;

                    // Stop current playback on this device before review actions that can mutate the repo/queue.
                    if (m_musicbox.IsThisDevicePlaying())
                        m_musicbox.StopPlaying();

                    if (text != "approve" && text != "deny" && text != "delete")
                    {
                        from.SendMessage("You decide not to review the track.");
                        return;
                    }

                    // Robustness: global collections must exist.
                    if (text == "approve")
                    {
                        ApproveTrack(from, m_rsm);
                    }
                    else if (text == "delete")
                    {
                        // Delete: remove from repo, review queue and this box's playlist, and delete the track itself, as
                        //  the 'delete' command does (it used to stay behind in internal storage).
                        if (MusicLibrary.Repository.ContainsKey(m_rsm))
                            MusicLibrary.Repository.Remove(m_rsm);

                        MusicLibrary.Unqueue(m_rsm);

                        if (m_musicbox.m_playContext != null && m_musicbox.m_playContext.Playlist.Contains(m_rsm))
                            m_musicbox.m_playContext.Playlist.Remove(m_rsm);

                        from.SendMessage("You delete the track {0}.", m_rsm);

                        if (!m_rsm.Deleted)
                            m_rsm.Delete();
                    }
                    else if (text == "deny")
                    {
                        // Deny: ensure repo entry exists and mark rejected.
                        if (MusicLibrary.Repository.ContainsKey(m_rsm))
                        {
                            MusicLibrary.Repository[m_rsm].Status = SubmissionStatus.Rejected;
                        }
                        else
                        {
                            MusicLibrary.Repository.Add(m_rsm, new MusicInfo(m_rsm.Owner, m_rsm.Price));
                            MusicLibrary.Repository[m_rsm].Status = SubmissionStatus.Rejected;
                        }

                        MusicLibrary.Unqueue(m_rsm);

                        // Prompt for rejection reason.
                        m_mobile?.SendMessage("Please give one of the following reasons:");
                        foreach (string name in Enum.GetNames(typeof(RejectionReason)))
                            m_mobile?.SendMessage(name);

                        if (m_mobile != null)
                            m_mobile.Prompt = new RejectionReasonPrompt(m_rsm, m_musicbox);
                    }
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }
            }

            public override void OnCancel(Mobile from)
            {
                // Intentionally no-op.
            }
        }
        private class RejectionReasonPrompt : Prompt
        {
            private readonly RolledUpSheetMusic m_rsm;
            private readonly MusicBox? m_musicbox;

            public RejectionReasonPrompt(RolledUpSheetMusic rsm, MusicBox musicbox)
            {
                m_rsm = rsm;
                m_musicbox = musicbox;
            }

            public override void OnResponse(Mobile from, string text)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(text))
                        return;

                    if (text.Length > 40)
                        text = text.Substring(0, 40);

                    text = text.ToLowerInvariant().Trim();

                    if (from == null || m_musicbox == null || m_rsm == null)
                        return;

                    if (!m_musicbox.CheckMobile(from, m_musicbox) || !m_musicbox.HasMusicBoxAccess(from))
                        return;

                    // Robustness: repo may be null or track may not exist.
                    if (!MusicLibrary.Repository.ContainsKey(m_rsm))
                        return;

                    RejectionReason reason;
                    if (!Enum.TryParse<RejectionReason>(text, true, out reason))
                        reason = RejectionReason.Other;

                    MusicLibrary.Repository[m_rsm].Reason = reason;

                    from.SendMessage("Your reason for the rejection was {0}.", reason.ToString());

                    // Other: in staff's own words, for the composer (TellComposer, 'status')
                    if (reason == RejectionReason.Other)
                    {
                        from.SendMessage("Tell the composer why, in one line (up to {0} characters; Esc for no note):", RejectionNotePrompt.MaxLength);
                        from.Prompt = new RejectionNotePrompt(m_rsm);
                    }
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }
            }

            public override void OnCancel(Mobile from)
            {
                // Intentionally no-op.
            }
        }
        // staff's note to the composer on a rejection for 'Other' (MusicInfo.Note)
        private class RejectionNotePrompt : Prompt
        {
            // what ClassicUO's chat box sends at most (its MAX_MESSAGE_LENGHT); longer text from another client is cut here
            public const int MaxLength = 100;
            private readonly RolledUpSheetMusic m_rsm;

            public RejectionNotePrompt(RolledUpSheetMusic rsm)
            {
                m_rsm = rsm;
            }

            public override void OnResponse(Mobile from, string text)
            {
                try
                {
                    if (from == null || m_rsm == null || string.IsNullOrWhiteSpace(text)
                        || !MusicLibrary.Repository.TryGetValue(m_rsm, out MusicInfo? info) || info.Status != SubmissionStatus.Rejected)
                        return;

                    text = text.Trim();
                    if (text.Length > MaxLength)
                        text = text.Substring(0, MaxLength);
                    info.Note = text;
                    info.ComposerTold = false;      // told already (they were at a music box)? then told again, with the note
                    from.SendMessage("The composer will be told: {0}", MusicLibrary.ReviewReason(info));
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }
            }

            public override void OnCancel(Mobile from)
            {
                from?.SendMessage("No note: the composer will be told only that the song wasn't approved.");
            }
        }
        private class BuyMusicBoxPrompt : Prompt
        {
            private readonly Mobile m_mobile;
            private readonly RolledUpSheetMusic m_rsm;
            private readonly int m_price;
            private readonly MusicBox m_musicBox;

            public BuyMusicBoxPrompt(Mobile m, RolledUpSheetMusic rsm, MusicBox musicbox, int price)
            {
                m_mobile = m;
                m_rsm = rsm;
                m_musicBox = musicbox;
                m_price = price;
            }

            public override void OnResponse(Mobile from, string text)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(text))
                        return;

                    if (text.Length > 40)
                        text = text.Substring(0, 40);

                    text = text.ToLowerInvariant().Trim();

                    // Robustness: validate critical refs.
                    if (from == null || m_musicBox == null || m_mobile == null || m_rsm == null)
                        return;

                    // Security: ensure the responding mobile is the expected mobile and has access.
                    if (!m_musicBox.CheckMobile(from, m_musicBox) || !m_musicBox.HasMusicBoxAccess(from))
                        return;

                    if (text != "yes" && text != "y")
                    {
                        from.SendMessage("You decide not to buy the track.");
                        return;
                    }

                    // Robustness: track must still exist in repository to be purchased.
                    if (!MusicLibrary.Repository.ContainsKey(m_rsm))
                    {
                        from.SendMessage("That track is no longer available.");
                        return;
                    }

                    // Withdraw funds.
                    if (!AimsHost.Current.Withdraw(m_mobile, m_price))
                    {
                        from.SendMessage("You do not have sufficient funds to purchase that track.");
                        return;
                    }

                    // Update purchaser's library.
                    from.SendMessage("You purchased {0} for {1} gold.", m_rsm, m_price);
                    m_musicBox.Say(string.Format("'{0}', added to your library.", m_rsm), hue: 54);

                    m_musicBox.RecordSale(from, m_rsm, m_price);

                    // the library window, if open, shows it Owned now (MusicBox.LibraryGump.cs)
                    m_musicBox.RefreshLibrary(from);
                }
                catch (Exception ex)
                {
                    AimsLog.LogException(ex);
                }
            }

            public override void OnCancel(Mobile from)
            {
                // Intentionally no-op.
            }
        }

        /// <summary>
        /// A purchase, paid for (BuyMusicBoxPrompt): the buyer joins the song's purchasers and its composer is paid (or
        /// owed credit), told, and earns sales points; the shard hears of it. An imported song ([MusicImport) pays no
        /// one: no one here composed it, so its price leaves the economy, unless the shard sends it somewhere
        /// (AimsHost.OnImportedSale).
        /// </summary>
        internal void RecordSale(Mobile from, RolledUpSheetMusic rsm, int price)
        {
            MusicInfo mi = MusicLibrary.Repository[rsm];

            if (mi.Purchasers == null)
                throw new InvalidOperationException("MusicInfo.Purchasers invariant violated.");

            mi.Purchasers.Add(new PurchaserInfo(from, mi.Price));

            // Refresh play queue so the new purchase becomes eligible immediately.
            if (m_playContext != null)
                m_playContext.RefreshPlayQueue();

            if (mi.Imported)
            {
                // an imported song: counted, but no one is paid, told, or awarded points
                mi.TotalSales += price;
                mi.Sold += 1;
                AimsHost.Current.OnImportedSale(from, rsm, mi, price);
            }
            // Now pay the author (or credit if deposit fails).
            // NOTE: We re-check ContainsKey above and reuse mi to avoid double indexing races.
            else if (mi.Owner != null)
            {
                if (AimsHost.Current.Deposit(mi.Owner, price))
                {
                    // Notify the seller via the buyer's notification list (used for later "you sold music" messages).
                    //  A song the shard holds for someone else (owned by AimsHost.SystemMobile) has no seller here
                    //  to tell: the host hears of the sale through OnMusicPurchased.
                    if (m_playContext != null && mi.Owner != AimsHost.Current.SystemMobile)
                    {
                        if (m_playContext.PlayerNotificationList != null)
                            m_playContext.PlayerNotificationList.Add(new YouSoldMusic(mi.Owner, from, rsm, mi));
                    }
                }
                else
                {
                    // Bank box can't hold gold: accumulate credit owed (paid at the composer's next music box
                    //  visit: MusicLibrary.CollectCredits).
                    mi.Credit += price;
                }

                mi.TotalSales += price;
                mi.Sold += 1;
            }
            else
            {
                // No owner: track becomes free.
                mi.Price = 0;
                mi.Sold += 1;
            }

            // Award points for selling a track (Bards Guild only, non-family IP; not an imported song).
            if (!mi.Imported &&
                rsm.Owner is PlayerMobile pm &&
                pm.NpcGuild == NpcGuild.BardsGuild &&
                !FamilyIP(rsm.Owner, from))
            {
                AwardSkillFameKarma(pm, SalesFactor);
            }

            // the shard hears of every purchase, as it happens (what it paid, for which song, by whom)
            AimsHost.Current.OnMusicPurchased(from, rsm, mi, price);
        }
    }
}