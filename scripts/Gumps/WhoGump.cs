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

/* Scripts/Gumps/WhoGump.cs
 * 
 * ChangeLog:
 *	3/25/07, Pix
 *		Added netstate check to weaver's coloring of the illegal client users.
 *	3/13/07, weaver
 *		Altered so that players using an illegal client (as flagged in NetState)
 *		are coloured bright orange.
 *  7/24/06, Rhiannon
 *		Changed to use Mobile.GetHueForNameInList() instead of GetHueFor().
 *  7/22/06, Rhiannon
 *		Set colors for Fight Broker and Reporter to robe colors (valorite and agapite).
 * 	3/2/06, weaver
 *		Added parameter to match character names against.
 */


using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace Server.Gumps
{
    public class WhoGump : Gump
    {
        public static void Initialize()
        {
            CommandSystem.Register("Who", AccessLevel.Counselor, new CommandEventHandler(WhoList_OnCommand));
            CommandSystem.Register("WhoList", AccessLevel.Counselor, new CommandEventHandler(WhoList_OnCommand));
        }

        [Usage("WhoList (<search word>)")]
        [Aliases("Who (<search word>)")]
        [Description("Lists all connected clients.")]
        private static void WhoList_OnCommand(CommandEventArgs e)
        {
            // wea: pass string parameter if there is one

            if (e.Length > 1)
            {
                e.Mobile.SendMessage("Format: who (<search word>)");
                return;
            }

            e.Mobile.SendGump(new WhoGump(e.Mobile, (e.Length == 1 ? e.GetString(0) : "")));
        }

        public static bool OldStyle = PropsConfig.OldStyle;

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

        private static bool PrevLabel = false, NextLabel = false;

        private const int PrevLabelOffsetX = PrevWidth + 1;
        private const int PrevLabelOffsetY = 0;

        private const int NextLabelOffsetX = -29;
        private const int NextLabelOffsetY = 0;

        private const int EntryWidth = 180;
        private const int EntryCount = 15;

        private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
        private const int TotalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (EntryCount + 1));

        private const int BackWidth = BorderSize + TotalWidth + BorderSize;
        private const int BackHeight = BorderSize + TotalHeight + BorderSize;

        private Mobile m_Owner;
        private ArrayList m_Mobiles;
        private int m_Page;

        // wea: added search string
        private string m_Filter;

        private class InternalComparer : IComparer
        {
            public static readonly IComparer Instance = new InternalComparer();

            public InternalComparer()
            {
            }

            public int Compare(object x, object y)
            {
                if (x == null && y == null)
                    return 0;
                else if (x == null)
                    return -1;
                else if (y == null)
                    return 1;

                Mobile a = x as Mobile;
                Mobile b = y as Mobile;

                if (a == null || b == null)
                    throw new ArgumentException();

                if (a.AccessLevel > b.AccessLevel)
                    return -1;
                else if (a.AccessLevel < b.AccessLevel)
                    return 1;
                else
                    return Insensitive.Compare(a.Name, b.Name);
            }
        }

        // wea: new overloaded WhoGump() for cases where no search string
        public WhoGump(Mobile owner)
            : this(owner, "")
        {
        }

        public WhoGump(Mobile owner, string filter)
            : this(owner, BuildList(owner, filter), 0, filter)
        {
        }

        public WhoGump(Mobile owner, ArrayList list, int page, string filter)
            : base(GumpOffsetX, GumpOffsetY)
        {
            owner.CloseGump(typeof(WhoGump));

            m_Owner = owner;
            m_Mobiles = list;
            m_Filter = filter;      // wea: added to handle pattern searching against char names

            Initialize(page);
        }

        public static ArrayList BuildList(Mobile owner)
        {
            return BuildList(owner, "");
        }

        public static ArrayList BuildList(Mobile from, string filter)
        {
            ArrayList list = new ArrayList();
            List<NetState> states = NetState.Instances;
            states = states.Where(ns => ns != null && ns.Mobile != null && from.Account != null &&
                (from.Account as Accounting.Account).AccessLevel >= (ns.Mobile.Account as Accounting.Account).AccessLevel).ToList();

            #region Jump table
            Server.Mobiles.PlayerMobile pm = from as Server.Mobiles.PlayerMobile;
            if (pm != null)
            {
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();
            }
            #endregion Jump table

            for (int i = 0; i < states.Count; ++i)
            {
                Mobile m = states[i].Mobile;

                // wea: added check against filter string
                if (filter != "")
                {
                    if (m.Name.ToLower().IndexOf(filter.ToLower()) != -1)
                    {
                        list.Add(m);
                        pm.JumpList.Add(m);
                    }
                }
                else
                {
                    list.Add(m);
                    pm.JumpList.Add(m);
                }
            }

            list.Sort(InternalComparer.Instance);
            return list;
        }

        public void Initialize(int page)
        {
            m_Page = page;

            int count = m_Mobiles.Count - (page * EntryCount);

            if (count < 0)
                count = 0;
            else if (count > EntryCount)
                count = EntryCount;

            int totalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (count + 1));

            AddPage(0);

            AddBackground(0, 0, BackWidth, BorderSize + totalHeight + BorderSize, BackGumpID);
            AddImageTiled(BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), totalHeight, OffsetGumpID);

            int x = BorderSize + OffsetSize;
            int y = BorderSize + OffsetSize;

            int emptyWidth = TotalWidth - PrevWidth - NextWidth - (OffsetSize * 4) - (OldStyle ? SetWidth + OffsetSize : 0);

            if (!OldStyle)
                AddImageTiled(x - (OldStyle ? OffsetSize : 0), y, emptyWidth + (OldStyle ? OffsetSize * 2 : 0), EntryHeight, EntryGumpID);

            AddLabel(x + TextOffsetX, y, TextHue, String.Format("Page {0} of {1} ({2})", page + 1, (m_Mobiles.Count + EntryCount - 1) / EntryCount, m_Mobiles.Count));

            x += emptyWidth + OffsetSize;

            if (OldStyle)
                AddImageTiled(x, y, TotalWidth - (OffsetSize * 3) - SetWidth, EntryHeight, HeaderGumpID);
            else
                AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);

            if (page > 0)
            {
                AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1, GumpButtonType.Reply, 0);

                if (PrevLabel)
                    AddLabel(x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous");
            }

            x += PrevWidth + OffsetSize;

            if (!OldStyle)
                AddImageTiled(x, y, NextWidth, EntryHeight, HeaderGumpID);

            if ((page + 1) * EntryCount < m_Mobiles.Count)
            {
                AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 2, GumpButtonType.Reply, 1);

                if (NextLabel)
                    AddLabel(x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next");
            }

            for (int i = 0, index = page * EntryCount; i < EntryCount && index < m_Mobiles.Count; ++i, ++index)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                Mobile m = (Mobile)m_Mobiles[index];

                AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
                //				AddLabelCropped( x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, GetHueFor( m ), m.Deleted ? "(deleted)" : m.Name );


                // wea: 13/Mar/2007 Krrios / playuo detection
                int displayhue = m.GetHueForNameInList();
                //SMD: commented out - we've merged runuo2.0's networking
                //if (m.NetState != null && m.NetState.IllegalClientIP)
                //{
                //	displayhue = 44;
                //}

                AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, displayhue, m.Deleted ? "(deleted)" : m.Name);

                x += EntryWidth + OffsetSize;

                if (SetGumpID != 0)
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);

                if (m.NetState != null && !m.Deleted)
                    AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 3, GumpButtonType.Reply, 0);
            }
        }

        //		private static int GetHueFor( Mobile m )
        //		{
        //			switch ( m.AccessLevel )
        //			{
        //				case AccessLevel.Owner: return 0x35;
        //				case AccessLevel.Administrator: return 0x516;
        //				case AccessLevel.Seer: return 0x144;
        //				case AccessLevel.GameMaster: return 0x21;
        //				case AccessLevel.Counselor: return 0x2;
        //				case AccessLevel.FightBroker: return 0x8AB;
        //				case AccessLevel.Reporter: return 0x979;
        //				case AccessLevel.Player: default:
        //				{
        //					if ( m.Murderer )
        //						return 0x21;
        //					else if ( m.Criminal )
        //						return 0x3B1;
        //
        //					return 0x58;
        //				}
        //			}
        //		}

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            switch (info.ButtonID)
            {
                case 0: // Closed
                    {
                        return;
                    }
                case 1: // Previous
                    {
                        if (m_Page > 0)
                            from.SendGump(new WhoGump(from, m_Mobiles, m_Page - 1, m_Filter));

                        break;
                    }
                case 2: // Next
                    {
                        if ((m_Page + 1) * EntryCount < m_Mobiles.Count)
                            from.SendGump(new WhoGump(from, m_Mobiles, m_Page + 1, m_Filter));

                        break;
                    }
                default:
                    {
                        int index = (m_Page * EntryCount) + (info.ButtonID - 3);

                        if (index >= 0 && index < m_Mobiles.Count)
                        {
                            Mobile m = (Mobile)m_Mobiles[index];

                            if (m.Deleted)
                            {
                                from.SendMessage("That player has deleted their character.");
                                from.SendGump(new WhoGump(from, m_Mobiles, m_Page, m_Filter));
                            }
                            else if (m.NetState == null)
                            {
                                from.SendMessage("That player is no longer online.");
                                from.SendGump(new WhoGump(from, m_Mobiles, m_Page, m_Filter));
                            }
                            else if (m == m_Owner || !m.Hidden || m_Owner.AccessLevel > m.AccessLevel)
                            {
                                from.SendGump(new ClientGump(from, m.NetState));
                            }
                            else
                            {
                                from.SendMessage("You cannot see them.");
                                from.SendGump(new WhoGump(from, m_Mobiles, m_Page, m_Filter));
                            }
                        }

                        break;
                    }
            }
        }
    }
}