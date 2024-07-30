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

/* Scripts/Items/Books/BaseBook.cs
 * ChangeLog
 *  7/17/2023, Adam
 *      Set the book's weight during construction
 *  11/07/21, Yoar
 *      Replaced BaseBook.m_Copyable field with a virtual BaseBook.Copyable getter.
 *  9/25/21, Adam
 *      Update SaveBook to save it in a format which is easy to create new books from:
 *      "line one",
 *      "line two",
 *      etc.
 *	4/17/10, adam
 *		Add BookSubtype and BookVersion - 
 *			allows us to check for types of a book already on a player so that we don't keep giving them
 *	3/22/10, adam
 *		use the better logger for logging book updates.
 *		PS. the old way didn't include the books itemid!
 *	03/11/07, weaver
 *		Added set option for pages collection which creates new pages based on
 *		those located at bpi references passed.
 *	03/01/06, weaver
 *		Added deserialisation to fix old mirrored books.
 *	03/01/06, weaver
 *		Altered AddPage() so that new BookPageInfo is generated rather than referencing
 *		source book's bpi.
 *	03/14/05, weaver
 *		Added logging of all changes made.
 *  07/02/05 TK
 *		Added Copyable property
 *  03/02/05 Taran Kain
 *		Changed page and line lists to ArrayLists to allow cleaner-looking and easier
 *		to create books.
 *		Added AddLine
 *		Added AddPage
 *		All existing interfaces still work exactly as they did with arrays, so no old
 *		code will be broken.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using Server.Network;
using Server.Targeting;
using System;
using System.IO;

namespace Server.Items
{
    public class BookPageInfo
    {
        private string[] m_Lines;

        public string[] Lines
        {
            get
            {
                return m_Lines;
            }
            set
            {
                m_Lines = value;
            }
        }

        public void AddLine(string line)
        {
            // m_Lines.Add(line);

            string[] copy = new string[m_Lines.Length + 1];

            for (int i = 0; i < copy.Length - 1; ++i)
                copy[i] = m_Lines[i];

            // add new page
            copy[copy.Length - 1] = line;

            m_Lines = copy;
        }

        public BookPageInfo()
        {
            m_Lines = new string[0];
        }

        public BookPageInfo(params string[] lines)
        {
            m_Lines = lines;
        }

        public BookPageInfo(GenericReader reader)
        {
            int length = reader.ReadInt();

            m_Lines = new string[length];

            for (int i = 0; i < m_Lines.Length; ++i)
                m_Lines[i] = Utility.Intern(reader.ReadString());
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(m_Lines.Length);

            for (int i = 0; i < m_Lines.Length; ++i)
                writer.Write(m_Lines[i]);
        }
    }

    public class BaseBook : Item
    {
        private string m_Title;
        private string m_Author;
        private BookPageInfo[] m_Pages;
        private bool m_Writable;

        #region BookSubtype
        // allows us to check for types of a book already on a player so that we don't keep giving them
        public enum BookSubtype { None, CTFHelp, CTFRules }
        private BookSubtype m_Subtype = BookSubtype.None;
        [CommandProperty(AccessLevel.GameMaster)]
        public BookSubtype Subtype { get { return m_Subtype; } set { m_Subtype = value; } }

        private double m_Version = 1.0;
        public double Version { get { return m_Version; } set { m_Version = value; } }
        #endregion BookSubtype

        private string m_LastEdited; //erl: new LastEdited property

        [CommandProperty(AccessLevel.GameMaster)]
        public string Title
        {
            get { return m_Title; }
            set { m_Title = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Author
        {
            get { return m_Author; }
            set { m_Author = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Writable
        {
            get { return m_Writable; }
            set { m_Writable = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PagesCount
        {
            get { return m_Pages.Length; }
        }

        public BookPageInfo[] Pages
        {
            get { return m_Pages; }

            /* We'll try doing it the RunUO 2.0 way
			// wea: 11/Mar/2007 : Added set option which creates new pages
			// based on bpi data passed
			set
			{
				m_Pages = new ArrayList();

				for (short ibpi = 0; ibpi < value.Length; ibpi++)
					this.AddPage(value[ibpi]);
			}*/
        }

        public virtual double BookWeight { get { return 1.0; } }

        public virtual bool Copyable { get { return true; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string LastEdited
        {
            get { return (m_LastEdited); }
            set
            {
                m_LastEdited = value;
            }
        }

        [Constructable]
        public BaseBook(int itemID)
            : this(itemID, 20, true)
        {
        }

        [Constructable]
        public BaseBook(int itemID, int pageCount, bool writable)
            : this(itemID, "Title", "Author", pageCount, writable)
        {
        }

        [Constructable]
        public BaseBook(int itemID, string title, string author, int pageCount, bool writable)
            : base(itemID)
        {
            m_Title = title;
            m_Author = author;
            m_Writable = writable;

            BookContent content = this.DefaultContent;

            if (content == null)
            {
                m_Pages = new BookPageInfo[pageCount];

                for (int i = 0; i < m_Pages.Length; ++i)
                    m_Pages[i] = new BookPageInfo();
            }
            else
            {
                m_Pages = content.Copy();
            }

            Weight = BookWeight;
        }

        // Intended for defined books only
        public BaseBook(int itemID, bool writable)
            : base(itemID)
        {
            m_Writable = writable;

            BookContent content = this.DefaultContent;

            if (content == null)
            {
                m_Pages = new BookPageInfo[0];
            }
            else
            {
                m_Title = content.Title;
                m_Author = content.Author;
                m_Pages = content.Copy();
            }

            Weight = BookWeight;
        }

        public virtual BookContent DefaultContent { get { return null; } }

        public BaseBook(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)5); // version

            // version 4
            // allows us to check for types of a book already on a player so that we don't keep giving them
            writer.Write((int)m_Subtype);       // type of book
            writer.Write(m_Version);            // book version

            // version 2
            writer.Write(m_LastEdited);         //erl: for change logging

            // version 1
            //writer.Write((bool)m_Copyable);

            // version 0
            //BookPageInfo[] bpi = (BookPageInfo[])m_Pages.ToArray(typeof(BookPageInfo));
            BookPageInfo[] bpi = m_Pages;

            writer.Write(m_Title);
            writer.Write(m_Author);
            writer.Write(m_Writable);

            writer.Write(bpi.Length);

            for (int i = 0; i < bpi.Length; ++i)
                bpi[i].Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_LastEdited = ""; // erl: init LastEdited string

            switch (version)
            {
                case 5: // Yoar: Replaced m_Copyable field with a virtual Copyable getter
                case 4:
                    {
                        m_Subtype = (BookSubtype)reader.ReadInt();
                        m_Version = reader.ReadDouble();
                        goto case 3;
                    }
                case 3: // wea: mirrored bpi fix
                    {
                        goto case 2;
                    }
                case 2: // erl: new LastEdited property
                    {
                        m_LastEdited = reader.ReadString();
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 5)
                            reader.ReadBool(); // m_Copyable
                        goto case 0;
                    }
                case 0:
                    {
                        m_Title = reader.ReadString();
                        m_Author = reader.ReadString();
                        m_Writable = reader.ReadBool();

                        BookPageInfo[] bpi = new BookPageInfo[reader.ReadInt()];
                        for (int i = 0; i < bpi.Length; ++i)
                            bpi[i] = new BookPageInfo(reader);

                        m_Pages = bpi;

                        /* Run2.0 style now
						if (version <= 2)
						{
							// wea: freshen up this bpi list so that the book's pages
							// are properly defined

							m_Pages = new ArrayList();

							for (int i = 0; i < bpi.Length; ++i)
							{
								BookPageInfo bpiFresh = new BookPageInfo();
								bpiFresh.Lines = bpi[i].Lines;

								m_Pages.Add(bpiFresh);
							}
						}
						else
						{
							// This book doesn't need fixing
							m_Pages = new ArrayList(bpi);
						}*/

                        break;
                    }
            }

            Weight = BookWeight;
        }

        public virtual void AddPage(BookPageInfo bpi)
        {
            /*
			// wea: modified so that new bpi is generated rather than associating source
			BookPageInfo bpiNewPage = new BookPageInfo();
			bpiNewPage.Lines = bpi.Lines;
			m_Pages.Add(bpiNewPage);*/

            BookPageInfo[] copy = new BookPageInfo[m_Pages.Length + 1];

            for (int i = 0; i < copy.Length - 1; ++i)
                copy[i] = new BookPageInfo(m_Pages[i].Lines);

            // add new page
            copy[copy.Length - 1] = bpi;

            m_Pages = copy;
        }

        public virtual void ClearPages()
        {
            m_Pages = new BookPageInfo[0];
        }

        public virtual void AddLine(string line)
        {
            if (m_Pages.Length <= 0 || m_Pages[m_Pages.Length - 1].Lines.Length >= 8)
                AddPage(new BookPageInfo());

            m_Pages[m_Pages.Length - 1].AddLine(line);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Title != null && m_Title.Length > 0)
                list.Add(1060658, "Title\t{0}", m_Title); // ~1_val~: ~2_val~

            if (m_Author != null && m_Author.Length > 0)
                list.Add(1060659, "Author\t{0}", m_Author); // ~1_val~: ~2_val~

            if (m_Pages != null && m_Pages.Length > 0)
                list.Add(1060660, "Pages\t{0}", m_Pages.Length); // ~1_val~: ~2_val~
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, "{0} by {1}", m_Title, m_Author);
            LabelTo(from, "[{0} pages]", m_Pages.Length);
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.Send(new BookHeader(from, this));
            from.Send(new BookPageDetails(this));
        }

        public static void Initialize()
        {
            PacketHandlers.Register(0xD4, 0, true, new OnPacketReceive(HeaderChange));
            PacketHandlers.Register(0x66, 0, true, new OnPacketReceive(ContentChange));
            PacketHandlers.Register(0x93, 99, true, new OnPacketReceive(OldHeaderChange));
            Server.CommandSystem.Register("SaveBook", AccessLevel.Administrator, new CommandEventHandler(SaveBook_OnCommand));
        }

        public static void SaveBook_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.Target = new SaveBookTarget();
                e.Mobile.SendMessage("Save which book?");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

        }

        private class SaveBookTarget : Target
        {
            public SaveBookTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                try
                {
                    if (targ is BaseBook == false)
                    {
                        from.SendMessage("That is not a book.");
                    }
                    else
                    {
                        BaseBook book = targ as BaseBook;
                        LogHelper Logger = new LogHelper("savebook.log", false, false);
                        string sitem = Logger.Format(LogType.Item, book, null);
                        for (int ix = 0; ix < book.PagesCount; ix++)
                        {
                            string line = "";
                            for (int mx = 0; mx < book.Pages[ix].Lines.Length; mx++)
                            {
                                line = string.Format("{0}", book.Pages[ix].Lines[mx]);
                                line = "\"" + line + "\"" + ",";
                                Logger.Log(line);
                            }
                            Logger.Log("--- next page ---");
                        }
                        Logger.Finish();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
                return;
            }
        }

        public static void OldHeaderChange(NetState state, PacketReader pvSrc)
        {
            Mobile from = state.Mobile;
            BaseBook book = World.FindItem(pvSrc.ReadInt32()) as BaseBook;

            if (book == null || !book.Writable || !from.InRange(book.GetWorldLocation(), 1))
                return;

            pvSrc.Seek(4, SeekOrigin.Current); // Skip flags and page count

            string title = pvSrc.ReadStringSafe(60);
            string author = pvSrc.ReadStringSafe(30);

            book.Title = Utility.FixHtml(title);
            book.Author = Utility.FixHtml(author);
        }

        public static void HeaderChange(NetState state, PacketReader pvSrc)
        {
            Mobile from = state.Mobile;
            BaseBook book = World.FindItem(pvSrc.ReadInt32()) as BaseBook;

            if (book == null || !book.Writable || !from.InRange(book.GetWorldLocation(), 1))
                return;

            pvSrc.Seek(4, SeekOrigin.Current); // Skip flags and page count

            int titleLength = pvSrc.ReadUInt16();

            if (titleLength > 60)
                return;

            string title = pvSrc.ReadUTF8StringSafe(titleLength);

            int authorLength = pvSrc.ReadUInt16();

            if (authorLength > 30)
                return;

            string author = pvSrc.ReadUTF8StringSafe(authorLength);

            book.Title = Utility.FixHtml(title);
            book.Author = Utility.FixHtml(author);
        }

        public static void ContentChange(NetState state, PacketReader pvSrc)
        {
            Mobile from = state.Mobile;
            BaseBook book = World.FindItem(pvSrc.ReadInt32()) as BaseBook;

            string ParsedContents = "";

            if (book == null || !book.Writable || !from.InRange(book.GetWorldLocation(), 1))
                return;

            int pageCount = pvSrc.ReadUInt16();

            if (pageCount > book.PagesCount)
                return;

            for (int i = 0; i < pageCount; ++i)
            {
                int index = pvSrc.ReadUInt16();

                if (index >= 1 && index <= book.PagesCount)
                {
                    --index;

                    int lineCount = pvSrc.ReadUInt16();

                    if (lineCount <= 8)
                    {
                        string[] lines = new string[lineCount];

                        for (int j = 0; j < lineCount; ++j)
                        {
                            if ((lines[j] = pvSrc.ReadUTF8StringSafe()).Length >= 80)
                                return;
                            ParsedContents = ParsedContents + lines[j];
                        }

                        book.Pages[index].Lines = lines;

                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            // log the changed content (applied on page turn or book close, records page changed)
            LogHelper Logger = new LogHelper("bookchange.log", false, true);
            string sitem = Logger.Format(LogType.Item, book, null);
            string smobile = Logger.Format(LogType.Mobile, from, null);
            Logger.Log(string.Format("Book: {0}\nOwner: {1}\nText: {2}", sitem, smobile, ParsedContents));
            Logger.Finish();

            // Update LastEdited property
            book.LastEdited = from.Serial.ToString();
        }

    }

    public sealed class BookPageDetails : Packet
    {
        public BookPageDetails(BaseBook book)
            : base(0x66)
        {
            EnsureCapacity(256);

            m_Stream.Write((int)book.Serial);
            m_Stream.Write((ushort)book.PagesCount);

            for (int i = 0; i < book.PagesCount; ++i)
            {
                BookPageInfo page = book.Pages[i];

                m_Stream.Write((ushort)(i + 1));
                m_Stream.Write((ushort)page.Lines.Length);

                for (int j = 0; j < page.Lines.Length; ++j)
                {
                    byte[] buffer = Utility.UTF8.GetBytes(page.Lines[j]);

                    m_Stream.Write(buffer, 0, buffer.Length);
                    m_Stream.Write((byte)0);
                }
            }
        }
    }

    public sealed class BookHeader : Packet
    {
        public BookHeader(Mobile from, BaseBook book)
            : base(0xD4)
        {
            string title = book.Title == null ? "" : book.Title;
            string author = book.Author == null ? "" : book.Author;

            byte[] titleBuffer = Utility.UTF8.GetBytes(title);
            byte[] authorBuffer = Utility.UTF8.GetBytes(author);

            EnsureCapacity(15 + titleBuffer.Length + authorBuffer.Length);

            m_Stream.Write((int)book.Serial);
            m_Stream.Write((bool)true);
            m_Stream.Write((bool)book.Writable && from.InRange(book.GetWorldLocation(), 1));
            m_Stream.Write((ushort)book.PagesCount);

            m_Stream.Write((ushort)(titleBuffer.Length + 1));
            m_Stream.Write(titleBuffer, 0, titleBuffer.Length);
            m_Stream.Write((byte)0); // terminate

            m_Stream.Write((ushort)(authorBuffer.Length + 1));
            m_Stream.Write(authorBuffer, 0, authorBuffer.Length);
            m_Stream.Write((byte)0); // terminate
        }
    }
}