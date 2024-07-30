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

/* Scripts/Mobiles/Guards/PatrolGuard.cs
 * Changelog:
 * 1/7/23, Adam (Interesting Guard Wandering)
 *      Reminiscent of OSI guards of old, guards now will walk around, seemingly going somewhere or doing something (and do something).
 * 6/21/10, adam
 *		rewrite such that PatrolGuard is now based upon on WarriorGuard
 * 04/19/05, Kit
 *		Added check to onmovement not to attack player initially if they are hidden.
 *		updated bank closeing code to not have guard change direction of player(was causing them to be paralyzed) 
 * 9/30/04, Pigpen
 * 		Fixed an issues where this guard would try to chase a hidden >player char if that char has more that 5 counts. Spamming Reveal etc.
 * 8/7/04, Old Salty
 * 		Patrol Guards now check to see if the region is guarded before attacking reds on sight.
 * 7/26/04, Old Salty
 * 		Added a few lines (97-100) to make the criminal turn, closing the bankbox, when the guard attacks.
 * 6/22/04, Old Salty
 * 		PatrolGuards now deal extra damage to NPC's like their *poof*guard counterparts.
 * 		PatrolGuards now respond more specifically to speech.
 * 6/21/04, Old Salty
 * 		Modified the search/reveal code so that guards can reveal players.
 * created 6/10/04 by mith
 *		These are guards designed for patrolling banks and other town areas without going *poof*
 */

using Server.Commands;
using Server.Spells.Fourth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Server.Utility;

namespace Server.Mobiles
{
    public partial class PatrolGuard : WarriorGuard
    {
        [Constructable]
        public PatrolGuard()
            : this(null)
        {
        }

        public PatrolGuard(Mobile target)
            : base(target)
        {

        }

        public PatrolGuard(Serial serial)
            : base(serial)
        {
        }

        // does this guard auto 'poof' when no longer needed?
        public override bool PoofingGuard { get { return false; } }

