namespace Acorn.ELF.Data;

/// <summary>
///     ELF（Executable and Linkable Format）格式常量。
/// </summary>
public static class ElfConstants
{
    /// <summary>
    ///     ELF 魔数字节序列（0x7F 'E' 'L' 'F'）。
    /// </summary>
    public static ReadOnlySpan<byte> Magic => new byte[] { 0x7F, 0x45, 0x4C, 0x46 };

    /// <summary>
    ///     ELF 文件类别 — 32 位（ELFCLASS32）。
    /// </summary>
    public const byte Class32 = 1;

    /// <summary>
    ///     ELF 文件类别 — 64 位（ELFCLASS64）。
    /// </summary>
    public const byte Class64 = 2;

    /// <summary>
    ///     数据编码 — 小端序（ELFDATA2LSB）。
    /// </summary>
    public const byte DataEncodingLittleEndian = 1;

    /// <summary>
    ///     数据编码 — 大端序（ELFDATA2MSB）。
    /// </summary>
    public const byte DataEncodingBigEndian = 2;

    /// <summary>
    ///     文件类型 — 可执行文件（ET_EXEC）。
    /// </summary>
    public const ushort TypeExecutable = 2;

    /// <summary>
    ///     文件类型 — 共享库（ET_DYN）。
    /// </summary>
    public const ushort TypeSharedLibrary = 3;

    /// <summary>
    ///     符号绑定 — 局部符号（STB_LOCAL）。
    /// </summary>
    public const byte SymbolBindLocal = 0;

    /// <summary>
    ///     符号绑定 — 全局符号（STB_GLOBAL）。
    /// </summary>
    public const byte SymbolBindGlobal = 1;

    /// <summary>
    ///     符号绑定 — 弱符号（STB_WEAK）。
    /// </summary>
    public const byte SymbolBindWeak = 2;

    /// <summary>
    ///     符号类型 — 未指定类型（STT_NOTYPE）。
    /// </summary>
    public const byte SymbolTypeNoType = 0;

    /// <summary>
    ///     符号类型 — 对象/数据（STT_OBJECT）。
    /// </summary>
    public const byte SymbolTypeObject = 1;

    /// <summary>
    ///     符号类型 — 函数/代码（STT_FUNC）。
    /// </summary>
    public const byte SymbolTypeFunc = 2;

    /// <summary>
    ///     符号类型 — 节区（STT_SECTION）。
    /// </summary>
    public const byte SymbolTypeSection = 3;

    /// <summary>
    ///     符号类型 — 文件名（STT_FILE）。
    /// </summary>
    public const byte SymbolTypeFile = 4;

    /// <summary>
    ///     特殊节区索引 — 未定义（SHN_UNDEF）。
    /// </summary>
    public const ushort SectionIndexUndefined = 0;

    /// <summary>
    ///     特殊节区索引 — 绝对地址（SHN_ABS）。
    /// </summary>
    public const ushort SectionIndexAbsolute = 0xFFF1;

    /// <summary>
    ///     特殊节区索引 — COMMON 块（SHN_COMMON）。
    /// </summary>
    public const ushort SectionIndexCommon = 0xFFF2;

    /// <summary>
    ///     64 位符号表条目大小（Elf64_Sym = 24 字节）。
    /// </summary>
    public const int SymbolEntrySize64 = 24;

    /// <summary>
    ///     32 位符号表条目大小（Elf32_Sym = 16 字节）。
    /// </summary>
    public const int SymbolEntrySize32 = 16;
}
