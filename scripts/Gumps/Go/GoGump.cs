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

/* Scripts/Gumps/Go/GoGump.cs
 * Changelog:
 *  10/22/2026, Adam
 *      OnResponse: a destination is checked by CommandHandlers.PlayerMayGo before moving (player-level users may not
 *      change facet, and on Trammel may only go to the new player starting area). Staff are unchanged.
 *  4/24/2024, Adam (One world, one map)
 *      Convert map entries to take a Map, have all maps use this new unified [go structure
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Network;
using System;

namespace Server.Gumps
{
    public class GoGump : Gump
    {
        public static void Initialize()
        {
            CommandSystem.Register("ReloadGo", AccessLevel.Administrator, ReloadGo_OnCommand);
        }
        public static LocationTree Felucca;
        public static LocationTree Trammel;
        public static LocationTree Ilshenar;
        public static LocationTree Malas;

        public static LocationTree FeluccaForPlayers;
        public static LocationTree TrammelForPlayers;
        public static LocationTree IlshenarForPlayers;
        public static LocationTree MalasForPlayers;

        public static void Configure()
        {
            Felucca = LoadTree("felucca.xml", Map.Felucca);
            Trammel = LoadTree("felucca.xml", Map.Trammel);
            Ilshenar = LoadTree("ilshenar.xml", Map.Ilshenar);
            Malas = LoadTree("malas.xml", Map.Malas);

            FeluccaForPlayers = Felucca.CreateForAccess(AccessLevel.Player);
            TrammelForPlayers = Trammel.CreateForAccess(AccessLevel.Player);    // test center
            IlshenarForPlayers = Ilshenar.CreateForAccess(AccessLevel.Player);
            MalasForPlayers = Malas.CreateForAccess(AccessLevel.Player);
        }
        public static void Reload()
        {
            Configure();

            Console.WriteLine("[Go] Location trees reloaded.");
        }
        private static LocationTree LoadTree(string file, Map map)
        {
            try
            {
                var tree = new LocationTree(file, map);
                Console.WriteLine("[Go] Loaded {0} for {1}.", file, map);
                return tree;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Go] ERROR loading {0} for {1}: {2}", file, map, ex);
                throw;
            }
        }

        private static void ReloadGo_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            try
            {
                DateTime start = DateTime.UtcNow;

                GoGump.Reload();

                // If/when you add this:
                // PlayerGo.ClearCache();

                TimeSpan elapsed = DateTime.UtcNow - start;

                from.SendMessage("Go location trees reloaded in {0:0.00}s.", elapsed.TotalSeconds);

                // Optional: echo counts if you expose them later
                // from.SendMessage("Felucca: {0} entries", GoGump.Felucca.Count);
            }
            catch (Exception ex)
            {
                from.SendMessage("Go reload failed.");

                // Player-friendly
                from.SendMessage("Error: {0}", ex.Message);

                // Dev-friendly (console)
                Console.WriteLine("[Go] Reload FAILED: {0}", ex);

                // Optional: full stack to staff only
                if (from.AccessLevel >= AccessLevel.Administrator)
                    from.SendMessage(ex.ToString());
            }
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

        public static void DisplayTo(Mobile from)
        {
            // 4/24/2024, Adam(One world, one map)
            //  Convert map entries to take a Map, have all maps use this new unified [go structure
            LocationTree tree;
            if (from.Map == Map.Ilshenar)
                tree = Ilshenar;
            else if (from.Map == Map.Felucca)
                tree = Felucca;
            else if (from.Map == Map.Trammel)
                tree = Trammel;
            else if (from.Map == Map.Malas)
                tree = Malas;
            else
                tree = Felucca; // default to Felucca

            ParentNode? branch = (ParentNode?)tree.LastBranch[from];

            if (branch == null)
                branch = tree.Root;

            if (branch != null)
                from.SendGump(new GoGump(0, from, tree, branch));
        }
        public static void DisplayTo(Mobile from, bool playerView)
        {
            LocationTree tree = GetTreeFor(from.Map, playerView);

            ParentNode? branch = (ParentNode?)tree.LastBranch[from];

            if (branch == null)
                branch = tree.Root;

            if (branch != null)
                from.SendGump(new GoGump(0, from, tree, branch));
        }

        public static LocationTree GetTreeFor(Map map, bool playerView)
        {
            if (map == Map.Ilshenar)
                return playerView ? IlshenarForPlayers : Ilshenar;

            if (map == Map.Felucca)
                return playerView ? FeluccaForPlayers : Felucca;

            if (map == Map.Trammel)
                return playerView ? TrammelForPlayers : Trammel;

            if (map == Map.Malas)
                return playerView ? MalasForPlayers : Malas;

            return playerView ? FeluccaForPlayers : Felucca;
        }
        private LocationTree m_Tree;
        private ParentNode m_Node;
        private int m_Page;

        private GoGump(int page, Mobile from, LocationTree tree, ParentNode node)
            : base(50, 50)
        {
            from.CloseGump(typeof(GoGump));

            tree.LastBranch[from] = node;

            m_Page = page;
            m_Tree = tree;
            m_Node = node;

            int x = BorderSize + OffsetSize;
            int y = BorderSize + OffsetSize;

            int count = node.Children.Length - (page * EntryCount);

            if (count < 0)
                count = 0;
            else if (count > EntryCount)
                count = EntryCount;

            int totalHeight = OffsetSize + ((EntryHeight + OffsetSize) * (count + 1));

            AddPage(0);

            AddBackground(0, 0, BackWidth, BorderSize + totalHeight + BorderSize, BackGumpID);
            AddImageTiled(BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), totalHeight, OffsetGumpID);

            if (OldStyle)
                AddImageTiled(x, y, TotalWidth - (OffsetSize * 3) - SetWidth, EntryHeight, HeaderGumpID);
            else
                AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);

            if (node.Parent != null)
            {
                AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1, GumpButtonType.Reply, 0);

                if (PrevLabel)
                    AddLabel(x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous");
            }

            x += PrevWidth + OffsetSize;

            int emptyWidth = TotalWidth - (PrevWidth * 2) - NextWidth - (OffsetSize * 5) - (OldStyle ? SetWidth + OffsetSize : 0);

            if (!OldStyle)
                AddImageTiled(x - (OldStyle ? OffsetSize : 0), y, emptyWidth + (OldStyle ? OffsetSize * 2 : 0), EntryHeight, EntryGumpID);

            AddHtml(x + TextOffsetX, y, emptyWidth - TextOffsetX, EntryHeight, string.Format("<center>{0}</center>", node.Name), false, false);

            x += emptyWidth + OffsetSize;

            if (OldStyle)
                AddImageTiled(x, y, TotalWidth - (OffsetSize * 3) - SetWidth, EntryHeight, HeaderGumpID);
            else
                AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);

            if (page > 0)
            {
                AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 2, GumpButtonType.Reply, 0);

                if (PrevLabel)
                    AddLabel(x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous");
            }

            x += PrevWidth + OffsetSize;

            if (!OldStyle)
                AddImageTiled(x, y, NextWidth, EntryHeight, HeaderGumpID);

            if ((page + 1) * EntryCount < node.Children.Length)
            {
                AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 3, GumpButtonType.Reply, 1);

                if (NextLabel)
                    AddLabel(x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next");
            }

            for (int i = 0, index = page * EntryCount; i < EntryCount && index < node.Children.Length; ++i, ++index)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                object child = node.Children[index];
                string name = "";

                if (child is ParentNode)
                    name = ((ParentNode)child).Name;
                else if (child is ChildNode)
                    name = ((ChildNode)child).Name;

                AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
                AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, name);

                x += EntryWidth + OffsetSize;

                if (SetGumpID != 0)
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);

                AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, index + 4, GumpButtonType.Reply, 0);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        if (m_Node.Parent != null)
                            from.SendGump(new GoGump(0, from, m_Tree, m_Node.Parent));

                        break;
                    }
                case 2:
                    {
                        if (m_Page > 0)
                            from.SendGump(new GoGump(m_Page - 1, from, m_Tree, m_Node));

                        break;
                    }
                case 3:
                    {
                        if ((m_Page + 1) * EntryCount < m_Node.Children.Length)
                            from.SendGump(new GoGump(m_Page + 1, from, m_Tree, m_Node));

                        break;
                    }
                default:
                    {
                        int index = info.ButtonID - 4;

                        if (index >= 0 && index < m_Node.Children.Length)
                        {
                            object o = m_Node.Children[index];

                            if (o is ParentNode)
                            {
                                from.SendGump(new GoGump(0, from, m_Tree, (ParentNode)o));
                            }
                            else
                            {
                                ChildNode n = (ChildNode)o;

                                Map targetMap = n.Map ?? m_Tree.Map;

                                if (n.Map == Map.Felucca && m_Tree.Map == Map.Trammel)
                                    targetMap = Map.Trammel;

                                // players: no change of facet, and on Trammel only the new player starting area
                                if (Server.Commands.CommandHandlers.PlayerMayGo(from, n.Location, targetMap))
                                    from.MoveToWorld(n.Location, targetMap);
                            }
                        }

                        break;
                    }
            }
        }
    }
}