        #region Interesting Guard Wandering
        /* Interesting Guard Wandering AI state machine
         * This AI is broken into two state machines, the Home state machine, and the Tick state machine.
         * The Home state machine (HMS) executes when the mobile reaches, or is at, his home base, or a temporary home or 'waypoint'.
         * The Tick state machine (TSM) executes on a timer and acts as the heartbeat for the 'adventure'.
         * Timeouts:
         * There a handful of timeouts for believability. The system would otherwise work fine, but the timeouts synchronize the mobiles actions to human expectations.
         * o m_nextAdventure: this defines the frequency of 'adventures'. And adventure is how often the mobile goes out to 'do something interesting'.
         * o m_adventureTimeout: If we are on an adventure, this timeout controls when we should just give up and go home.
         * o m_missionTimeout: When we reach a certain point on our adventure, we begin 'our mission'. Our mission may be as simple as emoting "*interesting*"
         *      We only schedule a short m_missionTimeout, after which we are 'mission complete' and we go home.
         * o m_fightingTimeout: If the guard must execute other duties, like guard-whacking some malcontent, we enter the Fighting Timeout. This is for believability only.
         *      The AI described here works fine without this timeout is to prevent the guard from doing cute things 2 seconds after guard-whacking someone. In this case
         *      the AI completely resets by clearing all stored destinations and memory.
         * Debugging:
         *  "[set DebugMob true" enables the smart, base class anti-spam output of mobile status. This is different than "[set base.Debug true" which displays BaseAI status.
         * Implementation notes:
         * o List<Tuple<Mobile, Info>>
         *      we use a List instead of a Dictionary since the ordering of the Dictionary is undefined.
         *      For our List, we need to dependably add to the tail of the list, or we will keep targeting the same mobile. 
         * o Destinations: 
         *      Currently we only make use of two destinations. The original home-base of the mobile, and the destination of our mission. We use a stack here since expanding this system
         *      with additional waypoints is planned.
         * o Walkthrough:
         *      1. Guard is spawned: BaseAI.Wander informs the mobile he is at his Home. HSM initializes by saving (pushing on the stack) the mobile's Home. Etc.
         *      2. A Player Mobile appears: BaseAI.OnSee informs us, and records the mobile in our memory, along with some simple state information, Status.Seen for instance
         *      3. TSM gets a tick. If we are not otherwise busy, our current focus is queried from the top of our memory. If say, the Status of our focus is Status.Seen, 
         *          we invoke the AIObject.InvestigativeMemoryAI() to get a reliable path (pathing algo) to acquire LOS of that mobile. This is largely part of the 'interesting'
         *          behavior of the guard.. he appears to peek around the corner at you, then go back to his Home.
         *      4. The guard at this points begins 'waiting' for his m_nextAdventure to start.
         *      5. When it's time to begin our adventure, we again call AIObject.InvestigativeMemoryAI() to get a path 'near' to our mobile, but not too near and change states 
         *          to Status.Adventuring. This new 'home' is pushed on the stack.
         *      6. Once we reach our destination (our new home,) we enter a 'mission waiting state' while we do whatever we came here to do.
         *      7. After Status.MissionAccomplished, we enter Status.ReturningHome.
         *      8. Once we reach our homebase, we will remove this mobile from our Memory (they can be added back, but they will go to the end of the line,) and otherwise reset.
         *      9. Resume behaviors at #2 above.
         *  Several steps and states have been left out for brevity.
         *  It may seem like a lot of work for such a simple task, but this is what I remember of the old OSI guards on the production shards. At some point, OSI removed patrol guards altogether.
         *  I think they add a finished feel to the shard, and players will I hope appreciate it.
         */
        private DateTime m_nextAdventure = DateTime.MinValue;       // when the next adventure starts
        private DateTime m_adventureTimeout = DateTime.MinValue;    // abort if we are having trouble getting somewhere
        private DateTime m_missionTimeout = DateTime.MinValue;      // abort if we are having trouble getting somewhere
        private DateTime m_lastTimeHome = DateTime.MaxValue;        // last time were home. We'll probably use this to tele home if there is no other way
        private DateTime m_fightingTimeout = DateTime.MinValue;     // if we are fighting, abort all states
        private const int TalkDistance = 2;                         // if we are this close to a player,we may talk to them 
#if DEBUG
        private const int DoSomethingChance = 2;                    // chance we will do something with our mission: more on TC
#else
        private const int DoSomethingChance = 4;                    // chance we will do something with our mission: less on production
#endif
        private Memory m_ignorePlayer = new Memory();               // memory used to remember players we cannot path to, or that another guard is already processing, or that we've already talked to
        private Timer Metronome = null;                             // heartbeat timer
        private double HeartBeat = Utility.RandomDouble(1.75, 2.5); // heartbeat timer
        private Stack<Point3D> m_destinations = new();              // Oh! The places you will go!
        private Direction m_saveDirection;
        private List<Tuple<Mobile, Info>> Memory = new();           // memory of players and states
        public enum Status
        {
            None,
            Seen,
            FoundYou,
            Targeting,
            WaitingOnAdventureStart,
            Approaching,
            AdventureBegin,
            Adventuring,
            WaitingOnMission,
            MissionAccomplished,
            ReturningHome,
        }
        internal class Info
        {
            public Mobile Mobile = null;
            public byte DidSomethingTick = 0;
            private Status m_status = Status.Seen;
            public Status Status { get { return m_status; } set { if (m_status != value) Reset(); m_status = value; } }
            private void Reset() { DidSomethingTick = 0; }
            public Info(Mobile m)
            {
                Mobile = m;
            }
        }
        public Status GetFocusStatus()
        {
            Defrag();
            Info focus = GetFocusMobInfo();
            if (focus == null)
                return Status.None;
            return focus.Status;
        }
        private Info GetFocusMobInfo()
        {
            List<Mobile> list = new();
            foreach (var kvp in Memory)
                return kvp.Item2;
            return null;
        }
        public override void OnSee(Mobile m)
        {   // don't add the usual suspects, but also exclude those we remember as being inaccessible
            if (m is PlayerMobile pm && pm.Alive && !pm.Hidden && !m_ignorePlayer.Recall(pm))
            {   // only add mobile once
                if (!Memory.Any(t => t.Item1 == m))
                    Memory.Add(new Tuple<Mobile, Info>(m, new Info(m)));
            }
            base.OnSee(m);
        }
        public override bool IAIOkToInvestigate(Mobile playerToInvestigate)
        {   // This limits how often we will investigate a player
            // But since we are a guard, we won't be dissuaded
            return true;
        }
        public override void IAIResult(Mobile m, bool canPath)
        {
            // called by InvestigativeAI when we've found a mobile we're interested in
            //  Note: IAIResult() can be called without an interleaving IAIQuerySuccess() if the InvestigativeAI cannot resolve the path
            Info focus = GetFocusMobInfo();
            if (focus != null && focus.Mobile == m && canPath)
            {
                if (focus.Status == Status.Adventuring)
                    ;   // don't change the state, will be handled in StateMachine1Home()
                else if (focus.Status == Status.Seen)
                    focus.Status = Status.FoundYou;
                else
                    ; // debug break
            }
            else if (focus != null)
            {
                if (!Spawner.ClearPathLand(this.Location, focus.Mobile.Location, map: Map))
                {
                    m_ignorePlayer.Remember(focus.Mobile, NewIgnoreTimeout());  // forget them for an adventure time
                    DebugSay(DebugFlags.Mobile, "Ignoring {0} for 5 minutes", focus.Mobile.Name);
                    RemoveFocus();
                }
                else
                {
                    DebugSay(DebugFlags.Mobile, "Relying on ClearPath to {0}", focus.Mobile.Name);
                    if (focus.Status == Status.Adventuring)
                        ;   // don't change the state, will be handled in StateMachine1Home()
                    else if (focus.Status == Status.Seen)
                        focus.Status = Status.FoundYou;
                    else
                        ; // debug break
                }
            }
        }
        public override bool IAIQuerySuccess(Mobile m)
        {   // called by InvestigativeAI when wondering if we are done
            if (!Fighting())
            {
                DebugSay(DebugFlags.Mobile, "({0})", GetFocusStatus().ToString());
                Info focus = GetFocusMobInfo();
                if (focus != null && focus.Mobile == m)
                {
                    Status status = focus.Status;
                    if (status == Status.Adventuring)
                    {
                        if (GetDistanceToSqrt(m) <= 3.0)
                            return true;
                        else
                            return false;
                    }
                    else if (status == Status.Seen)
                        return this.InLOS(m);
                }
            }

            // we should not get here unless we are fighting, but in case we do, return true to exit search
            return true;
        }
        private void MetronomeTick(object state)
        {
            object[] aState = (object[])state;
            if (this.Deleted || this.Map == Map.Internal)
            {   // just stop. If our owner is brought back to the Map, we will restart
                Metronome = null;
                return;
            }
            Info focus = GetFocusMobInfo();
            StateMachine2Tick(focus);
            Metronome = Timer.DelayCall(TimeSpan.FromSeconds(HeartBeat), new TimerStateCallback(MetronomeTick), new object[] { null });
        }
        public override void IAmHome()
        {   // Called from BaseAI wonder when we are at out home
            // if we are not at our homebase, don't call the base as it will cause us to assume the direction specified in our spawner
            if (WaitingAtHomeBase())
                base.IAmHome();
            StateMachine1Home();
        }
        public void StateMachine1Home()
        {
            if (!Fighting())
            {
                // never run if we have no home
                if (Home != Point3D.Zero && m_destinations.Count == 0)
                {
                    // initialize: our original home. we will never pop this.
                    DebugSay(DebugFlags.Mobile, "Initializing");
                    m_destinations.Push(Home);
                    if (this.Spawner != null)
                        m_saveDirection = Direction;
                    if (Metronome == null)
                        Metronome = Timer.DelayCall(TimeSpan.FromSeconds(HeartBeat), new TimerStateCallback(MetronomeTick), new object[] { this });

                    m_nextAdventure = NextAdventure();
                    m_adventureTimeout = DateTime.MaxValue;
                    m_lastTimeHome = DateTime.MaxValue;
                }
                else if (WaitingAtHomeBase())
                {   // we are back to our original home
                    Info focus = GetFocusMobInfo();
                    if (focus != null && focus.Status == Status.ReturningHome)
                    {
                        DebugSay(DebugFlags.Mobile, "I have returned home");
                        // remove this mobile from our table. We will re-add him if he sticks around
                        Memory.RemoveAll(item => item.Item1 == GetFocusMobInfo().Mobile);
                        // schedule the next adventure
                        m_nextAdventure = NextAdventure();
                        m_adventureTimeout = DateTime.MaxValue;
                    }
                    else
                        DebugSay(DebugFlags.Mobile, "I'm at my original home");

                    m_lastTimeHome = DateTime.UtcNow;
                }
                else
                {   // we are at an alternate home (location)
                    // we will wait here for the timeout
                    if (GetFocusStatus() == Status.WaitingOnAdventureStart && DateTime.UtcNow > m_nextAdventure)
                    {   // go to our next Destination after hovering here for a short bit
                        Home = m_destinations.Pop();
                        GetFocusMobInfo().Status = Status.AdventureBegin;
                        // this adventure times out in this long
                        m_adventureTimeout = NewAdventureTimeout();
                        DebugSay(DebugFlags.Mobile, "The adventure begins!");
                    }
                    else if (GetFocusStatus() == Status.Adventuring && DateTime.UtcNow > m_adventureTimeout)
                    {
                        // the guy we were watching left. Just go home
                        GiveUp();
                        GetFocusMobInfo().Status = Status.ReturningHome;
                        DebugSay(DebugFlags.Mobile, "Giving up!");
                    }
                    else if (GetFocusStatus() == Status.Adventuring)
                    {
                        // We reached our destination, time to do what we came here to do
                        Info focus = GetFocusMobInfo();
                        focus.Status = Status.WaitingOnMission;
                        m_missionTimeout = NewMissionTimeout();
                        DebugSay(DebugFlags.Mobile, "We're here");
                        DebugSay(DebugFlags.Mobile, "Beginning our mission");
                    }
                    else if (GetFocusStatus() == Status.WaitingOnMission && DateTime.UtcNow > m_missionTimeout)
                    {
                        // Mission complete
                        GetFocusMobInfo().Status = Status.MissionAccomplished;
                        DebugSay(DebugFlags.Mobile, "Mission accomplished");
                    }
                    else if (GetFocusStatus() == Status.MissionAccomplished)
                    {
                        // Mission accomplished, time to head home
                        GoHome();
                        GetFocusMobInfo().Status = Status.ReturningHome;
                        m_adventureTimeout = DateTime.MaxValue;
                        DebugSay(DebugFlags.Mobile, "Returning home");
                        Info focus = GetFocusMobInfo();
                        m_ignorePlayer.Remember(focus.Mobile, NewAntiSpamTimeout());  // Don't keep spamming the same mobile
                        DebugSay(DebugFlags.Mobile, "Ignoring {0} for 5 minutes", focus.Mobile.Name);
                    }
                    else if (GetFocusStatus() == Status.None)
                    {
                        // our target vanished
                        GiveUp();
                        DebugSay(DebugFlags.Mobile, "Our target vanished");
                        DebugSay(DebugFlags.Mobile, "Returning home");
                    }
                }
            }
        }
        private bool CheckExceptions(Info focus)
        {
            bool noFocus = focus == null;
            bool noAI = AIObject == null;
            bool goneTooLong = m_lastTimeHome < DateTime.MaxValue && DateTime.UtcNow > m_lastTimeHome + TimeSpan.FromMinutes(10);
            if (noFocus)
                DebugSay(DebugFlags.Mobile, string.Format("nothing to do"));
            if (noAI)
                DebugSay(DebugFlags.Mobile, string.Format("I got no stinking AI"));
            if (goneTooLong)
            {
                DebugSay(DebugFlags.Mobile, string.Format("I've been gone too long"));
                if (Spawner != null && Spawner.Deleted == false && Map == Spawner.Map && Deleted == false)
                {
                    DebugSay(DebugFlags.AI, "I am recalling home");
                    m_lastTimeHome = DateTime.UtcNow;
                    new NpcRecallSpell(this, null, Spawner.Location).Cast();
                }
            }
            return noFocus || noAI || goneTooLong;
        }
        private void StateMachine2Tick(Info focus)
        {
            Defrag();

            if (!Initialized())
            {   // this can happen if were interrupted to handle a lawbreaker. 
                //  we need to wait for initialization to reset our true home.
                DebugSay(DebugFlags.Mobile, string.Format("waiting for initialization..."));
            }
            else if (Fighting())
            {
                if (m_destinations.Count > 0)
                {
                    Abort();
                    DebugSay(DebugFlags.Mobile, string.Format("aborting..."));
                }
                else
                    // "waiting for initialization..." above, trumps "waiting for battle cooldown...", so you'll probably never see this message.
                    DebugSay(DebugFlags.Mobile, string.Format("waiting for battle cooldown..."));
            }
            else if (CheckExceptions(focus) == false)
            {
                if (DateTime.UtcNow > m_adventureTimeout)
                {   // we seem to be stuck going somewhere. Delete the current focus. That will 
                    //  allow us to acquire a new focus, and attempt a new adventure
                    DebugSay(DebugFlags.Mobile, "Houston, we got a problem...");
                    if (GetFocusMobInfo() != null) GetFocusMobInfo().Status = Status.None;
                    GiveUp();
                    m_adventureTimeout = NewAdventureTimeout();
                }
                else
                {
                    switch (focus.Status)
                    {
                        case Status.Seen:                       // seen, but not necessarily LOS
                            DebugSay(DebugFlags.Mobile, string.Format("Saw {0}", focus.Mobile.Name));
                            AIObject.InvestigativeMemoryAI(focus.Mobile);
                            break;
                        case Status.FoundYou:                   // InvestigativeAI found this guy
                            DebugSay(DebugFlags.Mobile, string.Format("Found {0}", focus.Mobile.Name));
                            focus.Status = Status.Targeting;    // find a destination near mobile
                            m_destinations.Push(FindLocationNear(focus.Mobile));
                            break;
                        case Status.Targeting:                  // we will target them
                            DebugSay(DebugFlags.Mobile, string.Format("Targeting {0}", focus.Mobile.Name));
                            if (InformGuards(focus.Mobile))     // tell other guards nearby we are on it!
                                focus.Status = Status.WaitingOnAdventureStart;
                            else                                // no can do, another guard is already processing - abort    
                                focus.Status = Status.None;
                            break;
                        case Status.WaitingOnAdventureStart:    // wait for the adventure to begin
                            DebugSay(DebugFlags.Mobile, string.Format("Waiting adventure begin..."));
                            // next state handled by StateMachine1Home
                            break;
                        case Status.AdventureBegin:             // We're initializing our Adventure 
                            AIObject.InvestigativeMemoryAI(focus.Mobile);
                            focus.Status = Status.Adventuring;  // okay, we're ready to start
                            DebugSay(DebugFlags.Mobile, string.Format("Adventure Begin"));
                            StateMachine2Tick(focus);           // don't allow results from InvestigativeMemoryAI() to change the state before we move to Adventuring
                            break;
                        case Status.Adventuring:                // We're Adventuring!
                            DebugSay(DebugFlags.Mobile, string.Format("Adventuring..."));
                            // next state handled by StateMachine1Home
                            break;
                        case Status.WaitingOnMission:           // wait to do what we came here to do
                                                                // wait for the Nth tick - on which WaitingOnMission tick do you wish to act?
                            if (focus.DidSomethingTick == 1)
                            {   // we will shorten the timeout here based on what our mission accomplished.
                                DebugSay(DebugFlags.Mobile, string.Format("Attempt do something"));
                                int len = DoSomething();
                                if (len == 0)
                                    // just wandering around
                                    m_missionTimeout = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                                else if (len == 1)
                                    // fart on them and leave immediately
                                    m_missionTimeout = DateTime.UtcNow + TimeSpan.FromSeconds(0);
                                else
                                {   // scale the length of time we spend here based on the length of text / number of sentences actually.
                                    // N seconds per sentence
                                    m_missionTimeout = DateTime.UtcNow + TimeSpan.FromSeconds(len * 2.5);
                                }
                            }
                            focus.DidSomethingTick++;
                            DebugSay(DebugFlags.Mobile, string.Format("Waiting mission completion..."));
                            // next state handled by StateMachine1Home
                            break;
                        case Status.MissionAccomplished:        // mission accomplished
                                                                // handled by StateMachine1Home
                            break;
                        case Status.ReturningHome:              // We're done with our adventure, go home
                                                                // handled by StateMachine1Home
                            break;
                    }
                }
            }
        }
        private bool InformGuards(Mobile m)
        {
            IPooledEnumerable eable = this.GetMobilesInRange(RangePerception);
            foreach (Mobile mob in eable)
                if (mob is PatrolGuard pg && pg != this && !pg.Deleted)
                {
                    DebugSay(DebugFlags.Mobile, string.Format("Asking {0} to ignore {1}", pg.Name, m.Name));
                    if (pg.Message(m) == false)
                    {   // this guy is already being processed by another guard
                        DebugSay(DebugFlags.Mobile, string.Format("{0} refused to ignore {1}", pg.Name, m.Name));
                        // we already waited and got canceled, okay to start a new adventure.
                        m_nextAdventure = DateTime.UtcNow;
                        // we will instead ignore this player for an adventure wait time
                        m_ignorePlayer.Remember(m, NewIgnoreTimeout());
                        // cleanup and we are done
                        eable.Free();
                        return false;
                    }
                }
            eable.Free();
            // all guards agreed to ignore player, we can continue processing
            DebugSay(DebugFlags.Mobile, string.Format("All guards agreed to ignore {0}", m.Name));
            return true;
        }
        public bool Message(Mobile m)
        {   // another guard is asking us if it's ok to ignore this player since they want to handle it
            Info focus = GetFocusMobInfo();
            if (m_ignorePlayer.Recall(m))
            {   // already being ignored
                m_ignorePlayer.Refresh(m);
                return true;
            }
            else if (focus != null && focus.Mobile == m)
            {   // should we allow the ignore?
                if (focus.Status == Status.Seen)
                {   // ignore this player for 5 minutes
                    m_ignorePlayer.Remember(m, NewAntiSpamTimeout());
                    // remove them from our queue
                    RemoveFocus();
                    // we already waited and got canceled, okay to start a new adventure.
                    m_nextAdventure = DateTime.UtcNow;
                    DebugSay(DebugFlags.Mobile, string.Format("I've been informed to ignore {0}", m.Name));
                    return true;
                }
                else
                {   // sorry, we're already processing this guy
                    return false;
                }
            }
            // we're not processing him, so you may go ahead
            // we will ignore this player for an adventure wait time
            m_ignorePlayer.Remember(m, NewIgnoreTimeout());
            return true;
        }
        private int DoSomething()
        {
            int somethingLength = 0;
            Mobile m = NearPlayer(TalkDistance);
            // one in five chance at hearing a Sun Tzu quote
            int index = Utility.Random(SunTzu.Count * DoSomethingChance);
            if (index < SunTzu.Count)
            {
                SmartSay(m, SunTzu[index]);
                // 2 is our minimum wait for spoken text
                somethingLength = Math.Max(SentenceComplexity(SunTzu[index]), 2);
            }
            else if (Utility.Random(DoSomethingChance) == 0)
            {
                // one in five chance at an emote
                EmoteCommand.RandomEmote(this);
                somethingLength = 1;
            }
            // if we 'say' something, we will delay our mission a tad so that it looks like we care about what we are saying.
            //  if we simply emote, (*fart*), we just turn around and leave
            return somethingLength;
        }
        private int SentenceComplexity(string text)
        {
            // Today, the average sentence length is between 15 and 20 words. This is significantly lower than it was in the past. 
            //  If you have ever read works from Medieval times, you will probably notice that the sentences were much longer.
            string[] toks = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return Math.Max(toks.Length / 15, CountSentences(text));
        }
        private int CountSentences(string text)
        {   // Sun Tzu uses a lot of compound sentences!
            return text.Count(c => c == '.' || c == ';');
        }
        private void SmartSay(Mobile m, string sourcestring)
        {
            if (sourcestring != null)
            {
                // start by ensuring a period
                sourcestring = sourcestring + ".";
                sourcestring = sourcestring.Replace("..", ".");
                // convert entire string to lower case
                var lowerCase = sourcestring.ToLower();
                // matches the first sentence of a string, as well as subsequent sentences
                var r = new Regex(@"(^[a-z])|\.\s+(.)", RegexOptions.ExplicitCapture);
                // MatchEvaluator delegate defines replacement of sentence starts to uppercase
                var result = r.Replace(lowerCase, s => s.Value.ToUpper());
                // now rebuild the string without extra spaces
                string[] chunks = result.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string text = string.Empty;
                foreach (string s in chunks) text += s + " ";
                text = text.Trim();
                if (m != null)
                {   // face our focus player
                    FacePlayer(m);
                    SayTo(m, text);
                }
                else
                {   // just face the nearest player
                    m = NearPlayer(RangePerception);
                    if (m != null)
                        FacePlayer(m);

                    Yell(text);
                }

            }
        }
        private Mobile NearPlayer(int distance)
        {
            List<PlayerMobile> list = new();
            IPooledEnumerable eable = this.GetMobilesInRange(RangePerception);
            foreach (Mobile mob in eable)
                if (mob is PlayerMobile pm && !pm.Hidden) /* we'll talk to ghosts, that's ok */
                    list.Add(pm);
            eable.Free();

            // sort the list on distance to me
            list.Sort((e1, e2) =>
            {
                return e1.GetDistanceToSqrt(this).CompareTo(e2.GetDistanceToSqrt(this));
            });

            if (list.Count > 0 && list[0].GetDistanceToSqrt(this) <= distance)
                return list[0];
            return null;
        }
        private void FacePlayer(Mobile m)
        {
            Direction = GetDirectionTo(m);
        }
        private Point3D FindLocationNear(Mobile m)
        {
            List<Point3D> exclude = new();
            IPooledEnumerable eable = this.GetMobilesInRange(RangePerception);
            foreach (Mobile mob in eable)
                exclude.Add(mob.Location);  // exclude the spots occupied by other mobiles
            eable.Free();

            Utility.Shuffle(exclude);

            for (int ix = 0; ix < 5; ix++)
            {   // find a free spot as close as 2 tiles, as far as 6. if we are within 2 tiles, the guard may speak directly to the player
                Point3D maybe = Mobiles.Spawner.GetSpawnPosition(m.Map, m.Location,
                    Utility.RandomBool() ? TalkDistance : Utility.RandomMinMax(TalkDistance, 6),
                    SpawnFlags.None, this);
                if (exclude.Contains(maybe))
                    continue;
                // we are trying to get near the mobile, so don't select a location inside a building if the player is outside or vice versa
                else if (Spawner.ClearPathLand(this.Location, maybe, map: Map, canOpenDoors: false) == false)
                    continue;
                else
                    return maybe;
            }
            // give up!
            return Home;
        }
        private void RemoveFocus() { if (Memory.Count > 0) Memory.RemoveAt(0); }
        public override Mobile Focus
        {
            get { return base.Focus; }
            set { base.Focus = value; if (value != null) m_fightingTimeout = DateTime.UtcNow + TimeSpan.FromMinutes(2); }
        }

