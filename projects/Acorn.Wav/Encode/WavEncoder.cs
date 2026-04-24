using Acorn.Frame;
using Acorn.Wav.Data;

namespace Acorn.Wav.Encode;

/// <summary>
///     WAV 文件编码器，将 C# 数据结构编码为 WAV 音频格式。
/// </summary>
/// <remarks>
///     WAV 是 Microsoft/IBM 的标准音频格式，基于 RIFF 容器，广泛用于游戏和多媒体应用。
///     编码器生成符合 RIFF/WAVE 规范的二进制数据。
/// </remarks>
public sealed class WavEncoder
{
    /// <summary>
    ///     将 WAV 音频数据编码为 WAV 二进制格式。
    /// </summary>
    /// <param name="data">WAV 音频数据。</param>
    /// <returns>WAV 二进制数据。</returns>
    public byte[] Encode(WavAudioData data)
    {
        var size = EstimateSize(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteRiffHeader(ref writer, data);
        WriteFmtChunk(ref writer, data);
        WriteDataChunk(ref writer, data);

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void WriteRiffHeader(ref ByteBufferWriter writer, WavAudioData data)
    {
        var fileSize = (uint)(4 + 24 + 8 + data.SampleData.Length);

        writer.WriteString(WavConstants.RiffTag);
        writer.WriteU32LE(fileSize);
        writer.WriteString(WavConstants.WaveTag);
    }

    private static void WriteFmtChunk(ref ByteBufferWriter writer, WavAudioData data)
    {
        writer.WriteString("fmt ");
        writer.WriteU32LE(16);
        writer.WriteU16LE((ushort)data.FormatTag);
        writer.WriteU16LE(data.Channels);
        writer.WriteU32LE(data.SampleRate);
        writer.WriteU32LE(data.ByteRate);
        writer.WriteU16LE(data.BlockAlign);
        writer.WriteU16LE(data.BitsPerSample);
    }

    private static void WriteDataChunk(ref ByteBufferWriter writer, WavAudioData data)
    {
        writer.WriteString("data");
        writer.WriteU32LE((uint)data.SampleData.Length);
        writer.Write(data.SampleData);
    }

    private static int EstimateSize(WavAudioData data)
    {
        return 12 + 24 + 8 + data.SampleData.Length + 256;
    }

    #endregion
}
