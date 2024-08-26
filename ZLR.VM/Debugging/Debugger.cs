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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nito.AsyncEx;

namespace ZLR.VM.Debugging
{
    public enum DebuggerState
    {
        PausedOnEntry,
        Running,
        Terminated,

        PausedByBreakpoint,
        PausedByError,
        PausedByStep,
        PausedByUser,
    }

    [PublicAPI]
    public static class DebuggerStateExtensions
    {
        public static bool IsRunning(this DebuggerState state) => state == DebuggerState.Running;
        
        public static bool IsTerminated(this DebuggerState state) => state == DebuggerState.Terminated;

        public static bool IsPaused(this DebuggerState state)
        {
            switch (state)
            {
                case DebuggerState.PausedByBreakpoint:
                case DebuggerState.PausedByError:
                case DebuggerState.PausedByStep:
                case DebuggerState.PausedByUser:
                case DebuggerState.PausedOnEntry:
                    return true;

                case DebuggerState.Running:
                case DebuggerState.Terminated:
                default:
                    return false;
            }
        }
    }

    [PublicAPI]
    public sealed class EnterFunctionEventArgs : EventArgs
    {
        public short PackedAddress { get; }

        public IReadOnlyList<short>? Args { get; }

        public int ResultStorage { get; }
        public int ReturnPC { get; }
        public int CallDepth { get; }

        public EnterFunctionEventArgs(short packedAddress, [CanBeNull] IReadOnlyList<short>? args, int resultStorage, int returnPC, int callDepth)
        {
            PackedAddress = packedAddress;
            Args = args;
            ResultStorage = resultStorage;
            ReturnPC = returnPC;
            CallDepth = callDepth;
        }
    }

    [PublicAPI]
    public sealed class DebuggerStateEventArgs : EventArgs
    {
        public DebuggerState State { get; }

        public DebuggerStateEventArgs(DebuggerState state)
        {
            State = state;
        }
    }

    public interface IDebuggerEvents
    {
        event EventHandler<EnterFunctionEventArgs> EnteringFunction;
        event EventHandler<DebuggerStateEventArgs> DebuggerStateChanged;
    }

    public sealed class DebuggerBreakException : Exception
    {
        public DebuggerBreakException() : base("The debuggee was paused.")
        {
        }
    }

    [PublicAPI]
    public interface IDebugger
    {
        DebuggerState State { get; }

        void Restart();

        Task StepIntoAsync();
        Task StepOverAsync();
        Task StepUpAsync();

        Task RunAsync();
        Task PauseAsync();
        CancellationToken PauseCancellationToken { get; }

        void SetBreakpoint(int address, bool enabled);
        int[] GetBreakpoints();

        /// <summary>
        /// Calls a routine within the Z-machine, returning its result unless it's interrupted.
        /// </summary>
        /// <param name="packedAddress">The packed address of the routine to call.</param>
        /// <param name="args">The arguments to pass to the routine.</param>
        /// <returns>The value returned by the routine, or <see langword="null"/> if the
        /// call was interrupted by a debugger break.</returns>
        Task<short?> CallAsync(short packedAddress, [NotNull] short[] args);

        byte ReadByte(int address);
        short ReadWord(int address);
        void WriteByte(int address, byte value);
        void WriteWord(int address, short value);
        short ReadVariable(byte number);
        void WriteVariable(byte number, short value);

        string DecodeString(int address);

        ushort GetObjectAddress(ushort number);
        string GetObjectName(ushort number);
        void ParseObject(ushort address, [NotNull] out byte[] attrs, out ushort parent,
            out ushort sibling, out ushort child, out ushort propertyTable);
        void UpdateObject(ushort address, [CanBeNull] byte[] attrs, ushort? parent, ushort? sibling,
            ushort? child, ushort? propertyTable);
        void MoveObject(ushort obj, ushort newParent);
        void ParseProperty(ushort address, out short number, out short length);
        ushort GetPropAddress(ushort obj, short prop);
        short GetPropLength(ushort address);
        short GetNextProp(ushort obj, short prop);

        int CallDepth { get; }

        [ItemNotNull, NotNull]
        ICallFrame[] GetCallFrames();

        int CurrentPC { get; }

