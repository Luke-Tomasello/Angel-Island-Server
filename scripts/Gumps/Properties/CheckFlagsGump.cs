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

/* Scripts/Gumps/Properties/CheckFlagsGump.cs
 * Changelog:
 *	1/15/23, Yoar
 *		Initial version. Allows us to set enum flags properties using the [props menu.
 */

using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Gumps
{
    public class CheckFlagsGump : Gump
    {
        private PropertyInfo m_Property;
        private Mobile m_Mobile;
        private object m_Object;
        private Stack m_Stack;
        private int m_Page;
        private ArrayList m_List;

        private CheckEntry[] m_Entries;
        private int m_MyPage;
        private int m_Pages;

        public const bool OldStyle = PropsConfig.OldStyle;

        public const int GumpOffsetX = PropsConfig.GumpOffsetX;
        public const int GumpOffsetY = PropsConfig.GumpOffsetY;

        public const int TextHue = PropsConfig.TextHue;
        public const int TextOffsetX = PropsConfig.TextOffsetX;

        public const int OffsetGumpID = PropsConfig.OffsetGumpID;
        public const int HeaderGumpID = PropsConfig.HeaderGumpID;
        public const int EntryGumpID = PropsConfig.EntryGumpID;
        public const int BackGumpID = PropsConfig.BackGumpID;
        public const int SetGumpID = PropsConfig.SetGumpID;

        public const int SetWidth = PropsConfig.SetWidth;
        public const int SetOffsetX = PropsConfig.SetOffsetX, SetOffsetY = PropsConfig.SetOffsetY;
        public const int SetButtonID1 = PropsConfig.SetButtonID1;
        public const int SetButtonID2 = PropsConfig.SetButtonID2;

        public const int PrevWidth = PropsConfig.PrevWidth;
        public const int PrevOffsetX = PropsConfig.PrevOffsetX, PrevOffsetY = PropsConfig.PrevOffsetY;
        public const int PrevButtonID1 = PropsConfig.PrevButtonID1;
        public const int PrevButtonID2 = PropsConfig.PrevButtonID2;

        public const int NextWidth = PropsConfig.NextWidth;
        public const int NextOffsetX = PropsConfig.NextOffsetX, NextOffsetY = PropsConfig.NextOffsetY;
        public const int NextButtonID1 = PropsConfig.NextButtonID1;
        public const int NextButtonID2 = PropsConfig.NextButtonID2;

        public const int OffsetSize = PropsConfig.OffsetSize;

        public const int EntryHeight = PropsConfig.EntryHeight;
        public const int BorderSize = PropsConfig.BorderSize;

        private const int EntryWidth = 212;

        private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;

        private const int BackWidth = BorderSize + TotalWidth + BorderSize;

        private const int EntryCount = 12;

        public CheckFlagsGump(PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list)
            : this(prop, mobile, o, stack, page, list, BuildArray(prop.PropertyType), 0)
        {
        }

        private CheckFlagsGump(PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list, CheckEntry[] entries, int myPage)
            : base(GumpOffsetX, GumpOffsetY)
        {
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;
            m_Stack = stack;
            m_Page = page;
            m_List = list;

            m_Entries = entries;
            m_MyPage = myPage;
            m_Pages = (m_Entries.Length + EntryCount - 1) / EntryCount;

            int start = m_MyPage * EntryCount;
            int count = m_Entries.Length - start;

            if (count > EntryCount)
                count = EntryCount;

            int totalHeight = OffsetSize + (count + 3) * (EntryHeight + OffsetSize);

            AddBackground(0, 0, BackWidth, BorderSize + totalHeight + BorderSize, BackGumpID);
            AddImageTiled(BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), totalHeight, OffsetGumpID);

            AddHeader();

            ulong value = ToUInt64(m_Property.GetValue(m_Object, null));

            for (int i = 0; i < count; i++)
            {
                CheckEntry e = m_Entries[start + i];

                AddRect(i + 3, e.Name, 0, 0);

                bool enabled = ((value & e.Value) == e.Value);

                AddButton(BorderSize + OffsetSize + EntryWidth + OffsetSize + SetOffsetX - 1, BorderSize + OffsetSize + (i + 3) * (EntryHeight + OffsetSize) + SetOffsetY - 1, enabled ? 0xD3 : 0xD2, enabled ? 0xd2 : 0xD3, 100 + start + i, GumpButtonType.Reply, 0);
            }
        }

        private void AddHeader()
        {
            int x = BorderSize + OffsetSize;
            int y = BorderSize + OffsetSize;

            int emptyWidth = TotalWidth - PrevWidth - NextWidth - (OffsetSize * 4) - (OldStyle ? SetWidth + OffsetSize : 0);

            AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);

            if (m_MyPage > 0)
            {
                AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1, GumpButtonType.Reply, 0);

                if (OldStyle)
                    AddLabel(x + PrevWidth + 1, y, TextHue, "Previous");
            }

            x += PrevWidth + OffsetSize;

            if (!OldStyle)
                AddImageTiled(x - (OldStyle ? OffsetSize : 0), y, emptyWidth + (OldStyle ? OffsetSize * 2 : 0), EntryHeight, HeaderGumpID);

            x += emptyWidth + OffsetSize;

            if (!OldStyle)
                AddImageTiled(x, y, NextWidth, EntryHeight, HeaderGumpID);

            if (m_MyPage < m_MyPage - 1)
            {
                AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 2, GumpButtonType.Reply, 0);

                if (OldStyle)
                    AddLabel(x - 29, y, TextHue, "Next");
            }

            AddRect(1, m_Property.Name, 0, 0);
            AddRect(2, PropertiesGump.ValueToString(m_Object, m_Property), 0, 0);
        }

        private void AddRect(int index, string str, int button, int text)
        {
            int x = BorderSize + OffsetSize;
            int y = BorderSize + OffsetSize + (index * (EntryHeight + OffsetSize));

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, str);

            if (text != -1)
                AddTextEntry(x + 16 + TextOffsetX, y, EntryWidth - TextOffsetX - 16, EntryHeight, TextHue, text, "");

            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);

            if (button != 0)
                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, button, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            switch (info.ButtonID)
            {
                case 0: // Closed
                    {
                        m_Mobile.SendGump(new PropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));
                        break;
                    }
                case 1: // Previous
                    {
                        from.SendGump(new CheckFlagsGump(m_Property, from, m_Object, m_Stack, m_Page, m_List, m_Entries, Math.Max(0, m_MyPage - 1)));
                        break;
                    }
                case 2: // Next
                    {
                        from.SendGump(new CheckFlagsGump(m_Property, from, m_Object, m_Stack, m_Page, m_List, m_Entries, Math.Min(m_Pages - 1, m_MyPage + 1)));
                        break;
                    }
                default: // Set Flag
                    {
                        if (info.ButtonID >= 100 && info.ButtonID < 100 + m_Entries.Length)
                        {
                            ulong value = ToUInt64(m_Property.GetValue(m_Object, null));

                            CheckEntry e = m_Entries[info.ButtonID - 100];

                            if ((value & e.Value) == e.Value)
                                value &= ~e.Value;
                            else
                                value |= e.Value;

                            try
                            {
                                object toSet = Enum.ToObject(m_Property.PropertyType, value);

                                Server.Commands.CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, toSet.ToString());

                                m_Property.SetValue(m_Object, toSet, null);

                                PropertiesGump.OnValueChanged(m_Object, m_Property, m_Stack);
                            }
                            catch
                            {
                                from.SendMessage("An exception was caught. The property may not have changed.");
                            }

                            from.SendGump(new CheckFlagsGump(m_Property, from, m_Object, m_Stack, m_Page, m_List, m_Entries, m_MyPage));
                        }

                        break;
                    }
            }
        }

        // TODO: Cache per enum type?
        private static CheckEntry[] BuildArray(Type enumType)
        {
            List<CheckEntry> list = new List<CheckEntry>();

            Array values;

            try
            {
                values = Enum.GetValues(enumType);
            }
            catch
            {
                values = new object[0];
            }

            string[] names;

            try
            {
                names = Enum.GetNames(enumType);
            }
            catch
            {
                names = new string[0];
            }

            ulong mask = 0x0;

            for (int i = 0; i < values.Length; i++)
            {
                ulong value = ToUInt64(values.GetValue(i));

                if (value != 0)
                    list.Add(new CheckEntry(i < names.Length ? names[i] : value.ToString(), value));

                mask |= value;
            }

            bool addMask = true;

            for (int i = list.Count - 1; i >= 0 && addMask; i--)
            {
                if (list[i].Value == mask)
                    addMask = false;
            }

            if (addMask)
                list.Add(new CheckEntry("MASK", mask));

            return list.ToArray();
        }

        private static ulong ToUInt64(object obj)
        {
            ulong value;

            try
            {
                value = Convert.ToUInt64(obj);
            }
            catch
            {
                value = 0;
            }

            return value;
        }

        private class CheckEntry
        {
            public readonly string Name;
            public readonly ulong Value;

            public CheckEntry(string name, ulong value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}