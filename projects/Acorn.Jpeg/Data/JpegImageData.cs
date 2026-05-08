namespace Acorn.Jpeg.Data;

/// <summary>
///     JPEG 图像文件数据。
/// </summary>
public sealed class JpegImageData
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
    ///     样本精度（位数），默认 8。
    /// </summary>
    public int Precision { get; init; } = 8;

    /// <summary>
    ///     色彩空间。
    /// </summary>
    public JpegColorSpace ColorSpace { get; init; }

    /// <summary>
    ///     分量信息列表。
    /// </summary>
    public JpegComponentInfo[] Components { get; init; } = [];

    /// <summary>
    ///     解码后的像素数据（RGBA 或灰度格式）。
    /// </summary>
    public byte[] PixelData { get; init; } = [];

    /// <summary>
    ///     是否为渐进式 JPEG。
    /// </summary>
    public bool IsProgressive { get; init; }
}

/// <summary>
///     JPEG 分量信息。
/// </summary>
public sealed class JpegComponentInfo
{
    /// <summary>
    ///     分量标识符。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    ///     水平采样因子。
    /// </summary>
    public int H { get; init; }

    /// <summary>
    ///     垂直采样因子。
    /// </summary>
    public int V { get; init; }

    /// <summary>
    ///     量化表标识符。
    /// </summary>
    public int QuantTableId { get; init; }

    /// <summary>
    ///     直流 Huffman 表标识符。
    /// </summary>
    public int DcTableId { get; init; }

    /// <summary>
    ///     交流 Huffman 表标识符。
    /// </summary>
    public int AcTableId { get; init; }
}
