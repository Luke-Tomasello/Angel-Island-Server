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

/* Engines/Factions/Gumps/New Gumps/Faction/CityStatusGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Factions.NewGumps.FactionMenu
{
    public class CityStatusGump : BaseFactionListGump<Town>
    {
        protected override GumpStyle Style { get { return GumpStyle.Parchment; } }

        private static readonly FieldInfo[] m_Fields = new FieldInfo[]
            {
                new FieldInfo( 11, null, StatusComparer.Instance ),
                new FieldInfo( 150, "<i>City</i>", NameComparer.Instance ),
                new FieldInfo( 150, "<i>Owner</i>", OwnerComparer.Instance ),
                new FieldInfo( 100, "<i>Silver</i>", SilverComparer.Instance ),
                new FieldInfo( 49, "<i>Tax Rate</i>", TaxComparer.Instance ),
            };

        protected override FieldInfo[] Fields { get { return m_Fields; } }

        #region Comparers

        private class StatusComparer : IComparer<Town>
        {
            public static readonly IComparer<Town> Instance = new StatusComparer();

            public StatusComparer()
            {
            }

            public int Compare(Town x, Town y)
            {
                if (x.Owner == null && y.Owner == null)
                    return 0;
                else if (x.Owner == null)
                    return IsPurifying(y) ? +1 : -1;
                else if (y.Owner == null)
                    return IsPurifying(x) ? -1 : +1;
                else
                    return IsPurifying(y).CompareTo(IsPurifying(x));
            }

            private static bool IsPurifying(Town town)
            {
                return town.Monolith != null && town.Monolith.Sigil != null && town.Monolith.Sigil.IsPurifying;
            }
        }

        private class NameComparer : IComparer<Town>
        {
            public static readonly IComparer<Town> Instance = new NameComparer();

            public NameComparer()
            {
            }

            public int Compare(Town x, Town y)
            {
                return Insensitive.Compare(x.Definition.FriendlyName, y.Definition.FriendlyName);
            }
        }

        private class OwnerComparer : IComparer<Town>
        {
            public static readonly IComparer<Town> Instance = new OwnerComparer();

            public OwnerComparer()
            {
            }

            public int Compare(Town x, Town y)
            {
                if (x.Owner == null && y.Owner == null)
                    return 0;
                else if (x.Owner == null)
                    return -1;
                else if (y.Owner == null)
                    return +1;
                else
                    return Insensitive.Compare(x.Owner.Definition.FriendlyName, y.Owner.Definition.FriendlyName);
            }
        }

        private class SilverComparer : IComparer<Town>
        {
            public static readonly IComparer<Town> Instance = new SilverComparer();

            public SilverComparer()
            {
            }

            public int Compare(Town x, Town y)
            {
                return x.Silver.CompareTo(y.Silver);
            }
        }

        private class TaxComparer : IComparer<Town>
        {
            public static readonly IComparer<Town> Instance = new TaxComparer();

            public TaxComparer()
            {
            }

            public int Compare(Town x, Town y)
            {
                return x.Tax.CompareTo(y.Tax);
            }
        }

        #endregion

        private Faction m_Faction;

        public CityStatusGump(Mobile m, Faction faction)
            : this(m, faction, new ListState(Town.Towns, 1, true))
        {
        }

        private CityStatusGump(Mobile m, Faction faction, ListState state)
            : base(state)
        {
            m_Faction = faction;

            AddBackground(632, 440);

            AddButtonLabeled(66, 40, 162, 1000, "Faction Status", false);
            AddButtonLabeled(243, 40, 162, 1001, "Faction Roster", false);
            AddButtonLabeled(420, 40, 163, 1002, "City Status", true);

            CompileList(m, 65, 75, false, false);

            AddImage(70, 370, 2361);
            AddHtmlLocalized(95, 365, 300, 20, 1011491, false, false); // sigil may be recaptured

            AddImage(70, 390, 2360);
            AddHtmlLocalized(95, 385, 300, 20, 1011492, false, false); // sigil may not be recaptured
        }

        protected override void Compile(Mobile m, int x, int y, int width, int height, int col, Town town)
        {
            switch (col)
            {
                case 0:
                    {
                        if (town.Owner != null)
                            AddImage(x, y + 5, (town.Monolith != null && town.Monolith.Sigil != null && town.Monolith.Sigil.IsPurifying) ? 0x938 : 0x939);

                        break;
                    }
                case 1:
                    {
                        AddHtmlText(x, y, width, height, town.Definition.FriendlyName, false, false);

                        break;
                    }
                case 2:
                    {
                        AddHtmlText(x, y, width, height, town.Owner == null ? "Neutral" : town.Owner.Definition.FriendlyName, false, false);

                        break;
                    }
                case 3:
                    {
                        string silver;

                        if (town.Silver == 0)
                            silver = "0";
                        else if (town.Silver % 1000000 == 0)
                            silver = string.Format("{0:N0}M", town.Silver / 1000000);
                        else if (town.Silver % 1000 == 0)
                            silver = string.Format("{0:N0}K", town.Silver / 1000);
                        else
                            silver = town.Silver.ToString("N0");

                        AddHtmlText(x, y, width, height, silver, false, false);

                        break;
                    }
                case 4:
                    {
                        AddHtmlText(x, y, width, height, string.Format("{0:+#;-#;0}%", town.Tax), false, false);

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

        protected override void Select(Mobile from, int row, Town town)
        {
            if (from.AccessLevel < AccessLevel.GameMaster && town.Owner != m_Faction)
            {
                from.SendLocalizedMessage(1010332); // Your faction does not control this town
            }
            else
            {
                FactionGumps.CloseGumps(from, typeof(TownMenu.TownGump));
                from.SendGump(new TownMenu.TownGump(from, town));
            }
        }

        protected override void ResendGump(Mobile from)
        {
            FactionGumps.CloseGumps(from, typeof(CityStatusGump));
            from.SendGump(new CityStatusGump(from, m_Faction, m_State));
        }
    }
}