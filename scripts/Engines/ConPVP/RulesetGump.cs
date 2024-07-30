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

/* Scripts\Engines\ConPVP\RulesetGump.cs
 * Changelog:
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 */

using Server.Gumps;
using Server.Network;
using System.Collections;

namespace Server.Engines.ConPVP
{
    public class RulesetGump : Gump
    {
        private Mobile m_From;
        private Ruleset m_Ruleset;
        private RulesetLayout m_Page;
        private DuelContext m_DuelContext;
        private bool m_ReadOnly;
        private RulesetClosedHandler m_OnRulesetClosed;

        public void AddGoldenButton(int x, int y, int bid)
        {
            AddButton(x, y, 0xD2, 0xD2, bid, GumpButtonType.Reply, 0);
            AddButton(x + 3, y + 3, 0xD8, 0xD8, bid, GumpButtonType.Reply, 0);
        }

        public RulesetGump(Mobile from, Ruleset ruleset, RulesetLayout page, DuelContext duelContext) : this(from, ruleset, page, duelContext, false)
        {
        }

        public RulesetGump(Mobile from, Ruleset ruleset, RulesetLayout page, DuelContext duelContext, bool readOnly) : this(from, ruleset, page, duelContext, readOnly, null)
        {
        }

        public RulesetGump(Mobile from, Ruleset ruleset, RulesetLayout page, DuelContext duelContext, bool readOnly, RulesetClosedHandler onRulesetClosed) : base(readOnly ? 310 : 50, 50)
        {
            m_From = from;
            m_Ruleset = ruleset;
            m_Page = page;
            m_DuelContext = duelContext;
            m_ReadOnly = readOnly;
            m_OnRulesetClosed = onRulesetClosed;

            Dragable = !readOnly;

            from.CloseGump(typeof(RulesetGump));
            from.CloseGump(typeof(DuelContextGump));
            from.CloseGump(typeof(ParticipantGump));

            RulesetLayout depthCounter = page;
            int depth = 0;

            while (depthCounter != null)
            {
                ++depth;
                depthCounter = depthCounter.Parent;
            }

            int count = page.Children.Length + page.Options.Length;

            AddPage(0);

            int height = 35 + 10 + 2 + (count * 22) + 2 + 30;

            AddBackground(0, 0, 260, height, 9250);
            AddBackground(10, 10, 240, height - 20, 0xDAC);

            AddHtml(35, 25, 190, 20, Center(page.Title), false, false);

            int x = 35;
            int y = 47;

            for (int i = 0; i < page.Children.Length; ++i)
            {
                AddGoldenButton(x, y, 1 + i);
                AddHtml(x + 25, y, 250, 22, page.Children[i].Title, false, false);

                y += 22;
            }

            for (int i = 0; i < page.Options.Length; ++i)
            {
                bool enabled = ruleset.Options[page.Offset + i];

                if (readOnly)
                    AddImage(x, y, enabled ? 0xD3 : 0xD2);
                else
                    AddCheck(x, y, 0xD2, 0xD3, enabled, i);

                AddHtml(x + 25, y, 250, 22, page.Options[i], false, false);

                y += 22;
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_DuelContext != null && !m_DuelContext.Registered)
                return;

            if (!m_ReadOnly)
            {
                BitArray opts = new BitArray(m_Page.Options.Length);

                for (int i = 0; i < info.Switches.Length; ++i)
                {
                    int sid = info.Switches[i];

                    if (sid >= 0 && sid < m_Page.Options.Length)
                        opts[sid] = true;
                }

                for (int i = 0; i < opts.Length; ++i)
                {
                    if (m_Ruleset.Options[m_Page.Offset + i] != opts[i])
                    {
                        m_Ruleset.Options[m_Page.Offset + i] = opts[i];
                        m_Ruleset.Changed = true;
                    }
                }
            }

            int bid = info.ButtonID;

            if (bid == 0)
            {
                if (m_Page.Parent != null)
                    m_From.SendGump(new RulesetGump(m_From, m_Ruleset, m_Page.Parent, m_DuelContext, m_ReadOnly, m_OnRulesetClosed));
                else if (!m_ReadOnly)
                    m_From.SendGump(new PickRulesetGump(m_From, m_DuelContext, m_Ruleset, m_OnRulesetClosed));
            }
            else
            {
                bid -= 1;

                if (bid >= 0 && bid < m_Page.Children.Length)
                    m_From.SendGump(new RulesetGump(m_From, m_Ruleset, m_Page.Children[bid], m_DuelContext, m_ReadOnly, m_OnRulesetClosed));
            }
        }
    }
}