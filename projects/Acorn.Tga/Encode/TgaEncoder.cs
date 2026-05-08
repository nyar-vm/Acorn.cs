using Acorn.Frame;
using Acorn.Tga.Data;

namespace Acorn.Tga.Encode;

/// <summary>
///     TGA 文件编码器，将 <see cref="TgaImageData" /> 数据结构编码为 TGA 二进制格式。
/// </summary>
/// <remarks>
///     支持 24 位和 32 位未压缩 TGA 输出。
///     像素数据应为 TGA 原生 BGR/BGRA 顺序。
/// </remarks>
public sealed class TgaEncoder
{
    /// <summary>
    ///     将 TGA 图像数据编码为 TGA 二进制格式。
    /// </summary>
    /// <param name="image">TGA 图像数据，像素数据应为 BGR/BGRA 顺序。</param>
    /// <returns>TGA 二进制数据。</returns>
    public byte[] Encode(TgaImageData image)
    {
        var size = EstimateSize(image);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteHeader(ref writer, image);

        if (image.HasColorMap && image.ColorMap.Length > 0)
        {
            WriteColorMap(ref writer, image);
        }

        if (image.IdLength > 0 && image.ImageId.Length > 0)
        {
            writer.Write(image.ImageId);
        }

        writer.Write(image.PixelData);

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void WriteHeader(ref ByteBufferWriter writer, TgaImageData image)
    {
        writer.WriteU8((byte)image.IdLength);
        writer.WriteU8(image.HasColorMap ? (byte)1 : (byte)0);
        writer.WriteU8((byte)image.ImageType);

        writer.WriteU16LE(0);
        writer.WriteU16LE((ushort)(image.HasColorMap ? image.ColorMap.Length / 4 : 0));
        writer.WriteU16LE((ushort)(image.HasColorMap ? 32 : 0));

        writer.WriteU16LE(0);
        writer.WriteU16LE(0);
        writer.WriteU16LE((ushort)image.Width);
        writer.WriteU16LE((ushort)image.Height);
        writer.WriteU8((byte)image.PixelDepth);
        writer.WriteU8(image.IsTopDown ? (byte)0x28 : (byte)0x00);
    }

    private static void WriteColorMap(ref ByteBufferWriter writer, TgaImageData image)
    {
        var entryCount = image.ColorMap.Length / 4;

        for (var i = 0; i < entryCount; i++)
        {
            var offset = i * 4;
            writer.WriteU8(image.ColorMap[offset + 2]);
            writer.WriteU8(image.ColorMap[offset + 1]);
            writer.WriteU8(image.ColorMap[offset]);
            writer.WriteU8(image.ColorMap[offset + 3]);
        }
    }

    private static int EstimateSize(TgaImageData image)
    {
        return TgaConstants.HeaderSize + image.ImageId.Length + image.ColorMap.Length + image.PixelData.Length;
    }

    #endregion
}
