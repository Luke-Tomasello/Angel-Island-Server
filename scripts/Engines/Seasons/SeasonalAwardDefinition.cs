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

/* Scripts\Engines\Seasons\SeasonalAwardDefinition.cs
 * ChangeLog
 *  3/27/2026, Adam
 *      First time checkin.
 */

using Server.Diagnostics;
using System;
using System.Diagnostics;

namespace Server.Engines.Seasons
{
    public class SeasonalAwardDefinition
    {
        public Type ItemType { get; private set; }
        public int Cost { get; private set; }

        public int? ItemIDOverride { get; private set; }
        public int? HueOverride { get; private set; }
        public string? NameOverride { get; private set; }      // Display name in list/gump
        public string? ItemNameOverride { get; private set; }  // Actual name when given to player
        public string[] Args { get; private set; }

        public SeasonalAwardDefinition(
            Type itemType,
            int cost,
            int? itemIDOverride = null,
            int? hueOverride = null,
            string? nameOverride = null,
            string? itemNameOverride = null,
            string[]? args = null)
        {
            ItemType = itemType;
            Cost = cost;
            ItemIDOverride = itemIDOverride;
            HueOverride = hueOverride;
            NameOverride = nameOverride;
            ItemNameOverride = itemNameOverride;
            Args = args ?? Array.Empty<string>();
        }

        /// <summary>
        /// Creates the actual award item to give to the player.
        /// </summary>
        public Item? Construct()
        {
            try
            {
                Item item = (Args != null && Args.Length > 0)
                    ? Activator.CreateInstance(ItemType, ResolveArgs()) as Item
                    : Activator.CreateInstance(ItemType) as Item;

                if (item == null)
                {
                    if (Core.Debug)
                        Debug.Assert(false, $"Failed to construct award: {ItemType.Name}");
                    return null;
                }

                if (HueOverride.HasValue)
                    item.Hue = HueOverride.Value;

                if (ItemIDOverride.HasValue)
                    item.ItemID = ItemIDOverride.Value;

                // Apply custom item name if specified
                if (!string.IsNullOrEmpty(ItemNameOverride))
                    item.Name = ItemNameOverride;

                item.LootType = LootType.Blessed;

                if (item.Weight == 0)
                    item.Weight = 1;

                return item;
            }
            catch (Exception ex)
            {
                if (Core.Debug)
                    Debug.Assert(false, $"Exception constructing {ItemType.Name}: {ex.Message}");
                LogHelper.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Creates a temporary item used only for gump display (name, hue, icon).
        /// </summary>
        private Item? CreateTempItem()
        {
            try
            {
                object[] resolvedArgs = ResolveArgs();
                if (ItemType.Name == "Spellbook")
                    ;
                Item item = resolvedArgs.Length > 0
                    ? Activator.CreateInstance(ItemType, resolvedArgs) as Item
                    : Activator.CreateInstance(ItemType) as Item;

                if (item == null && Core.Debug)
                {
                    Debug.Assert(false, $"CreateTempItem failed for type: {ItemType.Name}");
                }

                return item;
            }
            catch (Exception ex)
            {
                if (Core.Debug)
                    Debug.Assert(false, $"Exception in CreateTempItem for {ItemType.Name}: {ex.Message}");

                LogHelper.LogException(ex);

                // Light fallback
                try
                {
                    return Activator.CreateInstance(ItemType) as Item;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Resolves string args (especially fully qualified enum names) into real objects.
        /// </summary>
        private object[] ResolveArgs()
        {
            if (Args.Length == 0)
                return Array.Empty<object>();

            object[] resolved = new object[Args.Length];

            for (int i = 0; i < Args.Length; i++)
            {
                string arg = Args[i].Trim();
                if (string.IsNullOrEmpty(arg))
                    continue;

                // === Case 1: Fully qualified enum value (e.g. "Server.Items.MonsterStatuetteType.Wolf") ===
                if (arg.Contains('.'))
                {
                    int lastDot = arg.LastIndexOf('.');
                    string enumTypeFullName = arg.Substring(0, lastDot);
                    string enumValueName = arg.Substring(lastDot + 1);

                    Type? enumType = ScriptCompiler.FindTypeByName(enumTypeFullName);

                    // Fallback to short type name if full name fails
                    if (enumType == null)
                    {
                        string shortTypeName = enumTypeFullName.Substring(enumTypeFullName.LastIndexOf('.') + 1);
                        enumType = ScriptCompiler.FindTypeByName(shortTypeName);
                    }

                    if (enumType?.IsEnum == true)
                    {
                        if (Enum.TryParse(enumType, enumValueName, true, out object? enumValue))
                        {
                            resolved[i] = enumValue;
                            continue;
                        }
                    }
                }

                // === Case 2: Numeric value (int, uint, long, ulong, with or without 0x prefix) ===
                bool isHex = arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
                string numberPart = isHex ? arg.Substring(2) : arg;

                // Try ulong first (largest range, handles 0xFFFFFFFFFFFFFFFF)
                if (ulong.TryParse(numberPart, isHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out ulong ulongValue))
                {
                    resolved[i] = ulongValue;
                    continue;
                }

                // Try long
                if (long.TryParse(numberPart, isHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out long longValue))
                {
                    resolved[i] = longValue;
                    continue;
                }

                // Try uint
                if (uint.TryParse(numberPart, isHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out uint uintValue))
                {
                    resolved[i] = uintValue;
                    continue;
                }

                // Try int (last resort for small numbers)
                if (int.TryParse(numberPart, isHex ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer, null, out int intValue))
                {
                    resolved[i] = intValue;
                    continue;
                }

                // Final fallback: keep as string
                resolved[i] = arg;
            }

            return resolved;
        }

        // Display helpers (safe)
        public int GetDisplayItemID()
        {
            // Priority 1: Explicit ItemID override from XML
            if (ItemIDOverride.HasValue)
                return ItemIDOverride.Value;

            // Priority 2: Create temporary item to get its default ItemID
            Item? item = CreateTempItem();
            if (item == null)
            {
                if (Core.Debug)
                    Debug.Assert(false, $"GetDisplayItemID failed for type: {ItemType.Name}");
                return 0;                    // safe fallback
            }

            int id = item.ItemID;
            item.Delete();
            return id;
        }

        public int GetDisplayHue()
        {
            if (HueOverride.HasValue)
                return HueOverride.Value;

            Item? item = CreateTempItem();
            if (item == null)
                return 0;

            int hue = item.Hue;
            item.Delete();
            return hue;
        }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(NameOverride))
                return NameOverride;

            Item? item = CreateTempItem();
            if (item == null)
                return ItemType.Name ?? "Unknown";

            string name = item.Name ?? item.GetType().Name;
            item.Delete();
            return name;
        }

    }
}

