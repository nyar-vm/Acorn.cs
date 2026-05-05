using Acorn.Frame;
using Acorn.PostgreSql.Data;

namespace Acorn.PostgreSql.Encode;

/// <summary>
///     PostgreSQL 编码器，用于编码 PostgreSQL 协议消息。
/// </summary>
public ref struct PostgreSqlEncoder
{
    private ByteBufferWriter _writer;

    /// <summary>
    ///     初始化 <see cref="PostgreSqlEncoder" /> 结构的新实例。
    /// </summary>
    /// <param name="buffer">目标字节缓冲区。</param>
    public PostgreSqlEncoder(Span<byte> buffer)
    {
        _writer = new ByteBufferWriter(buffer);
    }

    /// <summary>
    ///     编码 PostgreSQL 消息。
    /// </summary>
    /// <param name="message">要编码的消息。</param>
    public void EncodeMessage(PostgreSqlMessageData message)
    {
        _writer.WriteU8((byte)message.Type);

        var length = (message.Data?.Length ?? 0) + 4;
        _writer.WriteI32BE(length);

        if (message.Data != null && message.Data.Length > 0)
        {
            _writer.Write(message.Data);
        }
    }

    /// <summary>
    ///     编码启动消息。
    /// </summary>
    /// <param name="user">用户名。</param>
    /// <param name="database">数据库名。</param>
    /// <param name="applicationName">应用程序名。</param>
    public void EncodeStartupMessage(string user, string database, string applicationName = "Acorn.PostgreSql")
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.WriteI32BE(PostgreSQLConstants.ProtocolVersion);

        writer.WriteNullTerminatedString("user");
        writer.WriteNullTerminatedString(user);

        if (!string.IsNullOrEmpty(database))
        {
            writer.WriteNullTerminatedString("database");
            writer.WriteNullTerminatedString(database);
        }

        writer.WriteNullTerminatedString("application_name");
        writer.WriteNullTerminatedString(applicationName);

        writer.WriteU8(0);

        var length = writer.Position + 4;
        _writer.WriteI32BE(length);

        _writer.Write(temp.Slice(0, writer.Position).ToArray());
    }

    /// <summary>
    ///     编码查询消息。
    /// </summary>
    /// <param name="query">查询语句。</param>
    public void EncodeQueryMessage(string query)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.WriteNullTerminatedString(query);

        var message = new PostgreSqlMessageData
        {
            Type = PostgreSQLConstants.MessageType.Query,
            Length = writer.Position + 4,
            Data = temp.Slice(0, writer.Position).ToArray()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码密码消息。
    /// </summary>
    /// <param name="password">密码。</param>
    public void EncodePasswordMessage(string password)
    {
        Span<byte> temp = stackalloc byte[4096];
        var writer = new ByteBufferWriter(temp);

        writer.WriteNullTerminatedString(password);

        var message = new PostgreSqlMessageData
        {
            Type = (PostgreSQLConstants.MessageType)'p',
            Length = writer.Position + 4,
            Data = temp.Slice(0, writer.Position).ToArray()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码同步消息。
    /// </summary>
    public void EncodeSyncMessage()
    {
        var message = new PostgreSqlMessageData
        {
            Type = PostgreSQLConstants.MessageType.SyncMessage,
            Length = 4,
            Data = Array.Empty<byte>()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码终止消息。
    /// </summary>
    public void EncodeTerminateMessage()
    {
        var message = new PostgreSqlMessageData
        {
            Type = PostgreSQLConstants.MessageType.TerminateMessage,
            Length = 4,
            Data = Array.Empty<byte>()
        };

        EncodeMessage(message);
    }

    #region 服务端编码方法

    /// <summary>
    ///     编码 AuthenticationOk 消息（类型 'R'，内容为 int32 0）。
    /// </summary>
    public void EncodeAuthenticationOk()
    {
        Span<byte> temp = stackalloc byte[16];
        var w = new ByteBufferWriter(temp);
        w.WriteI32BE(0);

        var message = new PostgreSqlMessageData
        {
            Type = (PostgreSQLConstants.MessageType)'R',
            Length = w.Position + 4,
            Data = temp.Slice(0, w.Position).ToArray()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 RowDescription 消息（类型 'T'）。
    /// </summary>
    /// <param name="columns">列元数据列表。</param>
    public void EncodeRowDescription(IReadOnlyList<(string Name, uint TableOid, short ColumnAttr, int TypeOid, short TypeSize, int TypeModifier, short FormatCode)> columns)
    {
        Span<byte> temp = stackalloc byte[8192];
        var w = new ByteBufferWriter(temp);

        w.WriteI16BE((short)columns.Count);

        foreach (var col in columns)
        {
            w.WriteNullTerminatedString(col.Name);
            w.WriteI32BE((int)col.TableOid);
            w.WriteI16BE(col.ColumnAttr);
            w.WriteI32BE(col.TypeOid);
            w.WriteI16BE(col.TypeSize);
            w.WriteI32BE(col.TypeModifier);
            w.WriteI16BE(col.FormatCode);
        }

        var message = new PostgreSqlMessageData
        {
            Type = (PostgreSQLConstants.MessageType)'T',
            Length = w.Position + 4,
            Data = temp.Slice(0, w.Position).ToArray()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 DataRow 消息（类型 'D'）。
    /// </summary>
    /// <param name="values">行中各列的字节值，null 表示 NULL。</param>
    public void EncodeDataRow(IReadOnlyList<byte[]?> values)
    {
        Span<byte> temp = stackalloc byte[8192];
        var w = new ByteBufferWriter(temp);

        w.WriteI16BE((short)values.Count);

        foreach (var value in values)
        {
            if (value is null)
            {
                w.WriteI32BE(-1);
            }
            else
            {
                w.WriteI32BE(value.Length);
                w.Write(value);
            }
        }

        var message = new PostgreSqlMessageData
        {
            Type = (PostgreSQLConstants.MessageType)'D',
            Length = w.Position + 4,
            Data = temp.Slice(0, w.Position).ToArray()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 CommandComplete 消息（类型 'C'）。
    /// </summary>
    /// <param name="tag">命令标签（如 "SELECT 42"、"INSERT 0 1"）。</param>
    public void EncodeCommandComplete(string tag)
    {
        Span<byte> temp = stackalloc byte[256];
        var w = new ByteBufferWriter(temp);

        w.WriteNullTerminatedString(tag);

        var message = new PostgreSqlMessageData
        {
            Type = (PostgreSQLConstants.MessageType)'C',
            Length = w.Position + 4,
            Data = temp.Slice(0, w.Position).ToArray()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 ReadyForQuery 消息（类型 'Z'）。
    /// </summary>
    /// <param name="transactionStatus">事务状态（'I'=空闲, 'T'=事务中, 'E'=失败事务）。</param>
    public void EncodeReadyForQuery(char transactionStatus = 'I')
    {
        Span<byte> temp = stackalloc byte[16];
        var w = new ByteBufferWriter(temp);

        w.WriteU8((byte)transactionStatus);

        var message = new PostgreSqlMessageData
        {
            Type = (PostgreSQLConstants.MessageType)'Z',
            Length = w.Position + 4,
            Data = temp.Slice(0, w.Position).ToArray()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 ErrorResponse 消息（类型 'E'）。
    /// </summary>
    /// <param name="severity">严重程度（如 "ERROR"）。</param>
    /// <param name="sqlState">5 字符 SQL 状态码（如 "42601"）。</param>
    /// <param name="message">错误消息文本。</param>
    public void EncodeErrorResponse(string severity, string sqlState, string message)
    {
        Span<byte> temp = stackalloc byte[4096];
        var w = new ByteBufferWriter(temp);

        w.WriteU8((byte)'S');
        w.WriteNullTerminatedString(severity);

        w.WriteU8((byte)'C');
        w.WriteNullTerminatedString(sqlState);

        w.WriteU8((byte)'M');
        w.WriteNullTerminatedString(message);

        w.WriteU8(0);

        var msg = new PostgreSqlMessageData
        {
            Type = (PostgreSQLConstants.MessageType)'E',
            Length = w.Position + 4,
            Data = temp.Slice(0, w.Position).ToArray()
        };

        EncodeMessage(msg);
    }

    /// <summary>
    ///     编码 ParseComplete 消息（类型 '1'）。
    /// </summary>
    public void EncodeParseComplete()
    {
        var message = new PostgreSqlMessageData
        {
            Type = PostgreSQLConstants.MessageType.ParseComplete,
            Length = 4,
            Data = Array.Empty<byte>()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 BindComplete 消息（类型 '2'）。
    /// </summary>
    public void EncodeBindComplete()
    {
        var message = new PostgreSqlMessageData
        {
            Type = PostgreSQLConstants.MessageType.BindComplete,
            Length = 4,
            Data = Array.Empty<byte>()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 CloseComplete 消息（类型 '3'）。
    /// </summary>
    public void EncodeCloseComplete()
    {
        var message = new PostgreSqlMessageData
        {
            Type = PostgreSQLConstants.MessageType.CloseComplete,
            Length = 4,
            Data = Array.Empty<byte>()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 NoData 消息（类型 'n'）。
    /// </summary>
    public void EncodeNoData()
    {
        var message = new PostgreSqlMessageData
        {
            Type = (PostgreSQLConstants.MessageType)'n',
            Length = 4,
            Data = Array.Empty<byte>()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 ParameterDescription 消息（类型 't'）。
    ///     包含参数类型 OID 列表。
    /// </summary>
    /// <param name="parameterOids">参数类型 OID 列表，可为空</param>
    public void EncodeParameterDescription(IReadOnlyList<int>? parameterOids)
    {
        Span<byte> temp = stackalloc byte[256];
        var w = new ByteBufferWriter(temp);

        if (parameterOids is null || parameterOids.Count == 0)
        {
            w.WriteI16BE(0);
        }
        else
        {
            w.WriteI16BE((short)parameterOids.Count);
            foreach (var oid in parameterOids)
            {
                w.WriteI32BE(oid);
            }
        }

        var message = new PostgreSqlMessageData
        {
            Type = PostgreSQLConstants.MessageType.ParameterDescription,
            Length = w.Position + 4,
            Data = temp.Slice(0, w.Position).ToArray()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 EmptyQueryResponse 消息（类型 'I'）。
    /// </summary>
    public void EncodeEmptyQueryResponse()
    {
        var message = new PostgreSqlMessageData
        {
            Type = PostgreSQLConstants.MessageType.EmptyQueryResponse,
            Length = 4,
            Data = Array.Empty<byte>()
        };

        EncodeMessage(message);
    }

    /// <summary>
    ///     编码 PortalSuspended 消息（类型 's'）。
    /// </summary>
    public void EncodePortalSuspended()
    {
        var message = new PostgreSqlMessageData
        {
            Type = PostgreSQLConstants.MessageType.PortalSuspended,
            Length = 4,
            Data = Array.Empty<byte>()
        };

        EncodeMessage(message);
    }

    #endregion
}
