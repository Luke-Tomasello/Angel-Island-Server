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
 * Detective / Forensic Lead Tracking
 *
 * Tracking is the convergence point for the Detective profession.
 *
 * The Detective system is intentionally split across three cooperating
 * components:
 *
 *  1. Forensic Evaluation
 *     Extracts facts from a target, especially corpses. It produces
 *     ForensicInfo but does not decide how that information is later used.
 *
 *  2. ForensicNotebook
 *     Stores forensic observations and active leads. It owns evidence
 *     lifetime, packing/expiry, suspect aggregation, and lead-strength
 *     scoring.
 *
 *  3. Tracking
 *     Consumes notebook leads and turns them into pursuit gameplay.
 *
 * Normal Tracking remains proximity-based:
 *
 *     Search range -> nearby candidates -> selected target -> quest arrow
 *
 * Forensic lead tracking is evidence-based:
 *
 *     Corpse -> Forensics -> Notebook -> Forensic Leads -> suspect pursuit
 *
 * A forensic lead is not meant to act as magical GPS. Tracking should not
 * reveal exact coordinates or online/offline status. Instead, it interprets
 * the evidence in the notebook and produces one of three outcomes:
 *
 *  1. Active trail
 *     If the suspect is within Arrow range, the detective picks up the trail
 *     and receives a quest arrow.
 *
 *  2. Directional hint
 *     If the suspect is outside Arrow range but within Hint range, the
 *     detective receives vague directional/distance information.
 *
 *  3. Cold trail
 *     If the suspect is beyond Hint range, on another map, unavailable, or
 *     otherwise unreachable, the detective receives only a failure/cold-trail
 *     message. The specific reason is intentionally not disclosed.
 *
 * Range terminology:
 *
 *  Search:
 *      The ordinary candidate-discovery radius used by normal Tracking.
 *
 *  Arrow:
 *      The pursuit radius within which a selected target can be followed by
 *      quest arrow.
 *
 *  Hint:
 *      The extended forensic radius within which evidence can still produce
 *      directional clues, but not an arrow.
 *
 * GetTrackingRanges(from) returns the baseline ranges derived from Tracking
 * skill, ruleset modifiers, and CoreAI tuning values.
 *
 * GetTrackingRanges(from, suspect) layers forensic evidence strength on top
 * of the baseline ranges. This overload is used only after a forensic suspect
 * has been selected, because evidence strength is suspect-specific.
 *
 * Evidence strength is owned by ForensicNotebook, not Tracking. Tracking asks
 * the notebook how strong the lead is; it does not inspect notebook entries or
 * score forensic evidence directly.
 *
 * Design rule:
 *
 *     Forensics identifies.
 *     The notebook remembers.
 *     Tracking pursues.
 *
 * The goal is to create investigation and manhunt gameplay, not a location
 * service.
 */

/*
 * Scripts/Skills/Tracking.cs
 *	ChangeLog
 *  6/3/2026, Adam
 *      Added Detective / Forensic Lead Tracking system
 *  5/9/23, Yoar
 *      Restored passive tracking gain
 *      Disabled pre-Pub16 tracking bug
 * 7/9/10, Pix
 *      Fixed special case where tracker's skill is < 20 and trackee's hiding is >= 80
 *	5/29/10, adam
 *		triple tracking range.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	9/19/04, Adam
 *		Return tracking to it's original distance.
 *	9/11/04, Pix
 *		Doubled tracking range.
 *	7/21/04, Pixie
 *		Changed tracking to be tracking vs stealth.
 *	7/11/04, Pixie
 *		Fixed my poor arithmetic ...
 *	7/7/04, Pixie
 *		Changed to use the Value of Tracking/Hiding/Stealth instead of the Base
 *	7/4/04, Pixie
 *		Changed the chance to track vs hiding/stealth/visible
 *	4/7/04, changes by mith
 *		TrackWhoGump.DisplayTo() code modified to include target's stealth.
 *	3/16/04, changes by mith 
 *		TrackWhoGump.DisplayTo() code to compare tracking vs. target's hiding.
 *	last modified 3/16/04 by mith.
*/

