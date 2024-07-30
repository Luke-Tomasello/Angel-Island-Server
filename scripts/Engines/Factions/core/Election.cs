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

using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Net;

namespace Server.Factions
{
    [PropertyObject]
    public class Election
    {
        private Faction m_Faction;
        private List<Candidate> m_Candidates;

        private ElectionState m_State;
        private DateTime m_LastStateTime;

        public Faction Faction { get { return m_Faction; } }

        public List<Candidate> Candidates { get { return m_Candidates; } }

        public ElectionState State { get { return m_State; } set { m_State = value; m_LastStateTime = DateTime.UtcNow; } }
        public DateTime LastStateTime { get { return m_LastStateTime; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public ElectionState CurrentState { get { return m_State; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan NextStateTime
        {
            get
            {
                TimeSpan period;

                switch (m_State)
                {
                    default:
                    case ElectionState.Pending: period = FactionConfig.ElectionPendingPeriod; break;
                    case ElectionState.Election: period = FactionConfig.ElectionVotingPeriod; break;
                    case ElectionState.Campaign: period = FactionConfig.ElectionCampaignPeriod; break;
                }

                TimeSpan until = (m_LastStateTime + period) - DateTime.UtcNow;

                if (until < TimeSpan.Zero)
                    until = TimeSpan.Zero;

                return until;
            }
            set
            {
                TimeSpan period;

                switch (m_State)
                {
                    default:
                    case ElectionState.Pending: period = FactionConfig.ElectionPendingPeriod; break;
                    case ElectionState.Election: period = FactionConfig.ElectionVotingPeriod; break;
                    case ElectionState.Campaign: period = FactionConfig.ElectionCampaignPeriod; break;
                }

                m_LastStateTime = DateTime.UtcNow - period + value;
            }
        }

        public Election(Faction faction)
        {
            m_Faction = faction;
            m_Candidates = new List<Candidate>();
        }

        public Election(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        m_Faction = Faction.ReadReference(reader);

                        m_LastStateTime = reader.ReadDateTime();
                        m_State = (ElectionState)reader.ReadEncodedInt();

                        m_Candidates = new List<Candidate>();

                        int count = reader.ReadEncodedInt();

                        for (int i = 0; i < count; ++i)
                        {
                            Candidate cd = new Candidate(reader);

                            if (cd.Mobile != null)
                                m_Candidates.Add(cd);
                        }

                        break;
                    }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt((int)0); // version

            Faction.WriteReference(writer, m_Faction);

            writer.Write((DateTime)m_LastStateTime);
            writer.WriteEncodedInt((int)m_State);

            writer.WriteEncodedInt(m_Candidates.Count);

            for (int i = 0; i < m_Candidates.Count; ++i)
                m_Candidates[i].Serialize(writer);
        }

        public void AddCandidate(Mobile mob)
        {
            if (IsCandidate(mob))
                return;

            m_Candidates.Add(new Candidate(mob));
            mob.SendLocalizedMessage(1010117); // You are now running for office.
        }

        public void RemoveVoter(Mobile mob)
        {
            if (m_State == ElectionState.Election)
            {
                for (int i = 0; i < m_Candidates.Count; ++i)
                {
                    List<Voter> voters = m_Candidates[i].Voters;

                    for (int j = 0; j < voters.Count; ++j)
                    {
                        Voter voter = voters[j];

                        if (voter.From == mob)
                            voters.RemoveAt(j--);
                    }
                }
            }
        }

        public void RemoveCandidate(Mobile mob)
        {
            Candidate cd = FindCandidate(mob);

            if (cd == null)
                return;

            m_Candidates.Remove(cd);
            mob.SendLocalizedMessage(1038031);

            if (m_State == ElectionState.Election)
            {
                if (m_Candidates.Count == 1)
                {
                    m_Faction.Broadcast(1038031); // There are no longer any valid candidates in the Faction Commander election.

                    Candidate winner = m_Candidates[0];

                    Mobile winMob = winner.Mobile;
                    PlayerState pl = PlayerState.Find(winMob);

                    if (pl == null || pl.Faction != m_Faction || winMob == m_Faction.Commander)
                    {
                        m_Faction.Broadcast(1038026); // Faction leadership has not changed.
                    }
                    else
                    {
                        m_Faction.Broadcast(1038028); // The faction has a new commander.
                        m_Faction.Commander = winMob;
                    }

                    m_Candidates.Clear();
                    State = ElectionState.Pending;
                }
                else if (m_Candidates.Count == 0) // well, I guess this'll never happen
                {
                    m_Faction.Broadcast(1038031); // There are no longer any valid candidates in the Faction Commander election.

                    m_Candidates.Clear();
                    State = ElectionState.Pending;
                }
            }
        }

        public bool IsCandidate(Mobile mob)
        {
            return (FindCandidate(mob) != null);
        }

        public bool CanVote(Mobile mob)
        {
            if (m_State != ElectionState.Election)
                return false;

            if (Core.RuleSets.MLRules() && mob.Account != null)
            {
                for (int i = 0; i < mob.Account.Length; ++i)
                {
                    if (mob.Account[i] != null && HasVoted(mob.Account[i]))
                        return false;
                }
            }
            else if (HasVoted(mob))
            {
                return false;
            }

            return true;
        }

        public bool HasVoted(Mobile mob)
        {
            return (FindVoter(mob) != null);
        }

        public Candidate FindCandidate(Mobile mob)
        {
            for (int i = 0; i < m_Candidates.Count; ++i)
            {
                if (m_Candidates[i].Mobile == mob)
                    return m_Candidates[i];
            }

            return null;
        }

        public Candidate FindVoter(Mobile mob)
        {
            for (int i = 0; i < m_Candidates.Count; ++i)
            {
                List<Voter> voters = m_Candidates[i].Voters;

                for (int j = 0; j < voters.Count; ++j)
                {
                    Voter voter = voters[j];

                    if (voter.From == mob)
                        return m_Candidates[i];
                }
            }

            return null;
        }

        public bool CanBeCandidate(Mobile mob)
        {
            if (Core.RuleSets.MLRules() && mob.Account != null)
            {
                for (int i = 0; i < mob.Account.Length; ++i)
                {
                    if (mob.Account[i] != null && IsCandidate(mob.Account[i]))
                        return false;
                }
            }
            else if (IsCandidate(mob))
            {
                return false;
            }

            if (m_Candidates.Count >= FactionConfig.ElectionMaxCandidates)
                return false;

            if (m_State != ElectionState.Campaign)
                return false; // sanity..

            PlayerState pl = PlayerState.Find(mob);

            return (pl != null && pl.Faction == m_Faction && pl.Rank.Rank >= FactionConfig.ElectionCandidateRank);
        }

        public void Slice()
        {
            switch (m_State)
            {
                case ElectionState.Pending:
                    {
                        if ((m_LastStateTime + FactionConfig.ElectionPendingPeriod) > DateTime.UtcNow)
                            break;

                        m_Faction.Broadcast(1038023); // Campaigning for the Faction Commander election has begun.

                        m_Candidates.Clear();
                        State = ElectionState.Campaign;

                        break;
                    }
                case ElectionState.Campaign:
                    {
                        if ((m_LastStateTime + FactionConfig.ElectionCampaignPeriod) > DateTime.UtcNow)
                            break;

                        if (m_Candidates.Count == 0)
                        {
                            m_Faction.Broadcast(1038025); // Nobody ran for office.
                            State = ElectionState.Pending;
                        }
                        else if (m_Candidates.Count == 1)
                        {
                            m_Faction.Broadcast(1038029); // Only one member ran for office.

                            Candidate winner = m_Candidates[0];

                            Mobile mob = winner.Mobile;
                            PlayerState pl = PlayerState.Find(mob);

                            if (pl == null || pl.Faction != m_Faction || mob == m_Faction.Commander)
                            {
                                m_Faction.Broadcast(1038026); // Faction leadership has not changed.
                            }
                            else
                            {
                                m_Faction.Broadcast(1038028); // The faction has a new commander.
                                m_Faction.Commander = mob;
                            }

                            m_Candidates.Clear();
                            State = ElectionState.Pending;
                        }
                        else
                        {
                            m_Faction.Broadcast(1038030);
                            State = ElectionState.Election;
                        }

                        break;
                    }
                case ElectionState.Election:
                    {
                        if ((m_LastStateTime + FactionConfig.ElectionVotingPeriod) > DateTime.UtcNow)
                            break;

                        m_Faction.Broadcast(1038024); // The results for the Faction Commander election are in

                        Candidate winner = null;

                        for (int i = 0; i < m_Candidates.Count; ++i)
                        {
                            Candidate cd = m_Candidates[i];

                            PlayerState pl = PlayerState.Find(cd.Mobile);

                            if (pl == null || pl.Faction != m_Faction)
                                continue;

                            //cd.CleanMuleVotes();

                            if (winner == null || cd.Votes > winner.Votes)
                                winner = cd;
                        }

                        if (winner == null)
                        {
                            m_Faction.Broadcast(1038026); // Faction leadership has not changed.
                        }
                        else if (winner.Mobile == m_Faction.Commander)
                        {
                            m_Faction.Broadcast(1038027); // The incumbent won the election.
                        }
                        else
                        {
                            m_Faction.Broadcast(1038028); // The faction has a new commander.
                            m_Faction.Commander = winner.Mobile;
                        }

                        m_Candidates.Clear();
                        State = ElectionState.Pending;

                        break;
                    }
            }
        }
    }

    public class Voter
    {
        private Mobile m_From;
        private Mobile m_Candidate;

        private IPAddress m_Address;
        private DateTime m_Time;

        public Mobile From
        {
            get { return m_From; }
        }

        public Mobile Candidate
        {
            get { return m_Candidate; }
        }

        public IPAddress Address
        {
            get { return m_Address; }
        }

        public DateTime Time
        {
            get { return m_Time; }
        }

        public object[] AcquireFields()
        {
            TimeSpan gameTime = TimeSpan.Zero;

            if (m_From is PlayerMobile)
                gameTime = ((PlayerMobile)m_From).GameTime;

            int kp = 0;

            PlayerState pl = PlayerState.Find(m_From);

            if (pl != null)
                kp = pl.KillPoints;

            int sk = m_From.Skills.Total;

            int factorSkills = 50 + ((sk * 100) / 10000);
            int factorKillPts = 100 + (kp * 2);
            int factorGameTime = 50 + (int)((gameTime.Ticks * 100) / TimeSpan.TicksPerDay);

            int totalFactor = (factorSkills * factorKillPts * Math.Max(factorGameTime, 100)) / 10000;

            if (totalFactor > 100)
                totalFactor = 100;
            else if (totalFactor < 0)
                totalFactor = 0;

            return new object[] { m_From, m_Address, m_Time, totalFactor };
        }

        public Voter(Mobile from, Mobile candidate)
        {
            m_From = from;
            m_Candidate = candidate;

            if (m_From.NetState != null)
                m_Address = m_From.NetState.Address;
            else
                m_Address = IPAddress.None;

            m_Time = DateTime.UtcNow;
        }

        public Voter(GenericReader reader, Mobile candidate)
        {
            m_Candidate = candidate;

            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        m_From = reader.ReadMobile();
                        m_Address = Utility.Intern(reader.ReadIPAddress());
                        m_Time = reader.ReadDateTime();

                        break;
                    }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt((int)0);

            writer.Write((Mobile)m_From);
            writer.Write((IPAddress)m_Address);
            writer.Write((DateTime)m_Time);
        }
    }

    public class Candidate
    {
        private Mobile m_Mobile;
        private List<Voter> m_Voters;

        public Mobile Mobile { get { return m_Mobile; } }
        public List<Voter> Voters { get { return m_Voters; } }

        public int Votes { get { return m_Voters.Count; } }

        public void CleanMuleVotes()
        {
            for (int i = 0; i < m_Voters.Count; ++i)
            {
                Voter voter = (Voter)m_Voters[i];

                if ((int)voter.AcquireFields()[3] < 90)
                    m_Voters.RemoveAt(i--);
            }
        }

        public Candidate(Mobile mob)
        {
            m_Mobile = mob;
            m_Voters = new List<Voter>();
        }

        public Candidate(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    {
                        m_Mobile = reader.ReadMobile();

                        int count = reader.ReadEncodedInt();
                        m_Voters = new List<Voter>(count);

                        for (int i = 0; i < count; ++i)
                        {
                            Voter voter = new Voter(reader, m_Mobile);

                            if (voter.From != null)
                                m_Voters.Add(voter);
                        }

                        break;
                    }
                case 0:
                    {
                        m_Mobile = reader.ReadMobile();

                        List<Mobile> mobs = reader.ReadStrongMobileList();
                        m_Voters = new List<Voter>(mobs.Count);

                        for (int i = 0; i < mobs.Count; ++i)
                            m_Voters.Add(new Voter(mobs[i], m_Mobile));

                        break;
                    }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt((int)1); // version

            writer.Write((Mobile)m_Mobile);

            writer.WriteEncodedInt((int)m_Voters.Count);

            for (int i = 0; i < m_Voters.Count; ++i)
                ((Voter)m_Voters[i]).Serialize(writer);
        }
    }

    public enum ElectionState
    {
        Pending,
        Campaign,
        Election
    }
}