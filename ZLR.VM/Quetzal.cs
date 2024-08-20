using System.IO;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using ZLR.IFF;
using ZLR.VM.Debugging;

namespace ZLR.VM
{
    public partial class ZMachine
    {
        internal async Task SaveQuetzalAndStoreAsync(int savedPC, int resultStorage, int retryPC, int nextPC)
        {
            try
            {
                var saved = await SaveQuetzalAsync(savedPC).ConfigureAwait(false);

                if (resultStorage >= 0)
                    StoreResult((byte)resultStorage, (short)(saved ? 1 : 0));

                pc = nextPC;
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == interruptToken)
            {
                pc = retryPC;
                debugState = DebuggerState.PausedByUser;
                throw new DebuggerBreakException();
            }
        }

        internal async Task SaveQuetzalAndBranchAsync(int savedPC, bool branchIfTrue, int branchOffset, int retryPC,
            int nextPC)
        {
            try
            {
                var saved = await SaveQuetzalAsync(savedPC).ConfigureAwait(false);

                pc = nextPC;

                if (branchIfTrue == saved)
                    BranchImpl(branchOffset);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == interruptToken)
            {
                pc = retryPC;
                debugState = DebuggerState.PausedByUser;
                throw new DebuggerBreakException();
            }
        }

        internal async Task<bool> SaveQuetzalAsync(int savedPC)
        {
            var quetzal = new Quetzal();
            var cancellationToken = interruptToken;

            // savedPC points to the result storage byte (V3) or branch offset (V4+) of the save instruction
            quetzal.AddBlock("IFhd", MakeIFHD(savedPC));
            quetzal.AddBlock("CMem", CompressRAM());
            quetzal.AddBlock("Stks", SerializeStacks());

            cancellationToken.ThrowIfCancellationRequested();

            BeginExternalWait();
            var inWait = true;
            try
            {
                using var stream = await io.OpenSaveFileAsync(quetzal.Length, cancellationToken).ConfigureAwait(false);
                EndExternalWait();
                inWait = false;

                cancellationToken.ThrowIfCancellationRequested();

                if (stream == null)
                {
                    return false;
                }
                else
                {
                    try
                    {
                        await quetzal.WriteToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            finally
            {
                if (inWait)
                    EndExternalWait();
            }
        }

        internal bool RestoreQuetzal(int failurePC)
        {
            // there are many ways this can go wrong, so let's just assume it will.
            // if the restore succeeds, what we change up here won't matter anyway.
            pc = failurePC;

            using var stream = io.OpenRestoreFile();
            if (stream == null)
                return false;

            try
            {
                var quetzal = new Quetzal(stream);

                // verify everything first
                var ifhd = quetzal.GetBlock("IFhd");
                if (ifhd == null || !VerifyIFHD(ifhd, out var savedPC))
                    return false;

                var compressedMem = quetzal.GetBlock("CMem");
                var uncompressedMem = compressedMem != null ? UncompressRAM(compressedMem) : quetzal.GetBlock("UMem");

                if (uncompressedMem == null || uncompressedMem.Length != RomStart)
                    return false;

                var stks = quetzal.GetBlock("Stks");
                if (stks == null)
                    return false;

                DeserializeStacks(stks, out var savedStack, out var savedCallStack);
                if (savedStack == null || savedCallStack == null)
                    return false;

                // ok, restore it
                SetBytes(0, uncompressedMem.Length, uncompressedMem, 0);
                stack = savedStack;
                callStack = savedCallStack;
                SetTopFrame();
                pc = savedPC;

                if (ZVersion < 4)
                {
                    DecodeBranch(out var branchIfTrue, out var branchOffset);
                    if (branchIfTrue)
                        pc += branchOffset - 2;
                }
                else
                {
                    // savedPC points to the save instruction's result storage byte
                    var dest = GetByte(pc++);
                    StoreResult(dest, 2);
                }

                ResetHeaderFields();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private byte[] CompressRAM()
        {
            var origRam = new byte[RomStart];
            gameFile.Seek(0, SeekOrigin.Begin);
            gameFile.Read(origRam, 0, RomStart);

            var result = new List<byte>(RomStart);
            int i = 0, lastNonZero = 0;
            while (i < RomStart)
            {
                var b = (byte)(GetByte(i) ^ origRam[i]);
                if (b == 0)
                {
                    var runLength = 1;
                    i++;
                    while (i < RomStart && GetByte(i) == origRam[i] && runLength < 256)
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
                    lastNonZero = result.Count;
                }
            }

            // remove trailing zeros
            if (result.Count > lastNonZero)
                result.RemoveRange(lastNonZero, result.Count - lastNonZero);

            return result.ToArray();
        }

        private byte[]? UncompressRAM(byte[] cmem)
        {
            var result = new byte[RomStart];
            gameFile.Seek(0, SeekOrigin.Begin);
            gameFile.Read(result, 0, RomStart);

            var rp = 0;
            try
            {
                for (var i = 0; i < cmem.Length; i++)
                {
                    var b = cmem[i];
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

        private byte[] MakeIFHD(int savePC)
        {
            var result = new byte[13];

            var br = new BinaryReader(gameFile);

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
            result[10] = (byte)(savePC >> 16);
            result[11] = (byte)(savePC >> 8);
            result[12] = (byte)savePC;

            return result;
        }

        private bool VerifyIFHD(byte[] ifhd, out int savedPC)
        {
            if (ifhd.Length < 13)
            {
                savedPC = 0;
                return false;
            }

            var myIFHD = MakeIFHD(0);
            for (var i = 0; i < 10; i++)
            {
                if (ifhd[i] == myIFHD[i])
                    continue;

                savedPC = 0;
                return false;
            }

            savedPC = (ifhd[10] << 16) + (ifhd[11] << 8) + ifhd[12];
            return true;
        }

        private byte[] SerializeStacks()
        {
            var result = new List<byte>(stack.Count * 2 + callStack.Count * 24);

            var flatStack = stack.ToArray();
            var flatCallStack = callStack.ToArray();

            int sp;

            // save dummy frame first (always, since we don't support V6)
            var dummyStackUsage = flatCallStack.Length == 0
                ? flatStack.Length
                : flatCallStack[^1].PrevStackDepth;

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
                var value = flatStack[flatStack.Length - 1 - sp];
                result.Add((byte)(value >> 8));
                result.Add((byte)value);
            }

            // save call frames and their respective stacks
            for (var i = flatCallStack.Length - 1; i >= 0; i--)
            {
                var frame = flatCallStack[i];
                var nextFrame = i == 0 ? null : flatCallStack[i - 1];

                // return PC
                result.Add((byte)(frame.ReturnPC >> 16));
                result.Add((byte)(frame.ReturnPC >> 8));
                result.Add((byte)frame.ReturnPC);
                // flags
                var flags = (byte)frame.Locals.Length;
                if (frame.ResultStorage == -1)
                    flags |= 16;
                result.Add(flags);
                // result storage
                if (frame.ResultStorage == -1)
                    result.Add(0);
                else
                    result.Add((byte)frame.ResultStorage);
                byte argbits = frame.ArgCount switch
                {
                    1 => 1,
                    2 => 3,
                    3 => 7,
                    4 => 15,
                    5 => 31,
                    6 => 63,
                    7 => 127,
                    _ => 0,
                };
                result.Add(argbits);
                // stack usage
                var curDepth = nextFrame?.PrevStackDepth ?? flatStack.Length;
                var stackUsage = curDepth - frame.PrevStackDepth;
                result.Add((byte)(stackUsage >> 8));
                result.Add((byte)stackUsage);
                // local variable values
                foreach (var value in frame.Locals)
                {
                    result.Add((byte)(value >> 8));
                    result.Add((byte)value);
                }
                // stack data
                System.Diagnostics.Debug.Assert(sp == frame.PrevStackDepth);
                for (var j = 0; j < stackUsage; j++)
                {
                    var value = flatStack[^(1 - sp)];
                    sp++;
                    result.Add((byte)(value >> 8));
                    result.Add((byte)value);
                }
            }

            return result.ToArray();
        }

        private static void DeserializeStacks(byte[] stks, out Stack<short>? savedStack,
            out Stack<CallFrame>? savedCallStack)
        {
            savedStack = new Stack<short>();
            savedCallStack = new Stack<CallFrame>();

            try
            {
                var prevStackDepth = 0;
                var i = 0;

                while (i < stks.Length)
                {
                    // return PC
                    var returnPC = (stks[i] << 16) + (stks[i + 1] << 8) + stks[i + 2];
                    // flags
                    var flags = stks[i + 3];
                    var numLocals = flags & 15;
                    // result storage
                    int resultStorage;
                    if ((flags & 16) != 0)
                        resultStorage = -1;
                    else
                        resultStorage = stks[i + 4];
                    // args supplied
                    var argbits = stks[i + 5];
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
                    var stackUsage = (stks[i + 6] << 8) + stks[i + 7];

                    // not done yet, but we know enough to create the frame
                    i += 8;
                    var frame = new CallFrame(
                        returnPC,
                        prevStackDepth,
                        numLocals,
                        argCount,
                        resultStorage);

                    // don't save the first frame on the call stack
                    if (i != 8)
                        savedCallStack.Push(frame);

                    // local variable values
                    for (var j = 0; j < numLocals; j++)
                    {
                        frame.Locals[j] = (short)((stks[i] << 8) + stks[i + 1]);
                        i += 2;
                    }
                    // stack data
                    for (var j = 0; j < stackUsage; j++)
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
                throw new ArgumentException("Not a Quetzal file", nameof(fromStream));
        }

        private static readonly uint IFHD_TYPE_ID = StringToTypeID("IFhd");

        protected override int CompareBlocks((uint type, byte[] data, int index) block1, (uint type, byte[] data, int index) block2)
        {
            // make sure IFhd is first, but leave other blocks in order
            if (block1.type == IFHD_TYPE_ID && block2.type != IFHD_TYPE_ID)
                return -1;

            if (block2.type == IFHD_TYPE_ID && block1.type != IFHD_TYPE_ID)
                return 1;

            return block1.index.CompareTo(block2.index);
        }
    }
}