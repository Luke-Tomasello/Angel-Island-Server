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

/* Scripts\Engines\Craft\Core\CraftGump.cs
 *	HANGE LOG
 *  1/9/22, Yoar
 *      Reverted changes by Sam to go back to OSI appearance.
 *      Added m_NumLines field to fit more than 10 craft groups.
 *  12/16/21, Yoar
 *      Generalized "Resmelt" option into "Recycle" option.
 *  11/17/21, Yoar
 *      - Now displaying the player's resource count in the resource selection.
 *      - Converted labeled text into html text.
 * 04,arpil,2004 edited gumps and uploaded gump changed for carp/woodworking lines 70-76 gump changes 
 * 07,april,2004 edited lines 70-76 again to refix the gumps i screwed up in the first place :)
 */

using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections;
using CI = Server.Engines.Craft.CraftItem;

namespace Server.Engines.Craft
{
    public class CraftGump : Gump
    {
        private Mobile m_From;
        private CraftSystem m_CraftSystem;
        private BaseTool m_Tool;

        private CraftPage m_Page;

        private int m_NumLines;

        private const int LabelColor = 0x7FFF;
        private const int FontColor = 0xFFFFFF;

        private enum CraftPage
        {
            None,
            PickResource,
            PickResource2
        }

        /*public CraftGump( Mobile from, CraftSystem craftSystem, BaseTool tool ): this( from, craftSystem, -1, -1, tool, null )
		{
		}*/

        public CraftGump(Mobile from, CraftSystem craftSystem, BaseTool tool, TextDefinition notice)
            : this(from, craftSystem, tool, notice, CraftPage.None)
        {
        }

