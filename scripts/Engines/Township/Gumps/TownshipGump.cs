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

/* Scripts/Engines/Township/TownshipStone.cs
 * CHANGELOG:
 *  8/23/23, Yoar
 *      Complete refactor. Moved a lot of functionality to separate source files.
 *  3/24/22, Yoar
 *      Added marble/sandstone to list of stockpile items.
 *      Added support for TownshipStockpileDeed.
 *  3/12/22, Adam
 *      Add Fertile Dirt to list of stockpile items
 * 2/18/22, Yoar
 *      Replaced NPC 'Dismiss' button with 'Manage' button which opens the TownshipNPCGump.
 * 2/18/22, Yoar
 *      Added TownshipStone.CanView checks.
 *      You may now add stock from pack animals.
 * 2/15/22, Yoar
 *      Now consistently using either the term 'charge' or 'fee' to describe flat payments vs daily fees respectively.
 * 2/2/22, Yoar
 *      Refactored completely + small tweaks to the gump layout.
 * 8/10/21, pix
 *      No UnemploymentCheck for township funds.
 *	05/06/09, plasma
 *		Added new security method that asserts the same rules for button creation
 *		Prevents exploits as its possible with razor to respond to any gump with any ID.
 * 11/26/08, Pix
 *		Added house distance check and teleporter distance check to moving township stone.
 *	8/3/08, Pix
 *		Change for CanExtend() call - now returns a reason.
 *		Also fixed placement of staff-only button.
 * 7/20/08, Pix
 *		De-coupled township stones from houses.
 *	12/11/07, Pix
 *		Since allies can now access stone, make sure they're restricted to just
 *		view access (non-guildmaster and non-admin) even if they're co-owned to the house.
 *	Pix: 4/30/07,
 *		Resticted Update Enemy list button.
 *		Added activity indicator.
 *	Pix: 4/19/07
 *		Added dials for all fees/charges and modifiers.
 */

using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Township;
using System;
using System.Collections.Generic;

namespace Server.Gumps
{
    public class TownshipGump : BaseTownshipGump
    {
        public enum Page
        {
            Main,
            DailyFees,
            LastWithdrawals,
            LastDeposits,
            NPCPurchase,
            NPCManage,
            Stockpile,
            EnemyList,
            DeleteTownship,
            BuildingPermits,
            Messages,
            PackUpTownship,
            Subsidies
        }

        private enum Button
        {
            Close,

            // page buttons 1-99
            Page_Main,
            Page_DailyFees,
            Page_LastWithdrawals,
            Page_LastDeposits,
            Page_NPCPurchase,
            Page_NPCManage,
            Page_Stockpile,
            Page_EnemyList,
            Page_DeleteTownship,
            Page_BuildingPermits,
            Page_Messages,
            Page_PackUpTownship,
            Page_Subsidies,

            // action buttons 100-999
            AddFunds = 100,
            ToggleNpcInteraction,
            AddStock,
            SyncEnemyList,
            MoveStone,
            DeleteTownship,
            GrantBuildingPermit,
            PackUpTownship,

            // list buttons >= 1000
            NPCPurchase = 1000,
            NPCManage = 2000,
            NPCMenu = 3000,
            RevokeBuildingPermit = 4000,
        }

        private object m_State;

        public TownshipGump(TownshipStone stone, Mobile from)
            : this(stone, from, Page.Main)
        {
        }

        public TownshipGump(TownshipStone stone, Mobile from, Page page)
            : base(stone)
        {
            AddPage(0);

            AddBackground();

            AddTitle(string.Format("The {0} of {1}", TownshipStone.GetTownshipSizeDesc(m_Stone.ActivityLevel).ToLower(), m_Stone.GuildName));

            m_Y = 380;

            if (page == Page.Main || !m_Stone.HasAccess(from, TownshipAccess.Ally))
                AddGridButton(2, 0, (int)Button.Close, "Exit");
            else
                AddGridButton(2, 0, (int)Button.Page_Main, "Return to the main menu");

            m_Y = 50;

            switch (page)
            {
                case Page.Main:
                    MainPage(from);
                    break;
                case Page.DailyFees:
                    DailyFeesPage(from);
                    break;
                case Page.LastWithdrawals:
                    LastWithdrawalsPage(from);
                    break;
                case Page.LastDeposits:
                    LastDepositsPage(from);
                    break;
                case Page.NPCPurchase:
                    NPCPurchasePage(from);
                    break;
                case Page.NPCManage:
                    NPCManagePage(from);
                    break;
                case Page.Stockpile:
                    StockpilePage(from);
                    break;
                case Page.EnemyList:
                    EnemyListPage(from);
                    break;
                case Page.DeleteTownship:
                    DeleteTownshipPage(from);
                    break;
                case Page.PackUpTownship:
                    PackUpTownshipPage(from);
                    break;
                case Page.Subsidies:
                    SubsidiesPage(from);
                    break;
                case Page.BuildingPermits:
                    BuildingPermitsPage(from);
                    break;
                case Page.Messages:
                    MessagesPage(from);
                    break;
            }
        }

