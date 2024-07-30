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

/* Scripts/Engines/Help/PageQueue.cs
 * Changelog:
 *	3/5/05: Pix
 *		Incorporated PageEnable command.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/02/04, Pixie
 *		Changed to inform user that there is no staff online to answer page.
 */

using Server.Mobiles;
using Server.Network;
using System;
using System.Collections;

namespace Server.Engines.Help
{
    public enum PageType
    {
        Bug,
        Stuck,
        Account,
        Question,
        Suggestion,
        Other,
        Harassment,
        PhysicalHarassment
    }

    public class PageEntry
    {
        private Mobile m_Sender;
        private Mobile m_Handler;
        private DateTime m_Sent;
        private string m_Message;
        private PageType m_Type;
        private Point3D m_PageLocation;
        private Map m_PageMap;

        public Mobile Sender
        {
            get
            {
                return m_Sender;
            }
        }

        public Mobile Handler
        {
            get
            {
                return m_Handler;
            }
            set
            {
                PageQueue.OnHandlerChanged(m_Handler, value, this);
                m_Handler = value;
            }
        }

        public DateTime Sent
        {
            get
            {
                return m_Sent;
            }
        }

        public string Message
        {
            get
            {
                return m_Message;
            }
        }

        public PageType Type
        {
            get
            {
                return m_Type;
            }
        }

        public Point3D PageLocation
        {
            get
            {
                return m_PageLocation;
            }
        }

        public Map PageMap
        {
            get
            {
                return m_PageMap;
            }
        }

        private Timer m_Timer;

        public void Stop()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;
        }

        public PageEntry(Mobile sender, string message, PageType type)
        {
            m_Sender = sender;
            m_Sent = DateTime.UtcNow;
            m_Message = Utility.FixHtml(message);
            m_Type = type;
            m_PageLocation = sender.Location;
            m_PageMap = sender.Map;

            m_Timer = new InternalTimer(this);
            m_Timer.Start();
        }

        private class InternalTimer : Timer
        {
            private static TimeSpan StatusDelay = TimeSpan.FromMinutes(2.0);

            private PageEntry m_Entry;

            public InternalTimer(PageEntry entry)
                : base(TimeSpan.FromSeconds(1.0), StatusDelay)
            {
                m_Entry = entry;
            }

            protected override void OnTick()
            {
                int index = PageQueue.IndexOf(m_Entry);

                if (m_Entry.Sender.NetState != null && index != -1)
                {
                    m_Entry.Sender.SendLocalizedMessage(1008077, true, (index + 1).ToString()); // Thank you for paging. Queue status : 
                                                                                                //m_Entry.Sender.SendLocalizedMessage( 1008084 ); // You can reference our website [...]

                    int iNumberOfGMsOnline = 0;
                    foreach (NetState ns in NetState.Instances)
                    {
                        Mobile m = ns.Mobile;

                        if (m != null && m.AccessLevel >= AccessLevel.Counselor && m.AutoPageNotify)
                        {
                            iNumberOfGMsOnline++;
                        }
                    }

                    if (iNumberOfGMsOnline == 0 || (Server.Commands.PageEnableCommand.Enabled == false))
                    {
                        m_Entry.Sender.SendMessage("We regret to inform you that there are no Angel Island staff online to handle your page.");
                    }

                    m_Entry.Sender.SendMessage("To cancel your page, select the help button again and select cancel.");
                }
                else
                {
                    PageQueue.Remove(m_Entry);
                }
            }
        }
    }

    public class PageQueue
    {
        private static ArrayList m_List = new ArrayList();
        private static Hashtable m_KeyedByHandler = new Hashtable();
        private static Hashtable m_KeyedBySender = new Hashtable();

        public static void Initialize()
        {
            Server.CommandSystem.Register("Pages", AccessLevel.Counselor, new CommandEventHandler(Pages_OnCommand));
        }

        public static bool CheckAllowedToPage(Mobile from)
        {
            PlayerMobile pm = from as PlayerMobile;

            if (pm == null)
                return true;

            if (pm.DesignContext != null)
            {
                from.SendLocalizedMessage(500182); // You cannot request help while customizing a house or transferring a character.
                return false;
            }
            else if (pm.PagingSquelched)
            {
                from.SendMessage("You cannot request help, sorry.");
                return false;
            }

            return true;
        }

        public static void OnHandlerChanged(Mobile old, Mobile value, PageEntry entry)
        {
            if (old != null)
                m_KeyedByHandler.Remove(old);

            if (value != null)
                m_KeyedByHandler[value] = entry;
        }

        [Usage("Pages")]
        [Description("Opens the page queue menu.")]
        private static void Pages_OnCommand(CommandEventArgs e)
        {
            PageEntry entry = (PageEntry)m_KeyedByHandler[e.Mobile];

            if (entry != null)
            {
                e.Mobile.SendGump(new PageEntryGump(e.Mobile, entry));
            }
            else if (m_List.Count > 0)
            {
                e.Mobile.SendGump(new PageQueueGump());
            }
            else
            {
                e.Mobile.SendMessage("The page queue is empty.");
            }
        }

        public static bool IsHandling(Mobile check)
        {
            return m_KeyedByHandler.ContainsKey(check);
        }

        public static bool Contains(Mobile sender)
        {
            return m_KeyedBySender.ContainsKey(sender);
        }

        public static int IndexOf(PageEntry e)
        {
            return m_List.IndexOf(e);
        }

        public static void Cancel(Mobile sender)
        {
            Remove((PageEntry)m_KeyedBySender[sender]);
        }

        public static void Remove(PageEntry e)
        {
            if (e == null)
                return;

            e.Stop();

            m_List.Remove(e);
            m_KeyedBySender.Remove(e.Sender);

            if (e.Handler != null)
                m_KeyedByHandler.Remove(e.Handler);
        }

        public static PageEntry GetEntry(Mobile sender)
        {
            return (PageEntry)m_KeyedBySender[sender];
        }

        public static void Remove(Mobile sender)
        {
            Remove(GetEntry(sender));
        }

        public static ArrayList List
        {
            get
            {
                return m_List;
            }
        }

        public static void Enqueue(PageEntry entry)
        {
            m_List.Add(entry);
            m_KeyedBySender[entry.Sender] = entry;

            foreach (NetState ns in NetState.Instances)
            {
                Mobile m = ns.Mobile;

                if (m != null && m.AccessLevel >= AccessLevel.Counselor && m.AutoPageNotify && !IsHandling(m))
                    m.SendMessage("A new page has been placed in the queue.");
            }
        }
    }
}