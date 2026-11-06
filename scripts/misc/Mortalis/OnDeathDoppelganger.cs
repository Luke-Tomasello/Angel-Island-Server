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

// *********************************************************************************************
// Mortalis – Doppelgänger Death Exhibit System
// *********************************************************************************************
// This module implements the Mortalis memorial mechanic. When a player dies, a hidden
// DoppelgangerPlayer is created on the internal map as a snapshot of their appearance,
// equipment, direction, killers, and murder counts.
//
// When the player's *real* corpse later decays, a corresponding DoppelgangerCorpse is restored
// from internal storage at the same location, becoming the persistent “death exhibit” visible
// in the world.
//
// Identity model (GUID/OwnerGUID chain):
//     Player.GUID
//         → DoppelgangerPlayer.OwnerGUID
//             → DoppelgangerCorpse.OwnerGUID
//
// The player’s GUID is the sole identity used to link all memorial components. CorpseKey
// and account-side corpse tracking are obsolete. Retrieval logic simply searches for a
// DoppelgangerCorpse whose OwnerGUID matches the player’s GUID.
//
// TooSoon():
//     We do not check corpse age here. Corpse decay timing is managed by the corpse itself.
//     TooSoon() only tests for *presence*: if a DoppelgangerCorpse with the player’s GUID
//     already exists, we suppress creation of a new memorial.
//
// Event flow:
//     BeforePlayerDeath  → create hidden DoppelgangerPlayer (identity only)
//     Corpse.Delete      → resurrect hidden DoppelgangerCorpse as memorial
//     PlayerDeath        → broadcast realm-wide death message
//
// This system produces:
//     • Seamless transition from lootable corpse → memorial exhibit
//     • Deterministic identity chains
//     • Zero bookkeeping on Account
//     • No proxying, no packet tricks, no special timers
//
// See also:
//     • DoppelgangerPlayer.cs   – identity snapshot logic
//     • DoppelgangerCorpse.cs   – memorial corpse activation
//     • Corpse.Delete()         – promotion of hidden corpse into the world
// *********************************************************************************************


/* Scripts\misc\Mortalis\OnDeathDoppelganger.cs
 * CHANGELOG:
 *  12/12/2025, Adam
 *      Checked in
 */

using Server.Mobiles;
using System;
using static Server.Utility;

namespace Server.Misc
{
    internal class OnDeathDoppelganger
    {
        public static void Initialize()
        {
            if (Core.RuleSets.UseDoppelgangers())
            {
                EventSink.PlayerDeath += new PlayerDeathEventHandler(EventSink_PlayerDeath);
                EventSink.BeforePlayerDeath += new BeforePlayerDeathEventHandler(EventSink_BeforePlayerDeath);
            }
        }

        public static void EventSink_BeforePlayerDeath(BeforePlayerDeathEventArgs e)
        {
            PlayerMobile? pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            // If the player died recently and their prior memorial is still processing,
            // suppress creation of another doppelgänger.
            if (TooSoon(pm.AcctGUID))
                return;

            // ─────────────────────────────────────────────────────────────────────────────
            // Create the Doppelganger exhibit
            // ─────────────────────────────────────────────────────────────────────────────
            // A DoppelgangerPlayer is a snapshot of the player at the moment of death.
            // This NPC is immediately moved to the internal map (hidden) and later paired
            // with a DoppelgangerCorpse when the real corpse decays.
            //
            // Identity chain for Mortalis:
            //     Player.GUID  →  DoppelgangerPlayer.OwnerGUID  →  DoppelgangerCorpse.OwnerGUID
            //
            // This ensures that all memorial components can always be found using the
            // player’s stable GUID—no CorpseKey or per-account tracking is needed now.
            // ─────────────────────────────────────────────────────────────────────────────
            Mobile killed = pm;
            DoppelgangerPlayer exhibit = new DoppelgangerPlayer();

            // Layer-by-layer duplication of appearance and gear
            Utility.CopyLayers(exhibit, killed, CopyLayerFlags.Default);

            // Mirror core appearance and metadata
            exhibit.Name = killed.Name;
            exhibit.Hue = killed.Hue;
            exhibit.Body = killed.Female ? 401 : 400;     // correct body graphic
            exhibit.Female = killed.Female;               // correct death sound
            exhibit.Direction = killed.Direction;         // preserve facing
            exhibit.LastKiller = killed.LastKiller;       // preserve kill attribution
            exhibit.ShortTermMurders = killed.ShortTermMurders;
            exhibit.LongTermMurders = killed.LongTermMurders;

            // Place in the world briefly (for corpse positioning), then hide it
            exhibit.MoveToWorld(killed.Location, killed.Map);
            exhibit.MoveToIntStorage(PreserveLocation: true);

            // Assign the player’s GUID as the exhibit’s OwnerGUID.
            // This identity follows through to the DoppelgangerCorpse, allowing the
            // memorial reconstruction code to locate everything by GUID alone.
            exhibit.OwnerGUID = killed.AcctGUID;
        }

