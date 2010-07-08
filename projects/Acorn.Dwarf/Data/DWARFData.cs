namespace Acorn.DWARF.Data;

/// <summary>
///     DWARF 编译单元数据。
/// </summary>
public sealed class DWARFCompilationUnitData
{
    /// <summary>
    ///     单元长度。
    /// </summary>
    public uint UnitLength { get; init; }

    /// <summary>
    ///     DWARF 版本。
    /// </summary>
    public ushort Version { get; init; }

    /// <summary>
    ///     调试信息偏移。
    /// </summary>
    public uint DebugInfoOffset { get; init; }

    /// <summary>
    ///     地址大小。
    /// </summary>
    public byte AddressSize { get; init; }

    /// <summary>
    ///     节区偏移大小。
    /// </summary>
    public byte SegmentSelectorSize { get; init; }

    /// <summary>
    ///     条目列表。
    /// </summary>
    public IReadOnlyList<DWARFEntryData> Entries { get; init; } = [];
}

/// <summary>
///     DWARF 条目数据。
/// </summary>
public sealed class DWARFEntryData
{
    /// <summary>
    ///     缩略码。
    /// </summary>
    public ulong AbbreviationCode { get; init; }

    /// <summary>
    ///     标签。
    /// </summary>
    public uint Tag { get; init; }

    /// <summary>
    ///     是否有子项。
    /// </summary>
    public bool HasChildren { get; init; }

    /// <summary>
    ///     属性列表。
    /// </summary>
    public IReadOnlyList<DWARFAttributeData> Attributes { get; init; } = [];
}

/// <summary>
///     DWARF 属性数据。
/// </summary>
public sealed class DWARFAttributeData
{
    /// <summary>
    ///     属性名称。
    /// </summary>
    public uint Name { get; init; }

    /// <summary>
    ///     属性形式。
    /// </summary>
    public uint Form { get; init; }

    /// <summary>
    ///     属性值。
    /// </summary>
    public object? Value { get; init; }
}

/// <summary>
///     DWARF 行号表数据。
/// </summary>
public sealed class DWARFLineNumberTableData
{
    /// <summary>
    ///     单元长度。
    /// </summary>
    public uint UnitLength { get; init; }

    /// <summary>
    ///     DWARF 版本。
    /// </summary>
    public ushort Version { get; init; }

    /// <summary>
    ///     地址大小。
    /// </summary>
    public byte AddressSize { get; init; }

    /// <summary>
    ///     段选择器大小。
    /// </summary>
    public byte SegmentSelectorSize { get; init; }

    /// <summary>
    ///     头长度。
    /// </summary>
    public uint HeaderLength { get; init; }

    /// <summary>
    ///     最小指令长度。
    /// </summary>
    public byte MinimumInstructionLength { get; init; }

    /// <summary>
    ///     最大操作数每指令。
    /// </summary>
    public byte MaximumOperationsPerInstruction { get; init; }

    /// <summary>
    ///     默认是否语句。
    /// </summary>
    public byte DefaultIsStatement { get; init; }

    /// <summary>
    ///     行基。
    /// </summary>
    public sbyte LineBase { get; init; }

    /// <summary>
    ///     行范围。
    /// </summary>
    public byte LineRange { get; init; }

    /// <summary>
    ///     操作码基。
    /// </summary>
    public byte OpcodeBase { get; init; }

    /// <summary>
    ///     标准操作码长度。
    /// </summary>
    public IReadOnlyList<byte> StandardOpcodeLengths { get; init; } = [];

    /// <summary>
    ///     文件列表。
    /// </summary>
    public IReadOnlyList<string> FileNames { get; init; } = [];
}

/// <summary>
///     DWARF 文件数据。
/// </summary>
public sealed class DWARFFileData
{
    /// <summary>
    ///     编译单元列表。
    /// </summary>
    public IReadOnlyList<DWARFCompilationUnitData> CompilationUnits { get; init; } = [];

    /// <summary>
    ///     行号表列表。
    /// </summary>
    public IReadOnlyList<DWARFLineNumberTableData> LineNumberTables { get; init; } = [];
}