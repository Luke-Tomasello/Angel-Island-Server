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

/* Scripts/Engines/Help/StuckMenu.cs
 * Changelog:
 *  6/7/2023, Adam (exit options)
 *      Add public moongates to the list of exit options.
 *      Also exclude teleporters and moongates as exit options if they lead to a dungeon.
 *  5/24/2023, Adam (StrandedAtSea())
 *      Check if they are StrandedAtSea without a boat (maybe the boat decayed.)
 *      If so, when we try to unstuck them, use the standard Strandedness.ProcessStranded()
 *  5/20/2023, Adam (AllowUnstuck())
 *      Disallow help stuck if:
 *          1. They are in a dungeon (all dungeons have teleporters)
 *          2. They can walk 8-10 tiles in some direction
 *          3. If T2A is not allowed, you get the Brit menu
 *  5/20/23, Yoar (Help Stuck)
 *      Complete refactor of help-stuck. Moved all code into StuckMenu.cs.
 *  12/2/22, Adam (Help Stuck)
 *      Overhaul of help stuck system.
 *          o) Add smart checks to see if the requester is actually near a means of escape.
 *              If so, give them some directional hints.
 *          o) Move mobile checks (is criminal etc.) from ValidUseLocation() into ValidUseMobile(). 
 *              Messaging is different.
 *          o) Reorganize logical flow. For instance, if they are physically bocked, try moving them first.
 *              If they are not physically blocked, begin the gumping process (or notify staff.)
 *  9/4/22, Yoar (WorldZone)
 *      Added support for world zone unstuck locations.
 *  8/28/22, Yoar
 *      Can no longer unstuck while holding a sigil.
 *  2/10/22, Adam (IsBlocked())
 *      Use the new mobile IsBlocked() function to see if they are truly stuck
 *      Add a check in TeleportTimer to see if they moved (like they had precast Teleport before hitting Help)
 *      If they are not in the same location as when they asked for help, ignore the request.
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	3/18/07, Pix
 *		Fixed logic for all cases for help stuck.
 *	3/2/07, Pix
 *		Implemented random selection of 3 towns and 6 shrines for help stuck.
 *	3/1/07, Pix
 *		Tweaks to last night's change.
 *	2/28/07, Pix
 *		Modifications to the help-stuck system.
 *  08/03/06 Taran Kain
 *		Re-added OnTick() reentry blocking, after further investigation. Added explanatory comment.
 *	07/30/06 Taran Kain
 *		Removed OnTick() reentry blocking, could prevent it from running at all
 *	05/30/06, Adam
 *		- Make ValidUseLocation() succeed for staff
 *		- Also add comments and some cleanup.
 *		- Block OnTick() reentry (Open separate SR)
 *  05/01/06 Taran Kain
 *		Consolidated validation checks into ValidUseLocation() to help checking at more than one point in the process.
 *  04/11/06, Kit
 *		Made check IsInSecondAgeArea public for use with testing at help menu to prvent helpstuck in lostlands.
 *	06/04/05, Pix
 *		Force mobile to drop whatever they're holding on HelpStuck teleport.
 *  03/14/05, Lego
 *           added Oc'Nivelle to help stuck menu
 *  11/09/04, Lego
 *           fixed problem with delucia teleporting
 *  9/26/04, Pigpen
 *		Added Buc's Den to Old Lands Locations and added Location in South East T2A to T2A Locations
 *	9/17/04, Pigpen
 *		Changed Cove teleport location, fixing problem of stuck players teleporting into wall of new Rug Shop.
 *  7/12/04, Pix
 *		Fixed problem with help-stuck option where if the person instalogs while
 *		waiting for teleport timer, they get teleported to trammel.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using Server.Engines.Alignment;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using System;
using System.Collections.Generic;
using static Server.Utility;
namespace Server.Menus.Questions
{
    public class StuckMenu : Gump
    {
        public const int MAXHELPSTUCKALIVEWEIGHT = 101;

        private static readonly Point3D m_DefaultLocation = new Point3D(1420, 1698, 0); // WBB

        private static Point3D[] m_Locations = new Point3D[]
            {
                new Point3D( 1522, 1757, 28 ), //0: Britain
				new Point3D( 2005, 2754, 30 ), //1: Trinsic
				new Point3D( 2973,  891,  0 ), //2: Vesper
				new Point3D( 2498,  392,  0 ), //3: Minoc
				new Point3D(  490, 1166,  0 ), //4: Yew
				new Point3D( 2249, 1192,  0 ), //5: Cove
				new Point3D( 2716, 2182,  0 ), //6: Buccaneer's Den
                new Point3D(  825, 1072,  0 ), //7: Oc'Nivelle
				new Point3D( 5720, 3109, -1 ), //8: Papua
				new Point3D( 5216, 4033, 37 ), //9: Delucia
				new Point3D( 5884, 3596,  1 ), //10: South East Lost Lands

				//Added for AutoMove option:
                new Point3D( 1458, 843, 7 ), //11: chaos shrine
				new Point3D( 1858, 873, -1 ), //12: compassion shrine
				new Point3D( 1728, 3528, 3 ), //13: honor shrine
				new Point3D( 1301, 635, 16 ), //14: justice shrine
				new Point3D( 3355, 292, 4 ), //15: sacrifice shrine
				new Point3D( 1600, 2490, 12 ), //16: spirituality shrine
			};

        public static Point3D[] GetUnstuckLocations(Mobile m)
        {
            #region World Zone
            if (WorldZone.IsInside(m.Location, m.Map))
                return WorldZone.ActiveZone.UnstuckLocations;
            #endregion

            return m_Locations;
        }

        public static Point3D GetUnstuckLocation(Mobile m, int index)
        {
            Point3D[] locations = GetUnstuckLocations(m);

            if (index >= 0 && index < locations.Length)
                return locations[index];

            return m_DefaultLocation;
        }

        public static bool IsInSecondAgeArea(Mobile m)
        {
            if (m.Map != Map.Trammel && m.Map != Map.Felucca)
                return false;

            if (m.X >= 5120 && m.Y >= 2304)
                return true;

            if (m.Region.IsPartOf("Terathan Keep"))
                return true;

            return false;
        }

        private Mobile m_Mobile, m_Sender;
        private bool m_MarkUse;

        private Timer m_Timer;

        public StuckMenu(Mobile beholder, Mobile beheld, bool markUse)
            : base(150, 50)
        {
            m_Sender = beholder;
            m_Mobile = beheld;
            m_MarkUse = markUse;

            Closable = false;
            Dragable = false;

            AddPage(0);

            AddBackground(0, 0, 280, 400, 2600);

            AddHtmlLocalized(50, 25, 170, 40, 1011027, false, false); //Chose a town:
            // adam: if they are in T2A, and Core.RuleSets.AllowLostLandsAccess() == false, they should not be here
            if (!IsInSecondAgeArea(beheld) || !Core.RuleSets.AllowLostLandsAccess())
            {
                AddButton(50, 60, 208, 209, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 60, 335, 40, 1011028, false, false); // Britain

                AddButton(50, 95, 208, 209, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 95, 335, 40, 1011029, false, false); // Trinsic

                AddButton(50, 130, 208, 209, 3, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 130, 335, 40, 1011030, false, false); // Vesper

                AddButton(50, 165, 208, 209, 4, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 165, 335, 40, 1011031, false, false); // Minoc

                AddButton(50, 200, 208, 209, 5, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 200, 335, 40, 1011032, false, false); // Yew

                AddButton(50, 235, 208, 209, 6, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 235, 335, 40, 1011033, false, false); // Cove

                AddButton(50, 270, 208, 209, 7, GumpButtonType.Reply, 0);
                AddHtml(75, 270, 335, 40, "Buccaneer's Den", false, false); // Buccaneer's Den

                AddButton(50, 305, 208, 209, 8, GumpButtonType.Reply, 0);
                AddHtml(75, 305, 335, 40, "Oc'Nivelle", false, false); //Oc'Nivelle
            }
            else
            {

                AddButton(50, 60, 208, 209, 9, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 60, 335, 40, 1011057, false, false); // Papua

                AddButton(50, 95, 208, 209, 10, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 95, 335, 40, 1011058, false, false); // Delucia

                AddButton(50, 120, 208, 209, 11, GumpButtonType.Reply, 0);
                AddHtml(75, 130, 335, 40, "South East Lost Lands", false, false); // South East Lost Lands

            }

            AddButton(55, 340, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(90, 340, 320, 40, 1011012, false, false); // CANCEL
        }

        public void AutoSelect()
        {
            if (m_Mobile != null)
            {
                Point3D[] locations = GetUnstuckLocations(m_Mobile);

                int index;

                if (locations == m_Locations)
                {
                    if (m_Mobile.Criminal || (m_Mobile.ShortTermMurders >= 5 && !Core.RedsInTown))
                    {
                        //don't send murderers or criminals to guarded town
                        index = Utility.RandomList(7, 11, 12, 13, 14, 15, 16);
                    }
                    else
                    {
                        index = Utility.RandomList(0, 3, 7, 11, 12, 13, 14, 15, 16);
                    }
                }
                else
                {
                    index = Utility.Random(locations.Length);
                }

                Teleport(index);
            }
        }

        public void BeginClose()
        {
            StopClose();

            m_Timer = new CloseTimer(m_Mobile);
            m_Timer.Start();

            m_Mobile.Frozen = true;
        }

        public void StopClose()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Mobile.Frozen = false;
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            StopClose();

            if (info.ButtonID == 0)
            {
                if (m_Mobile == m_Sender)
                    m_Mobile.SendLocalizedMessage(1010588); // You choose not to go to any city.
            }
            else if (!IsInSecondAgeArea(m_Mobile) || !Core.RuleSets.AllowLostLandsAccess())
            {
                if (info.ButtonID >= 1 && info.ButtonID <= 8)
                    Teleport(info.ButtonID - 1);
            }
            else if (info.ButtonID == 9 || info.ButtonID == 10 || info.ButtonID == 11)
            {
                Teleport(info.ButtonID - 1);
            }
        }

        private void Teleport(int index)
        {
            Point3D loc = GetUnstuckLocation(m_Mobile, index);

            if (m_MarkUse)
            {
                m_Mobile.SendLocalizedMessage(1010589); // You will be teleported within the next two minutes.

                new TeleportTimer(m_Mobile, loc, Map.Felucca, TimeSpan.FromSeconds(10.0 + (Utility.RandomDouble() * 110.0)), m_Sender.AccessLevel > AccessLevel.Player).Start();
            }
            else
            {
                new TeleportTimer(m_Mobile, loc, Map.Felucca, TimeSpan.Zero, m_Sender.AccessLevel > AccessLevel.Player).Start();
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("HelpStuckTest", AccessLevel.Counselor, new CommandEventHandler(HelpStuckTest_OnCommand));
            CommandSystem.Register("HelpStuckReset", AccessLevel.Player, new CommandEventHandler(HelpStuckReset_OnCommand));
        }

        #region Commands

        private static void HelpStuckTest_OnCommand(CommandEventArgs e)
        {
            m_Testing = true;

            DoHelpStuck(e.Mobile);

            m_Testing = false;
        }

        private static void HelpStuckReset_OnCommand(CommandEventArgs e)
        {
            if (Core.ReleasePhase == ReleasePhase.Production)
            {
                e.Mobile.SendMessage("You cannot reset help stuck on this shard.");
            }
            else
            {
                if (e.Mobile is PlayerMobile)
                    ((PlayerMobile)e.Mobile).ClearStuckMenu();

                e.Mobile.SendMessage("Help Stuck reset.");
            }
        }

        #endregion

        private class CloseTimer : Timer
        {
            private Mobile m_Mobile;
            private DateTime m_End;

            public CloseTimer(Mobile m)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                m_Mobile = m;
                m_End = DateTime.UtcNow + TimeSpan.FromMinutes(3.0);
            }

            protected override void OnTick()
            {
                if (m_Mobile.NetState == null || DateTime.UtcNow > m_End)
                {
                    m_Mobile.Frozen = false;
                    m_Mobile.CloseGump(typeof(StuckMenu));

                    Stop();
                }
                else
                {
                    m_Mobile.Frozen = true;
                }
            }
        }

        private static Dictionary<Mobile, TeleportTimer> m_Table = new Dictionary<Mobile, TeleportTimer>();

        public static bool IsTeleporting(Mobile m)
        {
            return m_Table.ContainsKey(m);
        }

        public class TeleportTimer : Timer
        {
            private Mobile m_Mobile;
            private Point3D m_TargetLocation;
            private Map m_TargetMap;
            private DateTime m_End;

            private bool m_Force;

            private Point3D m_OriginalLocation;
            private Map m_OriginalMap;

            public TeleportTimer(Mobile m, Point3D targetLoc, Map targetMap, TimeSpan delay, bool force)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                Priority = TimerPriority.TwoFiftyMS;

                m_Mobile = m;
                m_TargetLocation = targetLoc;
                m_TargetMap = targetMap;
                m_End = DateTime.UtcNow + delay;

                m_Force = force;

                m_OriginalLocation = m.Location;
                m_OriginalMap = m.Map;

                TeleportTimer concurrent;

                if (m_Table.TryGetValue(m, out concurrent))
                    concurrent.Stop();

                m_Table[m] = this;
            }

            protected override void OnTick()
            {
                if (!Running)
                    return; // we got a queued tick after we stopped the timer .. just ignore it

                if (DateTime.UtcNow < m_End)
                {
                    m_Mobile.Frozen = true;
                }
                else
                {
                    m_Mobile.Frozen = false;

                    Stop();
                    Flush();

                    m_Table.Remove(m_Mobile);

                    try
                    {
                        bool valid = (m_Mobile.Location == m_OriginalLocation && m_Mobile.Map == m_OriginalMap);

                        if (valid && !AllowUnstuck(m_Mobile))
                            valid = false;

                        if (valid && !IsHopeless(m_Mobile))
                            valid = false;

                        if (!m_Force && !valid)
                        {
                            m_Mobile.SendMessage("You do not appear to be in a valid stuck location. Use help-stuck again for more information.");
                        }
                        else
                        {
                            LogHelper log = new LogHelper("HelpStuck.log");
                            log.Log(LogType.Mobile, m_Mobile, String.Format("Moved stuck player to {0} ({1}) (T2A={2})", m_TargetLocation, m_TargetMap, Utility.World.LostLandsWrap.Contains(m_TargetLocation)));
                            log.Finish();

                            if (m_Mobile.Alive && m_Mobile.Region.IsDungeonRules)
                                m_Mobile.Kill();

                            BaseCreature.TeleportPets(m_Mobile, m_TargetLocation, m_TargetMap);
                            m_Mobile.MoveToWorld(m_TargetLocation, m_TargetMap);
                            m_Mobile.UsedStuckMenu();
                        }
                    }
                    catch (Exception exc)
                    {
                        LogHelper.LogException(exc);
                        Console.WriteLine("Exception caught in TeleportTimer.OnTick: " + exc.Message);
                        Console.WriteLine(exc.StackTrace.ToString());
                    }
                }
            }
        }
#if false
        public class RectifyZTimer : Timer
        {
            private Mobile m_Mobile;
            private Point3D m_TargetLocation;
            private Map m_TargetMap;
            private DateTime m_End;

            private bool m_Force;

            private Point3D m_OriginalLocation;
            private Map m_OriginalMap;

            public RectifyZTimer(Mobile m, Point3D targetLoc, Map targetMap, TimeSpan delay, bool force)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                Priority = TimerPriority.TwoFiftyMS;

                m_Mobile = m;
                m_TargetLocation = targetLoc;
                m_TargetMap = targetMap;
                m_End = DateTime.UtcNow + delay;

                m_Force = force;

                m_OriginalLocation = m.Location;
                m_OriginalMap = m.Map;

                TeleportTimer concurrent;

                if (m_Table.TryGetValue(m, out concurrent))
                    concurrent.Stop();

                m_Table[m] = this;
            }

            protected override void OnTick()
            {
                if (!Running)
                    return; // we got a queued tick after we stopped the timer .. just ignore it

                if (DateTime.UtcNow < m_End)
                {
                    m_Mobile.Frozen = true;
                }
                else
                {
                    m_Mobile.Frozen = false;

                    Stop();
                    Flush();

                    m_Table.Remove(m_Mobile);

                    try
                    {
                        bool valid = (m_Mobile.Location == m_OriginalLocation && m_Mobile.Map == m_OriginalMap);

                        if (valid && !AllowUnstuck(m_Mobile))
                            valid = false;

                        if (valid && !IsHopeless(m_Mobile))
                            valid = false;

                        if (!m_Force && !valid)
                        {
                            m_Mobile.SendMessage("You do not appear to be in a valid stuck location. Use help-stuck again for more information.");
                        }
                        else
                        {
                            LogHelper log = new LogHelper("HelpStuck.log");
                            log.Log(LogType.Mobile, m_Mobile, String.Format("Moved stuck player to {0} ({1}) (T2A={2})", m_TargetLocation, m_TargetMap, Utility.World.LostLandsWrap.Contains(m_TargetLocation)));
                            log.Finish();

                            if (m_Mobile.Alive && m_Mobile.Region.IsDungeonRules)
                                m_Mobile.Kill();

                            BaseCreature.TeleportPets(m_Mobile, m_TargetLocation, m_TargetMap);
                            m_Mobile.MoveToWorld(m_TargetLocation, m_TargetMap);
                            m_Mobile.UsedStuckMenu();
                        }
                    }
                    catch (Exception exc)
                    {
                        LogHelper.LogException(exc);
                        Console.WriteLine("Exception caught in TeleportTimer.OnTick: " + exc.Message);
                        Console.WriteLine(exc.StackTrace.ToString());
                    }
                }
            }
        }
#endif
        private static bool m_Testing;

        public static bool DoHelpStuck(Mobile from)
        {
            Utility.ConsoleWriteLine(String.Format("Player {0} is unstucking...", from), ConsoleColor.Magenta);

            if (Utility.BadZ(from) && !Utility.CheatingZ(from))
            {
                TeleportTimer tele = new TeleportTimer(from, new Point3D(from.X, from.Y, Utility.GoodZ(from)), from.Map, TimeSpan.FromSeconds(Utility.RandomMinMax(10, 60)), force: true);
                tele.Start();
                from.SendMessage("You will be moved sometime in the next minute.");
                return true;
            }

            if (!AllowUnstuck(from))
            {
                Utility.ConsoleWriteLine("We do not allow them to unstuck.", ConsoleColor.Magenta);
                return true;
            }

            if (TryUnblock(from, false))
            {
                Utility.ConsoleWriteLine("We unblocked them.", ConsoleColor.Magenta);

                if (!m_Testing)
                    from.UsedStuckMenu();

                return true;
            }

            List<object> exitOptions = new List<object>();

            GetExitOptions(from, exitOptions);

            if (exitOptions.Count != 0)
            {
                Utility.ConsoleWriteLine("Not hopeless, describing options.", ConsoleColor.Magenta);

                foreach (object o in exitOptions)
                    DescribeExitOption(from, o);

                // 5/19/23, Yoar: Let's not use up a stuck attempt here
                return true;
            }

            // we're probably in a quest zone
            if (from.Map != Map.Felucca)
            {
                Utility.ConsoleWriteLine("Probably a quest area. Too bad.", ConsoleColor.Magenta);
                from.SendMessage("You appear to be in a quest area. Sorry, there is no help stuck available here.");
                return true;
            }

            Utility.ConsoleWriteLine("Giving up, getting them some help.", ConsoleColor.Magenta);

            int staffOnline = 0;

            // 4/4/23, Adam: just give them the menu, currently I don't trust we will have enough staff to field requests
#if false
            foreach (NetState ns in NetState.Instances)
            {
                Mobile m = ns.Mobile;

                if (m != null && m.AccessLevel >= AccessLevel.Counselor && m.AutoPageNotify)
                    staffonline++;
            }
#endif

            if (staffOnline == 0)
            {
                StuckMenu menu = new StuckMenu(from, from, true);
                menu.BeginClose();
                from.SendGump(menu);
                return true;
            }

            return false;
        }

        public static bool AllowUnstuck(Mobile from)
        {
            if (!m_Testing && from.AccessLevel > AccessLevel.Player)
                return true;

            if (!from.CanUseStuckMenu())
                return false;

            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.HelpStuckDisabled))
            {
                from.SendMessage("Sorry, help stuck is unavailable at this time. Please page for staff assistance.");
                return false;
            }

            if (from.Region != null && (from.Region.IsAngelIslandRules || from.Region.IsJailRules))
            {
                if (from.Region.IsAngelIslandRules)
                    from.SendMessage("Nice try Louie.");
                else
                    from.SendLocalizedMessage(1041530, "", 0x23); // You'll need a better jailbreak plan then that!
                return false;
            }

            if (from.Region != null && from.Region.IsDungeonRules && CanMoveFreely(from))
            {
                from.SendMessage("But, you're not stuck.");
                return false;
            }

            if (from.Region != null && from.Region.IsAngelIslandRules)
            {
                from.SendLocalizedMessage(1041530, "", 0x23); // You'll need a better jailbreak plan then that!
                return false;
            }

            // 5/19/23, Yoar: Disabled this. Towns can have legit stuck locations.
#if false
            GuardedRegion guarded = from.Region.GetRegion(typeof(GuardedRegion)) as GuardedRegion;

            if (guarded != null && guarded.IsGuarded)
            {
                from.SendMessage("But you're in town!");
                return false;
            }
#endif

            if (from.Criminal)
            {
                from.SendLocalizedMessage(1005270, "", 0x23); // Thou'rt a criminal and cannot escape so easily...
                return false;
            }

            if (CheckCombat(from))
            {
                from.SendLocalizedMessage(1005271, "", 0x23); // Wouldst thou flee during the heat of battle??
                return false;
            }

            if (Factions.Sigil.ExistsOn(from))
            {
                from.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
                return false;
            }

            if (Engines.Alignment.TheFlag.ExistsOn(from))
            {
                from.SendMessage("You can't do that while carrying the flag.");
                return false;
            }

            if (from.TotalWeight >= MAXHELPSTUCKALIVEWEIGHT && !StrandedAtSea(from))
            {
                from.SendMessage("You are too encumbered to be moved, drop some of your stuff and try again.");
                return false;
            }

            if (HasPackie(from))
            {
                from.SendMessage("You cannot be transported because you have loaded pack animal!");
                return false;
            }

            // TODO: Handle this as an exit option?
            //  ya, we can issue the tillerman a command or two
            ShipKeyLocation shipKeyLocation = FindShipKey(from);

            if (shipKeyLocation != ShipKeyLocation.None)
            {
                switch (shipKeyLocation)
                {
                    case ShipKeyLocation.Hand: from.SendMessage("But the key is in your hands!"); break;
                    case ShipKeyLocation.Pack: from.SendMessage("But the key is in your backpack!"); break;
                    case ShipKeyLocation.Hold: from.SendMessage("But the key is in the hold!"); break;
                    case ShipKeyLocation.Deck: from.SendMessage("But the key is right there!"); break;
                }

                return false;
            }

            if (from.Region != null && !from.Region.CanUseStuckMenu(from))
                return false;

            return true;
        }
        private static bool StrandedAtSea(Mobile m)
        {
            if (BaseBoat.FindBoatAt(m, m.Map) == null && Utility.CanSpawnWaterMobile(m.Map, m.Location, Utility.CanFitFlags.none))
                return true;
            return false;
        }
        private static bool CanMoveFreely(Mobile m)
        {   // all dungeons have exit teles.
            if (m != null && m.Region != null && m.Region.IsDungeonRules)
                for (int ix = 0; ix < 10; ix++)
                {   // can they walk 8-10 tiles? If so, they are looking for a free ride
                    Point3D newLocation = Spawner.GetSpawnPosition(m.Map, m.Location, 10, SpawnFlags.SpawnFar, m);
                    if (newLocation != m.Location && m.GetDistanceToSqrt(newLocation) >= 8)
                        if (Spawner.ClearPathLand(m.Location, newLocation, map: m.Map, true))
                            return true;
                }
            return false;
        }

        private static bool CheckCombat(Mobile m)
        {
            for (int i = 0; i < m.Aggressed.Count; i++)
            {
                AggressorInfo info = m.Aggressed[i];

                if (DateTime.UtcNow - info.LastCombatTime < TimeSpan.FromSeconds(30.0))
                    return true;
            }

            return false;
        }

        private static bool HasPackie(Mobile m)
        {
            foreach (Mobile check in m.GetMobilesInRange(3))
            {
                BaseCreature bc = check as BaseCreature;

                if (bc == null)
                    continue;

                if (!bc.Controlled || bc.ControlMaster != m)
                    continue;

                if (bc.ControlOrder != OrderType.Come && bc.ControlOrder != OrderType.Follow && bc.ControlOrder != OrderType.Guard)
                    continue;

                if (!IsPackie(bc))
                    continue;

                if (bc.Backpack == null || bc.Backpack.Items.Count == 0)
                    continue;

                return true;
            }

            return false;
        }

        private static bool IsPackie(Mobile m)
        {
            return (m is PackHorse || m is PackLlama || m is Beetle || m is HordeMinion);
        }

        private enum ShipKeyLocation : byte
        {
            None,
            Hand,
            Pack,
            Hold,
            Deck,
        }

        private static ShipKeyLocation FindShipKey(Mobile from)
        {
            if (!from.Alive)
                return ShipKeyLocation.None;

            BaseBoat boat = BaseBoat.FindBoatAt(from);

            if (boat == null)
                return ShipKeyLocation.None;

            if (from.Holding is Key)
            {
                Key heldKey = (Key)from.Holding;

                if (heldKey.Link == boat)
                    return ShipKeyLocation.Hand;
            }

            if (from.Backpack != null)
            {
                foreach (Key packKey in from.Backpack.FindItemsByType<Key>())
                {
                    if (packKey.Link == boat)
                        return ShipKeyLocation.Pack;
                }
            }

            if (boat.Hold != null)
            {
                foreach (Key holdKey in boat.Hold.FindItemsByType<Key>())
                {
                    if (holdKey.Link == boat)
                        return ShipKeyLocation.Hold;
                }
            }

            if (boat.Map != null)
            {
                MultiComponentList mcl = boat.Components;

                foreach (Item item in boat.Map.GetItemsInBounds(new Rectangle2D(boat.X + mcl.Min.X, boat.Y + mcl.Min.Y, mcl.Width, mcl.Height)))
                {
                    if (boat.Contains(item) && item is Key)
                    {
                        Key deckKey = (Key)item;

                        if (deckKey.Link == boat)
                            return ShipKeyLocation.Deck;
                    }
                }
            }

            return ShipKeyLocation.None;
        }

        public static bool TryUnblock(Mobile from, bool force)
        {
            if (StrandedAtSea(from))
            {
                Strandedness.ProcessStranded(from, false);
                return true;
            }
            Point3D loc = FindUnblockLocation(from);

            if (loc != Point3D.Zero)
            {
                if (m_Testing)
                {
                    BaseCreature.TeleportPets(from, loc, from.Map);
                    from.MoveToWorld(loc, from.Map);
                    return true;
                }

                TeleportTimer tele = new TeleportTimer(from, loc, from.Map, TimeSpan.FromSeconds(Utility.RandomMinMax(10, 60)), force);
                tele.Start();
                from.SendMessage("You will be moved sometime in the next minute.");
                return true;
            }

            return false;
        }

        public const int UnblockRange = 4;

        private static Point3D FindUnblockLocation(Mobile from)
        {
            Map map = from.Map;

            if (map == null)
                return Point3D.Zero; // sanity

            HouseRegion hr = from.Region.GetRegion(typeof(HouseRegion)) as HouseRegion;

            if (hr != null && hr.House != null && hr.House.IsInside(from))
                return hr.GoLocation;

            TownshipRegion tsr = from.Region.GetRegion(typeof(TownshipRegion)) as TownshipRegion;

            if (tsr != null)
            {
                List<Point3D> edgeLocations = new List<Point3D>();

                if (tsr.Coords.Count != 0)
                    Utility.CalcRegionBounds(tsr, ref edgeLocations);

                Utility.Shuffle(edgeLocations);

                for (int i = 0; i < edgeLocations.Count; i++)
                {
                    Point3D edgeLocation = edgeLocations[i];

                    if (tsr.Map.CanSpawnLandMobile(edgeLocation))
                        return edgeLocation;
                }
            }

            if (from.IsBlocked(UnblockRange))
            {
                for (int i = 0; i < 8; i++)
                {
                    Point3D edgeLoc = GetPointInDirection(from.Location, (Direction)i, UnblockRange);

                    bool isValid = Utility.CanSpawnLandMobile(map, edgeLoc);

                    if (!isValid)
                    {
                        edgeLoc.Z = map.GetAverageZ(edgeLoc.X, edgeLoc.Y);

                        isValid = Utility.CanSpawnLandMobile(map, edgeLoc);
                    }

                    // Yoar: make sure we *cannot* path there, else there would be no point in TPing us there
                    if (isValid && !CanGetThere(from.Location, edgeLoc, from))
                        return edgeLoc;
                }

                Point3D spawnLoc = Spawner.GetSpawnPosition(map, from.Location, UnblockRange, SpawnFlags.None, from);

                // Yoar: make sure we *cannot* path there, else there would be no point in TPing us there
                if (spawnLoc != Point3D.Zero && spawnLoc != from.Location && !CanGetThere(from.Location, spawnLoc, from))
                    return spawnLoc;
            }

            return Point3D.Zero;
        }

        private static Point3D GetPointInDirection(Point3D loc, Direction dir, int range)
        {
            int dx = 0;
            int dy = 0;
            Movement.Movement.Offset(dir, ref dx, ref dy);

            return new Point3D(loc.X + dx * range, loc.Y + dy * range, loc.Z);
        }

        public static bool IsHopeless(Mobile from)
        {
            List<object> exitOptions = new List<object>();

            GetExitOptions(from, exitOptions);

            return (exitOptions.Count == 0);
        }

        public static void GetExitOptions(Mobile from, List<object> exitOptions)
        {
            Map map = from.Map;

            if (map == null || map == Map.Internal)
                return;

            Rectangle2D rect = new Rectangle2D(from.X - 128 / 2, from.Y - 128 / 2, 128, 128);

            foreach (object o in from.Map.GetObjectsInBounds(rect))
            {
                if (!from.Alive && !from.Red && o is BaseHealer healer)
                {
                    if (CanGetThere(from.Location, healer.Location, from))
                        exitOptions.Add(healer);
                }
                else if (!from.Alive && o is EvilWanderingHealer evilHealer)
                {
                    if (CanGetThere(from.Location, evilHealer.Location, from))
                        exitOptions.Add(evilHealer);
                }
                else if (o is PublicMoongate public_moongate)
                {
                    if (CanGetThere(from.Location, public_moongate.Location, from) && public_moongate.Visible)
                        exitOptions.Add(public_moongate);
                }
                else if (o is Moongate moongate)
                {
                    if (CanGetThere(from.Location, moongate.Location, from) && moongate.Visible && !Utility.IsDungeon(moongate.PointDest))
                        exitOptions.Add(moongate);
                }
                else if (o is Sungate sungate && sungate.Running && !Utility.IsDungeon(sungate.PointDest))
                {
                    if (CanGetThere(from.Location, sungate.Location, from))
                        exitOptions.Add(sungate);
                }
                else if (o is Teleporter tele && !(o is HouseTeleporter) && tele.Running && !Utility.IsDungeon(tele.PointDest))
                {
                    if (CanGetThere(from.Location, tele.Location, from))
                        exitOptions.Add(tele);
                }
            }
        }

        public static void DescribeExitOption(Mobile from, object o)
        {
            IPoint3D p = o as IPoint3D;

            if (p == null)
                return; // sanity

            string name;

            if (o is BaseHealer)
                name = "a healer";
            else if (o is Moongate || o is Sungate)
                name = "a moongate";
            else if (o is Teleporter)
                name = "a teleporter";
            else
                name = "an exit";

            Direction dir = from.GetDirectionTo(p);

            string dirStr = dir.ToString().ToLower();

            switch (dir)
            {
                case Direction.Right: dirStr = "north-east"; break;
                case Direction.Down: dirStr = "south-east"; break;
                case Direction.Left: dirStr = "south-west"; break;
                case Direction.Up: dirStr = "north-west"; break;
            }

            double distance = from.GetDistanceToSqrt(p);

            string format;

            if (distance > 200)
                format = "There's {0} a long journey {1} from here.";
            else if (distance > 150)
                format = "There's {0} quite a long {1} distance from here.";
            else if (distance > 100)
                format = "There's {0} a long way {1} from here.";
            else if (distance > 75)
                format = "There's {0} a fair distance {1} from there.";
            else if (distance > 50)
                format = "There's {0} quite a ways {1} from here.";
            else if (distance > 40)
                format = "There's {0} just a short way {1} from here.";
            else if (distance > 20)
                format = "There's {0} but a few steps {1} from here.";
            else
                format = "There's {0} is nearby!";

            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, String.Format(format, name, dirStr));
        }

        private static bool CanGetThere(Point3D here, Point3D there, Mobile from)
        {
            Movement.MovementObject obj_start = new Movement.MovementObject(here, there, from.Map);

            return MovementPath.PathTo(obj_start);
        }
    }
}