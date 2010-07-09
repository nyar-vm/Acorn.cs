namespace Acorn.ZeroMQ.Data;

/// <summary>
///     ZeroMQ 协议常量。
/// </summary>
public static class ZeroMQConstants
{
    /// <summary>
    ///     ZeroMQ 版本。
    /// </summary>
    public const int Version = 3;
    
    /// <summary>
    ///     帧大小。
    /// </summary>
    public const int FrameSize = 8;
    
    /// <summary>
    ///     消息类型。
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        ///     消息部分。
        /// </summary>
        MessagePart = 0,
        /// <summary>
        ///     消息结束。
        /// </summary>
        MessageEnd = 1
    }
    
    /// <summary>
    ///     命令类型。
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        ///     连接命令。
        /// </summary>
        Connect = 1,
        /// <summary>
        ///     绑定命令。
        /// </summary>
        Bind = 2,
        /// <summary>
        ///     轮询命令。
        /// </summary>
        Poll = 3,
        /// <summary>
        ///     发送命令。
        /// </summary>
        Send = 4,
        /// <summary>
        ///     接收命令。
        /// </summary>
        Recv = 5,
        /// <summary>
        ///     关闭命令。
        /// </summary>
        Close = 6,
        /// <summary>
        ///     终止命令。
        /// </summary>
        Terminate = 7
    }
    
    /// <summary>
    ///     标志。
    /// </summary>
    [Flags]
    public enum Flags
    {
        /// <summary>
        ///     无标志。
        /// </summary>
        None = 0,
        /// <summary>
        ///     更多消息部分。
        /// </summary>
        More = 1,
        /// <summary>
        ///     不要等待。
        /// </summary>
        DontWait = 2,
        /// <summary>
        ///     发送高水印。
        /// </summary>
        SndHwm = 4,
        /// <summary>
        ///     接收高水印。
        /// </summary>
        RcvHwm = 8,
        /// <summary>
        ///     发送超时。
        /// </summary>
        SndTimeout = 16,
        /// <summary>
        ///     接收超时。
        /// </summary>
        RcvTimeout = 32,
        /// <summary>
        ///      linger。
        /// </summary>
        Linger = 64,
        /// <summary>
        ///     重连间隔。
        /// </summary>
        ReconnectIvl = 128,
        /// <summary>
        ///     最大重连间隔。
        /// </summary>
        ReconnectIvlMax = 256,
        /// <summary>
        ///     退避。
        /// </summary>
        Backlog = 512,
        /// <summary>
        ///      ipv4 只。
        /// </summary>
        Ipv4Only = 1024,
        /// <summary>
        ///     延迟连接。
        /// </summary>
        DelayAttachOnConnect = 2048,
        /// <summary>
        ///     接受连接。
        /// </summary>
        AcceptConn = 4096,
        /// <summary>
        ///     最大消息大小。
        /// </summary>
        MaxMsgSize = 8192,
        /// <summary>
        ///     多播循环。
        /// </summary>
        MulticastLoop = 16384,
        /// <summary>
        ///     路由 ID。
        /// </summary>
        RouterMandatory = 32768,
        /// <summary>
        ///     路由 ID。
        /// </summary>
        RouterHandover = 65536,
        /// <summary>
        ///     路由 ID。
        /// </summary>
        RouterRaw = 131072,
        /// <summary>
        ///     订阅。
        /// </summary>
        Subscribe = 262144,
        /// <summary>
        ///     取消订阅。
        /// </summary>
        Unsubscribe = 524288
    }
    
    /// <summary>
    ///     套接字类型。
    /// </summary>
    public enum SocketType
    {
        /// <summary>
        ///     对一对一。
        /// </summary>
        Pair = 0,
        /// <summary>
        ///     发布-订阅。
        /// </summary>
        Pub = 1,
        /// <summary>
        ///     订阅-发布。
        /// </summary>
        Sub = 2,
        /// <summary>
        ///     请求-响应。
        /// </summary>
        Req = 3,
        /// <summary>
        ///     响应-请求。
        /// </summary>
        Rep = 4,
        /// <summary>
        ///     推送-拉取。
        /// </summary>
        Dealer = 5,
        /// <summary>
        ///     拉取-推送。
        /// </summary>
        Router = 6,
        /// <summary>
        ///     推送。
        /// </summary>
        Push = 7,
        /// <summary>
        ///     拉取。
        /// </summary>
        Pull = 8
    }
}