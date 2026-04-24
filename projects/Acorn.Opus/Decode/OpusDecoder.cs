using Acorn.Frame;
using Acorn.Opus.Data;

namespace Acorn.Opus.Decode;

/// <summary>
///     Opus 解码器。
/// </summary>
public ref struct OpusDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="OpusDecoder" /> 结构的新实例。
    /// </summary>
    public OpusDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 Opus 头部。
    /// </summary>
    public OpusAudioData Decode()
    {
        var id = _buffer.ReadBytes(8).ToArray();

        if (!id.AsSpan().SequenceEqual(OpusConstants.OpusHead))
        {
            throw new InvalidDataException("Opus 头部标识不匹配");
        }

        var version = _buffer.ReadU8();
        var channels = _buffer.ReadU8();
        var preSkip = _buffer.ReadU16LE();
        var sampleRate = _buffer.ReadU32LE();
        var outputGain = _buffer.ReadI16LE();
        var channelMappingFamily = _buffer.ReadU8();

        return new OpusAudioData
        {
            Channels = channels,
            SampleRate = sampleRate,
            PreSkip = preSkip,
            OutputGain = outputGain,
            ChannelMappingFamily = channelMappingFamily
        };
    }
}
