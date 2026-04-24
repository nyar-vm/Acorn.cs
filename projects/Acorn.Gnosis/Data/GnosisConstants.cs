namespace Acorn.Gnosis.Data;

/// <summary>
///     Gnosis 字节码模块格式常量。
/// </summary>
public static class GnosisConstants
{
    /// <summary>
    ///     GGBC 格式魔数（ScriptCompiler BytecodeGenerator 输出）。
    /// </summary>
    public static ReadOnlySpan<byte> GgbcMagic => new byte[] { 0x47, 0x47, 0x42, 0x43 };

    /// <summary>
    ///     GNOS 格式魔数（GnosisBackend NyarVM 集成输出）。
    /// </summary>
    public static ReadOnlySpan<byte> GnosMagic => new byte[] { 0x47, 0x4E, 0x4F, 0x53 };

    /// <summary>
    ///     GGBC 魔数值。
    /// </summary>
    public const uint GgbcMagicValue = 0x47474243;

    /// <summary>
    ///     GNOS 魔数值。
    /// </summary>
    public const uint GnosMagicValue = 0x474E4F53;

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
///     Gnosis 模块格式类型。
/// </summary>
public enum GnosisModuleFormat : byte
{
    /// <summary>
    ///     GGBC 格式（ScriptCompiler 输出）。
    /// </summary>
    Ggbc = 0,

    /// <summary>
    ///     GNOS 格式（GnosisBackend 输出）。
    /// </summary>
    Gnos = 1
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
    ///     浮点数常量。
    /// </summary>
    Float = 0x03
}
