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

/* Scripts/Commands/OwnerTools.cs
 * CHANGELOG:
 *  10/21/26, Adam ([LoadJump treasure)
 *      [LoadJump treasure loads the jump list with every treasure map location (TreasureMap.Locations, the
 *      Angel Island Treasure.json database loaded at world load) in a new random order, each location once. Entries
 *      are Felucca WorldLocations at GetAverageZ, where treasure chests and overland encounters use them. Nothing is
 *      regenerated. [LoadJump without arguments still targets a spawner, champ engine, region control or runebook.
 *      LoadTreasureJump is the one treasure jump-list loader: [JumpTreasureMapLocations and [GenTreasureMapLocations
 *      (TreasureMap.Locations.cs) delegate to it.
 *  10/18/26, Adam ([RecalcBankBoxes)
 *      Add [RecalcBankBoxes (Administrator): recalculates the cached totals (items, weight, gold) of every player's
 *      bank box, online and offline, with the recursive Item.UpdateTotals, and reports the boxes processed and
 *      corrected. Also reports, per box, when the capacity slots in use (potions count one slot each, at any depth)
 *      differ from the item count players see, and which boxes are full. Bank contents are never changed; the
 *      LastAccessed stamps UpdateTotals sets are restored. Refused while freeze dry is enabled. RecalcBankBoxes.log.
 *  10/15/26, Adam ([RepriceHousingVendors)
 *      Add [RepriceHousingVendors (Owner): synchronizes the custom house deeds (StaticDeed) on every staff-owned
 *      player vendor with their blueprints and reports the vendors scanned, deeds examined, listings repriced, and
 *      invalid deeds deleted. RepriceCustomHousingDeeds (also run by [PreLaunch and PatchV251) no longer requires the
 *      vendor to be on Felucca, and every run is written to HousingVendorSync.log with a summary.
 *  10/13/26, Adam ([ClearContainer)
 *      Add [ClearContainer <newer than minutes> (GameMaster): deletes items in the targeted container created less
 *      than the given number of minutes ago (Item.Created). Recurses into older nested containers; a newer container
 *      is deleted with all of its contents. The targeted container itself is never deleted.
 *  3/16/23, Adam ([PreLaunch)
 *      Add 'Initialize the ResourcePool' to steps.
 *      We don't want resources from another shard appearing on a new shard.
 *  9/29/22, Adam (loadjump)
 *      normalize the Z when loading a jump list from a region controller. (otherwise we end up at like -128 Z)
 *  9/22/22, Adam (PreserveTeleporter & DeleteAllTeleporters)
 *      More teleporter work. Delete's not interesting, but Preserve is.
 *      PreserveTeleporter is used after Nuke.AnalyzeTeleporters. Nuke.AnalyzeTeleporters generated a Jump list of teleporters to investigate (loads the players Jump List.)
 *      While reviewing the teleporter, you'll call PreserveTeleporter to indicate it is a keeper.
 *      The output 'patch file' will be used by Patcher.cs to patch the shards with the appropriate patch file.
 *  9/15/22, Adam (FindBuggedMap)
 *      Added [FindBuggedMap to locate bugged map tiles.
 *      add [GetAverageZ
 *  9/14/22, Adam (LoadJump)
 *      [loadjump now accepts RegionControl as a target
 *  9/13/22, Adam
 *      Added a StackDepth command. Counts items at targeted X/Y
 *      Enhance DistanceToSqrt to accept one or more points, 2D or 3D (although, Z is always thrown away.)
 *  8/5/22, Adam (IsOwnerIP())
 *      Owner IP addresses are granted special server access:
 *      - Access to the Login Server from the server list
 *      - Character creation on this shard is automatically made an administrator.
 *      Note: we need this since we no longer have (working) IPExceptions and we need to be able to create an account
 *          there for individuals in special circumstances. 
 *  2/19/22, Adam (TileOverload)
 *      Looks for, and deletes excessive items on a single tile
 *  12/11/21 (ItemProfiler)
 *      Add an item profiler command to search out and identify excessive item generation.
 *	3/15/16, Adam
  *		Initial Version
 */

//using NAudio.Wave;
using Server.Accounting;
using Server.Diagnostics;
using Server.Engines;
using Server.Engines.BulkOrders;
using Server.Engines.ChampionSpawn;
using Server.Engines.Craft;
using Server.Engines.DataRecorder;
using Server.Gumps;
using Server.Items;
using Server.Items.Triggers;
using Server.Misc;
using Server.Mobiles;
using Server.Prompts;
using Server.SkillHandlers;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TextCopy;
using static Server.Core;
using static Server.Utility;

namespace Server.Commands
{
    public partial class OwnerTools
    {
        public static void Initialize()
        {
            // Ensure all players reflect the currently active RuleSet.
            // 
            // On startup, this aligns loaded world data with the configured shard.
            // On dynamic reconfiguration (e.g., SetConfig), this must be called explicitly
            // to reapply shard semantics (such as Mortality) to existing PlayerMobiles.
            ShardInit();

            if (!Core.RuleSets.MortalisStyleRules())
            {
                CommandSystem.Register("RemoveBackpackHue", AccessLevel.Player, new CommandEventHandler(RemoveBackpackHue_OnCommand));
                CommandSystem.Register("SaveBackpack", AccessLevel.Player, new CommandEventHandler(SaveBackpack_OnCommand));
                CommandSystem.Register("RestoreBackpack", AccessLevel.Player, new CommandEventHandler(RestoreBackpack_OnCommand));
            }
            CommandSystem.Register("Trammel", AccessLevel.GameMaster, new CommandEventHandler(GoTrammel_OnCommand));
            CommandSystem.Register("Felucca", AccessLevel.GameMaster, new CommandEventHandler(GoFelucca_OnCommand));
            CommandSystem.Register("RefreshBankBox", AccessLevel.Owner, new CommandEventHandler(RefreshBankBox_OnCommand));
            CommandSystem.Register("RecalcBankBoxes", AccessLevel.Administrator, new CommandEventHandler(RecalcBankBoxes_OnCommand));
            CommandSystem.Register("DistanceToSqrt", AccessLevel.GameMaster, new CommandEventHandler(DistanceToSqrt_OnCommand));
            CommandSystem.Register("LoadJump", AccessLevel.GameMaster, new CommandEventHandler(LoadJump_OnCommand));
            CommandSystem.Register("LoadMEOPJump", AccessLevel.GameMaster, new CommandEventHandler(LoadMEOPJump_OnCommand));
            CommandSystem.Register("PreLaunch", AccessLevel.Owner, new CommandEventHandler(PreLaunch_OnCommand));
            CommandSystem.Register("RepriceHousingVendors", AccessLevel.Owner, new CommandEventHandler(RepriceHousingVendors_OnCommand));
            CommandSystem.Register("Clobber", AccessLevel.Owner, new CommandEventHandler(Clobber_OnCommand));
            CommandSystem.Register("Monitor", AccessLevel.Owner, new CommandEventHandler(Monitor_OnCommand));
            CommandSystem.Register("ItemProfiler", AccessLevel.Owner, new CommandEventHandler(ItemProfiler_OnCommand));
            CommandSystem.Register("TileOverload", AccessLevel.Owner, new CommandEventHandler(TileOverload_OnCommand));
            CommandSystem.Register("SeedAccounts", AccessLevel.Owner, new CommandEventHandler(SeedAccounts_OnCommand));
            CommandSystem.Register("CanSpawnMobile", AccessLevel.Administrator, new CommandEventHandler(CanSpawnMobile_OnCommand));
            CommandSystem.Register("CanSpawnMobileZ", AccessLevel.Administrator, new CommandEventHandler(CanSpawnMobileZ_OnCommand));
            CommandSystem.Register("CanSpawnMobileT", AccessLevel.Owner, new CommandEventHandler(CanSpawnMobileT_OnCommand));
            CommandSystem.Register("FindBuggedMap", AccessLevel.Owner, new CommandEventHandler(FindBuggedMap_OnCommand));
            CommandSystem.Register("GetAverageZ", AccessLevel.Administrator, new CommandEventHandler(GetAverageZ_OnCommand));
            CommandSystem.Register("GetSurfaceTop", AccessLevel.Administrator, new CommandEventHandler(GetSurfaceTop_OnCommand));
            CommandSystem.Register("StackDepth", AccessLevel.Administrator, new CommandEventHandler(StackDepth_OnCommand));
            CommandSystem.Register("SectorBounds", AccessLevel.GameMaster, new CommandEventHandler(SectorBounds_OnCommand));
            CommandSystem.Register("GetSectorInfo", AccessLevel.GameMaster, new CommandEventHandler(GetSectorInfo_OnCommand));
            CommandSystem.Register("Colorize", AccessLevel.GameMaster, new CommandEventHandler(ColorizeRect_OnCommand));
            CommandSystem.Register("BoundingRect", AccessLevel.GameMaster, new CommandEventHandler(BoundingRect_OnCommand));
            CommandSystem.Register("ViewItems", AccessLevel.GameMaster, new CommandEventHandler(ViewItems_OnCommand));
            CommandSystem.Register("CopyItems", AccessLevel.GameMaster, new CommandEventHandler(CopyItems_OnCommand));
            CommandSystem.Register("LocationRecorder", AccessLevel.Owner, new CommandEventHandler(LocationRecorder_OnCommand));
            CommandSystem.Register("ConstructRect", AccessLevel.Owner, new CommandEventHandler(ConstructRect_OnCommand));
            CommandSystem.Register("GetNoto", AccessLevel.GameMaster, new CommandEventHandler(GetNoto_OnCommand));
            CommandSystem.Register("TimeToDie", AccessLevel.Owner, new CommandEventHandler(TimeToDie_OnCommand));
            CommandSystem.Register("WipeIPAddresses", AccessLevel.Administrator, new CommandEventHandler(WipeIPAddresses_OnCommand));
            CommandSystem.Register("WipeMachines", AccessLevel.Administrator, new CommandEventHandler(WipeMachines_OnCommand));
            CommandSystem.Register("SetAccess", AccessLevel.GameMaster, new CommandEventHandler(SetAccess_OnCommand));
            TargetCommands.Register(new MoveToIntMapStorageCommand());
            TargetCommands.Register(new MakeEventObjectPermanent());
            TargetCommands.Register(new ControlDebugger());
            CommandSystem.Register("LinkTransport", AccessLevel.GameMaster, new CommandEventHandler(LinkTransport_OnCommand));
            CommandSystem.Register("StretchRect", AccessLevel.GameMaster, new CommandEventHandler(StretchRect_OnCommand));
            CommandSystem.Register("CompileSpawnablePoints", AccessLevel.GameMaster, new CommandEventHandler(CompileSpawnablePoints_OnCommand));
            CommandSystem.Register("SwapStatics", AccessLevel.Seer, new CommandEventHandler(SwapStatics_OnCommand));
            CommandSystem.Register("AddRare", AccessLevel.Seer, new CommandEventHandler(AddRare_OnCommand));
            CommandSystem.Register("SetConfig", AccessLevel.Owner, new CommandEventHandler(SetConfig_OnCommand));

            TargetCommands.Register(new YankCommand());
            TargetCommands.Register(new PutCommand());

            CommandSystem.Register("RefactorSPDForConsistency", AccessLevel.Administrator,
                new CommandEventHandler(RefactorSPDForConsistency.RefactorSPDForConsistency_OnCommand));
            if (BulkOrderSystem.Enabled)
            {
                TargetCommands.Register(new TestBODValueCommand());
                CommandSystem.Register("TestBODValueAll", AccessLevel.Administrator, new CommandEventHandler(TestBODValueAll.TestBODValueAll_OnCommand));
                CommandSystem.Register("CheckSPDConsistency", AccessLevel.Administrator,
                    new CommandEventHandler(CheckSPDConsistency.CheckSPDConsistency_OnCommand));
            }

            CommandSystem.Register("ResolveReservedName", AccessLevel.Administrator, new CommandEventHandler(ResolveReservedName_OnCommand));

            CommandSystem.Register("AddInventory", AccessLevel.GameMaster, new CommandEventHandler(AddInventory_OnCommand));
            CommandSystem.Register("ClearContainer", AccessLevel.GameMaster, new CommandEventHandler(ClearContainer_OnCommand));
            CommandSystem.Register("RespawnMobileType", AccessLevel.GameMaster, new CommandEventHandler(RespawnMobileType_OnCommand));
            TargetCommands.Register(new ForensicEvaluationCommand());
            CommandSystem.Register("PeekRecorder", AccessLevel.Administrator, new CommandEventHandler(PeekRecorder_OnCommand));
            CommandSystem.Register("PatchDataRecorder", AccessLevel.Administrator, new CommandEventHandler(PatchDataRecorder_OnCommand));

            // we need a cross shard communication to make this work :/
            //CommandSystem.Register("GoLive", AccessLevel.Owner, new CommandEventHandler(GoLive_OnCommand));
        }

        #region GO LIVE
        //[Usage("GoLive [<shard name>]")]
        //[Description("Displays shard launch status or makes the specified shard live.")]
        //public static void GoLive_OnCommand(CommandEventArgs e)
        //{
        //    if (e.Arguments.Length == 0)
        //    {
        //        Server.Commands.CommandHandlers.DisplayShardList(e.Mobile);

        //        e.Mobile.SendMessage(
        //            "Lockdown: {0}",
        //            AccountHandler.LockdownLevel);

        //        e.Mobile.SendMessage("Usage: [GoLive <shard name>");
        //        return;
        //    }

        //    string shardName = string.Join(" ", e.Arguments);

        //    Server.Misc.ServerList.ShardEntry shard =
        //        Server.Misc.ServerList.FindShard(shardName);

        //    if (shard == null)
        //    {
        //        e.Mobile.SendMessage(
        //            "Unknown shard '{0}'. Use [GoLive for valid shard names.",
        //            shardName);

        //        return;
        //    }

        //    e.Mobile.SendMessage("GoLive: {0}", shard.Name);

        //    e.Mobile.SendMessage(
        //        "Visibility: {0}",
        //        shard.Visible ? "visible" : "hidden");

        //    e.Mobile.SendMessage(
        //        "Lockdown: {0}",
        //        AccountHandler.LockdownLevel);
        //}
        #endregion GO LIVE

        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }

        #region Data Recorder
        [Usage("PatchDataRecorder <player> <account> <domain> <amount> <Add|Subtract|Set>")]
        [Description("Patches a player's DataRecorder entry.")]
        public static void PatchDataRecorder_OnCommand(CommandEventArgs e)
        {
            string[] args = e.Arguments;

            if (args.Length != 5)
            {
                e.Mobile.SendMessage("Usage: PatchDataRecorder <player> <account> <domain> <amount> <Add|Subtract|Set>");
                return;
            }

            string playerName = args[0];
            string accountName = args[1];
            string domainName = args[2];
            string operation = args[4];

            if (!double.TryParse(args[3], out double amount))
            {
                e.Mobile.SendMessage("Invalid amount: {0}", args[3]);
                return;
            }

            if (!Enum.TryParse(domainName, true, out RecorderDomain domain))
            {
                e.Mobile.SendMessage("Unknown RecorderDomain: {0}", domainName);
                return;
            }

            PlayerMobile? player = null;

            foreach (PlayerMobile pm in PlayerMobile.Instances)
            {
                if (pm == null || pm.Deleted)
                    continue;

                if (!string.Equals(pm.Name, playerName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (pm.Account is not Account acct)
                    continue;

                if (!string.Equals(acct.Username, accountName, StringComparison.OrdinalIgnoreCase))
                    continue;

                player = pm;
                break;
            }

            if (player == null)
            {
                e.Mobile.SendMessage("Cannot find player '{0}' on account '{1}'.", playerName, accountName);
                return;
            }

            double oldValue = DataRecorder.Get(player, domain);
            double newValue = oldValue;

            switch (operation.ToLowerInvariant())
            {
                case "add":
                    DataRecorder.Add(player, domain, amount);
                    newValue = DataRecorder.Get(player, domain);
                    break;

                case "subtract":
                    DataRecorder.Add(player, domain, -amount);
                    newValue = DataRecorder.Get(player, domain);
                    break;

                case "set":
                    {
                        double delta = amount - oldValue;

                        if (delta != 0)
                            DataRecorder.Add(player, domain, delta);

                        newValue = DataRecorder.Get(player, domain);
                        break;
                    }

                default:
                    e.Mobile.SendMessage("Operation must be Add, Subtract, or Set.");
                    return;
            }

            e.Mobile.SendMessage(
                "Patched {0} ({1}) Domain={2} Operation={3} Old={4:N0} New={5:N0}",
                player.Name,
                accountName,
                domain,
                operation,
                oldValue,
                newValue);
        }

        [Usage("PeekRecorder <player name>")]
        [Description("Displays DataRecorder values for matching players.")]
        public static void PeekRecorder_OnCommand(CommandEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.ArgString))
            {
                e.Mobile.SendMessage("Usage: PeekRecorder <player name>");
                return;
            }

            string name = e.ArgString.Trim();
            List<PlayerMobile> matches = new List<PlayerMobile>();

            foreach (PlayerMobile pm in PlayerMobile.Instances)
            {
                if (pm == null || pm.Deleted || pm.Name == null)
                    continue;

                if (pm.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    matches.Add(pm);
            }

            if (matches.Count == 0)
            {
                e.Mobile.SendMessage("No matching players found for '{0}'.", name);
                return;
            }

            foreach (PlayerMobile pm in matches)
                DumpRecorderData(e.Mobile, pm);
        }

        private static void DumpRecorderData(Mobile from, PlayerMobile pm)
        {
            string acctName = "(no account)";

            if (pm.Account is Account acct)
                acctName = acct.Username;

            double lootGold = DataRecorder.Get(pm, RecorderDomain.PvMLootGold);
            double chestGold = DataRecorder.Get(pm, RecorderDomain.ChestGold);
            double theftGold = DataRecorder.Get(pm, RecorderDomain.TheftGold);
            double goldTotal = lootGold + chestGold + theftGold;

            double pvmPoints = DataRecorder.Get(pm, RecorderDomain.PvMPoints);

            from.SendMessage("Recorder data for {0}, account {1}:", pm.Name, acctName);
            from.SendMessage("  PvMLootGold: {0}", lootGold);
            from.SendMessage("  ChestGold: {0}", chestGold);
            from.SendMessage("  TheftGold: {0}", theftGold);
            from.SendMessage("  Gold Earned Total: {0}", goldTotal);
            from.SendMessage("  PvMPoints: {0}", pvmPoints);
        }
        #endregion Recorder

        #region Inventory Management
        private static readonly string VendorInventoryPath = Path.Combine("Data", "Seasons", "VendorInventory.json");

        [Usage("AddInventory <type>")]
        [Description("Adds an item type entry to Data\\Seasons\\VendorInventory.json if it is not already present.")]
        public static void AddInventory_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null)
                return;

