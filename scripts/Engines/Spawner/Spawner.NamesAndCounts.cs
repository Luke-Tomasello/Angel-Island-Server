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

/* Scripts\Engines\Spawner\Spawner.NamesAndCounts.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        public List<string> ObjectNamesParsed
        {
            get
            {
                List<string> list = new List<string>();
                foreach (string name in m_ObjectNames)
                {
                    if (name.Contains(':'))
                    {
                        string[] tokens = name.Split(':');
                        foreach (string token in tokens)
                            list.Add(token);
                    }
                    else
                        list.Add(name);
                }

                return list;
            }
        }
        private List<string> GetObjectNames(string name)
        {
            List<string> list = new List<string>();
            if (name.Contains(':'))
            {
                string[] tokens = name.Split(':');
                foreach (string token in tokens)
                    list.Add(token);
            }
            else
                list.Add(name);

            return list;
        }
        public bool Contains(Type item)
        {
            List<Type> objects = ObjectTypes;
            if (objects == null || objects.Count == 0)
                return false;
            if (objects.Contains(item))
                return true;

            return false;
        }
        public ReadOnlyCollection<object> ObjectNames
        {
            get
            {
                List<object> list = new List<object>(ObjectNamesParsed);
                return list.AsReadOnly();
            }
        }
        public ArrayList ObjectNamesRaw
        {
            get { return m_ObjectNames; }
            set
            {
                m_ObjectNames = value;
                if (m_ObjectNames.Count < 1)
                    Stop();

                InvalidateProperties();
            }
        }
        public ArrayList Objects
        {
            get { return m_Objects; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Count
        {   // Nerun's spawners stores count(s) here
            get { return m_SlotCount == null ? 0 : m_SlotCount.Sum(); }
            set
            {
                if (GetSlotCount(index: 0) != value)
                {
                    if ((GetFlag(SpawnerAttribs.ModeNeruns) || GetFlag(SpawnerAttribs.ModeMulti)))
                    {
                        this.SendMessageNearby("Nerun's spawners cannot have their 'count' set in this way");
                        return;
                    }

                    SetSlotCount(index: 0, value: value);

                    InvalidateProperties();
                }
            }
        }
        public void WipeSlots()
        {
            m_SlotCount = new(0);
        }
        private void SlotCountEnsureCapacity(int index)
        {
            if (m_SlotCount == null)
                m_SlotCount = new(0);

            while (m_SlotCount.Count < index + 1)
                m_SlotCount.Add(0);
        }
        public void SetSlotCount(int index, int value)
        {
            SlotCountEnsureCapacity(index);
            m_SlotCount[index] = value;
        }
        public int GetSlotCount(int index)
        {
            SlotCountEnsureCapacity(index);
            return m_SlotCount[index];
        }
        private int GetSlotTableSize()
        {
            if (m_SlotCount == null)
                return 0;

            return m_SlotCount.Count;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string Counts
        {
            get
            {
                string counts = string.Empty;
                int size = GetSlotTableSize();
                if (m_SlotCount == null)
                    m_SlotCount = new(0);
                foreach (int ix in m_SlotCount)
                    counts += ix.ToString() + ", ";
                counts = counts.TrimEnd(new char[] { ',', ' ' });
                return counts;
            }
            set
            {
                if (value != null)
                {
                    string[] tokens = value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    for (int ix = 0; ix < tokens.Length; ix++)
                        SetSlotCount(index: ix, int.Parse(tokens[ix]));
                }
                else
                {
                    WipeSlots();
                }

                InvalidateProperties();
            }
        }
    }
}
