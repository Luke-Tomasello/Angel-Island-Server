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

/* Scripts/Gumps/BanDurationGump.cs
 * ChangeLog
 *  11/21/26, Adam (ban details that don't take)
 *      An account whose ban details couldn't be set is reported (it keeps the ban it had); the success line counts only
 *      the accounts that changed.
 *  11/18/26, Adam (account restrictions)
 *      Also asks the ban's scope (this shard, or every shard) and its reason; the choice goes to the shared restrictions
 *      (Restrictions.SetBanDetails) instead of the account's ban tags.
 */

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
            int height = 395;

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

            // the scope and the reason (11/18/26: bans are shared, Engines/Restrictions)
            AddRadio(15, 300, 0xD2, 0xD3, true, 100);
            AddHtml(40, 300, 120, 20, "This shard", false, false);
            AddRadio(15, 322, 0xD2, 0xD3, false, 101);
            AddHtml(40, 322, 120, 20, "Every shard", false, false);
            AddHtml(15, 345, 140, 20, "Reason:", false, false);
            AddTextField(15, 365, 140, 20, 4);
        }

        public void AddInput(int bid, int idx, string name)
        {
            int x = 15;
            int y = 95 + (idx * 50);

            AddButtonLabeled(x, y, bid, name);
            AddTextField(x + 35, y + 20, 100, 20, idx);
        }

        private static string Scope(RelayInfo info) =>
            info.IsSwitched(101) ? Server.Engines.Restrictions.All : Server.Engines.Restrictions.ThisShard;

        private static string Reason(RelayInfo info) =>
            info.GetTextEntry(4)?.Text?.Trim() ?? "";

        // a ban whose details couldn't be set keeps the ones it had (SetBanDetails changes nothing on failure)
        private static void ReportUnset(Mobile from, int unset)
        {
            if (unset > 0)
                from.SendMessage(0x22, "Restrictions: {0} not changed (no ban in force, or the accounts database can't be written): each keeps the ban it had. [restrictions <account> shows it.",
                    unset == 1 ? "1 account" : unset + " accounts");
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from.AccessLevel < AccessLevel.Administrator)
                return;

            TextRelay? d = info.GetTextEntry(0);
            TextRelay? h = info.GetTextEntry(1);
            TextRelay? m = info.GetTextEntry(2);
            TextRelay? s = info.GetTextEntry(3);

            TimeSpan duration;
            bool shouldSet;

            string fromString = from.ToString();

            switch (info.ButtonID)
            {
                case 0:
                    {
                        int unset = 0;
                        for (int i = 0; i < m_List.Count; ++i)
                        {
                            Account? a = (Account?)m_List[i];

                            if (a != null && !Server.Engines.Restrictions.SetBanDetails(a.Username, Scope(info), null, Reason(info), from.Name))
                                unset++;
                        }

                        from.SendMessage("Duration unspecified.");
                        ReportUnset(from, unset);
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
                int unset = 0;
                for (int i = 0; i < m_List.Count; ++i)
                {
                    Account? a = (Account?)m_List[i];

                    if (a != null && !Server.Engines.Restrictions.SetBanDetails(a.Username, Scope(info),
                            duration == TimeSpan.MaxValue ? null : DateTime.UtcNow + duration, Reason(info), from.Name))
                        unset++;
                }

                int set = m_List.Count - unset;
                if (set > 0)
                    from.SendMessage("Restrictions: {0}{1}, {2}.", duration == TimeSpan.MaxValue ? "banned for good" : "banned for " + duration,
                        info.IsSwitched(101) ? " on every shard" : " on this shard", set == 1 ? "1 account" : set + " accounts");
                ReportUnset(from, unset);
            }
            else
            {
                from.SendMessage("Values improperly formatted.");
                from.SendGump(new BanDurationGump(m_List));
            }
        }
    }
}