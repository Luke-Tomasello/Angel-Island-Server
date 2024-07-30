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

/* Items/Misc/KeywordController.cs
 * CHANGELOG:
 *  4/24/2024, Adam (HasKeyword)
 *      Add KeyWordMatchMode to HasKeyword to allow for AnyOf and AllOf processing
 *  3/8/23, Yoar
 *      Rewrote FormatReply + cleanups
 * 2/11/22, Yoar
 *	    Added Range, ConditionFlags
 *	    Minor cleanups
 * 1/29/22, Yoar
 *	    Initial version.
 */

using Server.Gumps;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Items
{
    public class KeywordController : Item
    {
        [Flags]
        public enum ConditionFlag : byte
        {
            None = 0x0,
            Alive = 0x1,
            DeadOnly = 0x2,
            CheckLOS = 0x4,
        }

        public override string DefaultName { get { return "Keyword Controller"; } }

        private List<KeywordNode> m_Nodes;
        private int m_MessageHue;
        private int m_Range;
        private ConditionFlag m_Conditions;
        private TimeSpan m_ResetDelay;

        private DateTime m_LastSpeech;
        private List<int> m_Unlocked;

        public List<KeywordNode> Nodes
        {
            get { return m_Nodes; }
            set { m_Nodes = value; }
        }

        [CommandProperty(AccessLevel.GameMaster), Hue]
        public int MessageHue
        {
            get { return m_MessageHue; }
            set { m_MessageHue = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ConditionFlag Conditions
        {
            get { return m_Conditions; }
            set { m_Conditions = value; }
        }

        private bool GetConditionFlag(ConditionFlag flag)
        {
            return ((m_Conditions & flag) != 0);
        }

        private void SetConditionFlag(ConditionFlag flag, bool value)
        {
            if (value)
                m_Conditions |= flag;
            else
                m_Conditions &= ~flag;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AliveOnly
        {
            get { return GetConditionFlag(ConditionFlag.Alive); }
            set { SetConditionFlag(ConditionFlag.Alive, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DeadOnly
        {
            get { return GetConditionFlag(ConditionFlag.DeadOnly); }
            set { SetConditionFlag(ConditionFlag.DeadOnly, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CheckLOS
        {
            get { return GetConditionFlag(ConditionFlag.CheckLOS); }
            set { SetConditionFlag(ConditionFlag.CheckLOS, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan ResetDelay
        {
            get { return m_ResetDelay; }
            set { m_ResetDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastSpeech
        {
            get { return m_LastSpeech; }
            set { m_LastSpeech = value; }
        }

        public List<int> Unlocked
        {
            get { return m_Unlocked; }
            set { m_Unlocked = value; }
        }

        [Constructable]
        public KeywordController()
            : base(0x1ED0)
        {
            Light = LightType.Circle150;
            Movable = false;
            m_Nodes = new List<KeywordNode>();
            m_MessageHue = -1;
            m_Range = -1;
            m_ResetDelay = TimeSpan.FromMinutes(2.0);
            m_Unlocked = new List<int>();
        }
        public override bool HandlesOnSpeech { get { return (m_Nodes.Count != 0); } }

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (e.Type == MessageType.Emote || from.Hidden || !Validate(from))
                return;

            if (m_ResetDelay != TimeSpan.Zero && DateTime.UtcNow >= m_LastSpeech + m_ResetDelay)
                m_Unlocked.Clear();

            int index = -1;

            for (int i = 0; i < m_Nodes.Count && index == -1; i++)
            {
                if (IsUnlocked(m_Nodes[i].Requires))
                {
                    foreach (TextEntry te in m_Nodes[i].Keywords)
                    {
                        if (HasKeyword(e, te))
                        {
                            index = i;
                            break;
                        }
                    }
                }
            }

            if (index == -1)
                return;

            if (!m_Unlocked.Contains(index))
                m_Unlocked.Add(index);

            KeywordNode node = m_Nodes[index];

            if (node.Replies.Count != 0)
            {
                TextEntry reply = node.Replies[Utility.Random(node.Replies.Count)];

                TransmitMessage(new TextEntry(reply.Number, FormatReply(reply.String, e)));
            }

            m_LastSpeech = DateTime.UtcNow;
        }

        private bool Validate(Mobile m)
        {
            if (m_Range != -1 && !m.InRange(this.GetWorldLocation(), m_Range))
                return false;

            if (m_Conditions.HasFlag(ConditionFlag.Alive) && !m.Alive)
                return false;

            if (m_Conditions.HasFlag(ConditionFlag.DeadOnly) && m.Alive)
                return false;

            if (m_Conditions.HasFlag(ConditionFlag.CheckLOS) && (this.Map == null || !this.Map.LineOfSight(this, m)))
                return false;

            return true;
        }
        public enum KeyWordMatchMode
        {
            Standard,   // whole string must match, but with wild cards leading/trailing words may ignored.
            AnyOf,      // any of these words/phrases will trigger a match: dog;my cat; purple umbrella
            AllOf,      // all of these words are required, any order, other words are ignored
        }
        public static bool HasKeyword(SpeechEventArgs e, TextEntry te, KeyWordMatchMode mode = KeyWordMatchMode.Standard)
        {
            if (te.Number > 0 && e.HasKeyword(te.Number))
                return true;

            if (te.String != null)
            {
                if (mode == KeyWordMatchMode.Standard)
                {
                    string pattern = String.Concat("^", te.String.Replace("*", ".*?"), "$");

                    if (Regex.IsMatch(e.Speech, pattern, RegexOptions.IgnoreCase))
                        return true;
                }
                else if (mode == KeyWordMatchMode.AnyOf)
                {
                    string[] tokens = te.String.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    List<string> match_tokens = new(tokens);
                    return match_tokens.Any(w => e.Speech.Equals(w, StringComparison.OrdinalIgnoreCase));
                }
                else if (mode == KeyWordMatchMode.AllOf)
                {
                    string[] tokens = te.String.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    List<string> match_tokens = new(tokens);
                    return match_tokens.All(kw => e.Speech.Contains(kw, StringComparison.OrdinalIgnoreCase));
                }
            }

            return false;
        }

        private const char Token = '$';

        private string FormatReply(string str, SpeechEventArgs e)
        {
            StringBuilder sb = null;

            int cur = 0;
            int start = str.IndexOf(Token);

            while (start != -1 && start != str.Length - 1)
            {
                int end = str.IndexOf(Token, start + 1);

                if (end == -1)
                    break;

                if (sb == null)
                    sb = new StringBuilder();

                sb.Append(str.Substring(cur, start - cur));
                sb.Append(Substitute(str.Substring(start + 1, end - start - 1), e));

                cur = end + 1;
                start = str.IndexOf(Token, cur);
            }

            if (sb == null)
                return str;

            if (cur < str.Length)
                sb.Append(str.Substring(cur));

            return sb.ToString();
        }

        private string Substitute(string label, SpeechEventArgs e)
        {
            switch (label)
            {
                case "name":
                    {
                        if (e.Mobile.Name != null)
                            return e.Mobile.Name;
                        else
                            return String.Empty;
                    }
                case "timeofday":
                    {
                        int hours, minutes;

                        Clock.GetTime(this.Map, this.X, this.Y, out hours, out minutes);

                        if (hours >= 6 && hours < 12)
                            return "morning";
                        else if (hours >= 12 && hours < 18)
                            return "afternoon";
                        else if (hours >= 18 && hours < 24)
                            return "evening";
                        else
                            return "night";
                    }
            }

            return "ERROR";
        }

        private bool m_Recurse;

        private bool IsUnlocked(int index)
        {
            if (m_Recurse || index < 0 || index >= m_Nodes.Count)
                return true;

            if (!m_Unlocked.Contains(index))
                return false;

            m_Recurse = true;

            bool unlocked = IsUnlocked(m_Nodes[index].Requires);

            m_Recurse = false;

            return unlocked;
        }

        public void TransmitMessage(TextEntry te)
        {
            object root = this.RootParent;

            if (root is Mobile)
            {
                Mobile m = (Mobile)root;

                TextEntry.PublicOverheadMessage(m, MessageType.Regular, te, m_MessageHue == -1 ? m.SpeechHue : m_MessageHue);
            }
            else
            {
                Item item = root as Item;

                if (item == null)
                    item = this;

                TextEntry.PublicOverheadMessage(item, MessageType.Regular, te, m_MessageHue == -1 ? 0x3B2 : m_MessageHue);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump(this));
            }
        }

        private void BeginAdd(Mobile from)
        {
            from.SendMessage("Set the keyword");
            from.Prompt = new KeywordPrompt(this);
        }

        private class KeywordPrompt : Prompt
        {
            private KeywordController m_Controller;

            public KeywordPrompt(KeywordController controller)
            {
                m_Controller = controller;
            }

            public override void OnResponse(Mobile from, string text)
            {
                from.SendMessage("Set the reply");
                from.Prompt = new ReplyPrompt(m_Controller, text);
            }
        }

        private class ReplyPrompt : Prompt
        {
            private KeywordController m_Controller;
            private string m_Keyword;

            public ReplyPrompt(KeywordController controller, string keyword)
            {
                m_Controller = controller;
                m_Keyword = keyword;
            }

            public override void OnResponse(Mobile from, string text)
            {
                KeywordNode node = new KeywordNode(TEList.Parse(m_Keyword), TEList.Parse(text));

                from.SendMessage("Added: {0}", node.ToString());
                m_Controller.Nodes.Add(node);

                if (from.HasGump(typeof(InternalGump)))
                {
                    from.CloseGump(typeof(InternalGump));
                    from.SendGump(new InternalGump(m_Controller));
                }
            }
        }

        public KeywordController(Serial serial)
            : base(serial)
        {
            m_Unlocked = new List<int>();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_Range);
            writer.Write((byte)m_Conditions);

            writer.Write((int)m_Nodes.Count);

            for (int i = 0; i < m_Nodes.Count; i++)
                m_Nodes[i].Serialize(writer);

            writer.Write((int)m_MessageHue);
            writer.Write((TimeSpan)m_ResetDelay);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Range = reader.ReadInt();
                        m_Conditions = (ConditionFlag)reader.ReadByte();
                        goto case 0;
                    }
                case 0:
                    {
                        int count = reader.ReadInt();

                        m_Nodes = new List<KeywordNode>(count);

                        for (int i = 0; i < count; i++)
                            m_Nodes.Add(new KeywordNode(reader));

                        if (version < 1)
                            CheckLOS = reader.ReadBool();

                        m_MessageHue = reader.ReadInt();
                        m_ResetDelay = reader.ReadTimeSpan();

                        break;
                    }
            }

            if (version < 1)
                m_Range = -1;
        }

        [PropertyObject]
        public class KeywordNode
        {
            private TEList m_Keywords;
            private TEList m_Replies;
            private int m_Requires;

            public TEList Keywords
            {
                get { return m_Keywords; }
                set { m_Keywords = value; }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public string KeywordsStr
            {
                get { return m_Keywords.ToString(); }
                set { m_Keywords = TEList.Parse(value); }
            }

            public TEList Replies
            {
                get { return m_Replies; }
                set { m_Replies = value; }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public string RepliesStr
            {
                get { return m_Replies.ToString(); }
                set { m_Replies = TEList.Parse(value); }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public int Requires
            {
                get { return m_Requires; }
                set { m_Requires = value; }
            }

            public KeywordNode()
                : this(new TEList(), new TEList())
            {
            }

            public KeywordNode(TEList keywords, TEList replies)
            {
                m_Keywords = keywords;
                m_Replies = replies;
                m_Requires = -1;
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write((byte)1); // version

                m_Keywords.Serialize(writer);
                m_Replies.Serialize(writer);
                writer.Write((int)m_Requires);
            }

            public KeywordNode(GenericReader reader)
            {
                byte version = reader.ReadByte();

                switch (version)
                {
                    case 1:
                    case 0:
                        {
                            if (version >= 1)
                                m_Keywords = new TEList(reader);
                            else
                                m_Keywords = TEList.Parse(reader.ReadString());

                            if (version >= 1)
                                m_Replies = new TEList(reader);
                            else
                                m_Replies = TEList.Parse(reader.ReadString());

                            m_Requires = reader.ReadInt();

                            break;
                        }
                }
            }

            public override string ToString()
            {
                if (m_Requires != -1)
                    return String.Format("{0}->{1} (Req. {2})", m_Keywords.ToString(), m_Replies.ToString(), m_Requires);
                else
                    return String.Format("{0}->{1}", m_Keywords.ToString(), m_Replies.ToString());
            }
        }

        private class InternalGump : Gump
        {
            private const int LinesPerPage = 15;

            private KeywordController m_Controller;

            public InternalGump(KeywordController controller)
                : base(20, 30)
            {
                m_Controller = controller;

                AddPage(0);

                AddBackground(0, 0, 450, 400, 5054);
                AddBackground(10, 10, 430, 380, 3000);

                AddHtml(20, 10, 430, 20, "<CENTER>Keyword Controller</CENTER>", false, false);

                AddButton(20, 360, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(55, 360, 120, 20, 1079279, false, false); // Add

                AddButton(320, 360, 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(355, 360, 120, 20, 1011403, false, false); // Remove

                AddPage(1);

                for (int i = 0; i < m_Controller.Nodes.Count; i++)
                {
                    int line = i % LinesPerPage;

                    if (i != 0 && line == 0)
                    {
                        AddButton(320, 280, 4005, 4007, 0, GumpButtonType.Page, (i / LinesPerPage) + 1);
                        AddHtmlLocalized(355, 340, 300, 20, 1011066, false, false); // Next page

                        AddPage((i / LinesPerPage) + 1);

                        AddButton(20, 280, 4014, 4016, 0, GumpButtonType.Page, (i / LinesPerPage));
                        AddHtmlLocalized(55, 340, 300, 20, 1011067, false, false); // Previous page
                    }

                    int y = 30 + 20 * line;

                    AddCheck(20, y, 9026, 9027, false, i);
                    AddButton(45, y, 4005, 4007, i + 3, GumpButtonType.Reply, 0);
                    AddHtml(80, y, 350, 20, m_Controller.Nodes[i].ToString(), false, false);
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile from = sender.Mobile;

                if (from.AccessLevel < AccessLevel.GameMaster)
                    return;

                switch (info.ButtonID)
                {
                    case 0: break;
                    case 1: // Add
                        {
                            m_Controller.BeginAdd(from);
                            from.CloseGump(typeof(InternalGump));
                            from.SendGump(new InternalGump(m_Controller));
                            break;
                        }
                    case 2: // Remove
                        {
                            for (int i = m_Controller.Nodes.Count; i >= 0; i--)
                            {
                                if (info.IsSwitched(i))
                                    m_Controller.Nodes.RemoveAt(i);
                            }

                            from.CloseGump(typeof(InternalGump));
                            from.SendGump(new InternalGump(m_Controller));
                            break;
                        }
                    default:
                        {
                            int index = info.ButtonID - 3;

                            if (index >= 0 && index < m_Controller.Nodes.Count)
                            {
                                from.CloseGump(typeof(InternalGump));
                                from.SendGump(new InternalGump(m_Controller));
                                from.SendGump(new PropertiesGump(from, m_Controller.Nodes[index]));
                            }
                        }

                        break;
                }
            }
        }
    }
}