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

/* Server\Main.cs
 * ChangeLog:
 *  8/15/2024, Adam
 *      When using the remote database, verify connectivity during server up.
 *      Issue errors, and inform the user what has happened.
 *  8/13/2024, Adam
 *      Add a notion of BuildInfoDir. This directory contains the "build.info" file
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.Deeds;
using Server.Network;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public class Core
    {
        private static bool m_Crashed;
        private static Thread timerThread;
        private static string m_BaseDirectory;
        private static string m_DataDirectory;
        private static string m_LogsDirectory;
        private static string m_SharedDirectory;
        private static string m_ExePath;
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
        //private static bool m_Developer;                // developers machine, allows derect login to any server

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

        public static Slice Slice;

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

        // import a non-encrypted world
        private static bool m_Import = false;
        public static bool Import { get { return m_Import; } }

        private static string m_Server;
        public static string Server { get { return m_Server; } }

        // perform a onetime upgrade from old non-locking boat holds to the new locking ones
        private static bool m_BoatHoldUpgrade = false;
        public static bool BoatHoldUpgrade { get { return m_BoatHoldUpgrade; } }
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

            string fullPath = null;

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
                return RuleSets.SiegeRules() || RuleSets.RenaissanceRules();
            }
        }

        public static bool OldEthics
        {
            get
            {
                return Core.RuleSets.SiegeStyleRules() && PublishInfo.Publish < 13.6;
            }
        }

        public static bool NewEthics
        {
            get
            {
                return !OldEthics;
            }
        }

        public static bool RedsInTown
        {
            get
            {   // add your 'reds in town' shards here:
                return Core.RuleSets.SiegeStyleRules();
            }
        }
        #region RunUO2.6 Port compatibility functions
#if false
        public static bool T2A
        {   // is T2A available to this shard?
            get
            {
                return Core.RuleSets.SiegeStyleRules();
            }
        }
#endif
        #endregion RunUO2.6 Port compatibility functions
        public static bool OldStyleTinkerTrap
        {
            get
            {   // enabling this for siege is not era accurate, but we'll make the exception here
                return PublishInfo.Publish < 4 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules();
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
                return (Core.RuleSets.SiegeStyleRules() && PublishInfo.Publish >= 8.0);
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
                return (Core.RuleSets.SiegeStyleRules() && PublishInfo.PublishDate >= new DateTime(1999, 7, 15));
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
        #region server configurations
        private static bool m_UOTC_CFG;                     // Test Center
        private static bool m_UOBETA_CFG;                   // BETA AI, allows most test center funcionality without the free loot
        private static bool m_UOEV_CFG;                     // Event Shard
        private static bool m_USERP_CFG = true;             // Use Resource Pool. Default to 'true'
        private static bool m_SiegeII_CFG;                  // SiegeII developer switch
        private static bool m_Tribute_CFG;                  // Special player tribute shard
        #endregion server configurations

        #region Rule Sets
        public static class RuleSets
        {
            #region SERVERS
            private static bool m_AOS_SVR;
            private static bool m_SE_SVR;
            private static bool m_ML_SVR;
            private static bool m_UOSP_SVR;                 // Siege
            private static bool m_UOAI_SVR;                 // Angel Island
            private static bool m_UOMO_SVR;                 // Mortalis
            private static bool m_UOREN_SVR;                // Renaissance
            private static bool m_UOLS_SVR;                 // Login Server
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
                    return UOLS_SVR;
                }
            }
            public static bool DatabaseMaster
            {
                get
                {   // used only for dedicated a database master 
                    // since the *_CFG options can be applied to any *, we exclude this as
                    // a valid database master.
                    return Core.RuleSets.AngelIslandRules() && !UOEV_CFG && !UOTC_CFG;
                    // Note: UOBETA_CFG is special since it can be applied to beta versions of the DatabaseMaster.
                }
            }
            public static bool UOLS_SVR
            {
                get { return m_UOLS_SVR; }
                set { m_UOLS_SVR = value; }
            }
            public static bool UOAI_SVR
            {   // January 9, 2002: killing shopkeepers in Felucca will now give a murder count, etc.
                // https://www.uoguide.com/Publish_15
                // (before the destruction of UO)
                get
                { return m_UOAI_SVR; }
                set
                {
                    PublishInfo.Publish = 15;
                    m_UOAI_SVR = value;
                }
            }
            public static bool UOSP_SVR
            {   /* Publish 13.6 (Siege Perilous Shards Only) - October 25, 2001
                 * Publish 13.5 - October 11, 2001
                 * Commodity Deeds, Repair Contracts, Secure House Trades
                 * https://www.uoguide.com/Publishes
                 */
                get
                { return m_UOSP_SVR; }
                set
                {
                    PublishInfo.Publish = 13.6;
                    m_UOSP_SVR = value;
                }
            }
            public static bool UOMO_SVR
            {   /* Publish 13.6 (Siege Perilous Shards Only) - October 25, 2001
                 * Publish 13.5 - October 11, 2001
                 * Commodity Deeds, Repair Contracts, Secure House Trades
                 * https://www.uoguide.com/Publishes
                 */
                get
                { return m_UOMO_SVR; }
                set
                {
                    PublishInfo.Publish = 13.6;
                    m_UOMO_SVR = value;
                }
            }
            public static bool UOREN_SVR
            {   /* Publish 13.6 (Siege Perilous Shards Only) - October 25, 2001
                 * Publish 13.5 - October 11, 2001
                 * Commodity Deeds, Repair Contracts, Secure House Trades
                 * https://www.uoguide.com/Publishes
                 */
                get { return m_UOREN_SVR; }
                set
                {
                    PublishInfo.Publish = 13.5;
                    m_UOREN_SVR = value;
                }
            }
            public static bool AOS_SVR
            {
                get
                {
                    return m_AOS_SVR || m_SE_SVR;
                }
                set
                {
                    m_AOS_SVR = value;
                }
            }
            public static bool SE_SVR
            {
                get
                {
                    return m_SE_SVR;
                }
                set
                {
                    m_SE_SVR = value;
                }
            }
            public static bool ML_SVR
            {
                get
                {
                    return m_ML_SVR;
                }
                set
                {
                    m_ML_SVR = value;
                }
            }
            #endregion SERVERS Variables
            #region Server Enablement
            public static bool AllServerRules()
            {   // typically used to denote an Angel Island feature that should be available everywhere
                return (m_AOS_SVR || m_SE_SVR || m_ML_SVR || m_UOSP_SVR || m_UOAI_SVR || m_UOMO_SVR || m_UOREN_SVR);
            }
            public static bool RenaissanceRules()
            {
                return (UOREN_SVR);
            }
            public static bool SERules()
            {
                return (SE_SVR);
            }
            public static bool MLRules()
            {
                return (ML_SVR);
            }
            public static bool LoginServerRules()
            {
                return (UOLS_SVR);
            }
            public static bool AOSRules()
            {
                return (AOS_SVR);
            }
            public static bool SiegeRules()
            {
                return (UOSP_SVR);
            }
            public static bool SiegeIIRules()
            {
                return SiegeRules() && SiegeII_CFG;
            }
            public static bool SiegeStyleRules()
            {
                return (UOSP_SVR || MortalisRules());
            }
            public static bool AngelIslandRules()
            {
                return (UOAI_SVR);
            }
            public static bool TestCenterRules()
            {
                return UOTC_CFG;
            }
            public static bool EventShardRules()
            {
                return UOEV_CFG;
            }
            public static bool PackUpStructureRules()
            {
                return CoreAI.EnablePackUp;
            }
            public static bool MortalisRules()
            {
                return (UOMO_SVR);
            }
            #endregion Server Enablement
            #region Enabled Server Rules

            public static bool TentAnnexation()
            { return AngelIslandRules(); }
            public static bool MiniBossArmor()
            { return AngelIslandRules() || Core.SiegeII_CFG; }
            public static bool InstaDeathGuards()
            { return AngelIslandRules(); }
            public static bool ModifiedMindBlast()
            { return AngelIslandRules() || MortalisRules(); }
            public static bool AllowGhostBlindness()
            { return AngelIslandRules() || MortalisRules(); }
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
                if (SiegeIIRules())
                    return 6;

                return 0;
            }

            public static int ShardStableBonus()
            {
                if (SiegeIIRules())
                    return 12;

                return 0;
            }
            public static bool HerdingBonus(Mobile from)
            {
                if (from is PlayerMobile pm)
                {
                    if (Core.RuleSets.AngelIslandRules())
                        return true;

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
                return (PublishInfo.Publish >= 14.0 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || Core.RuleSets.SiegeStyleRules());
            }
            public static bool AutoDispelChance()
            {
                // 7/22/2023, Adam: Incorporate the Auto Dispel chance Introduced in Samurai Empire.
                return Core.RuleSets.AngelIslandRules() ? false : PublishInfo.PublishDate >= Core.EraREN ? Utility.Chance(0.1) : false;
            }
            public static bool MurderersGetOneThirdGold()
            {   // An additional penalty for being red on Siege Perilous includes the amount of gold found on the corpse of a creature 
                //	which they have killed will be one-third. For example, if a player would normally receive 600 gold off a monster, 
                //	if that player is instead red, he will receive 200 gold.
                // http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                // 5/12/23, Adam: We are discontinuing this for our Siege shard. There was pretty much a blowup
                //  on the forums, and several Siege vets swore this was never implemented. Not sure I believe this
                //  but it's not worth losing players over.
                return !SiegeStyleRules();
            }
            public static bool StealableAtSpawnLoot()
            {   // Note Angel Island blocks this in another way - custom loot table
                // For other shards, i.e., Siege, this article says 'yes', you can steal from monsters
                // https://web.archive.org/web/20011027063305/http://guide.uo.com/skill_33.html
                return !SiegeStyleRules();
            }
            public static bool ThemedTreasureMaps()
            {   // 7/13/2023, Adam: Enabling themed treasure map chests. See SpecialMibRares() for the MIB equivalent.)
                return Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules();
            }
            public static bool SpecialMibRares()
            {   // 7/13/2023, Adam: See ThemedTreasureMaps() for the treasure map equivalent.)
                return Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules();
            }
            public static bool SpecialMonsterRares()
            {   // 7/13/2023, Adam: Allow rare drops on dungeon monsters
                return Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules();
            }
            public static bool FillableContainerRares()
            {   // 7/13/2023, Adam: Allow rare drops in FillableContainer
                return Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules();
            }
            public static bool DecorativeFurniture()
            {   // 7/14/2023, Adam: seems good for all shards
                return Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules();
            }
            public static bool KinSystemEnabled()
            {   // 3/28/23, Adam: allow kin on Siege.. we'll see how it goes.
                // 4/13/23, Yoar: disabled the Kin system in favor of the new Alignment system.
                return false;//Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules();
            }
            public static bool EnchantedScrollsEnabled()
            {
                return Core.RuleSets.SiegeStyleRules();
            }
            public static bool RoTShardEnabled()
            {
                return SiegeRules() || MortalisRules();
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
            public static int SiegePriceRules(Type type, int price)
            {
                if (Core.RuleSets.SiegeStyleRules())
                {   // Pub 13.6
                    // houses are 10x the price for Siege from Pub 13.6 on
                    // otherwise, apply the regular x3 price hike for Siege
                    // 1/19/2024, Adam. SiegeII: in order to spur the economy on such a small shard, we are adopting the 3x pricing for houses even though it's not era accurate
                    // http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                    if (typeof(HouseDeed).IsAssignableFrom(type))
                        price = RealEstateBroker.ComputeHousingMarkupForSiege(price);
                    // https://www.uoguide.com/Publish_16#Update_1
                    // Houses and boats on Siege Perilous and Mugen are now 9 times regular shard prices (rather than the previous 10 times or recent 30 times prices).
                    // https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                    // The cost of house deeds when purchased from NPC shopkeepers on Siege Perilous will be 10x the prices seen on "normal" shards. All other NPC shopkeeper sales on Siege Perilous will remain at 3x normal.
                    // Our siege is 13.6, so my interpretation of this data is that boats are 3x
                    else if (typeof(BaseBoatDeed).IsAssignableFrom(type) && PublishInfo.Publish >= 16)
                        price *= 10;
                    else
                        price *= 3;
                }

                return price;
            }
            public static bool StandardShardRules()
            {
                return (SiegeRules() || MortalisRules() || RenaissanceRules());
            }
            public static bool AnyAIShardRules()
            {
                return (AngelIslandRules() || SiegeRules() || MortalisRules() || RenaissanceRules());
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
                return !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.SiegeStyleRules();
            }
            public static bool ShopkeepersBuyResourcesRules()
            {
                return !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.SiegeStyleRules();
            }
            public static bool ShopkeepersBuyItemsRules()
            {
                return !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.SiegeStyleRules();
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
                return Core.UOTC_CFG || Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeRules();
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
            public static bool AllowMountRules()
            {   // currently all shards may have mounts except Angel Island
                return !AngelIslandRules();
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
        public static bool UOEV_CFG
        {
            get
            {
                return m_UOEV_CFG;
            }
        }

        public static bool UOTC_CFG
        {
            get
            {
                return m_UOTC_CFG;
            }
        }
        public static bool SiegeII_CFG
        {
            get
            {
                return m_SiegeII_CFG;
            }
        }
        public static bool Tribute_CFG
        {
            get
            {   // tribute shard is run under the EV port number
                return m_Tribute_CFG && m_UOEV_CFG;
            }
        }
        public static bool UOBETA_CFG
        {
            get
            {
                return m_UOBETA_CFG;
            }
        }
        #endregion SERVER CONFIGURATIONS

        public static bool Building
        {
            get
            {
                return m_Building;
            }
        }
        public static string ExePath
        {
            get
            {
                if (m_ExePath == null)
                    m_ExePath = Process.GetCurrentProcess().MainModule.FileName.Replace("vshost.", "");

                return m_ExePath;
            }
        }
        public static string BaseDirectory
        {
            get
            {
                if (m_BaseDirectory == null)
                {
                    try
                    {
                        m_BaseDirectory = ExePath;

                        if (m_BaseDirectory.Length > 0)
                            m_BaseDirectory = Path.GetDirectoryName(m_BaseDirectory);
                    }
                    catch
                    {
                        m_BaseDirectory = ".";
                    }
                }

                return m_BaseDirectory;
            }
        }
        public static string DataDirectory
        {
            get
            {
                if (m_DataDirectory == null)
                {
                    try
                    {
                        bool isDevelopmerMachine = false;
                        if (m_BaseDirectory.Contains(@"\debug\", StringComparison.OrdinalIgnoreCase) || m_BaseDirectory.Contains(@"\release\", StringComparison.OrdinalIgnoreCase))
                            isDevelopmerMachine = true;

                        if (isDevelopmerMachine)
                            m_DataDirectory = Path.GetFullPath(Path.Combine(m_BaseDirectory, "../../../", "Data"));
                        else
                            m_DataDirectory = Path.GetFullPath(Path.Combine(m_BaseDirectory, "Data"));
                    }
                    catch
                    {
                        // don't default this. we want the system to blow up if either the developer's or production machine is not setup correctly
                    }
                }

                return m_DataDirectory;
            }
        }
        public static string LogsDirectory
        {
            get
            {
                if (m_LogsDirectory == null)
                {
                    try
                    {
                        return m_LogsDirectory = Path.Combine(m_BaseDirectory, "Logs");
                    }
                    catch
                    {
                        // don't default this. we want the system to blow up if either the developer's or production machine is not setup correctly
                    }
                }

                return m_LogsDirectory;
            }
        }
        public static string SharedDirectory
        {
            get
            {
                if (m_SharedDirectory == null)
                {
#if DEBUG
                    string path = Path.Combine(m_BaseDirectory, "../../../../");
#else
                    string path = Path.Combine(m_BaseDirectory, "../");
#endif
                    path = Path.GetFullPath(path);
                    if (Directory.Exists(path))
                        m_SharedDirectory = path;
                    else
                        throw new ApplicationException(string.Format("Cannot establish shared folder at: {0}", path));
                }

                return m_SharedDirectory;
            }
        }
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

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
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
                Process.Start(ExePath, Arguments);

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

            #region HIDE_CLOSEBOX
#if !DEBUG
            IntPtr hMenu = Process.GetCurrentProcess().MainWindowHandle;
            IntPtr hSystemMenu = GetSystemMenu(hMenu, false);
            EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
            RemoveMenu(hSystemMenu, SC_CLOSE, MF_BYCOMMAND);
#endif
            #endregion HIDE_CLOSEBOX
            #region ARG PARSING
            Arguments = "";
            for (int i = 0; i < args.Length; ++i)
            {
                //if (Insensitive.Equals(args[i], "-debug"))
                //m_Debug = true;
                if (Insensitive.Equals(args[i], "-service"))
                    m_Service = true;
                else if (Insensitive.Equals(args[i], "-profile"))
                    Profiling = true;
                else if (Insensitive.Equals(args[i], "-haltonwarning"))
                    m_HaltOnWarning = true;
                else if (Insensitive.Equals(args[i], "-vb"))
                    m_VBdotNET = true;
                else if (Insensitive.Equals(args[i], "-import"))
                    m_Import = true;
                else if (Insensitive.Equals(args[i], "-boatholdupgrade"))
                    m_BoatHoldUpgrade = true;
                else if (Insensitive.Equals(args[i], "-usedb"))
                    m_useLoginDB = true;
                else if (Insensitive.Equals(args[i], "-uotc"))
                    m_UOTC_CFG = true;
                else if (Insensitive.Equals(args[i], "-uos2"))
                    m_SiegeII_CFG = true;
                else if (Insensitive.Equals(args[i], "-uopt"))
                    m_Tribute_CFG = true;
                else if (Insensitive.Equals(args[i], "-uols"))
                    RuleSets.UOLS_SVR = true;
                else if (Insensitive.Equals(args[i], "-uois"))
                    RuleSets.UOSP_SVR = true;
                else if (Insensitive.Equals(args[i], "-uosp"))
                    RuleSets.UOSP_SVR = true;
                else if (Insensitive.Equals(args[i], "-uomo"))
                    RuleSets.UOMO_SVR = true;
                else if (Insensitive.Equals(args[i], "-uoai"))
                    RuleSets.UOAI_SVR = true;
                else if (Insensitive.Equals(args[i], "-uoren"))
                    RuleSets.UOREN_SVR = true;
                else if (Insensitive.Equals(args[i], "-norp"))
                    m_USERP_CFG = false;
                else if (Insensitive.Equals(args[i], "-uoev"))
                    m_UOEV_CFG = true;
                else if (Insensitive.Equals(args[i], "-build"))
                    m_Building = true;
                else if (Insensitive.Equals(args[i], "-nopatch"))
                    m_Patching = false;
                // Disabling developer mode for now (for custom house building)
                //else if (Insensitive.Equals(args[i], "-developer"))
                //m_Developer = true;
                else if (Insensitive.Equals(args[i], "-uobeta"))
                    m_UOBETA_CFG = true;
                else if (Insensitive.Equals(args[i], "-red"))
                    Utility.PushColor(ConsoleColor.Red);

                Arguments += args[i] + " ";
            }
            Arguments += "-red ";    // 6/29/2021, Adam: if we restart due to a crash, all text will be red.
            #endregion
            #region VERIFY ARGS
            int server_count = 0;
            if (RuleSets.UOAI_SVR == true) server_count++;
            if (RuleSets.UOSP_SVR == true) server_count++;
            if (RuleSets.UOMO_SVR == true) server_count++;
            if (RuleSets.UOLS_SVR == true) server_count++;
            if (RuleSets.UOREN_SVR == true) server_count++;
            if (server_count == 0)
            {
                Utility.Monitor.WriteLine("Core: No server specified, defaulting to Angel Island", ConsoleColorInformational());
                RuleSets.UOAI_SVR = true;
            }
            if (server_count > 1)
            {
                Utility.Monitor.WriteLine("Core: Too many servers specified.", ConsoleColorWarning());
                return;
            }
            #endregion
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

            if (BaseDirectory.Length > 0)
                Directory.SetCurrentDirectory(BaseDirectory);

            Version ver = m_Assembly.GetName().Version;
            bool state = false;
            #region Server Name

            if (Core.RuleSets.SiegeRules())
            {
                m_Server = "Siege Perilous";
                m_releasePhase = ReleasePhase.Production;
            }
            else if (Core.RuleSets.MortalisRules())
            {
                m_Server = "Mortalis";
                m_releasePhase = ReleasePhase.Pre_Alpha;
            }
            else if (Core.RuleSets.LoginServerRules())
            {
                m_Server = "Login Server";
                m_releasePhase = ReleasePhase.Production;
            }
            else if (Core.RuleSets.RenaissanceRules())
            {
                m_Server = "Renaissance";
                m_releasePhase = ReleasePhase.Pre_Alpha;
            }
            else if (Core.RuleSets.AngelIslandRules())
            {
                m_Server = "Angel Island";
                m_releasePhase = ReleasePhase.Production;
            }
            else
                m_Server = "Unknown Configuration";
            #endregion Server Name


            Utility.Monitor.WriteLine("{0}{5} - [www.game-master.net] Version {1}.{2}.{3}, Build {4}",
                Utility.BuildColor(Utility.BuildRevision()), m_Server, Utility.BuildMajor(), Utility.BuildMinor(),
                Utility.BuildRevision(), Utility.BuildBuild(),
                Core.ReleasePhase < ReleasePhase.Production ? string.Format(" ({0})",
                Utility.GetCustomEnumNames(typeof(ReleasePhase))[(int)m_releasePhase]) : "");

            Utility.Monitor.WriteLine("[Siege II is {0}.]", ConsoleColorEnabled(Core.SiegeII_CFG), TextEnabled(Core.SiegeII_CFG));
            Utility.Monitor.WriteLine("[UO Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(DataPath.GetUOPath("Ultima Online")));
            Utility.Monitor.WriteLine("[Current Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(Directory.GetCurrentDirectory()));
            Utility.Monitor.WriteLine("[Base Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(BaseDirectory));
            Utility.Monitor.WriteLine("[Data Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(DataDirectory));
            Utility.Monitor.WriteLine("[Shared Directory: {0}]", ConsoleColorInformational(), Utility.GetShortPath(SharedDirectory));
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

            if (File.Exists(Path.Combine(Core.BuildInfoDir, "Build.info")) == false)
                Core.LoggerShortcuts.BootError(string.Format("Configuration error \"{0}\" is missing.", Path.Combine(Core.BuildInfoDir, "Build.info")));

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

            #endregion Boot Errors
#if DEBUG
            Utility.Monitor.WriteLine("[Debug Build Enabled]", ConsoleColorInformational());
#else
            Utility.Monitor.WriteLine("[Release Build Enabled]", ConsoleColorInformational());
#endif

#if DEBUG
            //  Turn off saves for DEBUG builds
            AutoSave.SavesEnabled = false;
#endif
            state = AutoSave.SavesEnabled;
            Utility.Monitor.WriteLine("[Saves are {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = m_useLoginDB;
            Utility.Monitor.WriteLine("[Using login database {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            Utility.Monitor.WriteLine("[Shard configuration is {0}.]", ConsoleColorInformational(), m_Server);
            state = RuleSets.ResourcePoolRules();
            Utility.Monitor.WriteLine("[Resource Pool is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.UOTC_CFG;
            Utility.Monitor.WriteLine("[Test Center functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.RuleSets.LoginServerRules();
            Utility.Monitor.WriteLine("[Login Server functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.UOBETA_CFG;
            Utility.Monitor.WriteLine("[Beta functionality is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = World.FreezeDryEnabled;
            Utility.Monitor.WriteLine("[Freeze dry system is {0}.]", ConsoleColorEnabled(state), TextEnabled(state));
            state = Core.UOEV_CFG;
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
#if false
            // we don't use the RunUO system for debugging scripts
            while (!ScriptCompiler.Compile(Core.Debug))
            {
                if (m_Quiet) //abort and exit if compile scripts failed
                    return;

                Console.WriteLine("Scripts: One or more scripts failed to compile or no script files were found.");
                Console.WriteLine(" - Press return to exit, or R to try again.");

                string line = Console.ReadLine();
                if (line == null || line.ToLower() != "r")
                    return;
            }
#endif

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
            Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { null });

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
        public static bool EmailCheck()
        {
            string noreply_password = Environment.GetEnvironmentVariable("AI.NOREPLY.PASSWORD");                // = (your email password. For this I use an AppPassword generated by Google)
            string noreply_address = Environment.GetEnvironmentVariable("AI.NOREPLY.ADDRESS");                  // = example: noreply @game-master.net
            string email_host = Environment.GetEnvironmentVariable("AI.EMAIL.HOST");                            //  = example: smtp.gmail.com
            string email_user = Environment.GetEnvironmentVariable("AI.EMAIL.USER");                            //  = example: luke.tomasello@gmail.com
            string email_port = Environment.GetEnvironmentVariable("AI.EMAIL.HOST.PORT");                       //  = 587
            string email_accounting = Environment.GetEnvironmentVariable("AI.EMAIL.ACCOUNTING");                // = example: aiaccounting @game - master.net    // new account created etc.
            string email_announcements = Environment.GetEnvironmentVariable("AI.EMAIL.ANNOUNCEMENTS");          // = announcements@game-master.net				// announce shard wide events
            string email_devnotify = Environment.GetEnvironmentVariable("AI.EMAIL.DEVNOTIFY");                  // = devnotify@game-master.net					// sent on server crash
            string email_distlist_password = Environment.GetEnvironmentVariable("AI.EMAIL.DISTLIST.PASSWORD");  //  = (your email password.For this I use an AppPassword generated by Google) ;
            string email_shardowner = Environment.GetEnvironmentVariable("AI.EMAIL.SHARDOWNER");                // = shard owners email address, certain private information

            if (noreply_password == null || noreply_address == null || email_host == null || email_user == null || email_port == null ||
                email_accounting == null || email_announcements == null || email_devnotify == null || email_distlist_password == null || email_shardowner == null)
            {
                return false;
            }

            return true;
        }
        public static bool GeoIPCheck()
        {
            string MMAccountId = Environment.GetEnvironmentVariable("AI.MMAccountId");
            string MMlicenseKey = Environment.GetEnvironmentVariable("AI.MMlicenseKey");

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
            Assembly m_scripts = null;
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
                    MethodInfo m = m_types[i].GetMethod("Configure", BindingFlags.Static | BindingFlags.Public);

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
                    MethodInfo m = m_types[i].GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);

                    if (m != null)
                        m_invoke.Add(m);
                }

                m_invoke.Sort(new CallPriorityComparer());

                for (int i = 0; i < m_invoke.Count; ++i)
                    ((MethodInfo)m_invoke[i]).Invoke(null, null);
            }
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

        public override void Write(string str)
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

        public override void WriteLine(string line)
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

        public override void WriteLine(string line)
        {
            for (int i = 0; i < m_Streams.Count; i++)
                ((TextWriter)m_Streams[i]).WriteLine(line);
        }

        public override void WriteLine(string line, params object[] args)
        {
            WriteLine(string.Format(line, args));
        }

        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.Default; }
        }
    }
}