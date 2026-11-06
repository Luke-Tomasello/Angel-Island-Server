
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

/* Engines/TestCenter/TestCenterRegion.cs
 * CHANGELOG:
 * 4/16/2026, Adam
 *      Added.
 */

using Server.Engines.CronScheduler;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Regions
{
    [Obsolete("Test Center enforcement is now facet-based. Use Map.TestCenter / OnMapEnter / OnMapExit logic instead.")]
    public class TestCenterRegion : CustomRegion
    {
        public TestCenterRegionControl TCController
        {
            get { return this.Controller as TestCenterRegionControl; }
        }

        public TestCenterRegion(CustomRegionControl rc)
            : base(rc)
        {
            Setup();
        }

        private void Setup()
        {
            // all region initialization happens in the region control
        }
        #region Test Center
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.System)]
        public override bool TestCenter
        {
            get
            {
                return base.TestCenter;
            }
            set
            {
                if (base.TestCenter == value)
                    return;

                base.TestCenter = value;

                if (value)
                {
                    ConfigureTestCenterRules();
                }
                else
                {
                    NotifyStaffInRegion(
                        "Warning: TestCenter was disabled. Region travel flags and restricted spell settings were not restored automatically.");
                }
            }
        }
        #region Test Center Helpers
        private void NotifyStaffInRegion(string message)
        {
            foreach (Mobile m in Players.Values)
            {
                if (m != null && !m.Deleted && m.AccessLevel > AccessLevel.Player)
                    m.SendMessage(message);
            }
        }
        private void ConfigureTestCenterRules()
        {
            SetFlag(RegionFlag.AllowTravelSpellsInRegion, true);
            SetFlag(RegionFlag.NoRecallInto, true);
            SetFlag(RegionFlag.NoGateInto, true);
            SetFlag(RegionFlag.ThisRegionOnly, true);

            // Allow internal TC travel, but block travel out by restricting source-cast spells.
            SetRestrictedSpell(31, true);  // RecallSpell
            SetRestrictedSpell(51, true);  // GateTravelSpell

            // Explicitly allow marking within TC.
            SetRestrictedSpell(44, false); // MarkSpell
        }
        #endregion Test Center Helpers
        #endregion Test Center
        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            Server.Engines.TestCenterRegion.HandleSpeech(this, e);
        }

        public override void OnEnter(Mobile m)
        {
            base.OnEnter(m);

            #region Test Center Clones
            if (m is PlayerMobile pm && !pm.Clone && this.TestCenter && pm.AccessLevel == AccessLevel.Player)
            {
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    if (pm == null || pm.Deleted || pm.Clone || Utility.IsInJail(pm) || pm.TcEntryViolation == true)
                        return;

                    pm.TcEntryViolation = true;
                    CJailClones jc = new CJailClones();
                    jc.JailClones();

                });
            }
            #endregion Test Center Clones
        }

        public override void OnExit(Mobile m)
        {
            base.OnExit(m);

            #region Test Center Clones
            if (m is PlayerMobile pm && pm.Clone && this.TestCenter && pm.AccessLevel == AccessLevel.Player)
            {
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    if (pm == null || pm.Deleted || !pm.Clone || Utility.IsInJail(pm) || pm.TcExitViolation == true)
                        return;

                    if (this.Contains(pm.Location))
                        return;

                    pm.TcExitViolation = true;
                    CJailClones jc = new CJailClones();
                    jc.JailClones();
                });
            }
            #endregion Test Center Clones
        }
    }
}
