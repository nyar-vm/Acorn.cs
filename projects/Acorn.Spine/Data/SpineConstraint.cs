namespace Acorn.Spine.Data;

/// <summary>
///     Spine IK 约束数据。
/// </summary>
public sealed class SpineIkConstraint
{
    /// <summary>
    ///     约束名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     受约束的骨骼名称列表。
    /// </summary>
    public IReadOnlyList<string> Bones { get; init; } = [];

    /// <summary>
    ///     目标骨骼名称。
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    ///     弯曲方向（1 为正向，-1 为反向）。
    /// </summary>
    public int BendDirection { get; init; } = 1;

    /// <summary>
    ///     是否压缩。
    /// </summary>
    public bool Compress { get; init; }

    /// <summary>
    ///     是否拉伸。
    /// </summary>
    public bool Stretch { get; init; }

    /// <summary>
    ///     是否均匀缩放。
    /// </summary>
    public bool Uniform { get; init; }

    /// <summary>
    ///     混合系数。
    /// </summary>
    public float Mix { get; init; } = 1.0f;

    /// <summary>
    ///     软度。
    /// </summary>
    public float Softness { get; init; }
}

/// <summary>
///     Spine 变换约束数据。
/// </summary>
public sealed class SpineTransformConstraint
{
    /// <summary>
    ///     约束名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     受约束的骨骼名称列表。
    /// </summary>
    public IReadOnlyList<string> Bones { get; init; } = [];

    /// <summary>
    ///     目标骨骼名称。
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    ///     旋转混合系数。
    /// </summary>
    public float RotateMix { get; init; } = 1.0f;

    /// <summary>
    ///     位移混合系数。
    /// </summary>
    public float TranslateMix { get; init; } = 1.0f;

    /// <summary>
    ///     缩放混合系数。
    /// </summary>
    public float ScaleMix { get; init; } = 1.0f;

    /// <summary>
    ///     剪切混合系数。
    /// </summary>
    public float ShearMix { get; init; } = 1.0f;
}

/// <summary>
///     Spine 路径约束数据。
/// </summary>
public sealed class SpinePathConstraint
{
    /// <summary>
    ///     约束名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     受约束的骨骼名称列表。
    /// </summary>
    public IReadOnlyList<string> Bones { get; init; } = [];

    /// <summary>
    ///     目标插槽名称。
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    ///     位置模式。
    /// </summary>
    public float PositionMode { get; init; }

    /// <summary>
    ///     间距模式。
    /// </summary>
    public float SpacingMode { get; init; }

    /// <summary>
    ///     旋转模式。
    /// </summary>
    public float RotateMode { get; init; }

    /// <summary>
    ///     旋转角度。
    /// </summary>
    public float Rotation { get; init; }

    /// <summary>
    ///     位置。
    /// </summary>
    public float Position { get; init; }

    /// <summary>
    ///     间距。
    /// </summary>
    public float Spacing { get; init; }

    /// <summary>
    ///     旋转混合系数。
    /// </summary>
    public float RotateMix { get; init; } = 1.0f;

    /// <summary>
    ///     位移混合系数。
    /// </summary>
    public float TranslateMix { get; init; } = 1.0f;
}
