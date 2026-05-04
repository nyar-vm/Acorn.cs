namespace Acorn.Pe.Data;

/// <summary>
///     PE（Portable Executable）格式常量。
/// </summary>
public static class PeConstants
{
    /// <summary>
    ///     DOS 头魔数 "MZ"（小端序）。
    /// </summary>
    public const ushort DosMagic = 0x5A4D;

    /// <summary>
    ///     DOS 魔数字节序列（大端序，用于 SpanScanner）。
    /// </summary>
    public static ReadOnlySpan<byte> DosMagicBytes => new byte[] { 0x4D, 0x5A };

    /// <summary>
    ///     PE 签名 "PE\0\0"（小端序）。
    /// </summary>
    public const uint PeMagic = 0x00004550;

    /// <summary>
    ///     PE32 可选头魔数。
    /// </summary>
    public const ushort OptionalMagicPE32 = 0x10B;

    /// <summary>
    ///     PE32+ 可选头魔数（64 位）。
    /// </summary>
    public const ushort OptionalMagicPE32Plus = 0x20B;

    /// <summary>
    ///     PE 偏移量在 DOS 头中的位置。
    /// </summary>
    public const int PeOffsetPosition = 60;

    /// <summary>
    ///     PE 头中可选头大小的偏移量。
    /// </summary>
    public const int OptionalHeaderSizeOffset = 16;

    /// <summary>
    ///     PE32 可选头大小。
    /// </summary>
    public const int OptionalHeaderSizePE32 = 96;

    /// <summary>
    ///     PE32+ 可选头大小。
    /// </summary>
    public const int OptionalHeaderSizePE32Plus = 112;

    /// <summary>
    ///     文件特征标志 — 可执行文件。
    /// </summary>
    public const ushort CharacteristicsExecutable = 0x0002;

    /// <summary>
    ///     文件特征标志 — DLL 文件。
    /// </summary>
    public const ushort CharacteristicsDll = 0x2000;

    /// <summary>
    ///     节区特征标志 — 包含代码。
    /// </summary>
    public const uint SectionCharacteristicsCode = 0x20000000;

    /// <summary>
    ///     节区特征标志 — 包含已初始化数据。
    /// </summary>
    public const uint SectionCharacteristicsInitializedData = 0x00000040;

    /// <summary>
    ///     节区特征标志 — 可读。
    /// </summary>
    public const uint SectionCharacteristicsReadable = 0x40000000;

    /// <summary>
    ///     节区特征标志 — 可写。
    /// </summary>
    public const uint SectionCharacteristicsWritable = 0x80000000;

    /// <summary>
    ///     导入描述符大小（20 字节）。
    /// </summary>
    public const int ImportDescriptorSize = 20;

    /// <summary>
    ///     重定位块头大小（8 字节：VirtualAddress + SizeOfBlock）。
    /// </summary>
    public const int RelocationBlockHeaderSize = 8;

    /// <summary>
    ///     重定位条目大小（2 字节：Type(4bit) + Offset(12bit)）。
    /// </summary>
    public const int RelocationEntrySize = 2;

    /// <summary>
    ///     重定位类型 — HIGHLOW（32 位绝对地址）。
    /// </summary>
    public const byte RelocationTypeHighLow = 3;

    /// <summary>
    ///     重定位类型 — DIR64（64 位绝对地址）。
    /// </summary>
    public const byte RelocationTypeDir64 = 10;

    /// <summary>
    ///     导入 thunk 大小（PE32 = 4 字节）。
    /// </summary>
    public const int ImportThunkSize32 = 4;

    /// <summary>
    ///     导入 thunk 大小（PE32+ = 8 字节）。
    /// </summary>
    public const int ImportThunkSize64 = 8;

    /// <summary>
    ///     导入序数标志（最高位为 1 表示按序数导入）。
    /// </summary>
    public const ulong ImportOrdinalFlag32 = 0x80000000;

    /// <summary>
    ///     导入序数标志（64 位）。
    /// </summary>
    public const ulong ImportOrdinalFlag64 = 0x8000000000000000;
}
