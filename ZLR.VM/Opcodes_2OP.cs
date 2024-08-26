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
using System.Reflection.Emit;
using JetBrains.Annotations;

namespace ZLR.VM
{
    partial class Opcode
    {
#pragma warning disable 0169
        [Opcode(OpCount.Two, 1, Branch = true)]
        private void op_je([NotNull] ILGenerator il)
        {
            switch (argc)
            {
                case 1:
                    throw new Exception("je with only one operand is illegal");
                case 2:
                    // simple version
                    LoadOperand(il, 0);
                    LoadOperand(il, 1);
                    Branch(il, OpCodes.Beq, OpCodes.Bne_Un);
                    break;
                default:
                    // complicated version

                    /* je can compare against up to 3 values, but we have to make sure the stack
                     * ends up the same no matter which one matches. after each comparison, we
                     * branch to a place that depends on the number of values remaining on the
                     * stack, so we can pop off the operands that aren't tested. */

                    var stackValues = 0;
                    for (var i = argc - 1; i > 0; i--)
                        if (operandTypes[i] == OperandType.Variable && operandValues[i] == 0)
                            stackValues++;

                    var decide = il.DefineLabel();
                    var matched = new Label[3]; // we never leave all 3 values on the stack
                    matched[0] = il.DefineLabel();
                    matched[1] = il.DefineLabel();
                    matched[2] = il.DefineLabel();

                    LoadOperand(il, 0);
                    il.Emit(OpCodes.Stloc, zm.TempWordLocal);

                    var remainingStackValues = stackValues;

                    for (var i = 1; i < argc; i++)
                    {
                        il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
                        LoadOperand(il, i);
                        if (operandTypes[i] == OperandType.Variable && operandValues[i] == 0)
                            remainingStackValues--;
                        il.Emit(OpCodes.Beq, matched[remainingStackValues]);
                    }

                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Br, decide);

                    for (var i = Math.Min(stackValues, 2); i > 0; i--)
                    {
                        il.MarkLabel(matched[i]);
                        PopFromStack(il);
                        il.Emit(OpCodes.Pop);
                    }
                    il.MarkLabel(matched[0]);
                    il.Emit(OpCodes.Ldc_I4_1);

                    il.MarkLabel(decide);
                    Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
                    break;
            }
        }

        [Opcode(OpCount.Two, 2, Branch = true)]
        private void op_jl([NotNull] ILGenerator il)
        {
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            Branch(il, OpCodes.Blt, OpCodes.Bge);
        }

        [Opcode(OpCount.Two, 3, Branch = true)]
        private void op_jg([NotNull] ILGenerator il)
        {
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            Branch(il, OpCodes.Bgt, OpCodes.Ble);
        }

        [Opcode(OpCount.Two, 4, Branch = true, IndirectVar = true)]
        private void op_dec_chk([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.IncImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Call, impl);

            LoadOperand(il, 1);
            Branch(il, OpCodes.Blt, OpCodes.Bge);
        }

        [Opcode(OpCount.Two, 5, Branch = true, IndirectVar = true)]
        private void op_inc_chk([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.IncImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, impl);

            LoadOperand(il, 1);
            Branch(il, OpCodes.Bgt, OpCodes.Ble);
        }

        [Opcode(OpCount.Two, 6, Branch = true)]
        private void op_jin([NotNull] ILGenerator il)
        {
            var getParentMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetObjectParent));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getParentMI);
            LoadOperand(il, 1);
            Branch(il, OpCodes.Beq, OpCodes.Bne_Un);
        }

        [Opcode(OpCount.Two, 7, Branch = true)]
        private void op_test([NotNull] ILGenerator il)
        {
            LoadOperand(il, 0);
            il.Emit(OpCodes.Stloc, zm.TempWordLocal);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.And);
            Branch(il, OpCodes.Beq, OpCodes.Bne_Un);
        }

        [Opcode(OpCount.Two, 8, Store = true)]
        private void op_or([NotNull] ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Or);
        }

        [Opcode(OpCount.Two, 9, Store = true)]
        private void op_and([NotNull] ILGenerator il)
        {
            BinaryOperation(il, OpCodes.And);
        }

        [Opcode(OpCount.Two, 10, Branch = true)]
        private void op_test_attr([NotNull] ILGenerator il)
        {
            var getAttrMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetObjectAttr));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, getAttrMI);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }

        [Opcode(OpCount.Two, 11)]
        private void op_set_attr([NotNull] ILGenerator il)
        {
            var setAttrMI = ZMachine.GetMethodInfo(nameof(ZMachine.SetObjectAttr));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, setAttrMI);
        }

        [Opcode(OpCount.Two, 12)]
        private void op_clear_attr([NotNull] ILGenerator il)
        {
            var setAttrMI = ZMachine.GetMethodInfo(nameof(ZMachine.SetObjectAttr));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, setAttrMI);
        }

        [Opcode(OpCount.Two, 13, IndirectVar = true)]
        private void op_store([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.StoreVariableImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Two, 14)]
        private void op_insert_obj([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.InsertObject));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Two, 15, Store = true)]
        private void op_loadw([NotNull] ILGenerator il)
        {
            var getWordMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetWord));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Conv_U2);
            il.Emit(OpCodes.Call, getWordMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Two, 16, Store = true)]
        private void op_loadb([NotNull] ILGenerator il)
        {
            var getByteMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetByte));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Conv_U2);
            il.Emit(OpCodes.Call, getByteMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Two, 17, Store = true)]
        private void op_get_prop([NotNull] ILGenerator il)
        {
            var getPropMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetPropValue));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, getPropMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Two, 18, Store = true)]
        private void op_get_prop_addr([NotNull] ILGenerator il)
        {
            var getPropAddrMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetPropAddr));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, getPropAddrMI);
            il.Emit(OpCodes.Conv_I2);
            StoreResult(il);
        }

        [Opcode(OpCount.Two, 19, Store = true)]
        private void op_get_next_prop([NotNull] ILGenerator il)
        {
            var getNextPropMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetNextProp));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, getNextPropMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Two, 20, Store = true)]
        private void op_add([NotNull] ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Add);
        }

        [Opcode(OpCount.Two, 21, Store = true)]
        private void op_sub([NotNull] ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Sub);
        }

        [Opcode(OpCount.Two, 22, Store = true)]
        private void op_mul([NotNull] ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Mul);
        }

        [Opcode(OpCount.Two, 23, Store = true)]
        private void op_div([NotNull] ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Div);
        }

        [Opcode(OpCount.Two, 24, Store = true)]
        private void op_mod([NotNull] ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Rem);
        }

        [Opcode(OpCount.Two, 25, Store = true, Terminates = true, MinVersion = 4)]
        private void op_call_2s([NotNull] ILGenerator il)
        {
            EnterFunction(il, true);
        }

        [Opcode(OpCount.Two, 26, Terminates = true, MinVersion = 5)]
        private void op_call_2n([NotNull] ILGenerator il)
        {
            EnterFunction(il, false);
        }

        [Opcode(OpCount.Two, 27, MinVersion = 5)]
        private void op_set_colour([NotNull] ILGenerator il)
        {
            var ioFI = ZMachine.GetFieldInfo(nameof(ZMachine.io));
            var impl = typeof(IZMachineIO).GetMethod(nameof(IZMachineIO.SetColors));
            System.Diagnostics.Debug.Assert(impl != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Callvirt, impl);
        }

        [Opcode(OpCount.Two, 28, Terminates = true, MinVersion = 5)]
        private void op_throw([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.ThrowImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, impl);
        }
#pragma warning restore 0169
    }
}