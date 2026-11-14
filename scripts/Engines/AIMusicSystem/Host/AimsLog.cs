/***************************************************************************
 *
 *                   The Angel Island Music System (AIMS)
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


/* Scripts\Engines\AIMusicSystem\Host\AimsLog.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 11/5/26)
 *      AimsLog: the music system's exception log and console output, through AimsHost (the shard's logging, or the
 *      console). Same calls as Angel Island's LogHelper.LogException and Utility.Monitor, which AIMS used before.
 */

using System;

namespace Server
{
    /// <summary>The music system's logging: exceptions and console lines, through <see cref="AimsHost"/>.</summary>
    public static class AimsLog
    {
        public static void LogException(Exception ex)
        {
            AimsHost.Current.LogException(ex, null);
        }

        public static void LogException(Exception ex, string additionalMessage)
        {
            AimsHost.Current.LogException(ex, additionalMessage);
        }

        public static void WriteLine(string text, ConsoleColor color)
        {
            AimsHost.Current.WriteLine(text, color);
        }

        public static void WriteLine(string format, ConsoleColor color, params object?[] args)
        {
            WriteLine(string.Format(format, args), color);
        }

        /// <summary>Debug builds only.</summary>
        public static void DebugOut(string text, ConsoleColor color = ConsoleColor.White)
        {
#if DEBUG
            WriteLine(text, color);
#endif
        }

        /// <summary>Debug builds only.</summary>
        public static void DebugOut(string format, ConsoleColor color = ConsoleColor.White, params object?[] args)
        {
            DebugOut(string.Format(format, args), color);
        }
    }
}
