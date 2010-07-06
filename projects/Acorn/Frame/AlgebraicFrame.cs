namespace Acorn.Frame;

/// <summary>
///     代数帧（判别联合）类型，表示一帧可能是多种类型之一。
/// </summary>
/// <typeparam name="TKind">帧类型枚举，必须实现 <see cref="IFrameKind" />。</typeparam>
/// <typeparam name="TProtocol">协议实现类型，必须实现 <see cref="IFrameProtocol" />。</typeparam>
/// <remarks>
///     <para>
///     AlgebraicFrame 持有 <see cref="Kind" /> 和 <see cref="Frame" />，
///     用于表达"一帧可能是多种类型之一"的语义。
///     </para>
///     <para>
///     <c>Match</c> / <c>Switch</c> 方法由源生成器提供，当前阶段仅提供基础设施。
///     </para>
/// </remarks>
public readonly ref struct AlgebraicFrame<TKind, TProtocol>
    where TKind : struct, IFrameKind
    where TProtocol : struct, IFrameProtocol
{
    private readonly TKind _kind;
    private readonly Frame _frame;

    /// <summary>
    ///     初始化 <see cref="AlgebraicFrame{TKind, TProtocol}" /> 结构的新实例。
    /// </summary>
    /// <param name="kind">帧类型标识。</param>
    /// <param name="frame">帧数据。</param>
    public AlgebraicFrame(TKind kind, Frame frame)
    {
        _kind = kind;
        _frame = frame;
    }

    /// <summary>
    ///     获取帧类型标识。
    /// </summary>
    public TKind Kind => _kind;

    /// <summary>
    ///     获取帧数据。
    /// </summary>
    public Frame Frame => _frame;
}
