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

/* Scripts\Gumps\GuildLastMemberWarningGump.cs
 * ChangeLog
 *  2/4/2024, Adam
 *      initial creation
 */

using Server.Diagnostics;
using Server.Guilds;
using Server.Network;
using System;

namespace Server.Gumps
{
    public enum GuildWarning
    {
        None,
        IResign,
        /*dismiss member*/
    }
    public class GuildLastMemberWarningGump : Gump
    {
        private Mobile m_Mobile;
        private GuildWarning m_ExitType;
        public static bool CheckTownship(Mobile from)
        {
            Guild guild = from.Guild as Guild;
            return from != null && guild != null && guild.TownshipStone != null;
        }
        public static bool CheckLastMember(Mobile from)
        {
            Guild guild = from.Guild as Guild;
            return from != null && guild != null && guild.Members != null && guild.IsMember(from) && guild.Members.Count == 1;
        }
        public GuildLastMemberWarningGump(Mobile mobile, GuildWarning exit_type)
            : base(110, 100)
        {
            m_Mobile = mobile;
            m_ExitType = exit_type;

            string first_line = "Unknown mode";
            switch (exit_type)
            {
                case GuildWarning.IResign:
                    first_line = "You are about to remove the last member in the guild but a township exists! ";
                    break;
            }

            mobile.CloseGump(typeof(GuildLastMemberWarningGump));

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, 420, 280, 5054);

            AddImageTiled(10, 10, 400, 20, 2624);
            AddAlphaRegion(10, 10, 400, 20);

            AddHtmlLocalized(10, 10, 400, 20, 1060635, 30720, false, false); // <CENTER>WARNING</CENTER>

            AddImageTiled(10, 40, 400, 200, 2624);
            AddAlphaRegion(10, 40, 400, 200);

            // The following warning is more appropriate for AI than the localized warning.
            String WarningString =
                first_line +
                "Disbanding the guild will result in the township and all township items being deleted. " +
                //"All items in the house will remain behind and can be freely picked up by anyone. " +
                //"Once the house is demolished, anyone can attempt to place a new house on the vacant land. " +
                "Are you sure you wish to continue?";

            AddHtml(10, 40, 400, 200, WarningString, false, true);

            AddImageTiled(10, 250, 400, 20, 2624);
            AddAlphaRegion(10, 250, 400, 20);

            AddButton(10, 250, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 250, 170, 20, 1011036, 32767, false, false); // OKAY

            AddButton(210, 250, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(240, 250, 170, 20, 1011012, 32767, false, false); // CANCEL
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;
            Guild guild = from.Guild as Guild;
            if (true)
            {
                if (info.ButtonID == 1)
                {
                    if (true)
                    {
                        LogHelper logger = new LogHelper("Last guild member to resign.log", overwrite: false, sline: true);
                        try
                        {
                            switch (m_ExitType)
                            {
                                case GuildWarning.IResign:
                                    {
                                        logger.Log(string.Format("{0} used phrase \"i resign from my guild\" ({1}) and accepted delete township warning.", from, guild));
                                        guild.ResignMember(from);
                                        break;
                                    }
                                default:
                                    {   // dismiss member
                                        guild.RemoveMember(from);
                                        guild.BanMember(from);
                                        break;
                                    }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(ex);
                        }
                        finally
                        {
                            logger.Finish();
                        }

                    }
                    else
                    {
                        m_Mobile.SendLocalizedMessage(501320); // Only the house owner may do this.
                    }
                }
            }
            else
                state.Mobile.SendMessage("Currently unavailable on production shards.");
        }
    }
}