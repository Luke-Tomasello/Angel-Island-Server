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

/* Scripts\Engines\CronScheduler\CronTasks.cs
 * CHANGELOG:
 *  7/12/2023, Adam
 *      Calculate the largest stack of each gem held by players.
 *          The Treasure Hunter NPC will then base their demands on this number.
 * 1/21/23, Adam (Automated Event System)
 *  Turn off Automated Event System unless AngelIslandRules()
 * 12/16/22, Adam (CKickPlayers)
 *  If a player is able to skate around the IP Address limits on a shard (a VPN perhaps,) and the MachinInfo 
 *      is collected for that player, subsequent logins to that account will blocked as normal. However, since the MachinInfo 
 *      comes in after the account is created and while the player is making their first character, we must
 *      schedule a Cron job to kick them.
 *      When the MachinInfo is collected after account creation, we set the ExceedsMachineInfoLimit flag on the account. 
 *      The Cron job runs every 5 minutes and kicks these players.
 * 9/4/22, Adam
 *      resuming normal saves for login in server. 
 *      since the shard is empty (no mobiles/items,) this is very fast.
 *      Why: some systems like FreezeDry, Recycle bin, music, file cleanup database, write bin files, but since these are not getting saved, we get startup complaints.
 *      Easier to just save the tiny world than to special case all those loaders.
 *  8/22/22, Adam
 *      Add EthicsPersistence item to the list of items excluded in IntMapItem Cleanup
 *      Add some debug output for InMapItemCleanup
 *  8/17/22, Adam (CLAutoSave)
 *      Add a call to AutoSave.Make Backup() so that the login srver saves get backed up
 *  8/5/22, Adam (Login Server Auto Save)
 *      Add the saving of shard config data (CoreAI.OnSave())
 *  8/3/22, Adam
 *      Turn off CCheckChamps for LoginServer
 *  8/1/22, Adam
 *      Add UONO_SVR to Inmate Maintenance.
 *      See comment below.
 *  7/31/22, Adam
 *      During Inmate Maintenance, where we kick players off the island if they have no murder counts. 
 *      Here we add a check for DateTime.UtcNow > pm.MinimumSentence. MinimumSentence is the new variable that can keep a player in prison regardless of murder counts.
 *  6/19/22, Yoar
 *      Account DB overhaul.
 *      
 *      Removed the 'CAccountDBSave' job. This job ran only for the DB master. This job wrote all account credentials
 *      from memory to the accounts DB. However, we don't need this job since we update the accounts DB on the fly
 *      whenever we set account credentials.
 *  8/18/22, Adam
 *      Add CLSAutoSave to handle the Login Server's special version of save (very limited.)
 *      Add a Save to LoginServer Wipe (save after wipe)
 *  6/15/22, Adam (Shard Wipe:DeleteItems)
 *      Add robustness checking around the item to delete
 *  3/15/22, Adam (PlantGrowthWorker)
 *      Replace explicit null parent check with a call to plant.ValidGrowthLocation as there more valid growth locations than we were allowing for.
 *  3/13/22, Adam
 *      Since Cron is in charge of growing plants, set plant.PlantSystem.NextGrowth = now to insure we grow the plant on our schedule
 *      Problem solved: If the server shuts down unexpectedly, plants growth timer won't have been saved. This change ensures if our 
 *      server is running at the scheduled time - plants will grow.
 *  2/28/22, Adam,
 *  Moving Auto save here
 *      We keep the framework 'class AutoSave' as it still has useful functionality/checks
 *  1/27/22, Adam (MusicSales)
 *      Add two leader boards for MusicSales
 *          a. total gold earned from sales
 *          b. total number of tracks sold
 *  1/16/22, Adam (Console.WriteLine)
 *      Fixed an exception caused by improper format syntax in Console.WriteLine
 *  1/15/22, Yoar
 *      Added TownshipItemDefrag
 *  10/25/21, Adam
 *      Add support for BOD Leaderboard
 *      Add a database of NonDecayingHouses (not serialized, not used.) (cuts down on console spam.)
 * 10/24/21, Adam
 * 	Improved/more house decay messages (maybe too much)
 * 	Add support for freezing account decay
 * 10/11/21, Adam
 *  Add support for spawning angry miners on a boat
 * 9/17/21, Adam
 *  Remove timer optimization from HouseDecay. 
 *  The problem was with Updating the BaseHouse.HouseDecayLast. BaseHouse.HouseDecayLast needs to happen after the houses are checked,
 *      but the way timer-decay-workers were firing, they were happining before the lastcheck.
 *      Performance is still pretty good here, so no worries.
 * 9/10/21, Pix
 *  Added CAccountDBSave cron job
 * 9/8/21, Adam
 *  Turn on AES for TestCenter (testing system before live)
 * 8/26/21, Adam (PlayerQuestCleanupWorker)
 *      Fix a bug in cleanup logic.
 * 8/16/21, Adam (MobileLifespanCleanup)
 *      Moving this to every 10 minutes. Even after many speedups, were still 3-4 second lags *on the old hardware*
 *      On our new hardawre, were able to delete 1884 mobiles in 0.02 seconds. This is acceptable.
 *      Setting our to timer 10 minutes from once a day, and we'll rarely even get close to this number of mobiles to delete.
 * 8/16/21, Adam (LoginServer - class CWipeShard)
 *      Warning: WIPE'S SHARD
 *      This job is run at 12am each night on the LoginServer.
 *      The loginserver has no need for items or mobiles, and doesn't need cron jobs to manage them.
 *      We do this here to save the developer from having to hand wipe a shard before use.
 *      This action saves at least 26.4M of runtime memory and untold proceesing time (item decay, mobile lifespan cleanup, etc.)
 * 6/23/2021, Adam: 
 *		New model: The old cron scheduler was eating too many cpu cycles by trying to complete its task in a single timer slice. 
 *			This was fine some of the time, but some jobs are somewhat time consuming. Like for instance, enumerating all the items in the world, 
 *				THEN performing some task on them. (like for instance, decaying the item.)
 *		The new modle will enumerate all the world items, do some quick filtering, then set a ~1 second callback timer to handle each of the items that matched the filter. 
 *			The benefit is that the slice-timer can break from processing these queued requests to manage more urgent foreground tasks (like networking, UI, etc.)
 *			The slice-timer will then resume processing of these queued tasks next 'slice'. By observing the console output, you will see the timer never really gets far behind, maybe ~800 queued tasks. 
 *			They are then usually cleaned up on the next slice. This is far better than lagging the player while you �grow plants�. Heh.
 *	7/31/11, Adam
 *		Check CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeHouseDecay) before cleaningup houses!
 *		this account should not be deleted because house decay is frozen, accounts must remain!
 *		((when an account is deleted, the associated houses are deleted.)
 *	2/20/11, Adam
 *		Due to low population, turn off:
 *		Server Wars, Town Invasion, Ransom Chest, Crazy Map, and Donation reminder
 *	2/8/11, Adam
 *		remove auto fightbroker feature as it's just too damn harsh
 *  11/21/10, adam
 *      Condition jobs on Core.AngelIsland
 *	5/10/10, adam
 *		Add a new StableCharge task that charges players for stabled pets. 
 *		See Scripts\Mobiles\Vendors\NPC\AnimalTrainer.cs for a full description.
 *	3/20/10, adam
 *		1. change all jobs with a minute-offset designed to avoid collisions to the new '?' specification
 *		2. Add compiler so we can use the '?' specification.
 *		Remember we need to precompile any specification that uses the '?' since the specification is reinterpreted each pass
 *		which means it is possibe a job will not fire since the time is marching forward and the random ('?') value keeps moving.
 *		We precompile these specifications to lock the random value at job creation time.
 *		3. Turn off Crazy Map day and turn on Server Wars - Automated Event System (12:00 noon on 5th Sunday)
 *	3/17/10, Adam
 *		Add a new Inmate Management routine that handles all of these cases:
 *		(1) player has inmate flag set but he is not in prison
 *		(2) player is in prison but does not have inmate flag set
 *		(3) player is in prison but has no counts
 *	6/26/09 - Adam
 *		correct serial value for the summer champ
 *	4/6/09, Adam
 *		Turn off ServerWarsAES - to many player complaints
 *	1/4/09, Adam
 *		Add IntMapStorage protection like we do for Items
 *	11/12/08, Adam
 *		Calculate and email Consumer Price Index log
 *	10/10/08, Pix
 *		Added call to TownshipSettings.CalculateFeesBasedOnServerActivity in CAccountStatistics.
 *	9/26/08, Adam
 *		Increase MobileLifespanCleanup from 10 seconds to 30 seconds max
 *	9/25/08, Adam
 *		Add "Performing routine maintenance, please wait." to MobileLifespanCleanup().
 *		We had improved MobileLifespanCleanup() MUCH (see comments below) but the performance stills causes a little lag
 *			we're therefore moving the call to once per day (3:29 AM) and adding a system message
 *	7/3/08, Adam
 *		Update ServerWarsAES to send a mass email announcing the 'cross shard challange'
 *	7/1/08, Adam
 *		Change over from the in-game email model to the distribution list email model (managed by the web server)	
 *		lets keep the old code around for future needs, but for now lets use our mailing list
 *		NOTE: If you change this make sure to remove the password from the subject line
 *	5/19/08. Adam
 *		Return Death Star to hourly execution since we resolved the slow performance issue.
 *	5/11/08, Adam
 *		- Remove sorting of lifespan lists
 *		- Add enhanced profiler tracking
 *	5/7/08, Adam
 *		Add JetBrains dotTrace profiler stuff.
 *		To use the API for profiling your applications, install JetBrains dotTrace:
 *		Reference JetBrains.dotTrace.Api.dll (located in the dotTrace installation directory) in your application project. 
 *		Insert the following code in a place of code you want to profile: 
 *		JetBrains.dotTrace.Api.CPUProfiler.Start();
 *			//The code which you want to profile
 *		JetBrains.dotTrace.Api.CPUProfiler.StopAndSaveSnapShot(); 
 *		Start the profiled application from within dotTrace, and in the Control Application dialog, clear Start profiling application immediately check box. 
 *	5/3/08. Adam
 *		MobileLifespanCleanup
 *		- Sort the list of mobiled to deleted base on age, oldest first.
 *			We do this to ensure we deleted the oldest first should we run out of time and have to abort the cleanup
 *		- Reschedule the remainder of the cleanup 1 hour from the time we ran out of time .. this is for TEST purposes only.
 *			I want to see how long the secon cleanup takes (I have a funny feeling it won't be slow like the first one .. Maybe Garbage Collection
 *	4/24/08, Adam
 *		Remove the periodic spawner cache reload from the Cron Scheduler as this is loaded once on server up.
 *	4/7/08, Adam
 *		- merge the Nth-Day-Of-The-Month checking into the Register() function.
 *		Add reference to syntax document
 *		http://en.wikipedia.org/wiki/Cron
 *		- Add the notion of: Cron.CronLimit.isldom.must_not_be_ldom to the Register() function.
 *			See the full explanation in CronScheduler.cs comments section.
 *	3/20/08, Adam
 *		I'm rewriting the heartbeat system (Engines\Heartbeat\heartbeat.cs)
 *		Over time we added lots of functionality to heartbeat, and it's time for a major cleanup.
 *		I'm using the system from my shareware product WinCron.
 */

