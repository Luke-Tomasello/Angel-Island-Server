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

/* Scripts/Gumps/ControlDebuggerRelationshipVisualizerGump.cs
 * ChangeLog
 *  6/1/2024, Adam,
 *		implementation
 */

using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Gumps
{
    public class ControlRelationshipVisualizerGump : Gump
    {
        private Mobile m_Mobile;
        private List<string> m_Chains;

        public ControlRelationshipVisualizerGump(Mobile mobile, List<string> chains)
            : base(110, 100)
        {
            m_Mobile = mobile;
            m_Chains = chains;

            mobile.CloseGump(typeof(ControlRelationshipVisualizerGump));

            Closable = true;
            Resizable = true;

            AddPage(0);

            AddBackground(0, 0, 420, 280, 5054);

            AddImageTiled(10, 10, 400, 20, 2624);
            AddAlphaRegion(10, 10, 400, 20);

            AddHtml(10, 10, 400, 20, Color(Center("Control Relationship Visualizer"), 0xFFFFFF), false, false);

            AddImageTiled(10, 40, 400, 200, 2624);
            AddAlphaRegion(10, 40, 400, 200);

            // The following warning is more appropriate for AI than the localized warning.
            String WarningString = string.Empty;//  string.Format("You are about to demolish your house. A deed to the house will be placed in your {0}. All items in the house will remain behind and can be freely picked up by anyone. Once the house is demolished, anyone can attempt to place a new house on the vacant land. Are you sure you wish to continue?", m_Mobile.Red ? "backpack" : "bank box");
            foreach (string ch in m_Chains)
            {
                string[] tokens = ch.Split("=>", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                tokens[0] = Color(tokens[0], /*0x0000FF*/0xFF7A59);
                for (int ix = 0; ix < tokens.Length; ix++)
                {
                    WarningString += tokens[ix] + " => ";
                    //if (ix > 0 && ix % 3 == 0)
                    //    WarningString += "<br>";
                }
                // cleanup the tail
                if (WarningString.EndsWith("<br>"))
                    WarningString = WarningString.Substring(0, WarningString.LastIndexOf("<br>"));
                if (WarningString.EndsWith(" => "))
                    WarningString = WarningString.Substring(0, WarningString.LastIndexOf(" => "));

                WarningString += "<br>";
            }
            //WarningString = WarningString.Replace("BASEFONT", "\"Verdana\", 8");
            //WarningString = "<p style=\"font-size:8px\">" + WarningString + "</p>";
            AddHtml(10, 40, 400, 200, WarningString, false, true);

            //			AddHtmlLocalized( 10, 40, 400, 200, 1061795, 32512, false, true ); /* You are about to demolish your house.
            //																				* You will be refunded the house's value directly to your bank box.
            //																				* All items in the house will remain behind and can be freely picked up by anyone.
            //																				* Once the house is demolished, anyone can attempt to place a new house on the vacant land.
            //																				* This action will not un-condemn any other houses on your account, nor will it end your 7-day waiting period (if it applies to you).
            //																				* Are you sure you wish to continue?
            //																				*/

            AddImageTiled(10, 250, 400, 20, 2624);
            AddAlphaRegion(10, 250, 400, 20);

            //AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
            //AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

            //AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
            //AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL

            AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 250, 170, 20, 1060675, 32767, false, false); // CLOSE

        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                ;
            }
        }
    }
}