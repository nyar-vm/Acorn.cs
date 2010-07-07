namespace Acorn.Wasm.Data;

/// <summary>
///     WebAssembly 模块数据，包含完整的 Wasm 二进制模块结构。
/// </summary>
public sealed class WasmModuleData
{
    /// <summary>
    ///     Wasm 二进制格式版本号。
    /// </summary>
    public uint Version { get; init; } = 1;

    /// <summary>
    ///     类型段，包含所有函数签名定义。
    /// </summary>
    public IReadOnlyList<WasmFunctionType> Types { get; init; } = [];

    /// <summary>
    ///     导入段，包含所有外部导入项。
    /// </summary>
    public IReadOnlyList<WasmImport> Imports { get; init; } = [];

    /// <summary>
    ///     函数段，包含每个函数的类型索引（不含导入函数）。
    /// </summary>
    public IReadOnlyList<uint> FunctionTypeIndices { get; init; } = [];

    /// <summary>
    ///     表段，包含所有表定义。
    /// </summary>
    public IReadOnlyList<WasmTable> Tables { get; init; } = [];

    /// <summary>
    ///     内存段，包含所有内存定义。
    /// </summary>
    public IReadOnlyList<WasmMemory> Memories { get; init; } = [];

    /// <summary>
    ///     全局段，包含所有全局变量定义。
    /// </summary>
    public IReadOnlyList<WasmGlobal> Globals { get; init; } = [];

    /// <summary>
    ///     导出段，包含所有导出项。
    /// </summary>
    public IReadOnlyList<WasmExport> Exports { get; init; } = [];

    /// <summary>
    ///     起始函数索引。
    /// </summary>
    public uint? StartFunctionIndex { get; init; }

    /// <summary>
    ///     元素段，包含表初始化数据。
    /// </summary>
    public IReadOnlyList<WasmElement> Elements { get; init; } = [];

    /// <summary>
    ///     代码段，包含函数体（不含导入函数）。
    /// </summary>
    public IReadOnlyList<WasmCode> Codes { get; init; } = [];

    /// <summary>
    ///     数据段，包含内存初始化数据。
    /// </summary>
    public IReadOnlyList<WasmData> DataSegments { get; init; } = [];

    /// <summary>
    ///     自定义段列表。
    /// </summary>
    public IReadOnlyList<WasmCustomSection> CustomSections { get; init; } = [];
}

/// <summary>
///     Wasm 值类型。
/// </summary>
public enum WasmValueType : byte
{
    /// <summary>
    ///     32 位整数。
    /// </summary>
    Int32 = 0x7F,

    /// <summary>
    ///     64 位整数。
    /// </summary>
    Int64 = 0x7E,

    /// <summary>
    ///     32 位浮点数。
    /// </summary>
    Float32 = 0x7D,

    /// <summary>
    ///     64 位浮点数。
    /// </summary>
    Float64 = 0x7C,

    /// <summary>
    ///     函数引用。
    /// </summary>
    FuncRef = 0x70,

    /// <summary>
    ///     外部引用。
    /// </summary>
    ExternRef = 0x6F
}

/// <summary>
///     Wasm 函数类型（函数签名）。
/// </summary>
public sealed class WasmFunctionType
{
    /// <summary>
    ///     参数类型列表。
    /// </summary>
    public IReadOnlyList<WasmValueType> Parameters { get; init; } = [];

    /// <summary>
    ///     返回值类型列表。
    /// </summary>
    public IReadOnlyList<WasmValueType> Results { get; init; } = [];
}

/// <summary>
///     Wasm 限制（用于表和内存的容量范围）。
/// </summary>
public sealed class WasmLimits
{
    /// <summary>
    ///     最小值。
    /// </summary>
    public uint Minimum { get; init; }

    /// <summary>
    ///     最大值，null 表示无上限。
    /// </summary>
    public uint? Maximum { get; init; }
}

/// <summary>
///     Wasm 表类型。
/// </summary>
public sealed class WasmTableType
{
    /// <summary>
    ///     元素类型。
    /// </summary>
    public WasmValueType ElementType { get; init; } = WasmValueType.FuncRef;

    /// <summary>
    ///     容量限制。
    /// </summary>
    public WasmLimits Limits { get; init; } = new();
}

/// <summary>
///     Wasm 内存类型。
/// </summary>
public sealed class WasmMemoryType
{
    /// <summary>
    ///     容量限制。
    /// </summary>
    public WasmLimits Limits { get; init; } = new();
}

/// <summary>
///     Wasm 全局类型。
/// </summary>
public sealed class WasmGlobalType
{
    /// <summary>
    ///     值类型。
    /// </summary>
    public WasmValueType ValueType { get; init; }

    /// <summary>
    ///     是否可变。
    /// </summary>
    public bool Mutable { get; init; }
}

