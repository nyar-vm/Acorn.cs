using Acorn.Frame;
using Acorn.Bmp.Data;

namespace Acorn.Bmp.Encode;

/// <summary>
///     BMP 文件编码器，将 C# 数据结构编码为 BMP 图像格式。
/// </summary>
/// <remarks>
///     BMP 是 Microsoft 的标准位图格式，广泛用于 Windows 应用和游戏开发。
///     编码器生成符合 BMP 规范的二进制数据，支持 8/24/32 位色深。
/// </remarks>
public sealed class BmpEncoder
{
    /// <summary>
    ///     将 BMP 图像数据编码为 BMP 二进制格式。
    /// </summary>
    /// <param name="data">BMP 图像数据。</param>
    /// <returns>BMP 二进制数据。</returns>
    public byte[] Encode(BmpImageData data)
    {
        var size = EstimateSize(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteFileHeader(ref writer, data);
        WriteInfoHeader(ref writer, data);
        WritePalette(ref writer, data);
        WritePixelData(ref writer, data);

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void WriteFileHeader(ref ByteBufferWriter writer, BmpImageData data)
    {
        var paletteSize = data.Palette.Length * 4;
        var pixelDataSize = data.PixelData.Length > 0 ? data.PixelData.Length : ComputeImageSize(data);
        var fileSize = BmpConstants.FileHeaderSize + BmpConstants.InfoHeaderSize + paletteSize + pixelDataSize;
        var dataOffset = BmpConstants.FileHeaderSize + BmpConstants.InfoHeaderSize + paletteSize;

        writer.WriteString(BmpConstants.MagicTag);
        writer.WriteU32LE((uint)fileSize);
        writer.WriteU16LE(0);
        writer.WriteU16LE(0);
        writer.WriteU32LE((uint)dataOffset);
    }

    private static void WriteInfoHeader(ref ByteBufferWriter writer, BmpImageData data)
    {
        writer.WriteU32LE(BmpConstants.InfoHeaderSize);
        writer.WriteI32LE(data.Width);
        writer.WriteI32LE(data.Height);
        writer.WriteU16LE(1);
        writer.WriteU16LE(data.BitsPerPixel);
        writer.WriteU32LE((uint)data.Compression);
        writer.WriteU32LE(data.ImageSize > 0 ? data.ImageSize : (uint)ComputeImageSize(data));
        writer.WriteI32LE(data.XPelsPerMeter);
        writer.WriteI32LE(data.YPelsPerMeter);
        writer.WriteU32LE(data.ColorsUsed);
        writer.WriteU32LE(data.ColorsImportant);
    }

    private static void WritePalette(ref ByteBufferWriter writer, BmpImageData data)
    {
        foreach (var color in data.Palette)
        {
            writer.WriteU8((byte)(color & 0xFF));
            writer.WriteU8((byte)((color >> 8) & 0xFF));
            writer.WriteU8((byte)((color >> 16) & 0xFF));
            writer.WriteU8((byte)((color >> 24) & 0xFF));
        }
    }

    private static void WritePixelData(ref ByteBufferWriter writer, BmpImageData data)
    {
        if (data.PixelData.Length > 0)
        {
            writer.Write(data.PixelData);
        }
    }

    private static int ComputeImageSize(BmpImageData data)
    {
        var stride = ((data.Width * data.BitsPerPixel + 31) / 32) * 4;
        return stride * Math.Abs(data.Height);
    }

    private static int EstimateSize(BmpImageData data)
    {
        var paletteSize = data.Palette.Length * 4;
        var pixelDataSize = data.PixelData.Length > 0 ? data.PixelData.Length : ComputeImageSize(data);
        return BmpConstants.FileHeaderSize + BmpConstants.InfoHeaderSize + paletteSize + pixelDataSize + 256;
    }

    #endregion
}
