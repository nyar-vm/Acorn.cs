namespace Acorn.Wasm.Data;

/// <summary>
///     WASM 组件顶级数据结构，表示 Component Model 二进制格式。
///     Component 与 Core Module 的主要区别：
///     1. 版本号为 0x0A (WASI p2) 而非 0x01
///     2. 支持嵌套组件/模块
///     3. 支持 Canonical ABI 的 lift/lower 操作
/// </summary>
public sealed class WasmComponentData
{
    /// <summary>
    ///     Component 中的类型定义，包括模块类型、实例类型、函数类型等。
    /// </summary>
    public IReadOnlyList<WasmComponentType> Types { get; init; } = [];

    /// <summary>
    ///     Component 的导入项（导入核心模块、导入实例、导入函数等）。
    /// </summary>
    public IReadOnlyList<WasmComponentImport> Imports { get; init; } = [];

    /// <summary>
    ///     Component 的嵌套组件定义。
    /// </summary>
    public IReadOnlyList<WasmComponentNestedComponent> NestedComponents { get; init; } = [];

    /// <summary>
    ///     Component 的导出项。
    /// </summary>
    public IReadOnlyList<WasmComponentExport> Exports { get; init; } = [];
}

/// <summary>
///     Component 类型定义抽象基类。
/// </summary>
public abstract record WasmComponentType;

/// <summary>
///     模块类型定义，描述一个核心 WASM 模块的类型签名。
///     包含该模块的类型段索引列表以及导入/导出签名。
/// </summary>
public sealed record WasmComponentModuleType : WasmComponentType
{
    /// <summary>
    ///     模块内部核心类型的索引列表。
    /// </summary>
    public IReadOnlyList<uint> CoreTypeIndices { get; init; } = [];
}

/// <summary>
///     实例类型定义，描述一个导入或导出的实例的类型。
/// </summary>
public sealed record WasmComponentInstanceType : WasmComponentType
{
    /// <summary>
    ///     实例的导出项类型列表。
    /// </summary>
    public IReadOnlyList<(string Name, WasmComponentType Type)> ExportTypes { get; init; } = [];
}

/// <summary>
///     组件函数类型定义，描述参数和结果类型（支持 WIT 类型）。
/// </summary>
public sealed record WasmComponentFuncType : WasmComponentType
{
    /// <summary>
    ///     函数参数列表，每个参数为 (名称, 类型索引)。
    /// </summary>
    public IReadOnlyList<(string Name, uint TypeIndex)> Parameters { get; init; } = [];

    /// <summary>
    ///     函数返回类型索引列表。
    /// </summary>
    public IReadOnlyList<uint> Results { get; init; } = [];
}

/// <summary>
///     Component 导入项，对应 component 格式中的 import 段。
/// </summary>
public sealed class WasmComponentImport
{
    /// <summary>
    ///     导入源模块名（外部依赖的组件名）。
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    ///     导入项在源模块中的名称。
    /// </summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>
    ///     导入项的类型索引，指向 Types 列表。
    /// </summary>
    public uint TypeIndex { get; init; }
}

/// <summary>
///     Component 导出项，对应 component 格式中的 export 段。
/// </summary>
public sealed class WasmComponentExport
{
    /// <summary>
    ///     导出名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     导出项的排序索引，指向组件内的定义。
    /// </summary>
    public uint SortIndex { get; init; }

    /// <summary>
    ///     导出项的类型索引。
    /// </summary>
    public uint TypeIndex { get; init; }
}

/// <summary>
///     Component 嵌套组件，component 可以包含内部核心模块或子组件。
/// </summary>
public sealed class WasmComponentNestedComponent
{
    /// <summary>
    ///     嵌套的核心 WASM 模块数据（如果嵌套的是模块）。
    /// </summary>
    public WasmModuleData? CoreModule { get; init; }

    /// <summary>
    ///     嵌套的子组件数据（如果嵌套的是组件）。
    /// </summary>
    public WasmComponentData? SubComponent { get; init; }
}

/// <summary>
///     WASM Component 版本标识。
/// </summary>
public static class WasmComponentVersion
{
    /// <summary>
    ///     WASI Preview 2 Component Model 版本号（兼容 WIT 类型系统）。
    /// </summary>
    public const uint Version = 0x0A;

    /// <summary>
    ///     WASM Core (MVP) 版本号。
    /// </summary>
    public const uint CoreVersion = 0x01;
}
