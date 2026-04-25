using System.Text;
using Acorn.Gnosis.Data;

namespace Acorn.Gnosis.Decode;

/// <summary>
///     Gnosis VM 反汇编器，将字节码指令转为可读的 .gnosis.asm 文本格式。
/// </summary>
/// <remarks>
///     输出格式：每行一条指令，格式为 "偏移量: 操作码 操作数"。
///     无操作数的指令仅显示操作码名称。
///     跳转指令的操作数显示为绝对地址。
/// </remarks>
public sealed class GnosisDisassembler
{
    /// <summary>
    ///     将指令列表反汇编为文本。
    /// </summary>
    /// <param name="instructions">指令列表。</param>
    /// <returns>反汇编文本。</returns>
    public string Disassemble(IReadOnlyList<GnosisInstruction> instructions)
    {
        var sb = new StringBuilder(instructions.Count * 32);

        foreach (var instruction in instructions)
        {
            DisassembleInstruction(sb, instruction);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    ///     将模块数据反汇编为完整的 .gnosis.asm 文本。
    /// </summary>
    /// <param name="module">模块数据。</param>
    /// <returns>反汇编文本。</returns>
    public string DisassembleModule(GnosisModuleData module)
    {
        var sb = new StringBuilder(4096);

        sb.AppendLine($"; .gnosis 反汇编输出");
        sb.AppendLine($"; 模块: {module.ModuleName}");
        sb.AppendLine($"; 版本: {module.Version}");
        sb.AppendLine();

        if (module.Constants.Count > 0)
        {
            sb.AppendLine($".constants [{module.Constants.Count}]");

            for (var i = 0; i < module.Constants.Count; i++)
            {
                var constant = module.Constants[i];
                sb.AppendLine($"  {i,4}: {constant.TagName} = {FormatConstantValue(constant)}");
            }

            sb.AppendLine();
        }

        if (module.ImportedSymbols.Count > 0)
        {
            sb.AppendLine($".imports [{module.ImportedSymbols.Count}]");

            foreach (var symbol in module.ImportedSymbols)
            {
                sb.AppendLine($"  {symbol}");
            }

            sb.AppendLine();
        }

        if (module.ExportedSymbols.Count > 0)
        {
            sb.AppendLine($".exports [{module.ExportedSymbols.Count}]");

            foreach (var symbol in module.ExportedSymbols)
            {
                sb.AppendLine($"  {symbol}");
            }

            sb.AppendLine();
        }

        if (module.Dependencies.Count > 0)
        {
            sb.AppendLine($".dependencies [{module.Dependencies.Count}]");

            foreach (var dep in module.Dependencies)
            {
                sb.AppendLine($"  {dep}");
            }

            sb.AppendLine();
        }

        var decodedInstructions = module.DecodeInstructions();

        if (decodedInstructions.Count > 0)
        {
            sb.AppendLine($".code [{decodedInstructions.Count} instructions, {module.Instructions.Length} bytes]");

            foreach (var instruction in decodedInstructions)
            {
                DisassembleInstruction(sb, instruction);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    #region 私有方法

    private static void DisassembleInstruction(StringBuilder sb, GnosisInstruction instruction)
    {
        sb.Append($"  {instruction.Offset,6}: {instruction.OpCodeName}");

        var format = instruction.OperandFormat;

        if (format == GnosisOperandFormat.None)
        {
            return;
        }

        sb.Append(' ');

        switch (format)
        {
            case GnosisOperandFormat.Int8:
                sb.Append((byte)instruction.RawOperand);
                break;
            case GnosisOperandFormat.Int16:
                sb.Append((short)instruction.RawOperand);
                break;
            case GnosisOperandFormat.Int32:
                FormatInt32Operand(sb, instruction.OpCode, (int)instruction.RawOperand);
                break;
            case GnosisOperandFormat.Int64:
                sb.Append(instruction.RawOperand);
                break;
            case GnosisOperandFormat.Float32:
                sb.Append(BitConverter.Int32BitsToSingle((int)instruction.RawOperand));
                break;
            case GnosisOperandFormat.Float64:
                sb.Append(BitConverter.Int64BitsToDouble(instruction.RawOperand));
                break;
        }
    }

    private static void FormatInt32Operand(StringBuilder sb, GnosisOpCode opCode, int operand)
    {
        if (opCode == GnosisOpCode.Jump || opCode == GnosisOpCode.JumpIfTrue || opCode == GnosisOpCode.JumpIfFalse)
        {
            sb.Append($"-> {operand}");
        }
        else if (opCode == GnosisOpCode.PushString)
        {
            sb.Append($"const[{operand}]");
        }
        else if (opCode == GnosisOpCode.CallNative)
        {
            sb.Append($"native[{operand}]");
        }
        else if (opCode == GnosisOpCode.Call)
        {
            sb.Append($"addr[{operand}]");
        }
        else if (opCode is GnosisOpCode.LoadLocal or GnosisOpCode.StoreLocal)
        {
            sb.Append($"local[{operand}]");
        }
        else if (opCode is GnosisOpCode.LoadGlobal or GnosisOpCode.StoreGlobal)
        {
            sb.Append($"global[{operand}]");
        }
        else if (opCode is GnosisOpCode.LoadField or GnosisOpCode.StoreField or GnosisOpCode.GetField or GnosisOpCode.SetField)
        {
            sb.Append($"field[{operand}]");
        }
        else if (opCode is GnosisOpCode.AddComponent or GnosisOpCode.GetComponent or GnosisOpCode.RemoveComponent
            or GnosisOpCode.SetComponent or GnosisOpCode.HasComponent or GnosisOpCode.QueryWith
            or GnosisOpCode.QueryWithout or GnosisOpCode.DefineComponent or GnosisOpCode.DefineSystem
            or GnosisOpCode.SystemSchedule)
        {
            sb.Append($"type[{operand}]");
        }
        else if (opCode is GnosisOpCode.QueryAll or GnosisOpCode.QueryAny)
        {
            sb.Append($"count={operand}");
        }
        else if (opCode == GnosisOpCode.NewObject)
        {
            sb.Append($"type[{operand}]");
        }
        else if (opCode == GnosisOpCode.NewArray)
        {
            sb.Append($"size={operand}");
        }
        else if (opCode == GnosisOpCode.MakeClosure)
        {
            sb.Append($"addr[{operand}]");
        }
        else if (opCode is GnosisOpCode.IsType or GnosisOpCode.TypeOf)
        {
            sb.Append($"type[{operand}]");
        }
        else
        {
            sb.Append(operand);
        }
    }

    private static string FormatConstantValue(GnosisConstant constant)
    {
        return constant.Tag switch
        {
            GnosisConstantTag.String => $"\"{constant.Value}\"",
            GnosisConstantTag.Int => $"{constant.Value}",
            GnosisConstantTag.Float => $"{constant.Value}f",
            _ => $"{constant.Value}"
        };
    }

    #endregion
}
