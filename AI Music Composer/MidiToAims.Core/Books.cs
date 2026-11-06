/***************************************************************************
 *
 *  MidiToAims.Core - Books: a composition laid out in music composition books, as the shard and the client hold them.
 *  Shared by the command line (a .rzr per post, for a book's Paste in the game) and the shard's [m2a (books in a bag).
 *
 *  The limits are copies, because Core has no shard dependencies; they are dependencies on the shard and the client.
 *  Change them together:
 *      MaxPages 96         MusicCompositionBook.MaxPages (Scripts\Engines\AIMusicSystem\RSM): about the most the
 *                          client supports (ClassicUO draws a book as one texture; a 300-page book crashed it)
 *      MaxBooks 9          ManuscriptPaper.LocationTarget.MaxBooks: the books one song may span
 *      LineLength 25       the client's book: characters on a line (a pasted line wraps at 25)
 *      LinesPerPage 8      the client's book: lines on a page
 *      PostLines 250       the most lines in one post (a book's Paste: the client's bulletin board gump, Post Message): the
 *                          client sends the count as a byte (255 at most, and a file's final line break adds one).
 *                          Pasting into the book itself takes far less: ClassicUO draws the whole book as one texture,
 *                          at most 16384 px tall, a blank 96-page book is already 13820 px of empty lines, and a paste is
 *                          drawn before the overflow is trimmed, so about 96 lines (12 pages) crashed the client beyond
 *                          that ("Texture2D creation failed"). Until 11/3/26 the command line cut books into such pastes.
 *      PostChars 18000     the most characters in one post: the client draws the post box as one texture, 220 px wide;
 *                          about 20,000 characters reach its 16384 px (measured: Golden, 15,326 characters, 10,878 px)
 *  [m2a compares MaxPages and MaxBooks with the shard's each time it loads this assembly, and refuses when they differ.
 *  Wrap (the client's book wrap, with GlyphWidths and BreakWidth) has a copy in the shard, MusicCompositionBook.Wrap
 *  (a book's Paste): change both.
 *
 *  DefaultPages (95) is how many of a book's pages the converter fills (the .m2a's 'Book pages'): a page of headroom
 *  for the composer's own lines (header settings, lyrics, notes). A composer may set it as high as MaxPages.
 *
 ***************************************************************************/

namespace MidiToAims
{
    public static class Books
    {
        public const int MaxPages = 96;
        public const int MaxBooks = 9;
        public const int LineLength = 25;
        public const int LinesPerPage = 8;
        public const int DefaultPages = 95;
        public const int PostLines = 250;
        public const int PostChars = 18000;

        // the composition as books of at most `pages` pages: one if it fits, else several (# Song: / # Book: n/m), each
        //  starting at a phrase block (after a wait line), with the header (the song's settings) in the first. Each book
        //  is its text, a line per line (wrapped as it goes into the book). Empty if it needs more than MaxBooks.
        public static string[] Split(string rzr, string song, int pages)
        {
            List<string> lines = rzr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Where(l => l.Length > 0).ToList();
            int capacity = pages * LinesPerPage;
            if (Wrap(lines).Count <= capacity)
                return new[] { Text(lines) };

            int headerCount = lines.TakeWhile(l => l.StartsWith("#")).Count();
            List<string> header = lines.Take(headerCount).ToList();
            List<List<string>> blocks = new();
            List<string> current = new();
            foreach (string line in lines.Skip(headerCount))
            {
                current.Add(line);
                if (line.StartsWith("wait "))
                {
                    blocks.Add(current);
                    current = new List<string>();
                }
            }
            if (current.Count > 0)
                blocks.Add(current);

            // pack the blocks (with "# Book: n/9" as a placeholder of the same length), then number the books
            List<List<string>> books = new();
            List<string> book = new(header) { "# Book: 1/9" };
            foreach (List<string> block in blocks)
            {
                if (Wrap(book.Concat(block).ToList()).Count > capacity && book.Count > 2)
                {
                    books.Add(book);
                    book = new List<string> { "# Song: " + song, "# Book: " + (books.Count + 1) + "/9" };
                }
                book.AddRange(block);
            }
            books.Add(book);
            if (books.Count > MaxBooks)
                return Array.Empty<string>();

            for (int b = 0; b < books.Count; b++)
            {
                int at = books[b].FindIndex(l => l.StartsWith("# Book: "));
                books[b][at] = string.Format("# Book: {0}/{1}", b + 1, books.Count);
            }
            return books.Select(Text).ToArray();
        }

        // a book's lines as the client wraps them, for writing its pages directly
        public static string[] Lines(string book) =>
            Wrap(book.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Where(l => l.Length > 0).ToList()).ToArray();

        // the pages a book fills
        public static int Pages(string book) => (Lines(book).Length + LinesPerPage - 1) / LinesPerPage;

        // a book as the posts a player makes with its Paste (MusicCompositionBook: the client's bulletin board gump, Post
        //  Message), each at most PostLines lines and PostChars characters, ending after a wait line where it can;
        //  joined, the posts are the book's text
        public static string[] Posts(string book) => Pieces(book, l => 1, PostLines, PostChars);

        // a book in pieces, each at most maxCost (each line's cost) and maxChars, ending after a wait line (a phrase
        //  block) where it can: a block too big for one piece is split between lines
        private static string[] Pieces(string book, Func<string, int> cost, int maxCost, int maxChars)
        {
            List<string> lines = book.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Where(l => l.Length > 0).ToList();
            List<string> pieces = new();
            List<string> piece = new(), block = new();
            int pieceCost = 0, pieceChars = 0, blockCost = 0, blockChars = 0;
            void EndPiece()
            {
                if (piece.Count > 0)
                    pieces.Add(Text(piece));
                piece = new List<string>();
                pieceCost = pieceChars = 0;
            }
            for (int i = 0; i < lines.Count; i++)
            {
                block.Add(lines[i]);
                blockCost += cost(lines[i]);
                blockChars += lines[i].Length + 1;
                if (!lines[i].StartsWith("wait ") && i < lines.Count - 1)
                    continue;
                if (pieceCost + blockCost > maxCost || pieceChars + blockChars > maxChars)
                    EndPiece();
                if (blockCost > maxCost || blockChars > maxChars)
                {
                    // a phrase block too big for one piece: split it between lines
                    foreach (string l in block)
                    {
                        int n = cost(l);
                        if (pieceCost + n > maxCost || pieceChars + l.Length + 1 > maxChars)
                            EndPiece();
                        piece.Add(l);
                        pieceCost += n;
                        pieceChars += l.Length + 1;
                    }
                }
                else
                {
                    piece.AddRange(block);
                    pieceCost += blockCost;
                    pieceChars += blockChars;
                }
                block = new List<string>();
                blockCost = blockChars = 0;
            }
            EndPiece();
            return pieces.ToArray();
        }

        private static string Text(List<string> lines) => string.Join(Environment.NewLine, lines) + Environment.NewLine;

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
        //  wait) is wrapped one character earlier, as ManuscriptPaper's line rejoining expects; a line's last piece too
        //  wide to end a line on screen (BreakWidth) leaves its last word (or character) to a book line of its own
        private static List<string> Wrap(List<string> lines)
        {
            List<string> book = new();
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
    }
}
