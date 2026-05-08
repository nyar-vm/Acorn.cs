using Acorn.Bmp.Data;
using Acorn.Bmp.Decode;
using Acorn.Image.Data;
using Acorn.Png.Data;
using Acorn.Png.Decode;

namespace Acorn.Image.Decode;

/// <summary>
///     统一图像解码器——自动检测格式并解码为 RGBA 像素数据
/// </summary>
/// <remarks>
///     支持 PNG、BMP、JPEG、TGA 格式的自动检测和解码。
///     所有格式统一输出为 RgbaImage（R,G,B,A 交错，从左到右从上到下）。
///     纯 C# 实现，无第三方依赖，可在所有 .NET 平台运行（包括 WASM）。
/// </remarks>
public static class ImageDecoder
{
    #region 公开方法

    /// <summary>
    ///     自动检测格式并解码图像为 RGBA 像素数据
    /// </summary>
    /// <param name="data">图像二进制数据</param>
    /// <returns>RGBA 图像数据</returns>
    public static RgbaImage Decode(ReadOnlySpan<byte> data)
    {
        var format = DetectFormat(data);

        return format switch
        {
            ImageFormat.Png => DecodePng(data),
            ImageFormat.Bmp => DecodeBmp(data),
            ImageFormat.Jpeg => DecodeJpeg(data),
            ImageFormat.Tga => DecodeTga(data),
            _ => throw new InvalidDataException("无法识别的图像格式")
        };
    }

