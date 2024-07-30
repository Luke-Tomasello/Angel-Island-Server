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

/* Gumps\WatchListGump.cs
 * CHANGELOG:
 *	11/19/06, Pix
 *		Changed default to one week.
 *	11/19/06, Pix
 *		Watchlist enhancements
 *  7/24/06, Rhiannon
 *		Changed to use Mobile.GetHueForNameInList() instead of GetHueFor().
 *	12/01/05 - Pix
 *		Initial Version.
 */

using Server.Accounting;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Gumps
{
    public class WatchListGump : Gump
    {
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

        private const int EntryWidth = 400; //Note - changing this changes the width of everything :-)
        private const int EntryCount = 15;

        private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize + SetWidth;
        private const int TotalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (EntryCount + 1));

        private const int BackWidth = BorderSize + TotalWidth + BorderSize;
        private const int BackHeight = BorderSize + TotalHeight + BorderSize;

        private Mobile m_Owner;
        private ArrayList m_Mobiles;
        private int m_Page;

        #region InternalComparer
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
        #endregion

        public WatchListGump(Mobile owner)
            : this(owner, BuildList(owner), 0)
        {
        }

        public WatchListGump(Mobile owner, ArrayList list, int page)
            : base(GumpOffsetX, GumpOffsetY)
        {
            owner.CloseGump(typeof(WatchListGump));

            m_Owner = owner;
            m_Mobiles = list;

            Initialize(page);
        }

        public static ArrayList BuildList(Mobile owner)
        {
            ArrayList list = new ArrayList();
            //ArrayList states = NetState.Instances;
            List<NetState> states = NetState.Instances;

            for (int i = 0; i < states.Count; ++i)
            {
                Mobile m = states[i].Mobile;
                PlayerMobile pm = m as PlayerMobile;
                Account pmacct = null;
                if (pm != null)
                {
                    pmacct = pm.Account as Account;
                }

                //check account level first
                if (pmacct != null && pmacct.Watched)
                {
                    if (DateTime.UtcNow < pmacct.WatchExpire)
                    {
                        list.Add(m);
                    }
                    else
                    {
                        pmacct.Watched = false;
                    }
                }

                if (m != null && pm != null)
                {
                    if (pm.WatchList)
                    {
                        if (DateTime.UtcNow < pm.WatchExpire)
                        {
                            if (!list.Contains(m))
                            {
                                list.Add(m);
                            }
                        }
                        else
                        {
                            pm.WatchList = false;
                        }
                    }
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
                Account a = m.Account as Account;

                AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);

                string strLabel = "NONE";
                if (a != null && a.Watched)
                {
                    strLabel = m.Name + " (acct) :: " + a.WatchReason;
                }
                else if (!m.Deleted)
                {
                    PlayerMobile pm = m as PlayerMobile;
                    strLabel = m.Name + " (char) :: ";
                    if (pm != null)
                    {
                        strLabel += pm.WatchReason;
                    }
                }
                AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, m.GetHueForNameInList(), strLabel);

                x += EntryWidth + OffsetSize;

                if (SetGumpID != 0)
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);

                if (m.NetState != null && !m.Deleted)
                {
                    AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 3, GumpButtonType.Reply, 0);
                    AddButton(x + SetWidth + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 3 + 10000, GumpButtonType.Reply, 0);
                }
            }
        }

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
                            from.SendGump(new WatchListGump(from, m_Mobiles, m_Page - 1));

                        break;
                    }
                case 2: // Next
                    {
                        if ((m_Page + 1) * EntryCount < m_Mobiles.Count)
                            from.SendGump(new WatchListGump(from, m_Mobiles, m_Page + 1));

                        break;
                    }
                default:
                    {
                        if (info.ButtonID > 10000)
                        {
                            int index = (m_Page * EntryCount) + (info.ButtonID - 10000 - 3);

                            if (index >= 0 && index < m_Mobiles.Count)
                            {
                                Mobile focus = (Mobile)m_Mobiles[index];

                                if (focus.Deleted)
                                {
                                    from.SendMessage("That player has deleted their character.");
                                    from.SendGump(new WatchListGump(from, m_Mobiles, m_Page));
                                }
                                else if (focus.NetState == null)
                                {
                                    from.SendMessage("That player is no longer online.");
                                    from.SendGump(new WatchListGump(from, m_Mobiles, m_Page));
                                }
                                else if (focus == m_Owner || !focus.Hidden || m_Owner.AccessLevel > focus.AccessLevel)
                                {
                                    if (focus.Map == null || focus.Map == Map.Internal)
                                    {
                                        from.SendMessage("That character is not in the world.");
                                    }
                                    else
                                    {
                                        from.MoveToWorld(focus.Location, focus.Map);
                                    }
                                }
                                else
                                {
                                    from.SendMessage("You cannot see them.");
                                    from.SendGump(new WatchListGump(from, m_Mobiles, m_Page));
                                }
                            }
                        }
                        else
                        {
                            int index = (m_Page * EntryCount) + (info.ButtonID - 3);

                            if (index >= 0 && index < m_Mobiles.Count)
                            {
                                Mobile m = (Mobile)m_Mobiles[index];

                                if (m.Deleted)
                                {
                                    from.SendMessage("That player has deleted their character.");
                                    from.SendGump(new WatchListGump(from, m_Mobiles, m_Page));
                                }
                                else if (m.NetState == null)
                                {
                                    from.SendMessage("That player is no longer online.");
                                    from.SendGump(new WatchListGump(from, m_Mobiles, m_Page));
                                }
                                else if (m == m_Owner || !m.Hidden || m_Owner.AccessLevel > m.AccessLevel)
                                {
                                    from.SendGump(new ClientGump(from, m.NetState));
                                }
                                else
                                {
                                    from.SendMessage("You cannot see them.");
                                    from.SendGump(new WatchListGump(from, m_Mobiles, m_Page));
                                }
                            }
                        }

                        break;
                    }
            }
        }

    }

    public class WatchListChooserGump : Gump
    {
        #region Commands
        public static void Initialize()
        {
            CommandSystem.Register("Watch", AccessLevel.Counselor, new CommandEventHandler(Watch_OnCommand));
            CommandSystem.Register("WatchList", AccessLevel.Counselor, new CommandEventHandler(WatchList_OnCommand));
            CommandSystem.Register("Unwatch", AccessLevel.Counselor, new CommandEventHandler(Unwatch_OnCommand));
        }

        [Usage("WatchList")]
        [Description("Lists all connected characters on the watchlist.")]
        private static void WatchList_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new WatchListGump(e.Mobile));
        }

        [Usage("Watch")]
        [Description("Adds a character to the watchlist.")]
        private static void Watch_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new WatchTarget(true);
        }

        private static void Unwatch_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new WatchTarget(false);
        }
        #endregion

        private Mobile m_Owner;
        private Mobile m_Target;
        private Accounting.Account m_AccountTarget;

        private enum Buttons
        {
            Proceed,
            Cancel,
            AccountCheck
        }

        private const int LabelHue = 0x480;

        public WatchListChooserGump(Mobile owner, Mobile target)
            : base(50, 50)
        {
            owner.CloseGump(typeof(WatchListChooserGump));

            m_Owner = owner;
            m_Target = target;
            m_AccountTarget = target.Account as Accounting.Account;

            AddPage(0);

            AddBackground(0, 0, 500, 370, 5054);
            AddBackground(10, 10, 480, 350, 3000);

            //AddHtml(20, 15, 300, 30, "Watchlisting character or account:", true, false);
            AddLabelCropped(20, 15, 300, 20, LabelHue, "Watchlisting Character or Account");

            AddLabelCropped(20, 50, 60, 20, LabelHue, "Reason:");
            AddTextField(70, 50, 400, 20, 0, "");

            AddCheck(20, 80, 210, 211, true, (int)Buttons.AccountCheck);
            AddLabelCropped(50, 80, 250, 20, LabelHue, "Watchlist Account");

            //---------------------------------------------------------
            AddLabelCropped(50, 120, 300, 20, LabelHue, "Duration:");

            AddRadio(50, 140, 0x25F8, 0x25FB, false, 100);
            AddLabelCropped(80, 140, 250, 20, LabelHue, "Permanent");

            AddRadio(50, 170, 0x25F8, 0x25FB, false, 101);
            AddLabelCropped(80, 170, 250, 20, LabelHue, "One Month");

            AddRadio(50, 200, 0x25F8, 0x25FB, true, 102);
            AddLabelCropped(80, 200, 250, 20, LabelHue, "One Week");

            AddRadio(50, 230, 0x25F8, 0x25FB, false, 103);
            AddLabelCropped(80, 230, 250, 20, LabelHue, "24 Hours");

            //----------------------------------------------------------

            AddButton(20, 320, 4005, 4007, (int)Buttons.Proceed, GumpButtonType.Reply, 0);
            AddHtml(55, 320, 75, 20, "Proceed", false, false);

            AddButton(135, 320, 4005, 4007, (int)Buttons.Cancel, GumpButtonType.Reply, 0);
            AddHtml(170, 320, 75, 20, "Cancel", false, false);

        }

        public void AddTextField(int x, int y, int width, int height, int index, string initialvalue)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, initialvalue);
        }


        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == (int)Buttons.Proceed)
            {
                bool watchaccount = info.IsSwitched((int)Buttons.AccountCheck);
                string strReason = "";

                TextRelay tr = info.GetTextEntry(0);
                strReason = (tr == null ? null : tr.Text.Trim());

                if (strReason == null || strReason.Length <= 3)
                {
                    m_Owner.SendMessage("A reason for watchlisting is required.");
                    return;
                }


                if (watchaccount)
                {
                    if (m_AccountTarget != null)
                    {
                        if (m_AccountTarget.Watched)
                        {
                            m_Owner.SendMessage("Account is already watched ({0}).", m_AccountTarget.WatchReason);
                        }
                        else
                        {
                            m_AccountTarget.Watched = true;
                            m_AccountTarget.WatchReason = strReason;

                            string duration = "";
                            if (info.IsSwitched(101)) //1 month
                            {
                                m_AccountTarget.WatchExpire = DateTime.UtcNow + TimeSpan.FromDays(30.0);
                                duration = "one month";
                            }
                            else if (info.IsSwitched(102)) //1 week
                            {
                                m_AccountTarget.WatchExpire = DateTime.UtcNow + TimeSpan.FromDays(7.0);
                                duration = "one week";
                            }
                            else if (info.IsSwitched(103)) //24 hours
                            {
                                m_AccountTarget.WatchExpire = DateTime.UtcNow + TimeSpan.FromDays(1.0);
                                duration = "24 hours";
                            }
                            else //permanent
                            {
                                m_AccountTarget.WatchExpire = DateTime.MaxValue;
                                duration = "permanent";
                            }

                            m_Owner.SendMessage("Added account {0} to watchlist ({1})", m_AccountTarget.Username, duration);
                        }
                    }
                }
                else
                {
                    PlayerMobile targ = m_Target as PlayerMobile;

                    if (targ != null)
                    {
                        if (m_AccountTarget.Watched)
                        {
                            m_Owner.SendMessage("Character's account is already watched ({0}).", m_AccountTarget.WatchReason);
                        }
                        else if (targ.WatchList)
                        {
                            m_Owner.SendMessage("Character is already watched ({0}).", targ.WatchReason);
                        }
                        else
                        {
                            targ.WatchList = true;
                            targ.WatchReason = strReason;

                            //WatchExpire
                            string duration = "";
                            if (info.IsSwitched(101)) //1 month
                            {
                                targ.WatchExpire = DateTime.UtcNow + TimeSpan.FromDays(30.0);
                                duration = "one month";
                            }
                            else if (info.IsSwitched(102)) //1 week
                            {
                                targ.WatchExpire = DateTime.UtcNow + TimeSpan.FromDays(7.0);
                                duration = "one week";
                            }
                            else if (info.IsSwitched(103)) //24 hours
                            {
                                targ.WatchExpire = DateTime.UtcNow + TimeSpan.FromDays(1.0);
                                duration = "24 hours";
                            }
                            else //permanent
                            {
                                targ.WatchExpire = DateTime.MaxValue;
                                duration = "permanent";
                            }

                            m_Owner.SendMessage("Added {0} to watch list ({1}).", targ.Name, duration);
                        }
                    }
                }
            }
        }


        private class WatchTarget : Target
        {
            private bool m_bWatch = true;
            public WatchTarget(bool bWatch)
                : base(-1, false, TargetFlags.None)
            {
                m_bWatch = bWatch;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is PlayerMobile)
                {
                    if (m_bWatch)
                    {
                        from.SendGump(new WatchListChooserGump(from, targeted as Mobile));
                    }
                    else
                    {
                        //unwatch
                        PlayerMobile targ = (PlayerMobile)targeted;
                        Accounting.Account targaccount = targ.Account as Accounting.Account;
                        if (targ.WatchList)
                        {
                            targ.WatchList = false;
                            from.SendMessage("Unwatching character {0}", targ.Name);
                        }
                        else if (targaccount != null && targaccount.Watched)
                        {
                            targaccount.Watched = false;
                            from.SendMessage("Unwatching account {0}", targaccount.Username);
                        }
                        else
                        {
                            from.SendMessage("Character and account already unwatched");
                        }
                    }
                }
                else
                {
                    from.SendMessage("You must target a player.");
                }
            }
        }
    }
}