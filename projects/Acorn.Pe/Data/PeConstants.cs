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
}
