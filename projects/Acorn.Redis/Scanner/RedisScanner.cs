using System.Text;
using Acorn.Frame;
using Acorn.Redis.Data;

namespace Acorn.Redis.Scanner;

/// <summary>
///     Redis RESP 协议扫描器，基于 <see cref="SpanScanner" /> 提供对 Redis RESP 协议消息的快速扫描。
/// </summary>
/// <remarks>
///     Redis 使用 RESP（REdis Serialization Protocol）文本协议。
///     消息以类型前缀（+、-、:、$、*）开头，以 CRLF 结尾。
///     扫描器逐行解析消息结构，提取消息类型和内容摘要。
/// </remarks>
public ref struct RedisScanner
{
    private SpanScanner _scanner;

    /// <summary>
    ///     初始化 <see cref="RedisScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 Redis 字节数据。</param>
    public RedisScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作。
    /// </summary>
    public SpanScanner Scanner => _scanner;

    /// <summary>
    ///     扫描 Redis RESP 消息，提取统计信息。
    /// </summary>
    public RedisScanStatistics ScanStatistics()
    {
        var stats = new RedisScanStatistics();
        var typeCounts = new Dictionary<char, int>();

        while (!_scanner.IsEnd)
        {
            var lineEnd = FindLineEnd();

            if (lineEnd < 0)
            {
                break;
            }

            var lineLength = lineEnd - _scanner.Position;
            var prefix = (char)_scanner.Buffer.ReadU8();

            if (!typeCounts.ContainsKey(prefix))
            {
                typeCounts[prefix] = 0;
            }

            typeCounts[prefix]++;

            switch (prefix)
            {
                case RedisConstants.SimpleStringPrefix:
                    stats.SimpleStringCount++;
                    break;
                case RedisConstants.ErrorPrefix:
                    stats.ErrorCount++;
                    break;
                case RedisConstants.IntegerPrefix:
                    stats.IntegerCount++;
                    break;
                case RedisConstants.BulkStringPrefix:
                    stats.BulkStringCount++;
                    break;
                case RedisConstants.ArrayPrefix:
                    stats.ArrayCount++;
                    break;
            }

            _scanner.Position = lineEnd + 2;
        }

        stats.TypeCounts = typeCounts;
        return stats;
    }

    /// <summary>
    ///     扫描 Redis RESP 消息，提取所有简单字符串和错误消息内容。
    /// </summary>
    public List<(char Type, string Content)> ScanMessages()
    {
        var messages = new List<(char Type, string Content)>();

        while (!_scanner.IsEnd)
        {
            var lineEnd = FindLineEnd();

            if (lineEnd < 0)
            {
                break;
            }

            var prefix = (char)_scanner.Buffer.ReadU8();
            var contentLength = lineEnd - _scanner.Position;
            var content = contentLength > 0 ? _scanner.Buffer.ReadString(contentLength) : string.Empty;

            messages.Add((prefix, content));
            _scanner.Position = lineEnd + 2;
        }

        return messages;
    }

    private int FindLineEnd()
    {
        var pos = _scanner.Position;

        while (pos + 1 < _scanner.Length)
        {
            if (_scanner.Buffer.ReadU8At(pos) == '\r' && _scanner.Buffer.ReadU8At(pos + 1) == '\n')
            {
                return pos;
            }

            pos++;
        }

        return -1;
    }
}

/// <summary>
///     Redis 扫描统计信息。
/// </summary>
public sealed class RedisScanStatistics
{
    public int SimpleStringCount { get; set; }
    public int ErrorCount { get; set; }
    public int IntegerCount { get; set; }
    public int BulkStringCount { get; set; }
    public int ArrayCount { get; set; }
    public Dictionary<char, int> TypeCounts { get; set; } = new();
}
