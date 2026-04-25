namespace Acorn.Nyar.Data;

/// <summary>
///     Nyar 字节码模块格式常量。
/// </summary>
public static class NyarConstants
{
    /// <summary>
    ///     .nyar 文件魔数（"NYAR" 大端序）。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => new byte[] { 0x4E, 0x59, 0x41, 0x52 };

    /// <summary>
    ///     魔数值。
    /// </summary>
    public const uint MagicValue = 0x4E594152;

    /// <summary>
    ///     当前版本号。
    /// </summary>
    public const uint CurrentVersion = 1;

    /// <summary>
    ///     头部大小（16 字节）。
    /// </summary>
    public const int HeaderSize = 16;

    /// <summary>
    ///     段头大小（9 字节：Kind 1 + Offset 4 + Size 4）。
    /// </summary>
    public const int SectionHeaderSize = 9;
}

/// <summary>
///     Nyar 段类型。
/// </summary>
public enum NyarSectionKind : byte
{
    /// <summary>
    ///     常量池段。
    /// </summary>
    Constants = 0x01,

    /// <summary>
    ///     函数表段。
    /// </summary>
    Functions = 0x02,

    /// <summary>
    ///     代码段。
    /// </summary>
    Code = 0x03,

    /// <summary>
    ///     导入表段。
    /// </summary>
    Imports = 0x04,

    /// <summary>
    ///     导出表段。
    /// </summary>
    Exports = 0x05,

    /// <summary>
    ///     调试信息段。
    /// </summary>
    DebugInfo = 0x10,

    /// <summary>
    ///     源码映射段。
    /// </summary>
    SourceMap = 0x11
}

/// <summary>
///     常量池条目类型。
/// </summary>
public enum NyarConstantKind : byte
{
    /// <summary>
    ///     32 位整数。
    /// </summary>
    Int32 = 0x01,

    /// <summary>
    ///     64 位浮点数。
    /// </summary>
    Float64 = 0x02,

    /// <summary>
    ///     布尔值。
    /// </summary>
    Bool = 0x03,

    /// <summary>
    ///     空值。
    /// </summary>
    Null = 0x04,

    /// <summary>
    ///     字符串。
    /// </summary>
    String = 0x05,

    /// <summary>
    ///     大整数。
    /// </summary>
    BigInt = 0x06
}

/// <summary>
///     导入类型。
/// </summary>
public enum NyarImportKind : byte
{
    /// <summary>
    ///     函数导入。
    /// </summary>
    Function = 0,

    /// <summary>
    ///     全局变量导入。
    /// </summary>
    Global = 1,

    /// <summary>
    ///     模块导入。
    /// </summary>
    Module = 2
}

/// <summary>
///     导出类型。
/// </summary>
public enum NyarExportKind : byte
{
    /// <summary>
    ///     函数导出。
    /// </summary>
    Function = 0,

    /// <summary>
    ///     全局变量导出。
    /// </summary>
    Global = 1
}
