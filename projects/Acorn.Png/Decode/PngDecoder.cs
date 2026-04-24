using System.IO.Compression;
using Acorn.Frame;
using Acorn.Png.Data;

namespace Acorn.Png.Decode;

/// <summary>
///     PNG 文件解码器，将 PNG 图像格式解码为 C# 数据结构。
/// </summary>
/// <remarks>
///     PNG 是无损压缩的位图格式，使用 zlib/deflate 压缩，支持灰度、索引色、真彩色和 Alpha 通道。
///     解码器解析所有块，合并 IDAT 块并解压像素数据。
/// </remarks>
public ref struct PngDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="PngDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">PNG 二进制数据。</param>
    public PngDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置。
    /// </summary>
    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    /// <summary>
    ///     解码 PNG 文件。
    /// </summary>
    /// <returns>PNG 图像数据。</returns>
    public PngImageData Decode()
    {
        VerifySignature();

        int width = 0;
        int height = 0;
        byte bitDepth = 0;
        PngColorType colorType = 0;
        PngCompressionMethod compressionMethod = 0;
        PngFilterMethod filterMethod = 0;
        PngInterlaceMethod interlaceMethod = 0;
        var palette = Array.Empty<byte>();
        var transparency = Array.Empty<byte>();
        var idatData = new List<byte[]>();
        var ancillaryChunks = new List<PngChunk>();

        while (!_buffer.IsEnd)
        {
            var chunkLength = _buffer.ReadU32BE();
            var chunkType = _buffer.ReadString(4);
            var chunkData = chunkLength > 0 ? _buffer.ReadBytes((int)chunkLength).ToArray() : [];
            var crc = _buffer.ReadU32BE();

            switch (chunkType)
            {
                case PngConstants.IhdrTag:
                    width = (int)((uint)(chunkData[0] << 24) | (uint)(chunkData[1] << 16) | (uint)(chunkData[2] << 8) | chunkData[3]);
                    height = (int)((uint)(chunkData[4] << 24) | (uint)(chunkData[5] << 16) | (uint)(chunkData[6] << 8) | chunkData[7]);
                    bitDepth = chunkData[8];
                    colorType = (PngColorType)chunkData[9];
                    compressionMethod = (PngCompressionMethod)chunkData[10];
                    filterMethod = (PngFilterMethod)chunkData[11];
                    interlaceMethod = (PngInterlaceMethod)chunkData[12];
                    break;

                case PngConstants.PlteTag:
                    palette = chunkData;
                    break;

                case PngConstants.TrnsTag:
                    transparency = chunkData;
                    break;

                case PngConstants.IdatTag:
                    idatData.Add(chunkData);
                    break;

                case PngConstants.IendTag:
                    goto Done;

                default:
                    ancillaryChunks.Add(new PngChunk { Type = chunkType, Data = chunkData });
                    break;
            }
        }

    Done:
        var rawPixelData = DecompressIdat(idatData);

        return new PngImageData
        {
            Width = width,
            Height = height,
            BitDepth = bitDepth,
            ColorType = colorType,
            CompressionMethod = compressionMethod,
            FilterMethod = filterMethod,
            InterlaceMethod = interlaceMethod,
            Palette = palette,
            Transparency = transparency,
            RawPixelData = rawPixelData,
            AncillaryChunks = ancillaryChunks
        };
    }

    /// <summary>
    ///     仅解码 PNG 文件头信息。
    /// </summary>
    public (int Width, int Height, byte BitDepth, PngColorType ColorType) DecodeHeader()
    {
        VerifySignature();

        var length = _buffer.ReadU32BE();
        var type = _buffer.ReadString(4);

        if (type != PngConstants.IhdrTag)
        {
            throw new InvalidDataException($"PNG 第一个块不是 IHDR，实际为 \"{type}\"");
        }

        var width = (int)_buffer.ReadU32BE();
        var height = (int)_buffer.ReadU32BE();
        var bitDepth = _buffer.ReadU8();
        var colorType = (PngColorType)_buffer.ReadU8();

        return (width, height, bitDepth, colorType);
    }

    #region 私有解析方法

    private void VerifySignature()
    {
        if (_buffer.Remaining < PngConstants.SignatureLength)
        {
            throw new InvalidDataException("PNG 文件数据过短");
        }

        for (var i = 0; i < PngConstants.SignatureLength; i++)
        {
            var b = _buffer.ReadU8();
            var expected = PngConstants.Signature[i];

            if (b != expected)
            {
                throw new InvalidDataException("PNG 文件签名不匹配");
            }
        }
    }

    private static byte[] DecompressIdat(List<byte[]> idatChunks)
    {
        if (idatChunks.Count == 0)
        {
            return [];
        }

        var totalLength = 0;

        foreach (var chunk in idatChunks)
        {
            totalLength += chunk.Length;
        }

        var compressed = new byte[totalLength];
        var offset = 0;

        foreach (var chunk in idatChunks)
        {
            Buffer.BlockCopy(chunk, 0, compressed, offset, chunk.Length);
            offset += chunk.Length;
        }

        using var inputStream = new MemoryStream(compressed);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        deflateStream.CopyTo(outputStream);

        return outputStream.ToArray();
    }

    #endregion
}
