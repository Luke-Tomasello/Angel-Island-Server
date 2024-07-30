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
 *	7/5/21, Adam
 *		pushed down to the server core, all except the 'cheater' functions which need knowledge of 
 *		the PlayerMobile and the Account info
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

using Server.Accounting;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Commands
{
    public class RecordCheater
    {
        public static void Cheater(Mobile from, string text)
        {
            try
            {
                Cheater(from, text, false);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void Cheater(Mobile from, string text, bool accomplice)
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
        }
        public static void TrackIt(Mobile from, string text, bool accomplice)
        {

            Server.Diagnostics.LogHelper Logger = new Server.Diagnostics.LogHelper("Cheater.log", false);
            Logger.Log(Server.Diagnostics.LogType.Mobile, from, text);
            if (accomplice == true)
            {
                IPooledEnumerable eable = from.GetMobilesInRange(24);
                foreach (Mobile m in eable)
                {
                    if (m is PlayerMobile && m != from)
                        Logger.Log(Server.Diagnostics.LogType.Mobile, m, "Possible accomplice.");
                }
                eable.Free();
            }
            Logger.Finish();
        }
    }
}