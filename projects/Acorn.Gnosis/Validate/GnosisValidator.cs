using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Validate;

/// <summary>
///     Gnosis 字节码模块验证器，确保加载的字节码安全且格式正确。
///     使用 Acorn.Gnosis.Data 数据模型，不依赖 GnosisVM 运行时类型。
/// </summary>
public sealed class GnosisValidator
{
    /// <summary>
    ///     验证模块的结构完整性和字节码安全性。
    /// </summary>
    /// <param name="module">要验证的模块数据</param>
    /// <param name="diagnostics">验证诊断信息</param>
    /// <returns>验证是否通过</returns>
    public bool Validate(GnosisModuleData module, out List<string> diagnostics)
    {
        diagnostics = new List<string>();
        var valid = true;

        valid = ValidateHeader(module, diagnostics) && valid;
        valid = ValidateConstants(module.Constants, diagnostics) && valid;
        valid = ValidateSymbols(module.ImportedSymbols, "导入", diagnostics) && valid;
        valid = ValidateSymbols(module.ExportedSymbols, "导出", diagnostics) && valid;
        valid = ValidateDependencies(module.Dependencies, diagnostics) && valid;
        valid = ValidateInstructions(module.Instructions, diagnostics) && valid;

        return valid;
    }

    /// <summary>
    ///     验证原始字节码的结构完整性（魔数 + 版本 + 字段范围）。
    /// </summary>
    /// <param name="bytecode">原始 .gnosis 字节数据</param>
    /// <param name="diagnostics">验证诊断信息</param>
    /// <returns>验证是否通过</returns>
    public bool ValidateRaw(byte[] bytecode, out List<string> diagnostics)
    {
        diagnostics = new List<string>();
        var valid = true;

        if (bytecode.Length < GnosisConstants.MinHeaderSize)
        {
            diagnostics.Add($"字节码长度 {bytecode.Length} 小于最小头部大小 {GnosisConstants.MinHeaderSize}。");
            return false;
        }

        var magic = BitConverter.ToUInt32(bytecode, 0);
        if (magic != GnosisConstants.MagicValue)
        {
            diagnostics.Add($"魔数无效，期望 GNOS(0x474E4F53)，实际 0x{magic:X8}。");
            valid = false;
        }

        var version = BitConverter.ToUInt16(bytecode, 4);
        if (version > GnosisConstants.CurrentVersion)
        {
            diagnostics.Add($"版本号 {version} 大于当前支持版本 {GnosisConstants.CurrentVersion}。");
            valid = false;
        }

        return valid;
    }

    #region 头部验证

    /// <summary>
    ///     验证模块头部字段。
    /// </summary>
    private static bool ValidateHeader(GnosisModuleData module, List<string> diagnostics)
    {
        var valid = true;

        if (string.IsNullOrEmpty(module.ModuleName))
        {
            diagnostics.Add("模块名称为空。");
            valid = false;
        }

        if (module.Version > GnosisConstants.CurrentVersion)
        {
            diagnostics.Add($"版本号 {module.Version} 大于当前支持版本 {GnosisConstants.CurrentVersion}。");
            valid = false;
        }

        if (module.Version == 0)
        {
            diagnostics.Add("版本号为 0，无效。");
            valid = false;
        }

        return valid;
    }

    #endregion

    #region 常量池验证

    /// <summary>
    ///     验证常量池条目的有效性。
    /// </summary>
    private static bool ValidateConstants(IReadOnlyList<GnosisConstant> constants, List<string> diagnostics)
    {
        var valid = true;

        for (var i = 0; i < constants.Count; i++)
        {
            var c = constants[i];

            if (!Enum.IsDefined(typeof(GnosisConstantTag), c.Tag))
            {
                diagnostics.Add($"常量池 [{i}]：未知标签 0x{(byte)c.Tag:X2}。");
                valid = false;
                continue;
            }

            if (c.Value is null)
            {
                diagnostics.Add($"常量池 [{i}]：值为 null（标签 {c.TagName}）。");
                valid = false;
                continue;
            }

            switch (c.Tag)
            {
                case GnosisConstantTag.String when c.Value is not string:
                    diagnostics.Add($"常量池 [{i}]：String 标签但值类型为 {c.Value.GetType().Name}。");
                    valid = false;
                    break;
                case GnosisConstantTag.Int when c.Value is not int and not long:
                    diagnostics.Add($"常量池 [{i}]：Int 标签但值类型为 {c.Value.GetType().Name}。");
                    valid = false;
                    break;
                case GnosisConstantTag.Float when c.Value is not float and not double:
                    diagnostics.Add($"常量池 [{i}]：Float 标签但值类型为 {c.Value.GetType().Name}。");
                    valid = false;
                    break;
            }
        }

        return valid;
    }

