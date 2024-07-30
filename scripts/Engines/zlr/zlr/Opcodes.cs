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
using System.Reflection;
using System.Reflection.Emit;

namespace ZLR.VM
{
    internal enum OpForm { Long, Short, Ext, Var };
    internal enum OpCount { Zero, One, Two, Var, Ext };
    internal enum OperandType : byte
    {
        LargeConst = 0,
        SmallConst = 1,
        Variable = 2,
        Omitted = 3
    }

    internal delegate void OpcodeCompiler(Opcode thisptr, ILGenerator il);

    internal struct OpcodeInfo
    {
        public OpcodeAttribute Attr;
        public OpcodeCompiler Compiler;

        public OpcodeInfo(OpcodeAttribute attr, OpcodeCompiler compiler)
        {
            this.Attr = attr;
            this.Compiler = compiler;
        }
    }

    internal partial class Opcode
    {
        public readonly int PC, ZCodeLength;
        private readonly OpcodeCompiler compiler;
        private readonly OpcodeAttribute attribute;
        private readonly ZMachine zm;
        private readonly int argc;
        private readonly OperandType[] operandTypes;
        private readonly short[] operandValues;
        private readonly string operandText;
        private readonly int resultStorage;
        private readonly bool branchIfTrue;
        private readonly int branchOffset;

        private bool compiling;

        // fields for ZMachine to use to string opcodes together
        public Opcode Next;
        public Opcode Target;
        public Label Label;

        public Opcode(ZMachine zm, OpcodeCompiler compiler, OpcodeAttribute attribute,
            int pc, int zCodeLength,
            int argc, OperandType[] operandTypes, short[] operandValues,
            string operandText, int resultStorage, bool branchIfTrue, int branchOffset)
        {
            this.zm = zm;
            this.compiler = compiler;
            this.attribute = attribute;
            this.PC = pc;
            this.ZCodeLength = zCodeLength;
            this.argc = argc;
            this.operandTypes = new OperandType[argc];
            Array.Copy(operandTypes, this.operandTypes, argc);
            this.operandValues = new short[argc];
            Array.Copy(operandValues, this.operandValues, argc);
            this.operandText = operandText;
            this.resultStorage = resultStorage;
            this.branchIfTrue = branchIfTrue;
            this.branchOffset = branchOffset;
        }

        public void Compile(ILGenerator il, ref bool compiling)
        {
            this.compiling = true;
            compiler.Invoke(this, il);
            compiling = this.compiling;
        }

        public override string ToString()
        {
            return OpcodeName(compiler);
        }

        /// <summary>
        /// Gets a value indicating whether the opcode is a conditional branch,
        /// not including rtrue/rfalse or unconditional jumps.
        /// </summary>
        public bool IsBranch
        {
            get { return attribute.Branch && branchOffset != 0 && branchOffset != 1; }
        }

        /// <summary>
        /// Gets a value indicating whether the opcode is a branch that will always be taken.
        /// </summary>
        public bool IsUnconditionalJump
        {
            get
            {
                if (attribute.OpCount == OpCount.One && attribute.Number == 140)
                {
                    // op_jump is unconditional unless the operand is a variable
                    return (operandTypes[0] != OperandType.Variable);
                }

                return false;
            }
        }

        public int BranchOffset
        {
            get
            {
                if (attribute.Branch)
                    return this.branchOffset;
                else
                    return operandValues[0];
            }
        }

        /// <summary>
        /// Gets a value indicating whether the opcode is a fragment terminator:
        /// the following instruction must not be compiled together with it.
        /// </summary>
        /// <remarks>
        /// Usually, this means the opcode changes PC at run time by taking a
        /// calculated branch, entering or leaving a routine, etc.
        /// </remarks>
        public bool IsTerminator
        {
            get { return attribute.Terminates; }
        }

        #region Static - Opcode Dictionary

