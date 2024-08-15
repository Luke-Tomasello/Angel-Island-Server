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

/* Scripts/Commands/Email.cs
 * ChangeLog
 *	3/27/08, Adam
 *		Switch from Heartbeat to CronScheduler
 *  12/24/06, Adam
 *      Add call to  CheckEmailAddy() to validate an email address
 *  11/18/06, Adam
 *      Update to new .NET 2.0 email services
 *	11/13/06, Adam
 *		Enhance [AddressDump to take either a 'DaysActive int' OR 'activation since date' paramater
 *	11/11/06, Adam
 *		Add AddressDump command to dump player addresses
 *	11/3/06, Adam
 *		Add generic emailer for testing, and rename old [email command to ==> [Announcement
 *	7/19/06, Adam
 *		Convert to generic emailer.
 *	6/24/06, Adam
 *		Convert to generic email tester.
 *		- testing the date format for the email header
 *	6/22/06, Adam
 *		Initial version
 *		Send a generic email announcement to the player base
 *		See Also: Scripts/Engines/Heartbeat/HeartbeatData.cs
 */

using Server.Accounting;			// Emailer
using Server.Diagnostics;
using Server.Misc;					// Test Center
using Server.SMTP;                          // new SMTP engine
using System;
using System.Collections;
using System.IO;

namespace Server.Commands
{