using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections;
using System.Linq;

namespace Server.SkillHandlers
{
    public class Tracking
    {

        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Tracking].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.SendLocalizedMessage(1011350); // What do you wish to track?

            m.CloseGump(typeof(TrackWhatGump));
            m.CloseGump(typeof(TrackWhoGump));
            m.SendGump(new TrackWhatGump(m));

            return TimeSpan.FromSeconds(10.0); // 10 second delay before beign able to re-use a skill
        }
    }

    public class TrackWhatGump : Gump
    {
        private Mobile m_From;
        private bool m_Success;
        private bool m_HasForensicLeads;

        public TrackWhatGump(Mobile from)
            : base(20, 30)
        {
            m_From = from;
            m_Success = from.CheckSkill(SkillName.Tracking, 0, 21.1, contextObj: new object[2]);
            ForensicNotebook? notebook = ForensicNotebook.Find(from);
            m_HasForensicLeads = notebook != null && notebook.GetActiveSuspects().Any();

            AddPage(0);

            int height = m_HasForensicLeads ? 270 : 135;
            AddBackground(0, 0, 440, height, 5054);
            //AddBackground(0, 0, 440, 135, 5054);

            AddBackground(10, 10, 420, 75, 2620);
            AddBackground(10, 85, 420, 25, 3000);

            AddItem(20, 20, 9682);
            AddButton(20, 110, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(20, 90, 100, 20, 1018087, false, false); // Animals

            AddItem(120, 20, 9607);
            AddButton(120, 110, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtmlLocalized(120, 90, 100, 20, 1018088, false, false); // Monsters

            AddItem(220, 20, 8454);
            AddButton(220, 110, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddHtmlLocalized(220, 90, 100, 20, 1018089, false, false); // Human NPCs

            AddItem(320, 20, 8455);
            AddButton(320, 110, 4005, 4007, 4, GumpButtonType.Reply, 0);
            AddHtmlLocalized(320, 90, 100, 20, 1018090, false, false); // Players

            if (m_HasForensicLeads)
            {
                AddBackground(10, 145, 420, 75, 2620);
                AddBackground(10, 220, 420, 25, 3000);

                AddItem(20, 155, 0x2252);
                AddButton(20, 245, 4005, 4007, 5, GumpButtonType.Reply, 0);
                AddHtml(20, 225, 140, 20, "Forensic Leads", false, false);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID >= 1 && info.ButtonID <= 4)
            {
                TrackWhoGump.DisplayTo(m_Success, m_From, info.ButtonID - 1);
            }
            else if (info.ButtonID == 5 && m_HasForensicLeads)
            {
                TrackWhoGump.DisplayForensicLeadsTo(m_Success, m_From);
            }
        }
    }

    public delegate bool TrackTypeDelegate(Mobile m);

    public partial class TrackWhoGump : Gump
    {
        private Mobile m_From;
        private int m_Range;
        private TrackingRanges m_Ranges;
        private bool m_ForensicLeads;
        private ArrayList m_List;

        private static TrackTypeDelegate[] m_Delegates = new TrackTypeDelegate[]
            {
                new TrackTypeDelegate( IsAnimal ),
                new TrackTypeDelegate( IsMonster ),
                new TrackTypeDelegate( IsHumanNPC ),
                new TrackTypeDelegate( IsPlayer )
            };

        private class InternalSorter : IComparer
        {
            private Mobile m_From;

            public InternalSorter(Mobile from)
            {
                m_From = from;
            }

            public int Compare(object? x, object? y)
            {
                if (x == null && y == null)
                    return 0;
                else if (x == null)
                    return -1;
                else if (y == null)
                    return 1;

                Mobile? a = x as Mobile;
                Mobile? b = y as Mobile;

                if (a == null || b == null)
                    throw new ArgumentException();

                return m_From.GetDistanceToSqrt(a).CompareTo(m_From.GetDistanceToSqrt(b));
            }
        }

        public static void DisplayTo(bool success, Mobile from, int type)
        {
            if (!success)
            {
                from.SendLocalizedMessage(1018092); // You see no evidence of those in the area.
                return;
            }

            Map map = from.Map;

            if (map == null)
                return;

            TrackTypeDelegate check = m_Delegates[type];

            from.CheckSkill(SkillName.Tracking, 21.1, 100.0, contextObj: new object[2]); // Passive gain

            TrackingRanges ranges = GetTrackingRanges(from);
            int range = ranges.Search;

            if (range < CoreAI.TrackRangeMin)
                range = CoreAI.TrackRangeMin;

            // Adam: 5/29/10 - triple tracking range.
            // Adam: 9/19/04 - Return tracking to it's original distance.
            // Pixie: 9/11/04 - increase tracking range (double it)
            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisStyleRules())
                range *= 3;

            ArrayList list = new ArrayList();

            IPooledEnumerable eable = from.GetMobilesInRange(range);
            foreach (Mobile m in eable)
            {
                if (Core.RuleSets.SiegeRules())
                {
                    #region NEW SIEGE CODE
                    // Ghosts can no longer be tracked 
                    if (m != from && (!Core.RuleSets.AOSRules() || m.Alive) && (!m.Hidden || m.AccessLevel == AccessLevel.Player || from.AccessLevel > m.AccessLevel) && check(m) && CheckDifficulty(from, m))
                        list.Add(m);
                    #endregion
                }
                else
                {
                    #region OLD AI CODE
                    if (m != from && (!m.Hidden || m.AccessLevel == AccessLevel.Player || from.AccessLevel > m.AccessLevel) && check(m))
                    {
                        // At this point, we have a list of all mobiles in range, whether hidden or not.
                        // Now we check to see if the current mobile, m, is a player.
                        if (!m.Player)
                            list.Add(m);
                        else
                        {
                            bool TRACKINGSKILLDEBUG = (from.AccessLevel > AccessLevel.Player);

                            double Tracking = from.Skills[SkillName.Tracking].Value;
                            double Forensics = from.Skills[SkillName.Forensics].Value;
                            double Hiding = m.Skills[SkillName.Hiding].Value;
                            double Stealth = m.Skills[SkillName.Stealth].Value;

                            double trackersSkill = Tracking;
                            double targetSkill = Stealth;

                            double chance = 0;
                            if (targetSkill == 0) // if target's stealth is 0, start with a base of 100%
                            {
                                chance = 1;
                            }
                            else // chance is tracking/stealth*2 (giving 50% at equal levels)
                            {
                                chance = trackersSkill / (targetSkill * 2);
                            }

                            //Special case - if tracking is < 20 and hiding >= 80,
                            // make sure there's difficulty in tracking
                            if (from.Skills[SkillName.Tracking].Base < 20.0
                                && m.Skills[SkillName.Hiding].Base >= 80.0)
                            {
                                if (TRACKINGSKILLDEBUG)
                                {
                                    from.SendMessage(string.Format("Changing chance to track {0} from {1} because tracker is below 20.0 base tracking", m.Name, chance));
                                }

                                chance = 0;
                            }

                            //if tracker can see the other, it's much
                            //easier to track them
                            if (from.CanSee(m))
                            {
                                if (TRACKINGSKILLDEBUG)
                                {
                                    from.SendMessage(string.Format("Changing chance to track {0} from {1} because he can be seen", m.Name, chance));
                                }

                                double newchance = Tracking / 25.0; //25 tracking == 100% success when visible
                                if (newchance > chance) // make sure we're not killing their chances
                                {
                                    chance = newchance;
                                }
                            }

                            // add bonus for fonensics (10% at GM forensics)
                            chance += Forensics / 1000;

                            // make sure there's always a small chance to
                            // succeed and a small chance to fail
                            if (chance <= 0)
                            {
                                chance = 0.001; // minimum .1% (1/1000) chance
                            }
                            if (chance >= 1.0)
                            {
                                chance = 0.999; // maximum 99.9% chance
                            }

                            if (TRACKINGSKILLDEBUG)
                            {
                                from.SendMessage(
                                    string.Format("Your chance to track {0} is {1}.  T:{2:0.00} F:{3:0.00} S:{4:0.00} H{5:0.00}",
                                    m.Name, chance, Tracking, Forensics, Stealth, Hiding)
                                    );
                            }

                            // Check Skill takes two arguments, the skill to check, and the chance to succeed.
                            bool succeeded = from.CheckSkill(SkillName.Tracking, chance, contextObj: new object[2]);

                            // If our skill check succeeds, add the mobile to the list.
                            if (succeeded)
                            {
                                //Can't track > Player level
                                if (m.AccessLevel <= AccessLevel.Player)
                                {
                                    list.Add(m);
                                }
                            }
                        }
                    }

                    #endregion
                }
            }
            eable.Free();

            if (list.Count > 0)
            {
                list.Sort(new InternalSorter(from));

                from.SendGump(new TrackWhoGump(from, list, ranges));
                from.SendLocalizedMessage(1018093); // Select the one you would like to track.
            }
            else
            {
                if (type == 0)
                    from.SendLocalizedMessage(502991); // You see no evidence of animals in the area.
                else if (type == 1)
                    from.SendLocalizedMessage(502993); // You see no evidence of creatures in the area.
                else
                    from.SendLocalizedMessage(502995); // You see no evidence of people in the area.
            }
        }

        /*
         * Determines whether a tracker can successfully track a player.
         *
         * Non-player creatures are always trackable.
         *
         * Player tracking is an opposed skill check:
         *
         *      Tracker:
         *          Tracking
         *          Detect Hidden
         *          Forensics (AI/Mortalis bonus)
         *
         *      Target:
         *          Hiding
         *          Stealth
         *
         * Higher Tracking and Detect Hidden increase the chance of acquiring
         * the target, while Hiding and Stealth make the target more difficult
         * to track.
         *
         * On Angel Island / Mortalis-style shards, Forensics provides an
         * additional bonus of up to +10% at Grandmaster.
         *
         * Historically, pre-Publish 16 shards contained a tracking bug that
         * granted automatic success once Tracking exceeded 20.0. That behavior
         * is preserved only on shards explicitly configured to emulate it.
         */
        private static bool CheckDifficulty(Mobile tracker, Mobile target)
        {
            // Non-player mobiles are always trackable.
            if (!target.Player)
                return true;

            int tracking = tracker.Skills[SkillName.Tracking].Fixed;
            int detectHidden = tracker.Skills[SkillName.DetectHidden].Fixed;
            int forensics = (int)tracker.Skills[SkillName.Forensics].Value;

            int hiding = target.Skills[SkillName.Hiding].Fixed;
            int stealth = target.Skills[SkillName.Stealth].Fixed;

            int targetDifficulty = hiding + stealth;

            // Legacy pre-Publish 16 tracking bug:
            // Tracking > 20.0 resulted in automatic success.
            bool useLegacyBug =
                !CoreAI.TrackDifficulty &&
                !Core.RuleSets.MortalisStyleRules() &&
                !Core.RuleSets.AngelIslandRules() &&
                PublishInfo.Publish < 16.0 &&
                tracker.Skills[SkillName.Tracking].Value >= 20.1;

            int chanceToTrack;

            if (useLegacyBug)
            {
                chanceToTrack = 100;
            }
            else if (targetDifficulty > 0)
            {
                if (Core.RuleSets.SamuraiEmpireRules())
                {
                    chanceToTrack =
                        50 * (tracking * 2 + detectHidden) / targetDifficulty;
                }
                else
                {
                    int randomBonus = 10 * Utility.RandomMinMax(1, 20);

                    chanceToTrack =
                        50 * (tracking + detectHidden + randomBonus) / targetDifficulty;
                }

                // Angel Island / Mortalis bonus:
                // +10% at GM Forensics.
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisStyleRules())
                    chanceToTrack += forensics / 10;
            }
            else
            {
                // Target has neither Hiding nor Stealth.
                chanceToTrack = 100;
            }

            return chanceToTrack > Utility.Random(100);
        }
        private static bool IsAnimal(Mobile m)
        {
            return (!m.Player && m.Body.IsAnimal);
        }

        private static bool IsMonster(Mobile m)
        {
            return (!m.Player && m.Body.IsMonster);
        }

        private static bool IsHumanNPC(Mobile m)
        {
            return (!m.Player && m.Body.IsHuman);
        }

        private static bool IsPlayer(Mobile m)
        {
            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisStyleRules() || PublishInfo.Publish >= 13)
                return m.Player;
            else
            {   // publish 13 bug
                // Update 2
                // On September 12, 2001, the following was published:
                // Players wearing savage paint will now be trackable using the tracking skill.
                return m.Player && (m is Mobiles.PlayerMobile) && (m as Mobiles.PlayerMobile).SavagePaintExpiration == TimeSpan.Zero;
            }
        }

        private TrackWhoGump(Mobile from, ArrayList list, TrackingRanges ranges)
            : this(from, list, ranges, false)
        {
        }

        private TrackWhoGump(Mobile from, ArrayList list, TrackingRanges ranges, bool forensicLeads)
            : base(20, 30)
        {
            m_From = from;
            m_List = list;
            m_Ranges = ranges;
            m_Range = ranges.Search; // legacy/display compatibility
            m_ForensicLeads = forensicLeads;

            AddPage(0);

            AddBackground(0, 0, 440, 155, 5054);

            AddBackground(10, 10, 420, 75, 2620);
            AddBackground(10, 85, 420, 45, 3000);

            if (list.Count > 4)
            {
                AddBackground(0, 155, 440, 155, 5054);

                AddBackground(10, 165, 420, 75, 2620);
                AddBackground(10, 240, 420, 45, 3000);

                if (list.Count > 8)
                {
                    AddBackground(0, 310, 440, 155, 5054);

                    AddBackground(10, 320, 420, 75, 2620);
                    AddBackground(10, 395, 420, 45, 3000);
                }
            }

            for (int i = 0; i < list.Count && i < 12; ++i)
            {
                Mobile? m = (Mobile?)list[i];

                AddItem(20 + ((i % 4) * 100), 20 + ((i / 4) * 155), ShrinkTable.Lookup(m));
                AddButton(20 + ((i % 4) * 100), 130 + ((i / 4) * 155), 4005, 4007, i + 1, GumpButtonType.Reply, 0);

                if (m.Name != null)
                    AddHtml(20 + ((i % 4) * 100), 90 + ((i / 4) * 155), 90, 40, m.Name, false, false);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            int index = info.ButtonID - 1;

            if (index >= 0 && index < m_List.Count && index < 12)
            {
                Mobile? m = (Mobile?)m_List[index];

                if (m_ForensicLeads)
                {
                    TrackingRanges ranges = GetTrackingRanges(m_From, m);
                    TrackForensicLead(m_From, m, ranges);
                    return;
                }

                m_From.QuestArrow = new TrackArrow(m_From, m, m_Ranges.Arrow);
            }
        }
        private static void TrackForensicLead(Mobile from, Mobile? suspect, TrackingRanges ranges)
        {
            if (from == null || suspect == null || suspect.Deleted)
                return;

            if (from.Map == null || suspect.Map == null || from.Map != suspect.Map)
            {
                from.SendMessage("You are unable to establish a trail.");
                return;
            }

            DungeonTransition transition;
            bool trackEntrance;

            if (IsDungeonBoundaryTransition(from, suspect, out transition, out trackEntrance))
            {
                Point3D point;

                if (TryGetDungeonTransitionPoint(from, transition, trackEntrance, out point))
                {
                    if (Core.Debug)
                    {
                        //from.SendMessage(
                        //    "Dungeon tracking: {0} trail for {1} via {2} at {3}.",
                        //    trackEntrance ? "entrance" : "exit",
                        //    suspect.Name,
                        //    transition.Name,
                        //    point);
                    }

                    TrackDungeonTransitionPoint(from, suspect, point, from.Map, ranges, transition, trackEntrance);
                    return;
                }
            }

            double distance = from.GetDistanceToSqrt(suspect);

            if (distance <= ranges.Arrow)
            {
                from.SendMessage("You pick up {0}'s trail.", suspect.Name);
                from.QuestArrow = new TrackArrow(from, suspect, ranges.Arrow);
            }
            else if (distance <= ranges.Hint)
            {
                GiveForensicDirectionalHint(from, suspect);
            }
            else
            {
                from.SendMessage("The trail has gone cold.");
            }
        }
        private static void GiveForensicDirectionalHint(Mobile from, Mobile suspect)
        {
            string direction = from.GetDirectionTo(suspect.Location).ToString();

            if (direction == "Left")
                direction = "South-West";
            else if (direction == "Right")
                direction = "North-East";
            else if (direction == "Up")
                direction = "North-West";
            else if (direction == "Down")
                direction = "South-East";
            else if (direction == "Mask")
                direction = "North-West";

            double distance = from.GetDistanceToSqrt(suspect);

            if (distance > 150)
                from.SendMessage("Your notes suggest {0} is quite a long distance to the {1}.", suspect.Name, direction);
            else if (distance > 100)
                from.SendMessage("Your notes suggest {0} is a long way to the {1}.", suspect.Name, direction);
            else if (distance > 75)
                from.SendMessage("Your notes suggest {0} is a fair distance to the {1}.", suspect.Name, direction);
            else if (distance > 50)
                from.SendMessage("Your notes suggest {0} is quite a ways to the {1}.", suspect.Name, direction);
            else if (distance > 40)
                from.SendMessage("Your notes suggest {0} is just a short way to the {1}.", suspect.Name, direction);
            else if (distance > 20)
                from.SendMessage("Your notes suggest {0} is but a few steps to the {1}.", suspect.Name, direction);
            else
                from.SendMessage("You pick up {0}'s trail.", suspect.Name);
        }
        private static void GiveForensicDirectionalHint(Mobile from, Mobile suspect, Point3D point)
        {
            string direction = from.GetDirectionTo(point).ToString();

            if (direction == "Left")
                direction = "South-West";
            else if (direction == "Right")
                direction = "North-East";
            else if (direction == "Up")
                direction = "North-West";
            else if (direction == "Down")
                direction = "South-East";
            else if (direction == "Mask")
                direction = "North-West";

            double distance = from.GetDistanceToSqrt(point);

            if (distance > 150)
                from.SendMessage("Your notes suggest {0}'s trail leads quite a long distance to the {1}.", suspect.Name, direction);
            else if (distance > 100)
                from.SendMessage("Your notes suggest {0}'s trail leads a long way to the {1}.", suspect.Name, direction);
            else if (distance > 75)
                from.SendMessage("Your notes suggest {0}'s trail leads a fair distance to the {1}.", suspect.Name, direction);
            else if (distance > 50)
                from.SendMessage("Your notes suggest {0}'s trail leads quite a ways to the {1}.", suspect.Name, direction);
            else if (distance > 40)
                from.SendMessage("Your notes suggest {0}'s trail leads just a short way to the {1}.", suspect.Name, direction);
            else if (distance > 20)
                from.SendMessage("Your notes suggest {0}'s trail leads but a few steps to the {1}.", suspect.Name, direction);
            else
                from.SendMessage("You pick up {0}'s trail.", suspect.Name);
        }
        public static void DisplayForensicLeadsTo(bool success, Mobile from)
        {
            if (!success)
            {
                from.SendLocalizedMessage(1018092); // You see no evidence of those in the area.
                return;
            }

            ForensicNotebook? notebook = ForensicNotebook.Find(from);

            if (notebook == null)
            {
                from.SendMessage("You have no forensic notebook.");
                return;
            }

            from.CheckSkill(SkillName.Tracking, 21.1, 100.0, contextObj: new object[2]); // Passive gain

            ArrayList list = new ArrayList();

            foreach (Mobile suspect in notebook.GetActiveSuspects())
            {
                if (suspect == null || suspect.Deleted)
                    continue;

                if (!Core.Debug && suspect == from)
                    continue;

                list.Add(suspect);
            }

            if (list.Count == 0)
            {
                from.SendMessage("Your notebook contains no fresh forensic leads.");
                return;
            }

            list.Sort(new InternalSorter(from));

            TrackingRanges ranges = GetTrackingRanges(from);

            from.SendGump(new TrackWhoGump(from, list, ranges, true));
            from.SendMessage("Select the forensic lead you would like to pursue.");
        }
        /*
         * Tracking Range Model
         *
         * Search Range:
         *   Base tracking distance is:
         *
         *      10 + (Tracking Skill × 0.1)
         *
         *   Examples:
         *      0.0 Tracking   = 10 tiles
         *      50.0 Tracking  = 15 tiles
         *      100.0 Tracking = 20 tiles
         *
         *   On Angel Island / Mortalis-style rulesets (including Seasons),
         *   the search range is tripled:
         *
         *      GM Tracking = 60 tiles
         *
         * Arrow Range:
         *   When tracking a suspect, a quest arrow is provided if the suspect
         *   is within:
         *
         *      Search Range × 2
         *
         *   Example:
         *      GM Tracking = 60 tile search range
         *      Arrow Range = 120 tiles
         *
         * Hint Range:
         *   Directional hints ("north", "southwest", etc.) are provided if the
         *   suspect is within:
         *
         *      Search Range × 3
         *
         *   Example:
         *      GM Tracking = 60 tile search range
         *      Hint Range = 180 tiles
         *
         * Forensic Evidence:
         *   Forensic Notebook evidence may further increase Arrow and Hint
         *   ranges based on the strength of the evidence collected against
         *   the suspect. Search Range itself is unaffected.
         */
        private struct TrackingRanges
        {
            public int Search;
            public int Arrow;
            public int Hint;
        }

        private static TrackingRanges GetTrackingRanges(Mobile from)
        {
            int search = GetTrackingSearchRange(from);

            TrackingRanges ranges = new TrackingRanges();

            ranges.Search = search;
            ranges.Arrow = (int)(search * CoreAI.TrackArrowRangeMultiplier);
            ranges.Hint = (int)(search * CoreAI.TrackHintRangeMultiplier);

            return ranges;
        }

        private static int GetTrackingSearchRange(Mobile from)
        {
            double drange = CoreAI.TrackRangeBase;

            drange += CoreAI.TrackRangePerSkill * from.Skills[SkillName.Tracking].Value;

            int range = (int)drange;

            if (range < CoreAI.TrackRangeMin)
                range = CoreAI.TrackRangeMin;

            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisStyleRules())
                range *= 3;

            return range;
        }

        private static TrackingRanges GetTrackingRanges(Mobile from, Mobile? suspect)
        {
            TrackingRanges ranges = GetTrackingRanges(from);

            double multiplier = GetEvidentiaryStrengthMultiplier(from, suspect);

            ranges.Arrow = (int)(ranges.Arrow * multiplier);
            ranges.Hint = (int)(ranges.Hint * multiplier);

            return ranges;
        }

        private static double GetEvidentiaryStrengthMultiplier(Mobile from, Mobile? suspect)
        {
            ForensicNotebook? notebook = ForensicNotebook.FindBest(from, suspect);

            if (notebook == null)
                return 1.0;

            return notebook.GetLeadStrengthMultiplier(suspect);
        }
    }
    public class TrackPointArrow : QuestArrow
    {
        private Mobile? m_From;
        private Timer m_Timer;

        public TrackPointArrow(Mobile from, Point3D target, Map map, int range)
            : base(from)
        {
            m_From = from;
            m_Timer = new TrackPointTimer(from, target, map, range, this);
            m_Timer.Start();
        }

        public override void OnClick(bool rightClick)
        {
            if (rightClick)
            {
                m_From = null;
                Stop();
            }
        }

        public override void OnStop()
        {
            m_Timer.Stop();
        }
    }
    public class TrackArrow : QuestArrow
    {
        private Mobile? m_From;
        private Timer m_Timer;

        public TrackArrow(Mobile from, Mobile target, int range)
            : base(from)
        {
            m_From = from;
            m_Timer = new TrackTimer(from, target, range, this);
            m_Timer.Start();
        }

        public override void OnClick(bool rightClick)
        {
            if (rightClick)
            {
                m_From = null;

                Stop();
            }
        }

        public override void OnStop()
        {
            m_Timer.Stop();

            // 6/3/2026, Adam: we start/stop arrows, and shouldn't always get this message
            //  If specific messaging needs to happen, it needs to happen outsice this call
            //if (m_From != null)
            //    m_From.SendLocalizedMessage(503177); // You have lost your quarry.
        }
    }
    public class TrackPointTimer : Timer
    {
        private Mobile m_From;
        private Point3D m_Target;
        private Map m_Map;
        private int m_Range;
        private QuestArrow m_Arrow;

        public TrackPointTimer(Mobile from, Point3D target, Map map, int range, QuestArrow arrow)
            : base(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(2.5))
        {
            m_From = from;
            m_Target = target;
            m_Map = map;
            m_Range = range;
            m_Arrow = arrow;
        }

        protected override void OnTick()
        {
            if (!m_Arrow.Running)
            {
                Stop();
                return;
            }

            if (m_From == null || m_From.NetState == null || m_From.Deleted || m_From.Map != m_Map)
            {
                if (m_From != null && m_From.NetState != null)
                    m_From.Send(new CancelArrow());

                Stop();
                return;
            }

            if (!m_From.InRange(m_Target, m_Range))
            {
                m_From.Send(new CancelArrow());
                m_From.SendLocalizedMessage(503177); // You have lost your quarry.

                Stop();
                return;
            }

            m_Arrow.Update(m_Target.X, m_Target.Y);
        }
    }
    public class TrackTimer : Timer
    {
        private Mobile m_From, m_Target;
        private int m_Range;
        private int m_LastX, m_LastY;
        private QuestArrow m_Arrow;

        public TrackTimer(Mobile from, Mobile target, int range, QuestArrow arrow)
            : base(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(2.5))
        {
            m_From = from;
            m_Target = target;
            m_Range = range;

            m_Arrow = arrow;
        }

        protected override void OnTick()
        {
            if (!m_Arrow.Running)
            {
                Stop();
                return;
            }
            else if (m_From.NetState == null || m_From.Deleted || m_Target.Deleted || m_From.Map != m_Target.Map || !m_From.InRange(m_Target, m_Range))
            {
                m_From.Send(new CancelArrow());
                m_From.SendLocalizedMessage(503177); // You have lost your quarry.

                Stop();
                return;
            }

            if (m_LastX != m_Target.X || m_LastY != m_Target.Y)
            {
                m_LastX = m_Target.X;
                m_LastY = m_Target.Y;

                m_Arrow.Update(m_LastX, m_LastY);
            }
        }
    }
}