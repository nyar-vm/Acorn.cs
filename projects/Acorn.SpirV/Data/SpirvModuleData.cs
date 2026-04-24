using Acorn;
using Acorn.Attributes;
using Acorn.Codec;

namespace Acorn.Spirv.Data;

/// <summary>
///     SPIR-V 文件头部（20 字节）。
/// </summary>
/// <remarks>
///     SPIR-V 文件头由 5 个 32 位字组成：魔数、版本号、生成器魔数、ID 绑定值和保留字。
///     字节序为小端序，魔数 0x07230203 同时用于检测字节序是否正确。
/// </remarks>
[BinarySerializable(Endianness = Endianness.LittleEndian)]
public partial struct SpirvFileHeader
{
    /// <summary>
    ///     SPIR-V 魔数（0x07230203）。
    /// </summary>
    [Field(Order = 0)]
    public uint MagicNumber;

    /// <summary>
    ///     SPIR-V 版本号（如 0x00010000 表示 1.0，0x00010300 表示 1.3）。
    /// </summary>
    [Field(Order = 1)]
    public uint Version;

    /// <summary>
    ///     生成器魔数，标识生成此 SPIR-V 模块的工具。
    /// </summary>
    [Field(Order = 2)]
    public uint GeneratorMagic;

    /// <summary>
    ///     ID 绑定值，所有 ID 必须小于此值。
    /// </summary>
    [Field(Order = 3)]
    public uint Bound;

    /// <summary>
    ///     保留字（通常为 0）。
    /// </summary>
    [Field(Order = 4)]
    public uint Schema;
}

/// <summary>
///     SPIR-V 模块数据，表示一个完整的 SPIR-V 着色器二进制中间语言模块。
/// </summary>
/// <remarks>
///     SPIR-V 是 Khronos 定义的着色器二进制中间语言，用于 Vulkan、OpenCL 等图形和计算 API。
///     整个模块由 32 位字流组成，包含文件头和指令序列。
/// </remarks>
public sealed class SpirvModuleData
{
    /// <summary>
    ///     SPIR-V 魔数（0x07230203）。
    /// </summary>
    public uint MagicNumber { get; init; } = SpirvConstants.MagicNumber;

    /// <summary>
    ///     SPIR-V 版本号（如 0x00010000 表示 1.0，0x00010300 表示 1.3）。
    /// </summary>
    public uint Version { get; init; } = SpirvConstants.Version10;

    /// <summary>
    ///     生成器魔数，标识生成此 SPIR-V 模块的工具。
    /// </summary>
    public uint GeneratorMagic { get; init; }

    /// <summary>
    ///     ID 绑定值，所有 ID 必须小于此值。
    /// </summary>
    public uint Bound { get; init; }

    /// <summary>
    ///     保留字（通常为 0）。
    /// </summary>
    public uint Schema { get; init; }

    /// <summary>
    ///     指令列表。
    /// </summary>
    public IReadOnlyList<SpirvInstruction> Instructions { get; init; } = [];

    /// <summary>
    ///     入口点列表（由 <c>DecodeAll</c> 填充）。
    /// </summary>
    public IReadOnlyList<SpirvEntryPoint> EntryPoints { get; init; } = [];

    /// <summary>
    ///     装饰信息列表（由 <c>DecodeAll</c> 填充）。
    /// </summary>
    public IReadOnlyList<SpirvDecorationInfo> Decorations { get; init; } = [];

    /// <summary>
    ///     名称信息列表（由 <c>DecodeAll</c> 填充）。
    /// </summary>
    public IReadOnlyList<SpirvName> Names { get; init; } = [];

    /// <summary>
    ///     类型信息列表（由 <c>DecodeAll</c> 填充）。
    /// </summary>
    public IReadOnlyList<SpirvTypeInfo> Types { get; init; } = [];
}

/// <summary>
///     SPIR-V 指令，表示一条完整的 SPIR-V 指令。
/// </summary>
/// <remarks>
///     每条指令的第一个字包含操作码（低 16 位）和字数（高 16 位），
///     后续字为操作数。操作数的含义取决于操作码。
/// </remarks>
public sealed class SpirvInstruction
{
    /// <summary>
    ///     操作码。
    /// </summary>
    public SpirvOpCode Opcode { get; init; }

    /// <summary>
    ///     指令总字数（包含操作码字本身）。
    /// </summary>
    public ushort WordCount { get; init; }

    /// <summary>
    ///     操作数字列表（不包含第一个操作码字）。
    /// </summary>
    public IReadOnlyList<uint> Operands { get; init; } = [];
}

/// <summary>
///     SPIR-V 入口点信息。
/// </summary>
public sealed class SpirvEntryPoint
{
    /// <summary>
    ///     执行模型。
    /// </summary>
    public SpirvExecutionModel ExecutionModel { get; init; }

    /// <summary>
    ///     入口点函数的 ID。
    /// </summary>
    public uint EntryPointId { get; init; }

    /// <summary>
    ///     入口点名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     使用的接口 ID 列表。
    /// </summary>
    public IReadOnlyList<uint> InterfaceIds { get; init; } = [];
}

/// <summary>
///     SPIR-V 装饰信息。
/// </summary>
public sealed class SpirvDecorationInfo
{
    /// <summary>
    ///     目标 ID。
    /// </summary>
    public uint TargetId { get; init; }

    /// <summary>
    ///     装饰类型。
    /// </summary>
    public SpirvDecoration Decoration { get; init; }

    /// <summary>
    ///     装饰的额外操作数。
    /// </summary>
    public IReadOnlyList<uint> ExtraOperands { get; init; } = [];
}

/// <summary>
///     SPIR-V 名称信息。
/// </summary>
public sealed class SpirvName
{
    /// <summary>
    ///     目标 ID。
    /// </summary>
    public uint TargetId { get; init; }

    /// <summary>
    ///     名称字符串。
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
///     SPIR-V 类型信息。
/// </summary>
public sealed class SpirvTypeInfo
{
    /// <summary>
    ///     结果 ID。
    /// </summary>
    public uint ResultId { get; init; }

    /// <summary>
    ///     类型操作码。
    /// </summary>
    public SpirvOpCode Opcode { get; init; }

    /// <summary>
    ///     类型操作数。
    /// </summary>
    public IReadOnlyList<uint> Operands { get; init; } = [];
}
