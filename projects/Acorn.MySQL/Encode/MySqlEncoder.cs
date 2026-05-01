using Acorn.Frame;
using Acorn.MySql.Data;

namespace Acorn.MySql.Encode;

/// <summary>
///     MySQL 编码器，用于编码 MySQL 协议数据包。
/// </summary>
public ref struct MySqlEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始化 <see cref="MySqlEncoder" /> 结构的新实例。
    /// </summary>
    /// <param name="buffer">目标字节缓冲区。</param>
    public MySqlEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     编码 MySQL 数据包。
    /// </summary>
    /// <param name="packet">要编码的数据包。</param>
    public void EncodePacket(MySqlPacketData packet)
    {
        var contentLength = packet.Data?.Length ?? 0;
        var header = MySqlPacketHeader.Create(contentLength, packet.SequenceId);
        header.WriteTo(ref _writer);

        if (packet.Data != null && packet.Data.Length > 0)
        {
            _writer.Write(packet.Data);
        }
    }

    /// <summary>
    ///     编码握手响应包。
    /// </summary>
    /// <param name="sequenceId">包序号。</param>
    /// <param name="clientFlags">客户端标志。</param>
    /// <param name="maxPacketSize">最大包大小。</param>
    /// <param name="charset">字符集。</param>
    /// <param name="username">用户名。</param>
    /// <param name="password">密码。</param>
    /// <param name="database">数据库名。</param>
    public void EncodeHandshakeResponse(byte sequenceId, ulong clientFlags, int maxPacketSize, byte charset, string username, string password, string database)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.WriteU64LE(clientFlags);

        writer.WriteI32LE(maxPacketSize);

        writer.WriteU8(charset);

        writer.Advance(23);

        writer.WriteString(username);
        writer.WriteU8(0);

        if (!string.IsNullOrEmpty(password))
        {
            var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            var pwLength = passwordBytes.Length;

            if (pwLength <= MySQLConstants.LengthEncodedMaxSingle)
            {
                writer.WriteU8((byte)pwLength);
            }
            else if (pwLength < MySQLConstants.LengthEncodedInt16Threshold)
            {
                writer.WriteU8(MySQLConstants.LengthEncodedInt16);
                writer.WriteU16LE((ushort)pwLength);
            }
            else
            {
                writer.WriteU8(MySQLConstants.LengthEncodedInt24);
                writer.WriteU32LE((uint)pwLength);
            }

            writer.Write(passwordBytes);
        }

        if (!string.IsNullOrEmpty(database))
        {
            writer.WriteString(database);
            writer.WriteU8(0);
        }

        var written = temp.Slice(0, writer.Position).ToArray();
        var packet = new MySqlPacketData
        {
            Length = written.Length,
            SequenceId = sequenceId,
            Type = MySqlPacketType.HandshakeResponse,
            Data = written
        };

        EncodePacket(packet);
    }

    /// <summary>
    ///     编码查询命令包。
    /// </summary>
    /// <param name="sequenceId">包序号。</param>
    /// <param name="query">查询语句。</param>
    public void EncodeQueryCommand(byte sequenceId, string query)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.WriteU8((byte)MySQLConstants.CommandType.Query);

        writer.WriteString(query);

        var written = temp.Slice(0, writer.Position).ToArray();
        var packet = new MySqlPacketData
        {
            Length = written.Length,
            SequenceId = sequenceId,
            Type = MySqlPacketType.Command,
            CommandType = MySQLConstants.CommandType.Query,
            Data = written
        };

        EncodePacket(packet);
    }

}