        public static void EventSink_PlayerDeath(PlayerDeathEventArgs e)
        {
            PlayerMobile? pm = e.Mobile as PlayerMobile;
            if (pm == null) return;
            Accounting.Account? acct = pm.Account as Accounting.Account;
            if (acct == null) return;
            if (pm.Corpse == null) return;

            // tell everyone
            BroadcastDeath(pm);
        }

        public static bool TooSoon(uint playerGUID)
        {
            // We do not check age or expiration windows here.
            // Corpse decay timing is handled by the corpse itself.
            //
            // Our only test is presence: if a DoppelgangerCorpse already exists
            // for this player identity (OwnerGUID match), then it is “too soon”
            // to create another exhibit.

            foreach (var it in World.Items.Values)
            {
                if (it == null || it.Deleted)
                    continue;

                if (it is DoppelgangerCorpse c && c.OwnerGUID == playerGUID)
                {
                    // A memorial exhibit already exists for this player.
                    // Do not create another.
                    return true;
                }
            }

            // No existing exhibit found → OK to spawn a new one.
            return false;
        }

        private static readonly string DeathPrefix = "Word reaches the realm:"; // constant for filtering

        private static readonly string[] DeathMessages =
        {
            "{0} has fallen upon the field of valor.",
            "{0} met a fate most unkind.",
            "{0} breathes no more beneath the cold sky.",
            "News spreads that {0} has perished.",
            "The gods whisper that {0} has joined the fallen.",
            "A chill wind carries the tale of {0}'s demise.",
            "Tales will be told of {0}, whose journey ends this day.",
            "{0} was struck down by cruel fate.",
            "Bards shall mourn the passing of {0}.",
            "The realm grows quieter; {0} walks among the dead."
        };

        public static void BroadcastDeath(PlayerMobile pm)
        {
            // Get last aggressor if available
            Mobile? killer = GetLastAggressor(pm);

            string msgTemplate;
            string message = String.Empty;

            if (killer == null)
            {
                // Use your existing random “fate” messages
                msgTemplate = DeathMessages[Utility.Random(DeathMessages.Length)];
                string msgBody = string.Format(msgTemplate, pm.Name);
                message = $"{DeathPrefix} {msgBody}";
                Commands.CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x482, message);
                return;
            }

            // Killer exists — construct attribution message
            string? killerName = FormatKillerName(killer);

            message =
                $"{DeathPrefix} {pm.Name} has fallen, slain by {killerName}.";

            Commands.CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x482, message);
        }

        private static string? FormatKillerName(Mobile m)
        {
            if (m == null)
                return null;

            // Players: always use the name directly, no article
            if (m is PlayerMobile)
                return m.Name;

            string name = m.Name;

            if (String.IsNullOrWhiteSpace(name))
                return "something";

            // If it already starts with an article, leave it as-is
            // e.g., "a troll", "an ettin", "the harrower"
            if (name.StartsWith("a ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("an ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }

            // Creature with a proper noun name (rare but possible)
            // e.g., a dragon named "Smaug"
            // Treat like a player -> no article
            if (char.IsUpper(name[0]))
                return name;

            // Creature with a bare noun identifier -> add article
            // Decide between "a" and "an"
            char first = char.ToLower(name[0]);
            bool useAn = "aeiou".IndexOf(first) >= 0;

            return (useAn ? "an " : "a ") + name;
        }

    }
}
