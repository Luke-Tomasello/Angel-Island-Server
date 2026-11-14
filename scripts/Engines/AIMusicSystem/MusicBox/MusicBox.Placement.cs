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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.Placement.cs
 * Changelog
 *  11/8/26, Adam (2026 modernization, 10/30/26 to 11/8/26)
 *      Where a music box is (in the world, carried by a hired minstrel, elsewhere) decides whom it serves and how it
 *      answers; every rule that depends on it is here (speech, rights, access, replies, range, the listener check). A
 *      box in a container says "This must be in a house and be locked down to work." (it said "fail").
 *      Split from MusicBox.Speech.cs, MusicBox.Playback.cs, MusicBox.CommandHelpers.cs and MusicBox.cs; their history
 *      before 10/30/26 is in MusicBox.cs.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsUtil: GetDistanceToSqrt. Angel Island only (#if ANGEL_ISLAND): the hired minstrel's box.
 *      A world box notices a player walking up to it (within 3 tiles): a composer collects music box credit owed to them
 *      (MusicLibrary.CollectCredits).
 *      ComposerVisit (was CollectCredits): the composer is also told review decisions on their songs (TellComposer).
 */

using Server.Diagnostics;
using Server.Mobiles;
using Server.Multis;
using System;

namespace Server.Items
{
    public partial class MusicBox
    {
        #region Placement
        /// <summary>
        /// Where a music box is:
        ///  World      placed in the world (it works locked down in a house): players speak to it directly, within 5 tiles.
        ///  Carried    carried by a mobile. A hired minstrel (HireMinstrel.OnSpeech) relays its master's speech to the box
        ///             as its own, so the speaker is the minstrel: replies go to the minstrel's master, the box speaks
        ///             through the minstrel, plays from the minstrel's profile, and has no shop (buy, sales, submit...).
        ///  Elsewhere  in a container: not supported. It hears nothing and gives no rights.
        /// </summary>
        public enum BoxPlacement
        {
            World,
            Carried,
            Elsewhere,
        }

        public BoxPlacement Placement => Parent == null ? BoxPlacement.World : Parent is Mobile ? BoxPlacement.Carried : BoxPlacement.Elsewhere;

        // the mobile carrying this box (null unless Carried)
        private Mobile? Carrier => Parent as Mobile;

        // a carried box hears through its carrier (HireMinstrel.OnSpeech), not from the world
        public override bool HandlesOnSpeech => Placement == BoxPlacement.World;

        // a world box answers speakers within 5 tiles; a carried box, whoever its carrier relays
        private bool InSpeechRange(Mobile m)
        {
            return Placement == BoxPlacement.Carried || AimsUtil.GetDistanceToSqrt(this, m) <= 5;
        }

        // a world box notices a player walking up to it (within 3 tiles): a composer collects credit owed from earlier
        //  sales (MusicLibrary.CollectCredits returns at once for anyone owed nothing, and tries a composer at most every
        //  two minutes)
        public override bool HandlesOnMovement => Placement == BoxPlacement.World;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (m != null && m.Player && AimsUtil.GetDistanceToSqrt(this, m) <= 3)
                MusicLibrary.ComposerVisit(m);
        }
        #endregion Placement

        #region Rights and access
        private enum Rights
        {
            okay,
            lockdown,
            access,
            fail
        }

        // may this speaker use the box here? (World: locked down, and the speaker has access; Carried: yes, the carrier
        //  decides who speaks to it; Elsewhere: no)
        private Rights CheckRights(Mobile from, bool message = true)
        {
            try
            {
                // Robustness: if caller passes null, we cannot send messages and must fail.
                if (from == null)
                    return Rights.fail;

                switch (Placement)
                {
                    case BoxPlacement.World:
                        // must be locked down to operate
                        if (!IsLockedDown)
                        {
                            if (message)
                                from.SendLocalizedMessage(502692); // This must be in a house and be locked down to work.

                            return Rights.lockdown;
                        }

                        // must be accessible to the player (house access)
                        if (!HasMusicBoxAccess(from))
                        {
                            if (message)
                                from.SendLocalizedMessage(502436); // That is not accessible.

                            return Rights.access;
                        }

                        return Rights.okay;

                    case BoxPlacement.Carried:
                        // the carrier decides who may speak to its box (a minstrel relays only its master's speech)
                        return Rights.okay;

                    default:
                        // unsupported (in a container, etc.): it works only placed and locked down
                        if (message)
                            from.SendLocalizedMessage(502692); // This must be in a house and be locked down to work.

                        return Rights.fail;
                }
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);

                // Fail closed; only attempt messaging if we actually have a mobile.
                if (message && from != null)
                    from.SendLocalizedMessage(502436); // That is not accessible.

                return Rights.fail;
            }
        }

        // the house access a World box requires (staff always; a Carried box always)
        public bool HasMusicBoxAccess(Mobile m)
        {
            try
            {
                // Robustness: no mobile, no access.
                if (m == null)
                    return false;

                // Staff override: GameMaster and above always have access.
                if (m.AccessLevel >= AccessLevel.GameMaster)
                    return true;

                // A carried box (a minstrel's): no house involved.
                if (Placement == BoxPlacement.Carried)
                    return true;

                // Access is granted only if the box is in a house that grants this mobile access.
                BaseHouse? house = BaseHouse.FindHouseAt(this);
                return (house != null && house.HasAccess(m));
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false; // Fail closed on error.
            }
        }

        // a carried box has no shop: buy, sales, purchases, submit, remaster, rename, find and dupe need a music box
        private bool CheckEmbedded(Mobile? m = null, bool message = false)
        {
            if (Placement == BoxPlacement.Carried)
            {
                if (m != null && message) SendMessage(m, "You will need a music box for this functionality.");
                return true;
            }

            return false;
        }
        #endregion Rights and access

        #region Words and replies
        // the box's spoken words: overhead from the box, or from the mobile carrying it; a box elsewhere is silent
        public void Say(string text, int hue = 54)
        {
            if (Placement == BoxPlacement.Carried)
                Carrier!.PublicOverheadMessage(Network.MessageType.Regular, hue, true, text);
            else if (Placement == BoxPlacement.World)
                this.PublicOverheadMessage(Network.MessageType.Regular, hue, true, text);
        }

        // whom a private reply to this speaker reaches: the master of a creature carrying the box (a minstrel relays its
        //  master's speech as its own), else the speaker
        private Mobile? Recipient(Mobile? m)
        {
            return Carrier is BaseCreature bc && bc.ControlMaster != null ? bc.ControlMaster : m;
        }

        private void SendMessage(Mobile m, string text)
        {
            if (m == null)
                return;

            SendMessage(m, 0x3B2, text ?? string.Empty);   // 0x3B2: the music box's usual hue
        }

        private void SendMessage(Mobile m, int hue, string text)
        {
            try
            {
                Recipient(m)?.SendMessage(hue, text ?? string.Empty);
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
            }
        }
        #endregion Words and replies

        #region Listener check
        // RazorInstrument's status callback while a song plays: is the listener still there? A carried box plays while
        //  its minstrel stays near its master; a world box, while the player is connected and near the box.
        public bool CheckMobileStatus(Mobile m, IEntity source)
        {   // called back from RazorInstrument
            return CheckMobile(m, source);
        }
        private bool CheckMobile(Mobile m, IEntity source)
        {
            try
            {
                // RootParent may be null or deleted
                if (RootParent is Mobile parent_mob)
                {
#if ANGEL_ISLAND
                    // Embedded music box (e.g., on a minstrel)
                    if (parent_mob is HireMinstrel hm && hm.ControlMaster != null)
                    {
                        Mobile master = hm.ControlMaster;

                        if (master.Deleted || parent_mob.Deleted || master.Map == null || parent_mob.Map == null)
                            return false;

                        return parent_mob.Map == master.Map &&
                               master.GetDistanceToSqrt(parent_mob) < 15;
                    }
#endif

                    return false; // no control master or unexpected parent
                }

                // Standard music box
                if (m is PlayerMobile pm)
                {
                    if (pm.Deleted || pm.Map == null || this.Map == null)
                        return false;

                    var ns = pm.NetState;

                    return (ns != null &&
                            ns.Mobile != null &&
                            ns.Running &&
                            this.Map == pm.Map &&
                            AimsUtil.GetDistanceToSqrt(this, pm) < 15);
                }

                // If it's some other Mobile type
                if (m != null && m.Deleted)
                    return false;

                return false;
            }
            catch (Exception ex)
            {
                AimsLog.LogException(ex);
                return false;
            }
        }
        #endregion Listener check
    }
}