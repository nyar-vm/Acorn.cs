namespace Acorn.Frame;

/// <summary>
///     协议帧接口，定义二进制协议中消息边界的识别方式。
/// </summary>
/// <remarks>
///     <para>
///     不同的二进制协议使用不同的帧格式来标识消息边界：
///     长度前缀（Wasm Section）、类型+长度+数据（GLB Chunk）、
///     魔数+头部+载荷（PSD）、变长标签+数据（Protobuf）等。
///     </para>
///     <para>
///     协议实现仅操作 <see cref="ReadOnlySpan{T}" />，
///     由 <see cref="FrameScanner{TProtocol}" /> 负责管理缓冲区位置推进，
///     通过 <see cref="Frame.Size" /> 确定消耗的字节数。
///     </para>
/// </remarks>
public interface IFrameProtocol
{
    /// <summary>
    ///     最小帧大小（字节），用于预读判断。
    ///     当缓冲区剩余数据少于此值时，无法构成一个完整帧。
    /// </summary>
    int MinFrameSize { get; }

    /// <summary>
    ///     尝试从剩余数据中预读帧大小，不消耗任何数据。
    /// </summary>
    /// <param name="buffer">从当前位置开始的剩余数据。</param>
    /// <param name="frameSize">如果成功则输出帧大小（字节），包含帧头。</param>
    /// <returns>如果剩余数据中有足够数据确定帧大小则返回 true。</returns>
    bool TryPeekFrameSize(ReadOnlySpan<byte> buffer, out int frameSize);

    /// <summary>
    ///     尝试从剩余数据中读取一个完整帧。
    /// </summary>
    /// <param name="buffer">从当前位置开始的剩余数据。</param>
    /// <param name="frame">如果成功则输出帧数据。</param>
    /// <returns>如果成功读取一个完整帧则返回 true。</returns>
    bool TryReadFrame(ReadOnlySpan<byte> buffer, out Frame frame);
}