        private CraftGump(Mobile from, CraftSystem craftSystem, BaseTool tool, TextDefinition notice, CraftPage page)
            : base(40, 40)
        {
            m_From = from;
            m_CraftSystem = craftSystem;
            m_Tool = tool;
            m_Page = page;
            m_NumLines = Math.Max(10, m_CraftSystem.CraftGroups.Count);

            CraftContext context = craftSystem.GetContext(from);

            from.CloseGump(typeof(CraftGump));
            from.CloseGump(typeof(CraftGumpItem));

            AddPage(0);

            AddBackground(0, 0, 530, 237 + 20 * m_NumLines, 5054);
            AddImageTiled(10, 10, 510, 22, 2624);
            AddImageTiled(10, 92 + 20 * m_NumLines, 150, 45, 2624);
            AddImageTiled(165, 92 + 20 * m_NumLines, 355, 45, 2624);
            AddImageTiled(10, 142 + 20 * m_NumLines, 510, 85, 2624);
            AddImageTiled(10, 37, 200, 50 + 20 * m_NumLines, 2624);
            AddImageTiled(215, 37, 305, 50 + 20 * m_NumLines, 2624);
            AddAlphaRegion(10, 10, 510, 217 + 20 * m_NumLines);

            TextDefinition.AddHtmlText(this, 10, 12, 510, 20, craftSystem.GumpTitle, false, false, LabelColor, FontColor);

            AddHtmlLocalized(10, 37, 200, 22, 1044010, LabelColor, false, false); // <CENTER>CATEGORIES</CENTER>
            AddHtmlLocalized(215, 37, 305, 22, 1044011, LabelColor, false, false); // <CENTER>SELECTIONS</CENTER>
            AddHtmlLocalized(10, 102 + 20 * m_NumLines, 150, 25, 1044012, LabelColor, false, false); // <CENTER>NOTICES</CENTER>

            AddButton(15, 202 + 20 * m_NumLines, 4017, 4019, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(50, 205 + 20 * m_NumLines, 150, 18, 1011441, LabelColor, false, false); // EXIT

            AddButton(270, 202 + 20 * m_NumLines, 4005, 4007, GetButtonID(6, 2), GumpButtonType.Reply, 0);
            AddHtmlLocalized(305, 205 + 20 * m_NumLines, 150, 18, 1044013, LabelColor, false, false); // MAKE LAST

            // Mark option
            if (craftSystem.MarkOption)
            {
                AddButton(270, 162 + 20 * m_NumLines, 4005, 4007, GetButtonID(6, 6), GumpButtonType.Reply, 0);
                AddHtmlLocalized(305, 165 + 20 * m_NumLines, 150, 18, 1044017 + (context == null ? 0 : (int)context.MarkOption), LabelColor, false, false); // MARK ITEM
            }
            // ****************************************

            // Recycle option
            if (craftSystem.Recycle != null)
            {
                AddButton(15, 142 + 20 * m_NumLines, 4005, 4007, GetButtonID(6, 1), GumpButtonType.Reply, 0);

                if (craftSystem == DefBlacksmithy.CraftSystem)
                    AddHtmlLocalized(50, 145 + 20 * m_NumLines, 150, 18, 1044259, LabelColor, false, false); // SMELT ITEM
                else
                    AddHtml(50, 145 + 20 * m_NumLines, 150, 18, Color("RECYCLE ITEM"), false, false);
            }
            // ****************************************

            // Repair option
            if (craftSystem.Repair)
            {
                AddButton(270, 142 + 20 * m_NumLines, 4005, 4007, GetButtonID(6, 5), GumpButtonType.Reply, 0);
                AddHtmlLocalized(305, 145 + 20 * m_NumLines, 150, 18, 1044260, LabelColor, false, false); // REPAIR ITEM
            }
            // ****************************************

            // Enhance option
            if (craftSystem.CanEnhance)
            {
                AddButton(270, 182 + 20 * m_NumLines, 4005, 4007, GetButtonID(6, 8), GumpButtonType.Reply, 0);
                AddHtmlLocalized(305, 185 + 20 * m_NumLines, 150, 18, 1061001, LabelColor, false, false); // ENHANCE ITEM
            }
            // ****************************************

            TextDefinition.AddHtmlText(this, 170, 95 + m_NumLines * 20, 350, 40, notice, false, false, LabelColor, FontColor);

            // If the system has more than one resource
            if (craftSystem.CraftSubRes.Init)
            {
                TextDefinition name = craftSystem.CraftSubRes.Name;

                int resIndex = (context == null ? -1 : context.LastResourceIndex);

                Type resourceType = craftSystem.CraftSubRes.ResType;

                if (resIndex > -1)
                {
                    CraftSubRes subResource = craftSystem.CraftSubRes.GetAt(resIndex);

                    name = subResource.Name;
                    resourceType = subResource.ItemType;
                }

                int resourceCount = 0;

                if (from.Backpack != null)
                {
                    Item[] items = from.Backpack.FindItemsByType(CI.GetEquivalentResources(resourceType), true);

                    for (int i = 0; i < items.Length; ++i)
                        resourceCount += items[i].Amount;
                }

                AddButton(15, 162 + 20 * m_NumLines, 4005, 4007, GetButtonID(6, 0), GumpButtonType.Reply, 0);

                if (context.DoNotColor)
                    AddHtmlLocalized(50, 165 + 20 * m_NumLines, 250, 18, 1061592, LabelColor, false, false); // *

                if (name.Number > 0)
                    AddHtmlLocalized(context.DoNotColor ? 60 : 50, 165 + 20 * m_NumLines, 250, 18, name.Number, resourceCount.ToString(), LabelColor, false, false);
                else if (!String.IsNullOrEmpty(name.String))
                    AddHtml(context.DoNotColor ? 60 : 50, 165 + 20 * m_NumLines, 250, 18, Color(String.Format("{0} ({1})", name.String, resourceCount.ToString())), false, false);
            }
            // ****************************************

            // For dragon scales
            if (craftSystem.CraftSubRes2.Init)
            {
                TextDefinition name = craftSystem.CraftSubRes2.Name;

                int resIndex = (context == null ? -1 : context.LastResourceIndex2);

                Type resourceType = craftSystem.CraftSubRes2.ResType;

                if (resIndex > -1)
                {
                    CraftSubRes subResource = craftSystem.CraftSubRes2.GetAt(resIndex);

                    name = subResource.Name;
                    resourceType = subResource.ItemType;
                }

                int resourceCount = 0;

                if (from.Backpack != null)
                {
                    Item[] items = from.Backpack.FindItemsByType(CI.GetEquivalentResources(resourceType), true);

                    for (int i = 0; i < items.Length; ++i)
                        resourceCount += items[i].Amount;
                }

                AddButton(15, 182 + 20 * m_NumLines, 4005, 4007, GetButtonID(6, 7), GumpButtonType.Reply, 0);

                if (context.DoNotColor)
                    AddHtmlLocalized(50, 185 + 20 * m_NumLines, 250, 18, 1061592, LabelColor, false, false); // *

                if (name.Number > 0)
                    AddHtmlLocalized(context.DoNotColor ? 60 : 50, 185 + 20 * m_NumLines, 250, 18, name.Number, resourceCount.ToString(), LabelColor, false, false);
                else if (!String.IsNullOrEmpty(name.String))
                    AddHtml(context.DoNotColor ? 60 : 50, 185 + 20 * m_NumLines, 250, 18, Color(String.Format("{0} ({1})", name.String, resourceCount.ToString())), false, false);
            }
            // ****************************************

            CreateGroupList();

            if (page == CraftPage.PickResource)
                CreateResList(false, from);
            else if (page == CraftPage.PickResource2)
                CreateResList(true, from);
            else if (context != null && context.LastGroupIndex > -1)
                CreateItemList(context.LastGroupIndex);
        }

        public void CreateResList(bool opt, Mobile from)
        {
            CraftSubResCol res = (opt ? m_CraftSystem.CraftSubRes2 : m_CraftSystem.CraftSubRes);

            for (int i = 0; i < res.Count; ++i)
            {
                int index = i % m_NumLines;

                CraftSubRes subResource = res.GetAt(i);

                if (index == 0)
                {
                    if (i > 0)
                        AddButton(485, 60 + 20 * m_NumLines, 4005, 4007, 0, GumpButtonType.Page, (i / m_NumLines) + 1);

                    AddPage((i / m_NumLines) + 1);

                    if (i > 0)
                        AddButton(455, 60 + 20 * m_NumLines, 4014, 4015, 0, GumpButtonType.Page, i / m_NumLines);

                    CraftContext context = m_CraftSystem.GetContext(m_From);

                    AddButton(220, 60 + 20 * m_NumLines, 4005, 4007, GetButtonID(6, 4), GumpButtonType.Reply, 0);
                    AddHtmlLocalized(255, 63 + 20 * m_NumLines, 200, 18, (context == null || !context.DoNotColor) ? 1061591 : 1061590, LabelColor, false, false);
                }

                int resourceCount = 0;

                if (from.Backpack != null)
                {
                    Item[] items = from.Backpack.FindItemsByType(CI.GetEquivalentResources(subResource.ItemType), true);

                    for (int j = 0; j < items.Length; ++j)
                        resourceCount += items[j].Amount;
                }

                AddButton(220, 60 + (index * 20), 4005, 4007, GetButtonID(5, i), GumpButtonType.Reply, 0);

                if (subResource.Name.Number > 0)
                    AddHtmlLocalized(255, 63 + (index * 20), 250, 18, subResource.Name.Number, resourceCount.ToString(), LabelColor, false, false);
                else if (!String.IsNullOrEmpty(subResource.Name.String))
                    AddHtml(255, 63 + (index * 20), 250, 18, Color(String.Format("{0} ({1})", subResource.Name.String, resourceCount.ToString())), false, false);
            }
        }

        public void CreateMakeLastList()
        {
            CraftContext context = m_CraftSystem.GetContext(m_From);

            if (context == null)
                return;

            ArrayList items = context.Items;

            if (items.Count > 0)
            {
                for (int i = 0; i < items.Count; ++i)
                {
                    int index = i % m_NumLines;

                    CraftItem craftItem = (CraftItem)items[i];

                    if (index == 0)
                    {
                        if (i > 0)
                        {
                            AddButton(370, 60 + 20 * m_NumLines, 4005, 4007, 0, GumpButtonType.Page, (i / m_NumLines) + 1);
                            AddHtmlLocalized(405, 63 + 20 * m_NumLines, 100, 18, 1044045, LabelColor, false, false); // NEXT PAGE
                        }

                        AddPage((i / m_NumLines) + 1);

                        if (i > 0)
                        {
                            AddButton(220, 60 + 20 * m_NumLines, 4014, 4015, 0, GumpButtonType.Page, i / m_NumLines);
                            AddHtmlLocalized(255, 63 + 20 * m_NumLines, 100, 18, 1044044, LabelColor, false, false); // PREV PAGE
                        }
                    }

                    AddButton(220, 60 + (index * 20), 4005, 4007, GetButtonID(3, i), GumpButtonType.Reply, 0);

                    TextDefinition.AddHtmlText(this, 255, 63 + (index * 20), 220, 18, craftItem.Name, false, false, LabelColor, FontColor);

                    AddButton(480, 60 + (index * 20), 4011, 4012, GetButtonID(4, i), GumpButtonType.Reply, 0);
                }
            }
            else
            {
                // NOTE: This is not as OSI; it is an intentional difference

                AddHtmlLocalized(230, 62, 200, 22, 1044165, LabelColor, false, false); // You haven't made anything yet.
            }
        }

        public void CreateItemList(int selectedGroup)
        {
            if (selectedGroup == 501) // 501 : Last 10
            {
                CreateMakeLastList();
                return;
            }

            CraftGroupCol craftGroupCol = m_CraftSystem.CraftGroups;
            CraftGroup craftGroup = craftGroupCol.GetAt(selectedGroup);
            CraftItemCol craftItemCol = craftGroup.CraftItems;

            for (int i = 0; i < craftItemCol.Count; ++i)
            {
                int index = i % m_NumLines;

                CraftItem craftItem = craftItemCol.GetAt(i);

                if (index == 0)
                {
                    if (i > 0)
                    {
                        AddButton(370, 60 + 20 * m_NumLines, 4005, 4007, 0, GumpButtonType.Page, (i / m_NumLines) + 1);
                        AddHtmlLocalized(405, 63 + 20 * m_NumLines, 100, 18, 1044045, LabelColor, false, false); // NEXT PAGE
                    }

                    AddPage((i / m_NumLines) + 1);

                    if (i > 0)
                    {
                        AddButton(220, 60 + 20 * m_NumLines, 4014, 4015, 0, GumpButtonType.Page, i / m_NumLines);
                        AddHtmlLocalized(255, 63 + 20 * m_NumLines, 100, 18, 1044044, LabelColor, false, false); // PREV PAGE
                    }
                }

                AddButton(220, 60 + (index * 20), 4005, 4007, GetButtonID(1, i), GumpButtonType.Reply, 0);

                TextDefinition.AddHtmlText(this, 255, 63 + (index * 20), 220, 18, craftItem.Name, false, false, LabelColor, FontColor);

                AddButton(480, 60 + (index * 20), 4011, 4012, GetButtonID(2, i), GumpButtonType.Reply, 0);
            }
        }

        public int CreateGroupList()
        {
            CraftGroupCol craftGroupCol = m_CraftSystem.CraftGroups;

            AddButton(15, 60, 4005, 4007, GetButtonID(6, 3), GumpButtonType.Reply, 0);
            AddHtmlLocalized(50, 63, 150, 18, 1044014, LabelColor, false, false); // LAST TEN

            for (int i = 0; i < craftGroupCol.Count; i++)
            {
                CraftGroup craftGroup = craftGroupCol.GetAt(i);

                AddButton(15, 80 + (i * 20), 4005, 4007, GetButtonID(0, i), GumpButtonType.Reply, 0);

                TextDefinition.AddHtmlText(this, 50, 83 + (i * 20), 150, 18, craftGroup.Name, false, false, LabelColor, FontColor);
            }

            return craftGroupCol.Count;
        }

        public static int GetButtonID(int type, int index)
        {
            return 1 + type + (index * 7);
        }

        public void CraftItem(CraftItem item)
        {
            TextDefinition badCraft = m_CraftSystem.CanCraft(m_From, m_Tool, item.ItemType);

            if (!TextDefinition.IsNullOrEmpty(badCraft))
            {
                m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, badCraft));
            }
            else
            {
                Type type = null;

                CraftContext context = m_CraftSystem.GetContext(m_From);

                if (context != null)
                {
                    CraftSubResCol res = (item.UseSubRes2 ? m_CraftSystem.CraftSubRes2 : m_CraftSystem.CraftSubRes);
                    int resIndex = (item.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex);

                    if (resIndex >= 0 && resIndex < res.Count)
                        type = res.GetAt(resIndex).ItemType;
                }

                m_CraftSystem.CreateItem(m_From, item.ItemType, type, m_Tool, item);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID <= 0)
                return; // Canceled

