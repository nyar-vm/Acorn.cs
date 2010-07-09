using Acorn.Frame;
using Acorn.ZeroMQ.Data;

namespace Acorn.ZeroMQ.Decode;

/// <summary>
///     ZeroMQ 解码器，用于解析 ZeroMQ 协议消息。
/// </summary>
public ref struct ZeroMQDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="ZeroMQDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要解码的字节数据。</param>
    public ZeroMQDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 ZeroMQ 消息。
    /// </summary>
    /// <returns>解码后的 ZeroMQ 消息数据。</returns>
    public ZeroMQMessageData DecodeMessage()
    {
        var message = new ZeroMQMessageData();
        var parts = new List<ZeroMQMessageData>();

        while (!_buffer.IsEnd)
        {
            var frame = DecodeFrame();
            if (frame == null)
            {
                break;
            }

            var part = new ZeroMQMessageData
            {
                Type = (frame.Flags & 1) == 0 ? ZeroMQConstants.MessageType.MessagePart : ZeroMQConstants.MessageType.MessageEnd,
                Length = frame.Length,
                Data = frame.Data,
                Flags = (ZeroMQConstants.Flags)frame.Flags
            };

            parts.Add(part);

            if (part.Type == ZeroMQConstants.MessageType.MessageEnd)
            {
                break;
            }
        }

        if (parts.Count == 1)
        {
            return parts[0];
        }

        message.Type = ZeroMQConstants.MessageType.MessageEnd;
        message.Parts = parts;
        return message;
    }

    /// <summary>
    ///     解码 ZeroMQ 帧。
    /// </summary>
    /// <returns>解码后的 ZeroMQ 帧数据。</returns>
    private ZeroMQFrameData? DecodeFrame()
    {
        if (_buffer.Remaining < ZeroMQConstants.FrameSize)
        {
            return null;
        }

        var header = _buffer.ReadBytes(ZeroMQConstants.FrameSize);

        var flags = header[0];
        var length = 0;

        for (var i = 1; i < ZeroMQConstants.FrameSize; i++)
        {
            length = (length * 256) + header[i];
        }

        if (_buffer.Remaining < length)
        {
            return null;
        }

        var data = _buffer.ReadBytes(length).ToArray();

        return new ZeroMQFrameData
        {
            Flags = flags,
            Length = length,
            Data = data
        };
    }

    /// <summary>
    ///     解码命令消息。
    /// </summary>
    /// <param name="message">消息数据。</param>
    public void DecodeCommand(ZeroMQMessageData message)
    {
        if (message.Data == null || message.Data.Length == 0)
        {
            return;
        }

        var buffer = new ByteBuffer(message.Data);

        var commandType = (ZeroMQConstants.CommandType)buffer.ReadU8();
        message.CommandType = commandType;

        switch (commandType)
        {
            case ZeroMQConstants.CommandType.Connect:
            case ZeroMQConstants.CommandType.Bind:
                DecodeConnectBindCommand(message, ref buffer);
                break;
        }
    }

    /// <summary>
    ///     解码连接/绑定命令。
    /// </summary>
    /// <param name="message">消息数据。</param>
    /// <param name="buffer">字节缓冲区。</param>
    private void DecodeConnectBindCommand(ZeroMQMessageData message, ref ByteBuffer buffer)
    {
        var socketType = (ZeroMQConstants.SocketType)buffer.ReadI32LE();
        message.SocketType = socketType;

        var addressLength = buffer.ReadI32LE();

        var addressBytes = buffer.ReadBytes(addressLength).ToArray();
        message.Address = System.Text.Encoding.UTF8.GetString(addressBytes);
    }
}
