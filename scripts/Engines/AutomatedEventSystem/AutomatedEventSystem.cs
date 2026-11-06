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

/* ***********************************
 * ** CAUTIONARY NOTE ON TIME USAGE **
 * This module uses AdjustedDateTime.GameTime which returns a DST adjusted version of time independent
 * of the underlying system. For instance our production server DOES NOT change time for DST, but this module
 * along with a select few others surfaces functionality to the players which require a standard (DST adjusted) notion of time.
 * The DST adjusted modules include: AutomatedEventSystem.cs, AutoRestart.cs, CronScheduler.cs.
 * ***********************************
 */

/* Scripts/Engines/AutomatedEventSystem/AutomatedEventSystem.cs
 * CHANGELOG:
 *  10/27/26, Adam
 *      SpawnTypes enum removed: TownInvasionAES keeps the invasion champ's definition name (null until one is picked).
 *      Version 3 saves the name; version 2 saves (a SpawnTypes ordinal) are read through LegacyChampTypes.
 *  10/15/22, Adam
 *      Update the cron event to allow specification of which clock to use.
 *      By default, all cron tasks use a 'flattened' time (Pacific time without an adjustment for DST,) but some
 *          modules, like AES, need to fire on GameTime and not GameTimeSansDST.
 *  9/8/2021, Adam
 *      Set all the m_test mode flags to Core.UOTC_CFG (testcenter)
 *      This puts all the events in accelerated mode so I can run them every 4 hours or so.
 *  3/29/10, adam
 *		create a new ransom chest to avoid exploits (maybe having it open when it is filled?)
 *	9/8/08, Adam
 *		Change Server Wars message from "Head to Dungeon Wrong for PvP." to "Head to West Britain Bank for PvP."
 *	4/14/08, Adam
 *		Replace all explicit use of AdjustedDateTime(DateTime.UtcNow).Value with the new:
 *			public static DateTime AdjustedDateTime.GameTime
 *		We do this so that it is clear what files need to opperate in this special time mode:
 *			CronScheduler.cs, AutoRestart.cs, AutomatedEventSystem.cs
 *	4/8/08, Adam
 *		turn off new player starting area during server wars
 *	4/7/08, Adam
 *		- add comment
 *	4/6/08, Adam
 *		- Have Server Wars place a potion stone and reagent stone at WBB.
 *		- Turn off guards at WBB
 *	2/26/08, Adam
 *		Increase the Server Wars from 1 hour to 3
 *		(this is for the Cross Shard Challange)
 *  11/10/07, Adam
 *      Have CoreAI.TreasureMapDrop be 3x normal instead of a hard 10%
 *	8/26/07, Adam
 *		Update the KinRansomAES to save a list of kin spawners that were shutoff during the event
 *		and restart those spawners when the event ends.
 *  2/28/06, Adam
 *      in OnChestOpenInit insure we have level 5 chest stats
 *  2/27/06, Adam
 *      - For the KinRansomAES, open the chest at some random point after the first 1.5 hours.
 *          We also suppress the "the chest is open" announcement
 *      - Allow for better status logging for all AESs
 *  11/23/06, Plasma
 *      Updated KinRansomAES to add chest area as a no camp zone
 *      For the duration of the event.
 *	10/28/06, Plasma
 *		Updated TownInvasionAES to use new ChampInvasion class derrived
 *		From the new ChampEngine system
 *	10/18/06, Adam
 *		Replace Fixed = true, with Visible = false - prevents normal decay
 *	9/4/06, Adam
 *		- set all ransom kind to fight mode 'closest'
 *		- set the kind to die off after the event (lifespan)
 *		- set the NextSpawn time on the kin spawners to be AFTER the event ends
 *	8/25/06, Adam
 *		Make the core properties access level admin
 *	8/25/06, Plasma
 *		Modified town invasion code to set champ = random using the new champ mode enum
 *	7/25/06, Adam
 *		turn CoreAI.FeatureBits.GuildKinChangeDisabled off/on during Ransom Quests.
 *		We turn off the ability to switch alignments withing the 24 hour event window.
 *	6/24/06, Adam
 *		In EventStartInit(), perform the following setup opperations.
 *		- Set Trap Power		// cleared with RemoveTrap
 *		- Set locked true		// cleared with lockpick
 *		- Set Trap Type			// cleared with RemoveTrap
 *	6/17/06, Adam
 *		Fix a logic (numbering) bug in a switch statement for selecting announce text
 *	6/16/06, Adam
 *		turn off Stuck Menu during Ransom AES, restore afterwards
 *	6/11/06, Adam
 *		- put Town Crier stuff in base class
 *	6/10/06, Adam
 *		- Cleanup logic
 *		- Normalize TEST code
 *		- Better messages
 *	6/8/06, Adam
 *		add - TownInvasionAES
 *		add - KinRansomAES
 *	6/5/06, Adam
 *		- Generalize base class to better encapsulate generic functionality.
 *		- Add Crazy Map Day AES
 *	6/3/06, Adam
 *		- Finalize logic, set production paramaters
 *		- add ServerWarsAES
 *	6/2/06, Adam
 *		Initial Version.
 */

