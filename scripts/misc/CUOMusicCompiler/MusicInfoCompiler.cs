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

/* Purpose:
 * CUO and AI/Sp use a different approach for locating music.
 * It was assumed that client software would only look in Music and or Music/Digital.
 * CUO however recurses all directories under Music looking for music files. This is problematic since original AI had all of Digital, Digitial.new, and Digitial.old directories.
 *  (It's impossible to know which one CUO is going to pick unless we use the same algo to find the music.)
 * So this routine uses CUO's logic for locating usable music files. We then take exactly those files and, with the help of NAudio, compile timing information
 *  for each file. We use this timing (duration) information in the Music Controller to allow tight looping of music not otherwise looped by the client.
 */

/* Scripts\misc\CUOMusicCompiler\MusicInfoCompiler.cs
 * CHANGELOG:
 *  4/29/2024, Adam 
 *      first time checkin. 
 */
using NAudio.Wave;
using Server;
using Server.Diagnostics;
using Server.Engines;
using Server.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using static Server.Utility;

namespace UOMusicDurationCompiler
{
    public static class MusicInfoCompiler
    {
        public static readonly Dictionary<int, Tuple<string, bool>> _musicData = new Dictionary<int, Tuple<string, bool>>();
        public static List<string> output = new List<string>();
        public static void Compile()
        {
            // CUO's music data structure, created as CUO sees our music library
            // We inject ourselves in here with AddAIStyleRecord() to generate a music database complete with music duration information
            InitMusic();
            _musicData.Clear();

            #region Static Table generation
#if false
            foreach (var kvp in UOMusicInfo)
                output.Add(string.Format("{0} MusicName.{1}, ( {2}, {3}) {4},", '{', kvp.Key.ToString(), kvp.Value.Item1, kvp.Value.Item2.ToString().ToLower(), '}'));

            File.WriteAllLines("table.txt", output, Encoding.UTF8);
#endif
            #endregion Static Table generation
            return;
        }
        public static bool TryParseLevenshtein(Type enumType, string? value, bool ignoreCase, out object? result)
        {
            result = null;
            int score;

            string real_name = IntelligentDialogue.Levenshtein.BestEnumMatchPure(enumType, value, out score);
            if (score < 2)
                if (Enum.TryParse(enumType, real_name, ignoreCase: true, out result))
                    return true;

            return false;
        }
        private static void InitMusic()
        {
            string uo_path = DataPath.GetUOPath("Ultima Online");
            string path = Path.Combine(uo_path, "Music", "Digital", "Config.txt");
            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (TryParseConfigLine(line, out Tuple<int, string, bool> songData))
                        {
                            _musicData[songData.Item1] = new Tuple<string, bool>(songData.Item2, songData.Item3);
                        }
                    }
                }
            }
        }
        private static readonly char[] _configFileDelimiters = { ' ', ',', '\t' };
        /// <summary>
        ///     Attempts to parse a line from UO's music Config.txt.
        /// </summary>
        /// <param name="line">A line from the file.</param>
        /// <param name="?">If successful, contains a tuple with these fields: int songIndex, string songName, bool doesLoop</param>
        /// <returns>true if line could be parsed, false otherwise.</returns>
        private static bool TryParseConfigLine(string line, out Tuple<int, string, bool> songData)
        {
            songData = null;

            // bug in our Config.txt (or really CUO's weak parsing)
            // Our line "64 SelimsBar.mp3                 " contains all these spaces. CUO parser thinks too many parameters and bails
            // we therefore add the .Trim() here to clean up the line
            //string[] splits = line.Split(_configFileDelimiters);
            string[] splits = line.Trim().Split(_configFileDelimiters);

            if (splits.Length < 2 || splits.Length > 3)
            {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            int index = int.Parse(splits[0]);

            // check if name exists as file, ignoring case since UO isn't consistent with file case (necessary for *nix)
            // also, not every filename in Config.txt has a file extension, so let's strip it out just in case.
            string full_pathname = string.Empty;
            string name = GetTrueFileName(Path.GetFileNameWithoutExtension(splits[1]), ref full_pathname);

            bool doesLoop = splits.Length == 3 && splits[2] == "loop";

            songData = new Tuple<int, string, bool>(index, name, doesLoop);

            // adam, since CUO is using this 'all directories' approach, we need to know where they found the file so we can grab duration information from THAT file
            //  BTW, with this new knowledge, we need to flatten our music folder to avoid this in a future release
            AddAIStyleRecord(Path.GetFileNameWithoutExtension(splits[1]), doesLoop, full_pathname);

            return true;
        }
        private static void AddAIStyleRecord(string name, bool doesLoop, string full_pathname)
        {
            // Does this name_only map to a valid MusicName?
            MusicName id = MusicName.Invalid;
            object o;
            if (Enum.TryParse(typeof(MusicName), name, ignoreCase: true, out o))
                id = (MusicName)o;
            else if (TryParseLevenshtein(typeof(MusicName), name, ignoreCase: true, out o))
                id = (MusicName)o;
            else
            {
                // songs like "Turfin" don't map to MusicName, we therefore ignore it
                return;
            }

            if (id != MusicName.Invalid)
            {
                var time = new AudioFileReader(full_pathname).TotalTime;
                UOMusicInfo[(int)id] = (time.TotalMilliseconds, doesLoop);
            }
        }
        /// <summary>
        ///     Returns true filename from name, ignoring case since UO isn't consistent with file case (necessary for *nix)
        /// </summary>
        /// <param name="name">The filename from the music Config.txt</param>
        /// <returns>a string with the true case sensitive filename</returns>
        private static string GetTrueFileName(string name, ref string full_pathname)
        {
            LogHelper logger = new LogHelper("MusicInfoCompiler.log", overwrite: true, sline: true);
            try
            {
                // don't worry about subdirectories, we'll recursively search them all
                //string dir = Settings.GlobalSettings.UltimaOnlineDirectory + $"/Music";
                string uo_path = DataPath.GetUOPath("Ultima Online");
                string dir = uo_path + $"/Music";

                // Enumerate all files in the directory, using the file name as a pattern
                // This will list all case variants of the filename even on file systems that
                // are case sensitive.
                Regex pattern = new Regex($"^{name}.mp3", RegexOptions.IgnoreCase);
                //string[] fileList = Directory.GetFiles(dir, "*.mp3", SearchOption.AllDirectories).Where(path => pattern.IsMatch(Path.GetFileName(path))).ToArray();
                string[] fileList = Directory.GetFiles(dir, "*.mp3", SearchOption.AllDirectories);
                fileList = Array.FindAll(fileList, path => pattern.IsMatch(Path.GetFileName(path)));

                if (fileList != null && fileList.Length != 0)
                {
                    if (fileList.Length > 1)
                    {
                        // More than one file with the same name but different case spelling found
                        //Log.Warn($"Ambiguous File reference for {name}. More than one file found with different spellings.");
                        logger.Log($"Warning: Ambiguous File reference for {name}. More than one file found with different spellings.");
                    }

                    //Log.Debug($"Loading music:\t\t{fileList[0]}");
                    logger.Log($"Info: Loading music:\t\t{fileList[0]}");

                    // adam, since CUO is using this 'all directories' approach, we need to know where they found the file so we can grab duration information from THAT file
                    //  BTW, with this new knowledge, we need to flatten our music folder to avoid this in a future release
                    full_pathname = fileList[0];

                    return Path.GetFileName(fileList[0]);
                }

                // If we've made it this far, there is no file with that name, regardless of case spelling
                // return name and GetMusic will fail gracefully (play nothing)
                //Log.Warn($"No File found known as {name}");
                logger.Log($"Warning: No File found known as {name}");
            }
            catch { }
            finally
            { logger.Finish(); }

            return name;
        }
    }
}