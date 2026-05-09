using Acorn.Bmp.Data;
using Acorn.Bmp.Decode;
using Acorn.Dds.Data;
using Acorn.Dds.Decode;
using Acorn.Exr.Data;
using Acorn.Exr.Decode;
using Acorn.Image.Data;
using Acorn.Jpeg.Data;
using Acorn.Jpeg.Decode;
using Acorn.Png.Data;
using Acorn.Png.Decode;
using Acorn.Psd.Data;
using Acorn.Psd.Decode;
using Acorn.Tga.Data;
using Acorn.Tga.Decode;

namespace Acorn.Image.Decode;

/// <summary>
///     统一图像解码器——自动检测格式并解码为 RGBA 像素数据
/// </summary>
/// <remarks>
///     支持 PNG、BMP、JPEG、TGA、DDS、EXR、PSD、GIF 格式的自动检测和解码。
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
            ImageFormat.Dds => DecodeDds(data),
            ImageFormat.Exr => DecodeExr(data),
            ImageFormat.Psd => DecodePsd(data),
            ImageFormat.Gif => DecodeGif(data),
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

        if (data.Length >= 4)
        {
            if (data[0] == 0x44 && data[1] == 0x44 && data[2] == 0x53 && data[3] == 0x20)
            {
                return ImageFormat.Dds;
            }
        }

        if (data.Length >= 4)
        {
            if (data[0] == 0x76 && data[1] == 0x2F && data[2] == 0x31 && data[3] == 0x01)
            {
                return ImageFormat.Exr;
            }
        }

        if (data.Length >= 4)
        {
            if (data[0] == 0x38 && data[1] == 0x42 && data[2] == 0x50 && data[3] == 0x53)
            {
                return ImageFormat.Psd;
            }
        }

        if (data.Length >= 4)
        {
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && (data[3] == 0x38 || data[3] == 0x39))
            {
                return ImageFormat.Gif;
            }
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
        var jpegData = decoder.Decode();
        return ConvertJpegToRgba(jpegData);
    }

    /// <summary>
    ///     解码 TGA 图像为 RGBA 像素数据
    /// </summary>
    public static RgbaImage DecodeTga(ReadOnlySpan<byte> data)
    {
        var decoder = new TgaDecoder(data);
        var tgaData = decoder.Decode();
        return ConvertTgaToRgba(tgaData);
    }

    /// <summary>
    ///     解码 DDS 图像为 RGBA 像素数据
    /// </summary>
    /// <remarks>
    ///     仅支持未压缩的 RGBA/RGB 格式 DDS，块压缩格式需专用解码器。
    /// </remarks>
    public static RgbaImage DecodeDds(ReadOnlySpan<byte> data)
    {
        var decoder = new DdsDecoder(data);
        var ddsData = decoder.Decode();
        return ConvertDdsToRgba(ddsData);
    }

    /// <summary>
    ///     解码 EXR 图像为 RGBA 像素数据
    /// </summary>
    /// <remarks>
    ///     当前仅解析 EXR 头部信息，像素数据解码尚未实现。
    /// </remarks>
    public static RgbaImage DecodeExr(ReadOnlySpan<byte> data)
    {
        var decoder = new ExrDecoder(data);
        var exrData = decoder.Decode();
        return ConvertExrToRgba(exrData);
    }

    /// <summary>
    ///     解码 PSD 图像为 RGBA 像素数据
    /// </summary>
    /// <remarks>
    ///     当前仅解析 PSD 头部和图层信息，合并图像数据解码尚未完整实现。
    /// </remarks>
    public static RgbaImage DecodePsd(ReadOnlySpan<byte> data)
    {
        var decoder = new PsdDecoder(data);
        var psdData = decoder.Decode();
        return ConvertPsdToRgba(psdData);
    }

    /// <summary>
    ///     解码 GIF 图像为 RGBA 像素数据
    /// </summary>
    /// <remarks>
    ///     返回 GIF 第一帧的 RGBA 数据。多帧动画需使用 Acorn.Gif 的专用解码器。
    /// </remarks>
    public static RgbaImage DecodeGif(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException("GIF 解码尚未实现，请使用 Acorn.Gif 的专用解码器");
    }

    #endregion

    #region JPEG 转换

    private static RgbaImage ConvertJpegToRgba(JpegImageData jpeg)
    {
        var width = jpeg.Width;
        var height = jpeg.Height;
        var rgbaData = new byte[width * height * 4];

        if (jpeg.PixelData.Length == 0)
        {
            return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
        }

        var srcBpp = jpeg.ColorSpace == JpegColorSpace.Grayscale ? 1 : 4;
        var srcStride = width * srcBpp;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIdx = y * srcStride + x * srcBpp;
                var dstIdx = (y * width + x) * 4;

                if (jpeg.ColorSpace == JpegColorSpace.Grayscale)
                {
                    var gray = jpeg.PixelData[srcIdx];
                    rgbaData[dstIdx] = gray;
                    rgbaData[dstIdx + 1] = gray;
                    rgbaData[dstIdx + 2] = gray;
                    rgbaData[dstIdx + 3] = 255;
                }
                else
                {
                    rgbaData[dstIdx] = jpeg.PixelData[srcIdx];
                    rgbaData[dstIdx + 1] = jpeg.PixelData[srcIdx + 1];
                    rgbaData[dstIdx + 2] = jpeg.PixelData[srcIdx + 2];
                    rgbaData[dstIdx + 3] = jpeg.PixelData[srcIdx + 3];
                }
            }
        }

        return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
    }

    #endregion

    #region TGA 转换

    private static RgbaImage ConvertTgaToRgba(TgaImageData tga)
    {
        var width = tga.Width;
        var height = tga.Height;
        var isTopDown = tga.IsTopDown;
        var bytesPerPixel = tga.PixelDepth / 8;
        var rgbaData = new byte[width * height * 4];

        if (tga.PixelData.Length == 0)
        {
            return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
        }

        for (var y = 0; y < height; y++)
        {
            var srcY = isTopDown ? y : height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var srcIdx = (srcY * width + x) * bytesPerPixel;
                var dstIdx = (y * width + x) * 4;

                switch (tga.PixelDepth)
                {
                    case 32:
                        rgbaData[dstIdx] = tga.PixelData[srcIdx + 2];
                        rgbaData[dstIdx + 1] = tga.PixelData[srcIdx + 1];
                        rgbaData[dstIdx + 2] = tga.PixelData[srcIdx];
                        rgbaData[dstIdx + 3] = tga.PixelData[srcIdx + 3];
                        break;
                    case 24:
                        rgbaData[dstIdx] = tga.PixelData[srcIdx + 2];
                        rgbaData[dstIdx + 1] = tga.PixelData[srcIdx + 1];
                        rgbaData[dstIdx + 2] = tga.PixelData[srcIdx];
                        rgbaData[dstIdx + 3] = 255;
                        break;
                    case 16:
                        var pixel16 = tga.PixelData[srcIdx] | (tga.PixelData[srcIdx + 1] << 8);
                        rgbaData[dstIdx] = (byte)(((pixel16 >> 10) & 0x1F) * 255 / 31);
                        rgbaData[dstIdx + 1] = (byte)(((pixel16 >> 5) & 0x1F) * 255 / 31);
                        rgbaData[dstIdx + 2] = (byte)((pixel16 & 0x1F) * 255 / 31);
                        rgbaData[dstIdx + 3] = (pixel16 & 0x8000) != 0 ? (byte)255 : (byte)0;
                        break;
                    case 8:
                        if (tga.HasColorMap && tga.ColorMap.Length > 0 && tga.PixelData[srcIdx] < tga.ColorMap.Length / 4)
                        {
                            var palIdx = tga.PixelData[srcIdx] * 4;
                            rgbaData[dstIdx] = tga.ColorMap[palIdx];
                            rgbaData[dstIdx + 1] = tga.ColorMap[palIdx + 1];
                            rgbaData[dstIdx + 2] = tga.ColorMap[palIdx + 2];
                            rgbaData[dstIdx + 3] = tga.ColorMap[palIdx + 3];
                        }
                        else
                        {
                            var gray = tga.PixelData[srcIdx];
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

    #endregion

    #region DDS 转换

    private static RgbaImage ConvertDdsToRgba(DdsTextureData dds)
    {
        var width = dds.Width;
        var height = dds.Height;
        var rgbaData = new byte[width * height * 4];

        if (dds.Surfaces.Count == 0 || dds.Surfaces[0].MipLevels.Count == 0)
        {
            return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
        }

        var mip0 = dds.Surfaces[0].MipLevels[0];
        var bpp = (int)dds.PixelFormat.RGBBitCount;

        if (dds.PixelFormat.FourCC != 0)
        {
            return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
        }

        if (bpp == 32)
        {
            var bytesPerPixel = 4;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var srcIdx = (y * width + x) * bytesPerPixel;
                    var dstIdx = (y * width + x) * 4;

                    if (srcIdx + 3 < mip0.Data.Length)
                    {
                        rgbaData[dstIdx] = mip0.Data[srcIdx + 2];
                        rgbaData[dstIdx + 1] = mip0.Data[srcIdx + 1];
                        rgbaData[dstIdx + 2] = mip0.Data[srcIdx];
                        rgbaData[dstIdx + 3] = mip0.Data[srcIdx + 3];
                    }
                }
            }
        }
        else if (bpp == 24)
        {
            var bytesPerPixel = 3;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var srcIdx = (y * width + x) * bytesPerPixel;
                    var dstIdx = (y * width + x) * 4;

                    if (srcIdx + 2 < mip0.Data.Length)
                    {
                        rgbaData[dstIdx] = mip0.Data[srcIdx + 2];
                        rgbaData[dstIdx + 1] = mip0.Data[srcIdx + 1];
                        rgbaData[dstIdx + 2] = mip0.Data[srcIdx];
                        rgbaData[dstIdx + 3] = 255;
                    }
                }
            }
        }

        return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
    }

    #endregion

    #region EXR 转换

    private static RgbaImage ConvertExrToRgba(ExrImageData exr)
    {
        var width = exr.Width;
        var height = exr.Height;
        var rgbaData = new byte[width * height * 4];

        return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
    }

    #endregion

    #region PSD 转换

    private static RgbaImage ConvertPsdToRgba(PsdImageData psd)
    {
        var width = psd.Width;
        var height = psd.Height;
        var rgbaData = new byte[width * height * 4];

        if (psd.MergedImageData == null || psd.MergedImageData.Length == 0)
        {
            return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
        }

        if (psd.ColorMode == 3 && psd.Depth == 8)
        {
            var channelCount = Math.Min(psd.Channels, 4);
            var planeSize = width * height;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dstIdx = (y * width + x) * 4;
                    var pixelIdx = y * width + x;

                    if (channelCount >= 1 && pixelIdx < planeSize)
                    {
                        rgbaData[dstIdx] = psd.MergedImageData[pixelIdx];
                    }
                    if (channelCount >= 2 && pixelIdx + planeSize < psd.MergedImageData.Length)
                    {
                        rgbaData[dstIdx + 1] = psd.MergedImageData[pixelIdx + planeSize];
                    }
                    if (channelCount >= 3 && pixelIdx + planeSize * 2 < psd.MergedImageData.Length)
                    {
                        rgbaData[dstIdx + 2] = psd.MergedImageData[pixelIdx + planeSize * 2];
                    }
                    rgbaData[dstIdx + 3] = channelCount >= 4 && pixelIdx + planeSize * 3 < psd.MergedImageData.Length
                        ? psd.MergedImageData[pixelIdx + planeSize * 3]
                        : (byte)255;
                }
            }
        }

        return new RgbaImage { Width = width, Height = height, RgbaData = rgbaData };
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