        private void MainPage(Mobile from)
        {
            AddGridText(2, 0, "Size: {0}", TownshipStone.GetTownshipSizeDesc(m_Stone.ActivityLevel));
            AddGridText(2, 1, "Activity: {0}", TownshipStone.GetTownshipActivityDesc(m_Stone.LastActivityLevel));

            AddGridText(2, 0, "Total fee per RL day: {0}", m_Stone.TotalFeePerDay.ToString("N0"));
            AddGridText(2, 1, "NPC fee per RL day: {0}", m_Stone.CalculateNPCFee().ToString("N0"));

            AddGridText(2, 0, "Funds held: {0}", m_Stone.GoldHeld.ToString("N0"));
            AddGridText(2, 1, "Funds for {0:F2} day(s) held", m_Stone.RLDaysLeftInFund);

            AddGridText(2, 0, "Lockdowns: {0:N0}/{1:N0}", m_Stone.LockdownRegistry.Count, m_Stone.MaxLockDowns);
            AddGridText(2, 1, "Lockboxes: {0:N0}/{1:N0}", m_Stone.LockBoxes, m_Stone.MaxLockBoxes);

            AddGridButton(2, 0, (int)Button.AddFunds, "Add Funds");
            AddGridButton(2, 1, (int)Button.Page_DailyFees, "Last Daily Fees Breakdown");

            AddGridButton(2, 0, (int)Button.Page_LastWithdrawals, "Last Withdrawals Record");
            AddGridButton(2, 1, (int)Button.Page_LastDeposits, "Last Deposits Record");

            AddGridButton(2, 0, (int)Button.Page_NPCPurchase, "Buy Township NPC");
            AddGridButton(2, 1, (int)Button.Page_NPCManage, "Manage Township NPCs");

            AddGridButton(2, 0, (int)Button.Page_Stockpile, "Stockpile");
            AddGridButton(2, 1, (int)Button.Page_BuildingPermits, "Building Permits");

            AddGridButton(2, 0, (int)Button.Page_Messages, "Messages");
            AddGridButton(2, 1, (int)Button.Page_EnemyList, "Enemy List");

            if (m_Stone.HasAccess(from, TownshipAccess.Leader))
            {
                AddGridButton(2, 0, (int)Button.MoveStone, "Move Stone");
                AddGridButton(2, 1, (int)Button.Page_DeleteTownship, "Delete Township");
                AddGridButton(2, 0, (int)Button.Page_Subsidies, "Subsidies");
                AddGridButton(2, 1, (int)Button.Page_PackUpTownship, "Pack Up Township");

            }
        }
        private void SubsidiesPage(Mobile from)
        {
            AddLine("Subsidies");

            AddText(300, m_Stone.FameAndTaxBankHTML);
        }

        private void DailyFeesPage(Mobile from)
        {
            AddLine("Last Daily Fees Breakdown");

            AddText(300, m_Stone.DailyFeesHTML);
        }

        private void LastWithdrawalsPage(Mobile from)
        {
            AddLine("Record of Last Withdrawals");

            AddText(300, m_Stone.LastWithdrawalsHTML);
        }

