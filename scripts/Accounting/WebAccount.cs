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

#if THIS_IS_NOT_USED
namespace Server.Engines.AngelIsland.WebAccount
{
	/// <summary>
	/// Summary description for WebAccount.
	/// </summary>
	public class  WebAccount
	{
		public static bool Enabled = false;
		public static TimeSpan CheckInterval = TimeSpan.FromMinutes( 5.0 );
		public static string RequestDirectory = "account_requests";

		// To add new account emailing, fill in EmailServer:
		// Example:
		// private const string EmailServer = "mail.domain.com";
		// private static string EmailServer = "mail.tamke.net";
		// private static string EmailServer = "192.168.0.99";
		private static string EmailServer = "66.235.184.96";
		private static string FromEmailAddress = Environment.GetEnvironmentVariable("AI.NOREPLY.ADDRESS");


        private static int iMaxAccountsPerIP = 2;

		private static bool bIgnoreIPAddressClashes = false;

		
		//Note, insert {0} for the passord
		private static string EmailMessageFormat = 
		"\nSomeone requested an account for Angel Island.\n" +
		"The initial password for the account is: {0}\n" +
		"If there are any further problems please contact an Angel Island\n" +
		"administrator at http://www.game-master.net/cgi-bin/ultimatebb.cgi?category=5\n\n" +
		"Enjoy!\n\nThe Angel Island Team\n";

		///////////////////////////////////////////////////
		//       End of the configuration settings       //
		///////////////////////////////////////////////////

		private static bool m_bNewAccountsCreated = false;

		public static void Initialize()
		{
			//register commands
			Commands.Register( "Webreg", AccessLevel.Administrator, new CommandEventHandler( WebReg_OnCommand ) );

			if ( Enabled )
			{
				Timer.DelayCall( TimeSpan.FromSeconds( 20.0 ), CheckInterval, new TimerCallback( Begin ) );
			}
		}

		private WebAccount()
		{
		}

		[Usage( "WebReg on|stop|now|<checkinterval>" )]
		[Description( "Turns web registration handling on/off or checks it now." )]
		public static void WebReg_OnCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;

			if( e.Length == 0 )
			{
				from.SendMessage("must specify one of: on, stop, now, or an integer minute interval.");
			}
			else
			{
				string strcommand = e.GetString( 0 );

				if( strcommand.ToLower().Equals("stop") )
				{
					WebAccountLogger.Log("Received manual command to turn checking OFF.");
					Enabled = false;
				}
				else if( strcommand.ToLower().Equals("on") )
				{
					WebAccountLogger.Log("Received manual command to turn checking ON.");
					Enabled = true;
				}
				else if( strcommand.ToLower().Equals("now") )
				{
					WebAccountLogger.Log("Received manual command to check for new accounts.");
					Begin();
				}
				else
				{
					try
					{
						int iInterval = Int32.Parse(strcommand);
					}
					catch
					{
						from.SendMessage("Got error parsing integer minute interval");
					}
				}
			}
		}

	
		static bool bProcessing = false;

		public static void Begin()
		{
			if( Enabled )
			{
				if( !bProcessing )
				{
					bProcessing = true;
							
					try
					{
						ProcessForNewAccounts();
					}
					catch( Exception e )
					{
						LogHelper.LogException(e);
						WebAccountLogger.Log("Caught Exception in WebAccount!");
						WebAccountLogger.Log(e.Message);
						WebAccountLogger.Log(e.StackTrace);
					}
					bProcessing = false;
				}
				else
				{
					WebAccountLogger.Log("WebAccount: Your time between new account checks is probably too small!!");
				}
			}
		}

