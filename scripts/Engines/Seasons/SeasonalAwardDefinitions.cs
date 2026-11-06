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

/* Scripts\Engines\Seasons\SeasonalAwardDefinitions.cs
 * ChangeLog
 *  3/27/2026, Adam
 *      First time checkin.
 */

using Server.Diagnostics;
using Server.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Server.Engines.Seasons
{
    public static class SeasonalAwardDefinitions
    {
        public static void Initialize()
        {
            if (Core.RuleSets.SeasonsRules())
            {
                Server.CommandSystem.Register("AddSeasonalAward", AccessLevel.Administrator, new CommandEventHandler(AddSeasonalAward_OnCommand));
            }

        }
        private const string AwardsXmlPath = "Data/Seasons/Awards.xml";

        /// <summary>
        /// Reads all awards from XML on every call (no caching).
        /// Returns them sorted by price (low to high).
        /// Supports optional 'args' attribute (comma-separated integers).
        /// </summary>
        public static List<SeasonalAwardDefinition> GetAll()
        {
            var awards = new List<SeasonalAwardDefinition>();

            if (!File.Exists(AwardsXmlPath))
            {
                Console.WriteLine($"[SeasonalAwards] Warning: {AwardsXmlPath} not found.");
                return awards;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(AwardsXmlPath);

                XmlNodeList? nodes = doc.SelectNodes("//Award");

                foreach (XmlNode node in nodes)
                {
                    if (node is not XmlElement elem)
                        continue;

                    string typeName = elem.GetAttribute("type");
                    if (string.IsNullOrWhiteSpace(typeName))
                        continue;

                    Type? itemType = ScriptCompiler.FindTypeByName(typeName);
                    if (itemType == null || !typeof(Item).IsAssignableFrom(itemType))
                        continue;

                    int cost = Utility.ToInt32(elem.GetAttribute("price"), 500);
                    int? hue = Utility.ToInt32Nullable(elem.GetAttribute("hue"));

                    // Smart name handling (cliloc or literal)
                    string? name = elem.GetAttribute("name");
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        if (int.TryParse(name, out int clilocNumber))
                        {
                            if (Cliloc.Lookup.TryGetValue(clilocNumber, out string? clilocName)
                                && !string.IsNullOrWhiteSpace(clilocName))
                            {
                                name = clilocName;
                            }
                        }
                    }
                    else
                    {
                        name = null;
                    }

                    // Parse args - now expecting fully qualified enum names like "Server.Items.MonsterStatuetteType.Wolf"
                    string[]? args = null;
                    string argsStr = elem.GetAttribute("args");
                    if (!string.IsNullOrWhiteSpace(argsStr))
                    {
                        args = argsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(x => x.Trim())
                                      .ToArray();
                    }

                    int? itemID = Utility.ToInt32Nullable(elem.GetAttribute("itemID"));
                    string? itemName = elem.GetAttribute("itemName");
                    if (string.IsNullOrWhiteSpace(itemName))
                        itemName = null;

                    awards.Add(new SeasonalAwardDefinition(
                        itemType,
                        cost,
                        itemIDOverride: itemID,
                        hueOverride: hue,
                        nameOverride: name,
                        itemNameOverride: itemName,
                        args: args
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeasonalAwards] Error reading {AwardsXmlPath}: {ex.Message}");
                LogHelper.LogException(ex);
            }

            return awards.OrderBy(a => a.Cost).ToList();
        }

        [Usage("AddSeasonalAward <price> [hue] [name]")]
        [Description("Targets an item and adds it as a seasonal award. Automatically places it in the correct tier based on price.")]
        public static void AddSeasonalAward_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length == 0 || !int.TryParse(e.Arguments[0], out int price) || price < 100)
            {
                e.Mobile.SendMessage("Usage: AddSeasonalAward <price> [hue] [name]");
                e.Mobile.SendMessage("Hue and name are optional - if omitted, uses the targeted item's values.");
                return;
            }

            int? hue = null;
            string? name = null;

            if (e.Arguments.Length >= 2 && int.TryParse(e.Arguments[1], out int parsedHue))
                hue = parsedHue;

            if (e.Arguments.Length >= 3)
                name = string.Join(" ", e.Arguments, 2, e.Arguments.Length - 2);

            e.Mobile.SendMessage("Target the item you want to add as a seasonal award...");
            e.Mobile.BeginTarget(-1, false, Server.Targeting.TargetFlags.None,
                (from, target) => OnTarget(from, target, price, hue, name));
        }
        private static void OnTarget(Mobile from, object target, int price, int? commandHue, string? commandName)
        {
            if (target is not Item item)
            {
                from.SendMessage("That is not an item.");
                return;
            }

            int finalHue = commandHue ?? item.Hue;
            string finalName = !string.IsNullOrWhiteSpace(commandName)
                ? commandName
                : (item.Name ?? item.GetType().Name);

            string xmlPath = "Data/Seasons/Awards.xml";

            try
            {
                XmlDocument doc = new XmlDocument();
                if (File.Exists(xmlPath))
                    doc.Load(xmlPath);
                else
                    doc.LoadXml("<Awards></Awards>");

                XmlElement newAward = doc.CreateElement("Award");
                newAward.SetAttribute("type", item.GetType().Name);
                newAward.SetAttribute("price", price.ToString());
                newAward.SetAttribute("hue", "0x" + finalHue.ToString("X"));   // Always save as hex
                newAward.SetAttribute("name", finalName);

                bool inserted = InsertIntoTier(doc, newAward, price);

                if (!inserted)
                    doc.DocumentElement.AppendChild(newAward);

                doc.Save(xmlPath);

                from.SendMessage($"Added '{finalName}' for {price} points (hue 0x{finalHue:X}).");
            }
            catch (Exception ex)
            {
                from.SendMessage("Failed to save award to XML.");
                LogHelper.LogException(ex);
            }
        }

        /// <summary>
        /// Intelligently inserts the new award into the correct tier section based on price and tier comments.
        /// </summary>
        private static bool InsertIntoTier(XmlDocument doc, XmlElement newAward, int price)
        {
            XmlNodeList? comments = doc.SelectNodes("//comment()");

            foreach (XmlComment comment in comments)
            {
                string text = comment.Value.ToLowerInvariant();

                // More precise tier detection
                if (price <= 999 && (text.Contains("tier 1") || text.Contains("entry level") || text.Contains("500")))
                {
                    InsertAfterComment(comment, newAward);
                    return true;
                }
                else if (price <= 1999 && (text.Contains("tier 2") || text.Contains("mid tier") || text.Contains("1000")))
                {
                    InsertAfterComment(comment, newAward);
                    return true;
                }
                else if (price <= 3499 && (text.Contains("tier 3") || text.Contains("high tier") || text.Contains("2000")))
                {
                    InsertAfterComment(comment, newAward);
                    return true;
                }
                else if (price <= 5000 && (text.Contains("tier 4") || text.Contains("premium") || text.Contains("3500") || text.Contains("5000")))
                {
                    InsertAfterComment(comment, newAward);
                    return true;
                }
            }

            return false;
        }

        private static void InsertAfterComment(XmlComment comment, XmlElement newAward)
        {
            if (comment.NextSibling != null)
                comment.ParentNode.InsertAfter(newAward, comment.NextSibling);
            else
                comment.ParentNode.AppendChild(newAward);
        }
    }
}