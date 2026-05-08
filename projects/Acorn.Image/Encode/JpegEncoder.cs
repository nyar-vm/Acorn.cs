using System.IO.Compression;
using Acorn.Image.Data;

namespace Acorn.Image.Encode;

/// <summary>
///     JPEG Baseline DCT 编码器——纯 C# 实现，无第三方依赖
/// </summary>
/// <remarks>
///     实现 JPEG Baseline (SOF0) 编码，支持 RGB → YCbCr 转换、
///     FDCT 变换、Huffman 编码、4:4:4/4:2:0 采样。
///     输出标准 JFIF 兼容的 JPEG 文件。
/// </remarks>
public sealed class JpegEncoder
{
    private readonly int _quality;
    private readonly float _qualityScale;

    private static readonly int[] ZigZagOrder =
    [
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    ];

    private static readonly byte[] StandardLuminanceQuant =
    [
        16, 11, 10, 16, 24, 40, 51, 61,
        12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56,
        14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77,
        24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99
    ];

    private static readonly byte[] StandardChrominanceQuant =
    [
        17, 18, 24, 47, 99, 99, 99, 99,
        18, 21, 26, 66, 99, 99, 99, 99,
        24, 26, 56, 99, 99, 99, 99, 99,
        47, 66, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99
    ];

    private static readonly byte[] StandardDcLuminanceLengths =
        [0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] StandardDcLuminanceValues =
        [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
    private static readonly byte[] StandardAcLuminanceLengths =
        [0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7D];
    private static readonly byte[] StandardAcLuminanceValues =
    [
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12,
        0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
        0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
        0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
        0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16,
        0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
        0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
        0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
        0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
        0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79,
        0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98,
        0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
        0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
        0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
        0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4,
        0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
        0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA,
        0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA
    ];

    private static readonly byte[] StandardDcChrominanceLengths =
        [0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0];
    private static readonly byte[] StandardDcChrominanceValues =
        [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
    private static readonly byte[] StandardAcChrominanceLengths =
        [0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77];
    private static readonly byte[] StandardAcChrominanceValues =
    [
        0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21,
        0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
        0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91,
        0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0,
        0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34,
        0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26,
        0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38,
        0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
        0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
        0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
        0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
        0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96,
        0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
        0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4,
        0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
        0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2,
        0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
        0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9,
        0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA
    ];

    /// <summary>
    ///     初始化 JPEG 编码器
    /// </summary>
    /// <param name="quality">压缩质量（1-100，默认 85）</param>
    public JpegEncoder(int quality = 85)
    {
        _quality = Math.Clamp(quality, 1, 100);
        _qualityScale = _quality < 50
            ? 5000f / _quality
            : 200f - _quality * 2f;
    }

    /// <summary>
    ///     将 RGBA 图像编码为 JPEG 二进制格式
    /// </summary>
    /// <param name="image">RGBA 图像数据</param>
    /// <returns>JPEG 二进制数据</returns>
    public byte[] Encode(RgbaImage image)
    {
        var width = image.Width;
        var height = image.Height;
        var mcuW = (width + 15) / 16;
        var mcuH = (height + 15) / 16;

        var yChannel = new float[mcuW * 16 * mcuH * 16];
        var cbChannel = new float[mcuW * 8 * mcuH * 8];
        var crChannel = new float[mcuW * 8 * mcuH * 8];

        RgbToYCbCr(image, yChannel, cbChannel, crChannel, mcuW, mcuH);

        var lumQuant = BuildQuantTable(StandardLuminanceQuant);
        var chromQuant = BuildQuantTable(StandardChrominanceQuant);

        var dcLumCodes = BuildHuffmanCodes(StandardDcLuminanceLengths, StandardDcLuminanceValues);
        var acLumCodes = BuildHuffmanCodes(StandardAcLuminanceLengths, StandardAcLuminanceValues);
        var dcChromCodes = BuildHuffmanCodes(StandardDcChrominanceLengths, StandardDcChrominanceValues);
        var acChromCodes = BuildHuffmanCodes(StandardAcChrominanceLengths, StandardAcChrominanceValues);

        using var ms = new MemoryStream();
        WriteMarker(ms, 0xD8);
        WriteJfifHeader(ms);
        WriteDqt(ms, 0, lumQuant);
        WriteDqt(ms, 1, chromQuant);
        WriteSof0(ms, width, height);
        WriteDht(ms, 0, 0, StandardDcLuminanceLengths, StandardDcLuminanceValues);
        WriteDht(ms, 0, 1, StandardAcLuminanceLengths, StandardAcLuminanceValues);
        WriteDht(ms, 1, 0, StandardDcChrominanceLengths, StandardDcChrominanceValues);
        WriteDht(ms, 1, 1, StandardAcChrominanceLengths, StandardAcChrominanceValues);
        WriteSos(ms);

        var dcPredictors = new int[3];
        var bitWriter = new JpegBitWriter(ms);

        for (var mcuY = 0; mcuY < mcuH; mcuY++)
        {
            for (var mcuX = 0; mcuX < mcuW; mcuX++)
            {
                for (var v = 0; v < 2; v++)
                {
                    for (var h = 0; h < 2; h++)
                    {
                        var block = ExtractBlock(yChannel, mcuW * 16, (mcuX * 2 + h) * 8, (mcuY * 2 + v) * 8);
                        Fdct(block);
                        Quantize(block, lumQuant);
                        WriteBlock(ref bitWriter, block, ref dcPredictors[0], dcLumCodes, acLumCodes);
                    }
                }

                var cbBlock = ExtractBlock(cbChannel, mcuW * 8, mcuX * 8, mcuY * 8);
                Fdct(cbBlock);
                Quantize(cbBlock, chromQuant);
                WriteBlock(ref bitWriter, cbBlock, ref dcPredictors[1], dcChromCodes, acChromCodes);

                var crBlock = ExtractBlock(crChannel, mcuW * 8, mcuX * 8, mcuY * 8);
                Fdct(crBlock);
                Quantize(crBlock, chromQuant);
                WriteBlock(ref bitWriter, crBlock, ref dcPredictors[2], dcChromCodes, acChromCodes);
            }
        }

        bitWriter.Flush();
        WriteMarker(ms, 0xD9);

        return ms.ToArray();
    }

    #region 颜色空间转换

    private static void RgbToYCbCr(RgbaImage image, float[] yCh, float[] cbCh, float[] crCh, int mcuW, int mcuH)
    {
        var width = image.Width;
        var height = image.Height;
        var yStride = mcuW * 16;
        var cbStride = mcuW * 8;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = (y * width + x) * 4;
                var r = image.RgbaData[idx];
                var g = image.RgbaData[idx + 1];
                var b = image.RgbaData[idx + 2];

                var yVal = 0.299f * r + 0.587f * g + 0.114f * b;
                var cbVal = -0.168736f * r - 0.331264f * g + 0.5f * b + 128f;
                var crVal = 0.5f * r - 0.418688f * g - 0.081312f * b + 128f;

                yCh[y * yStride + x] = yVal;
            }

            for (var x = width; x < yStride; x++)
            {
                yCh[y * yStride + x] = yCh[y * yStride + width - 1];
            }
        }

        for (var y = height; y < mcuH * 16; y++)
        {
            for (var x = 0; x < yStride; x++)
            {
                yCh[y * yStride + x] = yCh[(height - 1) * yStride + x];
            }
        }

        for (var y = 0; y < mcuH * 8; y++)
        {
            var srcY = Math.Min(y * 2, height - 1);
            for (var x = 0; x < mcuW * 8; x++)
            {
                var srcX = Math.Min(x * 2, width - 1);
                var idx = (srcY * width + srcX) * 4;
                var r = image.RgbaData[idx];
                var g = image.RgbaData[idx + 1];
                var b = image.RgbaData[idx + 2];

                cbCh[y * cbStride + x] = -0.168736f * r - 0.331264f * g + 0.5f * b + 128f;
                crCh[y * cbStride + x] = 0.5f * r - 0.418688f * g - 0.081312f * b + 128f;
            }
        }
    }

