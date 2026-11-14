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


/* Scripts\Engines\AIMusicSystem\Compiler\MusicCompiler.cs
 * Changelog
 *  10/30/26, Adam (2026 modernization)
 *      Initial creation. The music compiler: a composition book's sections, plays and repeats (gosub) are expanded
 *      into the plain script the player already plays (say '[play ...', wait, # comments) before an RSM is made or a
 *      book is previewed. The markers are comment lines, so Razor and [play ignore them, and a book without them
 *      compiles to itself. The expanded script is limited to 2,000 lines and 64 KB; errors say where each line is
 *      written ("book 2, page 14").
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Items
{
    /// <summary>
    /// The music compiler. A composition is a script of say '[play ...' lines, wait lines and # comments; these comment
    /// lines are its structure (keywords ignore case):
    ///  # Section: Name          a passage that plays where it is written and can be played again later
    ///  # Silent Section: Name   a passage that plays only where it is played (# Play)
    ///  # End  (or # End: Name)  closes the section
    ///  # Play: Name  [x3]       plays a section again here (three times)
    ///  # Repeat from here       the start of a repeat
    ///  # Repeat x3              plays from the start of the repeat to here, three times in all
    /// A section is played only after it is closed (so nothing can play itself), sections and repeats nest, and every
    /// other # line is a comment. A book that uses none of the openers (Section, Silent Section, Play, Repeat) is a
    /// plain script, and compiles to itself, unchanged.
    /// </summary>
    public static class MusicCompiler
    {
        public const int MaxDepth = 8;              // sections and repeats inside one another, and plays within plays
        public const int MaxCount = 99;             // x99
        public const int MaxLines = 2000;           // the expanded script: the longest songs are about 250 lines
        public const int MaxBytes = 64 * 1024;      //  and about 17 KB (a full 100-page book); every sheet keeps (and saves) its script
        public const int MaxErrors = 5;             // reported; the rest are counted

        public sealed class Result
        {
            public bool Success => Errors.Count == 0;
            public bool Structured { get; internal set; }       // the book has sections, plays or repeats
            public string Text { get; internal set; } = "";     // the expanded script (the book's own text if not Structured)
            public string Source { get; internal set; } = "";   // the book without its markers (for the # Song:, # Author: ... directives)
            public List<string> Errors { get; } = new();
            public int ErrorCount { get; internal set; }        // all of them (Errors keeps the first MaxErrors)
        }

        #region Markers
        private enum Kind { Text, Section, SilentSection, End, Play, RepeatFrom, Repeat }

        private sealed class Line
        {
            public Kind Kind;
            public string Raw = "";
            public string Name = "";            // Section, SilentSection, Play; End: the name it closes (optional)
            public int Count = 1;               // Play, Repeat
            public string? CountError;          // a count we could not use
            public int Number;                  // 1-based, counting the script's non-empty lines
        }

        private static readonly Regex s_section = new(@"^section\s*:\s*(?<name>.*)$", RegexOptions.IgnoreCase);
        private static readonly Regex s_silent = new(@"^silent\s+section\s*:\s*(?<name>.*)$", RegexOptions.IgnoreCase);
        private static readonly Regex s_end = new(@"^end\s*(:\s*(?<name>.*))?$", RegexOptions.IgnoreCase);
        private static readonly Regex s_play = new(@"^play\s*:\s*(?<name>.*?)(\s+x\s*(?<count>\S+))?$", RegexOptions.IgnoreCase);
        private static readonly Regex s_repeatFrom = new(@"^repeat\s+from\s+here$", RegexOptions.IgnoreCase);
        private static readonly Regex s_repeat = new(@"^repeat\s+x\s*(?<count>\S+)$", RegexOptions.IgnoreCase);
        // a marker the book joined onto the line above it (it did not start its own line)
        private static readonly Regex s_embedded = new(@"#\s*(silent\s+section\s*:|section\s*:|play\s*:|repeat\s+from\s+here|repeat\s+x\s*\d|end\s*:)", RegexOptions.IgnoreCase);

        private static Line Classify(string raw, int number)
        {
            Line line = new Line { Kind = Kind.Text, Raw = raw, Number = number };
            string text = raw.Trim();
            if (!text.StartsWith("#"))
                return line;

            string body = text.TrimStart('#').Trim();
            Match m;
            if ((m = s_silent.Match(body)).Success)
                Set(line, Kind.SilentSection, m);
            else if ((m = s_section.Match(body)).Success)
                Set(line, Kind.Section, m);
            else if ((m = s_play.Match(body)).Success)
                Set(line, Kind.Play, m);
            else if (s_repeatFrom.IsMatch(body))
                line.Kind = Kind.RepeatFrom;
            else if ((m = s_repeat.Match(body)).Success)
                Set(line, Kind.Repeat, m);
            else if ((m = s_end.Match(body)).Success)
                Set(line, Kind.End, m);
            return line;
        }

        private static void Set(Line line, Kind kind, Match m)
        {
            line.Kind = kind;
            line.Name = m.Groups["name"].Success ? m.Groups["name"].Value.Trim() : "";
            Group count = m.Groups["count"];
            if (count.Success)
            {
                if (!int.TryParse(count.Value, out int n) || n < 1 || n > MaxCount)
                    line.CountError = string.Format("\"x{0}\" is not a count from 1 to {1}", count.Value, MaxCount);
                else
                    line.Count = n;
            }
        }
        #endregion Markers

        #region Structure
        private sealed class Block
        {
            public Kind Kind;                   // Section, SilentSection, RepeatFrom (the root: Text)
            public string Name = "";
            public Line? Opener;
            public int Count = 1;
            public readonly List<object> Body = new();     // Line (text), Play or Block
        }

        private sealed class Play
        {
            public Line Line = null!;
            public Block? Target;
        }
        #endregion Structure

        /// <summary>
        /// Compiles a composition (the book's text after ManuscriptPaper's preprocessing). linePages, when given, is
        /// the book page of each non-empty line, for the error messages; without it they give the line number.
        /// </summary>
        public static Result Compile(string text, IReadOnlyList<int>? linePages = null)
        {
            return Compile(text, linePages?.Select(p => string.Format("page {0}", p)).ToList());
        }

        /// <summary>
        /// Compiles a composition; lineWhere, when given, says where each non-empty line is written, for the error
        /// messages ("page 3", "book 2, page 14").
        /// </summary>
        public static Result Compile(string text, IReadOnlyList<string>? lineWhere)
        {
            text ??= "";
            Result result = new Result { Text = text, Source = text };

            List<string> raw = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
            List<Line> lines = new();
            int number = 0;
            foreach (string r in raw)
                lines.Add(Classify(r, r.Trim().Length == 0 ? number : ++number));
            if (lineWhere != null && lineWhere.Count != number)
                lineWhere = null;               // the book and the script disagree: line numbers instead

            string Where(Line l) => lineWhere != null && l.Number >= 1 ? lineWhere[l.Number - 1] : string.Format("line {0}", l.Number);
            void Error(Line? l, string message)
            {
                result.ErrorCount++;
                if (result.Errors.Count < MaxErrors)
                    result.Errors.Add(l == null ? message : string.Format("{0}: '{1}' - {2}", Where(l), l.Raw.Trim(), message));
            }

            // a marker the book joined onto the line above it is lost in that line (and would play as notes)
            foreach (Line l in lines.Where(l => l.Kind == Kind.Text && !l.Raw.TrimStart().StartsWith("#") && s_embedded.IsMatch(l.Raw)))
                Error(l, "a section marker must start its own line");

            // a plain script compiles to itself (an old book's "# End" is a comment)
            result.Structured = lines.Any(l => l.Kind is Kind.Section or Kind.SilentSection or Kind.Play or Kind.RepeatFrom or Kind.Repeat);
            if (!result.Structured)
                return result;

            // parse: sections, repeats and plays, nested
            Dictionary<string, Block> closed = new(StringComparer.OrdinalIgnoreCase);     // sections that can be played
            Dictionary<string, Line> named = new(StringComparer.OrdinalIgnoreCase);       // every section name, where it opens
            List<Play> unresolved = new();
            Block root = new Block { Kind = Kind.Text };
            Stack<Block> open = new();
            open.Push(root);

            foreach (Line l in lines)
            {
                if (l.CountError != null)
                    Error(l, l.CountError);

                switch (l.Kind)
                {
                    case Kind.Text:
                        if (l.Raw.Trim().Length > 0)
                            open.Peek().Body.Add(l);
                        break;

                    case Kind.Section:
                    case Kind.SilentSection:
                    case Kind.RepeatFrom:
                        if (l.Kind != Kind.RepeatFrom)
                        {
                            if (l.Name.Length == 0)
                                Error(l, "the section needs a name");
                            else if (named.TryGetValue(l.Name, out Line? first))
                                Error(l, string.Format("there is already a section named \"{0}\" ({1})", l.Name, Where(first)));
                            else
                                named[l.Name] = l;
                        }
                        if (open.Count > MaxDepth)
                            Error(l, string.Format("sections and repeats nest at most {0} deep", MaxDepth));
                        Block block = new Block { Kind = l.Kind, Name = l.Name, Opener = l };
                        open.Peek().Body.Add(block);
                        open.Push(block);
                        break;

                    case Kind.End:
                        {
                            Block top = open.Peek();
                            if (top == root)
                                Error(l, "there is no section to end");
                            else if (top.Kind == Kind.RepeatFrom)
                                Error(l, string.Format("the repeat from {0} ends with '# Repeat x2' (or another count), not # End", Where(top.Opener!)));
                            else if (l.Name.Length > 0 && !string.Equals(l.Name, top.Name, StringComparison.OrdinalIgnoreCase))
                                Error(l, string.Format("the open section is \"{0}\" ({1})", top.Name, Where(top.Opener!)));
                            else
                            {
                                open.Pop();
                                if (top.Name.Length > 0 && !closed.ContainsKey(top.Name))
                                    closed[top.Name] = top;
                            }
                        }
                        break;

                    case Kind.Repeat:
                        {
                            Block top = open.Peek();
                            if (top.Kind != Kind.RepeatFrom)
                                Error(l, top == root
                                    ? "there is no '# Repeat from here' before it"
                                    : string.Format("the section \"{0}\" ({1}) must end before the repeat does", top.Name, Where(top.Opener!)));
                            else
                            {
                                top.Count = l.Count;
                                open.Pop();
                            }
                        }
                        break;

                    case Kind.Play:
                        {
                            Play play = new Play { Line = l };
                            if (l.Name.Length == 0)
                                Error(l, "which section? (# Play: Name)");
                            else if (closed.TryGetValue(l.Name, out Block? target))
                                play.Target = target;
                            else
                                unresolved.Add(play);
                            open.Peek().Body.Add(play);
                        }
                        break;
                }
            }

            // what is still open at the end of the book
            while (open.Count > 1)
            {
                Block b = open.Pop();
                Error(b.Opener, b.Kind == Kind.RepeatFrom ? "the repeat never ends (# Repeat x2)" : "the section never ends (# End)");
            }

            // plays of sections that are not (yet) closed where they are played
            foreach (Play play in unresolved)
            {
                Line l = play.Line;
                if (named.TryGetValue(l.Name, out Line? where))
                    Error(l, where.Number > l.Number
                        ? string.Format("\"{0}\" is written later ({1}); write a section before you play it", l.Name, Where(where))
                        : string.Format("this is inside \"{0}\", which cannot play itself", l.Name));
                else
                    Error(l, string.Format("no section named \"{0}\"{1}", l.Name, DidYouMean(l.Name, named.Keys)));
            }

            if (!result.Success)
                return result;

            // expand
            List<string> output = new();
            HashSet<Block> playing = new();
            bool stopped = false;
            long bytes = 0;

            void Emit(Block block, int depth)
            {
                foreach (object item in block.Body)
                {
                    if (stopped)
                        return;
                    switch (item)
                    {
                        case Line l:
                            bytes += l.Raw.Length + 2;
                            if (output.Count >= MaxLines || bytes > MaxBytes)
                            {
                                Error(null, output.Count >= MaxLines
                                    ? string.Format("the song is too long: it expands to more than {0} lines (the longest songs are about 250)", MaxLines)
                                    : string.Format("the song is too long: it expands to more than {0} KB (the longest songs are about 17 KB)", MaxBytes / 1024));
                                stopped = true;
                                return;
                            }
                            output.Add(l.Raw);
                            break;

                        case Block b:
                            if (b.Kind == Kind.SilentSection)
                                break;                                      // it plays only where it is played
                            for (int i = 0; i < b.Count && !stopped; i++)
                                Emit(b, depth);
                            break;

                        case Play p:
                            if (depth >= MaxDepth)
                            {
                                Error(p.Line, string.Format("sections play other sections at most {0} deep", MaxDepth));
                                stopped = true;
                                return;
                            }
                            if (!playing.Add(p.Target!))
                            {   // define-before-use makes this impossible; checked anyway
                                Error(p.Line, string.Format("\"{0}\" plays itself", p.Target!.Name));
                                stopped = true;
                                return;
                            }
                            for (int i = 0; i < p.Line.Count && !stopped; i++)
                                Emit(p.Target!, depth + 1);
                            playing.Remove(p.Target!);
                            break;
                    }
                }
            }
            Emit(root, 0);

            if (!result.Success)
                return result;

            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            result.Text = string.Join(newline, output) + (text.EndsWith("\n") ? newline : "");
            result.Source = string.Join(newline, lines.Where(l => l.Kind == Kind.Text).Select(l => l.Raw));
            return result;
        }

        // the text less its section markers (what the # Song:, # Author: ... directives are read from)
        public static string WithoutMarkers(string text)
        {
            text ??= "";
            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            return string.Join(newline, text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Where(l => Classify(l, 0).Kind == Kind.Text));
        }

        #region Did you mean
        private static string DidYouMean(string name, IEnumerable<string> names)
        {
            string? best = null;
            int bestDistance = int.MaxValue;
            foreach (string candidate in names)
            {
                int d = Distance(name.ToLowerInvariant(), candidate.ToLowerInvariant());
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = candidate;
                }
            }
            return best != null && bestDistance <= Math.Max(1, best.Length / 3) ? string.Format(" (did you mean \"{0}\"?)", best) : "";
        }

        private static int Distance(string a, string b)
        {   // Levenshtein
            int[] previous = Enumerable.Range(0, b.Length + 1).ToArray();
            for (int i = 1; i <= a.Length; i++)
            {
                int[] current = new int[b.Length + 1];
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                previous = current;
            }
            return previous[b.Length];
        }
        #endregion Did you mean
    }
}
