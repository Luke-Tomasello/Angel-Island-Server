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

/* Server/Gumps/Gump.cs
 * CHANGELOG
 *  11/25/2023, Adam
 *      Move the Center() and Color() functions which were replicated in like 30 different gumps to here.
 *  9/11/2023, Yoar
 *      Merge Mobile.CloseAllGumps with RunUO. We no longer send gump response 0. Instead, we cann Gump.OnServerClose.
 * 12/30/2022, Yoar (GetGumpID)
 * 		Added virtual GetGumpID method
 * 		Allows us to (for example) use the same gump ID over multiple gumps
 * 		The standard UO client caches gump (x,y) per gump ID
 * 12/2/2021, Adam: (GetTypeID)
 *      blows up under 64bit build
 *      swapping in the RunUO code
 *      // old call
 *      return type.GetHashCode() ^ type.FullName.GetHashCode() ^ type.TypeHandle.Value.ToInt32();
 *      // new call
 *      return type.FullName.GetHashCode();
 *  11/17/21, Yoar
 *      Added support for localized html text with string arguments.
 */

using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Server.Gumps
{
    public class Gump
    {
        private ArrayList m_Entries;
        private ArrayList m_Strings;

        internal int m_TextEntries, m_Switches;

        private static int m_NextSerial = 1;

        private int m_Serial;
        private int m_TypeID;
        private int m_X, m_Y;

        private bool m_Dragable = true;
        private bool m_Closable = true;
        private bool m_Resizable = true;
        private bool m_Disposable = true;

        public virtual int GetGumpID()
        {
            return GetTypeID(this.GetType());
        }

        public static int GetTypeID(Type type)
        {
            // 12/2/2021, Adam: blows up under 64bit build
            // swapping in the RunUO code
            //return type.GetHashCode() ^ type.FullName.GetHashCode() ^ type.TypeHandle.Value.ToInt32();
            return type.FullName.GetHashCode();
        }

        public Gump(int x, int y)
        {
            do
            {
                m_Serial = m_NextSerial++;
            } while (m_Serial == 0); // standard client apparently doesn't send a gump response packet if serial == 0

            m_X = x;
            m_Y = y;

            m_TypeID = GetGumpID();

            m_Entries = new ArrayList();
            m_Strings = new ArrayList();
        }

        public void Invalidate()
        {
            if (m_Packet != null)
            {
                m_Packet = null;

                if (m_Strings.Count > 0)
                    m_Strings.Clear();
            }
        }

        public int TypeID
        {
            get
            {
                return m_TypeID;
            }
        }

        public ArrayList Entries
        {
            get { return m_Entries; }
        }

        public int Serial
        {
            get
            {
                return m_Serial;
            }
            set
            {
                if (m_Serial != value)
                {
                    m_Serial = value;
                    Invalidate();
                }
            }
        }

        public int X
        {
            get
            {
                return m_X;
            }
            set
            {
                if (m_X != value)
                {
                    m_X = value;
                    Invalidate();
                }
            }
        }

        public int Y
        {
            get
            {
                return m_Y;
            }
            set
            {
                if (m_Y != value)
                {
                    m_Y = value;
                    Invalidate();
                }
            }
        }

        public bool Disposable
        {
            get
            {
                return m_Disposable;
            }
            set
            {
                if (m_Disposable != value)
                {
                    m_Disposable = value;
                    Invalidate();
                }
            }
        }

        public bool Resizable
        {
            get
            {
                return m_Resizable;
            }
            set
            {
                if (m_Resizable != value)
                {
                    m_Resizable = value;
                    Invalidate();
                }
            }
        }

        public bool Dragable
        {
            get
            {
                return m_Dragable;
            }
            set
            {
                if (m_Dragable != value)
                {
                    m_Dragable = value;
                    Invalidate();
                }
            }
        }

        public bool Closable
        {
            get
            {
                return m_Closable;
            }
            set
            {
                if (m_Closable != value)
                {
                    m_Closable = value;
                    Invalidate();
                }
            }
        }

        public void AddPage(int page)
        {
            Add(new GumpPage(page));
        }

        public void AddAlphaRegion(int x, int y, int width, int height)
        {
            Add(new GumpAlphaRegion(x, y, width, height));
        }

        public void AddBackground(int x, int y, int width, int height, int gumpID)
        {
            Add(new GumpBackground(x, y, width, height, gumpID));
        }

        public void AddButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param)
        {
            Add(new GumpButton(x, y, normalID, pressedID, buttonID, type, param));
        }

        public void AddCheck(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
        {
            Add(new GumpCheck(x, y, inactiveID, activeID, initialState, switchID));
        }

        public void AddGroup(int group)
        {
            Add(new GumpGroup(group));
        }

        public void AddHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar)
        {
            Add(new GumpHtml(x, y, width, height, text, background, scrollbar));
        }

        public void AddHtmlLocalized(int x, int y, int width, int height, int number, bool background, bool scrollbar)
        {
            Add(new GumpHtmlLocalized(x, y, width, height, number, background, scrollbar));
        }

        public void AddHtmlLocalized(int x, int y, int width, int height, int number, int color, bool background, bool scrollbar)
        {
            Add(new GumpHtmlLocalized(x, y, width, height, number, color, background, scrollbar));
        }

        public void AddHtmlLocalized(int x, int y, int width, int height, int number, string args, int color, bool background, bool scrollbar)
        {
            Add(new GumpHtmlLocalized(x, y, width, height, number, args, color, background, scrollbar));
        }

        public void AddImage(int x, int y, int gumpID)
        {
            Add(new GumpImage(x, y, gumpID));
        }

        public void AddImage(int x, int y, int gumpID, int hue)
        {
            Add(new GumpImage(x, y, gumpID, hue));
        }

        public void AddImageTiled(int x, int y, int width, int height, int gumpID)
        {
            Add(new GumpImageTiled(x, y, width, height, gumpID));
        }

        public void AddItem(int x, int y, int itemID)
        {
            Add(new GumpItem(x, y, itemID));
        }

        public void AddItem(int x, int y, int itemID, int hue)
        {
            Add(new GumpItem(x, y, itemID, hue));
        }

        public void AddLabel(int x, int y, int hue, string text)
        {
            Add(new GumpLabel(x, y, hue, text));
        }

        public void AddLabelCropped(int x, int y, int width, int height, int hue, string text)
        {
            Add(new GumpLabelCropped(x, y, width, height, hue, text));
        }

        public void AddRadio(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
        {
            Add(new GumpRadio(x, y, inactiveID, activeID, initialState, switchID));
        }

        public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText)
        {
            Add(new GumpTextEntry(x, y, width, height, hue, entryID, initialText));
        }

        public void Add(GumpEntry g)
        {
            if (g.Parent != this)
            {
                g.Parent = this;
            }
            else if (!m_Entries.Contains(g))
            {
                Invalidate();
                m_Entries.Add(g);
            }
        }

        public void Remove(GumpEntry g)
        {
            Invalidate();
            m_Entries.Remove(g);
            g.Parent = null;
        }
        public string Center(string text)
        {
            return string.Format("<CENTER>{0}</CENTER>", text);
        }

        public string Color(string text, int color)
        {
            return string.Format("<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", color, text);
        }
        public string Color(string text)
        {
            return Color(text, 0xFFFFFF);
        }
        public int Intern(string value)
        {
            int indexOf = m_Strings.IndexOf(value);

            if (indexOf >= 0)
            {
                return indexOf;
            }
            else
            {
                Invalidate();
                return m_Strings.Add(value);
            }
        }

        public void SendTo(NetState state)
        {
            state.AddGump(this);
            state.Send(Compile());
        }

        private DisplayGumpFast m_Packet;

        public static byte[] StringToBuffer(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        private static byte[] m_BeginLayout = StringToBuffer("{ ");
        private static byte[] m_EndLayout = StringToBuffer(" }");

        private static byte[] m_NoMove = StringToBuffer("{ nomove }");
        private static byte[] m_NoClose = StringToBuffer("{ noclose }");
        private static byte[] m_NoDispose = StringToBuffer("{ nodispose }");
        private static byte[] m_NoResize = StringToBuffer("{ noresize }");

        private Packet Compile()
        {
            if (m_Packet == null)
            {
                DisplayGumpFast disp = new DisplayGumpFast(this);

                if (!m_Dragable)
                    disp.AppendLayout(m_NoMove);

                if (!m_Closable)
                    disp.AppendLayout(m_NoClose);

                if (!m_Disposable)
                    disp.AppendLayout(m_NoDispose);

                if (!m_Resizable)
                    disp.AppendLayout(m_NoResize);

                int count = m_Entries.Count;
                GumpEntry e;

                for (int i = 0; i < count; ++i)
                {
                    e = (GumpEntry)m_Entries[i];

                    disp.AppendLayout(m_BeginLayout);
                    e.AppendTo(disp);
                    disp.AppendLayout(m_EndLayout);
                }

                //disp.WriteText( m_Strings );
                List<string> strings = new List<string>();
                for (int i = 0; i < m_Strings.Count; i++)
                {
                    strings.Add(m_Strings[i] as string);
                }
                disp.WriteStrings(strings);

                m_TextEntries = disp.TextEntries;
                m_Switches = disp.Switches;

                m_Packet = disp;
            }

            return m_Packet;
        }

        public virtual void OnResponse(NetState sender, RelayInfo info)
        {
        }

        public virtual void OnServerClose(NetState owner)
        {
        }
    }
}