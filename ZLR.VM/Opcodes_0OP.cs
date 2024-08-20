using System.Reflection.Emit;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace ZLR.VM
{
    partial class Opcode
    {
#pragma warning disable 0169
        [Opcode(OpCount.Zero, 176, Terminates = true)]
        private void op_rtrue([NotNull] ILGenerator il)
        {
            LeaveFunctionConst(il, 1);
        }

        [Opcode(OpCount.Zero, 177, Terminates = true)]
        private void op_rfalse([NotNull] ILGenerator il)
        {
            LeaveFunctionConst(il, 0);
        }

        [Opcode(OpCount.Zero, 178, Text = true)]
        private void op_print([NotNull] ILGenerator il)
        {
            var printStringMI = ZMachine.GetMethodInfo(nameof(ZMachine.PrintString));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, operandText);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.Zero, 179, Text = true, Terminates = true)]
        private void op_print_ret([NotNull] ILGenerator il)
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
        private void op_restart([NotNull] ILGenerator il)
        {
            var restartMI = ZMachine.GetMethodInfo(nameof(ZMachine.Restart));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, restartMI);
            compiling = false;
        }

        [Opcode(OpCount.Zero, 184, Terminates = true)]
        private void op_ret_popped([NotNull] ILGenerator il)
        {
            PopFromStack(il);
            LeaveFunction(il);
        }

        [Opcode(OpCount.Zero, 185, MaxVersion = 4)]
        private void op_pop([NotNull] ILGenerator il)
        {
            PopFromStack(il);
            il.Emit(OpCodes.Pop);
        }

        [Opcode(OpCount.Zero, 185, Store = true, MinVersion = 5)]
        private void op_catch([NotNull] ILGenerator il)
        {
            var callStackFI = ZMachine.GetFieldInfo(nameof(ZMachine.callStack));
            var getCountMI = typeof(Stack<ZMachine.CallFrame>).GetProperty(nameof(Stack<ZMachine.CallFrame>.Count))?.GetGetMethod();
            System.Diagnostics.Debug.Assert(getCountMI != null);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, callStackFI);
            il.Emit(OpCodes.Call, getCountMI);
            StoreResult(il);
        }

        [Opcode(OpCount.Zero, 186, Terminates = true)]
        private void op_quit([NotNull] ILGenerator il)
        {
            var runningFI = ZMachine.GetFieldInfo(nameof(ZMachine.running));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stfld, runningFI);
            compiling = false;
        }

        [Opcode(OpCount.Zero, 187)]
        private void op_new_line([NotNull] ILGenerator il)
        {
            var printZsciiMI = ZMachine.GetMethodInfo(nameof(ZMachine.PrintZSCII));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_S, (byte) 13);
            il.Emit(OpCodes.Call, printZsciiMI);
        }

        [Opcode(OpCount.Zero, 188)]
        private void op_show_status(ILGenerator il)
        {
            // "In theory this opcode is illegal in later Versions [>= V4] but an interpreter should treat it as nop,
            // because Version 5 Release 23 of 'Wishbringer' contains this opcode by accident." -- Standard 1.0

            if (zm.ZVersion < 4)
            {
                var showStatusMI = ZMachine.GetMethodInfo(nameof(ZMachine.ShowStatusImpl));

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, showStatusMI);
            }
        }

        [Opcode(OpCount.Zero, 189, Branch = true, MinVersion = 3)]
        private void op_verify([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.VerifyGameFile));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, impl);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }

        [Opcode(OpCount.Zero, 191, Branch = true, MinVersion = 5)]
        private void op_piracy(ILGenerator il)
        {
            // assume it's genuine
            if (branchIfTrue)
                Branch(il);
        }
#pragma warning restore 0169
    }
}
