using System.IO;
using Acorn.Frame;
using Acorn.MySql.Data;

namespace Acorn.MySql.Decode;

/// <summary>
///     MySQL 解码器，用于解析 MySQL 协议数据包。
/// </summary>
public ref struct MySqlDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="MySqlDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要解码的字节数据。</param>
    public MySqlDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 MySQL 数据包。
    /// </summary>
    /// <returns>解码后的 MySQL 数据包数据。</returns>
    public MySqlPacketData DecodePacket()
    {
        if (!MySqlPacketHeader.TryRead(ref _buffer, out var header))
        {
            throw new InvalidDataException("MySQL 数据包头部读取失败");
        }

        var length = header.PayloadLength;
        var sequenceId = header.SequenceId;

        var data = _buffer.ReadBytes(length).ToArray();

        var packet = new MySqlPacketData
        {
            Length = length,
            SequenceId = sequenceId,
            Data = data
        };

        if (data.Length > 0)
        {
            var firstByte = data[0];

            if (firstByte == MySqlConstants.PacketMarkerOk)
            {
                packet.Type = MySqlPacketType.Result;
                DecodeResultPacket(packet);
            }
            else if (firstByte == MySqlConstants.PacketMarkerError)
            {
                packet.Type = MySqlPacketType.Error;
                DecodeErrorPacket(packet);
            }
            else if (firstByte == MySqlConstants.PacketMarkerEof)
            {
                packet.Type = MySqlPacketType.Eof;
            }
            else if (firstByte == MySqlConstants.ProtocolVersion)
            {
                packet.Type = MySqlPacketType.Handshake;
            }
            else if (firstByte >= MySqlConstants.CommandTypeMin && firstByte <= MySqlConstants.CommandTypeMax)
            {
                packet.Type = MySqlPacketType.Command;
                packet.CommandType = (MySqlConstants.CommandType)firstByte;
            }
            else
            {
                packet.Type = MySqlPacketType.Unknown;
            }
        }

        return packet;
    }

    /// <summary>
    ///     解码结果包。
    /// </summary>
    /// <param name="packet">数据包。</param>
    private void DecodeResultPacket(MySqlPacketData packet)
    {
        var buffer = new ByteBuffer(packet.Data);

        buffer.ReadU8();

        var affectedRows = ReadLengthEncodedInteger(ref buffer);

        var lastInsertId = ReadLengthEncodedInteger(ref buffer);

        var serverStatus = (MySqlConstants.ServerStatus)buffer.ReadU16LE();
        packet.ServerStatus = serverStatus;

        var warningCount = buffer.ReadU16LE();
    }

    /// <summary>
    ///     解码错误包。
    /// </summary>
    /// <param name="packet">数据包。</param>
    private void DecodeErrorPacket(MySqlPacketData packet)
    {
        var buffer = new ByteBuffer(packet.Data);

        buffer.ReadU8();

        var errorCode = (MySqlConstants.ErrorCode)buffer.ReadU16LE();
        packet.ErrorCode = errorCode;

        if (!buffer.IsEnd && buffer.Peek(1)[0] == (byte)'#')
        {
            buffer.Advance(6);
        }

        var errorMessageBytes = buffer.ReadBytes(buffer.Remaining).ToArray();
        packet.ErrorMessage = System.Text.Encoding.UTF8.GetString(errorMessageBytes);
    }

    /// <summary>
    ///     读取长度编码的整数。
    /// </summary>
    /// <param name="buffer">字节缓冲区。</param>
    /// <returns>读取的整数。</returns>
    private ulong ReadLengthEncodedInteger(ref ByteBuffer buffer)
    {
        var firstByte = buffer.ReadU8();

        if (firstByte <= MySqlConstants.LengthEncodedMaxSingle)
        {
            return firstByte;
        }
        else if (firstByte == MySqlConstants.LengthEncodedNull)
        {
            return 0;
        }
        else if (firstByte == MySqlConstants.LengthEncodedInt16)
        {
            return buffer.ReadU16LE();
        }
        else if (firstByte == MySqlConstants.LengthEncodedInt24)
        {
            return buffer.ReadU32LE();
        }
        else
        {
            return buffer.ReadU64LE();
        }
    }
}
