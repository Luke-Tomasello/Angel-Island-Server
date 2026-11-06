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

/* Scripts/Engines/ChampionSpawn/LegacyChampTypes.cs
 * ChangeLog:
 *  10/27/26, Adam
 *      Created. FROZEN compatibility table: the ChampLevelData.SpawnTypes ordinals (removed 10/27/26) that saves before
 *      ChampEngine version 18 and TownInvasionAES version 3 contain, mapped to their champion definition names
 *      (Data/ChampSpawns.xml). Used only while deserializing those saves. Never edit, reorder or extend it: new
 *      champions are XML definitions and are saved by name.
 */

using System;

namespace Server.Engines.ChampionSpawn
{
    internal static class LegacyChampTypes
    {
        // index = the old SpawnTypes ordinal; 0 was SpawnTypes.None
        private static readonly string?[] m_Names = new string?[]
        {
            null,                   // 0  None
            "Abyss",                // 1
            "Arachnid",             // 2
            "ColdBlood",            // 3
            "ForestLord",           // 4
            "FrozenHost",           // 5
            "UnholyTerror",         // 6
            "VerminHorde",          // 7
            "Deceit_Mini",          // 8
            "Destard_Mini",         // 9
            "Hythloth_Mini",        // 10
            "Ice_Mini",             // 11
            "Wind_Mini",            // 12
            "Wrong_Mini",           // 13
            "AI_Escape",            // 14
            "AI_Guard",             // 15
            "Test",                 // 16
            "Test2",                // 17
            "Pirate_Full_Sea",      // 18
            "Bob",                  // 19
            "Vampire",              // 20
            "KinCity",              // 21
            "Doppelganger",         // 22
            "GargoyleQueen",        // 23
            "Christmas_Mini",       // 24
            "Pirate_Full_Land",     // 25
            "Pirate_Mini",          // 26
            "Cove_Orcs",            // 27
            "GOT_Full",             // 28
            "GOT_Mini",             // 29
            "Pirate_Micro",         // 30
            "Pirate_Micro_Melee",   // 31
        };

        /// <summary>
        /// The definition name for a serialized SpawnTypes ordinal; null for None (0) or an ordinal that never existed.
        /// </summary>
        public static string? ToName(int ordinal)
        {
            if (ordinal >= 0 && ordinal < m_Names.Length)
                return m_Names[ordinal];

            Utility.Monitor.WriteLine(string.Format("LegacyChampTypes: unknown SpawnTypes ordinal {0}", ordinal), ConsoleColor.Red);
            return null;
        }
    }
}