        [NotNull]
        string Disassemble(int address);

        int StackDepth { get; }
        void StackPush(short value);
        short StackPop();

        int UnpackAddress(short packedAddress, bool forString);
        short PackAddress(int address, bool forString);

        [NotNull]
        IDebuggerEvents Events { get; }
    }

    public interface ICallFrame
    {
        int ReturnPC { get; }
        int PrevStackDepth { get; }
        int ArgCount { get; }
        int ResultStorage { get; }

        [NotNull]
        short[] Locals { get; }
    }

    public static class DebuggerExtensions
    {
        [NotNull]
        public static byte[] GetObjectAttrs([NotNull] this IDebugger dbg, ushort obj)
        {
            dbg.ParseObject(dbg.GetObjectAddress(obj), out var attrs, out _, out _, out _, out _);
            return attrs;
        }

        public static ushort GetObjectParent([NotNull] this IDebugger dbg, ushort obj)
        {
            dbg.ParseObject(dbg.GetObjectAddress(obj), out _, out var parent, out _, out _, out _);
            return parent;
        }

        public static ushort GetObjectSibling([NotNull] this IDebugger dbg, ushort obj)
        {
            dbg.ParseObject(dbg.GetObjectAddress(obj), out _, out _, out var sibling, out _, out _);
            return sibling;
        }

        public static ushort GetObjectChild([NotNull] this IDebugger dbg, ushort obj)
        {
            dbg.ParseObject(dbg.GetObjectAddress(obj), out _, out _, out _, out var child, out _);
            return child;
        }

        public static ushort GetObjectPropertyTable([NotNull] this IDebugger dbg, ushort obj)
        {
            dbg.ParseObject(dbg.GetObjectAddress(obj), out _, out _, out _, out _, out var propertyTable);
            return propertyTable;
        }

        public static bool TestObjectAttribute([NotNull] this IDebugger dbg, ushort obj, int attr)
        {
            var attrs = dbg.GetObjectAttrs(obj);
            var bit = 128 >> (attr & 7);
            var offset = attr >> 3;
            return (attrs[offset] & bit) != 0;
        }

        private static void UpdateObjectAttribute([NotNull] IDebugger dbg, ushort obj, int attr, bool set)
        {
            var objAddr = dbg.GetObjectAddress(obj);

            dbg.ParseObject(objAddr, out var attrs, out _, out _, out _, out _);

            var bit = 128 >> (attr & 7);
            var offset = attr >> 3;

            if (set)
                attrs[offset] |= (byte) bit;
            else
                attrs[offset] &= (byte) ~bit;

            dbg.UpdateObject(objAddr, attrs, null, null, null, null);
        }

        public static void SetObjectAttribute([NotNull] this IDebugger dbg, ushort obj, int attr) =>
            UpdateObjectAttribute(dbg, obj, attr, true);

        public static void ClearObjectAttribute([NotNull] this IDebugger dbg, ushort obj, int attr) =>
            UpdateObjectAttribute(dbg, obj, attr, false);
    }
}

namespace ZLR.VM
{
    using Debugging;

    partial class ZMachine : IDebuggerEvents
    {
        private int stepping = -1;
        private readonly HashSet<int> breakpoints = new HashSet<int>();

        private DebuggerState debugState;

        private DebuggerState DebuggerState
        {
            get => debugState;

            set
            {
                debugState = value;
                HandleDebuggerStateChanged(value);
            }
        }

        [NotNull]
        public IDebugger Debug()
        {
            debugging = true;
            cache?.Clear();

            return new Debugger(this);
        }

#pragma warning disable 0169
        private bool DebugCheck(int pcToCheck)
        {
            if (stepping >= 0)
            {
                if (--stepping < 0)
                {
                    pc = pcToCheck;
                    return true;
                }
            }
            else if (breakpoints.Contains(pcToCheck))
            {
                pc = pcToCheck;
                this.DebuggerState = DebuggerState.PausedByBreakpoint;
                return true;
            }
            else if (interruptToken.IsCancellationRequested)
            {
                pc = pcToCheck;
                this.DebuggerState = DebuggerState.PausedByUser;
                return true;
            }

            // continue
            return false;
        }
#pragma warning restore 0169