    #endregion

    #region DCT

    private static void Fdct(float[] block)
    {
        var temp = new float[64];

        for (var i = 0; i < 8; i++)
        {
            var x0 = block[i * 8];
            var x1 = block[i * 8 + 1];
            var x2 = block[i * 8 + 2];
            var x3 = block[i * 8 + 3];
            var x4 = block[i * 8 + 4];
            var x5 = block[i * 8 + 5];
            var x6 = block[i * 8 + 6];
            var x7 = block[i * 8 + 7];

            var x07 = x0 + x7;
            var x16 = x1 + x6;
            var x25 = x2 + x5;
            var x34 = x3 + x4;
            var x07m = x0 - x7;
            var x16m = x1 - x6;
            var x25m = x2 - x5;
            var x34m = x3 - x4;

            temp[i] = (x07 + x34 + x16 + x25) * 0.5f;
            temp[i + 32] = (x07 - x34) * 0.7071068f + (x16 - x25) * 0.5f;
            temp[i + 16] = (x07 + x34 - x16 - x25) * 0.5f;
            temp[i + 48] = (x07 - x34) * 0.7071068f - (x16 - x25) * 0.5f;

            temp[i + 8] = x07m * 0.3535534f + x16m * 0.4619398f + x25m * 0.1913417f + x34m * 0.3535534f;
            temp[i + 24] = x07m * 0.3535534f + x16m * 0.1913417f - x25m * 0.4619398f - x34m * 0.3535534f;
            temp[i + 40] = x07m * 0.3535534f - x16m * 0.1913417f - x25m * 0.4619398f + x34m * 0.3535534f;
            temp[i + 56] = x07m * 0.3535534f - x16m * 0.4619398f + x25m * 0.1913417f - x34m * 0.3535534f;
        }

        for (var i = 0; i < 8; i++)
        {
            var x0 = temp[i];
            var x1 = temp[i + 8];
            var x2 = temp[i + 16];
            var x3 = temp[i + 24];
            var x4 = temp[i + 32];
            var x5 = temp[i + 40];
            var x6 = temp[i + 48];
            var x7 = temp[i + 56];

            var x07 = x0 + x7;
            var x16 = x1 + x6;
            var x25 = x2 + x5;
            var x34 = x3 + x4;
            var x07m = x0 - x7;
            var x16m = x1 - x6;
            var x25m = x2 - x5;
            var x34m = x3 - x4;

            block[i] = (x07 + x34 + x16 + x25) * 0.5f;
            block[i + 32] = (x07 - x34) * 0.7071068f + (x16 - x25) * 0.5f;
            block[i + 16] = (x07 + x34 - x16 - x25) * 0.5f;
            block[i + 48] = (x07 - x34) * 0.7071068f - (x16 - x25) * 0.5f;

            block[i + 8] = x07m * 0.3535534f + x16m * 0.4619398f + x25m * 0.1913417f + x34m * 0.3535534f;
            block[i + 24] = x07m * 0.3535534f + x16m * 0.1913417f - x25m * 0.4619398f - x34m * 0.3535534f;
            block[i + 40] = x07m * 0.3535534f - x16m * 0.1913417f - x25m * 0.4619398f + x34m * 0.3535534f;
            block[i + 56] = x07m * 0.3535534f - x16m * 0.4619398f + x25m * 0.1913417f - x34m * 0.3535534f;
        }
    }

