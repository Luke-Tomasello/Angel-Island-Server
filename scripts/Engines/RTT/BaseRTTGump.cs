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

/* Scripts/Engines/RTT/BaseRTTGump.cs
 * CHANGELOG:
 *  2010.06.10 - Pix
 *      Added this.Closable = false; to constuctor
 *      Close AFK gump on timeout.
 *	3/22/10, adam
 *		uncomment the lines and instead turn it off via CoreAI.FeatureBits.RTTNotifyEnabled
 *	3/18/10, adam
 *		Remove the spam to the staff about success and failures and only broadcast when someone passes in less than 
 *		1 second. (probably cheating)
 *	3/22/09, Adam
 *		Add a timer that timesout and auto adds the player to the [macroer list if the gump is not addressed in a reasonable time.
 *		Use random numbers to reduce predictability.
 *  8/26/2007, Pix
 *      InitialVersion
 */

using Server.Engines;
using Server.Gumps;
using Server.Mobiles;
using System;

namespace Server.RTT
{
    public enum RTTResult
    {
        Passed,
        Failed,
        TimedOut
    }

    public delegate void RTTCallback(Mobile from, RTTResult result, object state);

    class RTTTimer : Timer
    {
        private BaseRTTGump m_owner;

        public RTTTimer(BaseRTTGump owner, TimeSpan ts)
            : base(ts)
        {
            m_owner = owner;
        }

        protected override void OnTick()
        {
            try
            {
                if (m_owner != null)
                    m_owner.HandleTimeout();
            }
            catch (Exception exc)
            {
                Server.Diagnostics.LogHelper.LogException(exc);
            }
        }
    }

    abstract class BaseRTTGump : Gump
    {
        private Mobile m_Mobile;
        private string m_strNotify = "";
        private string m_strSkill = "";
        private int m_CorrectResponse = -1;
        private int m_CorrectResponseOffset = 0;
        private DateTime m_RTTLaunched;
        private RTTTimer? m_timeout = null;

        // New: optional callback + state (when provided, RTT does not apply legacy punishment paths)
        private RTTCallback m_Callback;
        private object m_CallbackState;

        // New: guard to prevent double-resolution (timeout + response race)
        private bool m_Resolved;

        protected Mobile Mobile
        {
            get { return m_Mobile; }
            set { m_Mobile = value; }
        }

        protected string Notification
        {
            get { return m_strNotify; }
            set { m_strNotify = value; }
        }

        protected string Skill
        {
            get { return m_strSkill; }
            set { m_strSkill = value; }
        }

        protected int CorrectResponse
        {
            get { return m_CorrectResponse; }
            set { m_CorrectResponse = value; }
        }

        protected int CorrectResponseOffset
        {
            get { return m_CorrectResponseOffset; }
            set { m_CorrectResponseOffset = value; }
        }

        // Existing ctor (legacy behavior unchanged)
        public BaseRTTGump(Mobile from, string strNotice, string strSkill, int x, int y)
            : this(from, strNotice, strSkill, x, y, null, null)
        {
        }

