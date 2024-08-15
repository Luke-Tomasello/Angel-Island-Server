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

/* Scripts\Engines\Township\Gumps\TSTowncrierGump.cs
 * CHANGELOG:
 *  3/12/2024, Adam (retracting township warning)
 *      Warn players of the possible consequences of retracting the size of their township.
 *  8/23/23, Yoar
 *      Initial version.
 */

using Server.Diagnostics;
using Server.Gumps;
using Server.Network;
using System;
using System.Text;

namespace Server.Township
{
    public class TSTownCrierGump : BaseTownshipGump
    {
        private enum Button
        {
            Close,
            ExtendBounds,
            RetractBounds,
        }

        public TSTownCrierGump(Items.TownshipStone stone, Mobile from)
            : base(stone)
        {
            AddBackground();

            AddTitle("The Town Crier");

            AddLine("Bounds: {0}", m_Stone.Extended ? "Extended" : "Original");

            Items.TownshipStone.ExtendFlag flags = m_Stone.GetExtendFlags();

            string info;

            if (flags == Items.TownshipStone.ExtendFlag.None)
            {
                if (m_Stone.Extended)
                    info = "Extension is legal";
                else
                    info = "Extension is possible";
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                if (flags.HasFlag(Items.TownshipStone.ExtendFlag.ConflictingRegion))
                {
                    if (sb.Length != 0)
                        sb.Append(", ");

                    sb.Append("Conflicting area");
                }

                if (flags.HasFlag(Items.TownshipStone.ExtendFlag.ConflictingRegion))
                {
                    if (sb.Length != 0)
                        sb.Append(", ");

                    sb.Append("Insufficient house ownership");
                }

                info = sb.ToString();
            }

            AddLine(info);

            AddButton((int)Button.ExtendBounds, string.Format("Extend Township Bounds ({0} gp)", TownshipSettings.ExtendedCharge.ToString("N0")), !m_Stone.Extended && m_Stone.HasAccess(from, TownshipAccess.CoLeader));
            AddButton((int)Button.RetractBounds, "Retract Township Bounds (0 gp)", m_Stone.Extended && m_Stone.HasAccess(from, TownshipAccess.CoLeader));

            AddLine("Daily fees:");

            AddLine("Original: 0 gp");
            AddLine("Extended: {0} gp", TownshipSettings.ExtendedFee);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Stone.HasNPC(typeof(Mobiles.TSTownCrier)))
                return;

            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.CoLeader))
                return;

            // TODO: Confirm gump?

            switch ((Button)info.ButtonID)
            {
                case Button.ExtendBounds:
                    {
                        SetExtended(from, true);

                        from.CloseGump(typeof(TSTownCrierGump));
                        from.SendGump(new TSTownCrierGump(m_Stone, from));

                        break;
                    }
                case Button.RetractBounds:
                    {
                        SetExtended(from, false);

                        from.CloseGump(typeof(TSTownCrierGump));
                        break;
                    }
            }
        }

        private void SetExtended(Mobile from, bool value)
        {
            if (m_Stone.Extended == value)
                return;

            if (value)
            {
                Items.TownshipStone.ExtendFlag flags = m_Stone.GetExtendFlags();

                if (flags != Items.TownshipStone.ExtendFlag.None)
                {
                    bool givenReason = false;

                    if (flags.HasFlag(Items.TownshipStone.ExtendFlag.ConflictingRegion))
                    {
                        from.SendMessage("Extended area would conflict with another township or a region controlled by Lord British.");
                        givenReason = true;
                    }

                    if (flags.HasFlag(Items.TownshipStone.ExtendFlag.HousingPercentage))
                    {
                        from.SendMessage("Your guild doesn't own enough of the housing in the extended area.");
                        givenReason = true;
                    }

                    if (!givenReason)
                        from.SendMessage("Can't extend region. Reason unknown - notify a staff member.");

                    return;
                }
            }

            TimeSpan MinWait = Core.RuleSets.TestCenterRules() ? TimeSpan.FromMinutes(2.0) : TimeSpan.FromMinutes(30.0);
            if (DateTime.UtcNow < m_Stone.LastExtendedChange + MinWait)
            {
                from.SendMessage("You must wait at least {0} minutes before changing your township's bounds again.", MinWait.TotalMinutes);
                return;
            }

            if (value && m_Stone.GoldHeld < TownshipSettings.ExtendedCharge)
            {
                from.SendMessage("You lack the necessary funds to extend your township's bounds.");
                return;
            }

            if (value)
                m_Stone.ExtendRegion();
            else
            {
                from.SendGump(new RetractionWarningGump(m_Stone, from));
                return;
            }

            m_Stone.LastExtendedChange = DateTime.UtcNow;

            if (value)
            {
                m_Stone.GoldHeld -= TownshipSettings.ExtendedCharge;

                m_Stone.RecordWithdrawal(TownshipSettings.ExtendedCharge, string.Format("{0} extended the township bounds", from.Name));
            }

            from.SendMessage("You {0} your township bounds.", value ? "extended" : "retracted");
        }
    }

    public class RetractionWarningGump : Gump
    {
        private Mobile m_From;
        private Items.TownshipStone m_Stone;

        public RetractionWarningGump(Items.TownshipStone stone, Mobile from)
            : base(110, 100)
        {
            m_From = from;
            m_Stone = stone;

            //mobile.CloseGump(typeof(ConfirmRedeedNPCGumpWarning));

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, 420, 280, 5054);

            AddImageTiled(10, 10, 400, 20, 2624);
            AddAlphaRegion(10, 10, 400, 20);

            AddHtmlLocalized(10, 10, 400, 20, 1060635, 30720, false, false); // <CENTER>WARNING</CENTER>

            AddImageTiled(10, 40, 400, 200, 2624);
            AddAlphaRegion(10, 40, 400, 200);

            String WarningString = string.Format(
                "You are about to reduce the size of your township region.<br>" +
                "One or more of the following objects in the extended region may be deleted or will no longer be locked down.<br>" +
                "Township craftables (walls/deco/etc)\r\nTownship addons\r\nTownship lockdowns\r\nTownship NPCs<br><br>" +
                "Are you sure you wish to continue?"
                );
            AddHtml(10, 40, 400, 200, WarningString, false, true);

            AddImageTiled(10, 250, 400, 20, 2624);
            AddAlphaRegion(10, 250, 400, 20);

            AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

            AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            switch (info.ButtonID)
            {
                case 0x0: // close
                case 0x2: // cancel
                    {
                        break;
                    }
                case 0x1: // okay
                    {
                        LogHelper logger = new LogHelper("Township reduced size warning.log", overwrite: false, sline: true);
                        logger.Log(LogType.Mobile, from, string.Format("Was warned of the consequences of retracting their township bounds."));
                        logger.Finish();

                        bool value = false;
                        m_Stone.ReduceRegion();

                        m_Stone.LastExtendedChange = DateTime.UtcNow;

                        from.SendMessage("You {0} your township bounds.", value ? "extended" : "retracted");

                        break;
                    }
            }
        }
    }
}