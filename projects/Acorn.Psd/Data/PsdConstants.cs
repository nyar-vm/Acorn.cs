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

    /// <summary>
    ///     PSD 文件头大小（26 字节：4 签名 + 2 版本 + 6 保留 + 2 通道 + 4 高度 + 4 宽度 + 2 深度 + 2 颜色模式）。
    /// </summary>
    public const int HeaderSize = 26;
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

/// <summary>
///     PSD 图层混合模式枚举。
/// </summary>
/// <remarks>
///     对应 PSD 规范中图层记录的混合模式 4 字节 ASCII 标识。
/// </remarks>
public enum PsdBlendMode
{
    /// <summary>
    ///     未知混合模式。
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     正常（norm）。
    /// </summary>
    Normal = 1,

    /// <summary>
    ///     溶解（diss）。
    /// </summary>
    Dissolve = 2,

    /// <summary>
    ///     正片叠底（mul）。
    /// </summary>
    Multiply = 3,

    /// <summary>
    ///     滤色（scrn）。
    /// </summary>
    Screen = 4,

    /// <summary>
    ///     叠加（over）。
    /// </summary>
    Overlay = 5,

    /// <summary>
    ///     柔光（sLit）。
    /// </summary>
    SoftLight = 6,

    /// <summary>
    ///     强光（hLit）。
    /// </summary>
    HardLight = 7,

    /// <summary>
    ///     颜色减淡（hLit）。
    /// </summary>
    ColorDodge = 8,

    /// <summary>
    ///     颜色加深（cBurn）。
    /// </summary>
    ColorBurn = 9,

    /// <summary>
    ///     深色（dkCl）。
    /// </summary>
    Darken = 10,

    /// <summary>
    ///     浅色（lgCl）。
    /// </summary>
    Lighten = 11,

    /// <summary>
    ///     差值（diff）。
    /// </summary>
    Difference = 12,

    /// <summary>
    ///     排除（smud）。
    /// </summary>
    Exclusion = 13,

    /// <summary>
    ///     色相（hue）。
    /// </summary>
    Hue = 14,

    /// <summary>
    ///     饱和度（sat）。
    /// </summary>
    Saturation = 15,

    /// <summary>
    ///     颜色（colr）。
    /// </summary>
    Color = 16,

    /// <summary>
    ///     明度（lum）。
    /// </summary>
    Luminosity = 17,

    /// <summary>
    ///     穿透（pass）。
    /// </summary>
    PassThrough = 18
}
