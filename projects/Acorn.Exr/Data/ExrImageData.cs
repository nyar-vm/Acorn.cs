namespace Acorn.Exr.Data;

/// <summary>
///     OpenEXR 图像数据。
/// </summary>
public sealed class ExrImageData
{
    /// <summary>
    ///     图像宽度。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     图像高度。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     通道列表。
    /// </summary>
    public IReadOnlyList<ExrChannel> Channels { get; init; } = [];

    /// <summary>
    ///     压缩类型。
    /// </summary>
    public ExrCompression Compression { get; init; }

    /// <summary>
    ///     像素类型。
    /// </summary>
    public ExrPixelType PixelType { get; init; }

    /// <summary>
    ///     显示窗口。
    /// </summary>
    public ExrBox2i DisplayWindow { get; init; }

    /// <summary>
    ///     数据窗口。
    /// </summary>
    public ExrBox2i DataWindow { get; init; }
}

/// <summary>
///     EXR 通道。
/// </summary>
public sealed class ExrChannel
{
    /// <summary>
    ///     通道名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     像素类型。
    /// </summary>
    public ExrPixelType PixelType { get; init; }
}

/// <summary>
///     EXR 2D 框（整数）。
/// </summary>
public sealed class ExrBox2i
{
    /// <summary>
    ///     X 最小值。
    /// </summary>
    public int XMin { get; init; }

    /// <summary>
    ///     Y 最小值。
    /// </summary>
    public int YMin { get; init; }

    /// <summary>
    ///     X 最大值。
    /// </summary>
    public int XMax { get; init; }

    /// <summary>
    ///     Y 最大值。
    /// </summary>
    public int YMax { get; init; }
}
