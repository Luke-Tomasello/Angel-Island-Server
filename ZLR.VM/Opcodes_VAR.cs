using System;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace ZLR.VM
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members",
        Justification = "Private methods with OpcodeAttribute are called via Reflection.")]
    partial class Opcode
    {
#pragma warning disable 0169
        [Opcode(OpCount.Var, 224, Store = true, Terminates = true, MaxVersion = 3, Alias = "call")]
        [Opcode(OpCount.Var, 224, Store = true, Terminates = true, MinVersion = 4)]
        private void op_call_vs([NotNull] ILGenerator il)
        {
            EnterFunction(il, true);
        }

        [Opcode(OpCount.Var, 225)]
        private void op_storew([NotNull] ILGenerator il)
        {
            var setWordCheckedMI = ZMachine.GetMethodInfo(nameof(ZMachine.SetWordChecked));
            var setWordMI = ZMachine.GetMethodInfo(nameof(ZMachine.SetWord));
            var trapMemoryMI = ZMachine.GetMethodInfo(nameof(ZMachine.TrapMemory));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, zm.TempWordLocal);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.Conv_U2);
            LoadOperand(il, 2);

            var impl = setWordCheckedMI;
            if (operandTypes[0] != OperandType.Variable && operandTypes[1] != OperandType.Variable)
            {
                var address = (ushort) operandValues[0] + 2 * operandValues[1];
                if (address > 64 && address + 1 < zm.RomStart)
                    impl = setWordMI;
            }
            il.Emit(OpCodes.Call, impl);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Call, trapMemoryMI);
        }

        [Opcode(OpCount.Var, 226)]
        private void op_storeb([NotNull] ILGenerator il)
        {
            var setByteCheckedMI = ZMachine.GetMethodInfo(nameof(ZMachine.SetByteChecked));
            var setByteMI = ZMachine.GetMethodInfo(nameof(ZMachine.SetByte));
            var trapMemoryMI = ZMachine.GetMethodInfo(nameof(ZMachine.TrapMemory));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, zm.TempWordLocal);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.Conv_U2);
            LoadOperand(il, 2);

            var impl = setByteCheckedMI;
            if (operandTypes[0] != OperandType.Variable && operandTypes[1] != OperandType.Variable)
            {
                var address = (ushort) operandValues[0] + operandValues[1];
                if (address > 64 && address < zm.RomStart)
                    impl = setByteMI;
            }
            il.Emit(OpCodes.Call, impl);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, trapMemoryMI);
        }

        [Opcode(OpCount.Var, 227)]
        private void op_put_prop([NotNull] ILGenerator il)
        {
            var setPropMI = ZMachine.GetMethodInfo(nameof(ZMachine.SetPropValue));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            il.Emit(OpCodes.Call, setPropMI);
        }

        [Opcode(OpCount.Var, 228, MaxVersion = 4, Async = true)]
        private void op_sread([NotNull] ILGenerator il)
        {
            CompileReadOp(il);
        }

        [Opcode(OpCount.Var, 228, Store = true, MinVersion = 5, Async = true)]
        private void op_aread([NotNull] ILGenerator il)
        {
            CompileReadOp(il);
        }

        private void CompileReadOp([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.ReadImplAsync));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Ldc_I4, PC);
            il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);
            il.Emit(OpCodes.Ldc_I4, resultStorage);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Ret);

            compiling = false;
        }

        [Opcode(OpCount.Var, 229)]
        private void op_print_char([NotNull] ILGenerator il)
        {
            var printZsciiMI = ZMachine.GetMethodInfo(nameof(ZMachine.PrintZSCII));
            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, printZsciiMI);
        }

        [Opcode(OpCount.Var, 230)]
        private void op_print_num([NotNull] ILGenerator il)
        {
            var toStringMI = typeof(Convert).GetMethod(nameof(Convert.ToString), new[] {typeof(short)});
            System.Diagnostics.Debug.Assert(toStringMI != null);
            var printStringMI = ZMachine.GetMethodInfo(nameof(ZMachine.PrintString));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, toStringMI);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.Var, 231, Store = true)]
        private void op_random([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.RandomImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
            StoreResult(il);
        }

        [Opcode(OpCount.Var, 232)]
        private void op_push([NotNull] ILGenerator il)
        {
            LoadOperand(il, 0);
            PushOntoStack(il);
        }

        [Opcode(OpCount.Var, 233, IndirectVar = true, MaxVersion = 5)]
        [Opcode(OpCount.Var, 233, Store = true, MinVersion = 6, MaxVersion = 6)]
        private void op_pull([NotNull] ILGenerator il)
        {
            if (zm.ZVersion == 6)
            {
                if (argc == 0)
                {
                    PopFromStack(il);
                }
                else
                {
                    var impl = ZMachine.GetMethodInfo(nameof(ZMachine.PullFromUserStack));

                    System.Diagnostics.Debug.Assert(argc == 1);

                    il.Emit(OpCodes.Ldarg_0);
                    LoadOperand(il, 0);
                    il.Emit(OpCodes.Call, impl);
                }

                StoreResult(il);
            }
            else
            {
                var impl = ZMachine.GetMethodInfo(nameof(ZMachine.StoreVariableImpl));

                System.Diagnostics.Debug.Assert(argc == 1);

                il.Emit(OpCodes.Ldarg_0);
                LoadOperand(il, 0);
                PopFromStack(il);
                il.Emit(OpCodes.Call, impl);
            }
        }

        [Opcode(OpCount.Var, 234, MinVersion = 3)]
        private void op_split_window([NotNull] ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var splitWindowMI = typeof(IZMachineIO).GetMethod(nameof(IZMachineIO.SplitWindow));
            System.Diagnostics.Debug.Assert(splitWindowMI != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Callvirt, splitWindowMI);

            if (zm.ZVersion == 3)
            {
                // clear the upper window after splitting
                var eraseWindowMI = typeof(IZMachineIO).GetMethod(nameof(IZMachineIO.EraseWindow));
                System.Diagnostics.Debug.Assert(eraseWindowMI != null);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, ioFI);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Callvirt, eraseWindowMI);
            }
        }

        [Opcode(OpCount.Var, 235, MinVersion = 3)]
        private void op_set_window([NotNull] ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var impl = typeof(IZMachineIO).GetMethod(nameof(IZMachineIO.SelectWindow));
            System.Diagnostics.Debug.Assert(impl != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Callvirt, impl);
        }

        [Opcode(OpCount.Var, 236, Store = true, Terminates = true, MinVersion = 4)]
        private void op_call_vs2([NotNull] ILGenerator il)
        {
            EnterFunction(il, true);
        }

        [Opcode(OpCount.Var, 237, MinVersion = 4)]
        private void op_erase_window([NotNull] ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var eraseWindowMI = typeof(IZMachineIO).GetMethod(nameof(IZMachineIO.EraseWindow));
            System.Diagnostics.Debug.Assert(eraseWindowMI != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Callvirt, eraseWindowMI);
        }

        [Opcode(OpCount.Var, 238, MinVersion = 4)]
        private void op_erase_line(ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var eraseLineMI = typeof(IZMachineIO).GetMethod(nameof(IZMachineIO.EraseLine));
            System.Diagnostics.Debug.Assert(eraseLineMI != null);

            Label? skip = null;
            if (argc >= 1)
            {
                if (operandTypes[0] == OperandType.Variable)
                {
                    skip = il.DefineLabel();
                    LoadOperand(il, 0);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Bne_Un, skip.Value);
                }
                else if (operandValues[0] != 1)
                    return;
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            il.Emit(OpCodes.Callvirt, eraseLineMI);

            if (skip != null)
                il.MarkLabel(skip.Value);
        }

        [Opcode(OpCount.Var, 239, MinVersion = 4)]
        private void op_set_cursor([NotNull] ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var moveCursorMI = typeof(IZMachineIO).GetMethod(nameof(IZMachineIO.MoveCursor));
            System.Diagnostics.Debug.Assert(moveCursorMI != null);

            LoadOperand(il, 0);
            il.Emit(OpCodes.Stloc, zm.TempWordLocal);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 1); // x
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal); // y
            il.Emit(OpCodes.Callvirt, moveCursorMI);
        }

        [Opcode(OpCount.Var, 240, MinVersion = 4)]
        private void op_get_cursor([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.GetCursorPos));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 241, MinVersion = 4)]
        private void op_set_text_style([NotNull] ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var impl = typeof(IZMachineIO).GetMethod(nameof(IZMachineIO.SetTextStyle));
            System.Diagnostics.Debug.Assert(impl != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Callvirt, impl);
        }

        [Opcode(OpCount.Var, 242, MinVersion = 4)]
        private void op_buffer_mode([NotNull] ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var impl = typeof(IZMachineIO).GetProperty(nameof(IZMachineIO.Buffering))?.GetSetMethod();
            System.Diagnostics.Debug.Assert(impl != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Callvirt, impl);
        }

        [Opcode(OpCount.Var, 243, MinVersion = 3, Async = true)]
        private void op_output_stream([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.SetOutputStreamAsync));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Ret);

            compiling = false;
        }

        [Opcode(OpCount.Var, 244, MinVersion = 3, Async = true)]
        private void op_input_stream([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.SetInputStreamAsync));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Ret);

            compiling = false;
        }

        [Opcode(OpCount.Var, 245, MinVersion = 3)]
        private void op_sound_effect([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.SoundEffectImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 246, Store = true, MinVersion = 4, Async = true)]
        private void op_read_char([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.ReadCharImplAsync));

            if (operandTypes.Length > 0)
            {
                // the operand value is ignored (standard says it must be 1)
                if (operandTypes[0] == OperandType.Variable && operandValues[0] == 0)
                {
                    PopFromStack(il);
                    il.Emit(OpCodes.Pop);
                }
            }

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            il.Emit(OpCodes.Ldc_I4, PC);
            il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);
            il.Emit(OpCodes.Ldc_I4, resultStorage);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Ret);
            
            compiling = false;
        }

        [Opcode(OpCount.Var, 247, Store = true, Branch = true, MinVersion = 4)]
        private void op_scan_table([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.ScanTableImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Conv_U2);
            StoreResult(il);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }

        [Opcode(OpCount.One, 143, Store = true, MaxVersion = 4)]
        [Opcode(OpCount.Var, 248, Store = true, MinVersion = 5)]
        private void op_not([NotNull] ILGenerator il)
        {
            LoadOperand(il, 0);
            il.Emit(OpCodes.Not);
            StoreResult(il);
        }

        [Opcode(OpCount.Var, 249, Terminates = true, MinVersion = 5)]
        private void op_call_vn([NotNull] ILGenerator il)
        {
            EnterFunction(il, false);
        }

        [Opcode(OpCount.Var, 250, Terminates = true, MinVersion = 5)]
        private void op_call_vn2([NotNull] ILGenerator il)
        {
            EnterFunction(il, false);
        }

        [Opcode(OpCount.Var, 251, MinVersion = 5)]
        private void op_tokenise([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.Tokenize));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 252, MinVersion = 5)]
        private void op_encode_text([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.EncodeTextImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 253, MinVersion = 5)]
        private void op_copy_table([NotNull] ILGenerator il)
        {
            if (operandTypes[1] != OperandType.Variable && operandValues[1] == 0)
            {
                var impl = ZMachine.GetMethodInfo(nameof(ZMachine.ZeroMemory));

                il.Emit(OpCodes.Ldarg_0);
                LoadOperand(il, 0);
                LoadOperand(il, 2);
                il.Emit(OpCodes.Call, impl);
            }
            else
            {
                var impl = ZMachine.GetMethodInfo(nameof(ZMachine.CopyTableImpl));

                il.Emit(OpCodes.Ldarg_0);
                LoadOperand(il, 0);
                LoadOperand(il, 1);
                LoadOperand(il, 2);
                il.Emit(OpCodes.Call, impl);
            }
        }

        [Opcode(OpCount.Var, 254, MinVersion = 5)]
        private void op_print_table([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.PrintTableImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 255, Branch = true, MinVersion = 5)]
        private void op_check_arg_count([NotNull] ILGenerator il)
        {
            var getTopFrameMI = typeof(ZMachine)
                .GetProperty(nameof(ZMachine.TopFrame), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetGetMethod(true);
            var argCountFI = typeof(ZMachine.CallFrame).GetField(nameof(ZMachine.CallFrame.ArgCount));

            System.Diagnostics.Debug.Assert(getTopFrameMI != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, getTopFrameMI);
            il.Emit(OpCodes.Ldfld, argCountFI);

            LoadOperand(il, 0);
            Branch(il, OpCodes.Bge, OpCodes.Blt);
        }
#pragma warning restore 0169
    }
}