        private static Dictionary<byte, OpcodeInfo> oneOpInfos = new Dictionary<byte, OpcodeInfo>();
        private static Dictionary<byte, OpcodeInfo> twoOpInfos = new Dictionary<byte, OpcodeInfo>();
        private static Dictionary<byte, OpcodeInfo> zeroOpInfos = new Dictionary<byte, OpcodeInfo>();
        private static Dictionary<byte, OpcodeInfo> varOpInfos = new Dictionary<byte, OpcodeInfo>();
        private static Dictionary<byte, OpcodeInfo> extOpInfos = new Dictionary<byte, OpcodeInfo>();

        static Opcode()
        {
            InitOpcodeTable();
        }

        private static void InitOpcodeTable()
        {
            MethodInfo[] mis = typeof(Opcode).GetMethods(
                BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (MethodInfo mi in mis)
            {
                OpcodeAttribute[] attrs = (OpcodeAttribute[])mi.GetCustomAttributes(typeof(OpcodeAttribute), false);
                if (attrs.Length > 0)
                {
                    OpcodeCompiler del = (OpcodeCompiler)Delegate.CreateDelegate(
                        typeof(OpcodeCompiler), null, mi);
                    OpcodeAttribute a = attrs[0];
                    OpcodeInfo info = new OpcodeInfo(a, del);
                    switch (a.OpCount)
                    {
                        case OpCount.Zero:
                            zeroOpInfos.Add((byte)(a.Number - 176), info);
                            break;
                        case OpCount.One:
                            oneOpInfos.Add((byte)(a.Number - 128), info);
                            break;
                        case OpCount.Two:
                            twoOpInfos.Add(a.Number, info);
                            break;
                        case OpCount.Var:
                            varOpInfos.Add((byte)(a.Number - 224), info);
                            break;
                        case OpCount.Ext:
                            extOpInfos.Add(a.Number, info);
                            break;
                    }
                }
            }
        }

        public static bool FindOpcodeInfo(OpCount count, byte opnum, out OpcodeInfo result)
        {
            Dictionary<byte, OpcodeInfo> dict;

            switch (count)
            {
                case OpCount.Zero:
                    dict = zeroOpInfos;
                    break;
                case OpCount.One:
                    dict = oneOpInfos;
                    break;
                case OpCount.Two:
                    dict = twoOpInfos;
                    break;
                case OpCount.Var:
                    dict = varOpInfos;
                    break;
                case OpCount.Ext:
                    dict = extOpInfos;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("count");
            }

            return dict.TryGetValue(opnum, out result);
        }

        public static string OpcodeName(OpcodeCompiler handler)
        {
            if (handler == null)
                return "<unknown>";

            MethodInfo mi = handler.Method;
            string name = mi.Name;
            if (name.StartsWith("op_"))
                return name.Remove(0, 3);
            else
                return name;
        }

        #endregion

        private void LoadOperand(ILGenerator il, int num)
        {
            OperandType type;
            short value;

            if (num < argc)
            {
                type = operandTypes[num];
                value = operandValues[num];
            }
            else
            {
                type = OperandType.Omitted;
                value = 0;
            }

            switch (type)
            {
                case OperandType.Omitted:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;

                case OperandType.SmallConst:
                case OperandType.LargeConst:
                    switch (value)
                    {
                        case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                        case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                        case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                        case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                        case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                        case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                        case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                        case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                        case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                        case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                        default:
                            if (value >= 0 && value < 128)
                                il.Emit(OpCodes.Ldc_I4_S, (byte)value);
                            else
                                il.Emit(OpCodes.Ldc_I4, (int)value);
                            break;
                    }
                    break;

                case OperandType.Variable:
                    if (value == 0)
                    {
                        PopFromStack(il);
                    }
                    else if (value < 16)
                    {
                        LoadLocals(il);
                        il.Emit(OpCodes.Ldc_I4_S, (byte)(value - 1));
                        il.Emit(OpCodes.Ldelem_I2);
                    }
                    else
                    {
                        MethodInfo getWordMI = typeof(ZMachine).GetMethod("GetWord", BindingFlags.NonPublic | BindingFlags.Instance);
                        int address = zm.GlobalsOffset + 2 * (value - 16);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldc_I4, address);
                        il.Emit(OpCodes.Call, getWordMI);
                    }
                    break;
            }
        }

