namespace Acorn.Jvm.Data;

/// <summary>
///     JVM ClassFile 格式常量。
/// </summary>
public static class JvmConstants
{
    /// <summary>
    ///     ClassFile 魔数（0xCAFEBABE）。
    /// </summary>
    public const uint Magic = 0xCAFEBABE;

    /// <summary>
    ///     ClassFile 魔数的大端字节序表示，用于 <see cref="Frame.SpanScanner.MatchMagic" />。
    /// </summary>
    public static readonly byte[] MagicBigEndian = [0xCA, 0xFE, 0xBA, 0xBE];
}
