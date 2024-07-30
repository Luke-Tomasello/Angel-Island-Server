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

/* Scripts/Items/Addons/TourneyStone/Rule.cs
 * ChangeLog :
 * 	02/25/07, weaver
 *		Initial creation (code moved out of TournamentStoneAddon.cs)
 *
 */

using System.Collections;

namespace Server.Items
{


    public class Rule
    {
        private string m_Desc;
        private string m_FailText;
        private string m_FailTextDyn;

        private bool m_Active;
        private ArrayList m_Conditions;

        public ArrayList Conditions
        {
            get { return m_Conditions; }
            set { m_Conditions = value; }
        }

        public string Desc
        {
            get { return m_Desc; }
            set { m_Desc = value; }
        }

        public string FailText
        {
            get { return m_FailText; }
            set { m_FailText = value; }
        }

        public string FailTextDyn
        {
            get { return m_FailTextDyn; }
            set { m_FailTextDyn = value; }
        }

        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; }
        }

        public Rule()
        {
            Desc = "";
            FailText = "";
            FailTextDyn = "";
            Active = false;
        }

        public string DynFill(string sFilltp)
        {
            string sFilled = sFilltp;
            for (int cpos = 0; cpos < Conditions.Count; cpos++)
            {
                RuleCondition RuleCon = (RuleCondition)Conditions[cpos];

                sFilled = sFilled.Replace("%Condition" + (cpos + 1) + "_Quantity%", RuleCon.Quantity.ToString());
                sFilled = sFilled.Replace("%Condition" + (cpos + 1) + "_Property%", RuleCon.Property);
                sFilled = sFilled.Replace("%Condition" + (cpos + 1) + "_PropertyVal%", RuleCon.PropertyVal);
            }

            return sFilled;
        }

    }

}