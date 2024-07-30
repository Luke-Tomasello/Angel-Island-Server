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

/* Scripts/Commands/Abstracted/Implementors/SingleCommandImplementor.cs
 * CHANGELOG
 *  6/14/04, Pix
 *		Removed debugging message.
 *  6/7/04, Pix
 *		Reverted the previous fix, but skipped the check for IsAccessible
 *		if it gets the BountyCommand.
 *  6/7/04, Pix
 *		1.0RC0's OnTarget added a check to the new IsAccessible command which
 *		broke the Bounty command.  Removing the check for IsAccessible.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Targeting;
using System.Collections.Generic;

namespace Server.Commands
{
    public class SingleCommandImplementor : BaseCommandImplementor
    {
        public SingleCommandImplementor()
        {
            Accessors = new string[] { "Single" };
            SupportRequirement = CommandSupport.Single;
            AccessLevel = AccessLevel.Counselor;
            Usage = "Single <command>";
            Description = "Invokes the command on a single targeted object. This is the same as just invoking the command directly.";
        }

        public override void Register(BaseCommand command)
        {
            base.Register(command);

            for (int i = 0; i < command.Commands.Length; ++i)
                Server.CommandSystem.Register(command.Commands[i], command.AccessLevel, new CommandEventHandler(Redirect));
        }

        public void Redirect(CommandEventArgs e)
        {
            BaseCommand command = (BaseCommand)Commands[e.Command];

            if (command == null)
                e.Mobile.SendMessage("That is either an invalid command name or one that does not support this modifier.");
            else if (e.Mobile.AccessLevel < command.AccessLevel)
                e.Mobile.SendMessage("You do not have access to that command.");
            else if (command.ValidateArgs(this, e))
                Process(e.Mobile, e.Target, command, e.Arguments);
        }

        public override void Process(Mobile from, object target, BaseCommand command, string[] args)
        {
            if (command.ValidateArgs(this, new CommandEventArgs(from, command.Commands[0], GenerateArgString(args), args)))
                if (target == null)
                    from.BeginTarget(-1, command.ObjectTypes == ObjectTypes.All, TargetFlags.None, new TargetStateCallback(OnTarget), new object[] { command, args });
                else
                    OnTarget(from, target, new object[] { command, args });
        }
        private string[] AllowParentheticals(string[] args)
        {
            List<string> result = new List<string>();
            for (int ix = 0; ix < args.Length; ix++)
            {
                if (args[ix].StartsWith("("))
                {
                    string token = string.Empty;
                    while (ix < args.Length)
                    {
                        token += args[ix];
                        if (args[ix].EndsWith(")"))
                        {
                            result.Add(token);
                            break;
                        }
                        ix++;
                    }
                }
                else
                {
                    result.Add(args[ix]);
                }
            }
            return result.ToArray();
        }
        public void OnTarget(Mobile from, object targeted, object state)
        {
            object[] states = (object[])state;
            BaseCommand command = (BaseCommand)states[0];
            string[] args = (string[])states[1];

            args = AllowParentheticals(args);

            if (command is BountySystem.BountyCommand)
            {
                //from.SendMessage("Use the bountycommand, Luke!");
            }
            else if (!BaseCommand.IsAccessible(from, targeted))
            {
                from.SendMessage("That is not accessible.");
                return;
            }

            switch (command.ObjectTypes)
            {
                case ObjectTypes.Both:
                    {
                        if (!(targeted is Item) && !(targeted is Mobile))
                        {
                            from.SendMessage("This command does not work on that.");
                            return;
                        }

                        break;
                    }
                case ObjectTypes.Items:
                    {
                        if (!(targeted is Item))
                        {
                            from.SendMessage("This command only works on items.");
                            return;
                        }

                        break;
                    }
                case ObjectTypes.Mobiles:
                    {
                        if (!(targeted is Mobile))
                        {
                            from.SendMessage("This command only works on mobiles.");
                            return;
                        }

                        break;
                    }
            }

            RunCommand(from, targeted, command, args);
        }
    }
}