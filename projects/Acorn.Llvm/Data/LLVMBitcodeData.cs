namespace Acorn.LLVM.Data;

/// <summary>
///     LLVM Bitcode 魔数和标识数据。
/// </summary>
public sealed class LLVMMagicData
{
    /// <summary>
    ///     魔数（'B' 'C' 0xC0 0xDE）。
    /// </summary>
    public uint Magic { get; init; }

    /// <summary>
    ///     LLVM 版本号。
    /// </summary>
    public ushort Version { get; init; }

    /// <summary>
    ///     是否为包装格式。
    /// </summary>
    public bool IsWrapped => Magic == 0x0B17C0DE;
}

/// <summary>
///     LLVM Bitcode 块数据。
/// </summary>
public sealed class LLVMBlockData
{
    /// <summary>
    ///     块 ID。
    /// </summary>
    public uint BlockID { get; init; }

    /// <summary>
    ///     块名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     块大小（位）。
    /// </summary>
    public uint BlockSize { get; init; }

    /// <summary>
    ///     子块列表。
    /// </summary>
    public IReadOnlyList<LLVMBlockData> SubBlocks { get; init; } = [];

    /// <summary>
    ///     记录列表。
    /// </summary>
    public IReadOnlyList<LLVMRecordData> Records { get; init; } = [];
}

/// <summary>
///     LLVM Bitcode 记录数据。
/// </summary>
public sealed class LLVMRecordData
{
    /// <summary>
    ///     记录代码。
    /// </summary>
    public uint Code { get; init; }

    /// <summary>
    ///     记录名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     操作数列表。
    /// </summary>
    public IReadOnlyList<ulong> Operands { get; init; } = [];
}

/// <summary>
///     LLVM Bitcode 模块数据。
/// </summary>
public sealed class LLVMModuleData
{
    /// <summary>
    ///     模块标识符。
    /// </summary>
    public string ModuleID { get; init; } = string.Empty;

    /// <summary>
    ///     目标三元组。
    /// </summary>
    public string TargetTriple { get; init; } = string.Empty;

    /// <summary>
    ///     数据布局。
    /// </summary>
    public string DataLayout { get; init; } = string.Empty;

    /// <summary>
    ///     函数列表。
    /// </summary>
    public IReadOnlyList<LLVMFunctionData> Functions { get; init; } = [];

    /// <summary>
    ///     全局变量列表。
    /// </summary>
    public IReadOnlyList<LLVMGlobalData> Globals { get; init; } = [];
}

/// <summary>
///     LLVM 函数数据。
/// </summary>
public sealed class LLVMFunctionData
{
    /// <summary>
    ///     函数名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     返回类型。
    /// </summary>
    public string ReturnType { get; init; } = string.Empty;

    /// <summary>
    ///     参数列表。
    /// </summary>
    public IReadOnlyList<string> Parameters { get; init; } = [];

    /// <summary>
    ///     基本块数量。
    /// </summary>
    public int BasicBlockCount { get; init; }

    /// <summary>
    ///     指令数量。
    /// </summary>
    public int InstructionCount { get; init; }
}

/// <summary>
///     LLVM 全局变量数据。
/// </summary>
public sealed class LLVMGlobalData
{
    /// <summary>
    ///     变量名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     变量类型。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    ///     是否常量。
    /// </summary>
    public bool IsConstant { get; init; }

    /// <summary>
    ///     链接类型。
    /// </summary>
    public uint Linkage { get; init; }
}

/// <summary>
///     LLVM Bitcode 文件数据。
/// </summary>
public sealed class LLVMBitcodeData
{
    /// <summary>
    ///     魔数数据。
    /// </summary>
    public LLVMMagicData Magic { get; init; } = new();

    /// <summary>
    ///     顶层块列表。
    /// </summary>
    public IReadOnlyList<LLVMBlockData> TopLevelBlocks { get; init; } = [];

    /// <summary>
    ///     模块列表。
    /// </summary>
    public IReadOnlyList<LLVMModuleData> Modules { get; init; } = [];
}