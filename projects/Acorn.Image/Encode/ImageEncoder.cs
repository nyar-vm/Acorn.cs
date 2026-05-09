using Acorn.Bmp.Data;
using Acorn.Bmp.Encode;
using Acorn.Gif.Data;
using Acorn.Gif.Encode;
using Acorn.Image.Data;
using Acorn.Jpeg.Data;
using Acorn.Jpeg.Encode;
using Acorn.Png.Data;
using Acorn.Png.Encode;
using Acorn.Tga.Data;
using Acorn.Tga.Encode;

namespace Acorn.Image.Encode;

/// <summary>
///     统一图像编码门面，提供各格式的编码能力
/// </summary>
public static class ImageEncoder
{
    /// <summary>
    ///     编码为指定格式的二进制数据
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <param name="format">目标格式</param>
    /// <param name="quality">编码质量（1-100），仅对 JPEG 等有损格式有效</param>
    /// <returns>编码后的二进制数据</returns>
    public static byte[] Encode(RgbaImage image, ImageFormat format, int quality = 85)
    {
        return format switch
        {
            ImageFormat.Png => EncodePng(image),
            ImageFormat.Bmp => EncodeBmp(image),
            ImageFormat.Jpeg => EncodeJpeg(image, quality),
            ImageFormat.Tga => EncodeTga(image),
            ImageFormat.Gif => EncodeGif(image),
            _ => throw new NotSupportedException($"不支持的编码格式：{format}")
        };
    }

    /// <summary>
    ///     编码为 PNG 格式的二进制数据
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <returns>PNG 二进制数据</returns>
    public static byte[] EncodePng(RgbaImage image)
    {
        var rawPixelData = BuildPngRawPixelData(image);
        var pngData = new PngImageData
        {
            Width = image.Width,
            Height = image.Height,
            BitDepth = 8,
            ColorType = PngColorType.TruecolorAlpha,
            CompressionMethod = PngCompressionMethod.Deflate,
            FilterMethod = PngFilterMethod.Adaptive,
            InterlaceMethod = PngInterlaceMethod.None,
            RawPixelData = rawPixelData
        };

        var encoder = new PngEncoder();
        return encoder.Encode(pngData);
    }

    /// <summary>
    ///     编码为 BMP 格式的二进制数据
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <returns>BMP 二进制数据</returns>
    public static byte[] EncodeBmp(RgbaImage image)
    {
        var stride = ((image.Width * 32 + 31) / 32) * 4;
        var pixelData = new byte[stride * image.Height];

        for (var y = 0; y < image.Height; y++)
        {
            var dstY = image.Height - 1 - y;
            for (var x = 0; x < image.Width; x++)
            {
                var srcIdx = (y * image.Width + x) * 4;
                var dstIdx = dstY * stride + x * 4;
                pixelData[dstIdx] = image.RgbaData[srcIdx + 2];
                pixelData[dstIdx + 1] = image.RgbaData[srcIdx + 1];
                pixelData[dstIdx + 2] = image.RgbaData[srcIdx];
                pixelData[dstIdx + 3] = image.RgbaData[srcIdx + 3];
            }
        }

        var bmpData = new BmpImageData
        {
            Width = image.Width,
            Height = image.Height,
            BitsPerPixel = 32,
            Compression = BmpCompression.None,
            ImageSize = (uint)pixelData.Length,
            PixelData = pixelData
        };

        var encoder = new BmpEncoder();
        return encoder.Encode(bmpData);
    }

    /// <summary>
    ///     编码为 JPEG 格式的二进制数据
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <param name="quality">编码质量（1-100）</param>
    /// <returns>JPEG 二进制数据</returns>
    public static byte[] EncodeJpeg(RgbaImage image, int quality = 85)
    {
        var jpegData = new JpegImageData
        {
            Width = image.Width,
            Height = image.Height,
            Precision = 8,
            ColorSpace = JpegColorSpace.YCbCr,
            PixelData = image.RgbaData,
            IsProgressive = false
        };

        var encoder = new JpegEncoder { Quality = quality };

        try
        {
            return encoder.Encode(jpegData);
        }
        catch (NotImplementedException)
        {
            throw new NotSupportedException("JPEG 编码尚未实现，请使用其他格式");
        }
    }

    /// <summary>
    ///     编码为 TGA 格式的二进制数据
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <returns>TGA 二进制数据</returns>
    public static byte[] EncodeTga(RgbaImage image)
    {
        var hasAlpha = HasAlphaChannel(image.RgbaData);
        var pixelDepth = hasAlpha ? 32 : 24;
        var bytesPerPixel = pixelDepth / 8;
        var pixelData = new byte[image.Width * image.Height * bytesPerPixel];

        for (var y = 0; y < image.Height; y++)
        {
            var srcY = image.Height - 1 - y;
            for (var x = 0; x < image.Width; x++)
            {
                var srcIdx = (y * image.Width + x) * 4;
                var dstIdx = (srcY * image.Width + x) * bytesPerPixel;

                pixelData[dstIdx] = image.RgbaData[srcIdx + 2];
                pixelData[dstIdx + 1] = image.RgbaData[srcIdx + 1];
                pixelData[dstIdx + 2] = image.RgbaData[srcIdx];

                if (hasAlpha)
                {
                    pixelData[dstIdx + 3] = image.RgbaData[srcIdx + 3];
                }
            }
        }

        var tgaData = new TgaImageData
        {
            Width = image.Width,
            Height = image.Height,
            PixelDepth = pixelDepth,
            ImageType = TgaImageType.UncompressedTruecolor,
            IsTopDown = false,
            PixelData = pixelData
        };

        var encoder = new TgaEncoder();
        return encoder.Encode(tgaData);
    }

    /// <summary>
    ///     编码为 GIF 格式的二进制数据（单帧）
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <returns>GIF 二进制数据</returns>
    public static byte[] EncodeGif(RgbaImage image)
    {
        var frame = new GifFrame
        {
            Width = image.Width,
            Height = image.Height,
            DelayCentiseconds = 0,
            DisposalMethod = GifDisposalMethod.None,
            RgbaData = image.RgbaData
        };

        var encoder = new GifEncoder();
        return encoder.Encode([frame]);
    }

    #region 私有方法

    private static byte[] BuildPngRawPixelData(RgbaImage image)
    {
        var bpp = 4;
        var stride = image.Width * bpp + 1;
        var rawPixelData = new byte[stride * image.Height];

        for (var y = 0; y < image.Height; y++)
        {
            rawPixelData[y * stride] = 0;

            for (var x = 0; x < image.Width; x++)
            {
                var srcIdx = (y * image.Width + x) * 4;
                var dstIdx = y * stride + 1 + x * bpp;

                rawPixelData[dstIdx] = image.RgbaData[srcIdx];
                rawPixelData[dstIdx + 1] = image.RgbaData[srcIdx + 1];
                rawPixelData[dstIdx + 2] = image.RgbaData[srcIdx + 2];
                rawPixelData[dstIdx + 3] = image.RgbaData[srcIdx + 3];
            }
        }

        return rawPixelData;
    }

    private static bool HasAlphaChannel(byte[] rgbaData)
    {
        for (var i = 3; i < rgbaData.Length; i += 4)
        {
            if (rgbaData[i] < 255) return true;
        }
        return false;
    }

    #endregion
}
