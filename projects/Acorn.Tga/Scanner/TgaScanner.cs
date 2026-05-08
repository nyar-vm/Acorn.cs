using Acorn.Frame;
using Acorn.Tga.Data;

namespace Acorn.Tga.Scanner;

/// <summary>
///     TGA 文件扫描器，提供对 TGA 图像文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     TGA 是一种位图图像格式，广泛用于游戏开发中的纹理资源。
///     扫描器只读取文件头信息，不做完整的像素数据解码，以实现快速探查。
/// </remarks>
public ref struct TgaScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="TgaScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 TGA 字节数据。</param>
    public TgaScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 TGA 文件头，提取基本图像信息。
    /// </summary>
    /// <returns>TGA 文件头信息。</returns>
    public TgaScanHeader ScanHeader()
    {
        if (_scanner.Length < TgaConstants.HeaderSize)
        {
            throw new InvalidDataException("TGA 文件数据过短，无法读取文件头");
        }

        var idLength = _scanner.Buffer.ReadU8();
        var colorMapType = _scanner.Buffer.ReadU8();
        var imageType = _scanner.Buffer.ReadU8();

        _scanner.Buffer.ReadU16LE();
        var colorMapLength = _scanner.Buffer.ReadU16LE();
        var colorMapEntrySize = _scanner.Buffer.ReadU8();

        _scanner.Buffer.ReadU16LE();
        _scanner.Buffer.ReadU16LE();

        var width = _scanner.Buffer.ReadU16LE();
        var height = _scanner.Buffer.ReadU16LE();
        var pixelDepth = _scanner.Buffer.ReadU8();
        var imageDescriptor = _scanner.Buffer.ReadU8();

        var isTopDown = (imageDescriptor & 0x20) != 0;
        var hasColorMap = colorMapType == 1;

        return new TgaScanHeader
        {
            IdLength = idLength,
            HasColorMap = hasColorMap,
            ImageType = (TgaImageType)imageType,
            ColorMapLength = colorMapLength,
            ColorMapEntrySize = colorMapEntrySize,
            Width = width,
            Height = height,
            PixelDepth = pixelDepth,
            IsTopDown = isTopDown,
            HasFooter = CheckFooter()
        };
    }

    /// <summary>
    ///     快速判断数据是否为 TGA 格式。
    /// </summary>
    public bool IsTga()
    {
        if (_scanner.Length < TgaConstants.HeaderSize)
        {
            return false;
        }

        var colorMapType = _scanner.Data[1];
        var imageType = _scanner.Data[2];

        if (colorMapType > 1)
        {
            return false;
        }

        if (!IsValidImageType(imageType))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     检查 TGA 文件是否包含文件尾。
    /// </summary>
    public bool CheckFooter()
    {
        if (_scanner.Length < TgaConstants.FooterSignatureLength)
        {
            return false;
        }

        var footerStart = _scanner.Length - TgaConstants.FooterSignatureLength;
        var signature = TgaConstants.FooterSignature;
        var data = _scanner.Data;

        for (var i = 0; i < signature.Length; i++)
        {
            if (data[footerStart + i] != signature[i])
            {
                return false;
            }
        }

        return true;
    }

    #region 私有方法

    private static bool IsValidImageType(byte imageType)
    {
        return imageType is 0 or 1 or 2 or 3 or 9 or 10 or 11;
    }

    #endregion
}

/// <summary>
///     TGA 扫描头部信息。
/// </summary>
public sealed class TgaScanHeader
{
    /// <summary>
    ///     图像 ID 长度。
    /// </summary>
    public int IdLength { get; init; }

    /// <summary>
    ///     是否包含调色板。
    /// </summary>
    public bool HasColorMap { get; init; }

    /// <summary>
    ///     图像类型。
    /// </summary>
    public TgaImageType ImageType { get; init; }

    /// <summary>
    ///     调色板条目数。
    /// </summary>
    public ushort ColorMapLength { get; init; }

    /// <summary>
    ///     调色板条目位数。
    /// </summary>
    public byte ColorMapEntrySize { get; init; }

    /// <summary>
    ///     图像宽度（像素）。
    /// </summary>
    public ushort Width { get; init; }

    /// <summary>
    ///     图像高度（像素）。
    /// </summary>
    public ushort Height { get; init; }

    /// <summary>
    ///     每像素位数。
    /// </summary>
    public byte PixelDepth { get; init; }

    /// <summary>
    ///     是否为从上到下的行序。
    /// </summary>
    public bool IsTopDown { get; init; }

    /// <summary>
    ///     是否包含文件尾签名。
    /// </summary>
    public bool HasFooter { get; init; }

    /// <summary>
    ///     图像类型名称。
    /// </summary>
    public string ImageTypeName => ImageType switch
    {
        TgaImageType.NoData => "无数据",
        TgaImageType.UncompressedColorMap => "未压缩调色板",
        TgaImageType.UncompressedTruecolor => "未压缩真彩色",
        TgaImageType.UncompressedGrayscale => "未压缩灰度",
        TgaImageType.RleColorMap => "RLE 调色板",
        TgaImageType.RleTruecolor => "RLE 真彩色",
        TgaImageType.RleGrayscale => "RLE 灰度",
        _ => $"未知({(byte)ImageType})"
    };
}
