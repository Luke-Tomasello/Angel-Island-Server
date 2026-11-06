/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

using Server.Mobiles;
using Server.Scripts.Gumps;
using Server.Targeting;
using System;
using System.IO;

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
            Server.CommandSystem.Register("TuneCreature", AccessLevel.Counselor, new CommandEventHandler(TuneCreature_OnCommand));
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

        private class TuneCreatureTarget : Target
        {
            private readonly CommandEventArgs m_Args;

            public TuneCreatureTarget(CommandEventArgs e)
                : base(-1, true, TargetFlags.None)
            {
                m_Args = e;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (!(o is BaseCreature bc))
                {
                    from.SendMessage("That is not a creature.");
                    return;
                }

                bool reset =
                    m_Args != null &&
                    m_Args.Length > 0 &&
                    string.Equals(m_Args.GetString(0), "reset", StringComparison.OrdinalIgnoreCase);

                if (reset)
                {
                    string serverName = Core.Server; // e.g. "Mortalis"
                    string dir = Path.Combine("Data", serverName);
                    string file = bc.GetType().Name + ".json";
                    string path = Path.Combine(dir, file);

                    try
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                            from.SendMessage("Creature tuning reset: {0}", file);
                        }
                        else
                        {
                            from.SendMessage("No tuning file found to reset: {0}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        from.SendMessage("Reset failed: {0}", ex.Message);
                    }

                    return;
                }

                from.SendGump(new CreatureTuningGump(from, bc));
            }
        }

        [Usage("Skills")]
        [Description("Opens a menu where you can view or edit skills of a targeted mobile.")]
        private static void Skills_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new SkillsTarget();
        }

        [Usage("TuneCreature [reset]")]
        [Description("Opens the creature tuning gump for a targeted creature. Optional 'reset' clears saved overrides.")]
        private static void TuneCreature_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new TuneCreatureTarget(e);
        }
    }
}