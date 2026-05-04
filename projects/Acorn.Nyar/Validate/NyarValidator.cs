using Acorn.Nyar.Data;

namespace Acorn.Nyar.Validate;

/// <summary>
///     Nyar 字节码验证器，确保加载的字节码安全且格式正确。
///     使用 Acorn.Nyar.Data 数据模型，不依赖 NyarVM 运行时类型。
/// </summary>
public sealed class NyarValidator
{
    /// <summary>
    ///     验证模块的字节码安全性
    /// </summary>
    /// <param name="module">要验证的模块数据</param>
    /// <param name="bytecode">原始字节码</param>
    /// <param name="diagnostics">验证诊断信息</param>
    /// <returns>验证是否通过</returns>
    public bool Validate(NyarModuleData module, byte[] bytecode, out List<string> diagnostics)
    {
        diagnostics = new List<string>();
        var valid = true;

        foreach (var func in module.Functions)
        {
            if (!ValidateFunction(func, bytecode, diagnostics))
            {
                valid = false;
            }
        }

        foreach (var import in module.Imports)
        {
            if (string.IsNullOrEmpty(import.ModuleName) || string.IsNullOrEmpty(import.SymbolName))
            {
                diagnostics.Add("Invalid import: module or symbol name is empty.");
                valid = false;
            }
        }

        return valid;
    }

    #region 函数验证

    /// <summary>
    ///     验证单个函数的字节码
    /// </summary>
    private static bool ValidateFunction(NyarFunction func, byte[] bytecode, List<string> diagnostics)
    {
        var valid = true;

        if (func.CodeOffset < 0 || func.CodeOffset >= bytecode.Length)
        {
            diagnostics.Add($"Function '{func.Name}': invalid code offset {func.CodeOffset}.");
            return false;
        }

        if (func.CodeOffset + func.CodeLength > bytecode.Length)
        {
            diagnostics.Add(
                $"Function '{func.Name}': code range [{func.CodeOffset}, {func.CodeOffset + func.CodeLength}) exceeds bytecode length {bytecode.Length}.");
            return false;
        }

        if (func.Arity < 0)
        {
            diagnostics.Add($"Function '{func.Name}': negative arity {func.Arity}.");
            valid = false;
        }

        if (func.LocalCount < 0)
        {
            diagnostics.Add($"Function '{func.Name}': negative local count {func.LocalCount}.");
            valid = false;
        }

        valid = ValidateJumpTargets(func, bytecode, diagnostics) && valid;

        return valid;
    }

    #endregion

    #region 跳转目标验证

    /// <summary>
    ///     验证跳转目标是否在函数代码范围内
    /// </summary>
    private static bool ValidateJumpTargets(NyarFunction func, byte[] bytecode, List<string> diagnostics)
    {
        var valid = true;
        var end = func.CodeOffset + func.CodeLength;

        for (var pc = func.CodeOffset; pc < end;)
        {
            var op = bytecode[pc];
            var opcode = (NyarOpcode)op;

            if (!Enum.IsDefined(typeof(NyarOpcode), opcode))
            {
                diagnostics.Add($"Function '{func.Name}': undefined opcode 0x{op:X2} at offset {pc}.");
                valid = false;
                pc++;
                continue;
            }

            var instructionSize = GetInstructionSize(opcode);

            if (opcode is NyarOpcode.Jump or NyarOpcode.JumpIfTrue or NyarOpcode.JumpIfFalse)
            {
                if (pc + 1 + 4 <= end)
                {
                    var target = BitConverter.ToInt32(bytecode, pc + 1);
                    if (target < func.CodeOffset || target >= end)
                    {
                        diagnostics.Add(
                            $"Function '{func.Name}': jump target {target} out of range [{func.CodeOffset}, {end}) at offset {pc}.");
                        valid = false;
                    }
                }
            }

            pc += instructionSize;
        }

        return valid;
    }

    #endregion

    #region 指令大小