    public class Email
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Email", AccessLevel.Administrator, new CommandEventHandler(Email_OnCommand));
        }

        private static void Usage(Mobile from)
        {
            if (from != null)
                from.SendMessage("Usage: [email <TO Address> <\"Subject\"> <\"Body\">");
        }

        [Usage("Email <TO Address> <\"Subject\"> <\"Body\">")]
        [Description("Send an email.")]
        public static void Email_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            // check arguments
            if (e.Length != 3)
            {
                Usage(from);
                return;
            }

            try
            {
                string To = e.GetString(0);
                string Subject = e.GetString(1);
                string Body = e.GetString(2);

                if (SmtpDirect.CheckEmailAddy(To, true) == false)
                {
                    from.SendMessage("Error: The 'to' address is ill formed.");
                    return;
                }

                // okay, now hand the list of users off to our mailer daemon
                new Emailer().SendEmail(To, Subject, Body, false);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                System.Console.WriteLine("Exception Caught in generic emailer: " + ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            }

            return;
        }
    }

    public class AddressDump
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("AddressDump", AccessLevel.Administrator, new CommandEventHandler(AddressDump_OnCommand));
        }

        private static void Usage(Mobile from)
        {
            if (from != null)
            {
                from.SendMessage("Usage: [AddressDump <PlayersDaysActive|Activations since 'date'>");
                from.SendMessage("Example: [AddressDump {0}", DateTime.UtcNow.ToShortDateString());
            }
        }

        private static bool ValidEmail(string addy)
        {
            if (addy == null) return false;                 // may not be null
            if (addy.IndexOf('@') <= 0) return false;       // must have '@' and not first
            if (addy.IndexOf('.') <= 0) return false;       // must have '.' and not first
            if (addy.Length < 5) return false;              // "a@b.c"
            if (addy.IndexOf(' ') >= 0) return false;       // must not have ' ' 
            if (addy.IndexOf('\t') >= 0) return false;      // must not have '\t' 
            if (addy.IndexOf(":\\/,;") >= 0) return false;  // must not have any of these

            return true;
        }

        private static bool LooksLikeInt(string text)
        {
            try { int temp = Convert.ToInt32(text); }
            catch { return false; }
            return true;
        }

        [Usage("AddressDump <PlayersDaysActive|Activations since 'date'>")]
        [Description("Dump email addresses of players active within X days or since Y date.")]
        public static void AddressDump_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            // check arguments
            if (e.Length < 1)
            {
                Usage(from);
                return;
            }

            int iChecked = 0;
            int Reminders = 0;
            try
            {
                // loop through the accouints looking for current users
                ArrayList results = new ArrayList();

                // assume DaysActive
                if (e.Length == 1 && LooksLikeInt(e.GetString(0)))
                {
                    int days = 0;
                    try { days = Convert.ToInt32(e.GetString(0)); }
                    catch { Usage(from); return; }
                    foreach (Account acct in Accounts.Table.Values)
                    {
                        iChecked++;
                        // logged in the last n days.
                        if (Server.Engines.CronScheduler.EmailHelpers.RecentLogin(acct, days) == true)
                        {
                            if (ValidEmail(acct.EmailAddress))
                            {
                                Reminders++;
                                results.Add(acct.EmailAddress);
                            }
                        }
                    }
                }
                // assume activations since date
                else
                {
                    string buff = null;
                    for (int ix = 0; ix < e.Length; ix++)
                        buff += e.GetString(ix) + " ";

                    DateTime Since;
                    try { Since = DateTime.Parse(buff); }
                    catch { Usage(from); return; }

                    foreach (Account acct in Accounts.Table.Values)
                    {
                        iChecked++;
                        // account created since...
                        if (acct.Created >= Since && acct.EmailAddress != null)
                        {
                            if (ValidEmail(acct.EmailAddress))
                            {
                                Reminders++;
                                results.Add(acct.EmailAddress);
                            }
                        }
                    }
                }

                if (Reminders > 0)
                {
                    from.SendMessage("Logging {0} email address(es).", Reminders);
                    LogHelper Logger = new LogHelper("accountEmails.log", true);

                    foreach (object ox in results)
                    {
                        string address = ox as string;
                        if (address == null) continue;
                        Logger.Log(LogType.Text, address);
                    }
                    Logger.Finish();
                }

            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                System.Console.WriteLine("Exception Caught in generic emailer: " + ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            }

            return;
        }
    }

    public class Announcement
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Announcement", AccessLevel.Administrator, new CommandEventHandler(Announcement_OnCommand));
        }

        private static void Usage(Mobile from)
        {
            if (from != null)
                from.SendMessage("Usage: [Announcement <PlayersDaysActive> <\"Subject\"> <MessageFileName>");
        }

        [Usage("Announcement <PlayersDaysActive> <\"Subject\"> <MessageFileName>")]
        [Description("Send a mass mailing to the players active within PlayersDaysActive.")]
        public static void Announcement_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            // check arguments
            if (e.Length != 3)
            {
                Usage(from);
                return;
            }

            // can only be run on Test Center
            if (TestCenter.Enabled == false)
            {
                from.SendMessage("This command may only be executed on Test Center.");
                return;
            }

            int iChecked = 0;
            int Reminders = 0;
            try
            {
                // loop through the accouints looking for current users
                ArrayList results = new ArrayList();

                int days = 0;
                try { days = Convert.ToInt32(e.GetString(0)); }
                catch { Usage(from); return; }

                foreach (Account acct in Accounts.Table.Values)
                {
                    iChecked++;
                    // logged in the last n days.
                    if (Server.Engines.CronScheduler.EmailHelpers.RecentLogin(acct, days) == true)
                    {
                        Reminders++;
                        results.Add(acct.EmailAddress);
                    }
                }

                if (Reminders > 0)
                {
                    from.SendMessage("Sending {0} email announcement(s).", Reminders);

                    string subject = string.Format(e.GetString(1));
                    string body = null;

                    try
                    {
                        // create reader & open file
                        TextReader tr = new StreamReader(string.Format("Msgs/{0}", e.GetString(2)));

                        // read it
                        body = tr.ReadToEnd();

                        // close the stream
                        tr.Close();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                        from.SendMessage(ex.Message);
                        Usage(from);
                        return;
                    }

                    // okay, now hand the list of users off to our mailer daemon
                    new Emailer().SendEmail(results, subject, body, false);
                }

            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                System.Console.WriteLine("Exception Caught in generic emailer: " + ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            }

            return;
        }
    }
}