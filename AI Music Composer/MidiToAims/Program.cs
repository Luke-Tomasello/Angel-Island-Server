/***************************************************************************
 *
 *  MidiToAims - a MIDI file to an AIMS composition (.rzr): the command line (the work is MidiToAims.Core).
 *
 *      MidiToAims init <song.mid>                     write <song>.m2a beside the MIDI: the arrangement settings
 *      MidiToAims <song.mid> [<out.rzr>] [options]    convert, using <song>.m2a when it exists
 *          --notes N   --bars N   --pages N   --song "..."   --composer "..."   --arranger "..."   --format post|whole
 *          (override the .m2a)
 *      MidiToAims preview <song.mid> [options]        convert, then play it as the game will (Player)
 *      MidiToAims preview <song.rzr>                  play a composition: a .rzr, or a song's pieces (name any one)
 *          (also play preview <song>, as [play preview in the game)
 *          <song> may be words from a file's name: preview too sweet, preview minuet.mid (Find)
 *          --from <time>   --to <time>   play part of the song (1:10, 1:10.5, or 70 seconds)
 *          --clip          with --from / --to: write that part, on whole blocks, as <song>.clipN.rzr (Player.Clip);
 *                          a song's clips are found only when named: preview golden clip1
 *          --uo <folder>   where the Ultima Online client's sounds are (remembered; normally found by itself)
 *          --check         report what the game will do (each line's skipped notes ...), without playing
 *      MidiToAims play <words>                       a [play line, as typed in the game: c e g 0.5 +81 -c; a setting
 *                                                     (instrument lute, tempo 150, timer anchored, newtimer true,
 *                                                     prefetch true) is remembered for the next; config; reset
 *          --uo <folder>   --check   as above
 *      A leading '[' is ignored, so the game's own commands work as typed: MidiToAims [play c e g, [play config,
 *      [play preview <song>.
 *      MidiToAims                                     the m2a> prompt: type the commands above without "MidiToAims"
 *                                                     (help, exit); the console's history replays and edits a line
 *      MidiToAims sound <81 | 300-330 | wood | percussion> ...   hear the client's sound effects (+81 in a song), with
 *                                                     their names and whether they are free of music karma
 *          --list          show them without playing   --uo <folder>   as above
 *      MidiToAims waits <song>                        set each wait in the song's .rzr files to how long its block plays
 *                                                     (preview reports the ones that don't match; Player.CheckWaits)
 *      --debug on any command (or MidiToAims --debug, or debug on|off at the prompt): the details too - the files found
 *      and played, each line's skipped notes, notes cut short; else one line for the skips. The options also take one
 *      dash (-from, -to, -clip, -debug, -check, -uo, -list).
 *
 *  Format (the .m2a, or --format): post (the default) writes a .rzr per post with a book's Paste (right-click the book,
 *  Paste, Post Message: at most 250 lines and 18,000 characters, Books.Posts): <song>.rzr when the song is one post,
 *  else <song>.post1.rzr ... (one book), <song>.book1.rzr ... (several books, one post each) or <song>.book1.post1.rzr
 *  ... (each book with its # Song: / # Book: n/m header). whole writes the song as one <song>.rzr, its books one after
 *  another. Files of an earlier conversion (also the .pasteN pieces made before 11/3/26) are noted, not deleted.
 *
 *  For players: publish a single self-contained MidiToAims.exe (no .NET install needed), Core inside it:
 *      dotnet publish MidiToAims.csproj -p:PublishProfile=Player        (bin\Player\MidiToAims.exe)
 *  No shard dependencies (the music compiler is the shard's source, linked: Player).
 *
 ***************************************************************************/

using System.Text.RegularExpressions;

namespace MidiToAims
{
    public static class Program
    {
        // --debug (MidiToAims --debug, the m2a> prompt's debug on, or one command's --debug): the details too (the files
        //  found and played, each line's skipped notes, notes cut short); else a line for what matters
        public static bool Debug;

        public static int Main(string[] args)
        {
            args = Options(args);
            if (args.Contains("--debug"))
            {
                Debug = true;
                args = args.Where(a => a != "--debug").ToArray();
            }
            return args.Length == 0 ? Prompt() : Dispatch(args);
        }

