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

/***************************************************************************
 *                      Startup Switch Processing
 *
 *  OVERVIEW
 *  --------
 *  Main.cs provides a centralized mechanism for assembling, validating,
 *  applying, and preserving the server's startup arguments.
 *
 *  The effective argument list may come from more than one source:
 *
 *      1. The process command line.
 *      2. Temporary out-of-band switches supplied through:
 *
 *             <drive>:\GMN\switches.txt
 *
 *      3. Interactive server selection when no shard was specified.
 *
 *  These sources are combined into a single authoritative string[] before
 *  startup configuration is applied. Subsequent switch processing should
 *  operate on this final argument array rather than reconstructing or
 *  reparsing the Arguments string.
 *
 *
 *  OUT-OF-BAND SWITCHES
 *  --------------------
 *  switches.txt provides a convenient way to temporarily inject startup
 *  switches across multiple shards without modifying their individual
 *  launch scripts.
 *
 *  This is intended primarily for exceptional operations such as migrations,
 *  transitions, or other one-time administrative startup requirements.
 *
 *  When switches.txt contains switches not already present on the command
 *  line, the administrator is prompted:
 *
 *      (A)pply  - apply the additional switches to this shard
 *      (S)kip   - ignore them for this shard
 *      (D)elete - delete switches.txt and globally retire the switches
 *
 *  The file itself is authoritative. A shard waiting at the prompt checks
 *  that switches.txt still exists before applying its previously-read
 *  contents. Consequently, deleting the file from one shard prevents other
 *  waiting shards from subsequently applying those switches.
 *
 *
 *  PROCESSING FLOW
 *  ---------------
 *
 *      original command line
 *              |
 *              v
 *      AddGMNSwitches()          // called once
 *              |
 *              v
 *      ParseArgs()               // validates/selects shard
 *              |
 *              +-- prompt for shard if necessary
 *              |
 *              v
 *      final string[] args       // authoritative argument set
 *              |
 *              v
 *      SetSwitches()             // applies Core/RuleSet state
 *              |
 *              v
 *      variant processing, etc.
 *
 *
 *  ARGUMENTS
 *  ---------
 *  The Arguments string is derived from the final argument array for later
 *  display/relaunch purposes. It is not the authoritative source for switch
 *  processing.
 *
 *  Ephemeral state switches such as -restarted and -crashed are consumed
 *  during the current startup but deliberately omitted from Arguments so
 *  they are not propagated indefinitely through subsequent relaunches.
 *
 *  When adding a new startup switch, normally add its interpretation to
 *  SetSwitches(). If it affects argument assembly, shard selection, or
 *  out-of-band processing itself, modify the appropriate earlier stage
 *  instead.
 *
 ***************************************************************************/

