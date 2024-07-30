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

using Server.Network;

namespace Server
{
    public class CurrentExpansion
    {
        public static void Configure()
        {
            if (Core.RuleSets.AngelIslandRules())
                Core.Expansion = Expansion.AI;
            else if (Core.RuleSets.SiegeStyleRules())
                Core.Expansion = Expansion.SP;
            else if (Core.RuleSets.RenaissanceRules())
                Core.Expansion = Expansion.REN;
            else
                Core.Expansion = Expansion.T2A;

            Core.RuleSets.AOS_SVR = false;

            //MobileStatus.SendAosInfo = Enabled;
            Mobile.InsuranceEnabled = false;
            ObjectPropertyList.Enabled = true; //  false;
            // off on prod, on on test center. You can also turn it on in the core command console (not saved)
            Mobile.VisibleDamageType = (Core.UOTC_CFG == true) ? VisibleDamageType.Related : VisibleDamageType.None;
            Mobile.GuildClickMessage = true;
            Mobile.AsciiClickMessage = true;

            /*
             * if you're looking for SupportedFeatures, they are now in the Expansion.
             */

            if (Core.AOS)
            {
                AOS.DisableStatInfluences();

                if (ObjectPropertyList.Enabled)
                    PacketHandlers.SingleClickProps = true; // single click for everything is overriden to check object property list

                //Mobile.ActionDelay = 1000;
                //Mobile.AOSStatusHandler = new AOSStatusHandler(AOS.GetStatus);
            }
        }
    }
}