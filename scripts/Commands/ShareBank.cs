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

/* Scripts/Commands/CheckLOS.cs
 * ChangeLog
 * 12/29/2021, Adam ([sharebank)
 *      Add new [sharebank command to enroll/disenroll them in the account-wide bankbox sharing.
 *      Implemented: Balance, Check, Withdraw
 *	12/29/21, Adam
 *		First time checkin
 *		Add a property PlayerMobile for bankbox sharing within account. 
 *      Each player that wants to share his bank, must enroll in the system with the [ShareBank command
 */


using Server.Diagnostics;
using Server.Mobiles;
using System;

namespace Server.Commands
{
    internal class ShareBank
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("ShareBank", AccessLevel.Player, new CommandEventHandler(ShareBank_OnCommand));
        }

        public static void ShareBank_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Mobile is PlayerMobile pm)
                {
                    if ((pm as PlayerMobile) != null)
                    {
                        LogHelper logger = new LogHelper("ShareBank.log", false, true);
                        pm.ShareBank = !pm.ShareBank;
                        if (pm.ShareBank)
                        {
                            pm.SendMessage("You have enrolled in bankbox sharing.");
                            logger.Log(LogType.Mobile, pm, "has enrolled in bankbox sharing.");
                        }
                        else
                        {
                            pm.SendMessage("You are no longer enrolled in bankbox sharing.");
                            logger.Log(LogType.Mobile, pm, "is no longer enrolled in bankbox sharing.");
                        }
                        logger.Finish();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

        }
    }
}