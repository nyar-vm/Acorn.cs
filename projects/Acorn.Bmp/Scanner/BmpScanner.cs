using Acorn.Frame;
using Acorn.Bmp.Data;

namespace Acorn.Bmp.Scanner;

/// <summary>
///     BMP 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 BMP 图像文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     BMP 是 Microsoft 的标准位图格式，由文件头、信息头、可选调色板和像素数据组成。
///     扫描器只读取文件头和信息头，不做完整的像素数据解码，以实现快速探查。
/// </remarks>
public ref struct BmpScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="BmpScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 BMP 字节数据。</param>
    public BmpScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 BMP 文件头，提取基本图像信息。
    /// </summary>
    /// <returns>BMP 文件头信息。</returns>
    public BmpScanHeader ScanHeader()
    {
        if (_scanner.Length < BmpConstants.FileHeaderSize + BmpConstants.InfoHeaderSize)
        {
            throw new InvalidDataException("BMP 文件数据过短，无法读取文件头");
        }

        if (!_scanner.MatchMagic(BmpConstants.MagicNumber))
        {
            throw new InvalidDataException("BMP 文件魔数不匹配");
        }

        _scanner.ConsumeMagic(BmpConstants.MagicNumber);

        var fileSize = _scanner.Buffer.ReadU32LE();
        _scanner.Advance(4);
        _scanner.Advance(2);

        var dataOffset = _scanner.Buffer.ReadU32LE();

        var headerSize = _scanner.Buffer.ReadU32LE();

        if (headerSize < BmpConstants.InfoHeaderSize)
        {
            throw new InvalidDataException($"BMP 信息头大小无效，期望 >= {BmpConstants.InfoHeaderSize}，实际 {headerSize}");
        }

        var width = _scanner.Buffer.ReadI32LE();
        var height = _scanner.Buffer.ReadI32LE();
        var planes = _scanner.Buffer.ReadU16LE();
        var bitsPerPixel = _scanner.Buffer.ReadU16LE();
        var compression = _scanner.Buffer.ReadU32LE();
        var imageSize = _scanner.Buffer.ReadU32LE();
        var xPelsPerMeter = _scanner.Buffer.ReadI32LE();
        var yPelsPerMeter = _scanner.Buffer.ReadI32LE();
        var colorsUsed = _scanner.Buffer.ReadU32LE();
        var colorsImportant = _scanner.Buffer.ReadU32LE();

        return new BmpScanHeader
        {
            Width = width,
            Height = height,
            BitsPerPixel = bitsPerPixel,
            Compression = (BmpCompression)compression,
            ImageSize = imageSize,
            DataOffset = (int)dataOffset,
            XPelsPerMeter = xPelsPerMeter,
            YPelsPerMeter = yPelsPerMeter,
            ColorsUsed = colorsUsed,
            ColorsImportant = colorsImportant
        };
    }

    /// <summary>
    ///     快速判断数据是否为 BMP 格式。
    /// </summary>
    public bool IsBmp()
    {
        if (_scanner.Length < 2)
        {
            return false;
        }

        return _scanner.MatchMagic(BmpConstants.MagicNumber);
    }
}

/// <summary>
///     BMP 扫描头部信息。
/// </summary>
public sealed class BmpScanHeader
{
    /// <summary>
    ///     图像宽度（像素）。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     图像高度（像素）。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     每像素位数。
    /// </summary>
    public ushort BitsPerPixel { get; init; }

    /// <summary>
    ///     压缩方式。
    /// </summary>
    public BmpCompression Compression { get; init; }

    /// <summary>
    ///     图像数据大小。
    /// </summary>
    public uint ImageSize { get; init; }

    /// <summary>
    ///     像素数据偏移量。
    /// </summary>
    public int DataOffset { get; init; }

    /// <summary>
    ///     水平分辨率。
    /// </summary>
    public int XPelsPerMeter { get; init; }

    /// <summary>
    ///     垂直分辨率。
    /// </summary>
    public int YPelsPerMeter { get; init; }

    /// <summary>
    ///     使用的颜色数。
    /// </summary>
    public uint ColorsUsed { get; init; }

    /// <summary>
    ///     重要的颜色数。
    /// </summary>
    public uint ColorsImportant { get; init; }

    /// <summary>
    ///     像素格式名称。
    /// </summary>
    public string PixelFormatName => BitsPerPixel switch
    {
        1 => "1 位黑白",
        4 => "4 位索引",
        8 => "8 位索引",
        16 => "16 位高彩",
        24 => "24 位真彩",
        32 => "32 位真彩",
        _ => $"{BitsPerPixel} 位"
    };
}
