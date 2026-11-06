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

/* Scripts\Mobiles\PlayerMobile.Death.cs
 * ChangeLog:
 *  6/6/2026, Adam
 *      first check-in
 */

using Server.Accounting;
using Server.Engines;
using Server.Engines.DataRecorder;
using Server.Guilds;
using Server.Items;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{
    public partial class PlayerMobile
    {
        private static void OnBeforePlayerDeathEventHandler(BeforePlayerDeathEventArgs e)
        {
            if (e.Account != null)
            {
                Account acct = e.Account;
                PlayerMobile? pm = e.Mobile as PlayerMobile;

                if (acct == null || pm == null)
                    return;

                // Mounted ethereal: force unmount and consume one use.
                if (pm.Mounted && pm.Mount is EtherealMount mountedEthie)
                {
                    mountedEthie.UnmountMe();
                    mountedEthie.Rider = null;

                    if (mountedEthie.Uses > 0)
                        mountedEthie.Uses--;
                }
                else
                {
                    // Backpack ethereals: consume one use from each carried ethereal mount.
                    List<Item> backpackContents = Utility.GetContainerItems(pm.Backpack, true);

                    if (backpackContents != null)
                    {
                        foreach (Item item in backpackContents)
                        {
                            if (item is EtherealMount ethie && !ethie.Deleted)
                            {
                                if (ethie.Uses > 0)
                                    ethie.Uses--;
                            }
                        }
                    }
                }
            }
        }

        private static void OnBeforeMortalisPlayerDeathEventHandler(BeforePlayerDeathEventArgs e)
        {
            // common death handling
            OnBeforePlayerDeathEventHandler(e);

            if (e.Account != null)
            {
                Account acct = e.Account;
                PlayerMobile? pm = e.Mobile as PlayerMobile;

                if (acct == null || pm == null)
                    return;

                #region Prep for a new life
                acct.TimeOfDeath = DateTime.UtcNow;
                acct.GuildStone = (pm.Guild is Guild g && g.Guildstone != null)
                    ? g.Guildstone.Serial
                    : Serial.MinusOne;
                acct.LongTermMurders = pm.LongTermMurders;
                acct.ShortTermMurders = pm.ShortTermMurders;
                acct.Karma = pm.Karma;
                // Player died: precompute Fame for reincarnation (10% loss, minimum 1).
                int fameLost = Math.Max(1, pm.Fame / 10);
                acct.Fame = Math.Max(0, pm.Fame - fameLost);
                // Save only non-standard hues; otherwise use 0 sentinel.
                Item? hair = pm.FindItemOnLayer(Layer.Hair);
                Item? beard = pm.FindItemOnLayer(Layer.FacialHair);
                acct.HairHue = (hair is Hair h && !Utility.IsStandardHairHue(h.Hue)) ? (h.Hue & 0x3FFF) : 0;
                acct.BeardHue = (beard is Beard b && !Utility.IsStandardHairHue(b.Hue)) ? (b.Hue & 0x3FFF) : 0;
                acct.BackpackHue = (pm.Backpack != null) ? pm.Backpack.Hue : 0;

                #region Release pets
                if (pm != null)
                {
                    var pets = pm.Pets;
                    if (pets != null)
                        foreach (BaseCreature pet in pets)
                        {
                            if (pet != null)
                                if (pet.OnBeforeRelease(pet.ControlMaster))
                                {
                                    pet.ControlTarget = null;
                                    pet.ControlOrder = OrderType.Release;
                                    if (pet.AIObject != null)
                                        pet.AIObject.Obey();
                                }
                        }
                }
                #endregion Release pets

                StrongBackpack? newBox = null; // carryover container for this soul (pm.GUID)

                #region Save Bank Box
                // The killed player's bankbox is saved and given to the new character created on the account.
                // Under the GUID/OwnerGUID model, we no longer track a bankbox Serial on the Account.
                // Instead, we create (or reuse) a StrongBackpack whose OwnerGUID is the player's GUID
                // and park it on the internal map for retrieval after reincarnation.

                if (pm.BankBox != null && pm.BankBox.Items != null && pm.BankBox.Items.Count > 0)
                {
                    // Create a new inheritance bankbox if we don't already have one in this flow.
                    if (newBox == null)
                    {
                        newBox = new StrongBackpack();
                        newBox.OwnerGUID = pm.AcctGUID;      // bind to this soul’s identity
                        newBox.MoveToIntStorage();       // park it safely off-world
                    }

                    // Recursively move all bankbox contents (except CivicBond) into the inheritance box.
                    Utility.RecursiveMove(newBox, pm.BankBox, new List<Type> { typeof(CivicBond) });
                }
                #endregion Save Bank Box

                #region Preserve identity items (Mortalis)

                // Mortalis: before this character is discarded, preserve only a narrow,
                // explicit set of identity items across reincarnation.
                //
                // Rules:
                //  - Only BaseClothing, RolledUpSheetMusic, and BaseBook are bank-preserved
                //    (if LootType != Regular or explicitly allowed).
                //  - Any other non-regular item is forced to UnStealable so it drops to corpse.
                //  - Includes equipped items and backpack contents (recursive).
                //
                // All preserved items are placed into the same inheritance StrongBackpack
                // (newBox) used for bank carryover above, identified solely by OwnerGUID.

                if (pm != null)
                {
                    // Gather equipped items and backpack contents
                    List<Item> equipped = Utility.GetEquippedItems(pm);
                    List<Item> backpackContents = Utility.GetContainerItems(pm.Backpack, true);

                    // Combine into a single working set
                    List<Item> allItems = new List<Item>();
                    allItems.AddRange(equipped);
                    allItems.AddRange(backpackContents);

                    // De-duplicate (equipped items may also appear in container scans)
                    allItems = allItems
                        .Where(i => i != null && !i.Deleted)
                        .GroupBy(i => i.Serial)
                        .Select(g => g.First())
                        .ToList();

                    List<Item> bankPreserve = new List<Item>();

                    foreach (Item item in allItems)
                    {
                        bool preserve =
                        (
                            (item is EtherealMount em && em.LootType == LootType.Blessed)
                            ||
                            (item is BaseClothing bc && bc.LootType == LootType.Blessed)
                            ||
                            (item is not BaseArmor && item is not BaseWeapon &&
                             (item.LootType == LootType.Rare || item.LootType == LootType.KeepOnDeath))
                            ||
                            item is RolledUpSheetMusic
                            ||
                            item is BaseBook
                        );

                        if (preserve)
                        {
                            // Allowlist: only these identity items follow the soul
                            item.FamilyID = 0; // clear the familyID to prevent it from being cleaned up
                            bankPreserve.Add(item);
                            continue;
                        }

                        // Everything else drops to corpse (normalize non-regular loot)
                        if (item.LootType != LootType.Regular)
                            item.LootType = LootType.UnStealable;
                    }


                    // Only create / use a carryover bankbox if something qualifies
                    if (bankPreserve.Count > 0)
                    {
                        // If we didn’t already create an inheritance bankbox in the “Save Bank Box”
                        // region, create one now and bind it to this soul via OwnerGUID.
                        if (newBox == null)
                        {
                            newBox = new StrongBackpack();
                            newBox.OwnerGUID = pm.AcctGUID;
                            newBox.MoveToIntStorage();
                        }

                        // Move preserved identity items into the inheritance bankbox
                        foreach (Item item in bankPreserve)
                        {
                            if (item.Parent is Container parentCont)
                                parentCont.RemoveItem(item);
                            else if (item.Parent is Mobile parentMob)
                                parentMob.RemoveItem(item);

                            newBox.AddItem(item);
                        }
                    }
                }

                #endregion Preserve identity items

                #region Cleanup starter items

                // If there are no starter families, we're done.
                if (acct.StarterFamilyIds != null && acct.StarterFamilyIds.Count > 0)
                {
                    // Delete all starter items, wherever they are.
                    var toDelete = new List<Item>();
                    foreach (Item item in World.Items.Values)
                    {
                        if (item == null || item.Deleted || item.FamilyID == 0)
                            continue;

                        if (acct.HasStarterFamilyId(item.FamilyID))
                        {
                            if (item is Container)
                            {
                                item.FamilyID = 0;
                                continue;
                            }

                            toDelete.Add(item);
                        }
                    }

                    foreach (Item item in toDelete)
                        if (item.Deleted == false)
                            item.Delete();

                    // Clear the family ids from this account for the next life
                    acct.ClearStarterFamilyIds();
                }

                #endregion Cleanup starter items

                #region Update Guild Association
                if (pm.Guild is Guild guild && guild.Guildstone != null)
                {
                    acct.GuildStone = guild.Guildstone.Serial;
                    if (guild.Members != null && guild.Members.Count == 1 && guild.Leader == pm)
                    {
                        guild.AddMember(World.GetSystemAcct());
                        guild.Leader = World.GetSystemAcct();
                    }
                }
                #endregion Update Guild Association

                #endregion Prep for a new life
                #region Save Previous Stats and Skills
                if (pm != null && acct != null)
                    if (!acct.IsSnapshotDowngrade(pm))
                        acct.Snapshot(pm);
                #endregion Save Previous Stats and Skills
                #region Patch Bounty Board and Preserve Player Legacy Records

                bool usedAsPlacer = (BountyKeeper.FindPlacer(pm) != null);
                bool usedAsWanted = (BountyKeeper.FindWanted(pm) != null);

                bool hasRecordedLegacy = false;

                foreach (RecorderDomain domain in Enum.GetValues(typeof(RecorderDomain)))
                {
                    if (DataRecorder.Get(pm, domain) > 0)
                    {
                        hasRecordedLegacy = true;
                        break;
                    }
                }

                if (usedAsPlacer || usedAsWanted || hasRecordedLegacy)
                {
                    Substitute? proxy = Substitute.Find(pm.AcctGUID);

                    if (proxy == null)
                        proxy = new Substitute(pm.Name, pm.AcctGUID);

                    // Patch Bounty Board semantic references.
                    if (usedAsPlacer)
                        BountyKeeper.ReplaceAllPlacer(pm, proxy);

                    if (usedAsWanted)
                        BountyKeeper.ReplaceAllWanted(pm, proxy);

                    // Preserve all recorded legacy buckets.
                    // Future RecorderDomain values are automatically included.
                    //foreach (RecorderDomain domain in Enum.GetValues(typeof(RecorderDomain)))
                    //{
                    //    double value = DataRecorder.Get(pm, domain);

                    //    if (value > 0)
                    //        DataRecorder.Add(proxy, domain, value);
                    //}

                    DataRecorder.Replace(pm, proxy);
                }

                #endregion Patch Bounty Board and Preserve Player Legacy Records

            }
        }
    }
}
