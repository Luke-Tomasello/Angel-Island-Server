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

/* Scripts\Engines\Mitigation\MultiBox.cs
 * ChangeLog:
 * 1/17/22, Adam
 *  removed console print statement when group members are attacking each other - too spammy
 * 1/14/22, Adam
 * Initial creation
 */

/* Description:
 * In this implementation we only handle MultiBoxPVP.
 * In essence, we find ‘groups’ of 2 or more players all on the same IP.
 * If they aggress someone outside their group, we heuristically determine the leader of the group,
 *  then break the LOS between all the followers and the leader.
 * This effectively breaks the ‘following’ initiated client-side.
 * It’s not perfect, but should work well enough to make MultiBoxPVP near worthless.
 */

namespace Server.Engines
{
}