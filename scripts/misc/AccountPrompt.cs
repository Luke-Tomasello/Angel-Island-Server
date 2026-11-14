/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Misc\AccountPrompt.cs
 * CHANGELOG:
 *  11/8/26, Adam (login server owner)
 *      The login server prompts too: a login server with no accounts asks for its owner account at startup, like any
 *      shard (it was skipped). This replaces the owner-IP rule in CharacterCreation: the login server keeps its accounts
 *      (its wipe deletes only items and mobiles, and now spares staff characters), so its owner is a real Owner account.
 *      With the login database, an account already there keeps the database's password (Accounts.AddAccount syncs it).
 *  12/08/21, Pix
 *      Changed/enhanced console error message.
 *	12/2/21, Adam (try/catch)
 *	    Added exception handling to handle the case when there are noy accounts and the accounts hashtable hs not been initialized.
 */

using Server.Accounting;
using System;

namespace Server.Misc
{
    public class AccountPrompt
    {
        // This script prompts the console for a username and password when 0 accounts have been loaded
        public static void Initialize()
        {
            // every server, the login server too (11/8/26): its owner account is made here, like any shard's
            try
            {
                if (Accounts.Table.Count == 0 && !Core.Service)
                {
                    Console.WriteLine("This server has no accounts.");
                    Console.WriteLine("Do you want to create an administrator account now? (y/n)");

                    if (Console.ReadLine().StartsWith("y"))
                    {
                        Console.Write("Username: ");
                        string? username = Console.ReadLine();

                        Console.Write("Password: ");
                        string? password = Console.ReadLine();

                        Account? a = Accounts.AddAccount(username, password);

                        // 8/10/2024: Adam, first account is Owner
                        //a.AccessLevel = AccessLevel.Administrator;
                        a.AccessLevelInternal = AccessLevel.Owner;
                        if (a.AccessLevel == AccessLevel.Owner)
                            Console.WriteLine("Owner account created, continuing");
                        else
                            Console.WriteLine("Administrator account created, continuing");
                        if (Core.UseLoginDB)
                            Console.WriteLine("(An account already in the login database keeps that database's password.)");
                    }
                    else
                    {
                        Console.WriteLine("Account not created.");
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Monitor.WriteLine(ex.Message, ConsoleColor.Red);
                Utility.Monitor.WriteLine("Error initializing accounts - maybe not able to create Saves/Account/account.xml or path in function Accounts.LoadAccountDB().", ConsoleColor.Red);
                Utility.Monitor.WriteLine("Check for previous errors!", ConsoleColor.Red);
            }
        }
    }
}