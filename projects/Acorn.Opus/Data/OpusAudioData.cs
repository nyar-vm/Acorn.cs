namespace Acorn.Opus.Data;

/// <summary>
///     Opus 音频数据。
/// </summary>
public sealed class OpusAudioData
{
    /// <summary>
    ///     通道数量。
    /// </summary>
    public byte Channels { get; init; }

    /// <summary>
    ///     采样率。
    /// </summary>
    public uint SampleRate { get; init; }

    /// <summary>
    ///     预跳过采样数。
    /// </summary>
    public ushort PreSkip { get; init; }

    /// <summary>
    ///     输出增益。
    /// </summary>
    public short OutputGain { get; init; }

    /// <summary>
    ///     通道映射族。
    /// </summary>
    public byte ChannelMappingFamily { get; init; }
}
