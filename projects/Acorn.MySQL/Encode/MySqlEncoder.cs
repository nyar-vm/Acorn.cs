using System.Text;
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
            var passwordBytes = Encoding.UTF8.GetBytes(password);
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

    #region 服务端编码方法

    /// <summary>
    ///     编码服务端初始握手包（Protocol::HandshakeV10）。
    /// </summary>
    /// <param name="sequenceId">包序号（通常为 0）。</param>
    /// <param name="serverVersion">服务端版本字符串（如 "5.7.32-OlympDB"）。</param>
    /// <param name="connectionId">连接 ID。</param>
    /// <param name="authPluginName">认证插件名（如 "mysql_native_password"）。</param>
    /// <param name="authPluginData">认证插件数据（20 字节随机盐值）。</param>
    /// <param name="capabilityFlags">服务端能力标志。</param>
    /// <param name="charset">默认字符集编号。</param>
    /// <param name="serverStatus">服务端状态标志。</param>
    public void EncodeHandshakeInit(byte sequenceId, string serverVersion, int connectionId,
        string authPluginName, byte[] authPluginData, uint capabilityFlags, byte charset,
        MySQLConstants.ServerStatus serverStatus)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.WriteU8(MySQLConstants.ProtocolVersion);
        w.WriteNullTerminatedString(serverVersion);
        w.WriteU32LE((uint)connectionId);

        var authDataLen = Math.Min(authPluginData.Length, 20);
        w.Write(authPluginData.AsSpan(0, authDataLen));

        if (authDataLen < 20)
        {
            w.Advance(20 - authDataLen);
        }

        w.Advance(1);

        w.WriteU16LE((ushort)(capabilityFlags & 0xFFFF));
        w.WriteU8(charset);
        w.WriteU16LE((ushort)((int)serverStatus));
        w.WriteU16LE((ushort)((capabilityFlags >> 16) & 0xFFFF));

        var authDataTotalLen = (byte)(authPluginData.Length + 1);
        w.WriteU8(authDataTotalLen);

        w.Advance(10);

        if (authPluginData.Length > 20)
        {
            var remaining = Math.Min(authPluginData.Length - 20, 12);
            w.Write(authPluginData.AsSpan(20, remaining));
            w.Advance(12 - remaining);
        }
        else
        {
            w.Advance(12);
        }

        w.WriteNullTerminatedString(authPluginName);

        var written = temp.Slice(0, w.Position).ToArray();
        var packet = new MySqlPacketData
        {
            Length = written.Length,
            SequenceId = sequenceId,
            Type = MySqlPacketType.Handshake,
            Data = written
        };

        EncodePacket(packet);
    }

    /// <summary>
    ///     编码 OK 响应包。
    /// </summary>
    /// <param name="sequenceId">包序号。</param>
    /// <param name="affectedRows">影响行数。</param>
    /// <param name="lastInsertId">最后插入的 ID。</param>
    /// <param name="statusFlags">服务端状态标志。</param>
    /// <param name="warnings">警告数。</param>
    /// <param name="info">可选的附加信息字符串。</param>
    public void EncodeOkResponse(byte sequenceId, ulong affectedRows, ulong lastInsertId,
        MySQLConstants.ServerStatus statusFlags, ushort warnings, string? info = null)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.WriteU8(MySQLConstants.PacketMarkerOk);
        WriteLengthEncodedInteger(ref w, affectedRows);
        WriteLengthEncodedInteger(ref w, lastInsertId);
        w.WriteU16LE((ushort)statusFlags);
        w.WriteU16LE(warnings);

        if (info is not null)
        {
            WriteLengthEncodedString(ref w, info);
        }

        var written = temp.Slice(0, w.Position).ToArray();
        var packet = new MySqlPacketData
        {
            Length = written.Length,
            SequenceId = sequenceId,
            Type = MySqlPacketType.Result,
            Data = written
        };

        EncodePacket(packet);
    }

    /// <summary>
    ///     编码错误响应包。
    /// </summary>
    /// <param name="sequenceId">包序号。</param>
    /// <param name="errorCode">MySQL 错误码。</param>
    /// <param name="sqlState">5 字符 SQL 状态标识（如 "42000"）。</param>
    /// <param name="errorMessage">错误消息。</param>
    public void EncodeErrorResponse(byte sequenceId, ushort errorCode, string sqlState, string errorMessage)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.WriteU8(MySQLConstants.PacketMarkerError);
        w.WriteU16LE(errorCode);

        w.WriteU8((byte)'#');

        if (sqlState.Length >= 5)
        {
            var stateBytes = Encoding.UTF8.GetBytes(sqlState[..5]);
            w.Write(stateBytes);
        }
        else
        {
            w.WriteString(sqlState);
            w.Advance(5 - sqlState.Length);
        }

        w.Write(Encoding.UTF8.GetBytes(errorMessage));

        var written = temp.Slice(0, w.Position).ToArray();
        var packet = new MySqlPacketData
        {
            Length = written.Length,
            SequenceId = sequenceId,
            Type = MySqlPacketType.Error,
            Data = written
        };

        EncodePacket(packet);
    }

    /// <summary>
    ///     编码 EOF 响应包。
    /// </summary>
    /// <param name="sequenceId">包序号。</param>
    /// <param name="warnings">警告数。</param>
    /// <param name="statusFlags">服务端状态标志。</param>
    public void EncodeEofResponse(byte sequenceId, ushort warnings, MySQLConstants.ServerStatus statusFlags)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.WriteU8(MySQLConstants.PacketMarkerEof);
        w.WriteU16LE(warnings);
        w.WriteU16LE((ushort)statusFlags);

        var written = temp.Slice(0, w.Position).ToArray();
        var packet = new MySqlPacketData
        {
            Length = written.Length,
            SequenceId = sequenceId,
            Type = MySqlPacketType.Eof,
            Data = written
        };

        EncodePacket(packet);
    }

    /// <summary>
    ///     编码 ColumnDefinition41 包（结果集元数据中的列定义）。
    /// </summary>
    /// <param name="sequenceId">包序号。</param>
    /// <param name="catalog">目录名（通常为 "def"）。</param>
    /// <param name="schema">模式名。</param>
    /// <param name="table">表名。</param>
    /// <param name="orgTable">原始表名。</param>
    /// <param name="name">列名。</param>
    /// <param name="orgName">原始列名。</param>
    /// <param name="charset">字符集编号。</param>
    /// <param name="columnLength">列最大长度。</param>
    /// <param name="columnType">列类型（MYSQL_TYPE_*）。</param>
    /// <param name="flags">列标志。</param>
    /// <param name="decimals">小数位数。</param>
    public void EncodeColumnDefinition41(byte sequenceId, string catalog, string schema, string table,
        string orgTable, string name, string orgName, ushort charset, uint columnLength,
        byte columnType, ushort flags, byte decimals)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        WriteLengthEncodedString(ref w, catalog);
        WriteLengthEncodedString(ref w, schema);
        WriteLengthEncodedString(ref w, table);
        WriteLengthEncodedString(ref w, orgTable);
        WriteLengthEncodedString(ref w, name);
        WriteLengthEncodedString(ref w, orgName);

        w.WriteU8(0x0C);

        w.WriteU16LE(charset);
        w.WriteU32LE(columnLength);
        w.WriteU8(columnType);
        w.WriteU16LE(flags);
        w.WriteU8(decimals);

        w.Advance(2);

        var written = temp.Slice(0, w.Position).ToArray();
        var packet = new MySqlPacketData
        {
            Length = written.Length,
            SequenceId = sequenceId,
            Type = MySqlPacketType.Field,
            Data = written
        };

        EncodePacket(packet);
    }

    /// <summary>
    ///     编码文本协议结果集行数据包。
    /// </summary>
    /// <param name="sequenceId">包序号。</param>
    /// <param name="values">行中各列的字符串值。</param>
    public void EncodeTextResultSetRow(byte sequenceId, IReadOnlyList<string?> values)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        foreach (var value in values)
        {
            if (value is null)
            {
                w.WriteU8(MySQLConstants.LengthEncodedNull);
            }
            else
            {
                var valueBytes = Encoding.UTF8.GetBytes(value);
                WriteLengthEncodedInteger(ref w, (ulong)valueBytes.Length);
                w.Write(valueBytes);
            }
        }

        var written = temp.Slice(0, w.Position).ToArray();
        var packet = new MySqlPacketData
        {
            Length = written.Length,
            SequenceId = sequenceId,
            Type = MySqlPacketType.RowData,
            Data = written
        };

        EncodePacket(packet);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     写入长度编码整数（Length-Encoded Integer）。
    /// </summary>
    private static void WriteLengthEncodedInteger(ref ByteBufferWriter writer, ulong value)
    {
        if (value <= MySQLConstants.LengthEncodedMaxSingle)
        {
            writer.WriteU8((byte)value);
        }
        else if (value < MySQLConstants.LengthEncodedInt16Threshold)
        {
            writer.WriteU8(MySQLConstants.LengthEncodedInt16);
            writer.WriteU16LE((ushort)value);
        }
        else if (value <= 0xFFFFFF)
        {
            writer.WriteU8(MySQLConstants.LengthEncodedInt24);
            writer.WriteU8((byte)(value & 0xFF));
            writer.WriteU8((byte)((value >> 8) & 0xFF));
            writer.WriteU8((byte)((value >> 16) & 0xFF));
        }
        else
        {
            writer.WriteU8(0xFE);
            writer.WriteU64LE(value);
        }
    }

    /// <summary>
    ///     写入长度编码字符串。
    /// </summary>
    private static void WriteLengthEncodedString(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteLengthEncodedInteger(ref writer, (ulong)bytes.Length);
        writer.Write(bytes);
    }

    #endregion
}
