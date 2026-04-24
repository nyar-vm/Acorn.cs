namespace Acorn.Opus.Data;

/// <summary>
///     Opus 音频格式常量。
/// </summary>
public static class OpusConstants
{
    /// <summary>
    ///     Opus 头部标识（"OpusHead"）。
    /// </summary>
    public static ReadOnlySpan<byte> OpusHead => "OpusHead"u8.ToArray();

    /// <summary>
    ///     Opus 标签标识（"OpusTags"）。
    /// </summary>
    public static ReadOnlySpan<byte> OpusTags => "OpusTags"u8.ToArray();

    /// <summary>
    ///     Opus 头部大小。
    /// </summary>
    public const int HeaderSize = 19;
}
