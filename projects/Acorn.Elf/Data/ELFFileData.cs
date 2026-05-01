namespace Acorn.ELF.Data;

/// <summary>
///     ELF 文件头数据。
/// </summary>
public sealed class ELFHeaderData
{
    /// <summary>
    ///     魔数（0x7F 'E' 'L' 'F'）。
    /// </summary>
    public byte[] Magic { get; init; } = [];

    /// <summary>
    ///     类别（32位/64位）。
    /// </summary>
    public byte Class { get; init; }

    /// <summary>
    ///     数据编码（小端/大端）。
    /// </summary>
    public byte DataEncoding { get; init; }

    /// <summary>
    ///     文件版本。
    /// </summary>
    public byte Version { get; init; }

    /// <summary>
    ///     OS/ABI。
    /// </summary>
    public byte OSABI { get; init; }

    /// <summary>
    ///     ABI 版本。
    /// </summary>
    public byte ABIVersion { get; init; }

    /// <summary>
    ///     文件类型。
    /// </summary>
    public ushort Type { get; init; }

    /// <summary>
    ///     机器类型。
    /// </summary>
    public ushort Machine { get; init; }

    /// <summary>
    ///     对象文件版本。
    /// </summary>
    public uint ObjectVersion { get; init; }

    /// <summary>
    ///     入口点地址。
    /// </summary>
    public ulong EntryPoint { get; init; }

    /// <summary>
    ///     程序头偏移。
    /// </summary>
    public ulong ProgramHeaderOffset { get; init; }

    /// <summary>
    ///     节区头偏移。
    /// </summary>
    public ulong SectionHeaderOffset { get; init; }

    /// <summary>
    ///     标志。
    /// </summary>
    public uint Flags { get; init; }

    /// <summary>
    ///     ELF 头大小。
    /// </summary>
    public ushort ELFHeaderSize { get; init; }

    /// <summary>
    ///     程序头大小。
    /// </summary>
    public ushort ProgramHeaderSize { get; init; }

    /// <summary>
    ///     程序头数量。
    /// </summary>
    public ushort ProgramHeaderCount { get; init; }

    /// <summary>
    ///     节区头大小。
    /// </summary>
    public ushort SectionHeaderSize { get; init; }

    /// <summary>
    ///     节区头数量。
    /// </summary>
    public ushort SectionHeaderCount { get; init; }

    /// <summary>
    ///     字符串表节区索引。
    /// </summary>
    public ushort StringTableIndex { get; init; }

    /// <summary>
    ///     是否为 64 位。
    /// </summary>
    public bool Is64Bit => Class == ElfConstants.Class64;

    /// <summary>
    ///     是否为小端。
    /// </summary>
    public bool IsLittleEndian => DataEncoding == ElfConstants.DataEncodingLittleEndian;
}

/// <summary>
///     ELF 节区头数据。
/// </summary>
public sealed class ELFSectionHeaderData
{
    /// <summary>
    ///     节区名称字符串表索引。
    /// </summary>
    public uint NameIndex { get; init; }

    /// <summary>
    ///     节区名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     节区类型。
    /// </summary>
    public uint Type { get; init; }

    /// <summary>
    ///     节区标志。
    /// </summary>
    public ulong Flags { get; init; }

    /// <summary>
    ///     虚拟地址。
    /// </summary>
    public ulong Address { get; init; }

    /// <summary>
    ///     文件偏移。
    /// </summary>
    public ulong Offset { get; init; }

    /// <summary>
    ///     节区大小。
    /// </summary>
    public ulong Size { get; init; }

    /// <summary>
    ///     链接索引。
    /// </summary>
    public uint Link { get; init; }

    /// <summary>
    ///     附加信息。
    /// </summary>
    public uint Info { get; init; }

    /// <summary>
    ///     对齐。
    /// </summary>
    public ulong Alignment { get; init; }

    /// <summary>
    ///     条目大小。
    /// </summary>
    public ulong EntrySize { get; init; }

    /// <summary>
    ///     节区原始内容数据。
    /// </summary>
    public byte[] Content { get; init; } = [];
}

/// <summary>
///     ELF 程序头数据。
/// </summary>
public sealed class ELFProgramHeaderData
{
    /// <summary>
    ///     段类型。
    /// </summary>
    public uint Type { get; init; }

    /// <summary>
    ///     段标志。
    /// </summary>
    public uint Flags { get; init; }

    /// <summary>
    ///     文件偏移。
    /// </summary>
    public ulong Offset { get; init; }

    /// <summary>
    ///     虚拟地址。
    /// </summary>
    public ulong VirtualAddress { get; init; }

    /// <summary>
    ///     物理地址。
    /// </summary>
    public ulong PhysicalAddress { get; init; }

    /// <summary>
    ///     文件大小。
    /// </summary>
    public ulong FileSize { get; init; }

    /// <summary>
    ///     内存大小。
    /// </summary>
    public ulong MemorySize { get; init; }

    /// <summary>
    ///     对齐。
    /// </summary>
    public ulong Alignment { get; init; }
}

/// <summary>
///     ELF 文件数据。
/// </summary>
public sealed class ELFFileData
{
    /// <summary>
    ///     ELF 头数据。
    /// </summary>
    public ELFHeaderData Header { get; init; } = new();

    /// <summary>
    ///     节区头列表。
    /// </summary>
    public IReadOnlyList<ELFSectionHeaderData> SectionHeaders { get; init; } = [];

    /// <summary>
    ///     程序头列表。
    /// </summary>
    public IReadOnlyList<ELFProgramHeaderData> ProgramHeaders { get; init; } = [];

    /// <summary>
    ///     是否为可执行文件。
    /// </summary>
    public bool IsExecutable => Header.Type == ElfConstants.TypeExecutable;

    /// <summary>
    ///     是否为共享库。
    /// </summary>
    public bool IsSharedLibrary => Header.Type == ElfConstants.TypeSharedLibrary;
}