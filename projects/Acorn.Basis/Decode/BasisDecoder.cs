using Acorn.Frame;
using Acorn.Basis.Data;

namespace Acorn.Basis.Decode;

/// <summary>
///     Basis/KTX2 解码器。
/// </summary>
public ref struct BasisDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="BasisDecoder" /> 结构的新实例。
    /// </summary>
    public BasisDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 Basis 文件。
    /// </summary>
    public BasisFileData Decode()
    {
        if (_buffer.MatchMagic(BasisConstants.Ktx2Magic))
        {
            return DecodeKtx2();
        }

        return DecodeBasis();
    }

    private BasisFileData DecodeKtx2()
    {
        _buffer.ConsumeMagic(BasisConstants.Ktx2Magic);

        var vkFormat = _buffer.ReadU32LE();
        var typeSize = _buffer.ReadU32LE();
        var pixelWidth = _buffer.ReadU32LE();
        var pixelHeight = _buffer.ReadU32LE();
        _buffer.Advance(4);
        var layerCount = _buffer.ReadU32LE();
        var faceCount = _buffer.ReadU32LE();
        var levelCount = _buffer.ReadU32LE();
        _buffer.Advance(36);

        return new BasisFileData
        {
            Width = (int)pixelWidth,
            Height = (int)pixelHeight,
            MipLevels = (int)levelCount,
            ImageCount = (int)(layerCount * faceCount)
        };
    }

    private BasisFileData DecodeBasis()
    {
        _buffer.ConsumeMagic(BasisConstants.BasisMagic);

        var version = _buffer.ReadU32LE();
        var headerSize = _buffer.ReadU32LE();
        var headerCRC16 = _buffer.ReadU16LE();
        _buffer.Advance(6);
        var imageCount = _buffer.ReadU32LE();
        var format = (BasisTextureFormat)_buffer.ReadU32LE();
        var flags = _buffer.ReadU16LE();
        _buffer.Advance(8);
        var pixelWidth = _buffer.ReadU32LE();
        var pixelHeight = _buffer.ReadU32LE();
        _buffer.Advance(8);
        var totalImages = _buffer.ReadU32LE();
        var mipLevels = _buffer.ReadU32LE();

        return new BasisFileData
        {
            Width = (int)pixelWidth,
            Height = (int)pixelHeight,
            MipLevels = (int)mipLevels,
            Format = format,
            IsSRGB = (flags & 0x01) != 0,
            ImageCount = (int)imageCount
        };
    }
}
