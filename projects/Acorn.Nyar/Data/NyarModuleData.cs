namespace Acorn.Nyar.Data;

/// <summary>
///     Nyar 字节码模块数据。
/// </summary>
public sealed class NyarModuleData
{
    /// <summary>
    ///     版本号。
    /// </summary>
    public uint Version { get; init; } = NyarConstants.CurrentVersion;

    /// <summary>
    ///     模块名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     常量池。
    /// </summary>
    public IReadOnlyList<NyarConstant> Constants { get; init; } = [];

    /// <summary>
    ///     函数表。
    /// </summary>
    public IReadOnlyList<NyarFunction> Functions { get; init; } = [];

    /// <summary>
    ///     导入表。
    /// </summary>
    public IReadOnlyList<NyarImport> Imports { get; init; } = [];

    /// <summary>
    ///     导出表。
    /// </summary>
    public IReadOnlyList<NyarExport> Exports { get; init; } = [];
}

/// <summary>
///     Nyar 常量池条目。
/// </summary>
public sealed class NyarConstant
{
    /// <summary>
    ///     常量类型。
    /// </summary>
    public NyarConstantKind Kind { get; init; }

    /// <summary>
    ///     常量值。
    /// </summary>
    public object? Value { get; init; }
}

/// <summary>
///     Nyar 函数定义。
/// </summary>
public sealed class NyarFunction
{
    /// <summary>
    ///     函数名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     参数数量。
    /// </summary>
    public int Arity { get; init; }

    /// <summary>
    ///     局部变量数量。
    /// </summary>
    public int LocalCount { get; init; }

    /// <summary>
    ///     代码偏移。
    /// </summary>
    public int CodeOffset { get; init; }

    /// <summary>
    ///     代码长度。
    /// </summary>
    public int CodeLength { get; init; }
}

/// <summary>
///     Nyar 导入条目。
/// </summary>
public sealed class NyarImport
{
    /// <summary>
    ///     导入类型。
    /// </summary>
    public NyarImportKind Kind { get; init; }

    /// <summary>
    ///     模块名称。
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    ///     符号名称。
    /// </summary>
    public string SymbolName { get; init; } = string.Empty;
}

/// <summary>
///     Nyar 导出条目。
/// </summary>
public sealed class NyarExport
{
    /// <summary>
    ///     导出类型。
    /// </summary>
    public NyarExportKind Kind { get; init; }

    /// <summary>
    ///     符号名称。
    /// </summary>
    public string SymbolName { get; init; } = string.Empty;

    /// <summary>
    ///     关联的函数索引（仅当 Kind 为 Function 时有效）。
    /// </summary>
    public int FunctionIndex { get; init; }
}