    #endregion

    #region 符号表验证

    /// <summary>
    ///     验证符号列表的有效性。
    /// </summary>
    private static bool ValidateSymbols(IReadOnlyList<string> symbols, string label, List<string> diagnostics)
    {
        var valid = true;

        for (var i = 0; i < symbols.Count; i++)
        {
            if (string.IsNullOrEmpty(symbols[i]))
            {
                diagnostics.Add($"{label}符号 [{i}]：名称为空。");
                valid = false;
            }
        }

        var duplicates = symbols
            .Select((s, i) => (s, i))
            .GroupBy(x => x.s)
            .Where(g => g.Count() > 1);

        foreach (var dup in duplicates)
        {
            var indices = string.Join(", ", dup.Select(x => x.i));
            diagnostics.Add($"{label}符号重复：'{dup.Key}' 出现在索引 [{indices}]。");
            valid = false;
        }

        return valid;
    }

    #endregion

    #region 依赖验证

    /// <summary>
    ///     验证依赖模块列表的有效性。
    /// </summary>
    private static bool ValidateDependencies(IReadOnlyList<string> dependencies, List<string> diagnostics)
    {
        var valid = true;

        for (var i = 0; i < dependencies.Count; i++)
        {
            if (string.IsNullOrEmpty(dependencies[i]))
            {
                diagnostics.Add($"依赖模块 [{i}]：名称为空。");
                valid = false;
            }
        }

        var duplicates = dependencies
            .Select((s, i) => (s, i))
            .GroupBy(x => x.s)
            .Where(g => g.Count() > 1);

        foreach (var dup in duplicates)
        {
            var indices = string.Join(", ", dup.Select(x => x.i));
            diagnostics.Add($"依赖模块重复：'{dup.Key}' 出现在索引 [{indices}]。");
            valid = false;
        }

        return valid;
    }

    #endregion

    #region 指令验证

    /// <summary>
    ///     验证指令字节码的有效性。
    /// </summary>
    private static bool ValidateInstructions(byte[] instructions, List<string> diagnostics)
    {
        if (instructions.Length == 0)
        {
            return true;
        }

        var valid = true;
        var instructionOffsets = new HashSet<int>();

        for (var pc = 0; pc < instructions.Length;)
        {
            var op = instructions[pc];
            var opcode = (GnosisOpCode)op;

            if (!Enum.IsDefined(typeof(GnosisOpCode), opcode))
            {
                diagnostics.Add($"偏移 {pc}：未定义操作码 0x{op:X2}。");
                valid = false;
                pc++;
                continue;
            }

            var operandSize = GnosisOpCodeInfo.GetOperandSize(opcode);
            var instructionSize = 1 + operandSize;

            if (pc + instructionSize > instructions.Length)
            {
                diagnostics.Add($"偏移 {pc}：指令 {opcode} 需要 {instructionSize} 字节，但剩余 {instructions.Length - pc} 字节。");
                valid = false;
                break;
            }

            instructionOffsets.Add(pc);
            pc += instructionSize;
        }

        valid = ValidateJumpTargets(instructions, instructionOffsets, diagnostics) && valid;

        return valid;
    }

    /// <summary>
    ///     验证跳转目标是否在指令流范围内且对齐到指令起始位置。
    /// </summary>
    private static bool ValidateJumpTargets(byte[] instructions, HashSet<int> instructionOffsets, List<string> diagnostics)
    {
        var valid = true;

        for (var pc = 0; pc < instructions.Length;)
        {
            var op = instructions[pc];
            var opcode = (GnosisOpCode)op;

            if (!Enum.IsDefined(typeof(GnosisOpCode), opcode))
            {
                pc++;
                continue;
            }

            var operandSize = GnosisOpCodeInfo.GetOperandSize(opcode);
            var instructionSize = 1 + operandSize;

            if (opcode is GnosisOpCode.Jump or GnosisOpCode.JumpIfTrue or GnosisOpCode.JumpIfFalse)
            {
                if (pc + 1 + 4 <= instructions.Length)
                {
                    var target = BitConverter.ToInt32(instructions, pc + 1);

                    if (target < 0 || target > instructions.Length)
                    {
                        diagnostics.Add($"偏移 {pc}：跳转目标 {target} 超出指令流范围 [0, {instructions.Length}]。");
                        valid = false;
                    }
                    else if (target != instructions.Length && !instructionOffsets.Contains(target))
                    {
                        diagnostics.Add($"偏移 {pc}：跳转目标 {target} 未对齐到指令起始位置。");
                        valid = false;
                    }
                }
            }

            pc += instructionSize;
        }

        return valid;
    }

    #endregion
}
