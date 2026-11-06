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

/* Scripts/Engines/Spawner/Spawner.cs
 * Changelog:
 *  5/24/2026, Adam
 *      Add updated Facade to Spawner.cs
 *      - See Spawner.History.txt for a historical record
 */

using System;
using static Server.Mobiles.Spawner;

namespace Server.Mobiles
{
    public sealed class SpawnerEntry
    {
        public string Name { get; }
        public int Count { get; }

        public SpawnerEntry(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }
    public sealed class SpawnerEntryCollection
    {
        private readonly Spawner _spawner;

        public SpawnerEntryCollection(Spawner spawner)
        {
            _spawner = spawner;
        }

        public void Clear()
        {
            _spawner.ObjectNamesRaw.Clear();
            _spawner.WipeSlots();
            _spawner.InvalidateProperties();
        }
        public int Count
        {
            get { return _spawner.ObjectNamesRaw.Count; }
        }
        public void Add(SpawnerEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (string.IsNullOrWhiteSpace(entry.Name))
                throw new ArgumentException("Spawner entry name cannot be empty.", nameof(entry));

            int count = Math.Max(0, entry.Count);

            _spawner.ObjectNamesRaw.Add(entry.Name);

            int index = _spawner.ObjectNamesRaw.Count - 1;

            if (_spawner.GetFlag(SpawnerAttribs.ModeNeruns) || _spawner.GetFlag(SpawnerAttribs.ModeMulti))
            {
                _spawner.SetSlotCount(index, count);
            }
            else
            {
                /*
                 * Non-slot spawners have one global Count shared by all creature names.
                 * If entries are added with counts, the count represents the global spawn count.
                 */
                _spawner.Count = count;
            }

            _spawner.InvalidateProperties();
        }
    }
    public partial class Spawner : Item
    {
        public SpawnerEntryCollection Entries
        {
            get { return new SpawnerEntryCollection(this); }
        }
    }
}