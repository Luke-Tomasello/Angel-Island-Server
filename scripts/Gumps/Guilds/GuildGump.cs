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

/* Scripts/Gumps/Guilds/GuildGump.cs
 * Changelog:
 *  05/28/08, Pix
 *		When listing fealty, ensure that GuildFealty is set to a member of the guild.
 *  01/14/08, Pix
 *      Added indication of the amount of votes you have when looking at the guildstone.
 *  01/14/08, Pix
 *      Added button for staff to be able to force a guildmaster vote calculation.
 *  12/21/07, Pix
 *      Added no counting flag for guilds.
 *  12/5/07, Adam
 *      Add support for 'peaceful' guilds. I.e., disable war etc. menus
 *	12/19/05, Pix
 *		Fixed spelling and button/text alignment for non-guildmaster.
 *  12/14/05, Kit
 *		Added Ally Menu
 *	3/12/05, mith
 *		Added "Move this guildstone" if guild member is house owner but not GM.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Guilds;
using Server.Multis;
using Server.Network;

namespace Server.Gumps
{
    public class GuildGump : Gump
    {
        private Mobile m_Mobile;
        private Guild m_Guild;

        public GuildGump(Mobile beholder, Guild guild)
            : base(20, 30)
        {
            m_Mobile = beholder;
            m_Guild = guild;

            Dragable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 420, 5054);
            AddBackground(10, 10, 530, 400, 3000);

            AddHtml(20, 15, 200, 35, guild.Name, false, false);

            Mobile leader = guild.Leader;

            if (leader != null)
            {
                string leadTitle;

                if ((leadTitle = leader.GuildTitle) != null && (leadTitle = leadTitle.Trim()).Length > 0)
                    leadTitle += ": ";
                else
                    leadTitle = "";

                string leadName;

                if ((leadName = leader.Name) == null || (leadName = leadName.Trim()).Length <= 0)
                    leadName = "(empty)";

                AddHtml(220, 15, 250, 35, leadTitle + leadName, false, false);
            }

            AddButton(20, 50, 4005, 4007, 1, GumpButtonType.Reply, 0);
            //AddHtmlLocalized( 55, 50, 100, 20, 1013022, false, false ); // Loyal to

            Mobile fealty = beholder.GuildFealty;

            //Pix: ensure that GuildFealty if set is a member of the guild - otherwise, null it.
            if (fealty != null)
            {
                if (!guild.IsMember(fealty))
                {
                    beholder.GuildFealty = null;
                    fealty = null;
                }
            }

            if (fealty == null)
            {
                fealty = beholder;
            }

            string fealtyName;

            if (fealty == null
                 || (fealtyName = fealty.Name) == null
                 || (fealtyName = fealtyName.Trim()).Length <= 0)
            {
                fealtyName = "(empty)";
            }

            if (beholder == fealty)
            {
                fealtyName = "Yourself";
            }

            AddHtml(55, 50, 300, 20, "Loyal to: " + fealtyName, false, false);
            //AddHtml(55, 70, 470, 20, "Loyal to: " + fealtyName, false, false);

            int votesForMe = m_Guild.GetVotesFor(beholder);
            AddHtml(55, 70, 300, 20, "Votes for you: " + votesForMe.ToString(), false, false);


            AddButton(215, 50, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtmlLocalized(250, 50, 170, 20, 1013023, false, false); // Display guild abbreviation
            AddHtmlLocalized(250, 70, 50, 20, beholder.DisplayGuildTitle ? 1011262 : 1011263, false, false); // on/off

            AddButton(20, 100, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 100, 470, 30, 1011086, false, false); // View the current roster.

            if (m_Guild.IsNoCountingGuild)
            {
                AddHtml(320, 100, 470, 20, "Guildmembers cannot report murders.", false, false);
            }
            else
            {
                AddHtml(320, 100, 470, 20, "Guildmembers can report murders.", false, false);
            }

            AddButton(20, 130, 4005, 4007, 4, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 130, 470, 30, 1011085, false, false); // Recruit someone into the guild.

            if (guild.Candidates.Count > 0)
            {
                AddButton(20, 160, 4005, 4007, 5, GumpButtonType.Reply, 0);
                AddHtmlLocalized(55, 160, 470, 30, 1011093, false, false); // View list of candidates who have been sponsored to the guild.
            }
            else
            {
                AddImage(20, 160, 4020);
                AddHtmlLocalized(55, 160, 470, 30, 1013031, false, false); // There are currently no candidates for membership.
            }

            AddButton(20, 220, 4005, 4007, 6, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 220, 470, 30, 1011087, false, false); // View the guild's charter.

            AddButton(20, 250, 4005, 4007, 7, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 250, 470, 30, 1011092, false, false); // Resign from the guild.

            if (guild.Peaceful == false)
            {
                AddButton(20, 280, 4005, 4007, 8, GumpButtonType.Reply, 0);
                AddHtmlLocalized(55, 280, 470, 30, 1011095, false, false); // View list of guilds you are at war with.
            }
            else
            {
                AddImage(20, 280, 4020);
                AddHtmlLocalized(55, 280, 470, 30, 1011095, false, false); // View list of guilds you are at war with.
            }

            if (m_Guild.GuildWarRing)
            {
                AddHtml(320, 280, 470, 20, "(Guild War Ring Active)", false, false);
            }
            else
            {
                AddHtml(320, 280, 470, 20, "(Guild War Ring Inactive)", false, false);
            }

            if (guild.Peaceful == false)
            {
                AddButton(20, 310, 4005, 4007, 10, GumpButtonType.Reply, 0);
                AddHtml(55, 310, 470, 30, "View list of guilds you are allied with.", false, false); // View list of guilds you are at war with.
            }
            else
            {
                AddImage(20, 310, 4020);
                AddHtml(55, 310, 470, 30, "View list of guilds you are allied with.", false, false); // View list of guilds you are at war with.
            }


            if (beholder.AccessLevel >= AccessLevel.GameMaster || beholder == leader)
            {
                AddButton(20, 340, 4005, 4007, 9, GumpButtonType.Reply, 0);
                AddHtmlLocalized(55, 340, 470, 30, 1011094, false, false); // Access guildmaster functions.
            }
            else
            {
                BaseHouse house = BaseHouse.FindHouseAt(beholder);

                if (house != null && beholder == house.Owner)
                {
                    AddButton(20, 340, 4005, 4007, 9, GumpButtonType.Reply, 0);
                    AddHtmlLocalized(55, 340, 470, 30, 1011119, false, false); // Move this guildstone
                }
                else
                {
                    AddImage(20, 340, 4020);
                    AddHtmlLocalized(55, 340, 470, 30, 1018013, false, false); // Reserved for guildmaster
                }
            }

            if (beholder.AccessLevel >= AccessLevel.Counselor)
            {
                AddButton(215, 390, 4005, 4007, 11, GumpButtonType.Reply, 0);
                AddHtml(250, 390, 100, 30, "Force Revote", false, false);
            }

            AddButton(20, 390, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 390, 470, 30, 1011441, false, false); // EXIT
        }

        public static void EnsureClosed(Mobile m)
        {
            m.CloseGump(typeof(DeclareFealtyGump));
            m.CloseGump(typeof(GrantGuildTitleGump));
            m.CloseGump(typeof(GuildAdminCandidatesGump));
            m.CloseGump(typeof(GuildCandidatesGump));
            m.CloseGump(typeof(GuildChangeTypeGump));
            m.CloseGump(typeof(GuildCharterGump));
            m.CloseGump(typeof(GuildDismissGump));
            m.CloseGump(typeof(GuildGump));
            m.CloseGump(typeof(GuildmasterGump));
            m.CloseGump(typeof(GuildRosterGump));
            m.CloseGump(typeof(GuildWarGump));
            m.CloseGump(typeof(MoveGuildstoneGump));
        }

        public static bool BadLeader(Mobile m, Guild g)
        {
            if (m.Deleted || g.Disbanded || (m.AccessLevel < AccessLevel.GameMaster && g.Leader != m))
                return true;

            Item stone = g.Guildstone;

            return (stone == null || stone.Deleted || !m.InRange(stone.GetWorldLocation(), 2));
        }

        public static bool BadMember(Mobile m, Guild g)
        {
            if (m.Deleted || g.Disbanded || (m.AccessLevel < AccessLevel.GameMaster && !g.IsMember(m)))
                return true;

            Item stone = g.Guildstone;

            return (stone == null || stone.Deleted || !m.InRange(stone.GetWorldLocation(), 2));
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (BadMember(m_Mobile, m_Guild))
                return;

            switch (info.ButtonID)
            {
                case 1: // Loyalty
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new DeclareFealtyGump(m_Mobile, m_Guild));

                        break;
                    }
                case 2: // Toggle display abbreviation
                    {
                        m_Mobile.DisplayGuildTitle = !m_Mobile.DisplayGuildTitle;

                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildGump(m_Mobile, m_Guild));

                        break;
                    }
                case 3: // View the current roster
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildRosterGump(m_Mobile, m_Guild));

                        break;
                    }
                case 4: // Recruit
                    {
                        m_Mobile.Target = new GuildRecruitTarget(m_Mobile, m_Guild);

                        break;
                    }
                case 5: // Membership candidates
                    {
                        GuildGump.EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildCandidatesGump(m_Mobile, m_Guild));

                        break;
                    }
                case 6: // View charter
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildCharterGump(m_Mobile, m_Guild));

                        break;
                    }
                case 7: // Resign
                    {
                        m_Guild.RemoveMember(m_Mobile);

                        break;
                    }
                case 8: // View wars
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildWarGump(m_Mobile, m_Guild));

                        break;
                    }
                case 9: // Guildmaster functions
                    {
                        BaseHouse house = BaseHouse.FindHouseAt(m_Mobile);

                        if (m_Mobile.AccessLevel >= AccessLevel.GameMaster || m_Guild.Leader == m_Mobile)
                        {
                            EnsureClosed(m_Mobile);
                            m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
                        }
                        else if (house != null && m_Mobile == house.Owner)
                        {
                            ((Items.Guildstone)m_Guild.Guildstone).OnPrepareMove(m_Mobile);
                        }

                        break;
                    }
                case 10: // View Allies
                    {
                        EnsureClosed(m_Mobile);
                        m_Mobile.SendGump(new GuildAllianceGump(m_Mobile, m_Guild));

                        break;
                    }
                case 11: //Force new guildmaster recalculation (available only to staff)
                    {
                        if (m_Mobile.AccessLevel >= AccessLevel.Counselor)
                        {
                            m_Guild.CalculateGuildmaster();
                        }
                        else
                        {
                            m_Mobile.SendMessage("You can't cheat like that.");
                        }
                        m_Mobile.SendGump(new GuildGump(m_Mobile, m_Guild));
                        break;
                    }
            }
        }
    }
}