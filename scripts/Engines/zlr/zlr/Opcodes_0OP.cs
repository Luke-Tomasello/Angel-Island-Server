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

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ZLR.VM
{
    partial class Opcode
    {
        [Opcode(OpCount.Zero, 176, Terminates = true)]
        private void op_rtrue(ILGenerator il)
        {
            LeaveFunctionConst(il, 1);
        }

        [Opcode(OpCount.Zero, 177, Terminates = true)]
        private void op_rfalse(ILGenerator il)
        {
            LeaveFunctionConst(il, 0);
        }

        [Opcode(OpCount.Zero, 178, false, false, true)]
        private void op_print(ILGenerator il)
        {
            MethodInfo printStringMI = typeof(ZMachine).GetMethod("PrintString", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, operandText);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.Zero, 179, false, false, true, Terminates = true)]
        private void op_print_ret(ILGenerator il)
        {
            op_print(il);
            op_new_line(il);
            LeaveFunctionConst(il, 1);
        }

        [Opcode(OpCount.Zero, 180)]
        private void op_nop(ILGenerator il)
        {
            // do nothing
        }

        // 0OP:181 and 182 are illegal in V5

        [Opcode(OpCount.Zero, 183, Terminates = true)]
        private void op_restart(ILGenerator il)
        {
            MethodInfo restartMI = typeof(ZMachine).GetMethod("Restart", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, restartMI);
            compiling = false;
        }

        [Opcode(OpCount.Zero, 184, Terminates = true)]
        private void op_ret_popped(ILGenerator il)
        {
            PopFromStack(il);
            LeaveFunction(il);
        }

        [Opcode(OpCount.Zero, 185, true)]
        private void op_catch(ILGenerator il)
        {
            FieldInfo callStackFI = typeof(ZMachine).GetField("callStack", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo getCountMI = typeof(Stack<ZMachine.CallFrame>).GetMethod("get_Count");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, callStackFI);
            il.Emit(OpCodes.Call, getCountMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Zero, 186, Terminates = true)]
        private void op_quit(ILGenerator il)
        {
            FieldInfo runningFI = typeof(ZMachine).GetField("running", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stfld, runningFI);
            compiling = false;
        }

        [Opcode(OpCount.Zero, 187)]
        private void op_new_line(ILGenerator il)
        {
            MethodInfo printZsciiMI = typeof(ZMachine).GetMethod("PrintZSCII", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_S, (byte)13);
            il.Emit(OpCodes.Call, printZsciiMI);
        }

        [Opcode(OpCount.Zero, 189, false, true, false)]
        private void op_verify(ILGenerator il)
        {
            MethodInfo impl = typeof(ZMachine).GetMethod("VerifyGameFile", BindingFlags.NonPublic | BindingFlags.Instance);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, impl);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }

        [Opcode(OpCount.Zero, 191, false, true, false)]
        private void op_piracy(ILGenerator il)
        {
            // assume it's genuine
            if (branchIfTrue)
                Branch(il);
        }
    }
}