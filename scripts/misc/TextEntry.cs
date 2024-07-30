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

/* Misc/TextEntry.cs
 * CHANGELOG:
 *  9/4/22, Yoar
 *      Added AddHtmlText.
 *      Rewrote member-helper methods into static-helper methods,
 *      which is consistent with TextDefinition.
 *  2/11/22, Yoar
 *      Rewrote TEList from a struct to a List<TextEntry>
 *      Added TextEntry.HasKeyword, TEList.HasKeyword
 *  1/29/22, Yoar
 *      Added PublicOverheadMessage
 *      Added TEList
 *  11/29/21, Yoar
 *      Initial version
 */

using Server.Gumps;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Server
{
    [Parsable]
    public struct TextEntry : IEquatable<TextEntry>
    {
        public static readonly TextEntry Empty = new TextEntry();

        private int m_Number;
        private string m_String;

        public int Number { get { return m_Number; } }
        public string String { get { return m_String; } }

        public TextEntry(int number)
            : this(number, null)
        {
        }

        public TextEntry(string str)
            : this(0, str)
        {
        }

        public TextEntry(int number, string str)
        {
            m_Number = number;
            m_String = str;
        }

        public static implicit operator TextEntry(int number)
        {
            return new TextEntry(number, null);
        }

        public static implicit operator TextEntry(string str)
        {
            return new TextEntry(0, str);
        }

        public static implicit operator int(TextEntry te)
        {
            return te.m_Number;
        }

        public static implicit operator string(TextEntry te)
        {
            return te.m_String;
        }

        public static bool operator ==(TextEntry a, TextEntry b)
        {
            return (a.m_Number == b.m_Number && a.m_String == b.m_String);
        }

        public static bool operator !=(TextEntry a, TextEntry b)
        {
            return (a.m_Number != b.m_Number || a.m_String != b.m_String);
        }

        public static void GetProperties(ObjectPropertyList list, TextEntry te)
        {
            if (te.Number > 0)
            {
                if (te.String != null)
                    list.Add(te.Number, te.String);
                else
                    list.Add(te.Number);
            }
            else if (te.String != null)
                list.Add(te.String);
        }

        public static void LabelTo(Item item, Mobile from, TextEntry te)
        {
            if (te.Number > 0)
            {
                if (te.String != null)
                    item.LabelTo(from, te.Number, te.String);
                else
                    item.LabelTo(from, te.Number);
            }
            else if (te.String != null)
                item.LabelTo(from, te.String);
        }

        public static void SendMessageTo(Mobile from, TextEntry te)
        {
            if (te.Number > 0)
            {
                if (te.String != null)
                    from.SendLocalizedMessage(te.Number, te.String);
                else
                    from.SendLocalizedMessage(te.Number);
            }
            else if (te.String != null)
                from.SendMessage(te.String);
        }

        public static void PublicOverheadMessage(Mobile m, MessageType messageType, TextEntry te, int hue = 0)
        {
            if (te.Number > 0)
            {
                if (te.String != null)
                    m.PublicOverheadMessage(messageType, hue, te.Number, te.String);
                else
                    m.PublicOverheadMessage(messageType, hue, te.Number);
            }
            else if (te.String != null)
                m.PublicOverheadMessage(messageType, hue, false, te.String);
        }

        public static void PublicOverheadMessage(Item item, MessageType messageType, TextEntry te, int hue = 0)
        {
            if (te.Number > 0)
            {
                if (te.String != null)
                    item.PublicOverheadMessage(messageType, hue, te.Number, te.String);
                else
                    item.PublicOverheadMessage(messageType, hue, te.Number);
            }
            else if (te.String != null)
                item.PublicOverheadMessage(messageType, hue, false, te.String);
        }

        public static bool HasKeyword(SpeechEventArgs e, TextEntry te)
        {
            if (te.Number > 0 && e.HasKeyword(te.Number))
                return true;

            if (te.String != null)
            {
                string pattern = String.Concat("^", te.String.Replace("*", ".*?"), "$");

                if (Regex.IsMatch(e.Speech, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        public static void AddHtmlText(Gump g, int x, int y, int width, int height, TextEntry te, bool background, bool scrollbar, int numberColor = -1, int stringColor = -1)
        {
            if (te.Number > 0)
            {
                if (numberColor >= 0)
                    g.AddHtmlLocalized(x, y, width, height, te.Number, numberColor, background, scrollbar);
                else
                    g.AddHtmlLocalized(x, y, width, height, te.Number, background, scrollbar);
            }
            else if (te.String != null)
            {
                if (stringColor >= 0)
                    g.AddHtml(x, y, width, height, String.Format("<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", stringColor, te.String), background, scrollbar);
                else
                    g.AddHtml(x, y, width, height, te.String, background, scrollbar);
            }
        }

        public override bool Equals(object obj)
        {
            return (obj is TextEntry && Equals((TextEntry)obj));
        }

        public bool Equals(TextEntry other)
        {
            return (m_Number == other.m_Number && m_String == other.m_String);
        }

        public override int GetHashCode()
        {
            if (m_Number > 0)
            {
                if (m_String != null)
                    return (0x3.GetHashCode() ^ m_Number.GetHashCode() ^ m_String.GetHashCode());
                else
                    return (0x1.GetHashCode() ^ m_Number.GetHashCode());
            }
            else if (m_String != null)
                return (0x2.GetHashCode() ^ m_String.GetHashCode());

            return 0x0.GetHashCode();
        }

        public override string ToString()
        {
            if (m_Number > 0)
            {
                if (m_String != null)
                    return String.Format("#{0}|{1}", m_Number, m_String);
                else
                    return String.Format("#{0}", m_Number);
            }
            else if (m_String != null)
                return m_String;

            return String.Empty;
        }

        public static TextEntry Parse(string value)
        {
            if (String.IsNullOrEmpty(value))
                return TextEntry.Empty;

            if (value[0] != '#' || value.Length <= 1)
                return new TextEntry(0, value);

            int index = value.IndexOf('|');

            if (index == -1)
                return new TextEntry(Utility.ToInt32(value.Substring(1)), null);
            else if (value.Length > index + 1)
                return new TextEntry(Utility.ToInt32(value.Substring(1, index - 1)), value.Substring(index + 1));
            else
                return new TextEntry(Utility.ToInt32(value.Substring(1, index - 1)), null);
        }

        [Flags]
        public enum SaveFlag
        {
            None = 0x0,

            Number = 0x1,
            String = 0x2,
        }

        public void Serialize(GenericWriter writer)
        {
            SaveFlag flags = SaveFlag.None;

            if (m_Number > 0)
                flags |= SaveFlag.Number;

            if (m_String != null)
                flags |= SaveFlag.String;

            writer.Write((byte)flags);

            if (flags.HasFlag(SaveFlag.Number))
                writer.Write((int)m_Number);

            if (flags.HasFlag(SaveFlag.String))
                writer.Write((string)m_String);
        }

        public TextEntry(GenericReader reader)
        {
            SaveFlag flags = (SaveFlag)reader.ReadByte();

            if (flags.HasFlag(SaveFlag.Number))
                m_Number = reader.ReadInt();
            else
                m_Number = 0;

            if (flags.HasFlag(SaveFlag.String))
                m_String = reader.ReadString();
            else
                m_String = null;
        }
    }

    [Parsable]
    public class TEList : List<TextEntry>
    {
        public TEList()
            : base()
        {
        }

        public TEList(params TextEntry[] array)
            : base()
        {
            this.AddRange(array);
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)this.Count);

            for (int i = 0; i < this.Count; i++)
                this[i].Serialize(writer);
        }

        public TEList(GenericReader reader)
            : base()
        {
            int count = reader.ReadInt();

            for (int i = 0; i < count; i++)
                this.Add(new TextEntry(reader));
        }

        public static TEList Parse(string value)
        {
            TEList list = new TEList();

            if (!String.IsNullOrEmpty(value))
            {
                string[] split = value.Replace("(", "").Replace(")", "").Split('|');

                for (int i = 0; i < split.Length; i++)
                    list.Add(TextEntry.Parse(split[i]));
            }

            return list;
        }

        public override string ToString()
        {
            if (this.Count == 0)
                return String.Empty;
            else if (this.Count == 1)
                return this[0].ToString();
            else
                return String.Format("({0})", String.Join("|", this));
        }
    }
}