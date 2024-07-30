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

/* Accounting/ProfileGump.cs
 * CHANGELOG:
 * 3/10/22, Adam (Email Address check)
 *  Commented out wobbly address check and update SMTP.SmtpDirect.CheckEmailAddy() to be more comprehensive. 
 * 8/30/21, Adam
 *      don't send users their password in clear text
 *      also warn them to contact AI staff if this password change is not recognized. 
 * 7/1/08, Adam
 *	Re: Email Notification Checkbox: leaving this code but turning it off for the time being.
 * 	We're now using our mailing list for players instead of the 'in game' list. 
 * 	the ingame list is better targeted, but adds confusion as the user has to opt out of two separate list (pissing them off)
 * 12/26/06, Pix
 *		Added call to SMTP.SmtpDirect.CheckEmailAddy() to check email address validity.
 * 11/06/06, Pix
 *		Changed the way accounts are activated - they are now activated without
 *		changing the password automatically.  Email addresses now are sent an
 *		activation key, which gets entered on this profile gump.  Users can also 
 *		resend their activation email and reset the activation attempt using
 *		the profile gump.
 * 10/15/06, Pix
 *		Added functionality for account.DoNotSendEmail.
 *	3/2/06, Pix
 *		Added SendActivationEmail() function.
 *	7/27/05, Pix
 *		Now new account activations have a different subject line than regular email changes.
 *	7/05/05, Pix
 *		Now sends audit emails instead of CCs.
 *	6/30/05, Pix
 *		Changes to email text.
 *	6/30/05, Pix
 *		Changed to send email to old email address when changing email address.
 *  6/29/05, Pix
 *		Added password length checking.
 *	6/29/05, Pix
 *		Changes to what's show based on whether we're activated.
 *		Checks and safetynets added to OnResponse()
 *	6/25/05, Pix
 *		Added checks to see whether sending email worked.
 *	6/11/05, Pixie
 *		Initial Version
 */

using Server.Accounting;
using Server.Diagnostics;
using System;
using System.Text;

namespace Server.Gumps
{
    public class ProfileGump : Gump
    {
        private Mobile m_From;
        private Account m_Account;

        private const int LabelHue = 0x480;
        private const int LabelColor32 = 0xFFFFFF;

        //buttons:
        private const int SET_EMAIL_BUTTON = 1;
        private const int SET_PASSWORD = 2;
        private const int SET_NOTIFICATION = 3;
        private const int RESEND_ACTIVATION = 4;
        private const int RESET_ACTIVATION_ATTEMPT = 5;
        private const int ENTER_ACTIVATION_KEY = 6;

        public static void Initialize()
        {
            CommandSystem.Register("Profile", AccessLevel.Player, new CommandEventHandler(Profile_OnCommand));
        }

