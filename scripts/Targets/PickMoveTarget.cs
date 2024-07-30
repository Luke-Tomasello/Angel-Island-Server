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

/* ChangeLog:
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Commands;
using Server.Targeting;

namespace Server.Targets
{
    public class PickMoveTarget : Target
    {
        private bool mobilesOnly = false;
        public PickMoveTarget(bool mobiles_only = false)
            : base(-1, false, TargetFlags.None)
        {
            mobilesOnly = mobiles_only;
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (!BaseCommand.IsAccessible(from, o))
            {
                from.SendMessage("That is not accessible.");
                return;
            }

            if (o is not Mobile && mobilesOnly)
            {
                from.SendMessage("That is not a mobile.");
                return;
            }

            if (o is Item item && Utility.IsRestrictedObject(item) && from.AccessLevel < AccessLevel.Owner)
            {
                string text = string.Format(string.Format("{0}s are not authorized to move core shard assets.", from.AccessLevel));
                CommandLogging.WriteLine(from, "{0} {1} attempting to move {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(item));
                from.SendMessage(text);
                return;
            }

            if (o is Item || o is Mobile)
                from.Target = new MoveTarget(o);
        }
    }
}