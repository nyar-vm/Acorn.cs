using Acorn.Frame;
using Acorn.Flac.Data;

namespace Acorn.Flac.Decode;

/// <summary>
///     FLAC 解码器。
/// </summary>
public ref struct FlacDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="FlacDecoder" /> 结构的新实例。
    /// </summary>
    public FlacDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 FLAC 文件。
    /// </summary>
    public FlacAudioData Decode()
    {
        if (!_buffer.MatchMagic(FlacConstants.StreamMarker))
        {
            throw new InvalidDataException("FLAC 流标记不匹配");
        }

        _buffer.ConsumeMagic(FlacConstants.StreamMarker);

        FlacAudioData? streamInfo = null;

        while (!_buffer.IsEnd)
        {
            if (_buffer.Remaining < 4) break;

            var header = _buffer.ReadU32BE();
            var isLast = (header & 0x80000000) != 0;
            var blockType = (FlacMetadataBlockType)((header >> 24) & 0x7F);
            var blockSize = (int)(header & 0x00FFFFFF);

            if (blockType == FlacMetadataBlockType.StreamInfo)
            {
                if (blockSize < 34)
                {
                    _buffer.Advance(blockSize);
                }
                else
                {
                    streamInfo = FlacAudioData.ParseStreamInfo(ref _buffer);
                }
            }
            else
            {
                _buffer.Advance(blockSize);
            }

            if (isLast) break;
        }

        return streamInfo ?? new FlacAudioData();
    }

}
