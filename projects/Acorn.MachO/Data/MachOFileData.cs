namespace Acorn.MachO.Data;

/// <summary>
///     Mach-O 文件头数据。
/// </summary>
public sealed class MachOHeaderData
{
    /// <summary>
    ///     魔数（0xFEEDFACE=32位, 0xFEEDFACF=64位）。
    /// </summary>
    public uint Magic { get; init; }

    /// <summary>
    ///     CPU 类型。
    /// </summary>
    public int CPUType { get; init; }

    /// <summary>
    ///     CPU 子类型。
    /// </summary>
    public int CPUSubtype { get; init; }

    /// <summary>
    ///     文件类型。
    /// </summary>
    public uint FileType { get; init; }

    /// <summary>
    ///     加载命令数量。
    /// </summary>
    public uint NumberOfLoadCommands { get; init; }

    /// <summary>
    ///     加载命令总大小。
    /// </summary>
    public uint SizeOfLoadCommands { get; init; }

    /// <summary>
    ///     标志。
    /// </summary>
    public uint Flags { get; init; }

    /// <summary>
    ///     保留（64位）。
    /// </summary>
    public uint Reserved { get; init; }

    /// <summary>
    ///     是否为小端序。
    /// </summary>
    public bool IsLittleEndian { get; init; } = true;

    /// <summary>
    ///     是否为 64 位。
    /// </summary>
    public bool Is64Bit => Magic == 0xFEEDFACF;

    /// <summary>
    ///     是否为可执行文件。
    /// </summary>
    public bool IsExecutable => FileType == 2;

    /// <summary>
    ///     是否为动态库。
    /// </summary>
    public bool IsDynamicLibrary => FileType == 6;
}

/// <summary>
///     Mach-O 加载命令数据。
/// </summary>
public sealed class MachOLoadCommandData
{
    /// <summary>
    ///     命令类型。
    /// </summary>
    public uint Command { get; init; }

    /// <summary>
    ///     命令大小。
    /// </summary>
    public uint Size { get; init; }

    /// <summary>
    ///     命令数据。
    /// </summary>
    public byte[] Data { get; init; } = [];
}

/// <summary>
///     Mach-O 节区数据。
/// </summary>
public sealed class MachOSectionData
{
    /// <summary>
    ///     节区名称。
    /// </summary>
    public string SectionName { get; init; } = string.Empty;

    /// <summary>
    ///     段名称。
    /// </summary>
    public string SegmentName { get; init; } = string.Empty;

    /// <summary>
    ///     内存地址。
    /// </summary>
    public ulong Address { get; init; }

    /// <summary>
    ///     大小。
    /// </summary>
    public ulong Size { get; init; }

    /// <summary>
    ///     文件偏移。
    /// </summary>
    public uint Offset { get; init; }

    /// <summary>
    ///     对齐。
    /// </summary>
    public uint Alignment { get; init; }

    /// <summary>
    ///     重定位文件偏移。
    /// </summary>
    public uint RelocationsOffset { get; init; }

    /// <summary>
    ///     重定位数量。
    /// </summary>
    public uint NumberOfRelocations { get; init; }

    /// <summary>
    ///     标志。
    /// </summary>
    public uint Flags { get; init; }

    /// <summary>
    ///     节区原始内容数据。
    /// </summary>
    public byte[] Content { get; set; } = [];
}

/// <summary>
///     Mach-O 文件数据。
/// </summary>
public sealed class MachOFileData
{
    /// <summary>
    ///     Mach-O 头数据。
    /// </summary>
    public MachOHeaderData Header { get; init; } = new();

    /// <summary>
    ///     加载命令列表。
    /// </summary>
    public IReadOnlyList<MachOLoadCommandData> LoadCommands { get; init; } = [];

    /// <summary>
    ///     节区列表。
    /// </summary>
    public IReadOnlyList<MachOSectionData> Sections { get; init; } = [];
}