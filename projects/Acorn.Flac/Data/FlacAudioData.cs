using Acorn.Frame;

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

    /// <summary>
    ///     从 STREAMINFO 块字节缓冲区解析音频信息。
    /// </summary>
    /// <param name="buffer">指向 STREAMINFO 块数据（34 字节）的字节缓冲区。</param>
    /// <returns>解析得到的 FLAC 音频数据。</returns>
    internal static FlacAudioData ParseStreamInfo(ref ByteBuffer buffer)
    {
        var minBlockSize = buffer.ReadU16BE();
        var maxBlockSize = buffer.ReadU16BE();
        var minFrameSize = (int)(buffer.ReadU8() << 16 | buffer.ReadU16BE());
        var maxFrameSize = (int)(buffer.ReadU8() << 16 | buffer.ReadU16BE());

        var sampleRateBits = buffer.ReadU32BE();
        var sampleRate = (int)((sampleRateBits >> 12) & 0xFFFFF);
        var channels = (int)(((sampleRateBits >> 9) & 0x07) + 1);
        var bitsPerSample = (int)(((sampleRateBits >> 4) & 0x1F) + 1);
        var totalSamplesHigh = (long)(sampleRateBits & 0x0F) << 32;
        var totalSamplesLow = buffer.ReadU32BE();
        var totalSamples = totalSamplesHigh | totalSamplesLow;

        var md5 = buffer.ReadBytes(16).ToArray();

        return new FlacAudioData
        {
            MinBlockSize = minBlockSize,
            MaxBlockSize = maxBlockSize,
            SampleRate = sampleRate,
            Channels = channels,
            BitsPerSample = bitsPerSample,
            TotalSamples = totalSamples,
            MD5Checksum = md5
        };
    }

    /// <summary>
    ///     从 sample rate 位字段中解析采样率。
    /// </summary>
    /// <param name="sampleRateBits">20+4+3+5 位组合字段。</param>
    /// <returns>采样率（Hz）。</returns>
    internal static int ParseSampleRate(uint sampleRateBits)
    {
        return (int)((sampleRateBits >> 12) & 0xFFFFF);
    }

    /// <summary>
    ///     从 sample rate 位字段中解析声道数。
    /// </summary>
    /// <param name="sampleRateBits">20+4+3+5 位组合字段。</param>
    /// <returns>声道数（1-8）。</returns>
    internal static int ParseChannels(uint sampleRateBits)
    {
        return (int)(((sampleRateBits >> 9) & 0x07) + 1);
    }

    /// <summary>
    ///     从 sample rate 位字段中解析每样本位数。
    /// </summary>
    /// <param name="sampleRateBits">20+4+3+5 位组合字段。</param>
    /// <returns>每样本位数（4-32）。</returns>
    internal static int ParseBitsPerSample(uint sampleRateBits)
    {
        return (int)(((sampleRateBits >> 4) & 0x1F) + 1);
    }
}
