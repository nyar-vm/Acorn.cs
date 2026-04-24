using Acorn.Frame;
using Acorn.Png.Data;

namespace Acorn.Png.Scanner;

/// <summary>
///     PNG 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 PNG 图像文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     PNG 是无损压缩的位图格式，使用 zlib/deflate 压缩，支持多种色彩类型和 Alpha 通道。
///     扫描器只读取 IHDR 块信息，不做完整的像素数据解码，以实现快速探查。
/// </remarks>
public ref struct PngScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="PngScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 PNG 字节数据。</param>
    public PngScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 PNG 文件头，提取基本图像信息。
    /// </summary>
    /// <returns>PNG 文件头信息。</returns>
    public PngScanHeader ScanHeader()
    {
        if (_scanner.Length < PngConstants.SignatureLength + PngConstants.ChunkHeaderSize + PngConstants.IhdrDataLength + PngConstants.ChunkCrcSize)
        {
            throw new InvalidDataException("PNG 文件数据过短，无法读取签名和 IHDR 块");
        }

        if (!_scanner.MatchMagic(PngConstants.Signature))
        {
            throw new InvalidDataException("PNG 文件签名不匹配");
        }

        _scanner.ConsumeMagic(PngConstants.Signature);

        var length = _scanner.Buffer.ReadU32BE();
        var type = _scanner.Buffer.ReadString(4);

        if (type != PngConstants.IhdrTag)
        {
            throw new InvalidDataException($"PNG 第一个块不是 IHDR，实际为 \"{type}\"");
        }

        var width = _scanner.Buffer.ReadU32BE();
        var height = _scanner.Buffer.ReadU32BE();
        var bitDepth = _scanner.Buffer.ReadU8();
        var colorType = (PngColorType)_scanner.Buffer.ReadU8();
        var compressionMethod = (PngCompressionMethod)_scanner.Buffer.ReadU8();
        var filterMethod = (PngFilterMethod)_scanner.Buffer.ReadU8();
        var interlaceMethod = (PngInterlaceMethod)_scanner.Buffer.ReadU8();

        return new PngScanHeader
        {
            Width = (int)width,
            Height = (int)height,
            BitDepth = bitDepth,
            ColorType = colorType,
            CompressionMethod = compressionMethod,
            FilterMethod = filterMethod,
            InterlaceMethod = interlaceMethod
        };
    }

    /// <summary>
    ///     快速判断数据是否为 PNG 格式。
    /// </summary>
    public bool IsPng()
    {
        if (_scanner.Length < PngConstants.SignatureLength)
        {
            return false;
        }

        return _scanner.MatchMagic(PngConstants.Signature);
    }
}

/// <summary>
///     PNG 扫描头部信息。
/// </summary>
public sealed class PngScanHeader
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
    ///     位深度。
    /// </summary>
    public byte BitDepth { get; init; }

    /// <summary>
    ///     色彩类型。
    /// </summary>
    public PngColorType ColorType { get; init; }

    /// <summary>
    ///     压缩方法。
    /// </summary>
    public PngCompressionMethod CompressionMethod { get; init; }

    /// <summary>
    ///     滤波方法。
    /// </summary>
    public PngFilterMethod FilterMethod { get; init; }

    /// <summary>
    ///     隔行扫描方法。
    /// </summary>
    public PngInterlaceMethod InterlaceMethod { get; init; }

    /// <summary>
    ///     色彩类型名称。
    /// </summary>
    public string ColorTypeName => ColorType switch
    {
        PngColorType.Grayscale => "灰度",
        PngColorType.Indexed => "索引色",
        PngColorType.Truecolor => "真彩色",
        PngColorType.GrayscaleAlpha => "灰度+Alpha",
        PngColorType.TruecolorAlpha => "真彩色+Alpha",
        _ => $"未知({(byte)ColorType})"
    };
}
