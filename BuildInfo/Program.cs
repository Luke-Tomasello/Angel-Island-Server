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

/* BuildInfo\Program.cs
 * ChangeLog
 *  8/13/2024, Adam
 *      Add a notion of BuildInfoDir. This directory contains the "build.info" file
 *  7/28/2024, Adam
 *      Bump the major version to 7. This will be our new GitHub version
 *  12/26/21, Adam 
 *      Add Major and Minor version numbers
 *      Important Note: after rebuilding this (because of a version update) BuildInfo.exe needs to go into the 
 *          root folder of the project for both Debug and Release
 *      
 *  Version History:
 *      10/29/22, Adam
 *      Minor 1:
 *          RunUO 2.6 networking.
 *          Add Renaissance server support
 *          Convert all Angel Island 'servers' to 'rule sets' so as not to conflict with RunUO's
 *              notion of Expansion's 
 *      7/3/22, Adam 
 *      Major 6: (Minor 0)
 *          New map and client 6.0.1.10
 *          Upgrade Map.cs, Sector.cs, and Tile modules to RunUO 2.6
 *      Minor 2:
 *          Move to .NET 6
 *      Major 5:
 *          Move to 64bit code
 *          Move to .NET 5
 *      Minor 1:
 *          New Intelligent Dialog System
 *          replaced Dictionary<> with ConcurrentDictionary<> for World Items and Mobiles.
 */
using System;
using System.IO;
using System.Reflection;
namespace BuildInfo
{
    class Program
    {
        static int Main(string[] args)
        {
            // major version number
            const int major = 7;    // if you change this, update Version History above
            // minor version number
            const int minor = 1;    // if you change this, update Version History above

            Console.WriteLine("BuildInfo: Generating Build Information...");
            string buildInfoFile = Path.Combine(BuildInfoDir, "build.info");
            bool okay = false;
            int buildNo = GetBuild();
            try
            {
                // this needs to go into the root folder of the project
                StreamWriter sw = new StreamWriter(buildInfoFile);
                //human readable build number
                sw.WriteLine((buildNo + 1).ToString());
                //human readable revision number
                sw.WriteLine(BuildRevision().ToString());
                //human readable major version number
                sw.WriteLine(major.ToString());
                //human readable minor version number
                sw.WriteLine(minor.ToString());
                sw.Close();
                okay = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }

            if (okay)
                Console.WriteLine("BuildInfo: Done.");
            else
                Console.WriteLine("BuildInfo: failed.");

            return 0;
        }
        public static string BuildInfoDir
        {
            get
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AI.BuildInfoDir")))
                    return Environment.GetEnvironmentVariable("AI.BuildInfoDir");
                return "./";
            }
        }
        public static int GetBuild()
        {
            string buildInfoFile = Path.Combine(BuildInfoDir, "build.info");
            try
            {
                // open our version info file
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the build number
                string line = sr.ReadLine();
                sr.Close();
                int result = 0;
                int.TryParse(line, out result);
                return result;

            }
            catch
            {
                Console.WriteLine("Creating a new Build.info file");
            }
            return 0;
        }

        public static int BuildRevision()
        {   // revisions are the time representation noted below
            var now = DateTime.UtcNow;
            return now.Year + now.Month + now.Day + now.Hour + now.Minute + now.Second;
        }
    }
}