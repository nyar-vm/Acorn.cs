using Acorn.Frame;
using Acorn.Bmp.Data;

namespace Acorn.Bmp.Decode;

/// <summary>
///     BMP 文件解码器，将 BMP 图像格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     BMP 是 Microsoft 的标准位图格式，广泛用于 Windows 应用和游戏开发。
///     支持 1/4/8/16/24/32 位色深，RLE 压缩和位域掩码。
/// </remarks>
public ref struct BmpDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="BmpDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">BMP 二进制数据。</param>
    public BmpDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 BMP 文件。
    /// </summary>
    /// <returns>BMP 图像数据。</returns>
    public BmpImageData Decode()
    {
        var magic = _buffer.ReadString(2);

        if (magic != BmpConstants.MagicTag)
        {
            throw new InvalidDataException($"BMP 文件签名无效，期望 \"BM\"，实际 \"{magic}\"");
        }

        var fileSize = _buffer.ReadU32LE();
        _buffer.Advance(4);
        _buffer.Advance(2);
        var dataOffset = _buffer.ReadU32LE();

        var headerSize = _buffer.ReadU32LE();

        if (headerSize < BmpConstants.InfoHeaderSize)
        {
            throw new InvalidDataException($"BMP 信息头大小无效，期望 >= {BmpConstants.InfoHeaderSize}，实际 {headerSize}");
        }

        var width = _buffer.ReadI32LE();
        var height = _buffer.ReadI32LE();
        var planes = _buffer.ReadU16LE();
        var bitsPerPixel = _buffer.ReadU16LE();
        var compression = (BmpCompression)_buffer.ReadU32LE();
        var imageSize = _buffer.ReadU32LE();
        var xPelsPerMeter = _buffer.ReadI32LE();
        var yPelsPerMeter = _buffer.ReadI32LE();
        var colorsUsed = _buffer.ReadU32LE();
        var colorsImportant = _buffer.ReadU32LE();

        var palette = ReadPalette(bitsPerPixel, colorsUsed);

        _buffer.Position = (int)dataOffset;

        var actualImageSize = imageSize > 0 ? (int)imageSize : ComputeImageSize(width, height, bitsPerPixel);
        var pixelData = _buffer.ReadBytes(actualImageSize).ToArray();

        return new BmpImageData
        {
            Width = width,
            Height = height,
            BitsPerPixel = bitsPerPixel,
            Compression = compression,
            ImageSize = imageSize,
            XPelsPerMeter = xPelsPerMeter,
            YPelsPerMeter = yPelsPerMeter,
            ColorsUsed = colorsUsed,
            ColorsImportant = colorsImportant,
            Palette = palette,
            PixelData = pixelData
        };
    }

    /// <summary>
    ///     仅解码 BMP 文件头信息。
    /// </summary>
    public (int Width, int Height, ushort BitsPerPixel, BmpCompression Compression) DecodeHeader()
    {
        var magic = _buffer.ReadString(2);

        if (magic != BmpConstants.MagicTag)
        {
            throw new InvalidDataException($"BMP 文件签名无效，期望 \"BM\"，实际 \"{magic}\"");
        }

        _buffer.Advance(8);
        _buffer.Advance(2);
        _buffer.Advance(4);

        var headerSize = _buffer.ReadU32LE();

        if (headerSize < BmpConstants.InfoHeaderSize)
        {
            throw new InvalidDataException($"BMP 信息头大小无效，期望 >= {BmpConstants.InfoHeaderSize}，实际 {headerSize}");
        }

        var width = _buffer.ReadI32LE();
        var height = _buffer.ReadI32LE();
        _buffer.Advance(2);
        var bitsPerPixel = _buffer.ReadU16LE();
        var compression = (BmpCompression)_buffer.ReadU32LE();

        return (width, height, bitsPerPixel, compression);
    }

    #region 私有解析方法

    private uint[] ReadPalette(ushort bitsPerPixel, uint colorsUsed)
    {
        if (bitsPerPixel > 8)
        {
            return [];
        }

        var maxColors = 1 << bitsPerPixel;
        var count = colorsUsed > 0 ? (int)Math.Min(colorsUsed, maxColors) : maxColors;
        var palette = new uint[count];

        for (var i = 0; i < count; i++)
        {
            var b = _buffer.ReadU8();
            var g = _buffer.ReadU8();
            var r = _buffer.ReadU8();
            var reserved = _buffer.ReadU8();
            palette[i] = (uint)((reserved << 24) | (r << 16) | (g << 8) | b);
        }

        return palette;
    }

    private static int ComputeImageSize(int width, int height, ushort bitsPerPixel)
    {
        var absHeight = Math.Abs(height);
        var stride = ((width * bitsPerPixel + 31) / 32) * 4;
        return stride * absHeight;
    }

    #endregion
}
