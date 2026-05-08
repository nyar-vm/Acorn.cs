using Acorn.Bmp.Data;
using Acorn.Bmp.Encode;
using Acorn.Image.Data;
using Acorn.Png.Data;
using Acorn.Png.Encode;

namespace Acorn.Image.Encode;

/// <summary>
///     统一图像编码器——将 RGBA 像素数据编码为各种图像格式
/// </summary>
/// <remarks>
///     支持 PNG、BMP、JPEG、TGA 格式编码。
///     所有格式统一接受 RgbaImage 输入，内部转换为各格式数据模型后编码。
///     纯 C# 实现，无第三方依赖，可在所有 .NET 平台运行（包括 WASM）。
/// </remarks>
public static class ImageEncoder
{
    #region 公开方法

    /// <summary>
    ///     将 RGBA 图像编码为指定格式的二进制数据
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <param name="format">目标图像格式</param>
    /// <returns>编码后的二进制数据</returns>
    public static byte[] Encode(RgbaImage image, ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Png => EncodePng(image),
            ImageFormat.Bmp => EncodeBmp(image),
            ImageFormat.Jpeg => EncodeJpeg(image),
            ImageFormat.Tga => EncodeTga(image),
            _ => throw new ArgumentException($"不支持的图像格式：{format}")
        };
    }

    /// <summary>
    ///     将 RGBA 图像编码为 PNG 格式
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <returns>PNG 二进制数据</returns>
    public static byte[] EncodePng(RgbaImage image)
    {
        var pngData = ConvertToPngData(image);
        var encoder = new PngEncoder();
        return encoder.Encode(pngData);
    }

    /// <summary>
    ///     将 RGBA 图像编码为 BMP 格式
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <returns>BMP 二进制数据</returns>
    public static byte[] EncodeBmp(RgbaImage image)
    {
        var bmpData = ConvertToBmpData(image);
        var encoder = new BmpEncoder();
        return encoder.Encode(bmpData);
    }

    /// <summary>
    ///     将 RGBA 图像编码为 JPEG 格式
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <param name="quality">JPEG 压缩质量（1-100，默认 85）</param>
    /// <returns>JPEG 二进制数据</returns>
    public static byte[] EncodeJpeg(RgbaImage image, int quality = 85)
    {
        var encoder = new JpegEncoder(quality);
        return encoder.Encode(image);
    }

    /// <summary>
    ///     将 RGBA 图像编码为 TGA 格式
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <returns>TGA 二进制数据</returns>
    public static byte[] EncodeTga(RgbaImage image)
    {
        var encoder = new TgaEncoder();
        return encoder.Encode(image);
    }

    #endregion

    #region 格式转换

    private static PngImageData ConvertToPngData(RgbaImage image)
    {
        var width = image.Width;
        var height = image.Height;
        var stride = width * 4 + 1;
        var rawPixelData = new byte[stride * height];

        for (var y = 0; y < height; y++)
        {
            rawPixelData[y * stride] = (byte)PngFilterType.None;

            for (var x = 0; x < width; x++)
            {
                var srcIdx = (y * width + x) * 4;
                var dstIdx = y * stride + 1 + x * 4;

                rawPixelData[dstIdx] = image.RgbaData[srcIdx];
                rawPixelData[dstIdx + 1] = image.RgbaData[srcIdx + 1];
                rawPixelData[dstIdx + 2] = image.RgbaData[srcIdx + 2];
                rawPixelData[dstIdx + 3] = image.RgbaData[srcIdx + 3];
            }
        }

        return new PngImageData
        {
            Width = width,
            Height = height,
            BitDepth = 8,
            ColorType = PngColorType.TruecolorAlpha,
            CompressionMethod = PngCompressionMethod.Deflate,
            FilterMethod = PngFilterMethod.Adaptive,
            InterlaceMethod = PngInterlaceMethod.None,
            RawPixelData = rawPixelData
        };
    }

    private static BmpImageData ConvertToBmpData(RgbaImage image)
    {
        var width = image.Width;
        var height = image.Height;
        var stride = ((width * 32 + 31) / 32) * 4;
        var pixelData = new byte[stride * height];

        for (var y = 0; y < height; y++)
        {
            var srcY = height - 1 - y;
            var srcRow = srcY * width * 4;
            var dstRow = y * stride;

            for (var x = 0; x < width; x++)
            {
                var srcIdx = srcRow + x * 4;
                var dstIdx = dstRow + x * 4;

                pixelData[dstIdx] = image.RgbaData[srcIdx + 2];
                pixelData[dstIdx + 1] = image.RgbaData[srcIdx + 1];
                pixelData[dstIdx + 2] = image.RgbaData[srcIdx];
                pixelData[dstIdx + 3] = image.RgbaData[srcIdx + 3];
            }
        }

        return new BmpImageData
        {
            Width = width,
            Height = height,
            BitsPerPixel = 32,
            Compression = BmpCompression.None,
            PixelData = pixelData
        };
    }

    #endregion
}
