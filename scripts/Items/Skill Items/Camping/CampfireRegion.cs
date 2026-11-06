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

/* Items/Skill Items/Camping/CampfireRegion.cs
 * CHANGELOG:
 *	10/22/04 - Pix
 *		Changed camping to not use campfireregion.
 *		#if'd out class so it won't be used anymore.
 *	9/11/04, Pixie
 *		Removed the override of the Contains() method - it was causing all sorts of grief.
 *		Changed GetLogoutDelay to require a bedroll and waiting for the camp to be secure.
 *	7/25/04, Pixie
 *		Added all the other override functions so that the campingregion calls all the functions for the region
 *		the campingregion was created in.
 *	7/15/04, Pixie
 *		Made it so spells and lightlevels use behavior for the region that the campfire is created in
 *	5/10/04, Pixie
 *		Initial working revision
 */

