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

/* Scripts\Engines\Township\Gumps\TSEvocatorGump.cs
 * CHANGELOG:
 *  8/23/23, Yoar
 *      Initial version.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Township
{
    public class TSEvocatorGump : BaseTownshipGump
    {
        private enum Button
        {
            Close,
            ToggleRecallIn,
            ToggleRecallOut,
            ToggleGateIn,
            ToggleGateOut,
        }

        private enum TravelRule
        {
            RecallIn,
            RecallOut,
            GateIn,
            GateOut,
        }

        public TSEvocatorGump(Items.TownshipStone stone, Mobile from)
            : base(stone)
        {
            AddBackground();

            AddTitle("The Evocator");

            AddLine("Change Travel Rules");

            if (!Core.RuleSets.SiegeRules())
            {
                AddButton((int)Button.ToggleRecallIn, string.Format("{0} Recall In ({1} gp)", m_Stone.NoRecallInto ? "Enable" : "Disable", TownshipSettings.ChangeTravelCharge.ToString("N0")), m_Stone.HasAccess(from, TownshipAccess.CoLeader));
                AddButton((int)Button.ToggleRecallOut, string.Format("{0} Recall Out ({1} gp)", m_Stone.NoRecallOut ? "Enable" : "Disable", TownshipSettings.ChangeTravelCharge.ToString("N0")), m_Stone.HasAccess(from, TownshipAccess.CoLeader));
            }

            AddButton((int)Button.ToggleGateIn, string.Format("{0} Gate In ({1} gp)", m_Stone.NoGateInto ? "Enable" : "Disable", TownshipSettings.ChangeTravelCharge.ToString("N0")), m_Stone.HasAccess(from, TownshipAccess.CoLeader));
            AddButton((int)Button.ToggleGateOut, string.Format("{0} Gate Out ({1} gp)", m_Stone.NoGateOut ? "Enable" : "Disable", TownshipSettings.ChangeTravelCharge.ToString("N0")), m_Stone.HasAccess(from, TownshipAccess.CoLeader));

            AddLine("Daily fees:");

            if (!Core.RuleSets.SiegeRules())
            {
                AddLine("No Recall In: {0} gp", TownshipSettings.NoRecallInFee.ToString("N0"));
                AddLine("No Recall Out: {0} gp", TownshipSettings.NoRecallOutFee.ToString("N0"));
            }

            AddLine("No Gate In: {0} gp", TownshipSettings.NoGateInFee.ToString("N0"));
            AddLine("No Gate Out: {0} gp", TownshipSettings.NoGateOutFee.ToString("N0"));
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Stone.HasNPC(typeof(Mobiles.TSEvocator)))
                return;

            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.CoLeader))
                return;

            switch ((Button)info.ButtonID)
            {
                case Button.ToggleRecallIn:
                    {
                        if (Core.RuleSets.SiegeRules())
                            return;

                        ToggleTravelRule(from, TravelRule.RecallIn);

                        from.CloseGump(typeof(TSEvocatorGump));
                        from.SendGump(new TSEvocatorGump(m_Stone, from));

                        break;
                    }
                case Button.ToggleRecallOut:
                    {
                        if (Core.RuleSets.SiegeRules())
                            return;

                        ToggleTravelRule(from, TravelRule.RecallOut);

                        from.CloseGump(typeof(TSEvocatorGump));
                        from.SendGump(new TSEvocatorGump(m_Stone, from));

                        break;
                    }
                case Button.ToggleGateIn:
                    {
                        ToggleTravelRule(from, TravelRule.GateIn);

                        from.CloseGump(typeof(TSEvocatorGump));
                        from.SendGump(new TSEvocatorGump(m_Stone, from));

                        break;
                    }
                case Button.ToggleGateOut:
                    {
                        ToggleTravelRule(from, TravelRule.GateOut);

                        from.CloseGump(typeof(TSEvocatorGump));
                        from.SendGump(new TSEvocatorGump(m_Stone, from));

                        break;
                    }
            }
        }

        private void ToggleTravelRule(Mobile from, TravelRule travelRule)
        {
            if (DateTime.UtcNow < m_Stone.LastTravelChange + TimeSpan.FromMinutes(30.0))
            {
                from.SendMessage("You must wait at least 30 minutes before changing your township's travel rules again.");
                return;
            }

            if (m_Stone.GoldHeld < TownshipSettings.ChangeTravelCharge)
            {
                from.SendMessage("You lack the necessary funds to change your township's travel rules.");
                return;
            }

            // TODO: Confirm gump?

            bool disabled;

            switch (travelRule)
            {
                case TravelRule.RecallIn: disabled = (m_Stone.NoRecallInto = !m_Stone.NoRecallInto); break;
                case TravelRule.RecallOut: disabled = (m_Stone.NoRecallOut = !m_Stone.NoRecallOut); break;
                case TravelRule.GateIn: disabled = (m_Stone.NoGateInto = !m_Stone.NoGateInto); break;
                case TravelRule.GateOut: disabled = (m_Stone.NoGateOut = !m_Stone.NoGateOut); break;

                default: return;
            }

            m_Stone.LastTravelChange = DateTime.UtcNow;
            m_Stone.GoldHeld -= TownshipSettings.ChangeTravelCharge;

            m_Stone.RecordWithdrawal(TownshipSettings.ChangeTravelCharge, string.Format("{0} changed {1} to {2}", from.Name, Utility.SplitCamelCase(travelRule.ToString()), disabled ? "disabled" : "enabled"));

            from.SendMessage("You {0} {1}.", disabled ? "disabled" : "enabled", Utility.SplitCamelCase(travelRule.ToString()));
        }
    }
}