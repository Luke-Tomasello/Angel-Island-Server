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


/* Scripts/Regions/Felucca/Dungeon.cs
 * ChangeLog
 *	04/24/09, plasma
 *		Commented out all regions, replaced with DRDT
 *	9/21/05, Adam
 *		Remove Wind and Deceit as they are controlled by DRDT
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/11/04, mith
 *		Moved Wind from Town.cs to Dungeon.cs, this removes guards and prevents recall, mark, and gate.
 */

using System;
using Server;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Seventh;
using Server.Spells.Fourth;
using Server.Spells.Sixth;

namespace Server.Regions
{
	public class FeluccaDungeon : Region
	{
		public static void Initialize()
		{
			/*
			Region.AddRegion( new FeluccaDungeon( "Covetous" ) );
			Region.AddRegion( new FeluccaDungeon( "Despise" ) );
			Region.AddRegion( new FeluccaDungeon( "Destard" ) );
			Region.AddRegion( new FeluccaDungeon( "Hythloth" ) );
			Region.AddRegion( new FeluccaDungeon( "Shame" ) );
			Region.AddRegion( new FeluccaDungeon( "Wrong" ) );
			Region.AddRegion( new FeluccaDungeon( "Terathan Keep" ) );
			Region.AddRegion( new FeluccaDungeon( "Fire" ) );
			Region.AddRegion( new FeluccaDungeon( "Ice" ) );
			Region.AddRegion( new FeluccaDungeon( "Orc Cave" ) );
			*/
			// Controlled by DRDT
			//Region.AddRegion( new FeluccaDungeon( "Wind" ) );
			//Region.AddRegion( new FeluccaDungeon( "Deceit" ) );
		}

		public FeluccaDungeon( string name ) : base( "the dungeon", name, Map.Felucca )
		{
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return false;
		}

		public override void OnEnter( Mobile m )
		{
			//base.OnEnter( m ); // You have entered the dungeon {0}
		}

		public override void OnExit( Mobile m )
		{
			//base.OnExit( m );
		}

		public override void AlterLightLevel( Mobile m, ref int global, ref int personal )
		{
			global = LightCycle.DungeonLevel;
		}

		/*RunUO 1.0RC0 had this commented out*/
		/**/public override bool OnBeginSpellCast( Mobile m, ISpell s )
		{
			if ( s is GateTravelSpell || s is RecallSpell || s is MarkSpell )
			{
				m.SendMessage( "You cannot cast that spell here." );
				return false;
			}
			else
			{
				return base.OnBeginSpellCast( m, s );
			}
		}/**/
	}
}
#endif