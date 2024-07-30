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

/* Scripts/Gumps/HouseGumpAOS.cs
 * ChangeLog
 *	5/11/08, Adam
 *		Performance Conversion: Regions now use HashTables instead of ArrayLists
 *	4/30/04, mith
 *		Removed ability to toggle house from public to private.
 */

using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections;

namespace Server.Gumps
{
    public enum HouseGumpPageAOS
    {
        Information,
        Security,
        Storage,
        Customize,
        Ownership,
        ChangeHanger,
        ChangeFoundation,
        ChangeSign,
        RemoveCoOwner,
        ListCoOwner,
        RemoveFriend,
        ListFriend,
        RemoveBan,
        ListBan,
        RemoveAccess,
        ListAccess,
        ChangePost
    }

    public class HouseGumpAOS : Gump
    {
        private BaseHouse m_House;
        private HouseGumpPageAOS m_Page;

        private const int LabelColor = 0x7FFF;
        private const int SelectedColor = 0x421F;
        private const int DisabledColor = 0x4210;

        private const int LabelHue = 0x480;

        private ArrayList m_List;

        private string GetOwnerName()
        {
            Mobile m = m_House.Owner;

            if (m == null || m.Deleted)
                return "(unowned)";

            string name;

            if ((name = m.Name) == null || (name = name.Trim()).Length <= 0)
                name = "(no name)";

            return name;
        }

        private string GetDateTime(DateTime val)
        {
            if (val == DateTime.MinValue)
                return "";

            return val.ToString("yyyy'-'MM'-'dd HH':'mm':'ss");
        }

