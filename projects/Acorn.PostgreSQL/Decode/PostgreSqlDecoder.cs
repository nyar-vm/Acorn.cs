using Acorn.Frame;
using Acorn.PostgreSql.Data;

namespace Acorn.PostgreSql.Decode;

/// <summary>
///     PostgreSQL 解码器，用于解析 PostgreSQL 协议消息。
/// </summary>
public ref struct PostgreSqlDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="PostgreSqlDecoder" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要解码的字节数据。</param>
    public PostgreSqlDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     解码 PostgreSQL 消息。
    /// </summary>
    /// <returns>解码后的 PostgreSQL 消息数据。</returns>
    public PostgreSqlMessageData DecodeMessage()
    {
        var messageTypeByte = _buffer.ReadU8();
        var messageType = (PostgreSqlConstants.MessageType)messageTypeByte;

        var length = _buffer.ReadI32BE();

        var contentLength = length - 4;

        var data = _buffer.ReadBytes(contentLength).ToArray();

        var message = new PostgreSqlMessageData
        {
            Type = messageType,
            Length = length,
            Data = data
        };

        switch (messageType)
        {
            case PostgreSqlConstants.MessageType.AuthenticationRequest:
                DecodeAuthenticationRequest(message);
                break;
            case PostgreSqlConstants.MessageType.ErrorResponse:
                DecodeErrorResponse(message);
                break;
            case PostgreSqlConstants.MessageType.CommandComplete:
                DecodeCommandComplete(message);
                break;
            case PostgreSqlConstants.MessageType.ReadyForQuery:
                DecodeReadyForQuery(message);
                break;
            case PostgreSqlConstants.MessageType.RowDescription:
                DecodeRowDescription(message);
                break;
        }

        return message;
    }

    /// <summary>
    ///     解码认证请求消息。
    /// </summary>
    /// <param name="message">消息数据。</param>
    private void DecodeAuthenticationRequest(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.Data);

        var authType = (PostgreSqlConstants.AuthenticationType)buffer.ReadI32BE();
        message.AuthenticationType = authType;

        if (buffer.Remaining > 0)
        {
            message.AuthenticationData = buffer.ReadBytes(buffer.Remaining).ToArray();
        }
    }

    /// <summary>
    ///     解码错误响应消息。
    /// </summary>
    /// <param name="message">消息数据。</param>
    private void DecodeErrorResponse(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.Data);

        while (!buffer.IsEnd)
        {
            var fieldType = buffer.ReadU8();
            if (fieldType == 0)
            {
                break;
            }

            var fieldValue = buffer.ReadNullTerminatedString();
            message.ErrorFields.Add(((char)fieldType).ToString(), fieldValue);
        }
    }

    /// <summary>
    ///     解码命令完成消息。
    /// </summary>
    /// <param name="message">消息数据。</param>
    private void DecodeCommandComplete(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.Data);

        message.CommandTag = buffer.ReadNullTerminatedString();
    }

    /// <summary>
    ///     解码就绪消息。
    /// </summary>
    /// <param name="message">消息数据。</param>
    private void DecodeReadyForQuery(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.Data);

        var transactionStatusByte = buffer.ReadU8();
        message.TransactionStatus = (PostgreSqlConstants.TransactionStatus)transactionStatusByte;
    }

    /// <summary>
    ///     解码行描述消息。
    /// </summary>
    /// <param name="message">消息数据。</param>
    private void DecodeRowDescription(PostgreSqlMessageData message)
    {
        var buffer = new ByteBuffer(message.Data);

        var fieldCount = buffer.ReadI16BE();

        for (var i = 0; i < fieldCount; i++)
        {
            var fieldDescription = new PostgreSqlFieldDescription
            {
                Name = buffer.ReadNullTerminatedString(),
                TableId = buffer.ReadU32BE(),
                ColumnId = buffer.ReadU16BE(),
                DataTypeSize = buffer.ReadU16BE(),
                DataTypeOid = buffer.ReadU32BE(),
                TypeModifier = buffer.ReadI32BE(),
                FormatCode = buffer.ReadU16BE()
            };

            message.FieldDescriptions.Add(fieldDescription);
        }
    }
}
