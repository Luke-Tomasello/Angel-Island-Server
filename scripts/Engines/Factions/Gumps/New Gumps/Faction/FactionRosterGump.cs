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

/* Engines/Factions/Gumps/New Gumps/Faction/FactionRosterGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Factions.NewGumps.FactionMenu
{
    public class FactionRosterGump : BaseFactionListGump<PlayerState>
    {
        protected override GumpStyle Style { get { return GumpStyle.Parchment; } }

        private static readonly FieldInfo[] m_Fields = new FieldInfo[]
            {
                new FieldInfo( 130, "<i>Name</i>", NameComparer.Instance ),
                new FieldInfo( 50, "<i>Guild</i>", GuildComparer.Instance ),
                new FieldInfo( 150, "<i>Role</i>", RoleComparer.Instance ),
                new FieldInfo( 80, "<i>Last On</i>", LastOnComparer.Instance ),
                new FieldInfo( 50, "<i>Score</i>", ScoreComparer.Instance ),
            };

        protected override FieldInfo[] Fields { get { return m_Fields; } }

        #region Comparers

        private class NameComparer : IComparer<PlayerState>
        {
            public static readonly IComparer<PlayerState> Instance = new NameComparer();

            public NameComparer()
            {
            }

            public int Compare(PlayerState x, PlayerState y)
            {
                return Insensitive.Compare(x.Mobile.Name, y.Mobile.Name);
            }
        }

        private class GuildComparer : IComparer<PlayerState>
        {
            public static readonly IComparer<PlayerState> Instance = new GuildComparer();

            public GuildComparer()
            {
            }

            public int Compare(PlayerState x, PlayerState y)
            {
                if (x.Mobile.Guild == null && y.Mobile.Guild == null)
                    return 0;
                else if (x.Mobile.Guild == null)
                    return -1;
                else if (y.Mobile.Guild == null)
                    return +1;
                else
                    return Insensitive.Compare(x.Mobile.Guild.Abbreviation, y.Mobile.Guild.Abbreviation);
            }
        }

        private class RoleComparer : IComparer<PlayerState>
        {
            public static readonly IComparer<PlayerState> Instance = new RoleComparer();

            public RoleComparer()
            {
            }

            public int Compare(PlayerState x, PlayerState y)
            {
                return Quantify(x).CompareTo(Quantify(y));
            }

            private static int Quantify(PlayerState player)
            {
                if (player.Mobile == player.Faction.Commander)
                    return 4;
                else if (player.Sheriff != null)
                    return 3;
                else if (player.Finance != null)
                    return 2;
                else if (player.MerchantTitle != MerchantTitle.None)
                    return 1;
                else
                    return 0;
            }
        }

        private class LastOnComparer : IComparer<PlayerState>
        {
            public static readonly IComparer<PlayerState> Instance = new LastOnComparer();

            public LastOnComparer()
            {
            }

            public int Compare(PlayerState x, PlayerState y)
            {
                if (x.Mobile.NetState == null && y.Mobile.NetState == null)
                    return GetLastOn(x.Mobile).CompareTo(GetLastOn(y.Mobile));
                else if (x.Mobile.NetState == null)
                    return -1;
                else if (y.Mobile.NetState == null)
                    return +1;
                else
                    return 0;
            }

            private static DateTime GetLastOn(Mobile mob)
            {
                return (mob is PlayerMobile) ? ((PlayerMobile)mob).LastLogin : DateTime.MinValue;
            }
        }

        private class ScoreComparer : IComparer<PlayerState>
        {
            public static readonly IComparer<PlayerState> Instance = new ScoreComparer();

            public ScoreComparer()
            {
            }

            public int Compare(PlayerState x, PlayerState y)
            {
                return x.KillPoints.CompareTo(y.KillPoints);
            }
        }

        #endregion

        private Faction m_Faction;

        public FactionRosterGump(Mobile m, Faction faction)
            : this(m, faction, new ListState(faction.Members, 3, false))
        {
        }

        private FactionRosterGump(Mobile m, Faction faction, ListState state)
            : base(state)
        {
            m_Faction = faction;
            m_State = state;

            AddBackground(632, 440);

            AddButtonLabeled(66, 40, 162, 1000, "Faction Status", false);
            AddButtonLabeled(243, 40, 162, 1001, "Faction Roster", true);
            AddButtonLabeled(420, 40, 163, 1002, "City Status", false);

            CompileList(m, 65, 75, true, true);
        }

        protected override string GetFilterTarget(PlayerState player)
        {
            return player.Mobile.Name;
        }

        protected override void Compile(Mobile m, int x, int y, int width, int height, int col, PlayerState player)
        {
            switch (col)
            {
                case 0:
                    {
                        string name = FormatName(player.Mobile);

                        if (m == player.Mobile)
                            name = string.Format("<basefont color=#006600>{0}</basefont>", name);
                        else if (player.Mobile.NetState != null)
                            name = string.Format("<basefont color=#000066>{0}</basefont>", name);

                        AddHtmlText(x, y, width, height, name, false, false);

                        break;
                    }
                case 1:
                    {
                        Guild guild = player.Mobile.Guild as Guild;

                        if (guild != null)
                            AddHtmlText(x, y, width, height, string.Format("({0}){1}", guild.Abbreviation, guild.Leader == player.Mobile ? "*" : string.Empty), false, false);

                        break;
                    }
                case 2:
                    {
                        AddHtmlText(x, y, width, height, FormatRole(player), false, false);

                        break;
                    }
                case 3:
                    {
                        TextDefinition lastOn = 0;

                        if (player.Mobile.NetState != null)
                            lastOn = 1063015; // Online
                        else if (player.Mobile is PlayerMobile)
                            lastOn = ((PlayerMobile)player.Mobile).LastLogin.ToString("yyyy-MM-dd");

                        AddHtmlText(x, y, width, height, lastOn, false, false);

                        break;
                    }
                case 4:
                    {
                        AddHtmlText(x, y, width, height, player.KillPoints.ToString(), false, false);

                        break;
                    }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Faction.IsMember(from))
                return;

            int buttonID = info.ButtonID;

            switch (buttonID)
            {
                case 1000: // Faction Status
                    {
                        FactionGumps.CloseGumps(from, typeof(FactionStatusGump));
                        from.SendGump(new FactionStatusGump(from, m_Faction));

                        break;
                    }
                case 1001: // Faction Roster
                    {
                        FactionGumps.CloseGumps(from, typeof(FactionRosterGump));
                        from.SendGump(new FactionRosterGump(from, m_Faction));

                        break;
                    }
                case 1002: // City Status
                    {
                        FactionGumps.CloseGumps(from, typeof(CityStatusGump));
                        from.SendGump(new CityStatusGump(from, m_Faction));

                        break;
                    }
                default:
                    {
                        HandleListResponse(sender, info);

                        break;
                    }
            }
        }

        protected override void Select(Mobile from, int row, PlayerState player)
        {
            FactionGumps.CloseGumps(from, typeof(Misc.StatisticsGump));
            from.SendGump(new Misc.StatisticsGump(from, player));
        }

        protected override void ResendGump(Mobile from)
        {
            FactionGumps.CloseGumps(from, typeof(FactionRosterGump));
            from.SendGump(new FactionRosterGump(from, m_Faction, m_State));
        }
    }
}