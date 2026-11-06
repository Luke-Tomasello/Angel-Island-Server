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

/* Scripts/Misc/WelcomeTimer.cs
 *  ChangeLog
 *  12/9/2025, Adam
 *      Complete refactor.
 *      Add APIs so that messages can be replaced, or appended
 *      For example, while filling a bank box on Test center, you can inform the user here that all items won't fit.
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

using Server.Diagnostics;
using Server.Engines;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using static Server.Misc.SeasonsHelper;

namespace Server.Misc
{
    /// <summary>
    /// This timer spouts some welcome messages to a user at a set interval. It is used on character creation and login.
    /// Now supports replacing a shard-specific middle block and appending extra lines (global or per-player).
    /// </summary>
    public partial class WelcomeTimer : Timer
    {
        // ----- STATIC CUSTOMIZATION SURFACE ---------------------------------

        private static readonly object s_Sync = new object();

        // Global customizations
        private static List<WelcomeLine> s_GlobalAppends = new List<WelcomeLine>();
        private static List<WelcomeLine>? s_ReplaceOverride = null;           // replaces the default replaceable block

        // Per-player one-shot customizations (consumed on first welcome)
        private static readonly Dictionary<Serial, List<WelcomeLine>> s_PendingPerPlayer = new();
        private static readonly Dictionary<Serial, List<WelcomeLine>> s_ReplacePerPlayer = new();

        /// <summary>
        /// A single welcome line with optional hue.
        /// Default hue is 0x3B2 (standard SendMessage color).
        /// </summary>
        public readonly struct WelcomeLine
        {
            public readonly string Text;
            public readonly int Hue;

            public WelcomeLine(string text, int hue = 0x3B2)
            {
                Text = text ?? string.Empty;
                Hue = hue;
            }
        }

        // ----- STATIC SHARD TEMPLATES ---------------------------------------

        private static string[] s_Header = Array.Empty<string>();
        private static string[] s_DefaultReplaceable = Array.Empty<string>();
        private static string[] s_DefaultAppend = Array.Empty<string>();

        /// <summary>
        /// Append lines globally for all players. Optional hue (defaults to normal color).
        /// </summary>
        public static void AppendLines(IEnumerable<string> lines, int hue = 0x3B2)
        {
            if (lines == null) return;
            lock (s_Sync)
            {
                foreach (var s in lines)
                {
                    if (!string.IsNullOrEmpty(s))
                        s_GlobalAppends.Add(new WelcomeLine(s, hue));
                }
            }
        }

        /// <summary>
        /// Append lines for a specific player only. Optional hue (defaults to normal color).
        /// Consumed the first time their welcome message runs.
        /// </summary>
        public static void AppendFor(Mobile m, IEnumerable<string> lines, int hue = 0x3B2)
        {
            if (m == null || lines == null) return;

            lock (s_Sync)
            {
                if (!s_PendingPerPlayer.TryGetValue(m.Serial, out var list))
                {
                    list = new List<WelcomeLine>();
                    s_PendingPerPlayer[m.Serial] = list;
                }

                foreach (var s in lines)
                {
                    if (!string.IsNullOrEmpty(s))
                        list.Add(new WelcomeLine(s, hue));
                }
            }
        }

        /// <summary>
        /// Replace the replaceable block for a specific player (one-time). Optional hue.
        /// </summary>
        public static void ReplaceFor(Mobile m, IEnumerable<string> lines, int hue = 0x3B2)
        {
            if (m == null) return;

            lock (s_Sync)
            {
                if (lines == null)
                {
                    s_ReplacePerPlayer.Remove(m.Serial);
                    return;
                }

                var list = new List<WelcomeLine>();
                foreach (var s in lines)
                {
                    if (!string.IsNullOrEmpty(s))
                        list.Add(new WelcomeLine(s, hue));
                }

                if (list.Count == 0)
                    s_ReplacePerPlayer.Remove(m.Serial);
                else
                    s_ReplacePerPlayer[m.Serial] = list;
            }
        }

        public static void ClearCustomizations()
        {
            lock (s_Sync)
            {
                s_ReplaceOverride = null;
                s_GlobalAppends.Clear();
                s_PendingPerPlayer.Clear();
                s_ReplacePerPlayer.Clear();
            }
        }

        public static void ClearFor(Mobile m)
        {
            if (m == null) return;
            lock (s_Sync)
            {
                s_PendingPerPlayer.Remove(m.Serial);
                s_ReplacePerPlayer.Remove(m.Serial);
            }
        }

        // ----- INSTANCE: PER-PLAYER RUNTIME ---------------------------------

        public enum GreetingType
        {
            NewPlayer,     // Long, staggered welcome for brand new characters
            Returning      // Short, fast greeting for returning players
        }

        private readonly Mobile m_Mobile;
        private readonly WelcomeLine[] m_Lines;
        private readonly TimeSpan m_Delay;
        private int m_State;
        private static readonly Dictionary<ServerID, WelcomeLine[]> s_ReturningTemplates = new();

        private WelcomeTimer(Mobile m, WelcomeLine[] lines, TimeSpan delay)
            : base(delay, delay)
        {
            m_Mobile = m;
            m_Lines = lines ?? Array.Empty<WelcomeLine>();
            m_Delay = delay;
            m_State = 0;
        }

        /// <summary>
        /// Main entry point for sending greetings.
        /// Handles both new players and returning players with appropriate timing.
        /// </summary>
        public static void SendGreeting(Mobile m, GreetingType type)
        {
            if (m == null || m.Deleted)
                return;

            // Right? they're Incarcerated. No other blabber
            // probly havn't been moved yet, we will get them on the timer tick as well)
            if (Utility.IsIncarcerated(m))
                return;

            WelcomeLine[] lines = type == GreetingType.NewPlayer
                ? ComposeNewPlayerLines(m)
                : ComposeReturningLines(m);

            // New players need a small initial delay because NetState isn't ready immediately during character creation
            TimeSpan delay = type == GreetingType.NewPlayer
                ? TimeSpan.FromSeconds(1.2)      // Give NetState time to attach
                : TimeSpan.FromSeconds(0.9);     // Fast for returning players

            new WelcomeTimer(m, lines, delay).Start();
        }
        protected override void OnTick()
        {
            try
            {
                if (m_Mobile == null || m_Mobile.Deleted || !Running)
                {
                    Stop();
                    return;
                }

                // Only check NetState after the first tick (important for new character creation)
                if (m_State > 0 && m_Mobile.NetState == null)
                {
                    Stop();
                    return;
                }

                // Right? they're Incarcerated. No other blabber
                if (Utility.IsIncarcerated(m_Mobile))
                {
                    Stop();
                    return;
                }

                if (m_State < m_Lines.Length)
                {
                    var line = m_Lines[m_State];
                    if (!string.IsNullOrEmpty(line.Text))
                    {
                        m_Mobile.SendMessage(line.Hue, line.Text);
                    }
                }

                // Stop when we've sent all lines
                if (m_State + 1 >= m_Lines.Length)
                    Stop();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                m_State++;
            }
        }

        // Build the final message list for this player
        private static WelcomeLine[] ComposeNewPlayerLines(Mobile m)
        {
            var list = new List<WelcomeLine>(32);

            lock (s_Sync)
            {
                // 1. Header (always normal color)
                foreach (var line in s_Header)
                    if (!string.IsNullOrWhiteSpace(line))
                        list.Add(new WelcomeLine(line));

                // 2. Replaceable block (per-player > global > default)
                List<WelcomeLine>? replaceBlock = null;

                if (m != null && s_ReplacePerPlayer.TryGetValue(m.Serial, out var perReplace) && perReplace.Count > 0)
                {
                    replaceBlock = perReplace;
                    s_ReplacePerPlayer.Remove(m.Serial);
                }
                else if (s_ReplaceOverride != null && s_ReplaceOverride.Count > 0)
                {
                    replaceBlock = s_ReplaceOverride;
                }
                else if (s_DefaultReplaceable != null && s_DefaultReplaceable.Length > 0)
                {
                    replaceBlock = new List<WelcomeLine>();
                    foreach (var s in s_DefaultReplaceable)
                        if (!string.IsNullOrWhiteSpace(s))
                            replaceBlock.Add(new WelcomeLine(s));
                }

                if (replaceBlock != null)
                    list.AddRange(replaceBlock);

                // 3. Default append (usually empty)
                if (s_DefaultAppend != null)
                {
                    foreach (var s in s_DefaultAppend)
                        if (!string.IsNullOrWhiteSpace(s))
                            list.Add(new WelcomeLine(s));
                }

                // 4. Global appends
                list.AddRange(s_GlobalAppends);

                // 5. Per-player appends
                if (m != null && s_PendingPerPlayer.TryGetValue(m.Serial, out var perAppend))
                {
                    if (perAppend != null && perAppend.Count > 0)
                        list.AddRange(perAppend);
                    s_PendingPerPlayer.Remove(m.Serial);
                }
            }

            // Remove any empty lines
            list.RemoveAll(l => string.IsNullOrWhiteSpace(l.Text));

            return list.ToArray();
        }

        protected bool IsInmate()
        {
            return m_Mobile is PlayerMobile pm && pm.PrisonInmate;
        }
    }

    public partial class WelcomeTimer
    {

        public static void Initialize()
        {
            // versio info
            string version = string.Format("{0}.{1} [Build {2}]",
                Utility.BuildMajor(), Utility.BuildMinor(), Utility.BuildBuild());

            // For each ruleset, define three segments:
            //  1) Header (non-negotiable, never replaced)
            //  2) Replaceable block (customizable via ReplaceBlock)
            //  3) Append (default empty; Add via AppendLines/AppendFor)
            List<string> header = new List<string>();
            List<string> replaceable = new List<string>();
            List<string> append = new List<string>();

            bool isTestCenter = TestCenterServer.Enabled;
            bool isEvent = Core.GMNEV_CFG;

            string shardSuffix =
                isTestCenter && isEvent ? " Test Center Event Shard" :
                isTestCenter ? " Test Center" :
                isEvent ? " Event Shard" :
                "";

            header.Add(string.Format("Welcome to {0}{1}.", Core.Server, shardSuffix));

            if (Core.ReleasePhase < ReleasePhase.Production)
            {
                header.Add("Version: " + Utility.GetCustomEnumNames(typeof(ReleasePhase))[(int)Core.ReleasePhase]);
                header.Add("This Server will be wiped.");
            }

            if (Core.RuleSets.SeasonsRules())
            {
                header.Add("This Server will be wiped " + SeasonInfo(SeasonRequest.Reset, "MMMM d, yyyy"));
            }

            header.Add(string.Format("GmnUO Core version {0}, launched March 2004.", version));

            if (Core.Tribute_CFG)
            {
                replaceable.Add("This shard brings back 5000 of the most recent players from the shards:");
                replaceable.Add("Angel Island 3.0 (2016), Angel Island 7.0 (2024), and Siege Perilous 7.0 (2024).");
                replaceable.Add("They're all here in Britain. Enjoy the trip down memory lane.");
                replaceable.Add("-- Adam Ant");
            }
            else if (Core.RuleSets.MortalisRules())
            {
                replaceable.Add("You are mortal here, and if you die you will need to reenter this world anew.");
                replaceable.Add("There is no resurrection, there are no second chances.");
                replaceable.Add("You have been warned.");
                replaceable.Add("Life is only as sweet as death is painful.");
            }
            else if (Core.RuleSets.LoginServerRules())
            {
                // No normal player-facing info block.
            }
            else
            {
                replaceable.Add("Please see www.game-master.net for game-play information.");
            }

            if (isTestCenter)
            {   // 6/10/26, Adam: too much journal spam - moved to gump
                //append.Add("You are able to customize your character's stats and skills any way you wish. To see the commands to do this just say 'help'.");
                //append.Add("You will find bank checks worth nearly 1.5 million gold in your bank!");
                //append.Add("A spellbook and a bag of reagents has been placed into your bank box.");
                //append.Add("Various tools have been placed into your bank.");
                //append.Add("Various raw materials like ingots, logs, feathers, hides, bottles, etc, have been placed into your bank.");
                //append.Add("Nine unmarked recall runes have been placed into your bank box.");
                //append.Add("A keg of each potion has been placed into your bank box.");
                //append.Add("Two of each level of treasure map have been placed in your bank box.");
                //append.Add("You will find 60,000 gold pieces deposited into your bank box. Spend it as you see fit and enjoy yourself!");
            }

            s_Header = header.ToArray();
            s_DefaultReplaceable = replaceable.ToArray();
            s_DefaultAppend = append.ToArray();

            InitializeReturningGreetings();
        }

        private static void InitializeReturningGreetings()
        {
            s_ReturningTemplates.Clear();

            List<WelcomeLine> lines = new List<WelcomeLine>();

            if (Core.GMNTC_CFG || Core.GMNEV_CFG)
            {
                lines.Add(new WelcomeLine(
                    $"Welcome, {{0}}, to {Core.Server} Test Center{(Core.GMNEV_CFG ? " Event Shard" : ".")} {{1}}",
                    0x3B2));
            }
            else
            {
                lines.Add(new WelcomeLine($"Welcome, {{0}}, to {Core.Server}. {{1}}", 0x3B2));
            }

            if (Core.RuleSets.SeasonsRules())
            {
                lines.Add(new WelcomeLine(
                    $"Seasons will be reset {SeasonInfo(SeasonRequest.Reset, "MMMM d, yyyy")}",
                    0x35));
            }

            lines.Add(new WelcomeLine("Type [about to learn about this shard.", 0x3B2));

            s_ReturningTemplates[Core.ServerID] = lines.ToArray();
        }

        private static WelcomeLine[] ComposeReturningLines(Mobile m)
        {
            if (m == null)
                return Array.Empty<WelcomeLine>();

            if (!s_ReturningTemplates.TryGetValue(Core.ServerID, out var template))
                return Array.Empty<WelcomeLine>();

            // Only show full stats for staff or when population is high
            bool showStats = NetState.Instances.Count >= CoreAI.StatsThreshold
                             || m.AccessLevel > AccessLevel.Player;

            var lines = new List<WelcomeLine>();

            foreach (var line in template)
            {
                string text = line.Text.Replace("{0}", m.Name ?? "Adventurer");

                // Only insert the stats line if we want to show it
                if (showStats && text.Contains("{1}"))
                {
                    string statsText = Server.Misc.Stats.Format(m.AccessLevel);
                    text = text.Replace("{1}", statsText ?? "");
                }
                else if (text.Contains("{1}"))
                {
                    text = text.Replace("{1}", "");   // remove the placeholder for normal players
                }

                lines.Add(new WelcomeLine(text, line.Hue));
            }

            return lines.ToArray();
        }
    }
}

