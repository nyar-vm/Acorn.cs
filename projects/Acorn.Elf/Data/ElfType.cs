namespace Acorn.ELF.Data;

/// <summary>
///     ELF 文件类型。
/// </summary>
public enum ElfType : ushort
{
    /// <summary>
    ///     未指定文件类型。
    /// </summary>
    None = 0,

    /// <summary>
    ///     可重定位文件。
    /// </summary>
    Relocatable = 1,

    /// <summary>
    ///     可执行文件。
    /// </summary>
    Executable = 2,

    /// <summary>
    ///     共享目标文件。
    /// </summary>
    SharedObject = 3,

    /// <summary>
    ///     核心转储文件。
    /// </summary>
    Core = 4
}
