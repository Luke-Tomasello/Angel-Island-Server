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
using ZLR.IFF;
using ZLR.VM.Debugging;

namespace ZLR.VM
{
    partial class ZMachine
    {
        public static readonly string ZLR_VERSION = "0.07";

        private struct CachedCode
        {
            public int NextPC;
            public ZCodeDelegate Code;
#if BENCHMARK
            public int Cycles;
#endif

            public CachedCode(int nextPC, ZCodeDelegate code)
            {
                this.NextPC = nextPC;
                this.Code = code;
#if BENCHMARK
                this.Cycles = 0;
#endif
            }
        }

        // compilation state
        byte zversion;
        int globalsOffset, objectTable, dictionaryTable, abbrevTable;
        bool compiling;
        int compilationStart;
        ILGenerator il;
        LocalBuilder tempArrayLocal, tempWordLocal, stackLocal, localsLocal;
        LruCache<int, CachedCode> cache;
        int cacheSize = DEFAULT_CACHE_SIZE;
        int maxUndoDepth = DEFAULT_MAX_UNDO_DEPTH;

        // compilation and runtime state
        int pc;
        bool clearable;

        // runtime state
        Stream gameFile;
        bool running;
        byte[] zmem;
        IZMachineIO io;
        CommandFileReader cmdRdr;
        CommandFileWriter cmdWtr;
        Stack<short> stack = new Stack<short>();
        Stack<CallFrame> callStack = new Stack<CallFrame>();
        CallFrame topFrame;
        Random rng = new Random();
        bool predictableRng;
        byte[] wordSeparators;
        int romStart;
        List<UndoState> undoStates = new List<UndoState>();
        bool normalOutput, tableOutput;
        Stack<ushort> tableOutputAddrStack = new Stack<ushort>();
        Stack<List<byte>> tableOutputBufferStack = new Stack<List<byte>>();
        char[] alphabet0, alphabet1, alphabet2, extraChars;
        byte[] terminatingChars;
        MemoryTraps traps = new MemoryTraps();

#if BENCHMARK
        long cycles;
        int startTime, waitStartTime;
        int creditedTime;
        int cacheHits, cacheMisses;
#endif

        DebugInfo debugFile;

        const int DEFAULT_MAX_UNDO_DEPTH = 3;
        const int DEFAULT_CACHE_SIZE = 35000;

        /// <summary>
        /// Initializes a new instance of the ZLR engine from a given stream.
        /// The stream must remain open while the engine is in use.
        /// </summary>
        /// <param name="gameStream">A stream containing either a plain Z-code
        /// file or a Blorb file which in turn contains a Z-code resource.</param>
        /// <param name="io"></param>
        public ZMachine(Stream gameStream, IZMachineIO io)
        {
            if (gameStream == null)
                throw new ArgumentNullException("gameStream");
            if (io == null)
                throw new ArgumentNullException("io");

            this.io = io;

            // check for Blorb
            byte[] temp = new byte[12];
            gameStream.Seek(0, SeekOrigin.Begin);
            gameStream.Read(temp, 0, 12);
            if (temp[0] == 'F' && temp[1] == 'O' && temp[2] == 'R' && temp[3] == 'M' &&
                temp[8] == 'I' && temp[9] == 'F' && temp[10] == 'R' && temp[11] == 'S')
            {
                Blorb blorb = new Blorb(gameStream);
                if (blorb.GetStoryType() == "ZCOD")
                    gameStream = blorb.GetStoryStream();
                else
                    throw new ArgumentException("Not a Z-code Blorb");
            }

            this.gameFile = gameStream;

            zmem = new byte[gameStream.Length];
            gameStream.Seek(0, SeekOrigin.Begin);
            gameStream.Read(zmem, 0, (int)gameStream.Length);

            if (zmem.Length < 64)
                throw new ArgumentException("Z-code file is too short: must be at least 64 bytes");

            zversion = zmem[0];

            if (zversion != 5 && zversion != 8)
                throw new ArgumentException("Z-code version must be 5 or 8");

            io.SizeChanged += new EventHandler(io_SizeChanged);
        }

        public int CodeCacheSize
        {
            get { return cacheSize; }
            set
            {
                if (running)
                    throw new InvalidOperationException("Can't change code cache size while running");
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Code cache size may not be negative");
                cacheSize = value;
            }
        }