        // the options that may be typed with one dash (-from 1:10, -debug): none of them is a [play word
        private static readonly string[] s_options = { "debug", "from", "to", "check", "uo", "list", "clip" };
        private static string[] Options(string[] args) =>
            args.Select(a => a.Length > 1 && a[0] == '-' && a[1] != '-' && s_options.Contains(a.Substring(1).ToLowerInvariant()) ? "-" + a.ToLowerInvariant() : a).ToArray();

        // the m2a> prompt (MidiToAims run without arguments, e.g. double-clicked): each line is a command as above,
        //  without "MidiToAims"; the console keeps the history (up and down arrows, F7). A line plays to its end
        //  (Ctrl+C stops it) before the prompt returns.
        private static int Prompt()
        {
            Console.WriteLine("MidiToAims: type [play c e g to hear a phrase, [play preview \"song.rzr\" to hear a song, help, or exit.");
            while (true)
            {
                Console.Write("m2a> ");
                string? line = Console.ReadLine();
                if (line == null)
                    return 0;
                string[] words = Words(line.TrimStart('\uFEFF'));        // a BOM, when piped in
                if (words.Length > 0 && Regex.IsMatch(words[0], @"^MidiToAims(\.exe)?$", RegexOptions.IgnoreCase))
                    words = words.Skip(1).ToArray();                    // "MidiToAims [play ..." typed out of habit
                if (words.Length == 0)
                    continue;
                string first = words[0].ToLowerInvariant();
                if (first is "exit" or "quit")
                    return 0;
                if (first is "help" or "?")
                {
                    Usage();
                    continue;
                }
                if (first == "debug")
                {
                    string arg = words.Length > 1 ? words[1].ToLowerInvariant() : "";
                    Debug = arg == "on" || (arg != "off" && !Debug);
                    Console.WriteLine(Debug ? "Debug on: the details too (files, each line's skipped notes ...). debug off turns it off."
                        : "Debug off.");
                    continue;
                }
                Dispatch(words);
            }
        }

        // a typed line's words: split at spaces, "quoted words" kept together (without the quotes)
        private static string[] Words(string line) =>
            Regex.Matches(line, "\"([^\"]*)\"?|(\\S+)").Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).ToArray();

        private static void Usage()
        {
            Console.WriteLine("Usage: MidiToAims init <song.mid> | MidiToAims <song.mid> [<out.rzr>] [--notes N] [--bars N] [--pages N] [--song \"...\"] [--composer \"...\"] [--arranger \"...\"] [--format post|whole]");
            Console.WriteLine("       MidiToAims preview <song.mid|song.rzr> [the same options] [--from m:ss] [--to m:ss] [--clip] [--uo <client folder>] [--check]");
            Console.WriteLine("       MidiToAims [play <as in the game: c e g 0.5 +81 | tempo 150 | instrument lute | timer anchored | config | reset> [--uo <client folder>] [--check]");
            Console.WriteLine("       MidiToAims sound <number | from-to | part of a name | percussion> ... [--list] [--uo <client folder>]");
            Console.WriteLine("       MidiToAims waits <song>: set each wait in the song's .rzr to how long its block plays");
            Console.WriteLine("       MidiToAims (nothing): the m2a> prompt, where you type these without \"MidiToAims\"");
            Console.WriteLine("       --debug (on any command, or debug on|off at the prompt): the details too");
        }

