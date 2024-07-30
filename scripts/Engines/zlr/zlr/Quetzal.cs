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
using ZLR.IFF;

namespace ZLR.VM
{
    public partial class ZMachine
    {
        private void SaveQuetzal(byte dest, int nextPC)
        {
            Quetzal quetzal = new Quetzal();
            quetzal.AddBlock("IFhd", MakeIFHD(nextPC - 1));
            quetzal.AddBlock("CMem", CompressRAM());
            quetzal.AddBlock("Stks", SerializeStacks());

            using (Stream stream = io.OpenSaveFile(quetzal.Length))
            {
                if (stream == null)
                {
                    StoreResult(dest, 0);
                    return;
                }

                try
                {
                    quetzal.WriteToStream(stream);
                    StoreResult(dest, 1);
                }
                catch
                {
                    StoreResult(dest, 0);
                }
            }
        }

        private void RestoreQuetzal(byte dest, int failurePC)
        {
            // there are many ways this can go wrong, so let's just assume it will.
            // if the restore succeeds, what we change up here won't matter anyway.
            StoreResult(dest, 0);
            pc = failurePC;

            using (Stream stream = io.OpenRestoreFile())
            {
                if (stream == null)
                    return;

                try
                {
                    Quetzal quetzal = new Quetzal(stream);

                    // verify everything first
                    int savedPC;
                    byte[] ifhd = quetzal.GetBlock("IFhd");
                    if (!VerifyIFHD(ifhd, out savedPC))
                        return;

                    byte[] cmem = quetzal.GetBlock("CMem");
                    byte[] umem;
                    if (cmem != null)
                        umem = UncompressRAM(cmem);
                    else
                        umem = quetzal.GetBlock("UMem");

                    if (umem == null || umem.Length != romStart)
                        return;

                    byte[] stks = quetzal.GetBlock("Stks");
                    Stack<short> savedStack;
                    Stack<CallFrame> savedCallStack;
                    DeserializeStacks(stks, out savedStack, out savedCallStack);
                    if (savedStack == null || savedCallStack == null)
                        return;

                    // ok, restore it
                    SetBytes(0, umem.Length, umem, 0);
                    stack = savedStack;
                    callStack = savedCallStack;
                    SetTopFrame();
                    pc = savedPC;

                    dest = GetByte(pc++);
                    StoreResult(dest, 2);

                    ResetHeaderFields(false);
                }
                catch
                {
                    StoreResult(dest, 0);
                }
            }
        }

        private byte[] CompressRAM()
        {
            byte[] origRam = new byte[romStart];
            gameFile.Seek(0, SeekOrigin.Begin);
            gameFile.Read(origRam, 0, romStart);

            List<byte> result = new List<byte>(romStart);
            int i = 0;
            while (i < romStart)
            {
                byte b = (byte)(GetByte(i) ^ origRam[i]);
                if (b == 0)
                {
                    int runLength = 1;
                    i++;
                    while (i < romStart && GetByte(i) == origRam[i] && runLength < 256)
                    {
                        runLength++;
                        i++;
                    }
                    result.Add(0);
                    result.Add((byte)(runLength - 1));
                }
                else
                {
                    result.Add(b);
                    i++;
                }
            }

            // remove trailing zeros
            while (result.Count >= 2 && result[result.Count - 2] == 0)
                result.RemoveRange(result.Count - 2, 2);

            return result.ToArray();
        }

