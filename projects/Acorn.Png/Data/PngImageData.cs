namespace Acorn.Png.Data;

/// <summary>
///     PNG 图像文件数据。
/// </summary>
public sealed class PngImageData
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
    ///     位深度。
    /// </summary>
    public byte BitDepth { get; init; }

    /// <summary>
    ///     色彩类型。
    /// </summary>
    public PngColorType ColorType { get; init; }

    /// <summary>
    ///     压缩方法。
    /// </summary>
    public PngCompressionMethod CompressionMethod { get; init; }

    /// <summary>
    ///     滤波方法。
    /// </summary>
    public PngFilterMethod FilterMethod { get; init; }

    /// <summary>
    ///     隔行扫描方法。
    /// </summary>
    public PngInterlaceMethod InterlaceMethod { get; init; }

    /// <summary>
    ///     调色板（仅索引色图像）。
    /// </summary>
    public byte[] Palette { get; init; } = [];

    /// <summary>
    ///     透明度数据（tRNS 块）。
    /// </summary>
    public byte[] Transparency { get; init; } = [];

    /// <summary>
    ///     像素数据（已解压，含每行滤波器字节）。
    /// </summary>
    public byte[] RawPixelData { get; init; } = [];

    /// <summary>
    ///     辅助块列表。
    /// </summary>
    public IReadOnlyList<PngChunk> AncillaryChunks { get; init; } = [];

    /// <summary>
    ///     每像素字节数。
    /// </summary>
    public int BytesPerPixel => ColorType switch
    {
        PngColorType.Grayscale => BitDepth <= 8 ? 1 : 2,
        PngColorType.Truecolor => BitDepth <= 8 ? 3 : 6,
        PngColorType.Indexed => 1,
        PngColorType.GrayscaleAlpha => BitDepth <= 8 ? 2 : 4,
        PngColorType.TruecolorAlpha => BitDepth <= 8 ? 4 : 8,
        _ => 1
    };
}

/// <summary>
///     PNG 块数据。
/// </summary>
public sealed class PngChunk
{
    /// <summary>
    ///     块类型（4 字节 ASCII）。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    ///     块数据。
    /// </summary>
    public byte[] Data { get; init; } = [];
}
