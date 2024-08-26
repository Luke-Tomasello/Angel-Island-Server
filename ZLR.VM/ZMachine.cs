/***************************************************************************
 *
 *   ZLR                     : May 1, 2007
 *   implementation          : (C) 2007-2023 Tara McGrew
 *   repository url          : https://foss.heptapod.net/zilf/zlr
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

// define TRACING to see every opcode
//#define TRACING

// define DISABLE_CACHE to test compilation speed
//#define DISABLE_CACHE

// define BENCHMARK to count instructions and see a performance report after the game ends
//#define BENCHMARK

#if TRACING
#define DISABLE_CACHE
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ZLR.IFF;
using ZLR.VM.Debugging;

using SystemDebugger = System.Diagnostics.Debugger;

namespace ZLR.VM
{
    [PublicAPI]
    public sealed class RandomNeededEventArgs : EventArgs
    {
        public RandomNeededEventArgs(short range)
        {
            Range = range;
        }

        public short Range { get; private set; }
        public short? Value { get; set; }
    }

    [PublicAPI]
    public sealed class RandomRolledEventArgs : EventArgs
    {
        public RandomRolledEventArgs(short value, short range)
        {
            Value = value;
            Range = range;
        }

        public short Value { get; private set; }
        public short Range { get; private set; }
    }

    partial class ZMachine
    {
        public static readonly string ZLR_VERSION = "0.07"; //XXX read from assembly

        private class CachedCode
        {
            public readonly int NextPC;
            public readonly ZCodeDelegate Code;
#if BENCHMARK
            public int Cycles;
#endif

            public CachedCode(int nextPC, ZCodeDelegate code)
            {
                NextPC = nextPC;
                Code = code;
#if BENCHMARK
                this.Cycles = 0;
#endif
            }
        }

        // compilation state
        readonly byte zversion;
        int objectTable, dictionaryTable, abbrevTable;
        bool compiling;
        ILGenerator il = default!;
        LocalBuilder? tempArrayLocal, tempWordLocal;
        LruCache<int, CachedCode> cache = default!;
        int cacheSize = DEFAULT_CACHE_SIZE;
        int maxUndoDepth = DEFAULT_MAX_UNDO_DEPTH;

        // compilation and runtime state
        internal int pc;
        bool clearable;
        bool debugging;
        CancellationToken interruptToken;

        // runtime state
        readonly Stream gameFile;
        internal bool running;
        readonly byte[] zmem;
        internal readonly IAsyncZMachineIO io;
        CommandFileReader? cmdRdr;
        CommandFileWriter? cmdWtr;
        Stack<short> stack = new Stack<short>();

        internal Stack<CallFrame> callStack = new Stack<CallFrame>();

        Random rng = new Random();
        bool predictableRng;
        byte[] wordSeparators = default!;
        int codeStart, stringStart; // V6-7

        readonly List<UndoState> undoStates = new List<UndoState>();

        bool normalOutput;

        readonly Stack<(ushort address, List<byte> buffer)> tableOutputStack = new Stack<(ushort address, List<byte> buffer)>();
        bool TableOutputEnabled => tableOutputStack.Count != 0;

        char[] alphabet0 = DefaultAlphabet0, alphabet1 = DefaultAlphabet1, alphabet2 = DefaultAlphabet2, extraChars = DefaultExtraChars;
        byte[] terminatingChars = default!;

        readonly MemoryTraps traps = new MemoryTraps();

#if BENCHMARK
        long cycles;
        int startTime, waitStartTime;
        int creditedTime;
        int cacheHits, cacheMisses;
#endif

        const int DEFAULT_MAX_UNDO_DEPTH = 3;
        const int DEFAULT_CACHE_SIZE = 35000;

        [Obsolete("Use the IAsyncZMachineIO constructor instead.")]
        public ZMachine([NotNull] Stream gameStream, [NotNull] IZMachineIO io)
            : this(gameStream, AsyncZMachineIOAdapter.Wrap(io))
        {
        }

        /// <summary>
        /// Initializes a new instance of the ZLR engine from a given stream.
        /// The stream must remain open while the engine is in use.
        /// </summary>
        /// <param name="gameStream">A stream containing either a plain Z-code
        /// file or a Blorb file which in turn contains a Z-code resource.</param>
        /// <param name="io"></param>
        public ZMachine([NotNull] Stream gameStream, [NotNull] IAsyncZMachineIO io)
        {
            if (gameStream == null)
                throw new ArgumentNullException(nameof(gameStream));
            this.io = io ?? throw new ArgumentNullException(nameof(io));

            // check for Blorb
            var temp = new byte[12];
            gameStream.Seek(0, SeekOrigin.Begin);
            gameStream.Read(temp, 0, 12);
            if (temp[0] == 'F' && temp[1] == 'O' && temp[2] == 'R' && temp[3] == 'M' &&
                temp[8] == 'I' && temp[9] == 'F' && temp[10] == 'R' && temp[11] == 'S')
            {
                var blorb = new Blorb(gameStream);
                if (blorb.GetStoryType() == "ZCOD")
                {
                    gameStream = blorb.GetStoryStream();
                    System.Diagnostics.Debug.Assert(gameStream != null, "gameStream != null");
                }
                else
                {
                    throw new ArgumentException("Not a Z-code Blorb", nameof(gameStream));
                }
            }

            gameFile = gameStream;

            // ReSharper disable once PossibleNullReferenceException
            zmem = new byte[gameStream.Length];
            gameStream.Seek(0, SeekOrigin.Begin);
            gameStream.Read(zmem, 0, (int)gameStream.Length);

            if (zmem.Length < 64)
                throw new ArgumentException("Z-code file is too short: must be at least 64 bytes", nameof(gameStream));

            zversion = zmem[0];

            if (zversion < 1 || zversion > 8)
                throw new ArgumentException("Z-code version must be between 1 and 8", nameof(gameStream));

            io.SizeChanged += IOSizeChanged;
        }

        [PublicAPI]
        public event EventHandler<RandomNeededEventArgs>? RandomNeeded;
        [PublicAPI]
        public event EventHandler<RandomRolledEventArgs>? RandomRolled;

        [PublicAPI]
        public int CodeCacheSize
        {
            get => cacheSize;
            set
            {
                if (running)
                    throw new InvalidOperationException("Can't change code cache size while running");
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Code cache size may not be negative");
                cacheSize = value;
            }
        }

        [PublicAPI]
        public int MaxUndoDepth
        {
            get => maxUndoDepth;
            set
            {
                if (running)
                    throw new InvalidOperationException("Can't change max undo depth while running");
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Max undo depth may not be negative");
                maxUndoDepth = value;
            }
        }

        public void LoadDebugInfo([NotNull] Stream fromStream)
        {
            var di = new DebugInfo(fromStream);
            if (!di.MatchesGameFile(gameFile))
                throw new ArgumentException("Debug file does not match loaded story file", nameof(fromStream));

            DebugInfo = di;
        }

        [CanBeNull]
        public DebugInfo DebugInfo { get; private set; } = default!;

        // ReSharper disable once InconsistentNaming
        [NotNull]
        public IAsyncZMachineIO IO => io;

        private CallFrame? topFrame;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        internal CallFrame TopFrame
        {
            get => topFrame ?? throw new InvalidOperationException(nameof(TopFrame) + " is null");
            private set => topFrame = value;
        }

        internal byte GetByte(int address)
        {
            return zmem[address];
        }

        internal short GetWord(int address)
        {
            return (short)(zmem[address] * 256 + zmem[address + 1]);
        }

        private void GetBytes(int address, int length, [NotNull] byte[] dest, int destIndex)
        {
            Array.Copy(zmem, address, dest, destIndex, length);
        }

#if HAVE_SPAN
        private Span<byte> GetSpan(int address, int length)
        {
            return new Span<byte>(zmem, address, length);
        }
#endif

        private void SetBytes(int address, int length, [NotNull] byte[] src, int srcIndex)
        {
            Array.Copy(src, srcIndex, zmem, address, length);
        }

        internal void SetByte(int address, byte value)
        {
            zmem[address] = value;
        }

#pragma warning disable 0169
        internal void SetByteChecked(int address, byte value)
        {
            if (address < RomStart && (address >= 64 || ValidHeaderWrite(address, ref value)))
                zmem[address] = value;

            if (address == 0x10)
            {
                // watch for changes to Flags 2's lower byte
                var b = zmem[0x11];
                io.Transcripting = (b & 1) != 0;
                io.ForceFixedPitch = (b & 2) != 0;
            }
        }
#pragma warning restore 0169

        internal void SetWord(int address, short value)
        {
            zmem[address] = (byte)(value >> 8);
            zmem[address + 1] = (byte)value;
        }

        internal void SetWordChecked(int address, short value)
        {
            if (address + 1 < RomStart && (address >= 64 || ValidHeaderWrite(address, ref value)))
            {
                zmem[address] = (byte)(value >> 8);
                zmem[address + 1] = (byte)value;
            }

            if (address == 0xF || address == 0x10)
            {
                // watch for changes to Flags 2's lower byte
                var b = zmem[0x11];
                io.Transcripting = (b & 1) != 0;
                io.ForceFixedPitch = (b & 2) != 0;
            }
        }

        private bool ValidHeaderWrite(int address, ref byte value)
        {
            // the game can only write to bits 0, 1, 2 of Flags 2's lower byte (offset 0x11)
            if (address == 0x11)
            {
                value = (byte)((value & 7) | (GetByte(address) & 0xF8));
                return true;
            }
            else
                return false;
        }

        private bool ValidHeaderWrite(int address, ref short value)
        {
            var b1 = (byte)(value >> 8);
            var b2 = (byte)value;

            var v1 = ValidHeaderWrite(address, ref b1);
            var v2 = ValidHeaderWrite(address + 1, ref b2);

            if (v1 || v2)
            {
                if (!v1)
                    b1 = GetByte(address);
                else if (!v2)
                    b2 = GetByte(address + 1);

                value = (short)((b1 << 8) | b2);
                return true;
            }
            else
                return false;
        }

        [PublicAPI]
        public bool PredictableRandom
        {
            get => predictableRng;
            set
            {
                if (value != predictableRng)
                {
                    rng = value ? new Random(12345) : new Random();
                    predictableRng = value;
                }
            }
        }

        [Obsolete("Use the async method instead.")]
        [PublicAPI]
        public void Run()
        {
            RunAsync().Wait(interruptToken);
        }

        public async Task RunAsync()
        {
            //DebugOut("Z-machine version {0}", zversion);

            ResetHeaderFields();
            io.EraseWindow(-1);
            if (zversion <= 4)
                io.ScrollFromBottom = true;

            if (zversion == 6)
            {
                EnterFunctionImpl(GetWord(0x06), null, -1, -1);
            }
            else
            {
                pc = (ushort)GetWord(0x06);
            }

            cache = new LruCache<int, CachedCode>(cacheSize);

#if BENCHMARK
            // reset performance stats
            cycles = 0;
            startTime = Environment.TickCount;
            creditedTime = 0;
            cacheHits = 0;
            cacheMisses = 0;
#endif

            running = true;
            try
            {
                await JitLoopAsync().ConfigureAwait(false);
            }
            finally
            {
                running = false;
                if (cmdRdr != null)
                {
                    cmdRdr.Dispose();
                    cmdRdr = null;
                }
                if (cmdWtr != null)
                {
                    cmdWtr.Dispose();
                    cmdWtr = null;
                }
            }

#if BENCHMARK
            // show performance report
            int billedMillis = Environment.TickCount - startTime - creditedTime;
            TimeSpan billedTime = new TimeSpan(10000 * billedMillis);
            io.PutString("\n\n*** Performance Report ***\n");
            io.PutString(string.Format("Cycles: {0}\nTime: {1}\nSpeed: {2:0.0} cycles/sec\n",
                cycles,
                billedTime,
                cycles * 1000 / (double)billedMillis));
#if DISABLE_CACHE
            io.PutString("Code cache was disabled.\n");
#else
            io.PutString(string.Format("Final cache use: {1} instructions in {0} fragments\n",
                cache.Count,
                cache.CurrentSize));
            io.PutString(string.Format("Peak cache use: {0} instructions\n", cache.PeakSize));
            io.PutString(string.Format("Cache hits: {0}. Misses: {1}.\n", cacheHits, cacheMisses));
            MeasureCacheOverlap();
#endif // DISABLE_CACHE
#endif // BENCHMARK
        }

#if BENCHMARK
        private struct Range
        {
            public readonly int Start, End;

            public Range(int start, int end)
            {
                this.Start = start;
                this.End = end;
            }
        }

        private void MeasureCacheOverlap()
        {
            Range[] ranges = new Range[cache.Count];
            int i = 0;
            int min = int.MaxValue, max = int.MinValue;

            foreach (int key in cache.Keys)
            {
                CachedCode value;
                if (cache.TryGetValue(key, out value) == true)
                {
                    Range thisRange = new Range(key, value.NextPC);

                    if (thisRange.Start < min)
                        min = thisRange.Start;
                    if (thisRange.End > max)
                        max = thisRange.End;

                    ranges[i++] = thisRange;
                }
            }

            System.Diagnostics.Debug.Assert(i == ranges.Length);

            int codeSize = max - min;
            ushort[] overlaps = new ushort[codeSize];
            for (i = 0; i < ranges.Length; i++)
                for (int j = ranges[i].Start; j < ranges[i].End; j++)
                    overlaps[j - min]++;

            max = 0;
            long totalLaps = 0;
            long denominator = overlaps.Length;
            for (i = 0; i < overlaps.Length; i++)
            {
                ushort val = overlaps[i];
                if (val == 0)
                {
                    denominator--;
                }
                else
                {
                    totalLaps += val;
                    if (val > max)
                        max = val;
                }
            }

            io.PutString(string.Format("Cache overlaps: average {0} per used cell, maximum {1}\n",
                (double)totalLaps / (double)denominator,
                max));
        }
#endif

        [PublicAPI]
        public void Reset()
        {
            if (running)
                throw new InvalidOperationException("Cannot reset while running");

            Restart();
        }

        private void BeginExternalWait()
        {
#if BENCHMARK
            waitStartTime = Environment.TickCount;
#endif

            clearable = true;
        }

        private void EndExternalWait()
        {
#if BENCHMARK
            creditedTime += Environment.TickCount - waitStartTime;
#endif

            clearable = false;
        }

        [PublicAPI]
        public void ClearCache()
        {
            if (!clearable)
                throw new InvalidOperationException("Code cache may only be cleared while waiting for input");

            cache.Clear();
        }

        /// <summary>
        /// Compiles and executes code, starting from the current <see cref="pc"/> and continuing
        /// until <see cref="running"/> becomes false or the current call frame is exited.
        /// </summary>
        private async Task JitLoopAsync()
        {
            var initialCallDepth = callStack.Count;

            while (running && callStack.Count >= initialCallDepth)
            {
#if TRACING
                Console.Write("===== Call: {1,2} Eval: {0,2}", stack.Count, callStack.Count);
                if (DebugInfo != null)
                {
                    var ri = DebugInfo.FindRoutine(pc);
                    if (ri != null)
                        Console.Write("   (in {0})", ri.Name);
                }
                Console.WriteLine();
#endif

                var thisPC = pc;
#pragma warning disable IDE0018 // Inline variable declaration
                // ReSharper disable once InlineOutVariableDeclaration
                CachedCode? entry;
#pragma warning restore IDE0018 // Inline variable declaration
#if !DISABLE_CACHE
                if (thisPC < RomStart || !cache.TryGetValue(thisPC, out entry))
#endif
                {
#if BENCHMARK
                    cacheMisses++;
#endif
                    var (code, nextPC, count) = CompileZCode();
                    entry = new CachedCode(nextPC, code);
#if BENCHMARK
                    entry.Cycles = count;   // only used to calculate the amount of cached z-code
#endif
#if !DISABLE_CACHE
                    if (thisPC >= RomStart)
                        cache.Add(thisPC, entry, count);
#endif
                }
#if BENCHMARK
                else
                    cacheHits++;
#endif
                pc = entry.NextPC;
                var task = entry.Code();
                if (task != null)
                    await task;
            }
        }

        // compilation state exposed internally for the Opcode class
        // TODO: clean up compilation state. the runtime PC shouldn't be used for compilation, especially.
        [NotNull]
        internal LocalBuilder TempWordLocal => tempWordLocal ??= il.DeclareLocal(typeof(short));

        [NotNull]
        internal LocalBuilder TempArrayLocal => tempArrayLocal ??= il.DeclareLocal(typeof(short[]));

        internal LocalBuilder? StackLocal { get; private set; }

        internal LocalBuilder? LocalsLocal { get; private set; }

        internal int GlobalsOffset { get; private set; }

        internal int PC => pc;

        internal int RomStart { get; private set; }

        private int CompilationStart { get; set; }

        public int ZVersion => zversion;

        [CanBeNull]
        private delegate Task ZCodeDelegate();

        private static readonly Type ZcodeReturnType = typeof(Task);
        private static readonly Type[] ZcodeParamTypes = { typeof(ZMachine) };

        private readonly struct CompileResult
        {
            public readonly ZCodeDelegate Code;
            public readonly int NextPC;
            public readonly int InstructionCount;

            public CompileResult(ZCodeDelegate code, int nextPC, int instructionCount)
            {
                InstructionCount = instructionCount;
                NextPC = nextPC;
                Code = code;
            }

            public void Deconstruct(out ZCodeDelegate code, out int nextPC, out int instructionCount)
            {
                code = this.Code;
                nextPC = this.NextPC;
                instructionCount = this.InstructionCount;
            }
        }

        /// <summary>
        /// Compiles code at the current <see cref="pc"/> into a <see cref="ZCodeDelegate"/>.
        /// </summary>
        /// <returns>The compilation result.</returns>
        private CompileResult CompileZCode()
        {
            var operandTypes = new OperandType[8];
            var argv = new short[8];
            var opcodes = new Dictionary<int, Opcode>();

            var dm = new DynamicMethod($"z_{pc:x}", ZcodeReturnType, ZcodeParamTypes,
                typeof(ZMachine));
            il = dm.GetILGenerator();
            tempArrayLocal = null;
            tempWordLocal = null;

            compiling = true;
            CompilationStart = pc;
            var instructionCount = 0;

            // initialize local variables for the stack and z-locals
            var stackFI = GetFieldInfo(nameof(stack));
            StackLocal = il.DeclareLocal(typeof(Stack<short>));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, stackFI);
            il.Emit(OpCodes.Stloc, StackLocal);

            var topFrameFI = GetFieldInfo(nameof(topFrame));
            var localsFI = typeof(CallFrame).GetField(nameof(CallFrame.Locals));
            LocalsLocal = il.DeclareLocal(typeof(short[]));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, topFrameFI);
            var haveLocals = il.DefineLabel();
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue, haveLocals);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stloc, LocalsLocal);
            var doneLocals = il.DefineLabel();
            il.Emit(OpCodes.Br, doneLocals);
            il.MarkLabel(haveLocals);
            il.Emit(OpCodes.Ldfld, localsFI);
            il.Emit(OpCodes.Stloc, LocalsLocal);
            il.MarkLabel(doneLocals);

            var todoList = new Queue<int>();

            // pass 1: make linear opcode chains, which might be disconnected from each other.
            Opcode? lastOp = null;
            while (compiling)
            {
                instructionCount++;
                var thisPC = pc;

                if (opcodes.ContainsKey(thisPC))
                {
                    // we've looped back, no need to compile this all again
                    if (lastOp != null)
                        lastOp.Next = opcodes[thisPC];

                    compiling = false;
                }
                else
                {
                    var op = DecodeOneOp(operandTypes, argv);
                    opcodes.Add(thisPC, op);
                    op.Label = il.DefineLabel();
                    if (lastOp != null)
                        lastOp.Next = op;
                    lastOp = op;

                    if (op.IsBranch || op.IsUnconditionalJump)
                    {
                        var targetPC = pc + op.BranchOffset - 2;
                        if (!opcodes.ContainsKey(targetPC))
                            todoList.Enqueue(targetPC);
                    }

                    if (op.IsTerminator)
                        compiling = false;
                }

                // if this op terminated the fragment, we might still have more code to compile
                if (!compiling && todoList.Count > 0)
                {
                    pc = todoList.Dequeue();
                    compiling = true;
                    lastOp = null;
                }
            }

            var firstNode = opcodes[CompilationStart];
            var todoNodes = new Queue<Opcode>();

            // pass 2: tie the chains together, so that every opcode's Target field is correct.
            var node = firstNode;
            while (node != null)
            {
                if (node.Target == null)
                {
                    if (node.IsBranch || node.IsUnconditionalJump)
                        opcodes.TryGetValue(node.PC + node.ZCodeLength + node.BranchOffset - 2, out node.Target);

                    if (node.Target != null)
                        todoNodes.Enqueue(node.Target);
                }

                if (node.Next == null && todoNodes.Count > 0)
                    node = todoNodes.Dequeue();
                else
                    node = node.Next;
            }

            // TODO: optimize constant comparisons here

            // pass 3: generate the IL
            node = opcodes[CompilationStart];
            compiling = true;
            lastOp = null;
            var needRet = false;
            var debugChkMI = GetMethodInfo(nameof(DebugCheck));
#if BENCHMARK
            FieldInfo cyclesFI = typeof(ZMachine).GetField(nameof(ZMachine.cycles), BindingFlags.NonPublic | BindingFlags.Instance);
#endif
            while (node != null && compiling)
            {
                if (opcodes.ContainsKey(node.PC))
                {
                    opcodes.Remove(node.PC);

                    if (needRet)
                    {
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ret);
                        needRet = false;
                    }

                    pc = node.PC + node.ZCodeLength;
                    il.MarkLabel(node.Label);

                    if (debugging)
                    {
                        // return immediately if DebugCheck(address) returns true
                        var noBreakLabel = il.DefineLabel();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldc_I4, node.PC);
                        il.Emit(OpCodes.Call, debugChkMI);
                        il.Emit(OpCodes.Brfalse_S, noBreakLabel);
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ret);
                        il.MarkLabel(noBreakLabel);
                    }

#if BENCHMARK
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldfld, cyclesFI);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Conv_I8);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Stfld, cyclesFI);
#endif

                    // handle unconditional jumps specially
                    if (node.IsUnconditionalJump && node.Target != null)
                    {
                        il.Emit(OpCodes.Br, node.Target.Label);
                        todoNodes.Enqueue(node.Target);
                    }
                    else
                    {
                        compiling = node.Compile(il);

                        if (node.Target != null)
                            todoNodes.Enqueue(node.Target);
                    }
                }
                else
                {
                    /* we've encountered an instruction that has already been compiled. if we're
                     * falling through from the instruction above, we need to branch to the
                     * previously generated code. otherwise, this is just a duplicated todo entry
                     * and we can ignore it. */
                    if (lastOp != null)
                    {
                        if (needRet)
                        {
                            il.Emit(OpCodes.Ldnull);
                            il.Emit(OpCodes.Ret);
                            needRet = false;
                        }

                        il.Emit(OpCodes.Br, node.Label);
                    }

                    compiling = false;
                }

                if ((node.Next == null || !compiling) && todoNodes.Count > 0)
                {
                    needRet = true;
                    lastOp = null;
                    node = todoNodes.Dequeue();
                    compiling = true;
                }
                else
                {
                    lastOp = node;
                    node = node.Next;
                }
            }

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);

            il = null!;
            tempArrayLocal = null;
            tempWordLocal = null;
            StackLocal = null;
            LocalsLocal = null;

            return new CompileResult((ZCodeDelegate) dm.CreateDelegate(typeof(ZCodeDelegate), this), pc, instructionCount);
        }

        [NotNull]
        internal static FieldInfo GetFieldInfo([NotNull] string name) =>
            typeof(ZMachine).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance) ??
            throw new ArgumentException($"No such field {name} on {nameof(ZMachine)}");

        [NotNull]
        internal static MethodInfo GetMethodInfo([NotNull] string name) =>
            typeof(ZMachine).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance) ??
            throw new ArgumentException($"No such method {name} on {nameof(ZMachine)}");

        [NotNull]
        internal static MethodInfo GetIOMethodInfo([NotNull] string name) =>
            typeof(IAsyncZMachineIO).GetMethod(name) ??
            typeof(IZMachineIO).GetMethod(name) ??
            throw new ArgumentException($"No such method {name} on {nameof(IAsyncZMachineIO)} or {nameof(IZMachineIO)}");

        [NotNull]
        private Opcode DecodeOneOp([NotNull] OperandType[] operandTypes, [NotNull] short[] argv)
        {
            var opc = pc;
            var opcode = GetByte(pc++);
            OpForm form;
            if (opcode == 0xBE)
                form = OpForm.Ext;
            else if ((opcode & 0xC0) == 0xC0)
                form = OpForm.Var;
            else if ((opcode & 0xC0) == 0x80)
                form = OpForm.Short;
            else
                form = OpForm.Long;

            OpCount count;
            byte opnum;

            // determine operand count and opcode number
            switch (form)
            {
                case OpForm.Short:
                    opnum = (byte)(opcode & 0xF);
                    operandTypes[0] = (OperandType)((opcode >> 4) & 3);
                    count = operandTypes[0] == OperandType.Omitted ? OpCount.Zero : OpCount.One;
                    break;

                case OpForm.Long:
                    opnum = (byte)(opcode & 0x1F);
                    count = OpCount.Two;
                    break;

                case OpForm.Var:
                    opnum = (byte)(opcode & 0x1F);
                    count = (opcode & 0x20) == 0 ? OpCount.Two : OpCount.Var;
                    break;

                case OpForm.Ext:
                    opnum = GetByte(pc++);
                    count = OpCount.Ext;
                    break;

                default:
                    throw new Exception("BUG:BADFORM");
            }

            // determine operand types and actual operand count
            int argc;
            switch (form)
            {
                case OpForm.Short:
                    // the operand type was already found above
                    argc = operandTypes[0] == OperandType.Omitted ? 0 : 1;
                    break;

                case OpForm.Long:
                    if ((opcode & 0x40) == 0x40)
                        operandTypes[0] = OperandType.Variable;
                    else
                        operandTypes[0] = OperandType.SmallConst;
                    if ((opcode & 0x20) == 0x20)
                        operandTypes[1] = OperandType.Variable;
                    else
                        operandTypes[1] = OperandType.SmallConst;
                    argc = 2;
                    break;

                case OpForm.Ext:
                case OpForm.Var:
                    argc = UnpackOperandTypes(
                        GetByte(pc++),
                        operandTypes,
                        0);
                    if (count == OpCount.Var && (opnum == 12 || opnum == 26))
                    {
                        // "double variable" VAR opcodes call_vs2/call_vn2
                        argc += UnpackOperandTypes(
                            GetByte(pc++),
                            operandTypes,
                            4);
                    }
                    break;

                default:
                    throw new Exception("BUG:BADFORM");
            }

            // read operands
            for (var i = 0; i < argc; i++)
            {
                switch (operandTypes[i])
                {
                    case OperandType.LargeConst:
                        argv[i] = GetWord(pc);
                        pc += 2;
                        break;

                    case OperandType.SmallConst:
                        argv[i] = GetByte(pc++);
                        break;

                    case OperandType.Variable:
                        argv[i] = GetByte(pc++);
                        break;

                    case OperandType.Omitted:
                        // shouldn't get here!
                        Console.WriteLine("[BUG:OMITTED]");
                        SystemDebugger.Break();
                        argv[i] = 0;
                        break;
                }
            }

            // look up a method to compile this opcode
            if (Opcode.FindOpcodeInfo(count, opnum, zversion, out var info) == false)
            {
                // EXT:29 to EXT:255 are silently ignored.
                // these are unrecognized custom opcodes, so the best we can do
                // is skip the opcode and its operands and hope it won't branch or store.
                if (count != OpCount.Ext || opnum < 29)
                    throw new NotImplementedException($"Opcode {FormatOpcode(count, form, opnum)} at ${opc:x5}");
            }

#if TRACING
            // write decoded opcode to console
            Console.Write("{0:x6}  {4,3} {2,5}  {1,-7}     {3}",
                opc, FormatOpcode(count, form, opnum), form, Opcode.GetOpcodeName(info.Attr, info.Compiler), opcode);

            for (int i = 0; i < argc; i++)
            {
                switch (operandTypes[i])
                {
                    case OperandType.SmallConst:
                        Console.Write(" short_{0}", argv[i]);
                        break;
                    case OperandType.LargeConst:
                        Console.Write(" long_{0}", argv[i]);
                        break;
                    case OperandType.Variable:
                        if (argv[i] == 0)
                            Console.Write(" sp");
                        else if (argv[i] < 16)
                            Console.Write(" local_{0}", argv[i]);
                        else
                            Console.Write(" global_{0}", argv[i]);
                        break;
                }
            }
#endif

            // decode branch info, store info, and/or text
            var resultStorage = -1;
            var branchIfTrue = false;
            var branchOffset = int.MinValue;
            string? text = null;

            if (info.Attr.Store)
            {
                resultStorage = GetByte(pc++);

#if TRACING
                if (resultStorage == 0)
                    Console.Write(" -> sp");
                else if (resultStorage < 16)
                    Console.Write(" -> local_{0}", resultStorage);
                else
                    Console.Write(" -> global_{0}", resultStorage);
#endif
            }

            if (info.Attr.Branch)
            {
                DecodeBranch(out branchIfTrue, out branchOffset);

#if TRACING
                Console.Write(" ?{0}{1}",
                    branchIfTrue ? "" : "~",
                    branchOffset == 0 ? "rfalse" : branchOffset == 1 ? "rtrue" : (branchOffset - 2).ToString());
                if (branchOffset != 0 && branchOffset != 1)
                    Console.Write(" [{0:x6}]", pc + branchOffset - 2);
#endif
            }

            if (info.Attr.Text)
            {
                text = DecodeStringWithLen(pc, out var len);
                pc += len;

#if TRACING
                string tstr;
                if (text.Length <= 10)
                    tstr = text;
                else
                    tstr = text.Substring(0, 7) + "...";
                Console.Write(" \"{0}\"", tstr);
#endif
            }

#if TRACING
            Console.WriteLine();
#endif

            return new Opcode(
                this, info.Compiler, info.Attr, opc, pc - opc,
                argc, operandTypes, argv,
                text, resultStorage, branchIfTrue, branchOffset);
        }

        private void DecodeBranch(out bool branchIfTrue, out int branchOffset)
        {
            var b = GetByte(pc++);
            branchIfTrue = (b & 128) == 128;
            if ((b & 64) == 64)
            {
                // short branch, 0 to 63
                branchOffset = b & 63;
            }
            else
            {
                // long branch, signed 14-bit offset
                branchOffset = ((b & 63) << 8) + GetByte(pc++);
                if ((branchOffset & 0x2000) != 0)
                    branchOffset = (int)((uint)branchOffset | 0xFFFFC000); // extend the sign
            }
        }

        [NotNull]
        private static string FormatOpCount(OpCount opc)
        {
            return opc switch
            {
                OpCount.Zero => "0OP",
                OpCount.One => "1OP",
                OpCount.Two => "2OP",
                OpCount.Var => "VAR",
                OpCount.Ext => "EXT",
                _ => "BUG",
            };
        }

        [NotNull]
        private static string FormatOpcode(OpCount opc, OpForm form, int opnum)
        {
            var sb = new StringBuilder(FormatOpCount(opc));
            sb.Append(':');

            if (form == OpForm.Ext)
            {
                sb.Append(opnum);
            }
            else
                switch (opc)
                {
                    case OpCount.Two:
                    case OpCount.Ext:
                        sb.Append(opnum);
                        break;
                    case OpCount.One:
                        sb.Append(128 + opnum);
                        break;
                    case OpCount.Zero:
                        sb.Append(176 + opnum);
                        break;
                    case OpCount.Var:
                        sb.Append(224 + opnum);
                        break;
                }

            return sb.ToString();
        }

        private static int UnpackOperandTypes(byte b, OperandType[] operandTypes, int start)
        {
            var count = 0;

            for (var i = 0; i < 4; i++)
            {
                operandTypes[i + start] = (OperandType)(b >> 6);
                b <<= 2;
                if (operandTypes[i + start] != OperandType.Omitted)
                    count++;
            }

            return count;
        }

        void ResetHeaderFields()
        {
            normalOutput = true;
            tableOutputStack.Clear();

            dictionaryTable = (ushort)GetWord(0x8);
            objectTable = (ushort)GetWord(0xA);
            GlobalsOffset = (ushort)GetWord(0xC);
            RomStart = (ushort)GetWord(0xE);
            abbrevTable = (ushort)GetWord(0x18);
            if (zversion == 6 || zversion == 7)
            {
                codeStart = GetWord(0x28) * 8;
                stringStart = GetWord(0x2A) * 8;
            }

            // load character tables (setting up memory traps if needed)
            LoadExtraChars();
            LoadAlphabets();
            LoadTerminatingChars();
            LoadWordSeparators();

            byte flags1;

            if (zversion <= 3)
            {
                // old-style flags1
                flags1 = GetByte(0x1);
                flags1 |= 16 | 32; // status line and screen splitting are always available
                if (io.VariablePitchAvailable)
                    flags1 |= 64;
                else
                    flags1 &= unchecked((byte) ~64);
            }
            else
            {
                // new-style flags1
                flags1 = 0; // depends on I/O capabilities

                if (io.ColorsAvailable)
                    flags1 |= 1;
                if (io.BoldAvailable)
                    flags1 |= 4;
                if (io.ItalicAvailable)
                    flags1 |= 8;
                if (io.FixedPitchAvailable)
                    flags1 |= 16;
                if (io.TimedInputAvailable)
                    flags1 |= 128;
            }

            ushort flags2 = 16; // always support UNDO

            if (io.Transcripting)
                flags2 |= 1;
            if (io.ForceFixedPitch)
                flags2 |= 2;
            if (io.GraphicsFontAvailable)
                flags2 |= 8;

            // TODO: support mouse input (flags2 & 32)

            SetByte(0x1, flags1);
            SetWord(0x10, (short)flags2);

            io.Transcripting = (flags2 & 1) != 0;
            io.ForceFixedPitch = (flags2 & 2) != 0;

            SetByte(0x1E, 6);                    // interpreter platform
            SetByte(0x1F, (byte)'A');            // interpreter version
            SetByte(0x20, io.HeightChars);       // screen height (rows)
            SetByte(0x21, io.WidthChars);        // screen width (columns)
            SetWord(0x22, io.WidthUnits);        // screen width (units)
            SetWord(0x24, io.HeightUnits);       // screen height (units)
            SetByte(0x26, io.FontWidth);         // font width (units)
            SetByte(0x27, io.FontHeight);        // font height (units)
            SetByte(0x2C, io.DefaultBackground); // default background color
            SetByte(0x2D, io.DefaultForeground); // default background color
            SetWord(0x32, 0x0100);               // z-machine standard version
        }

        private void LoadAlphabets()
        {
            var userAlphabets = (ushort)GetWord(0x34);
            if (userAlphabets == 0)
            {
                alphabet0 = DefaultAlphabet0;
                alphabet1 = DefaultAlphabet1;
                alphabet2 = DefaultAlphabet2;
            }
            else
            {
                alphabet0 = new char[26];
                for (var i = 0; i < 26; i++)
                    alphabet0[i] = CharFromZSCII(GetByte(userAlphabets + i));

                alphabet1 = new char[26];
                for (var i = 0; i < 26; i++)
                    alphabet1[i] = CharFromZSCII(GetByte(userAlphabets + 26 + i));

                alphabet2 = new char[26];
                alphabet2[0] = ' ';  // escape code
                alphabet2[1] = '\n'; // new line
                for (var i = 2; i < 26; i++)
                    alphabet2[i] = CharFromZSCII(GetByte(userAlphabets + 52 + i));

                if (userAlphabets < RomStart)
                    traps.Add(userAlphabets, 26 * 3, LoadAlphabets);
            }
        }

        private void LoadExtraChars()
        {
            var userExtraChars = (ushort)GetHeaderExtWord(3);
            if (userExtraChars == 0)
            {
                extraChars = DefaultExtraChars;
            }
            else
            {
                var n = GetByte(userExtraChars);
                extraChars = new char[n];
                for (var i = 0; i < n; i++)
                    extraChars[i] = (char)GetWord(userExtraChars + 1 + 2 * i);

                if (userExtraChars < RomStart)
                {
                    traps.Remove(userExtraChars);
                    traps.Add(userExtraChars, n * 2 + 1, LoadExtraChars);
                }
            }
        }

        private void LoadTerminatingChars()
        {
            var terminatingTable = (ushort)GetWord(0x2E);
            if (terminatingTable == 0)
            {
                terminatingChars = new byte[0];
            }
            else
            {
                var temp = new List<byte>();
                var b = GetByte(terminatingTable);
                var n = 1;
                while (b != 0)
                {
                    if (b == 255)
                    {
                        // 255 means every possible terminator, so don't bother with the rest of the list
                        temp.Clear();
                        temp.Add(255);
                        break;
                    }

                    temp.Add(b);
                    b = GetByte(++terminatingTable);
                    n++;
                }
                terminatingChars = temp.ToArray();

                if (terminatingTable < RomStart)
                {
                    traps.Remove(terminatingTable);
                    traps.Add(terminatingTable, n, LoadTerminatingChars);
                }
            }
        }

        private void LoadWordSeparators()
        {
            // read word separators
            var n = GetByte(dictionaryTable);
            wordSeparators = new byte[n];
            for (var i = 0; i < n; i++)
                wordSeparators[i] = GetByte(dictionaryTable + 1 + i);

            // the dictionary is almost certainly in ROM, but just in case...
            if (dictionaryTable < RomStart)
            {
                traps.Remove(dictionaryTable);
                traps.Add(dictionaryTable, n + 1, LoadWordSeparators);
            }
        }

        private void IOSizeChanged(object sender, EventArgs e)
        {
            SetByte(0x20, io.HeightChars); // screen height (rows)
            SetByte(0x21, io.WidthChars);  // screen width (columns)
            SetWord(0x22, io.WidthUnits);  // screen width (units)
            SetWord(0x24, io.HeightUnits); // screen height (units)
            SetByte(0x26, io.FontWidth);   // font width (units)
            SetByte(0x27, io.FontHeight);  // font height (units)
        }

        private short GetHeaderExtWord(int num)
        {
            var headerExt = (ushort)GetWord(0x36);
            if (headerExt == 0)
                return 0;

            var len = (ushort)GetWord(headerExt);
            return num > len ? (short) 0 : GetWord(headerExt + 2 * num);
        }

        internal int UnpackAddress(short packedAddr, bool forString)
        {
            switch (zversion)
            {
                case 1:
                case 2:
                case 3:
                    return 2*(ushort) packedAddr;

                case 4:
                case 5:
                    return 4*(ushort) packedAddr;

                case 6:
                case 7:
                    var offset = forString ? stringStart : codeStart;
                    return offset + 4*(ushort) packedAddr;

                case 8:
                    return 8*(ushort) packedAddr;

                default:
                    throw new NotImplementedException();
            }
        }

        internal void TrapMemory(ushort address, ushort length)
        {
            traps.Handle(address, length);
        }

        internal class CallFrame : ICallFrame
        {
            public CallFrame(int returnPC, int prevStackDepth, int numLocals, int argCount,
                int resultStorage)
            {
                ReturnPC = returnPC;
                PrevStackDepth = prevStackDepth;
                Locals = new short[numLocals];
                ArgCount = argCount;
                ResultStorage = resultStorage;
            }

            public readonly int ReturnPC;
            public readonly int PrevStackDepth;
            [NotNull] public readonly short[] Locals;
            public readonly int ArgCount;
            public readonly int ResultStorage;

            [NotNull]
            public CallFrame Clone()
            {
                var result = new CallFrame(ReturnPC, PrevStackDepth, Locals.Length,
                    ArgCount, ResultStorage);
                Array.Copy(Locals, result.Locals, Locals.Length);
                return result;
            }

            #region ICallFrame Members

            int ICallFrame.ReturnPC => ReturnPC;

            int ICallFrame.PrevStackDepth => PrevStackDepth;

            short[] ICallFrame.Locals => Locals;

            int ICallFrame.ArgCount => ArgCount;

            int ICallFrame.ResultStorage => ResultStorage;

            #endregion
        }

        private class UndoState
        {
            private readonly byte[] ram;
            private readonly short[] savedStack;
            private readonly CallFrame[] savedCallStack;
            private readonly int savedPC;
            private readonly byte savedDest;

            public UndoState([NotNull] byte[] zmem, int ramLength, [NotNull] Stack<short> stack, [ItemNotNull] [NotNull] Stack<CallFrame> callStack,
                int pc, byte dest)
            {
                ram = new byte[ramLength];
                Array.Copy(zmem, ram, ramLength);

                savedStack = stack.ToArray();
                savedCallStack = callStack.ToArray();
                for (var i = 0; i < savedCallStack.Length; i++)
                    savedCallStack[i] = savedCallStack[i].Clone();

                savedPC = pc;
                savedDest = dest;
            }

            public void Restore([NotNull] byte[] zmem, [NotNull] Stack<short> stack, [ItemNotNull] [NotNull] Stack<CallFrame> callStack,
                out int pc, out byte dest)
            {
                Array.Copy(ram, zmem, ram.Length);

                stack.Clear();
                for (var i = savedStack.Length - 1; i >= 0; i--)
                    stack.Push(savedStack[i]);

                callStack.Clear();
                for (var i = savedCallStack.Length - 1; i >= 0; i--)
                    callStack.Push(savedCallStack[i]);

                pc = savedPC;
                dest = savedDest;
            }
        }

        private delegate void MemoryTrapHandler();

        private class MemoryTraps
        {
            [NotNull] private readonly List<int> starts = new List<int>();
            [NotNull] private readonly List<int> lengths = new List<int>();
            [NotNull] private readonly List<MemoryTrapHandler> handlers = new List<MemoryTrapHandler>();

            private int firstAddress;
            private int lastAddress = -1;

            /// <summary>
            /// Adds a new trap for the specified memory region. Does nothing
            /// if a region with the same starting address is already trapped.
            /// </summary>
            /// <param name="trapStart">The starting address of the region.</param>
            /// <param name="trapLength">The length of the region.</param>
            /// <param name="trapHandler">The delegate to call when the memory
            /// is written.</param>
            public void Add(int trapStart, int trapLength, MemoryTrapHandler trapHandler)
            {
                var idx = starts.BinarySearch(trapStart);
                if (idx < 0)
                {
                    idx = ~idx;
                    starts.Insert(idx, trapStart);
                    lengths.Insert(idx, trapLength);
                    handlers.Insert(idx, trapHandler);
                }
            }

            /// <summary>
            /// Removes a memory trap. Does nothing if no trap is set with the
            /// given starting address.
            /// </summary>
            /// <param name="trapStart">The starting address of the trap to
            /// remove.</param>
            public void Remove(int trapStart)
            {
                var idx = starts.BinarySearch(trapStart);
                if (idx >= 0)
                {
                    starts.RemoveAt(idx);
                    lengths.RemoveAt(idx);
                    handlers.RemoveAt(idx);

                    var count = starts.Count;
                    if (count == 0)
                    {
                        firstAddress = 0;
                        lastAddress = -1;
                    }
                    else
                    {
                        firstAddress = starts[0];
                        lastAddress = starts[count - 1] + lengths[count - 1] - 1;
                    }
                }
            }

            /// <summary>
            /// Calls the appropriate handlers when a region of memory has been
            /// written.
            /// </summary>
            /// <param name="changeStart">The starting address of the region
            /// that was written.</param>
            /// <param name="changeLength">The length of the region that
            /// was written.</param>
            public void Handle(int changeStart, int changeLength)
            {
                var changeEnd = changeStart + changeLength - 1;

                if (changeStart > lastAddress || changeEnd < firstAddress)
                    return;

                /* the number of traps will be very limited, so we don't need to
                 * do anything fancy here. */
                var trapCount = starts.Count;
                for (var i = 0; i < trapCount; i++)
                {
                    var start = starts[i];
                    var len = lengths[i];
                    if (changeStart >= start && changeEnd < start + len)
                        handlers[i].Invoke();
                }
            }
        }
    }
}