using Server.Diagnostics;
using Server.Engines.ChampionSpawn;		// champ stuff
using Server.Items;						// containers
using Server.Mobiles;					// Town Crier
using Server.Regions;					// duh
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server
{
    #region AES CORE
    public class AutomatedEventSystem : Item
    {
        private TownCrierEntry? m_TownCrierMessage;  // town crier entry for telling the world about us
        private DateTime m_ActionTime = AdjustedDateTime.GameTime - TimeSpan.FromMinutes(1.0);
        private Timer? m_ActionTimer = null;
        private bool m_reset = true;                // not serialized - server restarted(true)
        private bool m_valid = true;                // not serialized - server restarted leaving the object in a bad state

        DateTime m_EventStartTime;                  // when we start
        DateTime m_EventEndTime;                    // when we end
        DateTime m_AnnounceTime;                    // when we make Town Crier announcements
        TimeSpan m_AnnounceDelta;                   // frequency between announcements
        DateTime m_BroadcastTime;                   // when we make Broadcast announcements 
        DateTime m_FinalCleanup;                    // delete this event object
        bool m_bEventStartInit = true;              // one time event start flag
        bool m_bEventEndInit = true;                // one time event end flag

        [Constructable]
        public AutomatedEventSystem()
            : base(0x1F14)
        {
            Weight = 1.0;                               // carry it around
            Hue = 0x3A;                                 // looks like other console runes (different hue)
            Name = "Automated Event System";            // yeah
            Visible = false;                            // prevents normal decay
            BeginAction(TimeSpan.FromMinutes(1.0)); // generic 1 minute timer for messages, email, etc 
            m_reset = false;                            // not serialized - server restarted(false)
        }

        public AutomatedEventSystem(Serial serial)
            : base(serial)
        {
        }

        private void RemoveTCEntry()
        {
            if (m_TownCrierMessage != null)
            {
                GlobalTownCrierEntryList.Instance.RemoveEntry(m_TownCrierMessage);
                m_TownCrierMessage = null;
            }
        }

        public void AddTCEntry(string[] lines, TimeSpan span)
        {
            try
            {
                if (lines[0].Length > 0)
                {
                    // first, clear any existing message and timers
                    RemoveTCEntry();

                    // Setup the Town Crier
                    m_TownCrierMessage = new TownCrierEntry(lines, span, Serial.MinusOne);
                    GlobalTownCrierEntryList.Instance.AddEntry(m_TownCrierMessage);
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool ValidState
        {
            get { return m_valid; }
            set { m_valid = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public DateTime EventStartTime
        {
            get { return m_EventStartTime; }
            set { m_EventStartTime = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public DateTime EventEndTime
        {
            get { return m_EventEndTime; }
            set { m_EventEndTime = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public DateTime AnnounceTime
        {
            get { return m_AnnounceTime; }
            set { m_AnnounceTime = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan AnnounceDelta
        {
            get { return m_AnnounceDelta; }
            set { m_AnnounceDelta = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public DateTime BroadcastTime
        {
            get { return m_BroadcastTime; }
            set { m_BroadcastTime = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public DateTime FinalCleanup
        {
            get { return m_FinalCleanup; }
            set { m_FinalCleanup = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool EventStarted
        {
            get { return AdjustedDateTime.GameTime >= m_EventStartTime; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool EventStartInit
        {
            get { return EventStarted ? m_bEventStartInit : false; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool EventEnded
        {
            get { return AdjustedDateTime.GameTime >= m_EventEndTime; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool EventEndInit
        {
            get { return EventEnded ? m_bEventEndInit : false; }
        }

        public virtual void OnEvent()
        {
            // Are we in an invalid state?
            if (ValidState == false)
            {
                OnInvalidState();
                FinalCleanup = DateTime.MaxValue;
                if (m_ActionTimer != null)
                    m_ActionTimer.Stop();
                this.Delete();
            }

            // has the server restarted?
            if (m_reset == true && ValidState)
            {
                m_reset = false;
                OnServerRestart();
            }

            // one time event init event
            if (EventStartInit == true && ValidState)
            {
                OnEventStartInit();
                m_bEventStartInit = false;
            }

            // one time event shutdown event
            if (EventEndInit == true && ValidState)
            {
                OnEventEndInit();
                m_bEventEndInit = false;
            }

            // periodic announcement
            if (AdjustedDateTime.GameTime - m_AnnounceTime >= m_AnnounceDelta && ValidState)
            {
                OnAnnounce();
                m_AnnounceTime = AdjustedDateTime.GameTime;
            }

            // one time system broadcast message
            if (AdjustedDateTime.GameTime > BroadcastTime && ValidState)
            {
                OnBroadcast();
                BroadcastTime = DateTime.MaxValue;
            }

            // final cleanup
            if (AdjustedDateTime.GameTime > FinalCleanup && ValidState)
            {
                OnFinalCleanup();
                FinalCleanup = DateTime.MaxValue;
                if (m_ActionTimer != null)
                    m_ActionTimer.Stop();
                this.Delete();
            }
        }

        public virtual void OnInvalidState()
        {
        }
        public virtual void OnServerRestart()
        {
        }
        public virtual void OnEventStartInit()
        {
        }
        public virtual void OnEventEndInit()
        {
        }
        public virtual void OnAnnounce()
        {
        }
        public virtual void OnBroadcast()
        {
        }
        public virtual void OnFinalCleanup()
        {
        }
        protected virtual void DumpState()
        {
            Console.WriteLine("CoreAES: Serial: {0}", this.Serial);
            Console.WriteLine("CoreAES: EventStartTime: {0}", m_EventStartTime);
            Console.WriteLine("CoreAES: EventEndTime: {0}", m_EventEndTime);
            Console.WriteLine("CoreAES: AnnounceDelta: {0}", m_AnnounceDelta.TotalMinutes);
            Console.WriteLine("CoreAES: BroadcastTime: {0}", m_BroadcastTime);

            // count down to next announcement
            TimeSpan delta = AdjustedDateTime.GameTime - m_AnnounceTime;
            delta = m_AnnounceDelta - delta;
            Console.WriteLine("CoreAES: Next announcement: {0:F2} minute(s).", delta.TotalMinutes);
        }

        public class ActionTimer : Timer
        {
            private AutomatedEventSystem m_AES;

            public ActionTimer(AutomatedEventSystem ix, TimeSpan delay)
                : base(TimeSpan.FromMinutes(0.5), delay)
            {
                m_AES = ix;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                m_AES.OnEvent();
            }
        }

        public override void OnDelete()
        {
            if (m_ActionTimer != null)
                m_ActionTimer.Stop();

            base.OnDelete();
        }

        public virtual void BeginAction(TimeSpan delay)
        {
            if (m_ActionTimer != null)
                m_ActionTimer.Stop();

            m_ActionTime = AdjustedDateTime.GameTime + delay;

            m_ActionTimer = new ActionTimer(this, m_ActionTime - AdjustedDateTime.GameTime);
            m_ActionTimer.Start();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)2); // version

            writer.Write(m_AnnounceDelta);      // version 2

            writer.Write(m_ActionTime);             // version 1
            writer.Write(m_EventStartTime);
            writer.Write(m_EventEndTime);
            writer.Write(m_AnnounceTime);
            writer.Write(m_BroadcastTime);
            writer.Write(m_FinalCleanup);
            writer.Write(m_bEventStartInit);
            writer.Write(m_bEventEndInit);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {   // all done
                        break;
                    }

                case 2:
                    {
                        m_AnnounceDelta = reader.ReadTimeSpan();
                        goto case 1;
                    }

                case 1:
                    {
                        m_ActionTime = reader.ReadDateTime();
                        m_ActionTimer = new ActionTimer(this, m_ActionTime - AdjustedDateTime.GameTime);
                        m_ActionTimer.Start();

                        m_EventStartTime = reader.ReadDateTime();
                        m_EventEndTime = reader.ReadDateTime();
                        m_AnnounceTime = reader.ReadDateTime();
                        m_BroadcastTime = reader.ReadDateTime();
                        m_FinalCleanup = reader.ReadDateTime();
                        m_bEventStartInit = reader.ReadBool();
                        m_bEventEndInit = reader.ReadBool();
                        goto case 0;
                    }
            }
        }
    }
    #endregion AES CORE

    #region CrazyMapDayAES
    public class CrazyMapDayAES : AutomatedEventSystem
    {
        double m_OldTreasureMapDropRate;            // restore this after the event
        bool m_test = Core.GMNTC_CFG;

        public CrazyMapDayAES(Serial serial)
            : base(serial)
        {
        }

        public CrazyMapDayAES()
        {
            AnnounceTime = AdjustedDateTime.GameTime;                        // when we make TC announcements

            if (m_test == false)
            {
                EventStartTime = AdjustedDateTime.GameTime.AddDays(1.0);    // 24 hours from now
                EventEndTime = EventStartTime.AddHours(3.0);                // 3 hour event
                AnnounceDelta = TimeSpan.FromHours(1.0);                    //	each hour
                BroadcastTime = EventStartTime.AddHours(-1.0);              // 1 hour before start - final Broadcast
                FinalCleanup = EventEndTime.AddHours(1.0);                  // cleanup this AES 1 hour after the event ends
            }
            else
            {
                EventStartTime = AdjustedDateTime.GameTime.AddMinutes(10.0);    // (TEST)
                EventEndTime = EventStartTime.AddMinutes(10.0);                 // (TEST)
                AnnounceDelta = TimeSpan.FromMinutes(2.0);                      // (TEST)
                BroadcastTime = EventStartTime.AddMinutes(-1.0);                // (TEST)
                FinalCleanup = EventEndTime.AddMinutes(1.0);                    // (TEST)
            }
        }

        public override void OnInvalidState()
        {
            Console.WriteLine("CrazyMapDayAES: Invalid state detected.");
            Console.WriteLine("CrazyMapDayAES: Deleting object.");
        }

        public override void OnServerRestart()
        {
            Console.WriteLine("CrazyMapDayAES: Restart detected.");
        }

        public override void OnEventStartInit()
        {
            // speed up announcements!
            AnnounceDelta = TimeSpan.FromMinutes(5.0); //	every 5 minutes

            // save the old value
            m_OldTreasureMapDropRate = CoreAI.TreasureMapDrop;

            // setup the new map drop chance at 3x normal (something like 10%)
            CoreAI.TreasureMapDrop *= 3.0;

            string line = string.Format("Crazy Map Day has begun!");
            World.Broadcast(0x22, true, line);  // tell the world

            Console.WriteLine("CrazyMapDayAES: Start event.");
        }

        public override void OnEventEndInit()
        {
            // Cleanup Crazy Map Day
            CoreAI.TreasureMapDrop = m_OldTreasureMapDropRate;
            Console.WriteLine("CrazyMapDayAES: Begin event cleanup.");
        }

        public override void OnAnnounce()
        {
            // pre event announcements
            if (EventStarted == false)
            {
                TimeSpan HowLong = EventStartTime - AdjustedDateTime.GameTime;
                bool bInHours = HowLong > TimeSpan.FromMinutes(120.0);

                // what we want the Town Crier to say
                string[] lines = new string[4];
                lines[0] = string.Format(
                    "Crazy Map Day starting in about {0} {1}.",
                    bInHours ? (int)HowLong.TotalHours : (int)HowLong.TotalMinutes,
                    bInHours ? (int)HowLong.TotalHours == 1 ? "hour" : "hours" : (int)HowLong.TotalMinutes == 1 ? "minute" : "minutes");

                lines[1] = string.Format("Monsters dropping maps at 3x the normal rate.");
                lines[2] = string.Format("Stock up on your treasure maps.");
                lines[3] = string.Format("Beware; the murderous shall be among us.");

                // use the smaller of the time spans
                TimeSpan s1 = EventStartTime - AdjustedDateTime.GameTime;
                TimeSpan s2 = TimeSpan.FromMinutes(20.0);
                TimeSpan span = (s1 > s2) ? s2 : s1;

                // Setup the Town Crier with a new message
                AddTCEntry(lines, span);
                Console.WriteLine("CrazyMapDayAES: Issue Town Crier pre event announcement.");
            }
            // during event announcements
            else if (EventStarted == true && EventEnded == false)
            {
                // remainder of event
                TimeSpan span = EventEndTime - AdjustedDateTime.GameTime;

                // Setup the Town Crier with a new message
                string[] lines = new string[1];
                lines[0] = "Crazy Map Day is on!";
                AddTCEntry(lines, span);
                Console.WriteLine("CrazyMapDayAES: Issue Town Crier during event announcement.");
            }
            // post event announcements
            else if (EventStarted == true && EventEnded == true)
            {
                // 20 minutes should do it
                TimeSpan span = TimeSpan.FromMinutes(20.0);

                // Setup the Town Crier with a new message
                string[] lines = new string[1];
                lines[0] = "We hope you enjoyed Crazy Map Day.";
                AddTCEntry(lines, span);
                Console.WriteLine("CrazyMapDayAES: Issue Town Crier post event announcement.");
            }
        }

        public override void OnBroadcast()
        {
            // issue a final event announcement
            TimeSpan HowLong = EventStartTime - AdjustedDateTime.GameTime;
            bool bInHours = HowLong > TimeSpan.FromMinutes(120.0);

            string[] lines = new string[1];
            lines[0] = string.Format(
                "Crazy Map Day starting in about {0} {1}.",
                bInHours ? (int)HowLong.TotalHours : (int)HowLong.TotalMinutes,
                bInHours ? (int)HowLong.TotalHours == 1 ? "hour" : "hours" : (int)HowLong.TotalMinutes == 1 ? "minute" : "minutes");

            World.Broadcast(0x22, true, lines[0]);
            Console.WriteLine("CrazyMapDayAES: Issue final Broadcast announcement.");
        }

        public override void OnFinalCleanup()
        {
            Console.WriteLine("CrazyMapDayAES: In final cleanup.");
        }

        public override void OnEvent()
        {
            Console.WriteLine("CrazyMapDayAES: OnEvent()");

            // need to process base class events
            base.OnEvent();

            // print diagnostics
            base.DumpState();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            writer.Write(m_OldTreasureMapDropRate);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {   // all done
                        break;
                    }

                case 1:
                    {
                        m_OldTreasureMapDropRate = reader.ReadDouble();
                        goto case 0;
                    }
            }
        }
    }
    #endregion CrazyMapDayAES

    #region TownInvasionAES
    public class TownInvasionAES : AutomatedEventSystem
    {
        ChampInvasion? m_ChampionSpawn = null;           // the ChampionInvasion we will be using for the invasion.
        bool m_test = Core.GMNTC_CFG;                    // are we in test mode?
        bool m_bChampDeadHandled = false;               // have we handled the case where the champ has died?
                                                        //		Mobile m_Champion = null;			// the champ
        bool m_bChampComplete = false;                  // Pla : Flag indicates when champ has been completed
        string? m_spawnType = null;                     // that champ we will be using (definition name)
        bool m_bOldGuardedState = false;

        public TownInvasionAES(Serial serial)
            : base(serial)
        {
        }

        public TownInvasionAES()
        {
            AnnounceTime = AdjustedDateTime.GameTime;                            // when we make TC announcements

            if (m_test == false)
            {
                EventStartTime = AdjustedDateTime.GameTime.AddDays(1.0);         // 24 hours from now
                EventEndTime = EventStartTime.AddHours(8.0);        // 8 hour event
                AnnounceDelta = TimeSpan.FromHours(1.0);            //	each hour
                BroadcastTime = EventStartTime.AddHours(-1.0);      // 1 hour before start - final Broadcast
                FinalCleanup = EventEndTime.AddHours(1.0);          // cleanup this AES 1 hour after the event ends				
            }
            else
            {
                EventStartTime = AdjustedDateTime.GameTime.AddMinutes(3.0);      // (TEST)
                EventEndTime = EventStartTime.AddHours(1.0);        // (TEST)
                AnnounceDelta = TimeSpan.FromMinutes(2.0);      // (TEST)
                BroadcastTime = EventStartTime.AddMinutes(-2.0);    // (TEST)
                FinalCleanup = EventEndTime.AddMinutes(1.0);        // (TEST)
            }
        }

        public TownInvasionAES(TimeSpan eventStartTime, TimeSpan duration, TimeSpan announceDelta)
        {
            AnnounceTime = AdjustedDateTime.GameTime;                       // when we make TC announcements
            EventStartTime = AdjustedDateTime.GameTime + eventStartTime;    // Timespan from now
            EventEndTime = EventStartTime + duration;                       // Timespan event
            AnnounceDelta = announceDelta;                                  //	every once in a while
            BroadcastTime = EventStartTime - eventStartTime * 0.5;          // Timespan before start - final Broadcast
            FinalCleanup = EventEndTime.AddMinutes(20.0);                   // cleanup this AES 20 minutes after the event ends
        }

        // plasma : indicates when champ is finished
        public bool ChampComplete
        {
            get
            {
                return m_bChampComplete;
            }
            set
            {
                m_bChampComplete = value;
            }
        }

        public override void OnInvalidState()
        {
            Console.WriteLine("TownInvasionAES: Invalid state detected.");
            if (m_ChampionSpawn != null)
            {
                Console.WriteLine("TownInvasionAES: Stopping champ spawn...");
                m_ChampionSpawn.Running = false;
                m_ChampionSpawn.ClearMonsters = true;

                Console.WriteLine("TownInvasionAES: Turning on guards.");
                // turn on guards
                Region? town = Region.FindByName("Britain", Map.Felucca);     // Britain specific
                GuardedRegion? reg = town as GuardedRegion;
                if (reg != null)
                    reg.IsGuarded = true;

                // bring back the guards
                DitchBritGuards(false);
            }

            Console.WriteLine("TownInvasionAES: Deleting object.");
        }

        public override void OnServerRestart()
        {
        }

        public override void OnEventStartInit()
        {
            // speed up announcements!
            AnnounceDelta = TimeSpan.FromMinutes(5.0); //	every 5 minutes

            foreach (Item ix in World.Items.Values)
            {
                if (ix is ChampInvasion)
                {
                    ChampInvasion? cs = ix as ChampInvasion;
                    if (cs != null)
                    {
                        m_ChampionSpawn = cs;

                        // basic setup
                        m_ChampionSpawn.NavDestination = "britain_invasion";              // Britain invasion
                        m_spawnType = m_ChampionSpawn.PickChamp();                  // pick a new random champ type

                        // turn off guards
                        Region? town = Region.FindByName("Britain", Map.Felucca);     // Britain specific
                        GuardedRegion? reg = town as GuardedRegion;
                        if (reg != null)
                            reg.IsGuarded = false;

                        // hide the guards
                        DitchBritGuards(true);

                        // tell the champ spawn that we would like to know what's going on
                        m_ChampionSpawn.AESMonitor = this;

                        // let the fun begin!
                        m_ChampionSpawn.Running = true;      // start the creatures;

                        break;                              // we only support one invasion at this time
                    }
                }
            }

            if (m_ChampionSpawn == null)
            {
                Console.WriteLine("TownInvasionAES: No appropriate Champion Spawn found, aborting event..");
                // tell our base class to stop processing.
                //	the base will also begin cleanup
                ValidState = false;
            }
            else
            {
                string line = string.Format("The Britain guards are nowhere to be found.");
                World.Broadcast(0x22, true, line);                      // tell the world
            }

            Console.WriteLine("TownInvasionAES: Start event.");
        }

        public override void OnEventEndInit()
        {
            // If the players never killed the champ before the time ran out, we'll just enable
            //	the guards and let them kill off the creatures. 
            //	See Also: OnChampDead()
            if (m_ChampionSpawn != null && m_ChampionSpawn.Deleted == false)
            {
                // turn on guards
                Region? town = Region.FindByName("Britain", Map.Felucca);     // Britain specific
                GuardedRegion? reg = town as GuardedRegion;
                if (reg != null)
                    reg.IsGuarded = true;

                // bring back the guards
                DitchBritGuards(false);

                // tell the champ spawn that we no longer need to know what's going on
                m_ChampionSpawn.AESMonitor = null;

                // let the fun end!
                m_ChampionSpawn.Running = false;                             // stop the creatures;

                string line = string.Format("The Britain guards are now on duty.");
                World.Broadcast(0x22, true, line);                      // tell the world
            }

            Console.WriteLine("TownInvasionAES: Begin event cleanup.");
        }

        public override void OnAnnounce()
        {
            // pre event announcements
            if (EventStarted == false)
            {
                TimeSpan HowLong = EventStartTime - AdjustedDateTime.GameTime;
                bool bInHours = HowLong > TimeSpan.FromMinutes(120.0);

                // what we want the Town Crier to say
                string[] lines = new string[2];

                switch (Utility.Random(3))
                {
                    case 0:
                        {
                            lines[0] = string.Format("An evil swarm approaches.");
                            lines[1] = string.Format(
                                "Expect this plague in about {0} {1}.",
                                bInHours ? (int)HowLong.TotalHours : (int)HowLong.TotalMinutes,
                                bInHours ? (int)HowLong.TotalHours == 1 ? "hour" : "hours" : (int)HowLong.TotalMinutes == 1 ? "minute" : "minutes");
                        }
                        break;

                    case 1:
                        {
                            lines[0] = string.Format("Doom marches ever nearer.");
                            lines[1] = string.Format(
                                "We have but {0} {1} to prepare",
                                bInHours ? (int)HowLong.TotalHours : (int)HowLong.TotalMinutes,
                                bInHours ? (int)HowLong.TotalHours == 1 ? "hour" : "hours" : (int)HowLong.TotalMinutes == 1 ? "minute" : "minutes");
                        }
                        break;

                    case 2:
                        {
                            lines[0] = string.Format("We do not deserve this!.");
                            lines[1] = string.Format(
                                "Rally ye citizens, we only have {0} {1}.",
                                bInHours ? (int)HowLong.TotalHours : (int)HowLong.TotalMinutes,
                                bInHours ? (int)HowLong.TotalHours == 1 ? "hour" : "hours" : (int)HowLong.TotalMinutes == 1 ? "minute" : "minutes");
                        }
                        break;
                }

                // use the smaller of the time spans
                TimeSpan s1 = EventStartTime - AdjustedDateTime.GameTime;
                TimeSpan s2 = TimeSpan.FromMinutes(20.0);
                TimeSpan span = (s1 > s2) ? s2 : s1;

                // Setup the Town Crier with a new message
                AddTCEntry(lines, span);
                Console.WriteLine("TownInvasionAES: Issue Town Crier pre event announcement.");
            }
            // during event announcements
            else if (EventStarted == true && EventEnded == false)
            {
                // remainder of event
                TimeSpan span = EventEndTime - AdjustedDateTime.GameTime;

                // Setup the Town Crier with a new message
                string[] lines = new string[2];
                if (m_bChampDeadHandled == false)
                {
                    if (Utility.RandomBool())
                    {
                        lines[0] = "Dear citizens, come defend Britain from this plague.";
                        lines[1] = "Where are the Britain guards when you need them most?";
                    }
                    else
                    {
                        lines[0] = "Citizens of Britannia, leave us not be overthrown";
                        lines[1] = "We shall take this foe even without the help of the Britain guards!";
                    }
                }
                else
                { // the champ is dead
                    lines[0] = "We appear to have the upper hand now.";
                    lines[1] = "I still do not see the Britain guards though.";
                }
                AddTCEntry(lines, span);

                // warn PK's to get out of town!!
                if (span.TotalMinutes <= 30)
                {
                    string line = string.Format("The Britain guards are coming on duty soon.");
                    World.Broadcast(0x22, true, line);                      // tell the world
                }

                Console.WriteLine("TownInvasionAES: Issue Town Crier during event announcement.");
            }
            // post event announcements
            else if (EventStarted == true && EventEnded == true)
            {
                // 20 minutes should do it
                TimeSpan span = TimeSpan.FromMinutes(20.0);

                // Setup the Town Crier with a new message
                string[] lines = new string[1];
                lines[0] = "Thank you good citizens for defending our fair city.";
                AddTCEntry(lines, span);
                Console.WriteLine("TownInvasionAES: Issue Town Crier post event announcement.");
            }
        }

        public override void OnBroadcast()
        {
            // issue a final event announcement
            TimeSpan HowLong = EventStartTime - AdjustedDateTime.GameTime;
            bool bInHours = HowLong > TimeSpan.FromMinutes(120.0);

            string line = string.Format(
                "The Britain guards going on break in about {0} {1}.",
                bInHours ? (int)HowLong.TotalHours : (int)HowLong.TotalMinutes,
                bInHours ? (int)HowLong.TotalHours == 1 ? "hour" : "hours" : (int)HowLong.TotalMinutes == 1 ? "minute" : "minutes");

            World.Broadcast(0x22, true, line);
            Console.WriteLine("TownInvasionAES: Issue final Broadcast announcement.");
        }

        public override void OnFinalCleanup()
        {
            Console.WriteLine("TownInvasionAES: In final cleanup.");
        }

        public void OnChampDead()
        {
            Console.WriteLine("TownInvasionAES: Champ dead, accelerate cleanup.");

            // We don't want to just end the event NOW because the red players need a warning
            //	that the guards are coming on. We therefore just deactivate the champ spawner and
            //	move the end time to NOW + one hour. The announcements will automatically handle
            //	informing the players the the guards are coming on.

            // stop the creatures
            if (m_ChampionSpawn != null && m_ChampionSpawn.Deleted == false)
            {
                // tell the champ spawn that we no longer need to know what's going on
                m_ChampionSpawn.AESMonitor = null;

                // stop the flow of creatures
                m_ChampionSpawn.Running = false;
            }

            EventEndTime = AdjustedDateTime.GameTime.AddHours(1.0);  // let the guards remain off for another hour
            FinalCleanup = EventEndTime.AddHours(1.0);  // cleanup this AES 1 hour after the event ends				
        }

        public override void OnEvent()
        {
            Console.WriteLine("TownInvasionAES: OnEvent()");

            // pla : changed to work with new engine
            if (m_bChampDeadHandled == false && IsChampDead() && ValidState)
            {   // early end to the event
                OnChampDead();
                m_bChampDeadHandled = true;
            }

            // need to process base class events
            base.OnEvent();

            // print diagnostics
            DumpState();
        }

        protected override void DumpState()
        {
            base.DumpState();

            if (string.IsNullOrEmpty(m_spawnType))
                Console.WriteLine("TownInvasionAES: Champ not yet selected.");
            else
                Console.WriteLine("TownInvasionAES: Champ selected:{0}", m_spawnType);
        }

        private bool IsChampDead()
        {
            // pla : changed to work with new engine
            if (m_ChampionSpawn != null)
                if (m_bChampComplete)
                    return true;

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 3;
            writer.Write((int)version);

            switch (version)
            {
                case 3:
                    {
                        writer.Write(m_spawnType);                   // version 3: the definition name (was the SpawnTypes ordinal)
                        writer.Write(m_bOldGuardedState);
                        goto case 1;
                    }
                case 1:
                    {
                        writer.Write(m_ChampionSpawn);
                        writer.Write(m_bChampDeadHandled);
                        writer.Write(m_bChampComplete);
                        break;
                    }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_spawnType = reader.ReadString();
                        m_bOldGuardedState = reader.ReadBool();
                        goto case 1;
                    }
                case 2:
                    {
                        m_spawnType = LegacyChampTypes.ToName(reader.ReadInt());     // a SpawnTypes ordinal
                        m_bOldGuardedState = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_ChampionSpawn = reader.ReadItem() as ChampInvasion; // pla :changed to invasion class
                        m_bChampDeadHandled = reader.ReadBool();
                        m_bChampComplete = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {   // all done
                        break;
                    }
            }
        }

        private void DitchBritGuards(bool hide)
        {
            bool unhide = !hide;
            Map eventMap = Map.Felucca;

            Console.WriteLine("TownInvasionAES: Ditching Brit Guards...");
            List<Mobile> toTouch = new List<Mobile>();
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob is BaseGuard)
                {
                    if (hide && mob.Map == eventMap)
                        if (mob.Region.Name == "Britain" || mob is BritannianRanger)
                        {
                            toTouch.Add(mob);
                            continue;
                        }

                    if (unhide && mob.Map == Map.Internal)
                    {
                        toTouch.Add(mob);
                        continue;
                    }
                }
            }
            // hide/reveal these guards
            foreach (Mobile mob in toTouch)
            {
                mob.Map = (hide) ? Map.Internal : eventMap;
            }

            Console.WriteLine("TownInvasionAES: The Ditching of Brit Guards complete with {0} guards {1}.", toTouch.Count, hide ? "hidden" : "revealed");
        }

        #region Commands
        public static void Initialize()
        {
            CommandSystem.Register("BritainInvasion", AccessLevel.Owner, new CommandEventHandler(BritainInvasion_OnCommand));
        }
        // (TimeSpan eventStartTime, TimeSpan duration, TimeSpan announceDelta)
        [Usage("BritainInvasion")]
        [Description("Launch a Britain Invasion <TimeSpan eventStartTime>, <TimeSpan duration>, <TimeSpan announceDelta>")]
        public static void BritainInvasion_OnCommand(CommandEventArgs e)
        {
            try
            {
                TimeSpan eventStartTime = TimeSpan.Parse(e.GetString(0));
                TimeSpan duration = TimeSpan.Parse(e.GetString(1));
                TimeSpan announceDelta = TimeSpan.Parse(e.GetString(2));
                new TownInvasionAES(eventStartTime, duration, announceDelta);
                return;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                if (e.Mobile != null)
                    e.Mobile.SendMessage("** Britain Invasion Failed to launch **");
            }

            e.Mobile.SendMessage("Usage: BritainInvasion <TimeSpan eventStartTime>, <TimeSpan duration>, <TimeSpan announceDelta>");
        }
        #endregion Commands
    }
    #endregion TownInvasionAES

    #region ServerWarsAES
    public class ServerWarsAES : AutomatedEventSystem
    {
        bool m_test = Core.GMNTC_CFG;        // for testing only
        bool m_construction = false;        // flag that we have built stuff .. not serialized

        public ServerWarsAES(Serial serial)
            : base(serial)
        {
        }

        public ServerWarsAES()
        {
            AnnounceTime = AdjustedDateTime.GameTime;                        // when we make TC announcements

            if (m_test == false)
            {
                EventStartTime = AdjustedDateTime.GameTime.AddDays(1.0);         // 24 hours from now
                EventEndTime = EventStartTime.AddHours(3.0);        // 3 hour event
                AnnounceDelta = TimeSpan.FromHours(1.0);            //	each hour
                BroadcastTime = EventStartTime.AddHours(-1.0);      // 1 hour before start - final Broadcast
                FinalCleanup = EventEndTime.AddHours(1.0);          // cleanup this AES 1 hour after the event ends				
            }
            else
            {
                EventStartTime = AdjustedDateTime.GameTime.AddMinutes(10.0);     // (TEST)
                EventEndTime = EventStartTime.AddMinutes(10.0);     // (TEST)
                AnnounceDelta = TimeSpan.FromMinutes(2.0);      // (TEST)
                BroadcastTime = EventStartTime.AddMinutes(-2.0);    // (TEST)
                FinalCleanup = EventEndTime.AddMinutes(1.0);        // (TEST)
            }
        }

        public override void OnInvalidState()
        {
            Console.WriteLine("ServerWarsAES: Invalid state detected.");
            Console.WriteLine("ServerWarsAES: Deleting object.");
        }

        //////////////////////////////////////////////////////////////
        // NOTE: at the end of Server Wars, the server restarts without saving.
        //	this means that this object that was deleted before the restart will be
        //	resurrected when the server comes back up. We will therefore need to destroy
        //	this object when AdjustedDateTime.GameTime > FinalCleanup.
        // NOTE: the base of this class had set FinalCleanup = DateTime.MaxValue so as to indicate
        //	cleanup had already been completed, but because the server did not save, this assignment
        //	did not stick.
        public override void OnServerRestart()
        {   // What's going on here is no bug, it's just a reality of restarting the server
            //	without saving the current state.
            if (AdjustedDateTime.GameTime > FinalCleanup)
            {
                // tell our base class to stop processing.
                //	the base will also begin cleanup
                ValidState = false;

                // post restart cleanup
                Console.WriteLine("ServerWarsAES: Post event restart detected. Handling...");
            }
            else
                Console.WriteLine("ServerWarsAES: Restart detected.");
        }

        public override void OnEventStartInit()
        {
            // speed up announcements!
            AnnounceDelta = TimeSpan.FromMinutes(5.0);          //	every 5 minutes

            // set the duration of server wars
            TimeSpan ts = EventEndTime - EventStartTime;
            Server.Misc.AutoRestart.ServerWarsMinutes =             // calculate minutes in SW
                (int)ts.TotalMinutes;

            Server.Misc.AutoRestart.RestartTime = EventStartTime;   // when to begin
            Server.Misc.AutoRestart.Enabled = true;                 // auto restart enabled
            Console.WriteLine("ServerWarsAES: Start event.");
        }

        public override void OnEventEndInit()
        {
            Console.WriteLine("ServerWarsAES: Begin event cleanup.");
        }

        public override void OnAnnounce()
        {
            // pre event announcements
            if (EventStarted == false)
            {
                TimeSpan HowLong = EventStartTime - AdjustedDateTime.GameTime;
                bool bInHours = HowLong > TimeSpan.FromMinutes(120.0);

                // what we want the Town Crier to say
                string[] lines = new string[4];
                lines[0] = string.Format(
                    "Server Wars starting in about {0} {1}.",
                    bInHours ? (int)HowLong.TotalHours : (int)HowLong.TotalMinutes,
                    bInHours ? (int)HowLong.TotalHours == 1 ? "hour" : "hours" : (int)HowLong.TotalMinutes == 1 ? "minute" : "minutes");

                lines[1] = string.Format("There are no World Saves during Server Wars.");
                lines[2] = string.Format("Please plan accordingly.");
                lines[3] = string.Format("Wait for the system message stating server wars have started.");

                // use the smaller of the time spans
                TimeSpan s1 = EventStartTime - AdjustedDateTime.GameTime;
                TimeSpan s2 = TimeSpan.FromMinutes(20.0);
                TimeSpan span = (s1 > s2) ? s2 : s1;

                // Setup the Town Crier with a new message
                AddTCEntry(lines, span);
                Console.WriteLine("ServerWarsAES: Issue Town Crier pre event announcement.");
            }
            // during event announcements
            else if (EventStarted == true && EventEnded == false && FinalSaveComplete())
            {
                // remainder of event
                TimeSpan span = EventEndTime - AdjustedDateTime.GameTime;

                // Setup the Town Crier with a new message
                string[] lines = new string[2];
                lines[0] = "Server Wars have begun.";
                lines[1] = "Head to West Britain Bank for PvP.";
                AddTCEntry(lines, span);
                Console.WriteLine("ServerWarsAES: Issue Town Crier during event announcement.");
            }
            // post event announcements
            else if (EventStarted == true && EventEnded == true)
            {
                // 20 minutes should do it
                TimeSpan span = TimeSpan.FromMinutes(20.0);

                // Setup the Town Crier with a new message
                string[] lines = new string[1];
                lines[0] = "We hope you enjoyed Server Wars.";
                AddTCEntry(lines, span);
                Console.WriteLine("ServerWarsAES: Issue Town Crier post event announcement.");
            }
        }

        public override void OnBroadcast()
        {
            // issue a final event announcement
            TimeSpan HowLong = EventStartTime - AdjustedDateTime.GameTime;
            bool bInHours = HowLong > TimeSpan.FromMinutes(120.0);

            string line = string.Format(
                "Server Wars starting in about {0} {1}.",
                bInHours ? (int)HowLong.TotalHours : (int)HowLong.TotalMinutes,
                bInHours ? (int)HowLong.TotalHours == 1 ? "hour" : "hours" : (int)HowLong.TotalMinutes == 1 ? "minute" : "minutes");

            World.Broadcast(0x22, true, line);
            Console.WriteLine("ServerWarsAES: Issue final Broadcast announcement.");
        }

        public override void OnFinalCleanup()
        {
            Console.WriteLine("ServerWarsAES: In final cleanup.");
        }

        public override void OnEvent()
        {
            Console.WriteLine("ServerWarsAES: OnEvent()");

            // make sure to not turn on TestCenter until we have done the LAST save before the restart.
            //	we also place some reagent and potion stones and turn off brit guards
            if (EventStarted == true && EventEnded == false)
            {   // only do these after the final save
                if (FinalSaveComplete() == true)
                {
                    OnInitializeServerWars();       // 1
                    OnInitializeTestCenter();       // 2
                    OnConstruct();                  // 3
                }
            }

            // need to process base class events
            base.OnEvent();

            // print diagnostics
            base.DumpState();
        }

        private bool FinalSaveComplete()
        {
            return (Server.Misc.AutoSave.SavesEnabled == false && Server.Misc.AutoRestart.Restarting == true);
        }

        // setup NoSaves so that the global Server.Misc.TestCenter.ServerWars() == true
        private void OnInitializeServerWars()
        {
            // Set NoSaves and announce to the world "Server Wars have begun!"
            if (World.SaveType != World.SaveOption.NoSaves)
            {
                Console.WriteLine("ServerWarsAES: Initializing ServerWars");
                World.SaveType = World.SaveOption.NoSaves;              // disable all saves from any source
                string line = string.Format("Server Wars have begun!");
                World.Broadcast(0x22, true, line);                      // tell the world
            }
        }

        // setup test center
        private void OnInitializeTestCenter()
        {
            // if test center is off, turn it on
            if (Server.Engines.TestCenterServer.Enabled == false)
            {   // tell the test center to initialize
                if (Server.Engines.TestCenterServer.ServerWars() == true)
                    Console.WriteLine("ServerWarsAES: Initialize TestCenter");
                else
                    Console.WriteLine("ServerWarsAES: FAILED to Initialize TestCenter");
            }
        }

        private void OnConstruct()
        {
            // if we've not built anything yet, build it now
            if (m_construction == false)
            {   // record that we've built what we need to build (no need to serialize since the portion of the event is not saved)
                m_construction = true;
                // turn off new player starting area
                CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.NewPlayerStartingArea);
                // potion and reagent stones
                Console.WriteLine("ServerWarsAES: Do Construction");
                PotionStone ps = new PotionStone();
                RegStone rs = new RegStone();
                ps.MoveToWorld(new Point3D(1421, 1697, 0), Map.Felucca);
                rs.MoveToWorld(new Point3D(1421, 1698, 0), Map.Felucca);
                // turn off guards
                Region? town = Region.FindByName("Britain", Map.Felucca);         // Britain specific
                GuardedRegion? reg = town as GuardedRegion;
                if (reg != null) reg.IsGuarded = false;
                // report any errors
                if (reg != null && ps != null && rs != null)
                {
                    Console.WriteLine("ServerWarsAES: Construction Complete");
                    World.Broadcast(0x35, true, "There are potion and reagent stones at West Britain Bank.");
                    World.Broadcast(0x35, true, "The guards in Britain are now off.");
                }
                else
                    Console.WriteLine("ServerWarsAES: Construction FAILED");
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {   // all done
                        break;
                    }

                case 1:
                    {
                        goto case 0;
                    }
            }
        }
    }
    #endregion ServerWarsAES
}