        public void GoHome()
        {
            while (m_destinations.Count > 1)
                m_destinations.Pop();
            Home = m_destinations.Peek();
        }
        public void GiveUp()
        {
            Defrag();
            GoHome();
        }
        private void Abort()
        {
            GoHome();
            while (m_destinations.Count > 0)    // clear all destinations but home
                m_destinations.Pop();
            Memory.Clear();                     // clear all memory
        }
        private bool Initialized()
        {
            return m_destinations.Count > 0;
        }
        private void Defrag()
        {
            List<Mobile> list = new();
            foreach (var kvp in Memory)
            {
                if (kvp.Item1.Deleted || kvp.Item1.Hidden || this.GetDistanceToSqrt(kvp.Item1) > this.RangePerception)
                    list.Add(kvp.Item1);
                if (kvp.Item2.Status == Status.None)
                    list.Add(kvp.Item1);
            }

            foreach (Mobile m in list)
                Memory.RemoveAll(item => item.Item1 == m);
        }
        private bool Fighting() { return DateTime.UtcNow < m_fightingTimeout; }
        public DateTime NextAdventure() { return DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomDouble(1.75, 2.5)); }
        public DateTime NewAdventureTimeout() { return DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomDouble(2.75, 3.5)); }
        private double NewIgnoreTimeout() { return (NextAdventure() - DateTime.UtcNow).TotalSeconds; }
        private double NewAntiSpamTimeout() { return (TimeSpan.FromMinutes(5)).TotalSeconds; } // after talking to a player, ignore that player for 5 minutes
        public DateTime NewMissionTimeout() { return DateTime.UtcNow + TimeSpan.FromSeconds(10); }// just a placeholder, changes dynamically based on the mission results
        //public byte MissionDurationSeconds() { return (byte)(NewMissionTimeout() - DateTime.UtcNow).TotalSeconds; }
        public bool WaitingAtHomeBase()
        {
            //return m_destinations.Count == 1 && Home == m_destinations.Peek(); 
            return GetHomeBase() == this.Location && m_destinations.Count == 1;
        }
        public Point3D GetHomeBase()
        {
            Point3D home = default;
            foreach (var h in m_destinations)
                home = h;

            return home;
        }

        #endregion Interesting Guard Wandering
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            if (base.Version > 0)
            {
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {   // no work
                            break;
                        }
                }
            }
        }
    }
}