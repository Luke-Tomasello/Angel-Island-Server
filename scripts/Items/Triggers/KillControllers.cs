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

/* Scripts\Items\Triggers\KillControllers.cs
 * CHANGELOG:
 *  5/1/2024, Adam
 * 		Initial version.
 * 		KillMonitor: Monitors kills, keeps a tally per player
 * 		KillHandler: Quires KillMonitor, issues messages, triggers when threshold is met
 */

using Server.Engines.ChampionSpawn;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Server.Items.Triggers
{
    [NoSort]
    public class KillMonitor : Item, ITriggerable, ITrigger
    {
        public override string DefaultName { get { return "Kill Monitor"; } }
        #region Props
        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }
        private Item m_Link;
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }
        private StateController m_Controller;
        //[CommandProperty(AccessLevel.GameMaster)]
        //public StateController StateController
        //{
        //    get { return m_Controller; }
        //    set { m_Controller = value; }
        //}
        #region Monitors
        private new Item[] Monitors = new Item[6];
        private void SetMonitor(int index, Item value)
        {
            if (value is Spawner || value is ChampEngine || value is null)
                Monitors[index] = value;
            else
                SendSystemMessage("You may only monitor spawners and champ engines.");
        }
        private Item GetMonitor(int index)
        {
            Defrag();
            return Monitors[index];
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Monitor1
        {
            get { return GetMonitor(0); }
            set { SetMonitor(0, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Monitor2
        {
            get { return GetMonitor(1); }
            set { SetMonitor(1, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Monitor3
        {
            get { return GetMonitor(2); }
            set { SetMonitor(2, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Monitor4
        {
            get { return GetMonitor(3); }
            set { SetMonitor(3, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Monitor5
        {
            get { return GetMonitor(4); }
            set { SetMonitor(4, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Monitor6
        {
            get { return GetMonitor(5); }
            set { SetMonitor(5, value); }
        }
        #endregion Monitors
        private TimeSpan m_Remember;
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Remember
        {
            get { return m_Remember; }
            set { m_Remember = value; }
        }
        public enum _Scope
        {
            Mobile,
            OneAtATime
        }
        private _Scope m_Scope = _Scope.Mobile;
        //[CommandProperty(AccessLevel.GameMaster)]
        public _Scope Scope
        {
            get { return m_Scope; }
            set { m_Scope = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Clear
        {
            get { return false; }
            set
            {
                if (value)
                    Memory = new();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Tripped
        {
            get { Defrag(); return Memory.Count > 0; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Report
        {
            get
            {
                Defrag();
                foreach (var kvp in Memory)
                    SendSystemMessage(string.Format("{0}: {1} kills, {2} points, {3} time remains",
                        kvp.Key.Name, kvp.Value.Kills, kvp.Value.Points, kvp.Value.DateTime - DateTime.UtcNow
                        ));

                return "done.";
            }
        }
        #endregion Props

        [Constructable]
        public KillMonitor()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
            Registry.Add(this);
        }
        public KillMonitor(Serial serial)
            : base(serial)
        {
            Registry.Add(this);
        }
        public bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return true;
        }
        public void OnTrigger(Mobile from)
        {
            if (from is PlayerMobile pm)
            {
                if (pm.KillQueue == null)
                    pm.KillQueue = new();

                while (pm.KillQueue.Count > 0)
                    ProcessKilled(pm.KillQueue.Dequeue());
            }
        }
        public override void OnDelete()
        {
            Registry.Remove(this);
            base.OnDelete();
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public void OnKill(Mobile from)
        {
            if (TriggerSystem.CheckEvent(this))
            {
                if (TriggerSystem.CheckTrigger(from, m_Link))
                    ;// debug
                else
                    ;
            }
            else
                ; // debug
        }
        public class PlayerUnit
        {
            private DateTime m_DateTime;
            private uint m_Kills;
            private uint m_Points;

            public DateTime DateTime => m_DateTime;
            public uint Kills { get { return m_Kills; } set { m_Kills = value; } }
            public uint Points { get { return m_Points; } set { m_Points = value; } }
            //public PlayerUnit(uint kills, uint points)
            //    : this(DateTime.UtcNow, kills, points)
            //{}
            public PlayerUnit(DateTime dateTime, uint kills, uint points)
            {
                m_DateTime = dateTime;
                m_Kills = kills;
                m_Points = points;
            }

        }
        private Dictionary<Mobile, PlayerUnit> Memory = new();
        private void Defrag()
        {   // first defrag memorty
            List<Mobile> list = new List<Mobile>();
            foreach (var kvp in Memory)
                if (DateTime.UtcNow > kvp.Value.DateTime || !Available(kvp.Key))
                    list.Add(kvp.Key);

            foreach (var m in list)
                Memory.Remove(m);

            // now defrag monitors
            for (int ix = 0; ix < Monitors.Length; ix++)
                if (Monitors[ix] != null && Monitors[ix].Deleted)
                    Monitors[ix] = null;
        }
        private bool Available(Mobile m)
        {
            if (m.Alive && m.Map == Map)
                // distance check?
                return true;

            return false;
        }
        #region External Access
        public PlayerUnit GetPlayerTallies(Mobile m)
        {
            Defrag();
            PlayerUnit pu = null;
            if (Memory.ContainsKey(m))
                pu = Memory[m];
            return pu;
        }
        public void ClearState(Mobile m)
        {
            if (Memory.ContainsKey(m))
                Memory.Remove(m);
        }
        #endregion External Access

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(m_Controller);

            // version 1
            writer.Write(Memory.Count);
            foreach (var kvp in Memory)
            {
                writer.Write(kvp.Key);
                writer.WriteDeltaTime(kvp.Value.DateTime);
                writer.Write(kvp.Value.Kills);
                writer.Write(kvp.Value.Points);
            }

            writer.Write(m_Link);
            writer.Write(m_Event);
            writer.Write((TimeSpan)m_Remember);
            writer.Write((int)m_Scope);
            for (int ix = 0; ix < Monitors.Length; ix++)
                writer.Write(Monitors[ix]);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Controller = (StateController)reader.ReadItem();
                        goto case 1;
                    }
                case 1:
                    {
                        int count = reader.ReadInt();
                        Mobile m = null;
                        DateTime dt;
                        uint kills;
                        uint points;
                        for (int ix = 0; ix < count; ix++)
                        {
                            m = reader.ReadMobile();
                            dt = reader.ReadDeltaTime();
                            kills = reader.ReadUInt();
                            points = reader.ReadUInt();
                            if (m != null)
                                Memory.Add(m, new PlayerUnit(dt, kills, points));
                        }

                        m_Link = reader.ReadItem();
                        m_Event = reader.ReadString();
                        m_Remember = reader.ReadTimeSpan();
                        m_Scope = (_Scope)reader.ReadInt();
                        for (int ix = 0; ix < Monitors.Length; ix++)
                            Monitors[ix] = reader.ReadItem();
                        break;
                    }
            }
        }
        public void MonitorMobileKilled(Mobile m)
        {
            if (m is BaseCreature bc && Monitors.Contains(bc.SpawnerHandle))
                ProcessKilled(m);
        }
        public void ProcessKilled(Mobile m)
        {
            Defrag();
            if (m is BaseCreature bc)
            {   // find legit damagers
                List<DamageStore> list = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);

                // divvy up points between party members
                SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(list);

                // anti-cheezing: Players have found that healing a monster (or allowing it to heal itself,) can yield unlimited damage points.
                //  We therefore limit the damage points to no more than the creature's HitsMax
                BaseCreature.ClipDamageStore(ref Results, bc);

                foreach (var kvp in Results)
                {
                    if (m_Controller != null)                   // by default, we will always trigger on a kill
                        if (!m_Controller.HasState(kvp.Key))    // if they associate a StateController, we will condition our trigger on that
                            continue;                           // if the StateController doesn't know about this player, no trigger, no credit

                    PlayerUnit pu = Memory.ContainsKey(kvp.Key) ? Memory[kvp.Key] : null;
                    if (pu == null)
                        pu = new PlayerUnit(DateTime.UtcNow + Remember, kills: 0, points: 0);
                    else
                        pu = Memory[kvp.Key];

                    // everyone gets credit for the kill (points tell the true story)
                    Memory[kvp.Key] = new PlayerUnit(pu.DateTime, pu.Kills + 1, pu.Points + (uint)kvp.Value);
                    DebugLabelTo(string.Format("{0}: {1} kills, {2} points", kvp.Key.Name, Memory[kvp.Key].Kills, Memory[kvp.Key].Points));
                    OnKill(kvp.Key);
                }
            }
        }
        public static void Configure()
        {
            EventSink.SpawnedMobileKilled += new SpawnedMobileKilledEventHandler(EventSink_SpawnedMobileKilled);
        }
        private static List<Item> Registry = new();
        public static void EventSink_SpawnedMobileKilled(SpawnedMobileKilledEventArgs e)
        {
            if (e is SpawnedMobileKilledInfo info && info.Mobile is BaseCreature bc)
            {
                foreach (Item item in Registry)
                    if (item is KillMonitor km)
                        km.MonitorMobileKilled(info.Mobile);
            }
        }
    }
    [NoSort]
    public class KillHandler : Item, ITriggerable, ITrigger
    {
        public override string DefaultName { get { return "Kill Handler"; } }

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }

        private Item m_Link;
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        private Item m_Else;
        [CommandProperty(AccessLevel.GameMaster)]
        public Item Else
        {
            get { return m_Else; }
            set { m_Else = value; }
        }

        private string m_Thresholds;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Thresholds
        {
            get { return m_Thresholds; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    int[] tab = Utility.IntParser(value);
                    if (tab.Length >= 1 && tab.Length <= 6)
                        m_Thresholds = NormalizeString(tab);
                    else
                    {
                        SendSystemMessage("Invalid threshold specified. Max 6");
                        SendSystemMessage("Example: 1, 5, 9, 10");
                        SendSystemMessage("In this example: 10 would be the trigger threshold.");
                    }
                }
                else
                    m_Thresholds = value;
            }
        }

        private KillMonitor m_Controller;
        [CommandProperty(AccessLevel.GameMaster)]
        public KillMonitor KillMonitor
        {
            get { return m_Controller; }
            set { m_Controller = value; }
        }
        #region Messages
        private string[] Messages = new string[6];
        private void SetMessage(int index, string value)
        {
            Messages[index] = value;
        }
        private string GetMessage(int index)
        {
            return Messages[index];
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Message1
        {
            get { return GetMessage(0); }
            set { SetMessage(0, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Message2
        {
            get { return GetMessage(1); }
            set { SetMessage(1, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Message3
        {
            get { return GetMessage(2); }
            set { SetMessage(2, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Message4
        {
            get { return GetMessage(3); }
            set { SetMessage(3, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Message5
        {
            get { return GetMessage(4); }
            set { SetMessage(4, value); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string Message6
        {
            get { return GetMessage(5); }
            set { SetMessage(5, value); }
        }
        #endregion Messages
        private Broadcaster m_Broadcaster;
        [CommandProperty(AccessLevel.GameMaster)]
        public Broadcaster Broadcaster
        {
            get { return m_Broadcaster; }
            set { m_Broadcaster = value; }
        }

        private bool m_ResetOnSuccess = true;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool ResetOnSuccess
        {
            get { return m_ResetOnSuccess; }
            set { m_ResetOnSuccess = value; }
        }

        [Constructable]
        public KillHandler()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            //SendSystemMessage(string.Format("True Branch {0}", TrueBranch == null ? "(null)" : TrueBranch.ToString()));
            //SendSystemMessage(string.Format("False Branch {0}", FalseBranch == null ? "(null)" : FalseBranch.ToString()));
            //if (KillMonitor != null)
            //{
            //    bool hasState = KillMonitor.GetPlayerTallies(from);
            //    SendSystemMessage(string.Format("{0} has state {1} for {2}", KillMonitor, hasState, from));
            //}
            //else
            //    SendSystemMessage("No KillMonitor link.");
            //if (TrueBranch != null)
            //{
            //    TrueBranch.Blink();
            //    TrueBranch.LabelTo(from, "(True)");
            //}
            //if (FalseBranch != null)
            //{
            //    FalseBranch.Blink();
            //    FalseBranch.LabelTo(from, "(False)");
            //}
            //if (KillMonitor != null)
            //{
            //    KillMonitor.Blink();
            //    KillMonitor.LabelTo(from, "(Source)");
            //}
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        private string NormalizeString(int[] ints)
        {
            string output = string.Empty;
            foreach (int @int in ints)
                output += @int.ToString() + ", ";

            return output.Trim(new char[] { ' ', ',' });
        }
        public bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            bool result;
            bool canTrigger = CheckCondition(from, out result) && TriggerSystem.CanTrigger(from, result ? m_Link : m_Else);
            DoMessage(from, CheckCondition(from, out result), @else: false);
            return canTrigger;
        }
        private void DoMessage(Mobile from, bool canTrigger, bool @else)
        {
            if (canTrigger == true && m_Broadcaster is Broadcaster bc && !bc.Deleted)
            {
                if (!string.IsNullOrEmpty(m_Thresholds))
                {
                    int tally = 0;
                    int[] tab = Utility.IntParser(m_Thresholds);
                    if (tab.Length >= 1 && tab.Length <= 6)
                        if ((tally = GetPlayerTallies(from)) > 0)
                        {
                            if (tab.Contains(tally))
                            {
                                int msgIndex = tab.ToList().IndexOf(tally);
                                if (!string.IsNullOrEmpty(GetMessage(msgIndex)))
                                {
                                    if (!Utility.StringTooSoon(source: from, second_timeout: 5.0, text: GetMessage(msgIndex)))
                                    {
                                        bc.SetMessage = GetMessage(msgIndex);
                                        TriggerSystem.CheckTrigger(from, bc);
                                    }
                                }
                            }
                        }
                }
            }
        }
        public void OnTrigger(Mobile from)
        {
            bool result;

            bool canTrigger = CheckCondition(from, out result);
            if (canTrigger)
                TriggerSystem.OnTrigger(from, result ? m_Link : m_Else);

            if (result && ResetOnSuccess)
                if (m_Controller != null)
                    m_Controller.ClearState(from);
        }
        private bool CheckCondition(Mobile from, out bool result)
        {
            result = false;
            if (GetPlayerTallies(from) == -1)
            {
                SendSystemMessage("this player is not being tracked");
                return false;
            }
            if (GetThresholdValue() == -1)
            {
                SendSystemMessage("no threshold values configured");
                return false;
            }

            // they are tracked
            // Link otherwise Else
            result = GetPlayerTallies(from) >= GetThresholdValue();
            return true;
        }
        public int GetPlayerTallies(Mobile m)
        {
            KillMonitor.PlayerUnit pu;
            if (m_Controller != null && (pu = m_Controller.GetPlayerTallies(m)) != null)
                return (int)pu.Kills;

            return -1;
        }
        private int GetThresholdValue()
        {
            if (!string.IsNullOrEmpty(m_Thresholds))
            {
                int[] tab = Utility.IntParser(m_Thresholds);
                if (tab.Length >= 1 && tab.Length <= 6)
                    return tab.Max();
            }
            return -1;
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        public KillHandler(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_Event);
            writer.Write((Item)m_Link);
            writer.Write((Item)m_Else);
            writer.Write((Item)m_Controller);
            writer.Write(m_ResetOnSuccess);
            writer.Write(m_Thresholds);
            writer.Write(m_Broadcaster);
            foreach (var @string in Messages)
                writer.Write(@string);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Event = reader.ReadString();
                        m_Link = reader.ReadItem();
                        m_Else = reader.ReadItem();
                        m_Controller = (KillMonitor)reader.ReadItem();
                        m_ResetOnSuccess = reader.ReadBool();
                        m_Thresholds = reader.ReadString();
                        m_Broadcaster = (Broadcaster)reader.ReadItem();
                        for (int ix = 0; ix < Messages.Length; ix++)
                            Messages[ix] = reader.ReadString();
                        break;
                    }
            }
        }
    }
}