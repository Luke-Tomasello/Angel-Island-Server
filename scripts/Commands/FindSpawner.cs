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

/* Scripts/Commands/FindSpawner.cs
 * ChangeLog
 *  6/27/2023, Adam
 *      Allow the specification of a specific region to search. I.e.,
 *      [Usage("FindSpawner <mobile|item type name> [(<tile range>) | (<region>)]")]
 *      [Description("Finds locations of all spawners spawning specified mobile|item type in a tile range or region.")]
 *	3/3/11, adam
 *		Fix a logic bug that was preventing Owners from accessing the results. I.e., access >= Administrators
 *  11/21/10, Adam
 *      1. Make available to GMs for world building/cleanup if the server is launched with the -build command
 *          (currently GM can't use thie system to find possibly secret items)
 *	03/28/05, erlein
 *		Altered so utilises generic call to Log(LogType.Item, etc.) in LogHelper class.
 *	03/25/05, erlein
 *		Integrated with LogHelper class.
 *		Normalized output.
 *  02/27/05, erlein
 *		Altered so Admin priviledges required for [findspawner <item-type>.
 *		Added ChestItemSpawners to matching process.
 *	02/25/05, erlein
 *		Altered output format to fit in single journal line and
 *		reflect running status of the spawner in question.
 *	02/24/05, erlein
 *		Added Z co-ordinate to spawner location display and changed
 *		distance approximation to display in tiles as opposed to differential
 *		co-ordinates.
 *	02/24/05, erlein
 *		Initial creation - designed to retrieve list of spawners
 *		spawning mobile type passed as parameter.
*/
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace Server.Commands
{
    public class FindSpawner
    {

        // Spawner search match holder (sortable)

        public struct SpawnerMatch : IComparable
        {
            public string Status;
            public string Matched;
            public int Distance;
            public bool Item;
            public object Sp;

            public Int32 CompareTo(Object obj)
            {
                SpawnerMatch tmpObj = (SpawnerMatch)obj;
                return (-this.Distance.CompareTo(tmpObj.Distance));
            }
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("FindSpawner", AccessLevel.GameMaster, new CommandEventHandler(FindSpawner_OnCommand));
            Server.CommandSystem.Register("FindItemSpawners", AccessLevel.GameMaster, new CommandEventHandler(FindItemSpawners_OnCommand));
            Server.CommandSystem.Register("FindTemplateSpawners", AccessLevel.GameMaster, new CommandEventHandler(FindTemplateSpawners_OnCommand));
            Server.CommandSystem.Register("FindBrokenTemplateSpawners", AccessLevel.GameMaster, new CommandEventHandler(FindBrokenTemplateSpawners_OnCommand));
        }

        [Usage("FindTemplateSpawners")]
        [Description("Finds locations of all 'Template' spawners.")]
        private static void FindTemplateSpawners_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;
            PlayerMobile pm = arg.Mobile as PlayerMobile;

            // Perform search and retrieve list of spawner matches
            LogHelper Logger = new LogHelper("FindTemplateSpawners.log", from, true);
            // reset jump table
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Spawner sp in SpawnerCache.Spawners)
            {
                if (sp == null) continue;
                if (sp.TemplateEnabled != true) continue;
                pm.JumpList.Add(sp);
                string message = string.Format("location: {0}, Map: {1}, Serial: {2}", sp.Location, sp.Map, sp.Serial);
                Logger.Log(LogType.Text, message);
            }

            Logger.Finish();
        }

        [Usage("FindBrokenTemplateSpawners")]
        [Description("Finds locations of all broken 'Template' spawners.")]
        private static void FindBrokenTemplateSpawners_OnCommand(CommandEventArgs arg)
        {   // Find all the broken template spawners and log them
            Mobile from = arg.Mobile;
            PlayerMobile pm = arg.Mobile as PlayerMobile;

            // Perform search and retrieve list of spawner matches
            // reset jump table
            pm.JumpIndex = 0;
            pm.JumpList = new ArrayList();
            foreach (Spawner sp in SpawnerCache.Spawners)
            {
                if (sp == null) continue;
                if (sp.TemplateEnabled != true) continue;
                bool broken = false;
                foreach (string thing in sp.ObjectNames)
                {
                    // Check if item type
                    Type TestType = SpawnerType.GetType(thing);
                    if (TestType.Namespace == "Server.Items")
                    {
                        if (sp.TemplateItem == null || sp.TemplateMobile != null)
                            broken = true;
                    }
                    else if (TestType.Namespace == "Server.Mobiles")
                    {
                        if (sp.TemplateMobile == null || sp.TemplateItem != null)
                            broken = true;
                    }
                    else if (TestType == null)
                    {   // if it looks like a serial it's probably a loot pack
                        if (sp.TemplateItem == null || sp.TemplateMobile != null)
                            broken = true;
                    }
                }

                if (broken == true)
                {
                    LogHelper Logger = new LogHelper("FindBrokenTemplateSpawners.log", from, true);
                    pm.JumpList.Add(sp);
                    string message = string.Format("location: {0}, Map: {1}, Serial: {2}", sp.Location, sp.Map, sp.Serial);
                    Logger.Log(LogType.Text, message);
                    Logger.Finish();
                }
            }
        }

        public static int LogBrokenTemplateSpawners()
        {   // Find all the broken template spawners and log them

            int count = 0;
            foreach (Spawner sp in SpawnerCache.Spawners)
            {
                if (sp == null) continue;
                if (sp.TemplateEnabled != true) continue;
                bool broken = false;
                foreach (string thing in sp.ObjectNames)
                {
                    // Check if item type
                    Type TestType = SpawnerType.GetType(thing);
                    if (TestType.Namespace == "Server.Items")
                    {
                        if (sp.TemplateItem == null || sp.TemplateMobile != null)
                        { broken = true; count++; }
                    }
                    else if (TestType.Namespace == "Server.Mobiles")
                    {
                        if (sp.TemplateMobile == null || sp.TemplateItem != null)
                        { broken = true; count++; }
                    }
                    else if (TestType == null)
                    {   // if it looks like a serial it's probably a loot pack
                        if (sp.TemplateItem == null || sp.TemplateMobile != null)
                        { broken = true; count++; }
                    }
                }

                if (broken == true)
                {
                    LogHelper Logger = new LogHelper("FindBrokenTemplateSpawners.log", false);
                    string message = string.Format("location: {0}, Map: {1}, Serial: {2}", sp.Location, sp.Map, sp.Serial);
                    Logger.Log(LogType.Text, message);
                    Logger.Finish();
                }
            }

            return count;
        }

        [Usage("FindSpawner <mobile|item type name> [(<tile range>) | (<region>)]")]
        [Description("Finds locations of all spawners spawning specified mobile|item type in a tile range or region.")]
        private static void FindSpawner_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            int X = from.Location.X;
            int Y = from.Location.Y;

            string SearchText = e.GetString(0);
            int TileRange = e.GetInt32(1);
            Region region = null;
            if (e.Length > 1)
                region = Region.FindByName(e.GetString(1), e.Mobile.Map);

            // Validate parameters

            if (SearchText == "")
            {
                from.SendMessage("To use : [findspawner <type> [(<tile range>) | (<region>)]");
                return;
            }

            Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");

            if (InvalidPatt.IsMatch(SearchText))
            {
                from.SendMessage("Invalid characters used in type, range, or region specification.");
                return;
            }


            // Perform search and retrieve list of spawner matches

            LogHelper Logger = new LogHelper("findspawner.log", from, true);

            ArrayList SpawnerList = FindSpawners(SearchText, X, Y, TileRange);

            if (SpawnerList.Count > 0)
            {
                PlayerMobile pm = e.Mobile as PlayerMobile;

                // Have results so sort, loop and display message for each match
                SpawnerList.Sort();

                // reset jump table
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();

                foreach (SpawnerMatch ms in SpawnerList)
                {
                    // this line prevents GMs from locating secret items unless the world is in build mode
                    if (ms.Item == false || from.AccessLevel >= Server.AccessLevel.Administrator || Core.Building)
                    {
                        if (region != null)
                        {
                            if (region.Contains((ms.Sp as Spawner).Location))
                            {
                                pm.JumpList.Add(ms.Sp);
                                Logger.Log(LogType.Item, ms.Sp, string.Format("{0}:{1}:{2}", ms.Matched, ms.Distance, ms.Status));
                            }
                        }
                        else
                        {
                            pm.JumpList.Add(ms.Sp);
                            Logger.Log(LogType.Item, ms.Sp, string.Format("{0}:{1}:{2}", ms.Matched, ms.Distance, ms.Status));
                        }
                    }
                }

            }

            Logger.Finish();

        }

        public static ArrayList FindLocalSpawners(Point2D source, int distance)
        {

            int X = source.X;
            int Y = source.Y;

            string SearchText = ".*";
            int TileRange = distance;

            // Perform search and retrieve list of spawner matches
            ArrayList SpawnerList = FindSpawners(SearchText, X, Y, TileRange);

            if (SpawnerList.Count > 0)
                // Have results so sort, loop and display message for each match
                SpawnerList.Sort();

            return SpawnerList;

        }
        // Searches for spawners and returns struct list containing
        // relevant detail
        public static ArrayList FindSpawners(string searchtext, int x, int y, int tilerange)
        {
            ArrayList SpawnerList = new ArrayList();
            Regex SearchPattern = new Regex(searchtext.ToLower());

            // Loop through mobiles and check for Spawners

            foreach (Item item in World.Items.Values)
            {

                if (item is Spawner)
                {

                    Spawner sp = (Spawner)item;

                    // Now check range / ignore range accordingly

                    int spX = sp.Location.X - x;
                    int spY = sp.Location.Y - y;

                    if ((tilerange == 0) || (
                        (sqr(sp.Location.X - x) <= sqr(tilerange) &&
                         sqr(sp.Location.Y - y) <= sqr(tilerange))
                                        ))
                    {

                        // Loop through spawners' creature list and match
                        // against search text

                        foreach (string CreatureName in sp.ObjectNames)
                        {

                            if (SearchPattern.IsMatch(CreatureName.ToLower()))
                            {

                                SpawnerMatch ms = new SpawnerMatch();

                                ms.Item = false;
                                ms.Sp = sp;

                                // Check if item type

                                Type TestType = SpawnerType.GetType(CreatureName);
                                if (TestType == null)
                                    continue;    // if it looks like a serial it's probably a loot pack

                                string strTest = TestType.ToString();

                                Regex InvalidPatt = new Regex("^Server.Item");

                                if (InvalidPatt.IsMatch(strTest))
                                {
                                    ms.Item = true;
                                }
                                // We have a match! Create new match struct
                                // and add to return reference list

                                if (sp.Running == true)
                                    ms.Status = "on";
                                else
                                    ms.Status = "off";

                                ms.Matched = CreatureName;
                                ms.Distance = (int)Math.Sqrt(sqr(spX) + sqr(spY));

                                SpawnerList.Add(ms);
                                break;
                            }

                        }

                    }

                }

                if (item is Server.Items.ChestItemSpawner)
                {

                    ChestItemSpawner sp = (ChestItemSpawner)item;

                    // Now check range / ignore range accordingly

                    int spX = sp.Location.X - x;
                    int spY = sp.Location.Y - y;

                    if ((tilerange == 0) || (
                        (sqr(sp.Location.X - x) <= sqr(tilerange) &&
                         sqr(sp.Location.Y - y) <= sqr(tilerange))
                                        ))
                    {

                        // Loop through spawners' creature list and match
                        // against search text

                        foreach (string ItemName in sp.ItemsName)
                        {

                            if (SearchPattern.IsMatch(ItemName.ToLower()))
                            {

                                SpawnerMatch ms = new SpawnerMatch();

                                ms.Item = false;
                                ms.Sp = sp;

                                // Check if item type

                                Type TestType = SpawnerType.GetType(ItemName);
                                if (TestType == null) // if it looks like a serial it's probably a loot pack
                                    continue;

                                string strTest = TestType.ToString();

                                Regex InvalidPatt = new Regex("^Server.Item");

                                if (InvalidPatt.IsMatch(strTest))
                                {
                                    ms.Item = true;
                                }
                                // We have a match! Create new match struct
                                // and add to return reference list

                                if (sp.Running == true)
                                    ms.Status = "on";
                                else
                                    ms.Status = "off";

                                ms.Matched = ItemName;
                                ms.Distance = (int)Math.Sqrt(sqr(spX) + sqr(spY));

                                SpawnerList.Add(ms);

                            }

                        }

                    }

                }

            }

            return (SpawnerList);
        }

        [Usage("FindItemSpawners (<tile range>)")]
        [Description("Finds locations of all item spawners.")]
        private static void FindItemSpawners_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;

            int X = from.Location.X;
            int Y = from.Location.Y;

            //string SearchText = arg.GetString(0);
            int TileRange = arg.GetInt32(0);

            // Validate parameters

            /*if (SearchText == "")
			{
				from.SendMessage("To use : [findspawner <type> (<range>)");
				return;
			}*/

            /*Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");

			if (InvalidPatt.IsMatch(SearchText))
			{
				from.SendMessage("Invalid characters used in type or range specification.");
				return;
			}*/


            // Perform search and retrieve list of spawner matches

            LogHelper Logger = new LogHelper("finditemspawners.log", from, true);

            ArrayList SpawnerList = FindItemSpawners(""/*remove*/, X, Y, TileRange);

            if (SpawnerList.Count > 0)
            {
                PlayerMobile pm = arg.Mobile as PlayerMobile;

                // Have results so sort, loop and display message for each match
                SpawnerList.Sort();

                // reset jump table
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();

                foreach (SpawnerMatch ms in SpawnerList)
                {
                    // this line prevents GMs from locating secret items unless the world is in build mode
                    if (ms.Item == false || from.AccessLevel >= Server.AccessLevel.Administrator || Core.Building)
                    {
                        pm.JumpList.Add(ms.Sp);
                        Logger.Log(LogType.Item, ms.Sp, string.Format("{0}:{1}:{2}", ms.Matched, ms.Distance, ms.Status));
                    }
                }

            }

            Logger.Finish();

        }

        // Searches for spawners and returns struct list containing
        // relevant detail

        public static ArrayList FindItemSpawners(string searchtext, int x, int y, int tilerange)
        {
            ArrayList SpawnerList = new ArrayList();
            //Regex SearchPattern = new Regex(searchtext.ToLower());

            // Loop through mobiles and check for Spawners

            foreach (Item item in World.Items.Values)
            {

                if (item is Spawner)
                {

                    Spawner sp = (Spawner)item;

                    // Now check range / ignore range accordingly

                    int spX = sp.Location.X - x;
                    int spY = sp.Location.Y - y;

                    if ((tilerange == 0) || (
                        (sqr(sp.Location.X - x) <= sqr(tilerange) &&
                         sqr(sp.Location.Y - y) <= sqr(tilerange))
                                        ))
                    {

                        // Loop through spawners' creature list and match
                        // against search text

                        foreach (string CreatureName in sp.ObjectNames)
                        {

                            if (true/*SearchPattern.IsMatch(CreatureName.ToLower())*/)
                            {

                                SpawnerMatch ms = new SpawnerMatch();

                                ms.Item = false;
                                ms.Sp = sp;

                                // Check if item type

                                Type TestType = SpawnerType.GetType(CreatureName);
                                if (TestType == null)
                                    continue;
                                string strTest = TestType.ToString();

                                Regex InvalidPatt = new Regex("^Server.Item");

                                if (InvalidPatt.IsMatch(strTest))
                                {
                                    ms.Item = true;
                                }
                                else
                                    continue;

                                // We have a match! Create new match struct
                                // and add to return reference list

                                if (sp.Running == true)
                                    ms.Status = "on";
                                else
                                    ms.Status = "off";

                                ms.Matched = CreatureName;
                                ms.Distance = (int)Math.Sqrt(sqr(spX) + sqr(spY));

                                SpawnerList.Add(ms);

                            }

                        }

                    }

                }

                if (item is Server.Items.ChestItemSpawner)
                {

                    ChestItemSpawner sp = (ChestItemSpawner)item;

                    // Now check range / ignore range accordingly

                    int spX = sp.Location.X - x;
                    int spY = sp.Location.Y - y;

                    if ((tilerange == 0) || (
                        (sqr(sp.Location.X - x) <= sqr(tilerange) &&
                         sqr(sp.Location.Y - y) <= sqr(tilerange))
                                        ))
                    {

                        // Loop through spawners' creature list and match
                        // against search text

                        foreach (string ItemName in sp.ItemsName)
                        {

                            if (true /*SearchPattern.IsMatch(ItemName.ToLower())*/)
                            {

                                SpawnerMatch ms = new SpawnerMatch();

                                ms.Item = false;
                                ms.Sp = sp;

                                // Check if item type

                                Type TestType = SpawnerType.GetType(ItemName);
                                if (TestType == null)
                                    continue;
                                string strTest = TestType.ToString();

                                Regex InvalidPatt = new Regex("^Server.Item");

                                if (InvalidPatt.IsMatch(strTest))
                                {
                                    ms.Item = true;
                                }
                                // We have a match! Create new match struct
                                // and add to return reference list

                                if (sp.Running == true)
                                    ms.Status = "on";
                                else
                                    ms.Status = "off";

                                ms.Matched = ItemName;
                                ms.Distance = (int)Math.Sqrt(sqr(spX) + sqr(spY));

                                SpawnerList.Add(ms);

                            }

                        }

                    }

                }

            }

            return (SpawnerList);
        }

        private static int sqr(int num)
        {
            return ((num * num));
        }

    }

}