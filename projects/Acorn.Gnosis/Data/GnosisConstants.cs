namespace Acorn.Gnosis.Data;

/// <summary>
///     Gnosis 字节码模块格式常量。
/// </summary>
/// <remarks>
///     .gnosis 文件是 Gnosis VM 的字节码模块格式，基于 Game 方言特化。
///     完整规范请参阅 Gnosis.cs/documentation/technical/gnosis-bytecode-format.md
/// </remarks>
public static class GnosisConstants
{
    /// <summary>
    ///     GNOS 格式魔数。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => new byte[] { 0x47, 0x4E, 0x4F, 0x53 };

    /// <summary>
    ///     GNOS 魔数值。
    /// </summary>
    public const uint MagicValue = 0x474E4F53;

    /// <summary>
    ///     当前版本号。
    /// </summary>
    public const ushort CurrentVersion = 1;

    /// <summary>
    ///     头部最小大小（Magic 4 + Version 2）。
    /// </summary>
    public const int MinHeaderSize = 6;
}

/// <summary>
///     常量池条目类型标签。
/// </summary>
public enum GnosisConstantTag : byte
{
    /// <summary>
    ///     字符串常量。
    /// </summary>
    String = 0x01,

    /// <summary>
    ///     整数常量。
    /// </summary>
    Int = 0x02,

    /// <summary>
    ///     浮点数常量（f32，4 字节小端序）。
    /// </summary>
    Float = 0x03
}
