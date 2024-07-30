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

using Server.Diagnostics;
using Server.Items;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Commands
{
    public class ContainedCommandImplementor : BaseCommandImplementor
    {
        public ContainedCommandImplementor()
        {
            Accessors = new string[] { "Contained" };
            SupportRequirement = CommandSupport.Contained;
            AccessLevel = AccessLevel.GameMaster;
            Usage = "Contained <command> [condition]";
            Description = "Invokes the command on all child items in a targeted container. Optional condition arguments can further restrict the set of objects.";
        }

        public override void Process(Mobile from, object target, BaseCommand command, string[] args)
        {
            if (command.ValidateArgs(this, new CommandEventArgs(from, command.Commands[0], GenerateArgString(args), args)))
                if (target == null)
                    from.BeginTarget(-1, command.ObjectTypes == ObjectTypes.All, TargetFlags.None, new TargetStateCallback(OnTarget), new object[] { command, args });
                else
                    OnTarget(from, target, new object[] { command, args });
        }

        public void OnTarget(Mobile from, object targeted, object state)
        {
            if (!BaseCommand.IsAccessible(from, targeted))
            {
                from.SendMessage("That is not accessible.");
                return;
            }

            object[] states = (object[])state;
            BaseCommand command = (BaseCommand)states[0];
            string[] args = (string[])states[1];

            if (command.ObjectTypes == ObjectTypes.Mobiles)
                return; // sanity check

            if (!(targeted is Container))
            {
                from.SendMessage("That is not a container.");
            }
            else
            {
                try
                {
                    ObjectConditional cond = ObjectConditional.Parse(from, ref args);

                    bool items, mobiles;

                    if (!CheckObjectTypes(command, cond, out items, out mobiles))
                        return;

                    if (!items)
                    {
                        from.SendMessage("This command only works on items.");
                        return;
                    }

                    Container cont = (Container)targeted;

                    Item[] found;

                    if (cond.Type == null)
                        found = cont.FindItemsByType(typeof(Item), true);
                    else
                        found = cont.FindItemsByType(cond.Type, true);

                    ArrayList list = new ArrayList();

                    for (int i = 0; i < found.Length; ++i)
                    {
                        if (cond.CheckCondition(found[i]))
                            list.Add(found[i]);
                    }

                    RunCommand(from, list, command, args);
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    from.SendMessage(e.Message);
                }
            }
        }
    }
}