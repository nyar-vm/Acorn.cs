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
}
