using System;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ZLR.VM
{
    partial class ZMachine
    {
        /// <summary>
        /// Implements the store semantics used for most store instructions.
        /// </summary>
        /// <param name="dest">The destination variable index, or 0 to store to the stack.</param>
        /// <param name="result">The value to store.</param>
        /// <remarks>
        /// Unlike <see cref="StoreVariableImpl"/>, this will push a new value onto the stack
        /// instead of replacing the existing value.
        /// </remarks>
        void StoreResult(byte dest, short result)
        {
            if (dest == 0)
                stack.Push(result);
            else if (dest < 16)
                TopFrame.Locals[dest - 1] = result;
            else
                SetWord(GlobalsOffset + 2 * (dest - 16), result);
        }

        internal void EnterFunctionImpl(short packedAddress, short[]? args, int resultStorage, int returnPC)
        {
            if (debugging)
            {
                HandleEnterFunction(packedAddress, args, resultStorage, returnPC);
            }

            if (packedAddress == 0)
            {
                if (resultStorage != -1)
                    StoreResult((byte)resultStorage, 0);
                pc = returnPC;
                return;
            }

            var address = UnpackAddress(packedAddress, false);
            var numLocals = GetByte(address);
            address++;

            var frame = new CallFrame(returnPC, stack.Count, numLocals,
                args?.Length ?? 0, resultStorage);

            // read initial local variable values for V1-V4
            if (zversion <= 4)
            {
                for (var i = 0; i < numLocals; i++)
                {
                    frame.Locals[i] = GetWord(address);
                    address += 2;
                }
            }

            if (args != null)
                Array.Copy(args, frame.Locals, Math.Min(args.Length, numLocals));
            callStack.Push(frame);
            TopFrame = frame;
            pc = address;
        }

        internal void LeaveFunctionImpl(short result)
        {
            var frame = callStack.Pop();
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

        private void BranchImpl(int branchOffset)
        {
            switch (branchOffset)
            {
                case 0:
                    LeaveFunctionImpl(0);
                    break;
                case 1:
                    LeaveFunctionImpl(1);
                    break;
                default:
                    pc += branchOffset - 2;
                    break;
            }
        }

#pragma warning disable 0169
        /// <summary>
        /// Implements the store semantics used by @store and @pull.
        /// </summary>
        /// <param name="dest">The destination variable index, or 0 to store to the stack.</param>
        /// <param name="result">The value to store.</param>
        /// <remarks>
        /// Unlike <see cref="StoreResult"/>, this will replace the top value on the stack
        /// instead of pushing a new value.
        /// </remarks>
        internal void StoreVariableImpl(byte dest, short result)
        {
            if (dest == 0)
            {
                stack.Pop();
                stack.Push(result);
            }
            else if (dest < 16)
            {
                TopFrame.Locals[dest - 1] = result;
            }
            else
            {
                SetWord(GlobalsOffset + 2 * (dest - 16), result);
            }
        }

        internal short LoadVariableImpl(byte num)
        {
            if (num == 0)
                return stack.Peek();

            if (num < 16)
                return this.TopFrame.Locals[num - 1];

            return GetWord(this.GlobalsOffset + 2 * (num - 16));
        }

        internal short IncImpl(byte dest, short amount)
        {
            short result;
            if (dest == 0)
            {
                result = (short)(stack.Pop() + amount);
                stack.Push(result);
            }
            else if (dest < 16)
            {
                var frame = TopFrame;
                result = (short)(frame.Locals[dest - 1] + amount);
                frame.Locals[dest - 1] = result;
            }
            else
            {
                var address = GlobalsOffset + 2 * (dest - 16);
                result = (short)(GetWord(address) + amount);
                SetWord(address, result);
            }
            return result;
        }

        internal short RandomImpl(short range)
        {
            short? result = null;

            // try raising RandomNeeded
            var handler = RandomNeeded;
            if (handler != null)
            {
                var eventArgs = new RandomNeededEventArgs(range);
                handler(this, eventArgs);

                if (eventArgs.Value != null)
                    result = (short)eventArgs.Value;
            }

            // if that didn't work, use the internal RNG
            if (result == null)
            {
                if (predictableRng && range <= 0)
                {
                    // don't change anything
                    result = 0;
                }
                else if (range == 0)
                {
                    rng = new Random();
                    result = 0;
                }
                else if (range < 0)
                {
                    rng = new Random(range);
                    result = 0;
                }
                else
                {
                    result = (short)(rng.Next(range) + 1);
                }
            }
            System.Diagnostics.Debug.Assert(result != null);

            // log it via RandomRolled
            var handler2 = RandomRolled;
            if (handler2 != null)
            {
                // ReSharper disable once PossibleInvalidOperationException
                var eventArgs = new RandomRolledEventArgs((short)result, range);
                handler2(this, eventArgs);
            }

            // ReSharper disable once PossibleInvalidOperationException
            return (short)result;
        }

        internal void SaveUndo(byte dest, int nextPC)
        {
            if (maxUndoDepth > 0)
            {
                var curState = new UndoState(zmem, RomStart, stack, callStack, nextPC, dest);
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

        internal void RestoreUndo(byte dest, int failurePC)
        {
            if (undoStates.Count == 0)
            {
                StoreResult(dest, 0);
                pc = failurePC;
            }
            else
            {
                var i = undoStates.Count - 1;
                var lastState = undoStates[i];
                undoStates.RemoveAt(i);
                lastState.Restore(zmem, stack, callStack, out pc, out dest);
                SetTopFrame();
                ResetHeaderFields();
                StoreResult(dest, 2);
            }
        }
#pragma warning restore 0169

        private void SetTopFrame()
        {
            TopFrame = callStack.Count > 0 ? callStack.Peek() : null;
        }

        internal void Restart()
        {
            gameFile.Seek(0, SeekOrigin.Begin);
            gameFile.Read(zmem, 0, (int)gameFile.Length);

            stack.Clear();
            callStack.Clear();
            TopFrame = null;
            undoStates.Clear();

            ResetHeaderFields();
            io.EraseWindow(-1);

            pc = (ushort)GetWord(0x06);
        }

#pragma warning disable 0169
        internal bool VerifyGameFile()
        {
            try
            {
                var br = new BinaryReader(gameFile);
                gameFile.Seek(0x1A, SeekOrigin.Begin);
                var packedLength = (ushort)((br.ReadByte() << 8) + br.ReadByte());
                var idealChecksum = (ushort)((br.ReadByte() << 8) + br.ReadByte());

                var length = packedLength * (zversion == 5 ? 4 : 8);
                gameFile.Seek(0x40, SeekOrigin.Begin);
                var data = br.ReadBytes(length);

                ushort actualChecksum = 0;
                foreach (var b in data)
                    actualChecksum += b;

                return idealChecksum == actualChecksum;
            }
            catch (IOException)
            {
                return false;
            }
        }

        internal static short LogShiftImpl(short a, short b)
        {
            return b < 0 ? (short) ((ushort) a >> -b) : (short) (a << b);
        }

        internal static short ArtShiftImpl(short a, short b)
        {
            return b < 0 ? (short) (a >> -b) : (short) (a << b);
        }

        internal void ThrowImpl(short value, ushort catchingFrame)
        {
            while (callStack.Count > catchingFrame)
                callStack.Pop();

            SetTopFrame();
            LeaveFunctionImpl(value);
        }

        internal short SaveAuxiliary(ushort table, ushort bytes, ushort nameAddr)
        {
            var name = GetLengthPrefixedZSCIIString(nameAddr);
#if HAVE_SPAN
            var span = GetSpan(table, bytes);
#endif
#if !HAVE_SPAN
            var data = new byte[bytes];
            GetBytes(table, bytes, data, 0);
#endif

            // TODO: asyncify SaveAuxiliary
            using var stream = io.OpenAuxiliaryFileAsync(name, bytes, true, interruptToken).GetAwaiter().GetResult();
            if (stream == null)
                return 0;

            try
            {
#if HAVE_SPAN
                stream.Write(span);
#endif
#if !HAVE_SPAN
                stream.Write(data, 0, data.Length);
#endif
                return 1;
            }
            catch (IOException)
            {
                return 0;
            }
        }

        [NotNull]
        internal string GetLengthPrefixedZSCIIString(ushort address)
        {
            var length = GetByte(address);

#if HAVE_SPAN
            return string.Create<object?>(length, default, (chars, _) =>
            {
                var zbytes = GetSpan(address + 1, chars.Length);

                for (var i = 0; i < chars.Length; i++)
                    chars[i] = CharFromZSCII(zbytes[i]);
            });
#endif
#if !HAVE_SPAN
            var buffer = new byte[length];
            GetBytes(address + 1, length, buffer, 0);
            return Encoding.ASCII.GetString(buffer);
#endif
        }

        internal ushort RestoreAuxiliary(ushort table, ushort bytes, ushort nameAddr)
        {
            var name = GetLengthPrefixedZSCIIString(nameAddr);
#if HAVE_SPAN
            var span = GetSpan(table, bytes);
#endif
#if !HAVE_SPAN
            var data = new byte[bytes];
#endif

            // TODO: asyncify RestoreAuxiliary
            using var stream = io.OpenAuxiliaryFileAsync(name, bytes, false, interruptToken).GetAwaiter().GetResult();
            if (stream == null)
                return 0;

            try
            {
#if HAVE_SPAN
                var count = stream.Read(span);
#endif
#if !HAVE_SPAN
                var count = stream.Read(data, 0, bytes);
                SetBytes(table, count, data, 0);
#endif
                return (ushort)count;
            }
            catch (IOException)
            {
                return 0;
            }
        }

        internal int ScanTableImpl(short x, ushort table, ushort tableLen, byte form)
        {
            if (form == 0)
                form = 0x82;

            var words = (form & 128) != 0;
            var entryLen = form & 127;

            if (entryLen == 0)
                return 0;

            if (words)
            {
                for (var i = 0; i < tableLen; i++)
                    if (GetWord(table + i * entryLen) == x)
                        return 0x10000 | (table + i * entryLen);
            }
            else
            {
#if HAVE_SPAN
                var span = GetSpan(table, tableLen * entryLen);
                for (var i = 0; i < tableLen; i++)
                    if (span[i * entryLen] == x)
                        return 0x10000 | (table + i * entryLen);
#endif
#if !HAVE_SPAN
                for (var i = 0; i < tableLen; i++)
                    if (GetByte(table + i * entryLen) == x)
                        return 0x10000 | (table + i * entryLen);
#endif
            }

            return 0;
        }

#if HAVE_SPAN
        internal void CopyTableImpl(ushort first, ushort second, short size)
        {
            if (second == 0)
            {
                var span = GetSpan(first, size);
                span.Fill(0);
                return;
            }

            var forceForward = false;
            if (size < 0)
            {
                forceForward = true;
                size = (short) -size;
            }

            var src = GetSpan(first, size);
            var dest = GetSpan(second, size);

            if (forceForward)
            {
                for (var i = 0; i < dest.Length; i++)
                    dest[i] = src[i];
            }
            else
            {
                src.CopyTo(dest);
            }

            TrapMemory(second, (ushort)size);
        }
#endif
#if !HAVE_SPAN
        internal void CopyTableImpl(ushort first, ushort second, short size)
        {
            if (second == 0)
            {
                ZeroMemory(first, size);
                return;
            }

            var forceForward = false;
            if (size < 0)
            {
                forceForward = true;
                size = (short)-size;
            }

            if (first > second || forceForward)
            {
                for (var i = 0; i < size; i++)
                    SetByte(second + i, GetByte(first + i));
            }
            else
            {
                for (var i = size - 1; i >= 0; i--)
                    SetByte(second + i, GetByte(first + i));
            }

            TrapMemory(second, (ushort)size);
        }
#endif
#pragma warning restore 0169

        internal void ZeroMemory(ushort address, short size)
        {
#if HAVE_SPAN
            var span = GetSpan(address, size);
            span.Fill(0);
#endif
#if !HAVE_SPAN
            for (var i = 0; i < size; i++)
                SetByte(address + i, 0);
#endif

            TrapMemory(address, (ushort)size);
        }

#pragma warning disable 0169
        internal void SoundEffectImpl(ushort number, short effect, ushort volRepeats, ushort routine)
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

        internal void PrintTableImpl(ushort table, short width, short height, short skip)
        {
            if (height == 0)
                height = 1;

            var lines = new string[height];
            int ptr = table;

            for (var y = 0; y < height; y++)
            {
                var sb = new StringBuilder(width);
                for (var x = 0; x < width; x++)
                    sb.Append(CharFromZSCII(GetByte(ptr + x)));
                lines[y] = sb.ToString();
                ptr += width + skip;
            }

            io.PutTextRectangle(lines);
        }

#if HAVE_SPAN
        internal void EncodeTextImpl(ushort buffer, ushort length, ushort start, ushort dest)
        {
            var src = GetSpan(buffer + start, length);
            var destSpan = GetSpan(dest, DictWordSizeInBytes);

            EncodeText(src, destSpan);
            TrapMemory(dest, (ushort)this.DictWordSizeInBytes);
        }
#endif
#if !HAVE_SPAN
        internal void EncodeTextImpl(ushort buffer, ushort length, ushort start, ushort dest)
        {
            var text = new byte[length];
            for (var i = 0; i < length; i++)
                text[i] = GetByte(buffer + start + i);

            var result = EncodeText(text, 0, length, DictWordSizeInZchars);
            for (var i = 0; i < result.Length; i++)
                SetByte(dest + i, result[i]);
        }
#endif

        private void PadStatusLine(int spacesToLeave)
        {
            io.GetCursorPos(out var x, out _);

            var width = io.WidthChars;

            while (x < width - spacesToLeave)
            {
                io.PutChar(' ');
                x++;
            }
        }

        internal void ShowStatusImpl()
        {
            if (zversion > 3)
                return;

            var locationStr = GetObjectName((ushort) GetWord(GlobalsOffset));
            var hoursOrScore = GetWord(GlobalsOffset + 2);
            var minsOrTurns = GetWord(GlobalsOffset + 4);

            bool useTime;
            if (zversion < 3)
            {
                useTime = false;
            }
            else
            {
                var flags1 = GetByte(0x1);
                useTime = (flags1 & 2) != 0;
            }

            // let the I/O module intercept the status line request...
            if (io.DrawCustomStatusLine(locationStr, hoursOrScore, minsOrTurns, useTime) == false)
            {
                // ... if it's not intercepted, draw it ourselves using Frotz's format

                // select upper window and turn on reverse video
                io.SelectWindow(1);
                io.MoveCursor(1, 1);
                io.SetTextStyle(TextStyle.Reverse);

                // use the brief format if the screen is too narrow
                var brief = io.WidthChars < 55;

                // print the location indented one space
                io.PutChar(' ');
                io.PutString(locationStr);

                // move over and print the score/turns or time
                if (useTime)
                {
                    PadStatusLine(brief ? 15 : 20);

                    io.PutString("Time: ");

                    var dispHrs = (hoursOrScore + 11) % 12 + 1;
                    if (dispHrs < 10)
                        io.PutChar(' ');
                    io.PutString(dispHrs.ToString());

                    io.PutChar(':');

                    if (minsOrTurns < 10)
                        io.PutChar('0');
                    io.PutString(minsOrTurns.ToString());

                    io.PutString(hoursOrScore >= 12 ? " pm" : " am");
                }
                else
                {
                    PadStatusLine(brief ? 15 : 30);

                    io.PutString(brief ? "S: " : "Score: ");
                    io.PutString(hoursOrScore.ToString());

                    PadStatusLine(brief ? 8 : 14);

                    io.PutString(brief ? "M: " : "Moves: ");
                    io.PutString(minsOrTurns.ToString());
                }

                // fill the rest of the line with spaces
                PadStatusLine(0);

                // return to the lower window
                io.SetTextStyle(TextStyle.Roman);
                io.SelectWindow(0);
            }
        }

        internal short PullFromUserStack(ushort userStack)
        {
            var freeSlots = GetWord(userStack);
            freeSlots++;
            var result = GetWord(userStack + 2 * freeSlots);
            SetWord(userStack, freeSlots);
            return result;
        }

        internal bool PushOntoUserStack(short value, ushort userStack)
        {
            var freeSlots = GetWord(userStack);
            if (freeSlots <= 0) return false;
            SetWord(userStack + 2 * freeSlots, value);
            SetWord(userStack, (short)(freeSlots - 1));
            return true;
        }

        internal void PopUserStack(short count, ushort userStack)
        {
            var freeSlots = GetWord(userStack);
            SetWord(userStack, (short)(freeSlots + count));
        }

        internal void PopStack(short count)
        {
            for (var i = 0; i < count; i++)
                stack.Pop();
        }
#pragma warning restore 0169
    }
}
