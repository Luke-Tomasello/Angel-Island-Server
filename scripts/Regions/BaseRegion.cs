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

/* Scripts/Regions/BaseRegion.cs
 * CHANGELOG
 *	02/21/11, Adam
 *		This new base class for regions serves two purposes:
 *			(1) To mimic the region FILE STRUCTURE of RunUO 2.0 to aid in major system upgrades (factions)
 *			(2) To serve as a thunk layer to convert RunUO 2.0 style regions (faction stronghold) to our style
 *		After looking at the RunUO 2.0 regions and how different they are than ours, I choose to instead use this thunk
 *		to simply translate the regions and give consistant file structure. In RunUO 2.0 this base region is used for 
 *		special spawner management (Doom Gauntlent I believe) and other things. For now we don't need and won't include
 *		this functionality.
 */

using System.Collections.Generic;

namespace Server.Regions
{
    public class BaseRegion : Region
    {
        //public StrongholdRegion(Faction faction)
        //: base(faction.Definition.FriendlyName, Faction.Facet, Region.DefaultPriority, faction.Definition.Stronghold.Area)

        public BaseRegion(string name, Map map, int priority, Rectangle2D[] area)
            : base("", name, map)
        {
            base.Priority = priority;
            if (area != null)
                base.Coords.AddRange(ConvertTo3D(new List<Rectangle2D>(area)));
        }

        /*
		public BaseRegion(string prefix, string name, Map map) : base("", name, map)
		{
			m_Prefix = prefix;
			m_Name = name;
			m_Map = map;

			m_Priority = Region.LowestPriority;
			m_GoLoc = Point3D.Zero;

			m_Players = new Dictionary<Serial, Mobile>(2);	// probably not many players 
			m_Mobiles = new Dictionary<Serial, Mobile>();	// probably a reasonable number of mobiles

			m_Load = true;

			m_UId = m_RegionUID++;
		}*/
    }
}