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

/* Scripts\Misc\FontHelper.cs
 * Changelog
 *  8/5/23, Yoar
 *      Moved from MusicRecorder.cs to its own source file
 */

using System.Collections.Generic;

namespace Server.Misc
{
    public static class FontHelper
    {
        private static readonly Dictionary<char, int> m_Widths;

        public static void Register(char c, int width)
        {
            m_Widths[c] = width;
        }

        static FontHelper()
        {
            m_Widths = new Dictionary<char, int>();

            Register('i', 3);
            Register('l', 3);
            Register('m', 9);
            Register('w', 9);
            Register('E', 7);
            Register('F', 7);
            Register('I', 3);
            Register('L', 7);
            Register('M', 10);
            Register('Q', 9);
            Register('T', 7);
            Register('W', 12);
            Register('Y', 9);
            Register(' ', 8);
            Register('.', 3);
            Register(',', 3);
            Register('\'', 3);
            Register('!', 3);
            Register('?', 7);
            Register(':', 3);
            Register(';', 3);
            Register('/', 9);
            Register('1', 4);
        }

        public static int Width(char c)
        {
            int width;

            if (m_Widths.TryGetValue(c, out width))
                return width;
            else if (c >= 'A' && c <= 'Z')
                return 8;
            else if (c >= '0' && c <= '9')
                return 8;
            else if ((int)c > 127)
                return 16; // example '♪' is 15 pixels. We'll play it safe with 16.
            else
                return 6;
        }

        public static int Width(string str)
        {
            if (str == null)
                return 0;

            int width = 0;

            for (int i = 0; i < str.Length; i++)
                width += Width(str[i]);

            return width;
        }
    }
}