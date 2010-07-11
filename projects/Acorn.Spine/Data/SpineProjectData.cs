namespace Acorn.Spine.Data;

/// <summary>
///     Spine 项目数据，包含骨骼、插槽、皮肤、动画等完整信息。
/// </summary>
public sealed class SpineProjectData
{
    /// <summary>
    ///     骨骼格式版本。
    /// </summary>
    public string SkeletonVersion { get; init; } = string.Empty;

    /// <summary>
    ///     项目哈希值。
    /// </summary>
    public string Hash { get; init; } = string.Empty;

    /// <summary>
    ///     画布宽度。
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    ///     画布高度。
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    ///     Spine 编辑器版本。
    /// </summary>
    public string SpineVersion { get; init; } = string.Empty;

    /// <summary>
    ///     骨骼列表。
    /// </summary>
    public IReadOnlyList<SpineBone> Bones { get; init; } = [];

    /// <summary>
    ///     插槽列表。
    /// </summary>
    public IReadOnlyList<SpineSlot> Slots { get; init; } = [];

    /// <summary>
    ///     皮肤列表。
    /// </summary>
    public IReadOnlyList<SpineSkin> Skins { get; init; } = [];

    /// <summary>
    ///     动画列表。
    /// </summary>
    public IReadOnlyList<SpineAnimation> Animations { get; init; } = [];

    /// <summary>
    ///     事件列表。
    /// </summary>
    public IReadOnlyList<SpineEvent> Events { get; init; } = [];

    /// <summary>
    ///     IK 约束列表。
    /// </summary>
    public IReadOnlyList<SpineIkConstraint> IkConstraints { get; init; } = [];

    /// <summary>
    ///     变换约束列表。
    /// </summary>
    public IReadOnlyList<SpineTransformConstraint> TransformConstraints { get; init; } = [];

    /// <summary>
    ///     路径约束列表。
    /// </summary>
    public IReadOnlyList<SpinePathConstraint> PathConstraints { get; init; } = [];
}
