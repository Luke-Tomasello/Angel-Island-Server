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

using Server.Accounting;
using Server.Diagnostics;
using Server.Engines.ResourcePool;
using Server.Engines.Seasons;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Commands
{
    public partial class OwnerTools
    {
        [Usage("PreLaunch")]
        [Description("Wipe Shard - Pre-Launch (with progress messages).")]
        public static void PreLaunch_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            try
            {
                string msg = "Beginning pre-launch wipe...";
                Console.WriteLine(msg);
                e.Mobile?.SendMessage(msg);

                // Pre-launch wipe (Beta → Live transition only)
                //
                // We intentionally clear all in-memory seasonal state here instead of deleting files.
                // The subsequent Save will persist a clean, empty state to disk.
                //
                // IMPORTANT:
                // - This is NOT part of normal season rollover logic. (WipeDriver())
                // - Do NOT run this at end-of-season.
                // - This permanently wipes all seasonal account data (including lifetime earned points).
                //
                int accountsCleared = SeasonalAccounts.ClearAll();
                int baselinesCleared = SeasonalEarningBaselineState.ClearAll();

                msg = $"[PreLaunch] Cleared {accountsCleared} seasonal accounts and {baselinesCleared} baseline entries.";

                Console.WriteLine(msg);
                e.Mobile?.SendMessage(msg);

                // alright wipe the world!
                var result = WipeDriver(m);

                if (m != null)
                {
                    m.SendMessage(0x35, $"{result.BoatsCount} boats deleted.");
                    m.SendMessage(0x35, $"{result.VendorsCount} player vendors deleted.");
                    m.SendMessage(0x35, $"{result.PlayerPetsCount} player-owned pets deleted.");
                    m.SendMessage(0x35, $"{result.AccountsDeletedCount} accounts deleted.");
                    m.SendMessage(0x35, $"{result.ItemsEmptiedCount} items deleted from containers.");
                    m.SendMessage(0x35, $"{result.ItemsDecayedCount} items decayed.");
                    m.SendMessage(0x35, $"{result.DoppelgangerCorpsesCount} doppelganger corpses deleted.");
                    m.SendMessage(0x35, $"{result.HousingDeedsRepriced} housing deeds repriced.");
                    m.SendMessage(0x35, $"Leaderboards wiped: {result.LeaderboardsWiped}");
                }

                m?.SendMessage(0x3B2, "Respawning world...");

                // Run the respawn
                TotalRespawnCommand.TotalRespawn(e.Mobile);

                if (m != null)
                {
                    m.SendMessage(0x35, $"{result.QuestGiversDeactivatedCount} quest givers deactivated.");
                    m.SendMessage(0x35, $"{result.ResourcePoolConsignmentsDeleted} resource pool consignments deleted.");
                    m.SendMessage(0x3B2, "Pre-launch wipe complete.");
                }

                // === SAFETY PROMPT BEFORE SAVE + RESTART ===
                if (m != null)
                {
                    m.SendMessage(0x3B2, " ");
                    m.SendMessage(0x35, "=== FINAL STEP ===");
                    m.SendMessage(0x35, "The server is about to SAVE the world and RESTART.");
                    m.SendMessage(0x35, "This will lock in all changes from the pre-launch wipe.");
                    m.SendMessage(0x35, " ");
                    m.SendMessage(0x3B2, "Type 'yes' in the console to continue, or anything else to abort.");
                }

                Console.WriteLine();
                Console.WriteLine("=== FINAL STEP ===");
                Console.WriteLine("The server is about to SAVE the world and RESTART.");
                Console.WriteLine("This will lock in all changes from the pre-launch wipe.");
                Console.Write("Type 'yes' to continue (or anything else to abort): ");

                string? response = Console.ReadLine()?.Trim().ToLowerInvariant();

                if (response != "yes")
                {
                    Console.WriteLine("Aborted by user. Server will NOT save or restart.");
                    if (m != null)
                        m.SendMessage(0x25, "Aborted by user. Server will NOT save or restart.");
                    return;
                }

                Console.WriteLine("User confirmed. Saving world and restarting server...");

                if (m != null)
                    m.SendMessage(0x3B2, "User confirmed. Saving world and restarting server...");

                // Final save and restart
                World.Save();
                /* Must be run again after final save - it was done previously for reporting reasons */
                Console.WriteLine("PreLaunch: Initializing resource pool (final)...");
                OwnerTools.InitializeResourcePool();
                Console.WriteLine("PreLaunch: Wiping leaderboards (final) ...");
                OwnerTools.WipeLeaderboards();
                Core.Kill(restart: true);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                if (m != null)
                    m.SendMessage(0x25, "** Wipe Failed ** — check logs.");
            }
        }

        public static PreLaunchWipeResult WipeDriver(Mobile? reporter = null)
        {
            var result = new PreLaunchWipeResult();

            Console.WriteLine("PreLaunch: Deleting player vendors...");
            result.VendorsCount = DeletePlayerVendors();

            Console.WriteLine("PreLaunch: Deleting boats...");
            result.BoatsCount = DeleteBoats();

            Console.WriteLine("PreLaunch: Deleting player-owned pets...");
            result.PlayerPetsCount = DeletePlayerOwnedPets();

            Console.WriteLine("PreLaunch: Deleting non-owner accounts...");
            result.AccountsDeletedCount = DeleteAccounts();

            Console.WriteLine("PreLaunch: Emptying containers...");
            result.ItemsEmptiedCount = EmptyContainers();

            Console.WriteLine("PreLaunch: Deleting doppelganger corpses...");
            result.DoppelgangerCorpsesCount = DeleteDoppelgangerCorpses();

            Console.WriteLine("PreLaunch: Deactivating quest givers...");
            result.QuestGiversDeactivatedCount = DeactivateQuestGivers();

            Console.WriteLine("PreLaunch: Forcing item decay...");
            result.ItemsDecayedCount = ForceItemDecayNow();

            Console.WriteLine("PreLaunch: Repricing custom housing deeds...");
            result.HousingDeedsRepriced = RepriceCustomHousingDeeds();

            /* Must be run again after final save - it's really only done here for reporting purposes */
            Console.WriteLine("PreLaunch: Initializing resource pool...");
            result.ResourcePoolConsignmentsDeleted = InitializeResourcePool();

            Console.WriteLine("PreLaunch: Wiping leaderboards...");
            result.LeaderboardsWiped = WipeLeaderboards();

            return result;
        }

        public class PreLaunchWipeResult
        {
            public int BoatsCount { get; set; }
            public int VendorsCount { get; set; }
            public int PlayerPetsCount { get; set; }
            public int AccountsDeletedCount { get; set; }
            public int ItemsEmptiedCount { get; set; }
            public int DoppelgangerCorpsesCount { get; set; }
            public int QuestGiversDeactivatedCount { get; set; }
            public int ResourcePoolConsignmentsDeleted { get; set; }
            public int ItemsDecayedCount { get; set; }
            public int HousingDeedsRepriced { get; set; }
            public bool LeaderboardsWiped { get; set; }
        }

        // ===================================================================
        // Protection Helpers (Simplified & Clear)
        // ===================================================================

        /// <summary>
        /// Returns true if this mobile should be PROTECTED from the pre-launch wipe.
        /// </summary>
        public static bool IsProtected(Mobile m)
        {
            if (m == null || m.Deleted)
                return false;

            if (m.IsStaffOwned)
                return true;

            if (m.AccessLevel >= AccessLevel.Owner)
                return true;

            if (m.Account is Account acct && acct.AccessLevel >= AccessLevel.Owner)
                return true;

            if (m == World.GetAdminAcct() || m == World.GetSystemAcct())
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if this item should be PROTECTED from the pre-launch wipe.
        /// Checks both the item itself and its root owner.
        /// </summary>
        public static bool IsProtected(Item item)
        {
            if (item == null || item.Deleted)
                return false;

            if (item.IsStaffOwned)
                return true;

            // Check root parent mobile
            if (item.RootParent is Mobile rootMobile && IsProtected(rootMobile))
                return true;

            return false;
        }

        // ===================================================================
        // Cleanup Routines (Updated to use IsProtected)
        // ===================================================================

        public static int DeletePlayerVendors()
        {
            int deleted = 0;
            var toDelete = new List<Mobile>();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m == null || m.Deleted || IsProtected(m))
                    continue;

                if (m is PlayerVendor or PlayerBarkeeper or RentedVendor or HouseSitter)
                    toDelete.Add(m);
            }

            foreach (var vendor in toDelete)
            {
                if (vendor != null && !vendor.Deleted)
                {
                    vendor.Delete();
                    deleted++;
                }
            }

            return deleted;
        }

        public static int DeleteBoats()
        {
            int deleted = 0;
            var toDelete = new List<Item>();

            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted || item is not BaseBoat boat)
                    continue;

                if (IsProtected(item))
                    continue;

                // Extra safety: also check boat.Owner independently
                if (boat.Owner != null && IsProtected(boat.Owner))
                    continue;

                toDelete.Add(item);
            }

            foreach (var boat in toDelete)
            {
                if (boat != null && !boat.Deleted)
                {
                    boat.Delete();
                    deleted++;
                }
            }

            return deleted;
        }

        public static int DeletePlayerOwnedPets()
        {
            var pets = new List<Mobile>();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m == null || m.Deleted || IsProtected(m))
                    continue;

                if (m is PlayerMobile pm)
                {
                    if (pm.Pets != null) pets.AddRange(pm.Pets);
                    if (pm.Stabled != null) pets.AddRange(pm.Stabled);
                    if (pm.Followers != null) pets.AddRange(pm.Followers);
                }
            }

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m == null || m.Deleted || IsProtected(m))
                    continue;

                if (m.Spawner is Spawner s && !s.IsCurrentlySpawned(m))
                    pets.Add(m);
            }

            pets = pets.Distinct().ToList();

            foreach (Mobile pet in pets)
                if (pet != null && !pet.Deleted)
                    pet.Delete();

            return pets.Count;
        }

        public static int DeleteAccounts(bool include_owner = false)
        {
            int deleted = 0;
            var accountsToDelete = new List<Account>();

            // Collect accounts to delete
            foreach (Account acct in Accounts.Table.Values)
            {
                if (acct == null)
                    continue;

                // If include_owner is false, protect all Owner-level accounts
                if (!include_owner && acct.AccessLevel >= AccessLevel.Owner)
                    continue;

                accountsToDelete.Add(acct);
            }

            // Kick connected players
            foreach (NetState ns in NetState.Instances.ToArray())   // ToArray prevents collection modified exception
            {
                if (ns != null && ns.Running && ns.Account is Account acct)
                {
                    // Never kick Owner-level accounts unless explicitly told to
                    if (!include_owner && acct.AccessLevel >= AccessLevel.Owner)
                        continue;

                    ns.Dispose();
                }
            }

            // Delete the accounts
            foreach (Account acct in accountsToDelete)
            {
                if (acct != null)
                {
                    acct.Delete();
                    deleted++;
                }
            }

            return deleted;
        }

        public static int EmptyContainers()
        {
            #region Get Loot Packs (protected)
            var lootPacks = new List<Container>();
            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted) continue;

                if (item is Spawner s)
                {
                    if (s.LootPack is Container sl) lootPacks.Add(sl);
                    if (s.ArtifactPack is Container sap) lootPacks.Add(sap);
                    if (s.CarvePack is Container scp) lootPacks.Add(scp);
                }
                else if (item is ChestItemSpawner cis && cis.LootPack is Container cislp)
                    lootPacks.Add(cislp);
            }
            #endregion

            int total_items = 0;
            var containers = new List<Container>();

            foreach (Item item in World.Items.Values)
            {
                if (item == null || item.Deleted || item is not Container cont)
                    continue;

                if (IsProtected(item))
                    continue;

                if (lootPacks.Contains(cont))
                    continue;

                containers.Add(cont);
            }

            var toDelete = new ArrayList();
            foreach (Container container in containers)
            {
                foreach (Item item in container.Items.ToArray())
                {
                    if (item != null && !item.Deleted)
                    {
                        toDelete.Add(new object[] { container, item });
                        total_items++;
                    }
                }
            }

            foreach (object o in toDelete)
            {
                var pair = (object[])o;
                var cont = pair[0] as Container;
                var item = pair[1] as Item;

                if (cont != null && item != null)
                {
                    cont.RemoveItem(item);
                    item.Delete();
                }
            }

            return total_items;
        }

        // ===================================================================
        // Remaining unchanged methods
        // ===================================================================

        public static int InitializeResourcePool()
        {
            int count = 0;
            string resourcePoolPath = Path.Combine(Core.ExeDirectory, "Saves", "ResourcePool");
            string transactionHistoriesPath = Path.Combine(Core.ExeDirectory, "Saves", "TransactionHistories");

            if (Directory.Exists(resourcePoolPath))
                RecursiveDelete(new DirectoryInfo(resourcePoolPath));

            Directory.CreateDirectory(resourcePoolPath);
            if (!Directory.Exists(transactionHistoriesPath))
                Directory.CreateDirectory(transactionHistoriesPath);

            if (ResourcePool.Consignments != null)
            {
                count = ResourcePool.Consignments.Count;
                ResourcePool.Consignments.Clear();
            }

            return count;
        }

        public static int ForceItemDecayNow()
        {
            int decayedCount = 0;
            var itemsToCheck = World.Items.Values.ToArray();

            foreach (Item item in itemsToCheck)
            {
                if (item == null || item.Deleted)
                    continue;

                bool canDecay = item.CanDelete() ||
                    (item.Decays && item.Parent == null && !item.GetItemBool(Item.ItemBoolTable.OnSpawner)
                     && !Utility.LinkedTo(item) && item.Map != Map.Internal);

                if (canDecay)
                {
                    try
                    {
                        item.Delete();
                        decayedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex, $"Failed to decay item {item.Serial}");
                    }
                }
            }
            return decayedCount;
        }

        public static bool WipeLeaderboards()
        {
            string path = "Saves/DataRecorder.bin";
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    return true;
                }
                catch { return false; }
            }
            return false;
        }

        public static int RepriceCustomHousingDeeds()
        {   // WipeDriver
            return RepriceCustomHousingDeeds("PreLaunch/world wipe", out _, out _, out _);
        }
        // One synchronization run of the StaticDeed listings of every staff-owned player vendor, on any facet
        //  (see PlayerVendor.SyncStaticDeedListings). Shared by WipeDriver, [RepriceHousingVendors, and PatchV251.
        //  Each run is always written to HousingVendorSync.log, with a summary. Returns the listings repriced.
        public static int RepriceCustomHousingDeeds(string source, out int vendors, out int examined, out int invalidDeleted)
        {
            List<PlayerVendor> list = new();
            foreach (Mobile m in World.Mobiles.Values)
                if (m is PlayerVendor pv && !pv.Deleted && pv.IsStaffOwned && pv.Backpack != null)
                    list.Add(pv);

            return PlayerVendor.SyncStaticDeedListings(source, list, out vendors, out examined, out invalidDeleted);
        }

        [Usage("RepriceHousingVendors")]
        [Description("Synchronize the custom house deeds on staff-owned player vendors with their blueprints.")]
        public static void RepriceHousingVendors_OnCommand(CommandEventArgs e)
        {
            int repriced = RepriceCustomHousingDeeds(string.Format("[RepriceHousingVendors by {0}", e.Mobile),
                out int vendors, out int examined, out int invalidDeleted);

            e.Mobile.SendMessage(0x35, $"{vendors} staff-owned player vendors scanned.");
            e.Mobile.SendMessage(0x35, $"{examined} custom house deeds (StaticDeed) examined.");
            e.Mobile.SendMessage(0x35, $"{repriced} custom house deed listings repriced.");
            e.Mobile.SendMessage(0x35, $"{invalidDeleted} invalid (PriceError) custom house deeds deleted.");
            e.Mobile.SendMessage(0x35, "Details are in HousingVendorSync.log.");
        }

        public static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists) return;

            foreach (var dir in baseDir.EnumerateDirectories())
                RecursiveDelete(dir);

            baseDir.Delete(true);
        }

        public static int DeactivateQuestGivers()
        {
            int count = 0;
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is QuestGiver qg)
                {
                    qg.NeedsReview = true;
                    count++;
                }
            }
            return count;
        }

        public static int DeleteDoppelgangerCorpses()
        {
            var toDelete = new List<Item>();
            foreach (Item item in World.Items.Values)
            {
                if (item is DoppelgangerCorpse corpse && !corpse.Deleted)
                    toDelete.Add(corpse);
            }

            foreach (Item item in toDelete)
                item.Delete();

            return toDelete.Count;
        }
    }
}