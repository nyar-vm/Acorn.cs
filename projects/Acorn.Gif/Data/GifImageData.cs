namespace Acorn.Gif.Data;

/// <summary>
///     GIF 图像文件数据——完整的 GIF 文件结构表示
/// </summary>
/// <remarks>
///     包含逻辑屏幕描述符、全局调色板、所有帧数据、扩展块等。
///     用于解码后的结构化表示，可传递给编码器重新编码。
/// </remarks>
public sealed class GifImageData
{
    /// <summary>
    ///     逻辑屏幕宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     逻辑屏幕高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     全局调色板（每 3 字节一组：R, G, B）
    /// </summary>
    public byte[] GlobalColorTable { get; init; } = [];

    /// <summary>
    ///     背景色索引
    /// </summary>
    public byte BackgroundColorIndex { get; init; }

    /// <summary>
    ///     像素宽高比
    /// </summary>
    public byte PixelAspectRatio { get; init; }

    /// <summary>
    ///     循环次数（0 = 无限循环，-1 = 未指定）
    /// </summary>
    public int LoopCount { get; init; }

    /// <summary>
    ///     帧列表
    /// </summary>
    public IReadOnlyList<GifImageFrame> Frames { get; init; } = [];
}

/// <summary>
///     GIF 图像帧——包含调色板索引数据和帧控制信息
/// </summary>
public sealed class GifImageFrame
{
    /// <summary>
    ///     帧左偏移
    /// </summary>
    public int Left { get; init; }

    /// <summary>
    ///     帧上偏移
    /// </summary>
    public int Top { get; init; }

    /// <summary>
    ///     帧宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     帧高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     局部调色板（每 3 字节一组：R, G, B）
    /// </summary>
    public byte[] LocalColorTable { get; init; } = [];

    /// <summary>
    ///     帧延迟时间（1/100 秒）
    /// </summary>
    public int DelayCentiseconds { get; init; }

    /// <summary>
    ///     帧处置方式
    /// </summary>
    public GifDisposalMethod DisposalMethod { get; init; }

    /// <summary>
    ///     是否有透明色
    /// </summary>
    public bool HasTransparentColor { get; init; }

    /// <summary>
    ///     透明色索引
    /// </summary>
    public int TransparentColorIndex { get; init; }

    /// <summary>
    ///     是否隔行扫描
    /// </summary>
    public bool Interlaced { get; init; }

    /// <summary>
    ///     像素索引数据（LZW 解压后的调色板索引）
    /// </summary>
    public byte[] Indices { get; init; } = [];
}