            int buttonID = info.ButtonID - 1;
            int type = buttonID % 7;
            int index = buttonID / 7;

            CraftSystem system = m_CraftSystem;
            CraftGroupCol groups = system.CraftGroups;
            CraftContext context = system.GetContext(m_From);

            switch (type)
            {
                case 0: // Show group
                    {
                        if (context == null)
                            break;

                        if (index >= 0 && index < groups.Count)
                        {
                            context.LastGroupIndex = index;
                            m_From.SendGump(new CraftGump(m_From, system, m_Tool, null));
                        }

                        break;
                    }
                case 1: // Create item
                    {
                        if (context == null)
                            break;

                        int groupIndex = context.LastGroupIndex;

                        if (groupIndex >= 0 && groupIndex < groups.Count)
                        {
                            CraftGroup group = groups.GetAt(groupIndex);

                            if (index >= 0 && index < group.CraftItems.Count)
                                CraftItem(group.CraftItems.GetAt(index));
                        }

                        break;
                    }
                case 2: // Item details
                    {
                        if (context == null)
                            break;

                        int groupIndex = context.LastGroupIndex;

                        if (groupIndex >= 0 && groupIndex < groups.Count)
                        {
                            CraftGroup group = groups.GetAt(groupIndex);

                            if (index >= 0 && index < group.CraftItems.Count)
                                m_From.SendGump(new CraftGumpItem(m_From, system, group.CraftItems.GetAt(index), m_Tool));
                        }

                        break;
                    }
                case 3: // Create item (last 10)
                    {
                        if (context == null)
                            break;

                        ArrayList lastTen = context.Items;

                        if (index >= 0 && index < lastTen.Count)
                            CraftItem((CraftItem)lastTen[index]);

                        break;
                    }
                case 4: // Item details (last 10)
                    {
                        if (context == null)
                            break;

                        ArrayList lastTen = context.Items;

                        if (index >= 0 && index < lastTen.Count)
                            m_From.SendGump(new CraftGumpItem(m_From, system, (CraftItem)lastTen[index], m_Tool));

                        break;
                    }
                case 5: // Resource selected
                    {
                        if (m_Page == CraftPage.PickResource && index >= 0 && index < system.CraftSubRes.Count)
                        {
                            int groupIndex = (context == null ? -1 : context.LastGroupIndex);

                            CraftSubRes res = system.CraftSubRes.GetAt(index);

                            if (m_From.Skills[system.MainSkill].Base < res.RequiredSkill)
                            {
                                m_From.SendGump(new CraftGump(m_From, system, m_Tool, res.Message));
                            }
                            else
                            {
                                if (context != null)
                                    context.LastResourceIndex = index;

                                m_From.SendGump(new CraftGump(m_From, system, m_Tool, null));
                            }
                        }
                        else if (m_Page == CraftPage.PickResource2 && index >= 0 && index < system.CraftSubRes2.Count)
                        {
                            int groupIndex = (context == null ? -1 : context.LastGroupIndex);

                            CraftSubRes res = system.CraftSubRes2.GetAt(index);

                            if (m_From.Skills[system.MainSkill].Base < res.RequiredSkill)
                            {
                                m_From.SendGump(new CraftGump(m_From, system, m_Tool, res.Message));
                            }
                            else
                            {
                                if (context != null)
                                    context.LastResourceIndex2 = index;

                                m_From.SendGump(new CraftGump(m_From, system, m_Tool, null));
                            }
                        }

                        break;
                    }
                case 6: // Misc. buttons
                    {
                        switch (index)
                        {
                            case 0: // Resource selection
                                {
                                    if (system.CraftSubRes.Init)
                                        m_From.SendGump(new CraftGump(m_From, system, m_Tool, null, CraftPage.PickResource));

                                    break;
                                }
                            case 1: // Smelt item
                                {
                                    if (system.Recycle != null)
                                        system.Recycle.Do(m_From, system, m_Tool);

                                    break;
                                }
                            case 2: // Make last
                                {
                                    if (context == null)
                                        break;

                                    CraftItem item = context.LastMade;

                                    if (item != null)
                                        CraftItem(item);
                                    else
                                        m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, 1044165, m_Page)); // You haven't made anything yet.

                                    break;
                                }
                            case 3: // Last 10
                                {
                                    if (context == null)
                                        break;

                                    context.LastGroupIndex = 501;
                                    m_From.SendGump(new CraftGump(m_From, system, m_Tool, null));

                                    break;
                                }
                            case 4: // Toggle use resource hue
                                {
                                    if (context == null)
                                        break;

                                    context.DoNotColor = !context.DoNotColor;

                                    m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, null, m_Page));

                                    break;
                                }
                            case 5: // Repair item
                                {
                                    if (system.Repair)
                                        Repair.Do(m_From, system, m_Tool);

                                    break;
                                }
                            case 6: // Toggle mark option
                                {
                                    if (context == null || !system.MarkOption)
                                        break;

                                    switch (context.MarkOption)
                                    {
                                        case CraftMarkOption.MarkItem: context.MarkOption = CraftMarkOption.DoNotMark; break;
                                        case CraftMarkOption.DoNotMark: context.MarkOption = CraftMarkOption.PromptForMark; break;
                                        case CraftMarkOption.PromptForMark: context.MarkOption = CraftMarkOption.MarkItem; break;
                                    }

                                    m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, null, m_Page));

                                    break;
                                }
                            case 7: // Resource selection 2
                                {
                                    if (system.CraftSubRes2.Init)
                                        m_From.SendGump(new CraftGump(m_From, system, m_Tool, null, CraftPage.PickResource2));

                                    break;
                                }
                            case 8: // Enhance item
                                {
                                    if (system.CanEnhance)
                                        Enhance.BeginTarget(m_From, system, m_Tool);

                                    break;
                                }
                        }

                        break;
                    }
            }
        }
    }
}