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

/* Items/Triggers/MotionController.cs
 * CHANGELOG:
 *  5/25/2024, Adam
 *      Split up TargetSpawner from the TargetItem and TargetMobile as Spawners are Items as well, and confuses staff.
 *      Now if you select TargetItem and target a spawner, it's treated like any other item 
 *      If you select TargetSpawner, all operations act on the objects on the spawner
 *  4/25/2024, Adam (Spawner)
 *      Allow targeting a Spawner. When we do this, we operate on all spawner objects, but not the spawner itself.
 *  3/20/2024, Adam (CanTrigger)
 *      Reversed the sense of the test on can trigger ... so no motion controllers were moving
 *  2/21/2024, Adam (CanTrigger())
 *      Change to !Running && !Paused
 *          Paused: FDBackup dupes these and also things like a (trigger) Lever.
 *          when CopyProperties sets the IsSwitched property, that logic fires a signal to the motion controller, and the
 *          motion controller starts moving. This movement messes up capture as instantiating the duplicates will have
 *          objects in mid motion.
 *  7/19/2023, Adam (MotionType.Flip/Loot.FlipTable)
 *      Add the Loot.FlipTable for those flipable items that have no [flipable meta data, a 'wall torch' for example
 *  7/11/23, Yoar
 *      Added Manual_Stop
 *  6/4/23, Yoar
 *      Now supporting Mobile targets
 *      If no target is specified, then target the Mobile who triggered the controller
 * 	3/8/23, Yoar
 * 		Rewrote entirely.
 * 	2/11/22, Yoar
 * 		Initial version.
 */

