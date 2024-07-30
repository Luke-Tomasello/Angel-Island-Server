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

/* Scripts/Commands/Abstracted/Implementors/GlobalCommandImplementor.cs
 * CHANGELOG
 *  8/25/22, Adam
 *      Add logging of what was found as well as seeding the jump list.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Server.Commands
{
    public class GlobalCommandImplementor : BaseCommandImplementor
    {
        public GlobalCommandImplementor()
        {
            Accessors = new string[] { "Global" };
            SupportRequirement = CommandSupport.Global;
            SupportsConditionals = true;
            AccessLevel = AccessLevel.Administrator;
            Usage = "Global <command> [condition]";
            Description = "Invokes the command on all appropriate objects in the world. Optional condition arguments can further restrict the set of objects.";
        }

        public override void Compile(Mobile from, BaseCommand command, ref string[] args, ref object obj)
        {
            try
            {
                ObjectConditional cond = ObjectConditional.Parse(from, ref args);

                bool items, mobiles;

                if (!CheckObjectTypes(command, cond, out items, out mobiles))
                    return;

                // log what we find
                LogHelper Logger = new LogHelper("global.log", from, false);

                // reset jump table
                Mobiles.PlayerMobile pm = from as Mobiles.PlayerMobile;
                pm.JumpIndex = 0;
                pm.JumpList = new System.Collections.ArrayList();

                ArrayList list = new ArrayList();

                if (items)
                {
                    foreach (Item item in World.Items.Values)
                    {
                        if (cond.CheckCondition(item))
                        {
                            pm.JumpList.Add(item);
                            Logger.Log(LogType.Item, item);
                            list.Add(item);
                        }
                    }
                }

                if (mobiles)
                {
                    foreach (Mobile mob in World.Mobiles.Values)
                    {
                        if (cond.CheckCondition(mob))
                        {
                            pm.JumpList.Add(mob);
                            Logger.Log(LogType.Mobile, mob);
                            list.Add(mob);
                        }
                    }
                }

                #region Sort
                if (items)
                {   // sort on most recent first
                    List<Item> temp = pm.JumpList.Cast<Item>().ToList();
                    temp.Sort((x, y) => { return y.Created.CompareTo(x.Created); });
                    pm.JumpList.Clear();
                    pm.JumpList.AddRange(temp);
                }

                if (mobiles)
                {   // sort on most recent first
                    List<Mobile> temp = pm.JumpList.Cast<Mobile>().ToList();
                    temp.Sort((x, y) => { return y.Created.CompareTo(x.Created); });
                    pm.JumpList.Clear();
                    pm.JumpList.AddRange(temp);
                }
                #endregion Sort

                Logger.Finish();
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