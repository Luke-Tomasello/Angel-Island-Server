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

/* Scripts/Engines/Spawner/SpawnerGump.cs
 * ChangeLog
 *  9/14/22, Yoar
 *      Now reopening the spawner gump on button press
 *  8/30/22, Adam
 *      Replace ObjectNames ==> ObjectNamesRaw
 *  8/29/22, Adam
 *      Add support for colon delimited creatures. For instance:
 *          cat:dog
 *          rat
 *      In the case one of cat and dog would have a 50% chance of spawning (25% each)
 *      and 'dog' would have a 50% chance.
 *  12/12/21. Adam
 *      Add updating of QuickTables when a spawner is changed.
 *	9/15/06, Adam
 *		Call new function to create a template object only if the Type of the existing template
 *			object has changed. The prevents complex templates from being lost when someone
 *			simply hits 'OK' on the Spawner gump
 *	6/30/06, Adam
 *		- move template creation/management into Spawner class.
 *		- make sure template is only created on the first object specified.
 *	02/27/05, erlein   
 *		Added change logging for all alterations to spawn list.
 *		Now logs to /logs/spawnerchange.log.
 */
using Server.Commands;
using Server.Diagnostics;
using Server.Engines;
using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Mobiles
{

    public class SpawnerGump : Gump
    {
        private static Spawner m_Spawner;
        private static Mobile m_Person;

        private SpawnerMemory m_SpawnBefore;
        private SpawnerMemory m_SpawnAfter;

        public static Mobile LinkedPerson
        {
            get
            {
                return m_Person;
            }
            set
            {
                if (value is PlayerMobile)
                    m_Person = value;
            }
        }


        public static Spawner LinkedSpawner
        {
            get { return m_Spawner; }
        }

        public SpawnerGump(Spawner spawner)
            : base(50, 50)
        {
            m_Spawner = spawner;

            m_SpawnBefore = new SpawnerMemory();
            m_SpawnAfter = new SpawnerMemory();

            AddPage(0);

            AddBackground(0, 0, 260, 371, 5054);

            AddLabel(52, 1, 0, string.Format("Creature List ({0})", spawner.Serial));

            AddButton(5, 347, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
            AddLabel(38, 347, 0x384, "Cancel");

            AddButton(5, 325, 0xFB7, 0xFB9, 1, GumpButtonType.Reply, 0);
            AddLabel(38, 325, 0x384, "Okay");

            AddButton(110, 325, 0xFB4, 0xFB6, 2, GumpButtonType.Reply, 0);
            AddLabel(143, 325, 0x384, "Bring to Home");

            AddButton(110, 347, 0xFA8, 0xFAA, 3, GumpButtonType.Reply, 0);
            AddLabel(143, 347, 0x384, "Total Respawn");

            for (int i = 0; i < 13; i++)
            {
                AddButton(5, (22 * i) + 20, 0xFA5, 0xFA7, 4 + (i * 2), GumpButtonType.Reply, 0);
                AddButton(38, (22 * i) + 20, 0xFA2, 0xFA4, 5 + (i * 2), GumpButtonType.Reply, 0);

                AddImageTiled(71, (22 * i) + 20, 159, 23, 0xA40);
                AddImageTiled(72, (22 * i) + 21, 157, 21, 0xBBC);

                string str = "";

                if (i < spawner.ObjectNamesRaw.Count)
                {
                    str = (string)spawner.ObjectNamesRaw[i];
                    int count = m_Spawner.CountObjects(str);

                    if (str != "")
                        m_SpawnBefore.Add(str);

                    AddLabel(232, (22 * i) + 20, 0, count.ToString());
                }

                AddTextEntry(75, (22 * i) + 21, 154, 21, 0, i, str);
            }

        }


        public ArrayList CreateArray(RelayInfo info, Mobile from)
        {
            ArrayList creaturesName = new ArrayList();

            for (int i = 0; i < 13; i++)
            {
                TextRelay te = info.GetTextEntry(i);

                if (te != null)
                {
                    string str = te.Text;

                    if (str.Length > 0)
                    {
                        str = str.Trim();
                        //adam 8/29/22, we now allow ':' delimited lists of creatures
                        string[] tokens = str.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        List<string> valid = new List<string>();
                        for (int ix = 0; ix < tokens.Length; ix++)
                        {
                            string token = tokens[ix];

                            Type type = SpawnerType.GetType(token);

                            if (type == null)
                            {
                                from.SendMessage("{0} is not a valid type name.", token);
                                tokens[ix] = string.Empty;
                            }
                            else
                                //valid.Add(token);
                                valid.Add(type.Name);   // 7/3/2024, Adam: beautifier
                        }
                        // rebuild the entry
                        str = string.Empty;
                        foreach (string obj in valid)
                            str += obj + ':';
                        str = str.TrimEnd(':');
                        // add it
                        creaturesName.Add(str);
                        m_SpawnAfter.Add(str);

                    }
                }
            }

            return creaturesName;
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_Spawner.Deleted)
                return;

            LinkedPerson = state.Mobile; // erl: link person responding to spawner

            switch (info.ButtonID)
            {
                case 0: // Closed
                    {
                        break;
                    }

                case 1: // Okay
                    {
                        m_Spawner.ObjectNamesRaw = CreateArray(info, state.Mobile);

                        // make sure we're not spawning region specific creature or items outside of their designated area
                        string bad_object = string.Empty;
                        if (!CheckRegion(m_Spawner, m_Spawner.ObjectNamesRaw, ref bad_object))
                        {
                            m_Spawner.ObjectNamesRaw = new ArrayList();
                            m_Spawner.SendSystemMessage(string.Format("{0} is a restricted type and may not be spawned here", bad_object));
                        }

                        // erl: compare the two lists of spawner creatures and
                        // log any changes
                        m_SpawnBefore.Compare(m_SpawnAfter);

                        // When the user presses OK, make sure we have a template created if appropriate
                        if (!m_Spawner.CheckTemplate())
                        {
                            m_Spawner.ObjectNamesRaw = new ArrayList();
                            m_Spawner.SendSystemMessage("Incompatible types");
                            m_Spawner.SendSystemMessage("You may not mix template types, for example Items and Mobiles");
                        }

                        state.Mobile.SendGump(new SpawnerGump(m_Spawner));
                        break;
                    }
                case 2: // Bring everything home
                    {
                        m_Spawner.BringToHome();

                        state.Mobile.SendGump(new SpawnerGump(m_Spawner));
                        break;
                    }
                case 3: // Complete respawn
                    {
                        m_Spawner.Respawn(force: true);
                        #region Logging
                        LogHelper logger = new LogHelper("Spawning_Spawner.Log", overwrite: false, sline: true);
                        string output = string.Format(
                            "{0} {1} {2}. Running={3}",
                            CommandLogging.Format(state.Mobile),
                            "Totally respawned",
                            CommandLogging.Format(m_Spawner),
                            m_Spawner.Running.ToString()
                            );

                        logger.Log(output);
                        logger.Finish();
                        #endregion Logging
                        state.Mobile.SendGump(new SpawnerGump(m_Spawner));
                        break;
                    }
                default:
                    {
                        int buttonID = info.ButtonID - 4;
                        int index = buttonID / 2;
                        int type = buttonID % 2;

                        TextRelay entry = info.GetTextEntry(index);

                        if (entry != null && entry.Text.Length > 0)
                        {
                            if (type == 0) // Spawn creature
                                m_Spawner.Spawn(entry.Text);
                            else // Remove creatures
                                m_Spawner.RemoveObjects(entry.Text);

                            #region Logging
                            LogHelper logger = new LogHelper("Spawning_Spawner.Log", overwrite: false, sline: true);
                            string output = string.Format(
                                "{0} {1} a {2} from {3}. Running={4}",
                                CommandLogging.Format(state.Mobile),
                                (type == 0) ? "Spawned" : "Removed",
                                entry.Text,
                                CommandLogging.Format(m_Spawner),
                                m_Spawner.Running.ToString()
                                );

                            logger.Log(output);
                            logger.Finish();
                            #endregion Logging

                            m_Spawner.ObjectNamesRaw = CreateArray(info, state.Mobile);
                        }

                        // erl: compare the two lists of spawner creatures and
                        // log any changes

                        m_SpawnBefore.Compare(m_SpawnAfter);

                        state.Mobile.SendGump(new SpawnerGump(m_Spawner));
                        break;
                    }
            }
        }

        private static List<string> RegionSpecificCreatures = new() { "AICellGuard", "AIGuardCaptain", "AIHealer", "AIPostGuard", "AIWarden", "ParoleOfficer",
        "AIDepotSpawner", "AIStinger", "LightHousePass", "RareTreasureChest", "VisitorsTicket"};
        public static bool CheckRegion(Spawner spawner, ArrayList list, ref string bad_object)
        {
            if (list != null)
            {
                foreach (object o in list)
                    if (o is string name)
                    {
                        if (RegionSpecificCreatures.Contains(name, StringComparer.OrdinalIgnoreCase))
                            if (Region.Find(spawner.Location, spawner.Map) != null && Region.Find(spawner.Location, spawner.Map).IsAngelIslandRules)
                                ;// good
                            else
                            {
                                bad_object = name;
                                return false;
                            }
                    }
            }
            return true;
        }
        // erl: SpawnerMemory class to hold, compare and log creature lists and changes
        //
        // 02/24/05

        private class SpawnerMemory
        {
            // Holds creature info.

            public string[] m_Names;
            public int[] m_Counts;
            public bool[] m_IsChecked;

            private Spawner m_Spawner;

            // Add to or create entry in memory

            public void Add(string name)
            {
                int cpos;

                for (cpos = 0; cpos < 13; cpos++)
                {

                    if (m_Names[cpos] == null)
                        break;

                    if (m_Names[cpos] == name)
                    {
                        m_Counts[cpos]++;
                        return;
                    }
                }

                if (cpos == 13)     // Should never happen
                    return;

                m_Names[cpos] = name;
                m_Counts[cpos] = 1;
            }

            // Return any instances of name type passed stored

            public int Retrieve(string name)
            {

                for (int cpos = 0; cpos < 13; cpos++)
                {

                    if (m_Names[cpos] == null)
                        return 0;

                    if (m_Names[cpos] == name)
                    {
                        m_IsChecked[cpos] = true;
                        return m_Counts[cpos];
                    }
                }

                return (0);
            }

            // Instance the SpawnerMemory, generating start creature
            // list

            public SpawnerMemory()
            {
                m_Spawner = LinkedSpawner;

                m_Names = new string[13];
                m_Counts = new int[13];
                m_IsChecked = new bool[13];
            }

            // Compare SpawnerMemory passed with this instance
            // and logs any changes to file

            public void Compare(SpawnerMemory sm)
            {
                ArrayList DiffList = new ArrayList();

                for (int i = 0; i < 13; i++)
                {
                    if (sm.Retrieve(m_Names[i]) != Retrieve(m_Names[i]))
                        DiffList.Add(m_Names[i] + " changed, " + Retrieve(m_Names[i]) + " to " + sm.Retrieve(m_Names[i]));
                }

                for (int i = 0; i < 13; i++)
                {
                    if (!sm.m_IsChecked[i] && sm.m_Names[i] != null)
                        DiffList.Add(sm.m_Names[i] + " changed, 0 to " + sm.m_Counts[i]);
                }

                if (DiffList.Count > 0)
                {

                    // There are differences, so log them!

                    StreamWriter LogFile = new StreamWriter("logs/spawnerchange.log", true);

                    foreach (string difference in DiffList)
                    {
                        // Log entries here
                        LogFile.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", DateTime.UtcNow, LinkedPerson.Account, m_Spawner.Location.X, m_Spawner.Location.Y, m_Spawner.Location.Z, difference);
                    }

                    LogFile.Close();

                    // inform our spawner cache something has changed
                    CacheFactory.UpdateQuickTables(m_Spawner, CacheFactory.QuickTableUpdate.Changed);
                }

                return;
            }

        }

    }

}