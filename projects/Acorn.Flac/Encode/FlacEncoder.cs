using System.Buffers.Binary;
using Acorn.Flac.Data;

namespace Acorn.Flac.Encode;

/// <summary>
///     FLAC 编码器，将 <see cref="FlacAudioData" /> 编码为 FLAC 二进制格式。
/// </summary>
/// <remarks>
///     FLAC 文件结构：流标记 "fLaC"(4) + 元数据块列表。
///     每个元数据块：头部(4) + 数据(N)。
///     STREAMINFO 块数据固定为 34 字节。
/// </remarks>
public sealed class FlacEncoder
{
    /// <summary>
    ///     STREAMINFO 块数据大小。
    /// </summary>
    private const int StreamInfoDataSize = 34;

    /// <summary>
    ///     将 FLAC 音频数据编码为 FLAC 二进制。
    /// </summary>
    /// <param name="data">FLAC 音频数据。</param>
    /// <returns>FLAC 二进制数据（42 字节：4 + 4 + 34）。</returns>
    public byte[] Encode(FlacAudioData data)
    {
        var buffer = new byte[FlacConstants.StreamMarkerLength + 4 + StreamInfoDataSize];
        var pos = 0;

        // 流标记 "fLaC"
        "fLaC"u8.CopyTo(buffer.AsSpan(pos));
        pos += 4;

        // 元数据块头部：isLast=1 | type=0(StreamInfo) | size=34
        var header = 0x80000000u | ((uint)StreamInfoDataSize & 0x00FFFFFF);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(pos), header);
        pos += 4;

        // STREAMINFO 数据体（34 字节）
        EncodeStreamInfo(buffer.AsSpan(pos), data);

        return buffer;
    }

    /// <summary>
    ///     编码 STREAMINFO 数据体。
    /// </summary>
    private static void EncodeStreamInfo(Span<byte> buffer, FlacAudioData data)
    {
        var pos = 0;

        // 最小块大小
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(pos), (ushort)data.MinBlockSize);
        pos += 2;

        // 最大块大小
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(pos), (ushort)data.MaxBlockSize);
        pos += 2;

        // 最小帧大小（U24BE）
        buffer[pos] = 0;
        buffer[pos + 1] = 0;
        buffer[pos + 2] = 0;
        pos += 3;

        // 最大帧大小（U24BE）
        buffer[pos] = 0;
        buffer[pos + 1] = 0;
        buffer[pos + 2] = 0;
        pos += 3;

        // 采样率组合字段：SampleRate(20) + (Channels-1)(3) + (BitsPerSample-1)(5) + TotalSamplesHigh(4)
        var totalSamples = (ulong)data.TotalSamples;
        var totalSamplesHigh = (uint)(totalSamples >> 32) & 0x0F;
        var combined = ((uint)data.SampleRate << 12)
                       | ((uint)(data.Channels - 1) << 9)
                       | ((uint)(data.BitsPerSample - 1) << 4)
                       | totalSamplesHigh;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(pos), combined);
        pos += 4;

        // 总采样数低位
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(pos), (uint)(totalSamples & 0xFFFFFFFF));
        pos += 4;

        // MD5 校验和（16 字节）
        data.MD5Checksum.AsSpan().CopyTo(buffer.Slice(pos));
    }
}
