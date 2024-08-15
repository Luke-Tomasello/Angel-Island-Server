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

/* Scripts\Misc\HardwareInfo.cs
 * CHANGELOG
 *  6/29/2023, Adam (GetHashCode)
 *      Replace GetStableHashCode() with a better version, GetStableHashCode2()
 *      We were getting a complaint from three guys that swear they are not sharing the same machine.
 *      While unlikely there is a hash collision, we:
 *      1. use this new, presumably better Stable Hash Code generator
 *      2. ADD in the boot drive serial instead of replacing the entire hash string with it.
 * 12/16/22, Adam (CheckDisconnect())
 *  If a player is able to skate around the IP Address limits on a shard (a VPN perhaps,) and the MachinInfo 
 *      is collected for that player, subsequent logins to that account will blocked as normal. However, since the MachinInfo 
 *      comes in after the account is created and while the player is making their first character, we must
 *      schedule a Cron job to kick them.
 *      When the MachinInfo is collected after account creation, we set the ExceedsMachineInfoLimit flag on the account. 
 *      The Cron job runs every 5 minutes and kicks these players.
 *  see also: CKickPlayers in Cron
 *  7/30/22, Adam
 *      Replace C#'s GetHachCode() with GetStableHashCode() since we need a deterministic hash.
 *      See the comments on GetStableHashCode() in Utils.cs
 *	8/24/10, Adam
 *		Add a HardwareInfo HASH function
 *		Save the hardware hash with the account
 *		Accounts will now use the hardware hash when the client fails to send a real hardware info packet
 */

using Server.Accounting;
using Server.Commands;
using Server.Diagnostics;
using Server.Misc;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using static Server.Accounting.Account;
using static Server.Misc.RazorFeatureEnforcer;

namespace Server
{
    public class HardwareInfo
    {
        public const string CUOCodeMessage =
            "Invalid Client detected." +
            "<BR>You must be running the latest version of ClassicUO from " +
            "<a href=\"http://www.game-master.net/\"> game-master.net</a>" +
            " to play on Siege Perilous or Angel Island.<BR>" +
            "You will be disconnected shortly.";
        public const string CUOCodeWarning =
            "Invalid Client detected." +
            "<BR>You must be running the latest version of ClassicUO from " +
            "<a href=\"http://www.game-master.net/\"> game-master.net</a>" +
            " to play on Siege Perilous or Angel Island.";

        private static Hashtable m_Table = new Hashtable();
        private static TimerStateCallback OnCUOCodeTimeout_Callback = new TimerStateCallback(OnCUOCodeTimeout);
        private static TimerStateCallback OnForceDisconnect_Callback = new TimerStateCallback(OnForceDisconnect);

