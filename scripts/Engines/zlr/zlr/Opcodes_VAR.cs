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
using System.Reflection;
using System.Reflection.Emit;

namespace ZLR.VM
{
    partial class Opcode
    {
        [Opcode(OpCount.Var, 224, true, Terminates = true)]
        private void op_call_vs(ILGenerator il)
        {
            EnterFunction(il, true);
        }

        [Opcode(OpCount.Var, 225)]
        private void op_storew(ILGenerator il)
        {
            MethodInfo setWordCheckedMI = typeof(ZMachine).GetMethod("SetWordChecked", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo setWordMI = typeof(ZMachine).GetMethod("SetWord", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo trapMemoryMI = typeof(ZMachine).GetMethod("TrapMemory", BindingFlags.NonPublic | BindingFlags.Instance);

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

            MethodInfo impl = setWordCheckedMI;
            if (operandTypes[0] != OperandType.Variable && operandTypes[1] != OperandType.Variable)
            {
                int address = (ushort)operandValues[0] + 2 * operandValues[1];
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
        private void op_storeb(ILGenerator il)
        {
            MethodInfo setByteCheckedMI = typeof(ZMachine).GetMethod("SetByteChecked", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo setByteMI = typeof(ZMachine).GetMethod("SetByte", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo trapMemoryMI = typeof(ZMachine).GetMethod("TrapMemory", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, zm.TempWordLocal);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.Conv_U2);
            LoadOperand(il, 2);

            MethodInfo impl = setByteCheckedMI;
            if (operandTypes[0] != OperandType.Variable && operandTypes[1] != OperandType.Variable)
            {
                int address = (ushort)operandValues[0] + operandValues[1];
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
        private void op_put_prop(ILGenerator il)
        {
            MethodInfo setPropMI = typeof(ZMachine).GetMethod("SetPropValue", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            il.Emit(OpCodes.Call, setPropMI);
        }

        [Opcode(OpCount.Var, 228, true)]
        private void op_aread(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("ReadImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
            StoreResult(il);
        }

        [Opcode(OpCount.Var, 229)]
        private void op_print_char(ILGenerator il)
        {
            MethodInfo printZsciiMI = typeof(ZMachine).GetMethod("PrintZSCII", BindingFlags.NonPublic | BindingFlags.Instance);
            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, printZsciiMI);
        }

        [Opcode(OpCount.Var, 230)]
        private void op_print_num(ILGenerator il)
        {
            MethodInfo toStringMI = typeof(Convert).GetMethod("ToString",
                new Type[] { typeof(short) });
            MethodInfo printStringMI = typeof(ZMachine).GetMethod("PrintString",
                BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, toStringMI);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.Var, 231, true)]
        private void op_random(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("RandomImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
            StoreResult(il);
        }

        [Opcode(OpCount.Var, 232)]
        private void op_push(ILGenerator il)
        {
            LoadOperand(il, 0);
            PushOntoStack(il);
        }

        [Opcode(OpCount.Var, 233)]
        private void op_pull(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("StoreVariableImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            System.Diagnostics.Debug.Assert(argc == 1);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            PopFromStack(il);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 234)]
        private void op_split_window(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo impl = typeof(IZMachineIO).GetMethod("SplitWindow");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 235)]
        private void op_set_window(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo impl = typeof(IZMachineIO).GetMethod("SelectWindow");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 236, true, Terminates = true)]
        private void op_call_vs2(ILGenerator il)
        {
            EnterFunction(il, true);
        }

        [Opcode(OpCount.Var, 237)]
        private void op_erase_window(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo eraseWindowMI = typeof(IZMachineIO).GetMethod("EraseWindow");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, eraseWindowMI);
        }

        [Opcode(OpCount.Var, 238)]
        private void op_erase_line(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo eraseLineMI = typeof(IZMachineIO).GetMethod("EraseLine");

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
            il.Emit(OpCodes.Call, eraseLineMI);

            if (skip != null)
                il.MarkLabel(skip.Value);
        }

        [Opcode(OpCount.Var, 239)]
        private void op_set_cursor(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo moveCursorMI = typeof(IZMachineIO).GetMethod("MoveCursor");

            LoadOperand(il, 0);
            il.Emit(OpCodes.Stloc, zm.TempWordLocal);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 1); // x
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal); // y
            il.Emit(OpCodes.Call, moveCursorMI);
        }

        [Opcode(OpCount.Var, 240)]
        private void op_get_cursor(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("GetCursorPos", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 241)]
        private void op_set_text_style(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo impl = typeof(IZMachineIO).GetMethod("SetTextStyle");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 242)]
        private void op_buffer_mode(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo impl = typeof(IZMachineIO).GetMethod("set_Buffering");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 243)]
        private void op_output_stream(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("SetOutputStream", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 244)]
        private void op_input_stream(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("SetInputStream", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 245)]
        private void op_sound_effect(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("SoundEffectImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 246, true)]
        private void op_read_char(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("ReadCharImpl", BindingFlags.NonPublic | BindingFlags.Instance);

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
            il.Emit(OpCodes.Call, impl);
            StoreResult(il);
        }

        [Opcode(OpCount.Var, 247, true)]
        private void op_scan_table(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("ScanTableImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
            StoreResult(il);
        }

        [Opcode(OpCount.Var, 248, true)]
        private void op_not(ILGenerator il)
        {
            LoadOperand(il, 0);
            il.Emit(OpCodes.Not);
            StoreResult(il);
        }

        [Opcode(OpCount.Var, 249, Terminates = true)]
        private void op_call_vn(ILGenerator il)
        {
            EnterFunction(il, false);
        }

        [Opcode(OpCount.Var, 250, Terminates = true)]
        private void op_call_vn2(ILGenerator il)
        {
            EnterFunction(il, false);
        }

        [Opcode(OpCount.Var, 251)]
        private void op_tokenise(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("Tokenize", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 252)]
        private void op_encode_text(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("EncodeTextImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 253)]
        private void op_copy_table(ILGenerator il)
        {
            if (operandTypes[1] != OperandType.Variable && operandValues[1] == 0)
            {
                MethodInfo impl = typeof(ZMachine).GetMethod("ZeroMemory", BindingFlags.NonPublic | BindingFlags.Instance);

                il.Emit(OpCodes.Ldarg_0);
                LoadOperand(il, 0);
                LoadOperand(il, 2);
                il.Emit(OpCodes.Call, impl);
            }
            else
            {
                MethodInfo impl = typeof(ZMachine).GetMethod("CopyTableImpl", BindingFlags.NonPublic | BindingFlags.Instance);

                il.Emit(OpCodes.Ldarg_0);
                LoadOperand(il, 0);
                LoadOperand(il, 1);
                LoadOperand(il, 2);
                il.Emit(OpCodes.Call, impl);
            }
        }

        [Opcode(OpCount.Var, 254)]
        private void op_print_table(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("PrintTableImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            LoadOperand(il, 2);
            LoadOperand(il, 3);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Var, 255, false, true, false)]
        private void op_check_arg_count(ILGenerator il)
        {
            MethodInfo getTopFrameMI = typeof(ZMachine).GetMethod("get_TopFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo argCountMI = typeof(ZMachine.CallFrame).GetField("ArgCount");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, getTopFrameMI);
            il.Emit(OpCodes.Ldfld, argCountMI);

            LoadOperand(il, 0);
            Branch(il, OpCodes.Bge, OpCodes.Blt);
        }
    }
}