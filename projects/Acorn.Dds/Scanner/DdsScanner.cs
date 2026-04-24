using Acorn.Frame;
using Acorn.Dds.Data;

namespace Acorn.Dds.Scanner;

/// <summary>
///     DDS 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 DirectDraw Surface 纹理文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     DDS 文件格式由魔数、文件头、可选 DX10 扩展头和纹理数据组成。
///     扫描器只读取文件头信息，不做完整的像素数据解码，以实现快速探查。
/// </remarks>
public ref struct DdsScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="DdsScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 DDS 字节数据。</param>
    public DdsScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 DDS 文件头，提取基本纹理信息。
    /// </summary>
    /// <returns>DDS 文件头信息。</returns>
    public DdsScanHeader ScanHeader()
    {
        if (_scanner.Length < DdsConstants.FullHeaderSize)
        {
            throw new InvalidDataException("DDS 文件数据过短，无法读取文件头");
        }

        if (!_scanner.MatchMagic(DdsConstants.MagicNumber))
        {
            throw new InvalidDataException("DDS 文件魔数不匹配");
        }

        _scanner.ConsumeMagic(DdsConstants.MagicNumber);

        var headerSize = _scanner.Buffer.ReadU32LE();

        if (headerSize != DdsConstants.HeaderSize)
        {
            throw new InvalidDataException($"DDS 头部大小无效，期望 {DdsConstants.HeaderSize}，实际 {headerSize}");
        }

        var flags = _scanner.Buffer.ReadU32LE();
        var height = _scanner.Buffer.ReadU32LE();
        var width = _scanner.Buffer.ReadU32LE();
        var pitchOrLinearSize = _scanner.Buffer.ReadU32LE();
        var depth = _scanner.Buffer.ReadU32LE();
        var mipMapCount = _scanner.Buffer.ReadU32LE();

        _scanner.Advance(44);

        var pfSize = _scanner.Buffer.ReadU32LE();
        var pfFlags = _scanner.Buffer.ReadU32LE();
        var fourCC = _scanner.Buffer.ReadU32LE();
        var rgbBitCount = _scanner.Buffer.ReadU32LE();

        return new DdsScanHeader
        {
            Width = (int)width,
            Height = (int)height,
            Depth = (int)depth,
            MipMapCount = (int)mipMapCount,
            FourCC = fourCC,
            RGBBitCount = rgbBitCount,
            IsCompressed = (pfFlags & (uint)DdsPixelFormatFlags.FourCC) != 0
        };
    }

    /// <summary>
    ///     快速判断数据是否为 DDS 格式。
    /// </summary>
    /// <returns>是否为 DDS 格式。</returns>
    public bool IsDds()
    {
        if (_scanner.Length < 4)
        {
            return false;
        }

        return _scanner.MatchMagic(DdsConstants.MagicNumber);
    }
}

/// <summary>
///     DDS 扫描头部信息。
/// </summary>
public sealed class DdsScanHeader
{
    /// <summary>
    ///     纹理宽度。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     纹理高度。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     纹理深度。
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    ///     Mipmap 级别数量。
    /// </summary>
    public int MipMapCount { get; init; }

    /// <summary>
    ///     FourCC 压缩格式代码。
    /// </summary>
    public uint FourCC { get; init; }

    /// <summary>
    ///     每像素位数。
    /// </summary>
    public uint RGBBitCount { get; init; }

    /// <summary>
    ///     是否为压缩格式。
    /// </summary>
    public bool IsCompressed { get; init; }

    /// <summary>
    ///     压缩格式名称。
    /// </summary>
    public string CompressionFormat => FourCC switch
    {
        DdsFourCC.DXT1 => "BC1/DXT1",
        DdsFourCC.DXT3 => "BC2/DXT3",
        DdsFourCC.DXT5 => "BC3/DXT5",
        DdsFourCC.ATI1 => "BC4/ATI1",
        DdsFourCC.ATI2 => "BC5/ATI2",
        DdsFourCC.BC6H => "BC6H",
        DdsFourCC.BC7 => "BC7",
        0 => IsCompressed ? "未知压缩" : "未压缩",
        _ => $"0x{FourCC:X8}"
    };
}
