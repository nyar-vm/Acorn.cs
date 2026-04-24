namespace Acorn.BmFont.Data;

/// <summary>
///     BMFont 位图字体文件数据。
/// </summary>
public sealed class BmFontData
{
    /// <summary>
    ///     字体信息。
    /// </summary>
    public BmFontInfo Info { get; init; } = new();

    /// <summary>
    ///     通用信息。
    /// </summary>
    public BmFontCommon Common { get; init; } = new();

    /// <summary>
    ///     页面名称列表。
    /// </summary>
    public IReadOnlyList<string> Pages { get; init; } = [];

    /// <summary>
    ///     字符列表。
    /// </summary>
    public IReadOnlyList<BmFontChar> Chars { get; init; } = [];

    /// <summary>
    ///     字距对列表。
    /// </summary>
    public IReadOnlyList<BmFontKerningPair> KerningPairs { get; init; } = [];
}

/// <summary>
///     BMFont 字体信息。
/// </summary>
public sealed class BmFontInfo
{
    /// <summary>
    ///     字体大小。
    /// </summary>
    public short Size { get; init; }

    /// <summary>
    ///     位深度（8/32）。
    /// </summary>
    public byte BitDepth { get; init; }

    /// <summary>
    ///     是否使用粗体。
    /// </summary>
    public bool Bold { get; init; }

    /// <summary>
    ///     是否使用斜体。
    /// </summary>
    public bool Italic { get; init; }

    /// <summary>
    ///     字体字符集。
    /// </summary>
    public byte CharSet { get; init; }

    /// <summary>
    ///     是否使用 Unicode。
    /// </summary>
    public bool Unicode { get; init; }

    /// <summary>
    ///     水平间距。
    /// </summary>
    public short SpacingH { get; init; }

    /// <summary>
    ///     垂直间距。
    /// </summary>
    public short SpacingV { get; init; }

    /// <summary>
    ///     行高。
    /// </summary>
    public short LineHeight { get; init; }

    /// <summary>
    ///     字体名称。
    /// </summary>
    public string FontName { get; init; } = string.Empty;
}

/// <summary>
///     BMFont 通用信息。
/// </summary>
public sealed class BmFontCommon
{
    /// <summary>
    ///     行高。
    /// </summary>
    public ushort LineHeight { get; init; }

    /// <summary>
    ///     基线高度。
    /// </summary>
    public ushort Base { get; init; }

    /// <summary>
    ///     纹理宽度。
    /// </summary>
    public ushort ScaleW { get; init; }

    /// <summary>
    ///     纹理高度。
    /// </summary>
    public ushort ScaleH { get; init; }

    /// <summary>
    ///     页面数量。
    /// </summary>
    public ushort Pages { get; init; }

    /// <summary>
    ///     是否使用 Alpha 通道。
    /// </summary>
    public bool AlphaChannel { get; init; }

    /// <summary>
    ///     是否使用红色通道。
    /// </summary>
    public bool RedChannel { get; init; }

    /// <summary>
    ///     是否使用绿色通道。
    /// </summary>
    public bool GreenChannel { get; init; }

    /// <summary>
    ///     是否使用蓝色通道。
    /// </summary>
    public bool BlueChannel { get; init; }

    /// <summary>
    ///     是否打包。
    /// </summary>
    public bool Packed { get; init; }
}

/// <summary>
///     BMFont 字符信息。
/// </summary>
public sealed class BmFontChar
{
    /// <summary>
    ///     字符 ID（Unicode 码点）。
    /// </summary>
    public uint Id { get; init; }

    /// <summary>
    ///     X 坐标。
    /// </summary>
    public ushort X { get; init; }

    /// <summary>
    ///     Y 坐标。
    /// </summary>
    public ushort Y { get; init; }

    /// <summary>
    ///     宽度。
    /// </summary>
    public ushort Width { get; init; }

    /// <summary>
    ///     高度。
    /// </summary>
    public ushort Height { get; init; }

    /// <summary>
    ///     X 偏移。
    /// </summary>
    public short XOffset { get; init; }

    /// <summary>
    ///     Y 偏移。
    /// </summary>
    public short YOffset { get; init; }

    /// <summary>
    ///     X 前进量。
    /// </summary>
    public short XAdvance { get; init; }

    /// <summary>
    ///     页面索引。
    /// </summary>
    public byte Page { get; init; }

    /// <summary>
    ///     通道。
    /// </summary>
    public BmFontChannel Channel { get; init; }
}

/// <summary>
///     BMFont 字距对。
/// </summary>
public sealed class BmFontKerningPair
{
    /// <summary>
    ///     第一个字符 ID。
    /// </summary>
    public uint First { get; init; }

    /// <summary>
    ///     第二个字符 ID。
    /// </summary>
    public uint Second { get; init; }

    /// <summary>
    ///     字距调整量。
    /// </summary>
    public short Amount { get; init; }
}
