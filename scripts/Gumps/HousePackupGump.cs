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

/* Scripts/Gumps/HousePackUpGump.cs
 * ChangeLog
 *  2/1/2024
 *      initial creation
 */

using Server.Diagnostics;
using Server.Multis;
using Server.Network;
using System;

namespace Server.Gumps
{
    public class HousePackUpGump : Gump
    {
        private Mobile m_Mobile;
        private BaseHouse m_House;

        public HousePackUpGump(Mobile mobile, BaseHouse house)
            : base(110, 100)
        {
            m_Mobile = mobile;
            m_House = house;

            mobile.CloseGump(typeof(HousePackUpGump));

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, 420, 280, 5054);

            AddImageTiled(10, 10, 400, 20, 2624);
            AddAlphaRegion(10, 10, 400, 20);

            AddHtmlLocalized(10, 10, 400, 20, 1060635, 30720, false, false); // <CENTER>WARNING</CENTER>

            AddImageTiled(10, 40, 400, 200, 2624);
            AddAlphaRegion(10, 40, 400, 200);

            // The following warning is more appropriate for AI than the localized warning.
            String WarningString =
                "You are about to pack up your house. " +
                "A deed and key for your belongings will be placed in your backpack. " +
                //"All items in the house will remain behind and can be freely picked up by anyone. " +
                //"Once the house is demolished, anyone can attempt to place a new house on the vacant land. " +
                "Are you sure you wish to continue?";

            AddHtml(10, 40, 400, 200, WarningString, false, true);

            AddImageTiled(10, 250, 400, 20, 2624);
            AddAlphaRegion(10, 250, 400, 20);

            AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

            AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL
        }
        //private bool CheckAddonPreviews(Mobile from, BaseHouse house)
        //{
        //    if (house.Addons != null)
        //        foreach (var o in house.Addons)
        //            if (o is BaseAddon ba && ba.GetItemBool(ItemBoolTable.Preview))
        //                return true;
        //    return false;
        //}
        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (Core.RuleSets.PackUpStructureRules())
            {
                if (info.ButtonID == 1 && !m_House.Deleted)
                {
                    if (m_House.IsOwner(m_Mobile))
                    {
                        LogHelper logger = new LogHelper("Pack up house.log", overwrite: false, sline: true);
                        try
                        {
                            if (m_House.IsPackedUp())
                                state.Mobile.SendMessage("You will need to finish unpacking before you can pack it up again.");
                            //else if (CheckAddonPreviews(state.Mobile, m_House))
                            //    // exploit prevention
                            //    state.Mobile.SendMessage("You cannot pack up a house while previewing an addon.");
                            else
                                m_House.PackUpHouse(state.Mobile, logger);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(ex);
                        }
                        finally
                        {
                            logger.Finish();
                        }

                    }
                    else
                    {
                        m_Mobile.SendLocalizedMessage(501320); // Only the house owner may do this.
                    }
                }
            }
            else
                state.Mobile.SendMessage("Currently unavailable on production shards.");
        }
    }
}