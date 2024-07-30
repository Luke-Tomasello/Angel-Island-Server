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

using System.Reflection;
using System.Reflection.Emit;

namespace ZLR.VM
{
    partial class Opcode
    {
        [Opcode(OpCount.Ext, 0, true)]
        private void op_save(ILGenerator il)
        {
            if (argc == 0)
            {
                MethodInfo impl = typeof(ZMachine).GetMethod("SaveQuetzal", BindingFlags.NonPublic | BindingFlags.Instance);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, resultStorage);
                il.Emit(OpCodes.Ldc_I4, this.PC + this.ZCodeLength);
                il.Emit(OpCodes.Call, impl);
            }
            else
            {
                MethodInfo impl = typeof(ZMachine).GetMethod("SaveAuxiliary", BindingFlags.NonPublic | BindingFlags.Instance);

                il.Emit(OpCodes.Ldarg_0);
                LoadOperand(il, 0);
                LoadOperand(il, 1);
                LoadOperand(il, 2);
                il.Emit(OpCodes.Call, impl);
                StoreResult(il);
            }
        }

        [Opcode(OpCount.Ext, 1, true, Terminates = true)]
        private void op_restore(ILGenerator il)
        {
            if (argc == 0)
            {
                MethodInfo impl = typeof(ZMachine).GetMethod("RestoreQuetzal", BindingFlags.NonPublic | BindingFlags.Instance);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, resultStorage);
                il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);
                il.Emit(OpCodes.Call, impl);

                compiling = false;
            }
            else
            {
                MethodInfo impl = typeof(ZMachine).GetMethod("RestoreAuxiliary", BindingFlags.NonPublic | BindingFlags.Instance);

                il.Emit(OpCodes.Ldarg_0);
                LoadOperand(il, 0);
                LoadOperand(il, 1);
                LoadOperand(il, 2);
                il.Emit(OpCodes.Call, impl);
                StoreResult(il);
            }
        }

        [Opcode(OpCount.Ext, 2, true)]
        private void op_log_shift(ILGenerator il)
        {
            if (operandTypes[1] == OperandType.Variable)
            {
                MethodInfo impl = typeof(ZMachine).GetMethod("LogShiftImpl", BindingFlags.NonPublic | BindingFlags.Static);

                LoadOperand(il, 0);
                LoadOperand(il, 1);
                il.Emit(OpCodes.Call, impl);
            }
            else if (operandValues[1] < 0)
            {
                // shift right
                int value = -operandValues[1];
                LoadOperand(il, 0);
                il.Emit(OpCodes.Conv_U2);
                il.Emit(OpCodes.Ldc_I4, value);
                il.Emit(OpCodes.Shr_Un);
            }
            else
            {
                // shift left
                LoadOperand(il, 0);
                il.Emit(OpCodes.Ldc_I4, (int)operandValues[1]);
                il.Emit(OpCodes.Shl);
            }
            StoreResult(il);
        }

        [Opcode(OpCount.Ext, 3, true)]
        private void op_art_shift(ILGenerator il)
        {
            if (operandTypes[1] == OperandType.Variable)
            {
                MethodInfo impl = typeof(ZMachine).GetMethod("ArtShiftImpl", BindingFlags.NonPublic | BindingFlags.Static);

                LoadOperand(il, 0);
                LoadOperand(il, 1);
                il.Emit(OpCodes.Call, impl);
            }
            else if (operandValues[1] < 0)
            {
                // shift right
                int value = -operandValues[1];
                LoadOperand(il, 0);
                il.Emit(OpCodes.Conv_I2);
                il.Emit(OpCodes.Ldc_I4, value);
                il.Emit(OpCodes.Shr);
            }
            else
            {
                // shift left
                LoadOperand(il, 0);
                il.Emit(OpCodes.Ldc_I4, (int)operandValues[1]);
                il.Emit(OpCodes.Shl);
            }
            StoreResult(il);
        }

        [Opcode(OpCount.Ext, 4, true)]
        private void op_set_font(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo setFontMI = typeof(IZMachineIO).GetMethod("SetFont");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, setFontMI);
            StoreResult(il);
        }

        // EXT:5 to EXT:8 are only in V6

        [Opcode(OpCount.Ext, 9, true)]
        private void op_save_undo(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("SaveUndo", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, resultStorage);
            il.Emit(OpCodes.Ldc_I4, this.PC + this.ZCodeLength);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Ext, 10, true, Terminates = true)]
        private void op_restore_undo(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("RestoreUndo", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, resultStorage);
            il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);
            il.Emit(OpCodes.Call, impl);

            compiling = false;
        }

        [Opcode(OpCount.Ext, 11)]
        private void op_print_unicode(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("PrintUnicode", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Ext, 12, true)]
        private void op_check_unicode(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo checkUnicodeMI = typeof(IZMachineIO).GetMethod("CheckUnicode");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, checkUnicodeMI);
            StoreResult(il);
        }
    }
}