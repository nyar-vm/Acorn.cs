using Acorn.Frame;
using Acorn.ZeroMQ.Data;

namespace Acorn.ZeroMQ.Encode;

/// <summary>
///     ZeroMQ 编码器，用于编码 ZeroMQ 协议消息。
/// </summary>
public ref struct ZeroMQEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始化 <see cref="ZeroMQEncoder" /> 结构的新实例。
    /// </summary>
    /// <param name="buffer">目标字节缓冲区。</param>
    public ZeroMQEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     编码 ZeroMQ 消息。
    /// </summary>
    /// <param name="message">要编码的消息。</param>
    public void EncodeMessage(ZeroMQMessageData message)
    {
        if (message.Parts != null && message.Parts.Count > 0)
        {
            for (var i = 0; i < message.Parts.Count; i++)
            {
                var part = message.Parts[i];
                EncodeFrame(part.Data, i == message.Parts.Count - 1);
            }
        }
        else
        {
            EncodeFrame(message.Data, message.Type == ZeroMQConstants.MessageType.MessageEnd);
        }
    }

    /// <summary>
    ///     编码 ZeroMQ 帧。
    /// </summary>
    /// <param name="data">帧数据。</param>
    /// <param name="isLast">是否是最后一帧。</param>
    private void EncodeFrame(byte[] data, bool isLast)
    {
        var length = data?.Length ?? 0;
        var flags = isLast ? (byte)1 : (byte)0;

        _writer.WriteU8(flags);
        _writer.WriteU64BE((ulong)length);

        if (length > 0 && data != null)
        {
            _writer.Write(data);
        }
    }

    /// <summary>
    ///     编码命令消息。
    /// </summary>
    /// <param name="commandType">命令类型。</param>
    /// <param name="socketType">套接字类型。</param>
    /// <param name="address">地址。</param>
    public void EncodeCommand(ZeroMQConstants.CommandType commandType, ZeroMQConstants.SocketType socketType, string address)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.WriteU8((byte)commandType);

        writer.WriteI32LE((int)socketType);

        var addressBytes = System.Text.Encoding.UTF8.GetBytes(address);
        writer.WriteI32LE(addressBytes.Length);

        writer.Write(addressBytes);

        var message = new ZeroMQMessageData
        {
            Type = ZeroMQConstants.MessageType.MessageEnd,
            Data = temp.Slice(0, writer.Position).ToArray(),
            CommandType = commandType,
            SocketType = socketType,
            Address = address
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码数据消息。
    /// </summary>
    /// <param name="data">消息数据。</param>
    /// <param name="more">是否有更多消息部分。</param>
    public void EncodeDataMessage(byte[] data, bool more = false)
    {
        var message = new ZeroMQMessageData
        {
            Type = more ? ZeroMQConstants.MessageType.MessagePart : ZeroMQConstants.MessageType.MessageEnd,
            Data = data
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码多部分消息。
    /// </summary>
    /// <param name="parts">消息部分。</param>
    public void EncodeMultipartMessage(params byte[][] parts)
    {
        var message = new ZeroMQMessageData
        {
            Type = ZeroMQConstants.MessageType.MessageEnd,
            Parts = new List<ZeroMQMessageData>()
        };

        for (var i = 0; i < parts.Length; i++)
        {
            message.Parts.Add(new ZeroMQMessageData
            {
                Type = i == parts.Length - 1 ? ZeroMQConstants.MessageType.MessageEnd : ZeroMQConstants.MessageType.MessagePart,
                Data = parts[i]
            });
        }

        EncodeMessage(message);
    }
}
