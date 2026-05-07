namespace Acorn.Pe.Data;

/// <summary>
/// PE 文件中可选头的格式标识。
/// </summary>
public enum OptionalHeaderMagic : ushort
{
    /// <summary>
    /// PE32 格式（32 位可执行文件），可选头使用 32 位地址。
    /// </summary>
    Pe32 = 0x10b,

    /// <summary>
    /// PE32+ 格式（64 位可执行文件），可选头使用 64 位地址。
    /// </summary>
    Pe32Plus = 0x20b
}
