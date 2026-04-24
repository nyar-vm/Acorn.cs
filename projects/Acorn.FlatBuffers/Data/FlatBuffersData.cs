namespace Acorn.FlatBuffers.Data;

/// <summary>
///     FlatBuffers 表数据。
/// </summary>
public sealed class FlatBufferTable
{
    /// <summary>
    ///     表在缓冲区中的偏移。
    /// </summary>
    public uint TableOffset { get; init; }

    /// <summary>
    ///     vtable 偏移。
    /// </summary>
    public uint VTableOffset { get; init; }

    /// <summary>
    ///     vtable 大小。
    /// </summary>
    public ushort VTableSize { get; init; }

    /// <summary>
    ///     字段列表。
    /// </summary>
    public IReadOnlyList<FlatBufferField> Fields { get; init; } = [];
}

/// <summary>
///     FlatBuffers 字段。
/// </summary>
public sealed class FlatBufferField
{
    /// <summary>
    ///     字段索引。
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    ///     字段在 vtable 中的偏移（0 表示不存在）。
    /// </summary>
    public ushort VTableOffset { get; init; }

    /// <summary>
    ///     字段类型。
    /// </summary>
    public FlatBufferFieldType Type { get; init; }

    /// <summary>
    ///     字段值。
    /// </summary>
    public object? Value { get; init; }
}
