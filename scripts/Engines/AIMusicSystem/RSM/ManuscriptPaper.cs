/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
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

/* Scripts/Engines/Music/ManuscriptPaper.cs
 * Changelog
 *  11/5/26, Adam (2026 modernization, 10/29/26 to 11/5/26)
 *      The book is compiled (MusicCompiler) before it becomes sheet music: sections, plays and repeats are expanded,
 *      and a book without them compiles to itself (same script, same hash); a book with mistakes is refused, page by
 *      page. A song too long for one book is written in several (# Song: <title>, # Book: 2/3) and made into one sheet
 *      from all of them. A preview ([play preview, or the book's Start / Stop) plays the book now, lyrics included. A
 *      book without # Song: / # Author: keeps its own title and author. Percussion needs no music karma, and a sound
 *      effect ending a line is checked too. Transcribing says what it made ("Sheet music created: ..."). MaxBooks is a
 *      documented dependency of MidiToAims.
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: the preview's temporary sheet. Angel Island only (#if ANGEL_ISLAND): Dupe.
 *  3/3/22, Adam (Music Karma / IsSound)
 *      Don't allow the creation of Rolled Up Sheet music if the author is using sound effects and doesn't have sufficient Music Karma.
 *  2/26/22, Adam (ConcertMode)
 *      Add support for ConcertMode
 *  2/19/22, (Napster)
 *      Add support for new Napster style music sharing
 *  2/4/22, Adam
 *      Add support for prefetch (need to pass it to the output RolledUpSheetMusic)
 *  1/27/22, Adam (CompileHashCode)
 *      Make CompileHashCode public so Nuke can use it to patch existing music
 *      Update CompileHashCode so that it adds a small amount of copy protection to music.
 *      Copy Protection: we only hash every mod 5th byte, so players can't recreate someone's work, change one note/pause
 *          and call it their own (well, they would need to be lucky and change mod 5th byte.)
 *  1/25/22, Adam
 *      Automatically parse Author, Title, Pricing, and Instrument from RZR file. 
 *      We then pass this metadata to the RolledUpSheetMusic.
 *  1/13/22, Adam (TrueLineParser)
 *      Add the TrueLineParser to fix all the funk linebreaks you get when pasting into a book.
 *  1/12/22, Adam
 *		Initial creation.
 */
