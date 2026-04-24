namespace Acorn.Flac.Data;

/// <summary>
///     FLAC 音频数据。
/// </summary>
public sealed class FlacAudioData
{
    /// <summary>
    ///     通道数量（1-8）。
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    ///     采样率（Hz）。
    /// </summary>
    public int SampleRate { get; init; }

    /// <summary>
    ///     每样本位数（4-32）。
    /// </summary>
    public int BitsPerSample { get; init; }

    /// <summary>
    ///     总采样数。
    /// </summary>
    public long TotalSamples { get; init; }

    /// <summary>
    ///     MD5 校验和。
    /// </summary>
    public byte[] MD5Checksum { get; init; } = new byte[16];

    /// <summary>
    ///     音频时长（秒）。
    /// </summary>
    public double Duration => SampleRate > 0 ? (double)TotalSamples / SampleRate : 0;

    /// <summary>
    ///     最小块大小。
    /// </summary>
    public int MinBlockSize { get; init; }

    /// <summary>
    ///     最大块大小。
    /// </summary>
    public int MaxBlockSize { get; init; }
}
