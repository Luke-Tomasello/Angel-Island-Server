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

/* Items/Special/Holiday/CollectionBox.cs
 * ChangeLog:
 * 5/29/23, Yoar
 *      Added Link, PointScalars
 * 2/10/22, Adam
 *      Remove CollectionNPC and move him from Items namespace to Mobiles namespace.
 * 1/29/22, Yoar
 *      Added ShoutMessage.
 * 12/16/21, Yoar
 *      Added CollectionNPC.
 * 12/12/21, Yoar
 *      Initial version.
 */

using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items.Triggers;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Items
{
    public enum CollectionMode : byte
    {
        Total,
        Max,
    }

    public class CollectionBox : Item
    {
        private string m_ConditionStr;
        private ObjectConditional m_ConditionImpl;
        private string m_AcceptMessage;
        private string m_DenyMessage;
        private bool m_PublicScores;
        private Dictionary<Mobile, double> m_Table;
        private int m_LotteryDraws;
        private List<Mobile> m_Winners;
        private byte m_Decimals;
        private CollectionMode m_CollectionMode;
        private string m_ItemPropertyStr;
        private PropertyInfo[] m_ItemPropertyImpl;
        private string m_ShoutMessage;
        private int m_ShoutRange;
        private TimeSpan m_ShoutDelay;
        private Item m_Link;
        private PointScalar[] m_PointScalars = new PointScalar[6];

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public string Condition
        {
            get { return m_ConditionStr; }
            set
            {
                if (value != null)
                {
                    try
                    {
                        string[] args = CommandSystem.Split(value);
                        m_ConditionImpl = ObjectConditional.Parse(null, ref args);
                        m_ConditionStr = value;
                    }
                    catch
                    {
                        m_ConditionImpl = null;
                        m_ConditionStr = null;
                    }
                }
                else
                {
                    m_ConditionImpl = null;
                    m_ConditionStr = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string AcceptMessage
        {
            get { return m_AcceptMessage; }
            set { m_AcceptMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DenyMessage
        {
            get { return m_DenyMessage; }
            set { m_DenyMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PublicScores
        {
            get { return m_PublicScores; }
            set { m_PublicScores = value; }
        }

        private double this[Mobile m]
        {
            get
            {
                double score;

                if (m_Table.TryGetValue(m, out score))
                    return score;
                else
                    return 0.0;
            }
            set
            {
                if (value == 0.0)
                    m_Table.Remove(m);
                else
                    m_Table[m] = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LotteryDraws
        {
            get { return m_LotteryDraws; }
            set { m_LotteryDraws = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public byte Decimals
        {
            get { return m_Decimals; }
            set { m_Decimals = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CollectionMode CollectionMode
        {
            get { return m_CollectionMode; }
            set { m_CollectionMode = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ItemProperty
        {
            get { return m_ItemPropertyStr; }
            set
            {
                bool okay = false;

                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        string failReason = null;
                        PropertyInfo[] chain = Properties.GetPropertyInfoChain(null, m_ConditionImpl == null ? typeof(Item) : m_ConditionImpl.Type, value, false, ref failReason);

                        if (chain != null && chain.Length != 0)
                        {
                            m_ItemPropertyImpl = chain;
                            m_ItemPropertyStr = value;
                            okay = true;
                        }
                    }
                    catch (Exception e)
                    {
                        EventSink.InvokeLogException(new LogExceptionEventArgs(e));
                    }
                }

                if (!okay)
                {
                    m_ItemPropertyImpl = null;
                    m_ItemPropertyStr = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ShoutMessage
        {
            get { return m_ShoutMessage; }
            set { m_ShoutMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ShoutRange
        {
            get { return m_ShoutRange; }
            set { m_ShoutRange = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan ShoutDelay
        {
            get { return m_ShoutDelay; }
            set { m_ShoutDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        private PointScalar GetPointScalar(int index)
        {
            if (index < 0 || index >= m_PointScalars.Length)
                return null;

            PointScalar ps = m_PointScalars[index];

            if (ps == null)
                m_PointScalars[index] = ps = new PointScalar();

            return ps;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PointScalar PointScalar1 { get { return GetPointScalar(0); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public PointScalar PointScalar2 { get { return GetPointScalar(1); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public PointScalar PointScalar3 { get { return GetPointScalar(2); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public PointScalar PointScalar4 { get { return GetPointScalar(3); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public PointScalar PointScalar5 { get { return GetPointScalar(4); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public PointScalar PointScalar6 { get { return GetPointScalar(5); } set { } }

        [Constructable]
        public CollectionBox()
            : base(0x09A8)
        {
            Movable = false;
            Name = "Collection Box";
            m_AcceptMessage = "Thank you for the donation!";
            m_DenyMessage = "That is not a collection item!";
            m_Table = new Dictionary<Mobile, double>();
            m_LotteryDraws = 3;
            m_Winners = new List<Mobile>();
            m_ShoutRange = 8;
            m_ShoutDelay = TimeSpan.FromSeconds(15.0);

#if DEBUG
            GenerateDummyData();
#endif
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            return HandleDragDrop(this, from, dropped);
        }

        public bool HandleDragDrop(IEntity ent, Mobile from, Item dropped)
        {
            if (IsValidItem(dropped))
            {
                double award = 1.0;

                if (dropped.Stackable)
                    award *= dropped.Amount;

                if (m_ItemPropertyImpl != null)
                {
                    double factor;

                    if (GetDynamicValue(dropped, out factor))
                        award *= factor;
                }

                for (int i = 0; i < m_PointScalars.Length; i++)
                {
                    int scalar = GetPointScalar(i).GetScalar(dropped);

                    if (scalar != 100)
                        award *= (scalar / 100.0);
                }

                double score = this[from];

                switch (m_CollectionMode)
                {
                    case CollectionMode.Total: score += award; break;
                    case CollectionMode.Max: score = Math.Max(score, award); break;
                }

                this[from] = score;

                if (m_AcceptMessage != null)
                    SayTo(ent, from, m_AcceptMessage);

                if (m_Link != null)
                    TriggerSystem.CheckTrigger(from, m_Link);

                ContainerData data = ContainerData.GetData(this.ItemID);

                if (data != null)
                    from.Send(new PlaySound(data.DropSound, from.Location));

                DisplayScore(ent, from);

                UpdateGump(from);

                dropped.Delete();
                return true;
            }
            else
            {
                if (m_DenyMessage != null)
                    SayTo(ent, from, m_DenyMessage);

                return false;
            }
        }

        private bool IsValidItem(Item item)
        {
            if (m_ConditionImpl == null)
                return false;

            bool okay;

            try
            {
                okay = m_ConditionImpl.CheckCondition(item);
            }
            catch
            {
                okay = false;
            }

            return okay;
        }

        private bool GetDynamicValue(object obj, out double value)
        {
            value = 0.0;

            try
            {
                string failReason = null;
                PropertyInfo p = Properties.GetPropertyInfo(ref obj, m_ItemPropertyImpl, ref failReason);

                value = (double)p.GetValue(obj);
                return true;
            }
            catch (Exception e)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(e));
                return false;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this.GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            HandleDoubleClick(this, from);
        }

        public void HandleDoubleClick(IEntity ent, Mobile from)
        {
            DisplayScore(ent, from);

            if (m_PublicScores || from.AccessLevel >= AccessLevel.Counselor)
            {
                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump(from, this));
            }
        }

        public override bool HandlesOnMovement { get { return !string.IsNullOrEmpty(m_ShoutMessage); } }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            HandleMovement(this, m, oldLocation);
        }

        public void HandleMovement(IEntity ent, Mobile m, Point3D oldLocation)
        {
            CheckShout(ent, m, oldLocation);
        }

        private Memory m_ShoutMemory = new Memory();

        private void CheckShout(IEntity ent, Mobile m, Point3D oldLocation)
        {
            if (string.IsNullOrEmpty(m_ShoutMessage) || m.Hidden || !m.Alive)
                return;

            Point3D loc = ((ent is Item) ? ((Item)ent).GetWorldLocation() : ent.Location);

            if (!Utility.InRange(loc, m.Location, m_ShoutRange))
                return;

            if (!m_ShoutMemory.Recall(m))
            {
                SayTo(ent, m, m_ShoutMessage);

                m_ShoutMemory.Remember(m, m_ShoutDelay.TotalSeconds);
            }
        }

        private void DisplayScore(IEntity ent, Mobile from)
        {
            SayTo(ent, from, string.Format("Your score: {0}", FormatValue(this[from])));
        }

        public string FormatValue(double value)
        {
            if (m_Decimals == 0)
                return value.ToString("N0");

            string format = string.Concat('F', m_Decimals.ToString());

            return value.ToString(format);
        }

        private static void SayTo(IEntity ent, Mobile to, string message)
        {
            if (ent is Item)
            {
                ((Item)ent).SendMessageTo(to, false, message);
            }
            else if (ent is Mobile)
            {
                Mobile m = (Mobile)ent;

                m.PrivateOverheadMessage(MessageType.Regular, m.SpeechHue, false, message, to.NetState);
                m.Direction = m.GetDirectionTo(to);

                // stop walking whilst talking to the player?
                /*if (m is BaseCreature)
                {
                    BaseCreature bc = (BaseCreature)m;

                    if (bc.AIObject != null)
                        bc.AIObject.NextMove = DateTime.UtcNow + TimeSpan.FromSeconds(3.0);
                }*/
            }
        }

        // based on LeaderboardGump
        private class InternalGump : Gump
        {
            private const int m_LinesPerPage = 10;

            private CollectionBox m_Box;
            private KeyValuePair<Mobile, double>[] m_Results;
            private int m_Page;

            public CollectionBox Box { get { return m_Box; } }

            public InternalGump(Mobile from, CollectionBox box)
                : this(from, box, box.GetResults(), 0)
            {
            }

            private InternalGump(Mobile from, CollectionBox box, KeyValuePair<Mobile, double>[] results, int page)
                : base(120, 50)
            {
                m_Box = box;
                m_Results = results;
                m_Page = page;

                int pages = (m_Results.Length + m_LinesPerPage - 1) / m_LinesPerPage;

                AddBackground(0, 0, 416, 310, 5170);

                AddHtml(0, 3, 416, 30, string.Format("<center>{0}</center>", m_Box.Name), false, false);

                if (from.AccessLevel >= AccessLevel.GameMaster)
                {
                    AddButton(30, 30, 4005, 4006, 3, GumpButtonType.Reply, 0);
                    AddHtml(65, 32, 90, 20, "Clear", false, false);

                    AddButton(130, 30, 4005, 4006, 4, GumpButtonType.Reply, 0);
                    AddHtml(165, 32, 90, 20, "Lottery", false, false);
                }

                if (m_Results.Length == 0)
                {
                    AddHtml(0, 75, 416, 20, "<center>Nothing to display</center>", false, false);
                    return;
                }

                AddHtml(30, 55, 240, 20, "Rank", false, false);
                AddHtml(70, 55, 240, 20, "Name", false, false);
                AddHtml(270, 55, 120, 20, "Score", false, false);

                int start = Math.Max(0, m_Page * m_LinesPerPage);

                for (int i = 0; i < m_LinesPerPage && start + i < m_Results.Length; i++)
                {
                    int index = start + i;

                    KeyValuePair<Mobile, double> kvp = m_Results[index];

                    if (from.AccessLevel >= AccessLevel.Counselor)
                        AddButton(18, 78 + i * 18, 2103, 2104, 10 + index, GumpButtonType.Reply, 0);

                    AddHtml(30, 75 + i * 18, 40, 20, (index + 1).ToString(), false, false);
                    AddHtml(70, 75 + i * 18, 200, 20, FormatMobile(kvp.Key), false, false);
                    AddLabel(270, 75 + i * 18, 0, m_Box.FormatValue(kvp.Value));

                    if (m_Box.IsWinner(kvp.Key))
                        AddLabel(370, 75 + i * 18, 0x26, "*");
                }

                if (pages > 1)
                {
                    if (m_Page > 0)
                        AddButton(370, 5, 2435, 2436, 1, GumpButtonType.Reply, 0); // Up

                    if (m_Page < pages - 1)
                        AddButton(370, 286, 2437, 2437, 2, GumpButtonType.Reply, 0); // Down

                    AddHtml(0, 286, 416, 30, string.Format("<center>Page {0} / {1}<center>", m_Page + 1, pages), false, false);
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile from = sender.Mobile;

                if (!m_Box.PublicScores && from.AccessLevel < AccessLevel.Counselor)
                    return;

                switch (info.ButtonID)
                {
                    case 0: return;
                    case 1: // scroll up
                        {
                            from.CloseGump(typeof(InternalGump));
                            from.SendGump(new InternalGump(from, m_Box, m_Results, Math.Max(0, m_Page - 1)));
                            break;
                        }
                    case 2: // scroll down
                        {
                            from.CloseGump(typeof(InternalGump));
                            from.SendGump(new InternalGump(from, m_Box, m_Results, Math.Min((m_Results.Length + m_LinesPerPage - 1) / m_LinesPerPage - 1, m_Page + 1)));
                            break;
                        }
                    case 3: // clear
                        {
                            if (from.AccessLevel >= AccessLevel.GameMaster)
                            {
                                from.CloseGump(typeof(InternalGump));
                                from.SendGump(new InternalGump(from, m_Box, m_Results, m_Page));
                                from.CloseGump(typeof(ConfirmClearGump));
                                from.SendGump(new ConfirmClearGump(m_Box));
                            }

                            break;
                        }
                    case 4: // lottery
                        {
                            if (from.AccessLevel >= AccessLevel.GameMaster)
                            {
                                from.CloseGump(typeof(InternalGump));
                                from.SendGump(new InternalGump(from, m_Box, m_Results, m_Page));
                                from.CloseGump(typeof(ConfirmLotteryGump));
                                from.SendGump(new ConfirmLotteryGump(m_Box));
                            }

                            break;
                        }
                    default:
                        {
                            if (info.ButtonID >= 10 && from.AccessLevel >= AccessLevel.Counselor)
                            {
                                int index = info.ButtonID - 10;

                                if (index < m_Results.Length)
                                {
                                    from.CloseGump(typeof(InternalGump));
                                    from.SendGump(new InternalGump(from, m_Box, m_Results, m_Page));
                                    from.SendGump(new PropertiesGump(from, new CBHandle(m_Box, m_Results[index].Key)));
                                }
                            }

                            break;
                        }
                }
            }

            private static string FormatMobile(Mobile m)
            {
                string name = Server.Misc.Titles.FormatShort(m);

                if (m.Guild != null)
                    return string.Format("{0} [{1}]", name, m.Guild.Abbreviation);
                else
                    return name;
            }
        }

        private KeyValuePair<Mobile, double>[] GetResults()
        {
            KeyValuePair<Mobile, double>[] result = new KeyValuePair<Mobile, double>[m_Table.Count];

            int index = 0;

            foreach (KeyValuePair<Mobile, double> kvp in m_Table)
                result[index++] = kvp;

            Array.Sort(result, InternalComparer.Instance);

            return result;
        }

        private class InternalComparer : IComparer<KeyValuePair<Mobile, double>>
        {
            public static readonly InternalComparer Instance = new InternalComparer();

            private InternalComparer()
            {
            }

            public int Compare(KeyValuePair<Mobile, double> a, KeyValuePair<Mobile, double> b)
            {
                return b.Value.CompareTo(a.Value); // sort in descending order
            }
        }

        private class ConfirmClearGump : Gump
        {
            private CollectionBox m_Box;

            public ConfirmClearGump(CollectionBox box)
                : base(50, 50)
            {
                m_Box = box;

                AddPage(0);

                AddBackground(10, 10, 190, 140, 0x242C);

                AddHtml(25, 30, 160, 70, "<center>Are you sure you wish to clear the collection box tally?</center>", false, false);

                AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile from = sender.Mobile;

                if (info.ButtonID == 1)
                    m_Box.Clear(from);
            }
        }

        private void Clear(Mobile from)
        {
            from.SendMessage("You clear the collection box tally.");

            m_Table.Clear();

            UpdateGump(from);
        }

        private class ConfirmLotteryGump : Gump
        {
            private CollectionBox m_Box;

            public ConfirmLotteryGump(CollectionBox box)
                : base(50, 50)
            {
                m_Box = box;

                AddPage(0);

                AddBackground(10, 10, 190, 140, 0x242C);

                AddHtml(25, 30, 160, 70, string.Format("<center>Are you sure you wish to (re)run the lottery? (to draw: {0})</center>", m_Box.LotteryDraws), false, false);

                AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile from = sender.Mobile;

                if (info.ButtonID == 1)
                    m_Box.Lottery(from);
            }
        }

        private void Lottery(Mobile from)
        {
            if (m_Table.Count == 0)
            {
                from.SendMessage("There is no one to draw from.");
                return;
            }
            else if (m_LotteryDraws <= 0)
            {
                from.SendMessage("There must be at least one lottery draw.");
                return;
            }

            from.SendMessage("You draw {0} winners...", m_LotteryDraws);

            m_Winners.Clear();

            double total = 0.0;

            foreach (KeyValuePair<Mobile, double> kvp in m_Table)
                total += kvp.Value;

            for (int i = 0; i < m_LotteryDraws && total > 0.0; i++)
            {
                double rnd = total * Utility.RandomDouble();

                foreach (KeyValuePair<Mobile, double> kvp in m_Table)
                {
                    if (m_Winners.Contains(kvp.Key))
                        continue;

                    if (rnd < kvp.Value)
                    {
                        m_Winners.Add(kvp.Key);
                        total -= kvp.Value;
                        break;
                    }

                    rnd -= kvp.Value;
                }
            }

            string[] names = new string[m_Winners.Count];

            for (int i = 0; i < m_Winners.Count; i++)
                names[i] = m_Winners[i].Name;

            from.SendMessage("And the winners are: {0}!", string.Join(", ", names));

            UpdateGump(from);
        }

        private bool IsWinner(Mobile m)
        {
            return m_Winners.Contains(m);
        }

        private void UpdateGump(Mobile from)
        {
            InternalGump g = from.FindGump(typeof(InternalGump)) as InternalGump;

            if (g != null && g.Box == this)
            {
                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump(from, this));
            }
        }

        [NoSort]
        [PropertyObject]
        private class CBHandle
        {
            private CollectionBox m_Box;
            private Mobile m_Mobile;

            [CommandProperty(AccessLevel.Counselor)]
            public Mobile Mobile
            {
                get { return m_Mobile; }
                set { }
            }

            [CommandProperty(AccessLevel.Counselor)]
            public string Name
            {
                get { return m_Mobile.Name; }
                set { }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
            public IAccount Account
            {
                get { return m_Mobile.Account; }
                set { }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
            public double Score
            {
                get { return m_Box[m_Mobile]; }
                set { m_Box[m_Mobile] = value; }
            }

            public CBHandle(CollectionBox box, Mobile m)
            {
                m_Box = box;
                m_Mobile = m;
            }
        }

        [PropertyObject]
        public class PointScalar
        {
            private string m_ConditionStr;
            private ObjectConditional m_ConditionImpl;
            private int m_Scalar;

            [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
            public string Condition
            {
                get { return m_ConditionStr; }
                set
                {
                    if (value != null)
                    {
                        try
                        {
                            string[] args = CommandSystem.Split(value);
                            m_ConditionImpl = ObjectConditional.Parse(null, ref args);
                            m_ConditionStr = value;
                        }
                        catch
                        {
                            m_ConditionImpl = null;
                            m_ConditionStr = null;
                        }
                    }
                    else
                    {
                        m_ConditionImpl = null;
                        m_ConditionStr = null;
                    }
                }
            }

            [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
            public int Scalar
            {
                get { return m_Scalar; }
                set { m_Scalar = value; }
            }

            public PointScalar()
            {
                m_Scalar = 100;
            }

            public int GetScalar(Item item)
            {
                if (m_ConditionImpl == null)
                    return 100;

                bool okay;

                try
                {
                    okay = m_ConditionImpl.CheckCondition(item);
                }
                catch
                {
                    okay = false;
                }

                if (okay)
                    return m_Scalar;

                return 100;
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write((string)m_ConditionStr);
                writer.Write((int)m_Scalar);
            }

            public PointScalar(GenericReader reader)
            {
                Condition = reader.ReadString();
                m_Scalar = reader.ReadInt();
            }

            public override string ToString()
            {
                return string.Format("{0} x{1}%", m_ConditionStr, m_Scalar);
            }
        }

        public CollectionBox(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)4); // version

            writer.Write((Item)m_Link);

            writer.Write((int)m_PointScalars.Length);

            for (int i = 0; i < m_PointScalars.Length; i++)
                GetPointScalar(i).Serialize(writer);

            writer.Write((string)m_ShoutMessage);
            writer.Write((int)m_ShoutRange);
            writer.Write((TimeSpan)m_ShoutDelay);

            writer.Write((byte)m_Decimals);
            writer.Write((byte)m_CollectionMode);
            writer.Write((string)m_ItemPropertyStr);

            writer.Write((int)m_LotteryDraws);
            writer.WriteMobileList(m_Winners);

            writer.Write((string)m_ConditionStr);
            writer.Write((string)m_AcceptMessage);
            writer.Write((string)m_DenyMessage);
            writer.Write((bool)m_PublicScores);

            writer.Write((int)m_Table.Count);

            foreach (KeyValuePair<Mobile, double> kvp in m_Table)
            {
                writer.Write((Mobile)kvp.Key);
                writer.Write((double)kvp.Value);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        m_Link = reader.ReadItem();

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            PointScalar ps = new PointScalar(reader);

                            if (i < m_PointScalars.Length)
                                m_PointScalars[i] = ps;
                        }

                        goto case 3;
                    }
                case 3:
                    {
                        m_ShoutMessage = reader.ReadString();
                        m_ShoutRange = reader.ReadInt();
                        m_ShoutDelay = reader.ReadTimeSpan();
                        goto case 2;
                    }
                case 2:
                    {
                        m_Decimals = reader.ReadByte();
                        m_CollectionMode = (CollectionMode)reader.ReadByte();
                        ItemProperty = reader.ReadString();
                        goto case 1;
                    }
                case 1:
                    {
                        m_LotteryDraws = reader.ReadInt();
                        m_Winners = reader.ReadStrongMobileList();
                        goto case 0;
                    }
                case 0:
                    {
                        Condition = reader.ReadString();
                        m_AcceptMessage = reader.ReadString();
                        m_DenyMessage = reader.ReadString();
                        m_PublicScores = reader.ReadBool();

                        int count = reader.ReadInt();

                        m_Table = new Dictionary<Mobile, double>();

                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();

                            double score;

                            if (version < 2)
                                score = (double)reader.ReadInt();
                            else
                                score = reader.ReadDouble();

                            if (m != null)
                                m_Table[m] = score;
                        }

                        break;
                    }
            }

            if (version < 1)
            {
                m_LotteryDraws = 3;
                m_Winners = new List<Mobile>();
            }

            if (version < 3)
            {
                m_ShoutRange = 8;
                m_ShoutDelay = TimeSpan.FromSeconds(15.0);
            }

#if DEBUG
            GenerateDummyData();
#endif
        }

#if DEBUG
        private void GenerateDummyData()
        {
            for (int i = 0; i < 50; i++)
            {
                Mobile dummy = new Server.Mobiles.Brigand();

                this[dummy] = Utility.RandomMinMax(0, 1000);

                dummy.Delete();
            }
        }
#endif
    }
}