        // New ctor overload: caller can supply callback/state
        public BaseRTTGump(Mobile from, string strNotice, string strSkill, int x, int y, RTTCallback callback, object callbackState)
            : base(x, y)
        {
            m_Mobile = from;
            m_strNotify = strNotice;
            m_strSkill = strSkill;

            m_Callback = callback;
            m_CallbackState = callbackState;

            this.Closable = false;

            // close any other RTT gumps
            from.CloseGump(typeof(BaseRTTGump));

            // notify staff
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RTTNotifyEnabled))
            {
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Administrator,
                    0x482,
                    string.Format("{0}({1}) is taking the RTT ({2}).", m_Mobile.Name, m_Mobile.Serial, strSkill));
            }

            // record the fact that the RTT test is being taken
            Server.Diagnostics.LogHelper lh = new Server.Diagnostics.LogHelper("RTT.log", false, true);
            lh.Log(Server.Diagnostics.LogType.Mobile, m_Mobile, string.Format("({0}) RTT Launched.", strSkill));
            lh.Finish();

            // child sets up gump UI + correct response
            SetupGump();

            m_RTTLaunched = DateTime.UtcNow;

            // timeout => counts as failure (legacy behavior may macroer-report)
            m_timeout = new RTTTimer(this, TimeSpan.FromSeconds(Utility.RandomList(90, 120, 180)));
            m_timeout.Start();
        }

        protected abstract void SetupGump();

        // New: called by RTTTimer
        internal void HandleTimeout()
        {
            try
            {
                // Stop timer
                if (m_timeout != null)
                {
                    m_timeout.Stop();
                    m_timeout = null;
                }

                if (m_Mobile == null || m_Mobile.Deleted)
                    return;

                // Close RTT gump (same as legacy)
                m_Mobile.CloseGump(typeof(BaseRTTGump));

                Resolve(RTTResult.TimedOut);
            }
            catch (Exception e)
            {
                Server.Diagnostics.LogHelper.LogException(e);
            }
        }

        // New: single choke point for outcomes
        private void Resolve(RTTResult result)
        {
            if (m_Resolved)
                return;

            m_Resolved = true;

            PlayerMobile? pm = m_Mobile as PlayerMobile;

            // If caller provided a callback, RTT becomes "present challenge, return result" only.
            if (m_Callback != null)
            {
                try
                {
                    m_Callback(m_Mobile, result, m_CallbackState);
                }
                catch (Exception e)
                {
                    Server.Diagnostics.LogHelper.LogException(e);
                }

                return;
            }

            // Legacy behavior (unchanged semantics)
            if (pm == null || m_Mobile == null || m_Mobile.Deleted)
                return;

            if (result == RTTResult.Passed)
            {
                pm.RTTResult(true);
            }
            else if (result == RTTResult.Failed)
            {
                pm.RTTResult(false);
            }
            else // TimedOut
            {
                // Legacy: timeout assumes AFK macroer
                MacroerCommand.ReportAsMacroer(World.GetSystemAcct(), pm);
            }
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            try
            {
                // kill our timeout timer
                if (m_timeout != null)
                {
                    m_timeout.Stop();
                    m_timeout = null;
                }

                int button = info.ButtonID;
                TimeSpan diff = DateTime.UtcNow - m_RTTLaunched;

                if (button == m_CorrectResponse + m_CorrectResponseOffset)
                {
                    // record answer
                    Server.Diagnostics.LogHelper lh = new Server.Diagnostics.LogHelper("RTT.log", false, true);
                    lh.Log(Server.Diagnostics.LogType.Mobile, m_Mobile, string.Format("PASSED the RTT in {0} ms", diff.TotalMilliseconds));
                    lh.Finish();

                    m_Mobile.SendMessage("Thanks for verifying that you're at your computer.");

                    if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RTTNotifyEnabled))
                    {
                        if (diff <= TimeSpan.FromSeconds(1.0))
                        {
                            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor,
                                0x22,
                                string.Format("{0}({1}) has quickly passed the RTT ({2} ms) ({3}).", m_Mobile.Name, m_Mobile.Serial, diff.TotalMilliseconds, m_strNotify));
                        }
                        else
                        {
                            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor,
                                0x482,
                                string.Format("{0}({1}) has passed the RTT ({2} ms).  ({3})", m_Mobile.Name, m_Mobile.Serial, diff.TotalMilliseconds, m_strNotify));
                        }

                        if (diff <= TimeSpan.FromSeconds(3.0))
                        {
                            Server.Diagnostics.LogHelper lh2 = new Server.Diagnostics.LogHelper("RTTAlert.log", false, true);
                            lh2.Log(Server.Diagnostics.LogType.Mobile, m_Mobile, string.Format("{0} ms", diff.TotalMilliseconds));
                            lh2.Finish();
                        }
                    }

                    Resolve(RTTResult.Passed);
                }
                else
                {
                    // record answer
                    Server.Diagnostics.LogHelper lh = new Server.Diagnostics.LogHelper("RTT.log", false, true);
                    lh.Log(Server.Diagnostics.LogType.Mobile, m_Mobile, string.Format("FAILED the RTT in {0} ms", diff.TotalMilliseconds));
                    lh.Finish();

                    m_Mobile.SendMessage("You have failed the AFK test.");

                    if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RTTNotifyEnabled))
                    {
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor,
                            0x482,
                            string.Format("{0}({1}) has failed the RTT. ({2})", m_Mobile.Name, m_Mobile.Serial, this.m_strNotify));
                    }

                    Resolve(RTTResult.Failed);
                }
            }
            catch (Exception e)
            {
                Server.Diagnostics.LogHelper.LogException(e);
            }
        }
    }
}

