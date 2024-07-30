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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZLR.VM
{
    public delegate bool TimedInputCallback();
    public delegate short CharTranslator(char ch);

    public partial class ZMachine
    {
        private short ReadImpl(ushort buffer, ushort parse, ushort time, ushort routine)
        {
            byte max = GetByte(buffer);
            byte initlen = GetByte(buffer + 1);

            byte terminator;
            string str;

            BeginExternalWait();
            try
            {
                if (cmdRdr == null)
                {
                    string initial = string.Empty;
                    if (initlen > 0)
                    {
                        StringBuilder sb = new StringBuilder(initlen);
                        for (int i = 0; i < initlen; i++)
                            sb.Append(CharFromZSCII(GetByte(buffer + 2 + i)));
                        initial = sb.ToString();
                    }
                    str = io.ReadLine(initial,
                        time, delegate { return HandleInputTimer(routine); },
                        terminatingChars, out terminator);
                }
                else
                {
                    str = cmdRdr.ReadLine(out terminator);
                    if (terminator == 13)
                        io.PutCommand(str + "\n");
                    else
                        io.PutCommand(str);
                }

                if (cmdWtr != null)
                    cmdWtr.WriteLine(str, terminator);
            }
            finally
            {
                EndExternalWait();
            }

            byte[] chars = StringToZSCII(str.ToLower());
            SetByte(buffer + 1, (byte)chars.Length);
            for (int i = 0; i < Math.Min(chars.Length, max); i++)
                SetByte(buffer + 2 + i, chars[i]);

            if (parse != 0)
                Tokenize(buffer, parse, 0, false);

            return terminator;
        }

        private short ReadCharImpl(ushort time, ushort routine)
        {
            BeginExternalWait();
            try
            {
                short result;

                if (cmdRdr != null && cmdRdr.EOF)
                {
                    cmdRdr.Dispose();
                    cmdRdr = null;
                }

                if (cmdRdr == null)
                    result = io.ReadKey(time,
                        delegate { return HandleInputTimer(routine); },
                        delegate (char c) { return FilterInput(CharToZSCII(c)); });
                else
                    result = cmdRdr.ReadKey();

                if (cmdWtr != null)
                    cmdWtr.WriteKey((byte)result);

                return result;
            }
            finally
            {
                EndExternalWait();
            }
        }

        private bool HandleInputTimer(ushort routine)
        {
            EnterFunctionImpl((short)routine, null, 0, pc);

            JitLoop();

            short result = stack.Pop();
            return (result != 0);
        }

        private short FilterInput(short ch)
        {
            // only allow characters that are defined for input: section 3.8
            if (ch < 32 && (ch != 8 && ch != 13 && ch != 27))
                return 0;
            else if (ch >= 127 && (ch <= 128 || ch >= 255))
                return 0;

            return ch;
        }

        private struct Token
        {
            public byte StartPos, Length;

            public Token(byte startPos, byte Length)
            {
                this.StartPos = startPos;
                this.Length = Length;
            }
        }

        private bool IsTokenSpace(byte ch)
        {
            return (ch == 9) || (ch == 32);
        }

        private List<Token> SplitTokens(byte[] buffer, ushort userDict)
        {
            List<Token> result = new List<Token>();
            byte[] seps;

            if (userDict == 0)
            {
                seps = wordSeparators;
            }
            else
            {
                byte n = GetByte(userDict);
                seps = new byte[n];
                GetBytes(userDict + 1, n, seps, 0);
            }

            int i = 0;
            do
            {
                // skip whitespace
                while (i < buffer.Length && IsTokenSpace(buffer[i]))
                    i++;

                if (i >= buffer.Length)
                    break;

                // found a separator?
                if (Array.IndexOf(seps, buffer[i]) >= 0)
                {
                    result.Add(new Token((byte)i, 1));
                    i++;
                }
                else
                {
                    byte start = (byte)i;

                    // find the end of the word
                    while (i < buffer.Length && !IsTokenSpace(buffer[i]) &&
                            Array.IndexOf(seps, buffer[i]) == -1)
                    {
                        i++;
                    }

                    // add it to the list
                    result.Add(new Token(start, (byte)(i - start)));
                }
            } while (i < buffer.Length);

            return result;
        }

        private void Tokenize(ushort buffer, ushort parse, ushort userDict, bool skipUnrecognized)
        {
            byte bufLen = GetByte(buffer + 1);
            byte max = GetByte(parse + 0);
            byte count = 0;

            byte[] myBuffer = new byte[bufLen];
            GetBytes(buffer + 2, bufLen, myBuffer, 0);
            List<Token> tokens = SplitTokens(myBuffer, userDict);

            foreach (Token tok in tokens)
            {
                ushort word = LookUpWord(userDict, myBuffer, tok.StartPos, tok.Length);
                if (word == 0 && skipUnrecognized)
                    continue;

                SetWord(parse + 2 + 4 * count, (short)word);
                SetByte(parse + 2 + 4 * count + 2, tok.Length);
                SetByte(parse + 2 + 4 * count + 3, (byte)(2 + tok.StartPos));
                count++;

                if (count == max)
                    break;
            }

            SetByte(parse + 1, count);
        }

        private ushort LookUpWord(int userDict, byte[] buffer, int pos, int length)
        {
            int dictStart;
            byte[] word;

            word = EncodeText(buffer, pos, length, DICT_WORD_SIZE);

            if (userDict != 0)
            {
                byte n = GetByte(userDict);
                dictStart = userDict + 1 + n;
            }
            else
            {
                dictStart = dictionaryTable + 1 + wordSeparators.Length;
            }

            byte entryLength = GetByte(dictStart++);

            int entries;
            if (userDict == 0)
                entries = (ushort)GetWord(dictStart);
            else
                entries = GetWord(dictStart);
            dictStart += 2;

            if (entries < 0)
            {
                // use linear search for unsorted user dictionary
                for (int i = 0; i < entries; i++)
                {
                    int addr = dictStart + i * entryLength;
                    if (CompareWords(word, addr) == 0)
                        return (ushort)addr;
                }
            }
            else
            {
                // use binary search
                int start = 0, end = entries;
                while (start < end)
                {
                    int mid = (start + end) / 2;
                    int addr = dictStart + mid * entryLength;
                    int cmp = CompareWords(word, addr);
                    if (cmp == 0)
                        return (ushort)addr;
                    else if (cmp < 0)
                        end = mid;
                    else
                        start = mid + 1;
                }
            }

            return 0;
        }

        private int CompareWords(byte[] word, int addr)
        {
            for (int i = 0; i < word.Length; i++)
            {
                int cmp = word[i] - GetByte(addr + i);
                if (cmp != 0)
                    return cmp;
            }

            return 0;
        }

        /// <summary>
        /// Encodes a section of text, optionally truncating or padding the output to a fixed size.
        /// </summary>
        /// <param name="input">The buffer containing the plain text.</param>
        /// <param name="start">The index within <paramref name="input"/> where the
        /// plain text starts.</param>
        /// <param name="length">The length of the plain text.</param>
        /// <param name="numZchars">The number of 5-bit characters that the output should be
        /// truncated or padded to, which must be a multiple of 3; or 0 to allow variable size
        /// output (padded up to a multiple of 2 bytes, if necessary).</param>
        /// <returns>The encoded text, with th.</returns>
        private byte[] EncodeText(byte[] input, int start, int length, int numZchars)
        {
            List<byte> zchars;
            if (numZchars == 0)
            {
                zchars = new List<byte>(length);
            }
            else
            {
                if (numZchars < 0 || numZchars % 3 != 0)
                    throw new ArgumentException("Output size must be a multiple of 3", "numZchars");
                zchars = new List<byte>(numZchars);
            }

            for (int i = 0; i < length; i++)
            {
                byte zc = input[start + i];
                char ch = CharFromZSCII(zc);

                if (ch == ' ')
                {
                    zchars.Add(0);
                }
                else
                {
                    int alpha;
                    if ((alpha = Array.IndexOf(alphabet0, ch)) >= 0)
                    {
                        zchars.Add((byte)(alpha + 6));
                    }
                    else if ((alpha = Array.IndexOf(alphabet1, ch)) >= 0)
                    {
                        zchars.Add(4);
                        zchars.Add((byte)(alpha + 6));
                    }
                    else if ((alpha = Array.IndexOf(alphabet2, ch)) >= 0)
                    {
                        zchars.Add(5);
                        zchars.Add((byte)(alpha + 6));
                    }
                    else
                    {
                        zchars.Add(5);
                        zchars.Add(6);
                        zchars.Add((byte)(zc >> 5));
                        zchars.Add((byte)(zc & 31));
                    }
                }
            }

            int resultBytes;
            if (numZchars == 0)
            {
                // pad up to a multiple of 3
                while (zchars.Count % 3 != 0)
                    zchars.Add(5);
                resultBytes = zchars.Count * 2 / 3;
            }
            else
            {
                // pad up to the fixed size
                while (zchars.Count < numZchars)
                    zchars.Add(5);
                resultBytes = numZchars * 2 / 3;
            }

            byte[] result = new byte[resultBytes];
            int zi = 0, ri = 0;
            while (ri < resultBytes)
            {
                result[ri] = (byte)((zchars[zi] << 2) | (zchars[zi + 1] >> 3));
                result[ri + 1] = (byte)((zchars[zi + 1] << 5) | zchars[zi + 2]);
                ri += 2;
                zi += 3;
            }

            result[resultBytes - 2] |= 128;
            return result;
        }

        private void SetInputStream(short num)
        {
            switch (num)
            {
                case 0:
                    if (cmdRdr != null)
                    {
                        cmdRdr.Dispose();
                        cmdRdr = null;
                    }
                    break;

                case 1:
                    Stream cmdStream = io.OpenCommandFile(false);
                    if (cmdStream != null)
                    {
                        if (cmdRdr != null)
                            cmdRdr.Dispose();

                        try
                        {
                            cmdRdr = new CommandFileReader(cmdStream);
                        }
                        catch
                        {
                            cmdRdr = null;
                        }
                    }
                    break;

                default:
                    throw new Exception("Invalid input stream #" + num.ToString());
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the player's inputs are to be
        /// written to a command file.
        /// </summary>
        /// <remarks>
        /// <para>This property enables or disables output stream 4.</para>
        /// <para>When this property is set to true, <see cref="IZMachineIO.OpenCommandFile"/>
        /// will be called to get a stream for the command file. The property will be
        /// reset to false after the game finishes running.</para>
        /// </remarks>
        public bool WritingCommandsToFile
        {
            get
            {
                return (cmdWtr != null);
            }
            set
            {
                if (value)
                    SetOutputStream(4, 0);
                else
                    SetOutputStream(-4, 0);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the player's inputs are to be
        /// read from a command file.
        /// </summary>
        /// <remarks>
        /// <para>This property switches between input stream 1 (true) and input stream 0
        /// (false).</para>
        /// <para>When this property is set to true, <see cref="IZMachineIO.OpenCommandFile"/>
        /// will be called to get a stream for the command file. The property will be
        /// reset to false after the game finishes running.</para>
        /// </remarks>
        public bool ReadingCommandsFromFile
        {
            get
            {
                return (cmdRdr != null);
            }
            set
            {
                if (value)
                    SetInputStream(1);
                else
                    SetInputStream(0);
            }
        }

        private class CommandFileReader : IDisposable
        {
            private StreamReader rdr;

            public CommandFileReader(Stream stream)
            {
                rdr = new StreamReader(stream);
            }

            public void Dispose()
            {
                if (rdr != null)
                {
                    rdr.Close();
                    rdr = null;
                }
            }

            public bool EOF
            {
                get { return rdr.EndOfStream; }
            }

            public string ReadLine(out byte terminator)
            {
                terminator = 13;
                string line = rdr.ReadLine();

                if (line.EndsWith("]"))
                {
                    int idx = line.LastIndexOf('[');
                    if (idx >= 0)
                    {
                        string key = line.Substring(idx + 1, line.Length - idx - 2);
                        int keyCode;
                        if (int.TryParse(key, out keyCode) == true)
                        {
                            line = line.Substring(0, idx);
                            terminator = (byte)keyCode;
                        }
                        else
                        {
                            terminator = 13;
                        }
                    }
                }

                return line;
            }

            public byte ReadKey()
            {
                string line = rdr.ReadLine();

                if (line.Length == 0)
                    return 13;

                if (line.StartsWith("["))
                {
                    int idx = line.IndexOf(']');
                    if (idx >= 0)
                    {
                        string key = line.Substring(1, idx - 1);
                        int keyCode;
                        if (int.TryParse(key, out keyCode) == true)
                            return (byte)keyCode;
                    }
                }

                return (byte)line[0];
            }
        }

        private class CommandFileWriter : IDisposable
        {
            private StreamWriter wtr;

            public CommandFileWriter(Stream stream)
            {
                wtr = new StreamWriter(stream);
            }

            public void Dispose()
            {
                if (wtr != null)
                {
                    wtr.Close();
                    wtr = null;
                }
            }

            public void WriteLine(string text, byte terminator)
            {
                if (terminator == 13 && !text.EndsWith("]"))
                    wtr.WriteLine(text);
                else
                    wtr.WriteLine("{0}[{1}]", text, terminator);
            }

            public void WriteKey(byte key)
            {
                if (key < 128 && key != '[')
                    wtr.WriteLine((char)key);
                else
                    wtr.WriteLine("[{0}]", key);
            }
        }
    }
}