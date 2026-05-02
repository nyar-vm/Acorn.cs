namespace Acorn.Wasm.Data;

/// <summary>
///     WASM GC 复合类型种类。
/// </summary>
public enum WasmCompositeTypeKind
{
    /// <summary>
    ///     结构体类型。
    /// </summary>
    Struct,

    /// <summary>
    ///     数组类型。
    /// </summary>
    Array,

    /// <summary>
    ///     递归类型组。
    /// </summary>
    Rec
}

/// <summary>
///     WASM GC 打包类型，用于结构体字段的内存压缩。
/// </summary>
public enum WasmPackedType : byte
{
    /// <summary>
    ///     8 位整数打包。
    /// </summary>
    I8 = 0x78,

    /// <summary>
    ///     16 位整数打包。
    /// </summary>
    I16 = 0x77
}

/// <summary>
///     WASM GC 子类型定义，支持 subtyping 和 final 标记。
/// </summary>
public sealed class WasmSubType
{
    /// <summary>
    ///     是否为 final 类型（不可被继承）。
    /// </summary>
    public bool Final { get; init; }

    /// <summary>
    ///     父类型索引，null 表示无父类型（顶层类型）。
    /// </summary>
    public uint? SuperTypeIndex { get; init; }

    /// <summary>
    ///     实际的复合类型定义。
    /// </summary>
    public WasmCompositeType Type { get; init; } = null!;
}

/// <summary>
///     WASM GC 复合类型定义（struct/array/rec）。
/// </summary>
public sealed class WasmCompositeType
{
    /// <summary>
    ///     类型种类。
    /// </summary>
    public WasmCompositeTypeKind Kind { get; init; }

    /// <summary>
    ///     字段定义（Struct 类型时有效）。
    /// </summary>
    public IReadOnlyList<WasmFieldType>? Fields { get; init; }

    /// <summary>
    ///     元素类型（Array 类型时有效）。
    /// </summary>
    public WasmStorageType? ElementType { get; init; }

    /// <summary>
    ///     递归子类型列表（Rec 类型时有效）。
    /// </summary>
    public IReadOnlyList<WasmSubType>? SubTypes { get; init; }
}

/// <summary>
///     WASM GC 结构体字段类型，包含存储类型和可变性。
/// </summary>
public sealed class WasmFieldType
{
    /// <summary>
    ///     字段的存储类型。
    /// </summary>
    public WasmStorageType StorageType { get; init; } = null!;

    /// <summary>
    ///     字段是否可变。
    /// </summary>
    public bool Mutable { get; init; }
}

/// <summary>
///     WASM GC 存储类型，支持打包和未打包的值类型。
/// </summary>
public sealed class WasmStorageType
{
    /// <summary>
    ///     打包类型，null 表示未打包。
    /// </summary>
    public WasmPackedType? PackedType { get; init; }

    /// <summary>
    ///     值类型或引用类型。
    /// </summary>
    public WasmValueType ValueType { get; init; }
}