/* Server\Main.cs
 * ChangeLog:
 *  8/15/2024, Adam
 *      When using the remote database, verify connectivity during server up.
 *      Issue errors, and inform the user what has happened.
 *  8/13/2024, Adam
 *      Add a notion of BuildInfoDir. This directory contains the "Core 6.info" file
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Engines.Seasons;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Server
{
    public delegate void Slice();
    [CustomEnum(new string[] { "Pre-Alpha", "Alpha", "Pre-Beta", "Beta", "Production" })]
    public enum ReleasePhase
    {
        Pre_Alpha,
        Alpha,
        Pre_Beta,
        Beta,
        Production
    }

    public enum ServerID
    {
        Unknown,
        AngelIsland,
        SiegePerilous,
        Mortalis,
        Seasons,
        Upheaval,
        LoginServer
    }
    public class Core
    {
        private static bool m_Crashed;
        private static Thread timerThread;

        private static ArrayList m_DataDirectories = new ArrayList();
        private static Assembly m_Assembly;
        private static Process m_Process;
        private static Thread m_Thread;
        private static bool m_Service;
        //private static bool m_Debug;
        private static bool m_HaltOnWarning;
        private static bool m_VBdotNET;
        private static MultiTextWriter m_MultiConOut;
        private static bool m_Quiet = false;

        #region Error Log Shortcuts
        public static class LoggerShortcuts
        {
            public static string Boot = "ShardBoot.log";
            public static void BootError(string text)
            {
                LogHelper logger = new LogHelper(Boot, false, true);
                logger.Log(text);
                logger.Finish();
                Utility.Monitor.WriteLine(text, ConsoleColor.Red);
            }
        }
        #endregion Error Log Shortcuts

        private static int m_HITMISS;                   // special flag to allow test center players to view their hit/miss stats over time
        private static int m_DAMAGE;                    // special flag to allow test center players to view their damage dealt stats over time
        private static bool m_Building;                 // gives GMs access to certain world building commands during world construction

        // Disabling developer mode for now (for custom house building)
        //private static bool m_Developer;              // developers machine, allows derect login to any server
        private static bool m_EnableDbgSaves = false;   // normally debug turns off saves. this enables saves for debug

        private static bool m_Profiling;
        private static DateTime m_ProfileStart;
        private static TimeSpan m_ProfileTime;

        private static bool m_Patching = true;          // default is to run the patcher at startup.
        public static bool Patching
        {
            get { return m_Patching; }
            set { m_Patching = value; }
        }

        private static bool m_useLoginDB = false;
        public static bool UseLoginDB
        {
            get { return m_useLoginDB; }
            set { m_useLoginDB = value; }
        }

        private static MessagePump m_MessagePump;
        public static MessagePump MessagePump
        {
            get { return m_MessagePump; }
            set { m_MessagePump = value; }
        }

        public static Slice? Slice;

        public static bool Profiling
        {
            get { return m_Profiling; }
            set
            {
                if (m_Profiling == value)
                    return;

                m_Profiling = value;

                if (m_ProfileStart > DateTime.MinValue)
                    m_ProfileTime += DateTime.UtcNow - m_ProfileStart;

                m_ProfileStart = (m_Profiling ? DateTime.UtcNow : DateTime.MinValue);
            }
        }

        public static TimeSpan ProfileTime
        {
            get
            {
                if (m_ProfileStart > DateTime.MinValue)
                    return m_ProfileTime + (DateTime.UtcNow - m_ProfileStart);

                return m_ProfileTime;
            }
        }

#if DEBUG
        public static bool Debug { get { return true; } }
#else
        public static bool Debug { get { return false; } }
#endif

        public static bool Service { get { return m_Service; } }
        // Disabling developer mode for now (for custom house building)
        //public static bool Developer { get { return m_Developer; } }                                    //Disallow direct logins to other servers if we are not a developer
        internal static bool HaltOnWarning { get { return m_HaltOnWarning; } }
        internal static bool VBdotNet { get { return m_VBdotNET; } }
        public static ArrayList DataDirectories { get { return m_DataDirectories; } }
        public static Assembly Assembly { get { return m_Assembly; } set { m_Assembly = value; } }
        public static Process Process { get { return m_Process; } }
        public static Thread Thread { get { return m_Thread; } }
        public static MultiTextWriter MultiConsoleOut { get { return m_MultiConOut; } }

        // true if the server auto-restarted
        private static bool m_Restarted = false;
        // true if the server auto-restarted due to crash
        private static bool m_CrashRecovery = false;

        private static string m_Server = "Unknown Configuration";
        private static ServerID m_serverID = ServerID.Unknown;
        public static string Server { get { return m_Server; } }

        public static bool MigratePatchHistory { get; set; } = false;
        #region Time Management
        /* 
        * DateTime.Now and DateTime.UtcNow are based on actual system clock time.
        * The resolution is acceptable but large clock jumps are possible and cause issues.
        * GetTickCount and GetTickCount64 have poor resolution.
        * GetTickCount64 is unavailable on Windows XP and Windows Server 2003.
        * Stopwatch.GetTimestamp() (QueryPerformanceCounter) is high resolution, but
        * somewhat expensive to call because of its deference to DateTime.Now,
        * which is why Stopwatch has been used to verify HRT before calling GetTimestamp(),
        * enabling the usage of DateTime.UtcNow instead.
        */

        private static readonly bool _HighRes = Stopwatch.IsHighResolution;

        private static readonly double _HighFrequency = 1000.0 / Stopwatch.Frequency;
        private static readonly double _LowFrequency = 1000.0 / TimeSpan.TicksPerSecond;

        private static bool _UseHRT;

        public static bool UsingHighResolutionTiming { get { return _UseHRT && _HighRes && !Unix; } }

        public static long TickCount { get { return (long)Ticks; } }

        public static double Ticks
        {
            get
            {
                if (_UseHRT && _HighRes && !Unix)
                {
                    return Stopwatch.GetTimestamp() * _HighFrequency;
                }

                return DateTime.UtcNow.Ticks * _LowFrequency;
            }
        }
        #endregion Time Management
        #region Processor Info
        public static readonly bool Is64Bit = (IntPtr.Size == 8);

        private static bool m_MultiProcessor;
        private static int m_ProcessorCount;

        public static bool MultiProcessor { get { return m_MultiProcessor; } }
        public static int ProcessorCount { get { return m_ProcessorCount; } }

        private static bool m_Unix;

        public static bool Unix { get { return m_Unix; } }
        #endregion Processor Info
        public static string FindDataFile(string path)
        {
            if (m_DataDirectories.Count == 0)
                throw new InvalidOperationException("Attempted to FindDataFile before DataDirectories list has been filled.");

            string? fullPath = null;

            for (int i = 0; i < m_DataDirectories.Count; ++i)
            {
                fullPath = Path.Combine((string)m_DataDirectories[i], path);

                if (File.Exists(fullPath))
                    break;

                fullPath = null;
            }

            return fullPath;
        }

        public static string FindDataFile(string format, params object[] args)
        {
            return FindDataFile(string.Format(format, args));
        }
        #region Expansions

        private static Expansion m_Expansion;
        public static Expansion Expansion
        {
            get { return m_Expansion; }
            set { m_Expansion = value; }
        }
        public static bool CheckExpansion(Expansion ex)
        {
            return ExpansionInfo.Table[(int)m_Expansion].Publish >= ExpansionInfo.Table[(int)ex].Publish;
        }
        public static bool T2A
        {
            get { return CheckExpansion(Expansion.T2A); }
        }

        public static bool UOR
        {
            get { return CheckExpansion(Expansion.UOR); }
        }

        public static bool UOTD
        {
            get { return CheckExpansion(Expansion.UOTD); }
        }

        public static bool LBR
        {
            get { return CheckExpansion(Expansion.LBR); }
        }

        public static bool AOS
        {
            get { return CheckExpansion(Expansion.AOS); }
        }

        public static bool SE
        {
            get { return CheckExpansion(Expansion.SE); }
        }

        public static bool ML
        {
            get { return CheckExpansion(Expansion.ML); }
        }

        public static bool SA
        {
            get { return CheckExpansion(Expansion.SA); }
        }

        public static bool HS
        {
            get { return CheckExpansion(Expansion.HS); }
        }

        #endregion

        // Scenario 4: Plague of Despair
        // http://www.uoguide.com/List_of_BNN_Articles_(2002)
        public static DateTime PlagueOfDespair
        {
            get
            {
                // Enemies and Allies - April 11
                return new DateTime(2002, 4, 11);
            }
        }


        // http://www.uoguide.com/Savage_Empire
        // http://uo.stratics.com/database/view.php?db_content=hunters&id=176
        // Savage Empire was the title of an EA-run UO scenario, active from May to July of 2001.
        public static DateTime EraSAVE  // Savage Empire active from May to July of 2001. 
        {
            get
            {
                return new DateTime(2001, 5, 1);
            }
        }

        public static DateTime EraSA    // The Second Age (October 1, 1998) 
        {
            get
            {
                return new DateTime(1998, 10, 1);
            }
        }
        public static DateTime EraREN   // Renaissance (May 4, 2000)
        {
            get
            {
                return new DateTime(2000, 5, 4);
            }
        }
        public static DateTime EraTD    // Third Dawn (March 7, 2001)
        {
            get
            {
                return new DateTime(2001, 3, 7);
            }
        }
        public static DateTime EraLBR   // Lord Blackthorn's Revenge (February 24, 2002)
        {
            get
            {
                return new DateTime(2002, 2, 24);
            }
        }
        public static DateTime EraAOS   // Age of Shadows (February 11, 2003)
        {
            get
            {
                return new DateTime(2003, 2, 11);
            }
        }
        public static DateTime EraSE    // Samurai Empire (November 2, 2004) 
        {
            get
            {
                return new DateTime(2004, 11, 2);
            }
        }
        public static DateTime EraML    // Mondain's Legacy (August 30, 2005) 
        {
            get
            {
                return new DateTime(2005, 8, 30);
            }
        }

        public static DateTime EraABYSS // Stygian Abyss (September 8, 2009) 
        {
            get
            {
                return new DateTime(2009, 9, 8);
            }
        }
        public static DateTime EraHS // High Seas (October 12, 2010) Publish 68
        {
            get
            {
                return new DateTime(2010, 10, 12);
            }
        }

        public static DateTime LocalizationUO   // I think this was UO Third Dawn
        {
            get
            {
                return EraTD;
            }
        }

        /// <summary>
        /// Use this for deciding between beautiful old-school UO gumps and the new style gumps designed to hold variable length text.
        /// We believe it was UO Third Dawn that saw the massive Localization changes. With these changes came the ugly gumps to ensure vatiable
        /// length text would fit. 
        /// </summary>
        public static bool Localized    // I think this was UO Third Dawn
        {
            get
            {
                return PublishInfo.PublishDate >= LocalizationUO;
            }
        }

        /// <summary>
        /// Without naming a shard, describes whether this shard is attempting era accuracy.
        /// </summary>
        public static bool EraAccurate
        {
            get
            {   // add your Era Accurate shards here
                return RuleSets.SiegeRules() || RuleSets.SeasonsRules() || RuleSets.UpheavalRules();
            }
        }

        public static bool OldEthics
        {
            get
            {
                return Core.RuleSets.SiegeRules() && PublishInfo.Publish < 13.6;
            }
        }

        public static bool NewEthics
        {
            get
            {
                return !OldEthics;
            }
        }

        public static bool DefaultRedsInTown
        {
            get
            {   // add your 'reds in town' shards here:
                return Core.RuleSets.SiegeRules() || Core.RuleSets.UpheavalRules();
            }
        }
        #region RunUO2.6 Port compatibility functions
        #endregion RunUO2.6 Port compatibility functions
        public static bool OldStyleTinkerTrap
        {
            get
            {
                // Old-style tinker traps are enabled for pre-Publish 4, Angel Island,
                // Seasons 2, and Upheaval.
                //
                // Siege uses new-style traps for era accuracy.
                // Season 3 uses new-style traps because it is PvE.
                // Mortalis uses new-style traps because it is permanent death.
                return PublishInfo.Publish < 4 ||
                       Core.RuleSets.AngelIslandRules() ||
                       Core.RuleSets.SeasonsRules(2) ||
                       Core.RuleSets.UpheavalRules();
            }
        }

        public static bool NewStyleTinkerTrap
        {
            get
            {
                return !Core.OldStyleTinkerTrap;
            }
        }

        public static bool Factions
        {
            get
            {   // add your factions enabled servers here
                // 4/14/23, Adam: Turn off Factions for all shards
                // 8/31/23, Yoar: Enabled Factions for all shards
                // 8/31/23, Adam: Enabled Factions for Siege
                // 1/9/2026, Adam: remove named shards, and leave it up to dynamic config
                return (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FactionsEnabled) && PublishInfo.Publish >= 8.0);
            }
        }

        public static bool Ethics
        {
            get
            {
                // Siege Perilous is a special ruleset shard that launched on July 15, 1999. 
                // 4/14/23, Adam: Turn off Ethics for all shards
                // 8/31/23, Yoar: Enabled Ethics for all shards
                // 8/31/23, Adam: Enabled Ethics for Siege
                // 1/9/2026, Adam: remove named shards, and leave it up to dynamic config
                return (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.EthicsEnabled) && PublishInfo.PublishDate >= new DateTime(1999, 7, 15));
            }
        }
        #region Damage Tracker
        public static int HITMISS
        {
            get
            {
                return m_HITMISS;
            }
            set
            {
                m_HITMISS = value;
            }
        }
        public static int DAMAGE
        {
            get
            {
                return m_DAMAGE;
            }
            set
            {
                m_DAMAGE = value;
            }
        }
        #endregion Damage tracker
        private static ReleasePhase m_releasePhase;
        public static ReleasePhase ReleasePhase
        {
            get { return m_releasePhase; }
            set { m_releasePhase = value; }
        }
        public static ServerID ServerID
        {
            get { return m_serverID; }
            set { m_serverID = value; }
        }
        #region server configurations
        private static bool m_GMNTC_CFG;                     // Test Center
        private static bool m_GMNBETA_CFG;                   // BETA AI, allows most test center funcionality without the free loot
        private static bool m_GMNEV_CFG;                     // Event Shard
        private static bool m_USERP_CFG = true;             // Use Resource Pool. Default to 'true'
        private static bool m_Tribute_CFG;                  // Special player tribute shard
        public static int SeasonVariant { get; set; } = SeasonManager.ReadSeason();
        public static int SiegeVariant { get; set; } = 1;
        #endregion server configurations

        #region Rule Sets
        public static class RuleSets
        {
            #region SERVERS
            // OSI
            private static bool m_OSIAOS_SVR;
            private static bool m_OSISE_SVR;
            private static bool m_OSIML_SVR;
            // GMN
            private static bool m_GMNSP_SVR;                 // Siege
            private static bool m_GMNAI_SVR;                 // Angel Island
            private static bool m_GMNMO_SVR;                 // Mortalis
            private static bool m_GMNSE_SVR;                 // Seasons
            private static bool m_GMNUP_SVR;                 // Upheaval   
            private static bool m_GMNLS_SVR;                 // Login Server
            #endregion SERVERS

            #region SERVERS Variables
            public static bool AllShards
            {
                get
                {   // Some creatures/Systems should operate (if they exist) in the way they were
                    //  designed. Example: If you have the Angel Island Prison system, those mobiles 
                    //  must drop their scheduled loot in order for the prisoner to escape.
                    return true;
                }
            }
            public static bool LoginServer
            {
                get
                {   // used only for dedicated a login server (no player account info)
                    return GMNLS_SVR;
                }
            }
            public static bool DatabaseMaster
            {
                get
                {   // used only for dedicated a database master 
                    // since the *_CFG options can be applied to any *, we exclude this as
                    // a valid database master.
                    return Core.RuleSets.AngelIslandRules() && !GMNEV_CFG && !GMNTC_CFG;
                    // Note: UOBETA_CFG is special since it can be applied to beta versions of the DatabaseMaster.
                }
            }
            public static bool GMNLS_SVR
            {
                get { return m_GMNLS_SVR; }
                set
                {
                    if (value)
                    {
                        m_Server = "Login Server";
                        m_serverID = ServerID.LoginServer;
                        m_releasePhase = ReleasePhase.Production;
                        PublishInfo.Publish = 15;
                    }
                    m_GMNLS_SVR = value;
                }
            }
            public static bool GMNAI_SVR
            {   // January 9, 2002: killing shopkeepers in Felucca will now give a murder count, etc.
                // https://www.uoguide.com/Publish_15
                // (before the destruction of UO)
                get
                { return m_GMNAI_SVR; }
                set
                {
                    if (value)
                    {
                        m_Server = "Angel Island";
                        m_serverID = ServerID.AngelIsland;
                        m_releasePhase = ReleasePhase.Production;
                        PublishInfo.Publish = 15;
                    }

                    m_GMNAI_SVR = value;
                }
            }
            public static bool GMNSP_SVR
            {   /* Publish 13.6 (Siege Perilous Shards Only) - October 25, 2001
                 * Publish 13.5 - October 11, 2001
                 * Commodity Deeds, Repair Contracts, Secure House Trades
                 * https://www.uoguide.com/Publishes
                 */
                get
                { return m_GMNSP_SVR; }
                set
                {
                    if (value)
                    {
                        m_Server = "Siege Perilous";
                        m_serverID = ServerID.SiegePerilous;
                        m_releasePhase = ReleasePhase.Production;
                        PublishInfo.Publish = 13.6;
                    }
                    m_GMNSP_SVR = value;
                }
            }
            public static bool GMNMO_SVR
            {
                // January 9, 2002: killing shopkeepers in Felucca will now give a murder count, etc.
                // https://www.uoguide.com/Publish_15
                // (before the destruction of UO)
                get
                { return m_GMNMO_SVR; }
                set
                {
                    if (value)
                    {
                        m_Server = "Mortalis";
                        m_serverID = ServerID.Mortalis;
                        m_releasePhase = ReleasePhase.Production;
                        PublishInfo.Publish = 15;
                    }
                    m_GMNMO_SVR = value;
                }
            }
            public static bool GMNSE_SVR
            {
                // January 9, 2002: killing shopkeepers in Felucca will now give a murder count, etc.
                // https://www.uoguide.com/Publish_15
                // (before the destruction of UO)
                get { return m_GMNSE_SVR; }
                set
                {
                    if (value)
                    {
                        m_Server = "Seasons";
                        m_serverID = ServerID.Seasons;
                        m_releasePhase = ReleasePhase.Production;
                        PublishInfo.Publish = 15;
                    }
                    m_GMNSE_SVR = value;
                }
            }
            public static bool GMNUP_SVR
            {
                // January 9, 2002: killing shopkeepers in Felucca will now give a murder count, etc.
                // https://www.uoguide.com/Publish_15
                // (before the destruction of UO)
                get { return m_GMNUP_SVR; }
                set
                {
                    if (value)
                    {
                        m_Server = "Upheaval";
                        m_serverID = ServerID.Upheaval;
                        m_releasePhase = ReleasePhase.Production;
                        PublishInfo.Publish = 15;
                    }
                    m_GMNUP_SVR = value;
                }
            }
            #region OSI Servers
            public static bool OSIAOS_SVR
            {
                get
                {
                    return m_OSIAOS_SVR || m_OSISE_SVR;
                }
                set
                {
                    m_OSIAOS_SVR = value;
                }
            }
            public static bool OSISE_SVR
            {
                get
                {
                    return m_OSISE_SVR;
                }
                set
                {
                    m_OSISE_SVR = value;
                }
            }
            public static bool OSIML_SVR
            {
                get
                {
                    return m_OSIML_SVR;
                }
                set
                {
                    m_OSIML_SVR = value;
                }
            }
            #endregion OSI Servers
            #endregion SERVERS Variables
            #region Server Enablement
            #region OSI Servers
            public static bool AOSRules()
            {
                return (OSIAOS_SVR);
            }
            public static bool SamuraiEmpireRules()
            {   // UO Samurai Empire (Not to be confused with GMN Seasons shard (UOSE_SVR))
                return (OSISE_SVR);
            }
            public static bool MondainsLegacyRules()
            {
                return (OSIML_SVR);
            }
            #endregion OSI Servers

            public static bool AllServerRules()
            {   // typically used to denote an Angel Island feature that should be available everywhere
                return
                    // OSI Servers (don't really care about these as we never run them)
                    (m_OSIAOS_SVR || m_OSISE_SVR || m_OSIML_SVR) ||
                    // GMN Servers
                    (m_GMNAI_SVR || m_GMNSP_SVR || m_GMNMO_SVR || m_GMNSE_SVR || m_GMNUP_SVR);
            }
            public static bool LoginServerRules()
            {
                return (GMNLS_SVR);
            }
            public static bool AngelIslandRules()
            {
                return (GMNAI_SVR);
            }
            public static bool SiegeRules()
            {
                return (GMNSP_SVR);
            }
            public static bool SiegeRules(params int[] configurations)
            {
                if (!GMNSP_SVR)
                    return false;

                // SiegeVersion is the effective siege configuration:
                // configured normally, or overridden by command line.
                return configurations.Contains(SiegeVariant);
            }
            public static bool MortalisRules()
            {
                return (GMNMO_SVR);
            }
            public static bool MortalisStyleRules()
            {   // Mortalis, Seasons, and Upheaval
                return (GMNMO_SVR || GMNSE_SVR || GMNUP_SVR);
            }
            public static bool SeasonsRules()
            {
                // Core Seasons functionality, common to all seasons.
                return GMNSE_SVR;
            }

            public static bool SeasonsRules(params int[] seasons)
            {
                if (GMNSE_SVR)
                    return seasons.Contains(SeasonVariant);

                return false;
            }

            public static bool UpheavalRules()
            {   // GMN Upheaval
                return (GMNUP_SVR);
            }
            #region GMN Server Modes
            public static bool TestCenterRules()
            {
                return GMNTC_CFG;
            }
            public static bool EventShardRules()
            {
                return GMNEV_CFG;
            }
            #endregion GMN Server Modes


            #endregion Server Enablement
            #region Enabled Server Rules
            public static bool CanTeach()
            {
                return !(SiegeRules() || SeasonsRules(2));
            }

            public static bool UseDoppelgangers()
            {
                return MortalisRules() || SeasonsRules(2, 3) || UpheavalRules();
            }

            /* Publish 15 behavior:
             * - Angel Island and Mortalis use the shard-specific creature entangle mechanic.
             * - All other shards use the classic mounted-rider dismount mechanic.
             */
            public static bool BolaKnocksOffRider()
            {
                return !BolaEntanglesCreatures();
            }

            public static bool BolaEntanglesCreatures()
            {
                return AngelIslandRules() || MortalisRules();
            }
            public static bool BolaHybridModel()
            {
                return SeasonsRules(2, 3) || UpheavalRules();
            }
            public static bool PoisonedArrowsRules()
            {
                return AngelIslandRules() || SeasonsRules(2, 3) || UpheavalRules();
            }

            public static bool UsesReservedNames()
            {
                return MortalisRules() || SeasonsRules() || UpheavalRules();
            }
            public static bool AIPrisonSystem()
            {
                return AngelIslandRules() || SeasonsRules() || UpheavalRules();
            }
            public static bool PackUpStructureRules()
            {
                return CoreAI.EnablePackUp;
            }

            public static bool StackablePotions()
            { return MortalisRules() || SeasonsRules() || UpheavalRules(); }
            public static bool TentAnnexation()
            { return AngelIslandRules() || MortalisRules() || SeasonsRules() || UpheavalRules(); }
            public static bool MiniBossArmor()
            { return AngelIslandRules() || SiegeRules(2) || MortalisRules() || SeasonsRules() || UpheavalRules(); }
            public static bool SlayerInstruments()
            { return AllShards; }
            public static bool InstaDeathGuards()
            { return AngelIslandRules(); }
            public static bool ModifiedMindBlast()
            { return AngelIslandRules() || MortalisRules() || SeasonsRules() || UpheavalRules(); }
            public static bool AllowGhostBlindness()
            { return AngelIslandRules() || MortalisRules() || SeasonsRules() || UpheavalRules(); }
            public static bool AllowLostLandsAccess()
            {   // we currently have no shards that allow this access.
                return Core.T2A && false;
            }
            public static bool VampiresAreScary()
            {
                return true;
            }
            public static int ShardStableMinimum()
            {
                if (SiegeRules(2))
                    return 6;

                return 0;
            }

            public static int ShardStableBonus()
            {
                if (SiegeRules(2))
                    return 12;

                return 0;
            }
            public static bool HerdingBonus(Mobile from)
            {
                if (from is PlayerMobile pm)
                {
                    if (AngelIslandRules())
                        return true;

                    if (SeasonsRules() || UpheavalRules())
                        return false;

                    // minimum skill to be, and maintain grandfather status
                    bool enoughSkill = pm.Skills[SkillName.Herding].Base > 70.0;

                    // grandfather in some folks from Siege as long as they maintain herding above 70
                    if (Core.RuleSets.SiegeRules() && pm.GetPlayerBool(PlayerBoolTable.GrandfatheredHerding))
                        if (enoughSkill)
                            return true;
                        else
                            // lost grandfather status
                            pm.SetPlayerBool(PlayerBoolTable.GrandfatheredHerding, false);
                }

                return false;
            }
            public static int OSIStableMax(int offset = 0)
            {
                // standard OSI calcs
                double taming = 100;
                double anlore = 100;
                double vetern = 100;
                double sklsum = taming + anlore + vetern + offset;

                int max;

                if (sklsum >= 240.0)
                    max = 5;
                else if (sklsum >= 200.0)
                    max = 4;
                else if (sklsum >= 160.0)
                    max = 3;
                else
                    max = 2;

                if (taming >= 100.0)
                    max += (int)((taming - 90.0) / 10);

                if (anlore >= 100.0)
                    max += (int)((anlore - 90.0) / 10);

                if (vetern >= 100.0)
                    max += (int)((vetern - 90.0) / 10);

                return max;
            }
            public static bool CarpetStore()
            {
                return Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeRules();
            }
            public static bool PlantSystem()
            {
                return (PublishInfo.Publish >= 14.0 || AngelIslandRules() || MortalisRules() || SiegeRules() || SeasonsRules() || UpheavalRules());
            }
            /*
             * Auto-Dispel (server-side passive dispelling) existed in OSI-era UO well before AoS.
             * This implementation is based on the behavior documented in:
             *   - The UO Demo AI (leaked OSI server logic from the Renaissance period)
             *   - Stratics-era monster behavior archives (1999–2002)
             *   - Publish 15–era field reports from OSI shards
             *   - Early RunUO 1.0 AI (which directly mirrored the UO Demo)
             *
             * In OSI’s system, auto-dispel was not a spell cast, had no mana cost, and fired as part
             * of the creature’s “Evaluate Target” routine. Chance to dispel depended on two primary factors:
             *
             *   1. The creature’s Magery skill (scaled)
             *   2. A hidden per-creature “anti-summon strength” modifier used by Dragons, Balrons,
             *      Lich Lords, Elemental Lords, and similar high-end casters
             *
             * This method reconstructs that logic in a simplified, transparent form that preserves
             * the original feel without reproducing every quirk of the old AI tables.
             *
             * Result:
             *   - Low-tier monsters rarely auto-dispel
             *   - Mid-tier casters can sometimes auto-dispel
             *   - High-end creatures (Balrons, Ancient Wyrms, Elemental Lords, etc.)
             *     aggressively and almost instantly remove EVs/BS as in OSI Renaissance.
             */
            public static bool AutoDispelChance(Mobile m)
            {
                // Defensive: no creature, no auto-dispel.
                if (m == null || m.Deleted)
                    return false;

                // If Angel Island rules explicitly disable auto-dispel, honor that.
                if (Core.RuleSets.AngelIslandRules())
                    return false;

                // Auto-dispel is a Renaissance-and-later mechanic for our purposes.
                if (PublishInfo.PublishDate < Core.EraREN)
                    return false;

                // Only meaningful for creatures with magery (and only if they are set up to auto-dispel elsewhere).
                double magery = 0.0;

                try
                {
                    magery = m.Skills[SkillName.Magery].Value;
                }
                catch
                {
                    magery = 0.0;
                }

                // Normalize magery into a 0.0–1.0 factor (0 at 0 skill, ~0.83 at GM).
                double mageryFactor = Math.Max(0.0, Math.Min(1.0, magery / 120.0));

                // Per-creature anti-summon emphasis (dragons, balrons, lich lords, etc.).
                double creatureBonus = GetAutoDispelBonus(m); // 0.0–0.5 range

                // Base chance: half from magery, half from the specific creature flag.
                // Example:
                //   GM magery (factor ~0.83) + strong bonus (0.40) → ~0.82 chance before clamping.
                double chance = (mageryFactor * 0.5) + creatureBonus;

                // Clamp to [0,1]
                if (chance < 0.0)
                    chance = 0.0;
                else if (chance > 1.0)
                    chance = 1.0;

                return Utility.RandomDouble() < chance;
            }

            /// <summary>
            /// Returns an additional auto-dispel emphasis for "anti-summon" creatures.
            /// This is intentionally simple and easy to tune per shard.
            /// </summary>
            private static double GetAutoDispelBonus(Mobile m)
            {
                // Strongest anti-summon mobs: near-instantly delete EV/BS most of the time.
                if (m is Balron
                    || m is AncientWyrm
                    || m is ShadowWyrm)
                {
                    return 0.40; // 40%
                }

                // Very strong: dragons, wyrms, top casters, elemental lords, avengers.
                if (m is Dragon
                    || m is WhiteWyrm
                    || m is Daemon
                    || m is LichLord
                    || m is ElderGazer
                    || m is Titan
                    || m is PoisonElemental
                    //|| m is OphidianAvenger
                    || m is TerathanAvenger)
                {
                    return 0.30; // 30%
                }

                // Moderate: standard lich, elemental casters, reapers.
                if (m is Lich
                    || m is FireElemental
                    || m is AirElemental
                    || m is EarthElemental
                    || m is WaterElemental
                    || m is Reaper)
                {
                    return 0.20; // 20%
                }

                // Default: no special bias beyond magery.
                return 0.0;
            }

            public static bool MurderersGetOneThirdGold()
            {   // An additional penalty for being red on Siege Perilous includes the amount of gold found on the corpse of a creature 
                //	which they have killed will be one-third. For example, if a player would normally receive 600 gold off a monster, 
                //	if that player is instead red, he will receive 200 gold.
                // http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                // 5/12/23, Adam: We are discontinuing this for our Siege shard. There was pretty much a blowup
                //  on the forums, and several Siege vets swore this was never implemented. Not sure I believe this
                //  but it's not worth losing players over.
                return !SiegeRules();
            }
            public static bool ThemedTreasureMaps()
            {   // 7/13/2023, Adam: Enabling themed treasure map chests. See SpecialMibRares() for the MIB equivalent.)
                return AngelIslandRules() || SiegeRules() || SeasonsRules() || UpheavalRules();
            }
            public static bool SpecialMibRares()
            {   // 7/13/2023, Adam: See ThemedTreasureMaps() for the treasure map equivalent.)
                return AngelIslandRules() || SiegeRules() || SeasonsRules() || UpheavalRules();
            }
            public static bool SpecialMonsterRares()
            {   // 7/13/2023, Adam: Allow rare drops on dungeon monsters
                return AngelIslandRules() || SiegeRules() || SeasonsRules() || UpheavalRules();
            }
            public static bool DecorativeFurniture()
            {   // 7/14/2023, Adam: seems good for all shards
                return AngelIslandRules() || SiegeRules() || MortalisRules() || SeasonsRules() || UpheavalRules();
            }
            public static bool KinSystemEnabled()
            {   // 3/28/23, Adam: allow kin on Siege.. we'll see how it goes.
                // 4/13/23, Yoar: disabled the Kin system in favor of the new Alignment system.
                return false;//Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeRules();
            }
            public static bool AlignmentSystemEnabled()
            {
                return (AngelIslandRules() || SiegeRules() || SeasonsRules(2, 3) || UpheavalRules());
            }
            public static bool EnchantedScrollsEnabled()
            {
                return Core.RuleSets.SiegeRules();
            }
            public static bool RoTShardEnabled()
            {
                return SiegeRules();
            }
            public static bool UsingRoT(Mobile m, Skill skill)
            {
                return RoTShardEnabled() && m.UseROTForSkill(skill);
            }
            public static bool BaseVendorInvulnerability()
            {
                /* Publish 4 on March 8, 2000
                 * Shopkeeper Changes
                 * NPC shopkeepers will no longer have colored sandals. Evil NPC Mages will carry these items.
                 * NPC shopkeepers will give a murder count when they die unless they are criminal or evil. The issue with murder counts from NPCs not decaying (as reported on Siege Perilous) will also be addressed.
                 * If a shopkeeper is killed, a new shopkeeper will appear as soon as another player (other than the one that killed it) approaches.
                 * Any shopkeeper that is currently [invulnerable] will lose that status except for stablemasters.
                 * https://www.uoguide.com/Publish_4
                 */
                /* Publish 14 on November 30, 2001.
                 * NPC Updates
                 * Shopkeepers will now give murder counts correctly
                 * https://www.uoguide.com/Publish_14
                 */
                /* Publish 15 on January 9, 2002
                 * NPC and Shopkeepers Updates
                 * All methods of killing shopkeepers in Felucca will now give a murder count (ie using pets, provoking).
                 * https://www.uoguide.com/Publish_15
                 */
                // so yes, all basevendor are vulnerable except stablemasters aka AnimalTrainer.
                return PublishInfo.Publish < 4;
            }
            public static bool BulkOrderSystemRules()
            {   /* 2001
                 * Publish 14 - November 30, 2001
                 * New Player Experience, context sensitive menus, Blacksmithing BoD system, Animal Lore changes, Crafting overhaul, Factions updates
                 * https://www.uoguide.com/Publishes
                 * Bulk Order Deeds (commonly abbreviated as BOD) were introduced for the Blacksmith skill on November 30, 2001. 
                 * The concept behind it was to create a system in which crafters could make goods for NPCs and get rewards.
                 * It was originally intended that all craft skills would eventually see BODs. 
                 * But after the addition of Tailor BODs in Publish 16, the development team decided the system was not performing as originally intended. 
                 * It is unclear whether BODs will ever be added for another craft skill.
                 * https://www.uoguide.com/Bulk_Order_Deed
                 */
                return (PublishInfo.Publish >= 14) && PublishInfo.PublishDate >= new DateTime(2001, 11, 30) || CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BulkOrdersEnabled);
            }
            public static bool ContextMenuRules()
            {
                /* 2001
                 * Publish 14 - November 30, 2001
                 * New Player Experience, context sensitive menus, Blacksmithing BoD system, Animal Lore changes, Crafting overhaul, Factions updates
                 */
                return (PublishInfo.Publish >= 14) && PublishInfo.PublishDate >= new DateTime(2001, 11, 30);
            }
            public static bool StandardShardRules()
            {
                return (SiegeRules() || MortalisRules() || SeasonsRules() || UpheavalRules());
            }
            public static bool AnyAIShardRules()
            {
                return (AngelIslandRules() || SiegeRules() || MortalisRules() || SeasonsRules() || UpheavalRules());
            }
            public static bool TownshipRules()
            {   // currently all shards may have townships
                return AnyAIShardRules();
            }
            public static bool CanBuyHouseRules()
            {   // Publish 11
                // o) NPC real estate brokers will now buy house deeds back at 20% below the base price of the deed, or the original
                //	price paid when the deed was purchased from a vendor (whichever is lower).
                // o) House deed prices when reselling to real estate agents raised to the correct level on Siege Perilous
                // https://www.uoguide.com/Publish_11
                return PublishInfo.Publish >= 11;
            }
            public static bool ShopkeepersSellResourcesRules()
            {
                return !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.SiegeRules();
            }
            public static bool ShopkeepersBuyResourcesRules()
            {
                return !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.SiegeRules();
            }
            public static bool ShopkeepersBuyItemsRules()
            {
                return !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.SiegeRules();
            }
            public static bool UnusualChestRules()
            {   // note: turning off unusual chests simply converts existing chests to
                //  level 1, 3, and 3 dungeon treasure chests
                return AngelIslandRules();
            }
            public static bool ResourcePoolRules()
            {   // currently only AI may have ResourcePool
                //  a special commandline override switch '-norp' can be used for turning off the ResourcePool
                //  for testing purposes. 
                return AngelIslandRules() && m_USERP_CFG;
            }

            /// <summary>
            /// If enabled, we can buy static housing deeds and place static housing using the house placement tool.
            /// </summary>
            public static bool StaticHousingRules()
            {
                return GMNTC_CFG || AngelIslandRules() || SiegeRules();
            }
            public static bool UseToggleSpecialAbility()
            {   // not sure what the cutoff is here. Angel Island is 15 and uses the toggle, Siege does not
                return PublishInfo.Publish > 13.6;
            }
            public static bool CommodityDeedRules()
            {
                return AngelIslandRules() ||
                    // not sure about the publish date here
                    (StandardShardRules() && PublishInfo.Publish >= 13.5) ||
                    ResourcePoolRules();
            }
            public static bool AllowMountRules(Mobile from)
            {
                Region region = from.Region;

                // Angel Island never allows mounts anywhere.
                if (AngelIslandRules() || UpheavalRules())
                    return false;

                // On other shards, region-level NoMounts overrides general allowance.
                if (region is StaticRegion sr)
                    if (sr.NoMounts)
                        return false;

                // Season 2 does not allow mounts in dungeons
                if ((SeasonsRules(2)) && from is PlayerMobile)
                    if (Utility.WorldMap.Dungeons.Contains(from.Location) &&
                        !Utility.TeleporterRuleHelpers.InRegion(from, "Green Acres") &&
                        !Utility.TeleporterRuleHelpers.InRegion(from, "Custom Housing Area"))
                        return false;

                // Default for non-Angel Island shards: mounts allowed.
                return true;
            }
            public static bool ShardAllowsItemRules(Type type)
            {   // special items, usually custom allowance rules

                // custom
                if (type == typeof(SeedBox))
                    return AnyAIShardRules();
                // custom - plant system
                else if (type == typeof(Cask))
                    return AnyAIShardRules();
                // custom - music system
                else if (type == typeof(ManuscriptPaper))
                    return AnyAIShardRules();
                else if (type == typeof(MusicCompositionBook))
                    return AnyAIShardRules();
                else if (type == typeof(MusicBox))
                    return AnyAIShardRules();
                // RunUO actually does have this, but they changed to the 'flipped' graphic
                else if (type == typeof(SmithHammer))
                    return AnyAIShardRules();
                // custom - enhanced treasure map system
                else if (type == typeof(CartographersSextant))
                    return AnyAIShardRules();
                // RunUO actually does have this, but they use the AOS graphic
                else if (type == typeof(Runebook))
                    return AnyAIShardRules();
                // Again, RunUO just using a flipped/alternate graphic id
                else if (type == typeof(LargeCrate))
                    return AnyAIShardRules();
                // pre Ultima Online: Renaissance, https://www.uoguide.com/Britannia_News_Network
                //  Publish 5 on April 27, 2000. https://www.uoguide.com/Publish_5
                //  RunUO calls their's BroadcastCrystal.
                else if (type == typeof(CommunicationCrystal))
                    return AnyAIShardRules() && PublishInfo.Publish >= 5;
                // custom - township system
                else if (type == typeof(TownshipDeed))
                    // part of the Item namespace
                    return TownshipRules();
                //else if (type == typeof(Township.Boxwood))
                //return TownshipRules();
                else if (type.Namespace == "Township")
                    // everything in the "Township" namespace is conditioned on TownshipRules()
                    return TownshipRules();

                return false;
            }
            #endregion Enabled Server Rules
        }
        #endregion Rule Sets
        #region SERVER CONFIGURATIONS
        public static bool GMNEV_CFG
        {
            get
            {
                return m_GMNEV_CFG;
            }
        }

        public static bool GMNTC_CFG
        {
            get
            {
                return m_GMNTC_CFG;
            }
        }
        public static bool Tribute_CFG
        {
            get
            {   // tribute shard is run under the EV port number
                return m_Tribute_CFG && m_GMNEV_CFG;
            }
        }
        public static bool GMNBETA_CFG
        {
            get
            {
                return m_GMNBETA_CFG;
            }
        }
        #endregion SERVER CONFIGURATIONS

        public static bool Developing
        {
            get
            {   // put your local development machine name here to automatically flip the switch for development-only features.
                return Environment.MachineName.Equals(CoreAI.OwnerLocalMachine, StringComparison.OrdinalIgnoreCase);
            }
        }
        public static bool Building
        {
            get
            {
                return m_Building;
            }
        }

        #region Where
        private static string m_ExeDirectory;
        private static string m_DataDirectory;
        private static string m_LogsDirectory;
        private static string m_SharedDirectory;
        private static string m_ExePath;
        private static string m_RootDirectory;
        private static string m_SavesDirectory;
        private static void InitializePaths()
        {
            m_ExePath = Process.GetCurrentProcess().MainModule.FileName.Replace("vshost.", "");

            m_ExeDirectory = Path.GetDirectoryName(m_ExePath);

            if (String.IsNullOrEmpty(m_ExeDirectory))
                throw new ApplicationException("Cannot establish executable directory.");

            m_ExeDirectory = Path.GetFullPath(m_ExeDirectory);

            m_RootDirectory = DiscoverRoot(m_ExeDirectory);

            m_DataDirectory = Path.Combine(m_RootDirectory, "Data");
            m_LogsDirectory = Path.Combine(m_RootDirectory, "Logs");
            m_SavesDirectory = Path.Combine(m_RootDirectory, "Saves");

            m_SharedDirectory = Path.GetFullPath(
                Path.Combine(m_RootDirectory, "..", "Shared"));
        }
        private static string DiscoverRoot(string exeDirectory)
        {
            DirectoryInfo? directory = new DirectoryInfo(exeDirectory);

            while (directory != null)
            {
                if ((directory.Name.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
                     directory.Name.Equals("Release", StringComparison.OrdinalIgnoreCase)) &&
                    directory.Parent != null &&
                    directory.Parent.Name.Equals("bin", StringComparison.OrdinalIgnoreCase))
                {
                    if (directory.Parent.Parent == null)
                        break;

                    return directory.Parent.Parent.FullName;
                }

                directory = directory.Parent;
            }

            // No bin\Debug or bin\Release structure.
            // Therefore the executable directory itself is the root.
            return Path.GetFullPath(exeDirectory);
        }
        public static string ExePath
        {
            get { return m_ExePath; }
        }

        public static string ExeDirectory
        {
            get { return m_ExeDirectory; }
        }

        public static string RootDirectory
        {
            get { return m_RootDirectory; }
        }

        public static string DataDirectory
        {
            get { return m_DataDirectory; }
        }

        public static string LogsDirectory
        {
            get { return m_LogsDirectory; }
        }

        public static string SavesDirectory
        {
            get { return m_SavesDirectory; }
        }

        public static string SharedDirectory
        {
            get { return m_SharedDirectory; }
        }
        #endregion Where

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.IsTerminating ? "Error:" : "Warning:");
            Console.WriteLine(e.ExceptionObject);

            if (e.IsTerminating)
            {
                m_Crashed = true;

                bool close = false;

                try
                {
                    CrashedEventArgs args = new CrashedEventArgs(e.ExceptionObject as Exception);

                    EventSink.InvokeCrashed(args);

                    close = args.Close;
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                if (!close && !m_Service)
                {
                    try
                    {
                        for (int i = 0; i < m_MessagePump.Listeners.Length; i++)
                        {
                            m_MessagePump.Listeners[i].Dispose();
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                    if (SocketPool.Created)
                        SocketPool.Destroy();

                    Console.WriteLine("This exception is fatal, press return to exit");
                    Console.ReadLine();
                }

                m_Closing = true;
            }
        }

        private enum ConsoleEventType
        {
            CTRL_C_EVENT,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private delegate bool ConsoleEventHandler(ConsoleEventType type);
        private static ConsoleEventHandler m_ConsoleEventHandler;

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventHandler callback, bool add);

        private static bool OnConsoleEvent(ConsoleEventType type)
        {
            if (World.Saving || (m_Service && type == ConsoleEventType.CTRL_LOGOFF_EVENT))
                return true;

            Kill();

            return true;
        }

        #region HIDE_CLOSEBOX
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr RemoveMenu(IntPtr hMenu, uint nPosition, uint wFlags);

        internal const uint SC_CLOSE = 0xF060;
        internal const uint MF_GRAYED = 0x00000001;
        internal const uint MF_BYCOMMAND = 0x00000000;
        #endregion HIDE_CLOSEBOX

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            HandleClosed();
        }

        private static bool m_Closing;
        public static bool Closing { get { return m_Closing; } }

        public static void Kill()
        {
            Kill(false);
        }

        public static void Kill(bool restart)
        {
            HandleClosed();

            if (restart)
            {
                string args = Arguments ?? string.Empty;

                // Append -restarted (avoid duplicates)
                bool hasRestarted = args
                    .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(t => string.Equals(t, "-restarted", StringComparison.OrdinalIgnoreCase));

                if (!hasRestarted)
                {
                    if (!string.IsNullOrWhiteSpace(args))
                        args += " ";

                    args += "-restarted";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = ExePath,
                    Arguments = args,
                    UseShellExecute = false,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                Process.Start(psi);
            }

            m_Process.Kill();
        }

        private static void HandleClosed()
        {
            if (m_Closing)
                return;

            m_Closing = true;

            Console.Write("Exiting...");

            if (!m_Crashed)
                EventSink.InvokeShutdown(new ShutdownEventArgs());

            if (SocketPool.Created)
                SocketPool.Destroy();

            Timer.TimerThread.Set();

            Console.WriteLine("done");
        }

        private static AutoResetEvent m_Signal = new AutoResetEvent(true);
        public static void Set() { m_Signal.Set(); }

        public static void Main(string[] args)
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#endif
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            InitializePaths();

            #region HIDE_CLOSEBOX
#if !DEBUG
            IntPtr hMenu = Process.GetCurrentProcess().MainWindowHandle;
            IntPtr hSystemMenu = GetSystemMenu(hMenu, false);
            EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
            RemoveMenu(hSystemMenu, SC_CLOSE, MF_BYCOMMAND);
#endif
            #endregion HIDE_CLOSEBOX

            // Reset console colors at startup because console color state persists across process restarts.
            // Ensures a clean boot does not inherit the previous instance’s crash (red) foreground setting.
            // btw, Console.ResetColor() does not work in this context.
            Utility.ResetColor();

            #region PARSE ARGS

            args = ParseArgs(args);

            if (args.Length == 0)
                return;

            SetSwitches(args);

            // Arguments is now only a display/restart representation
            // of the authoritative argument array.
            Arguments = string.Join(" ",
                args.Where(arg =>
                    !Insensitive.Equals(arg, "-restarted") &&
                    !Insensitive.Equals(arg, "-crashed")));

            #endregion PARSE ARGS

            #region Override Versions

            int overrideVariant = 0;

            if (TryGetVariantOverride(args, "-season", out overrideVariant))
            {
                // If the command line includes a "season" argument followed by a number,
                // use that number as the season variant.
                SeasonVariant = overrideVariant;
            }

            if (TryGetVariantOverride(args, "-siege", out overrideVariant))
            {
                // If the command line includes a "siege" argument followed by a number,
                // use that number as the siege variant.
                SiegeVariant = overrideVariant;
            }

            bool TryGetVariantOverride(string[] args, string baseName, out int variant)
            {
                variant = 0;

                foreach (string arg in args)
                {
                    if (!arg.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string suffix = arg.Substring(baseName.Length);

                    if (int.TryParse(suffix, out variant))
                        return true;
                }

                return false;
            }

            #endregion Override Versions

            #region LOG SETUP
            try
            {
                if (m_Service)
                {
                    if (!Directory.Exists("Logs"))
                        Directory.CreateDirectory("Logs");

                    Console.SetOut(m_MultiConOut = new MultiTextWriter(Console.Out, new FileLogger("Logs/Console.log")));
                }
                else
                {
                    Console.SetOut(m_MultiConOut = new MultiTextWriter(Console.Out));
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            #endregion


            m_Thread = Thread.CurrentThread;
            m_Process = Process.GetCurrentProcess();
            m_Assembly = Assembly.GetEntryAssembly();

            if (m_Thread != null)
                m_Thread.Name = "Core Thread";

            if (ExeDirectory.Length > 0)
                Directory.SetCurrentDirectory(ExeDirectory);

            Version? ver = m_Assembly.GetName().Version;
            bool state = false;

            #region Header
            string phase = Core.ReleasePhase < ReleasePhase.Production
                ? $" ({Utility.GetCustomEnumNames(typeof(ReleasePhase))[(int)m_releasePhase]})" : string.Empty;

            string seasonSuffix = string.Empty;

            // Display persisted version number during startup before world load occurs.
            if (RuleSets.SeasonsRules())
                seasonSuffix = $" {SeasonVariant}";
            if (RuleSets.SiegeRules())
                seasonSuffix = $" {SiegeVariant}";

            // Precedence: Crashed overrides Restarted (no redundancy)
            string stateSuffix = m_CrashRecovery
                ? " (Crashed)"
                : (m_Restarted ? " (Restarted)" : string.Empty);

            string version = $"{Utility.BuildMajor()}.{Utility.BuildMinor()}.{Utility.BuildRevision()}";

            string text =
                $"{m_Server}{seasonSuffix}{stateSuffix} - [www.game-master.net] Version {version}, Build {Utility.BuildBuild()}";

            ConsoleColor color = Utility.BuildColor(Utility.BuildRevision()); // same color selection behavior as before

            Utility.Monitor.WriteLine($"{phase} {text}".TrimStart(), color);
            #endregion Header

            if (RuleSets.SiegeRules())
                Utility.Monitor.WriteLine("[Siege II is {0}.]", ConsoleColorEnabled(Core.RuleSets.SiegeRules(2)), TextEnabled(Core.RuleSets.SiegeRules(2)));

            Utility.Monitor.WriteLine("[UO Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(DataPath.GetUOPath("Ultima Online")));
            Utility.Monitor.WriteLine("[Current Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(Directory.GetCurrentDirectory()));
            Utility.Monitor.WriteLine("[Base Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(ExeDirectory));
            Utility.Monitor.WriteLine("[Data Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(DataDirectory));
            //Utility.Monitor.WriteLine("[Shared Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(SharedDirectory));
            Utility.Monitor.WriteLine("[Game Time Zone: {0}]", ConsoleColorInformational(), AdjustedDateTime.GameTimezone);
            Utility.Monitor.WriteLine("[Server Time Zone: {0}]", ConsoleColorInformational(), AdjustedDateTime.ServerTimezone);

            #region ZLib
            bool zlib_loaded = false;
            try
            {
                zlib_loaded = (Compression.Compressor.Version != null);
            }
            catch (Exception ex)
            {
                Core.LoggerShortcuts.BootError(string.Format("Configuration error \"{0}\" is missing or cannot be loaded.", "zlib"));
            }

            state = zlib_loaded;
            Utility.Monitor.WriteLine("[ZLib version {0} ({1}) loaded.]", ConsoleColorInformational(), Compression.Compressor.Version, Compression.Compressor.GetType().Name);
            #endregion ZLib

            #region Email
            if (EmailCheck() == true)
                Utility.Monitor.WriteLine("[All of the required Email environment variables are set.]", ConsoleColorInformational());
            else
                Utility.Monitor.WriteLine("[Some or all of the required Email environment variables are not set.]", ConsoleColorWarning());
            #endregion Email

            #region Patcher
            Utility.Monitor.WriteLine("[Patch Library Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(ExeDirectory));
            if (!CoreAI.ReportStatus())
            {
                //Core.LoggerShortcuts.BootError(
                //    string.Format("Patch system validation failed. Startup aborted."));
                //return;
                Core.Halt("Patch system validation failed. Startup aborted.");
            }
            Utility.Monitor.WriteLine(
                "[All patch libraries are valid and ready to load.]",
                ConsoleColorInformational());
            #endregion Patcher

            #region Ports
            if (PortsCheck() == false)
            {
                Utility.Monitor.WriteLine("[Ports not configured. Using defaults.]", ConsoleColorWarning());
                if (DefaultPorts() == false)
                {
                    Utility.Monitor.WriteLine("Unable to find Ports.json. Press return to exit.", ConsoleColorWarning());
                    Console.ReadKey();
                    return;
                }
            }
            #endregion Ports

            #region BuildInfo 
            if (BuildInfoCheck() == true)
                Utility.Monitor.WriteLine($"[Reading build.info from {Utility.GetShortPath(BuildInfoDir, raw: true)}.]", ConsoleColorInformational());
            else
                Utility.Monitor.WriteLine($"[Reading build.info from default location {BuildInfoDir}.]", ConsoleColorWarning());
            #endregion BuildInfo

            #region GeoIP
            if (GeoIPCheck() == true)
                Utility.Monitor.WriteLine("[Geo IP Configured.]", ConsoleColorInformational());
            else
                Utility.Monitor.WriteLine("[Geo IP is not configured. See AccountHandler.cs for setup instructions.]", ConsoleColorWarning());
            #endregion GeoIP

            #region Boot Errors
            if (Directory.Exists(Path.Combine(Core.DataDirectory)) == false)
                Core.LoggerShortcuts.BootError(string.Format("Configuration error \"{0}\" is missing.", Core.DataDirectory));

            if (File.Exists(Path.Combine(Core.BuildInfoDir, "Core 6.info")) == false)
                Core.LoggerShortcuts.BootError(string.Format("Configuration error \"{0}\" is missing.", Path.Combine(Core.BuildInfoDir, "Core 6.info")));

            #region Check Login DB
            if (m_useLoginDB)
            {
                bool adError = false;
                bool ipeError = false;
                bool fwError = false;
                string adPath = AccountsDatabase.GetDatabasePath(ref adError);
                string ipePath = IPExceptionDatabase.GetDatabasePath(ref ipeError);
                string fwPath = FirewallDatabase.GetDatabasePath(ref fwError);
                if (adError || ipeError || fwError)
                {
                    if (adError)
                        Console.WriteLine($"Unable to create {adPath}");

                    if (ipeError)
                        Console.WriteLine($"Unable to create {ipePath}");

                    if (fwError)
                        Utility.Monitor.WriteLine($"Unable to create {fwPath}", ConsoleColor.Red);

                    Utility.Monitor.WriteLine($"Use the following environment variables to relocate the database(s)", ConsoleColor.Yellow);
                    Utility.Monitor.WriteLine("AI.IPEXCEPTIONDB, AI.FIREWALLDB, AI.LOGINDB", ConsoleColor.Yellow);

                    while (true)
                    {
                        Utility.Monitor.WriteLine("Insufficient privileges to create one or more databases.", ConsoleColor.Yellow);
                        Utility.Monitor.WriteLine("Press 'c' to continue without axillary database support, or 'q' to quit.", ConsoleColor.Yellow);

                        string input = Console.ReadLine().ToLower();
                        if (input.StartsWith("c"))
                        {
                            m_useLoginDB = false;
                            break;
                        }
                        else if (input.StartsWith("q"))
                        {
                            return;
                        }
                    }
                }
            }
            #endregion Check Login DB

            #endregion Boot Errors
#if DEBUG
            Utility.Monitor.WriteLine("[Debug Build Enabled]", ConsoleColorInformational());
#else
            Utility.Monitor.WriteLine("[Release Build Enabled]", ConsoleColorInformational());
#endif

#if DEBUG
            //  Turn off saves for DEBUG builds
            AutoSave.SavesEnabled = m_EnableDbgSaves;
#endif
            state = AutoSave.SavesEnabled;
            Utility.Monitor.WriteLine("[Saves are {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = m_useLoginDB;
            Utility.Monitor.WriteLine("[Using login database {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            Utility.Monitor.WriteLine("[Shard configuration is {0}.]", ConsoleColorInformational(), m_Server);
            state = RuleSets.ResourcePoolRules();
            Utility.Monitor.WriteLine("[Resource Pool is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.GMNTC_CFG;
            Utility.Monitor.WriteLine("[Test Center functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.RuleSets.LoginServerRules();
            Utility.Monitor.WriteLine("[Login Server functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.GMNBETA_CFG;
            Utility.Monitor.WriteLine("[Beta functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.GMNEV_CFG;
            Utility.Monitor.WriteLine("[Event Shard functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            Utility.Monitor.WriteLine("[Publish {0} enabled ({1}).]", ConsoleColorInformational(), PublishInfo.Publish, PublishInfo.PublishDate);
            state = Core.Building;
            Utility.Monitor.WriteLine("[World building is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));

            // Disabling developer mode for now (for custom house building)
            // state = Core.Developer;
            //Utility.ConsoleOut("[Developer mode is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));

            state = Core.Factions;
            Utility.Monitor.WriteLine("[Factions are {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.T2A;
            Utility.Monitor.WriteLine("[T2A is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));

            #region Map Rules
            if (Core.RuleSets.SeasonsRules(3))
                Utility.Monitor.WriteLine("[Default map configuration: Trammel rules (PvE).]", ConsoleColorInformational());
            else
                Utility.Monitor.WriteLine("[Default map configuration: Felucca rules (PvP).]", ConsoleColorInformational());
            #endregion Map Rules

            if (Arguments.Length > 0)
                Console.WriteLine("Core: Running with arguments: {0}", Arguments);

            m_ProcessorCount = Environment.ProcessorCount;

            if (m_ProcessorCount > 1)
                m_MultiProcessor = true;

            if (m_MultiProcessor || Is64Bit)
                Console.WriteLine("Core: Optimizing for {0} {2}processor{1}", m_ProcessorCount, m_ProcessorCount == 1 ? "" : "s", Is64Bit ? "64-bit " : "");

            int platform = (int)Environment.OSVersion.Platform;
            if ((platform == 4) || (platform == 128))
            { // MS 4, MONO 128
                m_Unix = true;
                Console.WriteLine("Core: Unix environment detected");
            }
            else
            {
                m_ConsoleEventHandler = new ConsoleEventHandler(OnConsoleEvent);
                SetConsoleCtrlHandler(m_ConsoleEventHandler, true);
            }

            // adam: I believe the new startup logic is more robust as it attempts to prevent timers from firing 
            //  before the shard is fully up and alive.
            #region WORLD BOOT 
            AIWorldBoot aiWorldBoot = new AIWorldBoot();
            aiWorldBoot.Configure();
            aiWorldBoot.WorldLoad();
            aiWorldBoot.Initialize();
            aiWorldBoot.ObjectInitialize();

            // this timer (and output) simply proves timers created during Configure, WorldLoad, and Initialize will be
            // respected, and processed as planned. 
            Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object?[] { null });

            SocketPool.Create();

            Timer.TimerThread ttObj = new Timer.TimerThread();
            timerThread = new Thread(new ThreadStart(ttObj.TimerMain));
            timerThread.Name = "Timer Thread";

            MessagePump ms = m_MessagePump = new MessagePump();

            timerThread.Start();

            for (int i = 0; i < Map.AllMaps.Count; ++i)
                ((Map)Map.AllMaps[i]).Tiles.Force();

            NetState.Initialize();

            EventSink.InvokeServerStarted();
            #endregion WORLD BOOT 
#if !DEBUG
            try
            {
#endif
            while (!m_Closing)
            {
                m_Signal.WaitOne();

                Mobile.ProcessDeltaQueue();
                Item.ProcessDeltaQueue();

                Timer.Slice();
                m_MessagePump.Slice();

                NetState.FlushAll();
                NetState.ProcessDisposedQueue();

                if (Slice != null)
                    Slice();
            }
#if !DEBUG
            }
            catch (Exception e)
            {
                CurrentDomain_UnhandledException(null, new UnhandledExceptionEventArgs(e, true));
            }
#endif
        }


        public static string[] ParseArgs(string[] args)
        {
            // Add out-of-band switches exactly once.
            string[] effectiveArgs = AddGMNSwitches(args);

            int serverCount = CountServers(effectiveArgs);

            if (serverCount == 1)
                return effectiveArgs;

            if (serverCount > 1)
            {
                Utility.Monitor.WriteLine(
                    "Core: Too many servers specified in arguments.",
                    ConsoleColorWarning());

                return Array.Empty<string>();
            }

            // Zero servers → prompt.
            Utility.Monitor.WriteLine(
                "Core: No server specified.",
                ConsoleColorWarning());

            Console.Write(
                "Enter server (-uoai / -uosp [-Siege[1|2]] / -uomo / -uols / -uose [-Season[1|2|...]] / -uoup): ");

            string input = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                Utility.Monitor.WriteLine(
                    "Core: No input received.",
                    ConsoleColorWarning());

                return Array.Empty<string>();
            }

            string[] promptTokens = input.Split(
                new[] { ' ', '\t', ',' },
                StringSplitOptions.RemoveEmptyEntries);

            int newServerCount = CountServers(promptTokens);

            if (newServerCount == 0)
            {
                Utility.Monitor.WriteLine(
                    "Core: No valid server selected from prompt.",
                    ConsoleColorWarning());

                return Array.Empty<string>();
            }

            if (newServerCount > 1)
            {
                Utility.Monitor.WriteLine(
                    "Core: Too many servers selected from prompt.",
                    ConsoleColorWarning());

                return Array.Empty<string>();
            }

            return effectiveArgs.Concat(promptTokens).ToArray();
        }

        /*
         * AddGMNSwitches
         *
         * Provides an out-of-band mechanism for supplying temporary startup switches
         * to one or more shards without modifying their individual launch scripts.
         *
         * Additional switches are read from \GMN\switches.txt on the current drive
         * and presented to the administrator before being applied.
         *
         *      (A)pply  - append the switches to this shard's command line
         *      (S)kip   - ignore the switches for this shard
         *      (D)elete - delete switches.txt and globally retire the switches
         *
         * The file itself is authoritative. Before applying previously-read switches,
         * its existence is checked again. This allows one shard to delete the file
         * while other shards are waiting at the prompt; those shards will then ignore
         * their cached copy regardless of the administrator's response.
         *
         * File deletion is retried briefly to tolerate another shard concurrently
         * reading the file.
         *
         * Intended primarily for temporary migration/transition switches.
         */
        private static string[] AddGMNSwitches(string[] tokens)
        {
            try
            {
                string? root = Path.GetPathRoot(Environment.CurrentDirectory);
                string path = Path.Combine(root, "GMN", "switches.txt");

                if (!File.Exists(path))
                    return tokens;

                string[] lines = File.ReadAllLines(path);

                List<string> switches = new List<string>();

                foreach (string line in lines)
                {
                    string text = line.Trim();

                    if (string.IsNullOrEmpty(text))
                        continue;

                    switches.AddRange(
                        text.Split(
                            new char[] { ' ', '\t' },
                            StringSplitOptions.RemoveEmptyEntries));
                }

                if (switches.Count == 0)
                    return tokens;

                Utility.Monitor.WriteLine(
                    $"Additional startup switches found in {path}:",
                    ConsoleColor.Yellow);

                foreach (string value in switches)
                {
                    Utility.Monitor.WriteLine(
                        $"  {value}",
                        ConsoleColor.Yellow);
                }

                Utility.Monitor.WriteLine(
                    "",
                    ConsoleColor.Yellow);

                Utility.Monitor.WriteLine(
                    "(A)pply, (S)kip, or (D)elete these startup switches?",
                    ConsoleColor.Yellow);

                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                    switch (char.ToUpperInvariant(key.KeyChar))
                    {
                        case 'A':
                            {
                                Utility.Monitor.WriteLine(
                                    "Apply selected.",
                                    ConsoleColor.Green);

                                // The file is authoritative. Another shard may have
                                // deleted it while this shard was waiting for input.
                                if (!File.Exists(path))
                                {
                                    Utility.Monitor.WriteLine(
                                        "Startup switches file has been deleted; switches will NOT be applied.",
                                        ConsoleColor.Yellow);

                                    return tokens;
                                }

                                List<string> result = new List<string>(tokens);

                                foreach (string value in switches)
                                {
                                    string clean = value.TrimStart('-');

                                    bool exists = result.Any(token =>
                                        string.Equals(
                                            token.TrimStart('-'),
                                            clean,
                                            StringComparison.OrdinalIgnoreCase));

                                    if (exists)
                                    {
                                        Utility.Monitor.WriteLine(
                                            $"  {value} already present; skipped.",
                                            ConsoleColor.Yellow);

                                        continue;
                                    }

                                    result.Add(value);
                                }

                                Utility.Monitor.WriteLine(
                                    "Additional startup switches applied.",
                                    ConsoleColor.Green);

                                return result.ToArray();
                            }

                        case 'S':
                            {
                                Utility.Monitor.WriteLine(
                                    "Skip selected; additional startup switches will NOT be applied.",
                                    ConsoleColor.Yellow);

                                return tokens;
                            }

                        case 'D':
                            {
                                Utility.Monitor.WriteLine(
                                    "Delete selected; retiring additional startup switches.",
                                    ConsoleColor.Yellow);

                                const int retries = 10;
                                const int delay = 100;

                                for (int attempt = 0; attempt < retries; attempt++)
                                {
                                    try
                                    {
                                        if (File.Exists(path))
                                            File.Delete(path);

                                        if (!File.Exists(path))
                                        {
                                            Utility.Monitor.WriteLine(
                                                $"Deleted {path}",
                                                ConsoleColor.Green);

                                            return tokens;
                                        }
                                    }
                                    catch (IOException)
                                    {
                                        // Another shard may momentarily be reading the file.
                                    }
                                    catch (UnauthorizedAccessException)
                                    {
                                        // May also be transient; retry before giving up.
                                    }

                                    System.Threading.Thread.Sleep(delay);
                                }

                                Utility.Monitor.WriteLine(
                                    $"Unable to delete {path}; startup switches remain active.",
                                    ConsoleColor.Red);

                                return tokens;
                            }

                        default:
                            {
                                Utility.Monitor.WriteLine(
                                    "Please enter (A)pply, (S)kip, or (D)elete.",
                                    ConsoleColor.Yellow);

                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                return tokens;
            }
        }
        /// <summary>
        /// Parses tokens and sets flags. Returns number of servers activated in this call.
        /// Normalizes arguments to always use -X format when appending to Arguments.
        /// </summary>
        private static void SetSwitches(string[] tokens)
        {
            foreach (string raw in tokens)
            {
                // Normalize to lowercase for comparison, but keep original for appending
                string token = raw.Trim();
                string lower = token.ToLowerInvariant();

                // Strip leading - for comparison if present
                string clean = lower.StartsWith("-") ? lower.Substring(1) : lower;

                // ── General / config flags ─────────────────────────────────────
                if (lower == "-service") m_Service = true;
                else if (lower == "-profile") Profiling = true;
                else if (lower == "-haltonwarning") m_HaltOnWarning = true;
                else if (lower == "-vb") m_VBdotNET = true;
                else if (lower == "-migratepatchhistory") MigratePatchHistory = true;
                else if (lower == "-usedb") m_useLoginDB = true;
                else if (lower == "-uotc") m_GMNTC_CFG = true;
                else if (lower == "-uopt") m_Tribute_CFG = true;
                else if (lower == "-norp") m_USERP_CFG = false;
                else if (lower == "-uoev") m_GMNEV_CFG = true;
                else if (lower == "-build") m_Building = true;
                else if (lower == "-nopatch") m_Patching = false;
                else if (lower == "-saves") m_EnableDbgSaves = true;
                else if (lower == "-uobeta") m_GMNBETA_CFG = true;
                else if (lower == "-restarted") m_Restarted = true;
                else if (lower == "-crashed")
                {
                    m_CrashRecovery = true;
                    Utility.PushColor(ConsoleColor.Red);
                }

                // ── uos2 is only meaningful with uosp ──────────────────────────
                else if (clean == "uos2" || lower == "-uos2")
                {
                    Core.Halt("uos2 is obsolete. Use -Siege[1|2] instead");
                }

                // ── Servers (only count real server selection) ─────────────────
                else if (clean == "uols" || lower == "-uols")
                {
                    RuleSets.GMNLS_SVR = true;
                }
                else if (clean == "uoai" || lower == "-uoai")
                {
                    RuleSets.GMNAI_SVR = true;
                }
                else if (clean == "uosp" || lower == "-uosp")
                {
                    RuleSets.GMNSP_SVR = true;
                }
                else if (clean == "uomo" || lower == "-uomo")
                {
                    RuleSets.GMNMO_SVR = true;
                }
                else if (clean == "uose" || lower == "-uose")
                {
                    RuleSets.GMNSE_SVR = true;
                }
                else if (clean == "uoup" || lower == "-uoup")
                {
                    RuleSets.GMNUP_SVR = true;
                }
            }
        }
        private static int CountServers(string[] tokens)
        {
            int serverCount = 0;

            foreach (string raw in tokens)
            {
                string clean = raw.Trim().TrimStart('-').ToLowerInvariant();

                if (clean == "uols" ||
                    clean == "uoai" ||
                    clean == "uosp" ||
                    clean == "uomo" ||
                    clean == "uose" ||
                    clean == "uoup")
                {
                    serverCount++;
                }
            }

            return serverCount;
        }
        public static ConsoleColor ConsoleColorEnabled(bool enabled)
        {
            if (enabled)
                return ConsoleColor.Green;
            else
                return ConsoleColor.Yellow;
        }
        public static ConsoleColor ConsoleColorInformational()
        {
            return ConsoleColor.Green;
        }
        public static ConsoleColor ConsoleColorWarning()
        {
            return ConsoleColor.Red;
        }
        public static string TextEnabled(bool enabled)
        {
            if (enabled)
                return "Enabled";
            else
                return "Disabled";
        }
        private static void Tick(object state)
        {
            object[] aState = (object[])state;
            Utility.Monitor.WriteLine("Timers initialized", ConsoleColor.Green);
        }
        public static string BuildInfoDir
        {
            get
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AI.BuildInfoDir")))
                    return Environment.GetEnvironmentVariable("AI.BuildInfoDir");
                return "./";
            }
        }
        public static bool BuildInfoCheck()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AI.BuildInfoDir")))
                return true;
            else
                return false;
        }
        public static bool DefaultPorts()
        {
            if (!File.Exists("Data/Ports.json"))
                return false;
            try
            {
                File.Copy("Data/Ports.json", "./Ports.json");
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool PortsCheck()
        {
            try
            {
                int port;
                port = SocketOptions.AngelIslandPort;
                port = SocketOptions.SiegePerilousPort;
                port = SocketOptions.MortalisPort;
                port = SocketOptions.SeasonsPort;
                port = SocketOptions.UpheavalPort;

                port = SocketOptions.EventShardPort;
                port = SocketOptions.TestCenterPort;

                port = SocketOptions.LoginServerPortBase;
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool EmailCheck()
        {
            string? noreply_password = Environment.GetEnvironmentVariable("AI.NOREPLY.PASSWORD");                // = (your email password. For this I use an AppPassword generated by Google)
            string? noreply_address = Environment.GetEnvironmentVariable("AI.NOREPLY.ADDRESS");                  // = example: noreply @game-master.net
            string? email_host = Environment.GetEnvironmentVariable("AI.EMAIL.HOST");                            //  = example: smtp.gmail.com
            string? email_user = Environment.GetEnvironmentVariable("AI.EMAIL.USER");                            //  = example: luke.tomasello@gmail.com
            string? email_port = Environment.GetEnvironmentVariable("AI.EMAIL.HOST.PORT");                       //  = 587
            string? email_accounting = Environment.GetEnvironmentVariable("AI.EMAIL.ACCOUNTING");                // = example: aiaccounting @game - master.net    // new account created etc.
            string? email_announcements = Environment.GetEnvironmentVariable("AI.EMAIL.ANNOUNCEMENTS");          // = announcements@game-master.net				// announce shard wide events
            string? email_devnotify = Environment.GetEnvironmentVariable("AI.EMAIL.DEVNOTIFY");                  // = devnotify@game-master.net					// sent on server crash
            string? email_distlist_password = Environment.GetEnvironmentVariable("AI.EMAIL.DISTLIST.PASSWORD");  //  = (your email password.For this I use an AppPassword generated by Google) ;
            string? email_shardowner = Environment.GetEnvironmentVariable("AI.EMAIL.SHARDOWNER");                // = shard owners email address, certain private information

            if (noreply_password == null || noreply_address == null || email_host == null || email_user == null || email_port == null ||
                email_accounting == null || email_announcements == null || email_devnotify == null || email_distlist_password == null || email_shardowner == null)
            {
                return false;
            }

            return true;
        }
        public static bool GeoIPCheck()
        {
            string? MMAccountId = Environment.GetEnvironmentVariable("AI.MMAccountId");
            string? MMlicenseKey = Environment.GetEnvironmentVariable("AI.MMlicenseKey");

            if (MMAccountId == null || MMlicenseKey == null)
            {
                return false;
            }

            return true;
        }
        private static string m_arguments;
        public static string Arguments
        {
            get
            {
                return m_arguments;
            }

            set
            {
                m_arguments = value;
            }
        }

        private static int m_GlobalMaxUpdateRange = 24;

        public static int GlobalMaxUpdateRange
        {
            get { return m_GlobalMaxUpdateRange; }
            set { m_GlobalMaxUpdateRange = value; }
        }

        private static int m_ItemCount, m_MobileCount, m_SerializableObjectCount;

        public static int ScriptItems { get { return m_ItemCount; } }
        public static int ScriptMobiles { get { return m_MobileCount; } }
        public static int ScriptSerializableObjects { get { return m_SerializableObjectCount; } }

        public static void VerifySerialization()
        {
            m_ItemCount = 0;
            m_MobileCount = 0;

            VerifySerialization(Assembly.GetCallingAssembly());

            for (int a = 0; a < ScriptCompiler.Assemblies.Length; ++a)
                VerifySerialization(ScriptCompiler.Assemblies[a]);
        }

        private static void VerifySerialization(Assembly a)
        {
            if (a == null) return;

            Type[] ctorTypes = new Type[] { typeof(Serial) };

            foreach (Type t in a.GetTypes())
            {
                bool isItem = t.IsSubclassOf(typeof(Item));
                bool isSerializableObject = t.IsSubclassOf(typeof(SerializableObject));
                bool isMobile = t.IsSubclassOf(typeof(Mobile));
                if (isItem || isMobile || isSerializableObject)
                {
                    if (isItem)
                        ++m_ItemCount;
                    else if (isMobile)
                        ++m_MobileCount;
                    else
                        ++m_SerializableObjectCount;

                    bool warned = false;

                    try
                    {
                        if (isSerializableObject == false)
                            if (t.GetConstructor(ctorTypes) == null)
                            {
                                if (!warned)
                                    Console.WriteLine("Warning: {0}", t);

                                warned = true;
                                Console.WriteLine("       - No serialization constructor");
                            }

                        if (t.GetMethod("Serialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null)
                        {
                            if (!warned)
                                Console.WriteLine("Warning: {0}", t);

                            warned = true;
                            Console.WriteLine("       - No Serialize() method");
                        }

                        if (t.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null)
                        {
                            if (!warned)
                                Console.WriteLine("Warning: {0}", t);

                            warned = true;
                            Console.WriteLine("       - No Deserialize() method");
                        }

                        if (warned)
                            Console.WriteLine();
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }
        }

        public class AIWorldBoot
        {
            Assembly? m_scripts = null;
            ArrayList m_invoke = new ArrayList();
            Type[] m_types;
            public AIWorldBoot()
            {   // get the current assembly
                Type t = typeof(Core);
                m_scripts = t.Assembly;
                m_types = m_scripts.GetTypes();
            }

            public void Configure()
            {
                for (int i = 0; i < m_types.Length; ++i)
                {
                    MethodInfo? m = m_types[i].GetMethod("Configure", BindingFlags.Static | BindingFlags.Public);

                    if (m != null)
                        m_invoke.Add(m);
                }

                m_invoke.Sort(new CallPriorityComparer());

                for (int i = 0; i < m_invoke.Count; ++i)
                    ((MethodInfo)m_invoke[i]).Invoke(null, null);

                m_invoke.Clear();
            }

            public void WorldLoad()
            {
                World.Load();
            }

            public void ObjectInitialize()
            {
                try
                {
                    // 4/4/23, Adam: Add individual object initialization. Unlike Initialize, WorldLoaded provides initialization
                    //  where the context of the object is known.
                    Console.WriteLine("Initializing {0} items", World.Items.Count);
                    foreach (Item item_dsr in World.Items.Values)
                        if (item_dsr != null) item_dsr.WorldLoaded();
                    Console.WriteLine("Initializing {0} Mobiles", World.Mobiles.Count);
                    foreach (Mobile mobile_dsr in World.Mobiles.Values)
                        if (mobile_dsr != null) mobile_dsr.WorldLoaded();
                    Console.WriteLine("{0} objects initialized", World.Items.Count + World.Mobiles.Count);
                }
                catch {; }
            }

            public void Initialize()
            {
                for (int i = 0; i < m_types.Length; ++i)
                {
                    MethodInfo? m = m_types[i].GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);

                    if (m != null)
                        m_invoke.Add(m);
                }

                m_invoke.Sort(new CallPriorityComparer());

                for (int i = 0; i < m_invoke.Count; ++i)
                    ((MethodInfo)m_invoke[i]).Invoke(null, null);
            }
        }

        public static void Halt(string message, int exitCode = 1)
        {
            Core.LoggerShortcuts.BootError(string.Format(message));

            if (Environment.UserInteractive)
            {
                Utility.Monitor.WriteLine("Press any key to exit...", ConsoleColor.Red);
                Console.ReadKey(true);
            }

            Environment.Exit(exitCode);
        }
    }

    public class FileLogger : TextWriter, IDisposable
    {
        private string m_FileName;
        private bool m_NewLine;
        public const string DateFormat = "[MMMM dd hh:mm:ss.f tt]: ";

        public string FileName { get { return m_FileName; } }

        public FileLogger(string file)
            : this(file, false)
        {
        }

        public FileLogger(string file, bool append)
        {
            m_FileName = file;
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                writer.WriteLine(">>>Logging started on {0}.", DateTime.UtcNow.ToString("f")); //f = Tuesday, April 10, 2001 3:51 PM 
            }
            m_NewLine = true;
        }

        public override void Write(char ch)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                if (m_NewLine)
                {
                    writer.Write(DateTime.UtcNow.ToString(DateFormat));
                    m_NewLine = false;
                }
                writer.Write(ch);
            }
        }

        public override void Write(string? str)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                if (m_NewLine)
                {
                    writer.Write(DateTime.UtcNow.ToString(DateFormat));
                    m_NewLine = false;
                }
                writer.Write(str);
            }
        }

        public override void WriteLine(string? line)
        {
            using (StreamWriter writer = new StreamWriter(new FileStream(m_FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
            {
                if (m_NewLine)
                    writer.Write(DateTime.UtcNow.ToString(DateFormat));
                writer.WriteLine(line);
                m_NewLine = true;
            }
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.Default; }
        }
    }

    public class MultiTextWriter : TextWriter
    {
        private ArrayList m_Streams;

        public MultiTextWriter(params TextWriter[] streams)
        {
            m_Streams = new ArrayList(streams);

            if (m_Streams.Count < 0)
                throw new ArgumentException("You must specify at least one stream.");
        }

        public void Add(TextWriter tw)
        {
            m_Streams.Add(tw);
        }

        public void Remove(TextWriter tw)
        {
            m_Streams.Remove(tw);
        }

        public override void Write(char ch)
        {
            for (int i = 0; i < m_Streams.Count; i++)
                ((TextWriter)m_Streams[i]).Write(ch);
        }

        public override void WriteLine(string? line)
        {
            for (int i = 0; i < m_Streams.Count; i++)
                ((TextWriter)m_Streams[i]).WriteLine(line);
        }

        public override void WriteLine(string line, params object?[] args)
        {
            WriteLine(string.Format(line, args));
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.Default; }
        }
    }
}