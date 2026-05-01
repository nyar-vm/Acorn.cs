using Acorn.Clr.Data;
using Acorn.Frame;
using Acorn.Pe.Data;
using Acorn.Pe.Scanner;

namespace Acorn.Clr.Scanner;

/// <summary>
///     CLR 模块扫描器，基于 <see cref="SpanScanner" /> 提供对 .NET 程序集的快速元信息扫描。
/// </summary>
/// <remarks>
///     .NET 程序集本质上是包含 CLR 元数据目录的 PE 文件。
///     扫描器通过检查 PE 头中的 CLR 目录标志快速判断是否为 .NET 程序集，
///     并提取基本的 CLR 元信息（版本号、模块名称等）。
/// </remarks>
public ref struct ClrScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="ClrScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 .NET 程序集字节数据。</param>
    public ClrScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 .NET 程序集头部，提取基本 CLR 信息。
    /// </summary>
    public ClrScanHeader ScanHeader()
    {
        var header = new ClrScanHeader();

        if (_scanner.Length < 64)
        {
            throw new InvalidDataException("数据过短，不是有效的 PE 文件");
        }

        if (!_scanner.MatchMagic(PeConstants.DosMagicBytes))
        {
            throw new InvalidDataException("MZ 魔数不匹配，不是有效的 PE 文件");
        }

        var peOffset = _scanner.Buffer.Position;
        _scanner.Buffer.Position = PeConstants.PeOffsetPosition;

        if (_scanner.Buffer.Remaining < 4)
        {
            throw new InvalidDataException("PE 偏移量数据不足");
        }

        var peHeaderOffset = _scanner.Buffer.ReadI32LE();

        if (peHeaderOffset <= 0 || peHeaderOffset + 24 > _scanner.Length)
        {
            throw new InvalidDataException("PE 头偏移量无效");
        }

        _scanner.Buffer.Position = peHeaderOffset;

        if (_scanner.Buffer.Remaining < 24)
        {
            throw new InvalidDataException("PE 头数据不足");
        }

        var peMagic = _scanner.Buffer.ReadU32LE();

        if (peMagic != PeConstants.PeMagic)
        {
            throw new InvalidDataException("PE 签名不匹配");
        }

        _scanner.Buffer.Position = peHeaderOffset + 4;
        var machine = _scanner.Buffer.ReadU16LE();
        var numberOfSections = _scanner.Buffer.ReadU16LE();
        _scanner.Buffer.Position += 8;
        var sizeOfOptionalHeader = _scanner.Buffer.ReadU16LE();
        var characteristics = _scanner.Buffer.ReadU16LE();

        header.IsPE32Plus = false;
        header.HasClrDirectory = false;

        var optionalHeaderOffset = peHeaderOffset + 24;

        if (optionalHeaderOffset + 2 <= _scanner.Length)
        {
            _scanner.Buffer.Position = optionalHeaderOffset;
            var optionalMagic = _scanner.Buffer.ReadU16LE();
            header.IsPE32Plus = optionalMagic == PeConstants.OptionalMagicPE32Plus;
        }

        int clrDirectoryDataIndex = header.IsPE32Plus ? 14 : 14;

        if (header.IsPE32Plus)
        {
            var dataDirOffset = optionalHeaderOffset + 112 + clrDirectoryDataIndex * 8;

            if (dataDirOffset + 8 <= _scanner.Length)
            {
                _scanner.Buffer.Position = dataDirOffset;
                var clrRva = _scanner.Buffer.ReadU32LE();
                var clrSize = _scanner.Buffer.ReadU32LE();
                header.HasClrDirectory = clrRva != 0 && clrSize != 0;
                header.ClrMetadataRva = clrRva;
                header.ClrMetadataSize = clrSize;
            }
        }
        else
        {
            var dataDirOffset = optionalHeaderOffset + 96 + clrDirectoryDataIndex * 8;

            if (dataDirOffset + 8 <= _scanner.Length)
            {
                _scanner.Buffer.Position = dataDirOffset;
                var clrRva = _scanner.Buffer.ReadU32LE();
                var clrSize = _scanner.Buffer.ReadU32LE();
                header.HasClrDirectory = clrRva != 0 && clrSize != 0;
                header.ClrMetadataRva = clrRva;
                header.ClrMetadataSize = clrSize;
            }
        }

        header.Machine = machine;
        header.NumberOfSections = numberOfSections;
        header.IsExecutable = (characteristics & PeConstants.CharacteristicsExecutable) != 0;
        header.IsDll = (characteristics & PeConstants.CharacteristicsDll) != 0;

        return header;
    }

    /// <summary>
    ///     快速判断数据是否为 .NET 程序集。
    /// </summary>
    public bool IsClrAssembly()
    {
        if (_scanner.Length < 64)
        {
            return false;
        }

        if (!_scanner.MatchMagic(PeConstants.DosMagicBytes))
        {
            return false;
        }

        try
        {
            var header = ScanHeader();
            return header.HasClrDirectory;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
///     CLR 扫描头部信息。
/// </summary>
public sealed class ClrScanHeader
{
    /// <summary>
    ///     目标机器类型。
    /// </summary>
    public ushort Machine { get; set; }

    /// <summary>
    ///     节区数量。
    /// </summary>
    public ushort NumberOfSections { get; set; }

    /// <summary>
    ///     是否为 PE32+ 格式（64 位）。
    /// </summary>
    public bool IsPE32Plus { get; set; }

    /// <summary>
    ///     是否包含 CLR 目录。
    /// </summary>
    public bool HasClrDirectory { get; set; }

    /// <summary>
    ///     是否为可执行文件。
    /// </summary>
    public bool IsExecutable { get; set; }

    /// <summary>
    ///     是否为 DLL。
    /// </summary>
    public bool IsDll { get; set; }

    /// <summary>
    ///     CLR 元数据 RVA。
    /// </summary>
    public uint ClrMetadataRva { get; set; }

    /// <summary>
    ///     CLR 元数据大小。
    /// </summary>
    public uint ClrMetadataSize { get; set; }
}
