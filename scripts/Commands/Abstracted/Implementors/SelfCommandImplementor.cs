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

namespace Server.Commands
{
    public class SelfCommandImplementor : BaseCommandImplementor
    {
        public SelfCommandImplementor()
        {
            Accessors = new string[] { "Self" };
            SupportRequirement = CommandSupport.Self;
            AccessLevel = AccessLevel.Counselor;
            Usage = "Self <command>";
            Description = "Invokes the command on the commanding player.";
        }

        public override void Compile(Mobile from, BaseCommand command, ref string[] args, ref object obj)
        {
            if (command.ObjectTypes == ObjectTypes.Items)
                return; // sanity check

            obj = from;
        }
    }
}