        private static int Dispatch(string[] args)
        {
            bool debug = Debug;
            try
            {
                // this command's --debug
                args = Options(args);
                if (args.Contains("--debug"))
                {
                    Debug = true;
                    args = args.Where(a => a != "--debug").ToArray();
                }

                // [play ... as typed in the game: the commands are the same with or without the '['
                if (args.Length > 0 && args[0].StartsWith("["))
                    args = args[0] == "[" ? args.Skip(1).ToArray() : args.Select((a, i) => i == 0 ? a.Substring(1) : a).ToArray();

                if (args.Length >= 2 && args[0].Equals("init", StringComparison.OrdinalIgnoreCase))
                {
                    string? midi = Find(string.Join(" ", args.Skip(1)), ".mid");
                    if (midi == null)
                        return 1;
                    string m2a = Path.ChangeExtension(midi, ".m2a");
                    if (File.Exists(m2a))
                    {
                        Console.WriteLine("{0} already exists; not replaced.", m2a);
                        return 1;
                    }
                    File.WriteAllText(m2a, Converter.Init(midi));
                    Console.WriteLine("Wrote {0}.", m2a);
                    return 0;
                }
                // preview <song>, or play preview <song> (as [play preview in the game)
                int previewAt = args.Length >= 1 && args[0].Equals("preview", StringComparison.OrdinalIgnoreCase) ? 1
                    : args.Length >= 2 && args[0].Equals("play", StringComparison.OrdinalIgnoreCase) && args[1].Equals("preview", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                if (previewAt > 0)
                {
                    string[] rest = args.Skip(previewAt).ToArray();
                    if (rest.Length == 0 || rest[0].StartsWith("--"))
                    {
                        Console.WriteLine("Usage: MidiToAims preview <song.mid|song.rzr> [the conversion options] [--uo <client folder>] [--check]");
                        return 1;
                    }
                    int uoAt = Array.IndexOf(rest, "--uo");
                    string? uo = uoAt >= 0 && uoAt + 1 < rest.Length ? rest[uoAt + 1] : null;

                    // --from 1:10 --to 1:40: the part to play
                    double? from = null, to = null;
                    foreach (string option in new[] { "--from", "--to" })
                    {
                        int i = Array.IndexOf(rest, option);
                        if (i < 0)
                            continue;
                        double? time = i + 1 < rest.Length ? Player.ParseTime(rest[i + 1]) : null;
                        if (time == null)
                        {
                            Console.WriteLine("{0} takes a time in the song: 1:10, 1:10.5 or 70 (seconds).", option);
                            return 1;
                        }
                        if (option == "--from") from = time; else to = time;
                    }

                    // the song: the words before the options (preview too sweet), a file or part of a song's name
                    int optionsAt = Array.FindIndex(rest, a => a.StartsWith("--"));
                    if (optionsAt < 0)
                        optionsAt = rest.Length;
                    string? song = Find(string.Join(" ", rest.Take(optionsAt)), ".rzr", ".mid");
                    if (song == null)
                        return 1;
                    List<string>? files = song.EndsWith(".rzr", StringComparison.OrdinalIgnoreCase) ? Pieces(song)
                        : Convert(new[] { song }.Concat(rest.Skip(optionsAt)).ToArray());
                    if (files == null || files.Count == 0)
                        return 1;
                    // --clip: the part as a song of its own (<song>.clipN.rzr), written instead of played
                    if (rest.Contains("--clip"))
                    {
                        if (from == null && to == null)
                        {
                            Console.WriteLine("--clip takes the part to clip: --from 1:10 --to 1:40 (either one alone: to the end, from the start).");
                            return 1;
                        }
                        return Player.Clip(files, from, to);
                    }
                    if (Debug || !song.EndsWith(".rzr", StringComparison.OrdinalIgnoreCase))
                        Console.WriteLine();
                    return Player.Run(files, uo, rest.Contains("--check"), from, to);
                }

                // play <the words of a [play line>
                if (args.Length >= 1 && args[0].Equals("play", StringComparison.OrdinalIgnoreCase))
                {
                    int uoAt = Array.IndexOf(args, "--uo");
                    string? uo = uoAt >= 0 && uoAt + 1 < args.Length ? args[uoAt + 1] : null;
                    string[] words = args.Where((a, i) => i > 0 && !a.StartsWith("--") && (uoAt < 0 || i != uoAt + 1)).ToArray();
                    if (words.Length == 0)
                    {
                        Console.WriteLine("Usage: MidiToAims [play <notes and pauses> | [play tempo 150 | [play config | [play reset | [play preview <song>  (as in the game; the '[' is optional)");
                        return 1;
                    }
                    return Player.Line(words, uo, args.Contains("--check"));
                }
                // waits <song>: each wait set to how long its block plays
                if (args.Length >= 1 && args[0].Equals("waits", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length == 1)
                    {
                        Console.WriteLine("Usage: MidiToAims waits <song>  (sets each wait in the song's .rzr to how long its block plays)");
                        return 1;
                    }
                    string? rzr = Find(string.Join(" ", args.Skip(1)), ".rzr");
                    List<string>? pieces = rzr != null ? Pieces(rzr) : null;
                    return pieces != null ? Player.FixWaits(pieces) : 1;
                }
                if (args.Length >= 2 && args[0].Equals("sound", StringComparison.OrdinalIgnoreCase))
                {
                    int uoAt = Array.IndexOf(args, "--uo");
                    string? uo = uoAt >= 0 && uoAt + 1 < args.Length ? args[uoAt + 1] : null;
                    string[] specs = args.Where((a, i) => i > 0 && !a.StartsWith("--") && (uoAt < 0 || i != uoAt + 1)).ToArray();
                    if (specs.Length > 0)
                        return Player.Sounds(specs, uo, args.Contains("--list"));
                }
                if (args.Length < 1 || args[0].StartsWith("--"))
                {
                    Usage();
                    return 1;
                }
                // convert: <song.mid> [<out.rzr>] [options], or words from a song's name (too sweet) before the options
                if (File.Exists(args[0]))
                    return Convert(args) != null ? 0 : 1;
                int at = Array.FindIndex(args, a => a.StartsWith("--"));
                if (at < 0)
                    at = args.Length;
                string? found = Find(string.Join(" ", args.Take(at)), ".mid");
                return found != null && Convert(new[] { found }.Concat(args.Skip(at)).ToArray()) != null ? 0 : 1;
            }
            catch (Exception e)
            {
                Console.WriteLine("MidiToAims: " + e.Message);
                return 1;
            }
            finally
            {
                Debug = debug;
            }
        }

        // a song named in a command: the file itself, or one whose name holds every word of spec (golden, "too sweet").
        //  It looks where spec says (songs\golden), else in the current folder and MidiToAims's own. A song's pieces
        //  (.bookN, .postM) are one song; one whose name is exactly spec wins over the others. extensions: the kinds
        //  wanted, preferred first (.rzr before .mid: the .rzr is what gets pasted, and converting the .mid again would
        //  replace it, edits and all); spec ending in one of them asks for that kind (golden.mid converts again).
        //  Null (and why, said) when there is none, or several.
        private static string? Find(string spec, params string[] extensions)
        {
            spec = spec.Trim();
            if (File.Exists(spec))
                return spec;
            foreach (string ext in extensions)
                if (File.Exists(spec + ext))
                    return spec + ext;

            string? dir = Path.GetDirectoryName(spec), name = Path.GetFileName(spec);
            string asked = Path.GetExtension(name).ToLowerInvariant();
            if (extensions.Contains(asked))
            {
                extensions = new[] { asked };
                name = Path.GetFileNameWithoutExtension(name);
            }
            string[] words = Regex.Split(name.ToLowerInvariant(), @"[^\p{L}\p{N}]+").Where(w => w.Length > 0).ToArray();
            IEnumerable<string> folders = !string.IsNullOrEmpty(dir) ? new[] { dir }
                : new[] { Environment.CurrentDirectory, AppContext.BaseDirectory }.Select(f => Path.GetFullPath(f).TrimEnd('\\', '/')).Distinct(StringComparer.OrdinalIgnoreCase);
            Regex piece = new(@"(\.book\d+)?(\.(?:paste|post)\d+)?$", RegexOptions.IgnoreCase);

            // the songs that match: their files, by song (folder + name without the piece suffixes)
            var songs = folders.Where(Directory.Exists)
                .SelectMany(f => Directory.GetFiles(f))
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => (File: f, Song: piece.Replace(Path.GetFileNameWithoutExtension(f), "")))
                .Where(x => words.Length > 0 && words.All(w => x.Song.Contains(w, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(x => Path.Combine(Path.GetDirectoryName(x.File)!, x.Song), StringComparer.OrdinalIgnoreCase)
                .ToList();

            // a song's clips (<song>.clipN) only when asked for (golden clip1): golden is the song
            bool clips = words.Any(w => Regex.IsMatch(w, @"^clip\d*$"));
            songs = songs.Where(g => Regex.IsMatch(g.First().Song, @"\.clip\d+$", RegexOptions.IgnoreCase) == clips).ToList();
            var exact = songs.Where(g => g.First().Song.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (exact.Count == 1)
                songs = exact;
            string kinds = string.Join(" or ", extensions);
            if (songs.Count == 0)
            {
                Console.WriteLine("No {0} file matches \"{1}\" in {2}.", kinds, spec, string.Join(" or ", folders));
                return null;
            }
            if (songs.Count > 1)
            {
                Console.WriteLine("\"{0}\" matches {1} songs; type more of the name:", spec, songs.Count);
                foreach (var g in songs.Take(10))
                    Console.WriteLine("  " + g.First().Song);
                if (songs.Count > 10)
                    Console.WriteLine("  ... and {0} more.", songs.Count - 10);
                return null;
            }

            // the song's newest file of the preferred kind (an older conversion's pieces may be beside it)
            var files = songs[0].Select(x => x.File).ToList();
            string? file = extensions.Select(ext => files.Where(f => Path.GetExtension(f).Equals(ext, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()).FirstOrDefault(f => f != null);
            if (file == null)
                return null;
            if (Debug)
                Console.WriteLine("Found {0}.", Path.GetFileName(file));
            if (Path.GetExtension(file).Equals(".rzr", StringComparison.OrdinalIgnoreCase))
            {   // the arrangement changed since: say how to hear it (converting again replaces the .rzr)
                string midi = Path.Combine(Path.GetDirectoryName(file)!, songs[0].First().Song + ".mid");
                DateTime made = File.GetLastWriteTimeUtc(file);
                if (File.Exists(midi) && (File.GetLastWriteTimeUtc(midi) > made || File.GetLastWriteTimeUtc(Path.ChangeExtension(midi, ".m2a")) > made))
                    Console.WriteLine("Note: {0} or its .m2a is newer than this .rzr. To convert it again (replacing the .rzr, and any edits in it), name the .mid: {1}.mid",
                        Path.GetFileName(midi), name);
            }
            return file;
        }

        // converts a MIDI (args: <song.mid> [<out.rzr>] [options]); the files written, in paste order (null: none)
        private static List<string>? Convert(string[] args)
        {
            string midi = args[0];
            string output = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : Path.ChangeExtension(midi, ".rzr");
            string m2aPath = Path.ChangeExtension(midi, ".m2a");
            string m2aText = File.Exists(m2aPath) ? File.ReadAllText(m2aPath) : "";

            // command-line overrides: later settings win
            foreach (var (option, key) in new[] { ("--notes", "Notes at once"), ("--bars", "Bars per block"), ("--pages", "Book pages"), ("--song", "Song"), ("--composer", "Composer"), ("--arranger", "Arranged by"), ("--format", "Format") })
            {
                int i = Array.IndexOf(args, option);
                if (i >= 0 && i + 1 < args.Length)
                    m2aText += Environment.NewLine + key + ": " + args[i + 1];
            }

            // { .rzr, report, book 1, book 2, ... }: no books when there is nothing to play or it is too long
            string[] result = Converter.Convert(midi, m2aText);
            Console.Write(result[1]);
            string[] books = result.Skip(2).ToArray();
            if (books.Length == 0)
                return null;
            string dir = Path.GetDirectoryName(Path.GetFullPath(output))!, name = Path.GetFileNameWithoutExtension(output);
            List<string> written = new();

            // Format: whole: the song in one file, its books one after another (each with its # Book: n/m header)
            if (Converter.Arrangement.Parse(m2aText).Format == "whole")
            {
                bool postable = books.Length == 1 && Books.Posts(books[0]).Length == 1;
                File.WriteAllText(output, books.Length == 1 ? result[0] : string.Concat(books));
                written.Add(Path.GetFullPath(output));
                Console.WriteLine("Wrote {0}: {1}.", output, postable ? "post it into a music composition book (right-click the book, Paste)"
                    : string.Format("the whole song in one file ({0}), too big for one post: Format: post (the default) makes the pieces to post",
                        books.Length == 1 ? "1 book" : books.Length + " books"));
            }
            else
                WritePosts(books, result[0], output, dir, name, written);

            // files from an earlier conversion of this song, which a player might paste by mistake
            Regex ours = Ours(name);
            foreach (string old in Directory.GetFiles(dir, name + "*.rzr"))
                if (ours.IsMatch(Path.GetFileName(old)) && !written.Contains(Path.GetFullPath(old), StringComparer.OrdinalIgnoreCase))
                    Console.WriteLine("Note: {0} is from an earlier conversion (not replaced): don't paste it.", Path.GetFileName(old));
            return written;
        }

        // Format: post: a file per post with the book's Paste (Books.Posts: at most 250 lines, 18,000 characters):
        //  <song>.rzr when the song is one post, else <song>.post1.rzr ... (one book), <song>.book1.rzr ... (several
        //  books, one post each) or <song>.book1.post1.rzr ...
        private static void WritePosts(string[] books, string rzr, string output, string dir, string name, List<string> written)
        {
            bool pieces = false;
            for (int b = 0; b < books.Length; b++)
            {
                string[] posts = Books.Posts(books[b]);
                pieces |= posts.Length > 1;
                for (int p = 0; p < posts.Length; p++)
                {
                    bool whole = books.Length == 1 && posts.Length == 1;
                    string path = whole ? output : Path.Combine(dir, string.Format("{0}{1}{2}.rzr", name,
                        books.Length > 1 ? ".book" + (b + 1) : "", posts.Length > 1 ? ".post" + (p + 1) : ""));
                    File.WriteAllText(path, whole ? rzr : posts[p]);
                    written.Add(Path.GetFullPath(path));
                    Console.WriteLine("Wrote {0}: {1}.", path,
                        whole ? "post it into a music composition book"
                        : books.Length == 1 ? string.Format("post {0} of {1} into the book", p + 1, posts.Length)
                        : posts.Length == 1 ? string.Format("post it into book {0} of {1}", b + 1, books.Length)
                        : string.Format("book {0} of {1}, post {2} of {3}", b + 1, books.Length, p + 1, posts.Length));
                }
            }
            Console.WriteLine("In the game: right-click the book in your backpack, Paste, Post Message, then paste the file (Ctrl+V) and Post.{0}",
                books.Length > 1 ? " Use a new book for each book number, and post each book's pieces in order."
                : pieces ? " Post the pieces in order; each one goes in after the last." : "");
        }

        // a song's files: <song>.rzr, or <song>[.bookN][.postM].rzr (or .pasteM: the pieces made before 11/3/26)
        private static Regex Ours(string name) => new("^" + Regex.Escape(name) + @"(\.book(\d+))?(\.(?:paste|post)(\d+))?\.rzr$", RegexOptions.IgnoreCase);

        // a piece's kind, from an Ours match: "paste", "post", or "" (a whole book)
        private static string Kind(Match m) => Regex.Replace(m.Groups[3].Value, @"[\d.]", "").ToLowerInvariant();

        // the files to play for a .rzr: the file itself, or when it is a piece (.bookN / .postM), all of the song's
        //  pieces of the same kind, in order (book, then post)
        private static List<string>? Pieces(string rzr)
        {
            if (!File.Exists(rzr))
            {
                Console.WriteLine("MidiToAims: {0} was not found.", rzr);
                return null;
            }
            string full = Path.GetFullPath(rzr);
            Match piece = Regex.Match(Path.GetFileName(full), @"^(?<name>.*?)(?<book>\.book\d+)?(?<paste>\.(?:paste|post)\d+)?\.rzr$", RegexOptions.IgnoreCase);
            bool hasBook = piece.Groups["book"].Success, hasPaste = piece.Groups["paste"].Success;
            if (!hasBook && !hasPaste)
                return new List<string> { full };

            // the pieces of the same kind (posts or pastes; a book of one piece has none: it goes with either). A
            //  whole book named: the kind of the newest piece beside it
            string dir = Path.GetDirectoryName(full)!, name = piece.Groups["name"].Value;
            Regex ours = Ours(name);
            var candidates = Directory.GetFiles(dir, name + "*.rzr")
                .Select(f => (File: f, M: ours.Match(Path.GetFileName(f))))
                .Where(x => x.M.Success && x.M.Groups[1].Success == hasBook).ToList();
            string kind = Regex.Replace(piece.Groups["paste"].Value, @"[\d.]", "").ToLowerInvariant();
            if (kind.Length == 0)
                kind = candidates.Where(x => Kind(x.M).Length > 0).OrderByDescending(x => File.GetLastWriteTimeUtc(x.File)).Select(x => Kind(x.M)).FirstOrDefault() ?? "";
            List<string> files = candidates
                .Where(x => Kind(x.M) == kind || (hasBook && Kind(x.M).Length == 0))
                .OrderBy(x => x.M.Groups[2].Success ? int.Parse(x.M.Groups[2].Value) : 0)
                .ThenBy(x => x.M.Groups[4].Success ? int.Parse(x.M.Groups[4].Value) : 0)
                .Select(x => x.File).ToList();
            if (Debug)
                Console.WriteLine("Playing {0}: {1}.", files.Count == 1 ? "1 file" : files.Count + " files", string.Join(", ", files.Select(Path.GetFileName)));
            return files;
        }
    }
}
