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
using System.IO;
using System.Text;

namespace ZLR.VM
{
    partial class ZMachine
    {
        private void StoreResult(byte dest, short result)
        {
            if (dest == 0)
                stack.Push(result);
            else if (dest < 16)
                TopFrame.Locals[dest - 1] = result;
            else
                SetWord(globalsOffset + 2 * (dest - 16), result);
        }

        private void EnterFunctionImpl(short packedAddress, short[] args, int resultStorage, int returnPC)
        {
            if (packedAddress == 0)
            {
                if (resultStorage != -1)
                    StoreResult((byte)resultStorage, 0);
                pc = returnPC;
                return;
            }

            int address = UnpackAddress(packedAddress);
            byte numLocals = GetByte(address);

            CallFrame frame = new CallFrame(returnPC, stack.Count, numLocals,
                args == null ? 0 : args.Length, resultStorage);
            if (args != null)
                Array.Copy(args, frame.Locals, Math.Min(args.Length, numLocals));
            callStack.Push(frame);
            topFrame = frame;
            pc = address + 1;
        }

        private void LeaveFunctionImpl(short result)
        {
            CallFrame frame = callStack.Pop();
            SetTopFrame();

            // the stack can be deeper than it was on entry, but not shallower
            if (stack.Count < frame.PrevStackDepth)
                throw new Exception("Routine returned after using too much stack");

            while (stack.Count > frame.PrevStackDepth)
                stack.Pop();

            pc = frame.ReturnPC;
            if (frame.ResultStorage != -1)
                StoreResult((byte)frame.ResultStorage, result);
        }

        private void StoreVariableImpl(byte dest, short result)
        {
            if (dest == 0)
            {
                stack.Pop();
                stack.Push(result);
            }
            else if (dest < 16)
                TopFrame.Locals[dest - 1] = result;
            else
                SetWord(globalsOffset + 2 * (dest - 16), result);
        }

        private short LoadVariableImpl(byte num)
        {
            if (num == 0)
                return stack.Peek();
            else if (num < 16)
                return TopFrame.Locals[num - 1];
            else
                return GetWord(globalsOffset + 2 * (num - 16));
        }

        private short IncImpl(byte dest, short amount)
        {
            short result;
            if (dest == 0)
            {
                result = (short)(stack.Pop() + amount);
                stack.Push(result);
            }
            else if (dest < 16)
            {
                CallFrame frame = TopFrame;
                result = (short)(frame.Locals[dest - 1] + amount);
                frame.Locals[dest - 1] = result;
            }
            else
            {
                int address = globalsOffset + 2 * (dest - 16);
                result = (short)(GetWord(address) + amount);
                SetWord(address, result);
            }
            return result;
        }

        private short RandomImpl(short range)
        {
            if (predictableRng && range <= 0)
            {
                // don't change anything
                return 0;
            }

            if (range == 0)
            {
                rng = new Random();
                return 0;
            }

            if (range < 0)
            {
                rng = new Random(range);
                return 0;
            }

            return (short)(rng.Next(range) + 1);
        }

        private void SaveUndo(byte dest, int nextPC)
        {
            if (maxUndoDepth > 0)
            {
                UndoState curState = new UndoState(zmem, romStart, stack, callStack, nextPC, dest);
                if (undoStates.Count >= maxUndoDepth)
                    undoStates.RemoveAt(0);
                undoStates.Add(curState);

                StoreResult(dest, 1);
            }
            else
            {
                StoreResult(dest, 0);
            }
        }

        private void RestoreUndo(byte dest, int failurePC)
        {
            if (undoStates.Count == 0)
            {
                StoreResult(dest, 0);
                pc = failurePC;
            }
            else
            {
                int i = undoStates.Count - 1;
                UndoState lastState = undoStates[i];
                undoStates.RemoveAt(i);
                lastState.Restore(zmem, stack, callStack, out pc, out dest);
                SetTopFrame();
                ResetHeaderFields(false);
                StoreResult(dest, 2);
            }
        }

        private void SetTopFrame()
        {
            if (callStack.Count > 0)
                topFrame = callStack.Peek();
            else
                topFrame = null;
        }

        private void Restart()
        {
            gameFile.Seek(0, SeekOrigin.Begin);
            gameFile.Read(zmem, 0, (int)gameFile.Length);

            stack.Clear();
            callStack.Clear();
            topFrame = null;
            undoStates.Clear();

            ResetHeaderFields(false);
            io.EraseWindow(-1);

            pc = (ushort)GetWord(0x06);
        }

