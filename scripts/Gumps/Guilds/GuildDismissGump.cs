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

/* Scripts/Gumps/Guilds/GuildDismissGump.cs
 * Changelog:
 * 01/23/22 Pix
 *      Changed base class to GuildMobileListServerPagedGump
 *  8/18/21, adam
 *      Add m_Guild.BanMember(m);       // currently only used by New in the [new command
 *      We check this in the [new command to prevennt a recently kicked member from rejoining for 14 days.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
    public class GuildDismissGump : GuildMobileListServerPagedGump
    {
        public GuildDismissGump(Mobile from, Guild guild)
            : this(from, guild, 0)
        {
        }

        public GuildDismissGump(Mobile from, Guild guild, int page)
            : base(from, guild, true, guild.Members, page)
        {
        }

        protected override void Design()
        {
            AddHtmlLocalized(20, 10, 400, 35, 1011124, false, false); // Whom do you wish to dismiss?

            AddButton(20, 400, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(55, 400, 245, 30, 1011125, false, false); // Kick them out!

            AddButton(300, 400, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtmlLocalized(335, 400, 100, 35, 1011012, false, false); // CANCEL
        }

        protected override void SendPage(Mobile from, Guild guild, int newpage)
        {
            m_Mobile.SendGump(new GuildDismissGump(from, guild, newpage));
        }

        protected override void HandleDesignedReponse(NetState state, RelayInfo info)
        {
            if (GuildGump.BadLeader(m_Mobile, m_Guild))
                return;

            if (info.ButtonID == 1)
            {
                int[] switches = info.Switches;

                if (switches.Length > 0)
                {
                    int index = switches[0];

                    if (index >= 0 && index < m_List.Count)
                    {
                        Mobile m = (Mobile)m_List[index];

                        if (m != null && !m.Deleted)
                        {
                            if (m != World.GetSystemAcct() || m_Mobile.AccessLevel >= AccessLevel.GameMaster)
                            {
                                m_Guild.RemoveMember(m);
                                m_Guild.BanMember(m);
                            }
                            else
                                m_Mobile.SendMessage("You cannot eject the system account.");

                            if (m_Mobile.AccessLevel >= AccessLevel.GameMaster || m_Mobile == m_Guild.Leader)
                            {
                                GuildGump.EnsureClosed(m_Mobile);
                                m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
                            }
                        }
                    }
                }
            }
            else if (info.ButtonID == 2 && (m_Mobile.AccessLevel >= AccessLevel.GameMaster || m_Mobile == m_Guild.Leader))
            {
                GuildGump.EnsureClosed(m_Mobile);
                m_Mobile.SendGump(new GuildmasterGump(m_Mobile, m_Guild));
            }
        }
    }
}