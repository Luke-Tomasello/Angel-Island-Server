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

/* Scripts\Engines\CourierSystem\Commands.cs
 * Changelog
 *  10/5/2026
 *  Added [CourierNetworkStatus] diagnostic.
 *  8/6/2026, Adam
 *  Created.
 */

using Server.Engines.TerritoryControl;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Server.Engines.CourierSystem
{
    public static class CourierCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register(
                "FindFriendly",
                AccessLevel.GameMaster,
                new CommandEventHandler(FindFriendly_OnCommand));

            CommandSystem.Register(
                "GenBag",
                AccessLevel.GameMaster,
                new CommandEventHandler(GenBag_OnCommand));

            CommandSystem.Register(
                "CourierNetworkStatus",
                AccessLevel.GameMaster,
                new CommandEventHandler(CourierNetworkStatus_OnCommand));

            CommandSystem.Register(
                "UnwindCourierTransactions",
                AccessLevel.Owner,
                new CommandEventHandler(UnwindCourierTransactions_OnCommand));
        }

        [Usage("CourierNetworkStatus")]
        [Description(
            "Audits Courier network registration for the personality vendors in your current TerritoryControl district.")]
        private static void CourierNetworkStatus_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            TerritoryDistrict? district = TerritorySystem.FindDistrict(from);

            if (district == null || district.Map == null || district.Map == Map.Internal ||
                !district.Map.Regions.TryGetValue(district.Name, out Region? region))
            {
                from.SendMessage("You are not standing in a TerritoryControl district.");
                return;
            }

            /*
             * Scope: every BaseVendor physically inside the district's region,
             * whether or not the vendor currently resolves to that district
             * (e.g. a vendor in a house region is in scope but not valid).
             *
             * Expected membership derives only from the vendor itself:
             * IntrinsicPersonality (Serial % 20) and registration eligibility.
             * Actual membership is then checked separately against the lists.
             */
            int personalityVendors = 0;
            int valid = 0;
            int correct = 0;

            List<string> missing = new List<string>();
            List<string> inactiveRegistered = new List<string>();
            List<string> wrongNetwork = new List<string>();
            List<string> unexpected = new List<string>();

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is not BaseVendor vendor || vendor.Deleted)
                    continue;

                if (vendor.Map != district.Map || !region.Contains(vendor.Location))
                    continue;

                VendorPersonality intrinsic = vendor.IntrinsicPersonality;
                IReadOnlyList<BaseVendor>? expectedNetwork = GetIntrinsicNetwork(intrinsic);

                bool inOrc = BaseVendor.OrcNetwork.Contains(vendor);
                bool inHuman = BaseVendor.HumanNetwork.Contains(vendor);

                string actual = inOrc && inHuman ? "Orc+Human Networks"
                    : inOrc ? "Orc Network"
                    : inHuman ? "Human Network"
                    : "no network";

                string line = string.Format(
                    "{0}: {1} {2} - {3} - {4}",
                    vendor.Serial,
                    GetVendorName(vendor),
                    GetVendorTitle(vendor),
                    intrinsic,
                    expectedNetwork == null ? actual : GetNetworkName(expectedNetwork));

                // Not a network personality (or not an eligible vendor type):
                // it should not be registered anywhere.
                if (expectedNetwork == null || !vendor.PersonalityRegistrationEligible)
                {
                    if (inOrc || inHuman)
                        unexpected.Add(line + " (registered in " + actual + ")");

                    continue;
                }

                personalityVendors++;

                bool inExpected = expectedNetwork == BaseVendor.OrcNetwork ? inOrc : inHuman;
                bool inOther = expectedNetwork == BaseVendor.OrcNetwork ? inHuman : inOrc;

                if (inOther)
                    wrongNetwork.Add(line + " (registered in " + actual + ")");

                if (vendor.ValidForPersonality)
                {
                    valid++;

                    if (!inExpected)
                        missing.Add(line);
                    else if (!inOther)
                        correct++;
                }
                else if (inOrc || inHuman)
                {
                    // Normal: registration reflects intrinsic personality, while
                    // participation is decided live by ValidForPersonality.
                    inactiveRegistered.Add(line + " (registered in " + actual + ")");
                }
            }

            from.SendMessage(0x40, "Courier Network Status - {0}", district.Name);
            from.SendMessage("Personality vendors:  {0}", personalityVendors);
            from.SendMessage("Valid:                {0}", valid);
            from.SendMessage("Registered correctly: {0}", correct);
            from.SendMessage("Missing:              {0}", missing.Count);
            from.SendMessage("Registered, inactive: {0} (normal)", inactiveRegistered.Count);
            from.SendMessage("Wrong network:        {0}", wrongNetwork.Count);
            from.SendMessage("Unexpected registered: {0}", unexpected.Count);

            if (missing.Count + wrongNetwork.Count + unexpected.Count == 0)
            {
                from.SendMessage(0x40, "No discrepancies found.");
                return;
            }

            SendSection(from, "MISSING:", missing);
            SendSection(from, "REGISTERED, INACTIVE (normal, not a discrepancy):", inactiveRegistered);
            SendSection(from, "WRONG NETWORK:", wrongNetwork);
            SendSection(from, "UNEXPECTED REGISTERED:", unexpected);
        }

        private static IReadOnlyList<BaseVendor>? GetIntrinsicNetwork(VendorPersonality personality)
        {
            switch (personality)
            {
                case VendorPersonality.OrcSympathizer:
                case VendorPersonality.OrcSpy:
                    return BaseVendor.OrcNetwork;

                case VendorPersonality.HumanSympathizer:
                case VendorPersonality.HumanSpy:
                    return BaseVendor.HumanNetwork;

                default:
                    return null;
            }
        }

        private static string GetNetworkName(IReadOnlyList<BaseVendor> network)
        {
            return network == BaseVendor.OrcNetwork ? "Orc Network" : "Human Network";
        }

        private static void SendSection(Mobile from, string header, List<string> lines)
        {
            if (lines.Count == 0)
                return;

            from.SendMessage(0x22, header);

            foreach (string line in lines)
                from.SendMessage(line);
        }

        #region Unwind Courier Transactions

        /*
         * Owner testing tool. Unwinds courier transactions so characters can be
         * re-tested without restoring a save. Courier documents are deleted through
         * their normal OnDelete, which also deletes unpaid held rewards and clears
         * the owning player's active assignment. Delivered rewards are never touched.
         */

        [Usage("UnwindCourierTransactions [-all]")]
        [Description(
            "Owner testing tool: unwinds courier transactions for a targeted player or source vendor, or for the whole world with -all.")]
        private static void UnwindCourierTransactions_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            string? arg = e.ArgString?.Trim().ToLowerInvariant();

            if (arg == "all" || arg == "-all")
            {
                // Same safety model as [TimeToDie -all]: warn, then require "yes".
                from.SendMessage(0x25, "WARNING: This deletes ALL courier documents in the world, including players' active deliveries and their unpaid rewards.");
                from.SendMessage(0x25, "It also clears every courier cooldown and vendor restock. This cannot be undone.");
                from.SendMessage(0x3B2, "Type 'yes' to continue.");
                from.Prompt = new UnwindAllCourierPrompt();
                return;
            }

            if (!string.IsNullOrEmpty(arg))
            {
                from.SendMessage("Usage: [UnwindCourierTransactions [-all]");
                return;
            }

            from.SendMessage("Target a player or a source vendor whose courier transactions should be unwound.");
            from.BeginTarget(-1, false, Server.Targeting.TargetFlags.None, UnwindCourier_OnTarget);
        }

        private static void UnwindCourier_OnTarget(Mobile from, object targeted)
        {
            if (targeted is PlayerMobile pm)
                UnwindPlayer(from, pm);
            else if (targeted is BaseVendor vendor)
                UnwindVendor(from, vendor);
            else
                from.SendMessage("That is neither a player nor a vendor.");
        }

        private static void UnwindPlayer(Mobile from, PlayerMobile pm)
        {
            UnwindCounts counts = new UnwindCounts();

            // Every document entrusted to this player, wherever it is in the world.
            DeleteLedgers(FindLedgers(ledger => ledger.CourierSerial == pm.Serial), counts);

            // Anything still referenced now points at a missing document.
            if (pm.ActiveCourierItemSerial != Serial.MinusOne)
            {
                pm.ActiveCourierItemSerial = Serial.MinusOne;
                counts.ActiveReferences++;
            }

            if (BaseVendor.ClearCourierCooldown(pm))
                counts.Cooldowns++;

            // Only restocks this player's assignments started (restock Context).
            counts.Restocks += BaseVendor.ClearCourierRestocksFor(pm);

            Report(from, $"Unwound courier transactions for {pm.Name}", counts);
        }

        private static void UnwindVendor(Mobile from, BaseVendor vendor)
        {
            UnwindCounts counts = new UnwindCounts();

            // Documents this vendor handed out; not those merely destined for it.
            List<Mobile> activeOwners =
                DeleteLedgers(FindLedgers(ledger => ledger.SourceVendorSerial == vendor.Serial), counts);

            /*
             * A player holds one active assignment at a time and the cooldown starts
             * when it is accepted, so a cooldown is attributable to this vendor only
             * when the deleted document was that player's active assignment.
             */
            foreach (Mobile owner in activeOwners)
            {
                if (BaseVendor.ClearCourierCooldown(owner))
                    counts.Cooldowns++;
            }

            if (BaseVendor.ClearCourierRestock(vendor))
                counts.Restocks++;

            Report(from, $"Unwound courier transactions sourced from {GetVendorName(vendor)}", counts);
        }

        private static List<VendorCourierItem> FindLedgers(Predicate<VendorCourierItem>? match)
        {
            List<VendorCourierItem> ledgers = new List<VendorCourierItem>();

            foreach (Item item in World.Items.Values)
            {
                if (item is VendorCourierItem ledger && !ledger.Deleted && (match == null || match(ledger)))
                    ledgers.Add(ledger);
            }

            return ledgers;
        }

        /// <summary>
        /// Deletes the documents through their normal OnDelete cleanup and returns the
        /// players whose active assignment was among them.
        /// </summary>
        private static List<Mobile> DeleteLedgers(List<VendorCourierItem> ledgers, UnwindCounts counts)
        {
            List<Mobile> activeOwners = new List<Mobile>();

            foreach (VendorCourierItem ledger in ledgers)
            {
                if (ledger.Deleted)
                    continue;

                Item? heldReward = ledger.Redeemed ? null : ledger.RewardItem;

                if (ledger.Courier is PlayerMobile owner && owner.ActiveCourierItemSerial == ledger.Serial)
                {
                    activeOwners.Add(owner);
                    counts.ActiveReferences++;
                }

                ledger.Delete();
                counts.Ledgers++;

                if (heldReward != null && heldReward.Deleted)
                    counts.HeldRewards++;
            }

            return activeOwners;
        }

        private class UnwindCounts
        {
            public int Ledgers;
            public int HeldRewards;
            public int ActiveReferences;
            public int Cooldowns;
            public int Restocks;
            public int Orphans = -1;    // reported only by -all
        }

        private static void Report(Mobile from, string title, UnwindCounts counts)
        {
            from.SendMessage(0x40, title + ":");
            from.SendMessage("  Documents deleted: {0}, held rewards deleted: {1}", counts.Ledgers, counts.HeldRewards);
            from.SendMessage("  Active references cleared: {0}", counts.ActiveReferences);
            from.SendMessage("  Player cooldowns cleared: {0}, vendor restocks cleared: {1}", counts.Cooldowns, counts.Restocks);

            if (counts.Orphans >= 0)
                from.SendMessage("  Orphaned held rewards removed: {0}", counts.Orphans);
        }

        private class UnwindAllCourierPrompt : Server.Prompts.Prompt
        {
            public override void OnResponse(Mobile from, string text)
            {
                if (!string.Equals(text?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                {
                    from.SendMessage("UnwindCourierTransactions cancelled.");
                    return;
                }

                UnwindCounts counts = new UnwindCounts { Orphans = 0 };

                DeleteLedgers(FindLedgers(null), counts);

                // Clear any remaining (stale) references, including logged-out characters.
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerMobile pm && pm.ActiveCourierItemSerial != Server.Serial.MinusOne)
                    {
                        pm.ActiveCourierItemSerial = Server.Serial.MinusOne;
                        counts.ActiveReferences++;
                    }
                }

                BaseVendor.ResetAllCourierThrottles(out int cooldowns, out int restocks);
                counts.Cooldowns = cooldowns;
                counts.Restocks = restocks;

                /*
                 * Positively identified orphans: courier reward bags still in held storage
                 * (IsIntMapStorage is set when a reward is escrowed and cleared when it is
                 * delivered) with no container. No courier documents remain to own them.
                 */
                List<Item> orphans = new List<Item>();

                foreach (Item item in World.Items.Values)
                {
                    if (item is VendorCourierBag bag && !bag.Deleted && bag.IsIntMapStorage &&
                        bag.Map == Map.Internal && bag.Parent == null)
                    {
                        orphans.Add(bag);
                    }
                }

                foreach (Item orphan in orphans)
                {
                    orphan.Delete();
                    counts.Orphans++;
                }

                Report(from, "Unwound ALL courier transactions", counts);
            }
        }

        #endregion Unwind Courier Transactions

        [Usage("FindFriendly <vendor type>")]
        [Description(
            "Finds friendly vendors of the specified type on the current map and loads them into the jump list.")]
        private static void FindFriendly_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is not PlayerMobile pm)
                return;

            string? search = e.ArgString?.Trim();

            if (string.IsNullOrWhiteSpace(search))
            {
                pm.SendMessage("Usage: [FindFriendly <vendor type>");
                return;
            }

            List<Type> matches = FindVendorTypes(search);

            if (matches.Count == 0)
            {
                pm.SendMessage(
                    "No vendor type matching \"{0}\" was found.",
                    search);

                return;
            }

            Type vendorType = ResolveVendorType(
                pm,
                search,
                matches);

            if (vendorType == null)
                return;

            bool orcAligned = BaseVendor.IsOrcAligned(pm);

            List<BaseVendor> vendors = FindFriendlyVendors(pm, vendorType);

            vendors.Sort(CompareVendors);

            pm.JumpList = new ArrayList();
            pm.JumpIndex = 0;
            pm.JumpList.AddRange(vendors);

            string allegiance = orcAligned
                ? "Orc"
                : "human";

            if (vendors.Count == 0)
            {
                pm.SendMessage(
                    "No {0}-friendly {1} vendors were found on {2}.",
                    allegiance,
                    vendorType.Name,
                    pm.Map);

                return;
            }

            pm.SendMessage(
                "Found {0} {1}-friendly {2} vendor{3} on {4}:",
                vendors.Count,
                allegiance,
                vendorType.Name,
                vendors.Count == 1 ? string.Empty : "s",
                pm.Map);

            foreach (BaseVendor vendor in vendors)
            {
                pm.SendMessage(
                    "{0}: {1} {2} — {3} — {4}",
                    vendor.Serial,
                    GetVendorName(vendor),
                    GetVendorTitle(vendor),
                    GetVendorLocation(vendor),
                    vendor.Personality);
            }

            pm.SendMessage(
                "Your jump list now contains {0} vendor{1}.",
                vendors.Count,
                vendors.Count == 1 ? string.Empty : "s");
        }

        [Usage("GenBag <vendor type>")]
        [Description(
    "Generates a courier reward by selecting a friendly source vendor of the specified type, " +
    "selecting a real courier destination, calculating the reward level, and placing the reward in your backpack.")]
        private static void GenBag_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is not PlayerMobile pm)
                return;

            string? search = e.ArgString?.Trim();

            if (string.IsNullOrWhiteSpace(search))
            {
                pm.SendMessage("Usage: [GenBag <vendor type>");
                return;
            }

            /*
             * Resolve the requested source vendor type using the same
             * machinery as FindFriendly.
             */
            List<Type> matches = FindVendorTypes(search);

            if (matches.Count == 0)
            {
                pm.SendMessage(
                    "No vendor type matching \"{0}\" was found.",
                    search);

                return;
            }

            Type vendorType = ResolveVendorType(
                pm,
                search,
                matches);

            if (vendorType == null)
                return;

            /*
             * Find all friendly source vendors of this type on the
             * commanding GM's current map.
             */
            List<BaseVendor> sources =
                FindFriendlyVendors(pm, vendorType);

            if (sources.Count == 0)
            {
                pm.SendMessage(
                    "No friendly {0} vendors were found on {1}.",
                    vendorType.Name,
                    pm.Map);

                return;
            }

            /*
             * Select an actual source vendor.
             */
            BaseVendor source =
                sources[Utility.Random(sources.Count)];

            /*
             * Use the real courier destination-selection machinery.
             */
            BaseVendor destination =
                source.FindCourierDestination(pm);

            if (destination == null)
            {
                pm.SendMessage(
                    "{0} ({1}) has no valid courier destination.",
                    source.Name,
                    source.GetType().Name);

                return;
            }

            /*
             * Run the real courier reward calculation.
             */
            CourierRewardCalculation calculation =
                CourierRewardCalculator.CalculateDetails(
                    source,
                    destination,
                    pm);

            /*
             * Run the real reward factory. The destination's live
             * inventory determines the eligible reward category.
             */
            Item reward =
                CourierRewardFactory.CreateReward(
                    destination,
                    calculation.RewardLevel);

            if (reward == null || reward.Deleted)
            {
                pm.SendMessage(
                    "CourierRewardFactory failed to create a reward.");

                return;
            }

            /*
             * This command intentionally bypasses escrow. We are testing
             * the generated reward itself, so deliver it directly.
             */
            if (!pm.AddToBackpack(reward))
            {
                reward.MoveToWorld(pm.Location, pm.Map);

                pm.SendMessage(
                    "Your backpack is full; the reward was placed at your feet.");
            }

            /*
             * Detailed diagnostics are the whole point of this command.
             */
            pm.SendMessage(
                "Courier reward generated:");

            pm.SendMessage(
                "  Source: {0} ({1}) - {2}",
                source.Name,
                source.GetType().Name,
                GetVendorLocation(source));

            pm.SendMessage(
                "  Destination: {0} ({1}) - {2}",
                destination.Name,
                destination.GetType().Name,
                GetVendorLocation(destination));

            pm.SendMessage(
                "  Distance: {0} tiles, {1}  (+{2})",
                calculation.Distance,
                calculation.DistanceBonus switch
                {
                    0 => "local",
                    1 => "inter-town",
                    _ => "remote"
                },
                calculation.DistanceBonus);

            pm.SendMessage(
                "  Threat: {0}  (+{1})",
                calculation.ThreatLevel,
                calculation.ThreatBonus);

            pm.SendMessage(
                "  Reward Level: {0}",
                calculation.RewardLevel);

            if (reward is VendorCourierBag bag)
            {
                pm.SendMessage(
                    "  Reward Type: {0}",
                    bag.RewardType);
            }
        }

        private static List<BaseVendor> FindFriendlyVendors(
            PlayerMobile pm,
            Type vendorType)
        {
            List<BaseVendor> vendors =
                new List<BaseVendor>();

            if (pm == null || vendorType == null)
                return vendors;

            bool orcAligned = BaseVendor.IsOrcAligned(pm);

            IReadOnlyList<BaseVendor> network = orcAligned
                ? BaseVendor.OrcNetwork
                : BaseVendor.HumanNetwork;

            foreach (BaseVendor vendor in network)
            {
                if (vendor == null)
                    continue;

                if (vendor.Deleted)
                    continue;

                if (!vendor.ValidForPersonality)
                    continue;

                if (vendor.Map != pm.Map)
                    continue;

                if (!vendorType.IsAssignableFrom(vendor.GetType()))
                    continue;

                vendors.Add(vendor);
            }

            return vendors;
        }

        private static Type? ResolveVendorType(
            PlayerMobile pm,
            string search,
            List<Type> matches)
        {
            /*
             * Prefer an exact class-name match.
             */
            foreach (Type type in matches)
            {
                if (string.Equals(
                    type.Name,
                    search,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return type;
                }
            }

            if (matches.Count == 1)
                return matches[0];

            pm.SendMessage(
                "\"{0}\" matches multiple vendor types:",
                search);

            matches.Sort(
                delegate (Type left, Type right)
                {
                    return string.Compare(
                        left.Name,
                        right.Name,
                        StringComparison.OrdinalIgnoreCase);
                });

            foreach (Type type in matches)
                pm.SendMessage("  {0}", type.Name);

            pm.SendMessage(
                "Please use a more specific vendor type.");

            return null;
        }

        private static List<Type> FindVendorTypes(string search)
        {
            List<Type> matches = new List<Type>();
            HashSet<Type> seen = new HashSet<Type>();

            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }
                catch
                {
                    continue;
                }

                if (types == null)
                    continue;

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    if (type.IsAbstract)
                        continue;

                    if (!typeof(BaseVendor).IsAssignableFrom(type))
                        continue;

                    if (type.Name.IndexOf(
                        search,
                        StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (seen.Add(type))
                        matches.Add(type);
                }
            }

            return matches;
        }

        private static int CompareVendors(
            BaseVendor left,
            BaseVendor right)
        {
            int result = string.Compare(
                GetVendorLocation(left),
                GetVendorLocation(right),
                StringComparison.OrdinalIgnoreCase);

            if (result != 0)
                return result;

            result = string.Compare(
                GetVendorName(left),
                GetVendorName(right),
                StringComparison.OrdinalIgnoreCase);

            if (result != 0)
                return result;

            return left.Serial.Value.CompareTo(
                right.Serial.Value);
        }

        private static string GetVendorName(BaseVendor vendor)
        {
            if (vendor == null ||
                string.IsNullOrWhiteSpace(vendor.Name))
            {
                return "(unnamed)";
            }

            return vendor.Name;
        }

        private static string GetVendorTitle(BaseVendor vendor)
        {
            if (vendor == null ||
                string.IsNullOrWhiteSpace(vendor.Title))
            {
                return string.Empty;
            }

            return vendor.Title;
        }

        private static string GetVendorLocation(BaseVendor vendor)
        {
            if (vendor == null)
                return "(unknown location)";

            if (vendor.District != null &&
                !string.IsNullOrWhiteSpace(vendor.District.Name))
            {
                return vendor.District.Name;
            }

            if (vendor.Region != null &&
                !string.IsNullOrWhiteSpace(vendor.Region.Name))
            {
                return vendor.Region.Name;
            }

            return string.Format(
                "{0}, {1}, {2}",
                vendor.X,
                vendor.Y,
                vendor.Z);
        }
    }
}