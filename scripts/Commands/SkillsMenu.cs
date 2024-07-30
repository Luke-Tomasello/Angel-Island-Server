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

using Server.Scripts.Gumps;
using Server.Targeting;

namespace Server.Commands
{
    public class Skills
    {
        public static void Initialize()
        {
            Register();
        }

        public static void Register()
        {
            Server.CommandSystem.Register("Skills", AccessLevel.Counselor, new CommandEventHandler(Skills_OnCommand));
        }

        private class SkillsTarget : Target
        {
            public SkillsTarget()
                : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Mobile)
                    from.SendGump(new SkillsGump(from, (Mobile)o));
            }
        }

        [Usage("Skills")]
        [Description("Opens a menu where you can view or edit skills of a targeted mobile.")]
        private static void Skills_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new SkillsTarget();
        }
    }
}