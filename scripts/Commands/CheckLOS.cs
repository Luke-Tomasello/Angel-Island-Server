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

/* Scripts/Commands/CheckLOS.cs
 * ChangeLog
 *  1/3/22, Adam
 *      added CanSee to the LOS check.
 *      Now you get messages for each test individually.
 *	4/28/08, Adam
 *		First time checkin
 */


using Server.Diagnostics;
using Server.Engines;
using Server.Targeting;
using System;

namespace Server.Commands
{
    public class CheckLOSCommand
    {
        public static void Initialize()
        {
            if (TestCenterServer.Enabled == true || Core.Debug == true)
                Server.CommandSystem.Register("CheckLOS", AccessLevel.Player, new CommandEventHandler(CheckLOS_OnCommand));
        }

        public static void CheckLOS_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile.AccessLevel > AccessLevel.Player)
            {
                // LOS and visibility checks are access-level sensitive.
                // To ensure accurate results, the test will run as AccessLevel.Player.
                e.Mobile.SendMessage("For accurate results, your access level will be temporarily set to Player during this test.");
            }

            try
            {
                e.Mobile.Target = new LOSTarget();
                e.Mobile.SendMessage("Check LOS to which object?");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

        }

        private class LOSTarget : Target
        {
            public LOSTarget()
                : base(15, false, TargetFlags.None)
            {
                CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                using (new AccessLevelScope(from, AccessLevel.Player))
                {
                    bool los = from.Map.LineOfSight(
                        from.Map.GetPoint(from, true),
                        from.Map.GetPoint(targ, false));

                    from.SendMessage("You {0} see that (LOS).", los ? "can" : "cannot");
                    from.SendMessage("You {0} see that (CanSee).", from.CanSee(targ) ? "can" : "cannot");
                }
            }

            public sealed class AccessLevelScope : IDisposable
            {
                private readonly Mobile _mobile;
                private readonly AccessLevel _oldLevel;
                private readonly bool _changed;

                public AccessLevelScope(Mobile mobile, AccessLevel forcedLevel)
                {
                    _mobile = mobile;
                    _oldLevel = mobile.AccessLevel;
                    _changed = _oldLevel != forcedLevel;

                    if (_changed)
                        _mobile.AccessLevel = forcedLevel;
                }

                public void Dispose()
                {
                    if (_changed)
                        _mobile.AccessLevelInternal = _oldLevel;
                }
            }

        }

    }
}