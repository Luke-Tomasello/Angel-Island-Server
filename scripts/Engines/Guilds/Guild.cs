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

/* Engines/Guilds/Guild.cs
 * CHANGELOG:
 *  5/29/23, Yoar (GuildBanClear)
 *      Added [GuildBanClear to clear the guild-ban status
 *  5/6/23, Adam (BanMember)
 *      Add [BanMember command for use by guild masters
 *      Institutes a 14 day ban
 *  4/30/23, Yoar
 *      Rewrote Suffix getter into GetSuffix(Mobile mob) method
 *  4/30/23, Adam
 *      Added a NoResign property. This keeps the player in the guild indefinitely. 
 *  4/15/23, Yoar
 *      Disabled Order/Chaos system
 *  4/14/23, Yoar
 *      Added support for new Alignment system.
 *  8/17/22. Adam
 *      Moved AdminGump.GetSharedAccounts() ==> Utility.GetSharedAccounts()
 *  1/27/22, Adam
 *      Minor output formatting.
 *  1/10/22, Adam
 *      Update to the change of 10/12/21
 *      If you have not logged in for >= 14 days, your vote us scrubbed (set to self)
 *      This keeps active members in control
 *      Also: logic cleanup, robustness: try/catch
 *  10/12/21, Adam
 *      don't allow members that have not logged in for 14 days to vote
 *  8/18/21, adam
 *      Add BanMember(m);       // currently only used by New in the [new command
 *      We check this in the [new command to prevent a recently kicked member from rejoining for 14 days.
 *  8/18/21, Adam (Guildmember)
 *      After two weeks New Guild members get their title upgraded from something like [8/29] to [Guildmember]
 *      I do this processing in the FixedGuildmaster processing, which is okay for now. But if we have other guild configurations
 *      we may want to rework this.
 *  8/15/21, Adam: (FixedGuildmaster)
 *      Guildmaster cannot be voted out (used for staff created guilds like [New])
 *      If something should happen to the guildmaster (.deleted) then the guild reverts to World.GetAdminAcct()
 *  8/2/08, Pix
 *		When Name changes and this guild is in a township, call UpdateRegionName() for the townshipstone.
 *	4/25/08, Adam
 *		Add new NewPlayerGuild flag to indicate this guild may be selected from available guilds to auto-add players.
 *		Change from Guild.Peaceful to guild.NewPlayerGuild when deciding auto adding
 *  01/14/08 Pix
 *      Reverted last change.  Added two methods:
 *      GetVotesFor - gets the votes for a member (for display to that member)
 *      UpdateFealtiesFor - gets called when a member declares his fealty - if anyone is
 *          declared to that member, then they get their fealty changed to the new vote.
 *  01/14/08, Pix
 *      Changed guildmaster voting calculation - now votes follow fealty chain.
 *  01/04/08, Pix
 *      Added delay to Guild War Ring toggling.
 *  12/21/07, Pix
 *      Added no counting flag for guilds.
 *	12/4/07, Adam
 *		Add support for peaceful guilds (no notoriety)
 *  02/26/07, Adam
 *      Generate a "GuildDisband.log" log entry for all disbanded guilds. When you recieve
 *      a missing guildstone exception from the guild restoration deed, check here first.
 *  02/04/07, Kit
 *      Made RemoveMember() update guild feality of members in guild to point to themselves if previously
 *      pointing to member being removed.
 *	12/03/06, Pix
 *		Changed hue for old-client guild/ally chat so it's readable in the journal.
 *	12/02/06, Pix
 *		Made guild/ally chats work with lower version clients (like it used to).
 *	11/19/06, Pix
 *		Changes for fixing guild and ally chat colors.
 *  10/14/06, Rhiannon
 *		Added ResignMember(), which displays a confirmation gump if the member is the guildmaster.
 *  9/01/06, Taran Kain
 *		Moved guildmaster fealty checks out of Serialize() and into Heartbeat.
 *	03/24/06, Pix
 *		Added IOBAlignment to guild.
 *	01/03/06, Pix
 *		Added AlliedMessage()
 *  12/14/05, Kit
 *		Added check to RemoveAlly()
 *	7/11/05: Pix
 *		Added ListenToGuild_OnCommand
 *	7/5/05: Pix
 *		Added new GuildMessage function which takes a string.
 */

