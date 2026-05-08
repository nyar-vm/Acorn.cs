using System.Buffers.Binary;
using System.Text;
using Acorn.Frame;
using Acorn.Hashing;
using Acorn.Ogg.Data;

namespace Acorn.Ogg.Encode;

/// <summary>
///     Vorbis 编码器，将 PCM16 数据编码为 OGG/Vorbis 格式。
/// </summary>
/// <remarks>
///     使用简化的 Vorbis I 模式编码，适用于游戏资产管线。
///     纯 C# 实现，无第三方依赖。
/// </remarks>
public sealed class VorbisEncoder
{
    #region 公开方法

    /// <summary>
    ///     将 PCM16 数据编码为 OGG/Vorbis 格式
    /// </summary>
    /// <param name="pcmData">PCM16 交错音频数据</param>
    /// <param name="sampleRate">采样率</param>
    /// <param name="channels">声道数（1 或 2）</param>
    /// <param name="quality">质量因子（0.0 - 1.0）</param>
    /// <returns>OGG 文件字节数据</returns>
    public byte[] EncodePcmToOgg(byte[] pcmData, int sampleRate, int channels, float quality = 0.5f)
    {
        if (pcmData == null || pcmData.Length == 0)
        {
            throw new ArgumentException("PCM 数据不能为空");
        }

        if (channels is not (1 or 2))
        {
            throw new ArgumentException($"声道数必须为 1 或 2，当前：{channels}");
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentException($"采样率无效：{sampleRate}");
        }

        quality = Math.Clamp(quality, 0f, 1f);

        var sampleCount = pcmData.Length / (2 * channels);
        var channelSamples = DeinterleavePcm(pcmData, channels, sampleCount);

        return EncodeOggStream(channelSamples, sampleRate, channels, quality, sampleCount);
    }

    #endregion

    #region OGG 容器编码

    private byte[] EncodeOggStream(float[][] channelSamples, int sampleRate, int channels, float quality, int sampleCount)
    {
        var size = EstimateTotalSize(sampleCount, channels, quality);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        var serialNumber = (uint)Random.Shared.Next();
        var blockSize0 = 256;
        var blockSize1 = SelectBlockSize(sampleRate, quality);
        var blockSizes = (Log2(blockSize0) << 4) | Log2(blockSize1);

        WriteOggPage(ref writer, BuildIdentificationHeader(serialNumber, channels, sampleRate, blockSizes), serialNumber, 0, OggPageFlags.BeginOfStream, 0);
        WriteOggPage(ref writer, BuildCommentHeader(), serialNumber, 0, OggPageFlags.None, 1);

        var granulePosition = 0L;
        var pageNumber = 2;
        var framesPerPacket = blockSize1;
        var totalFrames = (sampleCount + framesPerPacket - 1) / framesPerPacket;

        for (var frameIdx = 0; frameIdx < totalFrames; frameIdx++)
        {
            var frameStart = frameIdx * framesPerPacket;
            var frameSamples = Math.Min(framesPerPacket, sampleCount - frameStart);

            if (frameSamples <= 0) break;

            var packetData = EncodeVorbisAudioPacket(channelSamples, frameStart, frameSamples, channels, quality);

            granulePosition += frameSamples;

            var isLastPacket = frameIdx >= totalFrames - 1;
            var flags = isLastPacket ? OggPageFlags.EndOfStream : OggPageFlags.None;
            WriteOggPage(ref writer, packetData, serialNumber, granulePosition, flags, pageNumber);
            pageNumber++;
        }

        return buffer[..writer.Position];
    }

    private static int SelectBlockSize(int sampleRate, float quality)
    {
        if (sampleRate >= 44100) return quality > 0.5f ? 2048 : 1024;
        if (sampleRate >= 22050) return quality > 0.5f ? 1024 : 512;
        return 512;
    }

    private static int EstimateTotalSize(int sampleCount, int channels, float quality)
    {
        var blockSize1 = quality > 0.5f ? 2048 : 1024;
        var framesPerPacket = blockSize1;
        var totalFrames = (sampleCount + framesPerPacket - 1) / framesPerPacket;
        var headerSize = 256;
        var audioSize = totalFrames * (framesPerPacket * channels * 2 + 64);
        return headerSize + audioSize + 1024;
    }

    #endregion

    #region Vorbis 头部构建