		private static void ProcessForNewAccounts()
		{
			DirectoryInfo dir = new DirectoryInfo(RequestDirectory);
			foreach (FileInfo f in dir.GetFiles("*.new")) 
			{
				String name = f.FullName;
				name = f.Name;
				long size = f.Length;
				DateTime creationTime = f.CreationTime;
				WebAccountLogger.Log(string.Format("{0,-12:N0} {1,-20:g} {2}", size, creationTime, name));

				if( size > 0 ) //skip file if it's of 0 length!
				{
					ProcessFile(f.FullName);

					try
					{
						string targetName = f.FullName + ".processed";
						if( !File.Exists(targetName) )
						{
							File.Move(f.FullName, targetName);
						}
						else
						{
							int a = 0;
							while(File.Exists(targetName))
							{
								targetName = f.FullName + ".processed" + a;
								a++;
							}
							File.Move(f.FullName, targetName);
						}
					}
					catch
					{
						WebAccountLogger.Log("Error moving file - delete file instead.");
						File.Delete(f.FullName);
					}

				}
			}

			if( m_bNewAccountsCreated )
			{
				Accounts.Save( new WorldSaveEventArgs(false) );
				m_bNewAccountsCreated = false;
			}
		}

		private static void ProcessFile(string filename)
		{
			//read in and parse file
			try
			{
				string acctname;
				string email;
				string ip;

				FileStream fs = new FileStream(filename, System.IO.FileMode.Open);
				XmlDocument config = new XmlDocument();
				config.Load(fs);
				fs.Close();
				XmlNode rootnode = config.SelectSingleNode("ROOT");
				XmlNode node = rootnode.SelectSingleNode("email");
				email = node.InnerText;
				node = rootnode.SelectSingleNode("username");
				acctname = node.InnerText;
				node = rootnode.SelectSingleNode("ip");
				ip = node.InnerText;

				WebAccountLogger.Log(string.Format("Going to try to add new account '{0}' from {1} with email {2}", acctname, ip, email));

				Account acct = Accounts.GetAccount( acctname );
				if ( acct == null ) //account doesn't exist - safe to add
				{
					//need to create a random password
					string password = CreateRandomPassword();//"xx1122";

					Account newAccount = CreateAccount(ip, acctname, password);
					if( newAccount != null )
					{
						m_bNewAccountsCreated = true;

						//newAccount.Comments.Add("Created from the web");
						newAccount.EmailAddress = email;
	
						WebAccountLogger.Log("added " + acctname + " successfully.");
						SendEmail(newAccount, password);

						string regSubject = "Account created from the web";
						string regBody = "Web Account Creation.\n";
						regBody += "Info: \n";
						regBody += "\n";
						regBody += "Account: " + newAccount.Username + "\n";
						regBody += "IP: " + ip + "\n";
						regBody += "Email: " + newAccount.EmailAddress + "\n";
						regBody += "New Password: " + password + "\n";
						regBody += "\n";

						Emailer mail = new Emailer();
						mail.SendEmail( Environment.GetEnvironmentVariable("AI.EMAIL.ACCOUNTING"), regSubject, regBody, false );

					}
					else
					{
						WebAccountLogger.Log("Creation of " + acctname + " failed.");
					}
				}
				else
				{
					WebAccountLogger.Log("Account '" + acctname + "' already Exists!  No new account added.");
				}

			}
			catch(Exception e)
			{
				LogHelper.LogException(e);
				WebAccountLogger.Log(e.Message);
				WebAccountLogger.Log(e.StackTrace);
			}
		}