        private class Debugger : IDebugger
        {
            private readonly ZMachine zm;
            private readonly AsyncManualResetEvent whenStopped = new AsyncManualResetEvent(true);

            public Debugger(ZMachine zm)
            {
                this.zm = zm;
            }

            #region IDebugger Members

            public DebuggerState State => zm.DebuggerState;

            public CancellationToken PauseCancellationToken => zm.interruptToken;

            public void Restart()
            {
                zmachineInterruptSource.Cancel();
                whenStopped.Set();

                zm.Restart();
                if (zm.cache == null)
                    zm.cache = new LruCache<int, CachedCode>(zm.cacheSize);
                zm.DebuggerState = DebuggerState.PausedOnEntry;
            }

            private async Task OneStepAsync()
            {
                var thisPC = zm.pc;
                if (thisPC < zm.RomStart || zm.cache.TryGetValue(thisPC, out var entry) == false)
                {
                    var (code, nextPC, count) = zm.CompileZCode();
                    entry = new CachedCode(nextPC, code);
                    if (thisPC >= zm.RomStart)
                        zm.cache.Add(thisPC, entry, count);
                }

                zm.pc = entry.NextPC;
                try
                {
                    var task = entry.Code();
                    if (task != null)
                        await task;
                }
                catch (DebuggerBreakException)
                {
                    // OK
                }
            }

            private async Task Step([NotNull] Func<Task> operation)
            {
                try
                {
                    whenStopped.Reset();
                    zmachineInterruptSource = new CancellationTokenSource();
                    zm.interruptToken = zmachineInterruptSource.Token;

                    zm.stepping = 1;
                    zm.running = true;
                    zm.DebuggerState = DebuggerState.Running;

                    await operation().ConfigureAwait(false);

                    zm.stepping = -1;
                    if (!zm.running)
                    {
                        zm.DebuggerState = DebuggerState.Terminated;
                    }
                    else if (zm.DebuggerState.IsRunning())
                    {
                        zm.DebuggerState = DebuggerState.PausedByStep;
                    }
                }
                finally
                {
                    whenStopped.Set();
                }
            }

            [NotNull]
            public Task StepIntoAsync()
            {
                return Step(OneStepAsync);
            }

            [NotNull]
            public Task StepOverAsync()
            {
                return Step(async () =>
                {
                    var callDepth = zm.callStack.Count;
                    await OneStepAsync().ConfigureAwait(false);

                    while (zm.callStack.Count > callDepth && zm.running && zm.DebuggerState.IsRunning())
                        await OneStepAsync().ConfigureAwait(false);
                });
            }

            [NotNull]
            public Task StepUpAsync()
            {
                return Step(async () =>
                {
                    var callDepth = zm.callStack.Count;
                    await OneStepAsync().ConfigureAwait(false);

                    while (zm.callStack.Count >= callDepth && zm.running && zm.DebuggerState.IsRunning())
                        await OneStepAsync().ConfigureAwait(false);
                });
            }

            [NotNull]
            private CancellationTokenSource zmachineInterruptSource = new CancellationTokenSource();

            public async Task RunAsync()
            {
                // TODO: merge with Step()
                try
                {
                    whenStopped.Reset();
                    zmachineInterruptSource = new CancellationTokenSource();
                    zm.interruptToken = zmachineInterruptSource.Token;

                    // step past a breakpoint on the current line, if we're continuing
                    if (zm.breakpoints.Contains(zm.pc) && zm.DebuggerState != DebuggerState.PausedOnEntry)
                        await StepIntoAsync().ConfigureAwait(false);

                    zm.running = true;
                    zm.DebuggerState = DebuggerState.Running;
                    while (zm.running && zm.DebuggerState.IsRunning())
                        await OneStepAsync().ConfigureAwait(false);

                    if (!zm.running)
                        zm.DebuggerState = DebuggerState.Terminated;
                }
                finally
                {
                    whenStopped.Set();
                }
            }

            public async Task PauseAsync()
            {
                zmachineInterruptSource.Cancel();
                // ReSharper disable once MethodSupportsCancellation
                await whenStopped.WaitAsync().ConfigureAwait(false);
            }

            public void SetBreakpoint(int address, bool enabled)
            {
                if (enabled)
                    zm.breakpoints.Add(address);
                else
                    zm.breakpoints.Remove(address);
            }

