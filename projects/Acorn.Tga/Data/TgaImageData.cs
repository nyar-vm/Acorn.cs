namespace Acorn.Tga.Data;

/// <summary>
///     TGA 图像文件数据，保留 TGA 原生格式信息。
/// </summary>
public sealed class TgaImageData
{
    /// <summary>
    ///     图像宽度（像素）。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     图像高度（像素）。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     每像素位数（8/16/24/32）。
    /// </summary>
    public int PixelDepth { get; init; }

    /// <summary>
    ///     图像类型。
    /// </summary>
    public TgaImageType ImageType { get; init; }

    /// <summary>
    ///     是否为从上到下的行序。
    /// </summary>
    public bool IsTopDown { get; init; }

    /// <summary>
    ///     是否包含调色板。
    /// </summary>
    public bool HasColorMap { get; init; }

    /// <summary>
    ///     调色板数据（RGBA 交错排列）。
    /// </summary>
    public byte[] ColorMap { get; init; } = [];

    /// <summary>
    ///     像素数据（TGA 原生 BGR/BGRA 顺序，未翻转）。
    /// </summary>
    public byte[] PixelData { get; init; } = [];

    /// <summary>
    ///     图像 ID 长度。
    /// </summary>
    public int IdLength { get; init; }

    /// <summary>
    ///     图像 ID 数据。
    /// </summary>
    public byte[] ImageId { get; init; } = [];
}
