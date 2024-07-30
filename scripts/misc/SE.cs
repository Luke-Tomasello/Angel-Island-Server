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

/* Misc/SE.cs
 * CHANGELOG:
 *  2/16/05, Pix
 *		Initial Version :-p
 */

namespace Server
{
    public class SE
    {
        public const bool Enabled = false; //true;

        public static void Configure()
        {
            Core.RuleSets.SE_SVR = Enabled;
        }
    }
}