        [Usage("Profile")]
        [Description("Opens an interface for profile management.")]
        public static void Profile_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new ProfileGump(e.Mobile));
        }


        public ProfileGump(Mobile from)
            : base(50, 40)
        {
            from.CloseGump(typeof(ProfileGump));

            m_From = from;
            m_Account = from.Account as Account;

            if (m_Account == null)
            {
                m_From.SendMessage(0x35, "Account error!");
                return;
            }

            //if( !m_Account.AccountActivated && m_Account.ActivationKey != null
            //	&& m_Account.ActivationKey.Length > 0 && m_Account.EmailAddress != null
            //	&& m_Account.EmailAddress.Length > 0 )
            //{
            //	m_From.SendMessage(0x35, "You must log in with the new password emailed to you to complete activation.");
            //	return;
            //}

            AddPage(0);
            AddBackground(0, 0, 540, 450, 5054);
            AddImageTiled(10, 10, 520, 430, 2624);
            AddAlphaRegion(10, 10, 520, 430);

            //////
            ////// Header
            //////

            //x,y,width,height
            AddHtml(10, 10, 460, 20, Color(Center("Account Profile"), LabelColor32), false, false);
            if (m_Account.AccountActivated)
            {
                AddHtml(10, 30, 460, 20, Color(Center("(Activated)"), 0x0000FF), false, false);
            }
            else
            {
                AddHtml(10, 30, 460, 20, Color(Center("(NOT Activated)"), 0xFF0000), false, false);
            }

            AddLabelCropped(20, 50, 150, 20, LabelHue, "Current Password:");
            AddTextField(160, 50, 100, 20, 0, "");

            //////
            ////// Email notifications
            //////

            //Adam: leaving this code but turning it off for the time being.
            //	We're now using our mailing list for players instead of the 'in game' list. 
            //	the ingame list is better targeted, but adds confusion as the user has to opt out of two separate list (pissing them off)
            //AddCheck( 20, 80, 210, 211, !m_Account.DoNotSendEmail, 5 );
            //AddLabelCropped( 50, 80, 250, 20, LabelHue, "Send informational email about AI events." );

            //AddButtonLabeled( 320, 80, SET_NOTIFICATION, "Set Notification" );

            //////
            ////// Email Address
            //////

            if (!m_Account.AccountActivated)
            {
                //not activated

                if (m_Account.ActivationKey != null && m_Account.ActivationKey.Length > 0
                    && m_Account.EmailAddress != null && m_Account.EmailAddress.Length > 0)
                {
                    //Activation key isn't empty and Email address isn't empty, so we've sent them an email
                    // with the activation key.
                    AddLabelCropped(20, 110, 150, 20, LabelHue, "Email: " + m_Account.EmailAddress);

                    AddLabelCropped(20, 140, 150, 20, LabelHue, "Activation Key");
                    AddTextField(120, 140, 180, 20, 1, "");

                    AddButtonLabeled(320, 110, RESEND_ACTIVATION, "Resend Activation Email");
                    //AddHtml( 10, 160, 460, 20, Color( Center( "Current password must be supplied to resend activation email." ), LabelColor32 ), false, false );

                    AddButtonLabeled(320, 140, ENTER_ACTIVATION_KEY, "Enter Activation Key");
                    //AddHtml( 10, 160, 460, 20, Color( Center( "Current password must be supplied to enter activation key." ), LabelColor32 ), false, false );

                    AddButtonLabeled(320, 170, RESET_ACTIVATION_ATTEMPT, "Reset Activation Attempt");
                    //AddHtml( 10, 160, 460, 20, Color( Center( "Current password must be supplied to resend activation email." ), LabelColor32 ), false, false );
                }
                else
                {
                    AddLabelCropped(20, 110, 150, 20, LabelHue, "Email");
                    AddTextField(100, 110, 200, 20, 1, m_Account.EmailAddress);

                    AddLabelCropped(20, 140, 150, 20, LabelHue, "Verify Email");
                    AddTextField(100, 140, 200, 20, 2, "");

                    AddButtonLabeled(320, 140, SET_EMAIL_BUTTON, "Activate");
                    AddHtml(10, 160, 460, 20, Color(Center("Current password must be supplied to activate."), LabelColor32), false, false);
                }
            }
            else
            {
                //Activated, allow them to enter a new email address and re-activate
                AddLabelCropped(20, 110, 150, 20, LabelHue, "Email");
                AddTextField(100, 110, 200, 20, 1, m_Account.EmailAddress);

                AddLabelCropped(20, 140, 150, 20, LabelHue, "Verify Email");
                AddTextField(100, 140, 200, 20, 2, "");

                AddButtonLabeled(320, 140, SET_EMAIL_BUTTON, "Set Email");
                AddHtml(10, 160, 460, 20, Color(Center("Current password must be supplied to set email."), LabelColor32), false, false);
            }

            //////
            ////// Password Setting
            //////

            if (m_Account.AccountActivated)
            {
                AddLabelCropped(20, 200, 150, 20, LabelHue, "New Password");
                AddTextField(160, 200, 100, 20, 3, "");

                AddLabelCropped(20, 230, 150, 20, LabelHue, "Confirm New Password");
                AddTextField(160, 230, 100, 20, 4, "");

                AddButtonLabeled(280, 230, SET_PASSWORD, "Set New Password");
                AddHtml(10, 250, 460, 20, Color(Center("Current password must be supplied to set password."), LabelColor32), false, false);
            }

            //////
            ////// "Help" text
            //////

            StringBuilder sb = new StringBuilder();
            sb.Append("Setting email address is needed for lost password/account requests.");
            sb.Append("<br>In order to verify the email address entered, an activation key ");
            sb.Append("will be mailed to the email address entered.  You then enter this key using ");
            sb.Append("the [profile command again.  Then the activation process is complete.  ");
            sb.Append("After an account is activated, you will be able to change your password ");
            sb.Append("with the [profile command.");

            AddHtml(20, 280, 440, 140, sb.ToString(), true, true);
        }

        public static bool SendActivationEmail(string emailaddress, string activationKey, bool resend)
        {
            string subject = "Account Activation Information for Angel Island";

            if (resend) subject += " (resent)";

            string body = "\nThis email is intended to verify your email address.\n";
            body += "An activation key has been generated for your account.\n";
            body += "It is: " + activationKey;
            body += "\n\nTo complete the activation of your account, log on to Angel Island and use the [profile command again..";
            body += "Afterwards, you will be able to change your password using the [profile command.\n\n";
            body += "Regards,\n  The Angel Island Team\n\n";
            Emailer mail = new Emailer();

            return mail.SendEmail(emailaddress, subject, body, false);
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            int val = info.ButtonID;
            bool bResendGump = false;

            if (val <= 0)
                return;

            if (m_Account == null)
                return;

            try
            {
                TextRelay tr = info.GetTextEntry(0);
                string oldpassword = (tr == null ? null : tr.Text.Trim());

                if (oldpassword != null && oldpassword.Length != 0 && m_Account.CheckPassword(oldpassword))
                {
                    //valid password, proceed
                    switch (val)
                    {
                        case SET_EMAIL_BUTTON: //set email address
                            {
                                TextRelay tr1 = info.GetTextEntry(1);
                                TextRelay tr2 = info.GetTextEntry(2);

                                string newemail = (tr1 == null ? null : tr1.Text.Trim());
                                string confirmnewemail = (tr2 == null ? null : tr2.Text.Trim());

                                if (newemail == null || confirmnewemail == null ||
                                    newemail.Length == 0 || confirmnewemail.Length == 0)
                                {
                                    m_From.SendMessage(0x35, "Please enter email and verify email.");
                                    bResendGump = true;
                                }
                                else if (newemail != confirmnewemail)
                                {
                                    m_From.SendMessage(0x35, "Email addresses don't match");
                                    bResendGump = true;
                                }
                                else if (SMTP.SmtpDirect.CheckEmailAddy(newemail, false) == false)
                                {
                                    m_From.SendMessage(0x35, "Entered address isn't a valid email address.");
                                    bResendGump = true;
                                }
                                // 3/10/22, Adam: Commented out. Handled above in SMTP.SmtpDirect.CheckEmailAddy()
                                /*else if (newemail.IndexOf('@', 2, newemail.Length - 5) == -1)
                                {
                                    m_From.SendMessage(0x35, "Email address not of proper format");
                                    bResendGump = true;
                                }*/
                                else if (m_Account.AccountActivated && m_Account.EmailAddress != null && newemail.ToLower() == m_Account.EmailAddress.ToLower())
                                {
                                    m_From.SendMessage(0x35, "Email address matches current email address.");
                                    bResendGump = true;
                                }
                                else
                                {
                                    string newActivationKey = CreateActivationKey(8);

                                    //Send Email
                                    bool bSent = SendActivationEmail(newemail, newActivationKey, false);
                                    if (bSent)
                                    {
                                        string regSubject = "Email address change";
                                        string regBody = "Email address change made.\n";
                                        if (m_Account.AccountActivated == false)
                                        {
                                            regSubject = "Account Activation request";
                                            regBody = "Account Activation: Email address change made.\n";
                                        }
                                        regBody += "Info of from: \n";
                                        regBody += "\n";
                                        regBody += "Account: " + (m_Account == null ? "<unknown>" : m_Account.Username) + "\n";
                                        regBody += "Character: " + m_From.Name + "\n";
                                        regBody += "IP: " + sender.Address.ToString() + "\n";
                                        regBody += "Old Email: " + m_Account.EmailAddress + "\n";
                                        regBody += "New Email: " + newemail + "\n";
                                        regBody += "Activation Key: " + newActivationKey + "\n";
                                        regBody += "\n";

                                        Emailer mail = new Emailer();
                                        mail.SendEmail(Environment.GetEnvironmentVariable("AI.EMAIL.ACCOUNTING"), regSubject, regBody, false);

                                        if (m_Account.AccountActivated &&
                                            newemail.ToLower() != m_Account.EmailAddress.ToLower())
                                        {
                                            string subject2 = "Angel Island Account email changed";
                                            string body2 = "\nThis email is being sent to notify that someone has changed your email for your ";
                                            body2 += "Angel Island account from ";
                                            body2 += m_Account.EmailAddress;
                                            body2 += " to ";
                                            body2 += newemail;
                                            body2 += "\n\nIf you did not request this change, please contact an Angel Island ";
                                            body2 += "administrator as soon as possible.";
                                            body2 += "\n\nRegards,\n  The Angel Island Team\n\n";

                                            mail = new Emailer();
                                            mail.SendEmail(m_Account.EmailAddress, subject2, body2, false);
                                        }

                                        //success
                                        string[] tmp = new string[m_Account.EmailHistory.Length + 1];
                                        tmp[0] = newemail;
                                        m_Account.EmailHistory.CopyTo(tmp, 1);
                                        m_Account.EmailHistory = tmp;

                                        m_Account.EmailAddress = newemail;

                                        m_Account.ActivationKey = newActivationKey;
                                        m_Account.AccountActivated = false;
                                        m_Account.LastActivationResendTime = DateTime.UtcNow;


                                        //Notify user.
                                        m_From.SendMessage(0x35, "Email address accepted.");
                                        m_From.SendMessage(0x35, "An activation key has been sent to " + newemail);
                                        m_From.SendMessage(0x35, "After you get the key, use the [profile command again to complete activation.");
                                    }
                                    else
                                    {
                                        //failure
                                        //Notify user.
                                        m_From.SendMessage(0x35, "Error with email.");
                                        m_From.SendMessage(0x35, "Email address not changed.");
                                    }
                                }

                                break;
                            }
                        case RESEND_ACTIVATION:
                            {
                                if (m_Account.LastActivationResendTime + TimeSpan.FromHours(24.0) > DateTime.UtcNow)
                                {
                                    m_From.SendMessage(0x35, "You can only send an activation email once every 24 hours.");
                                }
                                else
                                {
                                    string email = m_Account.EmailAddress;
                                    string key = m_Account.ActivationKey;
                                    bool bSent = SendActivationEmail(email, key, true);

                                    if (bSent)
                                    {
                                        m_Account.LastActivationResendTime = DateTime.UtcNow;
                                        m_From.SendMessage(0x35, "The activation email has been resent to " + email);
                                    }
                                    else
                                    {
                                        m_From.SendMessage(0x35, "There was a problem resending the activation email to " + email);
                                        bResendGump = true;
                                    }
                                }
                                break;
                            }
                        case ENTER_ACTIVATION_KEY:
                            {
                                TextRelay tr1 = info.GetTextEntry(1);

                                string activationEntry = (tr1 == null ? null : tr1.Text.Trim());

                                if (activationEntry == m_Account.ActivationKey)
                                {
                                    m_Account.AccountActivated = true;
                                    m_Account.ActivationKey = "";

                                    m_From.SendMessage(0x35, "Your account is now activated.");
                                }
                                else
                                {
                                    m_From.SendMessage(0x35, "That is not the correct activation key.");
                                    m_From.SendMessage(0x35, "Make sure you have the correct capitalization.");
                                }

                                bResendGump = true;
                                break;
                            }
                        case RESET_ACTIVATION_ATTEMPT:
                            {
                                m_Account.AccountActivated = false;
                                m_Account.EmailAddress = "";
                                m_Account.ActivationKey = "";

                                m_From.SendMessage(0x35, "Your activation attempt has been reset.");
                                bResendGump = true;
                                break;
                            }
                        case SET_PASSWORD: //set password
                            {
                                TextRelay tr3 = info.GetTextEntry(3);
                                TextRelay tr4 = info.GetTextEntry(4);

                                string newpassword = (tr3 == null ? null : tr3.Text.Trim());
                                string confirmnewpassword = (tr4 == null ? null : tr4.Text.Trim());

                                if (newpassword == null || confirmnewpassword == null ||
                                    newpassword.Length == 0 || confirmnewpassword.Length == 0)
                                {
                                    m_From.SendMessage(0x35, "Please enter new password and confirm new password.");
                                    bResendGump = true;
                                }
                                else if (newpassword != confirmnewpassword)
                                {
                                    m_From.SendMessage(0x35, "Password input doesn't match");
                                    bResendGump = true;
                                }
                                else if (!m_Account.AccountActivated)
                                {
                                    m_From.SendMessage(0x35, "You may not change your password until you activate your account by providing a email address.");
                                }
                                else if (newpassword.Length < 6)
                                {
                                    m_From.SendMessage(0x35, "Your password must be at least 6 characters long.");
                                    bResendGump = true;
                                }
                                else if (newpassword.Length > 16)
                                {
                                    m_From.SendMessage(0x35, "Your password can be at most 16 characters long.");
                                    bResendGump = true;
                                }
                                else if (newpassword.IndexOfAny(new char[] { ' ', '@' }) != -1)
                                {
                                    m_From.SendMessage(0x35, "Your password cannot contain the following characters: <space>, @");
                                    bResendGump = true;
                                }
                                else
                                {
                                    m_Account.SetPassword(newpassword, true);
                                    m_From.SendMessage(0x35, "New password set.");

                                    if (!m_Account.AccountActivated && oldpassword == m_Account.ActivationKey)
                                    {
                                        m_Account.AccountActivated = true;
                                        Console.WriteLine("Account {0} just got activated by entering the ActivationKey.", m_Account.Username);
                                    }

                                    if (m_Account.AccountActivated)
                                    {
                                        //Send Email
                                        // 8/30/21, Adam: don't send users their password in clear text
                                        //  also warn them to contact AI staff if this password change is not recognized. 
                                        string subject = "Account Information for Angel Island";
                                        string body = "\nYour password has been changed in game.\n";
                                        //body += "It is now: " + newpassword;  
                                        body += "\n\nYou can change your password using the [profile command.\n\n";
                                        body += "\n\nIf you did not request this change, please contact an Angel Island ";
                                        body += "administrator as soon as possible.\n\n";
                                        body += "Regards,\n  The Angel Island Team\n\n";

                                        Emailer mail = new Emailer();
                                        if (mail.SendEmail(m_Account.EmailAddress, subject, body, false))
                                        {
                                            string regSubject = "Password change";
                                            string regBody = "Password change made.\n";
                                            regBody += "Info of from: \n";
                                            regBody += "\n";
                                            regBody += "Account: " + (m_Account == null ? "<unknown>" : m_Account.Username) + "\n";
                                            regBody += "Character: " + m_From.Name + "\n";
                                            regBody += "IP: " + sender.Address.ToString() + "\n";
                                            regBody += "Email: " + m_Account.EmailAddress + "\n";
                                            regBody += "New Password: " + newpassword + "\n";
                                            regBody += "\n";

                                            mail = new Emailer();
                                            mail.SendEmail(Environment.GetEnvironmentVariable("AI.EMAIL.ACCOUNTING"), regSubject, regBody, false);

                                            //Notify user
                                            m_From.SendMessage(0x35, "Your new password has been sent to " + m_Account.EmailAddress);
                                        }
                                        else
                                        {
                                            //Notify user
                                            m_From.SendMessage(0x35, "Error sending email to " + m_Account.EmailAddress + "!!");
                                        }
                                    }
                                }
                                break;
                            }
                        case SET_NOTIFICATION:
                            {
                                if (info.IsSwitched(5))
                                {
                                    m_Account.DoNotSendEmail = false;
                                    m_From.SendMessage("Your account is set to receive email about Angel Island.");
                                }
                                else
                                {
                                    m_Account.DoNotSendEmail = true;
                                    m_From.SendMessage("Your account is set to not receive email about Angel Island.");
                                }
                                bResendGump = true;
                                break;
                            }
                    }
                }
                else
                {
                    m_From.SendMessage(0x35, "Invalid password supplied");
                    bResendGump = true;
                }
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception);
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!");
                Console.WriteLine("Exception caught in ProfileGump.OnResponse() : " + exception.Message);
                Console.WriteLine(exception.StackTrace);
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!");
            }

            if (bResendGump)
            {
                m_From.SendGump(new ProfileGump(m_From));
            }
        }

        public static string CreateActivationKey(int length)
        {
            string charset = "ABCDEF123456789";
            string strPassword = "";
            System.Random r = new Random(DateTime.UtcNow.Millisecond);

            for (int i = 0; i < length; i++)
            {
                int x = r.Next(0, charset.Length);
                strPassword += charset[x];
            }

            return strPassword;
        }

        //helper functions
        public void AddTextField(int x, int y, int width, int height, int index)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
        }
        public void AddTextField(int x, int y, int width, int height, int index, string initialvalue)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, initialvalue);
        }
        public void AddButtonLabeled(int x, int y, int buttonID, string text)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID, GumpButtonType.Reply, 0);
            AddHtml(x + 35, y, 240, 20, Color(text, LabelColor32), false, false);
        }
    }
}