    /// <summary>
    ///     获取指令大小（操作码 + 操作数）
    /// </summary>
    private static int GetInstructionSize(NyarOpcode opcode)
    {
        return opcode switch
        {
            NyarOpcode.Nop => 1,
            NyarOpcode.Jump => 5,
            NyarOpcode.JumpIfTrue => 5,
            NyarOpcode.JumpIfFalse => 5,
            NyarOpcode.Call => 5,
            NyarOpcode.Return => 1,
            NyarOpcode.TailCall => 5,
            NyarOpcode.Throw => 1,
            NyarOpcode.Catch => 5,
            NyarOpcode.Yield => 1,
            NyarOpcode.Resume => 5,
            NyarOpcode.EffectHandle => 5,
            NyarOpcode.Const => 5,
            NyarOpcode.Pop => 1,
            NyarOpcode.Dup => 1,
            NyarOpcode.Swap => 1,
            NyarOpcode.LoadLocal => 5,
            NyarOpcode.StoreLocal => 5,
            NyarOpcode.LoadArg => 5,
            NyarOpcode.LoadGlobal => 5,
            NyarOpcode.StoreGlobal => 5,
            NyarOpcode.Alloc => 5,
            NyarOpcode.Free => 1,
            NyarOpcode.I32Load => 5,
            NyarOpcode.I32Store => 5,
            NyarOpcode.I64Load => 5,
            NyarOpcode.I64Store => 5,
            NyarOpcode.NewObject => 5,
            NyarOpcode.GetField => 5,
            NyarOpcode.SetField => 5,
            NyarOpcode.GetIndex => 1,
            NyarOpcode.SetIndex => 1,
            NyarOpcode.Length => 1,
            NyarOpcode.NewClosure => 5,
            NyarOpcode.GetUpvalue => 5,
            NyarOpcode.SetUpvalue => 5,
            NyarOpcode.Exit => 1,
            NyarOpcode.GetTime => 1,
            NyarOpcode.Sleep => 1,
            NyarOpcode.MathSin => 1,
            NyarOpcode.MathCos => 1,
            NyarOpcode.MathSqrt => 1,
            NyarOpcode.MathAbs => 1,
            NyarOpcode.MathRand => 1,
            NyarOpcode.I32Add => 1,
            NyarOpcode.I32Sub => 1,
            NyarOpcode.I32Mul => 1,
            NyarOpcode.I32DivS => 1,
            NyarOpcode.I32DivU => 1,
            NyarOpcode.I32RemS => 1,
            NyarOpcode.I32RemU => 1,
            NyarOpcode.I32And => 1,
            NyarOpcode.I32Or => 1,
            NyarOpcode.I32Xor => 1,
            NyarOpcode.I32Shl => 1,
            NyarOpcode.I32ShrS => 1,
            NyarOpcode.I32ShrU => 1,
            NyarOpcode.I32Eq => 1,
            NyarOpcode.I32Ne => 1,
            NyarOpcode.I32LtS => 1,
            NyarOpcode.I32LtU => 1,
            NyarOpcode.I32LeS => 1,
            NyarOpcode.I32LeU => 1,
            NyarOpcode.I32GtS => 1,
            NyarOpcode.I32GtU => 1,
            NyarOpcode.I32GeS => 1,
            NyarOpcode.I32GeU => 1,
            NyarOpcode.I64Add => 1,
            NyarOpcode.I64Sub => 1,
            NyarOpcode.I64Mul => 1,
            NyarOpcode.I64DivS => 1,
            NyarOpcode.I64DivU => 1,
            NyarOpcode.I64Neg => 1,
            NyarOpcode.F32Add => 1,
            NyarOpcode.F32Sub => 1,
            NyarOpcode.F32Mul => 1,
            NyarOpcode.F32Div => 1,
            NyarOpcode.F32Neg => 1,
            NyarOpcode.F64Add => 1,
            NyarOpcode.F64Sub => 1,
            NyarOpcode.F64Mul => 1,
            NyarOpcode.F64Div => 1,
            NyarOpcode.F64Neg => 1,
            NyarOpcode.I32ExtendI64S => 1,
            NyarOpcode.I32ExtendI64U => 1,
            NyarOpcode.I64TruncI32S => 1,
            NyarOpcode.I64TruncI32U => 1,
            NyarOpcode.I32ToF32S => 1,
            NyarOpcode.I32ToF64S => 1,
            NyarOpcode.StringConcat => 1,
            NyarOpcode.StringLenBytes => 1,
            NyarOpcode.StringLenChars => 1,
            NyarOpcode.StringSubstr => 1,
            NyarOpcode.BigIntAdd => 1,
            NyarOpcode.BigIntSub => 1,
            NyarOpcode.BigIntMul => 1,
            NyarOpcode.BuiltinCall => 5,
            _ => 1
        };
    }

    #endregion
}
