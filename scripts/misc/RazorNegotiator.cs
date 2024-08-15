/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
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

/* Scripts\misc\RazorNegotiator.cs
 * ChangeLog:
 *  6/12/2023, Adam (GMN Razor Enforcement)
 *      We now define new packets to support a mild cryptographic handshake with Razor.
 *      If Razor fails the handshake, and ForceGMNRZR is set, the user will get a dialog
 *      about 20s after logging in informing them of the rule, and that they will be 
 *      disconnected shortly.
 *      Mechanism: We encode the netstate seed and send it to razor disguised as a 
 *          standard RazorFeatureControl packet. Our version of razor will recognize this 
 *          encoded packet, and respond with one of three new packets.
 *          Any wrong answers will invalidate any future correct answers (for this session.)
 *          I.e., you can't just blast all three packets and expect to snake through.
 *          The encoded packet instructs Razor which of the packets to use. Without this
 *          knowledge, Razor will fail the check, and the player will be disconnected.
 */
using Server.Accounting;
using Server.Diagnostics;
using Server.Network;
using System;
using System.Collections;

namespace Server.Misc
{
    public class RazorFeatureControl
    {
        public const bool Enabled = true; // Is the "Feature Enforced" turned on?
        public const bool KickOnFailure = true; // When true, this will cause anyone who does not negotiate (include those not running Razor at all) to be disconnected from the server.
        public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(20.0); // How long to wait for a handshake response before showing warning and disconnecting
        public static readonly TimeSpan DisconnectDelay = TimeSpan.FromSeconds(10.0); // How long to show warning message before they are disconnected
        public const string RazorNegotiateMessage =
            "The server was unable to negotiate features with Razor on your system." +
            "<BR>You must be running the latest version of Razor from " +
            "<a href=\"http://www.game-master.net/\"> game-master.net</a>" +
            " to play on Siege Perilous or Angel Island." +
            "<BR>Once you have Razor installed and running, go to the <B>Options</B> tab and" +
            " check the box in the lower right-hand corner marked <B>Negotiate features with server</B>." +
            "  Once you have this box checked, you may log in and play normally.<BR>" +
            "You will be disconnected shortly.";
        public const string RazorCodeMessage =
            "Invalid Assistant detected." +
            "<BR>You must be running the latest version of Razor from " +
            "<a href=\"http://www.game-master.net/\"> game-master.net</a>" +
            " to play on Siege Perilous or Angel Island.<BR>" +
            "You will be disconnected shortly.";
        public const string RazorCodeWarning =
            "Invalid Assistant detected." +
            "<BR>You must be running the latest version of Razor from " +
            "<a href=\"http://www.game-master.net/\"> game-master.net</a>" +
            " to play on Siege Perilous or Angel Island.";

        public static void Configure()
        {
            // TODO: Add your server's feature allowances here
            // For example, the following line will disallow all looping macros on your server
            //DisallowFeature( RazorFeatures.LoopedMacros );
            DisallowFeature(RazorFeatures.FilterLight);
            DisallowFeature(RazorFeatures.PoisonedChecks);
            DisallowFeature(RazorFeatures.OverheadHealth);
        }

        [Flags]
        public enum RazorFeatures : ulong
        {
            None = 0,

            FilterWeather = 1 << 0, // Weather Filter
            FilterLight = 1 << 1, // Light Filter
            SmartTarget = 1 << 2, // Smart Last Target
            RangedTarget = 1 << 3, // Range Check Last Target
            AutoOpenDoors = 1 << 4, // Automatically Open Doors
            DequipOnCast = 1 << 5, // Unequip Weapon on spell cast
            AutoPotionEquip = 1 << 6, // Un/Re-equip weapon on potion use
            PoisonedChecks = 1 << 7, // Block heal If poisoned/Macro IIf Poisoned condition/Heal or Cure self
            LoopedMacros = 1 << 8, // Disallow Looping macros, For loops, and macros that call other macros
            UseOnceAgent = 1 << 9, // The use once agent
            RestockAgent = 1 << 10,// The restock agent
            SellAgent = 1 << 11,// The sell agent
            BuyAgent = 1 << 12,// The buy agent
            PotionHotkeys = 1 << 13,// All potion hotkeys
            RandomTargets = 1 << 14,// All random target hotkeys (Not target next, last target, target self)
            ClosestTargets = 1 << 15,// All closest target hotkeys
            OverheadHealth = 1 << 16,// Health and Mana/Stam messages shown over player's heads

