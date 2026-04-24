namespace Acorn.Gnosis.Data;

/// <summary>
///     Gnosis 字节码模块数据。
/// </summary>
public sealed class GnosisModuleData
{
    /// <summary>
    ///     模块格式类型。
    /// </summary>
    public GnosisModuleFormat Format { get; init; }

    /// <summary>
    ///     版本号。
    /// </summary>
    public ushort Version { get; init; }

    /// <summary>
    ///     模块名称。
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    ///     常量池。
    /// </summary>
    public IReadOnlyList<GnosisConstant> Constants { get; init; } = [];

    /// <summary>
    ///     导入符号列表。
    /// </summary>
    public IReadOnlyList<string> ImportedSymbols { get; init; } = [];

    /// <summary>
    ///     导出符号列表。
    /// </summary>
    public IReadOnlyList<string> ExportedSymbols { get; init; } = [];

    /// <summary>
    ///     依赖模块列表。
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    /// <summary>
    ///     指令字节码。
    /// </summary>
    public byte[] Instructions { get; init; } = [];

    /// <summary>
    ///     函数表（GNOS 格式）。
    /// </summary>
    public IReadOnlyList<GnosisFunction> Functions { get; init; } = [];
}

/// <summary>
///     Gnosis 常量池条目。
/// </summary>
public sealed class GnosisConstant
{
    /// <summary>
    ///     常量类型标签。
    /// </summary>
    public GnosisConstantTag Tag { get; init; }

    /// <summary>
    ///     常量值。
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    ///     类型名称。
    /// </summary>
    public string TagName => Tag switch
    {
        GnosisConstantTag.String => "String",
        GnosisConstantTag.Int => "Int",
        GnosisConstantTag.Float => "Float",
        _ => $"Unknown(0x{(byte)Tag:X2})"
    };
}

/// <summary>
///     Gnosis 函数定义（GNOS 格式）。
/// </summary>
public sealed class GnosisFunction
{
    /// <summary>
    ///     函数名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     参数数量。
    /// </summary>
    public int ParameterCount { get; init; }

    /// <summary>
    ///     代码长度。
    /// </summary>
    public int CodeLength { get; init; }

    /// <summary>
    ///     函数代码。
    /// </summary>
    public byte[] Code { get; init; } = [];
}