        public int MaxUndoDepth
        {
            get { return MaxUndoDepth; }
            set
            {
                if (running)
                    throw new InvalidOperationException("Can't change max undo depth while running");
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Max undo depth may not be negative");
                maxUndoDepth = value;
            }
        }

        public void LoadDebugInfo(Stream fromStream)
        {
            DebugInfo di = new DebugInfo(fromStream);
            if (!di.MatchesGameFile(gameFile))
                throw new ArgumentException("Debug file does not match loaded story file");

            debugFile = di;
        }

        private CallFrame TopFrame
        {
            get { return topFrame; }
        }

        private byte GetByte(int address)
        {
            return zmem[address];
        }

        private short GetWord(int address)
        {
            return (short)(zmem[address] * 256 + zmem[address + 1]);
        }

        private void GetBytes(int address, int length, byte[] dest, int destIndex)
        {
            Array.Copy(zmem, address, dest, destIndex, length);
        }

        private void SetBytes(int address, int length, byte[] src, int srcIndex)
        {
            Array.Copy(src, srcIndex, zmem, address, length);
        }

        private void SetByte(int address, byte value)
        {
            zmem[address] = value;
        }

        private void SetByteChecked(int address, byte value)
        {
            if (address < romStart && (address >= 64 || ValidHeaderWrite(address, ref value)))
                zmem[address] = value;

            if (address == 0x10)
            {
                // watch for changes to Flags 2's lower byte
                byte b = zmem[0x11];
                io.Transcripting = ((b & 1) != 0);
                io.ForceFixedPitch = ((b & 2) != 0);
            }
        }

        private void SetWord(int address, short value)
        {
            zmem[address] = (byte)(value >> 8);
            zmem[address + 1] = (byte)value;
        }

