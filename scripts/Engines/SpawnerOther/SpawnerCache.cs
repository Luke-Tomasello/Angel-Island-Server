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

/* Scripts/Engines/Spawner/SpawnerCache.cs
 * Changelog:
 *  12/12/21, Adam (QuickTables)
 *      Add QuickTables: QuickTables are fast lookups fo anything spawned on a spawner.
 *      A QuickTable is keyed on the mobile or item, and the Value is a list of spawners that spawn that object.
 *      There are three quick tables, one for all spawned objcts, one for items only, and one for mobiles only.
 *      QuickTables are populated on server load, and updated when a spawner is changed or deleted.
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	04/27/09, plasma
 *		Add region name convert for nujelm and skarabrae to Nujel'm and Skara Brae
 *	04/18/09, plasma
 *		Was neccesary to include spawners without something spawned, for factions
 *	5/21/08, Adam
 *		Cleanup up console output to match other outout
 *	5/2/08, Adam
 *		Move the Region checks to where we try to select a spawner because regions have not been loaded at the time
 *		the spawner cache is loaded.
 *	4/24/08, Adam
 *		Fixed a bug in GetRandomSpawner() where we were dereferencing 'spawners' before it was initialized. 
 *		I replaced 'spawners' with 'random' and all seems good.
 *	03/17/08, plasma
 *		Initial creation
 */

namespace Server.Mobiles
{
    public static class SpawnerCache
    {
        //public enum SpawnerType
        //{
        //    None = 0,
        //    Overland
        //}




    }

}