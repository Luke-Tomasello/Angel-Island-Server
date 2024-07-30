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

/* Scripts/Gumps/Guilds/JoinNEWGuildGump.cs
 * ChangeLog
 *  10/21/23, Yoar
 *      Refactored. Auto-join is no longer dependent on the guild abbreviation.
 * 11/5/21: Adam
 *	comment out the requirement that New be peaceful - there was a request that they can spar one another
 *	8/14/21, Adam
 *		Turning this back on for our relaunch.
 *		Add the [new command for players tha may have changed their minds.
 *  1/5/07, Adam
 *      Obsolete while we auto-add new players
 *  12/6/07, Adam
 *      First time check in.
 *      New gump to auto add players to the New Guild (a peaceful guild)
 */

using Server.Diagnostics;
using Server.Guilds;
using Server.Network;
using System;

namespace Server.Gumps
{
    public static class NewPlayerGuild
    {
        public static void Initialize()
        {
            CommandSystem.Register("New", AccessLevel.Player, new CommandEventHandler(New_OnCommand));
        }

        [Usage("New")]
        [Description("They would you like to join the guild for new players.")]
        private static void New_OnCommand(CommandEventArgs e)
        {
            try
            {
                OnCommand_Internal(e.Mobile);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        public static void OnWelcome(Mobile m)
        {
            try
            {
                OnWelcome_Internal(m);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private static void OnWelcome_Internal(Mobile m)
        {
            if (!CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.NewPlayerGuild))
                return;

            Guild g = Find();

            if (g == null)
                return;

            Accounting.Account a = m.Account as Accounting.Account;

            if (a == null)
                return;

            TimeSpan age = (DateTime.UtcNow - a.Created);

            if (age > TimeSpan.FromDays(30.0))
                return;

            if (CanJoin(m, g, false))
            {
                m.CloseGump(typeof(JoinGump));
                m.SendGump(new JoinGump());
            }
        }

        private static void OnCommand_Internal(Mobile m)
        {
            Guild g = Find();

            if (g == null)
            {
                m.SendMessage("The new player guild is temporarily unavailable.");
                return;
            }

            if (CanJoin(m, g))
            {
                m.CloseGump(typeof(JoinGump));
                m.SendGump(new JoinGump());
            }
        }

        private static bool CanJoin(Mobile m, Guild g, bool message = true)
        {
            if (g.IsMember(m))
            {
                if (message)
                    m.SendMessage("You are already a member of this guild.");

                return false;
            }

            Guild.GuildBanType banType = g.IsBannedMember(m);

            if (banType == Guild.GuildBanType.Self)
            {
                if (message)
                {
                    TimeSpan ts = g.BanExpiry(m);

                    m.SendMessage("You were banned from this guild.");
                    m.SendMessage("You may rejoin in {0:0} days and {1} hours.", ts.Days, ts.Hours);
                }

                return false;
            }
            else if (banType == Guild.GuildBanType.Friend)
            {
                if (message)
                {
                    TimeSpan ts = g.BanExpiry(g.GetBannedMember(m));

                    m.SendMessage("A shared account was banned from this guild.");
                    m.SendMessage("You may rejoin in {0:0} days and {1} hours.", ts.Days, ts.Hours);
                }

                return false;
            }
            else if (banType != Guild.GuildBanType.None)
            {
                return false; // unhandled
            }

            return true;
        }

        private static void AcceptJoin(Mobile m)
        {
            Guild g = Find();

            if (g == null)
            {
                m.SendMessage("The new player guild is temporarily unavailable.");
                return;
            }

            if (CanJoin(m, g))
                Join(m, g);
        }

        private static void Join(Mobile m, Guild g)
        {
            LogHelper logger = new LogHelper("PlayerAddedToNEWGuild.log", false, true);
            logger.Log(LogType.Mobile, m);
            logger.Finish();

            g.AddMember(m);
            m.DisplayGuildTitle = true;
            m.GuildTitle = "Citizen";
            m.GuildFealty = g.Leader;

            g.GuildMessage(String.Format("{0} has just joined {1}.", m.Name, g.Abbreviation));
        }

        private class JoinGump : Gump
        {
            public JoinGump()
                : base(50, 50)
            {
                AddBackground(10, 10, 190, 140, 0x242C);

                AddHtml(30, 30, 150, 75, String.Format("<div align=CENTER>{0}</div>", "Would you like to join the guild for new players?"), false, false);

                AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (info.ButtonID == 1)
                {
                    try
                    {
                        AcceptJoin(sender.Mobile);
                    }
                    catch (Exception ex)
                    {
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }
        }

        private static Guild Find()
        {
            foreach (BaseGuild g in BaseGuild.List.Values)
            {
                if (g is Guild && ((Guild)g).NewPlayerGuild)
                    return (Guild)g;
            }

            return null;
        }
    }
}