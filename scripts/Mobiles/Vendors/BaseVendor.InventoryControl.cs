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
 *
 *  BaseVendor.InventoryControl.cs
 *
 *  Purpose:
 *      Implements Season 2 vendor inventory filtering and policy control.
 *
 *  Overview:
 *      This module allows vendor inventory to be dynamically filtered using
 *      declarative inventory rules loaded from:
 *
 *          Data/Seasons/VendorInventory.json
 *
 *      Vendor inventories are first constructed normally through the
 *      standard SBInfo system and are then filtered against the configured
 *      rule set.
 *
 *      This allows Season 2 to tightly control which goods remain available
 *      in the economy without requiring invasive changes to existing vendor
 *      definitions.
 *
 *  Rule System:
 *
 *      Inventory rules support both:
 *
 *          - Type-only matching
 *          - Property-based matching
 *
 *      Examples:
 *
 *          "CommodityDeed"
 *          "BeverageBottle Content==Liquor"
 *          "Pitcher Content==Water"
 *
 *      Rules are parsed into VendorInventoryRule objects and evaluated
 *      against GenericBuyInfo entries during vendor inventory construction.
 *
 *  Architectural Notes:
 *
 *      Vendor inventories are built normally first:
 *
 *          InitSBInfo()
 *              -> Build vendor inventory
 *
 *      Then Season 2 filtering is applied:
 *
 *          IsSeason2AllowedBuyInfo(...)
 *
 *      This separation keeps the filtering system declarative and avoids
 *      coupling vendor policy directly into individual vendor definitions.
 *
 *  Runtime Reloading:
 *
 *      Administrators may reload the vendor inventory configuration at
 *      runtime using:
 *
 *          [ReloadVendorInventories
 *
 *      This rebuilds vendor inventories without requiring a world restart.
 *
 *  Design Goals:
 *
 *      - Declarative inventory policy
 *      - Minimal vendor-specific logic
 *      - Runtime configurability
 *      - Human-readable rules
 *      - Extensible matching system
 *      - Reduced hardcoded economy behavior
 *
 *  Maintenance Notes:
 *
 *      Property expressions currently support:
 *
 *          ==
 *
 *      Additional operators should only be added carefully to avoid turning
 *      the inventory rule system into a full scripting language.
 *
 *      Matching currently operates against GenericBuyInfo instances rather
 *      than instantiated items.
 *
 ***************************************************************************/

/* Scripts\Mobiles\Vendors\BaseVendor.InventoryControl.cs
 * CHANGELOG
 *  5/19/2026, Adam
 *      Added declarative Season 2 vendor inventory filtering system.
 *
 *      - Added JSON-driven vendor inventory rules.
 *      - Added VendorInventoryRule parser and matcher system.
 *      - Added property-based inventory filtering support.
 *      - Added reflection-based property evaluation.
 *      - Added enum-aware rule value conversion.
 *      - Added runtime inventory reload command.
 *      - Added support for selective inventory variants such as:
 *          "BeverageBottle Content==Liquor"
 *
 *      Vendor inventories are now filtered using declarative policy rules
 *      rather than simple allowed-type lists.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Server.Mobiles
{
    public partial class BaseVendor
    {
        private static readonly List<VendorInventoryRule> m_AllowedVendorInventoryRules = new();

        private sealed class VendorInventoryRule
        {
            public Type ItemType { get; private set; }
            public string PropertyName { get; private set; }
            public string? Operator { get; private set; }
            public string Value { get; private set; }
            public string RawText { get; private set; }

            public bool HasExpression => PropertyName != null;

            private VendorInventoryRule()
            {
            }

            public static bool TryParse(string text, out VendorInventoryRule rule)
            {
                rule = null;

                if (string.IsNullOrWhiteSpace(text))
                    return false;

                VendorInventoryRule candidate = new VendorInventoryRule();

                if (!candidate.Parse(text))
                    return false;

                rule = candidate;
                return true;
            }

            private bool Parse(string text)
            {
                RawText = text.Trim();

                string typeText;
                string? expressionText = null;

                int space = RawText.IndexOf(' ');

                if (space < 0)
                {
                    typeText = RawText;
                }
                else
                {
                    typeText = RawText.Substring(0, space).Trim();
                    expressionText = RawText.Substring(space + 1).Trim();

                    if (string.IsNullOrWhiteSpace(expressionText))
                        expressionText = null;
                }

                ItemType = ScriptCompiler.FindTypeByFullName(typeText, false)
                           ?? ScriptCompiler.FindTypeByName(typeText, false);

                if (ItemType == null)
                    return false;

                if (expressionText == null)
                    return true;

                return ParseExpression(expressionText);
            }

            private bool ParseExpression(string expressionText)
            {
                string[] operators = new string[]
                {
                    "=="
                };

                foreach (string op in operators)
                {
                    int index = expressionText.IndexOf(op, StringComparison.Ordinal);

                    if (index < 0)
                        continue;

                    string propertyName = expressionText.Substring(0, index).Trim();
                    string value = expressionText.Substring(index + op.Length).Trim();

                    if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(value))
                        return false;

                    PropertyName = propertyName;
                    Operator = op;
                    Value = value;

                    return true;
                }

                return false;
            }

            public bool Matches(GenericBuyInfo buyInfo)
            {
                if (buyInfo == null || buyInfo.Type == null)
                    return false;

                if (!ItemType.IsAssignableFrom(buyInfo.Type))
                    return false;

                if (!HasExpression)
                    return true;

                return MatchesExpression(buyInfo);
            }

            private bool MatchesExpression(GenericBuyInfo buyInfo)
            {
                if (Operator != "==")
                    return false;

                if (!TryGetMemberValue(buyInfo, PropertyName, out object actualValue, out Type valueType))
                    return false;

                if (!TryConvertValue(Value, valueType, out object expectedValue))
                    return false;

                if (actualValue == null)
                    return expectedValue == null;

                if (actualValue is string actualString && expectedValue is string expectedString)
                    return string.Equals(actualString, expectedString, StringComparison.OrdinalIgnoreCase);

                return actualValue.Equals(expectedValue);
            }

            private static bool TryGetMemberValue(object source, string memberName, out object value, out Type valueType)
            {
                value = null;
                valueType = null;

                if (source == null || string.IsNullOrWhiteSpace(memberName))
                    return false;

                Type sourceType = source.GetType();

                System.Reflection.PropertyInfo property = sourceType.GetProperty(
                    memberName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.IgnoreCase);

                if (property != null && property.CanRead)
                {
                    value = property.GetValue(source, null);
                    valueType = property.PropertyType;
                    return true;
                }

                System.Reflection.FieldInfo field = sourceType.GetField(
                    memberName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.IgnoreCase);

                if (field == null)
                {
                    field = sourceType.GetField(
                        "m_" + memberName,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.IgnoreCase);
                }

                if (field != null)
                {
                    value = field.GetValue(source);
                    valueType = field.FieldType;
                    return true;
                }

                return false;
            }

            private static bool TryConvertValue(string rawValue, Type targetType, out object value)
            {
                value = null;

                if (targetType == null)
                    return false;

                if (targetType == typeof(string))
                {
                    value = rawValue;
                    return true;
                }

                if (targetType.IsEnum)
                {
                    try
                    {
                        value = Enum.Parse(targetType, rawValue, true);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                try
                {
                    value = Convert.ChangeType(rawValue, targetType);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public override bool Equals(object? obj)
            {
                if (obj is not VendorInventoryRule other)
                    return false;

                return ItemType == other.ItemType
                       && string.Equals(PropertyName, other.PropertyName, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(Operator, other.Operator, StringComparison.Ordinal)
                       && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
            }

            public override int GetHashCode()
            {
                int hash = ItemType != null ? ItemType.GetHashCode() : 0;

                if (PropertyName != null)
                    hash ^= StringComparer.OrdinalIgnoreCase.GetHashCode(PropertyName);

                if (Operator != null)
                    hash ^= Operator.GetHashCode();

                if (Value != null)
                    hash ^= StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

                return hash;
            }

            public override string ToString()
            {
                return RawText;
            }
        }

        public new static void Initialize()
        {
            if ((Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules()))
            {
                Server.CommandSystem.Register("ReloadVendorInventories", AccessLevel.Administrator, new CommandEventHandler(ReloadVendorInventories_OnCommand));
            }
        }

        private static DateTime m_LastVendorInventoryWrite;

        private static void LoadVendorInventory(bool force = false)
        {
            string path = Path.Combine(Core.ExeDirectory, "Data/Seasons/VendorInventory.json");

            if (!force && File.Exists(path))
            {
                DateTime lastWrite = File.GetLastWriteTimeUtc(path);

                // No file change and we already have a loaded rule set.
                if (lastWrite == m_LastVendorInventoryWrite && m_AllowedVendorInventoryRules.Count > 0)
                    return;
            }

            m_AllowedVendorInventoryRules.Clear();

            AddDefaultAllowedVendorInventoryRules();

            if (!File.Exists(path))
            {
                Console.WriteLine($"Warning: VendorInventory.json not found at {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                List<string>? rules = JsonSerializer.Deserialize<List<string>>(json);

                if (rules != null)
                {
                    foreach (string ruleText in rules)
                    {
                        if (string.IsNullOrWhiteSpace(ruleText))
                            continue;

                        if (VendorInventoryRule.TryParse(ruleText, out VendorInventoryRule rule))
                        {
                            if (!m_AllowedVendorInventoryRules.Contains(rule))
                                m_AllowedVendorInventoryRules.Add(rule);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Could not parse vendor inventory rule '{ruleText}' in VendorInventory.json");
                        }
                    }
                }

                m_LastVendorInventoryWrite = File.GetLastWriteTimeUtc(path);

                Console.WriteLine($"Loaded {m_AllowedVendorInventoryRules.Count} allowed vendor inventory rules from JSON.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading VendorInventory.json:");
                Console.WriteLine(ex);

                m_AllowedVendorInventoryRules.Clear();
                AddDefaultAllowedVendorInventoryRules();
            }
        }

        private static void AddDefaultAllowedVendorInventoryRules()
        {
            AddDefaultRule("CommodityDeed");
            AddDefaultRule("HouseDeed");
            AddDefaultRule("ShipPlans");
            AddDefaultRule("ContractOfEmployment");
            AddDefaultRule("VendorRentalContract");
            AddDefaultRule("Beeswax");
        }

        private static void AddDefaultRule(string ruleText)
        {
            if (!VendorInventoryRule.TryParse(ruleText, out VendorInventoryRule rule))
            {
                Console.WriteLine($"Warning: Could not parse default vendor inventory rule '{ruleText}'");
                return;
            }

            if (!m_AllowedVendorInventoryRules.Contains(rule))
                m_AllowedVendorInventoryRules.Add(rule);
        }

        public static void ReloadVendorInventories_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;

            m.SendMessage("Reloading Vendor Inventory configuration...");

            foreach (BaseVendor vendor in World.Mobiles.Values.OfType<BaseVendor>())
            {
                vendor.ClearSBInfo();
            }

            // force a reload
            LoadVendorInventory(force: true);

            int updated = 0;

            foreach (BaseVendor vendor in World.Mobiles.Values.OfType<BaseVendor>())
            {
                vendor.LoadSBInfo();
                updated++;
            }

            m.SendMessage($"Reload complete. Updated {updated} vendors.");
        }

        private static bool IsSeason2AllowedBuyInfo(GenericBuyInfo buyInfo)
        {
            if (buyInfo == null)
                return false;

            foreach (VendorInventoryRule rule in m_AllowedVendorInventoryRules)
            {
                if (rule.Matches(buyInfo))
                    return true;
            }

            return false;
        }

        public static List<string> AllowedVendorInventoryRules
        {
            get
            {
                List<string> list = new();

                foreach (VendorInventoryRule rule in m_AllowedVendorInventoryRules)
                    list.Add(rule.ToString());

                return list;
            }
        }
    }
}