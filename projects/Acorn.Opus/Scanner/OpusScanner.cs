using Acorn.Frame;
using Acorn.Opus.Data;

namespace Acorn.Opus.Scanner;

/// <summary>
///     Opus 扫描器。
/// </summary>
public ref struct OpusScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="OpusScanner" /> 结构的新实例。
    /// </summary>
    public OpusScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     扫描 Opus 头部。
    /// </summary>
    public OpusScanHeader ScanHeader()
    {
        if (_buffer.Length < OpusConstants.HeaderSize)
        {
            return new OpusScanHeader();
        }

        if (!_buffer.MatchMagic(OpusConstants.OpusHead))
        {
            return new OpusScanHeader();
        }

        _buffer.ConsumeMagic(OpusConstants.OpusHead);
        var version = _buffer.ReadU8();
        var channels = _buffer.ReadU8();
        var preSkip = _buffer.ReadU16LE();
        var sampleRate = _buffer.ReadU32LE();

        return new OpusScanHeader
        {
            Version = version,
            Channels = channels,
            SampleRate = sampleRate
        };
    }

    /// <summary>
    ///     是否为 Opus 格式。
    /// </summary>
    public bool IsOpus()
    {
        return _buffer.Length >= 8 && _buffer.MatchMagic(OpusConstants.OpusHead);
    }
}

/// <summary>
///     Opus 扫描头部信息。
/// </summary>
public sealed class OpusScanHeader
{
    /// <summary>
    ///     版本号。
    /// </summary>
    public byte Version { get; init; }

    /// <summary>
    ///     通道数。
    /// </summary>
    public byte Channels { get; init; }

    /// <summary>
    ///     采样率。
    /// </summary>
    public uint SampleRate { get; init; }
}
