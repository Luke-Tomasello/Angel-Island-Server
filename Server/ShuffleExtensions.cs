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


/* Server/ShuffleExtensions.cs
 * ChangeLog:
 *  10/6/2026
 *      Moved from Utility.cs as part of a structural cleanup; no behavior change.
 *      Reversible keyed string shuffle extensions (ShuffleExtensions).
 */

using System;
using System.Linq;
namespace Server
{
    #region Reversible Shuffle
    public static class ShuffleExtensions
    {
        public static int[] GetShuffleExchanges(int size, int key)
        {
            int[] exchanges = new int[size - 1];
            var rand = new Random(key);
            for (int i = size - 1; i > 0; i--)
            {
                int n = rand.Next(i + 1);
                exchanges[size - 1 - i] = n;
            }
            return exchanges;
        }

        public static string Shuffle(this string toShuffle, int key)
        {
            int size = toShuffle.Length;
            char[] chars = toShuffle.ToArray();
            var exchanges = GetShuffleExchanges(size, key);
            for (int i = size - 1; i > 0; i--)
            {
                int n = exchanges[size - 1 - i];
                char tmp = chars[i];
                chars[i] = chars[n];
                chars[n] = tmp;
            }
            return new string(chars);
        }

        public static string DeShuffle(this string shuffled, int key)
        {
            int size = shuffled.Length;
            char[] chars = shuffled.ToArray();
            var exchanges = GetShuffleExchanges(size, key);
            for (int i = 1; i < size; i++)
            {
                int n = exchanges[size - i - 1];
                char tmp = chars[i];
                chars[i] = chars[n];
                chars[n] = tmp;
            }
            return new string(chars);
        }
    }
    #endregion Reversible Shuffle
}
