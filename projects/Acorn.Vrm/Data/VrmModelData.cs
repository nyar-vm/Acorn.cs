namespace Acorn.Vrm.Data;

/// <summary>
///     VRM 模型数据。
/// </summary>
public sealed class VrmModelData
{
    /// <summary>
    ///     VRM 版本。
    /// </summary>
    public VrmVersion Version { get; init; }

    /// <summary>
    ///     模型元信息。
    /// </summary>
    public VrmMeta Meta { get; init; } = new();

    /// <summary>
    ///     人形骨骼映射。
    /// </summary>
    public IReadOnlyList<VrmHumanoidBone> Humanoid { get; init; } = [];

    /// <summary>
    ///     BlendShape 列表。
    /// </summary>
    public IReadOnlyList<VrmBlendShape> BlendShapes { get; init; } = [];
}

/// <summary>
///     VRM 元信息。
/// </summary>
public sealed class VrmMeta
{
    /// <summary>
    ///     模型名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     版本字符串。
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    ///     作者。
    /// </summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>
    ///     许可证。
    /// </summary>
    public string LicenseName { get; init; } = string.Empty;
}

/// <summary>
///     VRM 人形骨骼映射。
/// </summary>
public sealed class VrmHumanoidBone
{
    /// <summary>
    ///     骨骼类型名称。
    /// </summary>
    public string BoneName { get; init; } = string.Empty;

    /// <summary>
    ///     对应节点索引。
    /// </summary>
    public int NodeIndex { get; init; }
}

/// <summary>
///     VRM BlendShape。
/// </summary>
public sealed class VrmBlendShape
{
    /// <summary>
    ///     BlendShape 名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     预设名称。
    /// </summary>
    public string Preset { get; init; } = string.Empty;
}
