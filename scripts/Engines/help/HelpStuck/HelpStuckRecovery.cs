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

/* Scripts/Engines/Help/HelpStuck/HelpStuckRecovery.cs
 * Changelog:
 *  10/13/2026, Adam (Help Stuck refactor)
 *      Moved here unchanged from StuckMenu.cs: the dungeon evacuation consequences (the recovery chest).
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Menus.Questions
{
    public static class HelpStuckRecovery
    {
        #region Mortalis Dungeon Rescue Consequences

        /*
         * Mortalis: Dungeon evacuation consequences
         * ----------------------------------------
         * Classic shards enforce "no living dungeon evac" by killing the player prior to relocation.
         * Mortalis replaces that with "evac alive, but nearly empty-handed" to avoid permadeath caused by
         * geometry faults while still preventing help-stuck from becoming a loot-extraction tool.
         *
         * Policy (Mortalis, dungeon-rules only):
         *  - Drop any cursor-held item first (prevents cursor-hold bypass).
         *  - Create an immovable, locked recovery chest near the stuck origin (spawnable, in LOS).
         *  - Rekey the chest; place the key onto the player at teleport time (key is Blessed).
         *  - Move all non-kept items into the chest (recursive), then evacuate the player.
         *  - Chest decays after 1 hour.
         *
         * Kept items (minimal survival + identity):
         *  - Blessed / Newbied items
         *  - Equipped clothing
         *  - Equipped weapon + equipped shield
         *  - Spellbook
         *  - Bandages
         *  - Potions: Heal, Cure, Refresh (all strengths)
         */
        public static void ApplyDungeonEvacuationConsequences(Mobile m, Point3D origin, Map originMap)
        {
            if (m == null || m.Deleted)
                return;

            if (originMap == null || originMap == Map.Internal)
                return;

            // Prevent cursor-hold bypass: drops to pack if it fits, to ground otherwise.
            Utility.DropHolding(m);

            if (m is not PlayerMobile pm)
                return;

            // Spawn a recovery chest near the stuck origin (not necessarily the exact tile).
            Point3D chestLoc = GetSpawnLocation(m, origin, range: 6);
            if (chestLoc == Point3D.Zero)
                chestLoc = origin; // last resort

            RescueChest chest = new RescueChest(TimeSpan.FromMinutes(60));
            chest.MoveToWorld(chestLoc, originMap);

            // Rekey the chest and put the key on the player now (before we strip backpack contents).
            uint keyValue = Key.RandomValue();
            chest.KeyValue = keyValue;
            chest.Locked = true;

            Key key = new Key(KeyType.Copper, keyValue)
            {
                LootType = LootType.Blessed,
                Name = "Recovery Chest Key"
            };

            if (!pm.PlaceInBackpack(key))
                key.MoveToWorld(pm.Location, pm.Map); // worst-case; should be rare

            HashSet<Item> keep = BuildRescueKeepSet(pm);

            // Move non-kept items out of the backpack (recursive).
            if (pm.Backpack != null)
                MoveNonKeptItemsToChest(pm.Backpack, chest, keep);

            // Optionally strip non-kept equipped items (jewelry, talismans, etc.).
            StripNonKeptEquipped(pm, chest, keep);
        }

        // recovery placement, not general spawning: LOS from the player's original location is deliberate (keeps the chest where the belongings were lost, not behind a wall)
        private static Point3D GetSpawnLocation(Mobile m, Point3D center, int range)
        {
            Map? map = m?.Map;

            if (map == null || map == Map.Internal)
                return Point3D.Zero;

            for (int i = 0; i < 20; i++)
            {
                int x = center.X + Utility.RandomMinMax(-range, range);
                int y = center.Y + Utility.RandomMinMax(-range, range);

                // First try: center Z
                int z = center.Z;
                Point3D p = new Point3D(x, y, z);

                if (map.CanFit(x, y, z, 16, false, false, true) && m.InLOS(p))
                    return p;

                // Second try: average Z
                z = map.GetAverageZ(x, y);
                p = new Point3D(x, y, z);

                if (map.CanFit(x, y, z, 16, false, false, true) && m.InLOS(p))
                    return p;
            }

            return Point3D.Zero;
        }

        private static HashSet<Item> BuildRescueKeepSet(PlayerMobile pm)
        {
            HashSet<Item> keep = new HashSet<Item>();

            // Keep: Blessed/Newbied (identity items, special keys, etc.)
            if (pm.Backpack != null)
                AddBlessedAndNewbied(pm.Backpack, keep);

            foreach (Item item in pm.Items)
            {
                if (item == null || item.Deleted)
                    continue;

                if (IsBlessedOrNewbied(item))
                    keep.Add(item);

                // Equipped clothing always stays
                if (item is BaseClothing)
                    keep.Add(item);

                // Equipped weapon/shield stays
                if (item is BaseWeapon || item is BaseShield)
                    keep.Add(item);

                // Equipped spellbook stays (if present)
                if (item is Spellbook)
                    keep.Add(item);
            }

            // Backpack essentials: bandages, spellbook, and specific potion effects.
            if (pm.Backpack != null)
                AddBackpackEssentials(pm.Backpack, keep);

            return keep;
        }

        private static void AddBlessedAndNewbied(Container cont, HashSet<Item> keep)
        {
            if (cont == null)
                return;

            foreach (Item item in new List<Item>(cont.Items))
            {
                if (item == null || item.Deleted)
                    continue;

                if (IsBlessedOrNewbied(item))
                    keep.Add(item);

                if (item is Container child)
                    AddBlessedAndNewbied(child, keep);
            }
        }

        private static void AddBackpackEssentials(Container cont, HashSet<Item> keep)
        {
            if (cont == null)
                return;

            foreach (Item item in new List<Item>(cont.Items))
            {
                if (item == null || item.Deleted)
                    continue;

                if (item is Bandage)
                    keep.Add(item);
                else if (item is Spellbook)
                    keep.Add(item);
                else if (item is BasePotion potion && IsRescuePotion(potion))
                    keep.Add(item);

                if (item is Container child)
                    AddBackpackEssentials(child, keep);
            }
        }

        private static bool IsRescuePotion(BasePotion potion)
        {
            // Keep only survival potions. (All strengths qualify.)
            return potion.PotionEffect == PotionEffect.Cure
                || potion.PotionEffect == PotionEffect.Heal
                || potion.PotionEffect == PotionEffect.Refresh;
        }

        private static bool IsBlessedOrNewbied(Item item)
        {
            return item.LootType == LootType.Blessed || item.LootType == LootType.Newbied;
        }

        private static void MoveNonKeptItemsToChest(Container from, Container chest, HashSet<Item> keep)
        {
            if (from == null || chest == null)
                return;

            // Iterate a copy to avoid collection mutation issues.
            List<Item> items = new List<Item>(from.Items);

            foreach (Item item in items)
            {
                if (item == null || item.Deleted)
                    continue;

                if (keep.Contains(item))
                    continue;

                if (item is Container child)
                {
                    // Recurse first so we don't accidentally carry kept items inside a moved container.
                    MoveNonKeptItemsToChest(child, chest, keep);

                    // Move the container itself only if it is now empty and not kept.
                    if (!keep.Contains(child) && child.Items.Count == 0)
                        SafeMoveItem(child, chest);

                    continue;
                }

                SafeMoveItem(item, chest);
            }
        }

        private static void StripNonKeptEquipped(Mobile m, Container chest, HashSet<Item> keep)
        {
            if (m == null || chest == null)
                return;

            // Equipped items only (m.Items). Do not touch the backpack itself.
            List<Item> equipped = new List<Item>(m.Items);

            foreach (Item item in equipped)
            {
                if (item == null || item.Deleted)
                    continue;

                if (item.Layer == Layer.Backpack)
                    continue;

                if (keep.Contains(item))
                    continue;

                // Anything not explicitly kept (e.g., jewelry) is deposited.
                SafeMoveItem(item, chest);
            }
        }

        private static void SafeMoveItem(Item item, Container dest)
        {
            if (item == null || item.Deleted || dest == null)
                return;

            // Do not attempt to move secured / immovable items.
            if (!item.Movable)
                return;

            if (item.Parent is Container pc)
                pc.RemoveItem(item);
            else if (item.Parent is Mobile pm)
                pm.RemoveItem(item);

            dest.DropItem(item);
        }




        #endregion
    }
}
