namespace Acorn.Png.Data;

/// <summary>
///     PNG 图像格式常量。
/// </summary>
public static class PngConstants
{
    /// <summary>
    ///     PNG 文件签名（8 字节）。
    /// </summary>
    public static ReadOnlySpan<byte> Signature => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    ///     PNG 签名长度。
    /// </summary>
    public const int SignatureLength = 8;

    /// <summary>
    ///     PNG 块头大小（长度 4 + 类型 4）。
    /// </summary>
    public const int ChunkHeaderSize = 8;

    /// <summary>
    ///     PNG 块尾大小（CRC 4）。
    /// </summary>
    public const int ChunkCrcSize = 4;

    /// <summary>
    ///     IHDR 块类型。
    /// </summary>
    public static ReadOnlySpan<byte> IhdrType => new byte[] { 0x49, 0x48, 0x44, 0x52 };

    /// <summary>
    ///     PLTE 块类型。
    /// </summary>
    public static ReadOnlySpan<byte> PlteType => new byte[] { 0x50, 0x4C, 0x54, 0x45 };

    /// <summary>
    ///     IDAT 块类型。
    /// </summary>
    public static ReadOnlySpan<byte> IdatType => new byte[] { 0x49, 0x44, 0x41, 0x54 };

    /// <summary>
    ///     IEND 块类型。
    /// </summary>
    public static ReadOnlySpan<byte> IendType => new byte[] { 0x49, 0x45, 0x4E, 0x44 };

    /// <summary>
    ///     IHDR 数据长度（固定 13 字节）。
    /// </summary>
    public const int IhdrDataLength = 13;

    /// <summary>
    ///     IHDR 块类型字符串。
    /// </summary>
    public const string IhdrTag = "IHDR";

    /// <summary>
    ///     PLTE 块类型字符串。
    /// </summary>
    public const string PlteTag = "PLTE";

    /// <summary>
    ///     IDAT 块类型字符串。
    /// </summary>
    public const string IdatTag = "IDAT";

    /// <summary>
    ///     IEND 块类型字符串。
    /// </summary>
    public const string IendTag = "IEND";

    /// <summary>
    ///     tRNS 块类型字符串。
    /// </summary>
    public const string TrnsTag = "tRNS";

    /// <summary>
    ///     gAMA 块类型字符串。
    /// </summary>
    public const string GamaTag = "gAMA";

    /// <summary>
    ///     cHRM 块类型字符串。
    /// </summary>
    public const string ChrmTag = "cHRM";

    /// <summary>
    ///     sRGB 块类型字符串。
    /// </summary>
    public const string SrgbTag = "sRGB";

    /// <summary>
    ///     iCCP 块类型字符串。
    /// </summary>
    public const string IccpTag = "iCCP";

    /// <summary>
    ///     tEXt 块类型字符串。
    /// </summary>
    public const string TextTag = "tEXt";

    /// <summary>
    ///     zTXt 块类型字符串。
    /// </summary>
    public const string ZtxtTag = "zTXt";

    /// <summary>
    ///     iTXt 块类型字符串。
    /// </summary>
    public const string ItxtTag = "iTXt";

    /// <summary>
    ///     bKGD 块类型字符串。
    /// </summary>
    public const string BkgdTag = "bKGD";

    /// <summary>
    ///     pHYs 块类型字符串。
    /// </summary>
    public const string PhysTag = "pHYs";

    /// <summary>
    ///     tIME 块类型字符串。
    /// </summary>
    public const string TimeTag = "tIME";
}

/// <summary>
///     PNG 色彩类型。
/// </summary>
public enum PngColorType : byte
{
    /// <summary>
    ///     灰度。
    /// </summary>
    Grayscale = 0,

    /// <summary>
    ///     索引色（调色板）。
    /// </summary>
    Indexed = 3,

    /// <summary>
    ///     真彩色（RGB）。
    /// </summary>
    Truecolor = 2,

    /// <summary>
    ///     灰度 + Alpha。
    /// </summary>
    GrayscaleAlpha = 4,

    /// <summary>
    ///     真彩色 + Alpha（RGBA）。
    /// </summary>
    TruecolorAlpha = 6
}

/// <summary>
///     PNG 压缩方法。
/// </summary>
public enum PngCompressionMethod : byte
{
    /// <summary>
    ///     Deflate/Inflate 压缩。
    /// </summary>
    Deflate = 0
}

/// <summary>
///     PNG 滤波方法。
/// </summary>
public enum PngFilterMethod : byte
{
    /// <summary>
    ///     自适应滤波。
    /// </summary>
    Adaptive = 0
}

/// <summary>
///     PNG 隔行扫描方法。
/// </summary>
public enum PngInterlaceMethod : byte
{
    /// <summary>
    ///     无隔行。
    /// </summary>
    None = 0,

    /// <summary>
    ///     Adam7 隔行。
    /// </summary>
    Adam7 = 1
}

/// <summary>
///     PNG 滤波器类型（每行第一个字节）。
/// </summary>
public enum PngFilterType : byte
{
    /// <summary>
    ///     无滤波。
    /// </summary>
    None = 0,

    /// <summary>
    ///     Sub 滤波。
    /// </summary>
    Sub = 1,

    /// <summary>
    ///     Up 滤波。
    /// </summary>
    Up = 2,

    /// <summary>
    ///     Average 滤波。
    /// </summary>
    Average = 3,

    /// <summary>
    ///     Paeth 滤波。
    /// </summary>
    Paeth = 4
}
