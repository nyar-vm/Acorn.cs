using System.Runtime.CompilerServices;

namespace Acorn.Frame;

/// <summary>
///     协议帧数据结构，表示二进制协议中的一个完整消息帧。
/// </summary>
/// <remarks>
///     <para>
///     Frame 使用 <c>ref struct</c> 以持有 <see cref="ReadOnlySpan{T}" />，
///     确保零拷贝访问帧载荷数据。Frame 不能存储在堆上。
///     </para>
///     <para>
///     <see cref="Size" /> 包含帧的完整大小（帧头 + 载荷），
///     <see cref="Payload" /> 仅包含帧的载荷部分（去除帧头），
///     <see cref="Raw" /> 包含帧的完整原始字节（帧头 + 载荷）。
///     </para>
/// </remarks>
public readonly ref struct Frame
{
    private readonly int _size;
    private readonly ReadOnlySpan<byte> _payload;
    private readonly ReadOnlySpan<byte> _raw;

    /// <summary>
    ///     初始化 <see cref="Frame" /> 结构的新实例。
    /// </summary>
    /// <param name="size">帧的完整大小（帧头 + 载荷）。</param>
    /// <param name="payload">帧的载荷数据。</param>
    /// <param name="raw">帧的完整原始字节（帧头 + 载荷）。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Frame(int size, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> raw)
    {
        _size = size;
        _payload = payload;
        _raw = raw;
    }

    /// <summary>
    ///     初始化 <see cref="Frame" /> 结构的新实例（不提供原始字节）。
    /// </summary>
    /// <param name="size">帧的完整大小（帧头 + 载荷）。</param>
    /// <param name="payload">帧的载荷数据。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Frame(int size, ReadOnlySpan<byte> payload)
    {
        _size = size;
        _payload = payload;
        _raw = default;
    }

    /// <summary>
    ///     帧的完整大小（字节），包含帧头和载荷。
    /// </summary>
    public int Size => _size;

    /// <summary>
    ///     帧的载荷数据（去除帧头后的数据）。
    /// </summary>
    public ReadOnlySpan<byte> Payload => _payload;

    /// <summary>
    ///     帧的完整原始字节（帧头 + 载荷）。
    /// </summary>
    public ReadOnlySpan<byte> Raw => _raw;
}
