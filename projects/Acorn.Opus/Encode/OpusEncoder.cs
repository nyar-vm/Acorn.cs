using System.Buffers.Binary;
using Acorn.Opus.Data;

namespace Acorn.Opus.Encode;

/// <summary>
///     Opus 编码器，将 <see cref="OpusAudioData" /> 编码为 Opus 头部二进制格式。
/// </summary>
/// <remarks>
///     Opus 头部格式（RFC 7845 Section 5.1）：
///     OpusHead(8) + Version(1) + Channels(1) + PreSkip(2) + SampleRate(4) + OutputGain(2) + ChannelMappingFamily(1)
///     = 19 字节。
/// </remarks>
public sealed class OpusEncoder
{
    /// <summary>
    ///     将 Opus 音频数据编码为 Opus 头部二进制。
    /// </summary>
    /// <param name="data">Opus 音频数据。</param>
    /// <returns>Opus 头部二进制数据（19 字节）。</returns>
    public byte[] Encode(OpusAudioData data)
    {
        var buffer = new byte[OpusConstants.HeaderSize];
        var pos = 0;

        // 魔数 "OpusHead"
        "OpusHead"u8.CopyTo(buffer.AsSpan(pos));
        pos += 8;

        // 版本号
        buffer[pos++] = 1;

        // 通道数
        buffer[pos++] = data.Channels;

        // 预跳过采样数
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(pos), data.PreSkip);
        pos += 2;

        // 采样率
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), data.SampleRate);
        pos += 4;

        // 输出增益
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(pos), data.OutputGain);
        pos += 2;

        // 通道映射族
        buffer[pos] = data.ChannelMappingFamily;

        return buffer;
    }
}
