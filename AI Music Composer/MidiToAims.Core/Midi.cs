/***************************************************************************
 *
 *  MidiToAims.Core - Midi: a minimal Standard MIDI File reader.
 *
 *  Note-ons (with their track and channel), tempo changes, the first time signature, and each track's name.
 *
 ***************************************************************************/

using System.Text;

namespace MidiToAims
{
    public sealed class Midi
    {
        public readonly record struct Note(long Tick, int Track, int Channel, int Pitch, int Velocity);

        public int Division { get; private set; }                        // ticks per quarter note
        public List<Note> Notes { get; } = new();
        public Dictionary<int, string> TrackNames { get; } = new();
        public long BarTicks { get; private set; }
        private readonly List<(long Tick, int MicrosPerQuarter)> m_tempos = new();

        public static Midi Read(string path)
        {
            byte[] b = File.ReadAllBytes(path);
            Midi midi = new();
            int at = 0;
            if (Ascii(b, at, 4) != "MThd")
                throw new InvalidDataException("not a MIDI file");
            int headerLength = Be32(b, at + 4);
            int tracks = Be16(b, at + 10);
            int division = Be16(b, at + 12);
            if ((division & 0x8000) != 0)
                throw new InvalidDataException("SMPTE time division is not supported");
            midi.Division = division;
            midi.BarTicks = division * 4;                                   // 4/4 until a time signature says otherwise
            at += 8 + headerLength;
            bool timeSignature = false;

            for (int t = 0; t < tracks && at < b.Length; t++)
            {
                if (Ascii(b, at, 4) != "MTrk")
                    throw new InvalidDataException("track " + t + ": expected MTrk");
                int end = at + 8 + Be32(b, at + 4);
                at += 8;
                long tick = 0;
                int status = 0;
                while (at < end)
                {
                    tick += VarLen(b, ref at);
                    int first = b[at];
                    if (first >= 0x80) { status = first; at++; }               // otherwise running status
                    if (status == 0xFF)
                    {
                        int type = b[at++];
                        int length = (int)VarLen(b, ref at);
                        if (type == 0x51 && length == 3)
                            midi.m_tempos.Add((tick, (b[at] << 16) | (b[at + 1] << 8) | b[at + 2]));
                        else if (type == 0x58 && length >= 2 && !timeSignature)
                        {
                            midi.BarTicks = (long)(division * 4 * b[at] / Math.Pow(2, b[at + 1]));
                            timeSignature = true;
                        }
                        else if (type == 0x03 && length > 0 && !midi.TrackNames.ContainsKey(t))
                            midi.TrackNames[t] = Encoding.Latin1.GetString(b, at, length).Trim();
                        at += length;
                    }
                    else if (status == 0xF0 || status == 0xF7)
                        at += (int)VarLen(b, ref at);
                    else
                    {
                        int kind = status & 0xF0, channel = status & 0x0F;
                        int data1 = b[at++];
                        int data2 = kind is 0xC0 or 0xD0 ? 0 : b[at++];
                        if (kind == 0x90 && data2 > 0)
                            midi.Notes.Add(new Note(tick, t, channel, data1, data2));
                    }
                }
                at = end;
            }
            midi.m_tempos.Sort((x, y) => x.Tick.CompareTo(y.Tick));
            return midi;
        }

        // seconds from the start to this tick, through the tempo changes (120 bpm until the first)
        public double Seconds(long tick)
        {
            double seconds = 0;
            long last = 0;
            int tempo = 500000;
            foreach (var (t, micros) in m_tempos)
            {
                if (t >= tick) break;
                seconds += (t - last) * tempo / 1e6 / Division;
                last = t;
                tempo = micros;
            }
            return seconds + (tick - last) * tempo / 1e6 / Division;
        }

        private static string Ascii(byte[] b, int at, int n) => at + n <= b.Length ? Encoding.ASCII.GetString(b, at, n) : "";
        private static int Be16(byte[] b, int at) => (b[at] << 8) | b[at + 1];
        private static int Be32(byte[] b, int at) => (b[at] << 24) | (b[at + 1] << 16) | (b[at + 2] << 8) | b[at + 3];
        private static long VarLen(byte[] b, ref int at)
        {
            long value = 0;
            byte c;
            do { c = b[at++]; value = (value << 7) | (uint)(c & 0x7F); } while ((c & 0x80) != 0);
            return value;
        }
    }
}
