/***************************************************************************
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

/* Scripts/Misc/StaffAlerts.cs
 * ChangeLog:
 *  11/11/2026, Adam
 *      First version. The Herald's first subscriber: Staff-audience events (exceptions) go, as a System Message in
 *      red, to online staff who asked for them (PlayerMobile.ServerAlerts; UOAdmin sets it at login). Other staff in
 *      game see nothing. Also runs the Herald's pump on the game thread, four times a second.
 */

using Server.Diagnostics;
using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Misc
{
    public sealed class StaffAlerts : IHeraldSubscriber
    {
        private const int AlertHue = 0x22;  // red

        public static void Initialize()
        {
            Herald.Subscribe(new StaffAlerts());
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(0.25), new TimerCallback(Herald.Pump));
        }

        public void Deliver(HeraldEvent e)
        {
            if (e.Audience != Audience.Staff)
                return;

            foreach (NetState state in NetState.Instances.ToArray())
            {
                if (state.Mobile is PlayerMobile pm && pm.AccessLevel > AccessLevel.Player && pm.ServerAlerts)
                    pm.SendMessage(AlertHue, e.Text);
            }
        }
    }
}
