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

/* Engines/BountySystem/BountyKeeper.cs
 * CHANGELOG:
 * 8/18/22, Adam(Utc)
 * Convert all read/write of times to handle UTC (the server format.)
 *  All DateTime objects are written as UTC.
 *  All DateTime objects are read as UTC and returned as LOCAL.
 *  Why: This insures that a Saves folder moved from production will run correctly on a local (developers) machine.
 *  Example: (old system) The AI server computer runs at UTC, but but time is saved in 'local' format. This works fine as long as the world files are saved and loaded
 *          on the server. But if you then move the world files to a local developer machine running, say Pacific Time, then when you read the world files all dateTime 
 *          objects will be off by 7-8 hours, depending on DST
 *  The above change fixes this problem by ensuring all DateTime objects are always written as UTC and converted to local on read.
 *  7/05/10, Pix
 *      Remove UpdateTownCrier, call PJUM.UpdateAnnouncements instead.
 *      Added allied-guilds cannot collect head.
 *	5/17/10, adam
 *		o Cap CurrentLBBonusAmount with a a new const MaxLBBonusAmount currently set to 300 gp.
 *			Without this set, a player gould get 100s of K in bonus gold.
 *		o Reward the bounty collector even if the bountied player had no gold but lord brit did have a bonus. 
 *			if (goldRewarded > 0 || goldForLBBonus > 0)
 *			Note: exploit potential - a possible exploit is to turn in the head of a friend for the 300 gp maximum LBBonus.			
 *				the second time the player attempts this, the macroer will go to jail for 12 hours, the third time 24 hours, then 36 hours.
 *				As you can see the exploit is extreemly expensive in player jail time; so we're not worried about it.
 *		o after a bounty has been collected, remove the old message from the town crier	and replace it with the "enemy of the kingdom" message
 * 			(UpdateTownCrier())
 *	5/16/10, adam
 *		Add null checks to CanCollectReward
 *	3/22/10, adam
 *		disallow placing a bounty on someone while in prison
 *	3/1/10, Adam
 *		1. Put a divide by zero safety check in CurrentLBBonusAmount
 *  1/5/07, Rhiannon
 *      Added check so bounties cannot be placed on staff members.
 *  6/2/04, Pixie
 *		Made Bounties property private for arraylist safety purposes.
 *	5/26/04, Pixie
 *		Changed CollectBounty() call for individual message capability for different NPC types.
 *		Changed flagging for the Bounty command to use the PermaFlag system, so the person placing
 *		the bounty is grey only to the person they placed the bounty on, but there's a 2-minute timer
 *		on the grey-flagging.
 *  5/24/04, Pixie
 *		Put try-catch block around saving of the bounty system
 *		so it will never crash the server.
 *		Also, put checks in that if the Placer or the Wanted of a bounty
 *		have been deleted, then the bounty is deleted and the funds are 
 *		added to the LBFund.
 *  5/18/04, Pixie
 *		Changs to LBBonus and to display only
 *		1 bounty per wanted player
 *	5/17/04, Pixie
 *		On use of the bounty command, flags player criminal, but doesn't guardwhack.
 *	5/16/04, Pixie
 *		Initial Version
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Xml;

namespace Server.BountySystem
{
    /// <summary>
    /// Summary description for BountyKeeper.
    /// </summary>
    public class BountyKeeper
    {
        private static bool bEnabled = true;
        private static bool DEBUG = true;
        private static ArrayList m_bounties = new ArrayList();
        private static int m_LBFundAmount;

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);
        }

        public static void Initialize()
        {
            TargetCommands.Register(new BountyCommand());
        }

        public static void Add(Bounty b)
        {
            m_bounties.Add(b);
        }

        public static void Remove(Bounty b)
        {
            m_bounties.Remove(b);
        }

        private static ArrayList Bounties
        {
            get { return m_bounties; }
        }

        public static bool Active
        {
            get { return bEnabled; }
        }

        public static int LBFund
        {
            get { return m_LBFundAmount; }
            set { m_LBFundAmount = value; }
        }

        public static bool CanCollectReward(PlayerMobile collector, Head head)
        {
            PlayerMobile dead = (head != null) ? head.Player : null;
            bool bReturn = true;

            // sanity
            if (dead == null || collector == null)
                return false;

            // Put all the checks in here to see if collector can collect the reward on dead's head.

            // we don't allow a shared account to turn in a head for a bounty			
            if (SharedAccount(dead, collector) == true)
                bReturn = false;

            // we were kill by a shared account
            if (head.FriendlyFire == true)
                bReturn = false;

            // can't collect on your own head
            if (collector == dead)
                bReturn = false;

            // cannot collect on a guildies head
            if (collector.Guild != null && collector.Guild == dead.Guild)
                bReturn = false;

            // cannot collect on an allied guild's head
            if (collector.Guild != null && dead.Guild != null
                && (collector.Guild as Server.Guilds.Guild) != null
                && (collector.Guild as Server.Guilds.Guild).IsAlly(dead.Guild as Server.Guilds.Guild))
            {
                bReturn = false;
            }

            // cannot collect a bounty on another player on this account
            if (collector.Account == dead.Account)
                bReturn = false;


            return bReturn;
        }

        public const int MaxLBBonusAmount = 300;

        public static int CurrentLBBonusAmount
        {
            get
            {
                int iAmount = 0;
                if (NumberOfUniqueBounties == 0)
                    iAmount = BountyKeeper.m_LBFundAmount / 1;
                else
                    iAmount = BountyKeeper.m_LBFundAmount / NumberOfUniqueBounties;

                // adam: cap this amount so as not to give crazy amounts if there are only a few outstanding bounties
                iAmount = Math.Min(iAmount, MaxLBBonusAmount);

                return iAmount;
            }
        }

        public static int RewardForPlayer(PlayerMobile p)
        {
            int iReturn = 0;

            foreach (Bounty b in BountyKeeper.Bounties)
            {
                if (b.WantedPlayer == p)
                {
                    iReturn += b.Reward;
                }
            }

            return iReturn;
        }

        public static bool IsEligibleForLBBonus(PlayerMobile p)
        {
            bool bReturn = false;
            foreach (Bounty b in BountyKeeper.Bounties)
            {
                if (b.WantedPlayer == p)
                {
                    if (b.LBBonus == true)
                    {
                        bReturn = true;
                        break;
                    }
                }
            }
            return bReturn;
        }

        public static int BountiesOnPlayer(PlayerMobile p)
        {
            int iCount = 0;
            foreach (Bounty b in BountyKeeper.Bounties)
            {
                if (b.WantedPlayer == p)
                {
                    iCount++;
                }
            }
            return iCount;
        }

        public static int NumberOfUniqueBounties
        {
            get
            {
                int iNumber = 0;

                ArrayList uniqueplayers = new ArrayList();
                foreach (Bounty b in BountyKeeper.Bounties)
                {
                    if (uniqueplayers.Contains(b.WantedPlayer))
                    {
                    }
                    else
                    {
                        uniqueplayers.Add(b.WantedPlayer);
                    }
                }
                iNumber = uniqueplayers.Count;

                return iNumber;
            }
        }

        public static Bounty GetBounty(int i)
        {
            int count = 0;
            if (i == 0)
            {
                return (Bounty)BountyKeeper.Bounties[0];
            }

            ArrayList uniqueplayers = new ArrayList();
            foreach (Bounty b in BountyKeeper.Bounties)
            {
                if (uniqueplayers.Contains(b.WantedPlayer))
                {
                }
                else
                {
                    uniqueplayers.Add(b.WantedPlayer);
                    count++;

                    if (i == (count - 1))
                    {
                        return b;
                    }
                }
            }

            return (Bounty)BountyKeeper.Bounties[0]; //safety
        }

        private static void RemoveOldBounties()
        {
            ArrayList toRemove = new ArrayList();
            foreach (Bounty b in m_bounties)
            {
                //Rewards stay viable for 4 weeks + 1 hour per 100 gold above 500
                //rewarddate + 4 weeks + (amount-500)/100 hours
                if ((b.RewardDate + TimeSpan.FromDays(28) + TimeSpan.FromHours((b.Reward - 500) / 100)) < DateTime.UtcNow)
                {
                    toRemove.Add(b);
                }
                else if (b.WantedPlayer == null || b.WantedPlayer.Deleted)
                {
                    toRemove.Add(b);
                }
                else if (b.Placer == null || b.Placer.Deleted)
                {
                    toRemove.Add(b);
                }
            }

            foreach (Bounty b in toRemove)
            {
                //add reward to LB fund
                m_LBFundAmount += b.Reward;
                //remove reward
                m_bounties.Remove(b);
            }
        }

        public static bool SharedAccount(Mobile dead, Mobile collector)
        {
            if (dead != null && dead.Account != null && collector != null && collector.Account != null)
            {
                Accounting.Account acct1 = collector.Account as Accounting.Account;
                Accounting.Account acct2 = dead.Account as Accounting.Account;
                if (acct1 != null && acct2 != null && acct1.LoginIPs != null && acct2.LoginIPs != null && acct1.LoginIPs.Length > 0 && acct2.LoginIPs.Length > 0)
                {
                    foreach (IPAddress ip1 in acct1.LoginIPs)
                    {
                        foreach (IPAddress ip2 in acct2.LoginIPs)
                        {
                            if (ip1.Equals(ip2))
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        // returns:
        // true if head taken, false if not
        // goldRewarded is the gold placed in the placer's back
        // message is:
        //		-1: default - meaningless
        //		-2: player head - innocent person's head, no bounty
        //		-3: player head - illegal return (friend return, same account return), no bounty given
        //		-4: player head - bounty given
        //		-5: player head - bounty already collected
        public static bool CollectBounty(Head h, Mobile from, Mobile collector, ref int goldGiven, ref int message)
        {
            bool bReturn = false;
            goldGiven = 0;
            message = -1;
            try
            {
                int goldRewarded = 0;
                int goldForLBBonus = 0;
                bool eligibleforlbbonus = false;
                if (h.IsPlayerHead && h.HeadType == HeadType.Regular)
                {
                    if (BountyKeeper.CanCollectReward((PlayerMobile)from, h))
                    {
                        ArrayList found_bounties = new ArrayList();
                        foreach (Bounty b in BountyKeeper.Bounties)
                        {
                            if (b.WantedPlayer == h.Player)
                            {
                                if (h.Created > b.RewardDate)
                                {
                                    goldRewarded += b.Reward;
                                    if (b.LBBonus)
                                    {
                                        eligibleforlbbonus = true;
                                    }
                                    found_bounties.Add(b);
                                }
                            }
                        }

                        if (eligibleforlbbonus)
                        {
                            goldForLBBonus = BountyKeeper.CurrentLBBonusAmount;
                        }
                        bool bRewardGiven = false;

                        // adam: issue the reward even if there is only a lord brit bonus
                        // see the commants at the top of this file for exploit potential
                        if (goldRewarded > 0 || goldForLBBonus > 0)
                        {
                            message = -4; // My thanks for slaying this vile person.  Here's the reward of {0} gold!, goldGiven
                            Container c = from.Backpack;
                            if (c != null)
                            {
                                BankCheck g = new BankCheck(goldRewarded + goldForLBBonus);
                                goldGiven = goldRewarded + goldForLBBonus;
                                BountyKeeper.LBFund -= goldForLBBonus;
                                from.AddToBackpack(g);
                                bReturn = true;
                                bRewardGiven = true;

                                LogHelper Logger = new LogHelper("BountySystem.log", false, true);
                                string p1 = Logger.Format(LogType.Mobile, from);
                                string p2 = Logger.Format(LogType.Mobile, h.Player);
                                Logger.Log(LogType.Text,
                                    string.Format("({0}) received {2} gold ({3} reward + {4} LB bonus) for turing in the head of ({1}). ", p1, p2, goldGiven, goldRewarded, goldForLBBonus)
                                );
                                Logger.Finish();
                            }
                        }
                        else
                        {
                            if (Engines.TCCS.FindEntry(h.Player) == null)
                                message = -2; // You disgusting miscreant!  Why are you giving me an innocent person's head?
                            else
                                message = -5; // My thanks for slaying this vile person, but the bounty has already been collected.
                        }

                        if (bRewardGiven)
                        {
                            foreach (Bounty b in found_bounties)
                            {
                                BountyKeeper.Bounties.Remove(b);
                            }
                            found_bounties.Clear();
                        }

                        //Call UpdateAnnouncements to update any messages on the town crier from the PJUM
                        // system that are affected by bounties being collected.
                        try
                        {
                            PJUM.PJUM.UpdateAnnouncements();
                        }
                        catch (Exception e)
                        {
                            LogHelper.LogException(e);
                        }
                    }
                    else
                    {
                        message = -3;   // I suspect treachery....
                                        // I'll take that head, you just run along now.
                        bReturn = true;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Error (nonfatal) in BaseGuard.OnDragDrop(): " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }

            return bReturn;
        }


        //Pix - 2010.07.05 - this 'work' is already done in the PJUM system, so call it instead.

        // after a bounty has been collected, remove the old message from the town crier
        //	and replace it with this message
        //		public static void UpdateTownCrier(Bounty b)
        //		{
        //			if (b == null)
        //				return;
        //
        //			Engines.ListEntry le = Engines.TCCS.FindEntry(b.TownCrierEntryID);
        //			if (le != null)
        //			{
        //				string[] lns = new string[2];
        //				if (le.Mobile != null)
        //				{	//  for unlawful resource gathering?
        //					lns[0] = String.Format("{0} is an enemy of the kingdom.", le.Mobile.Name);
        //					lns[1] = String.Format("{0} was last seen at {1}.", le.Mobile.Name, le.Mobile.Location);
        //					Engines.ListEntry nle = new Engines.ListEntry(lns, le.Mobile, le.DateTime, le.Type);
        //					Engines.TCCS.AddEntry(nle);		// add the updated message
        //				}
        //
        //				Engines.TCCS.RemoveEntry(le);	// kill the old message
        //			}
        //		}

        public static void OnSave(WorldSaveEventArgs e)
        {
            if (DEBUG) Console.WriteLine("Bounty Saving...");

            //Get rid of stale bounties
            RemoveOldBounties();

            try
            {
                if (!Directory.Exists("Saves/BountySystem"))
                    Directory.CreateDirectory("Saves/BountySystem");

                string filePath = Path.Combine("Saves/BountySystem", "Bounty.xml");

                using (StreamWriter op = new StreamWriter(filePath))
                {
                    XmlTextWriter xml = new XmlTextWriter(op);

                    xml.Formatting = Formatting.Indented;
                    xml.IndentChar = '\t';
                    xml.Indentation = 1;

                    xml.WriteStartDocument(true);

                    xml.WriteStartElement("Bounties");

                    xml.WriteAttributeString("count", m_bounties.Count.ToString());
                    xml.WriteAttributeString("LBFund", m_LBFundAmount.ToString());

                    foreach (Bounty b in m_bounties)
                    {
                        b.Save(xml);
                    }

                    xml.WriteEndElement();

                    xml.Close();
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Error in BountyKeeper.OnSave(): " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }

        public static void OnLoad()
        {
            if (DEBUG) Console.WriteLine("Bounty Loading...");

            string filePath = Path.Combine("Saves/BountySystem", "Bounty.xml");

            if (!File.Exists(filePath))
            {
                m_LBFundAmount = 0;
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                XmlElement root = doc["Bounties"];

                try
                {
                    string strFund = root.GetAttribute("LBFund");
                    m_LBFundAmount = Int32.Parse(strFund);
                }
                catch
                {
                    m_LBFundAmount = 0;
                }

                foreach (XmlElement bounty in root.GetElementsByTagName("bounty"))
                {
                    try
                    {
                        Bounty b = new Bounty(bounty);

                        if (b.WantedPlayer != null &&
                            b.Reward > 0 &&
                            b.RewardDate <= DateTime.UtcNow)
                        {
                            m_bounties.Add(b);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Warning: A bounty instance load failed");
                    }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static string GetText(XmlElement node, string defaultValue)
        {
            if (node == null)
                return defaultValue;

            return node.InnerText;
        }
        public static DateTime GetDateTime(string dateTimeString, DateTime defaultValue)
        {
            try
            {
                if (CoreAI.IsDynamicPatchSet(CoreAI.PatchIndex.HasPatchedTime) == false)
                    return XmlConvert.ToDateTime(dateTimeString);
                else
                    return XmlConvert.ToDateTime(dateTimeString, XmlDateTimeSerializationMode.Local);
            }
            catch
            {
                try
                {
                    return DateTime.Parse(dateTimeString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }
        public static int GetInt32(string intString, int defaultValue)
        {
            try
            {
                return XmlConvert.ToInt32(intString);
            }
            catch
            {
                try
                {
                    return Convert.ToInt32(intString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }


    }

    public class BountyCommand : BaseCommand
    {
        //private bool m_Value;

        public BountyCommand()
        {
            AccessLevel = AccessLevel.Player;
            Supports = CommandSupport.Area | CommandSupport.Global | CommandSupport.Multi | CommandSupport.Online | CommandSupport.Region | CommandSupport.Self | CommandSupport.Single;
            Commands = new string[] { "Bounty" };
            ObjectTypes = ObjectTypes.Mobiles;

            Usage = "Bounty <amount>";
            Description = "Places a bounty of <amount> on the head of another person.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            Mobile m = (Mobile)obj;

            //CommandLogging.WriteLine( e.Mobile, "{0} {1} {2} {3}", e.Mobile.AccessLevel, CommandLogging.Format( e.Mobile ), m_Value ? "hiding" : "unhiding", CommandLogging.Format( m ) );

            if (e != null && e.Mobile != null && e.Length == 1)
            {
                if (e.Mobile.Region != null && e.Mobile.Region.IsAngelIslandRules)
                {
                    AddResponse("You cannot place a bounty on someone while you are in prison.");
                }
                else if ((m is PlayerMobile) && (m.AccessLevel == AccessLevel.Player)) // Can't put bounty on staff.
                {
                    PlayerMobile bountyPlacer = (PlayerMobile)e.Mobile;
                    Container cont = bountyPlacer.BankBox;

                    int amount = 0;
                    try
                    {
                        amount = Int32.Parse(e.GetString(0));
                    }
                    catch
                    {
                        AddResponse("Bounty Amount was improperly formatted!");
                        return;
                    }

                    if (amount < 500)
                    {
                        AddResponse("No one would hunt for that low of a bounty!");
                    }
                    else if (cont != null && cont.ConsumeTotal(typeof(Gold), amount))
                    {
                        BountyKeeper.Add(new Bounty(bountyPlacer, (PlayerMobile)m, amount, false));
                        AddResponse("You have posted a bounty for the head of " + m.Name + "!");
                        AddResponse("Amount of GP removed from your bank: " + amount);
                        ((PlayerMobile)m).SendMessage(bountyPlacer.Name + " has just placed a bounty on your head for " + amount + " gold.");

                        //Flag player criminal, but don't guardwhack
                        //bountyPlacer.Criminal = true;
                        //Flag to only the player you place the bounty on.
                        if (!bountyPlacer.PermaFlags.Contains(m))
                        {
                            bountyPlacer.PermaFlags.Add(m);
                            bountyPlacer.Delta(MobileDelta.Noto);
                            ExpireTimer et = new ExpireTimer(bountyPlacer, m, TimeSpan.FromMinutes(2.0));
                            et.Start();
                        }
                    }
                    else
                    {
                        AddResponse("You do not have that much GP in your bank!");
                    }
                }
                else
                {
                    AddResponse("Please target a player.");
                }
            }
            else
            {
                AddResponse("You must specify the amount of the bounty.");
            }
        }

        private class ExpireTimer : Timer
        {
            private PlayerMobile m_placer;
            private Mobile m_target;

            public ExpireTimer(PlayerMobile bountyPlacer, Mobile target, TimeSpan delay)
                : base(delay)
            {
                Priority = TimerPriority.FiveSeconds;
                m_placer = bountyPlacer;
                m_target = target;
            }

            protected override void OnTick()
            {
                m_placer.PermaFlags.Remove(m_target);
                m_placer.Delta(MobileDelta.Noto);
            }
        }

    }

}