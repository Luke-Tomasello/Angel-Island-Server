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

using Server.Accounting;
using System;
using System.Net;
namespace Server.Commands
{
    public class GameIPCommand
    {
        public static void Initialize()
        {
            Register();
        }

        public static void Register()
        {
            Server.CommandSystem.Register("GameIP", AccessLevel.Administrator, new CommandEventHandler(GameIP_OnCommand));
        }

        [Usage("GameIP")]
        [Description("All game login ip commands")]
        private static void GameIP_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length < 1)
            {
                e.Mobile.SendMessage("You must supply arguments to use this command.");
                e.Mobile.SendMessage("DO NOT USE THIS COMMAND IF YOU DO NOT KNOW WHAT YOU ARE DOING!!");
                return;
            }

            if (e.Arguments[0].ToUpper() == "WIPE")
            {
                int count = 0;
                try
                {
                    foreach (Account a in Accounts.Table.Values)
                    {
                        a.ClearGAMELogin();
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    e.Mobile.SendMessage("EXCEPTION: " + ex.Message);
                }

                e.Mobile.SendMessage("{0} game login ips cleared.", count);
            }
            else if (e.Arguments[0].ToUpper() == "CLEAR")
            {
                if (e.Arguments.Length < 2)
                {
                    e.Mobile.SendMessage("GameIP Clear must specify account");
                    return;
                }

                string acctname = e.Arguments[1];
                Account a = Accounts.GetAccount(acctname);
                if (a == null)
                {
                    e.Mobile.SendMessage("Accountname {0} doesn't seem to exists.", acctname);
                }
                else
                {
                    a.ClearGAMELogin();
                    e.Mobile.SendMessage("Accountname {0}'s game login ip cleared.", acctname);
                }
            }
            else if (e.Arguments[0].ToUpper() == "GETIP")
            {
                if (e.Arguments.Length < 2)
                {
                    e.Mobile.SendMessage("GameIP GetIP must specify account");
                    return;
                }

                string acctname = e.Arguments[1];
                Account a = Accounts.GetAccount(acctname);
                if (a == null)
                {
                    e.Mobile.SendMessage("Accountname {0} doesn't seem to exists.", acctname);
                }
                else
                {
                    IPAddress ip = a.LastGAMELogin;
                    e.Mobile.SendMessage("Accountname {0}'s game login is: {1}", acctname, ip.ToString());
                }
            }
            else if (e.Arguments[0].ToUpper() == "GETACCTS")
            {
                if (e.Arguments.Length < 2)
                {
                    e.Mobile.SendMessage("GameIP GetAccts must specify ip");
                    return;
                }

                try
                {
                    int count = 0;
                    IPAddress ip = IPAddress.Parse(e.Arguments[1]);
                    foreach (Account a in Accounts.Table.Values)
                    {
                        try
                        {
                            if (a.LastGAMELogin != null && a.LastGAMELogin.Equals(ip))
                            {
                                e.Mobile.SendMessage("Account found for IP {0}: {2} : {1}", ip.ToString(), a.Username, a.AccessLevel.ToString());
                                count++;
                            }
                        }
                        catch { }
                    }
                    e.Mobile.SendMessage("{0} Accounts found with last game login ip of {1}", count, ip.ToString());
                }
                catch (Exception ex)
                {
                    e.Mobile.SendMessage("Exception: " + ex.Message);
                }
            }
            else
            {
                e.Mobile.SendMessage("Please use one of: Wipe, Clear, GetIP, GetAccts");
            }

        }
    }
}