        private void StoreResult(ILGenerator il)
        {
            if (resultStorage == -1)
                throw new InvalidOperationException("Storing from a non-store instruction");

            StoreResult(il, (byte)resultStorage);
        }

        private void StoreResult(ILGenerator il, byte dest)
        {
            if (dest == 0)
            {
                PushOntoStack(il);
            }
            else if (dest < 16)
            {
                il.Emit(OpCodes.Stloc, zm.TempWordLocal);
                LoadLocals(il);
                il.Emit(OpCodes.Ldc_I4_S, (byte)(dest - 1));
                il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
                il.Emit(OpCodes.Stelem_I2);
            }
            else
            {
                MethodInfo setWordMI = typeof(ZMachine).GetMethod("SetWord", BindingFlags.NonPublic | BindingFlags.Instance);
                int address = zm.GlobalsOffset + 2 * (dest - 16);
                il.Emit(OpCodes.Stloc, zm.TempWordLocal);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, address);
                il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
                il.Emit(OpCodes.Call, setWordMI);
            }
        }

        private void PopFromStack(ILGenerator il)
        {
            MethodInfo popMI = typeof(Stack<short>).GetMethod("Pop");

            il.Emit(OpCodes.Ldloc, zm.StackLocal);
            il.Emit(OpCodes.Call, popMI);
        }

