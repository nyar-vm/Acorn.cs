using Acorn.Jpeg.Data;

namespace Acorn.Jpeg.Decode;

/// <summary>
///     JPEG Baseline DCT 解码器——纯 C# 实现，无第三方依赖。
/// </summary>
/// <remarks>
///     实现了 JPEG Baseline (SOF0) 解码，支持 YCbCr 4:4:4/4:2:0/4:2:2 采样、
///     Huffman 解码、IDCT、颜色空间转换。
///     不支持 Progressive (SOF2)、算术编码、分层编码。
/// </remarks>
public ref struct JpegDecoder
{
    private ReadOnlySpan<byte> _data;
    private int _position;

    private int _width;
    private int _height;
    private int _precision;
    private JpegComponent[] _components;
    private int _maxH;
    private int _maxV;
    private byte[][] _quantTables;
    private JpegHuffmanTable[] _dcTables;
    private JpegHuffmanTable[] _acTables;
    private int[] _dcPredictors;

    /// <summary>
    ///     初始化 <see cref="JpegDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">JPEG 二进制数据。</param>
    public JpegDecoder(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
        _components = [];
        _maxH = 1;
        _maxV = 1;
        _quantTables = new byte[4][];
        _dcTables = new JpegHuffmanTable[4];
        _acTables = new JpegHuffmanTable[4];
        _dcPredictors = new int[4];
    }

    /// <summary>
    ///     解码 JPEG 图像为像素数据。
    /// </summary>
    /// <returns>JPEG 图像数据。</returns>
    public JpegImageData Decode()
    {
        ParseMarkers();

        var mcuWidth = (_width + _maxH * 8 - 1) / (_maxH * 8);
        var mcuHeight = (_height + _maxV * 8 - 1) / (_maxV * 8);

        var componentCount = _components.Length;
        var channelData = new float[componentCount][];

        for (var c = 0; c < componentCount; c++)
        {
            var comp = _components[c];
            var fullW = mcuWidth * comp.H * 8;
            var fullH = mcuHeight * comp.V * 8;
            channelData[c] = new float[fullW * fullH];
        }

        var bitReader = new JpegBitReader(_data, _position);

        for (var mcuY = 0; mcuY < mcuHeight; mcuY++)
        {
            for (var mcuX = 0; mcuX < mcuWidth; mcuX++)
            {
                for (var c = 0; c < componentCount; c++)
                {
                    var comp = _components[c];
                    var qt = _quantTables[comp.QuantTableId];
                    var dcTable = _dcTables[comp.DcTableId];
                    var acTable = _acTables[comp.AcTableId];

                    for (var v = 0; v < comp.V; v++)
                    {
                        for (var h = 0; h < comp.H; h++)
                        {
                            var block = new float[64];
                            DecodeBlock(ref bitReader, dcTable, acTable, qt, ref _dcPredictors[c], block);
                            Idct(block);

                            var blockX = mcuX * comp.H + h;
                            var blockY = mcuY * comp.V + v;
                            var baseX = blockX * 8;
                            var baseY = blockY * 8;
                            var stride = mcuWidth * comp.H * 8;

                            for (var y = 0; y < 8; y++)
                            {
                                for (var x = 0; x < 8; x++)
                                {
                                    var val = block[y * 8 + x] + 128f;
                                    channelData[c][(baseY + y) * stride + baseX + x] = Math.Clamp(val, 0, 255);
                                }
                            }
                        }
                    }
                }
            }
        }

        var pixelData = new byte[_width * _height * 4];
        var colorSpace = componentCount == 1 ? JpegColorSpace.Grayscale : JpegColorSpace.YCbCr;

        if (componentCount == 1)
        {
            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    var srcIdx = y * mcuWidth * _maxH * 8 + x;
                    var gray = (byte)channelData[0][srcIdx];
                    var dstIdx = (y * _width + x) * 4;
                    pixelData[dstIdx] = gray;
                    pixelData[dstIdx + 1] = gray;
                    pixelData[dstIdx + 2] = gray;
                    pixelData[dstIdx + 3] = 255;
                }
            }
        }
        else
        {
            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                {
                    var yIdx = y * mcuWidth * _maxH * 8 + x;
                    var cbX = x * _components[1].H / _maxH;
                    var cbY = y * _components[1].V / _maxV;
                    var cbStride = mcuWidth * _components[1].H * 8;
                    var cbIdx = cbY * cbStride + cbX;
                    var crX = x * _components[2].H / _maxH;
                    var crY = y * _components[2].V / _maxV;
                    var crStride = mcuWidth * _components[2].H * 8;
                    var crIdx = crY * crStride + crX;

                    var yVal = channelData[0][yIdx];
                    var cbVal = channelData[1][cbIdx] - 128f;
                    var crVal = channelData[2][crIdx] - 128f;

                    var r = yVal + 1.402f * crVal;
                    var g = yVal - 0.344136f * cbVal - 0.714136f * crVal;
                    var b = yVal + 1.772f * cbVal;

                    var dstIdx = (y * _width + x) * 4;
                    pixelData[dstIdx] = (byte)Math.Clamp(r, 0, 255);
                    pixelData[dstIdx + 1] = (byte)Math.Clamp(g, 0, 255);
                    pixelData[dstIdx + 2] = (byte)Math.Clamp(b, 0, 255);
                    pixelData[dstIdx + 3] = 255;
                }
            }
        }

        var componentInfos = new JpegComponentInfo[componentCount];

        for (var i = 0; i < componentCount; i++)
        {
            var comp = _components[i];
            componentInfos[i] = new JpegComponentInfo
            {
                Id = comp.Id,
                H = comp.H,
                V = comp.V,
                QuantTableId = comp.QuantTableId,
                DcTableId = comp.DcTableId,
                AcTableId = comp.AcTableId
            };
        }

        return new JpegImageData
        {
            Width = _width,
            Height = _height,
            Precision = _precision,
            ColorSpace = colorSpace,
            Components = componentInfos,
            PixelData = pixelData,
            IsProgressive = false
        };
    }

    #region 标记解析

    private void ParseMarkers()
    {
        while (_position < _data.Length - 1)
        {
            if (_data[_position++] != 0xFF) continue;

            var marker = _data[_position++];

            switch (marker)
            {
                case 0xD8: break;
                case 0xD9: return;
                case 0x00: break;
                case >= 0xD0 and <= 0xD7:
                    _dcPredictors[(marker - 0xD0) & 3] = 0;
                    break;
                case 0xDB: ParseDqt(); break;
                case 0xC0: ParseSof0(); break;
                case 0xC4: ParseDht(); break;
                case 0xDA: ParseSos(); return;
                default:
                    if (marker is not (0xD0 or 0xD1 or 0xD2 or 0xD3 or 0xD4 or 0xD5 or 0xD6 or 0xD7))
                    {
                        if (_position + 1 < _data.Length)
                        {
                            var len = (_data[_position] << 8) | _data[_position + 1];
                            _position += len;
                        }
                    }
                    break;
            }
        }
    }

    private void ParseSof0()
    {
        var len = ReadU16();
        _precision = _data[_position++];
        _height = ReadU16();
        _width = ReadU16();
        var numComponents = _data[_position++];

        _components = new JpegComponent[numComponents];
        _maxH = 0;
        _maxV = 0;

        for (var i = 0; i < numComponents; i++)
        {
            var id = _data[_position++];
            var sampling = _data[_position++];
            var qtId = _data[_position++];
            var h = (sampling >> 4) & 0x0F;
            var v = sampling & 0x0F;

            _components[i] = new JpegComponent(id, h, v, qtId);
            if (h > _maxH) _maxH = h;
            if (v > _maxV) _maxV = v;
        }
    }

    private void ParseDqt()
    {
        var len = ReadU16();
        var end = _position + len - 2;

        while (_position < end)
        {
            var info = _data[_position++];
            var tableId = info & 0x0F;
            var precision = (info >> 4) & 0x0F;

            _quantTables[tableId] = new byte[64];
            if (precision == 0)
            {
                for (var i = 0; i < 64; i++)
                {
                    _quantTables[tableId][i] = _data[_position++];
                }
            }
            else
            {
                for (var i = 0; i < 64; i++)
                {
                    _position += 2;
                    _quantTables[tableId][i] = _data[_position - 2];
                }
            }
        }
    }

    private void ParseDht()
    {
        var len = ReadU16();
        var end = _position + len - 2;

        while (_position < end)
        {
            var info = _data[_position++];
            var tableClass = (info >> 4) & 0x0F;
            var tableId = info & 0x0F;

            var counts = new int[16];
            var totalSymbols = 0;
            for (var i = 0; i < 16; i++)
            {
                counts[i] = _data[_position++];
                totalSymbols += counts[i];
            }

            var symbols = new byte[totalSymbols];
            for (var i = 0; i < totalSymbols; i++)
            {
                symbols[i] = _data[_position++];
            }

            var table = BuildHuffmanTable(counts, symbols);

            if (tableClass == 0)
            {
                _dcTables[tableId] = table;
            }
            else
            {
                _acTables[tableId] = table;
            }
        }
    }

    private void ParseSos()
    {
        var len = ReadU16();
        var numComponents = _data[_position++];

        for (var i = 0; i < numComponents; i++)
        {
            var componentId = _data[_position++];
            var tableInfo = _data[_position++];

            for (var c = 0; c < _components.Length; c++)
            {
                if (_components[c].Id == componentId)
                {
                    _components[c].DcTableId = (tableInfo >> 4) & 0x0F;
                    _components[c].AcTableId = tableInfo & 0x0F;
                    break;
                }
            }
        }

        _position += 3;
    }

    #endregion

    #region 块解码

    private static void DecodeBlock(ref JpegBitReader reader, JpegHuffmanTable dcTable,
        JpegHuffmanTable acTable, byte[] quantTable, ref int dcPredictor, float[] block)
    {
        Array.Clear(block);

        var dcCategory = DecodeHuffmanSymbol(ref reader, dcTable);
        var dcDiff = ReadExtend(ref reader, dcCategory);
        dcPredictor += dcDiff;
        block[0] = dcPredictor * quantTable[0];

        var k = 1;
        while (k < 64)
        {
            var acSymbol = DecodeHuffmanSymbol(ref reader, acTable);
            if (acSymbol == 0) break;

            var run = acSymbol >> 4;
            var category = acSymbol & 0x0F;
            k += run;

            if (k >= 64) break;
            if (category > 0)
            {
                var value = ReadExtend(ref reader, category);
                var zigIdx = k;
                if (zigIdx < 64)
                {
                    block[ZigZagOrder[zigIdx]] = value * quantTable[ZigZagOrder[zigIdx]];
                }
            }
            k++;
        }
    }

    private static int DecodeHuffmanSymbol(ref JpegBitReader reader, JpegHuffmanTable table)
    {
        var node = table.Root;
        while (node != null)
        {
            if (node.Symbol >= 0) return node.Symbol;
            var bit = reader.ReadBit();
            node = bit == 0 ? node.Zero : node.One;
        }
        return 0;
    }

    private static int ReadExtend(ref JpegBitReader reader, int category)
    {
        if (category == 0) return 0;
        var value = reader.ReadBits(category);
        var threshold = 1 << (category - 1);
        if (value < threshold)
        {
            value += (-1 << category) + 1;
        }
        return value;
    }

    private static void Idct(float[] block)
    {
        var temp = new float[64];

        for (var i = 0; i < 8; i++)
        {
            var x0 = block[i * 8];
            var x1 = block[i * 8 + 4];
            var x2 = block[i * 8 + 2];
            var x3 = block[i * 8 + 6];
            var x4 = block[i * 8 + 1];
            var x5 = block[i * 8 + 5];
            var x6 = block[i * 8 + 3];
            var x7 = block[i * 8 + 7];

            var x07 = x0 + x7;
            var x16 = x1 + x6;
            var x25 = x2 + x5;
            var x34 = x3 + x4;
            var x07m = x0 - x7;
            var x16m = x1 - x6;
            var x25m = x2 - x5;
            var x34m = x3 - x4;

            var even0 = x07 + x34;
            var even1 = x16 + x25;
            var even2 = x07 - x34;
            var even3 = x16 - x25;

            temp[i * 8] = even0 + even1;
            temp[i * 8 + 4] = even0 - even1;
            temp[i * 8 + 2] = even2 * 1.4142135f + even3 * 0.7071068f;
            temp[i * 8 + 6] = even2 * 1.4142135f - even3 * 0.7071068f;

            temp[i * 8 + 1] = x07m * 0.7071068f + x16m * 0.9238795f + x25m * 0.3826834f + x34m * 0.7071068f;
            temp[i * 8 + 3] = x07m * 0.7071068f + x16m * 0.3826834f - x25m * 0.9238795f - x34m * 0.7071068f;
            temp[i * 8 + 5] = x07m * 0.7071068f - x16m * 0.3826834f - x25m * 0.9238795f + x34m * 0.7071068f;
            temp[i * 8 + 7] = x07m * 0.7071068f - x16m * 0.9238795f + x25m * 0.3826834f - x34m * 0.7071068f;
        }

        for (var i = 0; i < 8; i++)
        {
            var x0 = temp[i];
            var x1 = temp[i + 32];
            var x2 = temp[i + 16];
            var x3 = temp[i + 48];
            var x4 = temp[i + 8];
            var x5 = temp[i + 40];
            var x6 = temp[i + 24];
            var x7 = temp[i + 56];

            var x07 = x0 + x7;
            var x16 = x1 + x6;
            var x25 = x2 + x5;
            var x34 = x3 + x4;
            var x07m = x0 - x7;
            var x16m = x1 - x6;
            var x25m = x2 - x5;
            var x34m = x3 - x4;

            var even0 = x07 + x34;
            var even1 = x16 + x25;

            block[i] = (even0 + even1) / 8f;
            block[i + 32] = (even0 - even1) / 8f;
            block[i + 16] = ((x07 - x34) * 1.4142135f + (x16 - x25) * 0.7071068f) / 8f;
            block[i + 48] = ((x07 - x34) * 1.4142135f - (x16 - x25) * 0.7071068f) / 8f;

            block[i + 8] = (x07m * 0.7071068f + x16m * 0.9238795f + x25m * 0.3826834f + x34m * 0.7071068f) / 8f;
            block[i + 24] = (x07m * 0.7071068f + x16m * 0.3826834f - x25m * 0.9238795f - x34m * 0.7071068f) / 8f;
            block[i + 40] = (x07m * 0.7071068f - x16m * 0.3826834f - x25m * 0.9238795f + x34m * 0.7071068f) / 8f;
            block[i + 56] = (x07m * 0.7071068f - x16m * 0.9238795f + x25m * 0.3826834f - x34m * 0.7071068f) / 8f;
        }
    }

    #endregion

    #region Huffman 表构建

    private static JpegHuffmanTable BuildHuffmanTable(int[] counts, byte[] symbols)
    {
        var root = new JpegHuffmanNode();
        var symbolIdx = 0;
        var code = 0;

        for (var len = 1; len <= 16; len++)
        {
            for (var i = 0; i < counts[len - 1]; i++)
            {
                if (symbolIdx >= symbols.Length) break;

                var node = root;
                for (var bit = len - 1; bit >= 0; bit--)
                {
                    var b = (code >> bit) & 1;
                    if (b == 0)
                    {
                        node.Zero ??= new JpegHuffmanNode();
                        node = node.Zero;
                    }
                    else
                    {
                        node.One ??= new JpegHuffmanNode();
                        node = node.One;
                    }
                }
                node.Symbol = symbols[symbolIdx];
                symbolIdx++;
                code++;
            }
            code <<= 1;
        }

        return new JpegHuffmanTable(root);
    }

    #endregion

    #region 辅助方法

    private ushort ReadU16()
    {
        var value = (ushort)((_data[_position] << 8) | _data[_position + 1]);
        _position += 2;
        return value;
    }

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

    #endregion
}

