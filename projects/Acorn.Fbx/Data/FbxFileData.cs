namespace Acorn.Fbx.Data;

/// <summary>
///     FBX 文件数据。
/// </summary>
public sealed class FbxFileData
{
    /// <summary>
    ///     FBX 版本号。
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    ///     根节点。
    /// </summary>
    public FbxNode Root { get; init; } = new();
}

/// <summary>
///     FBX 节点。
/// </summary>
public sealed class FbxNode
{
    /// <summary>
    ///     节点名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     属性列表。
    /// </summary>
    public IReadOnlyList<FbxProperty> Properties { get; init; } = [];

    /// <summary>
    ///     子节点列表。
    /// </summary>
    public IReadOnlyList<FbxNode> Children { get; init; } = [];
}

/// <summary>
///     FBX 属性。
/// </summary>
public sealed class FbxProperty
{
    /// <summary>
    ///     属性类型代码。
    /// </summary>
    public byte TypeCode { get; init; }

    /// <summary>
    ///     属性值。
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    ///     类型名称。
    /// </summary>
    public string TypeName => TypeCode switch
    {
        FbxConstants.PropertyType.Boolean => "Boolean",
        FbxConstants.PropertyType.Int8 => "Int8",
        FbxConstants.PropertyType.Int16 => "Int16",
        FbxConstants.PropertyType.Int32 => "Int32",
        FbxConstants.PropertyType.Int64 => "Int64",
        FbxConstants.PropertyType.Float32 => "Float32",
        FbxConstants.PropertyType.Float64 => "Float64",
        FbxConstants.PropertyType.String => "String",
        FbxConstants.PropertyType.RawBuffer => "RawBuffer",
        _ => $"Unknown(0x{TypeCode:X2})"
    };
}