/// <summary>
///     Wasm 导入描述符。
/// </summary>
public sealed class WasmImport
{
    /// <summary>
    ///     模块名称。
    /// </summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>
    ///     字段名称。
    /// </summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>
    ///     导入描述。
    /// </summary>
    public WasmImportDescriptor Descriptor { get; init; } = new();
}

/// <summary>
///     Wasm 导入描述，包含导入类型和对应索引。
/// </summary>
public sealed class WasmImportDescriptor
{
    /// <summary>
    ///     导入种类。
    /// </summary>
    public WasmExternalKind Kind { get; init; }

    /// <summary>
    ///     函数类型索引（当 Kind 为 Function 时有效）。
    /// </summary>
    public uint FunctionTypeIndex { get; init; }

    /// <summary>
    ///     表类型（当 Kind 为 Table 时有效）。
    /// </summary>
    public WasmTableType? TableType { get; init; }

    /// <summary>
    ///     内存类型（当 Kind 为 Memory 时有效）。
    /// </summary>
    public WasmMemoryType? MemoryType { get; init; }

    /// <summary>
    ///     全局类型（当 Kind 为 Global 时有效）。
    /// </summary>
    public WasmGlobalType? GlobalType { get; init; }
}

/// <summary>
///     Wasm 外部种类。
/// </summary>
public enum WasmExternalKind : byte
{
    /// <summary>
    ///     函数。
    /// </summary>
    Function = 0x00,

    /// <summary>
    ///     表。
    /// </summary>
    Table = 0x01,

    /// <summary>
    ///     内存。
    /// </summary>
    Memory = 0x02,

    /// <summary>
    ///     全局变量。
    /// </summary>
    Global = 0x03
}

/// <summary>
///     Wasm 表定义。
/// </summary>
public sealed class WasmTable
{
    /// <summary>
    ///     表类型。
    /// </summary>
    public WasmTableType Type { get; init; } = new();
}

/// <summary>
///     Wasm 内存定义。
/// </summary>
public sealed class WasmMemory
{
    /// <summary>
    ///     内存类型。
    /// </summary>
    public WasmMemoryType Type { get; init; } = new();
}

/// <summary>
///     Wasm 全局变量定义。
/// </summary>
public sealed class WasmGlobal
{
    /// <summary>
    ///     全局类型。
    /// </summary>
    public WasmGlobalType Type { get; init; } = new();

    /// <summary>
    ///     初始化指令字节码。
    /// </summary>
    public byte[] InitExpression { get; init; } = [];
}

/// <summary>
///     Wasm 导出项。
/// </summary>
public sealed class WasmExport
{
    /// <summary>
    ///     导出名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     导出种类。
    /// </summary>
    public WasmExternalKind Kind { get; init; }

    /// <summary>
    ///     导出项索引。
    /// </summary>
    public uint Index { get; init; }
}

/// <summary>
///     Wasm 元素段项（表初始化数据）。
/// </summary>
public sealed class WasmElement
{
    /// <summary>
    ///     目标表索引。
    /// </summary>
    public uint TableIndex { get; init; }

    /// <summary>
    ///     偏移量初始化指令字节码。
    /// </summary>
    public byte[] OffsetExpression { get; init; } = [];

    /// <summary>
    ///     初始化元素列表（函数索引）。
    /// </summary>
    public IReadOnlyList<uint> InitValues { get; init; } = [];
}

/// <summary>
///     Wasm 代码段项（函数体）。
/// </summary>
public sealed class WasmCode
{
    /// <summary>
    ///     局部变量声明列表。
    /// </summary>
    public IReadOnlyList<WasmLocal> Locals { get; init; } = [];

    /// <summary>
    ///     函数体字节码（不含局部变量声明）。
    /// </summary>
    public byte[] Body { get; init; } = [];
}

/// <summary>
///     Wasm 局部变量声明。
/// </summary>
public sealed class WasmLocal
{
    /// <summary>
    ///     变量数量。
    /// </summary>
    public uint Count { get; init; }

    /// <summary>
    ///     变量类型。
    /// </summary>
    public WasmValueType Type { get; init; }
}

/// <summary>
///     Wasm 数据段项（内存初始化数据）。
/// </summary>
public sealed class WasmData
{
    /// <summary>
    ///     目标内存索引。
    /// </summary>
    public uint MemoryIndex { get; init; }

    /// <summary>
    ///     偏移量初始化指令字节码。
    /// </summary>
    public byte[] OffsetExpression { get; init; } = [];

    /// <summary>
    ///     初始化数据。
    /// </summary>
    public byte[] Initializer { get; init; } = [];
}

/// <summary>
///     Wasm 自定义段。
/// </summary>
public sealed class WasmCustomSection
{
    /// <summary>
    ///     段名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     段数据。
    /// </summary>
    public byte[] Data { get; init; } = [];
}
