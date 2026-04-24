using Acorn.Frame;
using Acorn.Ttf.Data;

namespace Acorn.Ttf.Scanner;

/// <summary>
///     TTF 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 TrueType/OpenType 字体文件的快速元信息扫描。
/// </summary>
public ref struct TtfScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="TtfScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 TTF 字节数据。</param>
    public TtfScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 TTF 文件头，提取基本字体信息。
    /// </summary>
    public TtfScanHeader ScanHeader()
    {
        if (_scanner.Length < TtfConstants.OffsetTableSize)
        {
            throw new InvalidDataException("TTF 文件数据过短，无法读取偏移表");
        }

        var sfVersion = _scanner.Buffer.ReadU32BE();
        var fontType = sfVersion switch
        {
            TtfConstants.TrueTypeMagic => TtfFontType.TrueType,
            0x4F54544F => TtfFontType.Cff,
            0x74746366 => TtfFontType.Collection,
            _ => TtfFontType.Unknown
        };

        var tableCount = _scanner.Buffer.ReadU16BE();

        return new TtfScanHeader
        {
            FontType = fontType,
            TableCount = tableCount
        };
    }

    /// <summary>
    ///     快速判断数据是否为 TTF/OTF 格式。
    /// </summary>
    public bool IsFont()
    {
        if (_scanner.Length < 4)
        {
            return false;
        }

        var sfVersion = _scanner.Buffer.ReadU32BE();
        _scanner.Position = 0;

        return sfVersion is TtfConstants.TrueTypeMagic or 0x4F54544F or 0x74746366;
    }
}

/// <summary>
///     TTF 扫描头部信息。
/// </summary>
public sealed class TtfScanHeader
{
    /// <summary>
    ///     字体类型。
    /// </summary>
    public TtfFontType FontType { get; init; }

    /// <summary>
    ///     表数量。
    /// </summary>
    public ushort TableCount { get; init; }

    /// <summary>
    ///     字体类型名称。
    /// </summary>
    public string FontTypeName => FontType switch
    {
        TtfFontType.TrueType => "TrueType",
        TtfFontType.Cff => "OpenType/CFF",
        TtfFontType.Collection => "TTC",
        _ => "未知"
    };
}
