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

#if OLD_REGIONS


/* Scripts/Regions/GreenAcres.cs
 * CHANGELOG
 *	04/24/09, plasma
 *		Commented out all regions, replaced with DRDT
 *  5/1/07, Adam
 *      Allow house placement by players if TestCenter.Enabled == true
 */

using System;
using Server;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Seventh;
using Server.Spells.Fourth;
using Server.Spells.Sixth;
using Server.Misc;                      // test center

namespace Server.Regions
{
	public class GreenAcres : Region
	{
		public static void Initialize()
		{
			//Region.AddRegion( new GreenAcres( Map.Felucca ) );
			// Region.AddRegion( new GreenAcres( Map.Trammel ) );
		}

		public GreenAcres(Map map)
			: base("", "Green Acres", map)
		{
		}

		public override bool AllowHousing(Mobile from, Point3D p)
		{
			// we allow players to place houses on Green Acres on Test Center
			if (from.AccessLevel == AccessLevel.Player && TestCenter.Enabled == false)
				return false;
			else
				return true;
		}

		public override void OnEnter(Mobile m)
		{
		}

		public override void OnExit(Mobile m)
		{
		}

		public override bool OnBeginSpellCast(Mobile m, ISpell s)
		{
			if ((s is GateTravelSpell || s is RecallSpell || s is MarkSpell) && m.AccessLevel == AccessLevel.Player)
			{
				m.SendMessage("You cannot cast that spell here.");
				return false;
			}
			else
			{
				return base.OnBeginSpellCast(m, s);
			}
		}
	}
}
#endif