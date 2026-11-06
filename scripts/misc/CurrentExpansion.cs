/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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
                Core.Expansion = Expansion.AngelIsland;

            else if (Core.RuleSets.SiegeRules())
                Core.Expansion = Expansion.SiegePerilous;

            else if (Core.RuleSets.MortalisRules())
                Core.Expansion = Expansion.Mortalis;

            else if (Core.RuleSets.SeasonsRules())
                Core.Expansion = Expansion.Seasons;

            else if (Core.RuleSets.UpheavalRules())
                Core.Expansion = Expansion.Upheaval;

            else
                Core.Expansion = Expansion.T2A;

            Core.RuleSets.OSIAOS_SVR = false;

            /* New Server */
            // new code to patch expansion for season 3
            if ((Core.RuleSets.SeasonsRules(3) || Core.RuleSets.UpheavalRules()))
            {
                var patch = Server.ExpansionInfo.GetInfo(Core.Expansion);
                patch.CharacterListFlags |= CharacterListFlags.OneCharacterSlot;
            }

            //MobileStatus.SendAosInfo = Enabled;
            Mobile.InsuranceEnabled = false;
            ObjectPropertyList.Enabled = true; //  false;
            // off on prod, on on test center. You can also turn it on in the core command console (not saved)
            Mobile.VisibleDamageType = (Core.GMNTC_CFG == true) ? VisibleDamageType.Related : VisibleDamageType.None;
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