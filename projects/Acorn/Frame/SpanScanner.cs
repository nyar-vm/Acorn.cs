using System.Runtime.CompilerServices;

namespace Acorn.Frame;

/// <summary>
///     通用二进制扫描器，封装 <see cref="ByteBuffer" /> 并提供所有格式扫描器共享的基础方法。
/// </summary>
/// <remarks>
///     <para>
///     由于 C# 的 <c>ref struct</c> 不支持继承，各格式扫描器无法通过基类共享代码。
///     <see cref="SpanScanner" /> 通过组合模式解决此问题：各格式扫描器内部持有
///     <see cref="SpanScanner" /> 实例，通过 <see cref="P:Buffer" /> 属性访问底层
///     <see cref="ByteBuffer" /> 进行格式特定的读取操作。
///     </para>
///     <para>
///     本类型提供的方法是所有格式扫描器的公共子集：位置管理、魔数匹配、字节查看与读取。
///     格式特定的读取操作（如 <c>ReadU32LE</c>、<c>ReadLeb128U32</c> 等）
///     通过 <see cref="P:Buffer" /> 属性直接访问 <see cref="ByteBuffer" /> 完成。
///     </para>
/// </remarks>
public ref struct SpanScanner
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始化 <see cref="SpanScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的字节数据。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取或设置当前扫描位置。
    /// </summary>
    public int Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _buffer.Position = value;
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
    ///     获取从当前位置到末尾的只读字节 Span。
    /// </summary>
    public ReadOnlySpan<byte> Remaining
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.RemainingSpan;
    }

    /// <summary>
    ///     获取底层只读字节 Span。
    /// </summary>
    public ReadOnlySpan<byte> Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.Data;
    }

    /// <summary>
    ///     获取底层 <see cref="ByteBuffer" /> 的引用，用于格式特定的读取操作。
    /// </summary>
    /// <remarks>
    ///     通过 <c>ref</c> 返回确保对 <see cref="ByteBuffer" /> 的修改（如位置前进）
    ///     直接反映在 <see cref="SpanScanner" /> 的内部状态上。
    /// </remarks>
    public ref ByteBuffer Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _buffer;
    }

    /// <summary>
    ///     向前移动指定字节数，不返回任何数据。
    /// </summary>
    /// <param name="count">要跳过的字节数。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        _buffer.Advance(count);
    }

    /// <summary>
    ///     查看接下来的若干字节但不移动位置。
    /// </summary>
    /// <param name="count">要查看的字节数。</param>
    /// <returns>指定长度的只读字节 Span。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> Peek(int count)
    {
        return _buffer.Peek(count);
    }

    /// <summary>
    ///     读取指定数量的字节并前进位置。
    /// </summary>
    /// <param name="count">要读取的字节数。</param>
    /// <returns>指定长度的只读字节 Span。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> Read(int count)
    {
        return _buffer.ReadBytes(count);
    }

    /// <summary>
    ///     尝试匹配魔数（Magic Number）。
    /// </summary>
    /// <param name="magic">期望的魔数字节序列。</param>
    /// <returns>如果匹配成功则返回 true，否则返回 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MatchMagic(ReadOnlySpan<byte> magic)
    {
        return _buffer.MatchMagic(magic);
    }

    /// <summary>
    ///     尝试匹配魔数并自动前进位置。
    /// </summary>
    /// <param name="magic">期望的魔数字节序列。</param>
    /// <returns>如果匹配成功则返回 true 并前进位置，否则返回 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConsumeMagic(ReadOnlySpan<byte> magic)
    {
        return _buffer.ConsumeMagic(magic);
    }
}
