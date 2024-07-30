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

/* Scripts\Engines\Alignment\Items\KillCounter.cs
 * Changelog:
 *  5/22/23, Yoar
 *      Initial version. Tallies alignment kills
 */

using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Engines.Alignment
{
    public class KillCounter : Item
    {
        private static readonly List<KillCounter> m_Instances = new List<KillCounter>();

        public static List<KillCounter> Instances { get { return m_Instances; } }

        public static void RegisterKills(AlignmentType type)
        {
            foreach (KillCounter killCounter in m_Instances)
                killCounter.RegisterKill(type);
        }

        public override string DefaultName { get { return "kill counter"; } }

        private Dictionary<AlignmentType, int> m_Kills;

        private KillCountResults m_Results;

        public Dictionary<AlignmentType, int> Kills
        {
            get { return m_Kills; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_ClearKills
        {
            get { return false; }
            set
            {
                if (value)
                    ClearKills();
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public KillCountResults Results
        {
            get { return m_Results; }
            set { }
        }

        [Constructable]
        public KillCounter()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Kills = new Dictionary<AlignmentType, int>();

            m_Results = new KillCountResults();

            m_Instances.Add(this);
        }

        public void ClearKills()
        {
            m_Kills.Clear();

            m_Results.Clear();
        }

        public void RegisterKill(AlignmentType type)
        {
            if (m_Kills.ContainsKey(type))
                m_Kills[type]++;
            else
                m_Kills[type] = 1;

            UpdateResults();
        }

        public void UpdateResults()
        {
            m_Results.Array = GetResults();
        }

        public KillCountResult[] GetResults()
        {
            List<KillCountResult> results = new List<KillCountResult>();

            foreach (KeyValuePair<AlignmentType, int> kvp in m_Kills)
                results.Add(new KillCountResult(kvp.Key, kvp.Value));

            results.Sort();

            return results.ToArray();
        }

        public override void OnSingleClick(Mobile from)
        {
            KillCountResult[] results = m_Results.Array;

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

        private void DisplayResult(Mobile m, KillCountResult result)
        {
            string text = String.Format("{0} : {1}", AlignmentSystem.GetName(result.Alignment), result.Kills);

            if (m == null)
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, text);
            else
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, text, m.NetState);
        }

        public override void OnAfterDelete()
        {
            m_Instances.Remove(this);
        }

        public KillCounter(Serial serial)
            : base(serial)
        {
            m_Kills = new Dictionary<AlignmentType, int>();

            m_Results = new KillCountResults();

            m_Instances.Add(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Kills.Count);

            foreach (KeyValuePair<AlignmentType, int> kvp in m_Kills)
            {
                writer.Write((byte)kvp.Key);
                writer.Write((int)kvp.Value);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            AlignmentType type = (AlignmentType)reader.ReadByte();
                            int kills = reader.ReadInt();

                            m_Kills[type] = kills;
                        }

                        break;
                    }
            }

            UpdateResults();
        }

        public struct KillCountResult : IComparable<KillCountResult>
        {
            public static readonly KillCountResult Empty = new KillCountResult();

            public readonly AlignmentType Alignment;
            public readonly int Kills;

            public KillCountResult(AlignmentType alignment, int kills)
            {
                Alignment = alignment;
                Kills = kills;
            }

            int IComparable<KillCountResult>.CompareTo(KillCountResult other)
            {
                return other.Kills.CompareTo(Kills);
            }

            public override string ToString()
            {
                return String.Format("{0}: {1}", Alignment, Kills);
            }
        }

        [NoSort]
        [PropertyObject]
        public class KillCountResults
        {
            private KillCountResult[] m_Array;

            public KillCountResult[] Array
            {
                get { return m_Array; }
                set { m_Array = value; }
            }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank1 { get { return GetResult(0); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank2 { get { return GetResult(1); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank3 { get { return GetResult(2); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank4 { get { return GetResult(3); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank5 { get { return GetResult(4); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank6 { get { return GetResult(5); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank7 { get { return GetResult(6); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank8 { get { return GetResult(7); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank9 { get { return GetResult(8); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank10 { get { return GetResult(9); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank11 { get { return GetResult(10); } }

            [CommandProperty(AccessLevel.Counselor)]
            public KillCountResult Rank12 { get { return GetResult(11); } }

            public KillCountResults()
            {
                Clear();
            }

            private KillCountResult GetResult(int index)
            {
                if (index >= 0 && index < m_Array.Length)
                    return m_Array[index];

                return KillCountResult.Empty;
            }

            public void Clear()
            {
                m_Array = new KillCountResult[0];
            }

            public override string ToString()
            {
                return "...";
            }
        }
    }
}