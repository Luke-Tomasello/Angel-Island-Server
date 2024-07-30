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

/* Scripts/Commands/Abstracted/Implementors/OnlineCommandImplementor.cs
 * CHANGELOG
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Commands
{
    public class OnlineCommandImplementor : BaseCommandImplementor
    {
        public OnlineCommandImplementor()
        {
            Accessors = new string[] { "Online" };
            SupportRequirement = CommandSupport.Online;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.Administrator;
            Usage = "Online <command> [condition]";
            Description = "Invokes the command on all mobiles that are currently logged in. Optional condition arguments can further restrict the set of objects.";
        }

        public override void Compile(Mobile from, BaseCommand command, ref string[] args, ref object obj)
        {
            try
            {
                ObjectConditional cond = ObjectConditional.Parse(from, ref args);

                bool items, mobiles;

                if (!CheckObjectTypes(command, cond, out items, out mobiles))
                    return;

                if (!mobiles) // sanity check
                {
                    command.LogFailure("This command does not support mobiles.");
                    return;
                }

                ArrayList list = new ArrayList();

                //ArrayList states = NetState.Instances;
                List<NetState> states = NetState.Instances;

                for (int i = 0; i < states.Count; ++i)
                {
                    NetState ns = states[i];
                    Mobile mob = ns.Mobile;

                    if (mob != null && cond.CheckCondition(mob))
                        list.Add(mob);
                }

                obj = list;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                from.SendMessage(ex.Message);
            }
        }
    }
}