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

namespace ZLR.VM.Debugging
{
    public class DebugInfo
    {
        private struct LineRef
        {
            public byte FileNum;
            public ushort LineNum;
            public byte Column;

            public bool IsValid
            {
                get
                {
                    if (FileNum == 0 || FileNum == 255)
                        return false;
                    else
                        return true;
                }
            }
        }

        private List<RoutineInfo> routines = new List<RoutineInfo>();
        private byte[] matchingHeader;

        public DebugInfo(Stream fromStream)
        {
            using (BinaryReader br = new BinaryReader(fromStream))
            {
                if (ReadWord(br) != 0xDEBF)
                    throw new ArgumentException("Invalid debug file header");
                if (ReadWord(br) != 0)
                    throw new ArgumentException("Unrecognized debug file version");
                ReadWord(br); // skip Inform version

                Dictionary<byte, string> filenames = new Dictionary<byte, string>(5);
                Dictionary<ushort, int> routineStarts = new Dictionary<ushort, int>();

                int i, codeArea = 0;
                byte b;
                ushort w;
                string str;
                LineRef line;
                RoutineInfo routine = null;

                while (fromStream.Position < fromStream.Length)
                {
                    byte type = br.ReadByte();
                    switch (type)
                    {
                        case 0:
                            // EOF_DBR
                            fromStream.Seek(0, SeekOrigin.End);
                            break;

                        case 1:
                            // FILE_DBR
                            b = br.ReadByte();
                            ReadString(br); // skip include name
                            str = ReadString(br);
                            filenames[b] = str;
                            break;

                        case 2:
                            // CLASS_DBR
                            ReadString(br);
                            ReadLineRef(br);
                            ReadLineRef(br);
                            break;

                        case 3:
                            // OBJECT_DBR
                            ReadWord(br);
                            ReadString(br);
                            ReadLineRef(br);
                            ReadLineRef(br);
                            break;

                        case 4:
                            // GLOBAL_DBR
                            br.ReadByte();
                            ReadString(br);
                            break;

                        case 12: // ARRAY_DBR
                        case 5: // ATTR_DBR
                        case 6: // PROP_DBR
                        case 7: // FAKE_ACTION_DBR
                        case 8: // ACTION_DBR
                            ReadWord(br);
                            ReadString(br);
                            break;

                        case 9:
                            // HEADER_DBR
                            matchingHeader = br.ReadBytes(64);
                            break;

                        case 11:
                            // ROUTINE_DBR
                            routine = new RoutineInfo();
                            routines.Add(routine);
                            w = ReadWord(br);
                            line = ReadLineRef(br);
                            if (line.IsValid)
                                routine.DefinedAt = new LineInfo(
                                    filenames[line.FileNum],
                                    line.LineNum,
                                    line.Column);
                            routine.CodeStart = ReadAddress(br);
                            routineStarts[w] = routine.CodeStart;
                            routine.Name = ReadString(br);
                            while (ReadString(br) != "")
                            {
                                // skip local variable names
                            }
                            break;

                        case 10:
                            // LINEREF_DBR
                            ReadWord(br);
                            w = ReadWord(br);
                            while (w-- > 0)
                            {
                                ReadLineRef(br);
                                ReadWord(br);
                            }
                            break;

                        case 14:
                            // ROUTINE_END_DBR
                            // assume routine is still set from earlier...
                            ReadWord(br); // skip routine number
                            ReadLineRef(br); // skip defn end
                            i = ReadAddress(br);
                            routine.CodeLength = i - routine.CodeStart;
                            break;

                        case 13:
                            // MAP_DBR
                            do
                            {
                                str = ReadString(br);
                                if (str != "")
                                {
                                    i = ReadAddress(br);
                                    if (str == "code area")
                                        codeArea = i;
                                }
                            } while (str != "");
                            break;
                    }
                }

                // patch routine addresses
                foreach (RoutineInfo ri in routines)
                    ri.CodeStart += codeArea;

                routines.Sort(delegate (RoutineInfo r1, RoutineInfo r2) { return r1.CodeStart - r2.CodeStart; });
            }
        }

        private static ushort ReadWord(BinaryReader rdr)
        {
            byte b1 = rdr.ReadByte();
            byte b2 = rdr.ReadByte();
            return (ushort)((b1 << 8) + b2);
        }

        private static int ReadAddress(BinaryReader rdr)
        {
            byte b1 = rdr.ReadByte();
            byte b2 = rdr.ReadByte();
            byte b3 = rdr.ReadByte();
            return (b1 << 16) + (b2 << 8) + b3;
        }

        private static string ReadString(BinaryReader rdr)
        {
            StringBuilder sb = new StringBuilder();
            byte b = rdr.ReadByte();
            while (b != 0)
            {
                sb.Append((char)b);
                b = rdr.ReadByte();
            }
            return sb.ToString();
        }

        private static LineRef ReadLineRef(BinaryReader rdr)
        {
            LineRef result;
            result.FileNum = rdr.ReadByte();
            result.LineNum = ReadWord(rdr);
            result.Column = rdr.ReadByte();
            return result;
        }

        public bool MatchesGameFile(Stream gameFile)
        {
            if (matchingHeader == null)
                return true;

            byte[] gameHeader = new byte[64];
            gameFile.Seek(0, SeekOrigin.Begin);
            int len = gameFile.Read(gameHeader, 0, 64);
            if (len < 64)
                return false;

            for (int i = 0; i < 64; i++)
                if (gameHeader[i] != matchingHeader[i])
                    return false;

            return true;
        }

        public IEnumerable<RoutineInfo> Routines
        {
            get { return routines; }
        }

        public RoutineInfo FindRoutine(int pc)
        {
            int start = 0, end = routines.Count;

            while (start < end)
            {
                int mid = (start + end) / 2;

                RoutineInfo ri = routines[mid];
                if (pc >= ri.CodeStart && pc < ri.CodeStart + ri.CodeLength)
                    return ri;

                if (pc > ri.CodeStart)
                    start = mid + 1;
                else
                    end = mid;
            }

            return null;
        }
    }

    public class RoutineInfo
    {
        public string Name;
        public int CodeStart;
        public int CodeLength;
        public LineInfo DefinedAt;
        public string[] Locals;
    }

    public struct LineInfo
    {
        public string File;
        public int Line;
        public int Position;

        public LineInfo(string file, int line, int position)
        {
            this.File = file;
            this.Line = line;
            this.Position = position;
        }
    }
}