        private void LastDepositsPage(Mobile from)
        {
            AddLine("Record of Last Deposits");

            AddText(300, m_Stone.LastDepositsHTML);
        }
        /* Adam: Important design note, heh
         * private Township.ActivityLevel m_ActivityLevel; //this is the 'size' that the town has grown to
         * private Township.ActivityLevel m_LastActualActivityLevel; //this is the actual activity of the town last week (what we base NPC fees on)
         * So... once you reach BOOMING for instance, it never goes down and you will always be able to purchase all the NPCs. 
         * However m_LastActualActivityLevel dictates the cost of those NPCs
         */
        private void NPCPurchasePage(Mobile from)
        {
            List<Type> buyList = new List<Type>();

            foreach (Type type in TownshipNPCHelper.BuyList)
            {
                if (m_Stone.ActivityLevel >= TownshipNPCHelper.GetNPCActivityReq(type))
                    buyList.Add(type);
            }

            Type[] buyArray = buyList.ToArray();

            m_State = buyArray;

            AddLine("Purchase NPCs");

            AddLine();

            AddList(buyArray, 8, delegate (int index)
            {
                Type type = buyArray[index];

                AddButton((int)Button.NPCPurchase + index, string.Format("Buy {0} ({1} gp, {2} gp per RL day)", TownshipNPCHelper.GetNPCName(type), TownshipNPCHelper.GetNPCCharge(type).ToString("N0"), m_Stone.ModifyNPCFee(TownshipNPCHelper.GetNPCFee(type)).ToString("N0")));
            });
        }

        private void NPCManagePage(Mobile from)
        {
            Mobile[] npcs = m_Stone.TownshipNPCs.ToArray();

            m_State = npcs;

            AddLine("Manage NPCs");

            AddButton((int)Button.ToggleNpcInteraction, m_Stone.OutsideNPCInteractionAllowed ? "Disable Outside Interaction" : "Enable Outside Interaction", m_Stone.HasAccess(from, TownshipAccess.CoLeader));

            AddList(npcs, 8, delegate (int index)
            {
                Mobile npc = npcs[index];

                AddLine("{0} {1}", npc.Name, npc.Title);

                if (TownshipNPCHelper.IsOwner(npc, from))
                    AddGridButton(6, 4, (int)Button.NPCManage + index, "Manage");

                if (TownshipNPCHelper.HasNPCMenu(npc.GetType()))
                    AddGridButton(6, 5, (int)Button.NPCMenu + index, "Menu");
            });
        }

        private void StockpilePage(Mobile from)
        {
            AddLine("Stockpile");

            AddButton((int)Button.AddStock, "Add To Stockpile");

            AddGridText(2, 0, "Boards :");
            AddGridText(2, 1, m_Stone.Stockpile.Boards.ToString("N0"));

            AddGridText(2, 0, "Ingots :");
            AddGridText(2, 1, m_Stone.Stockpile.Ingots.ToString("N0"));

            AddGridText(2, 0, "Granite :");
            AddGridText(2, 1, m_Stone.Stockpile.Granite.ToString("N0"));

            AddGridText(2, 0, "Marble :");
            AddGridText(2, 1, m_Stone.Stockpile.Marble.ToString("N0"));

            AddGridText(2, 0, "Sandstone :");
            AddGridText(2, 1, m_Stone.Stockpile.Sandstone.ToString("N0"));

            AddGridText(2, 0, "Fertile Dirt :");
            AddGridText(2, 1, m_Stone.Stockpile.FertileDirt.ToString("N0"));

            AddGridText(2, 0, "Nightshade :");
            AddGridText(2, 1, m_Stone.Stockpile.Nightshade.ToString("N0"));
        }

        private void EnemyListPage(Mobile from)
        {
            AddLine("Enemy List");

            AddButton((int)Button.SyncEnemyList, string.Format("Update Enemy List ({0} gp)", TownshipSettings.UpdateEnemyCharge.ToString("N0")), m_Stone.HasAccess(from, TownshipAccess.CoLeader));

            AddList(m_Stone.Enemies, 8, delegate (int index)
            {
                AddLine(m_Stone.Enemies[index].Name);
            });
        }

        private void DeleteTownshipPage(Mobile from)
        {
            if ((m_Stone != null && m_Stone.Guild != null && m_Stone.Guild.FixedGuildmaster == false) || (from != null && from.AccessLevel > AccessLevel.GameMaster))
            {
                AddLine("Delete Township?");

                AddButton((int)Button.DeleteTownship, "YES, delete it forever!", m_Stone.HasAccess(from, TownshipAccess.Leader));
            }
            else
                AddLine("The guild for this township is staff-owned, and as such, it may not be deleted.");
        }

