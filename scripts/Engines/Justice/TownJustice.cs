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

using System;
namespace Server.Engines
{
    /// <summary>
    /// Town-level justice announcement producer.
    ///
    /// Responsibilities:
    /// - Emits short-lived Town Crier bulletins for town-based murderous offenses
    ///   committed in guarded regions.
    /// - Provides public narrative and deterrence for in-town violent acts without
    ///   leaking internal justice mechanics to players.
    ///
    /// Non-responsibilities:
    /// - Does NOT change criminal status, murder counts, or punishment escalation.
    ///   (Handled by GrieferEscalation / patrol guards / guard-whack logic.)
    /// - Does NOT track offenders or update locations over time.
    /// - Does NOT own bulletin persistence or Town Crier behavior
    ///   (see TownCrierBulletinSystem).
    /// </summary>
    public static class TownJustice
    {
        /// <summary>
        /// Adds a Town Crier announcement for a town based murderous offense.
        /// This does not change criminal status; GrieferEscalation handles that.
        /// </summary>
        public static ListEntry? AddMurderousOffenseAnnouncement(Mobile m)
        {
            if (m == null)
                return null;

            string[] lines = new string[2];

            // Line 0: high level accusation.
            lines[0] = string.Format(
                "{0} has been reported for attempted murder within the city walls.",
                m.Name
            );

            // Possible flavor lines (ASCII only).
            string[] flavor =
            {
                "Rumor says this criminal was dragged off toward Buccaneer's Den for punishment.",
                "Rumor says this criminal was cast out into the outlands for punishment.",
                "Word spreads that this criminal was driven into the outlands to face his fate.",
                "Some say the guards dragged him into the outlands, never to walk the city again.",
                "Witnesses report the criminal was escorted to the outlands for appropriate correction."
            };

            lines[1] = flavor[Utility.Random(flavor.Length)];

            // Expiration timestamp
            DateTime dt = DateTime.UtcNow + TimeSpan.FromMinutes(10);

            ListEntry entry = new ListEntry(
                lines,
                null,
                dt,
                ListEntryType.HarmfulOffense
            );

            TownCrierBulletinSystem.AddEntry(entry);
            return entry;
        }

    }
}