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

/* Engines/Factions/Gumps/New Gumps/Misc/ElectionGump.cs
 * CHANGELOG:
 *  12/30/22, Yoar
 *      Custom faction menu. Initial commit.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Factions.NewGumps.Misc
{
    public class ElectionGump : BaseFactionGump
    {
        private Faction m_Faction;

        public ElectionGump(Mobile m, Faction faction)
            : base()
        {
            m_Faction = faction;

            Election election = m_Faction.Election;

            switch (election.State)
            {
                case ElectionState.Pending:
                    {
                        TimeSpan toGo = (election.LastStateTime + FactionConfig.ElectionPendingPeriod) - DateTime.UtcNow;
                        int days = (int)(toGo.TotalDays + 0.5);

                        AddBackground(350, 115);

                        AddHtml(20, 15, 310, 26, "<center><i>Elections</i></center>", false, false);

                        AddSeparator(20, 40, 310);

                        AddHtmlLocalized(20, 50, 280, 20, 1038034, false, false); // A new election campaign is pending

                        if (days > 0)
                        {
                            AddHtmlLocalized(20, 70, 195, 20, 1018062, false, false); // Days until next election :
                            AddLabel(200, 70, 0, days.ToString());
                        }
                        else
                        {
                            AddHtmlLocalized(20, 70, 310, 20, 1018059, false, false); // Election campaigning begins tonight.
                        }

                        break;
                    }
                case ElectionState.Campaign:
                    {
                        TimeSpan toGo = (election.LastStateTime + FactionConfig.ElectionCampaignPeriod) - DateTime.UtcNow;
                        int days = (int)(toGo.TotalDays + 0.5);

                        AddBackground(350, 145);

                        AddHtml(20, 15, 310, 26, "<center><i>Elections</i></center>", false, false);

                        AddSeparator(20, 40, 310);

                        AddHtmlLocalized(20, 50, 310, 20, 1018058, false, false); // There is an election campaign in progress.

                        if (days > 0)
                        {
                            AddHtmlLocalized(20, 70, 105, 20, 1038033, false, false); // Days to go:
                            AddLabel(110, 70, 0, days.ToString());
                        }
                        else
                        {
                            AddHtmlLocalized(20, 70, 310, 20, 1018061, false, false); // Campaign in progress. Voting begins tonight.
                        }

                        AddSeparator(20, 92, 310);

                        if (election.IsCandidate(m))
                            AddHtmlLocalized(20, 100, 310, 40, 1010116, false, false); // You are already running for office
                        else if (election.CanBeCandidate(m))
                            AddButtonLabeled(20, 100, 310, 1, "Campaign For Leadership");
                        else
                            AddHtmlLocalized(20, 100, 310, 40, 1010118, false, false); // You must have a higher rank to run for office

                        break;
                    }
                case ElectionState.Election:
                    {
                        TimeSpan toGo = (election.LastStateTime + FactionConfig.ElectionVotingPeriod) - DateTime.UtcNow;
                        int days = (int)Math.Ceiling(toGo.TotalDays);

                        AddBackground(350, 145 + election.Candidates.Count * 20);

                        AddHtml(20, 15, 310, 26, "<center><i>Elections</i></center>", false, false);

                        AddSeparator(20, 40, 310);

                        AddHtmlLocalized(20, 50, 310, 20, 1018060, false, false); // There is an election vote in progress.

                        AddHtmlLocalized(20, 70, 105, 20, 1038033, false, false); // Days to go:
                        AddLabel(110, 70, 0, days.ToString());

                        bool canVote = election.CanVote(m);

                        if (canVote)
                            AddHtml(20, 90, 310, 20, "Vote For Leadership", false, false);
                        else
                            AddHtmlLocalized(20, 90, 310, 20, 1038032, false, false); // You have already voted in this election.

                        AddSeparator(20, 112, 310);

                        int y = 120;

                        for (int i = 0; i < election.Candidates.Count; i++)
                        {
                            Candidate cd = election.Candidates[i];

                            AddBackground(20, y, 150, 26, 0x2486);
                            AddHtml(50, y + 3, 115, 20, FormatName(cd.Mobile), false, false);

                            if (canVote)
                                AddButton(20, y + 2, 0x845, 0x846, 100 + i, GumpButtonType.Reply, 0);

                            AddLabel(300, y, 0, cd.Votes.ToString());

                            y += 20;
                        }

                        break;
                    }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Faction.IsMember(from))
                return;

            int buttonID = info.ButtonID;

            Election election = m_Faction.Election;

            switch (election.State)
            {
                case ElectionState.Campaign:
                    {
                        if (buttonID == 1) // Campaign For Leadership
                        {
                            if (election.CanBeCandidate(from))
                                election.AddCandidate(from);

                            FactionGumps.CloseGumps(from, typeof(ElectionGump));
                            from.SendGump(new ElectionGump(from, m_Faction));
                        }

                        break;
                    }
                case ElectionState.Election:
                    {
                        int candidateIndex = buttonID - 100;

                        if (candidateIndex >= 0 && candidateIndex < election.Candidates.Count)
                        {
                            election.Candidates[candidateIndex].Voters.Add(new Voter(from, election.Candidates[candidateIndex].Mobile));

                            FactionGumps.CloseGumps(from, typeof(ElectionGump));
                            from.SendGump(new ElectionGump(from, m_Faction));
                        }

                        break;
                    }
            }
        }
    }
}