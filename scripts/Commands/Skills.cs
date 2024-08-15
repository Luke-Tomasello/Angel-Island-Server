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

using Server.Targeting;
using System;

namespace Server.Commands
{
    public class SkillsCommand
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("SetSkill", AccessLevel.GameMaster, new CommandEventHandler(SetSkill_OnCommand));
            Server.CommandSystem.Register("GetSkill", AccessLevel.GameMaster, new CommandEventHandler(GetSkill_OnCommand));
            Server.CommandSystem.Register("SetAllSkills", AccessLevel.GameMaster, new CommandEventHandler(SetAllSkills_OnCommand));
        }

        [Usage("SetSkill <name> <value>")]
        [Description("Sets a skill value by name of a targeted mobile.")]
        public static void SetSkill_OnCommand(CommandEventArgs arg)
        {
            if (arg.Length != 2)
            {
                arg.Mobile.SendMessage("SetSkill <skill name> <value>");
            }
            else
            {
                SkillName skill;
                try
                {
                    skill = (SkillName)Enum.Parse(typeof(SkillName), arg.GetString(0), true);
                }
                catch
                {
                    arg.Mobile.SendLocalizedMessage(1005631); // You have specified an invalid skill to set.
                    return;
                }
                arg.Mobile.Target = new SkillTarget(skill, arg.GetDouble(1));
            }
        }

        [Usage("SetAllSkills <name> <value>")]
        [Description("Sets all skill values of a targeted mobile.")]
        public static void SetAllSkills_OnCommand(CommandEventArgs arg)
        {
            if (arg.Length != 1)
            {
                arg.Mobile.SendMessage("SetAllSkills <value>");
            }
            else
            {
                arg.Mobile.Target = new AllSkillsTarget(arg.GetDouble(0));
            }
        }

        [Usage("GetSkill <name>")]
        [Description("Gets a skill value by name of a targeted mobile.")]
        public static void GetSkill_OnCommand(CommandEventArgs arg)
        {
            if (arg.Length != 1)
            {
                arg.Mobile.SendMessage("GetSkill <skill name>");
            }
            else
            {
                SkillName skill;
                try
                {
                    skill = (SkillName)Enum.Parse(typeof(SkillName), arg.GetString(0), true);
                }
                catch
                {
                    arg.Mobile.SendLocalizedMessage(1005631); // You have specified an invalid skill to set.
                    return;
                }

                arg.Mobile.Target = new SkillTarget(skill);
            }
        }

        public class AllSkillsTarget : Target
        {
            private double m_Value;

            public AllSkillsTarget(double value)
                : base(-1, false, TargetFlags.None)
            {
                m_Value = value;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    Mobile targ = (Mobile)targeted;
                    Server.Skills skills = targ.Skills;

                    for (int i = 0; i < skills.Length; ++i)
                        skills[i].Base = m_Value;

                    CommandLogging.LogChangeProperty(from, targ, "EverySkill.Base", m_Value.ToString());
                }
                else
                {
                    from.SendMessage("That does not have skills!");
                }
            }
        }

        public class SkillTarget : Target
        {
            private bool m_Set;
            private SkillName m_Skill;
            private double m_Value;

            public SkillTarget(SkillName skill, double value)
                : base(-1, false, TargetFlags.None)
            {
                m_Set = true;
                m_Skill = skill;
                m_Value = value;
            }

            public SkillTarget(SkillName skill)
                : base(-1, false, TargetFlags.None)
            {
                m_Set = false;
                m_Skill = skill;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    Mobile targ = (Mobile)targeted;
                    Skill skill = targ.Skills[m_Skill];

                    if (skill == null)
                        return;

                    if (m_Set)
                    {
                        skill.Base = m_Value;
                        CommandLogging.LogChangeProperty(from, targ, string.Format("{0}.Base", m_Skill), m_Value.ToString());
                    }

                    from.SendMessage("{0} : {1} (Base: {2})", m_Skill, skill.Value, skill.Base);
                }
                else
                {
                    from.SendMessage("That does not have skills!");
                }
            }
        }
    }
}