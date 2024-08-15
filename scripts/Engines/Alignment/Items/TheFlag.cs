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

/* Scripts\Engines\Alignment\Items\TheFlag.cs
 * Changelog:
 *  4/30/23, Yoar
 *      Initial version. Alignment flag for event games.
 *      
 *      Currently supported game mode: King of the Hill.
 *      Planned game mode: Capture the Flag.
 *      
 *      King of the Hill:
 *      Changes hands to whichever alignment has players standing next to
 *      it. Displays "held time" of the top 3 alignments when single-
 *      clicked.
 *      
 *      Capture the Flag:
 *      Pick up/drop by double-clicking the flag. Works similarly to a
 *      sigil in that you cannot travel/perform certain actions when you
 *      have the flag in your pack.
 */

using Server.Items;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Engines.Alignment
{
    public enum GameType : byte
    {
        None,

        CaptureTheFlag,
        KingOfTheHill,
    }

    [FlipableAttribute]
    public class TheFlag : Item
    {
        public static TimeSpan DisplayInterval = TimeSpan.FromSeconds(3.0);

        public static void ReturnFlags(Mobile mob)
        {
            Container pack = mob.Backpack;

            if (pack == null)
                return;

            foreach (TheFlag flag in pack.FindItemsByType<TheFlag>())
                flag.ReturnHome();
        }

        public static bool ExistsOn(Mobile mob)
        {
            Container pack = mob.Backpack;

            return (pack != null && pack.FindItemByType(typeof(TheFlag)) != null);
        }

        public override string DefaultName { get { return "the flag"; } }

        private GameType m_GameType;

        private int m_BannerEastID;
        private int m_BannerSouthID;

        private int m_CarryHue;

        private int m_CaptureRange;

        private AlignmentType m_Alignment;
        private DateTime m_LastChangeDate;

        private Dictionary<AlignmentType, TimeSpan> m_HeldTimes;

        private HeldTimeResults m_Results;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public GameType GameType
        {
            get { return m_GameType; }
            set
            {
                if (m_GameType != value)
                {
                    m_GameType = value;

                    Reset();

                    if (m_GameType == GameType.None)
                        StopTimer();
                    else
                        StartTimer();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Running
        {
            get { return (m_Timer != null); }
            set
            {
                if (Running != value)
                {
                    if (value)
                        StartTimer();
                    else
                        StopTimer();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BannerEastID
        {
            get { return m_BannerEastID; }
            set { m_BannerEastID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BannerSouthID
        {
            get { return m_BannerSouthID; }
            set { m_BannerSouthID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CarryHue
        {
            get { return m_CarryHue; }
            set
            {
                if (m_CarryHue != value)
                {
                    m_CarryHue = value;

                    Mobile m = RootParent as Mobile;

                    if (m != null)
                        m.HueMod = m_CarryHue;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CaptureRange
        {
            get { return m_CaptureRange; }
            set { m_CaptureRange = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Placed
        {
            get { return (ItemID == m_BannerEastID || ItemID == m_BannerSouthID); }
            set
            {
                if (Placed != value)
                {
                    if (value)
                    {
                        if (ItemID == 0xDF0)
                            ItemID = m_BannerEastID;
                        else
                            ItemID = m_BannerSouthID;

                        IEntity ent = RootParent as IEntity;

                        if (ent != null)
                            MoveToWorld(ent.Location, ent.Map);
                    }
                    else
                    {
                        if (ItemID == m_BannerEastID)
                            ItemID = 0xDF0;
                        else
                            ItemID = 0xDF1;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public AlignmentType Alignment
        {
            get { return m_Alignment; }
            set
            {
                if (m_Alignment != value)
                {
                    if (m_Alignment != AlignmentType.None && m_LastChangeDate != DateTime.MinValue)
                    {
                        TimeSpan dt = (DateTime.UtcNow - m_LastChangeDate);

                        if (m_HeldTimes.ContainsKey(m_Alignment))
                            m_HeldTimes[m_Alignment] += dt;
                        else
                            m_HeldTimes[m_Alignment] = dt;
                    }

                    m_Alignment = value;

                    m_LastChangeDate = DateTime.UtcNow;

                    Hue = AlignmentSystem.GetHue(value);

                    PublicOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("THE FLAG CHANGED HANDS TO : {0}", ToUpper(AlignmentSystem.GetName(m_Alignment))));
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime LastChangeDate
        {
            get { return m_LastChangeDate; }
            set { m_LastChangeDate = value; }
        }

        public Dictionary<AlignmentType, TimeSpan> HeldTimes
        {
            get { return m_HeldTimes; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_ClearTimes
        {
            get { return false; }
            set
            {
                if (value)
                    ClearTimes();
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public HeldTimeResults Results
        {
            get { return m_Results; }
            set { }
        }

        [Constructable]
        public TheFlag()
            : base(0xDF1)
        {
            Movable = false;

            m_BannerEastID = 0x15B2;
            m_BannerSouthID = 0x15B3;

            m_CarryHue = 0xB;

            m_CaptureRange = 3;

            m_HeldTimes = new Dictionary<AlignmentType, TimeSpan>();

            m_Results = new HeldTimeResults();

            Reset();

            // a little hacky...
            Timer.DelayCall(TimeSpan.Zero, delegate { Placed = true; });
        }

        public void Reset()
        {
            Alignment = AlignmentType.None;

            ClearTimes();
        }

        public void ClearTimes()
        {
            m_HeldTimes.Clear();

            m_Results.Clear();
        }

        public void PlaceAt(Point3D loc, Map map)
        {
            MoveToWorld(loc, map);

            Placed = true;
        }

        private void Slice()
        {
            if (m_GameType == GameType.KingOfTheHill)
                CheckCapture();

            UpdateResults();
        }

        public void CheckCapture()
        {
            if (Parent != null)
                return;

            AlignmentType alignment = AlignmentType.None;

            foreach (Mobile m in GetMobilesInRange(m_CaptureRange))
            {
                if (!CanCapture(m))
                    continue;

                AlignmentType mobAlignment = AlignmentSystem.Find(m, true, true);

                if (mobAlignment == AlignmentType.None)
                    continue;

                if (alignment == AlignmentType.None)
                {
                    alignment = mobAlignment;
                }
                else if (alignment != mobAlignment)
                {
                    alignment = AlignmentType.None;
                    break;
                }
            }

            if (alignment != AlignmentType.None)
                Alignment = alignment;
        }

        private bool CanCapture(Mobile m)
        {
            return (m.Alive && !m.IsDeadBondedPet);
        }

        public void UpdateResults()
        {
            m_Results.Array = GetResults();
        }

        public HeldTimeResult[] GetResults()
        {
            DateTime now = DateTime.UtcNow;

            List<HeldTimeResult> results = new List<HeldTimeResult>();

            foreach (Alignment alignment in Engines.Alignment.Alignment.Table)
            {
                TimeSpan heldTime = GetHeldTime(alignment.Type, now);

                if (heldTime > TimeSpan.Zero)
                    results.Add(new HeldTimeResult(alignment.Type, heldTime));
            }

            results.Sort();

            return results.ToArray();
        }

        public TimeSpan GetHeldTime(AlignmentType alignment)
        {
            return GetHeldTime(alignment, DateTime.UtcNow);
        }

        private TimeSpan GetHeldTime(AlignmentType alignment, DateTime now)
        {
            TimeSpan ts;

            if (!m_HeldTimes.TryGetValue(alignment, out ts))
                ts = TimeSpan.Zero;

            if (m_Alignment == alignment && m_LastChangeDate != DateTime.MinValue)
                ts += (now - m_LastChangeDate);

            return ts;
        }

        public void ReturnHome()
        {
            Point3D homeLoc = Point3D.Zero;
            Map homeMap = Map.Felucca;

            // TODO: Check alignment flag homes

            if (homeLoc != Point3D.Zero)
                PlaceAt(homeLoc, homeMap);
            else
                Placed = true;
        }

        public override void OnSingleClick(Mobile from)
        {
            HeldTimeResult[] results = m_Results.Array;

            if (results.Length == 0)
            {
                base.OnSingleClick(from);
            }
            else
            {
                // display top 3 results
                for (int i = 0; i < m_Results.Array.Length && i < 3; i++)
                    DisplayResult(from, m_Results.Array[i]);
            }
        }

        private void DisplayResult(Mobile m, HeldTimeResult result)
        {
            string text = string.Format("{0} : {1}", ToUpper(AlignmentSystem.GetName(result.Alignment)), FormatTime(result.Time));

            if (m == null)
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, text);
            else
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, text, m.NetState);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!CanMove(from))
                return;

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (Placed)
            {
                if (!from.PlaceInBackpack(this))
                    from.SendMessage("You do not have room in your pack for the flag.");
                else
                    Placed = false;
            }
            else
            {
                if (this.IsChildOf(from.Backpack))
                {
                    from.SendMessage("Where do you wish to place the flag?");
                    from.Target = new PlaceTarget(this);
                }
            }
        }

        private bool CanMove(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            return (m_GameType != GameType.KingOfTheHill && AlignmentSystem.Find(m) != AlignmentType.None);
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);

            Mobile mob = FindOwner(parent);

            if (mob != null)
            {
                if (m_GameType == GameType.CaptureTheFlag)
                {
                    AlignmentType alignment = AlignmentSystem.Find(mob, true, true);

                    if (alignment != AlignmentType.None)
                        mob.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0} CAPTURED THE FLAG!!!", ToUpper(mob.Name)));
                }

                mob.SolidHueOverride = m_CarryHue;
            }
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            Mobile mob = FindOwner(parent);

            if (mob != null)
                mob.SolidHueOverride = -1;
        }

        private Mobile FindOwner(object parent)
        {
            if (parent is Item)
                return ((Item)parent).RootParent as Mobile;

            if (parent is Mobile)
                return (Mobile)parent;

            return null;
        }

        public override bool HandlesOnMovement { get { return (m_GameType == GameType.KingOfTheHill); } }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Parent == null && CanCapture(m))
            {
                bool inCaptureRangeOld = Utility.InRange(this.Location, oldLocation, m_CaptureRange);
                bool inCaptureRangeNew = Utility.InRange(this.Location, m.Location, m_CaptureRange);

                if (!inCaptureRangeOld && inCaptureRangeNew)
                    CheckCapture();
            }
        }

        public void Flip()
        {
            if (ItemID == BannerEastID)
            {
                ItemID = BannerSouthID;
            }
            else if (ItemID == BannerSouthID)
            {
                ItemID = BannerEastID;
            }
            else
            {
                switch (ItemID)
                {
                    case 0xDF0: ItemID = 0xDF1; break;
                    case 0xDF1: ItemID = 0xDF0; break;
                }
            }
        }

        public override void OnAfterDelete()
        {
            StopTimer();
        }

        public TheFlag(Serial serial)
            : base(serial)
        {
            m_HeldTimes = new Dictionary<AlignmentType, TimeSpan>();

            m_Results = new HeldTimeResults();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((int)m_CaptureRange);

            writer.Write((int)m_CarryHue);

            writer.Write((byte)m_GameType);

            writer.Write((int)m_BannerEastID);
            writer.Write((int)m_BannerSouthID);

            writer.Write((byte)m_Alignment);
            writer.WriteDeltaTime(m_LastChangeDate);

            writer.Write((int)m_HeldTimes.Count);

            foreach (KeyValuePair<AlignmentType, TimeSpan> kvp in m_HeldTimes)
            {
                writer.Write((byte)kvp.Key);
                writer.Write((TimeSpan)kvp.Value);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_CaptureRange = reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_CarryHue = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        m_GameType = (GameType)reader.ReadByte();

                        m_BannerEastID = reader.ReadInt();
                        m_BannerSouthID = reader.ReadInt();

                        m_Alignment = (AlignmentType)reader.ReadByte();
                        m_LastChangeDate = reader.ReadDeltaTime();

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            AlignmentType alignment = (AlignmentType)reader.ReadByte();
                            TimeSpan ts = reader.ReadTimeSpan();

                            if (ts != TimeSpan.Zero)
                                m_HeldTimes[alignment] = ts;
                        }

                        break;
                    }
            }

            if (version < 1)
                m_CarryHue = 0xB;

            if (version < 2)
                m_CaptureRange = 3;

            if (m_GameType != GameType.None)
                StartTimer();
        }

        private Timer m_Timer;

        public void StartTimer()
        {
            StopTimer();

            (m_Timer = new InternalTimer(this)).Start();
        }

        public void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        public static TimeSpan Interval = TimeSpan.FromSeconds(1.0);

        private class InternalTimer : Timer
        {
            private TheFlag m_Flag;

            public InternalTimer(TheFlag flag)
                : base(TheFlag.Interval, TheFlag.Interval)
            {
                m_Flag = flag;
            }

            protected override void OnTick()
            {
                m_Flag.Slice();
            }
        }

        private class PlaceTarget : Target
        {
            private TheFlag m_Flag;

            public PlaceTarget(TheFlag flag)
                : base(2, true, TargetFlags.None)
            {
                m_Flag = flag;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Map map = from.Map;

                if (map == null || m_Flag.Placed || !m_Flag.IsChildOf(from.Backpack))
                    return;

                Point3D loc = GetWorldLocation(targeted);

                if (loc == Point3D.Zero || !map.CanFit(loc, 16))
                    from.SendMessage("You cannot place the flag there.");
                else
                    m_Flag.PlaceAt(loc, map);
            }

            private static Point3D GetWorldLocation(object o)
            {
                return (o is Item ? ((Item)o).GetWorldLocation() : o is IPoint3D ? new Point3D((IPoint3D)o) : Point3D.Zero);
            }
        }

        public struct HeldTimeResult : IComparable<HeldTimeResult>
        {
            public static readonly HeldTimeResult Empty = new HeldTimeResult();

            public readonly AlignmentType Alignment;
            public readonly TimeSpan Time;

            public HeldTimeResult(AlignmentType alignment, TimeSpan time)
            {
                Alignment = alignment;
                Time = time;
            }

            int IComparable<HeldTimeResult>.CompareTo(HeldTimeResult other)
            {
                return other.Time.CompareTo(Time);
            }

            public override string ToString()
            {
                return string.Format("{0}: {1}", Alignment, Time);
            }
        }

        [NoSort]
        [PropertyObject]
        public class HeldTimeResults
        {
            private HeldTimeResult[] m_Array;

            public HeldTimeResult[] Array
            {
                get { return m_Array; }
                set { m_Array = value; }
            }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank1 { get { return GetResult(0); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank2 { get { return GetResult(1); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank3 { get { return GetResult(2); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank4 { get { return GetResult(3); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank5 { get { return GetResult(4); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank6 { get { return GetResult(5); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank7 { get { return GetResult(6); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank8 { get { return GetResult(7); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank9 { get { return GetResult(8); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank10 { get { return GetResult(9); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank11 { get { return GetResult(10); } }

            [CommandProperty(AccessLevel.Counselor)]
            public HeldTimeResult Rank12 { get { return GetResult(11); } }

            public HeldTimeResults()
            {
                Clear();
            }

            private HeldTimeResult GetResult(int index)
            {
                if (index >= 0 && index < m_Array.Length)
                    return m_Array[index];

                return HeldTimeResult.Empty;
            }

            public void Clear()
            {
                m_Array = new HeldTimeResult[0];
            }

            public override string ToString()
            {
                return "...";
            }
        }

        private static string ToUpper(string value)
        {
            return (value == null ? null : value.ToUpper());
        }

        private static string FormatTime(TimeSpan ts)
        {
            return ts.ToString(@"hh\:mm\:ss");
        }
    }
}