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

/* Scripts/Misc/PlayerStepTracking.cs
 * ChangeLog
 *  1/16/11, Pix,
 *      Not used w/Siege.
 *  6/11/04, Pix
 *		Initial version
 */

using Server.Accounting;
using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections;

namespace Server.Misc
{
    public class PlayerStepTracking
    {
        public static void Initialize()
        {
            if (!Core.RuleSets.SiegeStyleRules()) //we don't do this for SP.
            {
                EventSink.Movement += new MovementEventHandler(EventSink_Movement);
            }
        }


        public static void EventSink_Movement(MovementEventArgs e)
        {
            if (!Core.RuleSets.SiegeStyleRules()) //we don't do this for SP.
            {
                Mobile from = e.Mobile;

                if (!from.Player)
                    return;

                if (from is PlayerMobile)
                {
                    Account acct = from.Account as Account;

                    if (acct.m_STIntervalStart + TimeSpan.FromMinutes(20.0) > DateTime.UtcNow)
                    {//within 20 minutes from last step - count step
                        acct.m_STSteps++;
                    }
                    else
                    {
                        //ok, we're outside of a 20-minute period,
                        //so see if they've moved enough within the last 10 
                        //minutes... if so, increment time
                        if (acct.m_STSteps > 50)
                        {
                            //Add an house to the house's refresh time
                            BaseHouse house = null;
                            for (int i = 0; i < 5; i++)
                            {
                                Mobile m = acct[i];
                                if (m != null)
                                {
                                    ArrayList list = BaseHouse.GetHouses(m);
                                    if (list.Count > 0)
                                    {
                                        house = (BaseHouse)list[0];
                                        break;
                                    }
                                }
                            }
                            if (house != null)
                            {
                                house.RefreshHouseOneDay();
                            }
                        }
                        acct.m_STIntervalStart = DateTime.UtcNow;
                        acct.m_STSteps = 1;
                    }
                }
            }
        }

    }
}