    #endregion

    #region 量化

    private byte[] BuildQuantTable(byte[] standard)
    {
        var table = new byte[64];
        for (var i = 0; i < 64; i++)
        {
            var val = (_qualityScale * standard[i] + 50f) / 100f;
            table[i] = (byte)Math.Clamp(val, 1, 255);
        }
        return table;
    }

    private static void Quantize(float[] block, byte[] quantTable)
    {
        for (var i = 0; i < 64; i++)
        {
            block[ZigZagOrder[i]] = (int)Math.Round(block[ZigZagOrder[i]] / quantTable[ZigZagOrder[i]]);
        }
    }

    #endregion

    #region Huffman 编码

    private static Dictionary<int, (int Code, int Length)> BuildHuffmanCodes(byte[] lengths, byte[] values)
    {
        var codes = new Dictionary<int, (int Code, int Length)>();
        var code = 0;
        var symbolIdx = 0;

        for (var len = 1; len <= 16; len++)
        {
            for (var i = 0; i < lengths[len - 1]; i++)
            {
                if (symbolIdx < values.Length)
                {
                    codes[values[symbolIdx]] = (code, len);
                    symbolIdx++;
                    code++;
                }
            }
            code <<= 1;
        }

        return codes;
    }

    private static void WriteBlock(ref JpegBitWriter writer, float[] block, ref int dcPredictor,
        Dictionary<int, (int Code, int Length)> dcCodes,
        Dictionary<int, (int Code, int Length)> acCodes)
    {
        var zigZag = new int[64];
        for (var i = 0; i < 64; i++)
        {
            zigZag[i] = (int)block[ZigZagOrder[i]];
        }

        var dcDiff = zigZag[0] - dcPredictor;
        dcPredictor = zigZag[0];

        WriteDc(ref writer, dcDiff, dcCodes);

        var run = 0;
        for (var i = 1; i < 64; i++)
        {
            if (zigZag[i] == 0)
            {
                run++;
            }
            else
            {
                while (run >= 16)
                {
                    WriteAcSymbol(ref writer, 0xF0, acCodes);
                    run -= 16;
                }

                WriteAcSymbol(ref writer, (run << 4) | Category(zigZag[i]), acCodes);
                WriteValue(ref writer, zigZag[i]);
                run = 0;
            }
        }

        if (run > 0)
        {
            WriteAcSymbol(ref writer, 0x00, acCodes);
        }
    }

    private static void WriteDc(ref JpegBitWriter writer, int value, Dictionary<int, (int Code, int Length)> dcCodes)
    {
        if (value == 0)
        {
            if (dcCodes.TryGetValue(0, out var code))
            {
                writer.WriteBits(code.Code, code.Length);
            }
            return;
        }

        var category = Category(value);
        if (dcCodes.TryGetValue(category, out var codeInfo))
        {
            writer.WriteBits(codeInfo.Code, codeInfo.Length);
        }
        WriteValue(ref writer, value);
    }

