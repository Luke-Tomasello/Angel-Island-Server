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

/* Gumps/ReportMurderer.cs
 * ChangeLog
 *  4/12/23, Yoar
 *      Added 60k bounty cap.
 *  4/11/23, Yoar
 *      Implemented the pre-Pub16 bounty prompt.
 *      Misc. cleanups.
 *  1/9/23, Yoar
 *      Added call to Faction.HandleMurder to deal with faction mate murders
 *  12/21/07, Pix
 *      Added no counting flag for guilds.
 *  10/6/07, Adam
 *      No murder counts during ServerWars
 *	2/20/07, Pix
 *		Fix a premature change that was made for townships
 *	3/18/07, Pix
 *		Added functionality to thwart 'scripting' of responses to the murder count gump, 
 *		so people can't auto-respond 'yes' while AFK.
 *  1/5/07, Rhiannon
 *      Added check so bounties cannot be placed on staff members.
 *	6/15/06, Pix
 *		Now pays attention to new AggressorInfo.InitialAggressionInNoCountZone property.
 *	4/6/06, Adam
 *		Remove if( CoreAI.TempInt == 1 ) test from Pix's fix below
 *	3/27/06, Pix
 *		Added code to fix multiple-counting problem (Conditionalized by CoreAI.TempInt == 1)
 * 01/10/05, Pix
 *		Replaced NextMurderCountTime with KillerTimes arraylist for controlling repeated counting.
 *	9/2/04, Pix
 *		Made it so inmates can't give counts.
 *	8/7/04, mith
 *		modified so that a player can only respond to one series of count gumps in a 2 minute span.
 *			this is a temporary fix until I can get notoriety flagging working properly.
 *	5/16/04, Pixie
 *		BountyGump now comes up after the reportmurderer gump.
 *  4/19/04, pixie
 *    Gump now closes after 10 minutes.
 *	4/18/04, pixie
 *    Gump doesn't report murder 10 minutes after being created.
 */

using Server.BountySystem;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections;

namespace Server.Gumps
{
    public class ReportMurdererGump : Gump
    {
        public static bool NewMurdererGump
        {
            get { return (PublishInfo.Publish >= 16.0 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()); }
        }

        public static int BountyCap = 60000;

        private int m_iRandomNumberYesResponse = 1;

        private int m_Idx;
        private ArrayList m_Killers;
        private Mobile m_Victim;

        private DateTime m_MaxResponseTime;

        public static void Initialize()
        {
            EventSink.PlayerDeath += new PlayerDeathEventHandler(EventSink_PlayerDeath);
        }

        public class KillerTime
        {
            private Mobile m_Killer;
            private DateTime m_DateTime;

            public KillerTime(Mobile m, DateTime dt)
            {
                m_Killer = m;
                m_DateTime = dt;
            }

            public Mobile Killer { get { return m_Killer; } }
            public DateTime Time { get { return m_DateTime; } set { m_DateTime = value; } }
        }

