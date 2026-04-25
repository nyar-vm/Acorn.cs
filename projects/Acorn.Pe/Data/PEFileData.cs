using System.Text;
using Acorn;
using Acorn.Attributes;
using Acorn.Codec;

namespace Acorn.Pe.Data;

/// <summary>
///     PE 文件头数据。
/// </summary>
public sealed class PeHeaderData
{
    /// <summary>
    ///     DOS 头魔数（"MZ"）。
    /// </summary>
    public ushort DosMagic { get; init; }

    /// <summary>
    ///     PE 头偏移。
    /// </summary>
    public uint PeHeaderOffset { get; init; }

    /// <summary>
    ///     PE 头魔数（"PE\0\0"）。
    /// </summary>
    public uint PeMagic { get; init; }

    /// <summary>
    ///     机器类型。
    /// </summary>
    public ushort Machine { get; init; }

    /// <summary>
    ///     节区数量。
    /// </summary>
    public ushort NumberOfSections { get; init; }

    /// <summary>
    ///     时间戳。
    /// </summary>
    public uint TimeDateStamp { get; init; }

    /// <summary>
    ///     符号表偏移。
    /// </summary>
    public uint PointerToSymbolTable { get; init; }

    /// <summary>
    ///     符号数量。
    /// </summary>
    public uint NumberOfSymbols { get; init; }

    /// <summary>
    ///     可选头大小。
    /// </summary>
    public ushort SizeOfOptionalHeader { get; init; }

    /// <summary>
    ///     特征标志。
    /// </summary>
    public ushort Characteristics { get; init; }
}

/// <summary>
///     PE 可选头数据。
/// </summary>
public sealed class PeOptionalHeaderData
{
    /// <summary>
    ///     魔术数字（0x10B=PE32, 0x20B=PE32+）。
    /// </summary>
    public ushort Magic { get; init; }

    /// <summary>
    ///     主版本号。
    /// </summary>
    public byte MajorLinkerVersion { get; init; }

    /// <summary>
    ///     次版本号。
    /// </summary>
    public byte MinorLinkerVersion { get; init; }

    /// <summary>
    ///     代码节大小。
    /// </summary>
    public uint SizeOfCode { get; init; }

    /// <summary>
    ///     初始化数据节大小。
    /// </summary>
    public uint SizeOfInitializedData { get; init; }

    /// <summary>
    ///     未初始化数据节大小。
    /// </summary>
    public uint SizeOfUninitializedData { get; init; }

    /// <summary>
    ///     入口点地址。
    /// </summary>
    public uint AddressOfEntryPoint { get; init; }

    /// <summary>
    ///     代码基址。
    /// </summary>
    public uint BaseOfCode { get; init; }

    /// <summary>
    ///     数据基址（PE32 only）。
    /// </summary>
    public uint BaseOfData { get; init; }

    /// <summary>
    ///     镜像基址。
    /// </summary>
    public ulong ImageBase { get; init; }

    /// <summary>
    ///     节区对齐。
    /// </summary>
    public uint SectionAlignment { get; init; }

    /// <summary>
    ///     文件对齐。
    /// </summary>
    public uint FileAlignment { get; init; }

    /// <summary>
    ///     操作系统主版本号。
    /// </summary>
    public ushort MajorOperatingSystemVersion { get; init; }

    /// <summary>
    ///     操作系统次版本号。
    /// </summary>
    public ushort MinorOperatingSystemVersion { get; init; }

    /// <summary>
    ///     镜像主版本号。
    /// </summary>
    public ushort MajorImageVersion { get; init; }

    /// <summary>
    ///     镜像次版本号。
    /// </summary>
    public ushort MinorImageVersion { get; init; }

    /// <summary>
    ///     子系统主版本号。
    /// </summary>
    public ushort MajorSubsystemVersion { get; init; }

    /// <summary>
    ///     子系统次版本号。
    /// </summary>
    public ushort MinorSubsystemVersion { get; init; }

    /// <summary>
    ///     Win32 版本值。
    /// </summary>
    public uint Win32VersionValue { get; init; }

    /// <summary>
    ///     镜像大小。
    /// </summary>
    public uint SizeOfImage { get; init; }

    /// <summary>
    ///     头大小。
    /// </summary>
    public uint SizeOfHeaders { get; init; }

    /// <summary>
    ///     校验和。
    /// </summary>
    public uint CheckSum { get; init; }

    /// <summary>
    ///     子系统。
    /// </summary>
    public ushort Subsystem { get; init; }

    /// <summary>
    ///     DLL 特征。
    /// </summary>
    public ushort DllCharacteristics { get; init; }

    /// <summary>
    ///     栈保留大小。
    /// </summary>
    public ulong SizeOfStackReserve { get; init; }

