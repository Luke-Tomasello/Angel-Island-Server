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

/* Scripts\Misc\DataPath.cs
 * CHANGELOG:
 *	8/21/22, Adam
 *	Check both 
 *	    SOFTWARE\Angel Island
 *	    SOFTWARE\Origin Worlds Online
 *	for the UO folder. Prefer Angel Island
 */

using System;
using System.IO;

namespace Server.Misc
{
    public class DataPath
    {
        /* If you have not installed Ultima Online,
		 * or wish the server to use a seperate set of datafiles,
		 * change the 'CustomPath' value, example:
		 * 
		 * private const string CustomPath = @"C:\Program Files\Ultima Online";
		 */
        private static string CustomPath = null;

        /* The following is a list of files which a required for proper execution:
		 * 
		 * Multi.idx
		 * Multi.mul
		 * VerData.mul
		 * TileData.mul
		 * Map*.mul
		 * StaIdx*.mul
		 * Statics*.mul
		 * MapDif*.mul
		 * MapDifL*.mul
		 * StaDif*.mul
		 * StaDifL*.mul
		 * StaDifI*.mul
		 */

        public static void Configure()
        {
            string pathReg = GetUOPath("Ultima Online");
            string pathTD = GetUOPath("Ultima Online Third Dawn");

            if (CustomPath != null)
                Core.DataDirectories.Add(CustomPath);

            if (pathReg != null)
                Core.DataDirectories.Add(pathReg);

            if (pathTD != null)
                Core.DataDirectories.Add(pathTD);

            if (Core.DataDirectories.Count == 0)
            {
                Console.WriteLine("Enter the Ultima Online directory:");
                Console.Write("> ");

                Core.DataDirectories.Add(Console.ReadLine());
            }
        }

        public static string GetUOPath(string subName)
        {
            try
            {
                if (Core.Is64Bit)
                {
                    IntPtr key;
                    int error;
                    // adam: 8/21/22, first try the "Angel Island" key
                    error = WOW6432Node.RegOpenKeyEx(WOW6432Node.HKEY_LOCAL_MACHINE, String.Format(@"SOFTWARE\Angel Island\{0}\1.0", subName),
                        0, WOW6432Node.KEY_READ | WOW6432Node.KEY_WOW64_32KEY, out key);

                    // adam: 8/21/22, next try the standard "Origin Worlds Online" key
                    if (error != 0)
                        error = WOW6432Node.RegOpenKeyEx(WOW6432Node.HKEY_LOCAL_MACHINE, String.Format(@"SOFTWARE\Origin Worlds Online\{0}\1.0", subName),
                        0, WOW6432Node.KEY_READ | WOW6432Node.KEY_WOW64_32KEY, out key);

                    if (error != 0)
                        return null;
                    try
                    {
                        string v = WOW6432Node.RegQueryValue(key, "ExePath") as String;

                        if (v == null || v.Length <= 0)
                            return null;

                        // fix up the string, remove trailing \0
                        if (v.IndexOf("\0") != -1)
                            v = v.Trim('\0');

                        if (!File.Exists(v))
                            return null;

                        v = Path.GetDirectoryName(v);

                        if (v == null)
                            return null;

                        return v;
                    }
                    finally
                    {
                        WOW6432Node.RegCloseKey(key);
                    }
                    return null;
                }
                else
                {
#if false
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format(@"SOFTWARE\Origin Worlds Online\{0}\1.0", subName)))
                    {
                        if (key == null)
                            return null;

                        string v = key.GetValue("ExePath") as string;

                        if (v == null || v.Length <= 0)
                            return null;

                        if (!File.Exists(v))
                            return null;

                        v = Path.GetDirectoryName(v);

                        if (v == null)
                            return null;

                        return v;
                    }
#endif
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}