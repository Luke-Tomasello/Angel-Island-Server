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

/* Scripts/Misc/WelcomeTimer.cs
 *  ChangeLog	
 *  8/5/22, Adam
 *      Add welcome message for Login server (admin access only)
 *  7/3/22, Adam 
 *      Update version string to show .NET 6
 *	12/29/21, Adam (versioning)
 *      Update the way version information is collected. Uses BuildInfo now
 *	3/3/11, Adam
 *		Parameterize the welcome message based upon shard conf parameters (Event, Test Center, etc.)
 *	2/9/11, Adam
 *		Promote Mortalis to a real shard and give it a better welcome message
 *	02/3/11, Adam
 *		Add better Siege Wipe warning
 *		Add a scary message for our Island Siege secret shard AKA UO Mortalis
 *	02/23/10, Adam
 *		remove faction messages, added "GMN service since" notice
 *	06/29/09, plasma
 *		Added factions message
 *	4/25/08, Adam
 *		Change from Guild.Peaceful to guild.NewPlayerGuild when deciding auto adding
 *	1/20/08, Adam
 *		more sanity checking
 *	1/17/08, Adam
 *		Add sanity checking in OnTick()
 *	1/4/08, Adam
 *		- unconditional add to New guild (if their IP is unknown to us)
 *		- change New member titles from Day Month to Month Day
 *  12/12/07, Adam
 *      Cleanup code in Accounting.Accounts.IPLookup() test
 *  12/9/07, Adam
 *      Added NewPlayerGuild feature bit.
 *  12/6/07, Adam
 *      - Call new gump to auto add players to the New Guild (a peaceful guild)
 *		- Add call to Accounting.Accounts.IPLookup() to see if this a known IP address
 *      - log all players added to the NEW guild to PlayerAddedToNEWGuild.log
 *      - add exception handling
 *	8/26/07 - Pix
 *		Added WelcomeGump if NewPlayerStartingArea feature bit is enabled.
 *  09/14/05 Taran Kain
 *		Add TC message back in with a check for functionality, change it around to make it accurate with what we put in the bank.
 *	6/19/04, Adam
 *		1. Comment out TestCenter message
 *		2. add nowmal welcome message
 */

using Server.Diagnostics;			// log helper
using Server.Gumps;
using Server.Mobiles;
using System;

namespace Server.Misc
{
    /// <summary>
    /// This timer spouts some welcome messages to a user at a set interval. It is used on character creation.
    /// </summary>
    public class WelcomeTimer : Timer
    {
        private Mobile m_Mobile;
        private int m_State, m_Count;
        //private static Guildstone m_Stone;
        private static string[] m_Messages;

