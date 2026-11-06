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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.Prompts.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 10/29/26 to 11/5/26)
 *      The review, rejection reason and buy prompts.
 *      Split from MusicBox.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: paying for a track (Angel Island: all enrolled bank accounts).
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

                    MusicInfo mi = MusicLibrary.Repository[m_rsm];

                    if (mi.Purchasers == null)
                        throw new InvalidOperationException("MusicInfo.Purchasers invariant violated.");

                    mi.Purchasers.Add(new PurchaserInfo(from, mi.Price));

                    // Refresh play queue so the new purchase becomes eligible immediately.
                    if (m_musicBox.m_playContext != null)
                        m_musicBox.m_playContext.RefreshPlayQueue();

                    // Now pay the author (or credit if deposit fails).
                    // NOTE: We re-check ContainsKey above and reuse mi to avoid double indexing races.
                    if (mi.Owner != null)
                    {
                        if (Mobiles.Banker.Deposit(mi.Owner, m_price))
                        {
                            // Notify the seller via the buyer's notification list (used for later "you sold music" messages).
                            if (m_musicBox.m_playContext != null)
                            {
                                if (m_musicBox.m_playContext.PlayerNotificationList != null)
                                    m_musicBox.m_playContext.PlayerNotificationList.Add(new YouSoldMusic(mi.Owner, from, m_rsm, mi));
                            }
                        }
                        else
                        {
                            // Bank box can't hold gold: accumulate credit owed.
                            mi.Credit += m_price;
                        }

                        mi.TotalSales += m_price;
                        mi.Sold += 1;
                    }
                    else
                    {
                        // No owner: track becomes free.
                        mi.Price = 0;
                        mi.Sold += 1;
                    }

                    // Award points for selling a track (Bards Guild only, non-family IP).
                    if (m_rsm.Owner is PlayerMobile pm &&
                        pm.NpcGuild == NpcGuild.BardsGuild &&
                        !m_musicBox.FamilyIP(m_rsm.Owner, from))
                    {
                        AwardSkillFameKarma(pm, SalesFactor);
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
    }
}