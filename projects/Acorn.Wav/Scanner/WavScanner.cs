using Acorn.Frame;
using Acorn.Wav.Data;

namespace Acorn.Wav.Scanner;

/// <summary>
///     WAV 文件扫描器，基于 <see cref="SpanScanner" /> 提供对 WAV 音频文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     WAV 文件格式基于 RIFF 容器，由 RIFF 头、fmt 块和 data 块组成。
///     扫描器只读取格式信息，不做完整的采样数据解码，以实现快速探查。
/// </remarks>
public ref struct WavScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="WavScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 WAV 字节数据。</param>
    public WavScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 WAV 文件头，提取基本音频信息。
    /// </summary>
    /// <returns>WAV 文件头信息。</returns>
    public WavScanHeader ScanHeader()
    {
        if (_scanner.Length < 12)
        {
            throw new InvalidDataException("WAV 文件数据过短，无法读取 RIFF 头");
        }

        if (!_scanner.MatchMagic(WavConstants.RiffMagic))
        {
            throw new InvalidDataException("WAV 文件魔数不匹配");
        }

        _scanner.ConsumeMagic(WavConstants.RiffMagic);
        var fileSize = _scanner.Buffer.ReadU32LE();

        if (!_scanner.MatchMagic(WavConstants.WaveMagic))
        {
            throw new InvalidDataException("WAV 格式标识不匹配");
        }

        _scanner.ConsumeMagic(WavConstants.WaveMagic);

        while (!_scanner.IsEnd)
        {
            var chunkId = _scanner.Buffer.ReadString(4);
            var chunkSize = _scanner.Buffer.ReadU32LE();
            var chunkEnd = _scanner.Position + (int)chunkSize;

            if (chunkId == "fmt ")
            {
                var formatTag = (WavFormatTag)_scanner.Buffer.ReadU16LE();
                var channels = _scanner.Buffer.ReadU16LE();
                var sampleRate = _scanner.Buffer.ReadU32LE();
                var byteRate = _scanner.Buffer.ReadU32LE();
                var blockAlign = _scanner.Buffer.ReadU16LE();
                var bitsPerSample = _scanner.Buffer.ReadU16LE();

                return new WavScanHeader
                {
                    FormatTag = formatTag,
                    Channels = channels,
                    SampleRate = sampleRate,
                    ByteRate = byteRate,
                    BlockAlign = blockAlign,
                    BitsPerSample = bitsPerSample
                };
            }

            _scanner.Position = chunkEnd;

            if (chunkSize % 2 != 0)
            {
                _scanner.Advance(1);
            }
        }

        throw new InvalidDataException("WAV 文件缺少 fmt 块");
    }

    /// <summary>
    ///     快速判断数据是否为 WAV 格式。
    /// </summary>
    public bool IsWav()
    {
        if (_scanner.Length < 12)
        {
            return false;
        }

        return _scanner.MatchMagic(WavConstants.RiffMagic);
    }
}

/// <summary>
///     WAV 扫描头部信息。
/// </summary>
public sealed class WavScanHeader
{
    /// <summary>
    ///     音频格式标签。
    /// </summary>
    public WavFormatTag FormatTag { get; init; }

    /// <summary>
    ///     通道数量。
    /// </summary>
    public ushort Channels { get; init; }

    /// <summary>
    ///     采样率。
    /// </summary>
    public uint SampleRate { get; init; }

    /// <summary>
    ///     字节率。
    /// </summary>
    public uint ByteRate { get; init; }

    /// <summary>
    ///     块对齐。
    /// </summary>
    public ushort BlockAlign { get; init; }

    /// <summary>
    ///     每样本位数。
    /// </summary>
    public ushort BitsPerSample { get; init; }

    /// <summary>
    ///     格式名称。
    /// </summary>
    public string FormatName => FormatTag switch
    {
        WavFormatTag.Pcm => "PCM",
        WavFormatTag.IeeeFloat => "IEEE Float",
        WavFormatTag.ALaw => "A-Law",
        WavFormatTag.MuLaw => "μ-Law",
        WavFormatTag.Adpcm => "ADPCM",
        WavFormatTag.ImaAdpcm => "IMA ADPCM",
        WavFormatTag.Extensible => "Extensible",
        _ => $"未知(0x{(ushort)FormatTag:X4})"
    };
}
