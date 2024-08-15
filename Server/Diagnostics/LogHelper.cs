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

/* Scripts/Commands/LogHelper.cs
 * ChangeLog
 *  7/2/2024, Adam
 *      Switch back to using UtcNow logs need to match actual events (House decay for instance.)
 *  11/24/22, Adam
 *      Switch to using GameTime instead of local time to make deciphering logs a bit easier.
 *  8/26/22, Adam
 *      Add support for caller supplies paths - paths other than '.\logs
 *      Add support for 'quiet' mode
 *  7/30/22, Adam
 *      Add LogBlockedConnection()
 *      This logs blocked connections due to firewall, IPLimits, and HardwareLimits
 *  12/2/21, Adam
 *      Create the ./logs directory if it doesn't already exist
 *  9/26/21, Adam (logger Finish())
 *      why are we removing spaces from the outout text to the Console?
        It messes up one line output. Remove those lines for now
 *	6/18/10, Adam
 *		o Added a cleanup procedure to the Cheater function to prevent players comments from growing out of control
 *		o Add the region ID to the output
 *	5/17/10, Adam
 *		o Add new Format() command that takes no additional text data
 *			Format(LogType logtype, object data)
 *		o Don't output time stamp on intermediate results created with Format()
 *	3/22/10, adam
 *		separate the formatting the logging so we can format our own strings before write
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	8/28/07, Adam
 *		Add new EventSink for ItemAdded via [add
 *		Dedesign LogHelper EventSink logic to be static and not instance based.
 *  3/28/07, Adam
 *      Add protections around Cheater()
 *  3/26/07, Adam
 *      Limit game console output to the first 25 items
 *	01/07/07 - Pix
 *		Added new LogException override: LogException(Exception ex, string additionalMessage)
 *	10/20/06, Adam
 *		Put back auto-watchlisting and comments from Cheater logging.
 *		Removed auto-watchlisting and comments from TrackIt logging.
 *	10/20/06, Pix
 *		Removed auto-watchlisting and comments from Cheater logging.
 *	10/17/06, Adam
 *		Add new Cheater() logging functions.
 *	9/9/06, Adam
 *		- Add Name and Serial for type Item display
 *		- normalized LogType.Item and LogType.ItemSerial
 *  01/09/06, Taran Kain
 *		Added m_Finished, Crashed/Shutdown handlers to make sure we write the log
 *  12/24/05, Kit
 *		Added ItemSerial log type that adds serial number to standered item log type.
 *	11/14/05, erlein
 *		Added extra function to clear in-memory log.
 *  10/18/05, erlein
 *		Added constructors with additional parameter to facilitate single line logging.
 *	03/28/05, erlein
 *		Added additional parameter for Log() to allow more cases
 *		where generic item and mobile logging can take place.
 *		Normalized format of common fields at start of each log line.
 *	03/26/05, erlein
 *		Added public interface to m_Count via Count so can add
 *		allowance for headers & footers.
 *	03/25/05, erlein
 *		Updated to log decimal serials instead of hex.
 *		Replaced root type name output with serial for items
 *		with mobile roots.
 *	03/23/05, erlein
 *		Initial creation
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Diagnostics
{
    public class LogHelper
    {
        private ArrayList m_LogFile;
        private string m_LogFilename;
        private int m_MaxOutput = 25;   // only display first 25 lines
        private int m_Count;
        private static ArrayList m_OpenLogs = new ArrayList();
        public static ArrayList OpenLogs { get { return m_OpenLogs; } }

        public List<string> Lines
        {
            get { return m_LogFile.Cast<string>().ToList(); }
            set { m_LogFile = new ArrayList(value); }
        }
        public bool Contains(string text, bool ignorecase = true)
        {
            if (ignorecase)
                return Lines.Contains(text, StringComparer.OrdinalIgnoreCase);

            return Lines.Contains(text);
        }
        public int Count
        {
            get
            {
                return m_Count;
            }
            set
            {
                m_Count = value;
            }
        }

        private bool m_Overwrite;
        private bool m_SingleLine;
        private bool m_Quiet;
        private DateTime m_StartTime;
        private bool m_Finished;

        private Mobile m_Person;


        // Construct with : LogHelper(string filename (, Mobile mobile ) (, bool overwrite) )

        // Default append, no mobile constructor
        public LogHelper(string filename)
        {
            m_Overwrite = false;
            m_LogFilename = filename;
            m_SingleLine = false;

            Start();
        }

        // Mob spec. constructor
        public LogHelper(string filename, Mobile from)
        {
            m_Overwrite = false;
            m_Person = from;
            m_LogFilename = filename;
            m_SingleLine = false;

            Start();
        }

        // Overwrite spec. constructor
        public LogHelper(string filename, bool overwrite)
        {
            m_Overwrite = overwrite;
            m_LogFilename = filename;
            m_SingleLine = false;

            Start();
        }

        // Overwrite and singleline constructor
        public LogHelper(string filename, bool overwrite, bool sline, bool quiet = false)
        {
            m_Overwrite = overwrite;
            m_LogFilename = filename;
            m_SingleLine = sline;
            m_Quiet = quiet;
            Start();
        }

        // Overwrite + mobile spec. constructor
        public LogHelper(string filename, Mobile from, bool overwrite)
        {
            m_Overwrite = overwrite;
            m_Person = from;
            m_LogFilename = filename;
            m_SingleLine = false;

            Start();
        }

        // Overwrite, mobile spec. and singleline constructor
        public LogHelper(string filename, Mobile from, bool overwrite, bool sline)
        {
            m_Overwrite = overwrite;
            m_Person = from;
            m_LogFilename = filename;
            m_SingleLine = sline;

            Start();
        }
        public static string DataFolder(string filename)
        {
            return Path.Combine(Core.DataDirectory, Core.Server, filename);
        }
        public static void LogBlockedConnection(string text)
        {
            try
            {
                LogHelper Logger = new LogHelper("BlockedConnection.log", false);
                Logger.Log(LogType.Text, text);
                Logger.Finish();
                Utility.ConsoleWriteLine(text, ConsoleColor.Red);
            }
            catch
            {
                Utility.ConsoleWriteLine(text, ConsoleColor.Red);
            }
        }
        public static void LogLogicError(string text)
        {
            try
            {
                LogHelper Logger = new LogHelper("LogicError.log", false);
                Logger.Log(LogType.Text, text);
                Logger.Finish();
                Utility.ConsoleWriteLine(text, ConsoleColor.Red);
            }
            catch
            {
                Utility.ConsoleWriteLine(text, ConsoleColor.Red);
            }
        }
        private static int m_LogExceptionCount = 0;
        public static void LogException(Exception ex)
        {
            if (m_LogExceptionCount++ > 100)
                return;

            try
            {
                LogHelper Logger = new LogHelper("Exception.log", false);
                Logger.Log(LogType.Text, Core.Server);
                string text = string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace);
                Logger.Log(LogType.Text, text);
                Logger.Finish();
                Utility.ConsoleWriteLine(text, ConsoleColor.Red);
            }
            catch
            {
                // do nothing here as we do not want to enter a "cycle of doom!"
                //  Basically, we do not want the caller to catch an exception here, and call
                //  LogException() again, where it throws another exception, and so forth
            }
        }

        public static void LogException(Exception ex, string additionalMessage)
        {
            try
            {
                LogHelper Logger = new LogHelper("Exception.log", false);
                string text = string.Format("{0}\r\n{1}\r\n{2}", additionalMessage, ex.Message, ex.StackTrace);
                Logger.Log(LogType.Text, text);
                Logger.Finish();
                Console.WriteLine(text);
            }
            catch
            {
                // do nothing here as we do not want to enter a "cycle of doom!"
                //  Basically, we do not want the caller to catch an exception here, and call
                //  LogException() again, where it throws another exception, and so forth
            }
        }

        /*public static void Cheater(Mobile from, string text)
		{
			try
			{
				Cheater(from, text, false);
			}
			catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
		}*/

        /*public static void Cheater(Mobile from, string text, bool accomplice)
		{
			if (from is PlayerMobile == false)
				return;

			// log what's going on
			TrackIt(from, text, accomplice);

			//Add to watchlist
			(from as PlayerMobile).WatchList = true;

			//Add comment to account
			Account a = (from as PlayerMobile).Account as Account;
			if (a != null)
			{
				// We may add lots of these, so only keep the last 5 AUDIT records
				ArrayList delete = new ArrayList();
				for (int ix = 0; ix < a.Comments.Count; ix++)
				{
					if (a.Comments[ix] is AccountComment)
					{
						AccountComment temp = a.Comments[ix] as AccountComment;
						// old audit key "System", new audit key "AUDIT"
						if (temp.AddedBy.StartsWith("System", false, null) || temp.AddedBy.StartsWith("AUDIT", false, null))
							delete.Add(a.Comments[ix]);
					}
				}

				// delete all messages but the last 5
				if (delete.Count > 5)
				{
					int limit = delete.Count - 5;
					for (int jx = 0; jx < limit; jx++)
						a.Comments.Remove(delete[jx]);
				}

				// okay, now add a fresh message
				a.Comments.Add(new AccountComment("AUDIT", text));
			}
		}*/

        /*public static void TrackIt(Mobile from, string text, bool accomplice)
		{
			LogHelper Logger = new LogHelper("Cheater.log", false);
			Logger.Log(LogType.Mobile, from, text);
			if (accomplice == true)
			{
				IPooledEnumerable eable = from.GetMobilesInRange(24);
				foreach (Mobile m in eable)
				{
					if (m is PlayerMobile && m != from)
						Logger.Log(LogType.Mobile, m, "Possible accomplice.");
				}
				eable.Free();
			}
			Logger.Finish();
		}*/

        // Clear in memory log
        public void Clear()
        {
            m_LogFile.Clear();
        }

        public static void Initialize()
        {
            EventSink.AddItem += new AddItemEventHandler(EventSink_AddItem);
            EventSink.Crashed += new CrashedEventHandler(EventSink_Crashed); ;
            EventSink.Shutdown += new ShutdownEventHandler(EventSink_Shutdown);
            EventSink.LogException += new LogExceptionEventHandler(EventSink_LogException);
        }

        // Record start time and init counter + list
        private void Start()
        {
            m_StartTime = DateTime.UtcNow;
            m_Count = 0;
            m_Finished = false;
            m_LogFile = new ArrayList();

            if (!m_SingleLine && !m_Quiet)
                m_LogFile.Add(string.Format("Log start : {0}", m_StartTime));

            m_OpenLogs.Add(this);
        }

        // Log all the data and close the file
        public void Finish()
        {
            if (!m_Finished)
            {
                m_Finished = true;
                TimeSpan ts = DateTime.UtcNow - m_StartTime;

                if (!m_SingleLine && !m_Quiet)
                    m_LogFile.Add(string.Format("Completed in {0} seconds, {1} entr{2} logged", ts.TotalSeconds, m_Count, m_Count == 1 ? "y" : "ies"));

                // Report
                string sFilename = string.Empty;

                string path = Path.GetDirectoryName(m_LogFilename);
                if (string.IsNullOrEmpty(path))
                {   // default to "logs" directory
                    sFilename = Path.Combine(Core.LogsDirectory, m_LogFilename);
                }
                else
                {   // use the supplied path
                    sFilename = m_LogFilename;
                }

                path = Path.GetDirectoryName(sFilename);

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                StreamWriter LogFile = null;

                try
                {
                    LogFile = new StreamWriter(sFilename, !m_Overwrite);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to open logfile '{0}' for writing : {1}", sFilename, e);
                }

                // Loop through the list stored and log
                for (int i = 0; i < m_LogFile.Count; i++)
                {

                    if (LogFile != null)
                        LogFile.WriteLine(m_LogFile[i]);

                    // Send message to the player too
                    if (m_Person is Mobile) // was PlayerMobile
                    {
                        m_MaxOutput--;

                        if (m_MaxOutput > 0)
                        {   // 9/26/21, Adam, why are we removing spaces from the outout text?
                            //  It messes up one line output
                            /*if (i + 1 < m_LogFile.Count && i != 0)
                                m_Person.SendMessage(((string)m_LogFile[i]).Replace(" ", ""));
                            else*/
                            m_Person.SendMessage((string)m_LogFile[i]);
                        }
                        else if (m_MaxOutput == 0)
                        {
                            m_Person.SendMessage("Skipping remainder of output. See log file.");
                            m_Person.SendMessage("{0} item{1} logged.", m_LogFile.Count, (m_LogFile.Count != 1) ? "s" : "");
                        }
                    }
                }

                // If successfully opened a stream just now, close it off!

                if (LogFile != null)
                    LogFile.Close();

                if (m_OpenLogs.Contains(this))
                    m_OpenLogs.Remove(this);
            }
        }

        // Add data to list to be logged : Log( (LogType ,) object (, additional) )

        // Default to mixed type
        public void Log(object data)
        {
            this.Log(LogType.Mixed, data, null);
        }

        // Default to no additional
        public void Log(LogType logtype, object data)
        {
            this.Log(logtype, data, null);
        }

        // Specify LogType
        public void Log(LogType logtype, object data, string additional)
        {
            string LogLine = Format(logtype, data, additional);

            // If this is a "single line" loghelper instance, we need to replace all newline characters
            if (m_SingleLine && !m_Quiet)
            {
                LogLine = LogLine.Replace("\r\n", " || ");
                LogLine = LogLine.Replace("\r", " || ");
                LogLine = LogLine.Replace("\n", " || ");
                LogLine = m_StartTime.ToString() + ": " + LogLine;
            }

            m_LogFile.Add(LogLine);
            m_Count++;
        }

        public string Format(LogType logtype, object data)
        {
            return Format(logtype, data, null);
        }

        public string Format(LogType logtype, object data, string additional)
        {
            string LogLine = "";
            additional = (additional == null) ? "" : additional;

            if (logtype == LogType.Mixed)
            {
                // Work out most appropriate in absence of specific

                if (data is Mobile)
                    logtype = LogType.Mobile;
                else if (data is Item)
                    logtype = LogType.Item;
                else
                    logtype = LogType.Text;

            }

            switch (logtype)
            {

                case LogType.Mobile:

                    Mobile mob = data as Mobile;
                    if (mob != null)
                    {
                        LogLine = string.Format("{0}:Loc({1},{2},{3}):{4}:Mob({5})({6}):{7}:{8}:{9}",
                                    mob.GetType().Name,
                                    mob.Location.X, mob.Location.Y, mob.Location.Z,
                                    mob.Map,
                                    mob.Name,
                                    mob.Serial,
                                    string.Format("{0}[{1}]", mob.Region.Name != null && mob.Region.Name.Length > 0 ? mob.Region.Name : "Unnamed region", mob.Region.UId),
                                    mob.Account,
                                    additional);
                    }
                    else
                        LogLine = "Mobile (null)" + additional;
                    break;

                case LogType.ItemSerial:
                case LogType.Item:

                    Item item = data as Item;
                    if (item != null)
                    {
                        object root = item.RootParent;
                        //if (root is Mobile)
                        // Item loc, map, root type, root name
                        LogLine = string.Format("{0}:Loc{1}:{2}:{3}({4}):Parent({5})({6}):{7}",
                            item.GetType().Name,
                            item.GetWorldLocation(),
                            item.Map,
                            item.Name,
                            item.Serial,
                            root is Mobile mobile1 ? mobile1.SafeName : root is Item item1 ? item1.SafeName : null,
                            root is Mobile mobile2 ? mobile2.Serial : root is Item item2 ? item2.Serial : null,
                            additional
                        );
                        /*else
                            // Item loc, map
                            LogLine = string.Format("{0}:Loc{1}:{2}:{3}({4}):{5}",
                                item.GetType().Name,
                                new Point3D(item.GetWorldLocation()),
                                item.Map,
                                item.Name,
                                item.Serial,
                                additional
                            );*/
                    }
                    else
                        LogLine = "Item (null)" + additional;
                    break;

                case LogType.Text:

                    LogLine = data.ToString();
                    break;
            }

            return LogLine;
        }

        private static void EventSink_Crashed(CrashedEventArgs e)
        {
            for (int ix = 0; ix < LogHelper.OpenLogs.Count; ix++)
            {
                LogHelper lh = LogHelper.OpenLogs[ix] as LogHelper;
                if (lh != null)
                    lh.Finish();
            }
        }

        private static void EventSink_Shutdown(ShutdownEventArgs e)
        {
            for (int ix = 0; ix < LogHelper.OpenLogs.Count; ix++)
            {
                LogHelper lh = LogHelper.OpenLogs[ix] as LogHelper;
                if (lh != null)
                    lh.Finish();
            }
        }

        private static void EventSink_AddItem(AddItemEventArgs e)
        {
            LogHelper lh = new LogHelper("AddItem.log", false, true);
            lh.Log(LogType.Mobile, e.from, string.Format("Used [Add Item to create ItemID:{0}, Serial:{1}", e.item.ItemID.ToString(), e.item.Serial.ToString()));
            lh.Finish();
        }

        private static void EventSink_LogException(LogExceptionEventArgs e)
        {   // exception passed up from the server core
            try { LogException(e.Exception); }
            catch { Console.WriteLine("Nested exception while processing: {0}", e.Exception.Message); } // do not call LogException
        }

        public static void BroadcastMessage(AccessLevel ac, int hue, string message)
        {
            foreach (Server.Network.NetState state in Server.Network.NetState.Instances)
            {
                Mobile m = state.Mobile;

                if (m != null && m.AccessLevel >= ac)
                    m.SendMessage(hue, message);
            }
        }
    }

    public enum LogType
    {
        Mobile,
        Item,
        Mixed,
        Text,
        ItemSerial
    }
}