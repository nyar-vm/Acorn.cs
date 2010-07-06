using System.Runtime.CompilerServices;

namespace Acorn.Frame;

/// <summary>
///     泛型帧扫描器，在编译期绑定协议实现，实现零开销抽象的帧级扫描。
/// </summary>
/// <typeparam name="TProtocol">协议实现类型，必须实现 <see cref="IFrameProtocol" />。</typeparam>
/// <remarks>
///     <para>
///     通过泛型约束 <c>where TProtocol : struct, IFrameProtocol</c>，
///     JIT 编译器可以将协议方法内联，消除虚方法调用开销，
///     实现与手写代码相同的性能。
///     </para>
/// </remarks>
public ref struct FrameScanner<TProtocol> where TProtocol : struct, IFrameProtocol
{
    private ByteBuffer _buffer;
    private readonly TProtocol _protocol;

    /// <summary>
    ///     初始化 <see cref="FrameScanner{TProtocol}" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的字节数据。</param>
    /// <param name="protocol">协议实现实例。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FrameScanner(ReadOnlySpan<byte> data, TProtocol protocol = default)
    {
        _buffer = new ByteBuffer(data);
        _protocol = protocol;
    }

    /// <summary>
    ///     获取当前扫描位置。
    /// </summary>
    public int Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Position;
    }

    /// <summary>
    ///     获取数据总长度。
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Length;
    }

    /// <summary>
    ///     获取一个值，该值指示是否已到达数据末尾。
    /// </summary>
    public bool IsEnd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.IsEnd;
    }

    /// <summary>
    ///     获取底层缓冲区。
    /// </summary>
    public ByteBuffer Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer;
    }

    /// <summary>
    ///     获取协议实现实例。
    /// </summary>
    public TProtocol Protocol
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _protocol;
    }

    /// <summary>
    ///     尝试读取下一个帧。
    /// </summary>
    /// <param name="frame">如果成功则输出帧数据。</param>
    /// <returns>如果成功读取一个完整帧则返回 true。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadNext(out Frame frame)
    {
        if (_protocol.TryReadFrame(_buffer.RemainingSpan, out frame))
        {
            _buffer.Advance(frame.Size);
            return true;
        }

        frame = default;
        return false;
    }

    /// <summary>
    ///     尝试预读下一帧大小，不消耗任何数据。
    /// </summary>
    /// <param name="frameSize">如果成功则输出帧大小。</param>
    /// <returns>如果成功预读帧大小则返回 true。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeekFrameSize(out int frameSize)
    {
        return _protocol.TryPeekFrameSize(_buffer.RemainingSpan, out frameSize);
    }
}
