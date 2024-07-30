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

/* Scripts\Misc\Diagnostics.cs
 * ChangeLog
 *  8/11/07, Adam
 *      Have Assert return the result code so the Assert can be used within an IF
 *	8/2/07, Adam
 *		Add Assert() Diagnostic to raise an exception, record it an email the results to the team
 *      First time checkin
 */

using Server.Diagnostics;
using System;

namespace Server.Misc
{
    public class Diagnostics
    {
        public static bool Assert(bool assertion, string text, string fileinfo)
        {
            if (assertion == true)
                return true;

            try { throw new ApplicationException(text + " " + fileinfo); }
            catch (Exception ex) { LogHelper.LogException(ex); }

            return false;
        }
    }
}