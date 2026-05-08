namespace Acorn.Tga.Data;

/// <summary>
///     TGA 图像格式常量。
/// </summary>
public static class TgaConstants
{
    /// <summary>
    ///     TGA 文件尾签名字符串（18 字节，含空终止符）。
    /// </summary>
    public const string FooterSignature = "TRUEVISION-XFILE.";

    /// <summary>
    ///     TGA 文件尾签名长度。
    /// </summary>
    public const int FooterSignatureLength = 18;

    /// <summary>
    ///     TGA 文件头大小（固定 18 字节）。
    /// </summary>
    public const int HeaderSize = 18;
}

/// <summary>
///     TGA 图像类型。
/// </summary>
public enum TgaImageType : byte
{
    /// <summary>
    ///     无图像数据。
    /// </summary>
    NoData = 0,

    /// <summary>
    ///     未压缩的调色板图像。
    /// </summary>
    UncompressedColorMap = 1,

    /// <summary>
    ///     未压缩的真彩色图像。
    /// </summary>
    UncompressedTruecolor = 2,

    /// <summary>
    ///     未压缩的灰度图像。
    /// </summary>
    UncompressedGrayscale = 3,

    /// <summary>
    ///     RLE 压缩的调色板图像。
    /// </summary>
    RleColorMap = 9,

    /// <summary>
    ///     RLE 压缩的真彩色图像。
    /// </summary>
    RleTruecolor = 10,

    /// <summary>
    ///     RLE 压缩的灰度图像。
    /// </summary>
    RleGrayscale = 11
}

/// <summary>
///     TGA 像素位深度。
/// </summary>
public enum TgaPixelDepth : byte
{
    /// <summary>
    ///     8 位像素深度。
    /// </summary>
    Bit8 = 8,

    /// <summary>
    ///     16 位像素深度。
    /// </summary>
    Bit16 = 16,

    /// <summary>
    ///     24 位像素深度。
    /// </summary>
    Bit24 = 24,

    /// <summary>
    ///     32 位像素深度。
    /// </summary>
    Bit32 = 32
}
