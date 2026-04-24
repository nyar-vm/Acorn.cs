namespace Acorn.Dds.Data;

/// <summary>
///     DDS 纹理文件数据。
/// </summary>
public sealed class DdsTextureData
{
    /// <summary>
    ///     纹理高度（像素）。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     纹理宽度（像素）。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     纹理深度（体积纹理使用，默认为 0）。
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    ///     Mipmap 级别数量。
    /// </summary>
    public int MipMapCount { get; init; }

    /// <summary>
    ///     像素格式。
    /// </summary>
    public DdsPixelFormatData PixelFormat { get; init; } = new();

    /// <summary>
    ///     资源维度。
    /// </summary>
    public DdsResourceDimension Dimension { get; init; }

    /// <summary>
    ///     是否为立方体贴图。
    /// </summary>
    public bool IsCubeMap { get; init; }

    /// <summary>
    ///     立方体贴图面数（6 表示完整立方体贴图）。
    /// </summary>
    public int CubeMapFaceCount { get; init; }

    /// <summary>
    ///     纹理数据（按表面排列）。
    /// </summary>
    public IReadOnlyList<DdsSurfaceData> Surfaces { get; init; } = [];
}

/// <summary>
///     DDS 像素格式数据。
/// </summary>
public sealed class DdsPixelFormatData
{
    /// <summary>
    ///     像素格式标志位。
    /// </summary>
    public DdsPixelFormatFlags Flags { get; init; }

    /// <summary>
    ///     FourCC 压缩格式代码。
    /// </summary>
    public uint FourCC { get; init; }

    /// <summary>
    ///     每像素 RGB 位数。
    /// </summary>
    public uint RGBBitCount { get; init; }

    /// <summary>
    ///     红色通道位掩码。
    /// </summary>
    public uint RBitMask { get; init; }

    /// <summary>
    ///     绿色通道位掩码。
    /// </summary>
    public uint GBitMask { get; init; }

    /// <summary>
    ///     蓝色通道位掩码。
    /// </summary>
    public uint BBitMask { get; init; }

    /// <summary>
    ///     Alpha 通道位掩码。
    /// </summary>
    public uint ABitMask { get; init; }

    /// <summary>
    ///     FourCC 格式名称。
    /// </summary>
    public string FourCCName => FourCC switch
    {
        DdsFourCC.DXT1 => "DXT1",
        DdsFourCC.DXT3 => "DXT3",
        DdsFourCC.DXT5 => "DXT5",
        DdsFourCC.ATI1 => "ATI1",
        DdsFourCC.ATI2 => "ATI2",
        DdsFourCC.BC6H => "BC6H",
        DdsFourCC.BC7 => "BC7",
        0 => "无",
        _ => $"0x{FourCC:X8}"
    };
}

/// <summary>
///     DDS 表面数据（包含 Mipmap 链）。
/// </summary>
public sealed class DdsSurfaceData
{
    /// <summary>
    ///     Mipmap 级别数据。
    /// </summary>
    public IReadOnlyList<DdsMipLevelData> MipLevels { get; init; } = [];
}

/// <summary>
///     DDS Mipmap 级别数据。
/// </summary>
public sealed class DdsMipLevelData
{
    /// <summary>
    ///     级别宽度。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     级别高度。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     像素数据。
    /// </summary>
    public byte[] Data { get; init; } = [];
}
