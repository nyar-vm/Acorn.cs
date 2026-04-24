using Acorn.Frame;
using Acorn.Ogg.Data;

namespace Acorn.Ogg.Scanner;

/// <summary>
///     OGG 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 OGG 音频文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     OGG 文件格式由一系列页面组成，每个页面以 "OggS" 捕获模式开头。
///     扫描器只读取第一页的标识头信息，不做完整解码，以实现快速探查。
/// </remarks>
public ref struct OggScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="OggScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 OGG 字节数据。</param>
    public OggScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 OGG 文件头，提取基本音频信息。
    /// </summary>
    /// <returns>OGG 文件头信息。</returns>
    public OggScanHeader ScanHeader()
    {
        if (_scanner.Length < OggConstants.PageHeaderSize)
        {
            throw new InvalidDataException("OGG 文件数据过短，无法读取页面头");
        }

        if (!_scanner.MatchMagic(OggConstants.CapturePattern))
        {
            throw new InvalidDataException("OGG 文件捕获模式不匹配");
        }

        _scanner.ConsumeMagic(OggConstants.CapturePattern);

        var version = _scanner.Buffer.ReadU8();

        if (version != OggConstants.Version)
        {
            throw new InvalidDataException($"OGG 版本号无效，期望 0，实际 {version}");
        }

        var flags = (OggPageFlags)_scanner.Buffer.ReadU8();
        _scanner.Advance(8);
        var serialNumber = _scanner.Buffer.ReadU32LE();
        _scanner.Advance(8);
        var segmentCount = _scanner.Buffer.ReadU8();

        var segmentSizes = new int[segmentCount];
        var totalDataSize = 0;

        for (var i = 0; i < segmentCount; i++)
        {
            segmentSizes[i] = _scanner.Buffer.ReadU8();
            totalDataSize += segmentSizes[i];
        }

        if (_scanner.Buffer.Remaining < totalDataSize)
        {
            throw new InvalidDataException("OGG 页面数据不完整");
        }

        var firstPacketSize = 0;

        for (var i = 0; i < segmentCount; i++)
        {
            firstPacketSize += segmentSizes[i];

            if (segmentSizes[i] < 255)
            {
                break;
            }
        }

        var headerData = _scanner.Buffer.ReadBytes(firstPacketSize).ToArray();

        var (codecType, channels, sampleRate, nominalBitrate) = ParseIdHeader(headerData);

        return new OggScanHeader
        {
            CodecType = codecType,
            Channels = channels,
            SampleRate = sampleRate,
            NominalBitrate = nominalBitrate,
            SerialNumber = serialNumber,
            IsBeginOfStream = (flags & OggPageFlags.BeginOfStream) != 0
        };
    }

    /// <summary>
    ///     快速判断数据是否为 OGG 格式。
    /// </summary>
    public bool IsOgg()
    {
        if (_scanner.Length < 4)
        {
            return false;
        }

        return _scanner.MatchMagic(OggConstants.CapturePattern);
    }

    private static (OggCodecType, int, int, int) ParseIdHeader(byte[] header)
    {
        if (header.Length < 7)
        {
            return (OggCodecType.Unknown, 0, 0, 0);
        }

        if (header[0] == 0x01 && header.AsSpan(1, 6).SequenceEqual(OggConstants.VorbisId))
        {
            if (header.Length < 30)
            {
                return (OggCodecType.Vorbis, 0, 0, 0);
            }

            var channels = header[11];
            var sampleRate = (int)ReadLE32(header, 12);
            var nominalBitrate = (int)ReadLE32(header, 16);

            return (OggCodecType.Vorbis, channels, sampleRate, nominalBitrate);
        }

        if (header.AsSpan(0, 8).SequenceEqual(OggConstants.OpusId))
        {
            if (header.Length < 19)
            {
                return (OggCodecType.Opus, 0, 0, 0);
            }

            var channels = header[9];
            var sampleRate = (int)ReadLE32(header, 12);

            return (OggCodecType.Opus, channels, sampleRate, 0);
        }

        return (OggCodecType.Unknown, 0, 0, 0);
    }

    private static uint ReadLE32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }
}

/// <summary>
///     OGG 扫描头部信息。
/// </summary>
public sealed class OggScanHeader
{
    /// <summary>
    ///     编解码类型。
    /// </summary>
    public OggCodecType CodecType { get; init; }

    /// <summary>
    ///     通道数量。
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    ///     采样率。
    /// </summary>
    public int SampleRate { get; init; }

    /// <summary>
    ///     名义比特率。
    /// </summary>
    public int NominalBitrate { get; init; }

    /// <summary>
    ///     串行号。
    /// </summary>
    public uint SerialNumber { get; init; }

    /// <summary>
    ///     是否为流起始页。
    /// </summary>
    public bool IsBeginOfStream { get; init; }

    /// <summary>
    ///     编解码名称。
    /// </summary>
    public string CodecName => CodecType switch
    {
        OggCodecType.Vorbis => "Vorbis",
        OggCodecType.Opus => "Opus",
        _ => "未知"
    };
}
