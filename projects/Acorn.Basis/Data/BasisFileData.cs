namespace Acorn.Basis.Data;

/// <summary>
///     Basis/KTX2 纹理文件数据。
/// </summary>
public sealed class BasisFileData
{
    /// <summary>
    ///     纹理宽度。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     纹理高度。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     Mipmap 级别数。
    /// </summary>
    public int MipLevels { get; init; }

    /// <summary>
    ///     纹理格式。
    /// </summary>
    public BasisTextureFormat Format { get; init; }

    /// <summary>
    ///     是否为 sRGB。
    /// </summary>
    public bool IsSRGB { get; init; }

    /// <summary>
    ///     图像数量。
    /// </summary>
    public int ImageCount { get; init; }
}
