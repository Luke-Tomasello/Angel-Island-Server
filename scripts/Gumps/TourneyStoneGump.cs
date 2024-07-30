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

/* Scripts/Gumps/TourneyStoneGump.cs
 * ChangeLog :
 *  08/07/06, Kit
 *		Added try/catch to convert to int call that was crashing server with overflows.
 *	04/25/06, weaver
 *		- Added check to prevent fatal exception when sub component is used without
 *		the rest of the stone (only occurs if placed improperly via [add).
 *		- Changed comment instances of 'erlein' to 'weaver'.
 *	05/28/05, weaver
 *		Slightly modified gump to accommodate larger rules.
 *		Fixed the multiple condition editing labels.
 *	05/24/05, weaver
 *		Fully adapted to new condition based rule system.
 *	05/18/05, weaver
 *		Removed referencing to "opens are open" flag in TourneyStone.
 *	05/17/05, weaver
 *		Made gump close + not toggle first rule on right click.
 *		Added new fields + handling for configurable rules.
 *		Added new mini prompt to deal with new fields.
 *	05/15/05, weaver
 *		Initial creation.
 */

using Server.Items;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace Server.Gumps
{
    public class TourneyStoneUseGump : Gump
    {
        private TourneyStoneAddon m_TourneyStone;
        private int m_ConfCount;

        public TourneyStoneUseGump(Mobile from, TourneyStoneAddon tsaddon)
            : base(50, 50)
        {
            from.CloseGump(typeof(TourneyStoneUseGump));

            // Additional check to ensure we have a full tourney stone (!)
            if (tsaddon == null)
            {
                from.SendMessage("I'm afraid there is a fault. Please contact your supplier and request maintenance. Apologies for any inconveniences caused.");
                return;
            }

            m_TourneyStone = tsaddon;

            int RuleCount = m_TourneyStone.Ruleset.Count;

            // Calculate the gump sizes
            int optionheight = 50;
            int height = (RuleCount * optionheight) + 128;
            int width = 450;


            AddPage(0);

            AddBackground(10, 10, width, height, 0x242C);

            AddHtml(10, 20, width, 50, "<div align=CENTER>Tournament Stone Options</div>", false, false);
            AddHtml(10, 35, width, 50, string.Format("<div align=CENTER>Version {0}</div>", m_TourneyStone.RulesetVersion), false, false);

            int confcount = 0;

            for (int irule = 0; irule < RuleCount; irule++)
            {
                Rule CurRule = (Rule)m_TourneyStone.Ruleset[irule];

                string CurDesc = CurRule.DynFill(CurRule.Desc);

                AddHtml(90, 80 + (irule * optionheight), width - (80 * 2), 75, CurDesc, false, false);
                AddButton(45, 80 + (irule * optionheight), 4005, 4007, irule + 1, GumpButtonType.Reply, 0);
                AddHtml(width - 55, 80 + (irule * optionheight), width, 75, (((Rule)m_TourneyStone.Ruleset[irule]).Active ? "<b>Active</b>" : "Inactive"), false, false);


                bool confound = false;

                for (int rcpos = 0; rcpos < CurRule.Conditions.Count; rcpos++)
                    if (((RuleCondition)CurRule.Conditions[rcpos]).Configurable)
                        confound = true;

                if (confound)
                {
                    // Add configure button
                    AddButton(90, 105 + (irule * optionheight) + 3, 0x15E1, 0x15E5, RuleCount + irule, GumpButtonType.Reply, 0);
                    AddHtml(120, 105 + (irule * optionheight), 90, 110 + (irule * optionheight), "Edit", false, false);
                    confcount++;
                }
            }

            AddButton((width / 2) - 8, (height - 40), 0x81A, 0x81B, 98, GumpButtonType.Reply, 0); // Okay
            m_ConfCount = confcount;
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            if (info.ButtonID == 98 || info.ButtonID == 0)
                return;

            if (info.ButtonID - 1 < m_TourneyStone.Ruleset.Count)
            {
                ((Rule)m_TourneyStone.Ruleset[info.ButtonID - 1]).Active = !(((Rule)m_TourneyStone.Ruleset[info.ButtonID - 1]).Active);

                from.SendGump(new TourneyStoneUseGump(from, m_TourneyStone));
                return;
            }

            int rulecount = m_TourneyStone.Ruleset.Count;
            for (int confcheck = rulecount; confcheck < (rulecount * 2); confcheck++)
            {
                if (info.ButtonID == confcheck)
                {
                    int RelRuleIdx = confcheck - rulecount;
                    from.SendGump(new TourneyStoneEditGump(from, ref m_TourneyStone, RelRuleIdx));
                }
            }
        }
    }

    public class TourneyStoneEditGump : Gump
    {
        private TourneyStoneAddon m_TourneyStone;
        //private int m_ConfCount;
        private int m_iRule;

        private ArrayList m_ConfigOptions;
        public ArrayList ConfigOptions { get { return m_ConfigOptions; } set { m_ConfigOptions = value; } }

        public TourneyStoneEditGump(Mobile from, ref TourneyStoneAddon tsaddon, int irule)
            : base(50, 50)
        {
            from.CloseGump(typeof(TourneyStoneEditGump));
            m_TourneyStone = tsaddon;
            m_iRule = irule;

            ConfigOptions = new ArrayList();
            Rule CurRule = (Rule)m_TourneyStone.Ruleset[irule];

            // Ascertain which conditions are configurable and add them into array for reference
            foreach (RuleCondition rc in CurRule.Conditions)
                if (rc.Configurable)
                    ConfigOptions.Add(rc);

            int CondCount = ConfigOptions.Count;

            // Calculate the gump sizes
            int optionheight = 40;
            int height = (CondCount * optionheight) + 140;
            int width = 350;

            AddPage(0);
            AddBackground(10, 10, width, height, 0x242C);
            string CurDesc = CurRule.DynFill(CurRule.Desc);

            AddHtml(50, 35, width - 60, 50, string.Format("<div align=LEFT>Editing rule '{0}'</div>", CurDesc), false, false);
            AddHtml(50, 75, width, 50, string.Format("<div align=LEFT>Conditions :</div>", CurDesc), false, false);

            for (int icon = 0; icon < CondCount; icon++)
            {
                RuleCondition CurCon = (RuleCondition)ConfigOptions[icon];

                if (CurCon.PropertyVal == "")
                {
                    // Quantity is customizable property of this condition

                    AddButton(50, 105 + (optionheight * icon), 0x15E1, 0x15E5, icon + 1, GumpButtonType.Reply, 0);
                    AddHtml(100, 105 + (optionheight * icon), width, 50, string.Format("<div align=left>Quantity : {0}</div>", CurCon.Quantity), false, false);

                }
                else
                {
                    // PropertyVal is customizable property of this condition
                    AddButton(50, 105 + (optionheight * icon), 0x15E1, 0x15E5, icon + 1, GumpButtonType.Reply, 0);
                    AddHtml(100, 105 + (optionheight * icon), width, 50, string.Format("<div align=left>Value : {0}</div>", CurCon.PropertyVal), false, false);
                }

            }

            AddButton((width / 2) - 8, (height - 40), 0x81A, 0x81B, 98, GumpButtonType.Reply, 0); // Okay

        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            if (info.ButtonID == 0 || info.ButtonID == 98)
            {
                from.SendGump(new TourneyStoneUseGump(from, m_TourneyStone));
                return;
            }

            if (info.ButtonID > ConfigOptions.Count)
                return;

            // Condition of this rule that we need to change :
            // (1 + condition number) index in ConfigOptions

            // ((RuleCondition) ConfigOptions[ info.ButtonID - 1 ]);
            RuleCondition rc = (RuleCondition)ConfigOptions[info.ButtonID - 1];

            from.Prompt = new AlterCondValPrompt(info.ButtonID - 1, m_iRule, ref m_TourneyStone, from);
            //from.SendGump( new TourneyStoneEditGump( from, ref m_TourneyStone, m_iRule ) );
            return;
        }
    }

    public class AlterCondValPrompt : Prompt
    {

        private int m_iRule;
        private int m_CondID;
        //private Mobile from;
        public TourneyStoneAddon m_TourneyStone;

        public AlterCondValPrompt(int condid, int ruleid, ref TourneyStoneAddon ts, Mobile from)
        {
            m_iRule = ruleid;
            m_TourneyStone = ts;
            m_CondID = condid;

            string sMessage = "";

            if (((RuleCondition)((Rule)ts.Ruleset[ruleid]).Conditions[condid]).PropertyVal == "")
            {
                // It's the quantity
                sMessage = "Please enter the new quantity for this condition :";
            }
            else
            {
                // It's the property value
                sMessage = "Please enter the new value for this condition :";
            }

            from.SendMessage(sMessage);
        }

        public override void OnResponse(Mobile from, string text)
        {
            text = text.Trim();

            Rule EditRule = (Rule)m_TourneyStone.Ruleset[m_iRule];
            RuleCondition EditCon = (RuleCondition)EditRule.Conditions[m_CondID];

            Regex NumMatch = new Regex("^[0-9]+$");
            Regex AlphNum = new Regex("^[0-9A-Za-z]+$");

            if (EditCon.PropertyVal == "")
            {
                if (!NumMatch.IsMatch(text))
                    from.SendMessage("Non numeric characters are not permitted.");
                else
                {
                    try
                    {
                        EditCon.Quantity = Convert.ToInt32(text);
                    }
                    catch
                    {
                        from.SendMessage("You have entered a invalid quantity");
                    }
                }
            }
            else
            {
                if (!AlphNum.IsMatch(text))
                    from.SendMessage("Only alpha numeric characters are permitted.");
                else
                    EditCon.PropertyVal = text;
            }

            from.SendGump(new TourneyStoneEditGump(from, ref m_TourneyStone, m_iRule));
        }

        public override void OnCancel(Mobile from)
        {
            from.SendGump(new TourneyStoneEditGump(from, ref m_TourneyStone, m_iRule));
        }
    }
}