#region 内部类型

internal sealed class JpegHuffmanTable
{
    /// <summary>
    ///     Huffman 树根节点。
    /// </summary>
    public JpegHuffmanNode Root { get; }

    /// <summary>
    ///     初始化 <see cref="JpegHuffmanTable" /> 类的新实例。
    /// </summary>
    /// <param name="root">Huffman 树根节点。</param>
    public JpegHuffmanTable(JpegHuffmanNode root) => Root = root;
}

internal sealed class JpegHuffmanNode
{
    /// <summary>
    ///     符号值，-1 表示非叶节点。
    /// </summary>
    public int Symbol = -1;

    /// <summary>
    ///     0 分支子节点。
    /// </summary>
    public JpegHuffmanNode? Zero;

    /// <summary>
    ///     1 分支子节点。
    /// </summary>
    public JpegHuffmanNode? One;
}

internal sealed class JpegComponent
{
    /// <summary>
    ///     分量标识符。
    /// </summary>
    public int Id;

    /// <summary>
    ///     水平采样因子。
    /// </summary>
    public int H;

    /// <summary>
    ///     垂直采样因子。
    /// </summary>
    public int V;

    /// <summary>
    ///     量化表标识符。
    /// </summary>
    public int QuantTableId;

    /// <summary>
    ///     直流 Huffman 表标识符。
    /// </summary>
    public int DcTableId;

