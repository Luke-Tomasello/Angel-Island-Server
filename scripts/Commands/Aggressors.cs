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

/* scripts/commands/Aggressors.cs
 * 	CHANGELOG:
 * 	3/24/05, Kitaras
 *	Initial Version
 */

using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Commands
{

    public class AggressorsCommand : BaseCommand
    {

        public static void Initialize()
        {
            TargetCommands.Register(new AggressorsCommand());
        }

        public AggressorsCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.Simple;
            Commands = new string[] { "Aggressors", "Aggres" };
            ObjectTypes = ObjectTypes.Mobiles;

            Usage = "Aggressors <target>";
            Description = "Lists the aggressor list of the target";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            Mobile m = obj as Mobile;
            Mobile from = e.Mobile;

            if (m != null)
            {
                List<AggressorInfo> aggressors = m.Aggressors;
                if (aggressors.Count > 0)
                {
                    for (int i = 0; i < aggressors.Count; ++i)
                    {

                        AggressorInfo info = (AggressorInfo)aggressors[i];
                        Mobile temp = info.Attacker;
                        from.SendMessage("Aggressor:{0} '{1}' Ser:{2}, Time:{3}, Expired:{4}",
                                    (temp is PlayerMobile ? ((PlayerMobile)temp).Account.ToString() : ((Mobile)temp).Name),
                                    temp.GetType().Name,
                                    temp.Serial,
                                    info.LastCombatTime.TimeOfDay,
                                    info.Expired);
                    }
                }
            }
            else
            {
                AddResponse("Please target a mobile.");
            }
        }

    }


}