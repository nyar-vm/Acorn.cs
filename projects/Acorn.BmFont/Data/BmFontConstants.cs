namespace Acorn.BmFont.Data;

/// <summary>
///     BMFont 二进制格式常量。
/// </summary>
public static class BmFontConstants
{
    /// <summary>
    ///     BMFont 二进制格式魔数（"BMF"）。
    /// </summary>
    public static ReadOnlySpan<byte> BinaryMagic => "BMF"u8.ToArray();

    /// <summary>
    ///     BMFont 二进制版本号（3）。
    /// </summary>
    public const byte BinaryVersion = 3;

    /// <summary>
    ///     信息块 ID。
    /// </summary>
    public const byte BlockInfo = 1;

    /// <summary>
    ///     通用块 ID。
    /// </summary>
    public const byte BlockCommon = 2;

    /// <summary>
    ///     页面块 ID。
    /// </summary>
    public const byte BlockPages = 3;

    /// <summary>
    ///     字符块 ID。
    /// </summary>
    public const byte BlockChars = 4;

    /// <summary>
    ///     字距块 ID。
    /// </summary>
    public const byte BlockKerningPairs = 5;
}

/// <summary>
///     BMFont 通道类型。
/// </summary>
public enum BmFontChannel : byte
{
    /// <summary>
    ///     通道值等于字形属性。
    /// </summary>
    Glyph = 0,

    /// <summary>
    ///     轮廓通道。
    /// </summary>
    Outline = 1,

    /// <summary>
    ///     字形 + 轮廓通道。
    /// </summary>
    GlyphAndOutline = 2,

    /// <summary>
    ///     零通道（alpha = 0）。
    /// </summary>
    Zero = 3
}