using Scripts.Engines.Leaderboard;
using Server.Accounting;				// Emailer
using Server.Commands;
using Server.Diagnostics;               // log helper
using Server.Engines.ChampionSpawn;		// champs
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Misc;						// TestCenter
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.SMTP;						// new SMTP engine
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Server.Utility;
// look here for cron specifications https://crontab.guru/
namespace Server.Engines.CronScheduler
{
    class CronTasks
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(OnWorldLoad);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(OnWorldSave);
        }
        public static void OnWorldLoad()
        {
            Console.WriteLine("World Load Cron Tasks...");
            // careful here... don't call anything that depends on other data stores having been loaded.
            // Leader board for example needs the accounts database to be loaded.
            Console.WriteLine("World Load Cron Tasks Complete.");
        }
        public static void OnWorldSave(WorldSaveEventArgs e)
        {
            Console.WriteLine("World Save Cron Tasks...");
            Console.WriteLine("World Save Cron Tasks Complete.");
        }

        // Add your scheduled task here and implementation below
        public static void Initialize()
        {
            if (!Core.RuleSets.LoginServerRules())
            {
                // make sure all our champ engines are present and accounted for
                Cron.QueuePriorityTask(new CCheckChamps().CheckChamps);             // once on startup, and...
                Cron.Register(new CCheckChamps().CheckChamps, "? 6 * * *");         // random minute of 6 o'clock hour

                // command for resetting all champs ([run resetchamps)
                Cron.Register(new CResetChamps().ResetChamps, null);                // a NULL cron spec never runs, but is available for [Run <command>
            }

            // LoginServer special tasks
            if (Core.RuleSets.LoginServerRules())
            {
                // the LoginServer has no items, has no mobiles, has no tasks (yet)
                // We simply wipe the LoginServer of everything every 24 hours
                // We do it here since we have no ability to log in, and the 'shard' is likely a clone of some game-server
                Utility.Monitor.WriteLine("Shard wipe in progress. This may take several minutes...", ConsoleColor.Red);
                new CWipeShard().WipeShard();                                       // ASAP
                Cron.Register(new CWipeShard().WipeShard, "* 3 * * *");             // 3 AM
#if false
                // Login Server AutoSave (accounts, ip exceptions, firewall)
                double minutes = TimeSpan.FromMinutes(CoreAI.WorldSaveFrequency).TotalMinutes;
                string autoSaveDelay = string.Format("*/{0} * * * *", (int)minutes);
                Cron.Register(new CLSAutoSave().AutoSave, autoSaveDelay);
#else
                /*
                 * 9/4/22, Adam
                 *   resuming normal saves for login in server.
                 *   since the shard is empty(no mobiles/ items,) this is very fast.
                 *   Why: some systems like FreezeDry, Recycle bin, music, file cleanup database, write bin files, but since these are not getting saved, we get startup complaints.
                 */
                // shard AutoSave
                double minutes = TimeSpan.FromMinutes(CoreAI.WorldSaveFrequency).TotalMinutes;
                string autoSaveDelay = string.Format("*/{0} * * * *", (int)minutes);
                Cron.Register(new CAutoSave().AutoSave, autoSaveDelay);
#endif

                // Backup database cleanup - on demand
                Cron.Register(new CBackupDatabaseCleanup().BackupDatabaseCleanup, null);
            }
            else
            {
                // kick players that are in violation of our machine limit (HardwareInfo,) but were able to slip through
                //  due to the MachineInfo coming in after login, and during character creation - every 5 minutes
                Cron.Register(new CKickPlayers().KickPlayers, "*/5 * * * *");

                // Prison players that come to our shard over a Tor Exit Node.
                //  Anonymous access like a VPN, Proxy, or other  anonymous access mechanism gets you watch listed.
                Cron.Register(new CHandleIPCheaters().HandleIPCheaters, "*/3 * * * *");

                // shard AutoSave
                double minutes = TimeSpan.FromMinutes(CoreAI.WorldSaveFrequency).TotalMinutes;
                string autoSaveDelay = string.Format("*/{0} * * * *", (int)minutes);
                Cron.Register(new CAutoSave().AutoSave, autoSaveDelay);

                // Backup database cleanup - on demand
                Cron.Register(new CBackupDatabaseCleanup().BackupDatabaseCleanup, null);

                // Unusual Container Spawner defrag - every 5 minutes
                Cron.Register(new CUnusualContainerDefrag().UnusualContainerDefrag, "*/5 * * * *");

                // Unusual Container Spawner total respawn - on demand only
                Cron.Register(new CUnusualContainerTotalRespawn().UnusualContainerTotalRespawn, null);

                // Leader board update - every 5 minutes
                Cron.Register(new CLeaderBoard().LeaderBoard, "*/5 * * * *");

                // Stable Charges - every 10 minutes
                if (Core.RuleSets.AngelIslandRules())
                {
                    Cron.Register(new CStableCharge().StableCharge, "? 3 * * *");  // random minute of 3 o'clock hour
                }
                // Auto add players to Fight Broker - every 10 minutes
                // remove auto fightbroker feature as it's just too damn harsh
                //if (Core.UOMO && false)
                //Cron.Register(new CAutoFightBroker().AutoFightBroker, "*/10 * * * *");

                // Vendor Restock - every 30 minutes
                Cron.Register(new CVendorRestock().VendorRestock, "*/30 * * * *");

                // Recieve Message - once per year
                //  Because of the way cron works, we need to put something in here, so I picked the 12th month of the year.
                //  ReceiveMessage has no usual work to do, so this is really a placeholder to include it into the system.
                //  When a developer uses the Cron.PostMessage() function, that real message is placed in the Priority Queue for immediate execution.
                Cron.Register(new CReceiveMessage().ReceiveMessage, "* * * 12 *");

                // Item Decay - every 30 minutes
                Cron.Register(new CItemDecay().ItemDecay, "*/30 * * * *");

                // Check NPCWork - every 15 minutes
                //	6/22/2021, adam: we don't to enum all mobiles to call this function.
                //	any mobiles that need it can set their own work timer.
                //Cron.Register(new CNPCWork().NPCWork, "*/15 * * * *");

                // Town Crier - every 10 minutes
                Cron.Register(new CTownCrier().TownCrier, "*/10 * * * *");

                #region Cleanup functions
                // Account cleanup - daily (7:? AM)
                Cron.Register(new CAccountCleanup().AccountCleanup, "? 7 * * *");           // random minute the 7th hour

                // internal map mobile/item cleanup - every 60 minutes
                Cron.Register(new CIntMapMobileCleanup().IntMapMobileCleanup, "? * * * *"); // random minute of every hour

                Cron.Register(new CIntMapItemCleanup().IntMapItemCleanup, "? * * * *");     // random minute of every hour

                // expire old mobiles - every 10 minutes
                Cron.Register(new CMobileLifespanCleanup().MobileLifespanCleanup, "*/10 * * * *"); // every 10 minutes

                // remove deleted mobiles from music config database - every 60 minutes
                Cron.Register(new CMusicConfigCleanup().MusicConfigCleanup, "? * * * *");       // random minute of every hour

                // every 24 hours cleanup orphaned guildstones (6:00 AM)
                Cron.Register(new CGuildstoneCleanup().GuildstoneCleanup, "0 6 * * *");         // at exactly 6 (because players depend on this to loot the township.)

                // every 24 hours cleanup orphaned Player NPCs (6:? AM)
                Cron.Register(new CPlayerNPCCleanup().PlayerNPCCleanup, "? 6 * * *");           // random minute of the 6 o'clock hour

                // every 24 hours 'standard' cleanup (6:? AM)
                Cron.Register(new CStandardCleanup().StandardCleanup, "? 6 * * *");             // random minute of the 6 o'clock hour

                // every 24 hours Strongbox cleanup (6:? AM)
                Cron.Register(new CStrongboxCleanup().StrongboxCleanup, "? 6 * * *");           // random minute of the 6 o'clock hour
                #endregion Cleanup functions
                // find players using the robot skills - every 60 minutes
                Cron.Register(new CFindSkill().FindSkill, "? * * * *");                         // random minute of every hour

                // Killer time cleanup - every 30 minutes
                Cron.Register(new CKillerTimeCleanup().KillerTimeCleanup, "*/30 * * * *");

                // House Decay - every 10 minutes
                Cron.Register(new CHouseDecay().HouseDecay, "*/10 * * * *");

                // Murder Count Decay - every 15 minutes
                Cron.Register(new CMurderCountDecay().MurderCountDecay, "*/15 * * * *");

                if (Core.RuleSets.PlantSystem())
                {
                    // Plant Growth - daily (12:00 AM)
                    if (Plants.PlantSystem.Enabled)
                    {
                        Utility.Monitor.WriteLine("Plant System Enabled.", ConsoleColor.Green);
                        Cron.Register(new CPlantGrowth().PlantGrowth, "0 0 * * *");                 // every night at exactly midnight
                    }
                }

                if (Apiculture.ApicultureSystem.Enabled)
                    Cron.Register(new CHiveGrowth().HiveGrowth, "0 0 * * *"); // every night at exactly midnight

                // Overland Merchant CHANCE - every hour
                //  10/15/22 Adam: Allowing this for all shards. I don't think it hurts the Siege experience.
                //  4/28/23, Adam. Lets save this for a content patch. Loot will need to be adjusted.
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.AllShards)
                {
                    Cron.Register(new COverlandSystem().OverlandSystem, "? * * * *");           // random minute of every hour
                    Cron.Register(new CCountGems().CountGems, "0 */4 * * *");			        // every 4 hours
                }

                // track staff owned items
                Cron.Register(new CTrackStaffOwned().TrackStaffOwned, "0 */4 * * *");           // every 4 hours

                // Dynamic camps - every 30 minutes
                if (!Core.RuleSets.AngelIslandRules())
                {
                    Cron.Register(new CDynamicCampSystem().DynamicCampSystem, "*/30 * * * *");  // every 30 minutes
                }

                // Reload the Spawner Cache - daily (6:19 AM)
                // no longer used - cache is loaded on server-up
                // Cron.Register(new CReloadSpawnerCache().ReloadSpawnerCache, "19 6 * * *");	// minute 19 of 6 o'clock hour

                // send an email reminder to players to donate
                // Adam: turn off until we have players :\
                //Cron.Register(new CEmailDonationReminder().EmailDonationReminder, "? 6 20 * *");	// random minute of the 6 o'clock hour on the 20th of each month

                // email tester job
                //Cron.Register(new CTestJob().TestJob, "* * * * *");	

                // email a report of weapon distribution among players
                Cron.Register(new CWeaponDistribution().WeaponDistribution, "? 3 * * *");           // now random minute of 3 o'clock hour

                #region Automated Event System (Not on Siege, yet)
                // for now, make AES Angel Island only
                if (Core.RuleSets.AngelIslandRules())
                {
                    // Server Wars - Automated Event System (12:00 noon on 5th Sunday)
                    // Note: set 24 hour advance notice
                    // Adam: turn off untill we have players :\
                    //if (Core.UOAI || Core.UOAR)
                    //Cron.Register(null, new CServerWarsAES().ServerWarsAES, "0 12 * * 6",
                    //  true, new Cron.CronLimit(5, DayOfWeek.Saturday, Cron.CronLimit.isldom.must_not_be_ldom)/*, useGameTime: true*/);
                    Cron.Register(new CServerWarsAES().ServerWarsAES, null); // on demand

                    // Town Invasion - Automated Event System (12:00 noon on 3rd Sunday)
                    // Note: set 24 hour advance notice
                    Cron.Register(new CTownInvasionAES().TownInvasionAES, "0 12 * * 6",
                        true, new Cron.CronLimit(3, DayOfWeek.Saturday, Cron.CronLimit.isldom.must_not_be_ldom)/*, useGameTime: true*/);

                    // Kin Ransom Quest - Automated Event System (12:00 noon on 4th Sunday)
                    // Note: set 24 hour advance notice
                    //Cron.Register(null, new CKinRansomAES().KinRansomAES, "0 12 * * 6",
                    //  true, new Cron.CronLimit(4, DayOfWeek.Saturday, Cron.CronLimit.isldom.must_not_be_ldom)/*, useGameTime: true*/);
                    Cron.Register(new CKinRansomAES().KinRansomAES, null); // on demand

                    // Crazy Map Day - Automated Event System (12:00 noon on 5th Sunday)
                    // Note: set 24 hour advance notice
                    Cron.Register(new CCrazyMapDayAES().CrazyMapDayAES, "0 12 * * 6",
                        true, new Cron.CronLimit(5, DayOfWeek.Saturday, Cron.CronLimit.isldom.must_not_be_ldom)/*, useGameTime: true*/);
                }
                #endregion Automated Event System
                // every 10 minutes collect account statistics
                Cron.Register(new CAccountStatistics().AccountStatistics, "*/10 * * * *");

                // every hour run through guild fealty checks
                Cron.Register(new CGuildFealty().GuildFealty, "? * * * *");                         // random minute of each hour

                // every hour run through PlayerQuestCleanup checks
                //  10/15/22 Adam: Allowing this for all shards. I don't think it hurts the Siege experience.
                if (Core.RuleSets.AngelIslandRules() || true)
                {
                    Cron.Register(new CPlayerQuestCleanup().PlayerQuestCleanup, "? * * * *");       // random minute of each hour
                }
                //  10/15/22 Adam: Allowing this for all shards. I don't think it hurts the Siege experience.
                if (Core.RuleSets.AngelIslandRules() || true)
                {
                    // every 5 minutes run through PlayerQuest announcements				        // every 5 minutes
                    Cron.Register(new CPlayerQuestAnnounce().PlayerQuestAnnounce, "*/5 * * * *");
                }

                // every 24 hours rotate player command logs (12 AM - midnight)
                Cron.Register(new CLogRotation().LogRotation, "0 0 * * *");                         // every night at exactly midnight

                // every month archive logs (12 AM - midnight)
                Cron.Register(new CLogArchive().LogArchive, "0 0 1 * *");                           // every month at exactly midnight

                // 8/21/21, Adam: not using any more. All backups are done server-side now.
                // at 2AM backup the complete RunUO directory (~8min)
                // (at 1:50 issue warning)
                // Cron.Register(new CBackupRunUO().BackupRunUO, "50 1 * * *");            // 2AM

                // 8/21/21, Adam: not using any more. All backups are done server-side now.
                // at 4AM we backup only the world files (~4min)
                // (at 3:50 issue warning)
                // Cron.Register(new CBackupWorldData().BackupWorldData, "50 3 * * *");    // 4AM

                //Townships
                {
                    Cron.Register(new CTownshipCharges().TownshipCharges, "0 0 * * *");        // 12:00 AM - midnight (visits rollover weekly, so we want to be as close as possible to the change in weeks)
                    Cron.Register(new CTownshipWallDecay().TownshipWallDecay, "5 9 * * *");    // 9:05 AM every day
                    Cron.Register(new CTownshipItemDefrag().TownshipItemDefrag, "10 9 * * *"); // 9:10 AM every day
                }

                // WealthTracker - 3 AM daily
                Cron.Register(new CWealthTracker().WealthTracker, "? 3 * * *");                 // random minute of the 3AM hour

                // ConsumerPriceIndex - 3 AM daily
                Cron.Register(new CConsumerPriceIndex().ConsumerPriceIndex, "? 3 * * *");       // random minute of the 3AM hour

                // need to ask Yoar about this. Didn't know we had Kin Factions!
                //  if so, we'll probably want this for Siege as well
                if (Core.RuleSets.AngelIslandRules())
                {
                    #region KIN_FACTIONS
#if KIN_FACTIONS
			// Kin Sigils - bi-hourly 
			Cron.Register(new CKinFactions().KinFactions, "0 */2 * * *");			// every 2 hours

			// Kin Logging - 1AM daily
			Cron.Register(new CKinFactionsLogging().KinFactionsLogging, "0 1 * * *");		// 1 AM Every day
#endif
                    #endregion KIN_FACTIONS
                }
                // 1/23/23, Adam: Probably okay for all shards. We'll leave it off for now. Maybe a content patch for later
                if (Core.RuleSets.AngelIslandRules())
                {
                    // http://www.infoplease.com/ce6/weather/A0844225.html
                    Cron.Register(new CSpringChamp().SpringChamp, "0 0 21 3 *");    // spring (vamp), about Mar. 21, (12:00 AM)
                    Cron.Register(new CSummerChamp().SummerChamp, "0 0 22 6 *");    // summer (pirate), about June 22, (12:00 AM)
                    Cron.Register(new CAutumnChamp().AutumnChamp, "0 0 23 9 *");    // autumn (bob), about Sept. 23, (12:00 AM)
                    Cron.Register(new CWinterChamp().WinterChamp, "0 0 22 12 *");   // winter (Azothu), about Dec. 22, (12:00 AM)
                }

                // Check InmateManagement - every 15 minutes
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
                    Cron.Register(new CInmateManagement().InmateManagement, "*/15 * * * *");
            } // if not LoginServer
        }
    }

    /* ---------------------------------------------------------------------------------------------------
	 * Above are the scheduled jobs, and below are the scheduled jobs' implementations.
	 * Please follow convention!
	 * The convention is:
	 * 1. The class name for job Foo is CFoo, and the main wraper function in CFoo is Foo(), and the worker function is FooWorker().
	 *    (This allows the built-in reporting system to accurately report what's running and when, and how long it took.)
	 * 2. Each implementation is wrapped in a so named #region
	 * 3. Each implementation displays the elapsed time and any other meaningful status. (No spamming the console)
	 * 4. Even though the CronScheduler wraps all calls in a try-catch block, you should add a try-catch block to your implementation so that detailed errors can be logged.
	 * 5. As always, all catch blocks MUST make use of LogHelper.LogException(e) to log the exception.
	 * Note: You should never need to touch CronScheduler.cs. If you think you need to muck around in there, please let Adam know first.
	 * ---------------------------------------------------------------------------------------------------
	 */
    #region HiveGrowth
    class CHiveGrowth
    {
        public void HiveGrowth()
        {
            Console.Write("Hive Growth started... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            Apiculture.ApicultureSystem.GrowAll();
            tc.End();
            Console.WriteLine("Checked {0} hives in: {1}", Apiculture.ApicultureSystem.Hives.Count, tc.TimeTaken);
        }
    }
    #endregion PlantGrowth
    #region Handle IP Cheaters
    class CHandleIPCheaters
    {
        public void HandleIPCheaters()
        {
            System.Console.WriteLine("HandleIPCheaters running...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int kicked = HandleIPCheatersWorker();
            tc.End();
            Console.WriteLine("HandleIPCheaters in {0}", tc.TimeTaken);
            if (kicked > 0)
                Console.WriteLine("{0} Players handled!", kicked);
        }
        private int HandleIPCheatersWorker()
        {
            int count = 0;
            foreach (var ns in NetState.Instances)
            {
                if (ns.Mobile != null && ns.Mobile.Account != null)
                {
                    (ns.Mobile as PlayerMobile).HandleIpCheater();
                }
            }

            return count;
        }
    }
    #endregion Handle IP Cheaters
    #region KickPlayers
    class CKickPlayers
    {
        public void KickPlayers()
        {
            System.Console.WriteLine("KickPlayers running...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int kicked = KickPlayersWorker();
            tc.End();
            Console.WriteLine("KickPlayers in {0}", tc.TimeTaken);
            if (kicked > 0)
                Console.WriteLine("{0} Players kicked!", kicked);
        }
        private int KickPlayersWorker()
        {
            int count = 0;
            foreach (Account current in Accounting.Accounts.Table.Values)
            {
                if (current.AccessLevel > AccessLevel.Player)
                    continue; // ignore staff accounts

                if (current.GetFlag(Account.AccountFlag.ExceedsMachineInfoLimit))
                {
                    for (int ix = 0; ix < current.Length; ix++)
                    {
                        PlayerMobile pm = (PlayerMobile)current[ix];
                        // pm.Inmate == false because we now allow these cheaters to live in Angel Island Prison as Mortal >:-)
                        //  The next time they login, PlayerMobile EventSink_Connected will toss them in prison.
                        if (pm != null && pm.NetState != null && pm.NetState.Running && pm.PrisonInmate == false)
                        {
                            if (!AccountHardwareLimiter.IsOk(current))
                            {
                                pm.Say("I've been kicked!");
                                Utility.Monitor.WriteLine("{0} kicked, ExceedsMachineInfoLimit", ConsoleColor.Red, pm.Name);
                                LogHelper logger = new LogHelper("ExceedsMachineInfoLimit.log", false, true);
                                logger.Log(LogType.Mobile, pm, "kicked, ExceedsMachineInfoLimit");
                                logger.Finish();
                                pm.NetState.Dispose();
                                count++;
                            }
                            else
                                current.SetFlag(Account.AccountFlag.ExceedsMachineInfoLimit, false);

                        }
                    }
                }
            }
            return count;
        }
    }
    #endregion KickPlayers
    #region AutoSave
    class CAutoSave
    {
        public void AutoSave()
        {
            System.Console.WriteLine("AutoSave: ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            bool saved = AutoSaveWorker();
            tc.End();
            if (saved)
                Console.WriteLine("AutoSave in {0}", tc.TimeTaken);
            else
                Console.WriteLine("AutoSave disabled.");
        }
        private bool AutoSaveWorker()
        {
            // final check to see if we should save
            if (!Server.Misc.AutoSave.SavesEnabled || AutoRestart.Restarting)
                return false;

            Server.Misc.AutoSave.Save();

            return true;
        }
    }
#if false
    class CLSAutoSave
    {
        public void AutoSave()
        {
            System.Console.WriteLine("Login Server AutoSave: ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            bool saved = AutoSaveWorker();
            tc.End();
            if (saved)
                Console.WriteLine("Login Server AutoSave in {0}", tc.TimeTaken);
            else
                Console.WriteLine("Login Server AutoSave disabled.");
        }
        private bool AutoSaveWorker()
        {
            // final check to see if we should save
            if (!Server.Misc.AutoSave.SavesEnabled || AutoRestart.Restarting)
                return false;

            // backup the saves folder - must be first
            Server.Misc.AutoSave.Backup();
            /*
            // Backup file manager
            Misc.AutoSave.Save(new WorldSaveEventArgs(false));

            */
            // save shard config data
            CoreAI.OnSave(new WorldSaveEventArgs(false));

            // save accounts
            Accounts.Save(new WorldSaveEventArgs(false));

            // save ip exceptions
            Server.Accounting.IPException.EventSink_OnWorldSave(new WorldSaveEventArgs(false));

            // save firewall
            Server.Firewall.EventSink_OnWorldSave(new WorldSaveEventArgs(false));

            return true;
        }
    }
#endif
    #endregion AutoSave
    #region Backup Database Cleanup
    class CBackupDatabaseCleanup
    {
        public void BackupDatabaseCleanup()
        {
            System.Console.WriteLine("Backup Database Cleanup: ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int defragged = BackupDatabaseCleanupWorker();
            tc.End();
            System.Console.WriteLine("deleted {0} backup files in {1}", defragged, tc.TimeTaken);
        }

        private int BackupDatabaseCleanupWorker()
        {
            int defragged = 0;

            string root = Path.Combine(Core.BaseDirectory, "Backups\\Automatic");
            while (AutoSave.FileManagerDatabase.Count > CoreAI.BackupCount)
            {
                string[] existing = Directory.GetDirectories(root);
                DirectoryInfo dir;
                string toDelete = AutoSave.FileManagerDatabase.Dequeue();
                dir = Match(existing, toDelete);
                Console.WriteLine("File Manager: Dequeuing {0}", toDelete);

                if (dir != null && Directory.Exists(dir.FullName))
                {
                    Console.WriteLine("Backup File Manager: Deleting {0}", dir.Name);
                    try { Directory.Delete(dir.FullName, true); defragged++; }
                    catch (Exception ex)
                    {
                        Utility.Monitor.WriteLine(ex.Message.Trim(), ConsoleColor.Red);
                    }
                }
            }

            return defragged;
        }
        private DirectoryInfo Match(string[] paths, string match)
        {
            for (int i = 0; i < paths.Length; ++i)
            {
                DirectoryInfo info = new DirectoryInfo(paths[i]);

                if (info.Name.StartsWith(match))
                    return info;
            }

            return null;
        }
    }
    #endregion Backup Database Cleanup
    #region Unusual Container Spawner
    class CUnusualContainerDefrag
    {
        public void UnusualContainerDefrag()
        {
            //System.Console.Write("Unusual Container Defrag ... ");
            System.Console.WriteLine("Unusual Container Defrag: ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int defragged = UnusualContainerDefragWorker();
            tc.End();
            System.Console.WriteLine("added {0} Unusual Containers in {1}", defragged, tc.TimeTaken);
        }

        private int UnusualContainerDefragWorker()
        {
            int defragged = UnusualContainerSpawner.DeFrag();
            return defragged;
        }
    }
    class CUnusualContainerTotalRespawn
    {
        public void UnusualContainerTotalRespawn()
        {
            //System.Console.Write("Unusual Container total respawn ... ");
            System.Console.WriteLine("Unusual Container total respawn:");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int boxes = UnusualContainerTotalRespawnWorker();
            tc.End();
            System.Console.WriteLine("added {0} Unusual Containers in {1}", boxes, tc.TimeTaken);
        }

        private int UnusualContainerTotalRespawnWorker()
        {
            int generated = UnusualContainerSpawner.TotalRespawn();
            return generated;
        }
    }
    #endregion Unusual Container Spawner
    #region LeaderBoard
    class CLeaderBoard
    {
        public void LeaderBoard()
        {
            System.Console.Write("LeaderBoard ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int PlayersAdded = 0;
            int LeaderBoardsChecked = LeaderBoardWorker(out PlayersAdded);
            tc.End();
            System.Console.WriteLine("checked {0} LeaderBoards in {1}", LeaderBoardsChecked, tc.TimeTaken);
        }
        public enum LeaderBoardJob
        {
            farmed,
            pvm,
            wealth,
            BodPoints,
            BodGold,
            Music, // musicGold & musicSales
            prisonQuest,
            PrisonSpawn,
        }
        private int LeaderBoardWorker(out int added)
        {
            int iChecked = 0;
            added = 0;
            try
            {
                // order displayed is determined by order created
                Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(LBJobTick), new object[] { LeaderBoardJob.pvm }); iChecked++;
                Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(LBJobTick), new object[] { LeaderBoardJob.farmed }); iChecked++;
                Timer.DelayCall(TimeSpan.FromSeconds(3), new TimerStateCallback(LBJobTick), new object[] { LeaderBoardJob.wealth }); iChecked++;
                Timer.DelayCall(TimeSpan.FromSeconds(4), new TimerStateCallback(LBJobTick), new object[] { LeaderBoardJob.BodPoints }); iChecked++;
                Timer.DelayCall(TimeSpan.FromSeconds(5), new TimerStateCallback(LBJobTick), new object[] { LeaderBoardJob.BodGold }); iChecked++;
                Timer.DelayCall(TimeSpan.FromSeconds(6), new TimerStateCallback(LBJobTick), new object[] { LeaderBoardJob.Music }); iChecked++; iChecked++;
                Timer.DelayCall(TimeSpan.FromSeconds(7), new TimerStateCallback(LBJobTick), new object[] { LeaderBoardJob.prisonQuest }); iChecked++;
                Timer.DelayCall(TimeSpan.FromSeconds(8), new TimerStateCallback(LBJobTick), new object[] { LeaderBoardJob.PrisonSpawn }); iChecked++;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in LeaderBoard code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }

        private void LBJobTick(object state)
        {
            try
            {
                object[] aState = (object[])state;
                LeaderBoardJob job = (LeaderBoardJob)aState[0];
                switch (job)
                {
                    case LeaderBoardJob.pvm:
                        {
                            #region Initialize
                            const string pvm = "PvM Stats";
                            LeaderboardTab tab = LeaderboardTracker.GetTab(pvm);
                            if (tab == null)
                                tab = LeaderboardTracker.AddTab(pvm);
                            tab.Clear();
                            #endregion Initialize
                            #region PvM
                            List<KeyValuePair<Mobile, int>> PvM = DataRecorder.DataRecorder.TopPvMers(DataRecorder.DataRecorder.GeneralPvMRankings);
                            if (PvM != null)
                            {
                                int rank = 0;
                                foreach (KeyValuePair<Mobile, int> kvp in PvM)
                                {
                                    if (kvp.Key == null)
                                        continue;

                                    if (!(Core.Debug || Core.UOTC_CFG))
                                        if (kvp.Key.AccessLevel > AccessLevel.Player)
                                            continue;

                                    tab.AddLine(string.Format("#{2} {0} has dealt {1} damage points.", kvp.Key.Name, kvp.Value, ++rank));
                                    if (rank == 10)
                                        break;
                                }
                            }
                            #endregion PvM
                            break;
                        }
                    case LeaderBoardJob.farmed:
                        {
                            #region Initialize
                            const string farmed = "Gold Earned";
                            LeaderboardTab tab = LeaderboardTracker.GetTab(farmed);
                            if (tab == null)
                                tab = LeaderboardTracker.AddTab(farmed);
                            tab.Clear();
                            #endregion Initialize
                            #region GoldFarmed
                            {
                                int rank = 0;
                                foreach (KeyValuePair<Mobile, double> kvp in DataRecorder.DataRecorder.GoldEarned)
                                {
                                    if (kvp.Key == null)
                                        continue;

                                    tab.AddLine(string.Format("#{2} {0} earned {1} gold points.", kvp.Key.Name, (int)kvp.Value, ++rank));
                                    if (rank == 10)
                                        break;
                                }
                            }
                            #endregion GoldFarmed
                            break;
                        }
                    case LeaderBoardJob.wealth:
                        {
                            #region Initialize
                            const string wealth = "Wealthiest";
                            LeaderboardTab tab = LeaderboardTracker.GetTab(wealth);
                            if (tab == null)
                                tab = LeaderboardTracker.AddTab(wealth);
                            tab.Clear();
                            #endregion Initialize
                            #region Account Wealth
                            // Do total account wealth here
                            GoldTracker.GoldTotaller[] gt = GoldTracker.GetSortedWealthArray((Core.Debug || Core.UOTC_CFG) ? false : true);
                            if (gt != null && gt.Length > 0)
                            {
                                int rank = 0;
                                foreach (GoldTracker.GoldTotaller gte in gt)
                                {
                                    tab.AddLine(string.Format("#{2} {0} is worth {1} gold.", gte.CharacterName, gte.TotalGold, ++rank));
                                    if (rank == 10)
                                        break;
                                }
                            }
                            #endregion Account Wealth
                            break;
                        }
                    case LeaderBoardJob.BodPoints:
                        {
                            #region Initialize
                            const string BodPoints = "BOD Points";
                            LeaderboardTab tab = LeaderboardTracker.GetTab(BodPoints);
                            if (tab == null)
                                tab = LeaderboardTracker.AddTab(BodPoints);
                            tab.Clear();
                            #endregion Initialize
                            #region BOD Points
                            Dictionary<Mobile, int> BODPoints = DataRecorder.DataRecorder.GetBODPoints;

                            List<KeyValuePair<Mobile, int>> new_list = new List<KeyValuePair<Mobile, int>>();
                            foreach (KeyValuePair<Mobile, int> kvp in BODPoints)
                                new_list.Add(kvp);

                            new_list.Sort((e1, e2) =>
                            {
                                return e2.Value.CompareTo(e1.Value);
                            });

                            if (BODPoints != null)
                            {
                                int rank = 0;
                                foreach (KeyValuePair<Mobile, int> kvp in new_list)
                                {
                                    if (kvp.Key == null)
                                        continue;

                                    if (!(Core.Debug || Core.UOTC_CFG))
                                        if (kvp.Key.AccessLevel > AccessLevel.Player)
                                            continue;

                                    tab.AddLine(string.Format("#{2} {0} has earned {1} BOD points.", kvp.Key.Name, kvp.Value, ++rank));
                                    if (rank == 10)
                                        break;
                                }
                            }
                            #endregion BOD Points
                            break;
                        }
                    case LeaderBoardJob.BodGold:
                        {
                            #region Initialize
                            const string BodGold = "BOD Gold";
                            LeaderboardTab tab = LeaderboardTracker.GetTab(BodGold);
                            if (tab == null)
                                tab = LeaderboardTracker.AddTab(BodGold);
                            tab.Clear();
                            #endregion Initialize
                            #region BOD Gold
                            Dictionary<Mobile, int> BODGold = DataRecorder.DataRecorder.GetBODGold;

                            List<KeyValuePair<Mobile, int>> new_gold_list = new List<KeyValuePair<Mobile, int>>();
                            foreach (KeyValuePair<Mobile, int> kvp in BODGold)
                                new_gold_list.Add(kvp);

                            new_gold_list.Sort((e1, e2) =>
                            {
                                return e2.Value.CompareTo(e1.Value);
                            });

                            if (BODGold != null)
                            {
                                int rank = 0;
                                foreach (KeyValuePair<Mobile, int> kvp in new_gold_list)
                                {
                                    if (kvp.Key == null)
                                        continue;

                                    if (!(Core.Debug || Core.UOTC_CFG))
                                        if (kvp.Key.AccessLevel > AccessLevel.Player)
                                            continue;

                                    tab.AddLine(string.Format("#{2} {0} has earned {1} in BOD gold.", kvp.Key.Name, kvp.Value, ++rank));
                                    if (rank == 10)
                                        break;
                                }
                            }
                            #endregion BOD Gold
                            break;
                        }
                    case LeaderBoardJob.Music:
                        {
                            #region Initialize
                            const string musicGold = "Music Gold";
                            LeaderboardTab tabMGold = LeaderboardTracker.GetTab(musicGold);
                            if (tabMGold == null)
                                tabMGold = LeaderboardTracker.AddTab(musicGold);
                            tabMGold.Clear();

                            const string musicSales = "Music Sales";
                            LeaderboardTab tabMSold = LeaderboardTracker.GetTab(musicSales);
                            if (tabMSold == null)
                                tabMSold = LeaderboardTracker.AddTab(musicSales);
                            tabMSold.Clear();
                            #endregion Initialize
                            #region Music Sales
                            Dictionary<Mobile, int[]> MusicSales = DataRecorder.DataRecorder.GetMusicSales;

                            List<KeyValuePair<Mobile, int[]>> music_gold_list = new List<KeyValuePair<Mobile, int[]>>();
                            List<KeyValuePair<Mobile, int[]>> music_sold_list = new List<KeyValuePair<Mobile, int[]>>();
                            foreach (KeyValuePair<Mobile, int[]> kvp in MusicSales)
                            {
                                music_gold_list.Add(kvp);
                                music_sold_list.Add(kvp);
                            }

                            // sort by gold
                            music_gold_list.Sort((e1, e2) =>
                            {
                                return e2.Value[0].CompareTo(e1.Value[0]);
                            });

                            // sort by units sold
                            music_sold_list.Sort((e1, e2) =>
                            {
                                return e2.Value[1].CompareTo(e1.Value[1]);
                            });
                            if (MusicSales.Count > 0)
                            {
                                int rank = 0;
                                // go music gold page
                                foreach (KeyValuePair<Mobile, int[]> kvp in music_gold_list)
                                {
                                    if (kvp.Key == null)
                                        continue;

                                    if (!(Core.Debug || Core.UOTC_CFG))
                                        if (kvp.Key.AccessLevel > AccessLevel.Player)
                                            continue;

                                    tabMGold.AddLine(string.Format("#{2} {0} has earned {1} gold.", kvp.Key.Name, kvp.Value[0], ++rank));
                                    if (rank == 10)
                                        break;
                                }

                                rank = 0;
                                // do music sold page
                                foreach (KeyValuePair<Mobile, int[]> kvp in music_sold_list)
                                {
                                    if (kvp.Key == null)
                                        continue;

                                    if (!(Core.Debug || Core.UOTC_CFG))
                                        if (kvp.Key.AccessLevel > AccessLevel.Player)
                                            continue;

                                    tabMSold.AddLine(string.Format("#{2} {0} has sold {1} songs.", kvp.Key.Name, kvp.Value[1], ++rank));
                                    if (rank == 10)
                                        break;
                                }
                            }
                            #endregion Music Sales
                            break;
                        }
                    case LeaderBoardJob.prisonQuest:
                        {
                            #region Initialize
                            const string prisonQuest = "Prison Quest";
                            LeaderboardTab tab = LeaderboardTracker.GetTab(prisonQuest);
                            if (tab == null)
                                tab = LeaderboardTracker.AddTab(prisonQuest);
                            tab.Clear();
                            #endregion Initialize
                            #region Prison Quest Points
                            Dictionary<Mobile, int[]> PrisonQuestPoints = DataRecorder.DataRecorder.GetPQuestPoints;

                            List<KeyValuePair<Mobile, int[]>> sorted_list = new List<KeyValuePair<Mobile, int[]>>();
                            foreach (KeyValuePair<Mobile, int[]> kvp in PrisonQuestPoints)
                                sorted_list.Add(kvp);

                            sorted_list = sorted_list.OrderByDescending(x => x.Value[2]).ThenByDescending(x => x.Value[1]).ToList();

                            if (PrisonQuestPoints.Count > 0)
                            {
                                int rank = 0;
                                foreach (KeyValuePair<Mobile, int[]> kvp in sorted_list)
                                {
                                    if (kvp.Key == null)
                                        continue;

                                    if (!(Core.Debug || Core.UOTC_CFG))
                                        if (kvp.Key.AccessLevel > AccessLevel.Player)
                                            continue;

                                    tab.AddLine(string.Format("#{0} {1} {2} escape{3} {4} rare{5} {6} sterling.",
                                        ++rank,             // rank
                                        kvp.Key.Name,       // name

                                        // escapes
                                        kvp.Value[0],
                                        kvp.Value[0] > 1 ? "s" : "",

                                        // rares
                                        kvp.Value[1],
                                        kvp.Value[1] > 1 ? "s" : "",

                                        // sterling
                                        kvp.Value[2]
                                        ));

                                    if (rank == 10)
                                        break;
                                }
                            }
                            #endregion Prison Quest Points
                            break;
                        }
                    case LeaderBoardJob.PrisonSpawn:
                        {
                            #region Initialize
                            const string PrisonSpawn = "Prison Spawn";
                            LeaderboardTab tab = LeaderboardTracker.GetTab(PrisonSpawn);
                            if (tab == null)
                                tab = LeaderboardTracker.AddTab(PrisonSpawn);
                            tab.Clear();
                            #endregion Initialize
                            #region Prison Spawn
                            List<KeyValuePair<Mobile, int>> ChampPvM = DataRecorder.DataRecorder.TopPvMers(DataRecorder.DataRecorder.ChampPvMRankings);
                            if (ChampPvM != null)
                            {
                                int rank = 0;
                                foreach (KeyValuePair<Mobile, int> kvp in ChampPvM)
                                {
                                    if (kvp.Key == null)
                                        continue;

                                    if (!(Core.Debug || Core.UOTC_CFG))
                                        if (kvp.Key.AccessLevel > AccessLevel.Player)
                                            continue;

                                    tab.AddLine(string.Format("#{2} {0} has dealt {1} damage points.", kvp.Key.Name, kvp.Value, ++rank));
                                    if (rank == 10)
                                        break;
                                }
                            }
                            #endregion Prison Spawn
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in LeaderBoard code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion LeaderBoard
    #region AutoFightBroker (unused)
    // On UO Mortalis shard everyone is mortal and on the Fight Broker
    class CAutoFightBroker
    {
        public void AutoFightBroker()
        {
            System.Console.Write("Auto Fight Broker ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int PlayersAdded = 0;
            int PlayersChecked = AutoFightBrokerWorker(out PlayersAdded);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} players added", PlayersChecked, tc.TimeTaken, PlayersAdded);
        }

        private int AutoFightBrokerWorker(out int added)
        {
            int iChecked = 0;
            added = 0;
            try
            {
                ArrayList ToAdd = new ArrayList();
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerMobile)
                    {
                        iChecked++;

                        // living players get announced
                        if (m.AccessLevel == AccessLevel.Player && m.Alive && m.Map != Map.Internal)
                        {
                            added++;
                            ToAdd.Add(m);
                        }
                    }
                }

                if (ToAdd.Count > 0)
                    for (int i = 0; i < ToAdd.Count; i++)
                        FightBroker.AddParticipant((ToAdd[i] as PlayerMobile));
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Auto Fight Broker code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }
    }
    #endregion AutoFightBroker

    #region ReceiveMessage (ok.)
    class CReceiveMessage
    {
        public void ReceiveMessage()
        {

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            bool processed = false;
            int queued = ReceiveMessageWorker(ref processed);
            tc.End();
            if (processed == true)
            {
                Console.WriteLine("Message processed in {0}", tc.TimeTaken);
                Console.WriteLine("{0} Messages remain in the queue.", queued);
            }
            else
            {
                Console.WriteLine("No messages.");
            }
        }

        private int ReceiveMessageWorker(ref bool processed)
        {
            lock (Cron.MessageQueue.SyncRoot)
            {
                if (Cron.MessageQueue.Count == 0)
                    return 0;
            }
            processed = true;
            Cron.ServerMessage message;
            lock (Cron.MessageQueue.SyncRoot)
                message = Cron.MessageQueue.Dequeue() as Cron.ServerMessage;

            switch (message.Message)
            {
                case Cron.MessageType.GBL_ORE_SPAWN:
                    processed = true;
                    return DoGblOreSpawn(message);

                case Cron.MessageType.MSG_DUMMY:
                    processed = true;
                    Console.WriteLine("Received Message: message={0} with {1} arguments.", message.Message.ToString(), message.Args.Length);
                    return Cron.MessageQueue.Count;
            }

            return Cron.MessageQueue.Count;
        }

        private int DoGblOreSpawn(Cron.ServerMessage message)
        {   // args for DoGblOreSpawn are the mobile, the resource type, and target
            PlayerMobile miner = message.Args[0] as PlayerMobile;
            Type resourceType = message.Args[1] as Type;
            object target = message.Args[2] as object;
            Point3D location = Point3D.Zero;
            bool boat = BaseBoat.FindBoatAt(miner) != null;
            int level = 0;
            if (target is Targeting.LandTarget)
                location = (target as Targeting.LandTarget).Location;
            else
                return Cron.MessageQueue.Count;

            switch (resourceType.Name)
            {
                case "ValoriteOre":
                    level = 4;
                    break;
                case "VeriteOre":
                    level = 3;
                    break;
                case "AgapiteOre":
                    level = 2;
                    break;
                default:
                    level = 1;
                    break;
            }

            // log the high-end ore discovery, and location
            if (level >= 3)
            {
                LogHelper logger = new LogHelper("RareOreMined.log", false);
                logger.Log(LogType.Mobile, miner, string.Format("{0} was mined at {1}", resourceType.Name.ToString(), location));
                logger.Finish();
            }

            // once in a while a whole miner camp will spring up
            // We will spawn a large camp if possible for level 4, an smaller camp otherwise.
            //  In the case where can't fit a camp at all, we will just spawn the mobiles.
            if (miner.AccessLevel > AccessLevel.Player)
                Console.WriteLine("Attempting to build a level {0} camp.", level);
            if (BuildAngryMinerCamp(miner, location, "AngryMinerCamp", resourceType.Name.ToString(), level) == false)
            {
                if (miner.AccessLevel > AccessLevel.Player)
                    Console.WriteLine("Unable to build an angry miner camp at {0}.", location);
                // We weren't able to build a camp, so send some angry minors to protect their vein
                int homeRange = 7;
                Point3D new_location = Point3D.Zero;
                for (int jx = 0; jx < level; jx++)
                {   // spawn level number of angry miners
                    if (boat)
                        new_location = Spawner.GetSpawnPosition(Map.Felucca, miner.Location, homeRange, SpawnFlags.Boat, null);
                    else
                        new_location = Spawner.GetSpawnPosition(Map.Felucca, location, homeRange, SpawnFlags.SpawnFar | SpawnFlags.AvoidPlayers, null);
                    AngryMiner am = new AngryMiner(resourceType.Name);
                    am.MoveToWorld(new_location, Map.Felucca);
                }
                // the angry miner aren't quite available yet, so set a short timer to anger them
                Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(AngryMinerTick), new object[] { miner, (IPoint3D)new_location });
            }

            Console.WriteLine("Received Message: message={0} with {1} arguments.", message.Message.ToString(), message.Args.Length);
            return Cron.MessageQueue.Count;
        }

        public static bool BuildAngryMinerCamp(PlayerMobile miner, Point3D location, string camp, string resourceType, int level)
        {
            // we use these house foundations for house placement calcs. 
            //  If we can 'fit' one of these, we can place out camp there.
            int multiIDBig = 0x00001451;    // medium three-story stone mansion <width>15</width> < height > 13 </ height >
            int multiIDSmall = 0x6A;        // wooden house 7x7

            // first try and build the bigger camp if level is 4
            if (level >= 4)
                if (BuildAngryMinerCampWorker(miner, location, camp, resourceType, multiIDBig, level) == true)
                    return true;

            // okay, if that didn't work, go with the smaller camp
            return BuildAngryMinerCampWorker(miner, location, "AngryMinerCampSmall", resourceType, multiIDSmall, level);
        }
        public static bool BuildAngryMinerCampWorker(PlayerMobile miner, Point3D location, string camp, string resourceType, int multiID, int level)
        {
            /* 7/27/2021, Adam
            * Automated placement of an AngryMinerCamp
            * This system uses the HousePlacement logic to place the camp. This is because blindly placing one of these camps
            * can result (and probably will,) result in an ugly placement with trees and whatnot through roofs, fires on the water, etc.
            * This is because when placing a house (via ID) you are *really* only placing the foundation, which must be at the same Z as the ground.
            * If you try to place a camp (which has no foundation,) all the tiles are looked at resulting in tons of Z placement failures.
            * To make placement work with the HousePlacement tool, we use the foundation of a similarly sized house. We can't use the camp itself
            * because camps often have tiles that are not Z friendly, which is a requirement for the house placement tool.
            * I tried relaxing the Z rule in HousePlacement (for camps only,) and that kind of worked, (avoided trees,) but still resulted in
            * a sufficient of ill-placed camps to discourage the idea. So...
            * We use the foundation of the "medium three-story stone mansion" (<width>15</width> < height > 13 </ height >) for our calcs.
            * The BrigandCamp is ID 0x1F6 for reference.
            */
            ArrayList toMove = new ArrayList();
            int homeRange;
            Point3D new_location = Point3D.Zero;
            HousePlacementResult res = HousePlacementResult.BadLand;
            for (homeRange = 0; homeRange < 17; homeRange++)
            {   // try 17 times (one screen) to get a new spawn position
                new_location = Spawner.GetSpawnPosition(Map.Felucca, location, homeRange, SpawnFlags.SpawnFar | SpawnFlags.AvoidPlayers, null);
                res = HousePlacement.Check(null, multiID, new_location, out toMove, false);

                if (res == HousePlacementResult.Valid)
                {
                    if (miner.AccessLevel > AccessLevel.Player)
                        miner.SendMessage(string.Format("HousePlacementResult: {0}", res.ToString()));
                    break;
                }
            }
            if (res != HousePlacementResult.Valid)
            {   // give up (we will spawn the miners directly.)
                return false;
            }
            else
            {   // sweet, found location.
                if (miner.AccessLevel > AccessLevel.Player)
                    miner.SendMessage(string.Format("Found a spawn location at: {0} in {1} tries", new_location, homeRange));

                // build our camp!
                Add.Invoke(miner, new_location, new_location, new string[] { camp, resourceType, level.ToString() });

                // the angry miner aren't quite available yet, so set a short timer to anger them
                Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(AngryMinerTick), new object[] { miner, (IPoint3D)new_location });
            }

            // now, if there were any animals/items that were in the way, move them at this time.
            if (toMove.Count > 0)
            {
                foreach (object ox in toMove)
                {
                    for (homeRange = 0; homeRange < 50; homeRange++)
                    {   // try 50 times to get a new spawn position
                        new_location = Spawner.GetSpawnPosition(Map.Felucca, location, homeRange, SpawnFlags.SpawnFar | SpawnFlags.AvoidPlayers, null);
                        if (new_location != location)
                            break;
                    }

                    if (new_location == location)
                    {   // give up (we will delete the spawn.)
                        if (miner.AccessLevel > AccessLevel.Player)
                            miner.SendMessage(string.Format("Unable to relocate a spawn.", new_location));
                        if (ox is Mobile && (ox as Mobile).Player != true)
                            (ox as Mobile).Delete();
                        /*else item (maybe spawner,) or real player. In this case we just punt, and will deal with it as needed.
                         spawner under camp, stuck player, etc. */
                    }
                    else
                    {
                        if (miner.AccessLevel > AccessLevel.Player)
                            miner.SendMessage(string.Format("Found a spawn relocation location at: {0} in {1} tries", new_location, homeRange));
                        if (ox is Mobile mobile && !mobile.Player)
                            (ox as Mobile).MoveToWorld(new_location, Map.Felucca);
                        else if (ox is Item item && item.Visible && item.Movable)
                            (ox as Item).MoveToWorld(new_location, Map.Felucca);
                    }
                }
            }

            return true;
        }
        private static void AngryMinerTick(object state)
        {
            object[] aState = (object[])state;
            PlayerMobile miner = aState[0] as PlayerMobile;
            IPoint3D location = aState[1] as IPoint3D;

            // now, tell our Angry Miners where to go and who to attack
            List<Mobile> list = new List<Mobile>();
            IPooledEnumerable eable = Map.Felucca.GetMobilesInRange((Point3D)location, 14);
            foreach (Mobile m in eable)
            {   // remember this mobile
                if (m is AngryMiner && m.Alive)
                    list.Add(m);
            }
            eable.Free();

            foreach (Mobile angryMiner in list)
            {   // I originally tried setting (angryMiner as BaseCreature).ConstantFocus here, but it crashed the golden client
                // But not ClassicUO. It doesn't crash right away, but once the NPCs start talking..
                // No server issue, it's purly client.. likel some sort of packet overflow that ClassicUO handles.
                //  What I do now, is have OnSee() in the AngryMiner class, and that sets the ConstantFocus at likely a time more
                //  compatable with the Golden Client.
                // Testing notes: Even two or three Angry Miners were crashing the GC, dozens were fine with CUO.
                //      After moving ConstantFocus to OnSee() in AngryMiner, GC can now handle dozens of Angry Miners all yelping at the same time.
                (angryMiner as BaseCreature).Home = miner.Location;
                (angryMiner as BaseCreature).Combatant = miner;
            }
        }
    }

    #endregion ReceiveMessage

    #region StableCharge (updated to once per day.)
    class CStableCharge
    {
        public void StableCharge()
        {
            System.Console.WriteLine("Stable charge check started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int playersChecked = StableChargeWorker();
            tc.End();
            System.Console.WriteLine("checked {0} players in {1}", playersChecked, tc.TimeTaken);
        }

        private int StableChargeWorker()
        {
            int iChecked = 0;

            ArrayList restock = new ArrayList();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerMobile)
                {
                    iChecked++;
                    PlayerMobile pm = m as PlayerMobile;
                    if (AnimalTrainer.Table.ContainsKey(pm))
                        if (AnimalTrainer.Table[pm] != null && AnimalTrainer.Table[pm].Count > 0)
                            foreach (object ox in AnimalTrainer.Table[pm])
                                Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(StableChargeWorkerTick), new object[] { ox, pm });
                }
            }

            return iChecked;
        }

        private void StableChargeWorkerTick(object state)
        {
            object[] aState = (object[])state;
            object om = aState[0];
            PlayerMobile pm = aState[1] as PlayerMobile;
            if (om is BaseCreature ox)
            {
                if (AnimalTrainer.Table.ContainsKey(pm))
                    if (AnimalTrainer.Table[pm].Contains(ox))
                    {
                        BaseCreature bc = ox as BaseCreature;
                        if (DateTime.UtcNow > bc.LastStableChargeTime + TimeSpan.FromMinutes(Clock.MinutesPerUODay))
                        {
                            // reset the clock
                            bc.LastStableChargeTime = DateTime.UtcNow;

                            if (bc.GetCreatureBool(CreatureBoolTable.StableHold) == true)
                            {   // add this cost to the total fees
                                bc.StableBackFees += AnimalTrainer.UODayChargePerPet(pm, AnimalTrainer.Table[pm].IndexOf(ox));
                                LogHelper Logger = new LogHelper("PetHoldFromStables.log", false, true);
                                Logger.Log(string.Format("{0}'s pet {1} has back stable charges totaling {2}.", pm, bc, bc.StableBackFees));
                                Logger.Finish();
                            }
                            else
                            {   // try to charge the player for normal fees
                                if ((pm.BankBox != null && pm.BankBox.ConsumeTotal(typeof(Gold), AnimalTrainer.UODayChargePerPet(pm, AnimalTrainer.Table[pm].IndexOf(ox)))) == false)
                                {   // The player was unable to pay, he will now need to pay back fees to get this pet back
                                    bc.SetCreatureBool(CreatureBoolTable.StableHold, true);
                                    bc.StableBackFees = AnimalTrainer.UODayChargePerPet(pm, AnimalTrainer.Table[pm].IndexOf(ox));
                                    LogHelper Logger = new LogHelper("PetHoldFromStables.log", false, true);
                                    Logger.Log(string.Format("{0}'s pet {1} is now accumulating back stable charges totaling {2}.", pm, bc, bc.StableBackFees));
                                    Logger.Finish();
                                }
                                else
                                {   // okay, player charged for the pet
                                    if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.LogStableCharges) == true)
                                    {
                                        LogHelper Logger = new LogHelper("StableCharge.log", false, true);
                                        Logger.Log(string.Format("{0} paid {1} gold in stable charges for pet {2}.",
                                            pm, AnimalTrainer.UODayChargePerPet(pm, AnimalTrainer.Table[pm].IndexOf(ox)), bc));
                                        Logger.Finish();
                                    }
                                }
                            }
                        }
                    }
            }
        }
    }
    #endregion StableCharge

    #region WipeShard (LoginServer)
    // Warning: WIPE'S SHARD
    //  This job is run at 12am each night on the LoginServer.
    //  The loginserver has no need for items or mobiles, and doesn't need cron jobs to manage them.
    //  We do this here to save the developer from having to hand wipe a shard before use.
    class CWipeShard
    {
        public void WipeShard()
        {
            if (!Core.RuleSets.LoginServerRules())
            {
                System.Console.WriteLine("This command only works for the LoginServer");
                return;
            }

            System.Console.WriteLine("WipeShard started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int elementsDeleted = WipeShardWorker();
            tc.End();
            System.Console.WriteLine("shard wiped of {0} elements in {1}", elementsDeleted, tc.TimeTaken);

            try
            {   // we do one save after the wipe so we don't keep loading the same fat world
                Server.Misc.AutoSave.Save();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
        }

        public int WipeShardWorker()
        {
            int count = 0;
            int total_count = 0;
            try
            {
                Console.WriteLine("Begin Wipe.");

                // delete accounts 
                //Console.WriteLine("Deleting accounts...");
                //count = DeleteAccounts();
                //Console.WriteLine("{0} Accounts deleted.", count);
                //total_count += count;

                // delete all items
                Console.WriteLine("Deleting items...");
                count = DeleteItems();
                Console.WriteLine("{0} Items deleted.", count);
                total_count += count;

                // delete all mobiles
                Console.WriteLine("Deleting mobiles...");
                count = DeleteMobiles();
                Console.WriteLine("{0} Mobiles deleted.", count);
                total_count += count;

                Console.WriteLine("Wipe Complete.");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("** Wipe Failed **");
            }

            return total_count;
        }
        private static int DeleteItems()
        {
            Mobile Owner = World.GetAdminAcct();
            int total_items = 0;
            List<Item> items = new List<Item>();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted == true)
                    continue;

                if (Owner != null && item.RootParent == Owner)
                    continue;

                total_items++;
                items.Add(item);
            }

            foreach (Item item in items)
            {
                if (item != null && item.Deleted == false && World.FindItem(item.Serial) != null)
                    item.Delete();
            }

            return total_items;
        }

        private static int DeleteMobiles()
        {
            Mobile Owner = World.GetAdminAcct();
            int total_mobiles = 0;
            List<Mobile> mobiles = new List<Mobile>();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted == true)
                    continue;

                if (Owner != null && mobile == Owner)
                    continue;

                total_mobiles++;
                mobiles.Add(mobile);
            }

            foreach (Mobile mobile in mobiles)
            {
                mobile.Delete();
            }

            return total_mobiles;
        }

        private static int DeleteAccounts()
        {
            List<Account> clients = new List<Account>();
            int total_clients = 0;
            foreach (Account check in Accounts.Table.Values)
            {
                if (check == null)
                    continue;
                total_clients++;
                clients.Add(check);
            }

            foreach (Account check in clients)
            {
                check.Delete();
            }

            return total_clients;
        }
    }
    #endregion WipeShard

    #region VendorRestock (updated)
    // restock the vendors
    class CVendorRestock
    {
        public void VendorRestock()
        {
            System.Console.WriteLine("Vendor restock check started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int vendorsChecked = VendorRestockWorker();
            tc.End();
            System.Console.WriteLine("checked {0} vendors in {1}", vendorsChecked, tc.TimeTaken);
        }

        private int VendorRestockWorker()
        {
            int iChecked = 0;

            ArrayList restock = new ArrayList();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is IVendor)
                {
                    iChecked++;

                    if (((IVendor)m).LastRestock + ((IVendor)m).RestockDelay < DateTime.UtcNow)
                    {
                        restock.Add(m);
                    }
                }
            }
            Console.WriteLine("Restocking {0} vendors", restock.Count);
            for (int i = 0; i < restock.Count; i++)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { restock[i], null });
            }

            return iChecked;
        }

        private void Tick(object state)
        {
            object[] aState = (object[])state;
            IVendor vend = (IVendor)aState[0];
            vend.Restock();
            vend.LastRestock = DateTime.UtcNow;
        }
    }
    #endregion VendorRestock

    #region ItemDecay (updated)
    // decay items on the ground
    class CItemDecay
    {
        public void ItemDecay()
        {
            System.Console.WriteLine("Item decay started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int ItemsDeleted = 0;
            int ItemsChecked = ItemDecayWorker(out ItemsDeleted);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} Items Deleted", ItemsChecked, tc.TimeTaken, ItemsDeleted);
        }

        private bool ItemDecayRule(Item item)
        {
            bool rules =
                item.CanDelete() ||                                 // a temporary item that has aged out (no other rules apply)
                   item.Decays                                      // item decays (Movable && Visible)
                && item.Parent == null                              // not on a player / not in a container
                && !item.GetItemBool(Item.ItemBoolTable.OnSpawner)  // not on a spawner See Also: item creation and defrag in Spawner
                && !Utility.LinkedTo(item)                          // linked to from one of our controllers
                && item.Map != Map.Internal;                        // things on the internal map are dealt with elsewhere

            return rules && (item.LastMoved + item.DecayTime) <= DateTime.UtcNow;
        }
        private void LogItemInHouseDecay(LogHelper logger, Item item)
        {
            BaseHouse bh = null;
            if (logger != null && item != null && ((bh = BaseHouse.Find(item.GetWorldLocation(), item.Map)) != null))
            {
                logger.Log(string.Format("{0}:{1}", item, item.SafeName));
                if (item is Container cont)
                {
                    ArrayList list = cont.GetDeepItems();
                    if (list != null && list.Count > 0)
                    {
                        logger.Log("-- child items --");
                        foreach (object o in list)
                            if (o is Item ix)
                                logger.Log(string.Format("{0}:{1}", ix, ix.SafeName));
                        logger.Log("-- end child items --");
                    }
                }
            }
        }
        private int ItemDecayWorker(out int ItemsDeleted)
        {
            int iChecked = 0;
            ItemsDeleted = 0;
            LogHelper logger = new LogHelper("items in house decay.log", overwrite: false, sline: true);
            try
            {
                ArrayList decaying = new ArrayList();

                // tag items to decay
                foreach (Item item in Server.World.Items.Values)
                {
                    iChecked++;

                    if (ItemDecayRule(item))
                    {
                        LogItemInHouseDecay(logger, item);
                        decaying.Add(item);
                    }
                }

                // decay tagged items
                for (int i = 0; i < decaying.Count; ++i)
                {
                    Item item = (Item)decaying[i];

                    if (item.OnDecay())
                    {
                        if (CoreAI.DebugItemDecayOutput)
                        {
                            System.Console.WriteLine("Decaying {0} at {1} in {2}", item.ToString(), item.X + " " + item.Y + " " + item.Z, item.Map);
                        }
                        // schedule the delete
                        Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(ItemDecayWorkerTick), item);
                        ItemsDeleted++;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Item Decay Heartbeat code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            finally
            {
                if (logger != null)
                    logger.Finish();
            }
            return iChecked;
        }

        private void ItemDecayWorkerTick(object state)
        {
            Item item = state as Item;
            if (item != null && item.Deleted == false)
                item.Delete();
        }
    }
    #endregion ItemDecay (updatd)

    #region AccountCleanup (updated)
    // remove old/unused accounts
    class CAccountCleanup
    {
        public void AccountCleanup()
        {
            System.Console.Write("Account Cleanup started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int AcctsDeleted = 0;
            int AcctsChecked = AccountCleanupWorker(out AcctsDeleted);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} Accounts Deleted", AcctsChecked, tc.TimeTaken, AcctsDeleted);
        }

        private int AccountCleanupRule(Account acct)
        {
            if (AccountCleanupEnabled() == false)
                return 0;

            bool empty = true;  // any characters on the account?
            bool staff = false; // is it a staff account?

            for (int i = 0; i < 5; ++i)
            {
                if (acct[i] != null)
                    empty = false;  // not empty

                if (acct[i] != null && acct[i].AccessLevel > AccessLevel.Player)
                    staff = true;   // a staff
            }

            // RULE1;
            // if empty AND > 7 days old, AND never logged in
            if (empty)
            {
                // never logged in
                if (acct.LastLogin == acct.Created)
                {
                    // 7 days old
                    TimeSpan delta = DateTime.UtcNow - acct.LastLogin;
                    if (delta.TotalDays > 7)
                    {
                        return 1;
                    }
                }
            }

            // RULE2;
            // if empty AND inactive for 30 days (they have logged in at some point)
            if (empty)
            {
                TimeSpan delta = DateTime.UtcNow - acct.LastLogin;
                if (delta.TotalDays > 30)
                {
                    return 2;
                }
            }


            // RULE3 WARNING ONLY;
            // trim all non-staff accounts inactive for 360 days
            if (staff == false)
            {
                TimeSpan delta = DateTime.UtcNow - acct.LastLogin;
                if (delta.TotalDays == 360)
                {   // account will be deleted in about 5 days
                    EmailCleanupWarning(acct);
                }
            }


            // RULE3;
            // trim all non-staff accounts inactive for 365 days
            if (staff == false)
            {
                TimeSpan delta = DateTime.UtcNow - acct.LastLogin;
                if (delta.TotalDays > 365)
                {
                    return 3;
                }
            }

            // RULE4 - TestCenter only
            // trim all non-staff account and not logged in for 30 days
            if (TestCenter.Enabled == true && staff == false)
            {
                TimeSpan delta = DateTime.UtcNow - acct.LastLogin;
                if (delta.TotalDays > CoreAI.TCAcctCleanupDays)
                {
                    return 4;
                }
            }
            /*
									// RULE5
									// only one character, less than 5 minutes game play time, older than 30 days
									if (!empty && !staff && acct.Count == 1)
									{
											int place = 0; //hold place of character on account

											for (int i = 0; i < acct.Length; i++ )
											{
													if (acct[i] != null)
													{
															place = i;
															break;
													}
											}

											TimeSpan delta = DateTime.UtcNow - acct.LastLogin;
											TimeSpan gamePlayDelta = acct.LastLogin - acct[place].CreationTime;

											if (delta.TotalDays > 30 && gamePlayDelta.TotalMinutes < 5)
											{
													return 5;
											}
									}
			*/

            // RULE6;
            // trim all non-staff accounts now - shard wipe
            if (staff == false)
            {
                if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.PlayerAccountWipe))
                {   // delete it
                    return 6;
                }
            }

            // this account should not be deleted
            return 0;
        }

        private void EmailCleanupWarning(Account a)
        {
            try
            {   // only on the production shard
                if (TestCenter.Enabled == false)
                    if (a.EmailAddress != null && SmtpDirect.CheckEmailAddy(a.EmailAddress, false) == true)
                    {
                        string subject = "Angel Island: Your account will be deleted in about 5 days";
                        string body = string.Format("\nThis message is to inform you that your account '{0}' will be deleted on {1} if it remains unused.\n\nIf you decide not to return to Angel Island, we would like wish you well and thank you for playing our shard.\n\nBest Regards,\n  The Angel Island Team\n\n", a.ToString(), DateTime.UtcNow + TimeSpan.FromDays(5));
                        Emailer mail = new Emailer();
                        mail.SendEmail(a.EmailAddress, subject, body, false);
                    }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private bool AccountCleanupEnabled()
        {
            if (CoreAI.TCAcctCleanupEnable == false && TestCenter.Enabled == true)
            {
                System.Console.WriteLine("while CoreAI.TCAcctCleanupEnable is false, accounts will not be cleaned up on Test Center.");
                return false;
            }
            else if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeHouseDecay))
            {   // this account should not be deleted because house decay is frozen, accounts must remain!
                // (when an account is deleted, the associated houses are deleted.)
                System.Console.WriteLine("while global house decay is suspended, accounts will not be cleaned up.");
                return false;
            }
            else if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeAccountDecay))
            {   // this account should not be deleted because account decay is frozen
                System.Console.WriteLine("while global account decay is suspended, accounts will not be cleaned up.");
                return false;
            }

            return true;
        }
        private int AccountCleanupWorker(out int AcctsDeleted)
        {
            int iChecked = 0;
            AcctsDeleted = 0;

            if (AccountCleanupEnabled() == false)
                return 0;

            try
            {
                ArrayList results = new ArrayList();

                foreach (Account acct in Accounts.Table.Values)
                {
                    iChecked++;
                    if (AccountCleanupRule(acct) != 0)
                    {
                        results.Add(acct);
                    }
                }

                if (results.Count > 0)
                {
                    for (int i = 0; i < results.Count; i++)
                    {
                        AcctsDeleted++;
                        Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { results[i], null });
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Account Cleanup code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            return iChecked;
        }

        private void Tick(object state)
        {
            try
            {
                object[] aState = (object[])state;
                LogHelper Logger = new LogHelper("accountDeletion.log", false);

                Account acct = (Account)aState[0];

                // log it
                string temp = string.Format("Rule:{3}, Username:{0}, Created:{1}, Last Login:{4}, Email:{2}",
                    acct.Username,
                    acct.Created,
                    acct.EmailAddress,
                    AccountCleanupRule(acct),
                    acct.LastLogin);
                Logger.Log(LogType.Text, temp);

                // delete it!
                acct.Delete();

                Logger.Finish();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Account Cleanup code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion AccountCleanup

    #region NPCWork (obsolete)
    // see if any NPCs need to work (recall to spawner)
    // turned off to save cpu. All mobiles that need to work are now setting their own timer.
    class CNPCWork
    {
        public void NPCWork()
        {
            System.Console.Write("Npc work started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int npcsworked = 0;
            int mobileschecked = NPCWorkWorker(out npcsworked);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} Npcs Worked", mobileschecked, tc.TimeTaken, npcsworked);
        }

        private int NPCWorkWorker(out int mobsworked)
        {
            int iChecked = 0;
            mobsworked = 0;
            try
            {
                ArrayList ToDoWork = new ArrayList();
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is BaseCreature)
                    {
                        iChecked++;
                        BaseCreature bc = (BaseCreature)m;

                        if (!bc.Controlled && !bc.IOBFollower && !bc.Blessed && !bc.IsAnyStabled && !bc.IsHumanInTown())
                        {
                            // no nonger in the base class. Each creature is responsible for their own CheckWork()
                            /*if (bc.CheckWork())
							{
								ToDoWork.Add(bc);
								//System.Console.WriteLine("adding npc for work");
							}*/
                        }
                    }

                }

                if (ToDoWork.Count > 0)
                {
                    for (int i = 0; i < ToDoWork.Count; i++)
                    {
                        mobsworked++;
                        System.Console.Write("{0}, ", ((BaseCreature)ToDoWork[i]).Location);

                        ((BaseCreature)ToDoWork[i]).OnThink();
                        ((BaseCreature)ToDoWork[i]).AIObject.Think();
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Npc Work code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }
    }
    #endregion NPCWork

    #region InmateManagement (updated)
    // see if any NPCs need to work (recall to spawner)
    class CInmateManagement
    {
        public void InmateManagement()
        {
            System.Console.Write("Inmate check started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int InmatesTouched = 0;
            int mobileschecked = InmateManagementWorker(out InmatesTouched);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} inmates touched", mobileschecked, tc.TimeTaken, InmatesTouched);
        }

        private int InmateManagementWorker(out int InmatesTouched)
        {
            int iChecked = 0;
            InmatesTouched = 0;
            try
            {
                List<NetState> netStates = NetState.Instances;
                for (int i = 0; i < netStates.Count; ++i)
                {
                    NetState compState = netStates[i];
                    if (compState.Mobile != null && compState.Mobile.Account != null)
                    {
                        iChecked++;

                        // leave staff alone
                        // 6/22/2021, adam: Commenting thisout since staff (me) uses regular characters to test the system.
                        //Server.Accounting.Account acct = compState.Mobile.Account as Server.Accounting.Account;
                        //if (acct.GetAccessLevel() > AccessLevel.Player)
                        //continue;

                        // leave staff alone
                        PlayerMobile pm = compState.Mobile as PlayerMobile;
                        if (pm == null || pm.AccessLevel != AccessLevel.Player)
                            continue;

                        Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { pm, null });
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Inmate Management code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }

        private void Tick(object state)
        {
            object[] aState = (object[])state;
            PlayerMobile pm = aState[0] as PlayerMobile;
            try
            {
                // look for inmates
                if (pm.PrisonInmate == true)
                {   // if they are already in jail, that's good enough
                    if (pm.Region.Name == "Jail")
                        return;

                    // if they are on Angel Island, make sure they have a reason to be there
                    if (pm.Region != null && pm.Region.IsAngelIslandRules)
                    {
                        if (pm.ShortTermCriminalCounts == 0 && pm.ShortTermMurders == 0 && DateTime.UtcNow > pm.MinimumSentence)
                        {   // kick them from the island
                            AIParoleExit pe = new AIParoleExit();
                            pe.DoTeleport(pm);
                            pe.Delete();

                            // tell them what just happened
                            pm.SendMessage("The Angel Island Prison staff no longer wish to feed you, you must leave now.");

                            // tell staff
                            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} had no counts, so was kicked from prison.", pm));

                            // log the fact
                            LogHelper Logger = new LogHelper("InmateManagement.log", false, true);
                            Logger.Log(LogType.Mobile, pm, "Kicked from Angel Island Prison because they had no STMs or Criminal Counts.");
                            Logger.Finish();
                        }

                    }
                    else
                    {   // you cannot be out in the world with your inmate flag set
                        // the system is automatically jailing this player.
                        Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(pm, 3, "Caught in the world with inmate flag == true.", false);
                        jt.GoToJail();

                        // tell staff
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. (inmate flag set, but player not in prison.)", pm));

                        // log the fact
                        LogHelper Logger = new LogHelper("InmateManagement.log", false, true);
                        Logger.Log(LogType.Mobile, pm, "Caught in the world with inmate flag == true.");
                        Logger.Finish();
                    }
                }
                else
                {   // visitors can hang out here
                    if (pm.Region != null && pm.Region.IsAngelIslandRules && !pm.PrisonVisitor)
                    {
                        // you cannot be on Angel Island without your inmate flag set
                        // the system is automatically jailing this player.
                        Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(pm, 3, "Caught on Angel Island without inmate flag == true.", false);
                        jt.GoToJail();

                        // tell staff
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. (inmate flag not set, but player in prison.)", pm));

                        // log the fact
                        LogHelper Logger = new LogHelper("InmateManagement.log", false, true);
                        Logger.Log(LogType.Mobile, pm, "Caught on Angel Island without inmate flag == true.");
                        Logger.Finish();
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Inmate Management code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion InmateManagement

    #region TownCrier (ok)
    // process global town crier mewssages
    class CTownCrier
    {
        public void TownCrier()
        {
            System.Console.Write("Processing global Town Crier messages ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int CrierMessages = 0;
            int CriersChecked = TownCrierWorker(out CrierMessages);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} messages", CriersChecked, tc.TimeTaken, CrierMessages);
        }

        private int TownCrierWorker(out int CrierMessages)
        {
            int CriersChecked = 0;
            CrierMessages = 0;

            try
            {
                CrierMessages = TCCS.TheList.Count;
                if (CrierMessages > 0)
                {
                    //jumpstart the town criers if they're not already going.
                    ArrayList instances = Server.Mobiles.TownCrier.Instances;
                    CriersChecked = instances.Count;
                    for (int i = 0; i < instances.Count; ++i)
                        ((Server.Mobiles.TownCrier)instances[i]).ForceBeginAutoShout();
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Town Crier code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return CriersChecked;
        }
    }
    #endregion TownCrier

    #region MusicConfigCleanup (updated)
    // internal map mobile cleanup
    class CMusicConfigCleanup
    {
        public void MusicConfigCleanup()
        {
#if DEBUG
            System.Console.WriteLine("Music Config cleanup started ... ");
#else
            System.Console.Write("Music Config cleanup started ... ");
#endif

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int recordsDeleted = 0;
            int recordsChecked = MusicConfigCleanupWorker(out recordsDeleted);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} deleted", recordsChecked, tc.TimeTaken, recordsDeleted);
        }

        private int MusicConfigCleanupWorker(out int recordsDeleted)
        {
            int iChecked = 0;
            recordsDeleted = 0;
            try
            {
                List<Mobile> list = new List<Mobile>();
                foreach (var mco in SystemMusicPlayer.MusicConfig)
                {
                    iChecked++;
                    if (mco.Key.Deleted)
                        list.Add(mco.Key);
                }

                foreach (var m in list)
                {
                    recordsDeleted++;
                    SystemMusicPlayer.MusicConfig.Remove(m);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in MusicConfigCleanup code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }
        private bool AgedOut(IEntity o)
        {
            if (o is Mobile mobile)
            {   // Give a little slop time to delete
                return DateTime.UtcNow > mobile.LastMoveTime;
            }
            // default to delete
            return true;
        }
        private void Tick(object state)
        {
            object[] aState = (object[])state;
            if (aState[0] != null && aState[0] is Mobile && (aState[0] as Mobile).Deleted == false && (aState[0] as Mobile).ToDelete == true)
            {
                Mobile from = aState[0] as Mobile;
                from.Delete();
            }
        }
    }
    #endregion MusicConfigCleanup

    #region IntMapMobileCleanup (updated)
    // internal map mobile cleanup
    class CIntMapMobileCleanup
    {
        public void IntMapMobileCleanup()
        {
#if DEBUG
            System.Console.WriteLine("Internal MOB cleanup started ... ");
#else
            System.Console.Write("Internal MOB cleanup started ... ");
#endif

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int mobilesexpired = 0;
            int mobileschecked = IntMapMobileCleanupWorker(out mobilesexpired);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} expired", mobileschecked, tc.TimeTaken, mobilesexpired);
        }

        private int IntMapMobileCleanupWorker(out int mobsdeleted)
        {
            int iChecked = 0;
            mobsdeleted = 0;
            try
            {
                // first time if we see something we think we should delete, we simply mark it 
                //	as something to delete. If we see it again and we still think we should
                //	delete it, we delete it, otherwise, clear the mark.
                ArrayList list = new ArrayList();
                foreach (Mobile mob in World.Mobiles.Values)
                {
                    if (mob == null) continue;
                    if (mob.Map == Map.Internal)
                    {
                        iChecked++;
                        if (
                            mob.CanDelete() ||          // a temporary mobile that has aged out (no other rules apply) 
                               !mob.Deleted             // duh
                            && !mob.SpawnerTempMob      // spawner template Mobile no deleting!
                            && !mob.IsIntMapStorage     // int storage item no deleting!
                            && !mob.IsStaffOwned        // placed by staff,  no deleting!
                            && AgedOut(mob)             // Sanity: make sure now > last moved
                            )
                        {
                            if (mob.Account == null)
                            {
                                // if rider ignore
                                if (mob is BaseMount)
                                {
                                    BaseMount bm = mob as BaseMount;
                                    if (bm.Rider != null)
                                    {   // don't even think about it
                                        mob.ToDelete = false;
                                        continue;
                                    }
                                }

                                // if stabled ignore
                                if (mob is BaseCreature)
                                {
                                    BaseCreature bc = mob as BaseCreature;
                                    if (bc.IsAnyStabled == true)
                                    {   // don't even think about it
                                        mob.ToDelete = false;
                                        continue;
                                    }
                                }
#if DEBUG
                                Utility.Monitor.WriteLine("Debug: IntMapMobileCleanup {4}{3} :{0} at {1}{2}",
                                    ConsoleColor.Green,
                                    mob,
                                    mob.Location,
                                    mob.Map != null ? string.Format(": Map: {0}", mob.Map) : "",
                                    mob.Location == Utility.Dogtag.Get("Server.Mobiles.GenericBuyInfo+DisplayCache") ?
                                    " DisplayCache item" : "",
                                    (mob.ToDelete == false) ? "requested to delete" : "deleting");
#endif
                                // if already marked, add to delete list
                                if (mob.ToDelete == true)
                                {
                                    list.Add(mob);
                                }
                                else // mark to delete
                                {
                                    mob.ToDelete = true;
                                }
                            }
                            else
                                mob.ToDelete = false;
                        }
                    }
                }

                // cleanup
                foreach (Mobile m in list)
                {
                    mobsdeleted++;
                    Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { m, null });
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in IntMapMobileCleanup removal code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }
        private bool AgedOut(IEntity o)
        {
            if (o is Mobile mobile)
            {   // Give a little slop time to delete
                return DateTime.UtcNow > mobile.LastMoveTime;
            }
            // default to delete
            return true;
        }
        private void Tick(object state)
        {
            object[] aState = (object[])state;
            if (aState[0] != null && aState[0] is Mobile && (aState[0] as Mobile).Deleted == false && (aState[0] as Mobile).ToDelete == true)
            {
                Mobile from = aState[0] as Mobile;
                from.Delete();
            }
        }
    }
    #endregion IntMapMobileCleanup

    #region PlayerNPCCleanup (updated)
    // delete player owned vendors and barkeeps that are orphaned
    class CPlayerNPCCleanup
    {
        public void PlayerNPCCleanup()
        {
            System.Console.Write("Player NPC cleanup started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int ItemsExpired = 0;
            int ItemsChecked = PlayerNPCCleanupWorker(out ItemsExpired);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} expired", ItemsChecked, tc.TimeTaken, ItemsExpired);
        }

        private int PlayerNPCCleanupWorker(out int NumberDeleted)
        {
            int iChecked = 0;
            NumberDeleted = 0;
            try
            {
                LogHelper Logger = new LogHelper("PlayerNPCCleanup.log", false);

                // first time we see something we think we should delete, we simply mark it 
                //	as something to delete. If we see it again and we still think we should
                //	delete it, we delete it.
                ArrayList list = new ArrayList();
                foreach (Mobile i in World.Mobiles.Values)
                {
                    // only look at Player owned NPCs
                    bool PlayerNPC =
                        i is PlayerBarkeeper ||
                        i is PlayerVendor ||
                        i is RentedVendor ||
                        i is HouseSitter;

                    if (!PlayerNPC) continue;

                    iChecked++;
                    if (i.Map != Map.Internal)      // not internal (internal cleanup done elsewhere)
                    {
                        if (!i.SpawnerTempMob       // spawner template Mobile no deleting!
                            && !InHouse(i)          // not in a house (not with a mouse)
                            && !i.Deleted           // duh
                            && !GmPlaced(i))        // not GM placed
                        {

                            // if already marked, add to delete list
                            if (i.ToDelete == true)
                            {
                                list.Add(i);
                                Logger.Log(LogType.Mobile, i, "(Deleted)");
                            }

                            // mark to delete
                            else
                            {
                                i.ToDelete = true;
                                Logger.Log(LogType.Mobile, i, "(Marked For Deletion)");
                            }
                        }
                        else
                        {
                            if (i.ToDelete == true)
                                Logger.Log(LogType.Mobile, i, "(Unmarked For Deletion)");
                            i.ToDelete = false;
                        }
                    }
                }

                // Cleanup
                foreach (Mobile m in list)
                {
                    NumberDeleted++;
                    Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { m, null });
                }

                Logger.Finish();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in PlayerNPCCleanup removal code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }

        private void Tick(object state)
        {
            object[] aState = (object[])state;
            if (aState[0] != null && aState[0] is Mobile && (aState[0] as Mobile).Deleted == false && (aState[0] as Mobile).ToDelete == true)
            {
                Mobile from = aState[0] as Mobile;
                from.Delete();
            }
        }

        private bool GmPlaced(Mobile m)
        {
            // player vendors, regular and rented: probably never GM Placed
            if (m is PlayerVendor)
            {
                PlayerVendor cx = m as PlayerVendor;
                if (cx.Owner != null && cx.Owner.AccessLevel > AccessLevel.Player)
                    return true;
            }

            // PlayerBarkeeper: Used for work deco and player statues
            if (m is PlayerBarkeeper)
            {
                PlayerBarkeeper cx = m as PlayerBarkeeper;
                if (cx.Owner != null && cx.Owner.AccessLevel > AccessLevel.Player)
                    return true;
            }

            // HouseSitter: Don't know why we would ever have one of these GM placed
            if (m is HouseSitter)
            {
                HouseSitter cx = m as HouseSitter;
                if (cx.Owner != null && cx.Owner.AccessLevel > AccessLevel.Player)
                    return true;
            }

            // Barkeeps on a spawner are protected. like the Warden in prison
            if (m is BaseCreature)
            {
                BaseCreature cx = m as BaseCreature;
                if (cx.Spawner != null)
                    return true;
            }

            // 2/3/24, Yoar: Market stall vendors are protected
            if (m is MarketStallVendor)
                return true;

            return false;
        }

        private bool InHouse(object k)
        {
            Mobile m = k as Mobile;
            Item i = k as Item;

            if (i != null)
                return (Region.Find(i.Location, i.Map) is HouseRegion);
            if (m != null)
                return (Region.Find(m.Location, m.Map) is HouseRegion);
            else
                return false;
        }
    }
    #endregion PlayerNPCCleanup

    #region GuildstoneCleanup (updated)
    // delete orphaned guild stones
    class CGuildstoneCleanup
    {
        public void GuildstoneCleanup()
        {
            System.Console.Write("Guildstone cleanup started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int ItemsExpired = 0;
            int ItemsChecked = GuildstoneCleanupWorker(out ItemsExpired);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} expired", ItemsChecked, tc.TimeTaken, ItemsExpired);
        }

        private int GuildstoneCleanupWorker(out int NumberDeleted)
        {
            int iChecked = 0;
            NumberDeleted = 0;
            try
            {
                LogHelper Logger = new LogHelper("GuildstoneCleanup.log", false);

                // first time we see something we think we should delete, we simply mark it 
                //	as something to delete. If we see it again and we still think we should
                //	delete it, we delete it.
                ArrayList list = new ArrayList();
                foreach (Item i in World.Items.Values)
                {
                    // only look at guildstones
                    if (!(i is Guildstone)) continue;

                    iChecked++;
                    if (i.Map != Map.Internal)      // not internal (internal cleanup done elsewhere)
                    {
                        if (i.Parent == null        // not being carried
                            && !i.SpawnerTempItem   // spawner template item no deleting!
                            && !i.IsIntMapStorage   // int storage item no deleting!
                            && !i.IsStaffOwned      // placed by staff,  no deleting!
                            && !i.Deleted           // duh
                            && !InHouse(i))         // not in a house
                        {

                            // if already marked, add to delete list
                            if (i.ToDelete == true)
                            {
                                list.Add(i);
                                Logger.Log(LogType.Item, i, "(Deleted)");
                            }

                            // mark to delete
                            else
                            {
                                i.ToDelete = true;
                                Logger.Log(LogType.Item, i, "(Marked For Deletion)");
                            }
                        }
                        else
                        {
                            if (i.ToDelete == true)
                                Logger.Log(LogType.Item, i, "(Unmarked For Deletion)");
                            i.ToDelete = false;
                        }
                    }
                }

                // Cleanup
                foreach (Item item in list)
                {
                    NumberDeleted++;
                    Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { item, null });
                }

                Logger.Finish();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in GuildstoneCleanup removal code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }

        private void Tick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is Item && (aState[0] as Item).Deleted == false && (aState[0] as Item).ToDelete == true)
            {
                Item item = (Item)aState[0];
                item.Delete();
            }
        }

        private bool InHouse(object k)
        {
            Mobile m = k as Mobile;
            Item i = k as Item;

            if (i != null)
                return (Region.Find(i.Location, i.Map) is HouseRegion);
            if (m != null)
                return (Region.Find(m.Location, m.Map) is HouseRegion);
            else
                return false;
        }
    }
    #endregion GuildstoneCleanup

    #region StandardCleanup (updated)
    // standard RunUO server startup cleanup code - bankbox, hair, etc.
    class CStandardCleanup
    {
        public void StandardCleanup()
        {
            System.Console.Write("Standard cleanup started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int ItemsExpired = 0;
            int ItemsChecked = StandardCleanupWorker(out ItemsExpired);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} expired", ItemsChecked, tc.TimeTaken, ItemsExpired);
        }

        private int StandardCleanupWorker(out int NumberDeleted)
        {
            int iChecked = 0;
            NumberDeleted = 0;

            try
            {
                ArrayList items = new ArrayList();
                //ArrayList commodities = new ArrayList();

                int boxes = 0;

                foreach (Item item in World.Items.Values)
                {
                    iChecked++;

                    if (item.Deleted == true)
                    {   // ignore
                        continue;
                    }
                    else if (item is BankBox)
                    {
                        BankBox box = (BankBox)item;
                        Mobile owner = box.Owner;

                        if (owner == null)
                        {
                            items.Add(box);
                            ++boxes;
                        }
                        else if (!owner.Player && box.Items.Count == 0)
                        {
                            items.Add(box);
                            ++boxes;
                        }

                        continue;
                    }
                    else if ((item.Layer == Layer.Hair || item.Layer == Layer.FacialHair))
                    {
                        object rootParent = item.RootParent;

                        if (rootParent is Mobile && item.Parent != rootParent && ((Mobile)rootParent).AccessLevel == AccessLevel.Player)
                        {
                            items.Add(item);
                            continue;
                        }
                    }

                    if (item.Parent != null || item.Map != Map.Internal || item.HeldBy != null)
                        continue;

                    if (item.Location != Point3D.Zero)
                        continue;

                    if (!IsBuggable(item))
                        continue;

                    if (item.IsIntMapStorage == true)
                        continue;

                    if (item.IsStaffOwned == true)
                        continue;

                    items.Add(item);
                }

                //for ( int i = 0; i < commodities.Count; ++i )
                //	items.Remove( commodities[i] );

                if (items.Count > 0)
                {
                    NumberDeleted = items.Count;

                    if (boxes > 0)
                        Console.WriteLine("Cleanup: Detected {0} inaccessible items, including {1} bank boxes, removing..", items.Count, boxes);
                    else
                        Console.WriteLine("Cleanup: Detected {0} inaccessible items, removing..", items.Count);

                    for (int i = 0; i < items.Count; ++i)
                        // there is often tens of thousands of items, so we *really* spread it out (1.5, 30.5)
                        Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(1.5, 30.5)), new TimerStateCallback(Tick), new object[] { items[i], null });
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Standard Cleanup removal code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }

        private void Tick(object state)
        {
            object[] aState = (object[])state;
            Item item = aState[0] as Item;
            if (item != null && item.Deleted == false)
                item.Delete();
        }

        private bool IsBuggable(Item item)
        {
            if (item is Fists)
                return false;

            if (item is ICommodity || item is Multis.BaseBoat
                || item is Fish || item is BigFish
                || item is BasePotion || item is Food || item is CookableFood
                || item is SpecialFishingNet || item is BaseMagicFish
                || item is Shoes || item is Sandals
                || item is Boots || item is ThighBoots
                || item is TreasureMap || item is MessageInABottle
                || item is BaseArmor || item is BaseWeapon
                || item is BaseClothing
                || (item is BaseJewel && Core.RuleSets.AOSRules()))
                return true;

            return false;
        }
    }
    #endregion StandardCleanup

    #region StrongboxCleanup (updated)
    // delete orphaned strong boxes
    class CStrongboxCleanup
    {
        public void StrongboxCleanup()
        {
            System.Console.Write("Strongbox cleanup started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int ItemsExpired = 0;
            int ItemsChecked = StrongboxCleanupWorker(out ItemsExpired);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} expired", ItemsChecked, tc.TimeTaken, ItemsExpired);
        }

        private int StrongboxCleanupWorker(out int NumberDeleted)
        {
            int iChecked = 0;
            NumberDeleted = 0;
            try
            {
                LogHelper Logger = new LogHelper("StrongboxCleanup.log", false);

                // first time we see something we think we should delete, we simply mark it 
                //	as something to delete. If we see it again and we still think we should
                //	delete it, we delete it.
                ArrayList list = new ArrayList();
                foreach (Item i in World.Items.Values)
                {
                    // only look at guildstones
                    if (!(i is StrongBox)) continue;
                    StrongBox sb = i as StrongBox;

                    iChecked++;
                    if (i.Map != Map.Internal)          // not internal (internal cleanup done elsewhere)
                    {
                        if (sb.Owner != null                    // it is owned
                            && sb.House != null                 // in a house
                                                        && !sb.Deleted                          // duh
                            && !sb.House.IsCoOwner(sb.Owner))   // yet owner is not a co owner of the house
                        {

                            // if already marked, add to delete list
                            if (i.ToDelete == true)
                            {
                                list.Add(i);
                                Logger.Log(LogType.Item, i, "(Deleted)");
                            }

                            // mark to delete
                            else
                            {
                                i.ToDelete = true;
                                Logger.Log(LogType.Item, i, "(Marked For Deletion)");
                            }
                        }
                        else
                        {
                            if (i.ToDelete == true)
                                Logger.Log(LogType.Item, i, "(Unmarked For Deletion)");
                            i.ToDelete = false;
                        }
                    }
                }

                // Cleanup
                foreach (Item item in list)
                {
                    NumberDeleted++;
                    Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { item, null });
                }

                Logger.Finish();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in StrongboxCleanup removal code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }

        private void Tick(object state)
        {
            object[] aState = (object[])state;
            Item item = aState[0] as Item;

            if (item != null && item.Deleted == false && item.ToDelete == true)
                item.Delete();
        }
    }
    #endregion StrongboxCleanup

    #region IntMapItemCleanup (updated)
    // internal map item cleanup
    class CIntMapItemCleanup
    {
        public void IntMapItemCleanup()
        {
#if DEBUG
            System.Console.WriteLine("Internal Item cleanup started ... ");
#else
            System.Console.Write("Internal Item cleanup started ... ");
#endif
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int ItemsExpired = 0;
            int ItemsChecked = IntMapItemCleanupWorker(out ItemsExpired);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} expired", ItemsChecked, tc.TimeTaken, ItemsExpired);
        }

        private int IntMapItemCleanupWorker(out int ItemsDeleted)
        {
            int iChecked = 0;
            ItemsDeleted = 0;

            try
            {
                // first time we see something we think we should delete, we simply mark it 
                //	as something to delete. If we see it again and we still think we should
                //	delete it, we delete it.
                ArrayList list = new ArrayList();
                foreach (Item i in World.Items.Values)
                {
                    if (i == null) continue;
                    if (i.Map == Map.Internal)
                    {
                        iChecked++;
                        if (i is Guildstone && !i.IsIntMapStorage && !i.IsStaffOwned)
                        {
                            try
                            {
                                string strLog = string.Format(
                                    "Guildstone on internal map not marked as IsIntMapStorage - please investigate: serial: {0}",
                                    i.Serial.ToString());

                                LogHelper.LogException(new Exception("Pixie Needs To Investigate"), strLog);
                            }
                            catch (Exception e)
                            {
                                LogHelper.LogException(e, "Pixie needs to investigate this.");
                            }

                            i.ToDelete = false;
                        }
                        else
                            if (
                                i.CanDelete() ||                // a temporary item that has aged out (no other rules apply)
                                    i.Parent == null            // no parent (like stuff in a container)
                                && !i.SpawnerTempItem           // spawner template item no deleting!
                                && !i.IsIntMapStorage           // int storage item no deleting!
                                && !i.IsStaffOwned              // placed by staff,  no deleting!
                                && !(i is AutomatedEventSystem) // these are items! don't delete them!
                                && !(i is Fists)                // why?
                                && !(i is MountItem)            // When the horse or other mount is deleted, this will be deleted.
                                && !(i is EffectItem)           // why?
                                && !(i is IPersistence)         // one of those things 
                                && !Utility.LinkedTo(i)         // linked to from one of our controllers
                                && AgedOut(i)                   // Sanity: make sure now > last moved
                                && !i.Deleted                   // duh. 
                                && !IsVendorStock(i)            // is this vendor stock?
                            )
                        // why is this commented out?
                        // && !(i is BaseQuest) && !(i is QuestObjective)
                        {

#if DEBUG
                            if (i.GetType().Name.ToLower().Contains("Persistence") || i.GetType().Name.ToLower().Contains("Persistance"))
                                Utility.Monitor.WriteLine("Persistence item {0} not marked as IPersistence", ConsoleColor.Red, i);

                            Utility.Monitor.WriteLine("Debug: IntMapItemCleanup {4}{3} :{0} at {1}{2}",
                                ConsoleColor.Green,
                                i,
                                i.Location,
                                i.Parent != null ? string.Format(": Parent: {0}", i.Parent) : "",
                                i.Location == Utility.Dogtag.Get("Server.Mobiles.GenericBuyInfo+DisplayCache") ?
                                " DisplayCache item" : "",
                                (i.ToDelete == false) ? "requested to delete" : "deleting");
#endif

                            // if already marked, add to delete list
                            if (i.ToDelete == true)
                            {
                                list.Add(i);
                            }

                            // mark to delete
                            else
                            {
                                i.ToDelete = true;
                            }
                        }
                        else
                        {
                            i.ToDelete = false;
                        }
                    }
                }

                // Cleanup
                foreach (Item item in list)
                {
                    ItemsDeleted++;
                    Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { item, null });
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in IntMapItemCleanup removal code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return iChecked;
        }
        private bool AgedOut(IEntity o)
        {
            if (o is Item item)
            {   // Give a little slop time to delete
                return DateTime.UtcNow > item.LastMoved;
            }
            // default to delete
            return true;
        }
        private void Tick(object state)
        {
            object[] aState = (object[])state;
            Item item = aState[0] as Item;
            if (item != null && item.Deleted == false && item.ToDelete == true)
                item.Delete();
        }
        private bool IsVendorStock(Item i)
        {
            return i.GetType().DeclaringType == typeof(GenericBuyInfo);
        }
    }
    #endregion IntMapItemCleanup

    #region FindSkill (updated)
    // find and log players using certain skills that are often robot'ed
    class CFindSkill
    {
        public void FindSkill()
        {
            System.Console.Write("FindSkill routine started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int ResourceGatherers = FindSkillWorker();
            tc.End();
            System.Console.WriteLine("checked in {0} - {1} player(s) matched", tc.TimeTaken, ResourceGatherers);
        }

        private int FindSkillWorker()
        {
            int elapsed = 2;  // Default
            SkillName? LastSkill = null;// Server.SkillName.Null;
            DateTime LastTime = new DateTime();
            List<NetState> MobStates = NetState.Instances;
            int MobMatches = 0;

            // Loop through active connections' mobiles and check conditions
            for (int i = 0; i < MobStates.Count; ++i)
            {
                Mobile m = MobStates[i].Mobile;

                // If m defined & PlayerMobile, get involved (not explicit)
                if (m != null)
                {
                    if (m is PlayerMobile)
                    {
                        PlayerMobile pm = (PlayerMobile)m;
                        LastSkill = pm.LastSkillUsed;
                        LastTime = pm.LastSkillTime;

                        // Check time & skill, display/log if match
                        if ((LastSkill == SkillName.Lumberjacking || LastSkill == SkillName.Mining) && DateTime.UtcNow <= (LastTime + TimeSpan.FromSeconds(elapsed * 60)))
                        {
                            MobMatches++;
                            Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { pm, LastSkill });
                        }
                    }
                }
            }

            return MobMatches;
        }


        private void Tick(object state)
        {
            object[] aState = (object[])state;
            PlayerMobile pm = aState[0] as PlayerMobile;
            SkillName? LastSkill = (aState[1] != null) ? (SkillName)aState[1] : null; // SkillName.Null;

            if (pm != null && LastSkill != null /*SkillName.Null*/)
            {
                LogHelper Logger = new LogHelper("findskill.log", false, true);
                Logger.Log(LogType.Mobile, pm, LastSkill.ToString());
                Logger.Finish();
            }
        }
    }
    #endregion FindSkill

    #region MobileLifespanCleanup (updated)
    // cleanup mobiled that have outlived their useful lifespan (rotating population)
    class CMobileLifespanCleanup
    {
        public void MobileLifespanCleanup()
        {   // it's fast now, no need to Broadcast this
            //Server.World.Broadcast(0x35, true, "Performing routine maintenance, please wait.");
            System.Console.Write("MOB lifespan cleanup started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int mobilesexpired = 0;
            //CPUProfiler.Start();							// ** PROFILER START ** 
            int mobileschecked = MobileLifespanCleanup(out mobilesexpired);
            tc.End();
            System.Console.WriteLine("checked {0} in {1} - {2} expired", mobileschecked, tc.TimeTaken, mobilesexpired);
            //CPUProfiler.StopAndSaveSnapShot();				// ** PROFILER STOP ** 
            //Server.World.Broadcast(0x35, true, "Routine maintenance complete. The entire process took {0}.", tc.TimeTaken);
        }

        private int MobileLifespanCleanup(out int mobsdeleted)
        {
            int result = 0;
            result = MobileLifespanCleanupWorker(out mobsdeleted, 30.00); // keep deleting for 30 seconds max
            return result;

        }
        private bool Exhibit(BaseCreature bc)
        {
            if (bc != null && bc.Spawner != null && !bc.Spawner.Deleted && bc.Spawner.Exhibit)
                return true;

            return false;
        }

        private int MobileLifespanCleanupWorker(out int mobsdeleted, double timeout)
        {
            int iChecked = 0;
            mobsdeleted = 0;
            try
            {
                ArrayList ToDelete = new ArrayList();
                foreach (Mobile m in World.Mobiles.Values)
                {
                    // mobiles on the internal map are handled elsewhere (includes stabled)
                    // Note: PlayerVendors are not BaseCreature, and are handled elsewhere
                    if (m is BaseCreature bc && bc.Map != Map.Internal && !bc.Deleted)
                    {
                        iChecked++;

                        // process exclusions here
                        if (
                            bc.CanDelete() ||           // a temporary mobile that has aged out (no other rules apply) 
                               !bc.Controlled           // summons, pets, IOB followers, hire fighters
                            && !bc.IsStaffOwned         // placed by staff,  no deleting!
                            && !bc.SpawnerTempMob       // spawner template mobs, no deleting
                            && !(bc is BaseVendor)      // house sitters, barkeeps, auctioneers (Idea: exclude if Admin owned or InHouseRegion only. Others could be deleted.)
                            && !bc.IOBFollower          // probably handled by !bc.Controled
                            && !bc.Blessed              // Zoo animals etc.
                            && !Exhibit(bc)             // special exhibit mobiles
                            && !Utility.LinkedTo(bc)    // linked to from one of our controllers
                            && !bc.GetCreatureBool(CreatureBoolTable.IsTownshipLivestock)
                            && !(bc is ITownshipNPC)    // township NPC
                            )
                        {
                            // if it's time to die, and if he's not been attacked recently...
                            if (bc.IsPassedLifespan() && bc.Hits == bc.HitsMax || bc.CanDelete())
                            {
                                bool bSkip = false;

                                // if the creature has not thought in the last little while, then he is said to be Hibernating
                                //	 if he is Hibernating, there are no PlayerMobiles in the area. (cool to delete)
                                if (bc.Hibernating == false)
                                    bSkip = true;

                                if (!bSkip)
                                    ToDelete.Add(bc);
                            }
                        }
                    }
                }

                if (ToDelete.Count > 0)
                {
                    // ToDelete.Sort(new AgeCheck());
                    Utility.TimeCheck tc = new Utility.TimeCheck();
                    tc.Start();
                    for (int i = 0; i < ToDelete.Count; i++)
                    {
                        mobsdeleted++;
                        Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(MobileLifespanCleanupWorkerTick), new object[] { ToDelete[i], null });

                        // Adam: do not spend too much time here
                        //	this is no longer an issue as we fixed the very slow region.InternalExit and sector.OnLeave
                        if (tc.Elapsed() > timeout)
                            break;
                    }
                    tc.End();
                    if (ToDelete.Count - mobsdeleted > 0)
                        System.Console.WriteLine("Ran out of time, skipping {0} mobiles", ToDelete.Count - mobsdeleted);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in MOBCleanup removal code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            finally
            {
            }
            return iChecked;
        }
        private void MobileLifespanCleanupWorkerTick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is BaseCreature && (aState[0] as BaseCreature).Deleted == false)
            {
                BaseCreature from = (BaseCreature)aState[0];
                from.Delete();
            }
        }

        /*
		public void MobileLifespanCleanupExtern(double timeout)
		{
				System.Console.Write("MOB lifespan cleanup started ... ");
				Utility.TimeCheck tc = new Utility.TimeCheck();
				tc.Start();
				int mobilesexpired = 0;
				int mobileschecked = MobileLifespanCleanupWorker(out mobilesexpired, timeout);
				tc.End();
				System.Console.WriteLine("checked {0} in {1} - {2} expired", mobileschecked, tc.TimeTaken, mobilesexpired);
		}*/
    }
    #endregion MobileLifespanCleanup

    #region KillerTimeCleanup (updated)
    // report murder timeout logic
    class CKillerTimeCleanup
    {
        public void KillerTimeCleanup()
        {
            System.Console.Write("KillerTime cleanup started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int mobileschecked = KillerTimeCleanupWorker();
            tc.End();
            System.Console.WriteLine("checked " + mobileschecked + " in " + tc.TimeTaken);
        }

        private int KillerTimeCleanupWorker()
        {
            int mobileschecked = 0;

            try
            {
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerMobile && (m as PlayerMobile).KillerTimes != null)
                    {
                        mobileschecked++;
                        Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { m, null });
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in KillerTimeCleanup code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return mobileschecked;
        }

        private void Tick(object state)
        {
            object[] aState = (object[])state;
            PlayerMobile pm = (PlayerMobile)aState[0];
            try
            {
                if (pm != null && pm.KillerTimes != null)
                {
                    for (int i = pm.KillerTimes.Count - 1; i >= 0; i--)
                    {
                        if (DateTime.UtcNow - ((ReportMurdererGump.KillerTime)pm.KillerTimes[i]).Time > TimeSpan.FromMinutes(5.0))
                        {
                            pm.KillerTimes.RemoveAt(i);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in KillerTimeCleanup code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion KillerTimeCleanup

    #region HouseDecay (no timers here)
    // decay houses
    class CHouseDecay
    {
        public void HouseDecay()
        {
            System.Console.Write("House Decay started ... ");
            System.Console.Write("\n");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int houseschecked = HouseDecayWorker();
            tc.End();
            System.Console.WriteLine("checked " + houseschecked + " houses in: " + tc.TimeTaken);
        }

        private int HouseDecayWorker()
        {   // we can't use timers on individual house decay without synchronization problems
            //  HouseDecayLast must be updated after the all houses are processed
            //  Cron, by nature, is often late. HouseDecayLast insures that even if we are late, we decay the correct number of minutes.
            int numberchecked = 0;

            try
            {
                // even when a house sitter refreshes a house (one day,) we still to then do normal decay processing
                foreach (Mobile mob in World.Mobiles.Values)
                {
                    Mobiles.HouseSitter hs = mob as Mobiles.HouseSitter;

                    if (hs != null)
                    {
                        DoHouseDecay(new object[] { hs, null });
                    }
                }

                // okay, process decay on all houses
                foreach (ArrayList list in Server.Multis.BaseHouse.Multis.Values)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        Server.Multis.BaseHouse house = list[i] as Server.Multis.BaseHouse;
                        if (house != null)
                        {
                            DoHouseDecay(new object[] { house, null });
                            numberchecked++;
                        }
                    }
                }

                // record the 'last' check - sets HouseDecayLast
                DoHouseDecay(new object[] { null, null });
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in House Decay code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return numberchecked;
        }

        // in case we ever want to dump this info
        private static Dictionary<BaseHouse, string> m_NonDecayingHouses = new Dictionary<BaseHouse, string>();
        private void RecordNonDecayingHouse(BaseHouse house, string text)
        {
            if (m_NonDecayingHouses.ContainsKey(house))
                return;
            m_NonDecayingHouses.Add(house, text);
        }
        private bool CheckNeverDecay(BaseHouse house)
        {
            Point3D loc = house.Location;
            if (house.Sign != null) loc = house.Sign.Location;
            Mobile owner = house.Owner;

            if (house.NeverDecay)
            {   // this is explicitly set on the house sign
                if (owner != null)
                {   // don't list staff owned houses
                    if (owner.AccessLevel == AccessLevel.Player)
                        RecordNonDecayingHouse(house, string.Format("House: (Never Decays) owner: " + owner + " location: " + loc));
                    else
                    {
                        RecordNonDecayingHouse(house, string.Format("House: (Never Decays) staff ownership: " + owner + " location: " + loc));
                        house.TotalRefresh();
                        return true;
                    }
                }
                else
                {
                    RecordNonDecayingHouse(house, string.Format("House: (Never Decays) owner: NULL location: " + loc));
                }
                house.RefreshNonDecayingHouse();
                return true;
            }

            if (house.Sign != null && house.Sign.MembershipOnly == true)
            {   // it's a cooperative, leave it alone
                RecordNonDecayingHouse(house, string.Format("House: (Never Decays) members only cooperative location: " + loc));
                house.TotalRefresh();
                return true;
            }

            if (house.Owner != null && house.Owner.AccessLevel > AccessLevel.Player)
            {   // staff owned, leave it alone
                RecordNonDecayingHouse(house, string.Format("House: (Never Decays) staff ownership: " + owner + " location: " + loc));
                house.TotalRefresh();
                return true;
            }

            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeHouseDecay))
            {   // global decay if turned off
                RecordNonDecayingHouse(house, string.Format("House: (Never Decays) global FreezeHouseDecay enabled"));
                house.RefreshNonDecayingHouse();
                return true;
            }

            return false;
        }

        private void DoHouseDecay(object state)
        {
            object[] aState = (object[])state;
            HouseSitter houseSitter = aState[0] as HouseSitter;
            BaseHouse house = aState[0] as BaseHouse;
            if (houseSitter != null && houseSitter.Deleted == false)
            {
                try
                {
                    Server.Multis.BaseHouse sittersHouse = Server.Multis.BaseHouse.FindHouseAt(houseSitter);
                    // sitterHouse
                    if (sittersHouse != null)
                    {
                        if (sittersHouse.IsFriend(houseSitter.Owner))
                        {
                            if (houseSitter.NumberOfRefreshes < houseSitter.MaxNumberOfRefreshes)
                            {
                                if (!sittersHouse.NeverDecay && ((sittersHouse.StructureDecayTime - DateTime.UtcNow) < TimeSpan.FromDays(houseSitter.RefreshUntilDays)))
                                {
                                    int costperday = sittersHouse.MaxSecures * houseSitter.ChargPerSecure;
                                    Container cont = houseSitter.Owner.BankBox;
                                    if (cont != null)
                                    {
                                        int gold = cont.GetAmount(typeof(Gold), true);

                                        if (cont.ConsumeTotal(typeof(Gold), costperday, true))
                                        {
                                            System.Console.WriteLine("Refreshing house via housesitter {0} at location {1}.", houseSitter.Name, houseSitter.Location);
                                            sittersHouse.RefreshHouseOneDay();
                                            houseSitter.NumberOfRefreshes++;
                                        }
                                        //not enough money in bank
                                        else
                                        {
                                            LogHelper Logger = new LogHelper("houseDecay.log", false);
                                            // log it
                                            string temp = string.Format(
                                                "Owner:{0}, Account:{1}, Name:{2}, Serial:{3}, Location:{4}, BuiltOn:{5}, StructureDecayTime:{6}, Type:{7}: Hasn't enough enough money to pay their house sitter.",
                                                sittersHouse.Owner != null ? sittersHouse.Owner.Name : "null",
                                                sittersHouse.Owner.Account != null ? sittersHouse.Owner.Account.ToString() : "null",
                                                sittersHouse.Sign != null ? sittersHouse.Sign.Name : "NO SIGN",
                                                sittersHouse.Serial,
                                                sittersHouse.Location,
                                                sittersHouse.BuiltOn,
                                                sittersHouse.StructureDecayTime,
                                                sittersHouse.GetType()
                                                );
                                            Logger.Log(LogType.Text, temp);
                                            Logger.Finish();
                                        }
                                    }
                                }
                                //doesn't need refreshing
                                else
                                {
                                }
                            }
                            //reached max refreshes
                            else
                            {
                                LogHelper Logger = new LogHelper("houseDecay.log", false);
                                // log it
                                string temp = string.Format(
                                    "Owner:{0}, Account:{1}, Name:{2}, Serial:{3}, Location:{4}, BuiltOn:{5}, StructureDecayTime:{6}, Type:{7}: Reached max refreshes.",
                                    sittersHouse.Owner != null ? sittersHouse.Owner.Name : "null",
                                    sittersHouse.Owner.Account != null ? sittersHouse.Owner.Account.ToString() : "null",
                                    sittersHouse.Sign != null ? sittersHouse.Sign.Name : "NO SIGN",
                                    sittersHouse.Serial,
                                    sittersHouse.Location,
                                    sittersHouse.BuiltOn,
                                    sittersHouse.StructureDecayTime,
                                    sittersHouse.GetType()
                                    );
                                Logger.Log(LogType.Text, temp);
                                Logger.Finish();
                            }
                        }
                        //not a friend
                        else
                        {
                            LogHelper Logger = new LogHelper("houseDecay.log", false);
                            // log it
                            string temp = string.Format(
                                "Owner:{0}, Account:{1}, Name:{2}, Serial:{3}, Location:{4}, BuiltOn:{5}, StructureDecayTime:{6}, Type:{7}: House sitter's owner is not a 'friend' of the house.",
                                sittersHouse.Owner != null ? sittersHouse.Owner.Name : "null",
                                (sittersHouse.Owner != null && sittersHouse.Owner.Account != null) ? sittersHouse.Owner.Account.ToString() : "null",
                                sittersHouse.Sign != null ? sittersHouse.Sign.Name : "NO SIGN",
                                sittersHouse.Serial,
                                sittersHouse.Location,
                                sittersHouse.BuiltOn,
                                sittersHouse.StructureDecayTime,
                                sittersHouse.GetType()
                                );
                            Logger.Log(LogType.Text, temp);
                            Logger.Finish();
                        }
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    System.Console.WriteLine("Exception Caught in House Decay code (housesitter): " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
            }
            else if (house != null)
            {   // we are checking house decay without a housesitter
                if (CheckNeverDecay(house))
                    return;

                // this does the actual decay processing
                house.CheckDecay();
            }
            else
            {   // Adam: record the 'last' check.
                Server.Multis.BaseHouse.HouseDecayLast = DateTime.UtcNow;
            }
        }
    }
    #endregion HouseDecay

    #region MurderCountDecay (ok)
    // decay murder counts
    class CMurderCountDecay
    {
        public void MurderCountDecay()
        {
            System.Console.Write("Murder Count Decay started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int mobileschecked = MurderCountDecayWorker();
            tc.End();
            System.Console.WriteLine("checked " + mobileschecked + " characters in: " + tc.TimeTaken);
        }

        private int MurderCountDecayWorker()
        {
            int mobileschecked = 0;
            try
            {
                mobileschecked = Server.Mobiles.PlayerMobile.DoGlobalDecayKills();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in MurderCountDecay code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return mobileschecked;
        }
    }
    #endregion MurderCountDecay

    #region PlantGrowth (updated)
    // grow the plants
    class CPlantGrowth
    {
        public void PlantGrowth()
        {
            System.Console.Write("Plant Growth started ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int plantschecked = PlantGrowthWorker();
            tc.End();
            System.Console.WriteLine("checked " + plantschecked + " plants in: " + tc.TimeTaken);
        }

        private int PlantGrowthWorker()
        {
            int plantschecked = 0;
            try
            {
                foreach (Item item in World.Items.Values)
                {
                    Plants.PlantItem plant = item as Plants.PlantItem;

                    if (plant != null && plant.IsGrowable && plant.ValidGrowthLocation)
                    {
                        plantschecked++;
                        Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(Tick), new object[] { plant, null });
                    }
                }

                return plantschecked;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in PlantGrowth code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return plantschecked;
        }

        private void Tick(object state)
        {
            try
            {
                object[] aState = (object[])state;
                Plants.PlantItem plant = aState[0] as Plants.PlantItem;
                if (plant != null && plant.Deleted == false)
                {
                    plant.PlantSystem.NextGrowth = DateTime.UtcNow;
                    plant.PlantSystem.DoGrowthCheck();
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in PlantGrowth code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion PlantGrowth

    #region Track Staff Owned
    // produce an overland mobile
    class CTrackStaffOwned
    {
        public void TrackStaffOwned()
        {
            System.Console.Write("Tracking staff owned items ... ");

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int anomalies = 0;
            int count = TrackStaffOwnedWorker(out anomalies);
            tc.End();
            System.Console.WriteLine("{0} items checked. {1} anomalies found in:{2}", count, anomalies, tc.TimeTaken);
        }

        private int TrackStaffOwnedWorker(out int anomalies)
        {
            anomalies = 0;
            try
            {
                LogHelper logger = new LogHelper("Staff owned item anomalies.log", overwrite: true, sline: true);

                foreach (Item item in World.Items.Values)
                    if (item is Item ix && ix.Deleted == false && ix.IsStaffOwned)
                        if (ix.RootParent is Mobile m && m.AccessLevel == AccessLevel.Player)
                        {
                            anomalies++;
                            logger.Log(string.Format("Item {0}({1}) found on mobile {2}", ix, ix.Serial, m));
                        }
                logger.Finish();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in TrackStaffOwned code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return World.Items.Values.Count;
        }
    }
    #endregion Track Staff Owned
    #region Count Gems
    // produce an overland mobile
    class CCountGems
    {
        public void CountGems()
        {
            System.Console.Write("Counting player max gem stacks ... ");

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int max = 0;
            int count = CountGemsWorker(out max);
            tc.End();
            System.Console.WriteLine("{0} items checked. Max gems of type:{1} in:{2}", count, max, tc.TimeTaken);
        }

        private int CountGemsWorker(out int max)
        {
            max = 0;
            try
            {
                max = OverlandTreasureHunter.CountGems();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in CountGems code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return World.Items.Values.Count;
        }
    }
    #endregion Count Gems

    #region DynamicCampSystem
    // produce an overland mobile
    public class DynamicCamp
    {
        public int Want = 0;
        public Type Type = null;
        public DynamicCamp(int want, Type type)
        {
            Want = want;
            Type = type;
        }
    }
    class CDynamicCampSystem
    {
        public void DynamicCampSystem()
        {
            System.Console.WriteLine("Dynamic camp spawn... ");
            Utility.TimeCheck tcg = new Utility.TimeCheck();
            Dictionary<List<Point3D>, DynamicCamp> jobs = new()
            {
                {CampManager.Mineable,new DynamicCamp(want: 6, type: typeof(AngryMinerCampRare)) },
                {CampManager.NonMineable,new DynamicCamp(want: 6, type: typeof(GolemCamp)) },
            };

            tcg.Start();
            int totalCamps = 0;
            foreach (var kvp in jobs)
            {
                List<Point3D> locations = new();
                bool result = DynamicCampSystemWorker(kvp.Key, kvp.Value.Want, kvp.Value.Type, locations);
                totalCamps += locations.Count;
                if (result == false)
                    System.Console.WriteLine("Did not spawn any {0} Camps", kvp.Value.Type.Name);
                else
                {
                    System.Console.WriteLine("Spawned {0} {1} Camps", locations.Count, kvp.Value.Type.Name);
                    foreach (var px in locations)
                        System.Console.WriteLine("At {0}", px);
                }
            }
            System.Console.WriteLine("Spawned {0} Dynamic Camps in:{1}", totalCamps, tcg.TimeTaken);
        }
        private List<Point3D> InUse(List<Point3D> seeds, Type type)
        {
            List<Point3D> allocated = new();
            foreach (var px in seeds)
            {
                BaseMulti bm = BaseMulti.FindAt(px, Map.Felucca);
                if (bm != null && bm.GetType().IsAssignableFrom(type))
                    allocated.Add(px);
            }

            return allocated;
        }
        private bool MinDistance(List<Point3D> allocated, Point3D p1)
        {
            foreach (var p2 in allocated)
                if (Utility.GetDistanceToSqrt(p1, p2) < 40.0)
                    // too close
                    return false;
            return true;
        }
        private void MarkAsDynamic()
        {

        }
        private bool DynamicCampSystemWorker(List<Point3D> seeds, int Want, Type type, List<Point3D> locations)
        {
            List<Point3D> Have = InUse(seeds, type);
            if (Have.Count >= Want) return false;
            int GiveUp = 50;
            try
            {
                while (Have.Count < Want && GiveUp-- > 0)
                {
                    for (int ix = 0; ix < 10; ix++)
                    {
                        Point3D location = seeds[Utility.Random(seeds.Count)];
                        if (MinDistance(Have, location) && !Have.Contains(location))
                            if (CampManager.CheckLocation(ref location, Map.Felucca, retries: 1))
                            {
                                BaseMulti camp = (BaseMulti)Activator.CreateInstance(type);
                                camp.MoveToWorld(location, Map.Felucca);
                                Have.Add(location);
                                locations.Add(location);
                                break;
                            }
                    }
                }

                return (Have.Count >= Want);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Dynamic Camp code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return false;
        }
    }
    #endregion DynamicCampSystem

    #region OverlandSystem
    // produce an overland mobile
    class COverlandSystem
    {
        public void OverlandSystem()
        {
            System.Console.Write("Overland Merchant spawn ... ");
            if (Utility.RandomChance(10))
            {
                Utility.TimeCheck tc = new Utility.TimeCheck();
                tc.Start();
                Point3D location;
                bool result = OverlandSystem(out location);
                tc.End();
                if (result == false)
                    System.Console.WriteLine("Failed to spawn an Overland Mobile");
                else
                    System.Console.WriteLine("Spawned an Overland Mobile at {0} in:{1}", location, tc.TimeTaken);
            }
            else
                System.Console.WriteLine("None at this time");
        }

        private bool OverlandSystem(out Point3D location)
        {
            bool result = true;
            location = new Point3D(0, 0, 0);
            try
            {
                OverlandSpawner.OverlandSpawner om = new OverlandSpawner.OverlandSpawner();
                Mobile m = null;
                if (om != null)
                {
                    switch (Utility.Random(3))
                    {
                        case 0: m = om.OverlandSpawnerSpawn(new OverlandBandit()); break;
                        case 1: m = om.OverlandSpawnerSpawn(new OverlandMerchant()); break;
                        case 2: m = om.OverlandSpawnerSpawn(new OverlandTreasureHunter()); break;
                    }
                }
                if (m == null)
                    result = false;
                else
                    location = m.Location;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Overland code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return result;
        }
    }
    #endregion OverlandSystem

    #region ReloadSpawnerCache (obsolete)
    // no longer used -- cache is loaded on server-up
    // reload the spawner cache - being obsoleted 
    /*class CReloadSpawnerCache
	{
		public void ReloadSpawnerCache()
		{
			System.Console.Write("Reloading Spawner Cache ... ");
			Utility.TimeCheck tc = new Utility.TimeCheck();
			tc.Start();
			int count = ReloadSpawnerCacheWorker();
			tc.End();
			System.Console.WriteLine("Spawner cache reloaded with {0} spawners in:{1}", count, tc.TimeTaken);
		}

		private int ReloadSpawnerCacheWorker()
		{
			int UnitsChecked = 0;
			try
			{
				OverlandSpawner.OverlandSpawner om = new OverlandSpawner.OverlandSpawner();
				if (om != null)
					UnitsChecked = om.LoadSpawnerCache();
			}
			catch (Exception e)
			{
				LogHelper.LogException(e);
				System.Console.WriteLine("Exception Caught in ReloadSpawnerCache code: " + e.Message);
				System.Console.WriteLine(e.StackTrace);
			}

			return UnitsChecked;
		}
	}*/
    #endregion ReloadSpawnerCache

    #region EmailDonationReminder (unused)
    // send email donation reminders
    class CEmailDonationReminder
    {
        public void EmailDonationReminder()
        {
            if (TestCenter.Enabled == false)
            {
                System.Console.Write("Building list of donation reminders ... ");
                Utility.TimeCheck tc = new Utility.TimeCheck();
                tc.Start();
                int Reminders = 0;
                int AcctsChecked = EmailDonationReminderWorker(out Reminders);
                tc.End();
                System.Console.WriteLine("checked {0} in {1} - {2} Accounts reminders", AcctsChecked, tc.TimeTaken, Reminders);
            }
        }

        private int EmailDonationReminderWorker(out int Reminders)
        {
            int iChecked = 0;
            Reminders = 0;
            try
            {
                // loop through the accouints looking for current users
                ArrayList results = new ArrayList();
                /*foreach (Account acct in Accounts.Table.Values)
				{
					iChecked++;
					if (acct.DoNotSendEmail == false)
						if (EmailHelpers.RecentLogin(acct, 10) == true)
						{
							Reminders++;
							results.Add(acct.EmailAddress);
						}
				}*/

                // Adam: lets keep the above code around for future needs, but for now lets use our mailing list
                //	NOTE: If you change this make sure to remove the password from the subject line
                string password = Environment.GetEnvironmentVariable("AI.EMAIL.DISTLIST.PASSWORD");
                if (password == null || password.Length == 0)
                    throw new ApplicationException("the password for distribution list access is not set.");
                Reminders = 1;
                iChecked = 1;
                results.Clear();
                results.Add(Environment.GetEnvironmentVariable("AI.EMAIL.ANNOUNCEMENTS"));

                if (Reminders > 0)
                {
                    Server.Engines.CronScheduler.DonationReminderMsg mr = new Server.Engines.CronScheduler.DonationReminderMsg();
                    // okay, now hand the list of users off to our mailer daemon
                    new Emailer().SendEmail(results, password + mr.m_subject, mr.m_body, false);
                }

            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Donation Reminder code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            return iChecked;
        }
    }
    #endregion EmailDonationReminder

    #region WeaponDistribution (ok)
    // email a report of weapon distribution among players
    class CWeaponDistribution
    {
        public void WeaponDistribution()
        {
            if (TestCenter.Enabled == false)
            {
                System.Console.Write("Emailing a report of weapon distribution among players ... ");
                Utility.TimeCheck tc = new Utility.TimeCheck();
                tc.Start();
                int mobileschecked = WeaponDistributionWorker();
                tc.End();
                System.Console.WriteLine("checked " + mobileschecked + " characters in: " + tc.TimeTaken);
            }
        }

        private int WeaponDistributionWorker()
        {
            ArrayList toCatalog = new ArrayList();
            foreach (Mobile m in World.Mobiles.Values)
            {
                // if player and logged in and not staff...
                if (m is PlayerMobile && m.Map != Map.Internal && m.AccessLevel == AccessLevel.Player)
                {
                    toCatalog.Add(m);
                }
            }

            int Archery = 0, PureArchery = 0;
            int Fencing = 0, PureFencing = 0;
            int Macing = 0, PureMacing = 0;
            int Magery = 0, PureMagery = 0;
            int Swords = 0, PureSwords = 0;

            for (int i = 0; i < toCatalog.Count; i++)
            {
                // Catalog online players only
                PlayerMobile pm = toCatalog[i] as PlayerMobile;
                if (pm != null)
                {
                    int PrevTotal = Archery + Fencing + Macing + Magery + Swords;
                    Archery += pm.Skills[SkillName.Archery].Base >= 90 ? 1 : 0;
                    Fencing += pm.Skills[SkillName.Fencing].Base >= 90 ? 1 : 0;
                    Macing += pm.Skills[SkillName.Macing].Base >= 90 ? 1 : 0;
                    Magery += pm.Skills[SkillName.Magery].Base >= 90 ? 1 : 0;
                    Swords += pm.Skills[SkillName.Swords].Base >= 90 ? 1 : 0;

                    // now record 'pure' temps
                    if ((Archery + Fencing + Macing + Magery + Swords) - PrevTotal == 1)
                    {
                        PureArchery += pm.Skills[SkillName.Archery].Base >= 90 ? 1 : 0;
                        PureFencing += pm.Skills[SkillName.Fencing].Base >= 90 ? 1 : 0;
                        PureMacing += pm.Skills[SkillName.Macing].Base >= 90 ? 1 : 0;
                        PureMagery += pm.Skills[SkillName.Magery].Base >= 90 ? 1 : 0;
                        PureSwords += pm.Skills[SkillName.Swords].Base >= 90 ? 1 : 0;
                    }
                }
            }

            int totalFighters = toCatalog.Count;

            string archery = string.Format("{0:f2}% archers of which {1:f2}% are pure\n",
                percentage(totalFighters, Archery),
                percentage(Archery, PureArchery));

            string fencing = string.Format("{0:f2}% fencers of which {1:f2}% are pure\n",
                percentage(totalFighters, Fencing),
                percentage(Fencing, PureFencing));

            string macing = string.Format("{0:f2}% macers of which {1:f2}% are pure\n",
                percentage(totalFighters, Macing),
                percentage(Macing, PureMacing));

            string magery = string.Format("{0:f2}% mages of which {1:f2}% are pure\n",
                percentage(totalFighters, Magery),
                percentage(Magery, PureMagery));

            string swords = string.Format("{0:f2}% swordsmen of which {1:f2}% are pure\n",
                percentage(totalFighters, Swords),
                percentage(Swords, PureSwords));

            string body = string.Format(
                "There are {0} players logged in.\n" +
                archery + fencing + macing +
                magery + swords,
                toCatalog.Count);

            // okay, now send this report off
            new Emailer().SendEmail("luke@tomasello.com", "weapon distribution among players", body, false);

            return toCatalog.Count;
        }

        // totalFighters != 0 ? (Archery / totalFighters) * 100 : 0,
        private double percentage(int lside, int rside)
        {
            return (double)lside != 0.0 ? ((double)rside / (double)lside) * 100.0 : 0.0;
        }
    }
    #endregion WeaponDistribution 

    #region ServerWarsAES (off)
    // Kick off an automated Server Wars (AES)
    class CServerWarsAES
    {
        public void ServerWarsAES()
        {   // 1st Sunday of the month. However, we schedule it 24 hours prior!!
            System.Console.Write("AES Server Wars initiated ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            ServerWarsAES ServerWars = new ServerWarsAES();
            int Reminders = 0;
            if (TestCenter.Enabled == false && ServerWars != null)
                ServerWarsReminders(ServerWars, out Reminders);
            tc.End();
            System.Console.WriteLine("AES Server Wars scheduled.");
        }

        private int ServerWarsReminders(ServerWarsAES ServerWars, out int Reminders)
        {
            int iChecked = 0;
            Reminders = 0;
            try
            {
                // loop through the accouints looking for current users
                ArrayList results = new ArrayList();
                /*foreach (Account acct in Accounts.Table.Values)
				{
					iChecked++;
					if (acct.DoNotSendEmail == false)
						if (EmailHelpers.RecentLogin(acct, 120) == true)
						{
							Reminders++;
							results.Add(acct.EmailAddress);
						}
				}*/

                // Adam: lets keep the above code around for future needs, but for now lets use our mailing list
                //	NOTE: If you change this make sure to remove the password from the subject line
                string password = Environment.GetEnvironmentVariable("AI.EMAIL.DISTLIST.PASSWORD");
                if (password == null || password.Length == 0)
                    throw new ApplicationException("the password for distribution list access is not set.");
                Reminders = 1;
                iChecked = 1;
                results.Clear();
                results.Add(Environment.GetEnvironmentVariable("AI.EMAIL.ANNOUNCEMENTS"));

                if (Reminders > 0)
                {
                    Server.Engines.CronScheduler.ServerWarsMsg er = new Server.Engines.CronScheduler.ServerWarsMsg();

                    // from Ransome AES
                    DateTime EventStartTime = ServerWars.EventStartTime;        // DateTime.UtcNow.AddDays(1.0);		// 24 hours from now
                    DateTime EventEndTime = ServerWars.EventEndTime;            // EventStartTime.AddHours(3.0);	// 3 hour event
                    DateTime EventStartTimeEastern = EventStartTime + TimeSpan.FromHours(3);

                    // Sunday, November 20th
                    string subject = string.Format(password + er.m_subject,
                        EventStartTime);                // Sunday, November 20th

                    string body = string.Format(er.m_body,

                        // "Angel Island will be hosting another Cross Shard Challenge this {0:D} from {0:t} - {1:t} PM Pacific Time."
                        EventStartTime,                 // Sunday, November 20th / 3:00 PM
                        EventEndTime,                   // 6:00 PM End

                        // "({0:t} PM Pacific Time is {2:t} Eastern)"
                        EventStartTimeEastern,          // 6:00 PM Eastern Start

                        // "Server Wars are actually scheduled for {0:t} PM, but sometimes start from 3-5 minutes later. If you login at {3:t}, you should be safe.\n" +
                        EventStartTime + TimeSpan.FromMinutes(15)   // 3:15 PM Start

                        // "If you want to be there from the moment it begins, login at {0:t} and simply wait for the \"Server Wars have begun!\" global announcement.\n" +
                        );

                    // okay, now hand the list of users off to our mailer daemon
                    new Emailer().SendEmail(results, subject, body, false);
                }

            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Ransome Quest Donation Reminder code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            return iChecked;
        }
    }
    #endregion ServerWarsAES

    #region TownInvasionAES (off)
    // Kick off an automated Town Invasion (AES)
    class CTownInvasionAES
    {
        public void TownInvasionAES()
        {   // 3nd Sunday of the month. However, we schedule it 24 hours prior!!
            System.Console.Write("AES Town Invasion initiated ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            TownInvasionAES TownInvasion = new TownInvasionAES();
            tc.End();
            System.Console.WriteLine("AES Town Invasion scheduled.");
        }
    }
    #endregion TownInvasionAES

    #region KinRansomAES (off)
    // Kick off an automated Kin Ransom Quest (AES)
    class CKinRansomAES
    {
        public void KinRansomAES()
        {   // 4nd Sunday of the month. However, we schedule it 24 hours prior!!
            System.Console.Write("AES Kin Ransom Quest initiated ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            KinRansomAES KinRansom = new KinRansomAES();
            int Reminders = 0;
            if (TestCenter.Enabled == false && KinRansom != null)
                RansomQuestReminders(KinRansom, out Reminders);
            tc.End();
            System.Console.WriteLine("AES Kin Ransom Quest scheduled with {0} reminders sent.", Reminders);
        }

        private int RansomQuestReminders(KinRansomAES KinRansom, out int Reminders)
        {
            int iChecked = 0;
            Reminders = 0;
            try
            {
                // loop through the accouints looking for current users
                ArrayList results = new ArrayList();
                /*foreach (Account acct in Accounts.Table.Values)
				{
					iChecked++;
					if (acct.DoNotSendEmail == false)
						if (EmailHelpers.RecentLogin(acct, 120) == true)
						{
							Reminders++;
							results.Add(acct.EmailAddress);
						}
				}*/

                // Adam: lets keep the above code around for future needs, but for now lets use our mailing list
                //	NOTE: If you change this make sure to remove the password from the subject line
                string password = Environment.GetEnvironmentVariable("AI.EMAIL.DISTLIST.PASSWORD");
                if (password == null || password.Length == 0)
                    throw new ApplicationException("the password for distribution list access is not set.");
                Reminders = 1;
                iChecked = 1;
                results.Clear();
                results.Add(Environment.GetEnvironmentVariable("AI.EMAIL.ANNOUNCEMENTS"));

                if (Reminders > 0)
                {
                    Server.Engines.CronScheduler.RansomQuestReminderMsg er = new Server.Engines.CronScheduler.RansomQuestReminderMsg();

                    // from Ransome AES
                    DateTime EventStartTime = KinRansom.EventStartTime;     // DateTime.UtcNow.AddDays(1.0);		// 24 hours from now
                    DateTime EventEndTime = KinRansom.EventEndTime;         // EventStartTime.AddHours(3.0);	// 3 hour event
                                                                            //DateTime ChestOpenTime = KinRansom.ChestOpenTime;		// EventEndTime.AddMinutes(-15.0);	// at 2hrs and 45 min, open the chest

                    // Sunday, November 20th
                    string subject = string.Format(password + er.m_subject,
                        EventStartTime);                // Sunday, November 20th

                    string body = string.Format(er.m_body,
                        EventStartTime                  // Sunday, November 20th
                        );

                    // okay, now hand the list of users off to our mailer daemon
                    new Emailer().SendEmail(results, subject, body, false);
                }

            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Ransome Quest Donation Reminder code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            return iChecked;
        }

    }
    #endregion KinRansomAES

    #region CrazyMapDayAES (off)
    // Kick off an automated Crazy Map Day (AES)
    class CCrazyMapDayAES
    {
        public void CrazyMapDayAES()
        {   // 5th Sunday of the month. However, we schedule it 24 hours prior!!
            System.Console.Write("AES Crazy Map Day initiated ... ");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            CrazyMapDayAES CrazyMapDay = new CrazyMapDayAES();
            tc.End();
            System.Console.WriteLine("AES Crazy Map Day scheduled.");
        }
    }
    #endregion CrazyMapDayAES

    #region AccountStatistics
    // calculate account statistics and log
    class CAccountStatistics
    {
        public void AccountStatistics()
        {
            System.Console.Write("Account Statistics running... ");

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            AccountStatisticsWorker();
            tc.End();
            System.Console.WriteLine("Done in " + tc.TimeTaken);
        }

        private void AccountStatisticsWorker()
        {
            try
            {
                DateTime dtNow = DateTime.UtcNow;
                //Get Number Online Now
                int numberOnline = Server.Network.NetState.Instances.Count;
                int numberUniqueIPOnline = 0;

                Hashtable ipNS = new Hashtable();
                foreach (NetState n in Server.Network.NetState.Instances)
                {
                    if (!ipNS.ContainsKey(n.Address.ToString()))
                        ipNS.Add(n.Address.ToString(), null);
                }
                numberUniqueIPOnline = ipNS.Keys.Count;

                //Get the number of accounts accessed in the last 24 hours
                int numberAccessedInLastDay = 0;
                int numberAccessedInLastHour = 0;
                int numberAccessedInLastWeek = 0;

                //Unique IPs
                int numberUniqueIPLastDay = 0;
                int numberUniqueIPLastWeek = 0;
                Hashtable ipsDay = new Hashtable();
                Hashtable ipsWeek = new Hashtable();

                int accountsCreatedLastDay = 0;
                int accountsCreatedLastWeek = 0;

                ArrayList accountList = new ArrayList(Server.Accounting.Accounts.Table.Values);
                foreach (Account a in accountList)
                {
                    bool bOnline = false;
                    for (int j = 0; j < 5; ++j)
                    {
                        Mobile check = a[j];

                        if (check == null)
                            continue;

                        if (check.NetState != null)
                        {
                            bOnline = true;
                            break;
                        }
                    }

                    if (bOnline || a.LastLogin > dtNow.AddHours(-1.0))
                    {
                        numberAccessedInLastHour++;
                    }
                    if (bOnline || a.LastLogin > dtNow.AddDays(-1.0))
                    {
                        numberAccessedInLastDay++;
                        if (a.LoginIPs.Length > 0)
                        {
                            if (!ipsDay.ContainsKey(a.LoginIPs[0]))
                                ipsDay.Add(a.LoginIPs[0], null);
                        }
                    }
                    if (bOnline || a.LastLogin > dtNow.AddDays(-7.0))
                    {
                        numberAccessedInLastWeek++;
                        if (a.LoginIPs.Length > 0)
                        {
                            if (!ipsWeek.ContainsKey(a.LoginIPs[0]))
                                ipsWeek.Add(a.LoginIPs[0], null);
                        }
                    }

                    if (a.Created > dtNow.AddDays(-1.0))
                    {
                        accountsCreatedLastDay++;
                    }
                    if (a.Created > dtNow.AddDays(-7.0))
                    {
                        accountsCreatedLastWeek++;
                    }
                }

                numberUniqueIPLastDay = ipsDay.Keys.Count;
                numberUniqueIPLastWeek = ipsWeek.Keys.Count;

                //What else?

                try
                {
                    Server.Township.TownshipSettings.CalculateFeesBasedOnServerActivity(numberAccessedInLastWeek);
                }
                catch (Exception cfbosaException)
                {
                    LogHelper.LogException(cfbosaException);
                }

                string savePath = "Logs\\AccountStats.log";
                //Output to file:
                using (StreamWriter writer = new StreamWriter(new FileStream(savePath, FileMode.Append, FileAccess.Write, FileShare.Read)))
                {
                    writer.WriteLine("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}",
                        dtNow.ToString(), //0
                        numberOnline, //1
                        numberUniqueIPOnline, //2
                        numberAccessedInLastHour, //3
                        numberAccessedInLastDay, //4
                        numberAccessedInLastWeek, //5
                        numberUniqueIPLastDay, //6
                        numberUniqueIPLastWeek, //7
                        accountsCreatedLastDay, //8
                        accountsCreatedLastWeek //9
                        );

                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Heartbeat.AccountStatistics exceptioned");
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion AccountStatistics

    #region GuildFealty
    // calculate guild fealty
    class CGuildFealty
    {
        public void GuildFealty()
        {
            System.Console.WriteLine("Counting fealty votes for guildmasters...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int guildschecked = GuildFealtyWorker();
            tc.End();
            System.Console.WriteLine("checked " + guildschecked + " guilds in: " + tc.TimeTaken);
        }

        private int GuildFealtyWorker()
        {
            try
            {
                int count = 0;
                foreach (Guild g in BaseGuild.List.Values)
                {
                    count++;
                    if (g.LastFealty + TimeSpan.FromDays(1.0) < DateTime.UtcNow)
                        g.CalculateGuildmaster();
                }
                return count;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Exception while running Heartbeat.GuildFealty() job:");
                Console.WriteLine(e);
                return 0;
            }
        }
    }
    #endregion GuildFealty

    #region PlayerQuestCleanup
    // cleanup unclaimed player quest stuffs
    class CPlayerQuestCleanup
    {
        public void PlayerQuestCleanup()
        {
            System.Console.Write("Checking decay of PlayerQuest items...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int PlayerQuestsDeleted;
            int PlayerQuestsChecked = PlayerQuestCleanupWorker(out PlayerQuestsDeleted);
            tc.End();
            System.Console.WriteLine("checked {0} quests in {1}, {2} deleted.",
                    PlayerQuestsChecked, tc.TimeTaken, PlayerQuestsDeleted);
        }

        private int PlayerQuestCleanupWorker(out int PlayerQuestsDeleted)
        {
            PlayerQuestsDeleted = 0;
            int count = 0;
            LogHelper Logger = null;
            try
            {
                ArrayList ToDelete = new ArrayList();

                // find expired
                foreach (Item ix in World.Items.Values)
                {
                    if (ix is PlayerQuestDeed == false) continue;
                    PlayerQuestDeed pqd = ix as PlayerQuestDeed;    // get the deed
                    if (pqd.Expired == false) continue;             // do nothing, it's still in play
                    if (pqd.Deleted == true) continue;              // already been deleted (and the container along with it.)
                                                                    // we have a container that belongs to an expired quest
                                                                    //	We will delete it and set the container fields to null in the ticket.
                                                                    // The ticket will know what to do
                    if (pqd.Container != null && pqd.Container.Deleted == false)
                    {   // detele the storage, the ticket will take care of itself
                        ToDelete.Add(pqd.Container);
                    }
                    count++;
                }

                // okay, now create the log file
                if (ToDelete.Count > 0)
                    Logger = new LogHelper("PlayerQuest.log", false);

                // cleanup
                for (int i = 0; i < ToDelete.Count; i++)
                {
                    BaseContainer bc = ToDelete[i] as BaseContainer;
                    if (bc != null)
                    {
                        // record the expiring quest chest
                        Logger.Log(LogType.Item, bc, "Player Quest prize being deleted because the quest has expired.");
                        bc.Delete();
                        PlayerQuestsDeleted++;
                    }
                }

            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Exception while running Heartbeat.PlayerQuestCleanup() job");
                Console.WriteLine(e);
            }
            finally
            {
                if (Logger != null)
                    Logger.Finish();
            }

            return count;
        }
    }
    #endregion PlayerQuestCleanup

    #region PlayerQuestAnnounce
    // announce player quests
    class CPlayerQuestAnnounce
    {
        public void PlayerQuestAnnounce()
        {
            System.Console.Write("Checking for PlayerQuest announcements...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int PlayerQuestsAnnounced;
            int PlayerQuestsChecked = PlayerQuestAnnounceWorker(out PlayerQuestsAnnounced);
            tc.End();
            System.Console.WriteLine("checked {0} quests in {1}, {2} announced.",
                    PlayerQuestsChecked, tc.TimeTaken, PlayerQuestsAnnounced);
        }

        private int PlayerQuestAnnounceWorker(out int PlayerQuestsAnnounced)
        {
            PlayerQuestsAnnounced = 0;
            //LogHelper Logger = new LogHelper("PlayerQuest.log", false);
            int count = 0;

            try
            {
                count = PlayerQuestManager.Announce(out PlayerQuestsAnnounced);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Exception while running Heartbeat.PlayerQuestsAnnounce() job");
                Console.WriteLine(e);
            }
            finally
            {
                //Logger.Finish();
            }

            return count;
        }
    }
    #endregion PlayerQuestAnnounce

    #region LogRotation
    // rotate the command logs
    class CLogRotation
    {
        public void LogRotation()
        {
            System.Console.WriteLine("Rotation of command logs initiated...");
            bool ok = LogRotationWorker();
        }

        private bool LogRotationWorker()
        {
            try
            {
                RotateLogs.RotateNow();
                return true;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Exception while running Heartbeat.LogRotation() job");
                Console.WriteLine(e);
            }
            return false;
        }
    }
    #endregion LogRotation

    #region LogLogArchive
    // rotate the command logs
    class CLogArchive
    {
        public void LogArchive()
        {
            System.Console.WriteLine("Archive of command logs initiated...");
            bool ok = LogArchiveWorker();
        }

        private bool LogArchiveWorker()
        {
            try
            {
                RotateLogs.ArchiveNow();
                return true;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Exception while running Heartbeat.LogArchive() job");
                Console.WriteLine(e);
            }
            return false;
        }
    }
    #endregion LogArchive

    #region BackupRunUO
    // at 2AM backup the complete RunUO directory (~8min)
    class CBackupRunUO
    {
        public void BackupRunUO()
        {
            System.Console.Write("Sending maintenance message...");
            DateTime now = DateTime.UtcNow;
            DateTime when = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0, 0);
            TimeSpan delta = when - now;
            string text = string.Format("Maintenance in {0:00.00} minutes and should last for {1} minutes.", delta.TotalMinutes, 8);
            World.Broadcast(0x35, true, text);
            System.Console.WriteLine(text);
            System.Console.WriteLine("Done.");
        }
    }
    #endregion BackupRunUO

    #region BackupWorldData
    // at 4AM we backup only the world files (~4min)
    class CBackupWorldData
    {
        public void BackupWorldData()
        {
            System.Console.Write("Sending maintenance message...");
            DateTime now = DateTime.UtcNow;
            DateTime when = new DateTime(now.Year, now.Month, now.Day, 4, 0, 0, 0);
            TimeSpan delta = when - now;
            string text = string.Format("Maintenance in {0:00.00} minutes and should last for {1} minutes.", delta.TotalMinutes, 4);
            World.Broadcast(0x35, true, text);
            System.Console.WriteLine(text);
            System.Console.WriteLine("Done.");
        }
    }
    #endregion BackupWorldData

    #region TownshipCharges
    // calculate township charges
    class CTownshipCharges
    {
        public void TownshipCharges()
        {
            System.Console.Write("Performing Township Charges...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int townshipcount = TownshipStone.AllTownshipStones.Count;
            int townshipsremoved = TownshipStone.DoAllTownshipFees();
            tc.End();
            System.Console.WriteLine("removed {0} of {1} townships in: {2}.", townshipsremoved, townshipcount, tc.TimeTaken);
        }
    }
    #endregion TownshipCharges

    #region TownshipWallDecay
    // calculate township charges
    class CTownshipWallDecay
    {
        public void TownshipWallDecay()
        {
            System.Console.Write("Performing Township Wall Decay...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int townshipwallcount = Server.Township.TownshipItemHelper.AllTownshipItems.Count;
            int townshipwallsdeleted = Server.Township.TownshipItemHelper.Atrophy();
            tc.End();
            System.Console.WriteLine("removed {0} of {1} township walls in: {2}.", townshipwallsdeleted, townshipwallcount, tc.TimeTaken);
        }
    }
    #endregion TownshipCharges

    #region TownshipItemDefrag
    class CTownshipItemDefrag
    {
        public void TownshipItemDefrag()
        {
            System.Console.Write("Defragging Township Item Registries...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int count = Server.Township.TownshipItemRegistry.DefragAll();
            tc.End();
            System.Console.WriteLine("unregistered {0} township items in: {1}.", count, tc.TimeTaken);
        }
    }
    #endregion TownshipCharges

    #region FreezeDryInit
    // freezedry containers that need it after startup .. run once after startup
#if false
    class CFreezeDryInit
    {
        public void FreezeDryInit()
        {
            System.Console.Write("Freeze Dry starup init...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int scheduled = FreezeDryInitWorker();
            tc.End();
            System.Console.WriteLine("{0} containers scheduled for freeze drying in: {1}.", scheduled, tc.TimeTaken);
        }

        private int FreezeDryInitWorker()
        {
            int scheduled = 0;

            try
            {
                foreach (Item i in World.Items.Values)
                {
                    if (i as Container != null)
                    {
                        Container cx = i as Container;
                        if (cx.ScheduleFreeze())
                            scheduled++;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("FreezeDryInit code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return scheduled;
        }
    }
#endif
    #endregion FreezeDryInit

    #region WealthTracker
    // see who's making all the money on the shard (look for exploits)
    class CWealthTracker
    {
        public void WealthTracker()
        {
            System.Console.Write("Collecting WealthTracker information...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            WealthTrackerWorker();
            tc.End();
            System.Console.WriteLine("WealthTracker complete : {0}.", tc.TimeTaken);
        }

        private void WealthTrackerWorker()
        {
            try
            {
                int limit = 10;     // top 10 farmers
                int timeout = 90;   // active within the last 90 minutes

                // compile the constrained list
                Server.Engines.WealthTracker.IPDomain[] list = Server.Engines.WealthTracker.ReportCompiler(limit, timeout);

                LogHelper Logger1 = new LogHelper("WealthTrackerNightly.log", true);    // this one gets emailed each night
                LogHelper Logger2 = new LogHelper("WealthTracker.log", false);          // this is a running account

                // write a super minimal report
                for (int ix = 0; ix < list.Length; ix++)
                {
                    Server.Engines.WealthTracker.IPDomain node = list[ix] as Server.Engines.WealthTracker.IPDomain;
                    Server.Engines.WealthTracker.AccountDomain ad = Server.Engines.WealthTracker.GetFirst(node.accountList) as Server.Engines.WealthTracker.AccountDomain; // just first account
                    Mobile m = Server.Engines.WealthTracker.GetFirst(ad.mobileList) as Mobile;                   // just first mobile
                    string sx = string.Format("mob:{2}, gold:{0}, loc:{1}", node.gold, node.location, m);
                    Logger1.Log(LogType.Text, sx);
                    Logger2.Log(LogType.Text, sx);
                }

                Logger1.Finish();
                Logger2.Finish();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("WealthTracker code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion WealthTracker

    #region ConsumerPriceIndex
    // see who's making all the money on the shard (look for exploits)
    class CConsumerPriceIndex
    {
        public void ConsumerPriceIndex()
        {
            System.Console.Write("Collecting ConsumerPriceIndex information...");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            ConsumerPriceIndexWorker();
            tc.End();
            System.Console.WriteLine("ConsumerPriceIndex complete : {0}.", tc.TimeTaken);
        }

        private void ConsumerPriceIndexWorker()
        {
            try
            {
                LogHelper Logger1 = new LogHelper("ConsumerPriceIndexNightly.log", true);   // this one gets emailed each night
                LogHelper Logger2 = new LogHelper("ConsumerPriceIndex.log", false);         // this is a running account

                string s1, s2;
                Server.Commands.Diagnostics.CPI_Worker(out s1, out s2);
                Logger1.Log(LogType.Text, s1);
                Logger1.Log(LogType.Text, s2);
                Logger2.Log(LogType.Text, s1);
                Logger2.Log(LogType.Text, s2);

                Logger1.Finish();
                Logger2.Finish();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("ConsumerPriceIndex code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion ConsumerPriceIndex

    #region KinFactions
    /*
	// do kin faction processing
	class CKinFactions
	{
		public void KinFactions()
		{
			System.Console.Write("Processing faction kin sigils....");
			Utility.TimeCheck tc = new Utility.TimeCheck();
			tc.Start();
			KinCityManager.ProcessSigils();
			tc.End();
			System.Console.WriteLine("Kin faction sigil processing complete : {0}.", tc.TimeTaken);
		}
	}
	// do kin faction logging processing
	class CKinFactionsLogging
	{
		public void KinFactionsLogging()
		{
			System.Console.Write("Processing faction logs....");
			Utility.TimeCheck tc = new Utility.TimeCheck();
			tc.Start();
			KinCityManager.ProcessAndOutputLogs();
			tc.End();
			System.Console.WriteLine("Kin faction logs complete : {0}.", tc.TimeTaken);
		}
	}
    */
    #endregion KinFactions

    #region SpringChamp
    // turn on spring champ / turn off winter champ
    class CSpringChamp
    {
        public void SpringChamp()           // turn on spring champ / turn off winter champ()
        {
            System.Console.Write("Processing seasonal champ....");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            SpringChampWorker();
            tc.End();
            System.Console.WriteLine("Seasonal champ processing complete : {0}.", tc.TimeTaken);
        }

        // turn on spring champ / turn off winter champ
        private void SpringChampWorker()
        {
            try
            {
                // turn on the Spring Champ - Vamp
                ChampHelpers.ToggleChamp(ChampHelpers.Spring_Vampire, true);

                // turn off the Winter Champ - Frozen Host
                ChampHelpers.ToggleChamp(ChampHelpers.Winter_FrozenHost, false);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Seasonal champ code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion SpringChamp

    #region SummerChamp
    // turn on summer champ / turn off spring champ
    class CSummerChamp
    {
        public void SummerChamp()           // turn on summer champ / turn off spring champ()
        {
            System.Console.Write("Processing seasonal champ....");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            SummerChampWorker();
            tc.End();
            System.Console.WriteLine("Seasonal champ processing complete : {0}.", tc.TimeTaken);
        }

        private void SummerChampWorker()
        {
            try
            {
                // turn on summer champ - Pirate
                ChampHelpers.ToggleChamp(ChampHelpers.Summer_Pirate, true);

                // turn off spring champ - Vamp
                ChampHelpers.ToggleChamp(ChampHelpers.Spring_Vampire, false);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Seasonal champ code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion SummerChamp

    #region AutumnChamp
    // turn on autumn champ / turn off summer champ
    class CAutumnChamp
    {
        public void AutumnChamp()           // turn on autumn champ / turn off summer champ()
        {
            System.Console.Write("Processing seasonal champ....");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            AutumnChampWorker();
            tc.End();
            System.Console.WriteLine("Seasonal champ processing complete : {0}.", tc.TimeTaken);
        }

        private void AutumnChampWorker()
        {
            try
            {
                // turn on autumn champ
                ChampHelpers.ToggleChamp(ChampHelpers.Autumn_Bob, true);

                // turn off summer champ
                ChampHelpers.ToggleChamp(ChampHelpers.Summer_Pirate, false);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Seasonal champ code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion AutumnChamp

    #region WinterChamp
    // turn on winter champ / turn off autumn champ
    class CWinterChamp
    {
        public void WinterChamp()           // turn on winter champ / turn off autumn champ()
        {
            System.Console.Write("Processing seasonal champ....");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            WinterChampWorker();
            tc.End();
            System.Console.WriteLine("Seasonal champ processing complete : {0}.", tc.TimeTaken);
        }

        private void WinterChampWorker()
        {
            try
            {
                // turn on winter champ
                ChampHelpers.ToggleChamp(ChampHelpers.Winter_FrozenHost, true);

                // turn off autumn champ
                ChampHelpers.ToggleChamp(ChampHelpers.Autumn_Bob, false);
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Seasonal champ code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion WinterChamp

    #region CheckChamps
    // eg. turn on winter champ / turn off autumn champ
    class CCheckChamps
    {
        public void CheckChamps()           // turn on winter champ / turn off autumn champ()
        {
            System.Console.Write("Checking seasonal Champs....");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            CheckChampsWorker();
            tc.End();
            System.Console.WriteLine("Seasonal champ processing complete : {0}.", tc.TimeTaken);
        }

        private void CheckChampsWorker()
        {
            Serial[] toCheck = new[] { ChampHelpers.Summer_Pirate, ChampHelpers.Autumn_Bob, ChampHelpers.Winter_FrozenHost, ChampHelpers.Spring_Vampire };
            try
            {
                Console.WriteLine();
                foreach (Serial serial in toCheck)
                {
                    if (World.FindItem(serial) as ChampEngine != null)
                    {
                        ChampEngine champ = World.FindItem(serial) as ChampEngine;
                        Console.WriteLine("{0} seasonal champ found.", champ.Name);
                        if (champ.RestartTimer == true)
                            Utility.Monitor.WriteLine("{0} seasonal champ is running.", ConsoleColor.Green, champ.Name);
                        else
                            Utility.Monitor.WriteLine("{0} seasonal champ is not running.", ConsoleColor.Yellow, champ.Name);
                    }
                    else
                        Utility.Monitor.WriteLine("Error: seasonal champ {0} not found", ConsoleColor.Red, serial);
                }

            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Seasonal champ checking code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion CheckChamps

    #region ResetChamps
    // turn on winter champ / turn off autumn champ
    class CResetChamps
    {
        public void ResetChamps()           // turn on winter champ / turn off autumn champ()
        {
            System.Console.Write("Resetting seasonal Champs....");
            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            ResetChampsWorker();
            tc.End();
            System.Console.WriteLine("Seasonal champ reset complete : {0}.", tc.TimeTaken);
        }

        private void ResetChampsWorker()
        {
            string[] names = new[] { "Summer Pirate", "Autumn Bob", "Winter FrozenHost", "Spring Vampire" };
            Serial[] ids = new[] { ChampHelpers.Summer_Pirate, ChampHelpers.Autumn_Bob, ChampHelpers.Winter_FrozenHost, ChampHelpers.Spring_Vampire };
            try
            {
                Console.WriteLine();
                int index = 0;
                foreach (Serial serial in ids)
                {
                    if (World.FindItem(serial) as ChampEngine != null)
                    {
                        ChampEngine champ = World.FindItem(serial) as ChampEngine;
                        champ.Name = names[index++];
                        Console.WriteLine("Resetting seasonal champ {0}.", champ.Name);
                        ChampHelpers.ToggleChamp(serial, false);
                    }
                    else
                    {
                        Utility.Monitor.WriteLine("{0} seasonal not found", ConsoleColor.Red, serial);
                    }
                }

            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Seasonal champ ResetChamps code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
    #endregion ResetChamps

    #region CHAMP HELPERS
    public class ChampHelpers
    {
        #region Champ Codes
        public static Serial Summer_Pirate 
        {
            get 
            { 
                foreach (ChampEngine ce in ChampEngine.Instances)
                    if (ce.SpawnType == ChampLevelData.SpawnTypes.Pirate_Full_Sea)
                        return ce.Serial; 
                return Serial.Zero;
            }
        }
        public static Serial Autumn_Bob
        {
            get
            {
                foreach (ChampEngine ce in ChampEngine.Instances)
                    if (ce.SpawnType == ChampLevelData.SpawnTypes.Bob)
                        return ce.Serial;
                return Serial.Zero;
            }
        }
        public static Serial Winter_FrozenHost
        {
            get
            {
                foreach (ChampEngine ce in ChampEngine.Instances)
                    if (ce.SpawnType == ChampLevelData.SpawnTypes.FrozenHost)
                        return ce.Serial;
                return Serial.Zero;
            }
        }
        public static Serial Spring_Vampire
        {
            get
            {
                foreach (ChampEngine ce in ChampEngine.Instances)
                    if (ce.SpawnType == ChampLevelData.SpawnTypes.Vampire)
                        return ce.Serial;
                return Serial.Zero;
            }
        }
        #endregion Champ Codes
        public static void ToggleChamp(int champ, bool state)
        {
            try
            {
                // turn on a Champ
                ChampEngine Champ = World.FindItem(champ) as ChampEngine;
                if (Champ == null)
                    new ApplicationException(string.Format("World.FindItem({0:X}) as ChampEngine", champ));
                else
                {   // state will be true for ON and false for OFF
                    // We disable a champ by turning off the restart timer
                    // We enable by turning on the restart timer and making sure the Active property is true

                    // activate/deactivate the champ
                    Champ.Running = state;

                    // activate/deactivate the RestartTimer so that it does/doesn't restart
                    Champ.RestartTimer = state;

                    // cleanup any lingering monsters. Apologies to anyone actively fighting the champ
                    if (state == false)
                        Champ.ClearMonsters = true;

                    #region Guard Zones
                    // now address guard zones
                    // Update: 1/19/23, Adam: We no longer turn off guards for this champ as these creatures are 
                    //  'GuardIgnore' which means guards ignore them. This otherwise keeps this a guarded region.
                    if (champ == Autumn_Bob)
                    {   // this is Jhelom, must toggle guards
                        Region rx = Region.Find(Champ.Location, Map.Felucca);
                        if (rx != null)
                        {
                            if (state == true)
                            {
#if false
                                // champ goes on, guards go off
                                rx.IsGuarded = false;
#endif
                            }
                            else
                            {   // champ goes off, guards go on
                                rx.IsGuarded = true;
                            }
                        }
                    }
                    #endregion
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Seasonal champ code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

        }
        public static string ChampName(Serial serial)
        {
            ChampEngine Champ = World.FindItem(serial) as ChampEngine;
            if (Champ != null)
                return Champ.Name;
            else
                return "{Champ not found}";
        }
    }
    #endregion

    #region EMAIL HELPERS
    public class EmailHelpers
    {
        public static bool RecentLogin(Account acct, int days)
        {
            bool empty = true;      // any characters on the account?
            for (int i = 0; i < 5; ++i)
            {
                if (acct[i] != null)
                    empty = false;  // not empty
            }

            // if not empty AND active within the last N days, send a reminder
            if (empty == false)
            {
                TimeSpan delta = DateTime.UtcNow - acct.LastLogin;
                if (delta.TotalDays <= days)
                {   // send a reminder
                    return true;
                }
            }

            // no reminder should be sent
            return false;
        }
    }
    #endregion EMAIL HELPERS

}