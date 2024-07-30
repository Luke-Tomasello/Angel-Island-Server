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

/* Engines/Factions/Gumps/New Gumps/BaseFactionListGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Factions.NewGumps
{
    public abstract class BaseFactionListGump<T> : BaseFactionGump
    {
        protected const int ItemsPerPage = 8;
        protected const int ButtonOffset = 100;

        protected abstract FieldInfo[] Fields { get; }

        protected ListState m_State;

        protected BaseFactionListGump(ListState state)
            : base()
        {
            m_State = state;
        }

        protected void CompileList(Mobile m, int x, int y, bool useScroll, bool useFilter)
        {
            FieldInfo[] fields = Fields;

            int totalWidth = 12 * (fields.Length - 1);

            for (int i = 0; i < fields.Length; i++)
                totalWidth += fields[i].Width;

            if (useFilter && m_State.Filter != null && m_State.Filter.Length > 0)
            {
                m_State.List = new List<T>();

                foreach (T value in m_State.Source)
                {
                    string filterTarget = GetFilterTarget(value);

                    if (filterTarget == null || Insensitive.Contains(filterTarget, m_State.Filter))
                        m_State.List.Add(value);
                }
            }
            else
            {
                m_State.List = m_State.Source;
            }

            if (m_State.Comparer >= 0 && m_State.Comparer < fields.Length)
            {
                if (m_State.List == m_State.Source)
                    m_State.List = new List<T>(m_State.Source);

                m_State.List.Sort(fields[m_State.Comparer].Comparer);
            }

            m_State.Start = Math.Max(Math.Min(m_State.Start, m_State.List.Count - 1), 0);

            if (useScroll)
            {
                if (m_State.Start <= 0)
                    AddButton(x, y + 5, 0x15E3, 0x15E7, 0, GumpButtonType.Page, 0);
                else
                    AddButton(x, y + 5, 0x15E3, 0x15E7, ButtonOffset, GumpButtonType.Reply, 0);

                if (m_State.Start + ItemsPerPage > m_State.List.Count)
                    AddButton(x + 30, y + 5, 0x15E1, 0x15E5, 0, GumpButtonType.Page, 0);
                else
                    AddButton(x + 30, y + 5, 0x15E1, 0x15E5, ButtonOffset + 1, GumpButtonType.Reply, 0);
            }

            if (useFilter)
            {
                AddBackground(x + 65, y, totalWidth - 95, 30, 0xBB8);
                AddTextEntry(x + 70, y + 5, totalWidth - 105, 30, 0x481, 1, m_State.Filter);
                AddButton(x + totalWidth - 19, y, 0x867, 0x868, ButtonOffset + 2, GumpButtonType.Reply, 0);
            }

            y += 35;

            for (int col = 0; col < fields.Length; col++)
            {
                int width = fields[col].Width;

                AddImageTiled(x, y, width + 10, 26, 0xA40);
                AddImageTiled(x + 2, y + 2, width + 6, 22, 0xBBC);

                AddHtmlText(x + 5, y + 3, width, 20, fields[col].Name, false, false);

                int normalID;

                if (col != m_State.Comparer)
                    normalID = 0x2716;
                else if (m_State.Ascending)
                    normalID = 0x983;
                else
                    normalID = 0x985;

                int pressedID;

                if (col == m_State.Comparer)
                    pressedID = normalID + 1;
                else
                    pressedID = normalID;

                AddButton(x - 6 + width, y + 7, normalID, pressedID, 2 * ButtonOffset + col, GumpButtonType.Reply, 0);

                x += width + 12;
            }

            y += 28;

            int yMax = y + 28 * ItemsPerPage;

            for (int i = 0; i < ItemsPerPage; i++)
            {
                int row;

                if (m_State.Ascending)
                    row = m_State.Start + i;
                else
                    row = m_State.Start + m_State.List.Count - (i + 1);

                if (row >= 0 && row < m_State.List.Count)
                {
                    x = 65;

                    AddButton(x - 25, y + 5, 0x4B9, 0x4BA, 3 * ButtonOffset + row, GumpButtonType.Reply, 0);

                    for (int col = 0; col < fields.Length; col++)
                    {
                        int width = fields[col].Width;

                        AddImageTiled(x, y, width + 10, 26, 0xA40);
                        AddImageTiled(x + 2, y + 2, width + 6, 22, 0xBBC);

                        Compile(m, x + 5, y + 3, width, 20, col, m_State.List[row]);

                        x += width + 12;
                    }

                    y += 28;
                }
            }
        }

        protected virtual string GetFilterTarget(T value)
        {
            return null;
        }

        protected virtual void Compile(Mobile m, int x, int y, int width, int height, int col, T value)
        {
        }

        protected void HandleListResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            int group = (info.ButtonID / ButtonOffset);
            int index = (info.ButtonID % ButtonOffset);

            switch (group)
            {
                case 1: // Menu
                    {
                        switch (index)
                        {
                            case 0: // Back
                                {
                                    m_State.Start -= ItemsPerPage;

                                    ResendGump(from);

                                    break;
                                }
                            case 1: // Forward
                                {
                                    m_State.Start += ItemsPerPage;

                                    ResendGump(from);

                                    break;
                                }
                            case 2: // Filter
                                {
                                    m_State.Filter = info.GetTextEntry(1).Text.Trim();

                                    ResendGump(from);

                                    break;
                                }
                        }

                        break;
                    }
                case 2: // Sort
                    {
                        if (index >= 0 && index < Fields.Length)
                        {
                            if (index == m_State.Comparer)
                                m_State.Ascending = !m_State.Ascending;
                            else
                                m_State.Comparer = index;

                            ResendGump(from);
                        }

                        break;
                    }
                case 3: // Select
                    {
                        if (index >= 0 && index < m_State.List.Count)
                        {
                            ResendGump(from);

                            Select(from, index, m_State.List[index]);
                        }

                        break;
                    }
            }
        }

        protected virtual void Select(Mobile from, int row, T value)
        {
        }

        protected abstract void ResendGump(Mobile from);

        protected class FieldInfo
        {
            private int m_Width;
            private TextDefinition m_Name;
            private IComparer<T> m_Comparer;

            public int Width
            {
                get { return m_Width; }
            }

            public TextDefinition Name
            {
                get { return m_Name; }
            }

            public IComparer<T> Comparer
            {
                get { return m_Comparer; }
            }

            public FieldInfo(int width, TextDefinition name, IComparer<T> comparer)
            {
                m_Width = width;
                m_Name = name;
                m_Comparer = comparer;
            }
        }

        protected class ListState
        {
            private List<T> m_Source;
            private List<T> m_List;
            private string m_Filter;
            private int m_Comparer;
            private bool m_Ascending;
            private int m_Start;

            public List<T> Source
            {
                get { return m_Source; }
            }

            public List<T> List
            {
                get { return m_List; }
                set { m_List = value; }
            }

            public string Filter
            {
                get { return m_Filter; }
                set { m_Filter = value; }
            }

            public int Comparer
            {
                get { return m_Comparer; }
                set { m_Comparer = value; }
            }

            public bool Ascending
            {
                get { return m_Ascending; }
                set { m_Ascending = value; }
            }

            public int Start
            {
                get { return m_Start; }
                set { m_Start = value; }
            }

            public ListState(List<T> source, int comparer, bool ascending)
            {
                m_Source = source;
                m_Comparer = comparer;
                m_Ascending = ascending;
            }
        }
    }
}