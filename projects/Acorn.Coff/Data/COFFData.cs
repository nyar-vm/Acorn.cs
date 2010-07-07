using System.Text;
using Acorn;
using Acorn.Attributes;
using Acorn.Codec;

namespace Acorn.Coff.Data;

/// <summary>
///     COFF 文件头数据。
/// </summary>
[BinarySerializable(Endianness = Endianness.LittleEndian)]
public partial struct CoffHeaderData
{
    /// <summary>
    ///     机器类型。
    /// </summary>
    [Field(Order = 0)]
    public ushort Machine;

    /// <summary>
    ///     节区数量。
    /// </summary>
    [Field(Order = 1)]
    public ushort NumberOfSections;

    /// <summary>
    ///     时间戳。
    /// </summary>
    [Field(Order = 2)]
    public uint TimeDateStamp;

    /// <summary>
    ///     符号表指针。
    /// </summary>
    [Field(Order = 3)]
    public uint PointerToSymbolTable;

    /// <summary>
    ///     符号数量。
    /// </summary>
    [Field(Order = 4)]
    public uint NumberOfSymbols;

    /// <summary>
    ///     可选头大小。
    /// </summary>
    [Field(Order = 5)]
    public ushort SizeOfOptionalHeader;

    /// <summary>
    ///     特征标志。
    /// </summary>
    [Field(Order = 6)]
    public ushort Characteristics;
}

/// <summary>
///     COFF 节区头数据。
/// </summary>
[BinarySerializable(Endianness = Endianness.LittleEndian)]
public partial struct CoffSectionHeaderData
{
    /// <summary>
    ///     节区名称（原始 8 字节）。
    /// </summary>
    [Field(Order = 0, Length = 8)]
    public FixedBytes8 NameBytes;

    /// <summary>
    ///     物理地址或虚拟大小。
    /// </summary>
    [Field(Order = 1)]
    public uint PhysicalAddress;

    /// <summary>
    ///     虚拟地址。
    /// </summary>
    [Field(Order = 2)]
    public uint VirtualAddress;

    /// <summary>
    ///     原始数据大小。
    /// </summary>
    [Field(Order = 3)]
    public uint SizeOfRawData;

    /// <summary>
    ///     原始数据指针。
    /// </summary>
    [Field(Order = 4)]
    public uint PointerToRawData;

    /// <summary>
    ///     重定位指针。
    /// </summary>
    [Field(Order = 5)]
    public uint PointerToRelocations;

    /// <summary>
    ///     行号指针。
    /// </summary>
    [Field(Order = 6)]
    public uint PointerToLinenumbers;

    /// <summary>
    ///     重定位数量。
    /// </summary>
    [Field(Order = 7)]
    public ushort NumberOfRelocations;

    /// <summary>
    ///     行号数量。
    /// </summary>
    [Field(Order = 8)]
    public ushort NumberOfLinenumbers;

    /// <summary>
    ///     特征标志。
    /// </summary>
    [Field(Order = 9)]
    public uint Characteristics;

    /// <summary>
    ///     节区名称（解码后的字符串）。
    /// </summary>
    public readonly string Name => Encoding.UTF8.GetString(NameBytes.AsSpan()).TrimEnd('\0');
}

/// <summary>
///     COFF 符号表条目数据。
/// </summary>
public sealed class CoffSymbolData
{
    /// <summary>
    ///     符号名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     值。
    /// </summary>
    public uint Value { get; init; }

    /// <summary>
    ///     节区编号。
    /// </summary>
    public short SectionNumber { get; init; }

    /// <summary>
    ///     类型。
    /// </summary>
    public ushort Type { get; init; }

    /// <summary>
    ///     存储类别。
    /// </summary>
    public byte StorageClass { get; init; }

    /// <summary>
    ///     辅助计数。
    /// </summary>
    public byte NumberOfAuxSymbols { get; init; }
}

/// <summary>
///     COFF 重定位数据。
/// </summary>
[BinarySerializable(Endianness = Endianness.LittleEndian)]
public partial struct CoffRelocationData
{
    /// <summary>
    ///     虚拟地址。
    /// </summary>
    [Field(Order = 0)]
    public uint VirtualAddress;

    /// <summary>
    ///     符号表索引。
    /// </summary>
    [Field(Order = 1)]
    public uint SymbolTableIndex;

    /// <summary>
    ///     类型。
    /// </summary>
    [Field(Order = 2)]
    public ushort Type;
}

/// <summary>
///     COFF 文件数据。
/// </summary>
public sealed class CoffFileData
{
    /// <summary>
    ///     COFF 头数据。
    /// </summary>
    public CoffHeaderData Header { get; init; } = new();

    /// <summary>
    ///     节区头列表。
    /// </summary>
    public IReadOnlyList<CoffSectionHeaderData> Sections { get; init; } = [];

    /// <summary>
    ///     符号表。
    /// </summary>
    public IReadOnlyList<CoffSymbolData> Symbols { get; init; } = [];

    /// <summary>
    ///     重定位列表。
    /// </summary>
    public IReadOnlyList<CoffRelocationData> Relocations { get; init; } = [];
}