        public static void Initialize()
        {
            System.Reflection.Assembly m_Assembly = System.Reflection.Assembly.GetEntryAssembly();
            Version ver = m_Assembly.GetName().Version;
            string version = string.Format("{0}.{1} [Build {2}] [.NET 6, 64 bit]", Utility.BuildMajor(), Utility.BuildMinor(), Utility.BuildBuild());

            if (TestCenter.Enabled && !Core.UOBETA_CFG)
            {
                m_Messages = new string[]
                {	// Test Center
					String.Format("Welcome to {0} Test Center{1}",
                        Core.Server,
                        Core.UOEV_CFG ? " Event Shard." : "."),
                    string.Format("Angel Island core version {0}, launched March 2004.", version),
                    "You are able to customize your character's stats and skills at anytime to anything you wish.  To see the commands to do this just say 'help'.",
                    "You will find bank checks worth nearly 1.5 million gold in your bank!",
                    "A spellbook and a bag of reagents has been placed into your bank box.",
                    "Various tools have been placed into your bank.",
                    "Various raw materials like ingots, logs, feathers, hides, bottles, etc, have been placed into your bank.",
                    "Nine unmarked recall runes have been placed into your bank box.",
                    "A keg of each potion has been placed into your bank box.",
                    "Two of each level of treasure map have been placed in your bank box.",
                    "You will find 60,000 gold pieces deposited into your bank box.  Spend it as you see fit and enjoy yourself!",
                };
            }
            else
            {
                if (TestCenter.Enabled && Core.UOBETA_CFG)
                    m_Messages = new string[]
                    {	// Angel Island
						String.Format("Welcome to Angel Island {0}{1}", Core.UOBETA_CFG ? "BETA" : "Test Center", Core.UOEV_CFG ? " Event Shard." : "."),
                        string.Format("Angel Island core version {0}, launched March 2004.", version),
                        "You can set your own skill/stats in BETA: 'set [name] [value]'. Example: 'set str 125'",
                        "Please see www.game-master.net for game-play information."
                    };
                else if (Core.RuleSets.AngelIslandRules())
                    m_Messages = new string[]
                    {	// Angel Island
						String.Format("Welcome to Angel Island{0}", Core.UOEV_CFG ? " Event Shard." : "."),
                        string.Format("Angel Island core version {0}, launched March 2004.", version),
                        "Please see www.game-master.net for game-play information."
                    };
                else if (Core.RuleSets.SiegeRules())
                    m_Messages = new string[]
                    {	// Siege Perilous
						String.Format("Welcome to Siege Perilous{0}", Core.UOEV_CFG ? " Event Shard." : "."),
                        Core.ReleasePhase < ReleasePhase.Production ? "Version: " +
                        Utility.GetCustomEnumNames(typeof(ReleasePhase))[(int)Core.ReleasePhase] : "",
                        Core.ReleasePhase < ReleasePhase.Production ? "This Server will be wiped." : "",
                        string.Format("Angel Island core version {0}, launched March 2004.", version),
                        "Please see www.game-master.net for game-play information."
                    };
                else if (Core.RuleSets.RenaissanceRules())
                    m_Messages = new string[]
                    {	// Renaissance
						String.Format("Welcome to Renaissance{0}", Core.UOEV_CFG ? " Event Shard." : "."),
                        Core.ReleasePhase < ReleasePhase.Production ? "Version: " +
                        Utility.GetCustomEnumNames(typeof(ReleasePhase))[(int)Core.ReleasePhase] : "",
                        Core.ReleasePhase < ReleasePhase.Production ? "This Server will be wiped." : "",
                        string.Format("Angel Island core version {0}, launched March 2004.", version),
                        "Please see www.game-master.net for game-play information."
                    };
                else if (Core.RuleSets.MortalisRules())
                    m_Messages = new string[]
                    {	// Mortalis
						String.Format("Welcome to Mortalis{0}", Core.UOEV_CFG ? " Event Shard." : "."),
                        Core.ReleasePhase < ReleasePhase.Production ? "Version: " +
                        Utility.GetCustomEnumNames(typeof(ReleasePhase))[(int)Core.ReleasePhase] : "",
                        Core.ReleasePhase < ReleasePhase.Production ? "This Server will be wiped." : "",
                        string.Format("Angel Island core version {0}, launched March 2004.", version),
                        "You are mortal here, and if you die you will need to reenter this world anew.",
                        "There is no resurrection, there are no second chances.",
                        "You have been warned.",
                        "Life is only as sweet as death is painful."
                    };
                else if (Core.RuleSets.LoginServerRules())
                    m_Messages = new string[]
                    {	// Login Server
						String.Format("Welcome to Login Server{0}", Core.UOEV_CFG ? " Event Shard." : "."),
                        string.Format("Angel Island core version {0}, launched March 2004.", version)
                    };
                else
                    m_Messages = new string[0];
            }
        }

        public WelcomeTimer(Mobile m)
            : this(m, m_Messages.Length)
        {
        }

        public WelcomeTimer(Mobile m, int count)
            : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
        {
            m_Mobile = m;
            m_Count = count;
            m_State = 0;
        }
        protected bool IsInmate()
        {   // illegally created accounts are made inmates of Angel Island penitentiary.
            //  They're pretty much dead, or there forever. Don't bother giving them all the welcome blather.
            //  They were bad, and it is now time to pay the piper.
            return m_Mobile is PlayerMobile pm && pm.PrisonInmate;
        }
        protected override void OnTick()
        {
            try
            {
                // sanity
                if (m_Mobile == null || m_Mobile.Deleted == true || m_Mobile.NetState == null || IsInmate() || Running == false)
                {
                    Stop();
                    return;
                }

                // Let new players join the NEW guild (unless they are inmates)
                if (m_State == 0)
                    NewPlayerGuild.OnWelcome(m_Mobile);

                // print welcome messages
                if (m_State < m_Count)
                    m_Mobile.SendMessage(0x35, m_Messages[m_State]);

                // stop the timer
                if (m_State == m_Count)
                    Stop();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {   // make sure we keep marching forward no matter what
                m_State++;
            }
        }
    }
}