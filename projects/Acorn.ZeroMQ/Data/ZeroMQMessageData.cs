namespace Acorn.ZeroMQ.Data;

/// <summary>
///     ZeroMQ 消息数据结构。
/// </summary>
public class ZeroMQMessageData
{
    /// <summary>
    ///     获取或设置消息类型。
    /// </summary>
    public ZeroMQConstants.MessageType Type { get; set; }
    
    /// <summary>
    ///     获取或设置消息长度。
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    ///     获取或设置消息内容。
    /// </summary>
    public byte[] Data { get; set; }
    
    /// <summary>
    ///     获取或设置标志。
    /// </summary>
    public ZeroMQConstants.Flags Flags { get; set; }
    
    /// <summary>
    ///     获取或设置命令类型（仅适用于命令消息）。
    /// </summary>
    public ZeroMQConstants.CommandType? CommandType { get; set; }
    
    /// <summary>
    ///     获取或设置套接字类型（仅适用于连接/绑定命令）。
    /// </summary>
    public ZeroMQConstants.SocketType? SocketType { get; set; }
    
    /// <summary>
    ///     获取或设置地址（仅适用于连接/绑定命令）。
    /// </summary>
    public string Address { get; set; }
    
    /// <summary>
    ///     获取或设置消息部分（仅适用于多部分消息）。
    /// </summary>
    public List<ZeroMQMessageData> Parts { get; set; } = new();
}

/// <summary>
///     ZeroMQ 帧数据结构。
/// </summary>
public class ZeroMQFrameData
{
    /// <summary>
    ///     获取或设置帧标志。
    /// </summary>
    public byte Flags { get; set; }
    
    /// <summary>
    ///     获取或设置帧长度。
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    ///     获取或设置帧内容。
    /// </summary>
    public byte[] Data { get; set; }
}