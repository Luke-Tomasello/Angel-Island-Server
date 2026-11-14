/***************************************************************************
 *
 *                   The Angel Island Music System (AIMS)
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts/Engines/Music/MusicCompositionBook.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 10/30/26 to 11/5/26)
 *      Paste (context menu): a whole composition in one bulletin board post, wrapped as the client wraps it. Start /
 *      Stop to preview the book. At most 96 pages (MaxPages; a 300-page book crashed ClassicUO), a documented
 *      dependency of MidiToAims.
 *      Portable AIMS: Paste opens the board with its own PasteBoard packet (the same bytes as BBDisplayBoard). Angel
 *      Island only (#if ANGEL_ISLAND): the ArrayList context menu (else List<ContextMenuEntry>).
*  1/31/22, Adam
*		Display the Name property instead of the page count.
 *  1/12/22, Adam
 *		Initial creation.
 */

using Server.ContextMenus;
using Server.Items;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Misc
{
    public class MusicCompositionBook : TanBook
    {
        // The largest book a client can open. ClassicUO draws a whole book as one texture, 8 lines a page, and a texture
        //  is at most 16,384 pixels tall: a 100-page book opens, a 300-page book crashed the client as it opened
        //  (10/30/26). A long song is written with sections (MusicCompiler), not in a bigger book.
        // MidiToAims.Core (Books.MaxPages) keeps a copy of this: change both ([m2a refuses when they differ).
        public const int MaxPages = 96;

        [Constructable]
        public MusicCompositionBook()
            : this(MaxPages, true)
        {
        }

        [Constructable]
        public MusicCompositionBook(int pageCount, bool writable)
            : base(Math.Clamp(pageCount, 1, MaxPages), writable)
        {
            Name = "music composition book";
        }
        public MusicCompositionBook(Serial serial)
            : base(serial)
        {
        }
        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, "{0}", Name);
            LabelTo(from, "{0} by {1}", Title, Author);
        }

        // preview the book (in your backpack): Start, or Stop while its preview plays
#if ANGEL_ISLAND
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
#else
        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
