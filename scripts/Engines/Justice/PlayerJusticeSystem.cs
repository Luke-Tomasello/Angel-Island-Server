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

/* Scripts/Engines/PlayerJusticeSystem/PlayerJusticeSystem.cs
 * CHANGELOG:
 *  7/05/10, Pix
 *      changed UpdateLocations() to UpdateAnnouncements() - now changes the message after the bounty has been collected
 *  5/17/10, Adam
 *		eliminate the old PlayerJusticeSystem.MakeAllEntriesCriminal() feature and replace it with the new mobile function
 * 			m.MakeCriminal(dt - DateTime.UtcNow);
 *	02/11/06, Adam
 *		Make common the formatting of sextant coords.
 *	01/13/06, Pix
 *		New file - result from separation of TownCrierBulletinSystem and PlayerJusticeSystem systems
 */

using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Engines
{
    /// <summary>
    /// Player-facing justice bulletin producer + maintenance loop.
    ///
    /// Responsibilities:
    /// - Creates time-limited Town Crier bulletins for justice events (currently:
    ///   unattended macroing; optionally theft offenders via AddThiefOffender).
    /// - For unattended macroing, also applies the associated penalty window by
    ///   calling MakeCriminal() for the same duration as the bulletin.
    /// - Runs a 1-minute heartbeat that refreshes active macroing bulletins with
    ///   "last seen" location text (sextant/region) and downgrades the headline if
    ///   no bounty exists.
    ///
    /// Non-responsibilities:
    /// - Does NOT own the Town Crier system or persistence (see TownCrierBulletinSystem).
    /// - Does NOT decide theft/criminal penalties; higher-level justice logic
    ///   (e.g., HandleCriminalAction/TownJustice) is responsible for gray/fine/bounty.
    /// </summary>
    public class PlayerJusticeSystem
    {
        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0), new TimerCallback(PJHeartbeat));
        }

        public static void PJHeartbeat()
        {
            try
            {
                UpdateAnnouncements();
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static bool HasBeenReported(Mobile m)
        {
            for (int i = 0; i < TownCrierBulletinSystem.TheList.Count; i++)
            {
                if (((ListEntry)TownCrierBulletinSystem.TheList[i]).Mobile == m)
                {
                    return true;
                }
            }
            return false;
        }

        public static ListEntry AddMacroer(string[] lines, Mobile m)
        {
            TimeSpan duration = TimeSpan.FromMinutes(CoreAI.AFKGrayMinutes);
            DateTime announcementEnds = DateTime.UtcNow + duration;
            ListEntry le = new ListEntry(lines, m, announcementEnds, ListEntryType.MacroerOffense);
            TownCrierBulletinSystem.AddEntry(le);
            return le;
        }

        /// <summary>
        /// Adds a Town Crier announcement for a theft offense.
        /// This does not change criminal status; HandleCriminalAction (or higher-level justice)
        /// is responsible for gray/fine/bounty.
        /// </summary>
        public static ListEntry? AddThiefOffender(Mobile thief, Mobile? victim, object? root, TimeSpan duration, string? reason)
        {
            if (thief == null)
                return null;

            // Keep it short: 2 lines like MurderousOffense.
            string[] lines = new string[2];

            // Line 0: high level accusation.
            // Avoid non-ASCII punctuation.
            if (victim != null)
            {
                lines[0] = string.Format(
                    "{0} has been reported for theft against {1}.",
                    thief.Name,
                    victim.Name
                );
            }
            else
            {
                lines[0] = string.Format(
                    "{0} has been reported for theft.",
                    thief.Name
                );
            }

            // Line 1: flavor or brief context (optional).
            string[] flavor =
            {
                "Word spreads that a bounty has been posted for this thief.",
                "Citizens whisper that this thief was seen fleeing the scene.",
                "Rumor says the thief slipped away before the guards could act.",
                "Some say a price has been placed on this thief's head.",
                "Witnesses report the thief was marked and driven from the area."
            };

            // If you want to surface reason, keep it short and controlled.
            // Prefer flavor unless reason is a known short token.
            if (!string.IsNullOrEmpty(reason))
                lines[1] = reason;
            else
                lines[1] = flavor[Utility.Random(flavor.Length)];

            // Expiration timestamp: theft should live longer than 10 minutes if you want the 4 hour gray.
            // If you want the crier entry to match gray duration, set 4 hours. Otherwise use shorter.
            DateTime dt = DateTime.UtcNow + duration;

            ListEntry entry = new ListEntry(
                lines,
                thief,               // Pass thief so the heartbeat can update "last seen" like PlayerJusticeSystem entries
                dt,
                ListEntryType.TheftOffense
            );

            TownCrierBulletinSystem.AddEntry(entry);
            return entry;
        }


        public static void UpdateAnnouncements()
        {
            foreach (ListEntry le in TownCrierBulletinSystem.TheList)
            {
                if (le == null)
                    continue;

                // Only entries we know how to maintain (location + post-bounty headline).
                if (le.Type != ListEntryType.MacroerOffense &&
                    le.Type != ListEntryType.TheftOffense)
                    continue;

                if (le.Mobile == null || le.Mobile.Deleted)
                    continue;

                // 1) Update location line (Line 1)
                string location;
                int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;

                Map map = le.Mobile.Map;
                bool valid = Sextant.Format(le.Mobile.Location, map,
                    ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

                if (valid)
                    location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                else
                    location = string.Format("{0} {1}", le.Mobile.X, le.Mobile.Y);

                if (map != null)
                {
                    Region reg = le.Mobile.Region;

                    if (reg != map.DefaultRegion)
                        location += (" in " + reg);
                }

                le.Lines[1] = string.Format("{0} was last seen at {1}.", le.Mobile.Name, location);

                // 2) If no bounty remains, rewrite headline (Line 0) based on entry type
                PlayerMobile? pm = le.Mobile as PlayerMobile;
                if (pm == null)
                    continue;

                if (BountyKeeper.BountiesOnPlayer(pm) > 0)
                    continue;

                switch (le.Type)
                {
                    case ListEntryType.MacroerOffense:
                        le.Lines[0] = string.Format(
                            "{0} is an enemy of the kingdom for unlawful resource gathering.",
                            pm.Name);
                        break;

                    case ListEntryType.TheftOffense:
                        le.Lines[0] = string.Format(
                            "{0} is wanted for theft and has been marked as an outlaw.",
                            pm.Name);
                        break;
                }
            }
        }

    }
}