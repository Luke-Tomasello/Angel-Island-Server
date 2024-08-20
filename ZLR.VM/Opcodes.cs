using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Reflection.Emit;
using JetBrains.Annotations;
using System.Diagnostics;

namespace ZLR.VM
{
    internal enum OpForm { Long, Short, Ext, Var }
    internal enum OpCount { Zero, One, Two, Var, Ext }
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
        public readonly OpcodeAttribute Attr;
        public readonly OpcodeCompiler Compiler;

        public OpcodeInfo(OpcodeAttribute attr, OpcodeCompiler compiler)
        {
            Attr = attr;
            Compiler = compiler;
        }
    }

    internal delegate string VariableNameProvider(byte num);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "opcode names", Scope = "class")]
    internal partial class Opcode
    {
        public readonly int PC, ZCodeLength;
        private readonly OpcodeCompiler compiler;
        private readonly OpcodeAttribute attribute;
        private readonly ZMachine zm;
        private readonly int argc;
        private readonly OperandType[] operandTypes;
        private readonly short[] operandValues;
        private readonly string? operandText;
        private readonly int resultStorage;
        private readonly bool branchIfTrue;
        private readonly int branchOffset;

        private bool compiling;

        // fields for ZMachine to use to string opcodes together
        public Opcode? Next;
        public Opcode? Target;
        public Label Label;

        public Opcode(ZMachine zm, OpcodeCompiler compiler, OpcodeAttribute attribute,
            int pc, int zCodeLength,
            int argc, [NotNull] OperandType[] operandTypes, [NotNull] short[] operandValues,
            string? operandText, int resultStorage, bool branchIfTrue, int branchOffset)
        {
            this.zm = zm;
            this.compiler = compiler;
            this.attribute = attribute;
            PC = pc;
            ZCodeLength = zCodeLength;
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

        public bool Compile(ILGenerator il)
        {
            compiling = true;
            compiler.Invoke(this, il);
            return compiling;
        }

        // ReSharper disable once AnnotateNotNullTypeMember
        public override string ToString()
        {
            return GetOpcodeName(attribute, compiler);
        }

        [NotNull]
        public string Disassemble([NotNull] VariableNameProvider varNamer)
        {
            var sb = new StringBuilder();
            sb.Append(GetOpcodeName(attribute, compiler));

            for (var i = 0; i < argc; i++)
            {
                sb.Append(' ');

                if (i == 0 && attribute.IndirectVar)
                {
                    switch (operandTypes[i])
                    {
                        case OperandType.LargeConst:
                        case OperandType.SmallConst:
                            sb.Append('\'');
                            sb.Append(operandValues[i] == 0 ? "sp" : varNamer((byte) operandValues[i]));
                            break;

                        case OperandType.Variable:
                            sb.Append('[');
                            sb.Append(operandValues[i] == 0 ? "sp" : varNamer((byte) operandValues[i]));
                            sb.Append(']');
                            break;
                    }
                }
                else
                {
                    switch (operandTypes[i])
                    {
                        case OperandType.LargeConst:
                            sb.Append("long_");
                            sb.Append(operandValues[i]);
                            break;

                        case OperandType.SmallConst:
                            sb.Append("short_");
                            sb.Append(operandValues[i]);
                            break;

                        case OperandType.Variable:
                            sb.Append(operandValues[i] == 0 ? "sp" : varNamer((byte) operandValues[i]));
                            break;
                    }
                }
            }

            if (attribute.Text)
            {
                string tstr;
                Debug.Assert(operandText != null);
                if (operandText.Length <= 10)
                    tstr = operandText;
                else
                    tstr = operandText.Substring(0, 7) + "...";
                sb.Append(" \"");
                sb.Append(tstr);
                sb.Append('"');
            }

            if (attribute.Store)
            {
                sb.Append(" -> ");
                sb.Append(resultStorage == 0 ? "sp" : varNamer((byte) resultStorage));
            }

            if (attribute.Branch)
            {
                sb.Append(" ?");
                if (!branchIfTrue)
                    sb.Append('~');

                switch (branchOffset)
                {
                    case 0:
                        sb.Append("rfalse");
                        break;

                    case 1:
                        sb.Append("rtrue");
                        break;

                    default:
                        var off = branchOffset - 2;
                        if (off >= 0)
                            sb.Append('+');
                        sb.Append(off);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a value indicating whether the opcode is a conditional branch,
        /// not including rtrue/rfalse or unconditional jumps.
        /// </summary>
        public bool IsBranch => attribute.Branch && branchOffset != 0 && branchOffset != 1;

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
                    return operandTypes[0] != OperandType.Variable;
                }

                return false;
            }
        }

        public int BranchOffset => attribute.Branch ? branchOffset : operandValues[0];

        /// <summary>
        /// Gets a value indicating whether the opcode is a fragment terminator:
        /// the following instruction must not be compiled together with it.
        /// </summary>
        /// <remarks>
        /// Usually, this means the opcode changes PC at run time by taking a
        /// calculated branch, entering or leaving a routine, etc.
        /// </remarks>
        public bool IsTerminator => attribute.Terminates || attribute.Async;

        #region Static - Opcode Dictionary

        private static readonly Dictionary<byte, OpcodeInfo[]> OneOpInfos = new Dictionary<byte, OpcodeInfo[]>();
        private static readonly Dictionary<byte, OpcodeInfo[]> TwoOpInfos = new Dictionary<byte, OpcodeInfo[]>();
        private static readonly Dictionary<byte, OpcodeInfo[]> ZeroOpInfos = new Dictionary<byte, OpcodeInfo[]>();
        private static readonly Dictionary<byte, OpcodeInfo[]> VarOpInfos = new Dictionary<byte, OpcodeInfo[]>();
        private static readonly Dictionary<byte, OpcodeInfo[]> ExtOpInfos = new Dictionary<byte, OpcodeInfo[]>();

        static Opcode()
        {
            InitOpcodeTable();
        }

        private static void InitOpcodeTable()
        {
            var mis = typeof(Opcode).GetMethods(
                BindingFlags.NonPublic|BindingFlags.Instance);

            foreach (var mi in mis)
            {
                var attrs = (OpcodeAttribute[])mi.GetCustomAttributes(typeof(OpcodeAttribute), false);
                if (attrs.Length > 0)
                {
                    var del = (OpcodeCompiler)Delegate.CreateDelegate(
                        typeof(OpcodeCompiler), null, mi);
                    foreach (var a in attrs)
                    {
                        var info = new OpcodeInfo(a, del);

                        byte num;
                        Dictionary<byte, OpcodeInfo[]> dict;

                        switch (a.OpCount)
                        {
                            case OpCount.Zero:
                                dict = ZeroOpInfos;
                                num = (byte)(a.Number - 176);
                                break;
                            case OpCount.One:
                                dict = OneOpInfos;
                                num = (byte)(a.Number - 128);
                                break;
                            case OpCount.Two:
                                dict = TwoOpInfos;
                                num = a.Number;
                                break;
                            case OpCount.Var:
                                dict = VarOpInfos;
                                num = (byte)(a.Number - 224);
                                break;
                            case OpCount.Ext:
                                dict = ExtOpInfos;
                                num = a.Number;
                                break;
                            default:
                                throw new Exception("BUG:BADOPCOUNT");
                        }

                        if (dict.TryGetValue(num, out var array) == false)
                        {
                            array = new[] { info };
                            dict.Add(num, array);
                        }
                        else
                        {
                            var newArray = new OpcodeInfo[array.Length + 1];
                            Array.Copy(array, newArray, array.Length);
                            newArray[^1] = info;
                            dict[num] = newArray;
                        }
                    }
                }
            }
        }

        public static bool FindOpcodeInfo(OpCount count, byte opnum, byte zversion, out OpcodeInfo result)
        {
            var dict = count switch
            {
                OpCount.Zero => ZeroOpInfos,
                OpCount.One => OneOpInfos,
                OpCount.Two => TwoOpInfos,
                OpCount.Var => VarOpInfos,
                OpCount.Ext => ExtOpInfos,
                _ => throw new ArgumentOutOfRangeException(nameof(count)),
            };
            if (dict.TryGetValue(opnum, out var array))
            {
                foreach (var info in array)
                    if (zversion >= info.Attr.MinVersion && zversion <= info.Attr.MaxVersion)
                    {
                        result = info;
                        return true;
                    }
            }

            result = default;
            return false;
        }

        [NotNull]
        internal static string GetOpcodeName([CanBeNull] OpcodeAttribute attribute, OpcodeCompiler handler)
        {
            if (attribute?.Alias != null)
                return attribute.Alias;

            if (handler == null)
                return "<unknown>";

            var mi = handler.Method;
            var name = mi.Name;
            return name.StartsWith("op_") ? name.Remove(0, 3) : name;
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
                        var getWordMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetWord));
                        var address = zm.GlobalsOffset + 2 * (value - 16);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldc_I4, address);
                        il.Emit(OpCodes.Call, getWordMI);
                    }
                    break;
            }
        }

        private void StoreResult([NotNull] ILGenerator il)
        {
            if (resultStorage == -1)
                throw new InvalidOperationException("Storing from a non-store instruction");

            StoreResult(il, (byte) resultStorage);
        }

        private void StoreResult([NotNull] ILGenerator il, byte dest)
        {
            if (dest == 0)
            {
                PushOntoStack(il);
            }
            else if (dest < 16)
            {
                il.Emit(OpCodes.Stloc, zm.TempWordLocal);
                LoadLocals(il);
                il.Emit(OpCodes.Ldc_I4_S, (byte) (dest - 1));
                il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
                il.Emit(OpCodes.Stelem_I2);
            }
            else
            {
                var setWordMI = ZMachine.GetMethodInfo(nameof(ZMachine.SetWord));
                var address = zm.GlobalsOffset + 2 * (dest - 16);
                il.Emit(OpCodes.Stloc, zm.TempWordLocal);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, address);
                il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
                il.Emit(OpCodes.Call, setWordMI);
            }
        }

        private void PopFromStack([NotNull] ILGenerator il)
        {
            var popMI = typeof(Stack<short>).GetMethod(nameof(Stack<short>.Pop));
            System.Diagnostics.Debug.Assert(popMI != null);

            il.Emit(OpCodes.Ldloc, zm.StackLocal);
            il.Emit(OpCodes.Call, popMI);
        }

        private void PushOntoStack([NotNull] ILGenerator il)
        {
            var pushMI = typeof(Stack<short>).GetMethod(nameof(Stack<short>.Push));
            System.Diagnostics.Debug.Assert(pushMI != null);

            il.Emit(OpCodes.Stloc, zm.TempWordLocal);
            il.Emit(OpCodes.Ldloc, zm.StackLocal);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.Call, pushMI);
        }

        private void LoadLocals([NotNull] ILGenerator il)
        {
            il.Emit(OpCodes.Ldloc, zm.LocalsLocal);
        }

        /// <summary>
        /// Generates code to enter a function.
        /// </summary>
        /// <param name="il">The IL generator.</param>
        /// <param name="store">If true, a storage location will be read from the current PC (and PC
        /// will be advanced); otherwise, the function result will be discarded.</param>
        private void EnterFunction([NotNull] ILGenerator il, bool store)
        {
            var dest = store ? resultStorage : -1;
            EnterFunction(il, dest);
        }

        private void EnterFunction([NotNull] ILGenerator il, int dest)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.EnterFunctionImpl));

            // if the first operand is sp, save it in the temp local, since we don't use it until later
            var addressInTemp = false;
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

                for (var i = 1; i < argc; i++)
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

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            compiling = false;
        }

        private void LeaveFunction([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.LeaveFunctionImpl));
            il.Emit(OpCodes.Stloc, zm.TempWordLocal);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            compiling = false;
        }

        private void LeaveFunctionConst([NotNull] ILGenerator il, short result)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.LeaveFunctionImpl));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, (int)result);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            compiling = false;
        }

        // conditional version
        private void Branch([NotNull] ILGenerator il, OpCode ifTrue, OpCode ifFalse)
        {
            if (branchOffset == int.MinValue)
                throw new InvalidOperationException("Branching from non-branch opcode");

            if (Target != null)
            {
                il.Emit(branchIfTrue ? ifTrue : ifFalse, Target.Label);
                return;
            }

            // do it the hard way
            var skipBranch = il.DefineLabel();
            il.Emit(branchIfTrue ? ifFalse : ifTrue, skipBranch);

            switch (branchOffset)
            {
                case 0:
                    LeaveFunctionConst(il, 0);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ret);
                    compiling = true;
                    break;
                case 1:
                    LeaveFunctionConst(il, 1);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ret);
                    compiling = true;
                    break;
                default:
                    var pcFI = ZMachine.GetFieldInfo(nameof(ZMachine.pc));
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, zm.PC + branchOffset - 2);
                    il.Emit(OpCodes.Stfld, pcFI);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ret);
                    compiling = false;
                    break;
            }

            il.MarkLabel(skipBranch);
        }

        // unconditional version
        private void Branch([NotNull] ILGenerator il)
        {
            if (branchOffset == int.MinValue)
                throw new InvalidOperationException("Branching from non-branch opcode");

            if (Target != null)
            {
                il.Emit(OpCodes.Br, Target.Label);
                return;
            }

            // do it the hard way
            switch (branchOffset)
            {
                case 0:
                case 1:
                    LeaveFunctionConst(il, (short) branchOffset);
                    break;
                default:
                    var pcFI = ZMachine.GetFieldInfo(nameof(ZMachine.pc));
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, zm.PC + branchOffset - 2);
                    il.Emit(OpCodes.Stfld, pcFI);
                    break;
            }

            compiling = false;
        }

        private void BinaryOperation([NotNull] ILGenerator il, OpCode op)
        {
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(op);
            StoreResult(il);
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [MeansImplicitUse]
    internal class OpcodeAttribute : Attribute
    {
        public OpcodeAttribute(OpCount count, byte opnum)
        {
            OpCount = count;
            Number = opnum;
        }

        public OpCount OpCount { get; }
        public byte Number { get; }

        public bool Store { get; set; }
        public bool Branch { get; set; }
        public bool Text { get; set; }
        public bool Terminates { get; set; }
        public bool IndirectVar { get; set; }
        public bool Async { get; set; }
        public byte MinVersion { get; set; } = 1;
        public byte MaxVersion { get; set; } = 8;
        public string? Alias { get; set; }
    }
}
