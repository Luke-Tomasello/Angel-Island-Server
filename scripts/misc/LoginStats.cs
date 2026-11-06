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

/* Scripts/Misc/LoginStats.cs
 * CHANGELOG
 *	3/3/11, Adam
 *		if we're on a test center server, send a detailed message to the player telling what flavor of test center they are on
 *	11/17/10, Adam
 *		Add back stats printing on login for UOSP if you are staff
 *	11/11/10, pix
 *      Removed stats printing on login for UOSP.
 *	7/11/10, adam
 *		replace local copy of memory system with the common implementation in Utility.Memory
 *	6/21/10, adam
 *		remove item and mobile counts from stats delivered to accesslevel.player
 *	6/15/10, Adam
 *		Turn on online counts on login and display number of adventurers and pvpers currently active.
 *	6/10/10, Adam
 *		Turn off online counts on login.
 *  09/14/05 Taran Kain
 *		Notify of TC functionality if enabled.
 */

using Server.Mobiles;
using Server.Network;
using Server.Regions;
using System;
using System.IO;

namespace Server.Misc
{
    public class LoginStats
    {
        public static void Initialize()
        {
            // Register our event handler
            EventSink.Login += new LoginEventHandler(EventSink_Login);
        }

        private static void EventSink_Login(LoginEventArgs args)
        {
            Mobile m = args.Mobile;

            // New players get the full WelcomeTimer (handled automatically in character creation)
            // Returning players get the fast login greeting
            if (m.Age > TimeSpan.FromMinutes(5) && m is PlayerMobile pm)
            {
                // Returning from Test Center performs a login-equivalent transition.
                // Suppress the next welcome message to avoid confusing the player.
                if (!pm.GetPlayerBool(PlayerBoolTable.SuppressNextWelcomeMessage))
                    WelcomeTimer.SendGreeting(m, WelcomeTimer.GreetingType.Returning);
                else
                {
                    // clear it now
                    pm.SetPlayerBool(PlayerBoolTable.SuppressNextWelcomeMessage, false);
                }
            }
        }
    }

    public static class Stats
    {
        #region Average Daily Users
        public static int MaxPlayerCount { get; private set; }
        public static int AverageDailyUsers { get; private set; }

        private static DateTime m_ADUDate;
        private static int m_ADUSampleTotal;
        private static int m_ADUSampleCount;

        private static readonly string m_FilePath = Path.Combine("Saves", "Stats.bin");

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(EventSink_WorldLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(EventSink_WorldSave);
        }

        private static void EventSink_WorldLoad()
        {
            Load();
        }

        private static void EventSink_WorldSave(WorldSaveEventArgs e)
        {
            UpdatePopulationStats();
            Save();
        }

        private static void UpdatePopulationStats()
        {
            int users = NetState.Instances.Count;

            if (users > MaxPlayerCount)
                MaxPlayerCount = users;

            DateTime today = DateTime.UtcNow.Date;

            if (m_ADUDate == DateTime.MinValue)
                m_ADUDate = today;

            if (m_ADUDate != today)
            {
                if (m_ADUSampleCount > 0)
                    AverageDailyUsers = m_ADUSampleTotal / m_ADUSampleCount;

                m_ADUDate = today;
                m_ADUSampleTotal = 0;
                m_ADUSampleCount = 0;
            }

            m_ADUSampleTotal += users;
            m_ADUSampleCount++;
        }

        private static void Save()
        {
            BinaryFileWriter writer = new BinaryFileWriter(m_FilePath, true);

            writer.Write(0); // version

            writer.Write(MaxPlayerCount);
            writer.Write(AverageDailyUsers);
            writer.Write(m_ADUDate);
            writer.Write(m_ADUSampleTotal);
            writer.Write(m_ADUSampleCount);

            writer.Close();
        }

        private static void Load()
        {
            if (!File.Exists(m_FilePath))
                return;

            BinaryFileReader reader = new BinaryFileReader(new BinaryReader(File.OpenRead(m_FilePath)));

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    MaxPlayerCount = reader.ReadInt();
                    AverageDailyUsers = reader.ReadInt();
                    m_ADUDate = reader.ReadDateTime();
                    m_ADUSampleTotal = reader.ReadInt();
                    m_ADUSampleCount = reader.ReadInt();
                    break;
            }

            reader.Close();
        }
        #endregion Average Daily Users

        private static Memory m_AggressiveActionMemory = new Memory();

        public static void Initialize()
        {
            EventSink.AggressiveAction += new AggressiveActionEventHandler(EventSink_AggressiveAction);
        }

        public static string Format(AccessLevel ax)
        {
            int userCount = NetState.Instances.Count;
            int itemCount = World.Items.Count;
            int mobileCount = World.Mobiles.Count;
            string sx;

            sx = string.Format("There {0} currently {1} user{2} online",
                userCount == 1 ? "is" : "are",
                userCount, userCount == 1 ? "" : "s");

            if (ax > AccessLevel.Player)
            {
                sx += string.Format(", with {0} item{1} and {2} mobile{3} in the world.",
                    itemCount, itemCount == 1 ? "" : "s",
                    mobileCount, mobileCount == 1 ? "" : "s");
            }
            else
                sx += ".";

            int pvpers = m_AggressiveActionMemory.Count;
            int adventurers = CountAdventurers();

            if (pvpers > 1)
                sx += string.Format(" {0} {1} to be involved in player conflict",
                    pvpers, pvpers == 1 ? "appears" : "appear");

            if (adventurers > 1)
            {
                if (pvpers > 1)
                    sx += ", ";
                sx += string.Format(" {0} {1} to be out adventuring",
                    adventurers, adventurers == 1 ? "appears" : "appear");
            }

            if (pvpers > 1 || adventurers > 1)
                sx += ".";

            // --- Server Wars Notice ---
            if (Server.Misc.Maintenance.InServerWars)
                sx += "\nServer Wars are currently active!";

            return sx;
        }

        private static int CountAdventurers()
        {
            int count = 0;
            foreach (NetState state in NetState.Instances)
            {
                if (state != null && state.Mobile != null)
                {   // sanity ok!
                    if (state.Mobile.Map == Map.Felucca)
                    {   // we're in felucca

                        // don't count players in our PvP list
                        if (m_AggressiveActionMemory.Recall(state.Mobile) == true)
                            continue;

                        if (state.Mobile.Region != null)
                        {   // looks to be a valid region
                            bool guarded = state.Mobile.Region as GuardedRegion != null && (state.Mobile.Region as GuardedRegion).IsGuarded == true;
                            bool house = state.Mobile.Region as HouseRegion != null;
                            if (!guarded && !house)
                            {   // not guarded and not house
                                count++;
                            }
                        }
                        else
                        {   // not sure if we ever have a null region
                            count++;
                        }
                    }
                }
            }

            return count;
        }


        private static void EventSink_AggressiveAction(AggressiveActionEventArgs e)
        {
            Mobile aggressor = e.Aggressor;
            Mobile aggressed = e.Aggressed;

            if (!aggressor.Player || !aggressed.Player)
                return;

            // remember for 10 minutes
            m_AggressiveActionMemory.Remember(aggressor, 60 * 10);
            m_AggressiveActionMemory.Remember(aggressed, 60 * 10);
        }
    }
}