        private void SetWordChecked(int address, short value)
        {
            if (address + 1 < romStart && (address >= 64 || ValidHeaderWrite(address, ref value)))
            {
                zmem[address] = (byte)(value >> 8);
                zmem[address + 1] = (byte)value;
            }

            if (address == 0xF || address == 0x10)
            {
                // watch for changes to Flags 2's lower byte
                byte b = zmem[0x11];
                io.Transcripting = ((b & 1) != 0);
                io.ForceFixedPitch = ((b & 2) != 0);
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
            byte b1 = (byte)(value >> 8);
            byte b2 = (byte)value;

            bool v1 = ValidHeaderWrite(address, ref b1);
            bool v2 = ValidHeaderWrite(address + 1, ref b2);

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

        public bool PredictableRandom
        {
            get { return predictableRng; }
            set
            {
                if (value != predictableRng)
                {
                    if (value)
                        rng = new Random(12345);
                    else
                        rng = new Random();
                    predictableRng = value;
                }
            }
        }

        public void Run()
        {
            //DebugOut("Z-machine version {0}", zversion);

            ResetHeaderFields(true);
            io.EraseWindow(-1);

            pc = (ushort)GetWord(0x06);

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
                JitLoop();
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
#endif // DISABLE_CACHE
#endif // BENCHMARK
        }

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
        private void JitLoop()
        {
            int initialCallDepth = callStack.Count;

            while (running && callStack.Count >= initialCallDepth)
            {
#if TRACING
                Console.Write("===== Call: {1,2} Eval: {0,2}", stack.Count, callStack.Count);
                if (debugFile != null)
                {
                    RoutineInfo ri = debugFile.FindRoutine(pc);
                    if (ri != null)
                        Console.Write("   (in {0})", ri.Name);
                }
                Console.WriteLine();
#endif

                // Work around some kind of CLR bug!
                // The release build crashes with a NullReferenceException if this test is removed.
                // Cache can *never* be null, of course, since it's initialized outside this loop
                // and never changed. But it seems to magically become null unless we remind .NET
                // to check first. (Note: the bug doesn't occur when running under the debugger,
                // even in release builds.)
                //if (cache == null)
                //    throw new Exception("Something impossible happened");
                // NOTE: fixed it by making cache a field instead.

                CachedCode entry;
                int thisPC = pc;
#if !DISABLE_CACHE
                if (thisPC < romStart || cache.TryGetValue(thisPC, out entry) == false)
#endif
                {
#if BENCHMARK
                    cacheMisses++;
#endif
                    int count;
                    entry.Code = CompileZCode(out count);
                    entry.NextPC = pc;
#if BENCHMARK
                    entry.Cycles = count;   // only used to calculate the amount of cached z-code
#endif
#if !DISABLE_CACHE
                    if (thisPC >= romStart)
                        cache.Add(thisPC, entry, count);
#endif
                }
#if BENCHMARK
                else
                    cacheHits++;
#endif
                pc = entry.NextPC;
                entry.Code();
            }
        }

        // compilation state exposed internally for the Opcode class
        internal LocalBuilder TempWordLocal
        {
            get
            {
                if (tempWordLocal == null)
                    tempWordLocal = il.DeclareLocal(typeof(short));
                return tempWordLocal;
            }
        }

        internal LocalBuilder TempArrayLocal
        {
            get
            {
                if (tempArrayLocal == null)
                    tempArrayLocal = il.DeclareLocal(typeof(short[]));
                return tempArrayLocal;
            }
        }

        internal LocalBuilder StackLocal
        {
            get { return stackLocal; }
        }

        internal LocalBuilder LocalsLocal
        {
            get
            {
                return localsLocal;
            }
        }

        internal int GlobalsOffset
        {
            get { return globalsOffset; }
        }

        internal int PC
        {
            get { return pc; }
            set { pc = value; }
        }

        internal int RomStart
        {
            get { return romStart; }
        }

        internal int CompilationStart
        {
            get { return compilationStart; }
        }

        private delegate void ZCodeDelegate();
        private static readonly Type zcodeReturnType = null;
        private static readonly Type[] zcodeParamTypes = { typeof(ZMachine) };

        private ZCodeDelegate CompileZCode(out int instructionCount)
        {
            OperandType[] operandTypes = new OperandType[8];
            short[] argv = new short[8];
            Dictionary<int, Opcode> opcodes = new Dictionary<int, Opcode>();

            DynamicMethod dm = new DynamicMethod(string.Format("z_{0:x}", pc), zcodeReturnType, zcodeParamTypes,
                typeof(ZMachine));
            il = dm.GetILGenerator();
            tempArrayLocal = null;
            tempWordLocal = null;

            compiling = true;
            compilationStart = pc;
            instructionCount = 0;

            // initialize local variables for the stack and z-locals
            FieldInfo stackFI = typeof(ZMachine).GetField("stack", BindingFlags.NonPublic | BindingFlags.Instance);
            stackLocal = il.DeclareLocal(typeof(Stack<short>));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, stackFI);
            il.Emit(OpCodes.Stloc, stackLocal);

            MethodInfo getTopFrameMI = typeof(ZMachine).GetMethod("get_TopFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo localsFI = typeof(CallFrame).GetField("Locals");
            localsLocal = il.DeclareLocal(typeof(short[]));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, getTopFrameMI);
            Label haveLocals = il.DefineLabel();
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue, haveLocals);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stloc, localsLocal);
            Label doneLocals = il.DefineLabel();
            il.Emit(OpCodes.Br, doneLocals);
            il.MarkLabel(haveLocals);
            il.Emit(OpCodes.Ldfld, localsFI);
            il.Emit(OpCodes.Stloc, localsLocal);
            il.MarkLabel(doneLocals);

            Queue<int> todoList = new Queue<int>();