        private void PackUpTownshipPage(Mobile from)
        {
            if ((m_Stone != null && m_Stone.Guild != null && m_Stone.Guild.FixedGuildmaster == false) || (from != null && from.AccessLevel > AccessLevel.GameMaster))
            {
                AddLine("Pack up Township?");

                AddButton((int)Button.PackUpTownship, "YES, pack it up.", m_Stone.HasAccess(from, TownshipAccess.Leader));
            }
            else
                AddLine("The guild for this township is staff-owned, and as such, it may not be deleted.");
        }

        private void BuildingPermitsPage(Mobile from)
        {
            Mobile[] mobs = m_Stone.BuildingPermits.ToArray();

            m_State = mobs;

            AddLine("Building Permits");

            AddButton((int)Button.GrantBuildingPermit, "Grant Building Permit", m_Stone.HasAccess(from, TownshipAccess.CoLeader));

            AddList(mobs, 8, delegate (int index)
            {
                AddLine(mobs[index].Name);

                if (m_Stone.HasAccess(from, TownshipAccess.CoLeader))
                    AddGridButton(3, 2, (int)Button.RevokeBuildingPermit + index, "Revoke");
            });
        }

        private void MessagesPage(Mobile from)
        {
            AddLine("Last Messages");

            AddLine();

            AddList(m_Stone.Messages, 8, delegate (int index)
            {
                TownshipStone.MessageEntry e = m_Stone.Messages[index];

                AddLine("[{0}] {1}", e.Date.ToShortTimeString(), e.Text);
            });
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!from.CheckAlive() || !m_Stone.CheckView(from))
                return;

            int index;

            switch ((Button)GetButtonID(info.ButtonID, out index))
            {
                // page buttons 1-99
                case Button.Page_Main:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.Main));

