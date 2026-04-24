namespace Acorn.MessagePack.Data;

/// <summary>
///     MessagePack 数据。
/// </summary>
public sealed class MsgPackData
{
    /// <summary>
    ///     根值。
    /// </summary>
    public MsgPackValue Root { get; init; } = new();
}

/// <summary>
///     MessagePack 值。
/// </summary>
public sealed class MsgPackValue
{
    /// <summary>
    ///     值类型。
    /// </summary>
    public MsgPackType Type { get; init; }

    /// <summary>
    ///     原始值。
    /// </summary>
    public object? RawValue { get; init; }

    /// <summary>
    ///     数组元素（当类型为 Array 时）。
    /// </summary>
    public IReadOnlyList<MsgPackValue> ArrayItems { get; init; } = [];

    /// <summary>
    ///     映射条目（当类型为 Map 时）。
    /// </summary>
    public IReadOnlyList<MsgPackMapEntry> MapEntries { get; init; } = [];

    /// <summary>
    ///     扩展类型代码。
    /// </summary>
    public sbyte ExtensionType { get; init; }

    /// <summary>
    ///     扩展数据。
    /// </summary>
    public byte[] ExtensionData { get; init; } = [];
}

/// <summary>
///     MessagePack 映射条目。
/// </summary>
public sealed class MsgPackMapEntry
{
    /// <summary>
    ///     键。
    /// </summary>
    public MsgPackValue Key { get; init; } = new();

    /// <summary>
    ///     值。
    /// </summary>
    public MsgPackValue Value { get; init; } = new();
}
