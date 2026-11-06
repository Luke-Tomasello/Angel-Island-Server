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

/* Scripts\Engines\Spawner\Spawner.Timer.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        #region Spawner Tools
        public int IndexOf(string text)
        {
            for (int ix = 0; ix < this.ObjectNamesRaw.Count; ix++)
                if ((this.ObjectNamesRaw[ix] as string).ToLower().Contains(text.ToLower()))
                    return ix;
            return -1;
        }
        public int RemoveAtIndex(int index)
        {
            if (index < 0 || index >= this.ObjectNamesRaw.Count)
                return -1;

            if (ObjectNamesRaw[index] is string text && text.Contains(':'))
            {   // multi spawner
                if (text.Contains(text + ":", StringComparison.OrdinalIgnoreCase))
                    ObjectNamesRaw[index] = text.Replace(text + ":", "", StringComparison.OrdinalIgnoreCase);
                else
                    ObjectNamesRaw[index] = text.Replace(text, "", StringComparison.OrdinalIgnoreCase);
            }
            else
                this.ObjectNamesRaw.RemoveAt(index);

            return index;
        }
        public static List<int> Registry = new()
        {   // these are the item ids spawned by spawners
            0xE41,
            0x0,
            0x26AC,
            0xD17,
            0xE3F,
            0xE3C,
            0x9A9,
            0xE73,
            0x18E4,
            0x5B72,
            0x119A,
            0x11AC,
            0xE76,
            0x171A,
            0x171B,
            0x171C,
            0x1713,
            0x1717,
            0x1718,
            0x1714,
            0x1716,
            0x170F,
            0x170B,
            0x1F0B,
            0xE89,
            0x13B4,
            0xF52,
            0xEC4,
            0x13B2,
            0xF3F,
            0x13FF,
            0x1405,
            0xE87,
            0xF0A,
            0xE21,
            0xF0C,
            0xF07,
            0x1727,
            0xF08,
            0xF09,
            0xF0B,
            0xF0D,
            0x13C7,
            0x13CD,
            0x1DB9,
            0x13CB,
            0x13CC,
            0x13C6,
            0x1C02,
            0x13DB,
            0x13DC,
            0x1C0C,
            0x13D5,
            0x13D6,
            0x13DA,
            0xF62,
            0x13FD,
            0x1BFB,
            0xF61,
            0x1401,
            0xF50,
            0x9D0,
            0x1C06,
            0xF43,
            0x1412,
            0x1411,
            0x1413,
            0x1410,
            0x1414,
            0x1415,
            0x1C04,
            0x13B9,
            0x13EC,
            0x13EB,
            0x13F0,
            0x13EE,
            0x13BF,
            0x13BE,
            0xF49,
            0x1B74,
            0x1B7A,
            0x1B76,
            0x143E,
            0x1F09,
            0x1515,
            0x9E9,
            0x9C9,
            0x232A,
            0x9BB,
            0x1BDD,
            0x98C,
            0x2328,
            0x41F6,
            0xA97,
            0xA98,
            0x11A8,
            0x9EC,
            0x993,
            0xF8B,
            0xA26,
            0xE48,
            0x9B5,
            0x1363,
            0xE26,
            0x1914,     // frypan sold by cooks. correctly ignored
            0x9E2,      // this one though has a different graphic id, but looks the same, so we will explicitly ignore it here
            0xFAE,
            0xFF2,
            0xC6A,
            0x1125,
            0x1915,
            0xE77,
            0x14E0,
            0xE83,
            0xC6F,
            0xC63,
            0xC76,
            0xCB6,
            0xC56,
            0x1A9A,
            0xC5E,
            0xC5F,
            0xC62,
            0xC61,
            0xC60,
            0xC54,
            0xC55,
            0x1A9B,
            0xC51,
            0xC58,
            0xC57,
            0x1F4A,
            0x1F38,
            0x1F32,
            0x1F3E,
            0x1F56,
            0xF7A,
            0xF7B,
            0xF8D,
            0xF8C,
            0xF84,
            0xF85,
            0xF88,
            0xF86,
            0x1F03,
            0x1F42,
            0x1F5F,
            0x1F5C,
            0x1F37,
            0x1F57,
            0x1F49,
            0x1F31,
            0x1F69,
            0x1F40,
            0x1F2D,
            0x1EBA,
            0xE14,
            0xE13,
            0x1367,
            0xE81,
            0xF47,
            0x1F50,

        };

        public bool Spawns(int itemId)
        {
            return Registry.Contains(itemId);
        }

        public bool Spawns(string text)
        {
            foreach (string name in this.ObjectNamesRaw)
                if (name.ToLower().Contains(text.ToLower()))
                    return true;
            return false;
        }

        public bool Spawns(Type type, List<Type>? exclude = null)
        {   // note: can't be used for RaresSpawners
            foreach (string name in this.ObjectNamesRaw)
            {
                string[] chunks = name.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string typeName in chunks)
                {
                    Type? typeFound = ScriptCompiler.FindTypeByName(typeName);
                    if (typeFound != null)
                    {
                        if (exclude != null && exclude.Contains(typeFound))
                            continue;
                        if (type.IsAssignableFrom(typeFound))
                            return true;
                    }
                }
            }

            return false;
        }

        public bool IsCurrentlySpawned(IEntity o)
        {
            if (o == null)
                return false;

            // Objects is the spawner's ArrayList of mobiles/items it owns
            return Objects != null && Objects.Contains(o);
        }

        public List<Mobile> GetMobiles()
        {
            List<Mobile> list = new();
            if (Objects != null)
            {
                foreach (object o in Objects)
                    if (o is Mobile m && !m.Deleted)
                        list.Add(m);
            }
            return list;
        }
        public List<Item> GetItems()
        {
            List<Item> list = new();
            if (Objects != null)
            {
                foreach (object o in Objects)
                    if (o is Item item && !item.Deleted)
                        list.Add(item);
            }
            return list;
        }
        #endregion Spawner Tools
    }
}
