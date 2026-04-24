namespace Acorn.Bmp.Data;

/// <summary>
///     BMP 图像格式常量。
/// </summary>
public static class BmpConstants
{
    /// <summary>
    ///     BMP 文件魔数（"BM"）。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => new byte[] { 0x42, 0x4D };

    /// <summary>
    ///     BMP 文件头大小（14 字节）。
    /// </summary>
    public const int FileHeaderSize = 14;

    /// <summary>
    ///     BITMAPINFOHEADER 大小（40 字节）。
    /// </summary>
    public const int InfoHeaderSize = 40;

    /// <summary>
    ///     BMP 魔数字符串。
    /// </summary>
    public const string MagicTag = "BM";
}

/// <summary>
///     BMP 压缩方式。
/// </summary>
public enum BmpCompression : uint
{
    /// <summary>
    ///     无压缩。
    /// </summary>
    None = 0,

    /// <summary>
    ///     RLE 8 位压缩。
    /// </summary>
    Rle8 = 1,

    /// <summary>
    ///     RLE 4 位压缩。
    /// </summary>
    Rle4 = 2,

    /// <summary>
    ///     位域掩码。
    /// </summary>
    BitFields = 3,

    /// <summary>
    ///     JPEG 压缩。
    /// </summary>
    Jpeg = 4,

    /// <summary>
    ///     PNG 压缩。
    /// </summary>
    Png = 5
}
