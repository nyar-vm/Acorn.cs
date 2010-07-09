namespace Acorn.PostgreSql.Data;

/// <summary>
///     PostgreSQL 协议常量。
/// </summary>
public static class PostgreSqlConstants
{
    /// <summary>
    ///     PostgreSQL 协议版本。
    /// </summary>
    public const int ProtocolVersion = 196608;
    
    /// <summary>
    ///     消息类型。
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        ///     认证请求。
        /// </summary>
        AuthenticationRequest = 'R',
        /// <summary>
        ///     后端键数据。
        /// </summary>
        BackendKeyData = 'K',
        /// <summary>
        ///     绑定完成。
        /// </summary>
        BindComplete = '2',
        /// <summary>
        ///     关闭完成。
        /// </summary>
        CloseComplete = '3',
        /// <summary>
        ///     命令完成。
        /// </summary>
        CommandComplete = 'C',
        /// <summary>
        ///     复制数据。
        /// </summary>
        CopyData = 'd',
        /// <summary>
        ///     复制完成。
        /// </summary>
        CopyDone = 'c',
        /// <summary>
        ///     复制失败。
        /// </summary>
        CopyFail = 'f',
        /// <summary>
        ///     数据行。
        /// </summary>
        DataRow = 'D',
        /// <summary>
        ///     错误响应。
        /// </summary>
        ErrorResponse = 'E',
        /// <summary>
        ///     空查询响应。
        /// </summary>
        EmptyQueryResponse = 'I',
        /// <summary>
        ///     函数调用完成。
        /// </summary>
        FunctionCallComplete = 'V',
        /// <summary>
        ///     行描述。
        /// </summary>
        RowDescription = 'T',
        /// <summary>
        ///     通知响应。
        /// </summary>
        NotificationResponse = 'A',
        /// <summary>
        ///     参数描述。
        /// </summary>
        ParameterDescription = 't',
        /// <summary>
        ///     参数状态。
        /// </summary>
        ParameterStatus = 'S',
        /// <summary>
        ///     解析完成。
        /// </summary>
        ParseComplete = '1',
        /// <summary>
        ///     门户消息。
        /// </summary>
        PortalSuspended = 's',
        /// <summary>
        ///     就绪消息。
        /// </summary>
        ReadyForQuery = 'Z',
        /// <summary>
        ///     行描述。
        /// </summary>
        RowDescriptionMessage = 'T',
        /// <summary>
        ///     同步消息。
        /// </summary>
        Sync = 'S',
        /// <summary>
        ///     终止消息。
        /// </summary>
        Terminate = 'X',
        /// <summary>
        ///     绑定消息。
        /// </summary>
        Bind = 'B',
        /// <summary>
        ///     关闭消息。
        /// </summary>
        Close = 'C',
        /// <summary>
        ///     复制失败消息。
        /// </summary>
        CopyFailMessage = 'f',
        /// <summary>
        ///     复制数据消息。
        /// </summary>
        CopyDataMessage = 'd',
        /// <summary>
        ///     复制完成消息。
        /// </summary>
        CopyDoneMessage = 'c',
        /// <summary>
        ///     复制模式消息。
        /// </summary>
        CopyMode = 'H',
        /// <summary>
        ///     执行消息。
        /// </summary>
        Execute = 'E',
        /// <summary>
        ///     函数调用消息。
        /// </summary>
        FunctionCall = 'F',
        /// <summary>
        ///     解析消息。
        /// </summary>
        Parse = 'P',
        /// <summary>
        ///     查询消息。
        /// </summary>
        Query = 'Q',
        /// <summary>
        ///     同步消息。
        /// </summary>
        SyncMessage = 'S',
        /// <summary>
        ///     终止消息。
        /// </summary>
        TerminateMessage = 'X'
    }
    
    /// <summary>
    ///     认证类型。
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        ///     认证成功。
        /// </summary>
        AuthenticationOk = 0,
        /// <summary>
        ///     认证 Kerberos V5。
        /// </summary>
        AuthenticationKerberosV5 = 2,
        /// <summary>
        ///     认证密码。
        /// </summary>
        AuthenticationCleartextPassword = 3,
        /// <summary>
        ///     认证密码 MD5。
        /// </summary>
        AuthenticationMD5Password = 5,
        /// <summary>
        ///     认证 SCM 凭证。
        /// </summary>
        AuthenticationSCMCredential = 6,
        /// <summary>
        ///     认证 GSS。
        /// </summary>
        AuthenticationGSS = 7,
        /// <summary>
        ///     认证 GSS 继续。
        /// </summary>
        AuthenticationGSSContinue = 8,
        /// <summary>
        ///     认证 SSPI。
        /// </summary>
        AuthenticationSSPI = 9,
        /// <summary>
        ///     认证 SASL。
        /// </summary>
        AuthenticationSASL = 10,
        /// <summary>
        ///     认证 SASL 继续。
        /// </summary>
        AuthenticationSASLContinue = 11,
        /// <summary>
        ///     认证 SASL 最终。
        /// </summary>
        AuthenticationSASLFinal = 12
    }
    
    /// <summary>
    ///     事务状态。
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>
        ///     空闲。
        /// </summary>
        Idle = 'I',
        /// <summary>
        ///     在事务中。
        /// </summary>
        InTransaction = 'T',
        /// <summary>
        ///     事务失败。
        /// </summary>
        FailedTransaction = 'E'
    }
}