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

/* Scripts\Engines\Township\Gumps\TSEmissaryGump.cs
 * CHANGELOG:
 *  8/23/23, Yoar
 *      Initial version.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Township
{
    public class TSEmissaryGump : BaseTownshipGump
    {
        private enum Button
        {
            Close,
            SetStandard,
            SetLawless,
            SetAuthority,
        }

        public TSEmissaryGump(Items.TownshipStone stone, Mobile from)
            : base(stone)
        {
            AddBackground();

            AddTitle("The Emissary");

            AddLine("Current Law Level: {0}", Items.TownshipStone.FormatLawLevel(m_Stone.LawLevel));

            AddButton((int)Button.SetStandard, String.Format("Change to Standard ({0} gp)", TownshipSettings.LawNormCharge.ToString("N0")), m_Stone.HasAccess(from, TownshipAccess.CoLeader) && m_Stone.LawLevel != LawLevel.NONE);
            AddButton((int)Button.SetLawless, String.Format("Change to Lawless ({0} gp)", TownshipSettings.LawlessCharge.ToString("N0")), m_Stone.HasAccess(from, TownshipAccess.CoLeader) && m_Stone.LawLevel != LawLevel.LAWLESS);
            AddButton((int)Button.SetAuthority, String.Format("Change to Grant of Authority ({0} gp)", TownshipSettings.LawAuthCharge.ToString("N0")), m_Stone.HasAccess(from, TownshipAccess.CoLeader) && m_Stone.LawLevel != LawLevel.AUTHORITY);

            AddLine("Daily fees based on current activity level:");

            AddLine("Standard: {0} gp", m_Stone.CalculateLawLevelFee(LawLevel.NONE).ToString("N0"));
            AddLine("Lawless: {0} gp", m_Stone.CalculateLawLevelFee(LawLevel.LAWLESS).ToString("N0"));
            AddLine("Grant of Authority: {0} gp", m_Stone.CalculateLawLevelFee(LawLevel.AUTHORITY).ToString("N0"));
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Stone.HasNPC(typeof(Mobiles.TSEmissary)))
                return;

            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.CoLeader))
                return;

            switch ((Button)info.ButtonID)
            {
                case Button.SetStandard:
                    {
                        SetLawLevel(from, LawLevel.NONE);

                        from.CloseGump(typeof(TSEmissaryGump));
                        from.SendGump(new TSEmissaryGump(m_Stone, from));

                        break;
                    }
                case Button.SetLawless:
                    {
                        SetLawLevel(from, LawLevel.LAWLESS);

                        from.CloseGump(typeof(TSEmissaryGump));
                        from.SendGump(new TSEmissaryGump(m_Stone, from));

                        break;
                    }
                case Button.SetAuthority:
                    {
                        SetLawLevel(from, LawLevel.AUTHORITY);

                        from.CloseGump(typeof(TSEmissaryGump));
                        from.SendGump(new TSEmissaryGump(m_Stone, from));

                        break;
                    }
            }
        }

        private void SetLawLevel(Mobile from, LawLevel lawLevel)
        {
            if (m_Stone.LawLevel == lawLevel)
                return;

            if (DateTime.UtcNow < m_Stone.LastLawChange + TimeSpan.FromMinutes(30.0))
            {
                from.SendMessage("You must wait at least 30 minutes before changing your township's law level again.");
                return;
            }

            int charge;

            switch (lawLevel)
            {
                case LawLevel.NONE: charge = TownshipSettings.LawNormCharge; break;
                case LawLevel.LAWLESS: charge = TownshipSettings.LawlessCharge; break;
                case LawLevel.AUTHORITY: charge = TownshipSettings.LawAuthCharge; break;

                default: return;
            }

            if (m_Stone.GoldHeld < charge)
            {
                from.SendMessage("You lack the necessary funds to set your township's law level.");
                return;
            }

            // TODO: Confirm gump?

            m_Stone.LawLevel = lawLevel;
            m_Stone.LastLawChange = DateTime.UtcNow;
            m_Stone.GoldHeld -= charge;

            m_Stone.RecordWithdrawal(charge, String.Format("{0} changed law level to {1}", from.Name, Items.TownshipStone.FormatLawLevel(lawLevel)));

            from.SendMessage("You changed your township's law level to {0}.", Items.TownshipStone.FormatLawLevel(lawLevel));
        }
    }
}