            [NotNull]
            public int[] GetBreakpoints() => zm.breakpoints.ToArray();

            public async Task<short?> CallAsync(short packedAddress, short[] args)
            {
                zm.running = true;
                zm.EnterFunctionImpl(packedAddress, args, 0, zm.pc);
                try
                {
                    await zm.JitLoopAsync().ConfigureAwait(false);
                    return zm.stack.Pop();
                }
                catch (DebuggerBreakException)
                {
                    return null;
                }
            }

            public byte ReadByte(int address) => zm.zmem[address];

            public short ReadWord(int address) => (short) ((zm.zmem[address] << 8) | zm.zmem[address + 1]);

            public void WriteByte(int address, byte value)
            {
                zm.zmem[address] = value;
            }

            public void WriteWord(int address, short value)
            {
                zm.zmem[address] = (byte) (value >> 8);
                zm.zmem[address + 1] = (byte) value;
            }

            public short ReadVariable(byte number)
            {
                if (number == 0)
                {
                    return zm.stack.Peek();
                }

                if (number < 16)
                {
                    return zm.TopFrame.Locals[number - 1];
                }

                return zm.GetWord(zm.GlobalsOffset + 2 * (number - 16));
            }

            public void WriteVariable(byte number, short value)
            {
                if (number == 0)
                {
                    zm.stack.Pop();
                    zm.stack.Push(value);
                }
                else if (number < 16)
                {
                    zm.TopFrame.Locals[number - 1] = value;
                }
                else
                {
                    zm.SetWord(zm.GlobalsOffset + 2 * (number - 16), value);
                }
            }

            [NotNull]
            public string DecodeString(int address) => zm.DecodeString(address);

            public ushort GetObjectAddress(ushort number) => zm.GetObjectAddress(number);

            [NotNull]
            public string GetObjectName(ushort number) => zm.GetObjectName(number);

            // TODO: use System.Memory and/or ref returns to provide direct access?
            public void ParseObject(ushort address, out byte[] attrs,
                out ushort parent, out ushort sibling, out ushort child, out ushort propertyTable)
            {
                if (zm.zversion <= 3)
                {
                    attrs = new[]
                    {
                        zm.GetByte(address),
                        zm.GetByte(address + 1),
                        zm.GetByte(address + 2),
                        zm.GetByte(address + 3),
                    };
                    parent = zm.GetByte(address + 4);
                    sibling = zm.GetByte(address + 5);
                    child = zm.GetByte(address + 6);
                    propertyTable = (ushort) zm.GetWord(address + 7);
                }
                else
                {
                    attrs = new[]
                    {
                        zm.GetByte(address),
                        zm.GetByte(address + 1),
                        zm.GetByte(address + 2),
                        zm.GetByte(address + 3),
                        zm.GetByte(address + 4),
                        zm.GetByte(address + 5),
                    };
                    parent = (ushort) zm.GetWord(address + 6);
                    sibling = (ushort) zm.GetWord(address + 8);
                    child = (ushort) zm.GetWord(address + 10);
                    propertyTable = (ushort) zm.GetWord(address + 12);
                }
            }

            public void UpdateObject(ushort address, byte[] attrs, ushort? parent, ushort? sibling,
                ushort? child, ushort? propertyTable)
            {
                if (zm.zversion <= 3)
                {
                    if (attrs != null)
                    {
                        if (attrs.Length != 4)
                            throw new ArgumentException("Expected 4 bytes of attributes", nameof(attrs));

                        zm.SetByte(address, attrs[0]);
                        zm.SetByte(address + 1, attrs[1]);
                        zm.SetByte(address + 2, attrs[2]);
                        zm.SetByte(address + 3, attrs[3]);
                    }

                    if (parent != null)
                        zm.SetByte(address + 4, (byte) parent);

                    if (sibling != null)
                        zm.SetByte(address + 5, (byte) sibling);

                    if (child != null)
                        zm.SetByte(address + 6, (byte) child);

                    if (propertyTable != null)
                        zm.SetWord(address + 7, (short) propertyTable);
                }
                else
                {
                    if (attrs != null)
                    {
                        if (attrs.Length != 6)
                            throw new ArgumentException("Expected 6 bytes of attributes", nameof(attrs));

                        zm.SetByte(address, attrs[0]);
                        zm.SetByte(address + 1, attrs[1]);
                        zm.SetByte(address + 2, attrs[2]);
                        zm.SetByte(address + 3, attrs[3]);
                        zm.SetByte(address + 4, attrs[4]);
                        zm.SetByte(address + 5, attrs[5]);
                    }

                    if (parent != null)
                        zm.SetWord(address + 6, (short) parent);

                    if (sibling != null)
                        zm.SetWord(address + 8, (short) sibling);

                    if (child != null)
                        zm.SetWord(address + 10, (short) child);

                    if (propertyTable != null)
                        zm.SetWord(address + 12, (short) propertyTable);
                }
            }

