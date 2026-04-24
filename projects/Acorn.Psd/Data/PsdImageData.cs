namespace Acorn.Psd.Data;

/// <summary>
///     PSD 图像数据。
/// </summary>
public sealed class PsdImageData
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
    ///     通道数量（1-56）。
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    ///     颜色深度（1、8、16、32）。
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    ///     颜色模式（0=位图, 1=灰度, 2=索引色, 3=RGB, 4=CMYK, 7=多通道, 8=双色调, 9=Lab）。
    /// </summary>
    public int ColorMode { get; init; }

    /// <summary>
    ///     图层列表。
    /// </summary>
    public IReadOnlyList<PsdLayer> Layers { get; init; } = [];

    /// <summary>
    ///     合并后的图像数据（通道优先的像素数据）。
    /// </summary>
    public byte[]? MergedImageData { get; init; }
}

/// <summary>
///     PSD 图层数据。
/// </summary>
public sealed class PsdLayer
{
    /// <summary>
    ///     图层名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     图层矩形（上、左、下、右）。
    /// </summary>
    public (int Top, int Left, int Bottom, int Right) Bounds { get; init; }

    /// <summary>
    ///     通道数量。
    /// </summary>
    public int ChannelCount { get; init; }

    /// <summary>
    ///     混合模式（norm=正常, diss=溶解, mult=正片叠底 等）。
    /// </summary>
    public string BlendMode { get; init; } = "norm";

    /// <summary>
    ///     不透明度（0-255）。
    /// </summary>
    public byte Opacity { get; init; } = 255;

    /// <summary>
    ///     是否可见。
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    ///     各通道图像数据的字节长度（含压缩类型 2 字节）。
    /// </summary>
    public IReadOnlyList<uint> ChannelDataLengths { get; init; } = [];

    /// <summary>
    ///     图层像素数据（按通道存储）。
    /// </summary>
    public IReadOnlyDictionary<int, byte[]> ChannelData { get; init; } = new Dictionary<int, byte[]>();
}
