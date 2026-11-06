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

/*
 * NOTE:
 * This module represents the legacy, server-wide Test Center implementation.
 *
 * While still functional, it has been superseded by the region-based TestCenterRegion model,
 * which provides more granular control, better isolation, and safer containment of test activity.
 *
 * New development should target TestCenterRegion and related region/controller systems.
 * This module is retained temporarily for compatibility and will be removed in a future update.
 */

/* Scripts\Engines\TestCenter\TestCenter.Server.cs
 * CHANGELOG
 *	3/27/23, Yoar
 *	    Added bondpet
 *	6/30/21, Adam
 *		Add additional parsing an reverse lookup (Levenshtein.BestEnumMatch) to allow users to set
 *		set tasteid 100
 *		set taste identification 100
 *		set tasteidentification 100
 *	4/10/10, adam
 *		Add tamepet command
 *	5/12/08, Adam
 *		Switch from Convert.ToDouble() to double.TryParse()so that we could handle the error
 *	2/19/08, Adam
 *		Remove MaxAccountsPerIP setting for test center since it is now in the Core Command Console
 *  10/5/07, Adam
 *      Add  special ServerWars() function to turn on TEST CENTER for server wars on prod
 *  1/09/07 Taran Kain
 *      Added "petset"
 *	9/15/05, Adam
 *		set AccountHandler.MaxAccountsPerIP = 5 when test center is enabled
 *	9/14/05, Adam
 *		Cleaned up Test Center message that get written to the console
 */

using Server.Diagnostics;
using System;

namespace Server.Engines
{
    public class TestCenterServer
    {
        private static bool m_Enabled = false;
        private static bool m_Init = false;

        public static bool Enabled
        {
            get
            {
                if (m_Init == false)
                {
                    try { throw new ApplicationException("You cannot call TestCenter.Enabled from Configure()"); }
                    catch (Exception ex) { LogHelper.LogException(ex); }
                }
                return m_Enabled;
            }
        }

        public static void Configure()
        {
            if (Core.GMNTC_CFG == true)
            {
                m_Enabled = true;
            }
            else
                m_Enabled = false;

            m_Init = true;
        }

        public static void Initialize()
        {
            if (Enabled)
            {
                // Register shared handlers with server-wide permission check
                EventSink.Speech += new SpeechEventHandler(
                    args => TestCenterCommands.TCHelpGump.EventSink_Speech(args, m => TestCenterServer.Enabled));

                EventSink.Speech += new SpeechEventHandler(
                    args => TestCenterCommands.TCSetSkills.EventSink_Speech(args, m => TestCenterServer.Enabled));
            }
        }

        public static bool ServerWars()
        {
            if (Enabled == false)
            {
                if (Misc.AutoRestart.ServerWars == true)
                {
                    m_Enabled = true;
                    m_Init = true;

                    // Re-register handlers in case ServerWars activates late
                    EventSink.Speech += new SpeechEventHandler(
                        args => TestCenterCommands.TCHelpGump.EventSink_Speech(args, m => TestCenterServer.Enabled));

                    EventSink.Speech += new SpeechEventHandler(
                        args => TestCenterCommands.TCSetSkills.EventSink_Speech(args, m => TestCenterServer.Enabled));

                    return true;
                }
            }
            return false;
        }
    }
}