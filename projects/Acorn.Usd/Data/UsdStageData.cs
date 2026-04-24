namespace Acorn.Usd.Data;

/// <summary>
///     USD 场景数据。
/// </summary>
public sealed class UsdStageData
{
    /// <summary>
    ///     文件类型。
    /// </summary>
    public UsdFileType FileType { get; init; }

    /// <summary>
    ///     版本号。
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    ///     根层路径。
    /// </summary>
    public string RootLayerPath { get; init; } = string.Empty;

    /// <summary>
    ///     Prim 列表。
    /// </summary>
    public IReadOnlyList<UsdPrimData> Prims { get; init; } = [];
}

/// <summary>
///     USD Prim 数据。
/// </summary>
public sealed class UsdPrimData
{
    /// <summary>
    ///     Prim 路径。
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    ///     Prim 类型名称。
    /// </summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>
    ///     Prim 名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     子 Prim 列表。
    /// </summary>
    public IReadOnlyList<UsdPrimData> Children { get; init; } = [];
}
