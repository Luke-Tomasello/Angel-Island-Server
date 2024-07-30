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
        [Opcode(OpCount.One, 128, false, true, false)]
        private void op_jz(ILGenerator il)
        {
            LoadOperand(il, 0);
            Branch(il, OpCodes.Brfalse, OpCodes.Brtrue);
        }

        [Opcode(OpCount.One, 129, true, true, false)]
        private void op_get_sibling(ILGenerator il)
        {
            MethodInfo getSiblingMI = typeof(ZMachine).GetMethod("GetObjectSibling", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getSiblingMI);

            il.Emit(OpCodes.Dup);
            StoreResult(il);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }

        [Opcode(OpCount.One, 130, true, true, false)]
        private void op_get_child(ILGenerator il)
        {
            MethodInfo getChildMI = typeof(ZMachine).GetMethod("GetObjectChild", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getChildMI);

            il.Emit(OpCodes.Dup);
            StoreResult(il);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }

        [Opcode(OpCount.One, 131, true)]
        private void op_get_parent(ILGenerator il)
        {
            MethodInfo getParentMI = typeof(ZMachine).GetMethod("GetObjectParent", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getParentMI);
            StoreResult(il);
        }

        [Opcode(OpCount.One, 132, true)]
        private void op_get_prop_len(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("GetPropLength", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
            StoreResult(il);
        }

        [Opcode(OpCount.One, 133)]
        private void op_inc(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("IncImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Pop);
        }

        [Opcode(OpCount.One, 134)]
        private void op_dec(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("IncImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Pop);
        }

        [Opcode(OpCount.One, 135)]
        private void op_print_addr(ILGenerator il)
        {
            MethodInfo decodeStringMI = typeof(ZMachine).GetMethod("DecodeString", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo printStringMI = typeof(ZMachine).GetMethod("PrintString", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Conv_U2);
            il.Emit(OpCodes.Call, decodeStringMI);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.One, 136, true, Terminates = true)]
        private void op_call_1s(ILGenerator il)
        {
            EnterFunction(il, true);
        }

        [Opcode(OpCount.One, 137)]
        private void op_remove_obj(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("InsertObject", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.One, 138)]
        private void op_print_obj(ILGenerator il)
        {
            MethodInfo getNameMI = typeof(ZMachine).GetMethod("GetObjectName", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo printStringMI = typeof(ZMachine).GetMethod("PrintString", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getNameMI);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.One, 139, Terminates = true)]
        private void op_ret(ILGenerator il)
        {
            LoadOperand(il, 0);
            LeaveFunction(il);
        }

        [Opcode(OpCount.One, 140, Terminates = true)]
        private void op_jump(ILGenerator il)
        {
            FieldInfo pcFI = typeof(ZMachine).GetField("pc", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, zm.PC - 2);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stfld, pcFI);
            compiling = false;
        }

        [Opcode(OpCount.One, 141)]
        private void op_print_paddr(ILGenerator il)
        {
            MethodInfo decodeStringMI = typeof(ZMachine).GetMethod("DecodeString", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo printStringMI = typeof(ZMachine).GetMethod("PrintString", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo unpackAddrMI = typeof(ZMachine).GetMethod("UnpackAddress", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Dup);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, unpackAddrMI);
            il.Emit(OpCodes.Call, decodeStringMI);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.One, 142, true)]
        private void op_load(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("LoadVariableImpl", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
            StoreResult(il);
        }

        [Opcode(OpCount.One, 143, Terminates = true)]
        private void op_call_1n(ILGenerator il)
        {
            EnterFunction(il, false);
        }
    }
}