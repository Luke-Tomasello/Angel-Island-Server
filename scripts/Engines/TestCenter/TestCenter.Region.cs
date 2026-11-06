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
 * Test Center System Overview
 *
 * This module serves as the central documentation point for the shard's Test Center system.
 * While the legacy server-wide Test Center model still exists for compatibility, the active
 * design has shifted to a region-based architecture built around TestCenterRegion and its
 * associated controller.
 *
 * The modern Test Center model is based on containment:
 *
 * 1. Access is intentionally limited through dedicated teleporters and travel rules.
 * 2. Test Center regions are isolated from normal shard play.
 * 3. Region tripwires detect invalid transitions:
 *    - real players entering Test Center
 *    - clones leaving Test Center
 * 4. Violations are recorded on the mobile and processed asynchronously by cron.
 * 5. Main-character login destroys the clone and its possessions.
 * 6. Additional containment measures may track and destroy items associated with a breach.
 *
 * This layered design exists because Test Center is not merely a convenience feature; it is
 * a quarantine boundary. If a real player gains access to Test Center resources, or if a clone
 * escapes with Test Center state or items, the live shard economy can be contaminated.
 *
 * New development should target the region-based Test Center model rather than expanding the
 * legacy full-server implementation found in this class.
 */

/* Scripts\Engines\TestCenter\TestCenter.Region.cs
 * CHANGELOG
 *	4/16/2026, Adam
 *  Move to a region-based system instead of a full-server Test Center.
 *      This allows running of the new TestCenterRegion cloning system without
 *      needing to maintain two separate server configurations.
 */

namespace Server.Engines
{
    public class TestCenterRegion
    {
        public static void Configure()
        {
            // Region-based Test Center is configured via TestCenterRegion objects
        }

        public static void Initialize()
        {
            // Registration is typically done inside TestCenterRegion itself.
            // If you want global registration fallback, you can add it here later.
        }

        /// <summary>
        /// Called by TestCenterRegion when speech occurs inside a test center region.
        /// </summary>
        public static void HandleSpeech(Regions.TestCenterRegion region, SpeechEventArgs args)
        {
            if (region?.TestCenter != true)
                return;

            // Delegate to shared command system
            TestCenterCommands.TCSetSkills.EventSink_Speech(args, m => region.TestCenter);
            TestCenterCommands.TCHelpGump.EventSink_Speech(args, m => region.TestCenter);
        }
    }
}