        private byte[] UncompressRAM(byte[] cmem)
        {
            byte[] result = new byte[romStart];
            gameFile.Seek(0, SeekOrigin.Begin);
            gameFile.Read(result, 0, romStart);

            int rp = 0;
            try
            {
                for (int i = 0; i < cmem.Length; i++)
                {
                    byte b = cmem[i];
                    if (b == 0)
                        rp += cmem[++i] + 1;
                    else
                        result[rp++] ^= b;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }

            return result;
        }

        private byte[] MakeIFHD(int pc)
        {
            byte[] result = new byte[13];

            BinaryReader br = new BinaryReader(gameFile);

            // release number
            gameFile.Seek(2, SeekOrigin.Begin);
            result[0] = br.ReadByte();
            result[1] = br.ReadByte();
            // serial number
            gameFile.Seek(0x12, SeekOrigin.Begin);
            result[2] = br.ReadByte();
            result[3] = br.ReadByte();
            result[4] = br.ReadByte();
            result[5] = br.ReadByte();
            result[6] = br.ReadByte();
            result[7] = br.ReadByte();
            // checksum
            gameFile.Seek(0x1C, SeekOrigin.Begin);
            result[8] = br.ReadByte();
            result[9] = br.ReadByte();
            // PC
            result[10] = (byte)(pc >> 16);
            result[11] = (byte)(pc >> 8);
            result[12] = (byte)pc;

            return result;
        }

        private bool VerifyIFHD(byte[] ifhd, out int pc)
        {
            if (ifhd.Length < 13)
            {
                pc = 0;
                return false;
            }

            byte[] myIFHD = MakeIFHD(0);
            for (int i = 0; i < 10; i++)
                if (ifhd[i] != myIFHD[i])
                {
                    pc = 0;
                    return false;
                }

            pc = (ifhd[10] << 16) + (ifhd[11] << 8) + ifhd[12];
            return true;
        }

        private byte[] SerializeStacks()
        {
            List<byte> result = new List<byte>(stack.Count * 2 + callStack.Count * 24);

            short[] flatStack = stack.ToArray();
            CallFrame[] flatCallStack = callStack.ToArray();

            int sp = 0;

            // save dummy frame first (always, since we don't support V6)
            int dummyStackUsage;
            if (flatCallStack.Length == 0)
                dummyStackUsage = flatStack.Length;
            else
                dummyStackUsage = flatCallStack[flatCallStack.Length - 1].PrevStackDepth;

            // return PC
            result.Add(0);
            result.Add(0);
            result.Add(0);
            // flags
            result.Add(0);
            // result storage
            result.Add(0);
            // args supplied
            result.Add(0);
            // stack usage
            result.Add((byte)(dummyStackUsage >> 8));
            result.Add((byte)dummyStackUsage);
            // local variable values (none)
            // stack data
            for (sp = 0; sp < dummyStackUsage; sp++)
            {
                short value = flatStack[flatStack.Length - 1 - sp];
                result.Add((byte)(value >> 8));
                result.Add((byte)value);
            }

            // save call frames and their respective stacks
            for (int i = flatCallStack.Length - 1; i >= 0; i--)
            {
                CallFrame frame = flatCallStack[i];
                CallFrame nextFrame = (i == 0) ? null : flatCallStack[i - 1];

                // return PC
                result.Add((byte)(frame.ReturnPC >> 16));
                result.Add((byte)(frame.ReturnPC >> 8));
                result.Add((byte)frame.ReturnPC);
                // flags
                byte flags = (byte)(frame.Locals.Length);
                if (frame.ResultStorage == -1)
                    flags |= 16;
                result.Add(flags);
                // result storage
                if (frame.ResultStorage == -1)
                    result.Add(0);
                else
                    result.Add((byte)frame.ResultStorage);
                // args supplied
                byte argbits;
                switch (frame.ArgCount)
                {
                    case 1: argbits = 1; break;
                    case 2: argbits = 3; break;
                    case 3: argbits = 7; break;
                    case 4: argbits = 15; break;
                    case 5: argbits = 31; break;
                    case 6: argbits = 63; break;
                    case 7: argbits = 127; break;
                    default: argbits = 0; break;
                }
                result.Add(argbits);
                // stack usage
                int curDepth = (nextFrame == null) ? flatStack.Length : nextFrame.PrevStackDepth;
                int stackUsage = curDepth - frame.PrevStackDepth;
                result.Add((byte)(stackUsage >> 8));
                result.Add((byte)stackUsage);
                // local variable values
                for (int j = 0; j < frame.Locals.Length; j++)
                {
                    short value = frame.Locals[j];
                    result.Add((byte)(value >> 8));
                    result.Add((byte)value);
                }
                // stack data
                System.Diagnostics.Debug.Assert(sp == frame.PrevStackDepth);
                for (int j = 0; j < stackUsage; j++)
                {
                    short value = flatStack[flatStack.Length - 1 - sp];
                    sp++;
                    result.Add((byte)(value >> 8));
                    result.Add((byte)value);
                }
            }

            return result.ToArray();
        }

        private static void DeserializeStacks(byte[] stks, out Stack<short> savedStack,
            out Stack<CallFrame> savedCallStack)
        {
            savedStack = new Stack<short>();
            savedCallStack = new Stack<CallFrame>();

            try
            {
                int prevStackDepth = 0;
                int i = 0;

                while (i < stks.Length)
                {
                    // return PC
                    int returnPC = (stks[i] << 16) + (stks[i + 1] << 8) + stks[i + 2];
                    // flags
                    byte flags = stks[i + 3];
                    int numLocals = flags & 15;
                    // result storage
                    int resultStorage;
                    if ((flags & 16) != 0)
                        resultStorage = -1;
                    else
                        resultStorage = stks[i + 4];
                    // args supplied
                    byte argbits = stks[i + 5];
                    int argCount;
                    if ((argbits & 64) != 0)
                        argCount = 7;
                    else if ((argbits & 32) != 0)
                        argCount = 6;
                    else if ((argbits & 16) != 0)
                        argCount = 5;
                    else if ((argbits & 8) != 0)
                        argCount = 4;
                    else if ((argbits & 4) != 0)
                        argCount = 3;
                    else if ((argbits & 2) != 0)
                        argCount = 2;
                    else if ((argbits & 1) != 0)
                        argCount = 1;
                    else
                        argCount = 0;
                    // stack usage
                    int stackUsage = (stks[i + 6] << 8) + stks[i + 7];

                    // not done yet, but we know enough to create the frame
                    i += 8;
                    CallFrame frame = new CallFrame(
                        returnPC,
                        prevStackDepth,
                        numLocals,
                        argCount,
                        resultStorage);

                    // don't save the first frame on the call stack
                    if (i != 8)
                        savedCallStack.Push(frame);

                    // local variable values
                    for (int j = 0; j < numLocals; j++)
                    {
                        frame.Locals[j] = (short)((stks[i] << 8) + stks[i + 1]);
                        i += 2;
                    }
                    // stack data
                    for (int j = 0; j < stackUsage; j++)
                    {
                        savedStack.Push((short)((stks[i] << 8) + stks[i + 1]));
                        i += 2;
                    }
                    prevStackDepth += stackUsage;
                }
            }
            catch (IndexOutOfRangeException)
            {
                savedStack = null;
                savedCallStack = null;
            }
        }
    }

    internal class Quetzal : IffFile
    {
        private const string QUETZAL_TYPE = "IFZS";

        public Quetzal()
            : base(QUETZAL_TYPE)
        {
        }

        public Quetzal(Stream fromStream)
            : base(fromStream)
        {
            if (FileType != QUETZAL_TYPE)
                throw new ArgumentException("Not a Quetzal file");
        }

        private static readonly uint IFHD_TYPE_ID = StringToTypeID("IFhd");

        protected override int CompareBlocks(uint type1, uint type2, byte[] data1, byte[] data2,
            int index1, int index2)
        {
            // make sure IFhd is first, but leave other blocks in order
            if (type1 == IFHD_TYPE_ID && type2 != IFHD_TYPE_ID)
                return -1;
            else if (type2 == IFHD_TYPE_ID && type1 != IFHD_TYPE_ID)
                return 1;
            else
                return index1.CompareTo(index2);
        }
    }
}