            string? entry = e.ArgString != null ? e.ArgString.Trim() : null;

            string reason;
            if (AddVendorInventoryEntry(entry, out reason))
                e.Mobile.SendMessage("Inventory entry added: {0}", entry);
            else
                e.Mobile.SendMessage(reason);
        }

        [Usage("RespawnMobileType <type>")]
        [Description("Respawns all running spawners on your current map that spawn the specified mobile type.")]
        public static void RespawnMobileType_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null)
                return;

            string? typeName = e.ArgString != null ? e.ArgString.Trim() : null;

            Type? type = FindType(typeName);

            if (type == null)
            {
                e.Mobile.SendMessage("Unknown type: {0}", typeName);
                return;
            }

            if (!typeof(Mobile).IsAssignableFrom(type))
            {
                e.Mobile.SendMessage("{0} is not a Mobile type.", type.Name);
                return;
            }

            int count = RespawnMobileType(type, e.Mobile.Map);

            e.Mobile.SendMessage("Respawned {0} spawner{1} that spawn {2}.",
                count,
                count == 1 ? "" : "s",
                type.Name);
        }

        public static bool AddVendorInventoryEntry(string? entry, out string reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(entry))
            {
                reason = "Usage: AddInventory <type>";
                return false;
            }

            string typeName = GetEntryTypeName(entry);
            Type? type = FindType(typeName);

            if (type == null)
            {
                reason = string.Format("Unknown type: {0}", typeName);
                return false;
            }

            // get the proper case
            entry = type.Name;

            if (!typeof(Item).IsAssignableFrom(type))
            {
                reason = string.Format("{0} is not an Item type.", type.Name);
                return false;
            }

            List<string> entries = ReadSimpleJsonStringArray(VendorInventoryPath);

            if (entries.Any(s => string.Equals(s, entry, StringComparison.OrdinalIgnoreCase)))
            {
                reason = string.Format("Inventory entry already exists: {0}", entry);
                return false;
            }

            entries.Add(entry);
            entries.Sort(StringComparer.OrdinalIgnoreCase);

            WriteSimpleJsonStringArray(VendorInventoryPath, entries);

            reason = string.Format("Inventory entry added: {0}", entry);
            return true;
        }

        public static int RespawnMobileType(Type mobileType, Map map)
        {
            if (mobileType == null || map == null || map == Map.Internal)
                return 0;

            int respawned = 0;

            foreach (Spawner sp in World.Items.Values.OfType<Spawner>())
            {
                if (sp == null || sp.Deleted || sp.Map != map || !sp.Running)
                    continue;

                if (!Utility.SpawnerSpawns(sp, mobileType))
                    continue;

                sp.RemoveObjects();
                sp.Respawn();

                respawned++;
            }

            return respawned;
        }

        private static string GetEntryTypeName(string entry)
        {
            int index = entry.IndexOf(' ');

            if (index < 0)
                return entry;

            return entry.Substring(0, index).Trim();
        }

        private static Type? FindType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            Type? type = ScriptCompiler.FindTypeByName(typeName, ignoreCase: true);

            if (type != null)
                return type;

            return Type.GetType(typeName, false);
        }

        private static List<string> ReadSimpleJsonStringArray(string path)
        {
            List<string> list = new List<string>();

            if (!File.Exists(path))
                return list;

            string text = File.ReadAllText(path).Trim();

            if (text.Length == 0)
                return list;

            bool inString = false;
            bool escape = false;
            string current = "";

            foreach (char c in text)
            {
                if (!inString)
                {
                    if (c == '"')
                        inString = true;

                    continue;
                }

                if (escape)
                {
                    current += c;
                    escape = false;
                    continue;
                }

                if (c == '\\')
                {
                    escape = true;
                    continue;
                }

                if (c == '"')
                {
                    list.Add(current);
                    current = "";
                    inString = false;
                    continue;
                }

                current += c;
            }

            return list;
        }

        private static void WriteSimpleJsonStringArray(string path, List<string> entries)
        {
            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (StreamWriter writer = new StreamWriter(path, false))
            {
                writer.WriteLine("[");

                for (int i = 0; i < entries.Count; i++)
                {
                    writer.Write("  \"");
                    writer.Write(EscapeJsonString(entries[i]));
                    writer.Write("\"");

                    if (i + 1 < entries.Count)
                        writer.Write(",");

                    writer.WriteLine();
                }

                writer.WriteLine("]");
            }
        }

        private static string EscapeJsonString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
        #endregion Inventory Management

        #region ResolveReservedName

        [Usage("ResolveReservedName <name> <fromAccount|auto> <toAccount>")]
        [Description("Transfers a reserved name from one Seasons account to another. Specify 'auto' to resolve the current owner from the reserved-name database. Repairs live holders of that name and issues rename deeds where needed.")]
        public static void ResolveReservedName_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null)
                return;

            if (e.Length != 3)
            {
                e.Mobile.SendMessage("Usage: ResolveReservedName <name> <fromAccount> <toAccount>");
                return;
            }

            string? reservedName = e.GetString(0)?.Trim();
            string? fromUsername = e.GetString(1)?.Trim();
            string? toUsername = e.GetString(2)?.Trim();

            if (string.IsNullOrWhiteSpace(reservedName))
            {
                e.Mobile.SendMessage("Name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(fromUsername))
            {
                e.Mobile.SendMessage("fromAccount is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(toUsername))
            {
                e.Mobile.SendMessage("toAccount is required.");
                return;
            }

            if (string.Equals(reservedName, CharacterCreation.DefaultName, StringComparison.OrdinalIgnoreCase))
            {
                e.Mobile.SendMessage("You may not transfer the default character name.");
                return;
            }

            if (string.Equals(fromUsername, toUsername, StringComparison.OrdinalIgnoreCase))
            {
                e.Mobile.SendMessage("fromAccount and toAccount must be different.");
                return;
            }

            if (fromUsername.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                fromUsername = ReservedNames.LookupOwner(reservedName);

                if (string.IsNullOrWhiteSpace(fromUsername))
                {
                    e.Mobile.SendMessage("Name '{0}' is not currently reserved.", reservedName);
                    return;
                }

                e.Mobile.SendMessage("Resolved current owner to '{0}'.", fromUsername);
            }

            Account? fromAcct = Accounts.FindAccount(fromUsername);
            bool sourceAccountExists = (fromAcct != null);

            if (!sourceAccountExists)
            {
                e.Mobile.SendMessage("Warning: Source account '{0}' was not found. Continuing using the reserved-name database as authority.", fromUsername);
            }

            Account? toAcct = Accounts.FindAccount(toUsername);
            if (toAcct == null)
            {
                e.Mobile.SendMessage("Target account '{0}' was not found.", toUsername);
                return;
            }

            string? owner = ReservedNames.LookupOwner(reservedName);
            if (string.IsNullOrWhiteSpace(owner))
            {
                e.Mobile.SendMessage("Name '{0}' is not currently reserved.", reservedName);
                return;
            }

            if (!string.Equals(owner, fromUsername, StringComparison.OrdinalIgnoreCase))
            {
                e.Mobile.SendMessage("Name '{0}' is currently reserved by '{1}', not '{2}'.", reservedName, owner, fromUsername);
                return;
            }

            try
            {
                // Remove the right from the losing account and add it to the winning account.
                // The source account may no longer exist; the ReservedNames database is the authority here.
                RemoveReservedName(fromUsername, reservedName);
                ReservedNames.Add(toUsername, reservedName);

                int renamed = 0;
                int deedsIssued = 0;
                int deedFailures = 0;
                int displayNamesReset = 0;
                int displayNamesAwarded = 0;

                LogHelper logger = new LogHelper("reserved name manual reassignment.log", overwrite: false, sline: true);

                if (!sourceAccountExists)
                {
                    logger.Log(LogType.Text,
                        $"ResolveReservedName: source account '{fromUsername}' was not found; proceeding using ReservedNames database ownership.");
                }

                foreach (PlayerMobile pm in PlayerMobile.Instances)
                {
                    if (pm == null || pm.Deleted)
                        continue;

                    Account? acct = pm.Account as Account;
                    if (acct == null)
                        continue;

                    SeasonalAccount? sa = null;
                    if (Core.RuleSets.SeasonsRules())
                        sa = SeasonalAccounts.GetAccount(acct);

                    bool isLosingAccount = string.Equals(acct.Username, fromUsername, StringComparison.OrdinalIgnoreCase);
                    bool isWinningAccount = string.Equals(acct.Username, toUsername, StringComparison.OrdinalIgnoreCase);

                    // If a live character on the losing account currently holds the disputed name,
                    // force it to the default name and issue a deed.
                    if (isLosingAccount && string.Equals(pm.Name, reservedName, StringComparison.OrdinalIgnoreCase))
                    {
                        string oldName = pm.Name;
                        pm.Name = CharacterCreation.DefaultName;
                        renamed++;

                        PersonalNameChangeDeed deed = new PersonalNameChangeDeed();
                        deed.OwnerSerial = pm.Serial;

                        if (pm.Backpack != null && pm.Backpack.TryDropItem(pm, deed, false))
                        {
                            deedsIssued++;
                        }
                        else if (pm.BankBox != null && pm.BankBox.TryDropItem(pm, deed, false))
                        {
                            deedsIssued++;
                        }
                        else
                        {
                            deed.Delete();
                            deedFailures++;
                        }

                        logger.Log(LogType.Text,
                            $"ResolveReservedName: renamed '{oldName}' ({pm.Serial}) on losing account '{fromUsername}' to '{CharacterCreation.DefaultName}'.");
                    }

                    // If the losing account's DisplayName is the disputed name, clear it.
                    if (isLosingAccount && sa != null &&
                        string.Equals(sa.DisplayName, reservedName, StringComparison.OrdinalIgnoreCase))
                    {
                        sa.DisplayName = CharacterCreation.DefaultName;
                        displayNamesReset++;

                        logger.Log(LogType.Text,
                            $"ResolveReservedName: reset DisplayName '{reservedName}' on losing account '{fromUsername}' to '{CharacterCreation.DefaultName}'.");
                    }

                    // If the winning account's DisplayName is blank/default, award the disputed name.
                    if (isWinningAccount && sa != null &&
                        (string.IsNullOrWhiteSpace(sa.DisplayName) ||
                         string.Equals(sa.DisplayName, CharacterCreation.DefaultName, StringComparison.OrdinalIgnoreCase)))
                    {
                        sa.DisplayName = reservedName;
                        displayNamesAwarded++;

                        logger.Log(LogType.Text,
                            $"ResolveReservedName: awarded DisplayName '{reservedName}' to winning account '{toUsername}'.");
                    }
                }

                ReservedNames.Save(null);
                logger.Finish();

                e.Mobile.SendMessage("Reserved name '{0}' transferred from '{1}' to '{2}'.", reservedName, fromUsername, toUsername);
                e.Mobile.SendMessage("Renamed: {0}, Deeds issued: {1}, Deed failures: {2}, DisplayNames reset: {3}, DisplayNames awarded: {4}.",
                    renamed, deedsIssued, deedFailures, displayNamesReset, displayNamesAwarded);
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("ResolveReservedName failed: {0}", ex.Message);
            }
        }

        private static void RemoveReservedName(string username, string name)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username is required");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name is required");

            if (!ReservedNames.Database.TryGetValue(username, out List<string>? names) || names == null)
                throw new InvalidOperationException($"Account '{username}' does not own any reserved names.");

            string normalized = ReservedNames.Normalize(name);

            int index = -1;

            for (int i = 0; i < names.Count; i++)
            {
                if (ReservedNames.Normalize(names[i]) == normalized)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                throw new InvalidOperationException($"Account '{username}' does not own reserved name '{name}'.");

            names.RemoveAt(index);

            if (names.Count == 0)
                ReservedNames.Database.Remove(username);

            ReservedNames.Rebuild();
        }

        #endregion ResolveReservedName

        #region TestBODValueCommand
        public class TestBODValueCommand : BaseCommand
        {
            public TestBODValueCommand()
            {
                AccessLevel = AccessLevel.Administrator;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "TestBODValue" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "TestBODValue <count>";
                Description = "Simulates <count> random SmallBODs from a targeted BOD vendor and reports accurate gold values using current system logic.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is not BaseVendor vendor)
                {
                    LogFailure("That is not a vendor.");
                    return;
                }

                BulkOrderSystem system = vendor.BulkOrderSystem;
                if (system == null)
                {
                    LogFailure("This vendor does not support bulk orders.");
                    return;
                }

                if (e.Arguments.Length == 0 || !int.TryParse(e.Arguments[0], out int count) || count < 1 || count > 5000)
                {
                    LogFailure("Usage: TestBODValue <count> (1 - 5000 recommended)");
                    return;
                }

                e.Mobile.SendMessage($"Simulating {count} random SmallBODs from {vendor.Name}...");

                long totalRaw = 0;
                long totalFinal = 0;
                int validBODs = 0;

                for (int i = 0; i < count; i++)
                {
                    SmallBOD sbod = system.ConstructSBOD();
                    if (sbod == null)
                        continue;

                    sbod.Randomize(e.Mobile);

                    int points = system.ComputePoints(sbod);
                    int rawGold = system.ComputeGold(sbod, points);

                    double systemScalar = system.GoldScalar;
                    int finalGold = Math.Max(1, (int)Math.Round(rawGold * BulkOrderSystem.GlobalGoldScalar * systemScalar));

                    // Record Points (generic)
                    DataRecorder.Add(e.Mobile, RecorderDomain.BODPoints, points);

                    // Record Gold into correct profession bucket
                    RecorderDomain? bodGoldDomain = sbod switch
                    {
                        SmallSmithBOD => RecorderDomain.SmithBODGold,
                        SmallTailorBOD => RecorderDomain.TailorBODGold,
                        SmallCarpenterBOD => RecorderDomain.CarpenterBODGold,
                        _ => null
                    };

                    if (Core.Debug && bodGoldDomain == null)
                    {
                        string bodType = sbod?.GetType().Name ?? "null";
                        Utility.Monitor.WriteLine($"[TestBODValue] Unknown BOD type: {bodType} - gold NOT recorded", ConsoleColor.Red);
                        // Debug.Assert(false, $"Unknown BOD type: {bodType}");   // optional
                    }

                    if (bodGoldDomain != null)
                    {
                        DataRecorder.Add(e.Mobile, bodGoldDomain.Value, finalGold);
                    }

                    totalRaw += rawGold;
                    totalFinal += finalGold;
                    validBODs++;

                    sbod.Delete();
                }

                if (validBODs == 0)
                {
                    LogFailure("Failed to generate any valid BODs.");
                    return;
                }

                double avgRaw = (double)totalRaw / validBODs;
                double avgFinal = (double)totalFinal / validBODs;

                AddResponse($"Test completed: {validBODs} SmallBODs generated.");
                AddResponse($"Raw Average (pre-scalar) : {avgRaw:N2} gp");
                AddResponse($"Final Average (with scalars): {avgFinal:N2} gp");
                AddResponse($"GlobalGoldScalar = {BulkOrderSystem.GlobalGoldScalar:F3} | System Scalar = {system.GoldScalar:F3}");

                Console.WriteLine($"[TestBODValue] Vendor: {vendor.Name} | Count: {validBODs} | RawAvg: {avgRaw:F2} | FinalAvg: {avgFinal:F2}");
            }
        }
        public class TestBODValueAll
        {
            public static void TestBODValueAll_OnCommand(CommandEventArgs e)
            {
                if (e.Arguments.Length == 0 || !int.TryParse(e.Arguments[0], out int count) || count < 1 || count > 2000)
                {
                    e.Mobile.SendMessage("Usage: TestBODValueAll <count> (1 - 2000 recommended)");
                    return;
                }

                e.Mobile.SendMessage($"Testing {count} random SmallBODs for each available vendor...");

                // Find one vendor of each craft type
                BaseVendor? blacksmith = FindVendor(typeof(Blacksmith));
                BaseVendor? carpenter = FindVendor(typeof(Carpenter));
                BaseVendor? tailor = FindVendor(typeof(Tailor));

                if (blacksmith == null) e.Mobile.SendMessage("Warning: No Blacksmith vendor found.");
                if (carpenter == null) e.Mobile.SendMessage("Warning: No Carpenter vendor found.");
                if (tailor == null) e.Mobile.SendMessage("Warning: No Tailor vendor found.");

                if (blacksmith == null && carpenter == null && tailor == null)
                {
                    e.Mobile.SendMessage("Error: No relevant BOD vendors found in the world.");
                    return;
                }

                TestVendor(e.Mobile, blacksmith, "Blacksmith", count);
                TestVendor(e.Mobile, carpenter, "Carpenter", count);
                TestVendor(e.Mobile, tailor, "Tailor", count);
            }

            private static void TestVendor(Mobile from, BaseVendor? vendor, string name, int count)
            {
                if (vendor == null)
                    return;

                BulkOrderSystem system = vendor.BulkOrderSystem;
                if (system == null)
                    return;

                long totalRaw = 0;
                long totalFinal = 0;
                int validBODs = 0;

                for (int i = 0; i < count; i++)
                {
                    SmallBOD sbod = system.ConstructSBOD();
                    if (sbod == null) continue;

                    sbod.Randomize(from);

                    int points = system.ComputePoints(sbod);
                    int rawGold = system.ComputeGold(sbod, points);

                    double systemScalar = system.GoldScalar;
                    int finalGold = Math.Max(1, (int)Math.Round(rawGold * BulkOrderSystem.GlobalGoldScalar * systemScalar));

                    totalRaw += rawGold;
                    totalFinal += finalGold;
                    validBODs++;

                    sbod.Delete();
                }

                if (validBODs == 0)
                {
                    from.SendMessage($"{name}: Failed to generate any valid BODs.");
                    return;
                }

                double avgRaw = (double)totalRaw / validBODs;
                double avgFinal = (double)totalFinal / validBODs;

                from.SendMessage($"{name}:");
                from.SendMessage($"  Raw gold:    {avgRaw:N2} gp");
                from.SendMessage($"  Scaled gold: {avgFinal:N2} gp");
            }

            /// <summary>
            /// Finds the first vendor of the specified type in the world.
            /// </summary>
            private static BaseVendor? FindVendor(Type vendorType)
            {
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m.GetType() == vendorType && m is BaseVendor vendor)
                        return vendor;
                }
                return null;
            }
        }
        #endregion TestBODValueCommand

        #region Refactor Standard Pricing Disctionary For Consistency
        public class CheckSPDConsistency
        {
            public static void CheckSPDConsistency_OnCommand(CommandEventArgs e)
            {
                string logFile = "Logs/SPD_Consistency_Errors.log";

                e.Mobile.SendMessage("Running SPD consistency check...");
                e.Mobile.SendMessage($"Results will be logged to: {logFile}");

                int totalChecked = 0;
                int problemsFound = 0;
                const double WarningRatio = 1.75;   // Adjust as needed (1.75x = 75% markup)

                using (StreamWriter log = new StreamWriter(logFile, false))
                {
                    log.WriteLine("=== SPD Consistency Check ===");
                    log.WriteLine($"Run at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    log.WriteLine($"Warning if Sell Price > {WarningRatio:F2}x material cost");
                    log.WriteLine("============================================================");
                    log.WriteLine();

                    foreach (var kvp in BaseVendor.StandardPricingDictionary)
                    {
                        Type itemType = kvp.Key;

                        int vendorPays = GetPricing(itemType, StandardPricingType.Sell);
                        if (vendorPays <= 0)
                            continue;

                        totalChecked++;

                        if (!IsCraftable(itemType))
                            continue;

                        double calculatedCost = CalculateComponentCost(itemType);

                        if (calculatedCost <= 0)
                            continue;

                        double ratio = vendorPays / calculatedCost;

                        if (ratio > WarningRatio)
                        {
                            problemsFound++;

                            string line = $"[WARNING] {itemType.Name}: Sell = {vendorPays} | Material Cost ≈ {calculatedCost:F1} | Ratio = {ratio:F2}x";

                            e.Mobile.SendMessage(0x25, line);
                            log.WriteLine(line);

                            // Extra detail for fixing
                            log.WriteLine($"   → ItemType: {itemType.FullName}");
                            log.WriteLine($"   → VendorPays (Sell): {vendorPays}");
                            log.WriteLine($"   → Calculated Material Cost: {calculatedCost:F1}");
                            log.WriteLine("   ---");
                        }
                    }

                    log.WriteLine();
                    log.WriteLine($"Summary: Checked {totalChecked} items | Problems found: {problemsFound}");
                }

                e.Mobile.SendMessage("Check complete.");
                e.Mobile.SendMessage($"{problemsFound} potential arbitrage issues logged to {logFile}");
            }

            private static int GetPricing(Type type, StandardPricingType pricingType)
            {
                if (BaseVendor.StandardPricingDictionary.TryGetValue(type, out var list))
                {
                    foreach (var entry in list)
                    {
                        if (entry.Key == pricingType)
                            return entry.Value;
                    }
                }
                return 0;
            }

            private static bool IsCraftable(Type itemType)
            {
                foreach (CraftSystem cs in CraftSystem.Instances)
                {
                    if (cs.CraftItems.SearchFor(itemType) != null)
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Recursively calculates total material cost using VendorPays (Sell prices).
            /// Pure component cost — no labor or exceptional bonuses.
            /// </summary>
            private static double CalculateComponentCost(Type itemType)
            {
                CraftItem? craftItem = null;

                foreach (CraftSystem cs in CraftSystem.Instances)
                {
                    craftItem = cs.CraftItems.SearchFor(itemType);
                    if (craftItem != null)
                        break;
                }

                if (craftItem == null)
                    return 0.0;

                double totalCost = 0.0;
                HashSet<Type> visited = new HashSet<Type>();

                foreach (CraftRes res in craftItem.Resources)
                {
                    Type resType = res.ItemType;

                    int unitCost = GetPricing(resType, StandardPricingType.Sell);

                    if (unitCost > 0)
                    {
                        totalCost += unitCost * res.Amount;
                    }
                    else if (IsCraftable(resType) && !visited.Contains(resType))
                    {
                        visited.Add(resType);
                        totalCost += CalculateComponentCost(resType) * res.Amount;
                        visited.Remove(resType);
                    }
                }

                return totalCost;
            }
        }
        public class RefactorSPDForConsistency
        {
            public static void RefactorSPDForConsistency_OnCommand(CommandEventArgs e)
            {
                string filePath = Path.Combine(Core.ExeDirectory, "data", "vendor pricing.txt");
                string backupPath = Path.Combine(Core.ExeDirectory, "data", "vendor pricing.bak");

                if (!File.Exists(filePath))
                {
                    e.Mobile.SendMessage(0x25, $"Error: File not found: {filePath}");
                    return;
                }

                e.Mobile.SendMessage("Analyzing SPD for consistency (max 1.4x markup)...");

                const double MaxRatio = 1.4;
                List<string> newLines = new List<string>();
                int changesMade = 0;

                try
                {
                    string[] lines = File.ReadAllLines(filePath);

                    foreach (string originalLine in lines)
                    {
                        string trimmed = originalLine.Trim();

                        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                        {
                            newLines.Add(originalLine);
                            continue;
                        }

                        if (!trimmed.Contains(": sell:"))
                        {
                            newLines.Add(originalLine);
                            continue;
                        }

                        int colonIndex = trimmed.IndexOf(':');
                        string typeStr = trimmed.Substring(0, colonIndex).Trim();
                        string sellPart = trimmed.Substring(colonIndex + 1).Trim();

                        if (!typeStr.StartsWith("Server.Items."))
                        {
                            newLines.Add(originalLine);
                            continue;
                        }

                        Type itemType = ScriptCompiler.FindTypeByFullName(typeStr);
                        if (itemType == null)
                        {
                            newLines.Add(originalLine);
                            continue;
                        }

                        int currentSell = Utility.ToInt32(sellPart.Replace("sell:", "").Trim());

                        if (currentSell <= 0 || currentSell == BaseVendor.VendorPaysFailsafe)
                        {
                            newLines.Add(originalLine);
                            continue;
                        }

                        if (!HasMeaningfulCraftRecipe(itemType))
                        {
                            newLines.Add(originalLine);
                            continue;
                        }

                        double materialCost = CalculateComponentCost(itemType);

                        if (materialCost < 1.0)
                        {
                            newLines.Add(originalLine);
                            continue;
                        }

                        double ratio = currentSell / materialCost;

                        if (ratio > MaxRatio)
                        {
                            int newSell = (int)Math.Max(1, Math.Round(materialCost * MaxRatio));

                            // NEW CHECK: Only reprice if the new value is actually different
                            if (newSell != currentSell)
                            {
                                string newLine = $"{typeStr}: sell: {newSell}";
                                string comment = $"# Repriced from {currentSell} to {newSell} (1.4x material cost) to prevent arbitrage - {DateTime.UtcNow:yyyy-MM-dd}";

                                newLines.Add(comment);
                                newLines.Add(newLine);

                                changesMade++;
                                e.Mobile.SendMessage(0x35, $"Updated {itemType.Name}: {currentSell} → {newSell}");
                            }
                            else
                            {
                                newLines.Add(originalLine);
                            }
                        }
                        else
                        {
                            newLines.Add(originalLine);
                        }
                    }

                    if (changesMade > 0)
                    {
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);

                        File.Move(filePath, backupPath);
                        File.WriteAllLines(filePath, newLines);

                        e.Mobile.SendMessage(0x3B2, $"Success: {changesMade} items were actually repriced.");
                        e.Mobile.SendMessage("Original file backed up as 'vendor pricing.bak'.");
                    }
                    else
                    {
                        e.Mobile.SendMessage(0x3B2, "No changes needed. SPD is consistent within 1.4x ratio.");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    e.Mobile.SendMessage(0x25, "Error during SPD refactor.");
                }
            }

            private static bool HasMeaningfulCraftRecipe(Type itemType)
            {
                foreach (CraftSystem cs in CraftSystem.Instances)
                {
                    CraftItem? ci = cs.CraftItems.SearchFor(itemType);
                    if (ci != null && ci.Resources != null && ci.Resources.Count > 0)
                        return true;
                }
                return false;
            }

            private static double CalculateComponentCost(Type itemType)
            {
                CraftItem? craftItem = null;

                foreach (CraftSystem cs in CraftSystem.Instances)
                {
                    craftItem = cs.CraftItems.SearchFor(itemType);
                    if (craftItem != null)
                        break;
                }

                if (craftItem == null || craftItem.Resources == null)
                    return 0.0;

                double totalCost = 0.0;
                HashSet<Type> visited = new HashSet<Type>();

                foreach (CraftRes res in craftItem.Resources)
                {
                    Type resType = res.ItemType;
                    int unitCost = GetPricing(resType, StandardPricingType.Sell);

                    if (unitCost > 0)
                    {
                        totalCost += unitCost * res.Amount;
                    }
                    else if (HasMeaningfulCraftRecipe(resType) && !visited.Contains(resType))
                    {
                        visited.Add(resType);
                        totalCost += CalculateComponentCost(resType) * res.Amount;
                        visited.Remove(resType);
                    }
                }

                return totalCost;
            }

            private static int GetPricing(Type type, StandardPricingType pricingType)
            {
                if (BaseVendor.StandardPricingDictionary.TryGetValue(type, out var list))
                {
                    foreach (var entry in list)
                    {
                        if (entry.Key == pricingType)
                            return entry.Value;
                    }
                }
                return BaseVendor.VendorPaysFailsafe;
            }
        }
        #endregion Refactor Standard Pricing Disctionary For Consistency

        #region Add Rare
        private static void AddRare_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the item you would like to log as a rare...");
            e.Mobile.Target = new AddRareTarget(null);
        }
        public class AddRareTarget : Target
        {
            public AddRareTarget(string? command)
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                int id = 0;
                string name = string.Empty;
                if (Utility.ObjectInfo(target, ref name, ref id))
                {
                    LogHelper logger = new LogHelper("AddRare.log", overwrite: false, sline: true, quiet: true);
                    string text = string.Format($"new CrownSterlingReward(itemO: {id}, baseCost: 5000, 0,label: null),  // {name}");
                    logger.Log(text);
                    logger.Finish();
                    from.SendMessage(text);

                    // cleanup duplicates
                    string[] lines = File.ReadAllLines(Path.Combine(Core.LogsDirectory, "AddRare.log"));
                    lines = lines.Distinct().ToArray();
                    File.WriteAllLines(Path.Combine(Core.LogsDirectory, "AddRare.log"), lines);
                }
                else
                {
                    from.SendMessage("That is not an item.");
                    return;
                }
            }
        }
        #endregion Add Rare
        #region Swap Statics
        private static void SwapStatics_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the top tile or yourself if you are standing on the tile...");
            e.Mobile.Target = new SwapStaticsTarget(null);
        }
        public class SwapStaticsTarget : Target
        {
            public SwapStaticsTarget(string? command)
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Static || target is Mobile)
                {
                    IEntity? top = target as IEntity;
                    List<Item> items = new();
                    OwnerTools.GetStack(top.X, top.Y, top.Map, items: items);
                    items.RemoveAll(x => Math.Abs(x.Z - top.Z) > 1);
                    items.RemoveAll(x => x is not Static);

                    if (items.Count == 2)
                    {
                        Static lower = new Static(1/*top.ItemID*/);
                        Static higher = new Static(1/*top.ItemID*/);

                        if (items[0].Serial > items[1].Serial)
                        {
                            Utility.CopyProperties(items[0], lower);
                            Utility.CopyProperties(items[1], higher);
                            lower.MoveToWorld(items[0].Location);
                            higher.MoveToWorld(items[1].Location);
                            items[0].Delete();
                            items[1].Delete();
                        }
                        else
                        {
                            Utility.CopyProperties(items[0], higher);
                            Utility.CopyProperties(items[1], lower);
                            higher.MoveToWorld(items[0].Location);
                            lower.MoveToWorld(items[1].Location);
                            items[0].MoveToIntStorage();
                            items[1].MoveToIntStorage();
                            items[0].Delete();
                            items[1].Delete();
                        }

                        from.SendEverything();
                        from.SendMessage($"Swapped {items.Count} static tiles.");
                    }
                    else
                        from.SendMessage($"Can't swap {items.Count} static tiles.");
                }
                else
                {
                    from.SendMessage("That is not a static tile.");
                    return;
                }
            }
        }
        #endregion Swap Statics
        #region Clear Container
        [Usage("ClearContainer <newer than minutes>")]
        [Description("Deletes the items in a targeted container that were created less than the given number of minutes ago.")]
        private static void ClearContainer_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length != 1 || !int.TryParse(e.Arguments[0], out int minutes) || minutes < 0)
            {
                e.Mobile.SendMessage("Usage: ClearContainer <newer than minutes>");
                return;
            }

            e.Mobile.SendMessage("Target the container you would like to clear of items newer than {0} minutes...", minutes);
            e.Mobile.Target = new ClearContainerTarget(minutes);
        }
        public class ClearContainerTarget : Target
        {
            private int m_Minutes;
            public ClearContainerTarget(int minutes)
                : base(-1, false, TargetFlags.None)
            {
                m_Minutes = minutes;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is not Container container)
                {
                    from.SendMessage("That is not a container.");
                    return;
                }

                // newer than the cutoff: created less than m_Minutes ago. Items created exactly at the cutoff, or
                //  with no creation time (DateTime.MinValue), are kept.
                DateTime cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(m_Minutes);
                int deleted = ClearNewerThan(container, cutoff);
                container.UpdateAllTotals();

                CommandLogging.WriteLine(from, "{0} {1} cleared {2} items newer than {3} minutes from {4}", from.AccessLevel, CommandLogging.Format(from), deleted, m_Minutes, CommandLogging.Format(container));
                from.SendMessage("Deleted {0} item{1} newer than {2} minutes.", deleted, deleted == 1 ? "" : "s", m_Minutes);
            }

            private static int ClearNewerThan(Container container, DateTime cutoff)
            {
                int deleted = 0;

                // snapshot: deleting an item removes it from container.Items
                foreach (Item item in new List<Item>(container.Items))
                {
                    if (item.Deleted)
                        continue;

                    if (item.Created > cutoff)
                    {   // a new container goes with everything in it (Delete() cascades), old contents included
                        List<Item> doomed = new() { item };
                        if (item is Container) doomed.AddRange(item.GetDeepItems().Cast<Item>());
                        item.Delete();
                        deleted += doomed.Count(i => i.Deleted);
                    }
                    else if (item is Container child)
                        deleted += ClearNewerThan(child, cutoff);
                }

                return deleted;
            }
        }
        #endregion Clear Container
        #region Compile Spawnable Points
        [Usage("CompileSpawnablePoints")]
        [Description("CompileSpawnablePoints")]
        public static void CompileSpawnablePoints_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the FDRegionControl you wish to compile.");
            e.Mobile.Target = new CompileSpawnablePointsTarget(null);
        }
        public class CompileSpawnablePointsTarget : Target
        {
            public CompileSpawnablePointsTarget(object? o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                List<Point2D> points = new List<Point2D>();
                List<Point2D> unique = new List<Point2D>();
                if (targeted is FDFile.FDRegionControl rc)
                {
                    Console.WriteLine("Spawnable Points Compiling...");
                    Utility.TimeCheck tc = new Utility.TimeCheck();
                    tc.Start();
                    List<Rectangle2D> rects = new();
                    foreach (var rect in rc.CustomRegion.Coords)
                        rects.Add(new Rectangle2D(rect.Start, rect.End));

                    List<Point2D> temp = new List<Point2D>();
                    foreach (Rectangle2D rectangle in rects)
                        foreach (var point in rectangle.PointsInRect2D())
                            temp.Add(point);

                    unique = temp.Distinct().ToList();

                    int unusable = 0;
                    foreach (var point in unique)
                        if (Utility.CanSpawnLandMobile(rc.Map, new Point3D(point.X, point.Y, rc.Map.GetAverageZ(point.X, point.Y))))
                            points.Add(point);
                        else
                            unusable++;

                    SaveSpawnablePoints(rc.CustomRegion.Name, points);
                    tc.End();
                    Console.WriteLine("Compiled {0} points with {1} usable in {2}", points.Count + unusable, points.Count, tc.TimeTaken);
                }
                else
                    from.SendMessage("That is not an FDRegionControl.");
            }

            public static void SaveSpawnablePoints(string filename, List<Point2D> points)
            {
                Console.WriteLine(string.Format("Saves/{0}.bin Saving...", filename));
                try
                {
                    BinaryFileWriter writer = new BinaryFileWriter(string.Format("Saves/{0}.bin", filename), true);

                    writer.Write(points.Count);

                    foreach (var point in points)
                        writer.Write(point);

                    writer.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Error writing Saves/{0}.bin", filename));
                    Console.WriteLine(ex.ToString());
                    return;
                }

                Console.WriteLine(string.Format("Done writing Saves/{0}.bin", filename));
            }
        }
        #endregion Compile Spawnable Points
        #region StretchRect
        [Usage("StretchRect")]
        [Description("StretchRect")]
        public static void StretchRect_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new StretchRectTarget(null);
        }
        public class StretchRectTarget : Target
        {
            public StretchRectTarget(object? o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                StaticTarget? st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;

                int landZ = 0, landAvg = 0, landTop = 0;
                from.Map.GetAverageZ(loc.X, loc.Y, ref landZ, ref landAvg, ref landTop);
                if (loc != Point3D.Zero)
                {
                    Rectangle2D rect = new Rectangle2D(loc, new Point2D(loc.X + 1, loc.Y + 1));
                    rect = new SpawnableRect(rect).MaximumSpawnableRect(from.Map, 128, from.Map.GetAverageZ(rect.X, rect.Y));
                    from.SendMessage("Your max spawnable rect is {0}.", rect);
                    string text = string.Format("({0} {1}, {2}, {3})", rect.Start.X, rect.Start.Y, rect.End.X, rect.End.Y);
                    from.SendMessage(text);
                    ClipboardService.SetText(text);
                    Server.Gumps.EditAreaGump.FlashArea(from, rect, from.Map);
                }
                else
                    from.SendMessage("Can't grow a rect here.");
            }
        }
        #endregion StretchRect
        [Usage("LinkTransport")]
        [Description("Link a teleporter's MapDest and PointDest to <target>.")]
        public static void LinkTransport_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the spawner you would like to link...");
            e.Mobile.Target = new LinkTransportFirstTarget();
        }
        private class LinkTransportFirstTarget : Target
        {
            public LinkTransportFirstTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Teleporter? transport = targeted as Teleporter;

                if (transport == null)
                {
                    from.SendMessage("You must select a teleporter or other transport object");
                }
                else
                {
                    from.SendMessage("Target the location you would like to link to...");
                    from.Target = new LinkTransportSecondTarget(transport);
                }
            }
        }

        private class LinkTransportSecondTarget : Target
        {
            private Teleporter m_teleporter;

            public LinkTransportSecondTarget(Teleporter transport)
                : base(-1, false, TargetFlags.None)
            {
                m_teleporter = transport;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                IPoint3D? px = targeted as IPoint3D;

                if (px == null)
                {
                    from.SendMessage("Unable to resolve that location.");
                    return;
                }

                if (px is Item item)
                    px = item.GetWorldTop();
                else if (px is Mobile mobile)
                    px = mobile.Location;

                m_teleporter.PointDest = new Point3D(px);
                m_teleporter.MapDest = from.Map;
                from.SendMessage("Link established: {0} ({1}).", m_teleporter.PointDest, m_teleporter.MapDest);
            }
        }
        private class MakeEventObjectPermanent : BaseCommand
        {
            public MakeEventObjectPermanent()
            {
                AccessLevel = AccessLevel.Seer;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "MakeEventObjectPermanent", "MEOP" };
                ObjectTypes = ObjectTypes.Both;
                Usage = "MakeEventObjectPermanent <target> [-off]";
                Description = "Convert an Event Object (spawner, teleporter, etc.) to a Permanent Event Object.";
            }
            public override void Execute(CommandEventArgs e, object obj)
            {
                bool activate = true;
                if (e.GetString(0) == "-off")
                    activate = false;

                string reason;
                if (Utility.Meop(e.Mobile, obj, activate: activate, out reason))
                    e.Mobile.SendMessage(reason);
                else
                    LogFailure(reason);
            }
        }
        #region ControlDebugger
        private class ControlDebugger : BaseCommand
        {
            public ControlDebugger()
            {
                AccessLevel = AccessLevel.Seer;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "ControlDebugger", "CD" };
                ObjectTypes = ObjectTypes.Both;
                Usage = "ControlDebugger <target> [-link|-else <1|2|...>]";
                Description = "Display the links out of a controller and for the next N controllers.";
            }
            [Flags]
            public enum CDCommands
            {
                None = 0x00,
                Link = 0x01,
                Else = 0x02,
                Highlight = 0x04,
                Depth = 0x08,
                Label = 0x10,
                Jump = 0x20,
            }
            public class CDParms
            {
                private CDCommands m_Command;
                public CDCommands Command { get { return m_Command; } set { m_Command = value; } }
                private int m_Depth;
                public int Depth { get { return m_Depth; } set { m_Depth = value; } }
                public CDParms(CDCommands command, int depth)
                {
                    m_Command = command;
                    m_Depth = depth;
                }
            }
            public override void Execute(CommandEventArgs e, object obj)
            {
                string usage = "Usage: [cd -link|-else|-highlight|-depth <N>|-Label|-jump]";
                try
                {
                    if (obj is ITrigger || obj is TriggerRelay)
                    {
                        CDParms parms = new CDParms(CDCommands.None, 0);
                        Item? trigger = obj as Item;

                        string argString;
                        if (e.HasCommand("-link", out argString))
                        {
                            parms.Command |= CDCommands.Link;
                        }
                        if (e.HasCommand("-else", out argString))
                        {
                            parms.Command |= CDCommands.Else;
                        }
                        if (e.HasCommand("-highlight", out argString))
                        {
                            parms.Command |= CDCommands.Highlight;
                        }
                        if (e.HasCommand("-depth", out argString))
                        {
                            parms.Command |= CDCommands.Depth;
                            if (!uint.TryParse(argString, out uint value))
                                throw new ArgumentException("-depth <N>");
                            parms.Depth = int.Parse(argString);
                        }
                        if (e.HasCommand("-Label", out argString))
                        {
                            parms.Command |= CDCommands.Label;
                        }
                        if (e.HasCommand("-jump", out argString))
                        {
                            parms.Command |= CDCommands.Jump;
                        }

                        if (parms.Command == CDCommands.None)
                            throw new ArgumentException(usage);

                        object[]? args = null;
                        if (parms.Command.HasFlag(CDCommands.Depth))
                            args = new object[1] { parms.Depth };

                        ShowLinks(from: e.Mobile, trigger: trigger, command: parms.Command, args: args);
                    }
                    else
                        throw new ArgumentException("Not a triggerable object"); // LogFailure("Not a triggerable object");
                }
                catch (Exception ex)
                {
                    LogFailure(ex.Message);
                    if (ex.Message != usage)
                        e.Mobile.SendMessage(usage);
                }
            }
            private static void ShowLinks(Mobile from, Item trigger, CDCommands command, object[] args)
            {
                List<string> lines = new();

                int count = 128;                        // default depth if no depth is specified
                if (command.HasFlag(CDCommands.Depth))
                    count = (int)args[0];               // how deep we should look
                int depth = 0;                            // recursion protection

                // add the target. If it's a trigger relay, remove it
                List<Item> list = new() { trigger };
                if (trigger is TriggerRelay)
                    list.Clear();

                if (command.HasFlag(CDCommands.Jump) && from is PlayerMobile pm)
                {
                    pm.JumpIndex = 0;
                    pm.JumpList = new ArrayList();
                }

                // we will call ProcessLinks once for each traversal. This allows us to colorize based on path
                if (command.HasFlag(CDCommands.Link | CDCommands.Else))
                {
                    List<Item> LinkList = new(list);
                    List<Item> ElseList = new(list);
                    ProcessLinks(trigger, command & ~CDCommands.Else, LinkList, ref depth);
                    ProcessDisplay(from, command & ~CDCommands.Else, LinkList);
                    lines.AddRange(AssembleOutput(command & ~CDCommands.Else, LinkList));
                    depth = 0;                          // reset for a fresh traversal
                    ProcessLinks(trigger, command & ~CDCommands.Link, ElseList, ref depth);
                    ProcessDisplay(from, command & ~CDCommands.Link, ElseList);
                    lines.AddRange(AssembleOutput(command & ~CDCommands.Link, ElseList));
                }
                else
                {
                    ProcessLinks(trigger, command, list, ref depth);
                    ProcessDisplay(from, command, list);
                    lines.AddRange(AssembleOutput(command, list));
                }

                lines = lines.Take(count).ToList();     // trim to the requested depth

                // show the user
                from.CloseGump(typeof(ControlRelationshipVisualizerGump));
                from.SendGump(new ControlRelationshipVisualizerGump(from, lines));

                // tell the user about their jumplist
                if (command.HasFlag(CDCommands.Jump) && from is PlayerMobile staff && staff.JumpList.Count > 0)
                {
                    staff.SendMessage($"Your jumplist has been loaded with {staff.JumpList.Count}");
                    staff.SendMessage("Use the [next command to move to each object.");
                }
            }
            private static List<string> AssembleOutput(CDCommands command, List<Item> list)
            {
                List<string> lines = new();
                string text = string.Empty;
                for (int ix = 0; ix < list.Count; ix++)
                {
                    text += string.Format($"{list[ix].GetType().Name}({list[ix].Serial}) => ");
                    if (CheckEnd(command, list[ix]))
                    {
                        text += "(end)";            // add a tail node
                        //from.SendMessage(text);   // useful for debugging
                        lines.Add(text);            // another complete chain
                        text = string.Empty;        // fresh
                    }
                }
                return lines;
            }
            private static void ProcessDisplay(Mobile to, CDCommands command, List<Item> list)
            {
                // Highlight
                int blue = Utility.RandomSpecialHue(Utility.ColorSelect.Blue, Utility.GetStableHashCode(0));
                int gold = Utility.RandomSpecialHue(Utility.ColorSelect.Gold, Utility.GetStableHashCode(0));
                if (command.HasFlag(CDCommands.Highlight))
                    foreach (Item item in list)
                        item.Blink(command.HasFlag(CDCommands.Link) ? blue : gold);

                // label
                if (command.HasFlag(CDCommands.Label))
                    foreach (Item item in list)
                    {
                        int hue = Utility.RandomSpecialHue(item.GetType().ToString());
                        // LabelTo(from, '(' + Serial.ToString() + ')');
                        item.LabelToHued(to, item.GetType().Name, hue);
                        item.LabelToHued(to, '(' + item.Serial.ToString() + ')', hue);
                    }

                // jumplist
                if (command.HasFlag(CDCommands.Jump) && to is PlayerMobile pm)
                    foreach (Item item in list)
                        pm.JumpList.Add(item);
            }
            private static bool CheckEnd(CDCommands command, Item item)
            {
                if (item is TriggerRelay)
                {
                    return GetRelayLinks(item).Count == 0;
                }
                else
                {
                    string? name = null;
                    if (command.HasFlag(CDCommands.Link))
                        name = "Link";
                    else if (command.HasFlag(CDCommands.Else))
                        name = "Else";

                    return GetLink(item, name) == null;
                }
            }
            private static void ProcessLinks(Item trigger, CDCommands command, List<Item> list, ref int depth)
            {
                if (depth++ > 100)
                    throw new ApplicationException("Too complex or self-referential controls not supported");

                string? name = null;
                if (command.HasFlag(CDCommands.Link))
                    name = "Link";
                else if (command.HasFlag(CDCommands.Else))
                    name = "Else";

                if (trigger is TriggerRelay)
                {
                    List<Item> relays = GetRelayLinks(trigger);
                    foreach (Item relay_link in relays)
                    {
                        list.Add(trigger);
                        // Depth first: We follow each leg of the relay instead of list all legs up front
                        list.Add(relay_link);
                        ProcessLinks(relay_link, command, list, ref depth);
                    }
                }
                else
                {
                    Item? temp = null;
                    Item new_trigger = trigger;
                    while ((temp = GetLink(new_trigger, name)) != null)
                    {
                        new_trigger = temp;
                        if (new_trigger is TriggerRelay)
                            ProcessLinks(new_trigger, command, list, ref depth);
                        else
                            list.Add(new_trigger);

                        if (new_trigger == null)
                            break;
                    }
                }
            }
            private static List<Item> GetRelayLinks(Item trigger)
            {
                string name = "Link1;Link2;Link3;Link4;Link5;Link6;";
                string[] prop_names = name.Split(';', StringSplitOptions.RemoveEmptyEntries);
                List<Item> list = new();
                Item? temp = null;
                foreach (string prop_name in prop_names)
                    if ((temp = GetLink(trigger, prop_name)) != null)
                        list.Add(temp);
                return list;
            }
            private static Item? GetLink(Item trigger, string prop_name)
            {
                string? result = null;
                result = Server.Commands.Properties.GetValue(World.GetSystemAcct(), trigger, name: prop_name);
                if (result.Contains(Properties.PropNull) || result.Contains("not found"))
                    return null;
                return World.FindItem(Convert.ToInt32(KeyValue(result), 16));
            }
            private static readonly Regex SubStrKey = new(@"0[xX][0-9a-fA-F]+", RegexOptions.Compiled);
            private static string KeyValue(string s)
            {
                string result = SubStrKey.Match(s).Value;
                return result;
            }
        }
        #endregion ControlDebugger
        private class MoveToIntMapStorageCommand : BaseCommand
        {
            public MoveToIntMapStorageCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "MoveToIntMapStorage" };
                ObjectTypes = ObjectTypes.Both;
                Usage = "MoveToIntMapStorage <target>";
                Description = "Move the mobile/item to internal map storage.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Item item)
                {
                    item.MoveToIntStorage();
                }
                else if (obj is Mobile mobile)
                {
                    mobile.MoveToIntStorage();
                }
                else
                    LogFailure("That is not a mobile or an item");
            }
        }

        // === Forensic Evaluation Test Command ===
        private class ForensicEvaluationCommand : BaseCommand
        {
            public ForensicEvaluationCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "ForensicEvaluation", "FE" };
                ObjectTypes = ObjectTypes.Both;
                Usage = "ForensicEvaluation <target>";
                Description = "Tests the new ForensicEvaluation API on a target. Shows detailed results.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                Mobile from = e.Mobile;

                if (obj == null)
                {
                    LogFailure("No target selected.");
                    return;
                }

                ForensicEvaluation.ForensicResult result =
                    ForensicEvaluation.GetForensicInformation(from, obj, out ForensicEvaluation.ForensicInfo info);

                from.SendMessage("=== Forensic Evaluation Results ===");
                from.SendMessage($"Result: {result}");

                // Show all player-facing messages
                if (info.Messages.Count > 0)
                {
                    from.SendMessage("--- Messages ---");
                    foreach (string msg in info.Messages)
                    {
                        from.SendMessage(msg);
                    }
                }

                // Detailed API breakdown for GMs
                from.SendMessage("--- Detailed Info ---");

                if (info.HasBounty)
                {
                    from.SendMessage($"Wanted: Yes - {info.WhoWants} for {info.BountyAmount} GP");
                    if (info.HasLordBritishBonus)
                        from.SendMessage("   → Lord British Bonus Applied");
                }
                else if (result == ForensicEvaluation.ForensicResult.Success)
                {
                    from.SendMessage("Wanted: No");
                }

                if (info.IsThief)
                    from.SendMessage("Thieves Guild Member: Yes");

                if (info.IsCriminal)
                    from.SendMessage("Criminal Status: PERMA GREY (Criminal)");
                else if (info.IsThief || info.HasBounty) // Show clean only if we checked
                    from.SendMessage("Criminal Status: Clean");

                if (info.Killer != null)
                    from.SendMessage($"Killer: {info.Killer.Name}");

                if (info.Looters.Count > 0)
                {
                    from.SendMessage("Looters:");
                    foreach (Mobile looter in info.Looters)
                    {
                        if (looter != null && looter.AccessLevel <= from.AccessLevel)
                            from.SendMessage($"   • {looter.Name}");
                    }
                }
                else if (obj is Corpse)
                {
                    from.SendMessage("Looters: None");
                }

                if (info.LockPicker != null)
                    from.SendMessage($"Lock picked by: {info.LockPicker.Name}");

                // Final status
                from.SendMessage($"Forensic Evaluation: {(result == ForensicEvaluation.ForensicResult.Success ? "SUCCESS" : result.ToString())}");
            }
        }
        [Usage("WipeIPAddresses <account>")]
        [Description("WipeIPAddresses for the player account.")]
        public static void WipeIPAddresses_OnCommand(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
            {
                e.Mobile.SendMessage("WipeIPAddresses <account>");
                return;
            }

            Account? acct = Accounts.FindAccount(e.ArgString);
            if (acct == null)
            {
                e.Mobile.SendMessage("Cannot find account {0}", e.ArgString);
                return;
            }

            e.Mobile.SendMessage("This account has {0} login IP addresses", acct.LoginIPs.Length);
            acct.LoginIPs = new IPAddress[0];
            acct.ClearGAMELogin();
            e.Mobile.SendMessage("All IP addresses cleared.");
        }
        [Usage("WipeMachines <account>")]
        [Description("WipeMachines for the player account.")]
        public static void WipeMachines_OnCommand(CommandEventArgs e)
        {
            if (string.IsNullOrEmpty(e.ArgString))
            {
                e.Mobile.SendMessage("WipeMachines <account>");
                return;
            }

            Account? acct = Accounts.FindAccount(e.ArgString);
            if (acct == null)
            {
                e.Mobile.SendMessage("Cannot find account {0}", e.ArgString);
                return;
            }

            e.Mobile.SendMessage("This account has {0} machine signatures", acct.Machines.Count);
            acct.ClearMachines();
            e.Mobile.SendMessage("All machines cleared.");
        }

        [Usage("SetAccess <AccessLevel>")]
        [Description("Allow staff to downgrade their SccessLevel.")]
        public static void SetAccess_OnCommand(CommandEventArgs e)
        {
            AccessLevel al;
            string AccessLevel = "AccessLevel";
            try
            {
                al = (AccessLevel)Enum.Parse(typeof(AccessLevel), e.GetString(0), ignoreCase: true);
            }
            catch
            {
                goto usage;
            }

            if (e.Mobile.AccessLevel > al)
            {
                bool push_ok = true;
                string reason = string.Empty;
                // save their current access level so they can 'pop' it
                if (!Diagnostics.PropertyStackContains(e.Mobile, AccessLevel))
                    push_ok = Diagnostics.Push(e.Mobile as PlayerMobile, prop: AccessLevel, out reason);

                if (!push_ok)
                    e.Mobile.SendMessage(reason);
                else
                {
                    e.Mobile.AccessLevel = al;
                    e.Mobile.SendMessage("Use [Pop to recover your previous access level.");
                }
            }
            else if (e.Mobile.AccessLevel == al)
            {
                e.Mobile.SendMessage("AccessLevel unchanged.");
            }
            else
            {
                e.Mobile.SendMessage("You may not escalate your AccessLevel.");
            }

            return;

        usage:
            e.Mobile.SendMessage("SetAccess <Accesslevel>");
        }

        #region Time To Die

        [Usage("TimeToDie <all | spawners>")]
        [Description("Kills BaseCreatures. Use -all (with confirmation) or -spawners (spawned creatures only).")]
        public static void TimeToDie_OnCommand(CommandEventArgs e)
        {
            if (e?.Mobile == null)
                return;

            string? arg = e.ArgString?.Trim().ToLower();

            if (arg == "all" || arg == "-all")
            {
                // Strong warning + confirmation prompt
                e.Mobile.SendMessage(0x25, "WARNING: This will kill ALL BaseCreatures in the world, including non-spawned entities.");
                e.Mobile.SendMessage(0x25, "This action is NOT limited to spawners and cannot be undone.");
                e.Mobile.SendMessage(0x3B2, "Type 'yes' to continue.");
                e.Mobile.Prompt = new TimeToDiePrompt();
            }
            else if (arg == "spawners" || arg == "-spawners")
            {
                // Safe mode - only kill creatures spawned from spawners or champion engines,
                // plus BaseOverland creatures.
                var creaturesToKill = new HashSet<Mobile>();

                // 1. Standard Spawners
                foreach (Spawner spawner in Spawner.Instances)
                {
                    if (spawner == null || spawner.Deleted)
                        continue;

                    var mobiles = spawner.GetMobiles();
                    if (mobiles != null)
                    {
                        foreach (Mobile m in mobiles)
                        {
                            if (m is BaseCreature bc && !bc.Deleted)
                                creaturesToKill.Add(bc);
                        }
                    }
                }

                // 2. Champion Engines
                foreach (ChampEngine engine in ChampEngine.Instances)
                {
                    if (engine == null || engine.Deleted)
                        continue;

                    var mobiles = engine.GetMobiles();
                    if (mobiles != null)
                    {
                        foreach (Mobile m in mobiles)
                        {
                            if (m is BaseCreature bc && !bc.Deleted)
                                creaturesToKill.Add(bc);
                        }
                    }
                }

                // 3. BaseOverland creatures (wandering uniques / events)
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is BaseOverland bo && !bo.Deleted)
                        creaturesToKill.Add(bo);
                }

                // Schedule death with small random delay
                int count = 0;
                foreach (Mobile m in creaturesToKill)
                {
                    if (m is BaseCreature bc)
                    {
                        bc.TimeToDie(TimeSpan.FromMilliseconds(Utility.Random(1, 5000)));
                        count++;
                    }
                }

                e.Mobile.SendMessage(0x3B2,
                    $"TimeToDie (spawners + champions + overland) scheduled for {count} creatures.");
            }
            else
            {
                // Invalid usage
                e.Mobile.SendMessage(0x25, "Usage: [TimeToDie <all | spawners>");
                e.Mobile.SendMessage(0x3B2, "-all       : Kills everything (requires confirmation)");
                e.Mobile.SendMessage(0x3B2, "-spawners  : Kills only creatures spawned from spawners (no confirmation)");
            }
        }

        private class TimeToDiePrompt : Prompt
        {
            public override void OnResponse(Mobile from, string text)
            {
                if (!string.Equals(text?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                {
                    from.SendMessage("TimeToDie cancelled.");
                    return;
                }

                int count = 0;
                foreach (Mobile m in World.Mobiles.Values.ToArray())
                {
                    if (m is BaseCreature bc && !bc.Deleted)
                    {
                        bc.TimeToDie(TimeSpan.FromMilliseconds(Utility.Random(1, 5000)));
                        count++;
                    }
                }

                from.SendMessage(0x25, $"TimeToDie executed on {count} BaseCreatures.");
            }
        }

        #endregion Time To Die

        #region Set Config
        [Usage("SetConfig <flag> [on|off|true|false|1|0]")]
        //[Usage("SetConfig status")]
        [Description("Dynamically switch server configuration. Enabling one auto-disables all others.")]
        public static void SetConfig_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            if (e.Arguments.Length == 0 || Insensitive.Equals(e.Arguments[0], "status"))
            {
                ShowCurrentConfig(m);
                return;
            }

            if (e.Arguments.Length < 2)
            {
                m.SendMessage("Usage: setconfig <flag> [on|off|true|false|1|0]");
                m.SendMessage("Example: setconfig -uomo on");
                return;
            }

            string flag = e.Arguments[0].TrimStart('-').ToLower();
            string valueStr = e.Arguments[1].ToLower();

            bool enable;
            if (valueStr == "on" || valueStr == "true" || valueStr == "1")
                enable = true;
            else if (valueStr == "off" || valueStr == "false" || valueStr == "0")
                enable = false;
            else
            {
                m.SendMessage("Invalid value. Use on/off, true/false, or 1/0.");
                return;
            }

            bool changed = ApplyServerFlag(flag, enable, out string enabledServer, out string disabledServers);

            if (changed)
            {
                string action = enable ? "enabled" : "disabled";
                m.SendMessage($"Server flag '{flag}' {action}.");
                if (enable)
                    m.SendMessage($"Active server is now: {enabledServer}");
                else
                    m.SendMessage($"No server is active after disabling {flag}.");

                if (!string.IsNullOrEmpty(disabledServers))
                    m.SendMessage($"Auto-disabled: {disabledServers}");

                // Ensure all players reflect the currently active RuleSet.
                // 
                // On startup, this aligns loaded world data with the configured shard.
                // On dynamic reconfiguration (e.g., SetConfig), this must be called explicitly
                // to reapply shard semantics (such as Mortality) to existing PlayerMobiles.
                ShardInit();

                ShowCurrentConfig(m);
            }
            else
            {
                m.SendMessage($"Unknown or unsupported server flag: {flag}");
            }
        }

        private static bool ApplyServerFlag(string flag, bool enable, out string enabledServer, out string disabledServers)
        {
            enabledServer = null;
            disabledServers = null;

            // Reset all server flags first (enforce exactly one active)
            bool anyChanged = false;

            if (enable)
            {
                // Disable all others
                if (RuleSets.GMNAI_SVR) { RuleSets.GMNAI_SVR = false; anyChanged = true; }
                if (RuleSets.GMNSP_SVR) { RuleSets.GMNSP_SVR = false; anyChanged = true; }
                if (RuleSets.GMNMO_SVR) { RuleSets.GMNMO_SVR = false; anyChanged = true; }
                if (RuleSets.GMNSE_SVR) { RuleSets.GMNSE_SVR = false; anyChanged = true; }
                // Add others if you have more
            }

            switch (flag.ToLower())
            {
                case "uosp":
                case "gmsp":
                case "siege":
                    RuleSets.GMNSP_SVR = enable;
                    enabledServer = enable ? "Siege Perilous" : null;
                    anyChanged |= true;
                    break;

                case "uoai":
                case "gmnai":
                case "angelisland":
                    RuleSets.GMNAI_SVR = enable;
                    enabledServer = enable ? "Angel Island" : null;
                    anyChanged |= true;
                    break;

                case "uomo":
                case "gmnmo":
                case "mortalis":
                    RuleSets.GMNMO_SVR = enable;
                    enabledServer = enable ? "Mortalis" : null;
                    anyChanged |= true;
                    break;

                case "uose":
                case "gmnse":
                case "seasons":
                    RuleSets.GMNSE_SVR = enable;
                    enabledServer = enable ? "Seasons" : null;
                    anyChanged |= true;
                    break;

                // Add any other server flags here...

                default:
                    return false;
            }

            if (anyChanged && enable)
            {
                // Optional: refresh dependent systems (publish, expansion, etc.)
                RefreshRuleSets();
            }

            // Build list of what was disabled (for feedback)
            var disabledList = new List<string>();
            if (!RuleSets.GMNAI_SVR && flag != "uoai" && flag != "gmnai") disabledList.Add("Angel Island");
            if (!RuleSets.GMNSP_SVR && flag != "uosp" && flag != "gmsp") disabledList.Add("Siege");
            if (!RuleSets.GMNMO_SVR && flag != "uomo" && flag != "gmnmo") disabledList.Add("Mortalis");
            if (!RuleSets.GMNSE_SVR && flag != "uose" && flag != "gmnse") disabledList.Add("Seasons");
            disabledServers = disabledList.Any() ? string.Join(", ", disabledList) : null;

            return anyChanged;
        }

        private static void RefreshRuleSets()
        {
            // Re-apply publish / era logic based on the now-active server
            if (RuleSets.GMNSP_SVR)
            {
                // Publish 13.6 (Siege Perilous Shards Only) - October 25, 2001
                PublishInfo.Publish = 13.6;
            }
            else if (RuleSets.GMNAI_SVR || RuleSets.GMNMO_SVR || RuleSets.GMNSE_SVR)
            {
                // Resource/Crafting changes, Felucca Champion Spawns and Powerscrolls, Barding/Taming changes,
                // Felucca enhancements/changes, House Ownership changes, the GGS system,
                // the Justice Virtue, Siege Perilous ruleset changes
                PublishInfo.Publish = 15.0;
            }
            else
            {
                // Default/fallback — adjust as needed
                PublishInfo.Publish = 15.0;
            }

            // You can also force-refresh other cached values here if needed
            // e.g. ExpansionInfo.Refresh();
            // or call any Configure() methods that depend on ruleset
        }

        private static void ShowCurrentConfig(Mobile m)
        {
            string active = "None";

            if (RuleSets.GMNAI_SVR) active = "Angel Island (-uoai)";
            else if (RuleSets.GMNSP_SVR) active = "Siege Perilous (-uosp)";
            else if (RuleSets.GMNMO_SVR) active = "Mortalis (-uomo)";
            else if (RuleSets.GMNSE_SVR) active = "Seasons (-uose)";

            m.SendMessage("Current server configuration:");
            m.SendMessage($"Active server: {active}");
            m.SendMessage("Flags:");
            m.SendMessage($" - Angel Island (-uoai):   {(RuleSets.GMNAI_SVR ? "ON" : "OFF")}");
            m.SendMessage($" - Siege (-uosp):          {(RuleSets.GMNSP_SVR ? "ON" : "OFF")}");
            m.SendMessage($" - Mortalis (-uomo):       {(RuleSets.GMNMO_SVR ? "ON" : "OFF")}");
            m.SendMessage($" - Seasons (-uose):        {(RuleSets.GMNSE_SVR ? "ON" : "OFF")}");
            m.SendMessage("");
            m.SendMessage("Note: Enabling one server automatically disables all others.");
            m.SendMessage("Some changes may require world save/restart for full effect.");
        }
        #endregion Set Config
        #region Location Recorder
        [Usage("ConstructRect")]
        [Description("Build a rect from data collected by LocationRecorder.")]
        public static void ConstructRect_OnCommand(CommandEventArgs e)
        {
            try
            {   // gather the points
                List<Point2D> list2D = new();
                foreach (string line in File.ReadAllLines(Path.Combine(Core.LogsDirectory, "LocationRecorder.log")))
                    list2D.Add(Point2D.Parse(line));

                // construct the rectangle
                Rectangle2D rect = new Rectangle2D(list2D[0], list2D[1]);
                foreach (Point2D point in list2D)
                    rect.MakeHold(point);

                // let the user know
                e.Mobile.SendMessage("Your new rectangle is {0}", rect);
                e.Mobile.SendMessage("Start: {0}, End: {1}", rect.Start, rect.End);
            }
            catch
            {
                e.Mobile.SendMessage("LocationRecorder.log file missing or corrupt.");
            }
        }
        private static LogHelper? LocationRecorder = null;
        private static Server.Timer? LRTimer = null;
        private static Rectangle2D LRRect = new Rectangle2D();
        [Usage("LocationRecorder <on/off/record/clear>")]
        [Description("Records the mobiles location to a log file.")]
        public static void LocationRecorder_OnCommand(CommandEventArgs e)
        {
            if (e.GetString(0) == "on" || e.GetString(0) == "auto")
            {
                if (LocationRecorder != null)
                {
                    LocationRecorder.Finish();
                    LocationRecorder = null;
                }
                if (LRTimer != null)
                {
                    LRTimer.Stop();
                    LRTimer = null;
                }
                e.Mobile.SendMessage("Recorder started...");
                LocationRecorder = new LogHelper("LocationRecorder.log", overwrite: false, sline: true, quiet: true);
                if (e.GetString(0) == "auto")
                {
                    LRTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1), new TimerStateCallback(LocationTick), e.Mobile);
                    LRRect = new Rectangle2D(e.Mobile.X, e.Mobile.Y, 1, 1); // center on the mobile
                }
            }
            else if (e.GetString(0) == "off")
            {
                if (LocationRecorder != null)
                {
                    LocationRecorder.Finish();
                    LocationRecorder = null;
                    e.Mobile.SendMessage("Recorder stopped.");
                    e.Mobile.SendMessage("Find your recording in LocationRecorder.log");
                    if (LRTimer != null)
                    {
                        LRTimer.Stop();
                        LRTimer = null;
                    }
                }
            }
            else if (e.GetString(0) == "record")
            {
                if (LocationRecorder != null)
                {
                    e.Mobile.SendMessage("Target the location to record");
                    e.Mobile.Target = new LocationRecorderTarget();
                }
                else
                {
                    e.Mobile.SendMessage("Recorder not running.");
                    e.Mobile.SendMessage("Usage: LocationRecorder <on/off/record/clear>.");
                }
            }
            else if (e.GetString(0) == "clear")
            {
                if (LocationRecorder != null)
                    LocationRecorder.Finish();


                LocationRecorder = new LogHelper("LocationRecorder.log", overwrite: true, sline: true, quiet: true);
                LocationRecorder.Finish();
                e.Mobile.SendMessage("Recorder cleared.");
            }
            else
                e.Mobile.SendMessage("Usage: LocationRecorder <on/off/record/clear>.");
        }
        private static void LocationTick(object state)
        {
            Mobile? m = state as Mobile;
            LRRect.MakeHold(new Point2D(m.Location));
            string text = string.Format("({0} {1}, {2}, {3})", LRRect.Start.X, LRRect.Start.Y, LRRect.End.X, LRRect.End.Y);
            m.SendMessage(text);
            ClipboardService.SetText(text);
        }
        public class LocationRecorderTarget : Target
        {
            public LocationRecorderTarget() : base(12, true, TargetFlags.None) {; }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                StaticTarget? st = targeted as StaticTarget;
                IPoint3D? point = targeted as IPoint3D;
                string text = string.Format("{0}", new Point2D(point.X, point.Y));
                from.SendMessage("Logging:" + text);
                LocationRecorder.Log(text);
            }
        }
        #endregion Location Recorder
        [Usage("GetNoto")]
        [Description("Gets of the Notoriety of this creature.")]
        public static void GetNoto_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the Mobil you wish to check.");
            e.Mobile.Target = new GetMobileTarget(false);
        }
        public class GetMobileTarget : Target
        {
            private bool m_copy = false;
            public GetMobileTarget(object o) : base(12, true, TargetFlags.None) { m_copy = (bool)o; }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                StaticTarget? st = targeted as StaticTarget;
                string clipboard = string.Empty;
                if (mt == null)
                {
                    from.SendMessage("That is not a mobile.");
                    return;
                }

                int result = Server.Misc.NotorietyHandlers.MobileNotoriety(from, mt);
                switch (result)
                {
                    case Notoriety.Innocent:
                        from.SendMessage("Innocent");
                        break;
                    case Notoriety.Ally:
                        from.SendMessage("Ally");
                        break;
                    case Notoriety.CanBeAttacked:
                        from.SendMessage("CanBeAttacked");
                        break;
                    case Notoriety.Criminal:
                        from.SendMessage("Criminal");
                        break;
                    case Notoriety.Enemy:
                        from.SendMessage("Enemy");
                        break;
                    case Notoriety.Murderer:
                        from.SendMessage("Murderer");
                        break;
                    case Notoriety.Invulnerable:
                        from.SendMessage("Invulnerable");
                        break;
                    default:
                        from.SendMessage("Unknown");
                        break;
                }
            }
        }
        [Usage("ViewItems")]
        [Description("Views the 'Items' associated with this Mobile or Item.")]
        public static void ViewItems_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the Mobile or Item you wish to check.");
            e.Mobile.Target = new GetItemsTarget(false);
        }
        [Usage("CopyItems")]
        [Description("Copies the 'Items' associated with this Mobile or Item to the clipboard.")]
        public static void CopyItems_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the Mobile or Item you wish to check.");
            e.Mobile.Target = new GetItemsTarget(true);
        }
        public class GetItemsTarget : Target
        {
            private bool m_copy = false;
            public GetItemsTarget(object o) : base(12, true, TargetFlags.None) { m_copy = (bool)o; }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                StaticTarget? st = targeted as StaticTarget;
                string clipboard = string.Empty;
                if (mt != null)
                {
                    foreach (Item item in mt.Items)
                    {
                        from.SendMessage("{0}", item.ToString());
                        Console.WriteLine("{0}", item.ToString());
                        if (m_copy)
                            clipboard += item.ToString() + "\n";
                    }

                    if (m_copy)
                        ClipboardService.SetText(clipboard);

                    from.SendMessage("Total items: {0}", mt.Items.Count);
                    Console.WriteLine("Total items: {0}", mt.Items.Count);
                }
                else if (it != null)
                {
                    foreach (Item item in it.Items)
                    {
                        from.SendMessage("{0}", item.ToString());
                        Console.WriteLine("{0}", item.ToString());
                        if (m_copy)
                            clipboard += item.ToString() + "\n";
                    }

                    if (m_copy)
                        ClipboardService.SetText(clipboard);

                    from.SendMessage("Total items: {0}", it.Items.Count);
                    Console.WriteLine("Total items: {0}", it.Items.Count);
                }
                else
                    from.SendMessage("That is neither a mobile or item.");
            }
        }
        [Usage("Colorize")]
        [Description("Colorize all items in the bounding rect. This cannot be undone.")]
        public static void ColorizeRect_OnCommand(CommandEventArgs e)
        {
            if (e.Length != 1)
            {
                e.Mobile.SendMessage("Usage [Colorize <true|false>");
                return;
            }
            if (Core.GMNTC_CFG == false)
            {
                e.Mobile.SendMessage("This command may only be run on test center");
                return;
            }
            try
            {
                BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(Colorize_Callback), e.GetBoolean(0));
            }
            catch
            {
                e.Mobile.SendMessage("Usage [Colorize <true|false>");
            }
        }

        private static void Colorize_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            foreach (object o in from.Map.GetObjectsInBounds(rect))
                if (o is Item item)
                    item.Colorize = (bool)state;

            from.SendMessage("done.");
        }
        [Usage("BoundingRect")]
        [Description("You define a rect, it is shown to you, and the rect is copied to the clipboard.")]
        public static void BoundingRect_OnCommand(CommandEventArgs e)
        {
            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(Rect_Callback), 0x01);
        }

        private static void Rect_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            ClipboardService.SetText(rect.ToString());
            Server.Gumps.EditAreaGump.FlashArea(from, rect, from.Map);
            from.SendMessage("{0} has been copied to your clipboard.", rect.ToString());
            int mobs = 0;
            int items = 0;
            foreach (object o in from.Map.GetObjectsInBounds(rect))
                if (o is Mobile mob && !mob.Deleted)
                    mobs++;
                else if (o is Item item && !item.Deleted)
                    items++;
            from.SendMessage("There are {0} mobiles and {1} items in this rect.", mobs, items);
        }
        [Usage("SectorBounds")]
        [Description("Displays the bounding area of the current sector.")]
        public static void SectorBounds_OnCommand(CommandEventArgs e)
        {
            Sector mySect = e.Mobile.Map.GetSector(e.Mobile);
            int sectorX = mySect.X << Map.SectorShift;
            int sectorY = mySect.Y << Map.SectorShift;
            var rect = new Rectangle2D(sectorX, sectorY, Map.SectorSize, Map.SectorSize);
            Server.Gumps.EditAreaGump.FlashArea(e.Mobile, rect, e.Mobile.Map);
        }

        [Usage("GetSectorInfo")]
        [Description("Displays the XY current sector and number of mobiles.")]
        public static void GetSectorInfo_OnCommand(CommandEventArgs e)
        {
            Sector mySect = e.Mobile.Map.GetSector(e.Mobile);
            int sectorX = mySect.X << Map.SectorShift;
            int sectorY = mySect.Y << Map.SectorShift;
            var rect = new Rectangle2D(sectorX, sectorY, Map.SectorSize, Map.SectorSize);
            e.Mobile.SendMessage("Sector {0} with {1} mobiles.", rect, mySect.Mobiles.Count);
        }
        [Usage("GetSurfaceTop")]
        [Description("Get Surface Top")]
        public static void GetSurfaceTop_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new GetSurfaceTopTarget(null);
        }
        public class GetSurfaceTopTarget : Target
        {
            public GetSurfaceTopTarget(object? o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                StaticTarget? st = targeted as StaticTarget;
                IPoint3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                Spells.SpellHelper.GetSurfaceTop(ref loc);
                from.SendMessage(string.Format("GetSurfaceTop: Z:{0}", loc.Z));
            }
        }
        [Usage("GetAverageZ")]
        [Description("Get Average Z")]
        public static void GetAverageZ_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new GetAverageZTarget(null);
        }
        public class GetAverageZTarget : Target
        {
            public GetAverageZTarget(object? o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                StaticTarget? st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;

                int landZ = 0, landAvg = 0, landTop = 0;
                from.Map.GetAverageZ(loc.X, loc.Y, ref landZ, ref landAvg, ref landTop);
                from.SendMessage(string.Format("GetAverageZ(loc.X={0}, loc.Y={1}, landZ={2}, landAvg={3}, landTop={4})",
                    loc.X, loc.Y, landZ, landAvg, landTop));
            }
        }
        /// <summary>
        /// FindBuggedMap is a map walker that visits every tile on the map. At each location it will spawn check whether a mobile can be spawned there, an if so
        ///     record the maximum Z that is spawnable. The algo will then query if N more mobiles could be spawned on adjacent tiles. If not, we move along to the next location.
        ///     The reason a mobile cannot be spawned here is due to some blocking tile: boulder, mountain, tree etc.
        ///     When all N spawn locations are successfully acquired, we make the following assumption:
        ///     All mobiles spawned on adjacent tiles must have the same or similar Zz (+-5).
        ///     When we have one or more locations with a depth(Z) greater than our threshold (say a Z of 30,) we create a 'report' record for that location.
        ///     Before we write the report however, we want to remove records that likely point to the same hole. I.e., they are in close proximity to one another.
        ///     When this is accomplished, we write the report to the \Logs folder.
        /// </summary>
        /// <param name="e"></param>
        [Usage("FindBuggedMap")]
        [Description("Log 'holes' in the map. See FindBuggedMap.log")]
        public static void FindBuggedMap_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Calculating bugged map tiles.");
            e.Mobile.SendMessage("This will take 3-5 minutes...");
            Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(Tick), e);
        }
        private static void Tick(object state)
        {
            CommandEventArgs? e = state as CommandEventArgs;
            FindBuggedMap(e);
        }
        public static void FindBuggedMap(CommandEventArgs e)
        {
            int depth = 0;
            if (e.Arguments.Length == 0 || int.TryParse(e.ArgString, out depth) == false)
            {
                e.Mobile.SendMessage("Usage: FindBuggedMap <depth threshold>");
                return;
            }
            LogHelper logger = new LogHelper("FindBuggedMap.log", true, true, true);
            try
            {
                Console.WriteLine("Calculating bugged map tiles.");
                Console.WriteLine("This will take 3-5 minutes...");
                DateTime dt = DateTime.UtcNow;
                Point2D last = new Point2D(0, 0);               // last point seen
                Rectangle2D fel = Utility.WorldMap.BritWrap[0];    // our world
                List<int[]> records = new();                    // X, Y, X (depth of hole)
                int slop = 5;                                   // how far two holes are from one another to be considered unique
                int[] Ztab = new int[4];                        // storage for our 4 discovered Zs

                for (int y = fel.Start.Y; y < (fel.Height); y++)
                {
                    for (int x = fel.Start.X; x < (fel.Width); x++)
                    {
                        // check at end of map
                        if (x + 4 >= Map.Felucca.Width) continue;

                        int Z = 0;
                        int X = x;
                        int index = 0;

                        for (int jx = 0; jx < 4; jx++)
                        {
                            if (Utility.CanSpawnMobile(Map.Felucca, X++, y, ref Z))
                                Ztab[index++] = Z;
                            else break;

                            if (jx + 1 == 4)
                                goto Success;   // we got all 4 spawn positions
                        }
                        continue;
                    Success:
                        // okay, we were able to spawn 4 consecutive mobiles This should not be possible if there was some blocking tile in the way,
                        // like a wall, bounder, tree, etc.
                        // So if we were able to spawn all four, lets make sure the Zz match up (or are close)
                        int bd = BiggestDifference(Ztab);

                        if (bd >= depth)
                        {
                            records.Add(new int[] { x, y, bd });
                            last = new Point2D(X, y);
                            Console.Write(".");
                        }
                        continue;
                    }
                }

                // remove all neighbors within 'slop' distance from one another. I.e., we don't want 20 reports of the same 'hole'
                RemoveNeighbors(slop, records);

                // look for our known 'holes' in Jhelom and Minax
                int JhelomSlop = 8;     // we know this checker comes within 7.2 tiles of our Jhelom hole (get only closest match)
                int MinaxSlop = 6;      // we know this checker comes within 5.3 tiles of our Minax hole (get only closest match)
                for (int ix = 0; ix < records.Count; ix++)
                {
                    if (Utility.GetDistanceToSqrt(new Point2D(records[ix][0], records[ix][1]), new Point2D(1418, 3991)) <= JhelomSlop)
                        logger.Log(LogType.Text, string.Format("Found our known Jhelom 'hole' near {0} {1} at {2} {3}.", 1418, 3991, records[ix][0], records[ix][1]));

                    if (Utility.GetDistanceToSqrt(new Point2D(records[ix][0], records[ix][1]), new Point2D(1108, 2610)) <= MinaxSlop)
                        logger.Log(LogType.Text, string.Format("Found our known Minax 'hole' near {0} {1} at {2} {3}.", 1418, 3991, records[ix][0], records[ix][1]));
                }

                // output our results
                for (int ix = 0; ix < records.Count; ix++)
                {
                    logger.Log(LogType.Text, string.Format("Hole located at {0} {1} depth is {2}", records[ix][0], records[ix][1], records[ix][2]));
                }

                Console.WriteLine();
                Console.WriteLine("done in {0:0.00} seconds.", (DateTime.UtcNow - dt).TotalSeconds);
                e.Mobile.SendMessage("done in {0:0.00} seconds.", (DateTime.UtcNow - dt).TotalSeconds);
                logger.Finish();
            }
            catch
            {
                logger.Finish();
            }

        }
        /// <summary>
        /// The core algo here returns every point that seems to be a hole in the map. THis means that if the hole is 12 tiles, we will get 12 positives.
        /// RemoveNeighbors will for example attempt to remove all but reference to the hole - drastically shrinking the report file.
        /// 
        /// </summary>
        /// <param name="Slop"> The integer threshold that determines when report records are to be kept or they are noise and should be deleted.</param>
        /// <param name="records"> The records to analyze</param>
        /// <returns>number of records removed</returns>
        private static int RemoveNeighbors(int Slop, List<int[]> records)
        {
            int oldCount = records.Count;
            List<int[]> delete = new();
            for (int ix = 0; ix < records.Count; ix++)
            {
                for (int jx = 0; jx < records.Count; jx++)
                {
                    Point2D pxi = new Point2D(records[ix][0], records[ix][1]);
                    Point2D pxj = new Point2D(records[jx][0], records[jx][1]);
                    if (pxi == pxj || delete.Contains(records[ix]) || delete.Contains(records[jx]))
                        continue;
                    double dtsr = Utility.GetDistanceToSqrt(pxi, pxj);
                    if (dtsr <= Slop)
                        if (pxi > pxj)
                        {
                            if (!delete.Contains(records[ix]))
                                delete.Add(records[ix]);
                        }
                        else if (pxi < pxj)
                        {
                            if (!delete.Contains(records[jx]))
                                delete.Add(records[jx]);
                        }
                        else
                        {   // interestingly (to me anyway) you cannot compare (>, <) to two points if they are on different X's or Y's (see Geometry.cs) 
                            // so, we'll prefer (somewhat arbitrarily) the smaller X
                            if (records[jx][0]/*X*/ < records[ix][0]/*X*/)
                            {
                                if (!delete.Contains(records[jx]))
                                    delete.Add(records[jx]);
                            }
                            else if (!delete.Contains(records[ix]))
                                delete.Add(records[ix]);
                            else
                                ;// wtf
                        }
                }
            }

            for (int ix = 0; ix < delete.Count; ix++)
                if (records.Contains(delete[ix]))
                    records.Remove(delete[ix]);

            return oldCount - records.Count;
        }
        private static int NextX(int n, int x)
        {
            switch (n)
            {
                default:
                case 0: return x;
                case 1: return x + 1;
                case 2: return x - 1;
                case 3: return x + 1;
                case 4: return x - 1;
            }
        }
        private static int NextY(int n, int y)
        {
            switch (n)
            {
                default:
                case 0: return y;
                case 1: return y + 1;
                case 2: return y - 1;
                case 3: return y - 1;
                case 4: return y + 1;
            }
        }
        private static int BiggestDifference(int[] A)
        {
            int N = A.Length;
            if (N < 1) return 0;

            int max = 0;
            int result = 0;

            for (int i = N - 1; i >= 0; --i)
            {
                if (A[i] > max)
                    max = A[i];

                var tmpResult = max - A[i];
                if (tmpResult > result)
                    result = tmpResult;
            }

            return result;
        }
        [Usage("StackDepth")]
        [Description("How many items are at this point?")]
        public static void StackDepth_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new StackDepthTarget(null);
        }
        public class StackDepthTarget : Target
        {
            public StackDepthTarget(object? o)
                : base(12, false, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : Point3D.Zero;
                List<int> Zs = new();
                int count = StackDepth(loc.X, loc.Y, from.Map, Zs);
                Zs.Sort();
                from.SendMessage(string.Format("There are {0} items stacked at {1}, Z{3}={2}", count, new Point2D(loc.X, loc.Y),
                    (count == 0) ? from.Map.GetAverageZ(loc.X, loc.Y) :
                    (count == 1) ? Zs[0].ToString() :
                    string.Format("{0}-{1}", Zs.First(), Zs.Last()),
                    count > 1 ? "s" : ""
                    ));
            }
        }
        public static int StackDepth(int X, int Y, Map map)
        {
            List<int> Zs = new();
            foreach (object o in map.GetItemsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Item item && item.Deleted == false)
                    if (item.Location.X == X && item.Location.Y == Y)
                        Zs.Add(item.Z);

            return Zs.Count;
        }
        public static int StackDepth(int X, int Y, Map map, List<int> Zs)
        {
            foreach (object o in map.GetItemsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Item item && item.Deleted == false)
                    if (item.Location.X == X && item.Location.Y == Y)
                        Zs.Add(item.Z);

            return Zs.Count;
        }
        public static int GetStack(int X, int Y, Map map, List<Item> items)
        {
            foreach (object o in map.GetItemsInBounds(new Rectangle2D(X, Y, 1, 1)))   // does 0,0 work? seems not
                if (o is Item item && item.Deleted == false)
                    items.Add(item);

            return items.Count;
        }

        [Usage("CanSpawnMobile <Y, Y, Z>")]
        [Description("Can we fit a mobile at this loction? needed for treasure map locations.")]
        public static void CanSpawnMobileT_OnCommand(CommandEventArgs e)
        {
            Point3D px;
            if (e.Length != 3)
            {
                e.Mobile.SendMessage("Point3D format error. Format is (XXX, YYY, ZZZ)");
                return;
            }
            try
            {
                px = Point3D.Parse(e.ArgString);
                int z = px.Z;
                if (e.Mobile.Map.CanSpawnLandMobile(px))
                    e.Mobile.SendMessage("A mobile can be spawned at location {0}", px);
                else
                {
                    e.Mobile.SendMessage("A mobile cannot be spawned at location {0}, however...", px);
                    px.Z = e.Mobile.Map.GetAverageZ(px.X, px.Y);
                    if (e.Mobile.Map.CanSpawnLandMobile(px))
                        e.Mobile.SendMessage("Still cannot spawn a mobile at location {0} using GetAverageZ()", px);
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Point3D format error. Format is (XXX, YYY, ZZZ) {0}", ex);
            }
        }
        [Usage("CanSpawnMobileZ")]
        [Description("Can we spawn a mobile at this location?")]
        public static void CanSpawnMobileZ_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new CanSpawnMobileTargetZ(null);
        }
        public class CanSpawnMobileTargetZ : Target
        {
            public CanSpawnMobileTargetZ(object? o)
                : base(12, true, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                StaticTarget? st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                int Z = -1;
                if (loc != Point3D.Zero && Utility.CanSpawnMobile(from.Map, loc.X, loc.Y, ref Z))
                    from.SendMessage("A mobile can be spawned at {0}, Z = {1}", new Point2D(loc.X, loc.Y), Z);
                else
                    from.SendMessage("A mobile cannot be spawned at {0}, Z=?", new Point2D(loc.X, loc.Y));
            }
        }
        [Usage("CanSpawnMobile")]
        [Description("Can we spawn a mobile at this location?")]
        public static void CanSpawnMobile_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the location you wish to check.");
            e.Mobile.Target = new CanSpawnMobileTarget(null);
        }
        public class CanSpawnMobileTarget : Target
        {
            public CanSpawnMobileTarget(object? o)
                : base(12, true, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget? tl = targeted as LandTarget;
                Item? it = targeted as Item;
                Mobile? mt = targeted as Mobile;
                StaticTarget? st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;
                if (loc != Point3D.Zero && Utility.CanSpawnLandMobile(from.Map, new Point2D(loc.X, loc.Y), loc.Z, flags: CanFitFlags.requireSurface | CanFitFlags.canSwim))
                    from.SendMessage($"A mobile can be spawned at {loc}");
                else
                    from.SendMessage($"A mobile cannot be spawned at {loc}");
            }
        }
        [Usage("SeedAccounts")]
        [Description("Seeds the login database from the accounts.xml on this server.")]
        public static void SeedAccounts_OnCommand(CommandEventArgs e)
        {
            if (Core.UseLoginDB)
            {
                Utility.Monitor.WriteLine("Seeding Login Database from server {0}.", ConsoleColor.Green, Core.Server);
                Server.Accounting.AccountsDatabase.SeedAccounts();
            }
            else
            {
                Utility.Monitor.WriteLine("Not using the Login Database.", ConsoleColor.Red);
            }
        }
        public static bool IsOwnerIP(System.Net.IPAddress ip)
        {
            return (
                /*Yoar ip.Equals(System.Net.IPAddress.Parse("83.87.255.92"))*/
                /*Adam*/   ip.Equals(System.Net.IPAddress.Parse(CoreAI.OwnerIP))    // me logging into production from home
                /*Adam*/|| ip.Equals(System.Net.IPAddress.Parse("127.0.0.1"))       // me logging into localhost from home
                );
        }
        [Usage("TileOverload")]
        [Description("Looks for, and deletes excessive items on a single tile.")]
        public static void TileOverload_OnCommand(CommandEventArgs e)
        {

            Dictionary<Point3D, KeyValuePair<Map, int>> database = new Dictionary<Point3D, KeyValuePair<Map, int>>();
            try
            {
                foreach (Item i in World.Items.Values)
                {
                    if (i == null || i.Parent != null || i.Map == Map.Internal)
                        continue;   // only items 'in the world'

                    if (database.ContainsKey(i.Location))
                        database[i.Location] = new KeyValuePair<Map, int>(i.Map, database[i.Location].Value + 1);
                    else
                        database.Add(i.Location, new KeyValuePair<Map, int>(i.Map, 1));
                }

                Dictionary<Point3D, KeyValuePair<Map, int>> problemDatabase = new Dictionary<Point3D, KeyValuePair<Map, int>>();
                foreach (KeyValuePair<Point3D, KeyValuePair<Map, int>> kvp in database)
                {
                    if (kvp.Value.Value > 666)
                        if (!problemDatabase.ContainsKey(kvp.Key))
                            problemDatabase.Add(kvp.Key, kvp.Value);
                }

                foreach (KeyValuePair<Point3D, KeyValuePair<Map, int>> kvp in problemDatabase)
                {
                    LogHelper logger = new LogHelper("TileOverload.log", false, true);
                    string text = string.Format("{0} items found at {1}.", kvp.Value.Value, kvp.Key);
                    logger.Log(text);
                    logger.Finish();
                    e.Mobile.SendMessage(text);
                }

                if (e.Arguments.Length > 0 && e.ArgString.ToLower() == "fix")
                {
                    foreach (KeyValuePair<Point3D, KeyValuePair<Map, int>> kvp in problemDatabase)
                    {
                        LogHelper logger = new LogHelper("TileOverload.log", false, true);
                        string text = string.Format("Deleting {0} items at {1}.", kvp.Value.Value, kvp.Key);
                        logger.Log(text);
                        logger.Finish();
                        e.Mobile.SendMessage(text);

                        IPooledEnumerable eable = kvp.Value.Key.GetItemsInRange(kvp.Key, 0);
                        List<Item> list = new List<Item>();
                        foreach (Item ix in eable)
                        {   // list of items to delete
                            // Note: we need to recheck the problemdatabase since GetItemsInRange() ignores the Z plane
                            if (problemDatabase.ContainsKey(ix.Location))
                                list.Add(ix);
                        }
                        eable.Free();

                        foreach (Item item in list)
                            item.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        [Usage("ItemProfiler")]
        [Description("Profiles Item and looks for leaks.")]
        public static void ItemProfiler_OnCommand(CommandEventArgs e)
        {
            Dictionary<int, int> intItems = new Dictionary<int, int>();
            Dictionary<int, int> felItems = new Dictionary<int, int>();
            int oddMapCount = 0;
            try
            {   // catalog items
                foreach (Item i in World.Items.Values)
                {
                    if (i.Map == Map.Internal)
                    {
                        if (intItems.ContainsKey(i.ItemID))
                            intItems[i.ItemID]++;
                        else
                            intItems.Add(i.ItemID, 1);
                    }
                    else if (i.Map == Map.Felucca)
                    {
                        if (felItems.ContainsKey(i.ItemID))
                            felItems[i.ItemID]++;
                        else
                            felItems.Add(i.ItemID, 1);
                    }
                    else
                    {
                        oddMapCount++;
                    }
                }
                // sort by count
                List<KeyValuePair<int, int>> sortedIntItems = new List<KeyValuePair<int, int>>();
                foreach (KeyValuePair<int, int> kvp in intItems)
                    sortedIntItems.Add(kvp);

                sortedIntItems.Sort((e1, e2) =>
                {
                    return e2.Value.CompareTo(e1.Value);
                });

                List<KeyValuePair<int, int>> sortedFelItems = new List<KeyValuePair<int, int>>();
                foreach (KeyValuePair<int, int> kvp in felItems)
                    sortedFelItems.Add(kvp);

                sortedFelItems.Sort((e1, e2) =>
                {
                    return e2.Value.CompareTo(e1.Value);
                });

                Utility.Monitor.WriteLine("There are {0} items on the internal map.", ConsoleColor.Red, sortedIntItems.Count);
                Utility.Monitor.WriteLine("There are {0} items on the felucca map.", ConsoleColor.Red, sortedFelItems.Count);
                Utility.Monitor.WriteLine("There are {0} items on other maps.", ConsoleColor.Red, oddMapCount);

                if (sortedIntItems.Count > 0)
                    Utility.Monitor.WriteLine("Biggest offender on the internal map is ItemID {0} with a count of {1}.", ConsoleColor.Red, sortedIntItems[0].Key, sortedIntItems[0].Value);
                if (sortedFelItems.Count > 0)
                    Utility.Monitor.WriteLine("Biggest offender on the felucca map is ItemID {0} with a count of {1}.", ConsoleColor.Red, sortedFelItems[0].Key, sortedFelItems[0].Value);

                /////////////
                // the code that follows is intended for analysis in the debugger
                /////////////
                int ItemID = -1;
                Map map = Map.Felucca;
                if (sortedIntItems.Count > 0)
                {
                    ItemID = sortedIntItems[0].Key;
                    map = Map.Internal;
                }
                if (sortedFelItems.Count > 0 && sortedFelItems[0].Value > sortedIntItems[0].Value)
                {
                    ItemID = sortedFelItems[0].Key;
                    map = Map.Felucca;
                }

                Dictionary<Serial, DateTime> reportItems = new Dictionary<Serial, DateTime>();
                List<string> report = new List<string>();
                if (ItemID > -1)
                {   // catalog items
                    foreach (Item i in World.Items.Values)
                    {
                        if (i.Map == map && i.ItemID == ItemID)
                        {
                            report.Add(string.Format("{0}, created:{1}, location:{2}, parent{3}", i, i.Created, i.Location, i.Parent));
                            reportItems.Add(i.Serial, i.Created);
                        }
                    }
                }

                List<KeyValuePair<Item, DateTime>> sortedReportItems = new List<KeyValuePair<Item, DateTime>>();
                foreach (KeyValuePair<Serial, DateTime> kvp in reportItems)
                    sortedReportItems.Add(new KeyValuePair<Item, DateTime>(World.FindItem(kvp.Key), kvp.Value));

                sortedReportItems.Sort((e1, e2) =>
                {
                    return e2.Value.CompareTo(e1.Value);
                });

                return;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        [Usage("Monitor <item>")]
        [Description("Asks the item to updates us when interestings happen.")]
        public static void Monitor_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Target the item you wish to monitor.");
                e.Mobile.Target = new MonitorTarget(e);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public class MonitorTarget : Target
        {
            CommandEventArgs m_e;
            public MonitorTarget(object o)
                : base(12, false, TargetFlags.None)
            {
                m_e = (CommandEventArgs)o;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Item? item = (targeted as Item);
                Mobile? mobile = (targeted as Mobile);
                if (item != null)
                {
                    if (m_e.ArgString != null && m_e.ArgString.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        if (item.Monitors.Contains(from))
                            item.Monitors.Remove(from);

                        from.SendMessage("Monitor removed from {0}.", item);
                    }
                    else if (!item.Monitors.Contains(from))
                    {
                        item.Monitors.Add(from);
                        from.SendMessage("Monitor added from {0}.", item);
                    }
                    else if (item.Monitors.Contains(from))
                    {
                        item.Monitors.Remove(from);
                        from.SendMessage("Monitor removed from {0}.", item);
                    }
                }
                else if (mobile != null)
                {
                    if (m_e.ArgString != null && m_e.ArgString.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        if (mobile.Monitors.Contains(from))
                            mobile.Monitors.Remove(from);

                        from.SendMessage("Monitor removed from {0}.", mobile);
                    }
                    else if (!mobile.Monitors.Contains(from))
                    {
                        mobile.Monitors.Add(from);
                        from.SendMessage("Monitor added from {0}.", mobile);
                    }
                    else if (mobile.Monitors.Contains(from))
                    {
                        mobile.Monitors.Remove(from);
                        from.SendMessage("Monitor removed from {0}.", mobile);
                    }
                }
                else
                    from.SendMessage("That is neither a mobile or an item.");
            }
        }
        [Usage("Clobber [account name]")]
        [Description("Resets an account's player characters to starting skills/stats and empties bank box.")]
        public static void Clobber_OnCommand(CommandEventArgs e)
        {
            bool clobbered = false;
            try
            {
                if (e.Arguments == null || e.Length != 1)
                {
                    e.Mobile.SendMessage("Usage: Clobber [account name]");
                    return;
                }
                foreach (Account acct in Accounts.Table.Values)
                {
                    if (acct == null)
                        continue;

                    if (acct.Username.ToLower() != e.ArgString.ToLower())
                        continue;

                    for (int charndx = 0; charndx < acct.Limit; charndx++)
                    {
                        if (acct[charndx] == null)
                            break;                      // no more characters on this account

                        clobbered = true;
                        Console.WriteLine("Clobbering character {0}.", acct[charndx].Name);
                        ClobberMobile(acct[charndx]);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            if (clobbered == true)
                Console.WriteLine("Clobbering complete.");
            else
                Console.WriteLine("{0} not found.", e.ArgString);
        }
        private static void ClobberMobile(Mobile m)
        {
            // clobber stats
            m.Str = m.Str / 4;
            m.Dex = m.Dex / 4;
            m.Int = m.Int / 4;

            // clobber skills
            Server.Skills skills = m.Skills;
            for (int i = 0; i < skills.Length; ++i)
                if (skills[i].Base > 10)
                    skills[i].Base = skills[i].Base / 4;

            // clobber backpack
            if (m.BankBox != null)
            {
                List<Item> items = new List<Item>();
                items = new List<Item>(m.Backpack.Items);
                foreach (Item item in items)
                {
                    m.Backpack.RemoveItem(item);
                    item.Delete();
                }
            }

            // clobber bank box
            if (m.BankBox != null)
            {
                List<Item> items = new List<Item>();
                items = new List<Item>(m.BankBox.Items);
                foreach (Item item in items)
                {
                    m.BankBox.RemoveItem(item);
                    item.Delete();
                }
            }
        }

        [Usage("LoadJump [treasure]")]
        [Description("Load our 'jump' table with the creatures on this spawner, or with every treasure map location in random order.")]
        public static void LoadJump_OnCommand(CommandEventArgs e)
        {

            if (e.Arguments.Length > 0 && e.Arguments[0].Equals("treasure", StringComparison.OrdinalIgnoreCase))
            {
                LoadTreasureJump(e.Mobile);
                return;
            }

            try
            {
                e.Mobile.SendMessage("Target the object you wish to load from.");
                e.Mobile.Target = new LoadTarget();
            }
            catch
            {
                e.Mobile.SendMessage("There was a problem loading the jump list.");
            }

        }
        // The treasure jump-list loader shared by [LoadJump treasure, [JumpTreasureMapLocations and [GenTreasureMapLocations.
        //  Every treasure map location once, in a new random order. Reads the loaded database (TreasureMap.Locations);
        //  never regenerates it. Felucca WorldLocations at GetAverageZ, the Z treasure chests and overland encounters use.
        //  Returns the number of locations loaded (0: nothing loaded, the jump list is unchanged).
        public static int LoadTreasureJump(Mobile from)
        {
            if (from is not PlayerMobile pm)
                return 0;

            Point2D[] locations = TreasureMap.Locations;
            if (locations == null || locations.Length == 0)
            {
                from.SendMessage("No treasure map locations are loaded.");
                return 0;
            }

            Map map = Map.Felucca;
            List<Item.WorldLocation> list = new List<Item.WorldLocation>(locations.Length);
            foreach (Point2D p in locations)
                list.Add(new Item.WorldLocation(p.X, p.Y, map.GetAverageZ(p.X, p.Y), map));

            Utility.Shuffle(list);

            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList(list);

            from.SendMessage("Your jump list has been loaded with {0} treasure map locations in random order.", list.Count);
            return list.Count;
        }
        public class LoadTarget : Target
        {
            public LoadTarget()
                : base(12, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                // reset jump table
                PlayerMobile? pm = from as PlayerMobile;
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();
                if (pm != null)
                {
                    if (targeted is ChampEngine champ)
                    {
                        List<Mobile> list = new();
                        if (champ.Monsters != null)
                            foreach (Mobile m in champ.Monsters)
                                if (m != null)
                                    list.Add(m);

                        if (champ.FreeMonsters != null)
                            foreach (Mobile m in champ.FreeMonsters)
                                if (m != null)
                                    if (!list.Contains(m))
                                        list.Add(m);

                        if (list.Count != 0)
                        {
                            foreach (object ox in list)
                                if (ox is Mobile)
                                    pm.JumpList.Add(ox);

                            from.SendMessage(string.Format("Jump table loaded with {0} objects.", list.Count));
                        }
                        else
                            from.SendMessage("That champ engine has spawned no mobiles.");
                    }
                    else if (targeted is Spawner spawner)
                    {
                        if (spawner.Running != true)
                            from.SendMessage("That spawner is not running.");
                        if (spawner.Objects != null && spawner.Objects.Count != 0)
                        {
                            foreach (object ox in spawner.Objects)
                                if (ox is Item)
                                    pm.JumpList.Add((ox as Item).GetWorldLocation());
                                else if (ox is Mobile)
                                    pm.JumpList.Add(ox);
                                else
                                    pm.JumpList.Add(ox);

                            from.SendMessage(string.Format("Jump table loaded with {0} objects.", spawner.Objects.Count));
                        }
                        else
                            from.SendMessage("That spawner has spawned no objects.");
                    }
                    else if (targeted is CustomRegionControl rc)
                    {
                        int count = 0;
                        foreach (Rectangle3D rect in rc.CustomRegion.Coords)
                        {
                            pm.JumpList.Add(NormalizeZ(from.Map, rect.Start));
                            pm.JumpList.Add(NormalizeZ(from.Map, rect.End));
                            count += 2;
                        }

                        from.SendMessage(string.Format("Jump table loaded with {0} objects.", count));
                    }
                    else if (targeted is Runebook rb)
                    {
                        foreach (var o in rb.Entries)
                        {
                            RunebookEntry? entry = o as RunebookEntry;
                            Apple dummy = new Apple();
                            dummy.Location = entry.Location;
                            dummy.Map = entry.Map;
                            pm.JumpList.Add(dummy.GetWorldLocation());
                            dummy.Delete();
                        }
                    }
                    else
                    {
                        from.SendMessage("Don't know how to loadjump for {0}", targeted);
                        return;
                    }
                }

                from.SendMessage("Your jump list has been loaded with {0} objects", pm.JumpList.Count);

            }
        }

        [Usage("LoadMEOPJump")]
        [Description("Load our 'jump' table event objects that have been made permanent.")]
        public static void LoadMEOPJump_OnCommand(CommandEventArgs e)
        {

            try
            {
                PlayerMobile? pm = e.Mobile as PlayerMobile;
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();

                foreach (Item item in World.Items.Values)
                    if (Utility.IsEventObject(item))
                        if (item is FishController fc)
                        {
                            if (DateTime.Parse(fc.Event.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventTeleporter et)
                        {
                            if (DateTime.Parse(et.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventSungate es)
                        {
                            if (DateTime.Parse(es.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventSpawner esp)
                        {
                            if (DateTime.Parse(esp.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventKeywordTeleporter ekt)
                        {
                            if (DateTime.Parse(ekt.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }
                        else if (item is EventConfirmationSungate ecs)
                        {
                            if (DateTime.Parse(ecs.EventEnd) > DateTime.UtcNow + TimeSpan.FromDays(365))
                                pm.JumpList.Add(item);
                        }

                e.Mobile.SendMessage("Your jump list was loaded with {0} objects.", pm.JumpList.Count);
            }
            catch
            {
                e.Mobile.SendMessage("There was a problem loading the jump list.");
            }

        }
        private static Point3D NormalizeZ(Map map, Point3D px)
        {
            return new Point3D(px.X, px.Y, map.GetAverageZ(px.X, px.Y));
        }
        [Usage("DistanceToSqrt [X Y X Y]")]
        [Description("Calculate the distance between two points.")]
        public static void DistanceToSqrt_OnCommand(CommandEventArgs e)
        {
            try
            {
                // No arguments: interactive double-target mode
                if (e.Arguments.Length == 0)
                {
                    e.Mobile.SendMessage("Target the first location.");
                    e.Mobile.Target = new DistanceFirstTarget();
                    return;
                }

                Point2D[] points;

                try { points = GetPoints(e.ArgString); }
                catch
                {
                    e.Mobile.Say("Usage DistanceToSqrt [X Y X Y]");
                    return;
                }

                if (points.Length == 2)
                {
                    double result = Utility.GetDistanceToSqrt(points[0], points[1]);
                    e.Mobile.Say(string.Format("DistanceToSqrt returned: {0:0.00}", result));
                }
                else
                {
                    e.Mobile.Say("Usage DistanceToSqrt [X Y X Y]");
                }
            }
            catch
            {
                e.Mobile.Say("Usage DistanceToSqrt [X Y X Y]");
            }
        }
        public class DistanceFirstTarget : Target
        {
            public DistanceFirstTarget()
                : base(32, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Point3D loc = GetLocation(targeted);

                from.SendMessage("Target the second location.");
                from.Target = new DistanceSecondTarget(loc);
            }
        }
        public class DistanceSecondTarget : Target
        {
            private readonly Point3D _first;

            public DistanceSecondTarget(Point3D first)
                : base(32, true, TargetFlags.None)
            {
                _first = first;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Point3D second = GetLocation(targeted);

                double result = Utility.GetDistanceToSqrt(_first, second);

                from.Say(string.Format("DistanceToSqrt returned: {0:0.00}", result));
            }
        }
        private static Point3D GetLocation(object targeted)
        {
            if (targeted is LandTarget lt)
                return lt.Location;

            if (targeted is Item item)
                return item.Location;

            if (targeted is Mobile mob)
                return mob.Location;

            return Point3D.Zero;
        }
        private static Point2D[] GetPoints(string input)
        {
            List<Point2D> points = new List<Point2D>();
            if (!string.IsNullOrEmpty(input))
            {
                string[] sPoints = input.Split(new char[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                int index = 0;
                points.Add(Point2D.Parse(sPoints[index++] + " " + sPoints[index++]));       // first point
                if (sPoints.Length == 4)
                    points.Add(Point2D.Parse(sPoints[index++] + " " + sPoints[index]));     // second point
                else if (sPoints.Length == 5 || sPoints.Length == 6)                        // assume the third/sixth value is Z - we can't be sure if it's (X, Y, Z) (X, Y), or (X, Y) (X, Y, Z) 
                    // skip the Z
                    points.Add(Point2D.Parse(sPoints[++index] + " " + sPoints[++index]));

            }
            return points.ToArray();
        }
        [Usage("GoTrammel")]
        [Description("Moves you to Trammel at your current location.")]
        public static void GoTrammel_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null)
                return;

            e.Mobile.MoveToWorld(e.Mobile.Location, Map.Trammel);
        }
        [Usage("GoFelucca")]
        [Description("Moves you to Felucca at your current location.")]
        public static void GoFelucca_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null)
                return;

            e.Mobile.MoveToWorld(e.Mobile.Location, Map.Felucca);
        }
        [Usage("RemoveBackpackHue")]
        [Description("Removed any hue associated with your backpack.")]
        public static void RemoveBackpackHue_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null || e.Mobile.Backpack == null) return;

            if (e.Mobile.Backpack.Hue == 0)
                return;

            e.Mobile.Backpack.Hue = 0;
        }
        [Usage("SaveBackpack")]
        [Description("Backs up your backpack.")]
        public static void SaveBackpack_OnCommand(CommandEventArgs e)
        {
            bool shard_allow = Core.Debug || Core.GMNTC_CFG;
            PlayerMobile? pm = e.Mobile as PlayerMobile;
            try
            {
                if (shard_allow || e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                {
                    if (BackpackRecovery.ContainsKey(pm))
                    {
                        if (pm.Backpack != null)
                            e.Mobile.SendMessage("Overwriting your saved backpack...");
                        else
                        {
                            e.Mobile.SendMessage("No backpack");
                            return;
                        }
                    }
                    else
                        BackpackRecovery.Add(pm, new List<Item>());


                    BackpackRecovery[pm] = new();
                    BackpackRecovery[pm].AddRange(Utility.GetEquippedItems(pm));
                    BackpackRecovery[pm].AddRange(Utility.GetBackpackItems(pm));

                    e.Mobile.SendMessage("Done. Backed up {0} items", BackpackRecovery[pm].Count);
                }
                else
                {
                    e.Mobile.SendMessage("You are not authorized to use this command on this shard.");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        [Usage("RestoreBackpack")]
        [Description("Restores your backpack to its saved state. Any extra items will be deleted.")]
        public static void RestoreBackpack_OnCommand(CommandEventArgs e)
        {
            bool shard_allow = Core.Debug || Core.GMNTC_CFG;
            PlayerMobile? pm = e.Mobile as PlayerMobile;
            try
            {
                if (shard_allow || e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                {
                    if (!BackpackRecovery.ContainsKey(pm))
                        e.Mobile.SendMessage("No saved backpack");
                    else if (pm.Backpack == null)
                        e.Mobile.SendMessage("No backpack");
                    else
                    {
                        List<Item> to_delete = new();
                        foreach (Item item in pm.Backpack.Items)
                            if (!BackpackRecovery[pm].Contains(item))
                                to_delete.Add(item);

                        foreach (Item item in to_delete)
                            item.Delete();
                    }
                }
                else
                {
                    e.Mobile.SendMessage("You are not authorized to use this command on this shard.");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        [Usage("RefreshBankBox")]
        [Description("Drops your bankbox to the ground and generates a fresh one.")]
        public static void RefreshBankBox_OnCommand(CommandEventArgs e)
        {
            try
            {
                //e.Mobile.SendMessage("Begin nuclearization.");
                RefreshBankBoxWorker(e);
                //e.Mobile.SendMessage("Nuclearization complete with {0} items processed.", count);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public static void RefreshBankBoxWorker(CommandEventArgs e)
        {

            if (e.Mobile.BankBox == null || e.Mobile.BankBox.Deleted)
            {
                e.Mobile.SendMessage("You do not have a bank box.");
                return;
            }

            e.Mobile.BankBox = e.Mobile.FindItemOnLayer(Layer.Bank) as BankBox;

            if (e.Mobile.BankBox != null)
            {
                e.Mobile.BankBox.MoveToWorld(e.Mobile.Location, e.Mobile.Map);
                e.Mobile.BankBox = null;
                e.Mobile.AddItem(e.Mobile.BankBox = new BankBox(e.Mobile));
                e.Mobile.SendMessage("Bank box refreshed.");
            }
        }

        #region RecalcBankBoxes
        [Usage("RecalcBankBoxes")]
        [Description("Recalculates the cached item, weight and gold totals of every player's bank box (online and offline) " +
            "from its actual contents, and reports each box's capacity slot usage. Bank contents are never changed.")]
        public static void RecalcBankBoxes_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            int boxes = 0, changed = 0, differs = 0, full = 0, failed = 0;
            LogHelper logger = new LogHelper("RecalcBankBoxes.log", false, true);
            try
            {
                logger.Log(LogType.Text, string.Format("RecalcBankBoxes by {0}", from));

                // a snapshot: nothing here adds or removes mobiles, but the live collection must never be enumerated
                //  while other code could change it
                foreach (Mobile m in new List<Mobile>(World.Mobiles.Values))
                {
                    if (m is not PlayerMobile || m.Deleted)
                        continue;

                    BankBox? box = m.FindBankNoCreate();    // never creates a bank box
                    if (box == null || box.Deleted)
                        continue;

                    try
                    {
                        boxes++;
                        int items = box.TotalItems, weight = box.TotalWeight, gold = box.TotalGold;

                        RecalcTotals(box);

                        if (box.TotalItems != items || box.TotalWeight != weight || box.TotalGold != gold)
                        {
                            changed++;
                            box.InvalidateProperties();
                            logger.Log(LogType.Mobile, m, string.Format("totals corrected: items {0} -> {1}, weight {2} -> {3}, gold {4} -> {5}",
                                items, box.TotalItems, weight, box.TotalWeight, gold, box.TotalGold));
                        }

                        // what the capacity check counts (Container.CheckHold): a potion costs one slot per potion, at
                        //  any depth, while TotalItems (the number players see) counts a potion stack once
                        int slots = SlotUsage(box);
                        if (slots != box.TotalItems)
                        {
                            differs++;
                            logger.Log(LogType.Mobile, m, string.Format("shows {0} items but uses {1} of {2} slots ({3} extra slots from potion stacks)",
                                box.TotalItems, slots, box.MaxItems, slots - box.TotalItems));
                        }
                        if (box.MaxItems > 0 && slots >= box.MaxItems)
                        {
                            full++;
                            logger.Log(LogType.Mobile, m, string.Format("bank box full: {0} of {1} slots", slots, box.MaxItems));
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        LogHelper.LogException(ex);
                        logger.Log(LogType.Mobile, m, "recalculation failed: " + ex.Message);
                    }
                }
            }
            finally
            {
                string summary = string.Format("RecalcBankBoxes: {0} bank boxes processed, {1} totals corrected, {2} using more slots than they show, {3} full, {4} failed.",
                    boxes, changed, differs, full, failed);
                logger.Log(LogType.Text, summary);
                logger.Finish();
                from.SendMessage(summary);
                from.SendMessage("Details are in RecalcBankBoxes.log.");
            }
        }

        // Item.UpdateTotals is the authoritative, recursive recount (each nested item first, then the sums). It also
        //  stamps LastAccessed on every item it visits, which other code reads (TrashChest, instrument selection); this recount restores those stamps so it
        //  changes nothing but the cached totals.
        private static void RecalcTotals(BankBox box)
        {
            List<(Item item, DateTime accessed)> stamps = new();
            CollectStamps(box, stamps);

            box.UpdateTotals();

            foreach ((Item item, DateTime accessed) in stamps)
                item.LastAccessed = accessed;
        }

        private static void CollectStamps(Item item, List<(Item item, DateTime accessed)> stamps)
        {
            stamps.Add((item, item.LastAccessed));
            foreach (Item child in item.Items)
                CollectStamps(child, stamps);
        }

        // the slots the box's contents occupy, as Container.CheckHold computes them (ComputeContainerSlotUsage)
        private static int SlotUsage(BankBox box)
        {
            int slots = 0;
            foreach (Item item in box.Items)
                if (item != null && !item.Deleted)
                    slots += Container.GetSlotCost(item);   // 0 for virtual items
            return slots;
        }
        #endregion RecalcBankBoxes

        public class SetZRegionControl : CustomRegionControl
        {
            private int m_SetZ;
            [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
            public int SetZ { get { return m_SetZ; } set { m_SetZ = value; } }
            public override string DefaultName { get { return "SetZ System Region Control"; } }

            [Constructable]
            public SetZRegionControl()
            : base()
            {
                Movable = true;
                Visible = true;
                if (this.CustomRegion != null)
                    this.CustomRegion.Name = "SetZ region";
            }
            public SetZRegionControl(Serial serial) : base(serial) { }
            public override void OnEnter(Mobile m)
            {
                m.Z = m_SetZ;
                base.OnEnter(m);
            }
            public override void OnSingleClick(Mobile m)
            {
                base.OnSingleClick(m);
                LabelTo(m, string.Format("({0})", CustomRegion.Map));
            }
            public override Item Dupe(int amount)
            {   // when duping a CustomRegionControl, we don't actually want the region itself as it's already
                //  been 'registered' with its own UId.
                // The region carries all the following info, which we will need for our dupe
                SetZRegionControl new_crc = new();
                if (CustomRegion != null)
                {
                    Utility.CopyProperties(new_crc.CustomRegion, CustomRegion);
                    new_crc.CustomRegion.Coords = new(CustomRegion.Coords);
                }
                return base.Dupe(new_crc, amount);
            }
            //override O
            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);
                int version = 0;
                writer.Write(0);

                // version 0
                writer.Write(m_SetZ);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        m_SetZ = reader.ReadInt();
                        break;
                }

            }
        }
        // Re-evaluates and applies shard-dependent player state across all active PlayerMobiles.
        // 
        // The Mortal flag is not treated as persisted truth; it is derived from the current RuleSet.
        // This method normalizes all players to match the active shard configuration (e.g., Mortalis vs others).
        //
        // Safe to call multiple times (idempotent). Used both at startup and during live configuration changes.
        public static void ShardInit()
        {
            if (Core.RuleSets.AllShards)
                // throw away movement packets as we are trying to teleport
                CoreAI.PostTeleportMoveLockMs = 0.0;

            /* New Server */
            // Even those thses settings can be set in game, we enforce these settings here on startup.
            // these settings aren't something that should be tweakable in-game.
            // You will be able to tweak them in the current run-session, but next restart they will reset to these values.
            //  If you really want to change these core properties, do it here.
            switch (Core.ServerID)
            {
                case ServerID.AngelIsland:
                    CoreAI.MaxAccountsPerIP = 3;
                    CoreAI.MaxAccountsPerMachine = 3;
                    CoreAI.MaxConcurrentAddresses = 3;
                    CoreAI.MaxCharactersPerAccount = 5;
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                    break;
                case ServerID.SiegePerilous:
                    CoreAI.MaxAccountsPerIP = 1;
                    CoreAI.MaxAccountsPerMachine = 1;
                    CoreAI.MaxConcurrentAddresses = 1;
                    CoreAI.MaxCharactersPerAccount = 1;
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                    break;
                case ServerID.Mortalis:
                    CoreAI.MaxAccountsPerIP = 1;
                    CoreAI.MaxAccountsPerMachine = 1;
                    CoreAI.MaxConcurrentAddresses = 1;
                    CoreAI.MaxCharactersPerAccount = 1;
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                    break;
                case ServerID.Seasons:
                    if (Core.SeasonVariant == 2)
                    {
                        CoreAI.MaxAccountsPerIP = 1;
                        CoreAI.MaxAccountsPerMachine = 1;
                        CoreAI.MaxConcurrentAddresses = 1;
                        CoreAI.MaxCharactersPerAccount = 5; // changed from 3 to 5 (Curt, 6/14/2926)
                        CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                        CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                    }
                    else if (Core.SeasonVariant == 3)
                    {
                        CoreAI.MaxAccountsPerIP = 1;
                        CoreAI.MaxAccountsPerMachine = 1;
                        CoreAI.MaxConcurrentAddresses = 1;
                        CoreAI.MaxCharactersPerAccount = 5;
                        CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                        CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                    }
                    else
                    {   // Default: Season 1
                        CoreAI.MaxAccountsPerIP = 3;
                        CoreAI.MaxAccountsPerMachine = 3;
                        CoreAI.MaxConcurrentAddresses = 3;
                        CoreAI.MaxCharactersPerAccount = 5;
                        CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                        CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                    }
                    break;
                case ServerID.Upheaval:
                    CoreAI.MaxAccountsPerIP = 1;
                    CoreAI.MaxAccountsPerMachine = 1;
                    CoreAI.MaxConcurrentAddresses = 1;
                    CoreAI.MaxCharactersPerAccount = 1;
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                    break;
                default:
                    CoreAI.MaxAccountsPerIP = 1;
                    CoreAI.MaxAccountsPerMachine = 1;
                    CoreAI.MaxConcurrentAddresses = 1;
                    CoreAI.MaxCharactersPerAccount = 5;
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                    break;
            }
            // During development testing, we want actual configuration
            if (Core.RuleSets.TestCenterRules() && !Core.Debug)
            {
                CoreAI.MaxAccountsPerIP = 8;
                CoreAI.MaxAccountsPerMachine = 8;
                CoreAI.MaxConcurrentAddresses = 8;
                CoreAI.MaxCharactersPerAccount = 5;
                CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerGuild);
                CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
            }

            // set mortal status && congigure owner's look/title, etc
            foreach (var pm in PlayerMobile.Instances)
            {
                if (pm is null || pm.Deleted)
                    continue;

                if (pm.Account is not Account acct)
                    continue;

                if (pm.AccessLevel == AccessLevel.Player)
                    if (RuleSets.MortalisRules())
                        pm.Mortal = true;
                    else
                        pm.Mortal = false;

                // Refresh GodlyRobe effects (title, fame/karma, robe color) on login
                // This ensures everything is correct even if server config changed or robe was already equipped.
                if (pm.FindItemOnLayer(Layer.OuterTorso) is GodlyRobe robe)
                {   // nothing works. need to close character gump and reopen
                    GodlyRobe.RefreshGodlyEffects(pm);
                }
            }
        }
        #region Serialization
        private static Dictionary<PlayerMobile, List<Item>> BackpackRecovery = new();
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("BackpackRecovery Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/BackpackRecovery.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            int count = BackpackRecovery.Count;             // number of records
                            writer.Write(count);
                            foreach (var kvp in BackpackRecovery)
                            {
                                writer.Write(kvp.Key);                      // write the mobile
                                writer.Write(kvp.Value.Count);              // number of items

                                foreach (var el in kvp.Value)
                                    writer.Write(el);
                            }

                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Saves/BackpackRecovery.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        public static void Load()
        {
            if (File.Exists("Saves/BackpackRecovery.bin"))
                Console.WriteLine("Backpack Recovery Loading...");
            else
            {
                Console.WriteLine("No Backpack Recovery elements to Load.");
                return;
            }
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/BackpackRecovery.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int record_count = reader.ReadInt();           // number of records
                            for (int ix = 0; ix < record_count; ix++)
                            {
                                List<Item> items = new();
                                Mobile? m = reader.ReadMobile();             // the mobile
                                int item_count = reader.ReadInt();          // number of items
                                for (int jx = 0; jx < item_count; jx++)
                                {
                                    Item? item = reader.ReadItem();
                                    if (item != null)
                                        items.Add(item);
                                }
                                if (m != null)
                                    BackpackRecovery.Add(m as PlayerMobile, items);
                            }

                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid BackpackRecovery.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Saves/BackpackRecovery.bin, using default values:", ConsoleColor.Red);
            }
        }
        #endregion Serialization
    }
}