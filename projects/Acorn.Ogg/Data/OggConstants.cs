namespace Acorn.Ogg.Data;

/// <summary>
///     OGG 容器格式常量。
/// </summary>
public static class OggConstants
{
    /// <summary>
    ///     OGG 页面捕获模式（"OggS"）。
    /// </summary>
    public static ReadOnlySpan<byte> CapturePattern => new byte[] { 0x4F, 0x67, 0x67, 0x53 };

    /// <summary>
    ///     OGG 页面头部大小（27 字节）。
    /// </summary>
    public const int PageHeaderSize = 27;

    /// <summary>
    ///     OGG 版本号（始终为 0）。
    /// </summary>
    public const byte Version = 0;

    /// <summary>
    ///     Vorbis 音频标识（"vorbis"）。
    /// </summary>
    public static ReadOnlySpan<byte> VorbisId => new byte[] { 0x76, 0x6F, 0x72, 0x62, 0x69, 0x73 };

    /// <summary>
    ///     Opus 音频标识（"OpusHead"）。
    /// </summary>
    public static ReadOnlySpan<byte> OpusId => new byte[] { 0x4F, 0x70, 0x75, 0x73, 0x48, 0x65, 0x61, 0x64 };
}

/// <summary>
///     OGG 页面标志位。
/// </summary>
[Flags]
public enum OggPageFlags : byte
{
    /// <summary>
    ///     无标志。
    /// </summary>
    None = 0,

    /// <summary>
    ///     延续页。
    /// </summary>
    Continued = 0x01,

    /// <summary>
    ///     逻辑流的第一页。
    /// </summary>
    BeginOfStream = 0x02,

    /// <summary>
    ///     逻辑流的最后一页。
    /// </summary>
    EndOfStream = 0x04
}

/// <summary>
///     OGG 音频编解码类型。
/// </summary>
public enum OggCodecType : byte
{
    /// <summary>
    ///     未知编解码。
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     Vorbis 音频。
    /// </summary>
    Vorbis = 1,

    /// <summary>
    ///     Opus 音频。
    /// </summary>
    Opus = 2
}
