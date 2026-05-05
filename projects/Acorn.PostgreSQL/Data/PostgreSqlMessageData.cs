namespace Acorn.PostgreSql.Data;

/// <summary>
///     PostgreSQL 消息数据结构。
/// </summary>
public class PostgreSqlMessageData
{
    /// <summary>
    ///     获取或设置消息类型。
    /// </summary>
    public PostgreSQLConstants.MessageType Type { get; set; }
    
    /// <summary>
    ///     获取或设置消息长度。
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    ///     获取或设置消息内容。
    /// </summary>
    public byte[] Data { get; set; } = null!;

    /// <summary>
    ///     获取或设置认证类型（仅适用于认证请求消息）。
    /// </summary>
    public PostgreSQLConstants.AuthenticationType? AuthenticationType { get; set; }

    /// <summary>
    ///     获取或设置认证数据（仅适用于认证请求消息）。
    /// </summary>
    public byte[] AuthenticationData { get; set; } = null!;

    /// <summary>
    ///     获取或设置错误消息（仅适用于错误响应消息）。
    /// </summary>
    public Dictionary<string, string> ErrorFields { get; set; } = new();

    /// <summary>
    ///     获取或设置命令标签（仅适用于命令完成消息）。
    /// </summary>
    public string CommandTag { get; set; } = null!;
    
    /// <summary>
    ///     获取或设置事务状态（仅适用于就绪消息）。
    /// </summary>
    public PostgreSQLConstants.TransactionStatus? TransactionStatus { get; set; }
    
    /// <summary>
    ///     获取或设置字段描述（仅适用于行描述消息）。
    /// </summary>
    public List<PostgreSqlFieldDescription> FieldDescriptions { get; set; } = new();
}

/// <summary>
///     PostgreSQL 字段描述。
/// </summary>
public class PostgreSqlFieldDescription
{
    /// <summary>
    ///     获取或设置字段名称。
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    ///     获取或设置表 ID。
    /// </summary>
    public uint TableId { get; set; }
    
    /// <summary>
    ///     获取或设置字段 ID。
    /// </summary>
    public ushort ColumnId { get; set; }
    
    /// <summary>
    ///     获取或设置数据类型 ID。
    /// </summary>
    public uint DataTypeOid { get; set; }
    
    /// <summary>
    ///     获取或设置数据类型大小。
    /// </summary>
    public ushort DataTypeSize { get; set; }
    
    /// <summary>
    ///     获取或设置类型修饰符。
    /// </summary>
    public int TypeModifier { get; set; }
    
    /// <summary>
    ///     获取或设置格式代码。
    /// </summary>
    public ushort FormatCode { get; set; }
}