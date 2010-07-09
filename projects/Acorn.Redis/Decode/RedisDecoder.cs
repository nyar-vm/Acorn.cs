using Acorn.Frame;
using Acorn.Redis.Data;

namespace Acorn.Redis.Decode;

/// <summary>
///     Redis 解码器，用于解析 Redis RESP 协议消息。
/// </summary>
public ref struct RedisDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="RedisDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要解码的字节数据。</param>
    public RedisDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 Redis 消息。
    /// </summary>
    /// <returns>解码后的 Redis 消息数据。</returns>
    public RedisMessageData DecodeMessage()
    {
        var firstByte = (char)_buffer.Peek(1)[0];

        switch (firstByte)
        {
            case RedisConstants.SimpleStringPrefix:
                return DecodeSimpleString();
            case RedisConstants.ErrorPrefix:
                return DecodeError();
            case RedisConstants.IntegerPrefix:
                return DecodeInteger();
            case RedisConstants.BulkStringPrefix:
                return DecodeBulkString();
            case RedisConstants.ArrayPrefix:
                return DecodeArray();
            default:
                throw new FormatException($"未知的 Redis 消息类型：{firstByte}");
        }
    }

    /// <summary>
    ///     解码简单字符串。
    /// </summary>
    /// <returns>解码后的简单字符串消息。</returns>
    private RedisMessageData DecodeSimpleString()
    {
        _buffer.ReadU8();
        var line = ReadLine();

        return new RedisMessageData
        {
            Type = RedisMessageType.SimpleString,
            SimpleString = line
        };
    }

    /// <summary>
    ///     解码错误。
    /// </summary>
    /// <returns>解码后的错误消息。</returns>
    private RedisMessageData DecodeError()
    {
        _buffer.ReadU8();
        var line = ReadLine();

        return new RedisMessageData
        {
            Type = RedisMessageType.Error,
            Error = line
        };
    }

    /// <summary>
    ///     解码整数。
    /// </summary>
    /// <returns>解码后的整数消息。</returns>
    private RedisMessageData DecodeInteger()
    {
        _buffer.ReadU8();
        var line = ReadLine();
        long.TryParse(line, out var value);

        return new RedisMessageData
        {
            Type = RedisMessageType.Integer,
            Integer = value
        };
    }

    /// <summary>
    ///     解码批量字符串。
    /// </summary>
    /// <returns>解码后的批量字符串消息。</returns>
    private RedisMessageData DecodeBulkString()
    {
        _buffer.ReadU8();
        var line = ReadLine();

        if (!int.TryParse(line, out var length))
        {
            throw new FormatException($"无效的批量字符串长度：{line}");
        }

        if (length == RedisConstants.NullBulkString)
        {
            return new RedisMessageData
            {
                Type = RedisMessageType.BulkString,
                IsNullBulkString = true
            };
        }

        var data = _buffer.ReadBytes(length).ToArray();
        _buffer.Advance(2);

        return new RedisMessageData
        {
            Type = RedisMessageType.BulkString,
            BulkString = data
        };
    }

    /// <summary>
    ///     解码数组。
    /// </summary>
    /// <returns>解码后的数组消息。</returns>
    private RedisMessageData DecodeArray()
    {
        _buffer.ReadU8();
        var line = ReadLine();

        if (!int.TryParse(line, out var length))
        {
            throw new FormatException($"无效的数组长度：{line}");
        }

        if (length == RedisConstants.NullArray)
        {
            return new RedisMessageData
            {
                Type = RedisMessageType.Array,
                IsNullArray = true
            };
        }

        var array = new List<RedisMessageData>();
        for (var i = 0; i < length; i++)
        {
            var element = DecodeMessage();
            array.Add(element);
        }

        return new RedisMessageData
        {
            Type = RedisMessageType.Array,
            Array = array
        };
    }

    /// <summary>
    ///     读取一行数据（以 CRLF 结尾）。
    /// </summary>
    /// <returns>读取的行内容（不含 CRLF）。</returns>
    private string ReadLine()
    {
        var start = _buffer.Position;
        var end = start;

        while (end + 1 < _buffer.Length)
        {
            if (_buffer.ReadU8At(end) == '\r' && _buffer.ReadU8At(end + 1) == '\n')
            {
                break;
            }

            end++;
        }

        var length = end - start;
        var line = length > 0 ? _buffer.ReadString(length) : string.Empty;
        _buffer.Advance(2);

        return line;
    }
}
