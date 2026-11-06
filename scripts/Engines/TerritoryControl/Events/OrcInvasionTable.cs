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

/* Scripts\Engines\TerritoryControl\Events\OrcInvasionTable.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See TerritorySystem for an architectural overview
 */


using Server.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Server.Engines.TerritoryControl.Events
{
    public static class OrcInvasionTable
    {
        private static readonly string FilePath = Path.Combine("Data", "Seasons", "OrcInvasionTable.json");

        public class OrcEntry
        {
            public string Type { get; set; }
            public double Power { get; set; }
            public string Tier { get; set; }
        }

        public class OrcTableData
        {
            public string Faction { get; set; }
            public string Description { get; set; }
            public List<OrcEntry> Creatures { get; set; } = new();
        }

        private static OrcTableData? _data;
        private static bool _isLoaded = false;

        public static bool IsLoaded => _isLoaded;

        public static int CreatureCount => _data?.Creatures?.Count ?? 0;

        public static void Load()
        {
            _isLoaded = false;

            if (!File.Exists(FilePath))
            {
                Console.WriteLine($"[OrcInvasionTable] WARNING: File not found: {FilePath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                _data = JsonSerializer.Deserialize<OrcTableData>(json);

                _isLoaded = true;

                Console.WriteLine($"[OrcInvasionTable] Loaded {CreatureCount} Orc types.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[OrcInvasionTable] Error loading OrcInvasionTable.json");
                LogHelper.LogException(ex);
            }
        }

        public static List<OrcEntry> Creatures => _data?.Creatures ?? new List<OrcEntry>();

    }
}