using Server.Misc;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Server.Items
{
    public class ManuscriptPaper : Item
    {
        [Constructable]
        public ManuscriptPaper()
            : this(1)
        {
        }

        [Constructable]
        public ManuscriptPaper(int amount)
            : base(0xE34 /*0xEF3 has magically odd behavior*/)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
            Name = "manuscript paper";
        }

        public ManuscriptPaper(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage("Target the music composition book to transcribe...");
            from.Target = new LocationTarget(this); // Call our target
        }

        #region Preview
        // the preview each composer is hearing: a temporary sheet, never saved, deleted when it ends
        private static readonly Dictionary<Mobile, RolledUpSheetMusic> s_previews = new Dictionary<Mobile, RolledUpSheetMusic>();

        public static bool IsPreview(RazorInstrument? instrument)
        {
            return instrument is RolledUpSheetMusic rsm && s_previews.ContainsValue(rsm);
        }

        /// <summary>
        /// [play preview: hear a composition book as its sheet music will play (compiled), before any paper is used.
        /// Again while it plays: stop it.
        /// </summary>
        public static void Preview(Mobile from)
        {
            if (StopPreview(from) || !CanPreview(from))
                return;

            from.SendMessage("Target the music composition book to preview...");
            from.Target = new PreviewTarget();
        }

        // is this composer hearing a preview?
        public static bool IsPreviewing(Mobile from)
        {
            return s_previews.TryGetValue(from, out RolledUpSheetMusic? previewing) && !previewing.Deleted && previewing.IsThisDevicePlaying(from);
        }

        // stops this composer's preview, if one is playing
        public static bool StopPreview(Mobile from)
        {
            if (!IsPreviewing(from))
                return false;

            s_previews[from].CancelPlayback(from, say: false);
            from.SendMessage("Preview stopped.");
            return true;
        }

        public static bool CanPreview(Mobile from)
        {
            if (RazorInstrument.PerformanceOf(from) != null)
            {
                from.SendMessage("You are already playing music. Stop it first, then preview.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Plays a composition book now, compiled, on a temporary sheet that is never saved and is deleted when it ends
        /// ([play preview, or the book's context menu).
        /// </summary>
        public static void PreviewBook(Mobile from, MusicCompositionBook mcb)
        {
            RolledUpSheetMusic? rsm = new LocationTarget(null!).Transcribe(from, mcb);
            if (rsm == null)
                return;     // Transcribe told them why

            // a temporary sheet: it plays wherever it is, is never saved, and ends if the composer leaves
            rsm.RequiresBackpack = false;
            AimsHost.Current.MarkTemporary(rsm);       // never saved, and left alone by the shard's cleanup rules
            rsm.CheckSourceStatus = (m, source) => m != null && !m.Deleted && m.NetState != null;
            // the song's spoken lines (say 'la la') come from the sheet's source: its holder, or its PlayItem. The temporary
            //  sheet has no holder, so the book speaks for it: through the composer carrying it, or from the ground
            rsm.PlayItem = mcb;

            RazorInstrument.Performance before = rsm.CurrentPerformance;
            rsm.OnDoubleClick(from);
            RazorInstrument.Performance performance = rsm.CurrentPerformance;
            if (ReferenceEquals(performance, before))
            {   // it did not start (OnDoubleClick said why)
                rsm.Delete();
                return;
            }

            s_previews[from] = rsm;
            from.SendMessage("Previewing {0}. Type [play preview again (or Stop on the book) to stop it.", rsm.Title);
            performance.Completed += (p, reason) =>
            {
                if (s_previews.TryGetValue(from, out RolledUpSheetMusic? current) && ReferenceEquals(current, rsm))
                    s_previews.Remove(from);
                Timer.DelayCall(TimeSpan.Zero, () => { if (!rsm.Deleted) rsm.Delete(); });
            };
        }

        private class PreviewTarget : Target
        {
            public PreviewTarget()
                : base(2, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is not MusicCompositionBook mcb)
                {
                    from.SendMessage("That is not a music composition book.");
                    return;
                }

                if (CanPreview(from))
                    PreviewBook(from, mcb);
            }
        }
        #endregion Preview

        public class LocationTarget : Target // Create our targeting class (which we derive from the base target class)
        {
            private ManuscriptPaper m_manuscript;
            public LocationTarget(ManuscriptPaper manuscript)
                : base(2, false, TargetFlags.None)
            {
                m_manuscript = manuscript;
            }

            protected override void OnTarget(Mobile from, object target)
            {   // added owner for clarity only
                Mobile owner = from;
                if (target is MusicCompositionBook mcb)
                {
                    if (mcb != null)
                    {
                        RolledUpSheetMusic? rsm = Transcribe(owner, mcb);
                        if (rsm == null)
                            return;     // Transcribe told them why; the paper is not used

                        if (m_manuscript.Parent is Container c && c != null)
                        {
                            c.AddItem(rsm);
                            if (m_manuscript.Amount == 1)
                                rsm.Location = m_manuscript.Location;
                            m_manuscript.Consume();
                        }
                        else
                        {
                            rsm.MoveToWorld(m_manuscript.Location, m_manuscript.Map);
                            m_manuscript.Consume();
                        }

                        // tell the composer: the song, about how long it plays (its waits), the books it took, its price
                        long ms = 0;
                        foreach (Match wait in Regex.Matches(rsm.Composition ?? "", @"^\s*wait\s+(\d+)", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                            if (long.TryParse(wait.Groups[1].Value, out long w))
                                ms += w;
                        Match of = Regex.Match(BookText(mcb), @"#\s*Book:\s*\d+\s*/\s*(\d+)", RegexOptions.IgnoreCase);
                        int books = of.Success && int.TryParse(of.Groups[1].Value, out int n) ? n : 1;
                        List<string> details = new List<string>();
                        if (ms > 0)
                            details.Add(string.Format("about {0}:{1:00}", ms / 60000, ms / 1000 % 60));
                        if (books > 1)
                            details.Add(string.Format("from {0} books", books));
                        from.SendMessage("Sheet music created: \"{0}\" by {1}{2}{3}.", rsm.Title, rsm.Author,
                            details.Count > 0 ? " (" + string.Join(", ", details) + ")" : "",
                            rsm.Price > 0 ? string.Format(", priced at {0} gold", rsm.Price) : "");
                    }
                    else
                        from.SendMessage("That is not a music composition book.");
                }
            }

            /// <summary>
            /// The sheet music a composition book makes: its text, preprocessed and compiled (MusicCompiler: sections,
            /// plays and repeats expanded), with the book's directives. Null if the book cannot be made into music; the
            /// composer is told why. Used to transcribe (OnTarget) and to preview ([play preview).
            /// A song too long for one book is written in several (# Book: 2/3): any of them makes the whole song, from
            /// all of its books, which must be together in one backpack or bag (Books).
            /// </summary>
            public RolledUpSheetMusic? Transcribe(Mobile from, MusicCompositionBook mcb)
            {
                // the song's books, in order (most songs: this one)
                List<MusicCompositionBook>? books = Books(from, mcb);
                if (books == null)
                    return null;    // Books told them why
                mcb = books[0];     // the song's settings and title come from its first book

                // read the books text
                string lines = string.Empty;
                foreach (MusicCompositionBook book in books)
                    lines += BookText(book);
                // make sure the book contains something!
                if (string.IsNullOrEmpty(lines))
                {
                    from.SendMessage("your music composition book seems to be empty.");
                    return null;
                }

                // preprocess lines: remove 'Loop', empty lines,
                //  and fix 'book induced wrap'
                lines = PreProcesss(lines);

                // compile: sections, plays and repeats become the plain script the player plays (a book without them
                //  is unchanged); the directives are read from the book itself, less its section markers
                MusicCompiler.Result compiled = MusicCompiler.Compile(lines, LineWhere(books));
                if (!compiled.Success)
                {
                    from.SendMessage(33, "Your music composition book has {0}:", compiled.ErrorCount == 1 ? "a mistake" : compiled.ErrorCount + " mistakes");
                    foreach (string error in compiled.Errors)
                        from.SendMessage(33, error);
                    if (compiled.ErrorCount > compiled.Errors.Count)
                        from.SendMessage(33, "... and {0} more.", compiled.ErrorCount - compiled.Errors.Count);
                    return null;
                }
                string directives = books.Count == 1 ? compiled.Source : MusicCompiler.WithoutMarkers(PreProcesss(BookText(books[0])));
                lines = compiled.Text;

                // check for constructs not supported by current music karma
                if (CheckValidKarma(from, lines) == false)
                {
                    from.SendMessage("Error: You do not yet have sufficient CoreMusicPlayer Karma to play sound effects.");
                    return null;
                }

                // parse out title, author, instrument
                string title, author, instrument;
                int price;
                bool newTimer, prefetch, napster, concertmode;
                int tempo;
                ProcessDirectives(directives, out title, out author, out price, out instrument, out newTimer, out tempo, out prefetch, out napster, out concertmode);
                // # Song: and # Author: name the song; without them, the book's own title and author do
                if (!string.IsNullOrEmpty(title))
                    mcb.Title = title;
                if (!string.IsNullOrEmpty(author))
                    mcb.Author = author;

                if (mcb.Title == null || mcb.Title.Length == 0 || mcb.Title.ToLower() == "title")
                {
                    from.SendMessage("your music composition book must have a title.");
                    return null;
                }
                if (mcb.Author == null || mcb.Author.Length == 0 || mcb.Author.ToLower() == "author")
                {
                    from.SendMessage("your music composition book must have an author.");
                    return null;
                }
                if (price > 0 && napster == true)
                {
                    from.SendMessage("You cannot charge for Napster music.");
                    return null;
                }

                RolledUpSheetMusic rsm = new RolledUpSheetMusic(from, TonalQuality(instrument), mcb.Author, mcb.Title, price, lines);
                rsm.HashCode = CompileHashCode(lines, false);
                rsm.SongBytes = null;
                rsm.SongName = mcb.Title;
                rsm.Author = mcb.Author;
                rsm.ConfigureInstrument(instrument);
                rsm.NewTimer = newTimer;
                rsm.Tempo = tempo;
                rsm.Prefetch = prefetch;
                rsm.Napster = napster;
                rsm.ConcertMode = concertmode;
                return rsm;
            }

            private static string BookText(MusicCompositionBook book)
            {
                string text = string.Empty;
                for (int ix = 0; ix < book.PagesCount; ix++)
                    for (int mx = 0; mx < book.Pages[ix].Lines.Length; mx++)
                        text += string.Format("{0}", book.Pages[ix].Lines[mx]) + Environment.NewLine;
                return text;
            }

            // where each line PreProcesss makes is written ("page 3", or "book 2, page 14" for a song in several
            //  books), for the compiler's messages: a book line starts a new line unless the book wrapped it
            //  (CheckBookWrap); empty lines are dropped
            private List<string> LineWhere(List<MusicCompositionBook> books)
            {
                List<string> where = new List<string>();
                for (int bx = 0; bx < books.Count; bx++)
                    for (int ix = 0; ix < books[bx].PagesCount; ix++)
                        foreach (string text in books[bx].Pages[ix].Lines)
                        {
                            string line = RemoveLoop(text ?? string.Empty);
                            if (line.Trim().Length == 0)
                                continue;
                            if (where.Count == 0 || !CheckBookWrap(line))
                                where.Add(books.Count == 1 ? string.Format("page {0}", ix + 1) : string.Format("book {0}, page {1}", bx + 1, ix + 1));
                        }
                return where;
            }

            // # Book: 2/3 - this book's place in a song written in several books
            private static readonly Regex s_bookNumber = new Regex(@"^\s*#\s*book\s*:\s*(?<n>\S+?)\s*/\s*(?<of>\S+)\s*$", RegexOptions.IgnoreCase);
            private static readonly Regex s_songTitle = new Regex(@"^\s*#\s*song\s*:\s*(?<title>.*?)\s*$", RegexOptions.IgnoreCase);
            // MidiToAims.Core (Books.MaxBooks) keeps a copy of this: change both ([m2a refuses when they differ)
            public const int MaxBooks = 9;

            private static string? BookLine(MusicCompositionBook book, Regex regex, string group)
            {
                foreach (BookPageInfo page in book.Pages)
                    foreach (string line in page.Lines)
                    {
                        Match m = regex.Match(line ?? string.Empty);
                        if (m.Success)
                            return m.Groups[group].Value;
                    }
                return null;
            }

            private static string SongTitle(MusicCompositionBook book) => BookLine(book, s_songTitle, "title") ?? book.Title ?? string.Empty;

            // (n, of) from the book's # Book: line; (1, 1) without one; null if the line is not a book number
            private static (int N, int Of)? BookNumber(MusicCompositionBook book)
            {
                foreach (BookPageInfo page in book.Pages)
                    foreach (string line in page.Lines)
                    {
                        Match m = s_bookNumber.Match(line ?? string.Empty);
                        if (m.Success)
                            return int.TryParse(m.Groups["n"].Value, out int n) && int.TryParse(m.Groups["of"].Value, out int of) && n >= 1 && of >= 1 && n <= of && of <= MaxBooks
                                ? (n, of) : null;
                    }
                return (1, 1);
            }

            /// <summary>
            /// The books of this book's song, in order: this book alone, or, with # Book: n/of, all of the song's books
            /// (the same # Song:) from the backpack or bag this one is in. Null if they are not all there; the composer is
            /// told what is missing.
            /// </summary>
            private static List<MusicCompositionBook>? Books(Mobile from, MusicCompositionBook mcb)
            {
                (int N, int Of)? number = BookNumber(mcb);
                if (number == null)
                {
                    from.SendMessage(33, "'# Book:' needs the book's number and how many books the song has, such as '# Book: 2/3' (at most {0} books).", MaxBooks);
                    return null;
                }
                if (number.Value.Of == 1)
                    return new List<MusicCompositionBook> { mcb };

                string song = SongTitle(mcb);
                int of = number.Value.Of;
                if (mcb.Parent is not Container container)
                {
                    from.SendMessage(33, "'{0}' is in {1} books: keep them together in your backpack or a bag.", song, of);
                    return null;
                }

                MusicCompositionBook?[] books = new MusicCompositionBook?[of];
                foreach (MusicCompositionBook book in container.Items.OfType<MusicCompositionBook>())
                {
                    if (book.Deleted || !string.Equals(SongTitle(book), song, StringComparison.OrdinalIgnoreCase))
                        continue;
                    (int N, int Of)? n = BookNumber(book);
                    if (n == null || n.Value.Of == 1)
                        continue;       // not one of this song's books
                    if (n.Value.Of != of)
                    {
                        from.SendMessage(33, "'{0}': one book says '# Book: {1}/{2}' and another '# Book: {3}/{4}'.", song, number.Value.N, of, n.Value.N, n.Value.Of);
                        return null;
                    }
                    if (books[n.Value.N - 1] != null)
                    {
                        from.SendMessage(33, "'{0}': there are two copies of book {1}/{2} here. Keep only one.", song, n.Value.N, of);
                        return null;
                    }
                    books[n.Value.N - 1] = book;
                }

                List<string> missing = Enumerable.Range(1, of).Where(i => books[i - 1] == null).Select(i => i + "/" + of).ToList();
                if (missing.Count > 0)
                {
                    from.SendMessage(33, "'{0}': book{1} {2} {3} not here. Keep all {4} books together in the same backpack or bag.", song,
                        missing.Count == 1 ? "" : "s", string.Join(", ", missing), missing.Count == 1 ? "is" : "are", of);
                    return null;
                }
                return books.Select(b => b!).ToList();
            }

            private string NormalizeString(string lines)
            {
                return Regex.Replace(lines, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
            }
            private string RemoveLoop(string lines)
            {
                return Regex.Replace(lines, "#*loop", string.Empty, RegexOptions.IgnoreCase);
            }

            string PreProcesss(string lines)
            {
                // normalize input
                lines = NormalizeString(lines);

                // comment out 'loop' statements - not supported
                lines = RemoveLoop(lines);

                // remove all 'book induced wrap'
                return TrueLineParser(lines);
            }
            bool CheckValidKarma(Mobile m, string text)
            {
                string[] lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    if (line.ToLower().StartsWith("say '[play"))
                    {
                        // without the quotes, as the player reads the line (RazorInstrument.DoProcessPlay): a sound at the
                        //  end of the line ("+81'") is a sound
                        string[] chunks = line.Replace("'", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string chunk in chunks)
                            if (Commands.SystemMusicPlayer.IsSound(chunk) && !Commands.SystemMusicPlayer.IsPercussion(chunk) && MusicLibrary.GetMusicKarma(m) < 1000)
                                return false;
                    }
                }
                return true;
            }
            public int CompileHashCode(string lines, bool parse = true)
            {
                // when called externally, like from Nuke, we need to parse the lines so the format
                // matches that of the rsm created here.
                if (parse == true)
                    lines = PreProcesss(lines);

                // our hash is only the notes.
                //  we don't want someone stealing a composition book, changing the author to themselves, and claiming ownership.
                // If there is a legitimate update, it will (currently) require staff to delete the old track so the new track can be added. 
                // additionally, we 'munge' each line to make it difficult to submit a look alike tune by changing one note (or other element)
                string representation = string.Empty;
                foreach (var line in lines.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    if (line.StartsWith("#")) continue;     // throw away the header and other comments
                    representation += CompileLine(line);      // keep every 5th note (or other line element)
                }

                // normalize the string
                representation = representation.Replace(" ", "").ToLower().Trim();

                // okay, this is our hash
                return AimsUtil.GetStableHashCode(representation, version: 1);
            }
            private string CompileLine(string text)
            {   // munge string: The user will have had to change the mod 5th character(s) in order for the system
                //  to recognize this as a *different* song.
                //  I.e., stealing someone's song and changing one pause or note isn't likely to matter to this system.
                string output = string.Empty;
                int trip = 0;
                foreach (char ch in text)
                    if (trip++ == 0)
                    {   // changing the first character does not change the hash
                        continue;
                    }
                    else if (trip++ % 5 == 0)
                        //  magic character. If changed, hash changes
                        //  i.e., it's a new song
                        output += ch;
                    else
                    {
                        //  changing this character does not change the hash
                        continue;
                    }

                return AimsUtil.GetStableHashCode(output, version: 1).ToString();
            }
            private RazorInstrument.InstrumentSoundType TonalQuality(string instrument)
            {
                if (instrument == null)
                    return RazorInstrument.InstrumentSoundType.Harp;
                if (instrument.ToLower() == "harp")
                    return RazorInstrument.InstrumentSoundType.Harp;
                else if (instrument.ToLower() == "lapharp")
                    return RazorInstrument.InstrumentSoundType.LapHarp;
                else if (instrument.ToLower() == "lute")
                    return RazorInstrument.InstrumentSoundType.Lute;

                return RazorInstrument.InstrumentSoundType.Harp;
            }

            private void ProcessDirectives(string lines, out string title, out string author, out int price, out string instrument, out bool newTimer, out int tempo, out bool prefetch, out bool napster, out bool concertmode)
            {
                title = author = instrument = string.Empty;
                price = 0;
                newTimer = false;
                tempo = Commands.SystemMusicPlayer.DefaultTempo;
                prefetch = false;
                napster = false;
                concertmode = false;
                foreach (var line in lines.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.ToLower().Contains("author:"))
                    {
                        author = line;
                        author = author.Replace("#", "");
                        author = author.Replace("author:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                    }
                    else if (line.ToLower().Contains("napster:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("napster:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"true", RegexOptions.IgnoreCase).Value;
                        if (bool.TryParse(resultString, out napster) == false)
                            napster = false;
                    }
                    else if (line.ToLower().Contains("prefetch:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("prefetch:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"true", RegexOptions.IgnoreCase).Value;
                        if (bool.TryParse(resultString, out prefetch) == false)
                            prefetch = false;
                    }
                    else if (line.ToLower().Contains("concertmode:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("concertmode:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"true", RegexOptions.IgnoreCase).Value;
                        if (bool.TryParse(resultString, out concertmode) == false)
                            concertmode = false;
                    }
                    else if (line.ToLower().Contains("newtimer:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("newtimer:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"true", RegexOptions.IgnoreCase).Value;
                        if (bool.TryParse(resultString, out newTimer) == false)
                            newTimer = false;
                    }
                    else if (line.ToLower().Contains("tempo:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("tempo:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"\d+").Value;
                        if (int.TryParse(resultString, out tempo) == false)
                            tempo = Commands.SystemMusicPlayer.DefaultTempo;
                    }
                    else if (line.ToLower().Contains("instrument:"))
                    {
                        instrument = line;
                        instrument = instrument.Replace("#", "");
                        instrument = instrument.Replace("instrument:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                    }
                    else if (line.ToLower().Contains("price:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("place:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"\d+").Value;
                        if (int.TryParse(resultString, out price) == false)
                            price = 0;
                    }
                    else if (line.ToLower().Contains("song:"))
                    {
                        title = line;
                        title = title.Replace("#", "");
                        title = title.Replace("song:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                    }
                }
            }
            private string TrueLineParser(string text)
            {
                string trueLines = string.Empty;
                for (int ndx = 0; ndx < text.Length; ndx++)
                {
                    if (text.Substring(ndx).Length > 2 && text[ndx] == '\r' && text[ndx + 1] == '\n')
                        if (CheckBookWrap(text.Substring(ndx + 2)))
                        {   // still processing the same command, eat the newline
                            ndx += 1;
                            continue;
                        }

                    // the command changed, so we can just output as is.
                    //  until of course, we detect more 'book induced wrap'
                    // output this character
                    trueLines += text[ndx];
                }
                return trueLines;
            }

            private bool CheckBookWrap(string text)
            {
                text = text.ToLower();
                if (text.StartsWith("#"))
                    return false;
                if (text.StartsWith("wait "))
                    return false;
                if (text.StartsWith("say "))
                    return false;

                return true;
            }
        }

#if ANGEL_ISLAND
        public override Item Dupe(int amount)
        {
            return base.Dupe(new ManuscriptPaper(amount), amount);
        }
#endif

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}