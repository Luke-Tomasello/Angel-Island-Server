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

using Server.Accounting;
using System;
using System.Collections;

namespace Server.Gumps
{
    public class BanDurationGump : Gump
    {
        private ArrayList m_List;

        public void AddButtonLabeled(int x, int y, int buttonID, string text)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            AddHtml(x + 35, y, 240, 20, text, false, false);
        }

        public void AddTextField(int x, int y, int width, int height, int index)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
        }

        public static ArrayList MakeList(object obj)
        {
            ArrayList list = new ArrayList(1);
            list.Add(obj);
            return list;
        }

        public BanDurationGump(Account a)
            : this(MakeList(a))
        {
        }

        public BanDurationGump(ArrayList list)
            : base((640 - 170) / 2, (480 - 305) / 2)
        {
            m_List = list;

            int width = 170;
            int height = 305;

            AddPage(0);

            AddBackground(0, 0, width, height, 5054);

            //AddImageTiled( 10, 10, width - 20, 20, 2624 );
            //AddAlphaRegion( 10, 10, width - 20, 20 );
            AddHtml(10, 10, width - 20, 20, "<CENTER>Ban Duration</CENTER>", false, false);

            //AddImageTiled( 10, 40, width - 20, height - 50, 2624 );
            //AddAlphaRegion( 10, 40, width - 20, height - 50 );

            AddButtonLabeled(15, 45, 1, "Infinite");
            AddButtonLabeled(15, 65, 2, "From D:H:M:S");

            AddInput(3, 0, "Days");
            AddInput(4, 1, "Hours");
            AddInput(5, 2, "Minutes");
            AddInput(6, 3, "Seconds");
        }

        public void AddInput(int bid, int idx, string name)
        {
            int x = 15;
            int y = 95 + (idx * 50);

            AddButtonLabeled(x, y, bid, name);
            AddTextField(x + 35, y + 20, 100, 20, idx);
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from.AccessLevel < AccessLevel.Administrator)
                return;

            TextRelay d = info.GetTextEntry(0);
            TextRelay h = info.GetTextEntry(1);
            TextRelay m = info.GetTextEntry(2);
            TextRelay s = info.GetTextEntry(3);

            TimeSpan duration;
            bool shouldSet;

            string fromString = from.ToString();

            switch (info.ButtonID)
            {
                case 0:
                    {
                        for (int i = 0; i < m_List.Count; ++i)
                        {
                            Account a = (Account)m_List[i];

                            a.SetUnspecifiedBan(from);
                        }

                        from.SendMessage("Duration unspecified.");
                        return;
                    }
                case 1: // infinite
                    {
                        duration = TimeSpan.MaxValue;
                        shouldSet = true;
                        break;
                    }
                case 2: // From D:H:M:S
                    {
                        if (d != null && h != null && m != null && s != null)
                        {
                            try
                            {
                                duration = new TimeSpan(Utility.ToInt32(d.Text), Utility.ToInt32(h.Text), Utility.ToInt32(m.Text), Utility.ToInt32(s.Text));
                                shouldSet = true;

                                break;
                            }
                            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                case 3: // From D
                    {
                        if (d != null)
                        {
                            try
                            {
                                duration = TimeSpan.FromDays(Utility.ToDouble(d.Text));
                                shouldSet = true;

                                break;
                            }
                            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                case 4: // From H
                    {
                        if (h != null)
                        {
                            try
                            {
                                duration = TimeSpan.FromHours(Utility.ToDouble(h.Text));
                                shouldSet = true;

                                break;
                            }
                            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                case 5: // From M
                    {
                        if (m != null)
                        {
                            try
                            {
                                duration = TimeSpan.FromMinutes(Utility.ToDouble(m.Text));
                                shouldSet = true;

                                break;
                            }
                            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                case 6: // From S
                    {
                        if (s != null)
                        {
                            try
                            {
                                duration = TimeSpan.FromSeconds(Utility.ToDouble(s.Text));
                                shouldSet = true;

                                break;
                            }
                            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                        }

                        duration = TimeSpan.Zero;
                        shouldSet = false;

                        break;
                    }
                default: return;
            }

            if (shouldSet)
            {
                for (int i = 0; i < m_List.Count; ++i)
                {
                    Account a = (Account)m_List[i];

                    a.SetBanTags(from, DateTime.UtcNow, duration);
                }

                if (duration == TimeSpan.MaxValue)
                    from.SendMessage("Duration is infinite.");
                else
                    from.SendMessage("Duration is {0}.", duration);
            }
            else
            {
                from.SendMessage("Values improperly formatted.");
                from.SendGump(new BanDurationGump(m_List));
            }
        }
    }
}