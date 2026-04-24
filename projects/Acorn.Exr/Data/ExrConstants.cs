namespace Acorn.Exr.Data;

/// <summary>
///     OpenEXR 格式常量。
/// </summary>
public static class ExrConstants
{
    /// <summary>
    ///     EXR 魔数（0x762f3101）。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => new byte[] { 0x76, 0x2F, 0x31, 0x01 };

    /// <summary>
    ///     EXR 头部大小（魔数 + 版本）。
    /// </summary>
    public const int HeaderSize = 8;
}

/// <summary>
///     EXR 压缩类型。
/// </summary>
public enum ExrCompression : byte
{
    /// <summary>
    ///     无压缩。
    /// </summary>
    None = 0,

    /// <summary>
    ///     RLE 压缩。
    /// </summary>
    RLE = 1,

    /// <summary>
    ///     ZIPS 压缩。
    /// </summary>
    Zips = 2,

    /// <summary>
    ///     ZIP 压缩。
    /// </summary>
    Zip = 3,

    /// <summary>
    ///     PIZ 压缩。
    /// </summary>
    Piz = 4,

    /// <summary>
    ///     PXR24 压缩。
    /// </summary>
    Pxr24 = 5,

    /// <summary>
    ///     B44 压缩。
    /// </summary>
    B44 = 6,

    /// <summary>
    ///     B44A 压缩。
    /// </summary>
    B44A = 7,

    /// <summary>
    ///     DWAA 压缩。
    /// </summary>
    Dwaa = 8,

    /// <summary>
    ///     DWAB 压缩。
    /// </summary>
    Dwab = 9
}

/// <summary>
///     EXR 像素类型。
/// </summary>
public enum ExrPixelType : int
{
    /// <summary>
    ///     32 位无符号整数。
    /// </summary>
    Uint = 0,

    /// <summary>
    ///     16 位半精度浮点。
    /// </summary>
    Half = 1,

    /// <summary>
    ///     32 位单精度浮点。
    /// </summary>
    Float = 2
}
