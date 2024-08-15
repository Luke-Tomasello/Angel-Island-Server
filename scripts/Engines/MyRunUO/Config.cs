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

/* Scripts/Engines/MyRunUO/Config.cs
 * Changelog:
 *  04/28/05 TK
 *		Added DisplaySQL option to help in debugging
 *		Set up configuration parameters (re-configure for production server!)
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using System;
using System.Text;
using System.Threading;

namespace Server.Engines.MyRunUO
{
    public class Config
    {
        // Is MyRunUO enabled? - set in Configure()
        public static bool Enabled = false;

        // Details required for database connection string - setup in Configure()
        public static string DatabaseDriver = null;
        public static string DatabaseServer = null;
        public static string DatabaseName = null;
        public static string DatabaseUserID = null;
        public static string DatabasePassword = null;

        public static void Configure()
        {
            // see if we are runninbg on the production server with MyRunUO enabled
            string me = Environment.GetEnvironmentVariable("MYRUNUO_ENABLED");
            if (me == null)
            {
                Console.WriteLine("MyRunUO: No MYRUNUO_ENABLED environment variable found.");
                Console.WriteLine("MyRunUO: Disabled.");
                Enabled = false;
            }
            else if (me.Length == 0 || me.ToLower() != "true")
            {
                Console.WriteLine("MyRunUO: Disabled.");
                Enabled = false;
            }
            else
            {
                Enabled = true;

                if ((Core.RuleSets.AngelIslandRules()) && !Core.UOTC_CFG)
                {
                    Enabled = false;
                    Console.WriteLine("MyRunUO: Disabled.");
                }
                else if (Core.RuleSets.StandardShardRules() && !Core.UOTC_CFG)
                {
                    Enabled = false;
                    Console.WriteLine("MyRunUO: Disabled.");
                }
                else if (Core.UOTC_CFG)
                {
                    Console.WriteLine("MyRunUO: Enabled.");
                    DatabaseDriver = "{MySQL ODBC 3.51 Driver}";
                    DatabaseServer = "localhost"; // "game-master.net";		//"localhost";
                    DatabaseName = "gmn_myuotc";            // "MyRunUO";
                    DatabaseUserID = "myuotc";
                    DatabasePassword = "test99";
                }
            }
        }

        // Should we display all SQL commands? Useful for debugging.
        public static bool DisplaySQL = false;

        // Should the database use transactions? This is recommended
        public static bool UseTransactions = true;

        // Use optimized table loading techniques? (LOAD DATA INFILE)
        public static bool LoadDataInFile = false;

        // This must be enabled if the database server is on a remote machine.
        public static bool DatabaseNonLocal = (DatabaseServer != "localhost");

        // Text encoding used
        public static Encoding EncodingIO = Encoding.ASCII;

        // Database communication is done in a seperate thread. This value is the 'priority' of that thread, or, how much CPU it will try to use
        public static ThreadPriority DatabaseThreadPriority = ThreadPriority.BelowNormal;

        // Any character with an AccessLevel equal to or higher than this will not be displayed
        public static AccessLevel HiddenAccessLevel = AccessLevel.Counselor;

        // Export character database every 30 minutes
        public static TimeSpan CharacterUpdateInterval = TimeSpan.FromMinutes(30.0);

        // Export online list database every 5 minutes
        public static TimeSpan StatusUpdateInterval = TimeSpan.FromMinutes(5.0);

        public static string CompileConnectionString()
        {
            //string connectionString = string.Format("DRIVER={0};SERVER={1};DATABASE={2};UID={3};PASSWORD={4};",
            string connectionString = string.Format("DRIVER={0};SERVER={1};DATABASE={2};USER={3};PASSWORD={4};",
                DatabaseDriver, DatabaseServer, DatabaseName, DatabaseUserID, DatabasePassword);

            return connectionString;
        }
    }










}