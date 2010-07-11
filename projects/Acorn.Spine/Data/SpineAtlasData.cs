namespace Acorn.Spine.Data;

/// <summary>
///     Spine Atlas 数据，包含图集页面和区域信息。
/// </summary>
public sealed class SpineAtlasData
{
    /// <summary>
    ///     图集页面列表。
    /// </summary>
    public IReadOnlyList<SpineAtlasPage> Pages { get; init; } = [];

    /// <summary>
    ///     图集区域列表。
    /// </summary>
    public IReadOnlyList<SpineAtlasRegion> Regions { get; init; } = [];
}

/// <summary>
///     Spine Atlas 页面数据。
/// </summary>
public sealed class SpineAtlasPage
{
    /// <summary>
    ///     纹理文件路径。
    /// </summary>
    public string TextureFilePath { get; init; } = string.Empty;

    /// <summary>
    ///     纹理宽度。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     纹理高度。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     像素格式。
    /// </summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>
    ///     缩小过滤器。
    /// </summary>
    public string FilterMin { get; init; } = string.Empty;

    /// <summary>
    ///     放大过滤器。
    /// </summary>
    public string FilterMag { get; init; } = string.Empty;

    /// <summary>
    ///     水平环绕模式。
    /// </summary>
    public string WrapS { get; init; } = "clampToEdge";

    /// <summary>
    ///     垂直环绕模式。
    /// </summary>
    public string WrapT { get; init; } = "clampToEdge";
}

/// <summary>
///     Spine Atlas 区域数据。
/// </summary>
public sealed class SpineAtlasRegion
{
    /// <summary>
    ///     区域名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     所属页面索引。
    /// </summary>
    public int PageIndex { get; init; }

    /// <summary>
    ///     在页面中的 X 坐标。
    /// </summary>
    public int X { get; init; }

    /// <summary>
    ///     在页面中的 Y 坐标。
    /// </summary>
    public int Y { get; init; }

    /// <summary>
    ///     区域宽度。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     区域高度。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     原始图像偏移 X。
    /// </summary>
    public int OffsetX { get; init; }

    /// <summary>
    ///     原始图像偏移 Y。
    /// </summary>
    public int OffsetY { get; init; }

    /// <summary>
    ///     原始图像宽度。
    /// </summary>
    public int OriginalWidth { get; init; }

    /// <summary>
    ///     原始图像高度。
    /// </summary>
    public int OriginalHeight { get; init; }

    /// <summary>
    ///     是否旋转 90 度。
    /// </summary>
    public bool IsRotated { get; init; }

    /// <summary>
    ///     是否分割（九宫格）。
    /// </summary>
    public bool IsSplit { get; init; }

    /// <summary>
    ///     分割边界（左、上、右、下）。
    /// </summary>
    public int[]? Splits { get; init; }

    /// <summary>
    ///     填充边距（左、上、右、下）。
    /// </summary>
    public int[]? Pads { get; init; }
}
