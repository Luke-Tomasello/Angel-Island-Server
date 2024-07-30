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

/* Scripts/Commands/AddonSlice.cs
 * Changelog
 *	12/5/21 Yoar
 *		Initial version.
 */

using Server.Multis;
using Server.Targeting;

namespace Server.Commands
{
    public static class HouseDesignCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("DesignHouse", AccessLevel.GameMaster, new CommandEventHandler(DesignHouse_OnCommand));
            CommandSystem.Register("DesignCommit", AccessLevel.GameMaster, new CommandEventHandler(DesignCommit_OnCommand));
        }

        [Usage("DesignHouse")]
        [Description("Go into customization mode for the targted HouseFoundation.")]
        private static void DesignHouse_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the house sign of the house you wish to customize.");
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, DesignHouse_OnTarget);
        }

        private static void DesignHouse_OnTarget(Mobile from, object targeted)
        {
            HouseSign sign = targeted as HouseSign;

            if (sign == null)
                from.SendMessage("That is not a house sign.");
            else if (sign.Structure == null)
                from.SendMessage("That sign is not attached to any house.");
            else if (!(sign.Structure is HouseFoundation))
                from.SendMessage("That house cannot be customized.");
            else
                ((HouseFoundation)sign.Structure).BeginCustomize(from);
        }

        [Usage("DesignCommit")]
        [Description("Commit the house that you are currently designing. Skips design legality checks.")]
        private static void DesignCommit_OnCommand(CommandEventArgs e)
        {
            HouseFoundation foundation = BaseHouse.FindHouseAt(e.Mobile) as HouseFoundation;

            if (foundation == null || foundation.Customizer != e.Mobile)
                e.Mobile.SendMessage("You are not customizing any house.");
            else
                foundation.EndConfirmCommit(e.Mobile, true);
        }
    }
}