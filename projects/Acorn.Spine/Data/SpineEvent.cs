namespace Acorn.Spine.Data;

/// <summary>
///     Spine 事件数据。
/// </summary>
public sealed class SpineEvent
{
    /// <summary>
    ///     事件名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     整数值。
    /// </summary>
    public int? IntValue { get; init; }

    /// <summary>
    ///     浮点值。
    /// </summary>
    public float? FloatValue { get; init; }

    /// <summary>
    ///     字符串值。
    /// </summary>
    public string? StringValue { get; init; }

    /// <summary>
    ///     音频路径。
    /// </summary>
    public string? AudioPath { get; init; }
}
