using Acorn.Frame;
using Acorn.Flac.Data;

namespace Acorn.Flac.Scanner;

/// <summary>
///     FLAC 扫描器。
/// </summary>
public ref struct FlacScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="FlacScanner" /> 结构的新实例。
    /// </summary>
    public FlacScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     扫描 FLAC 文件头。
    /// </summary>
    public FlacScanHeader ScanHeader()
    {
        if (_buffer.Length < FlacConstants.StreamMarkerLength)
        {
            return new FlacScanHeader();
        }

        if (!_buffer.MatchMagic(FlacConstants.StreamMarker))
        {
            return new FlacScanHeader();
        }

        _buffer.ConsumeMagic(FlacConstants.StreamMarker);

        if (_buffer.Remaining < 4)
        {
            return new FlacScanHeader { IsFlac = true };
        }

        var header = _buffer.ReadU32BE();
        var isLast = (header & 0x80000000) != 0;
        var blockType = (FlacMetadataBlockType)((header >> 24) & 0x7F);
        var blockSize = (int)(header & 0x00FFFFFF);

        if (blockType == FlacMetadataBlockType.StreamInfo && _buffer.Remaining >= 34)
        {
            var minBlockSize = _buffer.ReadU16BE();
            var maxBlockSize = _buffer.ReadU16BE();
            _buffer.Advance(6);

            var sampleRateBits = _buffer.ReadU32BE();
            var sampleRate = (int)((sampleRateBits >> 12) & 0xFFFFF);
            var channels = (int)(((sampleRateBits >> 9) & 0x07) + 1);
            var bitsPerSample = (int)(((sampleRateBits >> 4) & 0x1F) + 1);

            return new FlacScanHeader
            {
                IsFlac = true,
                SampleRate = sampleRate,
                Channels = channels,
                BitsPerSample = bitsPerSample
            };
        }

        return new FlacScanHeader { IsFlac = true };
    }

    /// <summary>
    ///     是否为 FLAC 格式。
    /// </summary>
    public bool IsFlac()
    {
        return _buffer.Length >= 4 && _buffer.MatchMagic(FlacConstants.StreamMarker);
    }
}

/// <summary>
///     FLAC 扫描头部信息。
/// </summary>
public sealed class FlacScanHeader
{
    /// <summary>
    ///     是否为 FLAC 格式。
    /// </summary>
    public bool IsFlac { get; init; }

    /// <summary>
    ///     采样率。
    /// </summary>
    public int SampleRate { get; init; }

    /// <summary>
    ///     通道数。
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    ///     每样本位数。
    /// </summary>
    public int BitsPerSample { get; init; }
}
