using Acorn.Image.Data;

namespace Acorn.Image.Process;

/// <summary>
///     图像处理器——纯 C# 实现的图像缩放和 Mipmap 生成
/// </summary>
/// <remarks>
///     支持三种采样过滤器：最近邻、双线性、Lanczos3。
///     所有操作基于 RgbaImage，纯托管 C# 代码，可在所有 .NET 平台运行（包括 WASM）。
/// </remarks>
public static class ImageProcessor
{
    #region 公开方法

    /// <summary>
    ///     调整图像大小
    /// </summary>
    /// <param name="image">原始图像</param>
    /// <param name="targetWidth">目标宽度</param>
    /// <param name="targetHeight">目标高度</param>
    /// <param name="filter">采样过滤器</param>
    /// <returns>缩放后的图像</returns>
    public static RgbaImage Resize(RgbaImage image, int targetWidth, int targetHeight, ResizeFilter filter = ResizeFilter.Lanczos3)
    {
        if (image.Width == targetWidth && image.Height == targetHeight)
        {
            return image;
        }

        if (targetWidth <= 0 || targetHeight <= 0)
        {
            throw new ArgumentException($"目标尺寸无效：{targetWidth}x{targetHeight}");
        }

        return filter switch
        {
            ResizeFilter.Nearest => ResizeNearest(image, targetWidth, targetHeight),
            ResizeFilter.Bilinear => ResizeBilinear(image, targetWidth, targetHeight),
            ResizeFilter.Lanczos3 => ResizeLanczos3(image, targetWidth, targetHeight),
            _ => ResizeLanczos3(image, targetWidth, targetHeight)
        };
    }

    /// <summary>
    ///     为图像生成 Mipmap 链
    /// </summary>
    /// <param name="image">原始图像</param>
    /// <param name="filter">采样过滤器</param>
    /// <param name="maxLevels">最大 Mipmap 级别数（0 表示自动计算）</param>
    /// <returns>Mipmap 链（不包含原始图像）</returns>
    public static List<RgbaImage> GenerateMipmaps(RgbaImage image, ResizeFilter filter = ResizeFilter.Lanczos3, int maxLevels = 0)
    {
        var autoLevels = CalculateMipLevels(image.Width, image.Height);
        var mipLevels = maxLevels > 0 ? Math.Min(maxLevels, autoLevels) : autoLevels;

        var mipmaps = new List<RgbaImage>();
        var current = image;

        for (var level = 1; level < mipLevels; level++)
        {
            var mipWidth = Math.Max(1, current.Width >> 1);
            var mipHeight = Math.Max(1, current.Height >> 1);
            current = Resize(current, mipWidth, mipHeight, filter);
            mipmaps.Add(current);
        }

        return mipmaps;
    }

    /// <summary>
    ///     计算给定尺寸的最大 Mipmap 级别数
    /// </summary>
    public static int CalculateMipLevels(int width, int height)
    {
        var maxDim = Math.Max(width, height);
        var levels = 0;
        while (maxDim > 0)
        {
            levels++;
            maxDim >>= 1;
        }
        return levels;
    }

    /// <summary>
    ///     检查 RGBA 数据中是否包含非不透明 Alpha 通道
    /// </summary>
    public static bool HasAlphaChannel(byte[] rgbaData)
    {
        for (var i = 3; i < rgbaData.Length; i += 4)
        {
            if (rgbaData[i] < 255) return true;
        }
        return false;
    }

    #endregion

    #region 最近邻缩放

    private static RgbaImage ResizeNearest(RgbaImage image, int targetWidth, int targetHeight)
    {
        var rgbaData = new byte[targetWidth * targetHeight * 4];
        var xRatio = (float)image.Width / targetWidth;
        var yRatio = (float)image.Height / targetHeight;

        for (var y = 0; y < targetHeight; y++)
        {
            var srcY = (int)(y * yRatio);
            if (srcY >= image.Height) srcY = image.Height - 1;

            for (var x = 0; x < targetWidth; x++)
            {
                var srcX = (int)(x * xRatio);
                if (srcX >= image.Width) srcX = image.Width - 1;

                var srcIdx = (srcY * image.Width + srcX) * 4;
                var dstIdx = (y * targetWidth + x) * 4;

                rgbaData[dstIdx] = image.RgbaData[srcIdx];
                rgbaData[dstIdx + 1] = image.RgbaData[srcIdx + 1];
                rgbaData[dstIdx + 2] = image.RgbaData[srcIdx + 2];
                rgbaData[dstIdx + 3] = image.RgbaData[srcIdx + 3];
            }
        }

        return new RgbaImage { Width = targetWidth, Height = targetHeight, RgbaData = rgbaData };
    }

    #endregion

    #region 双线性缩放