        private bool VerifyGameFile()
        {
            try
            {
                BinaryReader br = new BinaryReader(gameFile);
                gameFile.Seek(0x1A, SeekOrigin.Begin);
                ushort packedLength = (ushort)((br.ReadByte() << 8) + br.ReadByte());
                ushort idealChecksum = (ushort)((br.ReadByte() << 8) + br.ReadByte());

                int length = packedLength * (zversion == 5 ? 4 : 8);
                gameFile.Seek(0x40, SeekOrigin.Begin);
                byte[] data = br.ReadBytes(length);

                ushort actualChecksum = 0;
                foreach (byte b in data)
                    actualChecksum += b;

                return (idealChecksum == actualChecksum);
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static short LogShiftImpl(short a, short b)
        {
            if (b < 0)
                return (short)((ushort)a >> (-b));
            else
                return (short)(a << b);
        }

        private static short ArtShiftImpl(short a, short b)
        {
            if (b < 0)
                return (short)(a >> (-b));
            else
                return (short)(a << b);
        }

        private void ThrowImpl(short value, ushort catchingFrame)
        {
            while (callStack.Count > catchingFrame)
                callStack.Pop();

            SetTopFrame();
            LeaveFunctionImpl(value);
        }

        private short SaveAuxiliary(ushort table, ushort bytes, ushort nameAddr)
        {
            byte nameLen = GetByte(nameAddr);
            byte[] nameBuffer = new byte[nameLen];
            GetBytes(nameAddr + 1, nameLen, nameBuffer, 0);

            string name = Encoding.ASCII.GetString(nameBuffer);
            byte[] data = new byte[bytes];
            GetBytes(table, bytes, data, 0);

            using (Stream stream = io.OpenAuxiliaryFile(name, bytes, true))
            {
                if (stream == null)
                    return 0;

                try
                {
                    stream.Write(data, 0, bytes);
                    return 1;
                }
                catch (IOException)
                {
                    return 0;
                }
            }
        }

        private ushort RestoreAuxiliary(ushort table, ushort bytes, ushort nameAddr)
        {
            byte nameLen = GetByte(nameAddr);
            byte[] nameBuffer = new byte[nameLen];
            GetBytes(nameAddr + 1, nameLen, nameBuffer, 0);

            string name = Encoding.ASCII.GetString(nameBuffer);
            byte[] data = new byte[bytes];

            using (Stream stream = io.OpenAuxiliaryFile(name, bytes, false))
            {
                if (stream == null)
                    return 0;

                try
                {
                    int count = stream.Read(data, 0, bytes);
                    SetBytes(table, count, data, 0);
                    return (ushort)count;
                }
                catch (IOException)
                {
                    return 0;
                }
            }
        }

        private ushort ScanTableImpl(short x, ushort table, ushort tableLen, byte form)
        {
            if (form == 0)
                form = 0x82;

            bool words = ((form & 128) != 0);
            int entryLen = form & 127;

            if (entryLen == 0)
                return 0;

            if (words)
            {
                for (int i = 0; i < tableLen; i += entryLen)
                    if (GetWord(table + i) == x)
                        return (ushort)(table + i);
            }
            else
            {
                byte b = (byte)x;
                for (int i = 0; i < tableLen; i += entryLen)
                    if (GetByte(table + i) == b)
                        return (ushort)(table + i);
            }

            return 0;
        }

        private void CopyTableImpl(ushort first, ushort second, short size)
        {
            if (second == 0)
            {
                ZeroMemory(first, size);
                return;
            }

            bool forceForward = false;
            if (size < 0)
            {
                forceForward = true;
                size = (short)-size;
            }

            if (first > second || forceForward)
            {
                for (int i = 0; i < size; i++)
                    SetByte(second + i, GetByte(first + i));
            }
            else
            {
                for (int i = size - 1; i >= 0; i--)
                    SetByte(second + i, GetByte(first + i));
            }

            TrapMemory(second, (ushort)size);
        }

        private void ZeroMemory(ushort address, short size)
        {
            for (int i = 0; i < size; i++)
                SetByte(address + i, 0);

            TrapMemory(address, (ushort)size);
        }

        private void SoundEffectImpl(ushort number, short effect, ushort volRepeats, ushort routine)
        {
            if (effect == 0)
            {
                switch (number)
                {
                    case 0:
                    case 1:
                        io.PlayBeep(true);
                        break;

                    case 2:
                        io.PlayBeep(false);
                        break;
                }
            }
            else
            {
                io.PlaySoundSample(number, (SoundAction)effect, (byte)volRepeats, (byte)(volRepeats >> 8),
                    delegate { HandleSoundFinished(routine); });
            }
        }

        private void PrintTableImpl(ushort table, short width, short height, short skip)
        {
            if (height == 0)
                height = 1;

            string[] lines = new string[height];
            int ptr = table;

            for (int y = 0; y < height; y++)
            {
                StringBuilder sb = new StringBuilder(width);
                for (int x = 0; x < width; x++)
                    sb.Append(CharFromZSCII(GetByte(ptr + x)));
                lines[y] = sb.ToString();
                ptr += width + skip;
            }

            io.PutTextRectangle(lines);
        }

        private void EncodeTextImpl(ushort buffer, ushort length, ushort start, ushort dest)
        {
            byte[] text = new byte[length];
            for (int i = 0; i < length; i++)
                text[i] = GetByte(buffer + i);

            byte[] result = EncodeText(text, 0, length, DICT_WORD_SIZE);
            for (int i = 0; i < result.Length; i++)
                SetByte(dest + i, result[i]);
        }
    }
}