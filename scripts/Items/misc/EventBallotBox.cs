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

/* Items/Misc/EventBallotBox.cs
 * CHANGELOG:
 *  3/5/22, Adam
 *      Add support for 'linked' ballot boxes.
 *      linked BBs allow you to have separate BBs for 1st, 2nd, and 3rd places - greatly simplifying vote counting
 *          in cases where you are likely to have a sweep (one player gets all the votes.)
 *      In the linked BBs, players MUST vote for different players for 1st, 2nd, and 3rd.
 *  11/30/21, Yoar
 *      Initial version.
 */

using Server.Accounting;
using Server.ContextMenus;
using Server.Gumps;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Server.Items
{
    [FlipableAttribute(0x9A8, 0xE80)]
    public class EventBallotBox : Item
    {
        public enum VoteRestriction : byte
        {
            PerCharacter,
            PerAccount,
            PerIP,
        }

        public override int LabelNumber { get { return 1041006; } } // a ballot box

        private string m_Topic;
        private List<Candidate> m_Candidates;
        private HashSet<Mobile> m_VotedChars; // votes are anonymous! only keep track of *who* voted
        private HashSet<string> m_VotedAccts;
        private HashSet<IPAddress> m_VotedIPs;
        private VoteRestriction m_Restriction;
        private bool m_proximityLink = false;
        private Dictionary<Mobile, string> m_WhoVotedForWho;

        public Dictionary<Mobile, string> WhoVotedForWho { get { return m_WhoVotedForWho; } }
        public HashSet<Mobile> VotedChars { get { return m_VotedChars; } set { m_VotedChars = value; } }
        public HashSet<string> VotedAccts { get { return m_VotedAccts; } set { m_VotedAccts = value; } }
        public HashSet<IPAddress> VotedIPs { get { return m_VotedIPs; } set { m_VotedIPs = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Topic
        {
            get { return m_Topic; }
            set
            {
                m_Topic = value;
                UpdateSisterBoxes(ProximityLink);
            }
        }

        public List<Candidate> Candidates
        {
            get { return m_Candidates; }
            set { m_Candidates = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ProximityLink
        {
            get { return m_proximityLink; }
            set
            {
                m_proximityLink = value;
                UpdateSisterBoxes(value);
                if (value == true)
                    Hue = 0x26;     // redish hue for the master
                else
                    Hue = 0;
            }
        }

        private void UpdateSisterBoxes(bool mode)
        {
            IPooledEnumerable eable = Map.Felucca.GetItemsInRange(this.Location, 4);
            foreach (Item ix in eable)
            {
                if (ix is EventBallotBox ebb && ebb != this)
                {
                    if (mode == true)
                    {
                        ebb.ProximityLink = false;                                  // only one link manager
                        ebb.Hue = 0;
                        ebb.Candidates = new List<Candidate>();                     // give our controlled boxes a copy of the candidates
                        foreach (Candidate candidate in this.Candidates)
                            ebb.Candidates.Add(new Candidate(candidate.Label));
                        ebb.Topic = this.Topic;                                     // copy over the topic
                        ebb.Restriction = this.Restriction;                         /// and the rerstrictions
                    }
                }
            }
            eable.Free();
        }

        #region Candidate Accessors

        private Candidate GetCandidate(int index)
        {
            if (index >= 0 && index < m_Candidates.Count)
                return m_Candidates[index];

            return null;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate1 { get { return GetCandidate(0); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate2 { get { return GetCandidate(1); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate3 { get { return GetCandidate(2); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate4 { get { return GetCandidate(3); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate5 { get { return GetCandidate(4); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate6 { get { return GetCandidate(5); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate7 { get { return GetCandidate(6); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate8 { get { return GetCandidate(7); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate9 { get { return GetCandidate(8); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate10 { get { return GetCandidate(9); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate11 { get { return GetCandidate(10); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate12 { get { return GetCandidate(11); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate13 { get { return GetCandidate(12); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate14 { get { return GetCandidate(13); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Candidate Candidate15 { get { return GetCandidate(14); } set { } }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalVotes
        {
            get
            {
                int total = 0;

                foreach (Candidate candidate in m_Candidates)
                    total += candidate.Votes;

                return total;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public VoteRestriction Restriction
        {
            get { return m_Restriction; }
            set
            {
                m_Restriction = value;
                UpdateSisterBoxes(ProximityLink);
            }
        }

        [Constructable]
        public EventBallotBox()
            : base(0x9A8)
        {
            Movable = false;
            Reset();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this.GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else
            {
                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump(this, from.AccessLevel >= AccessLevel.GameMaster));
            }
        }

        private class InternalGump : Gump
        {
            private const int TopicLines = 3;
            private const int CandidatesPerPage = 5;

            private EventBallotBox m_Box;

            public InternalGump(EventBallotBox box, bool isGM)
                : base(110, 70)
            {
                m_Box = box;

                AddPage(0);

                AddBackground(0, 0, 400, 185 + TopicLines * 20 + CandidatesPerPage * 25, 0xA28);

                if (isGM)
                    AddHtmlLocalized(0, 15, 400, 35, 1011000, false, false); // <center>Ballot Box Owner's Menu</center>
                else
                    AddHtmlLocalized(0, 15, 400, 35, 1011001, false, false); // <center>Ballot Box -- Vote Here!</center>

                AddHtmlLocalized(0, 50, 400, 35, 1011002, false, false); // <center>Topic</center>

                AddBackground(25, 85, 350, TopicLines * 20, 0x1400);

                if (m_Box.Topic != null)
                    AddHtml(30, 85, 340, TopicLines * 20, String.Format("<BASEFONT COLOR=#BDBDBD>{0}</BASEFONT>", m_Box.Topic), false, false);

                AddHtmlLocalized(0, 90 + TopicLines * 20, 400, 35, 1011003, false, false); // <center>votes</center>

                AddButton(45, 140 + TopicLines * 20 + CandidatesPerPage * 25, 0xFA5, 0xFA7, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(80, 143 + TopicLines * 20 + CandidatesPerPage * 25, 40, 35, 1011008, false, false); // done

                if (isGM)
                {
                    AddButton(120, 140 + TopicLines * 20 + CandidatesPerPage * 25, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
                    AddHtmlLocalized(155, 143 + TopicLines * 20 + CandidatesPerPage * 25, 100, 35, 1011006, false, false); // change topic

                    AddButton(240, 140 + TopicLines * 20 + CandidatesPerPage * 25, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0);
                    AddHtmlLocalized(275, 143 + TopicLines * 20 + CandidatesPerPage * 25, 100, 35, 1011007, false, false); // reset votes
                }

                AddPage(1);

                int totalVotes = m_Box.TotalVotes;

                for (int i = 0; i < m_Box.Candidates.Count; i++)
                {
                    int line = i % CandidatesPerPage;

                    if (i != 0 && line == 0)
                    {
                        AddButton(240, 115 + TopicLines * 20 + CandidatesPerPage * 25, 0xFA5, 0xFA7, 0, GumpButtonType.Page, (i / CandidatesPerPage) + 1);
                        AddHtmlLocalized(275, 118 + TopicLines * 20 + CandidatesPerPage * 25, 100, 35, 1011066, false, false); // Next page

                        AddPage((i / CandidatesPerPage) + 1);

                        AddButton(45, 115 + TopicLines * 20 + CandidatesPerPage * 25, 0xFAE, 0xFB0, 0, GumpButtonType.Page, i / CandidatesPerPage);
                        AddHtmlLocalized(80, 118 + TopicLines * 20 + CandidatesPerPage * 25, 100, 35, 1011067, false, false); // Previous page
                    }

                    Candidate candidate = m_Box.Candidates[i];

                    AddButton(20, 115 + TopicLines * 20 + line * 25, 0xFA5, 0xFA7, 3 + i, GumpButtonType.Reply, 0);
                    AddLabelCropped(55, 117 + TopicLines * 20 + line * 25, 100, 35, 0x0, candidate.Label);
                    AddLabel(155, 117 + TopicLines * 20 + line * 25, 0x0, String.Format("[{0}]", candidate.Votes));

                    if (totalVotes > 0)
                        AddImageTiled(205, 117 + TopicLines * 20 + line * 25, (candidate.Votes * 150) / totalVotes, 10, 0xD6);
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (m_Box.Deleted || info.ButtonID == 0)
                    return;

                Mobile from = sender.Mobile;

                if (from.AccessLevel < AccessLevel.GameMaster && (from.Map != m_Box.Map || !from.InRange(m_Box.GetWorldLocation(), 2)))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                    return;
                }

                bool isGM = (from.AccessLevel >= AccessLevel.GameMaster);

                switch (info.ButtonID)
                {
                    case 1: // change topic
                        {
                            if (isGM)
                            {
                                m_Box.ClearTopic();

                                from.SendLocalizedMessage(500370, "", 0x35); // Enter a line of text for your ballot, and hit ENTER. Hit ESC after the last line is entered.
                                from.Prompt = new TopicPrompt(m_Box);
                            }

                            break;
                        }
                    case 2: // reset votes
                        {
                            if (isGM)
                            {
                                m_Box.ClearVotes();
                                from.SendLocalizedMessage(500371); // Votes zeroed out.
                            }

                            from.CloseGump(typeof(InternalGump));
                            from.SendGump(new InternalGump(m_Box, isGM));

                            break;
                        }
                    default:
                        {
                            int choice = info.ButtonID - 3;

                            if (choice >= 0 && choice < m_Box.Candidates.Count)
                            {
                                // has voted on this box?
                                if (m_Box.HasVoted(from))
                                {
                                    from.SendLocalizedMessage(500374); // You have already voted on this ballot.
                                }
                                // have they already voted for this player on any other boxes?
                                else if (m_Box.HasVotedForPlayer(from, choice))
                                {
                                    from.SendMessage(string.Format("You already voted for {0}.", m_Box.Candidates[choice].Label));
                                }
                                // total votes cast by this (IP/Character/Account)
                                else if (m_Box.TotalVotesCastBy(from) >= m_Box.Candidates.Count)
                                {
                                    from.SendMessage(string.Format("You have already cast your {0} votes.", m_Box.Candidates.Count));
                                }
                                // Cool! Register this vote
                                else
                                {
                                    m_Box.Vote(from, choice); // TODO: Confirmation gump?
                                    from.SendLocalizedMessage(500373); // Your vote has been registered.
                                }
                            }

                            break;
                        }
                }
            }
        }

        private class TopicPrompt : Prompt
        {
            private const int MaxTopicLines = 100; // arbitrary cap

            private EventBallotBox m_Box;
            private List<string> m_Lines;

            public TopicPrompt(EventBallotBox box)
                : this(box, null)
            {
            }

            private TopicPrompt(EventBallotBox box, List<string> lines)
            {
                m_Box = box;
                m_Lines = lines;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Box.Deleted || from.AccessLevel < AccessLevel.GameMaster)
                    return;

                if (m_Lines == null)
                    m_Lines = new List<string>();

                m_Lines.Add(text.Trim());

                if (m_Lines.Count < MaxTopicLines)
                {
                    from.SendLocalizedMessage(500377, "", 0x35); // Next line or ESC to finish:
                    from.Prompt = new TopicPrompt(m_Box, m_Lines);
                }
                else
                {
                    Complete(from);
                }
            }

            public override void OnCancel(Mobile from)
            {
                if (m_Box.Deleted || from.AccessLevel < AccessLevel.GameMaster)
                    return;

                Complete(from);
            }

            private void Complete(Mobile from)
            {
                if (m_Lines != null)
                    m_Box.Topic = String.Join("<br>", m_Lines);

                from.SendLocalizedMessage(500376, "", 0x35); // Ballot entry complete.

                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump(m_Box, true));

                m_Box.UpdateSisterBoxes(true);
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new AddCME());
                list.Add(new ResetCME());
            }
        }

        private class AddCME : ContextMenuEntry
        {
            public AddCME()
                : base(5100, 2) // Add
            {
            }

            public override void OnClick()
            {
                EventBallotBox box = (EventBallotBox)Owner.Target;

                if (box.Deleted || Owner.From.AccessLevel < AccessLevel.GameMaster)
                    return;

                Owner.From.Prompt = new AddPrompt(box);
            }
        }

        private class AddPrompt : Prompt
        {
            private EventBallotBox m_Box;

            public AddPrompt(EventBallotBox box)
            {
                m_Box = box;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Box.Deleted || from.AccessLevel < AccessLevel.GameMaster)
                    return;

                m_Box.Candidates.Add(new Candidate(text.Trim()));

                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump(m_Box, true));
                m_Box.UpdateSisterBoxes(true);
            }
        }

        private class ResetCME : ContextMenuEntry
        {
            public ResetCME()
                : base(6162, 2) // Reset Game
            {
            }

            public override void OnClick()
            {
                EventBallotBox box = (EventBallotBox)Owner.Target;

                if (box.Deleted || Owner.From.AccessLevel < AccessLevel.GameMaster)
                    return;

                box.Reset();

                Owner.From.CloseGump(typeof(InternalGump));
                Owner.From.SendGump(new InternalGump(box, true));
            }
        }

        public int TotalVotesCastBy(Mobile m)
        {
            int totalVotes = 0;
            bool controlled = false;

            // first see if we are controlled
            IPooledEnumerable eable = Map.Felucca.GetItemsInRange(this.Location, 4);
            foreach (Item ix in eable)
            {   // if any BB in this range is controlled, we are all controlled
                if (ix is EventBallotBox ebb)
                    if (ebb.ProximityLink == true)
                    {
                        controlled = true;
                        break;
                    }
            }
            eable.Free();

            // if we are controlled, see if the player is trying to vote for the same candidate in two boxes
            eable = Map.Felucca.GetItemsInRange(this.Location, 4);
            if (controlled == true)
            {
                foreach (Item ix in eable)
                {
                    if (ix is EventBallotBox ebb)
                        // count the boxes m has voted on
                        if (ebb.HasVoted(m))
                            totalVotes++;
                }
            }
            eable.Free();

            if (totalVotes == 0)
                return HasVoted(m) ? 1 : 0; // we'll likely never get here, because previous tests would have already caught HasVoted(m)
            else
                return totalVotes;
        }

        public bool HasVotedForPlayer(Mobile m, int choice)
        {
            if (choice < 0 || choice >= m_Candidates.Count)
                return false;

            bool controlled = false;

            // first see if we are controlled
            IPooledEnumerable eable = Map.Felucca.GetItemsInRange(this.Location, 4);
            foreach (Item ix in eable)
            {   // if any BB in this range is controlled, we are all controlled
                if (ix is EventBallotBox ebb)
                    if (ebb.ProximityLink == true)
                    {
                        controlled = true;
                        break;
                    }
            }
            eable.Free();

            // if we are controlled, see if the player is trying to vote for the same candidate in two boxes
            eable = Map.Felucca.GetItemsInRange(this.Location, 4);
            if (controlled == true)
                foreach (Item ix in eable)
                {
                    if (ix is EventBallotBox ebb && ebb != this)
                        if (ebb.WhoVotedForWho.ContainsKey(m))
                        {   // can't vote for the same character voted for in another ballot box
                            if (ebb.WhoVotedForWho[m] == m_Candidates[choice].Label)
                                return true;
                        }
                }
            eable.Free();

            return false;
        }

        public bool HasVoted(Mobile m)
        {
            switch (m_Restriction)
            {
                case VoteRestriction.PerIP:
                    {
                        if (m.NetState != null && m.NetState.Address != null && m_VotedIPs.Contains(m.NetState.Address))
                            return true;

                        goto case VoteRestriction.PerAccount;
                    }
                case VoteRestriction.PerAccount:
                    {
                        if (m.Account is Account && ((Account)m.Account).Username != null && m_VotedAccts.Contains(((Account)m.Account).Username))
                            return true;

                        goto case VoteRestriction.PerCharacter;
                    }
                case VoteRestriction.PerCharacter:
                    {
                        if (m_VotedChars.Contains(m))
                            return true;

                        break;
                    }
            }

            return false;
        }

        public bool Vote(Mobile m, int choice)
        {
            if (choice < 0 || choice >= m_Candidates.Count)
                return false;

            Candidate candidate = m_Candidates[choice];

            candidate.Votes++;

            m_VotedChars.Add(m);

            if (!m_WhoVotedForWho.ContainsKey(m))
                m_WhoVotedForWho.Add(m, m_Candidates[choice].Label);

            if (m.Account is Account && ((Account)m.Account).Username != null)
                m_VotedAccts.Add(((Account)m.Account).Username);

            if (m.NetState != null && m.NetState.Address != null)
                m_VotedIPs.Add(m.NetState.Address);

            return true;
        }

        public void Reset()
        {
            m_Candidates = new List<Candidate>();

            ClearTopic();
            ClearVotes();

            if (ProximityLink == true)
            {
                IPooledEnumerable eable = Map.Felucca.GetItemsInRange(this.Location, 4);
                foreach (Item ix in eable)
                {
                    if (ix is EventBallotBox ebb && ebb != this)
                        ebb.Reset();
                }
                eable.Free();
            }
        }

        public void ClearTopic()
        {
            m_Topic = null;
        }

        public void ClearVotes()
        {
            foreach (Candidate candidate in m_Candidates)
                candidate.Votes = 0;

            m_VotedChars = new HashSet<Mobile>();
            m_VotedAccts = new HashSet<string>();
            m_VotedIPs = new HashSet<IPAddress>();
            m_WhoVotedForWho = new Dictionary<Mobile, string>();

            if (ProximityLink == true)
            {
                IPooledEnumerable eable = Map.Felucca.GetItemsInRange(this.Location, 4);
                foreach (Item ix in eable)
                {
                    if (ix is EventBallotBox ebb && ebb != this)
                        ebb.ClearVotes();
                }
                eable.Free();
            }
        }

        public EventBallotBox(Serial serial)
            : base(serial)
        {
            Reset();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            int version = 1;
            writer.Write(version); // version

            switch (version)
            {
                case 1:
                    {

                        writer.Write((int)m_WhoVotedForWho.Count);
                        foreach (KeyValuePair<Mobile, string> kvp in m_WhoVotedForWho)
                        {
                            writer.Write(kvp.Key);
                            writer.Write(kvp.Value);
                        }

                        writer.Write(m_proximityLink);
                        goto case 0;
                    }
                case 0:
                    {
                        writer.Write((string)m_Topic);

                        writer.Write((int)m_Candidates.Count);

                        for (int i = 0; i < m_Candidates.Count; i++)
                            m_Candidates[i].Serialize(writer);

                        writer.Write((int)m_VotedChars.Count);

                        foreach (Mobile voted in m_VotedChars)
                            writer.Write((Mobile)voted);

                        writer.Write((int)m_VotedAccts.Count);

                        foreach (string username in m_VotedAccts)
                            writer.Write((string)username);

                        writer.Write((int)m_VotedIPs.Count);

                        foreach (IPAddress address in m_VotedIPs)
                            writer.Write((long)address.Address);

                        writer.Write((byte)m_Restriction);
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        int count = reader.ReadInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            Mobile m = reader.ReadMobile();
                            string username = reader.ReadString();
                            m_WhoVotedForWho.Add(m, username);
                        }
                        m_proximityLink = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Topic = reader.ReadString();

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                            m_Candidates.Add(new Candidate(reader, version));

                        count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            Mobile voted = reader.ReadMobile();

                            if (voted != null)
                                m_VotedChars.Add(voted);
                        }

                        count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                            m_VotedAccts.Add(reader.ReadString());

                        count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                            m_VotedIPs.Add(new IPAddress(reader.ReadLong()));

                        m_Restriction = (VoteRestriction)reader.ReadByte();
                        break;
                    }
            }
        }

        [PropertyObject]
        public class Candidate
        {
            private string m_Label;
            private int m_Votes;

            [CommandProperty(AccessLevel.GameMaster)]
            public string Label
            {
                get { return m_Label; }
                set { m_Label = value; }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public int Votes
            {
                get { return m_Votes; }
                set { m_Votes = value; }
            }

            public Candidate()
                : this(null)
            {
            }

            public Candidate(string label)
            {
                m_Label = label;
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write((string)m_Label);
                writer.Write((int)m_Votes);
            }

            public Candidate(GenericReader reader, int version)
            {
                m_Label = reader.ReadString();
                m_Votes = reader.ReadInt();
            }

            public override string ToString()
            {
                return m_Label;
            }
        }
    }
}