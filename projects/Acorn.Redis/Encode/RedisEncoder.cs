using Acorn.Frame;
using Acorn.Redis.Data;

namespace Acorn.Redis.Encode;

/// <summary>
///     Redis 编码器，用于编码 Redis RESP 协议消息。
/// </summary>
public ref struct RedisEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始化 <see cref="RedisEncoder" /> 结构的新实例。
    /// </summary>
    /// <param name="buffer">目标字节缓冲区。</param>
    public RedisEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     编码 Redis 消息。
    /// </summary>
    /// <param name="message">要编码的消息。</param>
    public void EncodeMessage(RedisMessageData message)
    {
        switch (message.Type)
        {
            case RedisMessageType.SimpleString:
                EncodeSimpleString(message.SimpleString);
                break;
            case RedisMessageType.Error:
                EncodeError(message.Error);
                break;
            case RedisMessageType.Integer:
                EncodeInteger(message.Integer);
                break;
            case RedisMessageType.BulkString:
                EncodeBulkString(message.BulkString, message.IsNullBulkString);
                break;
            case RedisMessageType.Array:
                EncodeArray(message.Array, message.IsNullArray);
                break;
        }
    }

    /// <summary>
    ///     编码简单字符串。
    /// </summary>
    /// <param name="value">要编码的字符串。</param>
    public void EncodeSimpleString(string value)
    {
        _writer.WriteU8((byte)RedisConstants.SimpleStringPrefix);
        _writer.WriteString(value);
        WriteCRLF();
    }

    /// <summary>
    ///     编码错误。
    /// </summary>
    /// <param name="error">错误消息。</param>
    public void EncodeError(string error)
    {
        _writer.WriteU8((byte)RedisConstants.ErrorPrefix);
        _writer.WriteString(error);
        WriteCRLF();
    }

    /// <summary>
    ///     编码整数。
    /// </summary>
    /// <param name="value">整数值。</param>
    public void EncodeInteger(long value)
    {
        _writer.WriteU8((byte)RedisConstants.IntegerPrefix);
        _writer.WriteString(value.ToString());
        WriteCRLF();
    }

    /// <summary>
    ///     编码批量字符串。
    /// </summary>
    /// <param name="value">要编码的字节数组。</param>
    /// <param name="isNull">是否为空批量字符串。</param>
    public void EncodeBulkString(byte[] value, bool isNull = false)
    {
        _writer.WriteU8((byte)RedisConstants.BulkStringPrefix);

        if (isNull)
        {
            _writer.WriteString(RedisConstants.NullBulkString.ToString());
            WriteCRLF();
            return;
        }

        var length = value?.Length ?? 0;
        _writer.WriteString(length.ToString());
        WriteCRLF();

        if (length > 0)
        {
            _writer.Write(value);
            WriteCRLF();
        }
    }

    /// <summary>
    ///     编码数组。
    /// </summary>
    /// <param name="elements">数组元素。</param>
    /// <param name="isNull">是否为空数组。</param>
    public void EncodeArray(List<RedisMessageData> elements, bool isNull = false)
    {
        _writer.WriteU8((byte)RedisConstants.ArrayPrefix);

        if (isNull)
        {
            _writer.WriteString(RedisConstants.NullArray.ToString());
            WriteCRLF();
            return;
        }

        var length = elements?.Count ?? 0;
        _writer.WriteString(length.ToString());
        WriteCRLF();

        if (length > 0)
        {
            foreach (var element in elements!)
            {
                EncodeMessage(element);
            }
        }
    }

    /// <summary>
    ///     编码命令。
    /// </summary>
    /// <param name="command">命令名称。</param>
    /// <param name="args">命令参数。</param>
    public void EncodeCommand(string command, params string[] args)
    {
        var elements = new List<RedisMessageData>
        {
            new RedisMessageData
            {
                Type = RedisMessageType.BulkString,
                BulkString = System.Text.Encoding.UTF8.GetBytes(command.ToUpper())
            }
        };

        foreach (var arg in args)
        {
            elements.Add(new RedisMessageData
            {
                Type = RedisMessageType.BulkString,
                BulkString = System.Text.Encoding.UTF8.GetBytes(arg)
            });
        }

        EncodeArray(elements);
    }

    /// <summary>
    ///     写入 CRLF 行结束符。
    /// </summary>
    private void WriteCRLF()
    {
        _writer.WriteU8((byte)'\r');
        _writer.WriteU8((byte)'\n');
    }
}