                        break;
                    }
                case Button.Page_DailyFees:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.DailyFees));

                        break;
                    }
                case Button.Page_LastWithdrawals:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.LastWithdrawals));

                        break;
                    }
                case Button.Page_LastDeposits:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.LastDeposits));

                        break;
                    }
                case Button.Page_NPCPurchase:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.NPCPurchase));

                        break;
                    }
                case Button.Page_NPCManage:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.NPCManage));

                        break;
                    }
                case Button.Page_Stockpile:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.Stockpile));

                        break;
                    }
                case Button.Page_EnemyList:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.EnemyList));

                        break;
                    }
                case Button.Page_DeleteTownship:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Leader))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.DeleteTownship));

                        break;
                    }
                case Button.Page_PackUpTownship:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Leader))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.PackUpTownship));

                        break;
                    }
                case Button.Page_Subsidies:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Leader))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.Subsidies));

                        break;
                    }
                case Button.Page_BuildingPermits:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.BuildingPermits));

                        break;
                    }
                case Button.Page_Messages:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.Messages));

                        break;
                    }

                // action buttons 100-999
                case Button.AddFunds:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        AddFundsTarget.BeginAddFunds(m_Stone, from);

                        break;
                    }
                case Button.ToggleNpcInteraction:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.CoLeader))
                        {
                            from.SendMessage("You must be co-leader of the township to access this feature.");
                            return;
                        }

                        m_Stone.OutsideNPCInteractionAllowed = !m_Stone.OutsideNPCInteractionAllowed;

                        if (m_Stone.OutsideNPCInteractionAllowed)
                            from.SendMessage("You have set township NPCs to interact outside of houses.");
                        else
                            from.SendMessage("Township NPCs now only interact inside houses.");

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.NPCManage));

                        break;
                    }
                case Button.AddStock:
                    {
                        if (!m_Stone.AllowBuilding(from))
                        {
                            from.SendMessage("You have no building rights in this township.");
                            return;
                        }

                        TownshipStockpile.BeginDeposit(m_Stone, from);

                        break;
                    }
                case Button.SyncEnemyList:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.CoLeader))
                        {
                            from.SendMessage("You must be co-leader of the township to access this feature.");
                            return;
                        }

                        if (m_Stone.GoldHeld < TownshipSettings.UpdateEnemyCharge)
                        {
                            from.SendMessage("You lack the necessary funds to update the enemy list.");
                        }
                        else
                        {
                            m_Stone.SyncEnemies();
                            m_Stone.GoldHeld -= TownshipSettings.UpdateEnemyCharge;

                            m_Stone.RecordWithdrawal(TownshipSettings.UpdateEnemyCharge, string.Format("{0} updated the enemy list", from.Name));

                            from.SendMessage("The enemy list has been updated from the ban lists of the guild and ally houses in the township.");
                        }

                        from.CloseGump(typeof(TownshipGump));
                        from.SendGump(new TownshipGump(m_Stone, from, Page.EnemyList));

                        break;
                    }
                case Button.MoveStone:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Leader))
                            return;

                        MoveStoneTarget.BeginMoveStone(m_Stone, from);

                        break;
                    }
                case Button.DeleteTownship:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Leader))
                            return;

                        DeleteTownshipPrompt.BeginDeleteTownship(m_Stone, from);

                        break;
                    }
                case Button.PackUpTownship:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Leader))
                            return;

                        PackUpTownshipPrompt.BeginPackUpTownship(m_Stone, from);

                        break;
                    }
                case Button.GrantBuildingPermit:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.CoLeader))
                        {
                            from.SendMessage("You must be co-leader of the township to access this feature.");
                            return;
                        }

                        GrantPermitTarget.BeginGrantPermit(m_Stone, from);

                        break;
                    }

                // list buttons >= 1000
                case Button.NPCPurchase:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.CoLeader))
                        {
                            from.SendMessage("You must be co-leader of the township to access this feature.");
                            return;
                        }

                        if (!(m_State is Type[]))
                            return;

                        Type[] npcTypes = (Type[])m_State;

                        if (index >= 0 && index < npcTypes.Length)
                        {
                            // Allow a township leader to buy deeds to distribute amongst the members of the township
#if false
                            TownshipNPCHelper.PurchaseNPC(m_Stone, from, npcTypes[index]);
#else
                            TownshipNPCHelper.PurchaseDeed(m_Stone, from, npcTypes[index]);
#endif

                            from.CloseGump(typeof(TownshipGump));
                            from.SendGump(new TownshipGump(m_Stone, from, Page.NPCPurchase));
                        }

                        break;
                    }
                case Button.NPCManage:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        if (!(m_State is Mobile[]))
                            return;

                        Mobile[] npcs = (Mobile[])m_State;

                        if (index >= 0 && index < npcs.Length)
                        {
                            if (!TownshipNPCHelper.IsOwner(npcs[index], from))
                                return;

                            from.CloseGump(typeof(TownshipGump));
                            from.SendGump(new TownshipGump(m_Stone, from, Page.NPCManage));

                            from.CloseGump(typeof(TownshipNPCGump));
                            from.SendGump(new TownshipNPCGump(npcs[index], m_Stone));
                        }

                        break;
                    }
                case Button.NPCMenu:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.Ally))
                            return;

                        if (!(m_State is Mobile[]))
                            return;

                        Mobile[] npcs = (Mobile[])m_State;

                        if (index >= 0 && index < npcs.Length)
                        {
                            from.CloseGump(typeof(TownshipGump));
                            from.SendGump(new TownshipGump(m_Stone, from, Page.NPCManage));

                            TownshipNPCHelper.OpenNPCMenu(m_Stone, from, npcs[index].GetType());
                        }

                        break;
                    }
                case Button.RevokeBuildingPermit:
                    {
                        if (!m_Stone.HasAccess(from, TownshipAccess.CoLeader))
                        {
                            from.SendMessage("You must be co-leader of the township to access this feature.");
                            return;
                        }

                        if (!(m_State is Mobile[]))
                            return;

                        Mobile[] mobs = (Mobile[])m_State;

                        if (index >= 0 && index < mobs.Length)
                        {
                            if (m_Stone.BuildingPermits.Remove(mobs[index]))
                                from.SendMessage("{0} is no longer permitted to build in the township.", mobs[index].Name);

                            from.CloseGump(typeof(TownshipGump));
                            from.SendGump(new TownshipGump(m_Stone, from, Page.BuildingPermits));
                        }

                        break;
                    }
            }
        }
    }
}