            // pass 1: make linear opcode chains, which might be disconnected from each other.
            Opcode lastOp = null;
            while (compiling)
            {
                instructionCount++;
                int thisPC = pc;

                if (opcodes.ContainsKey(thisPC))
                {
                    // we've looped back, no need to compile this all again
                    if (lastOp != null)
                        lastOp.Next = opcodes[thisPC];

                    compiling = false;
                }
                else
                {
                    Opcode op = DecodeOneOp(operandTypes, argv);
                    opcodes.Add(thisPC, op);
                    op.Label = il.DefineLabel();
                    if (lastOp != null)
                        lastOp.Next = op;
                    lastOp = op;

                    if (op.IsBranch || op.IsUnconditionalJump)
                    {
                        int targetPC = pc + op.BranchOffset - 2;
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

            Opcode node, firstNode = opcodes[compilationStart];
            Queue<Opcode> todoNodes = new Queue<Opcode>();

            // pass 2: tie the chains together, so that every opcode's Target field is correct.
            node = firstNode;
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
            node = opcodes[compilationStart];
            compiling = true;
            lastOp = null;
            bool needRet = false;
#if BENCHMARK
            FieldInfo cyclesFI = typeof(ZMachine).GetField("cycles", BindingFlags.NonPublic| BindingFlags.Instance);
#endif
            while (node != null && compiling)
            {
                if (opcodes.ContainsKey(node.PC))
                {
                    opcodes.Remove(node.PC);

                    if (needRet)
                    {
                        il.Emit(OpCodes.Ret);
                        needRet = false;
                    }

                    pc = node.PC + node.ZCodeLength;
                    il.MarkLabel(node.Label);

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
                        node.Compile(il, ref compiling);

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

            il.Emit(OpCodes.Ret);

            il = null;
            tempArrayLocal = null;
            tempWordLocal = null;
            stackLocal = null;
            localsLocal = null;

            return (ZCodeDelegate)dm.CreateDelegate(typeof(ZCodeDelegate), this);
        }

        private Opcode DecodeOneOp(OperandType[] operandTypes, short[] argv)
        {
            int opc = pc;
            byte opcode = GetByte(pc++);
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
                    if (operandTypes[0] == OperandType.Omitted)
                        count = OpCount.Zero;
                    else
                        count = OpCount.One;
                    break;

                case OpForm.Long:
                    opnum = (byte)(opcode & 0x1F);
                    count = OpCount.Two;
                    break;

                case OpForm.Var:
                    opnum = (byte)(opcode & 0x1F);
                    if ((opcode & 0x20) == 0)
                        count = OpCount.Two;
                    else
                        count = OpCount.Var;
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
                    if (operandTypes[0] == OperandType.Omitted)
                        argc = 0;
                    else
                        argc = 1;
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
            for (int i = 0; i < argc; i++)
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
                        argv[i] = 0;
                        break;
                }
            }

            // look up a method to compile this opcode
            OpcodeInfo info;
            if (Opcode.FindOpcodeInfo(count, opnum, out info) == false)
            {
                // EXT:29 to EXT:255 are silently ignored.
                // these are unrecognized custom opcodes, so the best we can do
                // is skip the opcode and its operands and hope it won't branch or store.
                if (count != OpCount.Ext || opnum < 29)
                    throw new NotImplementedException(string.Format(
                        "Opcode {0} at ${1:x4}",
                        FormatOpcode(count, form, opnum),
                        opc));
            }

#if TRACING
            // write decoded opcode to console
            Console.Write("{0:x6}  {4,3} {2,5}  {1,-7}     {3}",
                opc, FormatOpcode(count, form, opnum), form, Opcode.OpcodeName(info.Compiler), opcode);

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
            int resultStorage = -1;
            bool branchIfTrue = false;
            int branchOffset = int.MinValue;
            string text = null;

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
                byte b = GetByte(pc++);
                branchIfTrue = ((b & 128) == 128);
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
                int len;
                text = DecodeStringWithLen(pc, out len);
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

        private string FormatOpCount(OpCount opc)
        {
            switch (opc)
            {
                case OpCount.Zero: return "0OP";
                case OpCount.One: return "1OP";
                case OpCount.Two: return "2OP";
                case OpCount.Var: return "VAR";
                case OpCount.Ext: return "EXT";
                default: return "BUG";
            }
        }

        private string FormatOpcode(OpCount opc, OpForm form, int opnum)
        {
            StringBuilder sb = new StringBuilder(FormatOpCount(opc));
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

        private int UnpackOperandTypes(byte b, OperandType[] operandTypes, int start)
        {
            int count = 0;

            for (int i = 0; i < 4; i++)
            {
                operandTypes[i + start] = (OperandType)(b >> 6);
                b <<= 2;
                if (operandTypes[i + start] != OperandType.Omitted)
                    count++;
            }

            return count;
        }

        void ResetHeaderFields(bool firstRun)
        {
            normalOutput = true;
            tableOutput = false;
            tableOutputAddrStack.Clear();
            tableOutputBufferStack.Clear();

            dictionaryTable = (ushort)GetWord(0x8);
            objectTable = (ushort)GetWord(0xA);
            globalsOffset = (ushort)GetWord(0xC);
            romStart = (ushort)GetWord(0xE);
            abbrevTable = (ushort)GetWord(0x18);

            // load character tables (setting up memory traps if needed)
            LoadAlphabets();
            LoadExtraChars();
            LoadTerminatingChars();
            LoadWordSeparators();

            byte flags1 = 0; // depends on I/O capabilities

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

            io.Transcripting = ((flags2 & 1) != 0);
            io.ForceFixedPitch = ((flags2 & 2) != 0);

            SetByte(0x1E, 6);                       // interpreter platform
            SetByte(0x1F, (byte)'A');               // interpreter version
            SetByte(0x20, io.HeightChars);          // screen height (rows)
            SetByte(0x21, io.WidthChars);           // screen width (columns)
            SetWord(0x22, io.WidthUnits);           // screen width (units)
            SetWord(0x24, io.HeightUnits);          // screen height (units)
            SetByte(0x26, io.FontWidth);            // font width (units)
            SetByte(0x27, io.FontHeight);           // font height (units)
            SetByte(0x2C, io.DefaultBackground);    // default background color
            SetByte(0x2D, io.DefaultForeground);    // default background color
            SetWord(0x32, 0x0100);                  // z-machine standard version
        }

        private void LoadAlphabets()
        {
            ushort userAlphabets = (ushort)GetWord(0x34);
            if (userAlphabets == 0)
            {
                alphabet0 = defaultAlphabet0;
                alphabet1 = defaultAlphabet1;
                alphabet2 = defaultAlphabet2;
            }
            else
            {
                alphabet0 = new char[26];
                for (int i = 0; i < 26; i++)
                    alphabet0[i] = CharFromZSCII(GetByte(userAlphabets + i));

                alphabet1 = new char[26];
                for (int i = 0; i < 26; i++)
                    alphabet1[i] = CharFromZSCII(GetByte(userAlphabets + 26 + i));

                alphabet2 = new char[26];
                alphabet2[0] = ' '; // escape code
                alphabet2[1] = '\n'; // new line
                for (int i = 2; i < 26; i++)
                    alphabet2[i] = CharFromZSCII(GetByte(userAlphabets + 52 + i));

                if (userAlphabets < romStart)
                    traps.Add(userAlphabets, 26 * 3, LoadAlphabets);
            }
        }

        private void LoadExtraChars()
        {
            ushort userExtraChars = (ushort)GetHeaderExtWord(3);
            if (userExtraChars == 0)
            {
                extraChars = defaultExtraChars;
            }
            else
            {
                byte n = GetByte(userExtraChars);
                extraChars = new char[n];
                for (int i = 0; i < n; i++)
                    extraChars[i] = (char)GetWord(userExtraChars + 1 + 2 * i);

                if (userExtraChars < romStart)
                {
                    traps.Remove(userExtraChars);
                    traps.Add(userExtraChars, n * 2 + 1, LoadExtraChars);
                }
            }
        }

        private void LoadTerminatingChars()
        {
            ushort terminatingTable = (ushort)GetWord(0x2E);
            if (terminatingTable == 0)
            {
                terminatingChars = new byte[0];
            }
            else
            {
                List<byte> temp = new List<byte>();
                byte b = GetByte(terminatingTable);
                int n = 1;
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

                if (terminatingTable < romStart)
                {
                    traps.Remove(terminatingTable);
                    traps.Add(terminatingTable, n, LoadTerminatingChars);
                }
            }
        }

        private void LoadWordSeparators()
        {
            // read word separators
            byte n = GetByte(dictionaryTable);
            wordSeparators = new byte[n];
            for (int i = 0; i < n; i++)
                wordSeparators[i] = GetByte(dictionaryTable + 1 + i);

            // the dictionary is almost certainly in ROM, but just in case...
            if (dictionaryTable < romStart)
            {
                traps.Remove(dictionaryTable);
                traps.Add(dictionaryTable, n + 1, LoadWordSeparators);
            }
        }

        private void io_SizeChanged(object sender, EventArgs e)
        {
            SetByte(0x20, io.HeightChars);          // screen height (rows)
            SetByte(0x21, io.WidthChars);           // screen width (columns)
            SetWord(0x22, io.WidthUnits);           // screen width (units)
            SetWord(0x24, io.HeightUnits);          // screen height (units)
            SetByte(0x26, io.FontWidth);            // font width (units)
            SetByte(0x27, io.FontHeight);           // font height (units)
        }

        private short GetHeaderExtWord(int num)
        {
            ushort headerExt = (ushort)GetWord(0x36);
            if (headerExt == 0)
                return 0;

            ushort len = (ushort)GetWord(headerExt);
            if (num > len)
                return 0;

            return GetWord(headerExt + 2 * num);
        }

        private int UnpackAddress(short packedAddr)
        {
            if (zversion == 5)
                return 4 * (ushort)packedAddr;
            else
                return 8 * (ushort)packedAddr;
        }

        private void TrapMemory(ushort address, ushort length)
        {
            traps.Handle(address, length);
        }

        internal class CallFrame
        {
            public CallFrame(int returnPC, int prevStackDepth, int numLocals, int argCount,
                int resultStorage)
            {
                this.ReturnPC = returnPC;
                this.PrevStackDepth = prevStackDepth;
                this.Locals = new short[numLocals];
                this.ArgCount = argCount;
                this.ResultStorage = resultStorage;
            }

            public readonly int ReturnPC;
            public readonly int PrevStackDepth;
            public readonly short[] Locals;
            public readonly int ArgCount;
            public readonly int ResultStorage;

            public CallFrame Clone()
            {
                CallFrame result = new CallFrame(ReturnPC, PrevStackDepth, Locals.Length,
                    ArgCount, ResultStorage);
                Array.Copy(Locals, result.Locals, Locals.Length);
                return result;
            }
        }

        private class UndoState
        {
            private byte[] ram;
            private short[] savedStack;
            private CallFrame[] savedCallStack;
            private int savedPC;
            private byte savedDest;

            public UndoState(byte[] zmem, int ramLength, Stack<short> stack, Stack<CallFrame> callStack,
                int pc, byte dest)
            {
                ram = new byte[ramLength];
                Array.Copy(zmem, ram, ramLength);

                savedStack = stack.ToArray();
                savedCallStack = callStack.ToArray();
                for (int i = 0; i < savedCallStack.Length; i++)
                    savedCallStack[i] = savedCallStack[i].Clone();

                savedPC = pc;
                savedDest = dest;
            }

            public void Restore(byte[] zmem, Stack<short> stack, Stack<CallFrame> callStack,
                out int pc, out byte dest)
            {
                Array.Copy(ram, zmem, ram.Length);

                stack.Clear();
                for (int i = savedStack.Length - 1; i >= 0; i--)
                    stack.Push(savedStack[i]);

                callStack.Clear();
                for (int i = savedCallStack.Length - 1; i >= 0; i--)
                    callStack.Push(savedCallStack[i]);

                pc = savedPC;
                dest = savedDest;
            }
        }

        private delegate void MemoryTrapHandler();

        private class MemoryTraps
        {
            private List<int> starts = new List<int>();
            private List<int> lengths = new List<int>();
            private List<MemoryTrapHandler> handlers = new List<MemoryTrapHandler>();

            private int firstAddress = 0;
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
                int idx = starts.BinarySearch(trapStart);
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
                int idx = starts.BinarySearch(trapStart);
                if (idx >= 0)
                {
                    starts.RemoveAt(idx);
                    lengths.RemoveAt(idx);
                    handlers.RemoveAt(idx);

                    int count = starts.Count;
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
                int changeEnd = changeStart + changeLength - 1;

                if (changeStart > lastAddress || changeEnd < firstAddress)
                    return;

                /* the number of traps will be very limited, so we don't need to
				 * do anything fancy here. */
                int trapCount = starts.Count;
                for (int i = 0; i < trapCount; i++)
                {
                    int start = starts[i];
                    int len = lengths[i];
                    if (changeStart >= start && changeEnd < start + len)
                        handlers[i].Invoke();
                }
            }
        }
    }
}