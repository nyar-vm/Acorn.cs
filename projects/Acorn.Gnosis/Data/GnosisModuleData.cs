using Acorn.Gnosis.Decode;

namespace Acorn.Gnosis.Data;

/// <summary>
///     Gnosis 字节码模块数据。
/// </summary>
/// <remarks>
///     .gnosis 文件是 Gnosis VM 的字节码模块格式，基于 Game 方言特化。
/// </remarks>
public sealed class GnosisModuleData
{
    /// <summary>
    ///     版本号。
    /// </summary>
    public ushort Version { get; init; }

    /// <summary>
    ///     模块名称。
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    ///     常量池。
    /// </summary>
    public IReadOnlyList<GnosisConstant> Constants { get; init; } = [];

    /// <summary>
    ///     导入符号列表。
    /// </summary>
    public IReadOnlyList<string> ImportedSymbols { get; init; } = [];

    /// <summary>
    ///     导出符号列表。
    /// </summary>
    public IReadOnlyList<string> ExportedSymbols { get; init; } = [];

    /// <summary>
    ///     依赖模块列表。
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    /// <summary>
    ///     指令字节码（扁平指令流，无函数表）。
    /// </summary>
    public byte[] Instructions { get; init; } = [];

    /// <summary>
    ///     解码指令字节码为结构化指令列表。
    /// </summary>
    public IReadOnlyList<GnosisInstruction> DecodeInstructions()
    {
        if (Instructions.Length == 0)
        {
            return [];
        }

        var decoder = new GnosisInstructionDecoder(Instructions);
        return decoder.DecodeAll();
    }

    /// <summary>
    ///     统计指令总数（不创建指令对象，性能优于 DecodeInstructions）。
    /// </summary>
    public int CountInstructions()
    {
        if (Instructions.Length == 0)
        {
            return 0;
        }

        var decoder = new GnosisInstructionDecoder(Instructions);
        return decoder.CountInstructions();
    }
}

/// <summary>
///     Gnosis 常量池条目。
/// </summary>
public sealed class GnosisConstant
{
    /// <summary>
    ///     常量类型标签。
    /// </summary>
    public GnosisConstantTag Tag { get; init; }

    /// <summary>
    ///     常量值。
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    ///     类型名称。
    /// </summary>
    public string TagName => Tag switch
    {
        GnosisConstantTag.String => "String",
        GnosisConstantTag.Int => "Int",
        GnosisConstantTag.Float => "Float",
        _ => $"Unknown(0x{(byte)Tag:X2})"
    };
}

/// <summary>
///     Gnosis VM 解码后的指令，包含操作码和操作数。
/// </summary>
public sealed class GnosisInstruction
{
    /// <summary>
    ///     指令在字节码流中的偏移量。
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    ///     操作码。
    /// </summary>
    public GnosisOpCode OpCode { get; init; }

    /// <summary>
    ///     操作数原始值（64 位统一存储，由 OpCode 决定实际宽度）。
    /// </summary>
    public long RawOperand { get; init; }

    /// <summary>
    ///     操作数格式。
    /// </summary>
    public GnosisOperandFormat OperandFormat => GnosisOpCodeInfo.GetOperandFormat(OpCode);

    /// <summary>
    ///     指令类别。
    /// </summary>
    public GnosisInstructionCategory Category => GnosisOpCodeInfo.GetCategory(OpCode);

    /// <summary>
    ///     指令总字节长度（操作码 + 操作数）。
    /// </summary>
    public int ByteLength => 1 + GnosisOpCodeInfo.GetOperandSize(OpCode);

    /// <summary>
    ///     操作码名称。
    /// </summary>
    public string OpCodeName => OpCode.ToString();
}