        public static void EventSink_PlayerDeath(PlayerDeathEventArgs e)
        {
            Mobile m = e.Mobile;

            //Check to make sure inmates don't give counts.
            if (m is PlayerMobile)
            {
                if (((PlayerMobile)m).PrisonInmate)
                {
                    return;
                }
            }

            //Check to make sure we don't give counts during Server Wars.
            if (Server.Misc.AutoRestart.ServerWars == true)
                return;

            ArrayList killers = new ArrayList();
            ArrayList toGive = new ArrayList();

            //if ( DateTime.UtcNow < ((PlayerMobile)m).NextMurderCountTime )
            //	return;

            bool bTimeRestricted = false; //false means they're out of time restriction

            foreach (AggressorInfo ai in m.Aggressors)
            {
                bTimeRestricted = false;

                //Pix: 3/20/07 - fix a premature change that was made for townships
                //if ( ai.Attacker.Player && ai.CanReportMurder && !ai.Reported && !ai.InitialAggressionNotCountable )
                if (ai.Attacker.Player && ai.CanReportMurder && !ai.Reported && !ai.InitialAggressionInNoCountZone)
                {
                    try //just for safety's sake
                    {
                        if (m is PlayerMobile)
                        {
                            PlayerMobile pm = (PlayerMobile)m;
                            if (pm.KillerTimes == null)
                            {
                                pm.KillerTimes = new ArrayList();
                            }

                            bool bFound = false;
                            KillerTime kt = null;
                            foreach (KillerTime k in pm.KillerTimes)
                            {
                                if (k.Killer == ai.Attacker)
                                {
                                    bFound = true;
                                    kt = k;
                                }
                            }

                            if (bFound)
                            {
                                if (kt != null)
                                {
                                    if (DateTime.UtcNow - kt.Time < TimeSpan.FromMinutes(2.0))
                                    {
                                        bTimeRestricted = true;
                                    }
                                    kt.Time = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                kt = new KillerTime(ai.Attacker, DateTime.UtcNow);
                                pm.KillerTimes.Add(kt);
                            }
                        }
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                    if (!bTimeRestricted)
                    {
                        killers.Add(ai.Attacker);
                    }
                    ai.Reported = true;
                }

                if (ai.Attacker.Player && (DateTime.UtcNow - ai.LastCombatTime) < TimeSpan.FromSeconds(30.0) && !toGive.Contains(ai.Attacker))
                    toGive.Add(ai.Attacker);
            }

            foreach (AggressorInfo ai in m.Aggressed)
            {
                if (ai.Defender.Player && (DateTime.UtcNow - ai.LastCombatTime) < TimeSpan.FromSeconds(30.0) && !toGive.Contains(ai.Defender))
                    toGive.Add(ai.Defender);
            }

            foreach (Mobile g in toGive)
            {
                int n = Notoriety.Compute(g, m);

                int theirKarma = m.Karma, ourKarma = g.Karma;
                bool innocent = (n == Notoriety.Innocent);
                bool criminal = (n == Notoriety.Criminal || n == Notoriety.Murderer);

                int fameAward = m.Fame / 200;
                int karmaAward = 0;

                if (innocent)
                    karmaAward = (ourKarma > -2500 ? -850 : -110 - (m.Karma / 100));
                else if (criminal)
                    karmaAward = 50;

                Titles.AwardFame(g, fameAward, false);
                Titles.AwardKarma(g, karmaAward, true);
            }

            #region Factions
            foreach (Mobile killer in killers)
                Factions.Faction.HandleMurder(m, killer);
            #endregion

            if (m is PlayerMobile && ((PlayerMobile)m).NpcGuild == NpcGuild.ThievesGuild)
                return;

            if (killers.Count > 0)
                new GumpTimer(m, killers).Start();

            //((PlayerMobile)m).NextMurderCountTime = DateTime.UtcNow + TimeSpan.FromMinutes(2.0);
        }

        private class GumpTimer : Timer
        {
            private Mobile m_Victim;
            private ArrayList m_Killers;

            public GumpTimer(Mobile victim, ArrayList killers)
                : base(TimeSpan.FromSeconds(4.0))
            {
                m_Victim = victim;
                m_Killers = killers;
            }

            protected override void OnTick()
            {
                try
                {
                    if (m_Victim.Guild != null)
                    {
                        Server.Guilds.Guild g = m_Victim.Guild as Server.Guilds.Guild;
                        if (g != null)
                        {
                            if (g.IsNoCountingGuild)
                            {
                                return;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Server.Diagnostics.LogHelper.LogException(e);
                }

                m_Victim.SendGump(new ReportMurdererGump(m_Victim, m_Killers));
            }
        }

        private class MurderGumpTimeoutTimer : Timer
        {
            private Mobile m_Player;

            public MurderGumpTimeoutTimer(Mobile m)
                : base(TimeSpan.FromMinutes(10.0))
            {
                m_Player = m;
            }

            protected override void OnTick()
            {
                m_Player.CloseGump(typeof(ReportMurdererGump));
                Stop();
            }
        }

        public ReportMurdererGump(Mobile victim, ArrayList killers)
            : this(victim, killers, 0)
        {
        }

        private ReportMurdererGump(Mobile victim, ArrayList killers, int idx)
            : base(0, 0)
        {
            m_Killers = killers;
            m_Victim = victim;
            m_Idx = idx;

            m_MaxResponseTime = DateTime.UtcNow + TimeSpan.FromMinutes(10);

            BuildGump();

            if (!Core.EraAccurate)
                new MurderGumpTimeoutTimer(m_Victim).Start();
        }

        private void BuildGump()
        {
            if (NewMurdererGump)
            {
                AddBackground(265, 205, 320, 290, 5054);
                Closable = false;
                Resizable = false;

                AddPage(0);

                AddImageTiled(225, 175, 50, 45, 0xCE);   //Top left corner
                AddImageTiled(267, 175, 315, 44, 0xC9);  //Top bar
                AddImageTiled(582, 175, 43, 45, 0xCF);   //Top right corner
                AddImageTiled(225, 219, 44, 270, 0xCA);  //Left side
                AddImageTiled(582, 219, 44, 270, 0xCB);  //Right side
                AddImageTiled(225, 489, 44, 43, 0xCC);   //Lower left corner
                AddImageTiled(267, 489, 315, 43, 0xE9);  //Lower Bar
                AddImageTiled(582, 489, 43, 43, 0xCD);   //Lower right corner

                AddPage(1);

                AddHtml(260, 234, 300, 140, ((Mobile)m_Killers[m_Idx]).Name, false, false); // Player's Name
                AddHtmlLocalized(260, 254, 300, 140, 1049066, false, false); // Would you like to report...

                m_iRandomNumberYesResponse = Utility.Random(10, 1000);

                AddButton(260, 300, 0xFA5, 0xFA7, m_iRandomNumberYesResponse, GumpButtonType.Reply, 0);
                AddHtmlLocalized(300, 300, 300, 50, 1046362, false, false); // Yes

                AddButton(360, 300, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(400, 300, 300, 50, 1046363, false, false); // No      
            }
            else
            {
                Closable = false;
                Resizable = false;

                int x = 265, y = 205;

                Mobile killer = (Mobile)m_Killers[m_Idx];

                m_iRandomNumberYesResponse = Utility.Random(10, 1000);

                AddPage(0);
                AddImage(x, y, 1140);

                AddHtml(x + 63, y + 51, 266, 40, string.Format("<H3>Would you like to report {0} as a murderer?</H3>", killer.Name), false, false);

                int balance;

                if (killer.LongTermMurders >= 5 && (balance = Banker.GetAccessibleBalance(m_Victim)) > 0)
                {
                    AddHtml(x + 63, y + 105, 281, 20, string.Format("<H3>Optional Bounty: ({0} max)</H3>", Math.Min(BountyCap, balance)), false, false);
                    AddImage(x + 63, y + 125, 1143);
                    AddTextEntry(x + 74, y + 129, 250, 18, 0, 0, "");
                    AddHtml(x + 63, y + 150, 281, 20, "<H3>amount will be deducted from your bank</H3>", false, false);
                }

                AddButton(x + 121, y + 191, 1147, 1148, m_iRandomNumberYesResponse, GumpButtonType.Reply, 0);
                AddButton(x + 210, y + 191, 1144, 1145, 2, GumpButtonType.Reply, 0);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            //check if we're more than 10 minutes from the gump creation time
            //if we are, then do nothing.
            if (!Core.EraAccurate)
                if (m_MaxResponseTime < DateTime.UtcNow)
                    return;

            int buttonID = info.ButtonID;

            if (m_iRandomNumberYesResponse == info.ButtonID)
            {
                buttonID = 1;
            }

            switch (buttonID)
            {
                case 1:
                    {
                        Mobile killer = (Mobile)m_Killers[m_Idx];

                        if (killer != null && !killer.Deleted)
                        {
                            if (NewMurdererGump)
                            {
                                // we cannot place bounties via the new gump
                                // if we're Angel Island or Mortalis, we'll send custom AI bounty gump later
                            }
                            else
                            {
                                int amount;

                                if (!ParseBounty(info, out amount) || !PlaceBounty(m_Victim, killer, amount))
                                {
                                    // let's give the player another chance at placing a bounty
                                    // not sure if this is era accurate
                                    from.SendGump(new ReportMurdererGump(from, m_Killers, m_Idx));
                                    return;
                                }
                            }

                            killer.LongTermMurders++;
                            killer.ShortTermMurders++;

                            if (killer is PlayerMobile)
                                ((PlayerMobile)killer).ResetKillTime();

                            from.RemoveAggressor(killer);

                            killer.SendLocalizedMessage(1049067); // You have been reported for murder!

                            if (killer.LongTermMurders == 5)
                                killer.SendLocalizedMessage(502134); // You are now known as a murderer!
                            else if (SkillHandlers.Stealing.SuspendOnMurder && killer.LongTermMurders == 1 && killer is PlayerMobile && ((PlayerMobile)killer).NpcGuild == NpcGuild.ThievesGuild)
                                killer.SendLocalizedMessage(501562); // You have been suspended by the Thieves Guild.

                            if (NewMurdererGump && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()))
                                from.SendGump(new BountyGump(from, killer));

                            killer.OnReportedForMurder(from);
                        }

                        break;
                    }
                case 2:
                    {
                        break;
                    }
                default:
                    {
                        //got an unknown response - just quit.
                        return;
                    }
            }

            m_Idx++;
            if (m_Idx < m_Killers.Count)
                from.SendGump(new ReportMurdererGump(from, m_Killers, m_Idx));
        }

        private bool ParseBounty(RelayInfo info, out int amount)
        {
            amount = 0;

            TextRelay relay = info.GetTextEntry(0);

            if (relay == null || string.IsNullOrEmpty(relay.Text))
                return true;

            try
            {
                amount = Convert.ToInt32(relay.Text);
            }
            catch
            {
            }

            if (amount <= 0 || amount > BountyCap)
            {
                m_Victim.SendMessage("You have entered an invalid bounty amount.");
                return false;
            }

            return true;
        }

        public static bool PlaceBounty(Mobile victim, Mobile killer, int amount)
        {
            if (amount <= 0)
                return true;

            if (!Banker.CombinedWithdrawFromAllEnrolled(victim, amount))
            {
                victim.SendMessage("You lack the funds to place the bounty.");
                return false;
            }

            if (victim is PlayerMobile && killer is PlayerMobile) // sanity check
            {
                BountyKeeper.Add(new Bounty((PlayerMobile)victim, (PlayerMobile)killer, amount, true));

                victim.SendMessage("You place a bounty of {0} gp on {1}'s head.", amount, killer.Name);
                killer.SendMessage("{0} has placed a bounty of {1} gp on your head!", victim.Name, amount);
            }

            return true;
        }
    }
}