namespace Acorn.Ogg.Data;

/// <summary>
///     OGG 音频文件数据。
/// </summary>
public sealed class OggAudioData
{
    /// <summary>
    ///     编解码类型。
    /// </summary>
    public OggCodecType CodecType { get; init; }

    /// <summary>
    ///     通道数量。
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    ///     采样率（Hz）。
    /// </summary>
    public int SampleRate { get; init; }

    /// <summary>
    ///     名义比特率（bps）。
    /// </summary>
    public int NominalBitrate { get; init; }

    /// <summary>
    ///     页面数量。
    /// </summary>
    public int PageCount { get; init; }

    /// <summary>
    ///     原始数据包列表。
    /// </summary>
    public IReadOnlyList<byte[]> Packets { get; init; } = [];
}
