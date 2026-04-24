namespace Acorn.Dds.Data;

/// <summary>
///     DirectDraw Surface (DDS) 二进制格式常量。
/// </summary>
public static class DdsConstants
{
    /// <summary>
    ///     DDS 文件魔数（"DDS "）。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => new byte[] { 0x44, 0x44, 0x53, 0x20 };

    /// <summary>
    ///     DDS 文件头大小（124 字节）。
    /// </summary>
    public const int HeaderSize = 124;

    /// <summary>
    ///     DDS 头部结构大小（含魔数 4 字节 + 头部 124 字节）。
    /// </summary>
    public const int FullHeaderSize = 128;

    /// <summary>
    ///     像素格式结构大小（32 字节）。
    /// </summary>
    public const int PixelFormatSize = 32;
}

/// <summary>
///     DDS 表面标志位。
/// </summary>
[Flags]
public enum DdsFlags : uint
{
    /// <summary>
    ///     包含高度信息。
    /// </summary>
    Height = 0x00000002,

    /// <summary>
    ///     包含宽度信息。
    /// </summary>
    Width = 0x00000004,

    /// <summary>
    ///     包含像素格式。
    /// </summary>
    PixelFormat = 0x00001000,

    /// <summary>
    ///     包含间距（压缩纹理）。
    /// </summary>
    Pitch = 0x00000008,

    /// <summary>
    ///     包含行间距（未压缩纹理）。
    /// </summary>
    LinearSize = 0x00080000,

    /// <summary>
    ///     包含 Mipmap 数量。
    /// </summary>
    MipMapCount = 0x00020000,

    /// <summary>
    ///     包含深度（体积纹理）。
    /// </summary>
    Depth = 0x00800000
}

/// <summary>
///     DDS 像素格式标志位。
/// </summary>
[Flags]
public enum DdsPixelFormatFlags : uint
{
    /// <summary>
    ///     包含 Alpha 数据。
    /// </summary>
    AlphaPixels = 0x00000001,

    /// <summary>
    ///     Alpha 预乘。
    /// </summary>
    Alpha = 0x00000002,

    /// <summary>
    ///     四字符代码（FourCC）压缩格式。
    /// </summary>
    FourCC = 0x00000004,

    /// <summary>
    ///     RGB 格式。
    /// </summary>
    RGB = 0x00000040,

    /// <summary>
    ///     YUV 格式。
    /// </summary>
    YUV = 0x00000200,

    /// <summary>
    ///     Luminance 格式。
    /// </summary>
    Luminance = 0x00020000
}

/// <summary>
///     DDS 资源维度。
/// </summary>
public enum DdsResourceDimension : uint
{
    /// <summary>
    ///     未知维度。
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     纹理 1D。
    /// </summary>
    Texture1D = 2,

    /// <summary>
    ///     纹理 2D。
    /// </summary>
    Texture2D = 3,

    /// <summary>
    ///     纹理 3D。
    /// </summary>
    Texture3D = 4
}

/// <summary>
///     DDS 常用 FourCC 压缩格式。
/// </summary>
public static class DdsFourCC
{
    /// <summary>
    ///     DXT1 压缩（BC1）。
    /// </summary>
    public const uint DXT1 = 0x31545844;

    /// <summary>
    ///     DXT3 压缩（BC2）。
    /// </summary>
    public const uint DXT3 = 0x33545844;

    /// <summary>
    ///     DXT5 压缩（BC3）。
    /// </summary>
    public const uint DXT5 = 0x35545844;

    /// <summary>
    ///     ATI1 压缩（BC4）。
    /// </summary>
    public const uint ATI1 = 0x31495441;

    /// <summary>
    ///     ATI2 压缩（BC5）。
    /// </summary>
    public const uint ATI2 = 0x32495441;

    /// <summary>
    ///     BC6H 压缩（HDR）。
    /// </summary>
    public const uint BC6H = 0x48364342;

    /// <summary>
    ///     BC7 压缩（高质量）。
    /// </summary>
    public const uint BC7 = 0x37434220;
}
