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
        [Opcode(OpCount.Two, 1, false, true, false)]
        private void op_je(ILGenerator il)
        {
            if (argc == 1)
            {
                throw new Exception("je with only one operand is illegal");
            }
            else if (argc == 2)
            {
                // simple version
                LoadOperand(il, 0);
                LoadOperand(il, 1);
                Branch(il, OpCodes.Beq, OpCodes.Bne_Un);
            }
            else
            {
                // complicated version

                /* je can compare against up to 3 values, but we have to make sure the stack
				 * ends up the same no matter which one matches. after each comparison, we
				 * branch to a place that depends on the number of values remaining on the
				 * stack, so we can pop off the operands that aren't tested. */

                int stackValues = 0;
                for (int i = argc - 1; i > 0; i--)
                    if (operandTypes[i] == OperandType.Variable && operandValues[i] == 0)
                        stackValues++;

                Label decide = il.DefineLabel();
                Label[] matched = new Label[3]; // we never leave all 3 values on the stack
                matched[0] = il.DefineLabel();
                matched[1] = il.DefineLabel();
                matched[2] = il.DefineLabel();

                LoadOperand(il, 0);
                il.Emit(OpCodes.Stloc, zm.TempWordLocal);

                int remainingStackValues = stackValues;

                for (int i = 1; i < argc; i++)
                {
                    il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
                    LoadOperand(il, i);
                    if (operandTypes[i] == OperandType.Variable && operandValues[i] == 0)
                        remainingStackValues--;
                    il.Emit(OpCodes.Beq, matched[remainingStackValues]);
                }

                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Br, decide);

                for (int i = Math.Min(stackValues, 2); i > 0; i--)
                {
                    il.MarkLabel(matched[i]);
                    PopFromStack(il);
                    il.Emit(OpCodes.Pop);
                }
                il.MarkLabel(matched[0]);
                il.Emit(OpCodes.Ldc_I4_1);

                il.MarkLabel(decide);
                Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
            }
        }

        [Opcode(OpCount.Two, 2, false, true, false)]
        private void op_jl(ILGenerator il)
        {
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            Branch(il, OpCodes.Blt, OpCodes.Bge);
        }

        [Opcode(OpCount.Two, 3, false, true, false)]
        private void op_jg(ILGenerator il)
        {
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            Branch(il, OpCodes.Bgt, OpCodes.Ble);
        }

        [Opcode(OpCount.Two, 4, false, true, false)]
        private void op_dec_chk(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("IncImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Call, impl);

            LoadOperand(il, 1);
            Branch(il, OpCodes.Blt, OpCodes.Bge);
        }

        [Opcode(OpCount.Two, 5, false, true, false)]
        private void op_inc_chk(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("IncImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, impl);

            LoadOperand(il, 1);
            Branch(il, OpCodes.Bgt, OpCodes.Ble);
        }

        [Opcode(OpCount.Two, 6, false, true, false)]
        private void op_jin(ILGenerator il)
        {
            MethodInfo getParentMI = typeof(ZMachine).GetMethod("GetObjectParent", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getParentMI);
            LoadOperand(il, 1);
            Branch(il, OpCodes.Beq, OpCodes.Bne_Un);
        }

        [Opcode(OpCount.Two, 7, false, true, false)]
        private void op_test(ILGenerator il)
        {
            LoadOperand(il, 0);
            il.Emit(OpCodes.Stloc, zm.TempWordLocal);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldloc, zm.TempWordLocal);
            il.Emit(OpCodes.And);
            Branch(il, OpCodes.Beq, OpCodes.Bne_Un);
        }

        [Opcode(OpCount.Two, 8, true)]
        private void op_or(ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Or);
        }

        [Opcode(OpCount.Two, 9, true)]
        private void op_and(ILGenerator il)
        {
            BinaryOperation(il, OpCodes.And);
        }

        [Opcode(OpCount.Two, 10, false, true, false)]
        private void op_test_attr(ILGenerator il)
        {
            MethodInfo getAttrMI = typeof(ZMachine).GetMethod("GetObjectAttr", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, getAttrMI);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }

        [Opcode(OpCount.Two, 11)]
        private void op_set_attr(ILGenerator il)
        {
            MethodInfo setAttrMI = typeof(ZMachine).GetMethod("SetObjectAttr", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, setAttrMI);
        }

        [Opcode(OpCount.Two, 12)]
        private void op_clear_attr(ILGenerator il)
        {
            MethodInfo setAttrMI = typeof(ZMachine).GetMethod("SetObjectAttr", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, setAttrMI);
        }

        [Opcode(OpCount.Two, 13)]
        private void op_store(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("StoreVariableImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Two, 14)]
        private void op_insert_obj(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("InsertObject", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Two, 15, true)]
        private void op_loadw(ILGenerator il)
        {
            MethodInfo getWordMI = typeof(ZMachine).GetMethod("GetWord", BindingFlags.NonPublic | BindingFlags.Instance);

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

        [Opcode(OpCount.Two, 16, true)]
        private void op_loadb(ILGenerator il)
        {
            MethodInfo getByteMI = typeof(ZMachine).GetMethod("GetByte", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Conv_U2);
            il.Emit(OpCodes.Call, getByteMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Two, 17, true)]
        private void op_get_prop(ILGenerator il)
        {
            MethodInfo getPropMI = typeof(ZMachine).GetMethod("GetPropValue", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, getPropMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Two, 18, true)]
        private void op_get_prop_addr(ILGenerator il)
        {
            MethodInfo getPropAddrMI = typeof(ZMachine).GetMethod("GetPropAddr", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, getPropAddrMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Two, 19, true)]
        private void op_get_next_prop(ILGenerator il)
        {
            MethodInfo getNextPropMI = typeof(ZMachine).GetMethod("GetNextProp", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, getNextPropMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Two, 20, true)]
        private void op_add(ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Add);
        }

        [Opcode(OpCount.Two, 21, true)]
        private void op_sub(ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Sub);
        }

        [Opcode(OpCount.Two, 22, true)]
        private void op_mul(ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Mul);
        }
        [Opcode(OpCount.Two, 23, true)]
        private void op_div(ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Div);
        }
        [Opcode(OpCount.Two, 24, true)]
        private void op_mod(ILGenerator il)
        {
            BinaryOperation(il, OpCodes.Rem);
        }

        [Opcode(OpCount.Two, 25, true, Terminates = true)]
        private void op_call_2s(ILGenerator il)
        {
            EnterFunction(il, true);
        }

        [Opcode(OpCount.Two, 26, Terminates = true)]
        private void op_call_2n(ILGenerator il)
        {
            EnterFunction(il, false);
        }

        [Opcode(OpCount.Two, 27)]
        private void op_set_colour(ILGenerator il)
        {
            FieldInfo ioFI = typeof(ZMachine).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo impl = typeof(IZMachineIO).GetMethod("SetColors");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, ioFI);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.Two, 28)]
        private void op_throw(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("ThrowImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            LoadOperand(il, 1);
            il.Emit(OpCodes.Call, impl);
        }
    }
}