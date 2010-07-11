namespace Acorn.Psd.Data;

/// <summary>
///     Adobe Photoshop PSD 二进制格式常量。
/// </summary>
/// <remarks>
///     所有常量值均来自 Adobe PSD 文件格式规范，Acorn 独占二进制编解码职责。
/// </remarks>
public static class PsdConstants
{
    /// <summary>
    ///     PSD 文件魔数（"8BPS"）。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => new byte[] { 0x38, 0x42, 0x50, 0x53 };

    /// <summary>
    ///     PSD 文件版本号。
    /// </summary>
    public const ushort Version = 1;

    /// <summary>
    ///     PSD 图层混合模式签名（"8BPS"）。
    /// </summary>
    public static ReadOnlySpan<byte> BlendModeSignature => new byte[] { 0x38, 0x42, 0x50, 0x53 };

    /// <summary>
    ///     PSD 扩展长度标记（4GB 以上）。
    /// </summary>
    public const uint ExtendedLengthMarker = 0xFFFFFFFF;
}

/// <summary>
///     PSD 颜色模式枚举。
/// </summary>
public enum PsdColorMode : ushort
{
    /// <summary>
    ///     位图。
    /// </summary>
    Bitmap = 0,

    /// <summary>
    ///     灰度。
    /// </summary>
    Grayscale = 1,

    /// <summary>
    ///     索引色。
    /// </summary>
    Indexed = 2,

    /// <summary>
    ///     RGB。
    /// </summary>
    Rgb = 3,

    /// <summary>
    ///     CMYK。
    /// </summary>
    Cmyk = 4,

    /// <summary>
    ///     多通道。
    /// </summary>
    Multichannel = 7,

    /// <summary>
    ///     双色调。
    /// </summary>
    Duotone = 8,

    /// <summary>
    ///     Lab。
    /// </summary>
    Lab = 9
}

/// <summary>
///     PSD 压缩方式枚举。
/// </summary>
public enum PsdCompression : short
{
    /// <summary>
    ///     无压缩。
    /// </summary>
    Raw = 0,

    /// <summary>
    ///     RLE 压缩。
    /// </summary>
    Rle = 1
}
