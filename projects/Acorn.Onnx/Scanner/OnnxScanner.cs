using System.Text;
using Acorn.Frame;
using Acorn.Onnx.Data;

namespace Acorn.Onnx.Scanner;

/// <summary>
///     ONNX 格式扫描器，基于 <see cref="SpanScanner" /> 提供零分配的快速 ONNX 模型数据探查。
/// </summary>
public ref struct OnnxScanner
{
    private SpanScanner _scanner;

    public OnnxScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 ONNX 模型统计信息。
    /// </summary>
    /// <returns>ONNX 模型统计信息。</returns>
    public OnnxStatistics ScanStatistics()
    {
        var stats = new OnnxStatistics();
        var pos = 0;

        while (pos < _scanner.Length)
        {
            var tagResult = ReadTagAt(pos, out var tagBytes);
            pos += tagBytes;

            if (tagResult == 0)
            {
                break;
            }

            var fieldNumber = (int)(tagResult >> 3);
            var wireType = (int)(tagResult & 0x7);

            switch (fieldNumber)
            {
                case 1:
                    stats.IrVersion = (long)ReadVarintAt(pos, out var irBytes);
                    pos += irBytes;
                    break;
                case 2:
                    var (producer, pBytes) = ReadStringAt(pos);
                    stats.ProducerName = producer;
                    pos += pBytes;
                    break;
                case 7:
                    var graphStats = ScanGraphAt(pos);
                    stats.NodeCount = graphStats.NodeCount;
                    stats.InputCount = graphStats.InputCount;
                    stats.OutputCount = graphStats.OutputCount;
                    stats.InitializerCount = graphStats.InitializerCount;
                    var graphLen = (int)ReadVarintAt(pos, out var glBytes);
                    pos += glBytes + graphLen;
                    break;
                case 8:
                    var opsetLen = (int)ReadVarintAt(pos, out var olBytes);
                    stats.OpsetCount++;
                    pos += olBytes + opsetLen;
                    break;
                default:
                    pos = SkipFieldAt(pos, wireType);
                    break;
            }
        }

        return stats;
    }

    private (int NodeCount, int InputCount, int OutputCount, int InitializerCount) ScanGraphAt(int startPos)
    {
        var nodeCount = 0;
        var inputCount = 0;
        var outputCount = 0;
        var initializerCount = 0;

        var graphLen = (int)ReadVarintAt(startPos, out var lenBytes);
        var pos = startPos + lenBytes;
        var endPos = pos + graphLen;

        while (pos < endPos && pos < _scanner.Length)
        {
            var tagResult = ReadTagAt(pos, out var tagBytes);
            pos += tagBytes;

            if (tagResult == 0)
            {
                break;
            }

            var fieldNumber = (int)(tagResult >> 3);

            switch (fieldNumber)
            {
                case 1:
                    nodeCount++;
                    goto default;
                case 3:
                    inputCount++;
                    goto default;
                case 4:
                    outputCount++;
                    goto default;
                case 10:
                    initializerCount++;
                    goto default;
                default:
                    var wireType = (int)(tagResult & 0x7);
                    pos = SkipFieldAt(pos, wireType);
                    break;
            }
        }

        return (nodeCount, inputCount, outputCount, initializerCount);
    }

    private ulong ReadTagAt(int pos, out int bytesRead)
    {
        bytesRead = 0;
        ulong value = 0;
        var shift = 0;

        while (pos + bytesRead < _scanner.Length)
        {
            var b = _scanner.Buffer.ReadU8At(pos + bytesRead);
            bytesRead++;
            value |= (ulong)(b & 0x7F) << shift;
            shift += 7;

            if ((b & 0x80) == 0)
            {
                break;
            }
        }

        return value;
    }

    private ulong ReadVarintAt(int pos, out int bytesRead)
    {
        bytesRead = 0;
        ulong value = 0;
        var shift = 0;

        while (pos + bytesRead < _scanner.Length)
        {
            var b = _scanner.Buffer.ReadU8At(pos + bytesRead);
            bytesRead++;
            value |= (ulong)(b & 0x7F) << shift;
            shift += 7;

            if ((b & 0x80) == 0)
            {
                break;
            }
        }

        return value;
    }

    private (string Value, int BytesRead) ReadStringAt(int pos)
    {
        var length = (int)ReadVarintAt(pos, out var lenBytes);
        var totalBytes = lenBytes + length;

        if (pos + totalBytes > _scanner.Length)
        {
            return (string.Empty, totalBytes);
        }

        var strBytes = _scanner.Data.Slice(pos + lenBytes, length);
        return (Encoding.UTF8.GetString(strBytes), totalBytes);
    }

    private int SkipFieldAt(int pos, int wireType)
    {
        switch (wireType)
        {
            case 0:
            {
                ReadVarintAt(pos, out var vb);
                return pos + vb;
            }
            case 1:
                return pos + 8;
            case 2:
            {
                var length = (int)ReadVarintAt(pos, out var lb);
                return pos + lb + length;
            }
            case 5:
                return pos + 4;
            default:
                return pos;
        }
    }
}

/// <summary>
///     ONNX 模型统计信息。
/// </summary>
public sealed class OnnxStatistics
{
    public long IrVersion { get; set; }
    public string ProducerName { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public int InputCount { get; set; }
    public int OutputCount { get; set; }
    public int InitializerCount { get; set; }
    public int OpsetCount { get; set; }

    public string IrVersionString => IrVersion switch
    {
        1 => "1.0",
        2 => "1.1",
        3 => "1.2",
        4 => "1.3",
        5 => "1.4",
        6 => "1.5",
        7 => "1.6",
        8 => "1.7",
        9 => "1.8",
        10 => "1.9",
        _ => $"{IrVersion}"
    };
}