    /// <summary>
    ///     栈提交大小。
    /// </summary>
    public ulong SizeOfStackCommit { get; init; }

    /// <summary>
    ///     堆保留大小。
    /// </summary>
    public ulong SizeOfHeapReserve { get; init; }

    /// <summary>
    ///     堆提交大小。
    /// </summary>
    public ulong SizeOfHeapCommit { get; init; }

    /// <summary>
    ///     加载器标志。
    /// </summary>
    public uint LoaderFlags { get; init; }

    /// <summary>
    ///     数据目录数量。
    /// </summary>
    public uint NumberOfRvaAndSizes { get; init; }

    /// <summary>
    ///     数据目录列表。每项包含 RVA 和 Size，索引对应 <see cref="PeDataDirectoryIndex" />。
    /// </summary>
    public IReadOnlyList<PeDataDirectoryEntry> DataDirectories { get; init; } = [];
}

/// <summary>
///     PE 数据目录条目。
/// </summary>
public sealed class PeDataDirectoryEntry
{
    /// <summary>
    ///     数据的相对虚拟地址（RVA）。
    /// </summary>
    public uint Rva { get; init; }

    /// <summary>
    ///     数据大小（字节）。
    /// </summary>
    public uint Size { get; init; }

    /// <summary>
    ///     数据目录是否为空（RVA 和 Size 均为 0）。
    /// </summary>
    public bool IsEmpty => Rva == 0 && Size == 0;
}

/// <summary>
///     PE 数据目录索引（ECMA-335 和 PE/COFF 标准定义）。
/// </summary>
public enum PeDataDirectoryIndex
{
    ExportTable = 0,
    ImportTable = 1,
    ResourceTable = 2,
    ExceptionTable = 3,
    CertificateTable = 4,
    BaseRelocationTable = 5,
    Debug = 6,
    Architecture = 7,
    GlobalPtr = 8,
    TlsTable = 9,
    LoadConfigTable = 10,
    BoundImport = 11,
    Iat = 12,
    DelayImportDescriptor = 13,
    ClrRuntimeHeader = 14,
    Reserved = 15
}

/// <summary>
///     PE 节区数据。
/// </summary>
[BinarySerializable(Endianness = Endianness.LittleEndian)]
public partial struct PeSectionData
{
    /// <summary>
    ///     节区名称（原始 8 字节）。
    /// </summary>
    [Field(Order = 0, Length = 8)]
    public FixedBytes8 NameBytes;

    /// <summary>
    ///     虚拟大小。
    /// </summary>
    [Field(Order = 1)]
    public uint VirtualSize;

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
///     PE 文件数据。
/// </summary>
public sealed class PeFileData
{
    /// <summary>
    ///     PE 头数据。
    /// </summary>
    public PeHeaderData Header { get; init; } = new();

    /// <summary>
    ///     可选头数据。
    /// </summary>
    public PeOptionalHeaderData OptionalHeader { get; init; } = new();

    /// <summary>
    ///     节区列表。
    /// </summary>
    public IReadOnlyList<PeSectionData> Sections { get; init; } = [];

    /// <summary>
    ///     节区内容数据，键为节区在 Sections 列表中的索引，值为原始字节数据。
    /// </summary>
    public Dictionary<int, byte[]> SectionContents { get; init; } = [];

    /// <summary>
    ///     是否为 DLL。
    /// </summary>
    public bool IsDll => (Header.Characteristics & 0x2000) != 0;

    /// <summary>
    ///     是否为可执行文件。
    /// </summary>
    public bool IsExecutable => (Header.Characteristics & 0x0002) != 0;

    /// <summary>
    ///     是否为 64 位。
    /// </summary>
    public bool Is64Bit => OptionalHeader.Magic == 0x20B;

    /// <summary>
    ///     获取指定索引的数据目录条目。索引不存在时返回空条目。
    /// </summary>
    public PeDataDirectoryEntry GetDataDirectory(PeDataDirectoryIndex index)
    {
        var i = (int)index;

        if (i < OptionalHeader.DataDirectories.Count)
        {
            return OptionalHeader.DataDirectories[i];
        }

        return new PeDataDirectoryEntry();
    }

    /// <summary>
    ///     将 RVA（相对虚拟地址）转换为文件偏移量。
    /// </summary>
    public int RvaToOffset(uint rva)
    {
        foreach (var section in Sections)
        {
            var sectionEnd = section.VirtualAddress + section.VirtualSize;

            if (rva >= section.VirtualAddress && rva < sectionEnd)
            {
                return (int)(rva - section.VirtualAddress + section.PointerToRawData);
            }
        }

        return (int)rva;
    }
}
