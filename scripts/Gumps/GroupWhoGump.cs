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

/* Scripts/Gumps/GroupWhoGump.cs
 * ChangeLog:
 * 5/28/10, Adam
 *	Created
 * Used for listing 'who's online' for Guilds members Aliance members and Party members
 */

using Server.Guilds;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections;

namespace Server.Gumps
{
    public class GroupWhoGump : Gump
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Guild", AccessLevel.Player, new CommandEventHandler(Guild_OnCommand));
            Server.CommandSystem.Register("Party", AccessLevel.Player, new CommandEventHandler(Party_OnCommand));
            Server.CommandSystem.Register("Allied", AccessLevel.Player, new CommandEventHandler(Allied_OnCommand));
        }

        public static void Guild_OnCommand(CommandEventArgs e)
        {
            if (e.ArgString == null || e.ArgString.Length == 0 || e.ArgString != "who")
            {
                e.Mobile.SendMessage("Usage: [guild who");
                return;
            }
            e.Mobile.CloseGump(typeof(GroupWhoGump));
            e.Mobile.SendGump(new GroupWhoGump(WhoType.Guild, e.Mobile));
        }

        public static void Party_OnCommand(CommandEventArgs e)
        {
            if (e.ArgString == null || e.ArgString.Length == 0 || e.ArgString != "who")
            {
                e.Mobile.SendMessage("Usage: [party who");
                return;
            }
            e.Mobile.CloseGump(typeof(GroupWhoGump));
            e.Mobile.SendGump(new GroupWhoGump(WhoType.Party, e.Mobile));
        }

        public static void Allied_OnCommand(CommandEventArgs e)
        {
            if (e.ArgString == null || e.ArgString.Length == 0 || e.ArgString != "who")
            {
                e.Mobile.SendMessage("Usage: [allied who");
                return;
            }
            e.Mobile.CloseGump(typeof(GroupWhoGump));
            e.Mobile.SendGump(new GroupWhoGump(WhoType.Allied, e.Mobile));
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

        public enum WhoType { Guild, Allied, Party }
        private WhoType m_WhoType;
        private Mobile m_Owner;
        private ArrayList m_Mobiles;
        private int m_Page;

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

        public GroupWhoGump(WhoType type, Mobile owner, int page)
            : base(GumpOffsetX, GumpOffsetY)
        {
            owner.CloseGump(typeof(GroupWhoGump));

            m_WhoType = type;
            m_Owner = owner;
            m_Mobiles = new ArrayList();
            LoadList(m_WhoType, m_Owner);
            Initialize(page);
        }

        public GroupWhoGump(WhoType type, Mobile owner)
            : this(type, owner, 0)
        {

        }

        public void LoadList(WhoType type, Mobile owner)
        {
            if (type == WhoType.Allied)
            {
                Guild g = owner.Guild as Guild;
                if (g == null || g.Allies == null)
                    return;

                foreach (object ox in g.Allies)
                {
                    if (ox is Guild == false)
                        continue;

                    if ((ox as Guild).Members != null && (ox as Guild).Members.Count > 0)
                    {
                        for (int ix = 0; ix < (ox as Guild).Members.Count; ix++)
                        {
                            if (((ox as Guild).Members[ix] as Mobile).NetState != null)
                                m_Mobiles.Add((ox as Guild).Members[ix]);
                        }
                    }
                }
            }
            else if (type == WhoType.Guild)
            {
                Guild g = owner.Guild as Guild;
                if (g == null)
                    return;

                if (g.Members != null && g.Members.Count > 0)
                {
                    for (int ix = 0; ix < g.Members.Count; ix++)
                    {
                        if ((g.Members[ix] as Mobile).NetState != null)
                            m_Mobiles.Add(g.Members[ix]);
                    }
                }
            }
            else if (type == WhoType.Party)
            {
                if (owner.Party != null)
                {
                    foreach (Server.Engines.PartySystem.PartyMemberInfo pmi in (owner.Party as Server.Engines.PartySystem.Party).Members)
                    {
                        if (pmi != null && pmi.Mobile != null && pmi.Mobile is PlayerMobile)
                        {
                            if (pmi.Mobile.NetState != null)
                                m_Mobiles.Add(pmi.Mobile);
                        }
                    }
                }
            }
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

            AddLabel(x + TextOffsetX, y, TextHue, string.Format("Page {0} of {1} ({2})", page + 1, (m_Mobiles.Count + EntryCount - 1) / EntryCount, m_Mobiles.Count));

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
                AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, m.GetHueForNameInList(), m.Deleted ? "(deleted)" : m.Name);

                x += EntryWidth + OffsetSize;

                if (SetGumpID != 0)
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);

                if (m != null && !m.Deleted)
                    AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 3, GumpButtonType.Reply, 0);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;
            PlayerMobile pm = (from as PlayerMobile);


            switch (info.ButtonID)
            {
                case 0: // Closed
                    {
                        return;
                    }
                case 1: // Previous
                    {
                        if (m_Page > 0)
                            from.SendGump(new GroupWhoGump(m_WhoType, from, m_Page - 1));

                        break;
                    }
                case 2: // Next
                    {
                        if ((m_Page + 1) * EntryCount < m_Mobiles.Count)
                            from.SendGump(new GroupWhoGump(m_WhoType, from, m_Page + 1));

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
                                from.SendMessage("{0} is no longer with us.", m.Name);
                                from.SendGump(new GroupWhoGump(m_WhoType, from, 0));
                            }
                            else
                            {
                                try
                                {
                                    from.SendMessage("What would you like to say to {0}?", (m_Mobiles[index] as Mobile).Name);
                                    from.Prompt = new GroupTellPrompt(m_Mobiles[index] as Mobile);
                                }
                                catch (Exception ex)
                                {
                                    Server.Diagnostics.LogHelper.LogException(ex);
                                }

                            }
                        }

                        break;
                    }
            }
        }

    }

    public class GroupTellPrompt : Prompt
    {
        private Mobile m_Mobile;

        public GroupTellPrompt(Mobile m)
        {
            m_Mobile = m;
        }

        public override void OnCancel(Mobile from)
        {
            from.SendMessage("You decide against sending {0} a message.", m_Mobile.Name);
        }

        public override void OnResponse(Mobile from, string text)
        {
            text = text.Trim();

            if (text.Length > 0)
            {
                m_Mobile.SendMessage(from.SpeechHue, string.Format("[Private][{0}]: {1}", from.Name, text));    // users senders speech color
                from.SendMessage(from.SpeechHue, string.Format("[Private][{0}]: {1}", from.Name, text));        // local echo
            }
        }
    }
}