        public void AddPageButton(int x, int y, int buttonID, int number, HouseGumpPageAOS page)
        {
            bool isSelection = (m_Page == page);

            AddButton(x, y, isSelection ? 4006 : 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            AddHtmlLocalized(x + 45, y, 200, 20, number, isSelection ? SelectedColor : LabelColor, false, false);
        }

        public void AddButtonLabeled(int x, int y, int buttonID, int number)
        {
            AddButtonLabeled(x, y, buttonID, number, true);
        }

        public void AddButtonLabeled(int x, int y, int buttonID, int number, bool enabled)
        {
            if (enabled)
                AddButton(x, y, 4005, 4007, buttonID, GumpButtonType.Reply, 0);

            AddHtmlLocalized(x + 35, y, 240, 20, number, enabled ? LabelColor : DisabledColor, false, false);
        }

        public void AddList(ArrayList list, int button, bool accountOf)
        {
            if (list == null)
                return;

            m_List = list;

            int lastPage = 0;
            int index = 0;

            for (int i = 0; i < list.Count; ++i)
            {
                int offset = index % 10;
                int page = 1 + (index / 10);

                if (page != lastPage)
                {
                    if (lastPage != 0)
                        AddButton(40, 360, 4005, 4007, 0, GumpButtonType.Page, page);

                    AddPage(page);

                    if (lastPage != 0)
                        AddButton(10, 360, 4014, 4016, 0, GumpButtonType.Page, lastPage);

                    lastPage = page;
                }


                Mobile m = (Mobile)list[i];

                string name;

                if (m == null || (name = m.Name) == null || (name = name.Trim()).Length <= 0)
                    continue;

                if (button != -1)
                    AddButton(10, 150 + (offset * 20), 4005, 4007, GetButtonID(button, i), GumpButtonType.Reply, 0);

                AddLabel(button > 0 ? 45 : 10, 150 + (offset * 20), LabelHue, accountOf && m.Player && m.Account != null ? String.Format("* Account of {0}", name) : String.Format("* {0}", name));
                ++index;
            }
        }

        public int GetButtonID(int type, int index)
        {
            return 1 + (index * 15) + type;
        }

        private static int[] m_HangerNumbers = new int[]
            {
                2968, 2970, 2972,
                2974, 2976, 2978
            };

        private static int[] m_FoundationNumbers = new int[]
            {
                20, 189, 765, 65, 101
            };

        private static int[] m_PostNumbers = new int[]
            {
                9, 29, 54, 90, 147, 169,
                177, 204, 251, 257, 263,
                298, 347, 424, 441, 466,
                514, 600, 601, 602, 603,
                660, 666, 672, 898, 970,
                974, 982
            };

        public HouseGumpAOS(HouseGumpPageAOS page, Mobile from, BaseHouse house)
            : base(50, 40)
        {
            m_House = house;
            m_Page = page;

            from.CloseGump(typeof(HouseGumpAOS));
            //from.CloseGump( typeof( HouseListGump ) );
            //from.CloseGump( typeof( HouseRemoveGump ) );

            bool isOwner = house.IsOwner(from);
            bool isCoOwner = isOwner || house.IsCoOwner(from);
            bool isFriend = isCoOwner || house.IsFriend(from);

            AddPage(0);

            if (isFriend)
            {
                AddBackground(0, 0, 420, 440, 5054);

                AddImageTiled(10, 10, 400, 100, 2624);
                AddAlphaRegion(10, 10, 400, 100);

                AddImageTiled(10, 120, 400, 260, 2624);
                AddAlphaRegion(10, 120, 400, 260);

                AddImageTiled(10, 390, 400, 40, 2624);
                AddAlphaRegion(10, 390, 400, 40);

                AddButtonLabeled(250, 410, 0, 1060675); // CLOSE
            }

            AddImage(10, 10, 100);

            if (m_House.Sign != null)
            {
                ArrayList lines = Wrap(m_House.Sign.Name);

                if (lines != null)
                {
                    for (int i = 0, y = (114 - (lines.Count * 14)) / 2; i < lines.Count; ++i, y += 14)
                    {
                        string s = (string)lines[i];

                        AddLabel(10 + ((160 - (s.Length * 8)) / 2), y, 0, s);
                    }
                }
            }

            if (!isFriend)
                return;

            if (house.Public)
            {
                AddButtonLabeled(10, 390, GetButtonID(0, 0), 1060674); // Banish
                AddButtonLabeled(10, 410, GetButtonID(0, 1), 1011261); // Lift a Ban
            }
            else
            {
                AddButtonLabeled(10, 390, GetButtonID(0, 2), 1060676); // Grant Access
                AddButtonLabeled(10, 410, GetButtonID(0, 3), 1060677); // Revoke Access
            }

            AddPageButton(150, 10, GetButtonID(1, 0), 1060668, HouseGumpPageAOS.Information);
            AddPageButton(150, 30, GetButtonID(1, 1), 1060669, HouseGumpPageAOS.Security);
            AddPageButton(150, 50, GetButtonID(1, 2), 1060670, HouseGumpPageAOS.Storage);
            AddPageButton(150, 70, GetButtonID(1, 3), 1060671, HouseGumpPageAOS.Customize);
            AddPageButton(150, 90, GetButtonID(1, 4), 1060672, HouseGumpPageAOS.Ownership);

            switch (page)
            {
                case HouseGumpPageAOS.Information:
                    {
                        AddHtmlLocalized(20, 130, 200, 20, 1011242, LabelColor, false, false); // Owned By: 
                        AddLabel(210, 130, LabelHue, GetOwnerName());

                        AddHtmlLocalized(20, 170, 380, 20, 1018032, SelectedColor, false, false); // This house is properly placed.
                        AddHtmlLocalized(20, 190, 380, 20, 1018035, SelectedColor, false, false); // This house is of modern design.
                        AddHtmlLocalized(20, 210, 380, 20, (house is HouseFoundation) ? 1060681 : 1060680, SelectedColor, false, false); // This is a (pre | custom)-built house.
                        AddHtmlLocalized(20, 230, 380, 20, house.Public ? 1060678 : 1060679, SelectedColor, false, false); // This house is (private | open to the public).
                        AddHtmlLocalized(20, 250, 380, 20, 1062209, SelectedColor, false, false); // This house is <a href = "?ForceTopic97">Automatically</a> refreshed.

                        AddHtmlLocalized(20, 290, 200, 20, 1060692, SelectedColor, false, false); // Built On:
                        AddLabel(250, 290, LabelHue, GetDateTime(house.BuiltOn));

                        AddHtmlLocalized(20, 310, 200, 20, 1060693, SelectedColor, false, false); // Last Traded:
                        AddLabel(250, 310, LabelHue, GetDateTime(house.LastTraded));

                        AddHtmlLocalized(20, 330, 200, 20, 1061793, SelectedColor, false, false); // House Value
                        AddLabel(250, 330, LabelHue, house.Price.ToString());

                        AddHtmlLocalized(20, 360, 300, 20, 1011241, SelectedColor, false, false); // Number of visits this building has had: 
                        AddLabel(350, 360, LabelHue, house.Visits.ToString());

                        break;
                    }
                case HouseGumpPageAOS.Security:
                    {
                        AddButtonLabeled(10, 130, GetButtonID(3, 0), 1011266, isCoOwner); // View Co-Owner List
                        AddButtonLabeled(10, 150, GetButtonID(3, 1), 1011267, isOwner); // Add a Co-Owner
                        AddButtonLabeled(10, 170, GetButtonID(3, 2), 1018036, isOwner); // Remove a Co-Owner
                        AddButtonLabeled(10, 190, GetButtonID(3, 3), 1011268, isOwner); // Clear Co-Owner List

                        AddButtonLabeled(10, 220, GetButtonID(3, 4), 1011243); // View Friends List
                        AddButtonLabeled(10, 240, GetButtonID(3, 5), 1011244, isCoOwner); // Add a Friend
                        AddButtonLabeled(10, 260, GetButtonID(3, 6), 1018037, isCoOwner); // Remove a Friend
                        AddButtonLabeled(10, 280, GetButtonID(3, 7), 1011245, isCoOwner); // Clear Friend List

                        if (house.Public)
                        {
                            AddButtonLabeled(10, 310, GetButtonID(3, 8), 1011260); // View Ban List
                            AddButtonLabeled(10, 330, GetButtonID(3, 9), 1060698); // Clear Ban List

                            //						AddButtonLabeled( 210, 130, GetButtonID( 3, 12 ), 1060695, isOwner ); // Make This House Private
                        }
                        else
                        {
                            AddButtonLabeled(10, 310, GetButtonID(3, 10), 1060699); // View Access List
                            AddButtonLabeled(10, 330, GetButtonID(3, 11), 1060700); // Clear Access List

                            AddButtonLabeled(210, 130, GetButtonID(3, 13), 1060694, isOwner); // Make This House Public
                        }

                        break;
                    }
                case HouseGumpPageAOS.Storage:
                    {
                        AddHtmlLocalized(10, 130, 400, 20, 1060682, LabelColor, false, false); // <CENTER>HOUSE STORAGE SUMMARY</CENTER>

                        // This is not as OSI; storage changes not yet implemented

                        /*AddHtmlLocalized( 10, 170, 275, 20, 1011237, LabelColor, false, false ); // Number of locked down items:
						AddLabel( 310, 170, LabelHue, m_House.LockDownCount.ToString() );

						AddHtmlLocalized( 10, 190, 275, 20, 1011238, LabelColor, false, false ); // Maximum locked down items:
						AddLabel( 310, 190, LabelHue, m_House.MaxLockDowns.ToString() );

						AddHtmlLocalized( 10, 210, 275, 20, 1011239, LabelColor, false, false ); // Number of secure containers:
						AddLabel( 310, 210, LabelHue, m_House.SecureCount.ToString() );

						AddHtmlLocalized( 10, 230, 275, 20, 1011240, LabelColor, false, false ); // Maximum number of secure containers:
						AddLabel( 310, 230, LabelHue, m_House.MaxSecures.ToString() );*/

                        int fromSecures, fromVendors, fromLockdowns;

                        int maxSecures = house.GetAosMaxSecures();
                        int curSecures = house.GetAosCurSecures(out fromSecures, out fromVendors, out fromLockdowns);

                        int maxLockdowns = house.GetAosMaxLockdowns();
                        int curLockdowns = house.GetAosCurLockdowns();

                        AddHtmlLocalized(10, 170, 300, 20, 1060683, LabelColor, false, false); // Maximum Secure Storage
                        AddLabel(310, 170, LabelHue, maxSecures.ToString());

                        AddHtmlLocalized(10, 190, 300, 20, 1060685, LabelColor, false, false); // Used by Moving Crate
                        AddLabel(310, 190, LabelHue, "0");

                        AddHtmlLocalized(10, 210, 300, 20, 1060686, LabelColor, false, false); // Used by Lockdowns
                        AddLabel(310, 210, LabelHue, fromLockdowns.ToString());

                        AddHtmlLocalized(10, 230, 300, 20, 1060687, LabelColor, false, false); // Used by Vendors
                        AddLabel(310, 230, LabelHue, fromVendors.ToString());

                        AddHtmlLocalized(10, 250, 300, 20, 1060688, LabelColor, false, false); // Used by Secure Containers
                        AddLabel(310, 250, LabelHue, fromSecures.ToString());

                        AddHtmlLocalized(10, 270, 300, 20, 1060689, LabelColor, false, false); // Available Storage
                        AddLabel(310, 270, LabelHue, Math.Max(maxSecures - curSecures, 0).ToString());

                        AddHtmlLocalized(10, 330, 300, 20, 1060690, LabelColor, false, false); // Maximum Lockdowns
                        AddLabel(310, 330, LabelHue, maxLockdowns.ToString());

                        AddHtmlLocalized(10, 350, 300, 20, 1060691, LabelColor, false, false); // Available Lockdowns
                        AddLabel(310, 350, LabelHue, Math.Max(maxLockdowns - curLockdowns, 0).ToString());

                        break;
                    }
                case HouseGumpPageAOS.Customize:
                    {
                        bool isCustomizable = isOwner && (house is HouseFoundation);

                        AddButtonLabeled(10, 120, GetButtonID(5, 0), 1060759, isOwner && !isCustomizable && (house.ConvertEntry != null)); // Convert Into Customizable House
                        AddButtonLabeled(10, 160, GetButtonID(5, 1), 1060765, isOwner && isCustomizable); // Customize This House
                        AddButtonLabeled(10, 180, GetButtonID(5, 2), 1060760, false); // Relocate Moving Crate
                        AddButtonLabeled(10, 210, GetButtonID(5, 3), 1060761, isOwner && house.Public); // Change House Sign
                        AddButtonLabeled(10, 230, GetButtonID(5, 4), 1060762, isOwner && isCustomizable); // Change House Sign Hanger
                        AddButtonLabeled(10, 250, GetButtonID(5, 5), 1060763, isOwner && isCustomizable && (((HouseFoundation)house).Signpost != null)); // Change Signpost
                        AddButtonLabeled(10, 280, GetButtonID(5, 6), 1062004, isOwner && isCustomizable); // Change Foundation Style
                        AddButtonLabeled(10, 310, GetButtonID(5, 7), 1060764, isCoOwner); // Rename House

                        break;
                    }
                case HouseGumpPageAOS.Ownership:
                    {
                        AddButtonLabeled(10, 130, GetButtonID(6, 0), 1061794, isOwner); // Demolish House
                        AddButtonLabeled(10, 150, GetButtonID(6, 1), 1061797, isOwner); // Trade House
                        AddButtonLabeled(10, 190, GetButtonID(6, 2), 1061798, false); // Make Primary

                        break;
                    }
                case HouseGumpPageAOS.ChangeHanger:
                    {
                        for (int i = 0; i < m_HangerNumbers.Length; ++i)
                        {
                            int x = 50 + ((i % 3) * 100);
                            int y = 180 + ((i / 3) * 80);

                            AddButton(x, y, 4005, 4007, GetButtonID(7, i), GumpButtonType.Reply, 0);
                            AddItem(x + 20, y, m_HangerNumbers[i]);
                        }

                        break;
                    }
                case HouseGumpPageAOS.ChangeFoundation:
                    {
                        for (int i = 0; i < m_FoundationNumbers.Length; ++i)
                        {
                            int x = 15 + ((i % 5) * 80);
                            int y = 180 + ((i / 5) * 100);

                            AddButton(x, y, 4005, 4007, GetButtonID(8, i), GumpButtonType.Reply, 0);
                            AddItem(x + 25, y, m_FoundationNumbers[i]);
                        }

                        break;
                    }
                case HouseGumpPageAOS.ChangeSign:
                    {
                        int index = 0;

                        for (int i = 0; i < 3; ++i)
                        {
                            AddPage(i + 1);

                            AddButton(10, 360, 4005, 4007, 0, GumpButtonType.Page, ((i + 1) % 3) + 1);

                            for (int j = 0; j < 18; ++j)
                            {
                                int x = 30 + ((j % 6) * 60);
                                int y = 130 + ((j / 6) * 60);

                                AddButton(x, y, 4005, 4007, GetButtonID(9, index), GumpButtonType.Reply, 0);
                                AddItem(x + 20, y, 2980 + (index++ * 2));
                            }
                        }

                        break;
                    }
                case HouseGumpPageAOS.RemoveCoOwner:
                    {
                        AddHtmlLocalized(10, 120, 400, 20, 1060730, LabelColor, false, false); // <CENTER>CO-OWNER LIST</CENTER>
                        AddList(house.CoOwners, 10, false);
                        break;
                    }
                case HouseGumpPageAOS.ListCoOwner:
                    {
                        AddHtmlLocalized(10, 120, 400, 20, 1060730, LabelColor, false, false); // <CENTER>CO-OWNER LIST</CENTER>
                        AddList(house.CoOwners, -1, false);
                        break;
                    }
                case HouseGumpPageAOS.RemoveFriend:
                    {
                        AddHtmlLocalized(10, 120, 400, 20, 1060731, LabelColor, false, false); // <CENTER>FRIENDS LIST</CENTER>
                        AddList(house.Friends, 11, false);
                        break;
                    }
                case HouseGumpPageAOS.ListFriend:
                    {
                        AddHtmlLocalized(10, 120, 400, 20, 1060731, LabelColor, false, false); // <CENTER>FRIENDS LIST</CENTER>
                        AddList(house.Friends, -1, false);
                        break;
                    }
                case HouseGumpPageAOS.RemoveBan:
                    {
                        AddHtmlLocalized(10, 120, 400, 20, 1060733, LabelColor, false, false); // <CENTER>BAN LIST</CENTER>
                        AddList(house.Bans, 12, true);
                        break;
                    }
                case HouseGumpPageAOS.ListBan:
                    {
                        AddHtmlLocalized(10, 120, 400, 20, 1060733, LabelColor, false, false); // <CENTER>BAN LIST</CENTER>
                        AddList(house.Bans, -1, true);
                        break;
                    }
                case HouseGumpPageAOS.RemoveAccess:
                    {
                        AddHtmlLocalized(10, 120, 400, 20, 1060732, LabelColor, false, false); // <CENTER>ACCESS LIST</CENTER>
                        AddList(house.Access, 13, false);
                        break;
                    }
                case HouseGumpPageAOS.ListAccess:
                    {
                        AddHtmlLocalized(10, 120, 400, 20, 1060732, LabelColor, false, false); // <CENTER>ACCESS LIST</CENTER>
                        AddList(house.Access, -1, false);
                        break;
                    }
                case HouseGumpPageAOS.ChangePost:
                    {
                        int index = 0;

                        for (int i = 0; i < 2; ++i)
                        {
                            AddPage(i + 1);

                            AddButton(10, 360, 4005, 4007, 0, GumpButtonType.Page, ((i + 1) % 2) + 1);

                            for (int j = 0; j < 16 && index < m_PostNumbers.Length; ++j)
                            {
                                int x = 15 + ((j % 8) * 50);
                                int y = 130 + ((j / 8) * 110);

                                AddButton(x, y, 4005, 4007, GetButtonID(14, index), GumpButtonType.Reply, 0);
                                AddItem(x + 10, y, m_PostNumbers[index++]);
                            }
                        }

                        break;
                    }
            }
        }

        public static void PublicPrivateNotice_Callback(Mobile from, object state)
        {
            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, (BaseHouse)state));
        }

