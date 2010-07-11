namespace Acorn.Spine.Data;

/// <summary>
///     Spine 动画数据。
/// </summary>
public sealed class SpineAnimation
{
    /// <summary>
    ///     动画名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     骨骼时间轴字典（骨骼名称 -> 时间轴列表）。
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<SpineTimeline>> Bones { get; init; } = new Dictionary<string, IReadOnlyList<SpineTimeline>>();

    /// <summary>
    ///     插槽时间轴字典（插槽名称 -> 时间轴列表）。
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<SpineTimeline>> Slots { get; init; } = new Dictionary<string, IReadOnlyList<SpineTimeline>>();

    /// <summary>
    ///     变形时间轴列表。
    /// </summary>
    public IReadOnlyList<SpineTimeline> Deforms { get; init; } = [];

    /// <summary>
    ///     绘制顺序时间轴列表。
    /// </summary>
    public IReadOnlyList<SpineTimeline> DrawOrders { get; init; } = [];

    /// <summary>
    ///     事件时间轴列表。
    /// </summary>
    public IReadOnlyList<SpineTimeline> Events { get; init; } = [];
}

/// <summary>
///     Spine 时间轴数据。
/// </summary>
public sealed class SpineTimeline
{
    /// <summary>
    ///     时间轴类型（rotate、translate、scale、shear、attachment、color 等）。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    ///     关键帧列表。
    /// </summary>
    public IReadOnlyList<SpineKeyframe> Keyframes { get; init; } = [];
}

/// <summary>
///     Spine 关键帧数据。
/// </summary>
public sealed class SpineKeyframe
{
    /// <summary>
    ///     时间点（秒）。
    /// </summary>
    public float Time { get; init; }

    /// <summary>
    ///     曲线类型（linear、stepped、bezier）。
    /// </summary>
    public string Curve { get; init; } = "linear";

    /// <summary>
    ///     贝塞尔曲线控制点。
    /// </summary>
    public IReadOnlyList<float>? CurveControlPoints { get; init; }

    /// <summary>
    ///     关键帧属性值字典。
    /// </summary>
    public IReadOnlyDictionary<string, object> Values { get; init; } = new Dictionary<string, object>();
}
