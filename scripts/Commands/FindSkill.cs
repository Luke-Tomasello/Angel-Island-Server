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

/* Scripts/Commands/FindSkill.cs
 * ChangeLog
 *	8/29/10, adam
 *		Close the log :(
 *	11/25/05, erlein
 *		Converted to single line log format.
 *	10/20/05, Adam
 *		Reduced access to AccessLevel.Counselor
 *	03/28/05, erlein
 *		Altered LogType in Log() call to LogType.Mobile, specifying skill now
 *		as a parameter rather than building full log string and sending it to
 *		LogHelper as a string.
 *	03/25/05, erlein
 *		Integrated with LogHelper class.
 *	02/21/05, erlein
 *		Updated FindSkillMobs routine so returns ArrayList of matched mobiles
 *		instead of true/false (for new heartbeat link).
 *		Added result logging, output is appended to 'logs/findskill.log'
 *	02/19/05, erlein
 *		Initial creation - designed to retrieve locations of all mobs who have used that
 *		skill last in time specified as argument (or 2 min default)
*/
using Server.Diagnostics;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Commands
{
    public class FindSkill
    {

        public static void Initialize()
        {
            Server.CommandSystem.Register("FindSkill", AccessLevel.Counselor, new CommandEventHandler(FindSkill_OnCommand));
        }

        [Usage("FindSkill <skill> (<time since used>)")]
        [Description("Finds locations of all mobs who have used skill in 2 minutes (or within specfied number of minutes).")]
        private static void FindSkill_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;
            SkillName skill;

            // Init elapsed with 2nd of arguments passed to command
            int elapsed = arg.GetInt32(1);

            // Test the skill argument input to make sure it's valid

            try
            {
                // Try to set var holding skill arg to enum equivalent
                skill = (SkillName)Enum.Parse(typeof(SkillName), arg.GetString(0), true);
            }
            catch
            {
                // Skill not valid, return without performing mob search
                from.SendMessage("You have specified an invalid skill.");
                return;
            }

            ArrayList MobsMatched = FindSkillMobs(skill, elapsed);

            if (MobsMatched.Count > 0)
            {

                // Found some, so loop and display

                foreach (PlayerMobile pm in MobsMatched)
                {
                    if (!pm.Hidden || from.AccessLevel > pm.AccessLevel || pm == from)
                        from.SendMessage("{0}, x:{1}, y:{2}, z:{3}", pm.Name, pm.Location.X, pm.Location.Y, pm.Location.Z);
                }

            }
            else
            {

                // Found none, so inform.
                from.SendMessage("Nobody online has used that skill recently.");
            }

        }

        // Does search... returns ArrayList reference for mobiles that matched

        public static ArrayList FindSkillMobs(SkillName skill, int elapsed)
        {
            if (elapsed == 0) elapsed = 2;  // Default

            //ArrayList MobStates = NetState.Instances;
            List<NetState> MobStates = NetState.Instances;
            ArrayList MobMatches = new ArrayList(NetState.Instances.Count);

            // Loop through active connections' mobiles and check conditions

            for (int i = 0; i < MobStates.Count; ++i)
            {

                Mobile m = MobStates[i].Mobile;

                // If m defined & PlayerMobile, get involved (not explicit)

                if (m != null)
                {

                    if (m is PlayerMobile)
                    {

                        PlayerMobile pm = (PlayerMobile)m;

                        SkillName LastSkill = pm.LastSkillUsed;
                        DateTime LastTime = pm.LastSkillTime;

                        // Check time & skill, display if match

                        if (LastSkill == skill && DateTime.UtcNow <= (LastTime + TimeSpan.FromSeconds(elapsed * 60)))
                            MobMatches.Add(pm);
                    }

                }

            }

            if (MobMatches.Count > 0)
            {
                LogHelper Logger = new LogHelper("findskill.log", false, true);

                // Loop through matches and log before returning
                foreach (PlayerMobile pm in MobMatches)
                    Logger.Log(LogType.Mobile, pm, skill.ToString());

                Logger.Finish();
            }

            return MobMatches;
        }

    }

}