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
 *  11/22/26, Adam (listening in)
 *      What a Listener box says also reaches whoever overhears the person it serves (MusicAudience).
 *  11/21/26, Adam (public music boxes, 11/19/26 to 11/21/26)
 *      A Public box in the world needs no house: CheckRights asks Attend instead. It hears only someone within
 *      PublicRange (2) tiles, and serves one person at a time: whoever used it last keeps it while they stay near and
 *      use it within AttentionTime (2 minutes); a person is served by one public box at a time (another box taking them
 *      releases this one).
 *      A public box keeps serving its listener while it plays for them (and they stay near), however long the song or
 *      playlist; the 2-minute rule applies again once it stops.
 *      Say (what a box announces and answers) follows its audience: a Listener box speaks to the person it serves alone,
 *      and a box with a Range speaks only that far.
 *      A Listener box still playing for someone another public box takes stops (it played on to no one), and who is
 *      served by which box forgets deleted people and boxes.
 *      Instead of stopping it: a public box still playing for someone holds them, wherever they've walked (they still
 *      hear it), and another public box refuses them ("You are already using <name>. Stop it there, or let it
 *      finish.") until it stops, finishes or someone else takes it; an idle box lets them go. Someone taking a box
 *      whose listener walked away while it played stops that song first (it plays on the listener's own music context:
 *      it went on to the newcomer, and kept its listener from playing anywhere else).
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
        public const int PublicRange = 2;
        public static readonly TimeSpan AttentionTime = TimeSpan.FromMinutes(2);
        private Mobile? m_Attending;                // whom a public box serves (kept after a lapse: a Listener box plays to them)
        private DateTime m_AttendedAt;
        private static readonly System.Collections.Generic.Dictionary<Mobile, MusicBox> s_Attended = new System.Collections.Generic.Dictionary<Mobile, MusicBox>();

        // who is served by which public box: deleted people and boxes, and boxes serving someone else now, drop out
        private static void PruneAttended()
        {
            System.Collections.Generic.List<Mobile>? stale = null;
            foreach (System.Collections.Generic.KeyValuePair<Mobile, MusicBox> kv in s_Attended)
                if (kv.Key.Deleted || kv.Value.Deleted || kv.Value.m_Attending != kv.Key)
                    (stale ??= new System.Collections.Generic.List<Mobile>()).Add(kv.Key);
            if (stale != null)
                foreach (Mobile m in stale)
                    s_Attended.Remove(m);
        }

        // a public box: near enough, and not serving someone else who is still here and still using it
        private Rights Attend(Mobile from, bool message)
        {
            if (!from.InRange(GetWorldLocation(), PublicRange))
            {
                if (message)
                    from.SendMessage("Come closer to use this music box.");
                return Rights.access;
            }
            Mobile? other = m_Attending;
            if (other != null && other != from && !other.Deleted && other.NetState != null
                && other.InRange(GetWorldLocation(), PublicRange) && (DateTime.UtcNow - m_AttendedAt < AttentionTime || IsThisDevicePlaying()))
            {
                if (message)
                    from.SendMessage("This music box is helping {0} right now: try another, or wait a moment.", other.Name);
                return Rights.access;
            }
            // one public box at a time. A box still playing for them holds them, wherever they've walked (they still hear
            //  it): this one refuses until they stop it there or it finishes, or someone else takes it. An idle box lets
            //  them go.
            if (s_Attended.TryGetValue(from, out MusicBox? previous) && previous != this && previous.m_Attending == from)
            {
                if (!previous.Deleted && previous.IsThisDevicePlaying())
                {
                    if (message)
                        from.SendMessage("You are already using {0}. Stop it there, or let it finish.",
                            string.IsNullOrWhiteSpace(previous.Name) ? "another music box" : previous.Name);
                    return Rights.access;
                }
                previous.m_Attending = null;
            }
            // taking a box from someone who walked away while it played for them: their song stops first (it plays on
            //  their own music context: it would go on to this newcomer, and keep them from playing anywhere else)
            if (m_Attending != null && m_Attending != from && IsThisDevicePlaying())
                StopPlaying();
            PruneAttended();
            s_Attended[from] = this;
            m_Attending = from;
            m_AttendedAt = DateTime.UtcNow;
            return Rights.okay;
        }

        private Rights CheckRights(Mobile from, bool message = true)
        {
            try
            {
                // Robustness: if caller passes null, we cannot send messages and must fail.
                if (from == null)
                    return Rights.fail;

                switch (Placement)
                {
                    case BoxPlacement.World when m_Public:
                        return Attend(from, message);

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
            else if (Placement == BoxPlacement.World && ListenerOnly)
            {   // a practice-room box speaks to the person it serves alone (what it plays, its replies), and to staff listening in
                Server.Misc.MusicAudience.SendWords(this, this, hue, text);
            }
            else if (Placement == BoxPlacement.World && Range > 0 && Map != null)
            {   // a box that carries less far than usual: its words too
                IPooledEnumerable clients = Map.GetClientsInRange(GetWorldLocation(), Range);
                foreach (Network.NetState ns in clients)
                    ns.Send(new Network.AsciiMessage(Serial, ItemID, Network.MessageType.Regular, hue, 3, Name ?? "", text));
                clients.Free();
            }
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