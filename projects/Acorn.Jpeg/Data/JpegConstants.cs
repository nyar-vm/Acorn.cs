namespace Acorn.Jpeg.Data;

/// <summary>
///     JPEG 图像格式常量。
/// </summary>
public static class JpegConstants
{
    /// <summary>
    ///     JPEG 文件签名（3 字节：0xFF 0xD8 0xFF）。
    /// </summary>
    public static ReadOnlySpan<byte> Signature => new byte[] { 0xFF, 0xD8, 0xFF };

    /// <summary>
    ///     SOI（Start of Image）标记。
    /// </summary>
    public const byte SoiMarker = 0xD8;

    /// <summary>
    ///     EOI（End of Image）标记。
    /// </summary>
    public const byte EoiMarker = 0xD9;

    /// <summary>
    ///     SOF0（Baseline DCT）标记。
    /// </summary>
    public const byte Sof0Marker = 0xC0;

    /// <summary>
    ///     SOF2（Progressive DCT）标记。
    /// </summary>
    public const byte Sof2Marker = 0xC2;

    /// <summary>
    ///     DHT（Define Huffman Table）标记。
    /// </summary>
    public const byte DhtMarker = 0xC4;

    /// <summary>
    ///     DQT（Define Quantization Table）标记。
    /// </summary>
    public const byte DqtMarker = 0xDB;

    /// <summary>
    ///     SOS（Start of Scan）标记。
    /// </summary>
    public const byte SosMarker = 0xDA;

    /// <summary>
    ///     RST0（Restart 0）标记。
    /// </summary>
    public const byte Rst0Marker = 0xD0;

    /// <summary>
    ///     RST1（Restart 1）标记。
    /// </summary>
    public const byte Rst1Marker = 0xD1;

    /// <summary>
    ///     RST2（Restart 2）标记。
    /// </summary>
    public const byte Rst2Marker = 0xD2;

    /// <summary>
    ///     RST3（Restart 3）标记。
    /// </summary>
    public const byte Rst3Marker = 0xD3;

    /// <summary>
    ///     RST4（Restart 4）标记。
    /// </summary>
    public const byte Rst4Marker = 0xD4;

    /// <summary>
    ///     RST5（Restart 5）标记。
    /// </summary>
    public const byte Rst5Marker = 0xD5;

    /// <summary>
    ///     RST6（Restart 6）标记。
    /// </summary>
    public const byte Rst6Marker = 0xD6;

    /// <summary>
    ///     RST7（Restart 7）标记。
    /// </summary>
    public const byte Rst7Marker = 0xD7;

    /// <summary>
    ///     APP0（JFIF 应用标记）。
    /// </summary>
    public const byte App0Marker = 0xE0;

    /// <summary>
    ///     APP1（EXIF 应用标记）。
    /// </summary>
    public const byte App1Marker = 0xE1;
}

/// <summary>
///     JPEG 色彩空间。
/// </summary>
public enum JpegColorSpace
{
    /// <summary>
    ///     灰度。
    /// </summary>
    Grayscale = 0,

    /// <summary>
    ///     YCbCr 色彩空间。
    /// </summary>
    YCbCr = 1,

    /// <summary>
    ///     YCCK 色彩空间。
    /// </summary>
    YCCK = 2,

    /// <summary>
    ///     CMYK 色彩空间。
    /// </summary>
    Cmyk = 3,

    /// <summary>
    ///     RGB 色彩空间。
    /// </summary>
    Rgb = 4
}