using Server.Commands;
using Server.Diagnostics;			// log helper
using Server.Engines.Alignment;
using Server.Items;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Guilds
{
    public enum GuildType
    {
        Regular,
        Chaos,
        Order
    }

    [Flags]
    public enum GuildFlags
    {   // make sure all values default to zero so that most guilds won't have to serialize these flags
        None = 0x00000000,
        Peaceful = 0x00000001,                  // cannot war, ally, or attack guildmates
        AnnounceNewMembers = 0x00000002,        // announce members to the New Guild
        IsNoCountingGuild = 0x00000004,         // honor guild
        NewPlayerGuild = 0x00000008,            // new player guild, usually only one
        FixedGuildmaster = 0x00000010,          // 8/15/21, Adam: Guildmaster cannot be voted out (used for staff created guilds like [New])
        NoResign = 0x00000020,                  // 4/30/23, Adam: Special 'no resign' guild stone for cheaters and hackers
    }

    public class Guild : BaseGuild
    {
        public static bool OrderChaosEnabled { get { return false; } }

        #region IOB

        private static int m_JoinDelay = 10;
        private IOBAlignment m_IOBAlignment;

        [CommandProperty(AccessLevel.GameMaster)]
        public IOBAlignment IOBAlignment
        {
            get { return m_IOBAlignment; }
            set
            {
                if (m_IOBAlignment != value)
                {
                    m_IOBAlignment = value;
                    m_NextTypeChange = DateTime.UtcNow + TimeSpan.FromDays(7.0);

                    InvalidateMemberProperties();
                }
            }
        }

        private DateTime m_LastIOBChangeTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastIOBChangeTime
        {
            get { return m_LastIOBChangeTime; }
            set { m_LastIOBChangeTime = value; }
        }

        private bool CanJoinIOB()
        {
            if (m_LastIOBChangeTime + TimeSpan.FromDays(m_JoinDelay) > DateTime.UtcNow)
                return false;

            return true;
        }

        public void IOBKick()
        {
            string message = string.Format("Your guild has been kicked from the {0} by the consensus of other {0}.  You cannot rejoin for {1} days.",
                Engines.IOBSystem.IOBSystem.GetIOBName(IOBAlignment), m_JoinDelay);
            GuildMessage(message);

            m_LastIOBChangeTime = DateTime.UtcNow;
            m_IOBAlignment = IOBAlignment.None;
        }

        #endregion //IOB Functionality

        #region Alignment

        private AlignmentType m_Alignment;

        [CommandProperty(AccessLevel.GameMaster)]
        public AlignmentType Alignment
        {
            get { return m_Alignment; }
            set
            {
                if (m_Alignment != value)
                {
                    AlignmentType oldAlignment = m_Alignment;

                    m_Alignment = value;

                    m_NextTypeChange = DateTime.UtcNow + AlignmentConfig.ChangeAlignmentDelay;

                    AlignmentSystem.OnAlign(this, oldAlignment);

                    InvalidateMemberProperties();

                    if (m_Alignment == AlignmentType.None)
                        GuildMessage("Your guild is now unaligned.");
                    else
                        GuildMessage(string.Format("Your guild is now {0} aligned.", AlignmentSystem.GetName(m_Alignment)));
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AlignmentState AlignmentState
        {
            get { return AlignmentState.GetState(m_Alignment); }
            set { }
        }

        #endregion

        private ArrayList m_Listeners;

        private bool m_bGuildWarRing = false;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool GuildWarRing
        {
            get
            {
                return m_bGuildWarRing;
            }
            set
            {
                m_bGuildWarRing = value;
            }
        }

        private DateTime m_dtGuildWarRingChangeTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime GuildWarRingChangeTime
        {
            get
            {
                return m_dtGuildWarRingChangeTime;
            }
            set
            {
                m_dtGuildWarRingChangeTime = value;
            }
        }

        public static BaseGuild EventSink_CreateGuild(CreateGuildEventArgs args)
        {
            return (BaseGuild)(new Guild(args.Id));
        }

        public static bool NewGuildSystem { get { return Core.RuleSets.SERules(); } }

        private Mobile m_Leader;

        private string m_Name;
        private string m_Abbreviation;

        private ArrayList m_Allies;
        private ArrayList m_Enemies;

        private ArrayList m_Members;

        private Dictionary<Mobile, DateTime> m_Banned;

        private Item m_Guildstone;
        private Item m_Teleporter;

        private Item m_TownshipStone;

        private string m_Charter;
        private string m_Website;

        private DateTime m_LastFealty;

        private GuildType m_Type;
        private DateTime m_NextTypeChange;

        private ArrayList m_AllyDeclarations, m_AllyInvitations;

        private ArrayList m_WarDeclarations, m_WarInvitations;
        private ArrayList m_Candidates, m_Accepted;

        private GuildFlags m_Flags;

        public Guild(Mobile leader, string name, string abbreviation)
            : base()
        {
            m_Leader = leader;

            m_Members = new ArrayList();
            m_Banned = new Dictionary<Mobile, DateTime>();
            m_Allies = new ArrayList();
            m_Enemies = new ArrayList();
            m_WarDeclarations = new ArrayList();
            m_WarInvitations = new ArrayList();
            m_AllyDeclarations = new ArrayList();
            m_AllyInvitations = new ArrayList();
            m_Candidates = new ArrayList();
            m_Accepted = new ArrayList();

            m_LastFealty = DateTime.UtcNow;

            m_Name = name;
            m_Abbreviation = abbreviation;

            AddMember(m_Leader);
            m_Listeners = new ArrayList();
        }

        public Guild(int id)
            : base(id)//serialization ctor
        {
            m_Listeners = new ArrayList();
        }

        public void AddMember(Mobile m)
        {
            if (!m_Members.Contains(m))
            {
                if (m.Guild != null && m.Guild != this)
                    ((Guild)m.Guild).RemoveMember(m);

                m_Members.Add(m);
                m.Guild = this;
                m.GuildFealty = m_Leader;
            }
        }

        public void ResignMember(Mobile m, bool force = false)
        {
            if (NoResign && !force && m.AccessLevel == AccessLevel.Player)
            {
                m.SendMessage("you were branded an outcast, and so you will stay");
            }
            else if (m == m_Leader && !force)
            {
                m.SendGump(new Gumps.ConfirmResignGump(m));
            }
            else
            {
                RemoveMember(m);
            }
        }

        public enum GuildBanType
        {
            None,
            Self = 1,
            Friend = 2,
        }
        public TimeSpan BanExpiry(Mobile m)
        {
            if (IsBannedMember(m) == GuildBanType.None)
                return TimeSpan.Zero;
            return m_Banned[m] - DateTime.UtcNow;
        }

        private void TidyBanList()
        {
            List<Mobile> list = new List<Mobile>();
            foreach (KeyValuePair<Mobile, DateTime> test in m_Banned)
            {
                if (DateTime.UtcNow > test.Value)
                    list.Add(test.Key);
            }

            foreach (Mobile m in list)
                m_Banned.Remove(m);
        }

        public GuildBanType IsBannedMember(Mobile m)
        {
            TidyBanList();

            if (m_Banned.ContainsKey(m))
                return GuildBanType.Self;           // you were banned

            // okay, see if this guy is using the same IP address of a banned account
            ArrayList list = Utility.GetSharedAccounts(((Server.Accounting.Account)m.Account).LoginIPs);

            foreach (Server.Accounting.Account acct in list)
            {
                if (acct == null)
                    continue;

                for (int charndx = 0; charndx < acct.Limit; charndx++)
                {
                    if (acct[charndx] == null)
                        break;                      // no more characters on this account

                    if (m_Banned.ContainsKey(acct[charndx]))
                        return GuildBanType.Friend; // this shared account's guy was banned, so you will be too!
                }
            }

            return GuildBanType.None;               // not banned
        }
        public Mobile GetBannedMember(Mobile m)
        {
            TidyBanList();

            if (m_Banned.ContainsKey(m))
                return m;                           // you were banned

            // okay, see if this guy is using the same IP address of a banned account
            ArrayList list = Utility.GetSharedAccounts(((Server.Accounting.Account)m.Account).LoginIPs);

            foreach (Server.Accounting.Account acct in list)
            {
                if (acct == null)
                    continue;

                for (int charndx = 0; charndx < acct.Limit; charndx++)
                {
                    if (acct[charndx] == null)
                        break;                      // no more characters on this account

                    if (m_Banned.ContainsKey(acct[charndx]))
                        return acct[charndx];       // this shared account's guy was banned
                }
            }

            return null;                            // not banned
        }
        public void BanMember(Mobile m)
        {   //  14 day ban
            if (!m_Banned.ContainsKey(m))
                m_Banned.Add(m, DateTime.UtcNow + TimeSpan.FromDays(14));
        }
        public void RemoveMember(Mobile m)
        {
            if (m_Members.Contains(m))
            {
                m_Members.Remove(m);
                m.Guild = null;

                m.SendLocalizedMessage(1018028); // You have been dismissed from your guild.

                if (m == m_Leader)
                {
                    if (FixedGuildmaster)
                    {   // staff sponsored guilds cannot be reassigned or disbanded.
                        m_Leader = World.GetSystemAcct();
                        if (!m_Members.Contains(m_Leader))
                            AddMember(m_Leader);

                        return;
                    }
                    else
                        m_Leader = null;

                    CalculateGuildmaster();

                    if (m_Leader == null)
                        Disband();
                }

                if (m_Members.Count == 0)
                    Disband();

                //update guild feality
                foreach (Mobile n in m_Members)
                {
                    if (n == null || n.Deleted || n.Guild != this)
                        continue;

                    if (n.GuildFealty == m)
                        n.GuildFealty = n; //set guild fealty to self
                }
            }
        }

        public void AddAlly(Guild g)
        {
            if (!m_Allies.Contains(g))
            {
                m_Allies.Add(g);

                g.AddAlly(this);
            }
        }

        public void RemoveAlly(Guild g)
        {
            if (m_Allies != null && m_Allies.Contains(g))
            {
                m_Allies.Remove(g);

                g.RemoveAlly(this);
            }
        }

        public void AddEnemy(Guild g)
        {
            if (!m_Enemies.Contains(g))
            {
                m_Enemies.Add(g);

                g.AddEnemy(this);
            }
        }

        public void RemoveEnemy(Guild g)
        {
            if (m_Enemies != null && m_Enemies.Contains(g))
            {
                m_Enemies.Remove(g);

                g.RemoveEnemy(this);
            }
        }

        //public void AlliedMessage( string message )
        //{
        //	//Send to us
        //	this.GuildMessage(message);
        //	//Send to all our allies
        //	foreach( Guild alliedguild in this.Allies )
        //	{
        //		if( alliedguild != null )
        //		{
        //			alliedguild.GuildMessage(message);
        //		}
        //	}
        //}

        public void GuildMessage(int num, string format, params object[] args)
        {
            GuildMessage(num, string.Format(format, args));
        }

        public void GuildMessage(int num, string append)
        {
            for (int i = 0; i < m_Members.Count; ++i)
                ((Mobile)m_Members[i]).SendLocalizedMessage(num, true, append);
        }

        public void GuildMessage(string message)
        {
            for (int i = 0; i < m_Members.Count; ++i)
                ((Mobile)m_Members[i]).SendMessage(68, message);

            if (m_Listeners.Count > 0)
            {
                foreach (Mobile m in m_Listeners)
                {
                    if (m != null)
                    {
                        m.SendMessage("[[" + this.Abbreviation + "]]" + message);
                    }
                }
            }

            if (true)
            {

                string filename = Utility.ValidFileName("[[" + this.Abbreviation + "]]") + ".log";
                LogHelper logger = new LogHelper(string.Format("Logs/Players/{0}", filename), false, true, true);
                logger.Log(message);
                logger.Finish();
            }
        }

        public void AlliedChat(string message, Server.Mobiles.PlayerMobile pm)
        {
            AlliedChat(pm, 0x3B2, message);
        }

        static ClientVersion GA_CHAT_MIN_VERSION = new ClientVersion("4.0.10a");

        public void AlliedChat(Mobile from, int hue, string text)
        {
            Packet p = null;
            for (int i = 0; i < m_Members.Count; i++)
            {
                Mobile m = m_Members[i] as Mobile;

                if (m != null)
                {
                    NetState state = m.NetState;

                    if (state != null)
                    {
                        if (state.Version >= GA_CHAT_MIN_VERSION)
                        {
                            if (p == null)
                            {
                                p = Packet.Acquire(new UnicodeMessage(from.Serial, from.Body, MessageType.Alliance, hue, 3, from.Language, from.Name, text));
                            }

                            state.Send(p);
                        }
                        else
                        {
                            m.SendMessage(0x587, "[Alliance][" + from.Name + "]: " + text);
                        }
                    }
                }
            }

            Packet.Release(p);

            if (true)
            {
                string filename = Utility.ValidFileName("[Alliance][" + from + "]:") + ".log";
                LogHelper logger = new LogHelper(string.Format("Logs/Players/{0}", filename), false, true, true);
                logger.Log(text);
                logger.Finish();
            }

            if (from.Guild == this)
            {
                //Then send to all allied members
                foreach (Guild alliedguild in this.Allies)
                {
                    if (alliedguild != null)
                    {
                        alliedguild.AlliedChat(from, hue, text);
                        //alliedguild.GuildChat(message);
                    }
                }
            }
        }

        public void GuildChat(string message, Server.Mobiles.PlayerMobile pm)
        {
            GuildChat(pm, 0x3B2, message);
        }
        public void GuildChat(Mobile from, int hue, string text)
        {
            Packet p = null;
            for (int i = 0; i < m_Members.Count; i++)
            {
                Mobile m = m_Members[i] as Mobile;

                if (m != null)
                {
                    NetState state = m.NetState;

                    if (state != null)
                    {
                        if (state.Version >= GA_CHAT_MIN_VERSION)
                        {
                            if (p == null)
                            {
                                p = Packet.Acquire(new UnicodeMessage(from.Serial, from.Body, MessageType.Guild, hue, 3, from.Language, from.Name, text));
                            }

                            state.Send(p);
                        }
                        else
                        {
                            m.SendMessage(0x1D8, "[Guild][" + from.Name + "]: " + text);
                        }
                    }
                }
            }

            Packet.Release(p);
        }


        public void Disband()
        {   // protect staff owned properties
            if (this.Guildstone != null && this.Guildstone.IsStaffOwned == true)
                return;

            // was it already disbanded?
            if (BaseGuild.List.Contains(this.Id))
            {
                LogHelper Logger = new LogHelper("GuildDisband.log", false);
                string abbreviation = "(null)";
                string name = "(null)";
                Serial sx = 0x0;
                if (Abbreviation != null) abbreviation = Abbreviation;
                if (Name != null) name = Name;
                if (m_Guildstone != null) sx = m_Guildstone.Serial;
                string text = string.Format("The Guild \"{0}\" [{1}] ({2}) was disbanded.", name, abbreviation, sx.ToString());
                Logger.Log(LogType.Text, text);
                Logger.Finish();
                Utility.ConsoleWriteLine(text, ConsoleColor.Yellow);
            }

            m_Leader = null;

            BaseGuild.List.Remove(this.Id);

            foreach (Mobile m in m_Members)
            {
                m.SendLocalizedMessage(502131); // Your guild has disbanded.
                m.Guild = null;
            }

            m_Members.Clear();

            for (int i = m_Allies.Count - 1; i >= 0; --i)
                if (i < m_Allies.Count)
                    RemoveAlly((Guild)m_Allies[i]);

            for (int i = m_Enemies.Count - 1; i >= 0; --i)
                if (i < m_Enemies.Count)
                    RemoveEnemy((Guild)m_Enemies[i]);

            if (m_Guildstone != null)
            {
                m_Guildstone.Delete();
                m_Guildstone = null;
            }

            if (m_TownshipStone != null)
            {
                m_TownshipStone.Delete();
                m_TownshipStone = null;
            }
        }

        public void UpdateFealtiesFor(Mobile originalVote, Mobile newVote)
        {
            try
            {
                if (originalVote == null)
                {
                    return;
                }

                for (int i = 0; i < m_Members.Count; i++)
                {
                    Mobile member = m_Members[i] as Mobile;
                    if (member != null)
                    {
                        if (member.GuildFealty == originalVote)
                        {
                            member.GuildFealty = newVote;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Server.Diagnostics.LogHelper.LogException(e);
            }
        }

        public int GetVotesFor(Mobile member)
        {
            int count = 0;

            try
            {
                if (member == null)
                {
                    return 0;
                }

                for (int i = 0; i < m_Members.Count; i++)
                {
                    Mobile m = m_Members[i] as Mobile;
                    if (m != null)
                    {
                        if (m == member)
                        {
                            if (m.GuildFealty == null || m.GuildFealty == member)
                            {
                                count++;
                            }
                        }
                        else
                        {
                            if (m.GuildFealty == member)
                            {
                                count++;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Server.Diagnostics.LogHelper.LogException(e);
            }

            return count;
        }

        private Mobile CalculateMemberVote(Mobile member)
        {
            try
            {
                if (member == null || member.Deleted || member.Guild != this)
                {
                    return null;
                }

                Mobile candidate = member.GuildFealty;

                if (candidate == null || candidate.Deleted || candidate.Guild != this)
                {
                    if (m_Leader != null && !m_Leader.Deleted && m_Leader.Guild == this)
                    {
                        candidate = m_Leader;
                    }
                    else
                    {   // set fealty to self
                        candidate.GuildFealty = member;
                        candidate = member;
                    }
                }

                return candidate;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            return null;
        }

        private bool IsTitleStartDate(string str)
        {   // "{0}/{1}", month, day

            foreach (char c in str)
            {
                if ((c < '0' || c > '9') && c != '/')
                    return false;
            }

            return true;
        }
        public void CalculateGuildmaster()
        {
            try
            {
                // 8/15/21, Adam: Guildmaster cannot be voted out (used for staff created guilds like [New])
                if (FixedGuildmaster)
                {
                    if (m_Leader == null || m_Leader.Deleted)
                    {
                        if (World.GetAdminAcct() != null)
                            Utility.ConsoleWriteLine("GuildFealty set to {1} for shard guild [{0}]", ConsoleColor.Red, this.Abbreviation.ToString(), World.GetAdminAcct());

                        m_Leader = World.GetAdminAcct();        // set the leader to shard owner should the shard guild leader be gone
                    }
                    Utility.ConsoleWriteLine("GuildFealty votes ignored for shard guild [{0}]", ConsoleColor.Yellow, this.Abbreviation.ToString());

                    // now update member titles if 1) they've been in this guild longer than two weeks, and 2) their title hasn't changed
                    for (int i = 0; m_Members != null && i < m_Members.Count; ++i)
                    {
                        if ((m_Members[i] as Server.Mobiles.PlayerMobile).GuildTitle == null)
                            continue;

                        string[] split = (m_Members[i] as Server.Mobiles.PlayerMobile).GuildTitle.Split(new char[] { '/' });
                        if (split.Length != 2)
                            continue;

                        int day;
                        bool result = int.TryParse(split[0], out day);
                        if (result == false)
                            continue;

                        int month;
                        result = int.TryParse(split[1], out month);
                        if (result == false)
                            continue;

                        DateTime join = new DateTime(DateTime.UtcNow.Year, month, day);
                        TimeSpan ts = join - DateTime.UtcNow;
                        if (ts.TotalDays >= 14)
                        {   // two weeks. Update: 4/28/23, no more self updating titles.
                            //  I left the code here in case we change our minds, but for now they are Citizens!
                            (m_Members[i] as Server.Mobiles.PlayerMobile).GuildTitle = "Citizen"; // "Guildmember";
                        }
                    }
                    // no more FixedGuildmaster processing
                    return;
                }

                Hashtable votes = new Hashtable();

                // tally votes
                for (int i = 0; m_Members != null && i < m_Members.Count; ++i)
                {
                    // don't allow members that have not logged in for 14 days to vote
                    if (m_Members[i] as Mobile is Mobiles.PlayerMobile pm && pm != null && pm.Account as Accounting.Account != null)
                    {
                        Accounting.Account acct = pm.Account as Accounting.Account;
                        TimeSpan delta = DateTime.UtcNow - acct.LastLogin;
                        if (delta.TotalDays > 14)
                        {   // can't vote / set guild fealty to self
                            if (pm.GuildFealty != pm)
                                pm.GuildFealty = pm;
                        }
                    }

                    Mobile v = CalculateMemberVote(m_Members[i] as Mobile);

                    if (v == null)
                        continue;
                    if (votes[v] == null)
                        votes[v] = (int)1;
                    else
                        votes[v] = (int)(votes[v]) + 1;
                }

                Mobile winner = null;
                int highVotes = 0;

                // find winner
                foreach (DictionaryEntry de in votes)
                {
                    if (de.Key is Mobile m && m != null && m.Deleted == false)
                    {
                        int val = (int)de.Value;
                        if (winner == null || val > highVotes)
                        {
                            winner = m;
                            highVotes = val;
                        }
                    }
                }

                if (winner == null) //make sure we have a winner!
                {
                    if (m_Leader == null || m_Leader.Guild != this)
                    {
                        if (m_Members.Count > 0)
                        {
                            winner = m_Members[0] as Mobile;
                        }
                    }
                    else
                    {
                        winner = m_Leader;
                    }
                }

                if (m_Leader != winner && winner != null)
                {
                    GuildMessage(1018015, winner.Name); // Guild Message: Guildmaster changed to:
                    Utility.ConsoleWriteLine("Guildmaster for {0} changed to {1}", ConsoleColor.Yellow, this.Name, winner);
                }

                m_Leader = winner;
                m_LastFealty = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        public GuildFlags Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        public bool GetFlag(GuildFlags flag)
        {
            return ((m_Flags & flag) != 0);
        }

        public void SetFlag(GuildFlags flag, bool value)
        {
            if (value)
                m_Flags |= flag;
            else
                m_Flags &= ~flag;
        }

        /* FixedGuildmaster
         * FixedGuildmaster serves two purposes:
         * 1. Prevents the current guildmaster from being voted out
         * 2. Prevents the guild from being disbanded.
         * Should the current guild have no guildmaster, the SYSTEM account will be assigned.
         * This assignment prevents the guild from being disbanded, and subsequent destruction of any associated township.
         */
        [CommandProperty(AccessLevel.GameMaster)]
        public bool FixedGuildmaster
        {
            get { return GetFlag(GuildFlags.FixedGuildmaster); }
            set { SetFlag(GuildFlags.FixedGuildmaster, value); }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public bool NoResign
        {
            get { return GetFlag(GuildFlags.NoResign); }
            set { SetFlag(GuildFlags.NoResign, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NewPlayerGuild
        {
            get { return GetFlag(GuildFlags.NewPlayerGuild); }
            set { SetFlag(GuildFlags.NewPlayerGuild, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Peaceful
        {
            get { return GetFlag(GuildFlags.Peaceful); }
            set { SetFlag(GuildFlags.Peaceful, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AnnounceNewMembers
        {
            get { return GetFlag(GuildFlags.AnnounceNewMembers); }
            set { SetFlag(GuildFlags.AnnounceNewMembers, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsNoCountingGuild
        {
            get { return GetFlag(GuildFlags.IsNoCountingGuild); }
            set { SetFlag(GuildFlags.IsNoCountingGuild, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Guildstone
        {
            get
            {
                return m_Guildstone;
            }
            set
            {
                m_Guildstone = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item TownshipStone
        {
            get
            {
                return m_TownshipStone;
            }
            set
            {
                m_TownshipStone = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Teleporter
        {
            get
            {
                return m_Teleporter;
            }
            set
            {
                m_Teleporter = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;

                try
                {
                    if (m_TownshipStone != null)
                    {
                        if (m_TownshipStone is TownshipStone)
                        {
                            ((TownshipStone)m_TownshipStone).UpdateRegionName();
                        }
                    }
                }
                catch (Exception omgwtfwouldthisbe)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(omgwtfwouldthisbe));
                }

                if (m_Guildstone != null)
                    m_Guildstone.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Website
        {
            get
            {
                return m_Website;
            }
            set
            {
                m_Website = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Abbreviation
        {
            get
            {
                return m_Abbreviation;
            }
            set
            {
                m_Abbreviation = value;

                InvalidateMemberProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Charter
        {
            get
            {
                return m_Charter;
            }
            set
            {
                m_Charter = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public GuildType Type
        {
            get
            {
                return m_Type;
            }
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;

                    m_NextTypeChange = DateTime.UtcNow + TimeSpan.FromDays(7.0);

                    InvalidateMemberProperties();

                    GuildMessage(1018022, m_Type.ToString()); // Guild Message: Your guild type has changed:  
                }
            }
        }

        public override string GetSuffix(Mobile mob)
        {
            if (OrderChaosEnabled)
            {
                switch (m_Type)
                {
                    case GuildType.Order: return " (Order)";
                    case GuildType.Chaos: return " (Chaos)";
                }
            }

            #region Alignment
            if (AlignmentSystem.Enabled && AlignmentSystem.GetTitleDisplay(mob).HasFlag(TitleDisplay.GuildSuffix) && m_Alignment != AlignmentType.None)
                return string.Format(" ({0})", AlignmentSystem.GetName(m_Alignment));
            #endregion

            return null;
        }

        public override bool ForceGuildTitle
        {
            get { return (OrderChaosEnabled && m_Type != GuildType.Regular); }
        }

        public void InvalidateMemberProperties()
        {
            if (m_Members != null)
            {
                for (int i = 0; i < m_Members.Count; i++)
                    ((Mobile)m_Members[i]).InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Leader
        {
            get
            {
                if (m_Leader == null || m_Leader.Deleted || m_Leader.Guild != this)
                    CalculateGuildmaster();

                return m_Leader;
            }
            set
            {
                if (m_Members.Contains(value))
                    m_Leader = value;
                else
                {
                    if (value is Mobile && (value as Mobile).Player)
                    {
                        if (value.Guild != null)
                            this.Guildstone.SendSystemMessage("They must resign from their current guild before controlling another.");
                        else
                        {
                            AddMember(value);
                            m_Leader = value;
                        }
                    }
                    else
                        this.Guildstone.SendSystemMessage("You cannot make that a guildmaster.");
                }
            }
        }

        public override bool Disbanded
        {
            get
            {
                return (m_Leader == null || m_Leader.Deleted);
            }
        }

        public ArrayList Allies
        {
            get
            {
                return m_Allies;
            }
        }

        public ArrayList Enemies
        {
            get
            {
                return m_Enemies;
            }
        }

        public ArrayList AllyDeclarations
        {
            get
            {
                return m_AllyDeclarations;
            }
        }

        public ArrayList AllyInvitations
        {
            get
            {
                return m_AllyInvitations;
            }
        }

        public ArrayList WarDeclarations
        {
            get
            {
                return m_WarDeclarations;
            }
        }

        public ArrayList WarInvitations
        {
            get
            {
                return m_WarInvitations;
            }
        }

        public ArrayList Candidates
        {
            get
            {
                return m_Candidates;
            }
        }

        public ArrayList Accepted
        {
            get
            {
                return m_Accepted;
            }
        }

        public ArrayList Members
        {
            get
            {
                return m_Members;
            }
        }

        public bool IsMember(Mobile m)
        {
            return m_Members.Contains(m);
        }

        public bool IsAlly(Guild g)
        {
            return m_Allies.Contains(g);
        }

        public bool IsEnemy(Guild g, bool ignoreAlignment = false)
        {
            if (OrderChaosEnabled && m_Type != GuildType.Regular && g.m_Type != GuildType.Regular && m_Type != g.m_Type)
                return true;

            if (this != g && this.GuildWarRing && g.GuildWarRing)
                return true;

            // guild diplomacy takes precedent over guild alignment
            if (m_Enemies.Contains(g))
                return true;

            if (AlignmentSystem.Enabled && !ignoreAlignment && m_Alignment != AlignmentType.None && g.m_Alignment != AlignmentType.None && m_Alignment != g.m_Alignment && !m_Allies.Contains(g))
                return true;

            return false;
        }

        public bool IsWar(Guild g)
        {
            return m_Enemies.Contains(g);
        }

        public override void OnDelete(Mobile mob)
        {
            RemoveMember(mob);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastFealty
        {
            get
            {
                return m_LastFealty;
            }
            set
            {
                m_LastFealty = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextTypeChange
        {
            get
            {
                return m_NextTypeChange;
            }
            set
            {
                m_NextTypeChange = value;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            writer.Write((int)11);//version

            writer.Write((byte)m_Alignment);

            // version 9 - banned list
            //  We use a banned list since folks can (re)join New by just typing [new
            //  If other guils want to use this feature, it's available
            writer.Write(m_Banned.Count);
            foreach (KeyValuePair<Mobile, DateTime> de in m_Banned)
            {
                writer.Write(de.Key);
                writer.Write(de.Value);
            }

            //version 8 addition
            writer.Write((int)m_Flags);

            //version 7 addition
            writer.Write(m_bGuildWarRing);

            //version 6 addition
            writer.Write(m_TownshipStone);

            //version 5 additions
            writer.Write((int)m_IOBAlignment);
            //end version 5 additions

            writer.WriteGuildList(m_AllyDeclarations, true);
            writer.WriteGuildList(m_AllyInvitations, true);

            writer.Write((DateTime)m_NextTypeChange);

            writer.Write((int)m_Type);

            writer.Write(m_LastFealty);

            writer.Write(m_Leader);
            writer.Write(m_Name);
            writer.Write(m_Abbreviation);

            writer.WriteGuildList(m_Allies, true);
            writer.WriteGuildList(m_Enemies, true);
            writer.WriteGuildList(m_WarDeclarations, true);
            writer.WriteGuildList(m_WarInvitations, true);

            writer.WriteMobileList(m_Members, true);
            writer.WriteMobileList(m_Candidates, true);
            writer.WriteMobileList(m_Accepted, true);

            writer.Write(m_Guildstone);
            writer.Write(m_Teleporter);

            writer.Write(m_Charter);
            writer.Write(m_Website);
        }

        public override void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 11:
                    {
                        m_Alignment = (AlignmentType)reader.ReadByte();

                        goto case 10;
                    }
                case 10:
                case 9:
                    {
                        int count = reader.ReadInt();
                        m_Banned = new Dictionary<Mobile, DateTime>(count);
                        for (int ix = 0; ix < count; ix++)
                        {   // while we're here, we'll tidy the list;
                            Mobile m = reader.ReadMobile();
                            DateTime expiry = reader.ReadDateTime();
                            if (DateTime.UtcNow > expiry)
                                continue;               // don't add to the list. No longer banned
                            else if (m != null)
                                m_Banned.Add(m, expiry);
                        }
                        goto case 8;
                    }
                case 8:
                    {
                        m_Flags = (GuildFlags)reader.ReadInt();
                        goto case 7;
                    }
                case 7:
                    {
                        m_bGuildWarRing = reader.ReadBool();
                        goto case 6;
                    }
                case 6:
                    {
                        m_TownshipStone = reader.ReadItem();
                        goto case 5;
                    }
                case 5:
                    {
                        m_IOBAlignment = (IOBAlignment)reader.ReadInt();
                        goto case 4;
                    }
                case 4:
                    {
                        m_AllyDeclarations = reader.ReadGuildList();
                        m_AllyInvitations = reader.ReadGuildList();

                        goto case 3;
                    }
                case 3:
                    {
                        if (version >= 10)
                            m_NextTypeChange = reader.ReadDateTime();
                        else
                            m_NextTypeChange = reader.ReadDateTime() + TimeSpan.FromDays(7.0);

                        goto case 2;
                    }
                case 2:
                    {
                        m_Type = (GuildType)reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_LastFealty = reader.ReadDateTime();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Leader = reader.ReadMobile();
                        m_Name = reader.ReadString();
                        m_Abbreviation = reader.ReadString();

                        m_Allies = reader.ReadGuildList();
                        m_Enemies = reader.ReadGuildList();
                        m_WarDeclarations = reader.ReadGuildList();
                        m_WarInvitations = reader.ReadGuildList();

                        m_Members = reader.ReadMobileList();
                        m_Candidates = reader.ReadMobileList();
                        m_Accepted = reader.ReadMobileList();

                        m_Guildstone = reader.ReadItem();
                        m_Teleporter = reader.ReadItem();

                        m_Charter = reader.ReadString();
                        m_Website = reader.ReadString();

                        break;
                    }
            }

            if (m_AllyDeclarations == null)
                m_AllyDeclarations = new ArrayList();

            if (m_AllyInvitations == null)
                m_AllyInvitations = new ArrayList();

            if (m_Banned == null)
                m_Banned = new Dictionary<Mobile, DateTime>();

            if (m_Guildstone == null || m_Members.Count == 0)
                Disband();
        }
        public static void Configure()
        {
            EventSink.CreateGuild += new CreateGuildHandler(EventSink_CreateGuild);

            Server.CommandSystem.Register("ListenToGuild", AccessLevel.GameMaster, new CommandEventHandler(ListenToGuild_OnCommand));
            Server.CommandSystem.Register("BanMember", AccessLevel.Player, new CommandEventHandler(BanMember_OnCommand));
            TargetCommands.Register(new GuildBanClear());
        }

        public static void ListenToGuild_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(ListenToGuild_OnTarget));
            e.Mobile.SendMessage("Target a guilded player.");
        }

        public static void ListenToGuild_OnTarget(Mobile from, object obj)
        {
            try
            {
                if (obj is Mobile)
                {
                    Guild g = ((Mobile)obj).Guild as Guild;

                    if (g == null)
                    {
                        from.SendMessage("They are not in a guild.");
                    }
                    else if (g.m_Listeners.Contains(from))
                    {
                        g.m_Listeners.Remove(from);
                        from.SendMessage("You are no longer listening to the guild [" + g.Abbreviation + "].");
                    }
                    else
                    {
                        g.m_Listeners.Add(from);
                        from.SendMessage("You are now listening to the guild [" + g.Abbreviation + "].");
                    }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
        private static void BanMember_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile != null)
            {
                Guild g = e.Mobile.Guild as Guild;
                if (g == null) return;  // no message
                if (g.Leader != e.Mobile)
                {
                    e.Mobile.SendMessage("You must be the guildmaster to use this command.");
                    return;
                }
                e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(BanMember_OnTarget));
                e.Mobile.SendMessage("Target a guilded player.");
            }
        }
        public static void BanMember_OnTarget(Mobile me, object obj)
        {
            Guild my_guild = me.Guild as Guild;
            try
            {
                Mobile them = obj as Mobile;
                if (them != null)
                {
                    Guild targeted_guild = them.Guild as Guild;
                    if (targeted_guild == null)
                    {
                        me.SendMessage("They are not in a guild.");
                    }
                    else if (!targeted_guild.Name.Equals(my_guild.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        me.SendMessage("They are not in your guild.");
                    }
                    else if (them == me)
                    {
                        me.SendMessage("You cannot ban yourself.");
                    }
                    else
                    {
                        my_guild.RemoveMember(obj as Mobile);
                        my_guild.BanMember(obj as Mobile);
                        me.SendMessage("{0} has been banned from your guild for {1} days.",
                            (them).Name, my_guild.BanExpiry(them).TotalDays.ToString("0.##"));
                    }
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public class GuildBanClear : BaseCommand
        {
            public GuildBanClear()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "GuildBanClear" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "GuildBanClear";
                Description = "Clears the guild-ban status of the targeted mobile.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                Mobile m = (Mobile)obj;

                bool any = false;

                foreach (BaseGuild g in Guild.List.Values)
                {
                    if (g is Guild)
                    {
                        Guild guild = (Guild)g;

                        if (guild.m_Banned != null && guild.m_Banned.Remove(m))
                            any = true;
                    }
                }

                if (any)
                    e.Mobile.SendMessage("They are now unbanned from all guilds.");
                else
                    e.Mobile.SendMessage("They were not banned from any guild.");
            }
        }
    }
}