using Server.Commands;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Items.Triggers
{
    [NoSort]
    [TypeAlias("Server.Items.MotionController")]
    public class MotionController : Item, ITriggerable, ITrigger
    {
        public enum MotionType : byte
        {
            None,
            Set,
            Inc,
            Toggle,
            Flip,
        }

        public const int Limit = 6;

        public override string DefaultName { get { return "Motion Controller"; } }

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }

        private IEntity m_Target;
        private List<Frame> m_Frames; // note: may contain null values

        private TimeSpan m_Interval;

        private bool m_Reversable;
        private bool m_Seesaw;

        private Item m_Link;

        private bool m_Raised;

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Item TargetItem
        {
            get { return m_Target as Item; }
            set
            {
                if (value != null)
                    m_Spawner = null;
                m_Target = value;
            }
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile TargetMobile
        {
            get { return m_Target as Mobile; }
            set
            {
                if (value != null)
                    m_Spawner = null;
                m_Target = value;
            }
        }

        private Spawner m_Spawner;
        //[CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Spawner TargetSpawner
        {
            get { return m_Spawner; }
            set
            {
                if (value != null)
                    m_Target = null;
                m_Spawner = value;
            }
        }

        [CopyableAttribute(CopyType.Copy)]  // CopyProperties will copy this one and not the DoNotCopy ones
        public IEntity Target { get { return m_Target; } set { m_Target = value; } }

        private bool m_TreatSpawnerAsItem = true;
        //[CommandProperty(AccessLevel.GameMaster)]
        public bool TreatSpawnerAsItem
        {
            get { return m_TreatSpawnerAsItem; }
            set { m_TreatSpawnerAsItem = value; }
        }

        public List<Frame> Frames
        {
            get { return m_Frames; }
            set { m_Frames = value; }
        }

        #region Frame Accessors

        private Frame GetFrame(int index)
        {
            if (index >= 0 && index < m_Frames.Count)
                return m_Frames[index];

            return null;
        }

        private void SetFrame(int index, Frame value)
        {
            if (index < 0 || index >= Limit)
                return;

            while (index >= m_Frames.Count)
                m_Frames.Add(null);

            m_Frames[index] = value;
        }

        private void EnsureFrames()
        {
            while (m_Frames.Count < Limit)
                m_Frames.Add(null);

            for (int i = 0; i < m_Frames.Count && i < Limit; i++)
            {
                if (m_Frames[i] == null)
                {
                    Frame frame = new Frame();

                    frame.Repeats = 0;

                    m_Frames[i] = frame;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Frame Frame1
        {
            get { return GetFrame(0); }
            set { SetFrame(0, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Frame Frame2
        {
            get { return GetFrame(1); }
            set { SetFrame(1, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Frame Frame3
        {
            get { return GetFrame(2); }
            set { SetFrame(2, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Frame Frame4
        {
            get { return GetFrame(3); }
            set { SetFrame(3, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Frame Frame5
        {
            get { return GetFrame(4); }
            set { SetFrame(4, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Frame Frame6
        {
            get { return GetFrame(5); }
            set { SetFrame(5, value); }
        }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Interval
        {
            get { return m_Interval; }
            set { m_Interval = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Reversable
        {
            get { return m_Reversable; }
            set { m_Reversable = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Seesaw
        {
            get { return m_Seesaw; }
            set { m_Seesaw = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Raised
        {
            get { return m_Raised; }
            set { m_Raised = value; }
        }

        [Constructable]
        public MotionController()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Frames = new List<Frame>();

            m_Interval = TimeSpan.FromSeconds(1.0);

            EnsureFrames();
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public void OnTrigger(Mobile from)
        {
            if (m_Frames.Count == 0 || Running)
                return;

            int totalTicks = GetTotalTicks();

            if (totalTicks <= 0)
                return;

            IEntity target = m_Spawner != null ? m_Spawner : m_Target;

            if (target == null)
                target = from;

            if (target == null)
                return;

            bool reverse = (m_Reversable && m_Raised);

            int tick;

            if (reverse)
                tick = totalTicks - 1;
            else
                tick = 0;

            DoTick(target, reverse, tick);

            if (totalTicks == 1)
            {
                if (m_Reversable)
                {
                    m_Raised = !reverse;

                    if (m_Seesaw && !reverse)
                    {
                        StartTimer(from, target, true, 1);
                        return;
                    }
                }

                TriggerSystem.CheckTrigger(from, m_Link);
            }
            else
            {
                StartTimer(from, target, reverse, tick);
            }
        }

        private int GetTotalTicks()
        {
            int total = 0;

            foreach (Frame frame in m_Frames)
            {
                if (frame == null)
                    continue;

                total += frame.Repeats;
            }

            return total;
        }
        private List<IEntity> GetEntities(IEntity ent)
        {
            List<IEntity> list = new();
            if (ent != null && ent.Deleted == false)
            {
                if (ent == TargetSpawner && ent is Spawner spawner)
                {
                    if (spawner.Objects != null && spawner.Objects.Count > 0)
                        foreach (object o in spawner.Objects)
                            if (o is Item item && !item.Deleted)
                                list.Add(item);
                            else if (o is Mobile mob && !mob.Deleted)
                                list.Add(mob);
                }
                else
                    list.Add(ent);
            }
            return list;
        }
        private void DoTick(IEntity ent, bool reverse, int tick)
        {
            foreach (IEntity entity in GetEntities(ent))
                DoTickInternal(entity, reverse, tick);
        }
        private void DoTickInternal(IEntity ent, bool reverse, int tick)
        {
            foreach (Frame frame in m_Frames)
            {
                if (frame == null)
                    continue;

                tick -= frame.Repeats;

                if (tick < 0)
                {
                    DoFrame(ent, reverse, frame);
                    break;
                }
            }
        }

        private static void DoFrame(IEntity ent, bool reverse, Frame frame)
        {
            if (ent == null || ent.Deleted)
                return;

            DoMotion(ent, reverse, frame.Motion, frame.PropName, frame.PropValue);

            if (frame.SoundID != -1)
            {
                Point3D worldLoc;

                if (ent is Item)
                    worldLoc = ((Item)ent).GetWorldLocation();
                else
                    worldLoc = ent.Location;

                Effects.PlaySound(worldLoc, ent.Map, frame.SoundID);
            }
        }

        private static void DoMotion(IEntity ent, bool reverse, MotionType motion, string propName, string propValue)
        {
            if (ent == null || ent.Deleted)
                return;

            switch (motion)
            {
                case MotionType.Set:
                    {
                        SetValue(ent, propName, propValue);

                        break;
                    }
                case MotionType.Inc:
                    {
                        IncValue(ent, reverse, propName, propValue);

                        break;
                    }
                case MotionType.Toggle:
                    {
                        ToggleValue(ent, propName);

                        break;
                    }
                case MotionType.Flip:
                    {
                        if (ent is Item)
                        {
                            Item item = (Item)ent;

                            FlipableAttribute[] attributes = (FlipableAttribute[])item.GetType().GetCustomAttributes(typeof(FlipableAttribute), false);

                            if (attributes.Length != 0)
                                attributes[0].Flip(item);
                            else if (Loot.FlipTable.ContainsKey(item.ItemID))
                            {
                                FlipableAttribute fa = null;
                                fa = new FlipableAttribute(Loot.FlipTable[item.ItemID]);
                                fa.Flip(item);
                            }
                        }

                        break;
                    }
            }
        }

        private static void SetValue(object obj, string propName, string propValue)
        {
            try
            {
                PropertyInfo prop = obj.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);

                if (prop == null)
                    return;

                // do not allow AccessLevel to be set
                if (prop.PropertyType == typeof(AccessLevel))
                    return;

                CommandPropertyAttribute cpa = Properties.GetCPA(prop);

                if (cpa == null || cpa.WriteLevel > AccessLevel.GameMaster)
                    return;

                object toSet = null;
                string result = Properties.ConstructFromString(prop.PropertyType, obj, propValue, ref toSet);

                if (result == null)
                    prop.SetValue(obj, toSet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void IncValue(object obj, bool reverse, string propName, string propValue)
        {
            try
            {
                PropertyInfo prop = obj.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);

                if (prop == null)
                    return;

                // do not allow AccessLevel to be set
                if (prop.PropertyType == typeof(AccessLevel))
                    return;

                CommandPropertyAttribute cpa = Properties.GetCPA(prop);

                if (cpa == null || cpa.ReadLevel > AccessLevel.GameMaster || cpa.WriteLevel > AccessLevel.GameMaster)
                    return;

                object curValue = prop.GetValue(obj, null);

                if (!(curValue is IConvertible))
                    return;

                int incValueAsInt = Utility.ToInt32(propValue);

                if (reverse)
                    incValueAsInt = -incValueAsInt;

                if (prop.PropertyType.IsEnum)
                {
                    long curValueAsLong = (long)Convert.ChangeType(Convert.ChangeType(curValue, Enum.GetUnderlyingType(prop.PropertyType)), TypeCode.Int64);
                    long newValueAsLong = curValueAsLong + incValueAsInt;

                    prop.SetValue(obj, Enum.ToObject(prop.PropertyType, newValueAsLong), null);
                }
                else
                {
                    long curValueAsLong = (long)Convert.ChangeType(curValue, TypeCode.Int64);
                    long newValueAsLong = curValueAsLong + incValueAsInt;

                    prop.SetValue(obj, Convert.ChangeType(newValueAsLong, prop.PropertyType), null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ToggleValue(object obj, string propName)
        {
            try
            {
                PropertyInfo prop = obj.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);

                if (prop == null)
                    return;

                // do not allow AccessLevel to be set
                if (prop.PropertyType == typeof(AccessLevel))
                    return;

                CommandPropertyAttribute cpa = Properties.GetCPA(prop);

                if (cpa == null || cpa.ReadLevel > AccessLevel.GameMaster || cpa.WriteLevel > AccessLevel.GameMaster)
                    return;

                object curValue = prop.GetValue(obj, null);

                if (!(curValue is IConvertible))
                    return;

                if (prop.PropertyType.IsEnum)
                {
                    long curValueAsLong = (long)Convert.ChangeType(Convert.ChangeType(curValue, Enum.GetUnderlyingType(prop.PropertyType)), TypeCode.Int64);
                    long newValueAsLong = (curValueAsLong == 0 ? 1 : 0);

                    prop.SetValue(obj, Enum.ToObject(prop.PropertyType, newValueAsLong), null);
                }
                else
                {
                    long curValueAsLong = (long)Convert.ChangeType(curValue, TypeCode.Int64);
                    long newValueAsLong = (curValueAsLong == 0 ? 1 : 0);

                    prop.SetValue(obj, Convert.ChangeType(newValueAsLong, prop.PropertyType), null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private InternalTimer m_Timer;

        public bool Running { get { return (m_Timer != null); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Stop
        {
            get { return false; }
            set
            {
                if (value)
                    StopTimer();
            }
        }

        private void StartTimer(Mobile from, IEntity target, bool reverse, int tick)
        {
            StopTimer();

            (m_Timer = new InternalTimer(this, from, target, reverse, tick)).Start();
        }

        private void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private class InternalTimer : Timer
        {
            private MotionController m_Controler;
            private Mobile m_From;
            private IEntity m_Target;
            private bool m_Reverse;
            private int m_Tick;

            public Mobile From { get { return m_From; } }
            public IEntity Target { get { return m_Target; } }
            public bool Reverse { get { return m_Reverse; } }
            public int Tick { get { return m_Tick; } }

            public InternalTimer(MotionController controller, Mobile from, IEntity target, bool reverse, int tick)
                : base(controller.Interval, controller.Interval)
            {
                m_Controler = controller;
                m_From = from;
                m_Target = target;
                m_Reverse = reverse;
                m_Tick = tick;
            }

            protected override void OnTick()
            {
                bool last;

                if (m_Reverse)
                    last = (--m_Tick <= 0);
                else
                    last = (++m_Tick >= m_Controler.GetTotalTicks() - 1);

                m_Controler.DoTick(m_Target, m_Reverse, m_Tick);

                if (last)
                {
                    if (m_Controler.Reversable)
                    {
                        m_Controler.Raised = !m_Reverse;

                        if (m_Controler.Seesaw && !m_Reverse)
                        {
                            m_Reverse = true;
                            m_Tick++;
                            return;
                        }
                    }

                    m_Controler.StopTimer();

                    // special case, I think here we want to allow the movement to continue and not stop mid swing
                    //if (TriggerSystem.CanTrigger(m_From, m_Controler)) // make sure WE can trigger
                    TriggerSystem.CheckTrigger(m_From, m_Controler.Link);
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(((ITriggerable)this).CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            StopTimer();
        }

        #region ITriggerable

        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return !Running;
        }

        void ITriggerable.OnTrigger(Mobile from)
        {
            OnTrigger(from);
        }

        #endregion

        public MotionController(Serial serial)
            : base(serial)
        {
            m_Frames = new List<Frame>();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)6); // version

            // version 6
            writer.Write(m_Spawner);

            // version 5 (obsolete)
            //writer.Write(m_TreatSpawnerAsItem);

            // version 4
            writer.Write(m_Event);

            // version 3
            if (m_Target is Item)
                writer.Write((Item)m_Target);
            else if (m_Target is Mobile)
                writer.Write((Mobile)m_Target);
            else
                writer.Write(Serial.MinusOne);

            writer.Write((int)m_Frames.Count);

            for (int i = 0; i < m_Frames.Count; i++)
            {
                if (m_Frames[i] != null)
                {
                    writer.Write((bool)true);

                    m_Frames[i].Serialize(writer);
                }
                else
                {
                    writer.Write((bool)false);
                }
            }

            writer.Write((TimeSpan)m_Interval);
            writer.Write((bool)m_Seesaw);
            writer.Write((bool)m_Raised);
            writer.Write((bool)m_Reversable);
            writer.Write((Item)m_Link);

            if (m_Timer != null)
            {
                writer.Write((bool)true);

                writer.Write((Mobile)m_Timer.From);

                if (m_Timer.Target is Item)
                    writer.Write((Item)m_Timer.Target);
                else if (m_Timer.Target is Mobile)
                    writer.Write((Mobile)m_Timer.Target);
                else
                    writer.Write(Serial.MinusOne);

                writer.Write((bool)m_Timer.Reverse);
                writer.Write((int)m_Timer.Tick);
            }
            else
            {
                writer.Write((bool)false);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 6:
                    {
                        m_Spawner = (Spawner)reader.ReadItem();
                        goto case 4;    // skip 5 (obsolete)
                    }
                case 5:
                    {   // obsolete in version 6)
                        m_TreatSpawnerAsItem = reader.ReadBool();
                        goto case 4;
                    }
                case 4:
                    {
                        m_Event = reader.ReadString();
                        goto case 3;
                    }
                case 3:
                case 2:
                case 1:
                    {
                        if (version < 2)
                        {
                            reader.ReadByte(); // Select
                            reader.ReadInt(); // Range
                        }

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 3)
                            m_Target = reader.ReadItem();
                        else
                            m_Target = World.FindEntity(reader.ReadInt());

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            if (version < 2 || reader.ReadBool())
                                m_Frames.Add(new Frame(reader));
                            else
                                m_Frames.Add(null);
                        }

                        if (version < 2)
                            reader.ReadTimeSpan(); // Delay

                        m_Interval = reader.ReadTimeSpan();
                        m_Seesaw = reader.ReadBool();
                        m_Raised = reader.ReadBool();
                        m_Reversable = reader.ReadBool();
                        m_Link = reader.ReadItem();

                        if (reader.ReadBool())
                        {
                            Mobile from = reader.ReadMobile();

                            IEntity target;

                            if (version < 3)
                                target = m_Target;
                            else
                                target = World.FindEntity(reader.ReadInt());

                            bool reverse = reader.ReadBool();
                            int tick = reader.ReadInt();

                            StartTimer(from, target, reverse, tick);
                        }

                        break;
                    }
            }

            EnsureFrames();
        }

        [PropertyObject]
        public class Frame
        {
            private MotionType m_Motion;
            private string m_PropName;
            private string m_PropValue;
            private int m_SoundID;
            private int m_Repeats;

            [CommandProperty(AccessLevel.GameMaster)]
            public MotionType Motion
            {
                get { return m_Motion; }
                set { m_Motion = value; }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public string PropName
            {
                get { return m_PropName; }
                set { m_PropName = value; }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public string PropValue
            {
                get { return m_PropValue; }
                set { m_PropValue = value; }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public int SoundID
            {
                get { return m_SoundID; }
                set { m_SoundID = value; }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public int Repeats
            {
                get { return m_Repeats; }
                set { m_Repeats = value; }
            }

            public Frame()
            {
                m_SoundID = -1;
                m_Repeats = 1;
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write((byte)0); // version

                writer.Write((byte)m_Motion);
                writer.Write((string)m_PropName);
                writer.Write((string)m_PropValue);
                writer.Write((int)m_SoundID);
                writer.Write((int)m_Repeats);
            }

            public Frame(GenericReader reader)
            {
                byte version = reader.ReadByte();

                m_Motion = (MotionType)reader.ReadByte();
                m_PropName = reader.ReadString();
                m_PropValue = reader.ReadString();
                m_SoundID = reader.ReadInt();
                m_Repeats = reader.ReadInt();
            }

            public override string ToString()
            {
                return String.Format("x{0}:{1}|{2}|{3}", m_Repeats, m_Motion, m_PropName, m_PropValue);
            }
        }
    }
}