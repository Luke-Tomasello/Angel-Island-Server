using System.Reflection.Emit;
using JetBrains.Annotations;

namespace ZLR.VM
{
    partial class Opcode
    {
#pragma warning disable 0169
        [Opcode(OpCount.One, 128, Branch = true)]
        private void op_jz([NotNull] ILGenerator il)
        {
            LoadOperand(il, 0);
            Branch(il, OpCodes.Brfalse, OpCodes.Brtrue);
        }

        [Opcode(OpCount.One, 129, Store = true, Branch = true)]
        private void op_get_sibling([NotNull] ILGenerator il)
        {
            var getSiblingMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetObjectSibling));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getSiblingMI);

            il.Emit(OpCodes.Dup);
            StoreResult(il);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }

        [Opcode(OpCount.One, 130, Store = true, Branch = true)]
        private void op_get_child([NotNull] ILGenerator il)
        {
            var getChildMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetObjectChild));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getChildMI);

            il.Emit(OpCodes.Dup);
            StoreResult(il);
            Branch(il, OpCodes.Brtrue, OpCodes.Brfalse);
        }

        [Opcode(OpCount.One, 131, Store = true)]
        private void op_get_parent([NotNull] ILGenerator il)
        {
            var getParentMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetObjectParent));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getParentMI);
            StoreResult(il);
        }

        [Opcode(OpCount.One, 132, Store = true)]
        private void op_get_prop_len([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.GetPropLength));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
            StoreResult(il);
        }

        [Opcode(OpCount.One, 133, IndirectVar = true)]
        private void op_inc([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.IncImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Pop);
        }

        [Opcode(OpCount.One, 134, IndirectVar = true)]
        private void op_dec([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.IncImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Call, impl);
            il.Emit(OpCodes.Pop);
        }

        [Opcode(OpCount.One, 135)]
        private void op_print_addr([NotNull] ILGenerator il)
        {
            var decodeStringMI = ZMachine.GetMethodInfo(nameof(ZMachine.DecodeString));
            var printStringMI = ZMachine.GetMethodInfo(nameof(ZMachine.PrintString));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Conv_U2);
            il.Emit(OpCodes.Call, decodeStringMI);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.One, 136, Store = true, Terminates = true, MinVersion = 4)]
        private void op_call_1s([NotNull] ILGenerator il)
        {
            EnterFunction(il, true);
        }

        [Opcode(OpCount.One, 137)]
        private void op_remove_obj([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.InsertObject));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, impl);
        }

        [Opcode(OpCount.One, 138)]
        private void op_print_obj([NotNull] ILGenerator il)
        {
            var getNameMI = ZMachine.GetMethodInfo(nameof(ZMachine.GetObjectName));
            var printStringMI = ZMachine.GetMethodInfo(nameof(ZMachine.PrintString));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, getNameMI);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.One, 139, Terminates = true)]
        private void op_ret([NotNull] ILGenerator il)
        {
            LoadOperand(il, 0);
            LeaveFunction(il);
        }

        [Opcode(OpCount.One, 140, Terminates = true)]
        private void op_jump([NotNull] ILGenerator il)
        {
            var pcFI = ZMachine.GetFieldInfo(nameof(ZMachine.pc));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, zm.PC - 2);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stfld, pcFI);
            compiling = false;
        }

        [Opcode(OpCount.One, 141)]
        private void op_print_paddr([NotNull] ILGenerator il)
        {
            var decodeStringMI = ZMachine.GetMethodInfo(nameof(ZMachine.DecodeString));
            var printStringMI = ZMachine.GetMethodInfo(nameof(ZMachine.PrintString));
            var unpackAddrMI = ZMachine.GetMethodInfo(nameof(ZMachine.UnpackAddress));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Dup);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, unpackAddrMI);
            il.Emit(OpCodes.Call, decodeStringMI);
            il.Emit(OpCodes.Call, printStringMI);
        }

        [Opcode(OpCount.One, 142, Store = true, IndirectVar = true)]
        private void op_load([NotNull] ILGenerator il)
        {
            var impl = ZMachine.GetMethodInfo(nameof(ZMachine.LoadVariableImpl));

            il.Emit(OpCodes.Ldarg_0);
            LoadOperand(il, 0);
            il.Emit(OpCodes.Call, impl);
            StoreResult(il);
        }

        [Opcode(OpCount.One, 143, Terminates = true, MinVersion = 5)]
        private void op_call_1n([NotNull] ILGenerator il)
        {
            EnterFunction(il, false);
        }
#pragma warning restore 0169
    }
}