		//NOTE: this is based on Accounting\AccountHandeler->CreateAccount()
		private static Account CreateAccount( string strIP, string un, string pw )
		{
			if ( un.Length == 0 || pw.Length == 0 )
				return null;

			bool isSafe = true;

			for ( int i = 0; isSafe && i < un.Length; ++i )
				isSafe = ( un[i] >= 0x20 && un[i] < 0x80 );

			for ( int i = 0; isSafe && i < pw.Length; ++i )
				isSafe = ( pw[i] >= 0x20 && pw[i] < 0x80 );

			if ( !isSafe )
				return null;

			IPAddress ip;
			try
			{
				//ip = ((IPEndPoint)state.Socket.RemoteEndPoint).Address;
				ip = IPAddress.Parse(strIP);
			}
			catch
			{
				return null;
			}
			
			int count = 0;

			foreach ( Account a in Accounts.Table.Values )
			{
				if ( a.LoginIPs.Length > 0 && a.LoginIPs[0].Equals( ip ) )
				{
					++count;

					if ( count >= iMaxAccountsPerIP && !bIgnoreIPAddressClashes )
					{
						WebAccountLogger.Log( string.Format("Login: {0}: Account '{1}' not created, ip already has account(s).", strIP, un ) );
						return null;
					}
				}
			}
			
			WebAccountLogger.Log( string.Format("Login: {0}: Creating new account '{1}'", strIP, un) );
			
			return Accounts.AddAccount( un, pw );
		}

		//This function stolen from Misc/CrashGuard.cs
		private static void SendEmail( Account account, string password )
		{
			WebAccountLogger.Log( string.Format("Attempting to send email to {0}/{1} at {2}...", account.Username, password, account.EmailAddress) );

			if( EmailServer == null || FromEmailAddress == null )
			{
				WebAccountLogger.Log("Not sending email because EmailServer or FromEmailAddress is not set.");
				return;
			}

			try
			{
				string path = WebAccount.RequestDirectory + "\\message.txt";
				if( File.Exists(path) )
				{
					using ( StreamReader sr = new StreamReader(path) )
					{
						EmailMessageFormat = sr.ReadToEnd();
						sr.Close();
					}
				}
				else
				{
					WebAccountLogger.Log("Error: " + path + " doesn't exist - sending default message.");
				}
			}
			catch
			{
				WebAccountLogger.Log("Failed to read message.txt for account email - using default message.");
			}

			try
			{
				MailMessage message = new MailMessage();

				message.Subject = "New Angel Island account request."; WebAccountLogger.Log( message.Subject );
				message.From = FromEmailAddress; // WebAccountLogger.Log( message.From );
				message.To = account.EmailAddress; // WebAccountLogger.Log( message.To );
				message.Body = string.Format(EmailMessageFormat, password); // WebAccountLogger.Log( message.Body );
				SmtpMail.SmtpServer = EmailServer; // WebAccountLogger.Log( SmtpMail.SmtpServer );
				SmtpMail.Send( message );

				WebAccountLogger.Log( "done" );
			}
			catch
			{
				WebAccountLogger.Log( "failed" );
			}
		}

		private static string CreateRandomPassword()
		{
			string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
			string strPassword = "";
			System.Random r = new Random(DateTime.UtcNow.Millisecond);

			for(int i=0; i<10; i++)
			{
				int x = r.Next(0, charset.Length);
				strPassword += charset[x];
			}

			return strPassword;
		}

		private class WebAccountLogger
		{
			private const string filename = "accountcreation.log";
			private static StreamWriter logwriter;
			
			public static void Initialize()
			{
				string logfilename = WebAccount.RequestDirectory + "\\" + filename;
				try
				{
					logwriter = new StreamWriter(logfilename , true );
					if( logwriter != null )
					{
						Log("Logging started " + DateTime.UtcNow);
					}
				}
				catch(Exception e)
				{
					LogHelper.LogException(e);
					System.Console.WriteLine("WebAccountLogger failed to open logfile " + logfilename + ".  Logging is disabled.");
					System.Console.WriteLine("Reason: " + e.Message);
					logwriter = null;
				}
			}

			private WebAccountLogger()
			{
			}

			public static bool Log(string message)
			{
				bool bSuccess = false;
				if( logwriter != null )
				{
					try
					{
						logwriter.WriteLine(message);
						logwriter.Flush();
						bSuccess = true;
					}
					catch(Exception e)
					{
						LogHelper.LogException(e);
						System.Console.WriteLine("WebAccountLogger failed to write to logfile.");
						System.Console.WriteLine("Reason: " + e.Message);
					}
				}
				return bSuccess;
			}
		}
	}
}
#endif