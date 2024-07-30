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

/* Scripts/Engines/RTT/RTTCommand.cs
 * CHANGELOG:
 *  8/26/2007, Pix
 *      Moved command to own class.
 *      Now takes optional 'mode' argument.
 */

using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.RTT
{
    class RTTCommand
    {
        #region Command
        public static void Initialize()
        {
            Server.CommandSystem.Register("RTT", AccessLevel.Counselor, new CommandEventHandler(RTT_OnCommand));
        }

        [Usage("RTT")]
        [Description("Does a RTT on yourself, or if you're staff, does an RTT on a target player.")]
        public static void RTT_OnCommand(CommandEventArgs e)
        {
            int mode = 0;
            try
            {
                if (e.Arguments.Length > 0)
                {
                    try
                    {
                        mode = int.Parse(e.Arguments[0]);
                    }
                    catch //(Exception e1)
                    {
                        e.Mobile.SendMessage("Error with argument - using default test.");
                    }
                }

                if (e.Mobile.AccessLevel > AccessLevel.Player)
                {
                    e.Mobile.SendMessage("Target a player to RTT");
                    e.Mobile.Target = new RTTTarget(mode);
                }
                else
                {
                    if (e.Mobile is PlayerMobile)
                    {
                        ((PlayerMobile)e.Mobile).RTT("Forced AFK check!", true, mode, "Command");
                    }
                }
            }
            catch (Exception ex)
            {
                Server.Diagnostics.LogHelper.LogException(ex);
            }
        }

        private class RTTTarget : Target
        {
            private int m_Mode = 0;

            public RTTTarget(int mode)
                : base(11, false, TargetFlags.None)
            {
                m_Mode = mode;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!(targeted is PlayerMobile))
                {
                    from.SendMessage("You can only target players!");
                    return;
                }
                else
                {
                    ((PlayerMobile)targeted).RTT("AFK check!", true, m_Mode, "Command");
                }
            }
        }
        #endregion
    }
}