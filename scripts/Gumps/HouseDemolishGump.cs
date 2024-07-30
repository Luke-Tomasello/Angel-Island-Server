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

/* Scripts/Gumps/HouseDemolishGump.cs
 * ChangeLog
 *  2/18/2024, Adam,
 *      Players now need to 
 *          1. move the townstone out of the house to redeed
 *          2. dismiss/redeed any Township NPCs
 *  4/13/23, Adam (Reds)
 *      If you're a murderer, the deed goes into your backpack.
 *      If blue, directly to the bank
 *      http://web.archive.org/web/20010803144701fw_/http://uo.stratics.com/homes/
 *  7/20/08, Pix
 *		De-coupled township stones from houses.
 *  7/6/08, Adam
 *      Fix text typo
 *  8/07/06, Rhiannon
 *		Changed warning gump to reflect how demolishing a house works on AI.
 *	2/20/06, Adam
 *		Add check m_House.FindPlayer() to find players in a house.
 *		We no longer allow a house to be deeded when players are inside (on roof) 
 *		This was used as an exploit.
 */

using Server.Items;
using Server.Multis;
using Server.Network;
using System;

namespace Server.Gumps
{
    public class HouseDemolishGump : Gump
    {
        private Mobile m_Mobile;
        private BaseHouse m_House;

        public HouseDemolishGump(Mobile mobile, BaseHouse house)
            : base(110, 100)
        {
            m_Mobile = mobile;
            m_House = house;

            mobile.CloseGump(typeof(HouseDemolishGump));

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, 420, 280, 5054);

            AddImageTiled(10, 10, 400, 20, 2624);
            AddAlphaRegion(10, 10, 400, 20);

            AddHtmlLocalized(10, 10, 400, 20, 1060635, 30720, false, false); // <CENTER>WARNING</CENTER>

            AddImageTiled(10, 40, 400, 200, 2624);
            AddAlphaRegion(10, 40, 400, 200);

            // The following warning is more appropriate for AI than the localized warning.
            String WarningString = string.Format("You are about to demolish your house. A deed to the house will be placed in your {0}. All items in the house will remain behind and can be freely picked up by anyone. Once the house is demolished, anyone can attempt to place a new house on the vacant land. Are you sure you wish to continue?", m_Mobile.Red ? "backpack" : "bank box");
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

            AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

            AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 1 && !m_House.Deleted)
            {
                if (m_House.IsOwner(m_Mobile))
                {
                    if (m_House.FindGuildstone() != null)
                    {
                        m_Mobile.SendLocalizedMessage(501389); // You cannot redeed a house with a guildstone inside.
                        return;
                    }
                    // 2/18/2024, Adam: Putting the next two 'else if' clauses back. We want players to manage this so we don't have to
                    else if (m_House.FindTownshipStone() != null)
                    {
                        m_Mobile.SendMessage("You can't demolish a house which holds a Township stone.");
                        return;
                    }
                    //Players need to clean this up themselves
                    else if (m_House.FindTownshipNPCs().Count > 0)
                    {
                        m_Mobile.SendMessage("You need to dismiss your Township NPC before moving.");
                        return;
                    }
                    else if (m_House.FindPlayerVendor() != null)
                    {
                        m_Mobile.SendLocalizedMessage(503236); // You need to collect your vendor's belongings before moving.
                        return;
                    }
                    else if (m_House.FindPlayer() != null)
                    {
                        m_Mobile.SendMessage("It is not safe to demolish this house with someone still inside."); // You need to collect your vendor's belongings before moving.
                                                                                                                  //Tell staff that an exploit is in progress
                                                                                                                  //Server.Commands.CommandHandlers.BroadcastMessage( AccessLevel.Counselor, 
                                                                                                                  //0x482, 
                                                                                                                  //String.Format( "Exploit in progress at {0}. Stay hidden, Jail involved players, get acct name, ban.", m_House.Location.ToString() ) );
                        return;
                    }

                    Item toGive = null;
                    Item Refund = null;     // for various home upgrades

                    if (m_House.IsAosRules)
                    {
                        if (m_House.Price > 0)
                            toGive = new BankCheck(m_House.Price);
                        else
                            toGive = m_House.GetDeed();
                    }
                    else
                    {
                        toGive = m_House.GetDeed();

                        if (toGive == null && m_House.Price > 0)
                            toGive = new BankCheck(m_House.Price);

                        if (m_House.UpgradeCosts > 0)
                            Refund = new BankCheck((int)m_House.UpgradeCosts);
                    }

                    // for reds it goes in their backpack. For blues, bank box
                    Container box = m_Mobile.Red ? m_Mobile.Backpack : m_Mobile.BankBox;

                    if (toGive != null && box != null)
                    {
                        // Adam: TryDropItem() fails if the bank is full, and this isn't the time to be 
                        //  failing .. just overload their bank.
                        if (box != null /*&& box.TryDropItem( m_Mobile, toGive, false )*/ )
                        {
                            box.AddItem(toGive);
                            if (toGive is BankCheck)
                                m_Mobile.SendLocalizedMessage(1060397, ((BankCheck)toGive).Worth.ToString()); // ~1_AMOUNT~ gold has been deposited into your bank box.

                            if (Refund != null)
                            {
                                box.AddItem(Refund);
                                if (Refund is BankCheck)
                                    m_Mobile.SendLocalizedMessage(1060397, ((BankCheck)Refund).Worth.ToString()); // ~1_AMOUNT~ gold has been deposited into your bank box.
                            }

                            m_House.RemoveKeys(m_Mobile);
                            m_House.Delete();
                        }
                        else
                        {
                            toGive.Delete();
                            m_Mobile.SendLocalizedMessage(500390); // Your bank box is full.
                        }
                    }
                    else
                    {
                        m_Mobile.SendMessage("Unable to refund house.");
                    }
                }
                else
                {
                    m_Mobile.SendLocalizedMessage(501320); // Only the house owner may do this.
                }
            }
        }
    }
}