    /// <summary>
    ///     检测图像格式
    /// </summary>
    /// <param name="data">图像二进制数据（至少需要 12 字节）</param>
    /// <returns>检测到的图像格式</returns>
    public static ImageFormat DetectFormat(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4) return ImageFormat.Unknown;

        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        {
            return ImageFormat.Png;
        }

        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return ImageFormat.Jpeg;
        }

        if (data[0] == 0x42 && data[1] == 0x4D)
        {
            return ImageFormat.Bmp;
        }

        if (data.Length >= 18)
        {
            var tgaFooter = data.Length >= 26;
            if (tgaFooter)
            {
                var footer = data.Slice(data.Length - 18, 18);
                if (footer[0] == 0x00 && footer[1] == 0x00 && footer[2] == 0x00 && footer[3] == 0x00)
                {
                    return ImageFormat.Tga;
                }
            }

            var colorMapType = data[1];
            var imageType = data[2];
            if (colorMapType <= 1 && imageType is >= 1 and <= 11)
            {
                return ImageFormat.Tga;
            }
        }

        return ImageFormat.Unknown;
    }

    /// <summary>
    ///     解码 PNG 图像为 RGBA 像素数据
    /// </summary>
    public static RgbaImage DecodePng(ReadOnlySpan<byte> data)
    {
        var decoder = new PngDecoder(data);
        var pngData = decoder.Decode();
        return ExpandPngToRgba(pngData);
    }

    /// <summary>
    ///     解码 BMP 图像为 RGBA 像素数据
    /// </summary>
    public static RgbaImage DecodeBmp(ReadOnlySpan<byte> data)
    {
        var decoder = new BmpDecoder(data);
        var bmpData = decoder.Decode();
        return ExpandBmpToRgba(bmpData);
    }

    /// <summary>
    ///     解码 JPEG 图像为 RGBA 像素数据
    /// </summary>
    public static RgbaImage DecodeJpeg(ReadOnlySpan<byte> data)
    {
        var decoder = new JpegDecoder(data);
        return decoder.Decode();
    }

    /// <summary>
    ///     解码 TGA 图像为 RGBA 像素数据
    /// </summary>
    public static RgbaImage DecodeTga(ReadOnlySpan<byte> data)
    {
        var decoder = new TgaDecoder(data);
        return decoder.Decode();
    }

    #endregion

    #region PNG 像素展开

    private static RgbaImage ExpandPngToRgba(PngImageData png)
    {
        var width = png.Width;
        var height = png.Height;
        var bpp = png.BytesPerPixel;
        var stride = width * bpp + 1;
        var rgbaData = new byte[width * height * 4];

        var filtered = png.RawPixelData;
        if (filtered.Length == 0)
        {
            return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
        }

        var reconstructed = ReconstructFilter(filtered, width, height, bpp, stride);

        switch (png.ColorType)
        {
            case PngColorType.TruecolorAlpha:
                ExpandTruecolorAlpha(reconstructed, width, height, bpp, png.BitDepth, rgbaData);
                break;
            case PngColorType.Truecolor:
                ExpandTruecolor(reconstructed, width, height, bpp, png.BitDepth, rgbaData);
                break;
            case PngColorType.GrayscaleAlpha:
                ExpandGrayscaleAlpha(reconstructed, width, height, png.BitDepth, rgbaData);
                break;
            case PngColorType.Grayscale:
                ExpandGrayscale(reconstructed, width, height, png.BitDepth, png.Transparency, rgbaData);
                break;
            case PngColorType.Indexed:
                ExpandIndexed(reconstructed, width, height, png.Palette, png.Transparency, rgbaData);
                break;
        }

        return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
    }

    private static byte[] ReconstructFilter(byte[] filtered, int width, int height, int bpp, int stride)
    {
        var reconstructed = new byte[height * (stride - 1)];

        for (var y = 0; y < height; y++)
        {
            var filterType = filtered[y * stride];
            var srcOffset = y * stride + 1;
            var dstOffset = y * (stride - 1);
            var prevOffset = y > 0 ? (y - 1) * (stride - 1) : -1;

            for (var x = 0; x < stride - 1; x++)
            {
                var raw = filtered[srcOffset + x];
                var a = x >= bpp ? reconstructed[dstOffset + x - bpp] : (byte)0;
                var b = prevOffset >= 0 ? reconstructed[prevOffset + x] : (byte)0;
                var c = prevOffset >= 0 && x >= bpp ? reconstructed[prevOffset + x - bpp] : (byte)0;

                reconstructed[dstOffset + x] = filterType switch
                {
                    0 => raw,
                    1 => (byte)(raw + a),
                    2 => (byte)(raw + b),
                    3 => (byte)(raw + ((a + b) >> 1)),
                    4 => (byte)(raw + PaethPredictor(a, b, c)),
                    _ => raw
                };
            }
        }

        return reconstructed;
    }

    private static int PaethPredictor(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        if (pb <= pc) return b;
        return c;
    }

    private static void ExpandTruecolorAlpha(byte[] reconstructed, int width, int height, int bpp, byte bitDepth, byte[] rgbaData)
    {
        var srcStride = width * bpp;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIdx = y * srcStride + x * bpp;
                var dstIdx = (y * width + x) * 4;

                if (bitDepth == 8)
                {
                    rgbaData[dstIdx] = reconstructed[srcIdx];
                    rgbaData[dstIdx + 1] = reconstructed[srcIdx + 1];
                    rgbaData[dstIdx + 2] = reconstructed[srcIdx + 2];
                    rgbaData[dstIdx + 3] = reconstructed[srcIdx + 3];
                }
                else
                {
                    rgbaData[dstIdx] = reconstructed[srcIdx];
                    rgbaData[dstIdx + 1] = reconstructed[srcIdx + 2];
                    rgbaData[dstIdx + 2] = reconstructed[srcIdx + 4];
                    rgbaData[dstIdx + 3] = reconstructed[srcIdx + 6];
                }
            }
        }
    }

    private static void ExpandTruecolor(byte[] reconstructed, int width, int height, int bpp, byte bitDepth, byte[] rgbaData)
    {
        var srcStride = width * bpp;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIdx = y * srcStride + x * bpp;
                var dstIdx = (y * width + x) * 4;

                if (bitDepth == 8)
                {
                    rgbaData[dstIdx] = reconstructed[srcIdx];
                    rgbaData[dstIdx + 1] = reconstructed[srcIdx + 1];
                    rgbaData[dstIdx + 2] = reconstructed[srcIdx + 2];
                }
                else
                {
                    rgbaData[dstIdx] = reconstructed[srcIdx];
                    rgbaData[dstIdx + 1] = reconstructed[srcIdx + 2];
                    rgbaData[dstIdx + 2] = reconstructed[srcIdx + 4];
                }
                rgbaData[dstIdx + 3] = 255;
            }
        }
    }

    private static void ExpandGrayscaleAlpha(byte[] reconstructed, int width, int height, byte bitDepth, byte[] rgbaData)
    {
        var srcBpp = bitDepth <= 8 ? 2 : 4;
        var srcStride = width * srcBpp;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIdx = y * srcStride + x * srcBpp;
                var dstIdx = (y * width + x) * 4;

                byte gray, alpha;
                if (bitDepth <= 8)
                {
                    gray = reconstructed[srcIdx];
                    alpha = reconstructed[srcIdx + 1];
                }
                else
                {
                    gray = reconstructed[srcIdx];
                    alpha = reconstructed[srcIdx + 2];
                }

                rgbaData[dstIdx] = gray;
                rgbaData[dstIdx + 1] = gray;
                rgbaData[dstIdx + 2] = gray;
                rgbaData[dstIdx + 3] = alpha;
            }
        }
    }

    private static void ExpandGrayscale(byte[] reconstructed, int width, int height, byte bitDepth, byte[] transparency, byte[] rgbaData)
    {
        var srcBpp = bitDepth <= 8 ? 1 : 2;
        var srcStride = (width * bitDepth + 7) / 8;

        byte transGray = 0;
        if (transparency.Length >= 2)
        {
            transGray = transparency[1];
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dstIdx = (y * width + x) * 4;
                byte gray;

                if (bitDepth == 8)
                {
                    gray = reconstructed[y * srcStride + x];
                }
                else if (bitDepth < 8)
                {
                    var byteIdx = y * srcStride + (x * bitDepth) / 8;
                    var bitIdx = 8 - bitDepth - (x * bitDepth) % 8;
                    var mask = (1 << bitDepth) - 1;
                    gray = (byte)((reconstructed[byteIdx] >> bitIdx) & mask);
                    gray = (byte)(gray * 255 / mask);
                }
                else
                {
                    gray = reconstructed[y * srcStride + x * 2];
                }

                rgbaData[dstIdx] = gray;
                rgbaData[dstIdx + 1] = gray;
                rgbaData[dstIdx + 2] = gray;
                rgbaData[dstIdx + 3] = transparency.Length >= 2 && gray == transGray ? (byte)0 : (byte)255;
            }
        }
    }

    private static void ExpandIndexed(byte[] reconstructed, int width, int height, byte[] palette, byte[] transparency, byte[] rgbaData)
    {
        var srcStride = (width + 3) & ~3;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIdx = y * srcStride + x;
                var dstIdx = (y * width + x) * 4;
                var index = reconstructed[srcIdx];

                var palOffset = index * 3;
                if (palOffset + 2 < palette.Length)
                {
                    rgbaData[dstIdx] = palette[palOffset];
                    rgbaData[dstIdx + 1] = palette[palOffset + 1];
                    rgbaData[dstIdx + 2] = palette[palOffset + 2];
                }

                rgbaData[dstIdx + 3] = index < transparency.Length ? transparency[index] : (byte)255;
            }
        }
    }

    #endregion

    #region BMP 像素展开

    private static RgbaImage ExpandBmpToRgba(BmpImageData bmp)
    {
        var width = bmp.Width;
        var height = bmp.AbsoluteHeight;
        var isTopDown = bmp.IsTopDown;
        var bpp = bmp.BitsPerPixel;
        var stride = bmp.Stride;
        var rgbaData = new byte[width * height * 4];

        if (bmp.PixelData.Length == 0)
        {
            return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
        }

        switch (bpp)
        {
            case 32:
                ExpandBmp32(bmp, width, height, isTopDown, stride, rgbaData);
                break;
            case 24:
                ExpandBmp24(bmp, width, height, isTopDown, stride, rgbaData);
                break;
            case 16:
                ExpandBmp16(bmp, width, height, isTopDown, stride, rgbaData);
                break;
            case 8:
                ExpandBmp8(bmp, width, height, isTopDown, stride, rgbaData);
                break;
            case 4:
                ExpandBmp4(bmp, width, height, isTopDown, stride, rgbaData);
                break;
            case 1:
                ExpandBmp1(bmp, width, height, isTopDown, stride, rgbaData);
                break;
        }

        return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
    }

    private static void ExpandBmp32(BmpImageData bmp, int width, int height, bool isTopDown, int stride, byte[] rgbaData)
    {
        for (var y = 0; y < height; y++)
        {
            var srcY = isTopDown ? y : height - 1 - y;
            var srcRow = srcY * stride;
            var dstRow = y * width * 4;

            for (var x = 0; x < width; x++)
            {
                var srcIdx = srcRow + x * 4;
                var dstIdx = dstRow + x * 4;
                rgbaData[dstIdx] = bmp.PixelData[srcIdx + 2];
                rgbaData[dstIdx + 1] = bmp.PixelData[srcIdx + 1];
                rgbaData[dstIdx + 2] = bmp.PixelData[srcIdx];
                rgbaData[dstIdx + 3] = bmp.PixelData[srcIdx + 3];
            }
        }
    }

    private static void ExpandBmp24(BmpImageData bmp, int width, int height, bool isTopDown, int stride, byte[] rgbaData)
    {
        for (var y = 0; y < height; y++)
        {
            var srcY = isTopDown ? y : height - 1 - y;
            var srcRow = srcY * stride;
            var dstRow = y * width * 4;

            for (var x = 0; x < width; x++)
            {
                var srcIdx = srcRow + x * 3;
                var dstIdx = dstRow + x * 4;
                rgbaData[dstIdx] = bmp.PixelData[srcIdx + 2];
                rgbaData[dstIdx + 1] = bmp.PixelData[srcIdx + 1];
                rgbaData[dstIdx + 2] = bmp.PixelData[srcIdx];
                rgbaData[dstIdx + 3] = 255;
            }
        }
    }

    private static void ExpandBmp16(BmpImageData bmp, int width, int height, bool isTopDown, int stride, byte[] rgbaData)
    {
        for (var y = 0; y < height; y++)
        {
            var srcY = isTopDown ? y : height - 1 - y;
            var srcRow = srcY * stride;
            var dstRow = y * width * 4;

            for (var x = 0; x < width; x++)
            {
                var srcIdx = srcRow + x * 2;
                var pixel = bmp.PixelData[srcIdx] | (bmp.PixelData[srcIdx + 1] << 8);
                var dstIdx = dstRow + x * 4;
                rgbaData[dstIdx] = (byte)(((pixel >> 10) & 0x1F) * 255 / 31);
                rgbaData[dstIdx + 1] = (byte)(((pixel >> 5) & 0x1F) * 255 / 31);
                rgbaData[dstIdx + 2] = (byte)((pixel & 0x1F) * 255 / 31);
                rgbaData[dstIdx + 3] = 255;
            }
        }
    }

    private static void ExpandBmp8(BmpImageData bmp, int width, int height, bool isTopDown, int stride, byte[] rgbaData)
    {
        for (var y = 0; y < height; y++)
        {
            var srcY = isTopDown ? y : height - 1 - y;
            var srcRow = srcY * stride;
            var dstRow = y * width * 4;

            for (var x = 0; x < width; x++)
            {
                var index = bmp.PixelData[srcRow + x];
                var dstIdx = dstRow + x * 4;

                if (index < bmp.Palette.Length)
                {
                    var color = bmp.Palette[index];
                    rgbaData[dstIdx] = (byte)((color >> 16) & 0xFF);
                    rgbaData[dstIdx + 1] = (byte)((color >> 8) & 0xFF);
                    rgbaData[dstIdx + 2] = (byte)(color & 0xFF);
                    rgbaData[dstIdx + 3] = (byte)((color >> 24) & 0xFF);
                }
                else
                {
                    rgbaData[dstIdx + 3] = 255;
                }
            }
        }
    }

    private static void ExpandBmp4(BmpImageData bmp, int width, int height, bool isTopDown, int stride, byte[] rgbaData)
    {
        for (var y = 0; y < height; y++)
        {
            var srcY = isTopDown ? y : height - 1 - y;
            var srcRow = srcY * stride;
            var dstRow = y * width * 4;

            for (var x = 0; x < width; x++)
            {
                var byteIdx = srcRow + x / 2;
                var index = (x % 2 == 0)
                    ? (bmp.PixelData[byteIdx] >> 4) & 0x0F
                    : bmp.PixelData[byteIdx] & 0x0F;
                var dstIdx = dstRow + x * 4;

                if (index < bmp.Palette.Length)
                {
                    var color = bmp.Palette[index];
                    rgbaData[dstIdx] = (byte)((color >> 16) & 0xFF);
                    rgbaData[dstIdx + 1] = (byte)((color >> 8) & 0xFF);
                    rgbaData[dstIdx + 2] = (byte)(color & 0xFF);
                    rgbaData[dstIdx + 3] = (byte)((color >> 24) & 0xFF);
                }
                else
                {
                    rgbaData[dstIdx + 3] = 255;
                }
            }
        }
    }

    private static void ExpandBmp1(BmpImageData bmp, int width, int height, bool isTopDown, int stride, byte[] rgbaData)
    {
        for (var y = 0; y < height; y++)
        {
            var srcY = isTopDown ? y : height - 1 - y;
            var srcRow = srcY * stride;
            var dstRow = y * width * 4;

            for (var x = 0; x < width; x++)
            {
                var byteIdx = srcRow + x / 8;
                var bitIdx = 7 - (x % 8);
                var index = (bmp.PixelData[byteIdx] >> bitIdx) & 0x01;
                var dstIdx = dstRow + x * 4;

                if (index < bmp.Palette.Length)
                {
                    var color = bmp.Palette[index];
                    rgbaData[dstIdx] = (byte)((color >> 16) & 0xFF);
                    rgbaData[dstIdx + 1] = (byte)((color >> 8) & 0xFF);
                    rgbaData[dstIdx + 2] = (byte)(color & 0xFF);
                    rgbaData[dstIdx + 3] = (byte)((color >> 24) & 0xFF);
                }
                else
                {
                    rgbaData[dstIdx + 3] = 255;
                }
            }
        }
    }

    #endregion
}
