/***************************************************************************
 *
 *  MidiToAims - the sounds of the player's own Ultima Online client (sound.mul), for MidiToAims play, preview and
 *  sound. Nothing of the
 *  game ships with MidiToAims: it reads the client files already on this computer.
 *
 *  Where the files are, first found wins:
 *      --uo <folder>                   given on the command line (then remembered in MidiToAims.ini, beside the exe)
 *      MidiToAims.ini                  "UO folder: <folder>"
 *      the Angel Island launcher       HKLM\SOFTWARE\(WOW6432Node\)Angel Island\Ultima Online\1.0, InstCDPath
 *                                      (its installer writes it: <launcher>\UO Angel Island)
 *      ClassicUO's settings.json       "ultimaonlinedirectory", in <launcher>\ClassicUO
 *      the Golden Client installer     ...\Origin Worlds Online\Ultima Online\1.0, InstCDPath
 *      the usual install folders
 *  (Never the launcher's appstate.json: it holds the player's passwords.)
 *
 *  Ini: MidiToAims.ini beside the exe, "Key: value" lines: the UO folder, and MidiToAims play's settings.
 *
 *  sound.mul: an entry is a 40-byte name, then 16-bit mono PCM at 22050 Hz; soundidx.mul: 12 bytes an entry (offset,
 *  length, extra); the sound ID the shard sends is the entry's index. Sound.def fills empty entries from others (as
 *  ClassicUO's SoundsLoader does: the last usable one of the group wins, -1 empties it).
 *
 ***************************************************************************/

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MidiToAims
{
    public sealed class ClientSounds
    {
        public const int SampleRate = 22050;
        public const int MaxSounds = 4096;              // ClassicUO's MAX_SOUND_DATA_INDEX_COUNT
        private const string IniKey = "UO folder:";

        public string Folder { get; }
        private readonly string m_mul;
        private readonly (int Offset, int Length)[] m_index = new (int, int)[MaxSounds];
        private readonly Dictionary<int, short[]?> m_cache = new();

        private ClientSounds(string folder)
        {
            Folder = folder;
            m_mul = Path.Combine(folder, "sound.mul");
            byte[] idx = File.ReadAllBytes(Path.Combine(folder, "soundidx.mul"));
            for (int i = 0; i < MaxSounds && i * 12 + 8 <= idx.Length; i++)
                m_index[i] = (BitConverter.ToInt32(idx, i * 12), BitConverter.ToInt32(idx, i * 12 + 4));

            // Sound.def: "index {a, b, ...} volume" fills an empty entry
            string def = Path.Combine(folder, "Sound.def");
            if (File.Exists(def))
                foreach (string line in File.ReadAllLines(def))
                {
                    Match m = Regex.Match(line, @"^\s*(\d+)\s*\{([^}]*)\}");
                    if (!m.Success || !int.TryParse(m.Groups[1].Value, out int index) || index >= MaxSounds || Usable(index))
                        continue;
                    foreach (string g in m.Groups[2].Value.Split(',', ' ', StringSplitOptions.RemoveEmptyEntries))
                        if (int.TryParse(g, out int from) && from >= -1 && from < MaxSounds)
                        {
                            if (from == -1)
                                m_index[index] = (-1, 0);
                            else if (Usable(from))
                                m_index[index] = m_index[from];
                        }
                }
        }

        private bool Usable(int id) => id >= 0 && id < MaxSounds && m_index[id].Offset >= 0 && m_index[id].Length > 40;

        // the sound's samples (null: the client has no such sound)
        public short[]? Samples(int id)
        {
            if (m_cache.TryGetValue(id, out short[]? cached))
                return cached;
            short[]? samples = null;
            if (Usable(id))
            {
                var (offset, length) = m_index[id];
                byte[] data = new byte[length - 40];
                using (FileStream fs = File.OpenRead(m_mul))
                {
                    fs.Position = offset + 40;
                    fs.ReadExactly(data);
                }
                samples = new short[data.Length / 2];
                Buffer.BlockCopy(data, 0, samples, 0, samples.Length * 2);
            }
            return m_cache[id] = samples;
        }

        // the sounds the client has
        public IEnumerable<int> Ids => Enumerable.Range(0, MaxSounds).Where(Usable);

        // a sound's name in sound.mul (its file name, e.g. "switch01.wav")
        public string Name(int id)
        {
            if (!Usable(id))
                return "";
            byte[] name = new byte[40];
            using (FileStream fs = File.OpenRead(m_mul))
            {
                fs.Position = m_index[id].Offset;
                fs.ReadExactly(name);
            }
            int end = Array.IndexOf(name, (byte)0);
            return Encoding.ASCII.GetString(name, 0, end < 0 ? name.Length : end).Trim();
        }

        // ClassicUO plays one instance of a sound: playing it again within this many ms of its start does nothing
        //  (Sound.Play: Delay = (bytes - 32) / 88.2, about half the sound); later, it restarts it
        public double LockoutMs(int id) => Usable(id) ? (m_index[id].Length - 40 - 32) / 88.2 : 0;

        // finds the client files (see the header); tried: where we looked, for the message when nothing is found
        public static ClientSounds? Find(string? given, List<string> tried)
        {
            if (given != null)
            {
                string folder = File.Exists(given) ? Path.GetDirectoryName(Path.GetFullPath(given))! : given;
                tried.Add(folder + " (--uo)");
                if (!HasSounds(folder))
                    return null;
                Ini.Set(IniKey, Path.GetFullPath(folder));
                return new ClientSounds(Path.GetFullPath(folder));
            }

            foreach (string folder in Candidates())
            {
                if (tried.Contains(folder, StringComparer.OrdinalIgnoreCase))
                    continue;
                tried.Add(folder);
                if (HasSounds(folder))
                    return new ClientSounds(folder);
            }
            return null;
        }

        public static bool HasSounds(string folder) =>
            File.Exists(Path.Combine(folder, "sound.mul")) && File.Exists(Path.Combine(folder, "soundidx.mul"));

        // a client that keeps its sounds in a .uop (newer clients): MidiToAims reads only sound.mul
        public static bool HasUop(string folder) => File.Exists(Path.Combine(folder, "soundLegacyMUL.uop"));

        private static IEnumerable<string> Candidates()
        {
            if (Ini.Get(IniKey) is string remembered)
                yield return remembered;

            string? launcherUo = Registered(@"SOFTWARE\Angel Island\Ultima Online\1.0");
            if (launcherUo != null)
                yield return launcherUo;

            string x86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            List<string> launchers = new();
            if (launcherUo != null && Path.GetDirectoryName(launcherUo.TrimEnd('\\', '/')) is string parent)
                launchers.Add(parent);
            launchers.Add(Path.Combine(x86, "Angel Island", "Angel Island Launcher"));
            foreach (string launcher in launchers)
                if (ClassicUoFolder(Path.Combine(launcher, "ClassicUO", "settings.json")) is string cuo)
                    yield return cuo;

            string? golden = Registered(@"SOFTWARE\Origin Worlds Online\Ultima Online\1.0");
            if (golden != null)
                yield return golden;

            yield return Path.Combine(x86, "Angel Island", "Angel Island Launcher", "UO Angel Island");
            yield return Path.Combine(x86, "Ultima Online");
            yield return Path.Combine(x86, "Ultima Online Classic");
        }

        // an install folder from the registry (the installers are 32-bit: WOW6432Node on 64-bit Windows)
        private static string? Registered(string key)
        {
            if (!OperatingSystem.IsWindows())
                return null;
            foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                    using RegistryKey? k = hklm.OpenSubKey(key);
                    if (k?.GetValue("InstCDPath") is string path && path.Trim().Length > 0)
                        return path.Trim();
                }
                catch { }
            }
            return null;
        }

        // ClassicUO's "ultimaonlinedirectory" (a JSON string: \\ and \" escaped)
        private static string? ClassicUoFolder(string settings)
        {
            try
            {
                if (!File.Exists(settings))
                    return null;
                Match m = Regex.Match(File.ReadAllText(settings), "\"ultimaonlinedirectory\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.IgnoreCase);
                if (!m.Success)
                    return null;
                StringBuilder s = new();
                string v = m.Groups[1].Value;
                for (int i = 0; i < v.Length; i++)
                    s.Append(v[i] == '\\' && i + 1 < v.Length ? v[++i] : v[i]);
                return s.Length > 0 ? s.ToString() : null;
            }
            catch
            {
                return null;
            }
        }
    }

    // MidiToAims.ini beside the exe: "Key: value" lines (the UO folder, MidiToAims play's settings); a write keeps the
    //  other lines. When it can't be read or written, the defaults apply.
    public static class Ini
    {
        private static string File => Path.Combine(AppContext.BaseDirectory, "MidiToAims.ini");

        // the value after key (which ends with ':'), or null
        public static string? Get(string key)
        {
            try
            {
                if (System.IO.File.Exists(File))
                    foreach (string line in System.IO.File.ReadAllLines(File))
                        if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                            return line.Substring(key.Length).Trim();
            }
            catch { }
            return null;
        }

        // sets key's value (null: removes it)
        public static void Set(string key, string? value)
        {
            try
            {
                List<string> lines = System.IO.File.Exists(File)
                    ? System.IO.File.ReadAllLines(File).Where(l => !l.StartsWith(key, StringComparison.OrdinalIgnoreCase)).ToList() : new();
                if (value != null)
                    lines.Add(key + " " + value);
                System.IO.File.WriteAllLines(File, lines);
            }
            catch { }
        }
    }
}
