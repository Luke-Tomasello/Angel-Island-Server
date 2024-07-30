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

/* Scripts\Commands\TakeOwnership.cs
 * ChangeLog
 *  06/12/07, Adam
 *      First time checkin
 *      Takes ownership oif a house.
 *      Could be extended to take ownership of a boat as well.
 */

using Server.Diagnostics;
using Server.Multis;        // HouseSign
using Server.Targeting;
using System;

namespace Server.Commands
{
    public class TakeOwnership
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("TakeOwnership", AccessLevel.GameMaster, new CommandEventHandler(TakeOwnership_OnCommand));
        }

        [Usage("TakeOwnership")]
        [Description("take ownership of a house.")]
        private static void TakeOwnership_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new TakeOwnershipTarget();
            e.Mobile.SendMessage("What do you wish to take ownership of?");
        }

        private class TakeOwnershipTarget : Target
        {
            public TakeOwnershipTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is HouseSign && (target as HouseSign).Structure != null)
                {
                    try
                    {
                        BaseHouse bh = (target as HouseSign).Structure as BaseHouse;
                        bh.AdminTransfer(from);
                    }
                    catch (Exception tse)
                    {
                        LogHelper.LogException(tse);
                    }
                }
                else
                {
                    from.SendMessage("That is not a house sign.");
                }
            }
        }

    }
}