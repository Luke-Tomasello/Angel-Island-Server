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

/* Scripts\Engines\Spawner\PushBackSpawner.cs
 * ChangeLog
 *  9/13/22, Adam 
 *      Add properties for:
 *          ConnectedSpawners
 *          DeactivatedSpawners
 *  9/6/22, Adam
 *		Initial Version!
 *		A Push Back Spawner (PBS) can be thought of a standalone champ level.
 *		PBSs are linked together into a 'Web' of levels. (While a single PBS is easily added, a web of them with initialization would be complex and tedious.)
 *		Each PBS has a List<> of the other spawners to which it is linked (siblings.) This is known as the 'Web'. This dictionary contains the actual serial numbers
 *		    of the other spawners, so each PBS can communicate with the others (siblings.)
 *		Each dictionary entry is KeyValuePair<PushBackSpawner, List<PushBackSpawner>>. The list is the siblings of the 'Key'. This structure is how an individual PBS can communicate
 *		    with the other PBSs in it's 'Web'.
 *		How it all works: Some automated system (example [PushbackWeb) creates N PBSs. The automated system 'links' the spawners, and sets up some parameters: Count, Mobile, Time between spawns, etc.
 *		    The PBSs then just kind of act like normal spawners .. they spawn mobiles.
 *		    1. When a spawned mobile is killed, the SpawnerKills is incremented, and a timer is started (SpawnerLevelTimeout)
 *		    2. When SpawnerKillsNeeded is reached, and SpawnerLevelTimeout has not elapsed, the PBS is deactivated, and the level is said to be 'complete'.
 *		    3. If however, SpawnerLevelTimeout elapses before SpawnerKillsNeeded is met, SpawnerKills is reset to zero, and the players start over for that level (spawner.)
 *		    4. When a PBS is deactivated (because SpawnerKillsNeeded is reached in the time alloted,) all the rest of the PBSs in the web still need to be contended with.
 *		When does it all end?: For lack of a better term, we will call all of the PDSs in the web - The Web Of Hate (WOH).
 *		    1. There are two ways to defeat the WOH:
 *		        1a. by deactivating each spawner individually by meeting its requisite SpawnerKillsNeeded.
 *		        1b. by reaching WebKillsNeeded by killing any mobiles on any of the PBSs. Timeouts still apply. I.e., you lose credit (SpawnerKillsNeeded) for any PBSs that timeout.
 *		PBS placement strategies: 
 *		    1. if you want to confound players, place them in randomized concentric circles. This will make it impossible for players to guess which PBS they are trying to deactivate.
 *		        they will be forced to take on the whole WOH at once.
 *		    2. In a line: If you want players to be able to systematically take out PBSs, sprinkle PBSs from the place players are most likely to encounter them => to the player's destination.
 *		        in this way, players will, almost by default, be 'deactivating' PBSs as they push forward to their destination. 
 *	    A few notes on the tools and such:
 *	        1. Console Messages:
 *	            1a. Green messages are from a specific spawner
 *	            1b. Blue messages are messages 'received' from another spawner
 *	        2. Tools: 
 *	            2a. [PushBackWeb is a demo tool and not intended for production use. [PushBackWeb creates 6 PBSs randomly placed about the GM.
 *	                [PushBackWeb sets the mobile to 'mongbat', count 4, SpawnerKillsNeeded 10, WebKillsNeeded 30, etc.
 *	            2b. [RemoveWeb allows you to target any PBS in the web and remove all of them.
 *	        3. Properties: Properties have two flavors:
 *	            3a. Web Global:
 *	                3a1. WebActive: activates or deactivates the entire web
 *	                3a2. WebKillsNeeded: how many kills, across all spawners, is needed to defeat the WOH
 *	                3a3. WebActive: Is the web running
 *	                3a4. WebTotalKills: total kills across all participants in the WOH
 *	                3a5. WebTotalMobs: total spawned mobiles across all participants in the WOH
 *	            3b. Spawner Local:
 *	                3b1. SpawnerDeactivate: activates/deactivates this spawner
 *	                3b2. SpawnerKills: how many kills on this spawner
 *	                3b3. SpawnerKillsNeeded: how many kills are needed to deactivate this spawner
 *	                3b4. SpawnerLevelRestartDelay: level completion timer for this spawner.
 */

