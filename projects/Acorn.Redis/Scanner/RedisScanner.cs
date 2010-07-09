using System.Text;
using Acorn.Frame;
using Acorn.Redis.Data;

namespace Acorn.Redis.Scanner;

/// <summary>
///     Redis RESP 协议扫描器，基于 <see cref="ByteBuffer" /> 提供对 Redis RESP 协议消息的快速扫描。
/// </summary>
/// <remarks>
///     Redis 使用 RESP（REdis Serialization Protocol）文本协议。
///     消息以类型前缀（+、-、:、$、*）开头，以 CRLF 结尾。
///     扫描器逐行解析消息结构，提取消息类型和内容摘要。
/// </remarks>
public ref struct RedisScanner
{
    private ByteBuffer _buffer;

    public RedisScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    public int Length => _buffer.Length;

    public bool IsEndOfData => _buffer.IsEnd;

    /// <summary>
    ///     扫描 Redis RESP 消息，提取统计信息。
    /// </summary>
    public RedisScanStatistics ScanStatistics()
    {
        var stats = new RedisScanStatistics();
        var typeCounts = new Dictionary<char, int>();

        while (!_buffer.IsEnd)
        {
            var lineEnd = FindLineEnd();

            if (lineEnd < 0)
            {
                break;
            }

            var lineLength = lineEnd - _buffer.Position;
            var prefix = (char)_buffer.ReadU8();

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

            _buffer.Position = lineEnd + 2;
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

        while (!_buffer.IsEnd)
        {
            var lineEnd = FindLineEnd();

            if (lineEnd < 0)
            {
                break;
            }

            var prefix = (char)_buffer.ReadU8();
            var contentLength = lineEnd - _buffer.Position;
            var content = contentLength > 0 ? _buffer.ReadString(contentLength) : string.Empty;

            messages.Add((prefix, content));
            _buffer.Position = lineEnd + 2;
        }

        return messages;
    }

    private int FindLineEnd()
    {
        var pos = _buffer.Position;

        while (pos + 1 < _buffer.Length)
        {
            if (_buffer.ReadU8At(pos) == '\r' && _buffer.ReadU8At(pos + 1) == '\n')
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
