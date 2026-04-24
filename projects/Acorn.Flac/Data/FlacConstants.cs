namespace Acorn.Flac.Data;

/// <summary>
///     FLAC 无损音频格式常量。
/// </summary>
public static class FlacConstants
{
    /// <summary>
    ///     FLAC 流标记（"fLaC"）。
    /// </summary>
    public static ReadOnlySpan<byte> StreamMarker => "fLaC"u8.ToArray();

    /// <summary>
    ///     流标记长度。
    /// </summary>
    public const int StreamMarkerLength = 4;
}

/// <summary>
///     FLAC 元数据块类型。
/// </summary>
public enum FlacMetadataBlockType : byte
{
    /// <summary>
    ///     流信息。
    /// </summary>
    StreamInfo = 0,

    /// <summary>
    ///   填充。
    /// </summary>
    Padding = 1,

    /// <summary>
    ///     应用程序。
    /// </summary>
    Application = 2,

    /// <summary>
    ///     查找表。
    /// </summary>
    SeekTable = 3,

    /// <summary>
    ///     Vorbis 注释。
    /// </summary>
    VorbisComment = 4,

    /// <summary>
    ///     CUE 表。
    /// </summary>
    CueSheet = 5,

    /// <summary>
    ///     图片。
    /// </summary>
    Picture = 6
}
