using System.Text;
using Acorn.Frame;
using Acorn.Dds.Data;

namespace Acorn.Dds.Encode;

/// <summary>
///     DDS 文件编码器，将 C# 数据结构编码为 DirectDraw Surface 纹理格式。
/// </summary>
/// <remarks>
///     DDS 是 Microsoft DirectDraw 的纹理容器格式，广泛用于 PC 游戏和图形应用。
///     编码器生成符合 Microsoft DDS 规范的二进制数据。
/// </remarks>
public sealed class DdsEncoder
{
    /// <summary>
    ///     将 DDS 纹理数据编码为 DDS 二进制格式。
    /// </summary>
    /// <param name="data">DDS 纹理数据。</param>
    /// <returns>DDS 二进制数据。</returns>
    public byte[] Encode(DdsTextureData data)
    {
        var size = EstimateSize(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteMagic(ref writer);
        WriteHeader(ref writer, data);
        WriteSurfaces(ref writer, data);

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void WriteMagic(ref ByteBufferWriter writer)
    {
        writer.WriteString("DDS ");
    }

    private static void WriteHeader(ref ByteBufferWriter writer, DdsTextureData data)
    {
        writer.WriteU32LE(DdsConstants.HeaderSize);

        var flags = DdsFlags.Height | DdsFlags.Width | DdsFlags.PixelFormat;

        if (data.MipMapCount > 1)
        {
            flags |= DdsFlags.MipMapCount | DdsFlags.LinearSize;
        }

        if (data.Depth > 0)
        {
            flags |= DdsFlags.Depth;
        }

        writer.WriteU32LE((uint)flags);
        writer.WriteU32LE((uint)data.Height);
        writer.WriteU32LE((uint)data.Width);
        writer.WriteU32LE(0);
        writer.WriteU32LE((uint)data.Depth);
        writer.WriteU32LE((uint)data.MipMapCount);

        writer.Write(new byte[44]);

        WritePixelFormat(ref writer, data.PixelFormat);

        var caps1 = 0x1000u;

        if (data.MipMapCount > 1)
        {
            caps1 |= 0x400008;
        }

        writer.WriteU32LE(caps1);

        var caps2 = 0u;

        if (data.IsCubeMap)
        {
            caps2 |= 0x200 | 0x400 | 0x800 | 0x1000 | 0x2000 | 0x4000 | 0x8000;
        }

        writer.WriteU32LE(caps2);
        writer.WriteU32LE(0);
        writer.WriteU32LE(0);
        writer.WriteU32LE(0);
    }

    private static void WritePixelFormat(ref ByteBufferWriter writer, DdsPixelFormatData format)
    {
        writer.WriteU32LE(DdsConstants.PixelFormatSize);
        writer.WriteU32LE((uint)format.Flags);
        writer.WriteU32LE(format.FourCC);
        writer.WriteU32LE(format.RGBBitCount);
        writer.WriteU32LE(format.RBitMask);
        writer.WriteU32LE(format.GBitMask);
        writer.WriteU32LE(format.BBitMask);
        writer.WriteU32LE(format.ABitMask);
    }

    private static void WriteSurfaces(ref ByteBufferWriter writer, DdsTextureData data)
    {
        foreach (var surface in data.Surfaces)
        {
            foreach (var mip in surface.MipLevels)
            {
                writer.Write(mip.Data);
            }
        }
    }

    private static int EstimateSize(DdsTextureData data)
    {
        var headerSize = DdsConstants.FullHeaderSize;
        var dataSize = 0;

        foreach (var surface in data.Surfaces)
        {
            foreach (var mip in surface.MipLevels)
            {
                dataSize += mip.Data.Length;
            }
        }

        return headerSize + dataSize + 256;
    }

    #endregion
}