/// <summary>
///     Gnosis VM 操作码辅助信息，提供操作数格式、大小和类别查询。
/// </summary>
public static class GnosisOpCodeInfo
{
    /// <summary>
    ///     获取操作码的操作数格式。
    /// </summary>
    public static GnosisOperandFormat GetOperandFormat(GnosisOpCode opCode)
    {
        return opCode switch
        {
            GnosisOpCode.PushInt8 => GnosisOperandFormat.Int8,
            GnosisOpCode.PushInt16 => GnosisOperandFormat.Int16,
            GnosisOpCode.PushInt64 => GnosisOperandFormat.Int64,
            GnosisOpCode.PushFloat32 => GnosisOperandFormat.Float32,
            GnosisOpCode.PushFloat64 => GnosisOperandFormat.Float64,
            GnosisOpCode.PushInt32 or GnosisOpCode.Jump or GnosisOpCode.JumpIfTrue
                or GnosisOpCode.JumpIfFalse or GnosisOpCode.Call or GnosisOpCode.CallNative
                or GnosisOpCode.LoadLocal or GnosisOpCode.StoreLocal or GnosisOpCode.LoadGlobal
                or GnosisOpCode.StoreGlobal or GnosisOpCode.LoadField or GnosisOpCode.StoreField
                or GnosisOpCode.NewObject or GnosisOpCode.GetField or GnosisOpCode.SetField
                or GnosisOpCode.AddComponent or GnosisOpCode.GetComponent or GnosisOpCode.RemoveComponent
                or GnosisOpCode.SetComponent or GnosisOpCode.HasComponent
                or GnosisOpCode.QueryWith or GnosisOpCode.QueryWithout
                or GnosisOpCode.DefineComponent or GnosisOpCode.DefineSystem or GnosisOpCode.SystemSchedule
                or GnosisOpCode.PushString or GnosisOpCode.NewArray or GnosisOpCode.MakeClosure
                or GnosisOpCode.IsType or GnosisOpCode.TypeOf or GnosisOpCode.QueryAll
                or GnosisOpCode.QueryAny => GnosisOperandFormat.Int32,
            GnosisOpCode.CallModule => GnosisOperandFormat.Int64,
            _ => GnosisOperandFormat.None
        };
    }

    /// <summary>
    ///     获取操作码的操作数字节大小。
    /// </summary>
    public static int GetOperandSize(GnosisOpCode opCode)
    {
        return GetOperandFormat(opCode) switch
        {
            GnosisOperandFormat.None => 0,
            GnosisOperandFormat.Int8 => 1,
            GnosisOperandFormat.Int16 => 2,
            GnosisOperandFormat.Int32 => 4,
            GnosisOperandFormat.Int64 => 8,
            GnosisOperandFormat.Float32 => 4,
            GnosisOperandFormat.Float64 => 8,
            _ => 0
        };
    }

    /// <summary>
    ///     获取操作码的指令类别。
    /// </summary>
    public static GnosisInstructionCategory GetCategory(GnosisOpCode opCode)
    {
        var categoryByte = (byte)opCode >> 4;

        return categoryByte switch
        {
            0x0 => GnosisInstructionCategory.Control,
            0x1 => GnosisInstructionCategory.Constant,
            0x2 => GnosisInstructionCategory.Stack,
            0x3 => opCode >= GnosisOpCode.EqualInt
                ? GnosisInstructionCategory.Comparison
                : GnosisInstructionCategory.Arithmetic,
            0x4 => opCode <= GnosisOpCode.JumpIfFalse
                ? GnosisInstructionCategory.ControlFlow
                : opCode <= GnosisOpCode.GreaterEqualFloat
                    ? GnosisInstructionCategory.Comparison
                    : GnosisInstructionCategory.Logic,
            0x5 => GnosisInstructionCategory.Call,
            0x6 => GnosisInstructionCategory.Variable,
            0x7 => GnosisInstructionCategory.Object,
            0x8 => GnosisInstructionCategory.Ecs,
            0x9 => GnosisInstructionCategory.String,
            0xA => GnosisInstructionCategory.Array,
            0xB => GnosisInstructionCategory.Closure,
            0xC => GnosisInstructionCategory.TypeCheck,
            0xD => GnosisInstructionCategory.Coroutine,
            _ => GnosisInstructionCategory.Control
        };
    }
}