            public void MoveObject(ushort obj, ushort newParent) => zm.InsertObject(obj, newParent);

            public void ParseProperty(ushort address, out short number, out short length)
            {
                length = zm.GetPropLength(address);
                number = (short) (zm.GetByte(address) & (zm.zversion <= 3 ? 31 : 63));
            }

            public ushort GetPropAddress(ushort obj, short prop) => zm.GetPropAddr(obj, prop);

            public short GetPropLength(ushort address) => zm.GetPropLength(address);

            public short GetNextProp(ushort obj, short prop) => zm.GetNextProp(obj, prop);

            public int CallDepth => zm.callStack.Count;

            public ICallFrame[] GetCallFrames() => zm.callStack.ToArray<ICallFrame>();

            public int CurrentPC => zm.pc;

            public string Disassemble(int address)
            {
                var opc = zm.pc;
                try
                {
                    zm.pc = address;
                    var types = new OperandType[8];
                    var argv = new short[8];

                    var opcode = zm.DecodeOneOp(types, argv);

                    var rtn = zm.DebugInfo?.FindRoutine(address);

                    return opcode.Disassemble(varnum =>
                    {
                        if (rtn != null && varnum - 1 < rtn.Locals.Length)
                            return "local_" + varnum + "(" + rtn.Locals[varnum - 1] + ")";

                        if (varnum < 16)
                            return "local_" + varnum;

                        if (zm.DebugInfo != null && zm.DebugInfo.Globals.Contains((byte) (varnum - 16)))
                            return "global_" + varnum + "(" + zm.DebugInfo.Globals[(byte) (varnum - 16)] + ")";

                        return "global_" + varnum;
                    });
                }
                finally
                {
                    zm.pc = opc;
                }
            }

            public int StackDepth => zm.stack.Count;

            public void StackPush(short value) => zm.stack.Push(value);

            public short StackPop() => zm.stack.Pop();

            public int UnpackAddress(short packedAddress, bool forString) => zm.UnpackAddress(packedAddress, forString);

            public short PackAddress(int address, bool forString)
            {
                switch (zm.zversion)
                {
                    case 1:
                    case 2:
                    case 3:
                        return (short) (address / 2);

                    case 4:
                    case 5:
                        return (short) (address / 4);

                    case 6:
                    case 7:
                        const int HDR_CODE_OFFSET = 0x28;
                        const int HDR_STR_OFFSET = 0x2A;
                        var offset = ReadWord(forString ? HDR_STR_OFFSET : HDR_CODE_OFFSET) * 8;
                        return (short) ((address - offset) / 4);

                    case 8:
                        return (short) (address / 8);

                    default:
                        throw new NotImplementedException();
                }
            }

            public IDebuggerEvents Events => zm;

            #endregion
        }

        #region IDebuggerEvents Members

        public event EventHandler<EnterFunctionEventArgs>? EnteringFunction;
        public event EventHandler<DebuggerStateEventArgs>? DebuggerStateChanged;

        #endregion

        private void HandleEnterFunction(short packedAddress, short[]? args, int resultStorage, int returnPC)
        {
            EnteringFunction?.Invoke(this, new EnterFunctionEventArgs(
                packedAddress, args, resultStorage, returnPC, callStack.Count));
        }

        private void HandleDebuggerStateChanged(DebuggerState state)
        {
            DebuggerStateChanged?.Invoke(this, new DebuggerStateEventArgs(state));
        }
    }
}