    private static RgbaImage ResizeBilinear(RgbaImage image, int targetWidth, int targetHeight)
    {
        var rgbaData = new byte[targetWidth * targetHeight * 4];
        var xRatio = (float)(image.Width - 1) / Math.Max(1, targetWidth - 1);
        var yRatio = (float)(image.Height - 1) / Math.Max(1, targetHeight - 1);

        for (var y = 0; y < targetHeight; y++)
        {
            var srcYf = y * yRatio;
            var srcY0 = (int)srcYf;
            var srcY1 = Math.Min(srcY0 + 1, image.Height - 1);
            var fy = srcYf - srcY0;

            for (var x = 0; x < targetWidth; x++)
            {
                var srcXf = x * xRatio;
                var srcX0 = (int)srcXf;
                var srcX1 = Math.Min(srcX0 + 1, image.Width - 1);
                var fx = srcXf - srcX0;

                var i00 = (srcY0 * image.Width + srcX0) * 4;
                var i10 = (srcY0 * image.Width + srcX1) * 4;
                var i01 = (srcY1 * image.Width + srcX0) * 4;
                var i11 = (srcY1 * image.Width + srcX1) * 4;
                var dstIdx = (y * targetWidth + x) * 4;

                for (var c = 0; c < 4; c++)
                {
                    var v00 = image.RgbaData[i00 + c];
                    var v10 = image.RgbaData[i10 + c];
                    var v01 = image.RgbaData[i01 + c];
                    var v11 = image.RgbaData[i11 + c];

                    var top = v00 + (v10 - v00) * fx;
                    var bottom = v01 + (v11 - v01) * fx;
                    rgbaData[dstIdx + c] = (byte)Math.Clamp(top + (bottom - top) * fy, 0, 255);
                }
            }
        }

        return new RgbaImage { Width = targetWidth, Height = targetHeight, RgbaData = rgbaData };
    }

    #endregion

    #region Lanczos3 缩放

    private static RgbaImage ResizeLanczos3(RgbaImage image, int targetWidth, int targetHeight)
    {
        var tempWidth = targetWidth;
        var tempHeight = image.Height;
        var tempData = new byte[tempWidth * tempHeight * 4];

        var xScale = (double)image.Width / targetWidth;
        var filterRadius = 3.0 * xScale;
        if (filterRadius < 1.0) filterRadius = 1.0;

        for (var y = 0; y < tempHeight; y++)
        {
            for (var x = 0; x < tempWidth; x++)
            {
                var srcCenter = (x + 0.5) * xScale - 0.5;
                var srcStart = (int)Math.Floor(srcCenter - filterRadius);
                var srcEnd = (int)Math.Ceiling(srcCenter + filterRadius);

                var sumR = 0.0;
                var sumG = 0.0;
                var sumB = 0.0;
                var sumA = 0.0;
                var sumW = 0.0;

                for (var sx = srcStart; sx <= srcEnd; sx++)
                {
                    var clampedSx = Math.Clamp(sx, 0, image.Width - 1);
                    var weight = Lanczos3Kernel(srcCenter - sx, filterRadius);

                    var srcIdx = (y * image.Width + clampedSx) * 4;
                    sumR += image.RgbaData[srcIdx] * weight;
                    sumG += image.RgbaData[srcIdx + 1] * weight;
                    sumB += image.RgbaData[srcIdx + 2] * weight;
                    sumA += image.RgbaData[srcIdx + 3] * weight;
                    sumW += weight;
                }

                var dstIdx = (y * tempWidth + x) * 4;
                if (sumW > 0)
                {
                    tempData[dstIdx] = (byte)Math.Clamp(sumR / sumW, 0, 255);
                    tempData[dstIdx + 1] = (byte)Math.Clamp(sumG / sumW, 0, 255);
                    tempData[dstIdx + 2] = (byte)Math.Clamp(sumB / sumW, 0, 255);
                    tempData[dstIdx + 3] = (byte)Math.Clamp(sumA / sumW, 0, 255);
                }
            }
        }

        var yScale = (double)image.Height / targetHeight;
        filterRadius = 3.0 * yScale;
        if (filterRadius < 1.0) filterRadius = 1.0;

        var finalData = new byte[targetWidth * targetHeight * 4];

        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                var srcCenter = (y + 0.5) * yScale - 0.5;
                var srcStart = (int)Math.Floor(srcCenter - filterRadius);
                var srcEnd = (int)Math.Ceiling(srcCenter + filterRadius);

                var sumR = 0.0;
                var sumG = 0.0;
                var sumB = 0.0;
                var sumA = 0.0;
                var sumW = 0.0;

                for (var sy = srcStart; sy <= srcEnd; sy++)
                {
                    var clampedSy = Math.Clamp(sy, 0, tempHeight - 1);
                    var weight = Lanczos3Kernel(srcCenter - sy, filterRadius);

                    var srcIdx = (clampedSy * tempWidth + x) * 4;
                    sumR += tempData[srcIdx] * weight;
                    sumG += tempData[srcIdx + 1] * weight;
                    sumB += tempData[srcIdx + 2] * weight;
                    sumA += tempData[srcIdx + 3] * weight;
                    sumW += weight;
                }

                var dstIdx = (y * targetWidth + x) * 4;
                if (sumW > 0)
                {
                    finalData[dstIdx] = (byte)Math.Clamp(sumR / sumW, 0, 255);
                    finalData[dstIdx + 1] = (byte)Math.Clamp(sumG / sumW, 0, 255);
                    finalData[dstIdx + 2] = (byte)Math.Clamp(sumB / sumW, 0, 255);
                    finalData[dstIdx + 3] = (byte)Math.Clamp(sumA / sumW, 0, 255);
                }
            }
        }

        return new RgbaImage { Width = targetWidth, Height = targetHeight, RgbaData = finalData };
    }

    private static double Lanczos3Kernel(double x, double radius)
    {
        if (Math.Abs(x) < 1e-10) return 1.0;
        if (Math.Abs(x) >= radius) return 0.0;

        var piX = Math.PI * x;
        return (radius * Math.Sin(piX) * Math.Sin(piX / radius)) / (piX * piX);
    }

    #endregion
}
