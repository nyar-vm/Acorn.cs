namespace Acorn.MachO.Data;

/// <summary>
///     Mach-O 格式常量。
/// </summary>
public static class MachOConstants
{
    /// <summary>
    ///     LC_SEGMENT — 32 位段加载命令。
    /// </summary>
    public const uint LcSegment = 0x01;

    /// <summary>
    ///     LC_SEGMENT_64 — 64 位段加载命令。
    /// </summary>
    public const uint LcSegment64 = 0x19;
}
