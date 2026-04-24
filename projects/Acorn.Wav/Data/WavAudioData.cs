namespace Acorn.Wav.Data;

/// <summary>
///     WAV 音频文件数据。
/// </summary>
public sealed class WavAudioData
{
    /// <summary>
    ///     音频格式标签。
    /// </summary>
    public WavFormatTag FormatTag { get; init; }

    /// <summary>
    ///     通道数量。
    /// </summary>
    public ushort Channels { get; init; }

    /// <summary>
    ///     采样率（Hz）。
    /// </summary>
    public uint SampleRate { get; init; }

    /// <summary>
    ///     字节率（字节/秒）。
    /// </summary>
    public uint ByteRate { get; init; }

    /// <summary>
    ///     块对齐（字节）。
    /// </summary>
    public ushort BlockAlign { get; init; }

    /// <summary>
    ///     每样本位数。
    /// </summary>
    public ushort BitsPerSample { get; init; }

    /// <summary>
    ///     音频采样数据。
    /// </summary>
    public byte[] SampleData { get; init; } = [];

    /// <summary>
    ///     音频时长（秒）。
    /// </summary>
    public double Duration => ByteRate > 0 ? (double)SampleData.Length / ByteRate : 0;

    /// <summary>
    ///     总采样数。
    /// </summary>
    public long TotalSamples => BlockAlign > 0 ? SampleData.Length / BlockAlign : 0;
}
