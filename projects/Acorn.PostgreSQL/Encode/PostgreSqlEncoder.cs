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

        writer.WriteI32BE(PostgreSqlConstants.ProtocolVersion);

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
            Type = PostgreSqlConstants.MessageType.Query,
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
            Type = (PostgreSqlConstants.MessageType)'p',
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
            Type = PostgreSqlConstants.MessageType.SyncMessage,
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
            Type = PostgreSqlConstants.MessageType.TerminateMessage,
            Length = 4,
            Data = Array.Empty<byte>()
        };

        EncodeMessage(message);
    }
}
