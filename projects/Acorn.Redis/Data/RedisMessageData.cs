namespace Acorn.Redis.Data;

/// <summary>
///     Redis 消息数据结构。
/// </summary>
public class RedisMessageData
{
    /// <summary>
    ///     获取或设置消息类型。
    /// </summary>
    public RedisMessageType Type { get; set; }
    
    /// <summary>
    ///     获取或设置简单字符串值（仅适用于简单字符串类型）。
    /// </summary>
    public string SimpleString { get; set; }
    
    /// <summary>
    ///     获取或设置错误消息（仅适用于错误类型）。
    /// </summary>
    public string Error { get; set; }
    
    /// <summary>
    ///     获取或设置整数值（仅适用于整数类型）。
    /// </summary>
    public long Integer { get; set; }
    
    /// <summary>
    ///     获取或设置批量字符串值（仅适用于批量字符串类型）。
    /// </summary>
    public byte[] BulkString { get; set; }
    
    /// <summary>
    ///     获取或设置数组元素（仅适用于数组类型）。
    /// </summary>
    public List<RedisMessageData> Array { get; set; } = new();
    
    /// <summary>
    ///     获取或设置是否为空批量字符串。
    /// </summary>
    public bool IsNullBulkString { get; set; }
    
    /// <summary>
    ///     获取或设置是否为空数组。
    /// </summary>
    public bool IsNullArray { get; set; }
}

/// <summary>
///     Redis 消息类型。
/// </summary>
public enum RedisMessageType
{
    /// <summary>
    ///     简单字符串。
    /// </summary>
    SimpleString,
    /// <summary>
    ///     错误。
    /// </summary>
    Error,
    /// <summary>
    ///     整数。
    /// </summary>
    Integer,
    /// <summary>
    ///     批量字符串。
    /// </summary>
    BulkString,
    /// <summary>
    ///     数组。
    /// </summary>
    Array
}