using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Server.Utility;
namespace Server.Engines
{
    public class PushBackSpawner : EventSpawner
    {
        public static Dictionary<PushBackSpawner, List<PushBackSpawner>> Web = new();
        [Constructable]
        public PushBackSpawner()
            : base()
        {
            Web.Add(this, new List<PushBackSpawner>());     // add to our table
            m_SpawnerKillsNeeded = 4;                       // just some default: local to this spawner
            m_WebKillsNeeded = 4;                           // just some default: global to all web spawners
        }
        public override void OnDelete()
        {
            // kill the timer
            if (m_SpawnerLevelTimeout != null)
            {
                m_SpawnerLevelTimeout.Stop();   // stop the timer
                m_SpawnerLevelTimeout.Flush();  // remove any queued ticks
            }
            // we are no longer a sibling to anyone
            foreach (KeyValuePair<PushBackSpawner, List<PushBackSpawner>> kvp in Web)
                if (kvp.Value.Contains(this))
                    kvp.Value.Remove(this);

            // remove ourself from the table entirely
            Web.Remove(this);
            base.OnDelete();
        }
        public override void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
                return;

            if (from.AccessLevel == AccessLevel.Player)
            {
                LabelTo(from, string.Format("[{0}]", "Super Secret"));
                return;
            }
            if (EventSink.InvokeOnSingleClick(new OnSingleClickEventArgs(from, this)) == false)
            {
                // display the name
                NetState ns = from.NetState;
                ns.Send(new UnicodeMessage(this.Serial, this.ItemID, MessageType.Label, 0x3B2, 3, "ENU", "", "Push Back " + this.Name));
                int labels = 0;

                LabelTo(from, string.Format("[{0}/{1} kills]", m_SpawnerKills, m_SpawnerKillsNeeded));

                if (labels < 2)
                    if (Running)
                        LabelTo(from, "[Active]");
                    else
                        LabelTo(from, "[Deactivated]");
            }
        }
        #region Event Spawner Overrides
        public override void EventStarted(object o)
        {
            this.SpawnerDeactivated = false;
            base.Respawn();
            if (Core.Debug && false)
                Utility.Monitor.WriteLine(string.Format("{0} got 'Event started' event.", this), ConsoleColor.Yellow);
        }
        public override void EventEnded(object o)
        {
            this.SpawnerDeactivated = true;
            base.RemoveObjects();
            if (Core.Debug && false)
                Utility.Monitor.WriteLine(string.Format("{0} got 'Event ended' event.", this), ConsoleColor.Yellow);
        }
        #endregion Event Spawner Overrides
        public PushBackSpawner(Serial serial)
            : base(serial)
        {
        }
        private bool m_SpawnerDeactivated = false;
        private TimeSpan m_SpawnerLevelRestartDelay = new TimeSpan(0, 5, 0);
        private Timer m_SpawnerLevelTimeout = null;
        private int m_WebKillsNeeded = 0;               // global to this web of spawners
        private int m_SpawnerKillsNeeded = 0;           // local to this spawner
        private int m_SpawnerKills = 0;                 // local to this spawner
        private bool m_SpawnerLevelFail = false;
        [CommandProperty(AccessLevel.GameMaster)]
        public int WebKillsNeeded                       // global to this web of spawners
        {
            get { return m_WebKillsNeeded; }
            set
            {
                List<PushBackSpawner> list = Web[this];
                foreach (PushBackSpawner pbs in list)
                    pbs.WebKillsNeeded = value;
                this.m_WebKillsNeeded = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnerKillsNeeded { get { return m_SpawnerKillsNeeded; } set { m_SpawnerKillsNeeded = value; } }    // local to this web of spawners
        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnerKills { get { return m_SpawnerKills; } set { m_SpawnerKills = value; } }                      // local to this spawner
        [CommandProperty(AccessLevel.GameMaster)]
        public int WebTotalKills                                                                                        // global to this web of spawners
        {
            get
            {
                int total = 0;
                List<PushBackSpawner> list = Web[this];
                foreach (PushBackSpawner pbs in list)
                    total += pbs.SpawnerKills;
                total += this.SpawnerKills;
                return total;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan SpawnerLevelRestartDelay { get { return m_SpawnerLevelRestartDelay; } set { m_SpawnerLevelRestartDelay = value; } }
        public Timer SpawnerLevelTimeout { get { return m_SpawnerLevelTimeout; } set { m_SpawnerLevelTimeout = value; } }
        public override void SpawnedMobileKilled(Mobile killed)
        {
            SpawnerKills++;
            if (WebTotalKills >= WebKillsNeeded)
            {
                WebRunning = false;
                Utility.Monitor.WriteLine("Web of hate conquered!", ConsoleColor.Green);
            }
            else if (SpawnerKills >= SpawnerKillsNeeded && !SpawnerLevelFail)
            {   // level has been completed, kill the LevelRestartTimer
                SpawnerDeactivated = true;
                Utility.Monitor.WriteLine("LevelRestartTimer: stopped for {0}.", ConsoleColor.Green, this.Serial);
                ReceiveMessage(this, WebMessage.LevelCleared);
            }
            else if (SpawnerLevelFail)
            {   // starting over baby
                SpawnerLevelFail = false;
                SpawnerDeactivated = false;
                SpawnerKills = 0;
                Utility.Monitor.WriteLine("Failed to clear level in alloted time: {0}.", ConsoleColor.Green, this.Serial);
            }
            else if (!IsTimerRunning(m_SpawnerLevelTimeout))
            {
                m_SpawnerLevelTimeout = Timer.DelayCall(m_SpawnerLevelRestartDelay, new TimerStateCallback(LevelRestartTimerCallBack), this);
                Utility.Monitor.WriteLine("LevelRestartTimer: started for {0}.", ConsoleColor.Green, this.Serial);
            }
            // let PBSDistrictManager know what's going on
            EventSink.InvokeSpawnedMobileKilled(new SpawnedMobileKilled(killed, this));
        }
        private bool SpawnerLevelFail { get { return m_SpawnerLevelFail; } set { m_SpawnerLevelFail = value; } }
        [Flags]
        public enum WebMessage
        {
            None = 0x00,
            LevelCleared = 0x01,
            LevelRestart = 0x02,
            Registered = 0x04,
        }
        private void SendMessage(WebMessage msg)
        {
            List<PushBackSpawner> Siblings = Web[this];
            foreach (PushBackSpawner pbs in Siblings)
                if (pbs != this)
                    pbs.ReceiveMessage(this, msg);
                else
                    Utility.Monitor.WriteLine("Logic Error: The list of siblings should not include self.", ConsoleColor.Red);
        }
        public void ReceiveMessage(PushBackSpawner pbs, WebMessage msg)
        {
            switch (msg)
            {
                case WebMessage.LevelCleared:
                    {
                        Utility.Monitor.WriteLine("ReceiveMessage: LevelCleared for {0}.", ConsoleColor.Blue, pbs.Serial);
                        break;
                    }
                case WebMessage.LevelRestart:
                    {   // players were unable to clear the level on one of the PushBackSpawners in the web
                        Utility.Monitor.WriteLine("ReceiveMessage: LevelRestart for {0}.", ConsoleColor.Blue, pbs.Serial);
                        if (IsTimerRunning(m_SpawnerLevelTimeout))
                        {   // players failed to complete one PushBackSpawner in the web, therefore if they were working this spawner,
                            //  we will resume running.
                            m_SpawnerLevelTimeout.Stop();
                            m_SpawnerLevelTimeout.Flush();  // remove any queued ticks
                            Running = true;
                            Utility.Monitor.WriteLine("ReceiveMessage: LevelRestart: Resuming spawn for: {0}.", ConsoleColor.Green, this.Serial);
                        }
                        break;
                    }
                case WebMessage.Registered:
                    {   // too spammy
                        //Utility.ConsoleOut("ReceiveMessage: Registered: {0}.", ConsoleColor.Blue, pbs.Serial);
                        break;
                    }
            }
        }
        private void LevelRestartTimerCallBack(object state)
        {   // level has not been completed, resume spawning
            PushBackSpawner pbs = state as PushBackSpawner;
            pbs.SpawnerLevelFail = true;
            Utility.Monitor.WriteLine("LevelRestartTimer: executed for {0}.", ConsoleColor.Green, pbs.Serial);
            pbs.SendMessage(WebMessage.LevelRestart);
        }
        private bool IsTimerRunning(Timer t)
        {
            if (t != null && t.Running == true)
                return true;
            return false;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public override bool Running
        {
            get { return base.Running; }
            set
            {   // you cannot set a deactivated spawner to running
                //  (See TotalRespawn command
                if (value == true)
                {
                    if (m_SpawnerDeactivated == false)
                        base.Running = value;
                }
                else
                    base.Running = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool SpawnerDeactivated
        {
            get { return m_SpawnerDeactivated; }
            set
            {
                this.m_SpawnerDeactivated = value;
                if (value == true)
                {
                    if (IsTimerRunning(m_SpawnerLevelTimeout))
                    {
                        this.m_SpawnerLevelTimeout.Stop();
                        this.m_SpawnerLevelTimeout.Flush();  // remove any queued ticks
                        this.m_SpawnerLevelTimeout = null;
                    }
                    Running = false;
                    this.RemoveObjects();
                }
                else
                {
                    this.Running = true;
                    if (this.Objects == null || this.Objects.Count == 0)
                        this.Respawn();
                    else
                        // otherwise, just continue killing.
                        ; // debug break;

                }

                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool WebActive
        {
            get { return WebTotalMobs > 0; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool WebRunning
        {
            get { return WebTotalMobs > 0; }
            set
            {
                this.SpawnerKills = 0;
                this.SpawnerLevelFail = false;
                this.SpawnerDeactivated = !value;
                this.Running = value;
                if (value == true) this.Respawn(); else this.RemoveObjects();

                List<PushBackSpawner> list = Web[this];
                foreach (PushBackSpawner pbs in list)
                {
                    pbs.SpawnerKills = 0;
                    pbs.SpawnerLevelFail = false;
                    pbs.SpawnerDeactivated = !value;
                    pbs.Running = value;
                    if (value == true) pbs.Respawn(); else pbs.RemoveObjects();
                }

                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int WebTotalMobs
        {
            get
            {
                int total = 0;
                List<PushBackSpawner> list = Web[this];
                foreach (PushBackSpawner pbs in list)
                    total += MobilesOnSpawner(pbs);
                total += MobilesOnSpawner(this);
                return total;
            }
        }
        private int MobilesOnSpawner(PushBackSpawner pbs)
        {
            int total = 0;
            if (pbs.Objects != null)
                foreach (object obj in pbs.Objects)
                    if (obj is Mobile mobile && mobile.Deleted == false)
                        total++;
            return total;
        }
        public int ConnectedSpawners
        {
            get
            {   // siblings + me
                return Web[this].Count + 1;
            }
        }
        public int DeactivatedSpawners
        {
            get
            {
                int total = 0;
                List<PushBackSpawner> list = Web[this];
                foreach (PushBackSpawner pbs in list)
                    if (pbs.SpawnerDeactivated)
                        total++;
                if (this.SpawnerDeactivated)
                    total++;
                return total;
            }
        }
        #region STUFF YOU PROBABLY DON'T CARE ABOUT
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    {
                        writer.Write(m_SpawnerDeactivated);
                        writer.Write(m_SpawnerLevelRestartDelay);
                        writer.Write(m_WebKillsNeeded);
                        writer.Write(m_SpawnerKillsNeeded);
                        writer.Write(m_SpawnerKills);
                        writer.Write(m_SpawnerLevelFail);
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
                case 1:
                    {
                        m_SpawnerDeactivated = reader.ReadBool();
                        m_SpawnerLevelRestartDelay = reader.ReadTimeSpan();
                        m_WebKillsNeeded = reader.ReadInt();
                        m_SpawnerKillsNeeded = reader.ReadInt();
                        m_SpawnerKills = reader.ReadInt();
                        m_SpawnerLevelFail = reader.ReadBool();
                        break;
                    }
            }
        }
        #region PushBackCommands
        public new static void Initialize()
        {
            Server.CommandSystem.Register("PushBackWeb", AccessLevel.Administrator, new CommandEventHandler(PushBackWeb_OnCommand));
            Server.CommandSystem.Register("RemoveWeb", AccessLevel.Administrator, new CommandEventHandler(RemoveWeb_OnCommand));
        }
        public new static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(Load);
            EventSink.WorldSave += new WorldSaveEventHandler(Save);
        }
        public static void PushBackWeb_OnCommand(CommandEventArgs e)
        {
            List<PushBackSpawner> list = new();
            for (int ix = 0; ix < 6; ix++)
                list.Add(new PushBackSpawner());

            for (int ix = 0; ix < list.Count; ix++)
                list[ix].MoveToWorld(GetSpawnPosition(e.Mobile.Map, e.Mobile.Location, 10, SpawnFlags.None, list[ix]), e.Mobile.Map);

            for (int ix = 0; ix < list.Count; ix++)
            {
                list[ix].ObjectNamesRaw.Add("mongbat");
                list[ix].Running = true;
                list[ix].Count = 4;                                     // initial spawn levels - the most the spawner will produce at a time
                list[ix].MinDelay = TimeSpan.FromSeconds(1);            // don't make players wait. Remember, they are trying to complete the level in the time alloted
                list[ix].MaxDelay = TimeSpan.FromSeconds(2);            // same
                list[ix].SpawnerKillsNeeded = 10;                       // local to the spawner
                list[ix].WebKillsNeeded = 30;                           // global to the web of spawners
                list[ix].SpawnerLevelRestartDelay = TimeSpan.FromMinutes(5);   // how long you have to beat this level (spawner)
                PushBackSpawner.Web[list[ix]] = new List<PushBackSpawner>(list);
                list[ix].Respawn();
                list[ix].ReceiveMessage(list[ix], WebMessage.Registered);
            }

            // remove thyself
            foreach (KeyValuePair<PushBackSpawner, List<PushBackSpawner>> kvp in Web)
                if (kvp.Value.Contains(kvp.Key))
                    kvp.Value.Remove(kvp.Key);

        }
        public static void RemoveWeb_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target a spawner of the web.");
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(OnTarget));
        }
        private static void OnTarget(Mobile from, object target)
        {
            if (target is PushBackSpawner spawner)
            {
                List<PushBackSpawner> siblings = new();
                List<PushBackSpawner> toDelete = new();

                // find the siblings for this spawner
                if (PushBackSpawner.Web.ContainsKey(spawner))
                    siblings = PushBackSpawner.Web[spawner];

                // now look through the Web and select those keys (spawners) that are a part of this web
                foreach (KeyValuePair<PushBackSpawner, List<PushBackSpawner>> kvp in Web)
                    if (siblings.Contains(kvp.Key))
                        toDelete.Add(kvp.Key);

                // now we just delete each spawner in the web.
                //  We don't need to handle the list of siblings, or removing the spawner from the Web,
                //  that's handled in OnDelete()
                foreach (PushBackSpawner pbs in toDelete)
                    pbs.Delete();

                // now delete the target itself
                spawner.Delete();
            }
            else
            {
                from.SendMessage("You must target a PushBackSpawner.");
                return;
            }
        }
        #endregion PushBackCommands
        #region Web Database
        #region Serialization
        private static List<PushBackSpawner> Defrag(List<PushBackSpawner> al)
        {
            return al.Where(i => i != null && i.Deleted == false).ToList();
        }
        public new static void Load()
        {
            if (!File.Exists("Saves/PushBackWeb.bin"))
                return;

            Console.WriteLine("PushBackWeb Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/PushBackWeb.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Item item = reader.ReadItem();
                                if (item is not null && item.Deleted == false)
                                    Web.Add(item as PushBackSpawner, Defrag(reader.ReadItemList<PushBackSpawner>()));
                                else
                                    reader.ReadItemList(); // discard list
                            }
                            break;
                        }

                    default:
                        {
                            reader.Close();
                            throw new Exception("Invalid version in PushBackWeb.bin.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading PushBackWeb.bin, using default values:", ConsoleColor.Red);
            }
        }
        public new static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("PushBackWeb Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/PushBackWeb.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(Web.Count);
                            foreach (KeyValuePair<PushBackSpawner, List<PushBackSpawner>> kvp in Web)
                            {
                                writer.Write(kvp.Key);
                                writer.WriteItemList(kvp.Value);
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing PushBackWeb.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
        #endregion Web Database
        #endregion STUFF YOU PROBABLY DON'T CARE ABOUT
    }
}