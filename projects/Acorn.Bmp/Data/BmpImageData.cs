namespace Acorn.Bmp.Data;

/// <summary>
///     BMP 图像文件数据。
/// </summary>
public sealed class BmpImageData
{
    /// <summary>
    ///     图像宽度（像素）。
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     图像高度（像素），正值表示自底向上，负值表示自顶向下。
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     每像素位数。
    /// </summary>
    public ushort BitsPerPixel { get; init; }

    /// <summary>
    ///     压缩方式。
    /// </summary>
    public BmpCompression Compression { get; init; }

    /// <summary>
    ///     图像数据大小（字节）。
    /// </summary>
    public uint ImageSize { get; init; }

    /// <summary>
    ///     水平分辨率（像素/米）。
    /// </summary>
    public int XPelsPerMeter { get; init; }

    /// <summary>
    ///     垂直分辨率（像素/米）。
    /// </summary>
    public int YPelsPerMeter { get; init; }

    /// <summary>
    ///     使用的颜色数。
    /// </summary>
    public uint ColorsUsed { get; init; }

    /// <summary>
    ///     重要的颜色数。
    /// </summary>
    public uint ColorsImportant { get; init; }

    /// <summary>
    ///     调色板（仅 8 位及以下图像）。
    /// </summary>
    public uint[] Palette { get; init; } = [];

    /// <summary>
    ///     像素数据。
    /// </summary>
    public byte[] PixelData { get; init; } = [];

    /// <summary>
    ///     绝对高度（始终为正值）。
    /// </summary>
    public int AbsoluteHeight => Math.Abs(Height);

    /// <summary>
    ///     是否为自顶向下存储。
    /// </summary>
    public bool IsTopDown => Height < 0;

    /// <summary>
    ///     每行字节数（含 4 字节对齐填充）。
    /// </summary>
    public int Stride => ((Width * BitsPerPixel + 31) / 32) * 4;
}
