namespace Acorn.Gif.Data;

/// <summary>
///     GIF 图像帧数据
/// </summary>
public sealed class GifFrame
{
    /// <summary>
    ///     帧宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     帧高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     帧延迟时间（1/100 秒）
    /// </summary>
    public int DelayCentiseconds { get; init; }

    /// <summary>
    ///     帧处置方式
    /// </summary>
    public GifDisposalMethod DisposalMethod { get; init; }

    /// <summary>
    ///     RGBA 像素数据
    /// </summary>
    public byte[] RgbaData { get; init; } = [];
}

/// <summary>
///     GIF 帧处置方式
/// </summary>
public enum GifDisposalMethod
{
    /// <summary>未指定</summary>
    None = 0,
    /// <summary>不处置</summary>
    DoNotDispose = 1,
    /// <summary>恢复为背景色</summary>
    RestoreToBackground = 2,
    /// <summary>恢复为前一帧</summary>
    RestoreToPrevious = 3
}