            All = 0xFFFFFFFFFFFFFFFF  // Every feature possible
        }

        private static RazorFeatures m_DisallowedFeatures = RazorFeatures.None;

        public static void DisallowFeature(RazorFeatures feature)
        {
            SetDisallowed(feature, true);
        }

        public static void AllowFeature(RazorFeatures feature)
        {
            SetDisallowed(feature, false);
        }

        public static void SetDisallowed(RazorFeatures feature, bool value)
        {
            if (value)
                m_DisallowedFeatures |= feature;
            else
                m_DisallowedFeatures &= ~feature;
        }

        public static RazorFeatures DisallowedFeatures { get { return m_DisallowedFeatures; } }
    }

    public class RazorFeatureEnforcer
    {
        private static Hashtable m_Table = new Hashtable();
        private static TimerStateCallback OnHandshakeTimeout_Callback = new TimerStateCallback(OnRazorNegotiateTimeout);
        private static TimerStateCallback OnRazorCodeTimeout_Callback = new TimerStateCallback(OnRazorCodeTimeout);
        private static TimerStateCallback OnForceDisconnect_Callback = new TimerStateCallback(OnForceDisconnect);

        public static void Initialize()
        {
            if (RazorFeatureControl.Enabled)
            {
                EventSink.Login += new LoginEventHandler(EventSink_Login);

                ProtocolExtensions.Register(0xFF, true, new OnPacketReceive(OnRazorNegotiateResponse));

                ProtocolExtensions.Register(0xFE, true, new OnPacketReceive(OnHandshakeCode1Response));
                ProtocolExtensions.Register(0xFD, true, new OnPacketReceive(OnHandshakeCode2Response));
                ProtocolExtensions.Register(0xFC, true, new OnPacketReceive(OnHandshakeCode3Response));
            }
        }
        private const string RazorCodeHandshake = "RazorCodeHandshake";
        private static void EventSink_Login(LoginEventArgs e)
        {
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled))
            {
                Mobile m = e.Mobile;
                if (m != null && m.NetState != null && m.NetState.Running)
                {
                    uint seed = 0;
                    #region RazorCode
                    seed = (uint)Utility.GetStableHashCode(string.Format("{0}{1}", RazorCodeHandshake, (uint)m.NetState.m_Seed));
                    if (CoreAI.FakeBadGMNRZR)
                        // pervert the seed to force a failure
                        seed = ~seed;
                    Timer razorCodeTimer;
                    m.Send(new ExchangeRazorCode(seed));
                    if (m_Table.ContainsKey(seed))
                    {
                        razorCodeTimer = m_Table[seed] as Timer;
                        if (razorCodeTimer != null && razorCodeTimer.Running)
                            razorCodeTimer.Stop();
                    }

                    m_Table[seed] = razorCodeTimer = Timer.DelayCall(RazorFeatureControl.HandshakeTimeout, OnRazorCodeTimeout_Callback, m);
                    razorCodeTimer.Start();
                    #endregion RazorCode

                    #region Razor Feature Negotiation
                    if (true)
                    {
                        Timer handshakeTimer;
                        m.Send(new BeginRazorHandshake());

                        if (m_Table.ContainsKey(m))
                        {
                            handshakeTimer = m_Table[m] as Timer;
                            if (handshakeTimer != null && handshakeTimer.Running)
                                handshakeTimer.Stop();
                        }

                        m_Table[m] = handshakeTimer = Timer.DelayCall(RazorFeatureControl.HandshakeTimeout, OnHandshakeTimeout_Callback, m);
                        handshakeTimer.Start();
                    }
                    #endregion Razor Feature Negotiation
                }
            }
        }
        private static void OnRazorNegotiateResponse(NetState state, PacketReader pvSrc)
        {
            pvSrc.Trace(state);

            if (state == null || state.Mobile == null || !state.Running)
                return;

            Mobile m = state.Mobile;
            Timer t = null;
            if (m_Table.Contains(m))
            {
                t = m_Table[m] as Timer;

                if (t != null)
                    t.Stop();

                m_Table.Remove(m);
            }
        }
        public enum Talker
        {
            Unknown,
            Razor,
            CUO
        }
        private static Talker GetTalker(uint data, ref uint seed)
        {
            uint razor = (uint)Utility.GetStableHashCode(string.Format("{0}{1}", RazorCodeHandshake, (uint)seed));
            uint razor_payload, dont_care;
            MakePacket(out razor_payload, out dont_care, razor);
            if (data == razor_payload)
            {
                seed = razor;
                return Talker.Razor;
            }
            else
            {
                seed = 0;
                return Talker.Unknown;
            }
        }
        private static void HandleHandshakeCode(NetState state, uint code, PacketReader pvSrc)
        {
            uint data = pvSrc.ReadUInt32(); // chunk1

            uint seed = (uint)state.m_Seed;
            uint ResponseCode = code;

            switch (GetTalker(data, ref seed))
            {
                case Talker.Unknown:
                    return;
                case Talker.Razor:
                    break;
                case Talker.CUO:
                    break;
            }

            uint NeededCode = ServerVerify(seed);
            if (NeededCode == ResponseCode)
            {
                Timer t = null;
                if (m_Table.Contains(seed))
                {
                    t = m_Table[seed] as Timer;

                    if (t != null)
                        t.Stop();

                    m_Table.Remove(seed);

                    Utility.ConsoleWriteLine(string.Format("Valid Razor detected for: {0}: Account '{1}'",
                        state, (state.Mobile.Account as Accounting.Account).Username), ConsoleColor.Yellow);
                }
                else
                {
                    Utility.ConsoleWriteLine(string.Format("Previous invalid Razor detected for: {0}: Account '{1}'",
                        state, (state.Mobile.Account as Accounting.Account).Username), ConsoleColor.Red);
                }
            }
            else
            {
                Utility.ConsoleWriteLine(string.Format("Invalid Razor detected for: {0}: Account '{1}'",
                        state, (state.Mobile.Account as Accounting.Account).Username), ConsoleColor.Red);
                // if they failed the test, remove the seed from the table, but don't stop the timer
                //  this prevents someone from just blasting all three valid responses and getting a win
                if (m_Table.Contains(seed))
                    m_Table.Remove(seed);
            }
        }
        private static void OnHandshakeCode1Response(NetState state, PacketReader pvSrc)
        {
            pvSrc.Trace(state);

            if (state == null || state.Mobile == null || !state.Running)
                return;

            HandleHandshakeCode(state, 0xfe, pvSrc);
        }
        private static void OnHandshakeCode2Response(NetState state, PacketReader pvSrc)
        {
            pvSrc.Trace(state);

            if (state == null || state.Mobile == null || !state.Running)
                return;

            HandleHandshakeCode(state, 0xfd, pvSrc);
        }
        private static void OnHandshakeCode3Response(NetState state, PacketReader pvSrc)
        {
            pvSrc.Trace(state);

            if (state == null || state.Mobile == null || !state.Running)
                return;

            HandleHandshakeCode(state, 0xfc, pvSrc);
        }
        public static uint ServerVerify(uint seed)
        {
            uint packet = 0;
            uint session_low = seed & 0x0000ffff;   // clip to 4 bytes
            uint lookup = session_low << 16;            // special value
            int[] table = new[] { 0xfe, 0xfd, 0xfc };   // our table of legal packets
            packet = (uint)table[~lookup % table.Length];     // the right packet
            return packet;
        }
        private static void OnRazorNegotiateTimeout(object state)
        {
            Timer t = null;
            Mobile m = state as Mobile;
            if (m == null)
                return;

            m_Table.Remove(m);

            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RazorNegotiateWarnAndKick))
            {
                if (!RazorFeatureControl.KickOnFailure)
                {
                    Utility.ConsoleWriteLine(string.Format("Player '{0}' failed to negotiate Razor features.", m), ConsoleColor.Red);
                }
                else if (m.NetState != null && m.NetState.Running)
                {
                    m.SendGump(new Gumps.WarningGump(1060635, 30720, RazorFeatureControl.RazorNegotiateMessage, 0xFFC000, 420, 250, null, null));

                    if (m.AccessLevel <= AccessLevel.Player)
                    {
                        m_Table[m] = t = Timer.DelayCall(RazorFeatureControl.DisconnectDelay, OnForceDisconnect_Callback, m);
                        t.Start();
                    }
                }
            }
        }
        private static void OnRazorCodeTimeout(object state)
        {
            Timer t = null;
            if (state == null || state is not Mobile || (state as Mobile).NetState == null)
                return;

            Mobile m = state as Mobile;
            uint seed = (uint)Utility.GetStableHashCode(string.Format("{0}{1}", RazorCodeHandshake, (uint)(state as Mobile).NetState.m_Seed));

            m_Table.Remove(seed);
            bool hasException = ClientException.IsException((state as Mobile).NetState.Address);
            if (!CoreAI.ForceGMNCUO || hasException)
            {
                string text = string.Format("Player '{0}' failed the Razor 'Code' test{1}.", m, hasException ? ", but has a client exception" : "");
                Utility.ConsoleWriteLine(text, ConsoleColor.Red);
                LogHelper logger = new LogHelper("Failed Code Tests.log", overwrite: false, sline: true, quiet: true);
                if (!logger.Contains(text))
                    logger.Log(text);
                logger.Finish();
                Accounting.Account acct = (state as Mobile).Account as Accounting.Account;
                if (acct != null)
                    acct.SetFlag(Accounting.Account.AccountFlag.InvalidRazor, true);
                //m.SendGump(new Gumps.WarningGump(1060635, 30720, RazorFeatureControl.RazorCodeWarning, 0xFFC000, 420, 250, null, null));
            }
            else if (m.NetState != null && m.NetState.Running)
            {
                if (CoreAI.WarnBadGMNRZR)
                {
                    m.SendGump(new Gumps.WarningGump(1060635, 30720, RazorFeatureControl.RazorCodeWarning, 0xFFC000, 420, 250, null, null));
                }
                else
                {
                    m.SendGump(new Gumps.WarningGump(1060635, 30720, RazorFeatureControl.RazorCodeMessage, 0xFFC000, 420, 250, null, null));

                    if (m.AccessLevel <= AccessLevel.Player)
                    {
                        m_Table[m] = t = Timer.DelayCall(RazorFeatureControl.DisconnectDelay, OnForceDisconnect_Callback, m);
                        t.Start();
                    }
                }
            }
        }
        private static void OnForceDisconnect(object state)
        {
            if (state is Mobile)
            {
                Mobile m = (Mobile)state;

                if (m.NetState != null && m.NetState.Running)
                    m.NetState.Dispose();
                m_Table.Remove(m);

                Console.WriteLine("Player {0} kicked (Failed Razor handshake)", m);
            }
        }
        private sealed class BeginRazorHandshake : ProtocolExtension
        {
            public BeginRazorHandshake()
                : base(0xFE, 8)
            {
                m_Stream.Write((uint)((ulong)RazorFeatureControl.DisallowedFeatures >> 32));
                m_Stream.Write((uint)((ulong)RazorFeatureControl.DisallowedFeatures & 0xFFFFFFFF));
            }
        }
        private sealed class ExchangeRazorCode : ProtocolExtension
        {
            public ExchangeRazorCode(uint seed)
                : base(0xFE, 8)
            {
                uint chunk1, chunk2;
                MakePacket(out chunk1, out chunk2, seed);
                m_Stream.Write(chunk1);
                m_Stream.Write(chunk2);
            }
        }
        public static bool MakePacket(out uint chunk1, out uint chunk2, uint seed)
        {
            chunk1 = chunk2 = 0;
            uint session_low = seed & 0x0000ffff;               // clip to 4 bytes

            uint flag = 0x0000beef;                             // special flag beef
            flag = ~flag;                                       // 4110 (obscure)

            chunk1 = session_low << 16;                         // set high word
            chunk1 = chunk1 & 0xFFFF0000 | flag & 0x0000FFFF;   // set low

            chunk2 = ~seed;                                     // (obscure)           

            return chunk1 != 0 && chunk2 != 0;
        }
    }
}