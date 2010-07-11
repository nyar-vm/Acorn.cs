namespace Acorn.Spine.Data;

/// <summary>
///     Spine 插槽数据。
/// </summary>
public sealed class SpineSlot
{
    /// <summary>
    ///     插槽名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     所属骨骼名称。
    /// </summary>
    public string Bone { get; init; } = string.Empty;

    /// <summary>
    ///     默认附件名称。
    /// </summary>
    public string? Attachment { get; init; }

    /// <summary>
    ///     混合模式。
    /// </summary>
    public string? Blend { get; init; }

    /// <summary>
    ///     颜色值（RGBA 十六进制字符串）。
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    ///     暗色值（RGBA 十六进制字符串）。
    /// </summary>
    public string? DarkColor { get; init; }
}