        #region Data
        private int m_InstanceID;
        private int m_OSMajor, m_OSMinor, m_OSRevision;
        private int m_CpuManufacturer, m_CpuFamily, m_CpuModel, m_CpuClockSpeed, m_CpuQuantity;
        private int m_PhysicalMemory;
        private int m_ScreenWidth, m_ScreenHeight, m_ScreenDepth;
        private int m_DXMajor, m_DXMinor;
        private int m_VCVendorID, m_VCDeviceID, m_VCMemory;
        private int m_Distribution, m_ClientsRunning, m_ClientsInstalled, m_PartialInstalled;
        private string m_VCDescription;
        private string m_Language;
        private string m_Unknown;

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuModel { get { return m_CpuModel; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuClockSpeed { get { return m_CpuClockSpeed; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuQuantity { get { return m_CpuQuantity; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OSMajor { get { return m_OSMajor; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OSMinor { get { return m_OSMinor; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OSRevision { get { return m_OSRevision; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int InstanceID { get { return m_InstanceID; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScreenWidth { get { return m_ScreenWidth; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScreenHeight { get { return m_ScreenHeight; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScreenDepth { get { return m_ScreenDepth; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PhysicalMemory { get { return m_PhysicalMemory; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuManufacturer { get { return m_CpuManufacturer; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuFamily { get { return m_CpuFamily; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VCVendorID { get { return m_VCVendorID; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VCDeviceID { get { return m_VCDeviceID; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VCMemory { get { return m_VCMemory; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DXMajor { get { return m_DXMajor; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DXMinor { get { return m_DXMinor; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string VCDescription { get { return m_VCDescription; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Language { get { return m_Language; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Distribution { get { return m_Distribution; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ClientsRunning { get { return m_ClientsRunning; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ClientsInstalled { get { return m_ClientsInstalled; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PartialInstalled { get { return m_PartialInstalled; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Unknown { get { return m_Unknown; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string HashCode { get { return string.Format("{0:X}", GetHashCode()); } }
        #endregion Data
        public static void Initialize()
        {
            PacketHandlers.Register(0xD9, 0x10C, false, new OnPacketReceive(OnReceive));
            CommandSystem.Register("HWInfo", AccessLevel.GameMaster, new CommandEventHandler(HWInfo_OnCommand));
            EventSink.Login += new LoginEventHandler(EventSink_Login);
        }
        private const string CUOCodeHandshake = "CUOCodeHandshake";
        private static void EventSink_Login(LoginEventArgs e)
        {
            if (e.Mobile != null && e.Mobile.Account != null && e.Mobile.NetState != null)
                try
                {
                    #region Hardware Info Logging
                    LogHelper Logger = new LogHelper("HardwareInfo.log", false, true);
                    Account acct = e.Mobile.Account as Account;
                    string hi = "{null}";
                    if (acct.HardwareInfo != null)
                    {
                        hi = string.Format("{{{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {12}, {22}, {23}, {24}}}",
                            acct.HardwareInfo.m_InstanceID,
                            acct.HardwareInfo.m_OSMajor,
                            acct.HardwareInfo.m_OSMinor,
                            acct.HardwareInfo.m_OSRevision,
                            acct.HardwareInfo.m_CpuManufacturer,
                            acct.HardwareInfo.m_CpuFamily,
                            acct.HardwareInfo.m_CpuModel,
                            acct.HardwareInfo.m_CpuClockSpeed,
                            acct.HardwareInfo.m_CpuQuantity,
                            acct.HardwareInfo.m_PhysicalMemory,
                            acct.HardwareInfo.m_ScreenWidth,
                            acct.HardwareInfo.m_ScreenHeight,
                            acct.HardwareInfo.m_ScreenDepth,
                            acct.HardwareInfo.m_DXMajor,
                            acct.HardwareInfo.m_DXMinor,
                            acct.HardwareInfo.m_VCDescription,
                            acct.HardwareInfo.m_VCVendorID,
                            acct.HardwareInfo.m_VCDeviceID,
                            acct.HardwareInfo.m_VCMemory,
                            acct.HardwareInfo.m_Distribution,
                            acct.HardwareInfo.m_ClientsRunning,
                            acct.HardwareInfo.m_ClientsInstalled,
                            acct.HardwareInfo.m_PartialInstalled,
                            acct.HardwareInfo.m_Language,
                            acct.HardwareInfo.m_Unknown
                        );
                    }

                    Logger.Log(LogType.Mobile, e.Mobile,
                        string.Format("Current HardwareInfo={0}(hash={1}), Previous hash={2}",
                        hi,
                        acct.HardwareInfo != null ?
                            string.Format("{0:X}", acct.HardwareInfo.GetHashCode()) :
                            "null",
                        string.Format("{0:X}", acct.HardwareHash)));
                    Logger.Finish();
                    #endregion Hardware Info Logging

                    #region CUOCode
                    uint seed = (uint)Utility.GetStableHashCode(string.Format("{0}{1}", CUOCodeHandshake, (uint)e.Mobile.NetState.m_Seed));
                    if (CoreAI.FakeBadGMNCUO)
                        // pervert the seed to force a failure
                        seed = ~seed;
                    Timer CUOCodeTimer;
                    e.Mobile.Send(new ExchangeCUOCode(seed));
                    if (m_Table.ContainsKey(seed))
                    {
                        CUOCodeTimer = m_Table[seed] as Timer;
                        if (CUOCodeTimer != null && CUOCodeTimer.Running)
                            CUOCodeTimer.Stop();
                    }

                    m_Table[seed] = CUOCodeTimer = Timer.DelayCall(RazorFeatureControl.HandshakeTimeout, OnCUOCodeTimeout_Callback, e.Mobile);
                    CUOCodeTimer.Start();
                    #endregion CUOCode
                }
                catch (Exception ex) { LogHelper.LogException(ex); }
        }
        private static void OnCUOCodeTimeout(object state)
        {
            Timer t = null;
            if (state == null || state is not Mobile || (state as Mobile).NetState == null)
                return;

            Mobile m = state as Mobile;
            uint seed = (uint)Utility.GetStableHashCode(string.Format("{0}{1}", CUOCodeHandshake, (uint)(state as Mobile).NetState.m_Seed));

            m_Table.Remove(seed);
            bool hasException = ClientException.IsException((state as Mobile).NetState.Address);
            if (!CoreAI.ForceGMNCUO || hasException)
            {
                string text = string.Format("Player '{0}' failed the CUO 'Code' test{1}.", m, hasException ? ", but has a client exception" : "");
                Utility.ConsoleWriteLine(text, ConsoleColor.Red);
                LogHelper logger = new LogHelper("Failed Code Tests.log", overwrite: false, sline: true, quiet: true);
                if (!logger.Contains(text))
                    logger.Log(text);
                logger.Finish();
                Accounting.Account acct = (state as Mobile).Account as Accounting.Account;
                if (acct != null)
                    acct.SetFlag(AccountFlag.InvalidClient, true);

                //m.SendGump(new Gumps.WarningGump(1060635, 30720, HardwareInfo.CUOCodeWarning, 0xFFC000, 420, 250, null, null));
            }
            else if (m.NetState != null && m.NetState.Running)
            {
                if (CoreAI.WarnBadGMNCUO)
                {
                    m.SendGump(new Gumps.WarningGump(1060635, 30720, HardwareInfo.CUOCodeWarning, 0xFFC000, 420, 250, null, null));
                }
                else
                {
                    m.SendGump(new Gumps.WarningGump(1060635, 30720, HardwareInfo.CUOCodeMessage, 0xFFC000, 420, 250, null, null));

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

                Console.WriteLine("Player {0} kicked (Failed CUO handshake)", m);
            }
        }
        private sealed class ExchangeCUOCode : Packet
        {   // Handlers.Add(0x15, FollowR);
            public ExchangeCUOCode(uint seed)
                : base(0x15, 8 + 1)
            {
                uint chunk1, chunk2;
                RazorFeatureEnforcer.MakePacket(out chunk1, out chunk2, seed);
                m_Stream.Write(chunk1);
                m_Stream.Write(chunk2);
            }
        }

        [Usage("HWInfo")]
        [Description("Displays information about a targeted player's hardware.")]
        public static void HWInfo_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(HWInfo_OnTarget));
            e.Mobile.SendMessage("Target a player to view their hardware information.");
        }
        public static void HWInfo_OnTarget(Mobile from, object obj)
        {
            if (obj is Mobile && ((Mobile)obj).Player)
            {
                Mobile m = (Mobile)obj;
                Account acct = m.Account as Account;

                if (acct != null)
                {
                    HardwareInfo hwInfo = acct.HardwareInfo;

                    if (hwInfo != null)
                        CommandLogging.WriteLine(from, "{0} {1} viewing hardware info of {2}",
                            from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(m));

                    if (hwInfo != null)
                        from.SendGump(new Gumps.PropertiesGump(from, hwInfo));
                    else
                    {
                        from.SendMessage("No hardware information for that account was found.");
                        from.SendMessage("Previous hardware info hash code {0:X}.", acct.HardwareHash);
                    }
                }
                else
                {
                    from.SendMessage("No account has been attached to that player.");
                }
            }
            else
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(HWInfo_OnTarget));
                from.SendMessage("That is not a player. Try again.");
            }
        }
        public override int GetHashCode()
        {   // create a hash code that represents this clients computer
            string temp = "";

            temp += this.m_InstanceID.ToString();
            temp += this.m_OSMajor.ToString();
            temp += this.m_OSMinor.ToString();
            temp += this.m_OSRevision.ToString();
            temp += this.m_CpuManufacturer.ToString();
            temp += this.m_CpuFamily.ToString();
            temp += this.m_CpuModel.ToString();
            temp += this.m_CpuClockSpeed.ToString();
            temp += this.m_CpuQuantity.ToString();

            // OSI does PhysicalMemory wrong and cannot be relied upon
            // we therefore use it for our 'code lock' to bing our version of CUO to our server
            //temp += this.m_PhysicalMemory.ToString();     
            temp += this.m_ScreenWidth.ToString();
            temp += this.m_ScreenHeight.ToString();
            temp += this.m_ScreenDepth.ToString();
            temp += this.m_DXMajor.ToString();
            temp += this.m_DXMinor.ToString();
            if (!string.IsNullOrEmpty(this.m_VCDescription))
                temp += this.m_VCDescription.ToString();
            temp += this.m_VCVendorID.ToString();
            temp += this.m_VCDeviceID.ToString();
            temp += this.m_VCMemory.ToString();
            // Like PhysicalMemory above, we use Distribution to carry our 'code lock'.
            //temp += this.m_Distribution.ToString();       
            // the following may change and are therefore unreliable
            //temp += this.m_ClientsRunning.ToString();
            //temp += this.m_ClientsInstalled.ToString();
            //temp += this.m_PartialInstalled.ToString();
            if (!string.IsNullOrEmpty(this.m_Language))
                temp += this.m_Language.ToString();
            if (!string.IsNullOrEmpty(this.m_Unknown))
                // by far, the most robust measure (Boot Drive Serial) (AI's custom ClassicUO)
                temp += this.m_Unknown;

            return Utility.GetStableHashCode(temp);
        }
        public static void OnReceive(NetState state, PacketReader pvSrc)
        {
            pvSrc.ReadByte(); // 1: <4.0.1a, 2>=4.0.1a

            HardwareInfo info = new HardwareInfo();

            info.m_InstanceID = pvSrc.ReadInt32();
            info.m_OSMajor = pvSrc.ReadInt32();
            info.m_OSMinor = pvSrc.ReadInt32();
            info.m_OSRevision = pvSrc.ReadInt32();
            info.m_CpuManufacturer = pvSrc.ReadByte();
            info.m_CpuFamily = pvSrc.ReadInt32();
            info.m_CpuModel = pvSrc.ReadInt32();
            info.m_CpuClockSpeed = pvSrc.ReadInt32();
            info.m_CpuQuantity = pvSrc.ReadByte();
            info.m_PhysicalMemory = pvSrc.ReadInt32();
            info.m_ScreenWidth = pvSrc.ReadInt32();
            info.m_ScreenHeight = pvSrc.ReadInt32();
            info.m_ScreenDepth = pvSrc.ReadInt32();
            info.m_DXMajor = pvSrc.ReadInt16();
            info.m_DXMinor = pvSrc.ReadInt16();
            info.m_VCDescription = pvSrc.ReadUnicodeStringLESafe(64);
            info.m_VCVendorID = pvSrc.ReadInt32();
            info.m_VCDeviceID = pvSrc.ReadInt32();
            info.m_VCMemory = pvSrc.ReadInt32();
            info.m_Distribution = pvSrc.ReadByte();
            info.m_ClientsRunning = pvSrc.ReadByte();
            info.m_ClientsInstalled = pvSrc.ReadByte();
            info.m_PartialInstalled = pvSrc.ReadByte();
            info.m_Language = pvSrc.ReadUnicodeStringLESafe(4);
            info.m_Unknown = pvSrc.ReadStringSafe(64);

            HandleHandshakeCode(state, (uint)info.m_Distribution,/*code*/ (uint)info.m_PhysicalMemory/*payload*/);

            #region Hardware Info Maintenance and Logging
            Account acct = state.Account as Account;
            if (acct != null)
            {
                acct.HardwareInfo = info;
                acct.HardwareHash = info.GetHashCode();     // serialized - used again when no hardwareinfo is sent
                Utility.ConsoleWriteLine(string.Format("Hardware info acquired for account {0}", acct.ToString()), ConsoleColor.DarkGreen);
                Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(CheckDisconnect), new object[] { acct });
            }
            else
                Console.WriteLine("HardwareInfo lost");
            #endregion Hardware Info Maintenance and Logging
        }
        private static Talker GetTalker(uint data, ref uint seed)
        {
            uint cuo = (uint)Utility.GetStableHashCode(string.Format("{0}{1}", CUOCodeHandshake, (uint)seed));
            uint cuo_payload, dont_care;
            MakePacket(out cuo_payload, out dont_care, cuo);
            if (data == cuo_payload)
            {
                seed = cuo;
                return Talker.CUO;
            }
            else
            {
                seed = 0;
                return Talker.Unknown;
            }
        }
        private static void HandleHandshakeCode(NetState state, uint code, uint data)
        {
            uint seed = (uint)state.m_Seed;
            uint ResponseCode = code;

            switch (GetTalker(data, ref seed))
            {
                case Talker.Unknown:
                    return;
                case Talker.Razor:
                    return;
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

                    Utility.ConsoleWriteLine(string.Format("Valid Client detected for: {0}: Account '{1}'",
                        state, (state.Mobile.Account as Accounting.Account).Username), ConsoleColor.Yellow);
                }
                else
                {
                    Utility.ConsoleWriteLine(string.Format("Previous invalid Client detected for: {0}: Account '{1}'",
                        state, (state.Mobile.Account as Accounting.Account).Username), ConsoleColor.Red);
                }
            }
            else
            {
                Utility.ConsoleWriteLine(string.Format("Invalid Client detected for: {0}: Account '{1}'",
                        state, (state.Mobile.Account as Accounting.Account).Username), ConsoleColor.Red);
                // if they failed the test, remove the seed from the table, but don't stop the timer
                //  this prevents someone from just blasting all three valid responses and getting a win
                if (m_Table.Contains(seed))
                    m_Table.Remove(seed);
            }
        }
        private static void CheckDisconnect(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is Account acct)
            {
                if (!AccountHardwareLimiter.IsOk(acct))
                {
                    Server.Diagnostics.LogHelper.LogBlockedConnection(
                        string.Format("Login: {0}: Past machine limit threshold. Player will be kicked", acct)
                        );
                    // tell other accounts on this machine what's going on
                    AccountHardwareLimiter.Notify(acct);
                    // bye bye! (See Cron for the 'kick player' job)
                    acct.SetFlag(Account.AccountFlag.ExceedsMachineInfoLimit, true);
                }
                else
                    acct.SetFlag(Account.AccountFlag.ExceedsMachineInfoLimit, false);
            }
        }
    }
}