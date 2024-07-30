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

/* Scripts/Gumps/Properties/SetDateTimeGump.cs
 * Changelog:
 *	5/17/23, Yoar
 *	    Added DatePrompt, TimePrompt
 *	12/28/22, Yoar
 *		Initial version. Allows us to set DateTime properties using the props gumps.
 */

#pragma warning disable 429

using Server.Network;
using Server.Prompts;
using System;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Server.Gumps
{
    public class SetDateTimeGump : Gump
    {
        private PropertyInfo m_Property;
        private Mobile m_Mobile;
        private object m_Object;
        private Stack m_Stack;
        private int m_Page;
        private ArrayList m_List;

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
        private const int TotalHeight = OffsetSize + (8 * (EntryHeight + OffsetSize));

        private const int BackWidth = BorderSize + TotalWidth + BorderSize;
        private const int BackHeight = BorderSize + TotalHeight + BorderSize;

        public SetDateTimeGump(PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list)
            : base(GumpOffsetX, GumpOffsetY)
        {
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;
            m_Stack = stack;
            m_Page = page;
            m_List = list;

            DateTime dt = (DateTime)prop.GetValue(o, null);

            AddPage(0);

            AddBackground(0, 0, BackWidth, BackHeight, BackGumpID);
            AddImageTiled(BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), TotalHeight, OffsetGumpID);

            AddRect(0, prop.Name, 0, -1);
            AddRect(1, dt.ToString(), 1, -1);
            AddRect(2, "Min", 2, -1);
            AddRect(3, "Now", 3, -1);
            AddRect(4, "From H:M:S", 4, -1);
            AddRect(5, "H:", 5, 0);
            AddRect(6, "M:", 6, 1);
            AddRect(7, "S:", 7, 2);
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

        private class DatePrompt : Prompt
        {
            protected PropertyInfo m_Property;
            protected Mobile m_Mobile;
            protected object m_Object;
            protected Stack m_Stack;
            protected int m_Page;
            protected ArrayList m_List;

            public DatePrompt(PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list) : base()
            {
                m_Property = prop;
                m_Mobile = mobile;
                m_Object = o;
                m_Stack = stack;
                m_Page = page;
                m_List = list;
            }

            public override void OnResponse(Mobile from, string text)
            {
                CultureInfo provider = CultureInfo.CurrentCulture;

                DateTime dt;

                if (!DateTime.TryParseExact(text, provider.DateTimeFormat.ShortDatePattern, provider, DateTimeStyles.None, out dt))
                {
                    from.SendMessage("Bad format");
                }
                else
                {
                    from.SendMessage("Enter a time ({0})", CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern);
                    from.Prompt = new TimePrompt(m_Property, from, m_Object, m_Stack, m_Page, m_List, dt);
                }
            }

            public override void OnCancel(Mobile from)
            {
                from.SendGump(new SetDateTimeGump(m_Property, from, m_Object, m_Stack, m_Page, m_List));
            }
        }

        private class TimePrompt : Prompt
        {
            protected PropertyInfo m_Property;
            protected Mobile m_Mobile;
            protected object m_Object;
            protected Stack m_Stack;
            protected int m_Page;
            protected ArrayList m_List;

            private DateTime m_Date;

            public TimePrompt(PropertyInfo prop, Mobile mobile, object o, Stack stack, int page, ArrayList list, DateTime date) : base()
            {
                m_Property = prop;
                m_Mobile = mobile;
                m_Object = o;
                m_Stack = stack;
                m_Page = page;
                m_List = list;

                m_Date = date;
            }

            public override void OnResponse(Mobile from, string text)
            {
                CultureInfo provider = CultureInfo.CurrentCulture;

                DateTime dt;

                if (!DateTime.TryParseExact(text, provider.DateTimeFormat.ShortTimePattern, provider, DateTimeStyles.None, out dt))
                {
                    from.SendMessage("Bad format");
                }
                else
                {
                    DateTime toSet = m_Date + dt.TimeOfDay;

                    try
                    {
                        Server.Commands.CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, toSet.ToString());
                        m_Property.SetValue(m_Object, toSet, null);
                        PropertiesGump.OnValueChanged(m_Object, m_Property, m_Stack);
                    }
                    catch
                    {
                        m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                    }
                }

                from.SendGump(new SetDateTimeGump(m_Property, from, m_Object, m_Stack, m_Page, m_List));
            }

            public override void OnCancel(Mobile from)
            {
                from.SendGump(new SetDateTimeGump(m_Property, from, m_Object, m_Stack, m_Page, m_List));
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            DateTime toSet = DateTime.MinValue;
            bool shouldSet = false, shouldSend = false;

            TextRelay h = info.GetTextEntry(0);
            TextRelay m = info.GetTextEntry(1);
            TextRelay s = info.GetTextEntry(2);

            switch (info.ButtonID)
            {
                case 1:
                    {
                        toSet = DateTime.MinValue;
                        shouldSet = false;
                        shouldSend = false;

                        sender.Mobile.SendMessage("Enter a date ({0})", CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                        sender.Mobile.Prompt = new DatePrompt(m_Property, sender.Mobile, m_Object, m_Stack, m_Page, m_List);

                        break;
                    }
                case 2: // Min
                    {
                        toSet = DateTime.MinValue;
                        shouldSet = true;
                        shouldSend = true;

                        break;
                    }
                case 3: // Now
                    {
                        toSet = DateTime.UtcNow;
                        shouldSet = true;
                        shouldSend = true;

                        break;
                    }
                case 4: // From H:M:S
                    {
                        if (h != null && m != null && s != null)
                        {
                            try
                            {
                                toSet = DateTime.UtcNow + TimeSpan.Parse(h.Text + ":" + m.Text + ":" + s.Text);
                                shouldSet = true;
                                shouldSend = true;
                            }
                            catch
                            {
                                m_Mobile.SendMessage("Invalid format. The property has not been changed.");
                            }
                        }
                        break;
                    }
                case 5: // From H
                    {
                        if (h != null)
                        {
                            try
                            {
                                toSet = DateTime.UtcNow + TimeSpan.FromHours(double.Parse(h.Text));
                                shouldSet = true;
                                shouldSend = true;
                            }
                            catch
                            {
                                m_Mobile.SendMessage("Invalid format. The property has not been changed.");
                            }
                        }
                        break;
                    }
                case 6: // From M
                    {
                        if (m != null)
                        {
                            try
                            {
                                toSet = DateTime.UtcNow + TimeSpan.FromMinutes(double.Parse(m.Text));
                                shouldSet = true;
                                shouldSend = true;
                            }
                            catch
                            {
                                m_Mobile.SendMessage("Invalid format. The property has not been changed.");
                            }
                        }
                        break;
                    }
                case 7: // From S
                    {
                        if (s != null)
                        {
                            try
                            {
                                toSet = DateTime.UtcNow + TimeSpan.FromSeconds(double.Parse(s.Text));
                                shouldSet = true;
                                shouldSend = true;
                            }
                            catch
                            {
                                m_Mobile.SendMessage("Invalid format. The property has not been changed.");
                            }
                        }
                        break;
                    }
                default:
                    {
                        toSet = DateTime.MinValue;
                        shouldSet = false;
                        shouldSend = true;
                        break;
                    }
            }

            if (shouldSet)
            {
                try
                {
                    Server.Commands.CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, toSet.ToString());
                    m_Property.SetValue(m_Object, toSet, null);
                    PropertiesGump.OnValueChanged(m_Object, m_Property, m_Stack);
                }
                catch
                {
                    m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                }
            }

            if (shouldSend)
                m_Mobile.SendGump(new PropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));
        }
    }
}