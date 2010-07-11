namespace Acorn.Spine.Data;

/// <summary>
///     Spine 骨骼数据。
/// </summary>
public sealed class SpineBone
{
    /// <summary>
    ///     骨骼名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     父骨骼名称。
    /// </summary>
    public string? Parent { get; init; }

    /// <summary>
    ///     X 坐标。
    /// </summary>
    public float X { get; init; }

    /// <summary>
    ///     Y 坐标。
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    ///     旋转角度（度）。
    /// </summary>
    public float Rotation { get; init; }

    /// <summary>
    ///     X 轴缩放。
    /// </summary>
    public float ScaleX { get; init; } = 1.0f;

    /// <summary>
    ///     Y 轴缩放。
    /// </summary>
    public float ScaleY { get; init; } = 1.0f;

    /// <summary>
    ///     X 轴剪切。
    /// </summary>
    public float ShearX { get; init; }

    /// <summary>
    ///     Y 轴剪切。
    /// </summary>
    public float ShearY { get; init; }

    /// <summary>
    ///     骨骼长度。
    /// </summary>
    public float Length { get; init; }

    /// <summary>
    ///     变换模式。
    /// </summary>
    public string TransformMode { get; init; } = "normal";

    /// <summary>
    ///     是否需要皮肤。
    /// </summary>
    public bool IsSkinRequired { get; init; }
}
