using Acorn.Frame;
using Acorn.Opus.Data;

namespace Acorn.Opus.Scanner;

/// <summary>
///     Opus 扫描器。
/// </summary>
public ref struct OpusScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="OpusScanner" /> 结构的新实例。
    /// </summary>
    public OpusScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     扫描 Opus 头部。
    /// </summary>
    public OpusScanHeader ScanHeader()
    {
        if (_scanner.Length < OpusConstants.HeaderSize)
        {
            return new OpusScanHeader();
        }

        if (!_scanner.MatchMagic(OpusConstants.OpusHead))
        {
            return new OpusScanHeader();
        }

        _scanner.ConsumeMagic(OpusConstants.OpusHead);
        var version = _scanner.Buffer.ReadU8();
        var channels = _scanner.Buffer.ReadU8();
        var preSkip = _scanner.Buffer.ReadU16LE();
        var sampleRate = _scanner.Buffer.ReadU32LE();

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
        return _scanner.Length >= 8 && _scanner.MatchMagic(OpusConstants.OpusHead);
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
