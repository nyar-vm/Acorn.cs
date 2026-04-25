using Acorn.Frame;
using Acorn.Nyar.Data;

namespace Acorn.Nyar.Scanner;

/// <summary>
///     Nyar 字节码模块扫描器，基于 <see cref="SpanScanner" /> 提供对 .nyar 字节码模块的快速元信息扫描。
/// </summary>
/// <remarks>
///     .nyar 是 NyarVM 的字节码模块格式，采用分段式二进制布局。
///     扫描器只读取头部元信息和段表概要，不做完整的数据解码，以实现快速探查。
///     二进制布局：[Header 16B] → [Section Headers N*9B] → [Name Section] → [Section Data...]
/// </remarks>
public ref struct NyarScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="NyarScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 .nyar 字节数据。</param>
    public NyarScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 .nyar 模块头部，提取基本模块信息。
    /// </summary>
    /// <returns>Nyar 模块头部信息。</returns>
    public NyarScanHeader ScanHeader()
    {
        if (_scanner.Length < NyarConstants.HeaderSize)
        {
            throw new InvalidDataException($".nyar 文件数据过短，期望至少 {NyarConstants.HeaderSize} 字节");
        }

        if (!_scanner.MatchMagic(NyarConstants.MagicNumber))
        {
            throw new InvalidDataException($".nyar 文件魔数不匹配，期望 NYAR(0x{NyarConstants.MagicValue:X8})");
        }

        _scanner.ConsumeMagic(NyarConstants.MagicNumber);

        var version = _scanner.Buffer.ReadU32LE();
        var sectionCount = _scanner.Buffer.ReadI32LE();
        var nameOffset = _scanner.Buffer.ReadI32LE();

        var header = new NyarScanHeader
        {
            Version = version,
            SectionCount = sectionCount
        };

        header.Sections = ScanSectionHeaders(sectionCount);

        header.ModuleName = ScanModuleName(nameOffset);

        foreach (var section in header.Sections)
        {
            switch (section.Kind)
            {
                case NyarSectionKind.Constants:
                    header.HasConstantsSection = true;
                    break;
                case NyarSectionKind.Functions:
                    header.HasFunctionsSection = true;
                    break;
                case NyarSectionKind.Imports:
                    header.HasImportsSection = true;
                    break;
                case NyarSectionKind.Exports:
                    header.HasExportsSection = true;
                    break;
                case NyarSectionKind.Code:
                    header.HasCodeSection = true;
                    break;
            }
        }

        return header;
    }

    /// <summary>
    ///     快速判断数据是否为 .nyar 格式。
    /// </summary>
    public bool IsNyar()
    {
        if (_scanner.Length < 4)
        {
            return false;
        }

        return _scanner.MatchMagic(NyarConstants.MagicNumber);
    }

    #region 私有扫描方法

    private List<NyarSectionScanInfo> ScanSectionHeaders(int count)
    {
        var sections = new List<NyarSectionScanInfo>(count);

        for (var i = 0; i < count && !_scanner.Buffer.IsEnd; i++)
        {
            if (_scanner.Buffer.Remaining < NyarConstants.SectionHeaderSize)
            {
                break;
            }

            var kind = (NyarSectionKind)_scanner.Buffer.ReadU8();
            var offset = _scanner.Buffer.ReadI32LE();
            var size = _scanner.Buffer.ReadI32LE();

            sections.Add(new NyarSectionScanInfo
            {
                Kind = kind,
                Offset = offset,
                Size = size
            });
        }

        return sections;
    }

    private string ScanModuleName(int nameOffset)
    {
        if (nameOffset <= 0 || nameOffset >= _scanner.Length)
        {
            return string.Empty;
        }

        var savedPosition = _scanner.Buffer.Position;
        _scanner.Buffer.Position = nameOffset;

        if (_scanner.Buffer.Remaining < 4)
        {
            _scanner.Buffer.Position = savedPosition;
            return string.Empty;
        }

        var nameLength = _scanner.Buffer.ReadI32LE();

        if (nameLength <= 0 || _scanner.Buffer.Remaining < nameLength)
        {
            _scanner.Buffer.Position = savedPosition;
            return string.Empty;
        }

        var name = _scanner.Buffer.ReadString(nameLength);
        _scanner.Buffer.Position = savedPosition;
        return name;
    }

    #endregion
}

/// <summary>
///     Nyar 扫描头部信息。
/// </summary>
public sealed class NyarScanHeader
{
    /// <summary>
    ///     版本号。
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    ///     模块名称。
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    ///     段数量。
    /// </summary>
    public int SectionCount { get; set; }

    /// <summary>
    ///     段概要信息列表。
    /// </summary>
    public IReadOnlyList<NyarSectionScanInfo> Sections { get; set; } = [];

    /// <summary>
    ///     是否包含常量池段。
    /// </summary>
    public bool HasConstantsSection { get; set; }

    /// <summary>
    ///     是否包含函数表段。
    /// </summary>
    public bool HasFunctionsSection { get; set; }

    /// <summary>
    ///     是否包含导入表段。
    /// </summary>
    public bool HasImportsSection { get; set; }

    /// <summary>
    ///     是否包含导出表段。
    /// </summary>
    public bool HasExportsSection { get; set; }

    /// <summary>
    ///     是否包含代码段。
    /// </summary>
    public bool HasCodeSection { get; set; }
}

/// <summary>
///     Nyar 段扫描概要信息。
/// </summary>
public sealed class NyarSectionScanInfo
{
    /// <summary>
    ///     段类型。
    /// </summary>
    public NyarSectionKind Kind { get; set; }

    /// <summary>
    ///     段数据偏移量。
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    ///     段数据大小。
    /// </summary>
    public int Size { get; set; }
}
