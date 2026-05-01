namespace Acorn.MySql.Data;

/// <summary>
///     MySQL 协议常量。
/// </summary>
public static class MySqlConstants
{
    /// <summary>
    ///     MySQL 协议版本。
    /// </summary>
    public const int ProtocolVersion = 10;

    /// <summary>
    ///     数据包首字节 — OK 包标记。
    /// </summary>
    public const byte PacketMarkerOk = 0x00;

    /// <summary>
    ///     数据包首字节 — 错误包标记。
    /// </summary>
    public const byte PacketMarkerError = 0xFF;

    /// <summary>
    ///     数据包首字节 — EOF 包标记。
    /// </summary>
    public const byte PacketMarkerEof = 0xFE;

    /// <summary>
    ///     长度编码整数 — NULL 标记。
    /// </summary>
    public const byte LengthEncodedNull = 0xFB;

    /// <summary>
    ///     长度编码整数 — UInt16 长度前缀。
    /// </summary>
    public const byte LengthEncodedInt16 = 0xFC;

    /// <summary>
    ///     长度编码整数 — UInt16 最大值的下一值（即 Int24 编码阈值）。
    /// </summary>
    public const int LengthEncodedInt16Threshold = 0x10000;

    /// <summary>
    ///     长度编码整数 — UInt24 长度前缀。
    /// </summary>
    public const byte LengthEncodedInt24 = 0xFD;

    /// <summary>
    ///     长度编码整数 — 单字节最大值（含）。
    /// </summary>
    public const byte LengthEncodedMaxSingle = 0xFA;

    /// <summary>
    ///     命令类型 — 最小值。
    /// </summary>
    public const byte CommandTypeMin = 0x01;

    /// <summary>
    ///     命令类型 — 最大值。
    /// </summary>
    public const byte CommandTypeMax = 0x1F;
    
    /// <summary>
    ///     服务器状态标志。
    /// </summary>
    [Flags]
    public enum ServerStatus
    {
        /// <summary>
        ///     状态正常。
        /// </summary>
        Normal = 0,
        /// <summary>
        ///     服务器处于自动提交模式。
        /// </summary>
        AutoCommit = 1 << 0,
        /// <summary>
        ///     服务器处于事务中。
        /// </summary>
        InTransaction = 1 << 1,
        /// <summary>
        ///     结果集已完成。
        /// </summary>
        MoreResultsExists = 1 << 2
    }
    
    /// <summary>
    ///     命令类型。
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        ///     退出命令。
        /// </summary>
        Quit = 0x01,
        /// <summary>
        ///     初始化数据库命令。
        /// </summary>
        InitDb = 0x02,
        /// <summary>
        ///     查询命令。
        /// </summary>
        Query = 0x03,
        /// <summary>
        ///     字段列表命令。
        /// </summary>
        FieldList = 0x04,
        /// <summary>
        ///     创建数据库命令。
        /// </summary>
        CreateDb = 0x05,
        /// <summary>
        ///     删除数据库命令。
        /// </summary>
        DropDb = 0x06,
        /// <summary>
        ///     刷新命令。
        /// </summary>
        Refresh = 0x07,
        /// <summary>
        ///     关闭命令。
        /// </summary>
        Shutdown = 0x08,
        /// <summary>
        ///     统计命令。
        /// </summary>
        Statistics = 0x09,
        /// <summary>
        ///     进程信息命令。
        /// </summary>
        ProcessInfo = 0x0a,
        /// <summary>
        ///     连接跟踪命令。
        /// </summary>
        Connect = 0x0b,
        /// <summary>
        ///     杀死命令。
        /// </summary>
        Kill = 0x0c,
        /// <summary>
        ///     调试命令。
        /// </summary>
        Debug = 0x0d,
        /// <summary>
        ///     预编译命令。
        /// </summary>
        Ping = 0x0e,
        /// <summary>
        ///     时间命令。
        /// </summary>
        Time = 0x0f,
        /// <summary>
        ///     延迟命令。
        /// </summary>
        DelayedInsert = 0x10,
        /// <summary>
        ///     更改用户命令。
        /// </summary>
        ChangeUser = 0x11,
        /// <summary>
        ///     二进制日志命令。
        /// </summary>
        BinlogDump = 0x12,
        /// <summary>
        ///     表转储命令。
        /// </summary>
        TableDump = 0x13,
        /// <summary>
        ///     连接关闭命令。
        /// </summary>
        ConnectOut = 0x14,
        /// <summary>
        ///     注册命令。
        /// </summary>
        RegisterSlave = 0x15,
        /// <summary>
        ///     准备语句命令。
        /// </summary>
        StmtPrepare = 0x16,
        /// <summary>
        ///     执行语句命令。
        /// </summary>
        StmtExecute = 0x17,
        /// <summary>
        ///     发送长数据命令。
        /// </summary>
        StmtSendLongData = 0x18,
        /// <summary>
        ///     关闭语句命令。
        /// </summary>
        StmtClose = 0x19,
        /// <summary>
        ///     重置语句命令。
        /// </summary>
        StmtReset = 0x1a,
        /// <summary>
        ///     设置选项命令。
        /// </summary>
        SetOption = 0x1b,
        /// <summary>
        ///     语句获取命令。
        /// </summary>
        StmtFetch = 0x1c,
        /// <summary>
        ///     守护进程命令。
        /// </summary>
        Daemon = 0x1d,
        /// <summary>
        ///     二进制日志转储 GTID 命令。
        /// </summary>
        BinlogDumpGtid = 0x1e,
        /// <summary>
        ///     重置连接命令。
        /// </summary>
        ResetConnection = 0x1f
    }
    
    /// <summary>
    ///     错误码。
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        ///     成功。
        /// </summary>
        Success = 0,
        /// <summary>
        ///     访问被拒绝。
        /// </summary>
        AccessDenied = 1045,
        /// <summary>
        ///     数据库不存在。
        /// </summary>
        DatabaseNotFound = 1049,
        /// <summary>
        ///     表不存在。
        /// </summary>
        TableNotFound = 1146
    }
}