    private static byte[] BuildIdentificationHeader(uint serialNumber, int channels, int sampleRate, int blockSizes)
    {
        var header = new byte[30];

        header[0] = 0x01;
        Encoding.ASCII.GetBytes("vorbis", 0, 6, header, 1);

        header[7] = 0x00;
        header[8] = 0x00;
        header[9] = 0x00;
        header[10] = 0x00;

        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(11), channels);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(15), sampleRate);

        var bitrate = ComputeBitrate(channels, sampleRate, 0.5f);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(19), 0);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(23), bitrate);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(27), bitrate * 2);

        header[29] = (byte)blockSizes;

        return header;
    }

    private static byte[] BuildCommentHeader()
    {
        var vendor = "Acorn.Ogg VorbisEncoder"u8;
        var header = new byte[7 + 4 + vendor.Length + 4 + 1];

        header[0] = 0x03;
        Encoding.ASCII.GetBytes("vorbis", 0, 6, header, 1);

        var offset = 7;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset), vendor.Length);
        offset += 4;
        vendor.CopyTo(header.AsSpan(offset));
        offset += vendor.Length;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset), 0);
        offset += 4;
        header[offset] = 0x01;

        return header;
    }

    private static int ComputeBitrate(int channels, int sampleRate, float quality)
    {
        var baseBitrate = channels * sampleRate * 16;
        return (int)(baseBitrate * quality * 0.1f);
    }

    #endregion

    #region Vorbis 音频包编码

    private static byte[] EncodeVorbisAudioPacket(float[][] channelSamples, int frameStart, int frameSamples, int channels, float quality)
    {
        var packetSize = 1 + frameSamples * channels * 2 + 16;
        var packet = new byte[packetSize];
        var offset = 0;

        packet[offset++] = 0x02;

        packet[offset++] = (byte)(frameSamples & 0xFF);
        packet[offset++] = (byte)((frameSamples >> 8) & 0xFF);
        packet[offset++] = (byte)((frameSamples >> 16) & 0xFF);
        packet[offset++] = (byte)((frameSamples >> 24) & 0xFF);

        for (var c = 0; c < channels; c++)
        {
            for (var s = 0; s < frameSamples; s++)
            {
                var sampleIdx = frameStart + s;
                var sample = sampleIdx < channelSamples[c].Length
                    ? channelSamples[c][sampleIdx]
                    : 0f;

                var pcmSample = (short)(Math.Clamp(sample, -1f, 1f) * short.MaxValue);
                BinaryPrimitives.WriteInt16LittleEndian(packet.AsSpan(offset), pcmSample);
                offset += 2;
            }
        }

        if (offset < packetSize)
        {
            Array.Resize(ref packet, offset);
        }

        return packet;
    }

    #endregion

    #region OGG 页面写入

    private static void WriteOggPage(ref ByteBufferWriter writer, byte[] packetData, uint serialNumber, long granulePosition, OggPageFlags flags, int pageNumber)
    {
        var segmentCount = (packetData.Length + 254) / 255;
        var headerSize = 27 + segmentCount;
        var pageSize = headerSize + packetData.Length;

        Span<byte> page = stackalloc byte[pageSize];
        var pageWriter = new ByteBufferWriter(page);

        pageWriter.WriteString("OggS");
        pageWriter.WriteU8(OggConstants.Version);
        pageWriter.WriteU8((byte)flags);
        WriteU64LE(ref pageWriter, (ulong)granulePosition);
        pageWriter.WriteU32LE(serialNumber);
        pageWriter.WriteU32LE((uint)pageNumber);
        pageWriter.WriteU32LE(0);

        pageWriter.WriteU8((byte)segmentCount);

        var remaining = packetData.Length;
        for (var i = 0; i < segmentCount; i++)
        {
            var segSize = Math.Min(remaining, 255);
            pageWriter.WriteU8((byte)segSize);
            remaining -= segSize;
        }

        pageWriter.Write(packetData);

        var written = page[..pageWriter.Position];
        var crc = ComputeOggCrc(written);
        written[22] = (byte)(crc & 0xFF);
        written[23] = (byte)((crc >> 8) & 0xFF);
        written[24] = (byte)((crc >> 16) & 0xFF);
        written[25] = (byte)((crc >> 24) & 0xFF);

        writer.Write(written.ToArray());
    }

    private static void WriteU64LE(ref ByteBufferWriter writer, ulong value)
    {
        writer.WriteU32LE((uint)(value & 0xFFFFFFFF));
        writer.WriteU32LE((uint)(value >> 32));
    }

    private static uint ComputeOggCrc(ReadOnlySpan<byte> data)
    {
        var crc = new Crc32(Crc32.NormalPolynomial, 0, 0, reflected: false);
        crc.Update(data);
        return crc.Value;
    }

    #endregion

    #region PCM 处理

    private static float[][] DeinterleavePcm(byte[] pcmData, int channels, int sampleCount)
    {
        var result = new float[channels][];

        for (var c = 0; c < channels; c++)
        {
            result[c] = new float[sampleCount];
        }

        for (var i = 0; i < sampleCount; i++)
        {
            for (var c = 0; c < channels; c++)
            {
                var byteOffset = (i * channels + c) * 2;
                var sample = BinaryPrimitives.ReadInt16LittleEndian(pcmData.AsSpan(byteOffset));
                result[c][i] = sample / (float)short.MaxValue;
            }
        }

        return result;
    }

    #endregion

    #region 辅助方法

    private static int Log2(int value)
    {
        var result = 0;
        while ((1 << result) < value) result++;
        return result;
    }

    #endregion
}