        public static void ClearCoOwners_Callback(Mobile from, bool okay, object state)
        {
            BaseHouse house = (BaseHouse)state;

            if (okay && house.IsOwner(from))
            {
                if (house.CoOwners != null)
                    house.CoOwners.Clear();

                from.SendLocalizedMessage(501333); // All co-owners have been removed from this house.
            }

            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, house));
        }

        public static void ClearFriends_Callback(Mobile from, bool okay, object state)
        {
            BaseHouse house = (BaseHouse)state;

            if (okay && house.IsCoOwner(from))
            {
                if (house.Friends != null)
                    house.Friends.Clear();

                from.SendLocalizedMessage(501332); // All friends have been removed from this house.
            }

            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, house));
        }

        public static void ClearBans_Callback(Mobile from, bool okay, object state)
        {
            BaseHouse house = (BaseHouse)state;

            if (okay && house.IsFriend(from))
            {
                if (house.Bans != null)
                    house.Bans.Clear();

                from.SendLocalizedMessage(1060754); // All bans for this house have been lifted.
            }

            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, house));
        }

        public static void ClearAccess_Callback(Mobile from, bool okay, object state)
        {
            BaseHouse house = (BaseHouse)state;

            if (okay && house.IsFriend(from))
            {
                ArrayList list = new ArrayList(house.Access);

                if (house.Access != null)
                    house.Access.Clear();

                for (int i = 0; i < list.Count; ++i)
                {
                    Mobile m = (Mobile)list[i];

                    if (!house.HasAccess(m) && house.IsInside(m))
                    {
                        m.Location = house.BanLocation;
                        m.SendLocalizedMessage(1060734); // Your access to this house has been revoked.
                    }
                }

                from.SendLocalizedMessage(1061843); // This house's Access List has been cleared.
            }

            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, house));
        }

        public static void ConvertHouse_Callback(Mobile from, bool okay, object state)
        {
            BaseHouse house = (BaseHouse)state;

            if (okay && house.IsOwner(from))
            {
                HousePlacementEntry e = house.ConvertEntry;

                if (e != null)
                {
                    int cost = e.Cost - house.Price;

                    if (cost > 0)
                    {
                        if (Banker.CombinedWithdrawFromAllEnrolled(from, cost))
                        {
                            from.SendLocalizedMessage(1060398, cost.ToString()); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                        }
                        else
                        {
                            from.SendLocalizedMessage(1061624); // You do not have enough funds in your bank to cover the difference between your old house and your new one.
                            return;
                        }
                    }
                    else if (cost < 0)
                    {
                        if (Banker.Deposit(from, -cost))
                            from.SendLocalizedMessage(1060397, (-cost).ToString()); // ~1_AMOUNT~ gold has been deposited into your bank box.
                        else
                            return;
                    }

                    BaseHouse newHouse = e.ConstructHouse(from);

                    if (newHouse != null)
                    {
                        newHouse.Price = e.Cost;

                        newHouse.Friends = new ArrayList(house.Friends);
                        newHouse.CoOwners = new ArrayList(house.CoOwners);
                        newHouse.Bans = new ArrayList(house.Bans);
                        newHouse.Access = new ArrayList(house.Access);
                        newHouse.Addons = new ArrayList(house.Addons);
                        newHouse.BuiltOn = house.BuiltOn;
                        newHouse.LastTraded = house.LastTraded;

                        newHouse.MoveToWorld(new Point3D(house.X + house.ConvertOffsetX, house.Y + house.ConvertOffsetY, house.Z + house.ConvertOffsetZ), house.Map);
                        house.Delete();

                        /* You have successfully replaced your original house with a new house.
						 * The value of the replaced house has been deposited into your bank box.
						 * All of the items in your original house have been relocated to a Moving Crate in the new house.
						 * Any deed-based house add-ons have been converted back into deeds.
						 * Vendors and barkeeps in the house, if any, have been stored in the Moving Crate as well.
						 * Use the <B>Get Vendor</B> context-sensitive menu option on your character to retrieve them.
						 * These containers can be used to re-create the vendor in a new location.
						 * Any barkeepers have been converted into deeds.
						 */
                        from.SendGump(new NoticeGump(1060637, 30720, 1060012, 32512, 420, 280, null, null));
                        return;
                    }
                }
            }

            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, house));
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_House.Deleted)
                return;

            Mobile from = sender.Mobile;

            bool isOwner = m_House.IsOwner(from);
            bool isCoOwner = isOwner || m_House.IsCoOwner(from);
            bool isFriend = isCoOwner || m_House.IsFriend(from);

            if (!isFriend || !from.CheckAlive())
                return;

            Item sign = m_House.Sign;

            if (sign == null || from.Map != sign.Map || !from.InRange(sign.GetWorldLocation(), 18))
                return;

            HouseFoundation foundation = m_House as HouseFoundation;
            bool isCustomizable = (foundation != null);

            int val = info.ButtonID - 1;

            if (val < 0)
                return;

            int type = val % 15;
            int index = val / 15;

            switch (type)
            {
                case 0:
                    {
                        switch (index)
                        {
                            case 0: // Banish
                                {
                                    if (m_House.Public)
                                    {
                                        from.SendLocalizedMessage(501325); // Target the individual to ban from this house.
                                        from.Target = new HouseBanTarget(true, m_House);
                                    }

                                    break;
                                }
                            case 1: // Lift Ban
                                {
                                    if (m_House.Public)
                                        from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.RemoveBan, from, m_House));

                                    break;
                                }
                            case 2: // Grant Access
                                {
                                    if (!m_House.Public)
                                    {
                                        from.SendLocalizedMessage(1060711); // Target the person you would like to grant access to.
                                        from.Target = new HouseAccessTarget(m_House);
                                    }

                                    break;
                                }
                            case 3: // Revoke Access
                                {
                                    if (!m_House.Public)
                                        from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.RemoveAccess, from, m_House));

                                    break;
                                }
                        }

                        break;
                    }
                case 1:
                    {
                        HouseGumpPageAOS page;

                        switch (index)
                        {
                            case 0: page = HouseGumpPageAOS.Information; break;
                            case 1: page = HouseGumpPageAOS.Security; break;
                            case 2: page = HouseGumpPageAOS.Storage; break;
                            case 3: page = HouseGumpPageAOS.Customize; break;
                            case 4: page = HouseGumpPageAOS.Ownership; break;
                            default: return;
                        }

                        from.SendGump(new HouseGumpAOS(page, from, m_House));
                        break;
                    }
                case 3:
                    {
                        switch (index)
                        {
                            case 0: // View Co-Owner List
                                {
                                    if (isCoOwner)
                                        from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.ListCoOwner, from, m_House));

                                    break;
                                }
                            case 1: // Add a Co-Owner
                                {
                                    if (isOwner)
                                    {
                                        from.SendLocalizedMessage(501328); // Target the person you wish to name a co-owner of your household.
                                        from.Target = new CoOwnerTarget(true, m_House);
                                    }

                                    break;
                                }
                            case 2: // Remove a Co-Owner
                                {
                                    if (isOwner)
                                        from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.RemoveCoOwner, from, m_House));

                                    break;
                                }
                            case 3: // Clear Co-Owner List
                                {
                                    if (isOwner)
                                        from.SendGump(new WarningGump(1060635, 30720, 1060736, 32512, 420, 280, new WarningGumpCallback(ClearCoOwners_Callback), m_House));

                                    break;
                                }
                            case 4: // View Friends List
                                {
                                    from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.ListFriend, from, m_House));

                                    break;
                                }
                            case 5: // Add a Friend
                                {
                                    if (isCoOwner)
                                    {
                                        from.SendLocalizedMessage(501317); // Target the person you wish to name a friend of your household.
                                        from.Target = new HouseFriendTarget(true, m_House);
                                    }

                                    break;
                                }
                            case 6: // Remove a Friend
                                {
                                    if (isCoOwner)
                                        from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.RemoveFriend, from, m_House));

                                    break;
                                }
                            case 7: // Clear Friend List
                                {
                                    if (isCoOwner)
                                        from.SendGump(new WarningGump(1060635, 30720, 1018039, 32512, 420, 280, new WarningGumpCallback(ClearFriends_Callback), m_House));

                                    break;
                                }
                            case 8: // View Ban List
                                {
                                    from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.ListBan, from, m_House));

                                    break;
                                }
                            case 9: // Clear Ban List
                                {
                                    from.SendGump(new WarningGump(1060635, 30720, 1060753, 32512, 420, 280, new WarningGumpCallback(ClearBans_Callback), m_House));

                                    break;
                                }
                            case 10: // View Access List
                                {
                                    from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.ListAccess, from, m_House));

                                    break;
                                }
                            case 11: // Clear Access List
                                {
                                    from.SendGump(new WarningGump(1060635, 30720, 1061842, 32512, 420, 280, new WarningGumpCallback(ClearAccess_Callback), m_House));

                                    break;
                                }
                            //						case 12: // Make Private
                            //						{
                            //							if ( isOwner )
                            //							{
                            //								if ( m_House.FindPlayerVendor() != null )
                            //								{
                            //									// You have vendors working out of this building. It cannot be declared private until there are no vendors in place.
                            //									from.SendGump( new NoticeGump( 1060637, 30720, 501887, 32512, 320, 180, new NoticeGumpCallback( PublicPrivateNotice_Callback ), m_House ) );
                            //									break;
                            //								}
                            //
                            //								m_House.Public = false;
                            //
                            //								m_House.ChangeLocks( from );
                            //
                            //								// This house is now private.
                            //								from.SendGump( new NoticeGump( 1060637, 30720, 501888, 32512, 320, 180, new NoticeGumpCallback( PublicPrivateNotice_Callback ), m_House ) );
                            //
                            //								Region r = m_House.Region;
                            //								ArrayList list = new ArrayList( r.Mobiles );
                            //
                            //								for ( int i = 0; i < list.Count; ++i )
                            //								{
                            //									Mobile m = (Mobile)list[i];
                            //
                            //									if ( !m_House.HasAccess( m ) && m_House.IsInside( m ) )
                            //										m.Location = m_House.BanLocation;
                            //								}
                            //							}
                            //
                            //							break;
                            //						}
                            case 13: // Make Public
                                {
                                    if (isOwner)
                                    {
                                        //								m_House.Public = true;

                                        m_House.RemoveKeys(from);
                                        m_House.RemoveLocks();

                                        // This house is now public. Friends of the house may now have vendors working out of this building.
                                        from.SendGump(new NoticeGump(1060637, 30720, 501886, 32512, 320, 180, new NoticeGumpCallback(PublicPrivateNotice_Callback), m_House));

                                        Region r = m_House.Region;
                                        ArrayList list = new ArrayList(r.Mobiles.Values);

                                        for (int i = 0; i < list.Count; ++i)
                                        {
                                            Mobile m = (Mobile)list[i];

                                            if (m_House.IsBanned(m) && m_House.IsInside(m))
                                                m.Location = m_House.BanLocation;
                                        }
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 5:
                    {
                        switch (index)
                        {
                            case 0: // Convert Into Customizable House
                                {
                                    if (isOwner && !isCustomizable)
                                    {
                                        HousePlacementEntry e = m_House.ConvertEntry;

                                        if (e != null)
                                        {
                                            /* You are about to turn your house into a customizable house.
											 * You will be refunded the value of this house, and then be charged the cost of the equivalent customizable dirt lot.
											 * All of your possessions in the house will be transported to a Moving Crate.
											 * Deed-based house add-ons will be converted back into deeds.
											 * Vendors and barkeeps will also be stored in the Moving Crate.
											 * Your house will be leveled to its foundation, and you will be able to build new walls, windows, doors, and stairs.
											 * Are you sure you wish to continue?
											 */
                                            from.SendGump(new WarningGump(1060635, 30720, 1060013, 32512, 420, 280, new WarningGumpCallback(ConvertHouse_Callback), m_House));
                                        }
                                    }

                                    break;
                                }
                            case 1: // Customize This House
                                {
                                    if (isOwner && isCustomizable)
                                        foundation.BeginCustomize(from);

                                    break;
                                }
                            case 2: // Relocate Moving Crate
                                break;
                            case 3: // Change House Sign
                                {
                                    if (isOwner && m_House.Public)
                                        from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.ChangeSign, from, m_House));

                                    break;
                                }
                            case 4: // Change House Sign Hanger
                                {
                                    if (isOwner && isCustomizable)
                                        from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.ChangeHanger, from, m_House));

                                    break;
                                }
                            case 5: // Change Signpost
                                {
                                    if (isOwner && isCustomizable && foundation.Signpost != null)
                                        from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.ChangePost, from, m_House));

                                    break;
                                }
                            case 6: // Change Foundation Style
                                {
                                    if (isOwner && isCustomizable)
                                        from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.ChangeFoundation, from, m_House));

                                    break;
                                }
                            case 7: // Rename House
                                {
                                    if (isCoOwner)
                                    {
                                        from.Prompt = new RenamePrompt(m_House);
                                        from.SendLocalizedMessage(501302); // What dost thou wish the sign to say?
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 6:
                    {
                        switch (index)
                        {
                            case 0: // Demolish
                                {
                                    if (isOwner)
                                    {
                                        if (m_House.FindGuildstone() != null)
                                            from.SendLocalizedMessage(501389); // You cannot redeed a house with a guildstone inside.
                                        else
                                            from.SendGump(new HouseDemolishGump(from, m_House));
                                    }

                                    break;
                                }
                            case 1: // Trade House
                                {
                                    if (isOwner)
                                    {
                                        from.SendLocalizedMessage(501309); // Target the person to whom you wish to give this house.
                                        from.Target = new HouseOwnerTarget(m_House);
                                    }

                                    break;
                                }
                            case 2: // Make Primary
                                break;
                        }

                        break;
                    }
                case 7:
                    {
                        if (isOwner && isCustomizable && index >= 0 && index < m_HangerNumbers.Length)
                        {
                            Item hanger = foundation.SignHanger;

                            if (hanger != null)
                                hanger.ItemID = m_HangerNumbers[index];

                            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Customize, from, m_House));
                        }

                        break;
                    }
                case 8:
                    {
                        if (isOwner && isCustomizable)
                        {
                            FoundationType newType;

                            switch (index)
                            {
                                case 0: newType = FoundationType.DarkWood; break;
                                case 1: newType = FoundationType.LightWood; break;
                                case 2: newType = FoundationType.Dungeon; break;
                                case 3: newType = FoundationType.Brick; break;
                                case 4: newType = FoundationType.Stone; break;
                                default: return;
                            }

                            foundation.Type = newType;

                            DesignState state = foundation.BackupState;
                            HouseFoundation.ApplyFoundation(newType, state.Components);
                            state.OnRevised();

                            state = foundation.DesignState;
                            HouseFoundation.ApplyFoundation(newType, state.Components);
                            state.OnRevised();

                            state = foundation.CurrentState;
                            HouseFoundation.ApplyFoundation(newType, state.Components);
                            state.OnRevised();

                            foundation.Delta(ItemDelta.Update);

                            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Customize, from, m_House));
                        }

                        break;
                    }
                case 9:
                    {
                        if (isOwner && m_House.Public && index >= 0 && index < 54)
                        {
                            m_House.ChangeSignType(2980 + (index * 2));
                            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Customize, from, m_House));
                        }

                        break;
                    }
                case 10:
                    {
                        if (isOwner && m_List != null && index >= 0 && index < m_List.Count)
                        {
                            m_House.RemoveCoOwner(from, (Mobile)m_List[index]);

                            if (m_List.Count > 0)
                                from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.RemoveCoOwner, from, m_House));
                            else
                                from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, m_House));
                        }

                        break;
                    }
                case 11:
                    {
                        if (isCoOwner && m_List != null && index >= 0 && index < m_List.Count)
                        {
                            m_House.RemoveFriend(from, (Mobile)m_List[index]);

                            if (m_List.Count > 0)
                                from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.RemoveFriend, from, m_House));
                            else
                                from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, m_House));
                        }

                        break;
                    }
                case 12:
                    {
                        if (m_List != null && index >= 0 && index < m_List.Count)
                        {
                            m_House.RemoveBan(from, (Mobile)m_List[index]);

                            if (m_List.Count > 0)
                                from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.RemoveBan, from, m_House));
                            else
                                from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, m_House));
                        }

                        break;
                    }
                case 13:
                    {
                        if (m_List != null && index >= 0 && index < m_List.Count)
                        {
                            m_House.RemoveAccess(from, (Mobile)m_List[index]);

                            if (m_List.Count > 0)
                                from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.RemoveAccess, from, m_House));
                            else
                                from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Security, from, m_House));
                        }

                        break;
                    }
                case 14:
                    {
                        if (isOwner && isCustomizable && index >= 0 && index < m_PostNumbers.Length)
                        {
                            foundation.SignpostGraphic = m_PostNumbers[index];
                            foundation.CheckSignpost();

                            from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Customize, from, m_House));
                        }

                        break;
                    }
            }
        }

        private ArrayList Wrap(string value)
        {
            if (value == null || (value = value.Trim()).Length <= 0)
                return null;

            string[] values = value.Split(' ');
            ArrayList list = new ArrayList();
            string current = "";

            for (int i = 0; i < values.Length; ++i)
            {
                string val = values[i];

                string v = current.Length == 0 ? val : current + ' ' + val;

                if (v.Length < 10)
                {
                    current = v;
                }
                else if (v.Length == 10)
                {
                    list.Add(v);

                    if (list.Count == 6)
                        return list;

                    current = "";
                }
                else if (val.Length <= 10)
                {
                    list.Add(current);

                    if (list.Count == 6)
                        return list;

                    current = val;
                }
                else
                {
                    while (v.Length >= 10)
                    {
                        list.Add(v.Substring(0, 10));

                        if (list.Count == 6)
                            return list;

                        v = v.Substring(10);
                    }

                    current = v;
                }
            }

            if (current.Length > 0)
                list.Add(current);

            return list;
        }
    }
}