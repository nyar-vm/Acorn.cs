using Acorn.Frame;
using Acorn.Wav.Data;

namespace Acorn.Wav.Decode;

/// <summary>
///     WAV 文件解码器，将 WAV 音频格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     WAV 是 Microsoft/IBM 的标准音频格式，基于 RIFF 容器，广泛用于游戏和多媒体应用。
///     支持 PCM、IEEE Float、ADPCM 等多种编码格式。
/// </remarks>
public ref struct WavDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="WavDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">WAV 二进制数据。</param>
    public WavDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 WAV 文件。
    /// </summary>
    /// <returns>WAV 音频数据。</returns>
    public WavAudioData Decode()
    {
        var riffTag = _buffer.ReadString(4);

        if (riffTag != WavConstants.RiffTag)
        {
            throw new InvalidDataException($"WAV 文件签名无效，期望 \"RIFF\"，实际 \"{riffTag}\"");
        }

        var fileSize = _buffer.ReadU32LE();
        var waveTag = _buffer.ReadString(4);

        if (waveTag != WavConstants.WaveTag)
        {
            throw new InvalidDataException($"WAV 格式标识无效，期望 \"WAVE\"，实际 \"{waveTag}\"");
        }

        WavFormatTag formatTag = 0;
        ushort channels = 0;
        uint sampleRate = 0;
        uint byteRate = 0;
        ushort blockAlign = 0;
        ushort bitsPerSample = 0;
        byte[] sampleData = [];

        while (!_buffer.IsEnd)
        {
            var chunkId = _buffer.ReadString(4);
            var chunkSize = _buffer.ReadU32LE();
            var chunkEnd = _buffer.Position + (int)chunkSize;

            if (chunkId == "fmt ")
            {
                formatTag = (WavFormatTag)_buffer.ReadU16LE();
                channels = _buffer.ReadU16LE();
                sampleRate = _buffer.ReadU32LE();
                byteRate = _buffer.ReadU32LE();
                blockAlign = _buffer.ReadU16LE();
                bitsPerSample = _buffer.ReadU16LE();
            }
            else if (chunkId == "data")
            {
                sampleData = _buffer.ReadBytes((int)chunkSize).ToArray();
                break;
            }

            _buffer.Position = chunkEnd;

            if (chunkSize % 2 != 0)
            {
                _buffer.Advance(1);
            }
        }

        return new WavAudioData
        {
            FormatTag = formatTag,
            Channels = channels,
            SampleRate = sampleRate,
            ByteRate = byteRate,
            BlockAlign = blockAlign,
            BitsPerSample = bitsPerSample,
            SampleData = sampleData
        };
    }

    /// <summary>
    ///     仅解码 WAV 文件头信息。
    /// </summary>
    public (WavFormatTag FormatTag, ushort Channels, uint SampleRate, ushort BitsPerSample) DecodeHeader()
    {
        var riffTag = _buffer.ReadString(4);

        if (riffTag != WavConstants.RiffTag)
        {
            throw new InvalidDataException($"WAV 文件签名无效，期望 \"RIFF\"，实际 \"{riffTag}\"");
        }

        _buffer.Advance(4);
        var waveTag = _buffer.ReadString(4);

        if (waveTag != WavConstants.WaveTag)
        {
            throw new InvalidDataException($"WAV 格式标识无效，期望 \"WAVE\"，实际 \"{waveTag}\"");
        }

        while (!_buffer.IsEnd)
        {
            var chunkId = _buffer.ReadString(4);
            var chunkSize = _buffer.ReadU32LE();
            var chunkEnd = _buffer.Position + (int)chunkSize;

            if (chunkId == "fmt ")
            {
                var formatTag = (WavFormatTag)_buffer.ReadU16LE();
                var channels = _buffer.ReadU16LE();
                var sampleRate = _buffer.ReadU32LE();
                _buffer.Advance(4);
                _buffer.Advance(2);
                var bitsPerSample = _buffer.ReadU16LE();

                return (formatTag, channels, sampleRate, bitsPerSample);
            }

            _buffer.Position = chunkEnd;

            if (chunkSize % 2 != 0)
            {
                _buffer.Advance(1);
            }
        }

        throw new InvalidDataException("WAV 文件缺少 fmt 块");
    }
}
