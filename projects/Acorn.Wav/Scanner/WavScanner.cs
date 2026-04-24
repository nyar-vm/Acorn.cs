using Acorn.Frame;
using Acorn.Wav.Data;

namespace Acorn.Wav.Scanner;

/// <summary>
///     WAV 文件扫描器，基于 <see cref="ByteBuffer" /> 提供对 WAV 音频文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     WAV 文件格式基于 RIFF 容器，由 RIFF 头、fmt 块和 data 块组成。
///     扫描器只读取格式信息，不做完整的采样数据解码，以实现快速探查。
/// </remarks>
public ref struct WavScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="WavScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 WAV 字节数据。</param>
    public WavScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前扫描位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     数据总长度。
    /// </summary>
    public int Length => _buffer.Length;

    /// <summary>
    ///     是否已到达数据末尾。
    /// </summary>
    public bool IsEndOfData => _buffer.IsEnd;

    /// <summary>
    ///     扫描 WAV 文件头，提取基本音频信息。
    /// </summary>
    /// <returns>WAV 文件头信息。</returns>
    public WavScanHeader ScanHeader()
    {
        if (_buffer.Length < 12)
        {
            throw new InvalidDataException("WAV 文件数据过短，无法读取 RIFF 头");
        }

        if (!_buffer.MatchMagic(WavConstants.RiffMagic))
        {
            throw new InvalidDataException("WAV 文件魔数不匹配");
        }

        _buffer.ConsumeMagic(WavConstants.RiffMagic);
        var fileSize = _buffer.ReadU32LE();

        if (!_buffer.MatchMagic(WavConstants.WaveMagic))
        {
            throw new InvalidDataException("WAV 格式标识不匹配");
        }

        _buffer.ConsumeMagic(WavConstants.WaveMagic);

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
                var byteRate = _buffer.ReadU32LE();
                var blockAlign = _buffer.ReadU16LE();
                var bitsPerSample = _buffer.ReadU16LE();

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

            _buffer.Position = chunkEnd;

            if (chunkSize % 2 != 0)
            {
                _buffer.Advance(1);
            }
        }

        throw new InvalidDataException("WAV 文件缺少 fmt 块");
    }

    /// <summary>
    ///     快速判断数据是否为 WAV 格式。
    /// </summary>
    public bool IsWav()
    {
        if (_buffer.Length < 12)
        {
            return false;
        }

        return _buffer.MatchMagic(WavConstants.RiffMagic);
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
