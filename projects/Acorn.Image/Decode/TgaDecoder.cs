using Acorn.Image.Data;

namespace Acorn.Image.Decode;

/// <summary>
///     TGA (Targa) 图像解码器——纯 C# 实现，无第三方依赖
/// </summary>
/// <remarks>
///     支持 TGA 格式的未压缩和 RLE 压缩像素数据，
///     支持 8/16/24/32 位色深、调色板、上下翻转。
///     TGA 广泛用于游戏开发中的纹理资源。
/// </remarks>
public ref struct TgaDecoder
{
    private ReadOnlySpan<byte> _data;
    private int _position;

    /// <summary>
    ///     初始化 TGA 解码器
    /// </summary>
    /// <param name="data">TGA 二进制数据</param>
    public TgaDecoder(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    /// <summary>
    ///     解码 TGA 图像为 RGBA 像素数据
    /// </summary>
    public RgbaImage Decode()
    {
        var idLength = _data[_position++];
        var colorMapType = _data[_position++];
        var imageType = _data[_position++];
        var colorMapFirstEntry = ReadU16();
        var colorMapLength = ReadU16();
        var colorMapEntrySize = _data[_position++];
        var xOrigin = ReadU16();
        var yOrigin = ReadU16();
        var width = ReadU16();
        var height = ReadU16();
        var pixelDepth = _data[_position++];
        var imageDescriptor = _data[_position++];

        _position += idLength;

        var hasColorMap = colorMapType == 1;
        var colorMap = ReadColorMap(hasColorMap, colorMapLength, colorMapEntrySize);

        var isRle = imageType is 9 or 10 or 11;
        var isTopDown = (imageDescriptor & 0x20) != 0;
        var bytesPerPixel = pixelDepth / 8;

        var pixelData = isRle
            ? DecodeRlePixels(width, height, bytesPerPixel)
            : DecodeRawPixels(width, height, bytesPerPixel);

        var rgbaData = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            var srcY = isTopDown ? y : height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var srcIdx = (srcY * width + x) * bytesPerPixel;
                var dstIdx = (y * width + x) * 4;

                switch (pixelDepth)
                {
                    case 32:
                        rgbaData[dstIdx] = pixelData[srcIdx + 2];
                        rgbaData[dstIdx + 1] = pixelData[srcIdx + 1];
                        rgbaData[dstIdx + 2] = pixelData[srcIdx];
                        rgbaData[dstIdx + 3] = pixelData[srcIdx + 3];
                        break;
                    case 24:
                        rgbaData[dstIdx] = pixelData[srcIdx + 2];
                        rgbaData[dstIdx + 1] = pixelData[srcIdx + 1];
                        rgbaData[dstIdx + 2] = pixelData[srcIdx];
                        rgbaData[dstIdx + 3] = 255;
                        break;
                    case 16:
                        var pixel16 = pixelData[srcIdx] | (pixelData[srcIdx + 1] << 8);
                        rgbaData[dstIdx] = (byte)(((pixel16 >> 10) & 0x1F) * 255 / 31);
                        rgbaData[dstIdx + 1] = (byte)(((pixel16 >> 5) & 0x1F) * 255 / 31);
                        rgbaData[dstIdx + 2] = (byte)((pixel16 & 0x1F) * 255 / 31);
                        rgbaData[dstIdx + 3] = (pixel16 & 0x8000) != 0 ? (byte)255 : (byte)0;
                        break;
                    case 8:
                        if (hasColorMap && pixelData[srcIdx] < colorMap.Length / 4)
                        {
                            var palIdx = pixelData[srcIdx] * 4;
                            rgbaData[dstIdx] = colorMap[palIdx];
                            rgbaData[dstIdx + 1] = colorMap[palIdx + 1];
                            rgbaData[dstIdx + 2] = colorMap[palIdx + 2];
                            rgbaData[dstIdx + 3] = colorMap[palIdx + 3];
                        }
                        else
                        {
                            var gray = pixelData[srcIdx];
                            rgbaData[dstIdx] = gray;
                            rgbaData[dstIdx + 1] = gray;
                            rgbaData[dstIdx + 2] = gray;
                            rgbaData[dstIdx + 3] = 255;
                        }
                        break;
                }
            }
        }

        return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
    }

    #region 私有方法

    private byte[] ReadColorMap(bool hasColorMap, int length, int entrySize)
    {
        if (!hasColorMap || length == 0) return [];

        var entryBytes = (entrySize + 7) / 8;
        var colorMap = new byte[length * 4];

        for (var i = 0; i < length; i++)
        {
            var r = (byte)0;
            var g = (byte)0;
            var b = (byte)0;
            var a = (byte)255;

            switch (entrySize)
            {
                case 32:
                    b = _data[_position++];
                    g = _data[_position++];
                    r = _data[_position++];
                    a = _data[_position++];
                    break;
                case 24:
                    b = _data[_position++];
                    g = _data[_position++];
                    r = _data[_position++];
                    break;
                case 16:
                    var pixel = _data[_position] | (_data[_position + 1] << 8);
                    _position += 2;
                    r = (byte)(((pixel >> 10) & 0x1F) * 255 / 31);
                    g = (byte)(((pixel >> 5) & 0x1F) * 255 / 31);
                    b = (byte)((pixel & 0x1F) * 255 / 31);
                    a = (pixel & 0x8000) != 0 ? (byte)255 : (byte)0;
                    break;
                default:
                    _position += entryBytes;
                    break;
            }

            var offset = i * 4;
            colorMap[offset] = r;
            colorMap[offset + 1] = g;
            colorMap[offset + 2] = b;
            colorMap[offset + 3] = a;
        }

        return colorMap;
    }

    private byte[] DecodeRawPixels(int width, int height, int bytesPerPixel)
    {
        var totalBytes = width * height * bytesPerPixel;
        var pixels = new byte[totalBytes];
        var count = Math.Min(totalBytes, _data.Length - _position);
        _data.Slice(_position, count).CopyTo(pixels);
        _position += totalBytes;
        return pixels;
    }

    private byte[] DecodeRlePixels(int width, int height, int bytesPerPixel)
    {
        var totalPixels = width * height;
        var pixels = new byte[totalPixels * bytesPerPixel];
        var pixelIdx = 0;

        while (pixelIdx < totalPixels)
        {
            var header = _data[_position++];
            var isRun = (header & 0x80) != 0;
            var count = (header & 0x7F) + 1;

            if (isRun)
            {
                var runStart = pixelIdx * bytesPerPixel;
                for (var b = 0; b < bytesPerPixel; b++)
                {
                    var val = _data[_position++];
                    for (var p = 0; p < count; p++)
                    {
                        pixels[runStart + p * bytesPerPixel + b] = val;
                    }
                }
            }
            else
            {
                var copyBytes = count * bytesPerPixel;
                var srcStart = _position;
                var dstStart = pixelIdx * bytesPerPixel;
                for (var i = 0; i < copyBytes; i++)
                {
                    pixels[dstStart + i] = _data[srcStart + i];
                }
                _position += copyBytes;
            }

            pixelIdx += count;
        }

        return pixels;
    }

    private ushort ReadU16()
    {
        var value = (ushort)(_data[_position] | (_data[_position + 1] << 8));
        _position += 2;
        return value;
    }

    #endregion
}
