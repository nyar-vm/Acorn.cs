using System.IO.Compression;
using Acorn.Frame;
using Acorn.Hashing;
using Acorn.Png.Data;

namespace Acorn.Png.Encode;

/// <summary>
///     PNG 文件编码器，将 C# 数据结构编码为 PNG 图像格式。
/// </summary>
/// <remarks>
///     PNG 是无损压缩的位图格式，使用 zlib/deflate 压缩，支持灰度、索引色、真彩色和 Alpha 通道。
///     编码器生成符合 PNG 规范的二进制数据。
/// </remarks>
public sealed class PngEncoder
{
    /// <summary>
    ///     将 PNG 图像数据编码为 PNG 二进制格式。
    /// </summary>
    /// <param name="data">PNG 图像数据。</param>
    /// <returns>PNG 二进制数据。</returns>
    public byte[] Encode(PngImageData data)
    {
        var size = EstimateSize(data);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        WriteSignature(ref writer);
        WriteIhdrChunk(ref writer, data);

        if (data.Palette.Length > 0)
        {
            WritePlteChunk(ref writer, data.Palette);
        }

        if (data.Transparency.Length > 0)
        {
            WriteChunk(ref writer, PngConstants.TrnsTag, data.Transparency);
        }

        WriteIdatChunks(ref writer, data);

        foreach (var chunk in data.AncillaryChunks)
        {
            WriteChunk(ref writer, chunk.Type, chunk.Data);
        }

        WriteIendChunk(ref writer);

        return buffer[..writer.Position];
    }

    #region 私有编码方法

    private static void WriteSignature(ref ByteBufferWriter writer)
    {
        writer.Write(PngConstants.Signature);
    }

    private static void WriteIhdrChunk(ref ByteBufferWriter writer, PngImageData data)
    {
        var ihdrData = new byte[PngConstants.IhdrDataLength];
        var ihdrWriter = new ByteBufferWriter(ihdrData);

        ihdrWriter.WriteU32BE((uint)data.Width);
        ihdrWriter.WriteU32BE((uint)data.Height);
        ihdrWriter.WriteU8(data.BitDepth);
        ihdrWriter.WriteU8((byte)data.ColorType);
        ihdrWriter.WriteU8((byte)data.CompressionMethod);
        ihdrWriter.WriteU8((byte)data.FilterMethod);
        ihdrWriter.WriteU8((byte)data.InterlaceMethod);

        WriteChunk(ref writer, PngConstants.IhdrTag, ihdrData[..ihdrWriter.Position]);
    }

    private static void WritePlteChunk(ref ByteBufferWriter writer, byte[] palette)
    {
        WriteChunk(ref writer, PngConstants.PlteTag, palette);
    }

    private static void WriteIdatChunks(ref ByteBufferWriter writer, PngImageData data)
    {
        byte[] compressedData;

        if (data.RawPixelData.Length > 0)
        {
            compressedData = CompressData(data.RawPixelData);
        }
        else
        {
            var rawRowSize = data.Width * data.BytesPerPixel;
            var stride = rawRowSize + 1;
            var totalSize = stride * data.Height;
            var rawData = new byte[totalSize];

            for (var y = 0; y < data.Height; y++)
            {
                rawData[y * stride] = (byte)PngFilterType.None;
            }

            compressedData = CompressData(rawData);
        }

        WriteChunk(ref writer, PngConstants.IdatTag, compressedData);
    }

    private static void WriteIendChunk(ref ByteBufferWriter writer)
    {
        WriteChunk(ref writer, PngConstants.IendTag, []);
    }

    private static void WriteChunk(ref ByteBufferWriter writer, string type, ReadOnlySpan<byte> data)
    {
        writer.WriteU32BE((uint)data.Length);
        writer.WriteString(type);
        writer.Write(data);
        var crc = ComputeCrc(type, data);
        writer.WriteU32BE(crc);
    }

    private static byte[] CompressData(byte[] data)
    {
        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflateStream.Write(data, 0, data.Length);
        }

        return outputStream.ToArray();
    }

    private static uint ComputeCrc(string type, ReadOnlySpan<byte> data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        var crc = new Acorn.Hashing.Crc32();

        crc.Update(typeBytes);
        crc.Update(data);

        return crc.Value;
    }

    private static int EstimateSize(PngImageData data)
    {
        var rawRowSize = data.Width * data.BytesPerPixel + 1;
        var rawSize = rawRowSize * data.Height;
        return PngConstants.SignatureLength + rawSize + 4096 + data.Palette.Length + data.AncillaryChunks.Sum(c => c.Data.Length + 12);
    }

    #endregion
}


