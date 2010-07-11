namespace Acorn.Spine.Data;

/// <summary>
///     Spine 皮肤数据。
/// </summary>
public sealed class SpineSkin
{
    /// <summary>
    ///     皮肤名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     附件列表。
    /// </summary>
    public IReadOnlyList<SpineSkinAttachment> Attachments { get; init; } = [];
}

/// <summary>
///     Spine 皮肤附件数据。
/// </summary>
public sealed class SpineSkinAttachment
{
    /// <summary>
    ///     插槽名称。
    /// </summary>
    public string SlotName { get; init; } = string.Empty;

    /// <summary>
    ///     附件名称。
    /// </summary>
    public string AttachmentName { get; init; } = string.Empty;

    /// <summary>
    ///     附件类型（region、mesh、weightedmesh 等）。
    /// </summary>
    public string Type { get; init; } = "region";

    /// <summary>
    ///     资源路径。
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    ///     X 坐标。
    /// </summary>
    public float X { get; init; }

    /// <summary>
    ///     Y 坐标。
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    ///     旋转角度。
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
    ///     宽度。
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    ///     高度。
    /// </summary>
    public float Height { get; init; }

    /// <summary>
    ///     顶点数据（网格类型使用）。
    /// </summary>
    public IReadOnlyList<float>? Vertices { get; init; }

    /// <summary>
    ///     三角形索引（网格类型使用）。
    /// </summary>
    public IReadOnlyList<int>? Triangles { get; init; }

    /// <summary>
    ///     UV 坐标（网格类型使用）。
    /// </summary>
    public IReadOnlyList<float>? Uvs { get; init; }
}
