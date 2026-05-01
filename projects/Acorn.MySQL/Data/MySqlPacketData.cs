using Acorn.Attributes;

namespace Acorn.MySql.Data;

/// <summary>
///     MySQL 数据包数据结构。
/// </summary>
public class MySqlPacketData
{
    /// <summary>
    ///     获取或设置包长度。
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    ///     获取或设置包序号。
    /// </summary>
    public byte SequenceId { get; set; }
    
    /// <summary>
    ///     获取或设置包类型。
    /// </summary>
    public MySqlPacketType Type { get; set; }
    
    /// <summary>
    ///     获取或设置包内容。
    /// </summary>
    public byte[] Data { get; set; }
    
    /// <summary>
    ///     获取或设置命令类型（仅适用于命令包）。
    /// </summary>
    public MySQLConstants.CommandType? CommandType { get; set; }
    
    /// <summary>
    ///     获取或设置错误码（仅适用于错误包）。
    /// </summary>
    public MySQLConstants.ErrorCode? ErrorCode { get; set; }
    
    /// <summary>
    ///     获取或设置错误消息（仅适用于错误包）。
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    ///     获取或设置服务器状态（仅适用于结果包）。
    /// </summary>
    public MySQLConstants.ServerStatus? ServerStatus { get; set; }
}

/// <summary>
///     MySQL 数据包类型。
/// </summary>
public enum MySqlPacketType
{
    /// <summary>
    ///     未知类型。
    /// </summary>
    Unknown,
    /// <summary>
    ///     握手包。
    /// </summary>
    Handshake,
    /// <summary>
    ///     握手响应包。
    /// </summary>
    HandshakeResponse,
    /// <summary>
    ///     命令包。
    /// </summary>
    Command,
    /// <summary>
    ///     结果包。
    /// </summary>
    Result,
    /// <summary>
    ///     错误包。
    /// </summary>
    Error,
    /// <summary>
    ///     字段包。
    /// </summary>
    Field,
    /// <summary>
    ///     行数据包。
    /// </summary>
    RowData,
    /// <summary>
    ///     EOF 包。
    /// </summary>
    Eof
}

/// <summary>
///     MySQL 数据包头部（4 字节）。
/// </summary>
/// <remarks>
///     MySQL 包头包含 3 字节载荷长度（小端序）和 1 字节序号。
/// </remarks>
[BinarySerializable]
public partial struct MySqlPacketHeader
{
    [Field(Order = 0)]
    public byte Length0;

    [Field(Order = 1)]
    public byte Length1;

    [Field(Order = 2)]
    public byte Length2;

    [Field(Order = 3)]
    public byte SequenceId;

    /// <summary>
    ///     载荷长度（由 3 字节小端序组合）。
    /// </summary>
    public int PayloadLength => Length0 | (Length1 << 8) | (Length2 << 16);

    /// <summary>
    ///     从载荷长度和序号创建头部。
    /// </summary>
    public static MySqlPacketHeader Create(int payloadLength, byte sequenceId)
    {
        return new MySqlPacketHeader
        {
            Length0 = (byte)(payloadLength & 0xFF),
            Length1 = (byte)((payloadLength >> 8) & 0xFF),
            Length2 = (byte)((payloadLength >> 16) & 0xFF),
            SequenceId = sequenceId
        };
    }
}