#endif
        {
            base.GetContextMenuEntries(from, list);

            if (from.CheckAlive() && from.Backpack != null && IsChildOf(from.Backpack))
            {
                list.Add(new PreviewEntry(from, this, ManuscriptPaper.IsPreviewing(from)));
                list.Add(new PasteEntry(from, this));
            }
        }

        // Paste: the client's bulletin board gump, opened for this book; its Post Message takes a long composition in
        //  one paste, line breaks and all (BaseBulletinBoard.BBClientRequest, OnPost)
        private class PasteEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;
            private readonly MusicCompositionBook m_Book;

            public PasteEntry(Mobile from, MusicCompositionBook book)
                : base(2082 /*Paste*/)
            {
                m_From = from;
                m_Book = book;
            }

            public override void OnClick()
            {
                if (!m_From.CheckAlive() || m_Book.Deleted || m_From.Backpack == null || !m_Book.IsChildOf(m_From.Backpack))
                    return;

                m_From.Send(new PasteBoard(m_Book.Serial, "paste a composition"));
                m_From.SendMessage("Click Post Message, paste your composition (Ctrl+V), and click Post.");
            }
        }

        // the client's bulletin board gump, for this book (ClassicUO uses only the serial): the same packet as a board's
        //  (BBDisplayBoard), so the music system needs nothing from the shard's bulletin boards to open it
        private sealed class PasteBoard : Packet
        {
            public PasteBoard(Serial serial, string name)
                : base(0x71)
            {
                if (name == null)
                    name = "";

                EnsureCapacity(38);

                byte[] buffer = Utility.UTF8.GetBytes(name);

                m_Stream.Write((byte)0x00); // PacketID
                m_Stream.Write((int)serial); // Bulletin board serial

                // Bulletin board name
                if (buffer.Length >= 29)
                {
                    m_Stream.Write(buffer, 0, 29);
                    m_Stream.Write((byte)0);
                }
                else
                {
                    m_Stream.Write(buffer, 0, buffer.Length);
                    m_Stream.Fill(30 - buffer.Length);
                }
            }
        }

        // a post from Paste (BaseBulletinBoard.BBClientRequest, 0x71 sub 5, after this book's serial): the thread replied
        //  to, the subject, the line count, the lines. ClassicUO sends the count as a byte, so a post of more than
        //  MaxPostLines lines arrives with its count wrapped and bytes left over: refused, nothing written
        public const int MaxPostLines = 255;
        public void OnPost(Mobile from, PacketReader pvSrc)
        {
            pvSrc.ReadInt32();                                          // the thread replied to: none
            pvSrc.ReadUTF8StringSafe(pvSrc.ReadByte());                 // the subject: not used
            string[] lines = new string[pvSrc.ReadByte()];
            for (int i = 0; i < lines.Length; ++i)
                lines[i] = pvSrc.ReadUTF8StringSafe(pvSrc.ReadByte());

            if (pvSrc.Size - pvSrc.Seek(0, SeekOrigin.Current) > 0)
            {
                from.SendMessage(0x22, "That is too long for one post: at most {0} lines. Paste it in parts.", MaxPostLines);
                return;
            }
            Paste(from, lines);
        }

        // the client's book: characters on a line (a pasted line wraps at 25), lines on a page
        public const int LineLength = 25, LinesPerPage = 8;

        // a post from Paste: its lines, wrapped as the client wraps them, added after the book's text. All or nothing: a
        //  paste that doesn't fit is refused. Blank lines are left out (reading a book drops them: ManuscriptPaper).
        //  A book still titled "Title" / "Author" takes the text's # Song: / # Author:.
        public void Paste(Mobile from, string[] posted)
        {
            if (!Writable)
            {
                from.SendMessage(0x22, "This book can't be written in.");
                return;
            }
            List<string> text = posted.Where(l => l.Trim().Length > 0).ToList();
            if (text.Count == 0)
            {
                from.SendMessage(0x22, "There was nothing to paste.");
                return;
            }

            // the book's text so far, without the empty lines at its end
            List<string> book = Pages.SelectMany(p => p.Lines ?? Array.Empty<string>()).ToList();
            while (book.Count > 0 && book[book.Count - 1].Trim().Length == 0)
                book.RemoveAt(book.Count - 1);
            List<string> added = Wrap(text);

            int used = (book.Count + LinesPerPage - 1) / LinesPerPage, needed = (added.Count + LinesPerPage - 1) / LinesPerPage;
            if (book.Count + added.Count > PagesCount * LinesPerPage)
            {
                from.SendMessage(0x22, "That needs {0} pages, and this book has {1} of its {2} pages left: nothing was written. Paste it into a new book, or in parts.",
                    needed, PagesCount - used, PagesCount);
                return;
            }

            book.AddRange(added);
            for (int p = 0; p < PagesCount; p++)
                Pages[p].Lines = book.Skip(p * LinesPerPage).Take(LinesPerPage).ToArray();

            // the cover, from the text's header (when the book has none of its own)
            string? Header(string key) => text.Select(l => l.TrimStart('#', ' ', '\t')).Where(l => l.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                .Select(l => l.Substring(key.Length).Trim()).FirstOrDefault(v => v.Length > 0);
            if ((string.IsNullOrWhiteSpace(Title) || Title == "Title") && Header("song:") is string song)
                Title = song;
            if ((string.IsNullOrWhiteSpace(Author) || Author == "Author") && Header("author:") is string author)
                Author = author;

            from.SendMessage(0x40, "Wrote {0} lines into the book: it now fills {1} of its {2} pages. If the book is open, close it and open it again to see them.",
                text.Count, (book.Count + LinesPerPage - 1) / LinesPerPage, PagesCount);
        }

        // the width of each character ' ' to '~' in the client's book font (unifont1.mul, font 1: offset + width + 1;
        //  a space is 8), in pixels; others count 8
        private static readonly byte[] GlyphWidths =
        {
            8, 3, 4, 12, 9, 10, 11, 3, 4, 4, 10, 7, 3, 6, 3, 9, 8, 4, 8, 8, 8, 8, 8, 8, 8, 8, 3, 3, 8, 6, 8, 7,
            12, 8, 8, 8, 8, 7, 7, 8, 8, 3, 8, 8, 7, 10, 8, 8, 8, 9, 8, 8, 7, 8, 8, 12, 8, 9, 8, 4, 9, 5, 10, 8,
            3, 6, 6, 6, 6, 6, 6, 6, 6, 3, 6, 6, 3, 9, 6, 6, 6, 6, 6, 6, 6, 6, 6, 8, 6, 6, 6, 5, 2, 5, 6,
        };

        // a book line ends a line (ClassicUO puts a line break after it as it shows the book) only when it is narrower
        //  than this: its width and a space under the page's 156 px (ModernBookGump.ServerSetBookText). A wider one is
        //  joined to the next line on screen, and an edit of its page sends them back joined.
        private const int BreakWidth = 156 - 8;

        private static int Width(string s) => s.Sum(c => c >= ' ' && c <= '~' ? GlyphWidths[c - ' '] : 8);

        // the client's book wrap: lines of at most LineLength characters; a piece that would read as a new line (#, say,
        //  wait) is wrapped one character earlier, as ManuscriptPaper's line rejoining (CheckBookWrap) expects; a line's
        //  last piece too wide to end a line on screen (BreakWidth) leaves its last word (or character) to a book line of
        //  its own. A copy of MidiToAims.Core's Books.Wrap (which lays out [m2a's books): change them together.
        public static List<string> Wrap(IEnumerable<string> lines)
        {
            List<string> book = new List<string>();
            foreach (string line in lines)
            {
                for (int at = 0; at < Math.Max(1, line.Length); at += LineLength)
                {
                    string piece = line.Length == 0 ? "" : line.Substring(at, Math.Min(LineLength, line.Length - at));
                    string probe = piece.ToLowerInvariant();
                    if (at > 0 && (probe.StartsWith("#") || probe.StartsWith("say ") || probe.StartsWith("wait ")))
                    {
                        book[book.Count - 1] += piece[0];
                        piece = piece.Substring(1);
                    }
                    book.Add(piece);
                }
                string last = book[book.Count - 1];
                if (Width(last) >= BreakWidth)
                {
                    int cut = last.LastIndexOf(' ') > 0 ? last.LastIndexOf(' ') : last.Length - 1;
                    string tail = last.Substring(cut), probe = tail.ToLowerInvariant();
                    if (probe.StartsWith("#") || probe.StartsWith("say ") || probe.StartsWith("wait "))
                    {
                        cut++;
                        tail = last.Substring(cut);
                    }
                    book[book.Count - 1] = last.Substring(0, cut);
                    book.Add(tail);
                }
            }
            return book;
        }

        private class PreviewEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;
            private readonly MusicCompositionBook m_Book;
            private readonly bool m_Stop;

            public PreviewEntry(Mobile from, MusicCompositionBook book, bool stop)
                : base(stop ? 5114 /*Stop*/ : 5113 /*Start*/)
            {
                m_From = from;
                m_Book = book;
                m_Stop = stop;
            }

            public override void OnClick()
            {
                if (!m_From.CheckAlive() || m_Book.Deleted)
                    return;

                if (m_Stop)
                    ManuscriptPaper.StopPreview(m_From);
                else if (m_From.Backpack != null && m_Book.IsChildOf(m_From.Backpack) && ManuscriptPaper.CanPreview(m_From))
                    ManuscriptPaper.PreviewBook(m_From, m_Book);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }
    }
}