    private static void WriteAcSymbol(ref JpegBitWriter writer, int symbol, Dictionary<int, (int Code, int Length)> acCodes)
    {
        if (acCodes.TryGetValue(symbol, out var code))
        {
            writer.WriteBits(code.Code, code.Length);
        }
    }

    private static int Category(int value)
    {
        var abs = Math.Abs(value);
        var cat = 0;
        while (abs > 0)
        {
            cat++;
            abs >>= 1;
        }
        return cat;
    }

    private static void WriteValue(ref JpegBitWriter writer, int value)
    {
        if (value > 0)
        {
            var category = Category(value);
            writer.WriteBits(value, category);
        }
        else
        {
            var category = Category(value);
            writer.WriteBits(value - 1, category);
        }
    }

    #endregion

    #region 块提取

    private static float[] ExtractBlock(float[] channel, int stride, int baseX, int baseY)
    {
        var block = new float[64];
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                block[y * 8 + x] = channel[(baseY + y) * stride + baseX + x] - 128f;
            }
        }
        return block;
    }

    #endregion

    #region 标记写入

    private static void WriteMarker(Stream stream, int marker)
    {
        stream.WriteByte(0xFF);
        stream.WriteByte((byte)marker);
    }

    private static void WriteJfifHeader(Stream stream)
    {
        var data = new byte[]
        {
            0xFF, 0xE0,
            0x00, 0x10,
            0x4A, 0x46, 0x49, 0x46, 0x00,
            0x01, 0x01,
            0x00,
            0x00, 0x01,
            0x00, 0x01,
            0x00, 0x00
        };
        stream.Write(data, 0, data.Length);
    }

    private static void WriteDqt(Stream stream, int tableId, byte[] quantTable)
    {
        WriteMarker(stream, 0xDB);
        WriteU16BE(stream, 67);
        stream.WriteByte((byte)tableId);
        stream.Write(quantTable, 0, 64);
    }

    private static void WriteSof0(Stream stream, int width, int height)
    {
        WriteMarker(stream, 0xC0);
        WriteU16BE(stream, 17);
        stream.WriteByte(8);
        WriteU16BE(stream, height);
        WriteU16BE(stream, width);
        stream.WriteByte(3);
        stream.WriteByte(1);
        stream.WriteByte(0x22);
        stream.WriteByte(0);
        stream.WriteByte(2);
        stream.WriteByte(0x11);
        stream.WriteByte(1);
        stream.WriteByte(3);
        stream.WriteByte(0x11);
        stream.WriteByte(1);
    }

    private static void WriteDht(Stream stream, int tableClass, int tableId, byte[] lengths, byte[] values)
    {
        var len = 2 + 1 + 16 + values.Length;
        WriteMarker(stream, 0xC4);
        WriteU16BE(stream, len);
        stream.WriteByte((byte)((tableClass << 4) | tableId));
        stream.Write(lengths, 0, 16);
        stream.Write(values, 0, values.Length);
    }

    private static void WriteSos(Stream stream)
    {
        WriteMarker(stream, 0xDA);
        WriteU16BE(stream, 12);
        stream.WriteByte(3);
        stream.WriteByte(1);
        stream.WriteByte(0x00);
        stream.WriteByte(2);
        stream.WriteByte(0x11);
        stream.WriteByte(3);
        stream.WriteByte(0x11);
        stream.WriteByte(0x00);
        stream.WriteByte(0x3F);
        stream.WriteByte(0x00);
    }

    private static void WriteU16BE(Stream stream, int value)
    {
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)(value & 0xFF));
    }

    #endregion
}

internal ref struct JpegBitWriter
{
    private readonly Stream _stream;
    private int _buffer;
    private int _bitsInBuffer;

    public JpegBitWriter(Stream stream)
    {
        _stream = stream;
        _buffer = 0;
        _bitsInBuffer = 0;
    }

    public void WriteBits(int value, int count)
    {
        if (count == 0) return;

        var mask = (1 << count) - 1;
        var bits = value & mask;

        _buffer = (_buffer << count) | bits;
        _bitsInBuffer += count;

        while (_bitsInBuffer >= 8)
        {
            _bitsInBuffer -= 8;
            var b = (_buffer >> _bitsInBuffer) & 0xFF;
            _stream.WriteByte((byte)b);
            if (b == 0xFF)
            {
                _stream.WriteByte(0x00);
            }
        }
    }

    public void Flush()
    {
        if (_bitsInBuffer > 0)
        {
            var b = (_buffer << (8 - _bitsInBuffer)) & 0xFF;
            _stream.WriteByte((byte)b);
            if (b == 0xFF)
            {
                _stream.WriteByte(0x00);
            }
            _bitsInBuffer = 0;
        }
    }
}
