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

/* Scripts/Accounting/Emailer.cs
 *  CHANGELOG:
 *  7/29/2024, Adam
 *      Before calling Post(), make sure email has been setup with a call to Core.EmailCheck()
 *	5/7/08, Adam
 *		- make threads low priority
 *		- make threads background
 *		- beter exception logging
 *		- move sleep to top of loop to catch looping over bad addresses
 *  5/6/08, Adam
 *		Turned off taran's thread manager as we're not using it and it makes profiling a bit tougher
 *  12/24/06, Adam
 *      Add call to  CheckEmailAddy() to validate an email address
 *  11/18/06, Adam
 *      Update to new .NET 2.0 email services
 *	11/8/06, Adam
 *		modify debug output to mesh with the debug output in engines/email/smtp.cs
 *	2/14/06, Adam
 *		turn on JM once again
 *	2/12/06, Adam
 *		use simple 'native' threads for background email
 *	2/10/06, Adam
 *		Disable backgrounding of email until the Job Manager is fixed.
 *		Search key "EMAIL_BACKGROUND"
 *	6/25/05: Pix
 *		Changed where we log to.  SendEmail now returns success or failure.
 *	6/11/05: Pixie
 *		created by Pixie June 2005
 *		Initial Version.
 */

using Server.Diagnostics;
using Server.SMTP;					// new SMTP engine
using System;
using System.Collections;	// ArrayList
using System.Threading;		// threads

namespace Server.Accounting
{
    /// <summary>
    /// Summary description for Emailer.
    /// </summary>
    public class Emailer
    {
        private class ParamPack
        {
            public ArrayList m_distributionList = null;
            public string m_toAddress = null;
            public string m_subject = null;
            public string m_body = null;
            public bool m_ccRegistration = false;

            public ParamPack(string toAddress, string subject, string body, bool ccRegistration)
            {
                m_toAddress = toAddress;
                m_subject = subject;
                m_body = body;
                m_ccRegistration = ccRegistration;
            }

            public ParamPack(ArrayList toAddresses, string subject, string body, bool ccRegistration)
            {
                m_distributionList = toAddresses;
                m_subject = subject;
                m_body = body;
                m_ccRegistration = ccRegistration;
            }

            public void post()
            {
                if (Core.EmailCheck())
                {
                    if (m_distributionList != null)
                        ThreadWorkers.DistributionList(this);
                    else
                        ThreadWorkers.StandardEmail(this);
                }
            }
        }

        public Emailer()
        {
        }

        public bool SendEmail(string toAddress, string subject, string body, bool ccRegistration)
        {
            ParamPack parms = new ParamPack(toAddress, subject, body, ccRegistration);

            // okay, now hand the list of users off to our mailer daemon
            //ThreadJob job = new ThreadJob(new JobWorker(ThreadWorkers.StandardEmail), parms, null);
            //job.Start(JobPriority.Critical);

            // non threaded version
            //ThreadWorkers.StandardEmail(parms);

            // simple threads version
            Thread t = new Thread(new ThreadStart(parms.post));
            t.Priority = ThreadPriority.Lowest;
            t.IsBackground = true;
            t.Start();
            return true;
        }

        public bool SendEmail(ArrayList toAddresses, string subject, string body, bool ccRegistration)
        {
            ParamPack parms = new ParamPack(toAddresses, subject, body, ccRegistration);

            // okay, now hand the list of users off to our mailer daemon
            //ThreadJob job = new ThreadJob(new JobWorker(ThreadWorkers.DistributionList), parms, null);
            //job.Start(JobPriority.Critical);

            // non threaded version
            //ThreadWorkers.DistributionList(parms);

            // simple threads version
            Thread t = new Thread(new ThreadStart(parms.post));
            t.Priority = ThreadPriority.Lowest;
            t.IsBackground = true;
            t.Start();
            return true;
        }

        public class ThreadWorkers
        {
            public static object StandardEmail(object parms)
            {
                try
                {
                    ParamPack px = parms as ParamPack;

                    // hmm.. badness
                    if (px == null || px.m_toAddress == null)
                    {
                        string message = String.Format("Error: in Accounting.Emailer. parameter pack.");
                        throw new ApplicationException(message);
                    }

                    // Adam: bacause we may send this to both the recipient and the email archive server
                    //  we allow distribution lists
                    if (SmtpDirect.CheckEmailAddy(px.m_toAddress, true) == false)
                    {
                        string message = String.Format("Email: Bad address detected '{0}'.", px.m_toAddress);
                        throw new ApplicationException(message);
                    }

                    SmtpDirect mail = new SmtpDirect();
                    mail.SendEmail(px.m_toAddress, px.m_subject, px.m_body, px.m_ccRegistration);
                    return true;
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    Console.WriteLine("Exception: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                return false;
            }

            public static object DistributionList(object parms)
            {
                try
                {
                    ParamPack px = parms as ParamPack;

                    // hmm.. badness
                    if (px == null || px.m_distributionList == null)
                    {
                        try { throw new ApplicationException("Error: in Accounting.Emailer. paramater pack."); }
                        catch (Exception ex) { Server.EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                        return false;
                    }

                    // for now, just print them
                    for (int ix = 0; ix < px.m_distributionList.Count; ix++)
                    {
                        // pause for 1 second between sends
                        //	if we are sending to N thousand players, we don't want to 
                        //	eat all our bandwidth
                        System.Threading.Thread.Sleep(1000);

                        string addy = px.m_distributionList[ix] as string;
                        if (addy == null) continue;             // may not be null

                        // Adam: bacause recipient is specified, we do not allow distribution lists
                        if (SmtpDirect.CheckEmailAddy(addy, false) == false)
                        {
                            Console.WriteLine("Email: Bad address detected '{0}'.", addy);
                            continue;
                        }

                        Console.WriteLine("Email: Sending  {0} of {1}.", ix + 1, px.m_distributionList.Count);

                        // send it baby!
                        new SmtpDirect().SendEmail(addy, px.m_subject, px.m_body, false);
                    }

                    Console.WriteLine("Done.");
                    return px.m_distributionList.Count;
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    Console.WriteLine("Exception: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }

                return 0;
            }
        }
    }
}