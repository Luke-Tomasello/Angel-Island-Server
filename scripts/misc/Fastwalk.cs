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

/* Scripts/Misc/Fastwalk.cs
 * CHANGELOG
 *
 *  02/05/07 Taran Kain
 *		Added a bit of flexibility and logging capabilities.
 */

using Server.Diagnostics;
using System;
using System.Collections.Generic;

namespace Server.Misc
{
    // This fastwalk detection is no longer required
    // As of B36 PlayerMobile implements movement packet throttling which more reliably controls movement speeds
    public class Fastwalk
    {
        public static bool ProtectionEnabled = false;
        public static int WarningThreshold = 4;
        public static TimeSpan WarningCooldown = TimeSpan.FromSeconds(0.4);

        private static Dictionary<Mobile, List<DateTime>> m_Blocks = new Dictionary<Mobile, List<DateTime>>();

        public static void Initialize()
        {
            EventSink.FastWalk += new FastWalkEventHandler(OnFastWalk);
        }

        public static void OnFastWalk(FastWalkEventArgs e)
        {
            if (!m_Blocks.ContainsKey(e.NetState.Mobile))
            {
                m_Blocks.Add(e.NetState.Mobile, new List<DateTime>());
            }
            m_Blocks[e.NetState.Mobile].Add(DateTime.UtcNow);

            if (ProtectionEnabled)
            {
                e.Blocked = true;//disallow this fastwalk
                                 //Console.WriteLine("Client: {0}: Fast movement detected (name={1})", e.NetState, e.NetState.Mobile.Name);
            }

            try
            {
                List<DateTime> blocks = m_Blocks[e.NetState.Mobile];
                if (e.FastWalkCount > WarningThreshold &&
                    blocks.Count >= 2 && // sanity check, shouldn't be possible to reach this point w/o Count >= 2
                    (blocks[blocks.Count - 1] - blocks[blocks.Count - 2]) > WarningCooldown)
                {
                    Console.WriteLine("FW Warning");
                }
            }
            catch (Exception ex) // we can only exception if Mobile.FwdMaxSteps < 2 - make sure SecurityManagementConsole doesn't set it too low
            {
                LogHelper.LogException(ex);
                Console.WriteLine(ex);
            }
        }
    }
}