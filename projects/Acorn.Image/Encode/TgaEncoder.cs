using Acorn.Image.Data;

namespace Acorn.Image.Encode;

/// <summary>
///     TGA (Targa) 图像编码器——纯 C# 实现，无第三方依赖
/// </summary>
/// <remarks>
///     支持 24 位和 32 位色深的未压缩 TGA 编码。
///     TGA 广泛用于游戏开发中的纹理资源，因其简单的格式和 Alpha 通道支持。
/// </remarks>
public sealed class TgaEncoder
{
    /// <summary>
    ///     将 RGBA 图像编码为 TGA 二进制格式
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <returns>TGA 二进制数据</returns>
    public byte[] Encode(RgbaImage image)
    {
        return Encode(image, HasAlpha(image));
    }

    /// <summary>
    ///     将 RGBA 图像编码为 TGA 二进制格式
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <param name="includeAlpha">是否包含 Alpha 通道（32 位色深）</param>
    /// <returns>TGA 二进制数据</returns>
    public byte[] Encode(RgbaImage image, bool includeAlpha)
    {
        var width = image.Width;
        var height = image.Height;
        var bytesPerPixel = includeAlpha ? 4 : 3;
        var pixelDepth = includeAlpha ? (byte)32 : (byte)24;
        var imageDataSize = width * height * bytesPerPixel;
        var footerSize = 26;
        var headerSize = 18;
        var totalSize = headerSize + imageDataSize + footerSize;

        var output = new byte[totalSize];
        var pos = 0;

        output[pos++] = 0;
        output[pos++] = 0;
        output[pos++] = 2;

        output[pos++] = 0;
        output[pos++] = 0;
        output[pos++] = 0;
        output[pos++] = 0;
        output[pos++] = 0;

        output[pos++] = 0;
        output[pos++] = 0;

        output[pos++] = 0;
        output[pos++] = 0;

        output[pos++] = (byte)(width & 0xFF);
        output[pos++] = (byte)((width >> 8) & 0xFF);
        output[pos++] = (byte)(height & 0xFF);
        output[pos++] = (byte)((height >> 8) & 0xFF);
        output[pos++] = pixelDepth;
        output[pos++] = 0x28;

        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIdx = (y * width + x) * 4;
                var dstIdx = pos;

                output[dstIdx] = image.RgbaData[srcIdx + 2];
                output[dstIdx + 1] = image.RgbaData[srcIdx + 1];
                output[dstIdx + 2] = image.RgbaData[srcIdx];

                if (includeAlpha)
                {
                    output[dstIdx + 3] = image.RgbaData[srcIdx + 3];
                }

                pos += bytesPerPixel;
            }
        }

        for (var i = 0; i < 24; i++)
        {
            output[pos++] = 0;
        }

        output[pos++] = (byte)'T';
        output[pos++] = (byte)'R';
        output[pos++] = (byte)'U';
        output[pos++] = (byte)'E';
        output[pos++] = (byte)'V';
        output[pos++] = (byte)'I';
        output[pos++] = (byte)'S';
        output[pos++] = (byte)'I';
        output[pos++] = (byte)'O';
        output[pos++] = (byte)'N';
        output[pos++] = (byte)'-';
        output[pos++] = (byte)'X';
        output[pos++] = (byte)'F';
        output[pos++] = (byte)'I';
        output[pos++] = (byte)'L';
        output[pos++] = (byte)'E';
        output[pos++] = (byte)'.';

        return output;
    }

    private static bool HasAlpha(RgbaImage image)
    {
        for (var i = 3; i < image.RgbaData.Length; i += 4)
        {
            if (image.RgbaData[i] < 255) return true;
        }
        return false;
    }
}