    /// <summary>
    ///     交流 Huffman 表标识符。
    /// </summary>
    public int AcTableId;

    /// <summary>
    ///     初始化 <see cref="JpegComponent" /> 类的新实例。
    /// </summary>
    /// <param name="id">分量标识符。</param>
    /// <param name="h">水平采样因子。</param>
    /// <param name="v">垂直采样因子。</param>
    /// <param name="qtId">量化表标识符。</param>
    public JpegComponent(int id, int h, int v, int qtId)
    {
        Id = id; H = h; V = v; QuantTableId = qtId;
    }
}

internal ref struct JpegBitReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _bytePos;
    private int _bitPos;

    /// <summary>
    ///     初始化 <see cref="JpegBitReader" /> 结构的新实例。
    /// </summary>
    /// <param name="data">JPEG 扫描数据。</param>
    /// <param name="startPos">起始字节位置。</param>
    public JpegBitReader(ReadOnlySpan<byte> data, int startPos)
    {
        _data = data;
        _bytePos = startPos;
        _bitPos = 0;
    }

    /// <summary>
    ///     读取单个比特。
    /// </summary>
    /// <returns>比特值（0 或 1）。</returns>
    public int ReadBit()
    {
        if (_bytePos >= _data.Length) return 0;

        var bit = (_data[_bytePos] >> (7 - _bitPos)) & 1;
        _bitPos++;

        if (_bitPos >= 8)
        {
            _bitPos = 0;
            _bytePos++;

            if (_bytePos < _data.Length && _data[_bytePos] == 0xFF)
            {
                if (_bytePos + 1 < _data.Length && _data[_bytePos + 1] == 0x00)
                {
                    _bytePos++;
                }
            }
        }

        return bit;
    }

    /// <summary>
    ///     读取指定数量的比特。
    /// </summary>
    /// <param name="count">要读取的比特数。</param>
    /// <returns>读取的整数值。</returns>
    public int ReadBits(int count)
    {
        var value = 0;
        for (var i = 0; i < count; i++)
        {
            value = (value << 1) | ReadBit();
        }
        return value;
    }
}

#endregion
