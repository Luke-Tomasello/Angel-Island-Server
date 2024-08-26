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

using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace ZLR.VM
{
    partial class Opcode
    {
#pragma warning disable 0169
        [Opcode(OpCount.Zero, 181, Branch = true, MaxVersion = 3, Async = true)]
        [Opcode(OpCount.Zero, 181, Store = true, MinVersion = 4, MaxVersion = 4, Async = true)]
        [Opcode(OpCount.Ext, 0, Store = true, MinVersion = 5, Async = true)]
        private void op_save([NotNull] ILGenerator il)
        {
            MethodInfo impl;

            switch (zm.ZVersion)
            {
                case 1:
                case 2:
                case 3:
                    // branching version
                    impl = ZMachine.GetMethodInfo(nameof(ZMachine.SaveQuetzalAndBranchAsync));

                    il.Emit(OpCodes.Ldarg_0);
                    // pass the address of this instruction's branch offset, which is always the 2nd instruction byte because this is 0OP
                    il.Emit(OpCodes.Ldc_I4, PC + 1);            // savedPC
                    il.Emit(branchIfTrue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);    // branchIfTrue
                    il.Emit(OpCodes.Ldc_I4, branchOffset);                          // branchOffset
                    il.Emit(OpCodes.Ldc_I4, PC);                // retryPC
                    il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);  // nextPC
                    il.Emit(OpCodes.Call, impl);
                    il.Emit(OpCodes.Ret);
                    compiling = false;
                    break;

                case var _ when argc == 0:
                    // storing version
                    // in V4, argc is always 0 since this is a 0OP instruction
                    impl = ZMachine.GetMethodInfo(nameof(ZMachine.SaveQuetzalAndStoreAsync));

                    il.Emit(OpCodes.Ldarg_0);
                    // pass the address of this instruction's result storage, which is always the last instruction byte
                    il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength - 1);  // savedPC
                    il.Emit(OpCodes.Ldc_I4, resultStorage);         // resultStorage
                    il.Emit(OpCodes.Ldc_I4, PC);                    // retryPC
                    il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);      // nextPC
                    il.Emit(OpCodes.Call, impl);
                    il.Emit(OpCodes.Ret);
                    compiling = false;
                    break;

                default:
                    impl = ZMachine.GetMethodInfo(nameof(ZMachine.SaveAuxiliary));

                    il.Emit(OpCodes.Ldarg_0);
                    LoadOperand(il, 0);
                    LoadOperand(il, 1);
                    LoadOperand(il, 2);
                    il.Emit(OpCodes.Call, impl);
                    StoreResult(il);
                    il.Emit(OpCodes.Ldnull);
                    break;
            }
        }

        [Opcode(OpCount.Zero, 182, Branch = true, Terminates = true, MaxVersion = 3)]
        [Opcode(OpCount.Zero, 182, Store = true, Terminates = true, MinVersion = 4, MaxVersion = 4)]
        [Opcode(OpCount.Ext, 1, Store = true, Terminates = true, MinVersion = 5)]
        private void op_restore([NotNull] ILGenerator il)
        {
            MethodInfo impl;

            switch (zm.ZVersion)
            {
                case 1:
                case 2:
                case 3:
                    // branching version
                    impl = ZMachine.GetMethodInfo(nameof(ZMachine.RestoreQuetzal));

                    var failurePC = PC + ZCodeLength;
                    if (!branchIfTrue)
                        failurePC += branchOffset - 2;

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, failurePC);
                    il.Emit(OpCodes.Call, impl);
                    il.Emit(OpCodes.Pop);
                    compiling = false;
                    break;

                default:
                    // storing version
                    // in V4, argc is always 0 since this is a 0OP instruction
                    if (argc == 0)
                    {
                        impl = ZMachine.GetMethodInfo(nameof(ZMachine.RestoreQuetzal));

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);
                        il.Emit(OpCodes.Call, impl);
                        StoreResult(il);

                        compiling = false;
                    }
                    else
                    {
                        impl = ZMachine.GetMethodInfo(nameof(ZMachine.RestoreAuxiliary));

                        il.Emit(OpCodes.Ldarg_0);
                        LoadOperand(il, 0);
                        LoadOperand(il, 1);
                        LoadOperand(il, 2);
                        il.Emit(OpCodes.Call, impl);
                        StoreResult(il);
                    }
                    break;
            }
        }

        [Opcode(OpCount.Ext, 2, Store = true, MinVersion = 5)]
        private void op_log_shift([NotNull] ILGenerator il)
        {
            if (operandTypes[1] == OperandType.Variable)
            {
                var impl = typeof(ZMachine).GetMethod(nameof(ZMachine.LogShiftImpl), BindingFlags.NonPublic | BindingFlags.Static);
                System.Diagnostics.Debug.Assert(impl != null);

                LoadOperand(il, 0);
                LoadOperand(il, 1);
                il.Emit(OpCodes.Call, impl);
            }
            else if (operandValues[1] < 0)
            {
                // shift right
                var value = -operandValues[1];
                LoadOperand(il, 0);
                il.Emit(OpCodes.Conv_U2);
                il.Emit(OpCodes.Ldc_I4, value);
                il.Emit(OpCodes.Shr_Un);
            }
            else
            {
                // shift left
                LoadOperand(il, 0);
                il.Emit(OpCodes.Ldc_I4, (int) operandValues[1]);
                il.Emit(OpCodes.Shl);
            }
            StoreResult(il);
        }

        [Opcode(OpCount.Ext, 3, Store = true)]
        private void op_art_shift([NotNull] ILGenerator il)
        {
            if (operandTypes[1] == OperandType.Variable)
            {
                var impl = typeof(ZMachine).GetMethod(nameof(ZMachine.ArtShiftImpl), BindingFlags.NonPublic | BindingFlags.Static);
                System.Diagnostics.Debug.Assert(impl != null);

                LoadOperand(il, 0);
                LoadOperand(il, 1);
                il.Emit(OpCodes.Call, impl);
            }
            else if (operandValues[1] < 0)
            {
                // shift right
                var value = -operandValues[1];
                LoadOperand(il, 0);
                il.Emit(OpCodes.Conv_I2);
                il.Emit(OpCodes.Ldc_I4, value);
                il.Emit(OpCodes.Shr);
            }
            else
            {
                // shift left
                LoadOperand(il, 0);
                il.Emit(OpCodes.Ldc_I4, (int) operandValues[1]);
                il.Emit(OpCodes.Shl);
            }
            StoreResult(il);
        }

        [Opcode(OpCount.Ext, 4, Store = true, MinVersion = 5)]
        private void op_set_font([NotNull] ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var setFontMI = typeof(IZMachineIO).GetMethod(nameof(IZMachineIO.SetFont));
            System.Diagnostics.Debug.Assert(setFontMI != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Callvirt, setFontMI);
            StoreResult(il);
        }

        // EXT:5 to EXT:8 are only in V6

        [Opcode(OpCount.Ext, 9, Store = true, MinVersion = 5)]
        private void op_save_undo([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.SaveUndo));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, resultStorage);
            il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Ext, 10, Store = true, Terminates = true, MinVersion = 5)]
        private void op_restore_undo([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.RestoreUndo));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, resultStorage);
            il.Emit(OpCodes.Ldc_I4, PC + ZCodeLength);
            il.Emit(OpCodes.Call, impl);

            compiling = false;
        }

        [Opcode(OpCount.Ext, 11, MinVersion = 5)]
        private void op_print_unicode([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.PrintUnicode));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Ext, 12, Store = true, MinVersion = 5)]
        private void op_check_unicode([NotNull] ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var checkUnicodeMI = ZMachine.GetIOMethodInfo(nameof(IAsyncZMachineIO.CheckUnicode));
            System.Diagnostics.Debug.Assert(checkUnicodeMI != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Callvirt, checkUnicodeMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Ext, 21, MinVersion = 6, MaxVersion = 6)]
        private void op_pop_stack([NotNull] ILGenerator il)
        {
            if (argc == 1)
            {
                var impl = ZMachine.GetMethodInfo(nameof(ZMachine.PopStack));

                il.Emit(OpCodes.Ldarg_0);
                LoadOperand(il, 0);
                il.Emit(OpCodes.Call, impl);
            }
            else
            {
                var impl = ZMachine.GetMethodInfo(nameof(ZMachine.PopUserStack));

                il.Emit(OpCodes.Ldarg_0);
                LoadOperand(il, 0);
                LoadOperand(il, 1);
                il.Emit(OpCodes.Call, impl);
            }
        }

        [Opcode(OpCount.Ext, 24, Branch = true, MinVersion = 6, MaxVersion = 6)]
        private void op_push_stack([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.PushOntoUserStack));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, impl);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }
#pragma warning restore 0169
    }
}