        private void PushOntoStack(ILGenerator il)
        {
            MethodInfo pushMI = typeof(Stack<short>).GetMethod("Push");

            il.Emit(OpCodes.Stloc, zm.TempWordLocal);
            il.Emit(OpCodes.Ldloc, zm.StackLocal);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.Call, pushMI);
        }

        private void LoadLocals(ILGenerator il)
        {
            il.Emit(OpCodes.Ldloc, zm.LocalsLocal);
        }

        /// <summary>
        /// Generates code to enter a function.
        /// </summary>
        /// <param name="argc">The number of arguments, including the function address. Must be at least 1.</param>
        /// <param name="operandTypes">An array of operand types.</param>
        /// <param name="argv">An array of operand values or addresses.</param>
        /// <param name="store">If true, a storage location will be read from the current PC (and PC
        /// will be advanced); otherwise, the function result will be discarded.</param>
        private void EnterFunction(ILGenerator il, bool store)
        {
            int dest = store ? resultStorage : -1;
            EnterFunction(il, dest);
        }

        private void EnterFunction(ILGenerator il, int dest)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("EnterFunctionImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            // if the first operand is sp, save it in the temp local, since we don't use it until later
            bool addressInTemp = false;
            if (operandTypes[0] == OperandType.Variable && operandValues[0] == 0)
            {
                PopFromStack(il);
                il.Emit(OpCodes.Stloc, zm.TempWordLocal);
                addressInTemp = true;
            }

            System.Diagnostics.Debug.Assert(argc >= 1);

            if (argc > 1)
            {
                il.Emit(OpCodes.Ldc_I4, argc - 1);
                il.Emit(OpCodes.Newarr, typeof(short));
                il.Emit(OpCodes.Stloc, zm.TempArrayLocal);

                for (int i = 1; i < argc; i++)
                {
                    il.Emit(OpCodes.Ldloc, zm.TempArrayLocal);
                    il.Emit(OpCodes.Ldc_I4, i - 1);
                    LoadOperand(il, i);
                    il.Emit(OpCodes.Stelem_I2);
                }
            }

            // self
            il.Emit(OpCodes.Ldarg_0);
            // address
            if (addressInTemp)
                il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            else
                LoadOperand(il, 0);
            // args
            if (argc > 1)
                il.Emit(OpCodes.Ldloc, zm.TempArrayLocal);
            else
                il.Emit(OpCodes.Ldnull);
            // resultStorage
            if (dest >= 0)
                il.Emit(OpCodes.Ldc_I4, dest);
            else
                il.Emit(OpCodes.Ldc_I4_M1);
            // returnPC
            il.Emit(OpCodes.Ldc_I4, zm.PC);
            // EnterFunctionImpl()
            il.Emit(OpCodes.Call, impl);

            il.Emit(OpCodes.Ret);
            compiling = false;
        }

        private void LeaveFunction(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("LeaveFunctionImpl", BindingFlags.NonPublic | BindingFlags.Instance);
            il.Emit(OpCodes.Stloc, zm.TempWordLocal);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.Call, impl);
            compiling = false;
        }

        private void LeaveFunctionConst(ILGenerator il, short result)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("LeaveFunctionImpl", BindingFlags.NonPublic | BindingFlags.Instance);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, (int)result);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Ret);
            compiling = false;
        }

        // conditional version
        private void Branch(ILGenerator il, OpCode ifTrue, OpCode ifFalse)
        {
            if (branchOffset == int.MinValue)
                throw new InvalidOperationException("Branching from non-branch opcode");

            if (Target != null)
            {
                il.Emit(branchIfTrue ? ifTrue : ifFalse, Target.Label);
                return;
            }

            // do it the hard way
            Label skipBranch = il.DefineLabel();
            il.Emit(branchIfTrue ? ifFalse : ifTrue, skipBranch);

            if (branchOffset == 0)
            {
                LeaveFunctionConst(il, 0);
                il.Emit(OpCodes.Ret);
                compiling = true;
            }
            else if (branchOffset == 1)
            {
                LeaveFunctionConst(il, 1);
                il.Emit(OpCodes.Ret);
                compiling = true;
            }
            else
            {
                FieldInfo pcFI = typeof(ZMachine).GetField("pc", BindingFlags.NonPublic | BindingFlags.Instance);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, zm.PC + branchOffset - 2);
                il.Emit(OpCodes.Stfld, pcFI);
                il.Emit(OpCodes.Ret);
                compiling = false;
            }

            il.MarkLabel(skipBranch);
        }

        // unconditional version
        private void Branch(ILGenerator il)
        {
            if (branchOffset == int.MinValue)
                throw new InvalidOperationException("Branching from non-branch opcode");

            if (Target != null)
            {
                il.Emit(OpCodes.Br, Target.Label);
                return;
            }

            // do it the hard way
            if (branchOffset == 0)
            {
                LeaveFunctionConst(il, 0);
            }
            else if (branchOffset == 1)
            {
                LeaveFunctionConst(il, 1);
            }
            else
            {
                FieldInfo pcFI = typeof(ZMachine).GetField("pc", BindingFlags.NonPublic | BindingFlags.Instance);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, zm.PC + branchOffset - 2);
                il.Emit(OpCodes.Stfld, pcFI);
            }

            compiling = false;
        }

        private void BinaryOperation(ILGenerator il, OpCode op)
        {
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(op);
            StoreResult(il);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal class OpcodeAttribute : Attribute
    {
        public OpcodeAttribute(OpCount count, byte opnum)
            : this(count, opnum, false, false, false)
        {
        }

        public OpcodeAttribute(OpCount count, byte opnum, bool store)
            : this(count, opnum, store, false, false)
        {
        }

        public OpcodeAttribute(OpCount count, byte opnum,
            bool store, bool branch, bool text)
        {
            _count = count;
            _opnum = opnum;
            _store = store;
            _branch = branch;
            _text = text;
        }

        private OpCount _count;
        private byte _opnum;
        private bool _store, _branch, _text;
        private bool _noReturn;

        public OpCount OpCount { get { return _count; } }
        public byte Number { get { return _opnum; } }
        public bool Store { get { return _store; } }
        public bool Branch { get { return _branch; } }
        public bool Text { get { return _text; } }

        public bool Terminates
        {
            get { return _noReturn; }
            set { _noReturn = value; }
        }
    }
}