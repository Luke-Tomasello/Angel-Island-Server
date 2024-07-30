/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
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

/* Scripts/Misc/MapDefinitions.cs
 * ChangeLog
 *  10/4/22, Adam
 *      Turned off TileMatrixPatch since our map version doesn't require it (see notes), and in fact 
 *          turning it on cause 'holes' in the map (Jhelom small island, and Minax fortress.)
 *  2/10/2022, Adam
 *      Changed Malas to a Felucca ruleset.
 *  12/23/04, Jade
 *      Changed Ilshenar to a Felucca ruleset.
 *  5/26/04, Old Salty
 * 		Changed Felucca season to 1 (Old Style UO landscape).  
 *	5/21/04, mith
 *		Changed season to 5 (Blooming/Spring). Added comment detailing which numbers = which seasons.
 */

namespace Server.Misc
{
    public class MapDefinitions
    {
        public static void Configure()
        {
            /* Here we configure all maps. Some notes:
			 * 
			 * 1) The first 32 maps are reserved for core use.
			 * 2) Map 0x7F is reserved for core use.
			 * 3) Map 0xFF is reserved for core use.
			 * 4) Changing or removing any predefined maps may cause server instability.
			 */

            RegisterMap(0, 0, 0, 6144, 4096, 1, "Felucca", MapRules.FeluccaRules);
            RegisterMap(1, 1, 0, 6144, 4096, 0, "Trammel", MapRules.FeluccaRules); // 12/9/23, Yoar: Change to Felucca rules for the purpose of the xmas event
            RegisterMap(2, 2, 2, 2304, 1600, 1, "Ilshenar", MapRules.FeluccaRules);
            RegisterMap(3, 3, 3, 2560, 2048, 1, "Malas", MapRules.FeluccaRules);
            RegisterMap(4, 4, 4, 1448, 1448, 1, "Tokuno", MapRules.TrammelRules);
            RegisterMap(5, 5, 5, 1280, 4096, 1, "TerMur", MapRules.TrammelRules);

            RegisterMap(0x7F, 0x7F, 0x7F, Map.SectorSize, Map.SectorSize, 1, "Internal", MapRules.Internal);

            /* Example of registering a custom map:
			 * RegisterMap( 32, 0, 0, 6144, 4096, 3, "Iceland", MapRules.FeluccaRules );
			 * 
			 * Defined:
			 * RegisterMap( <index>, <mapID>, <fileIndex>, <width>, <height>, <season>, <name>, <rules> );
			 *  - <index> : An unreserved unique index for this map
			 *  - <mapID> : An identification number used in client communications. For any visible maps, this value must be from 0-3
			 *  - <fileIndex> : A file identification number. For any visible maps, this value must be 0, 2, or 3
			 *  - <width>, <height> : Size of the map (in tiles)
			 *  - <season> : 0 = spring, 1 = summer, 2 = autumn, 3 = winter, 4 = desolation, 5 = blooming (pre-UO:R Feluca)
			 *  - <name> : Reference name for the map, used in props gump, get/set commands, region loading, etc
			 *  - <rules> : Rules and restrictions associated with the map. See documentation for details
			*/

            TileMatrixPatch.Enabled = false; // OSI Client Patch 6.0.0.0

            MultiComponentList.PostHSFormat = false; // OSI Client Patch 7.0.9.0
        }

        public static void RegisterMap(int mapIndex, int mapID, int fileIndex, int width, int height, int season, string name, MapRules rules)
        {
            Map newMap = new Map(mapID, mapIndex, fileIndex, width, height, season, name, rules);

            Map.Maps[mapIndex] = newMap;
            Map.AllMaps.Add(newMap);
        }
    }
}