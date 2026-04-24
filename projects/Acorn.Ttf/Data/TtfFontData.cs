namespace Acorn.Ttf.Data;

/// <summary>
///     TrueType/OpenType 字体文件数据。
/// </summary>
public sealed class TtfFontData
{
    /// <summary>
    ///     字体类型。
    /// </summary>
    public TtfFontType FontType { get; init; }

    /// <summary>
    ///     表数量。
    /// </summary>
    public ushort TableCount { get; init; }

    /// <summary>
    ///     字体表记录。
    /// </summary>
    public IReadOnlyList<TtfTableRecord> Tables { get; init; } = [];

    /// <summary>
    ///     字体头部信息（来自 head 表）。
    /// </summary>
    public TtfHeadInfo? HeadInfo { get; init; }

    /// <summary>
    ///     字体名称信息（来自 name 表）。
    /// </summary>
    public TtfNameInfo? NameInfo { get; init; }
}

/// <summary>
///     字体类型。
/// </summary>
public enum TtfFontType
{
    /// <summary>
    ///     TrueType 轮廓。
    /// </summary>
    TrueType,

    /// <summary>
    ///     CFF 轮廓（OpenType）。
    /// </summary>
    Cff,

    /// <summary>
    ///     TrueType 集合。
    /// </summary>
    Collection,

    /// <summary>
    ///     未知类型。
    /// </summary>
    Unknown
}

/// <summary>
///     字体表记录。
/// </summary>
public sealed class TtfTableRecord
{
    /// <summary>
    ///     表标签（4 字节 ASCII）。
    /// </summary>
    public string Tag { get; init; } = string.Empty;

    /// <summary>
    ///     表校验和。
    /// </summary>
    public uint Checksum { get; init; }

    /// <summary>
    ///     表偏移量。
    /// </summary>
    public uint Offset { get; init; }

    /// <summary>
    ///     表长度。
    /// </summary>
    public uint Length { get; init; }
}

/// <summary>
///     字体头部信息（head 表）。
/// </summary>
public sealed class TtfHeadInfo
{
    /// <summary>
    ///     字体版本。
    /// </summary>
    public uint Version { get; init; }

    /// <summary>
    ///     单位每 Em。
    /// </summary>
    public ushort UnitsPerEm { get; init; }

    /// <summary>
    ///     创建时间。
    /// </summary>
    public long Created { get; init; }

    /// <summary>
    ///     修改时间。
    /// </summary>
    public long Modified { get; init; }

    /// <summary>
    ///     X 最小值。
    /// </summary>
    public short XMin { get; init; }

    /// <summary>
    ///     Y 最小值。
    /// </summary>
    public short YMin { get; init; }

    /// <summary>
    ///     X 最大值。
    /// </summary>
    public short XMax { get; init; }

    /// <summary>
    ///     Y 最大值。
    /// </summary>
    public short YMax { get; init; }
}

/// <summary>
///     字体名称信息（name 表）。
/// </summary>
public sealed class TtfNameInfo
{
    /// <summary>
    ///     字体族名称。
    /// </summary>
    public string FamilyName { get; init; } = string.Empty;

    /// <summary>
    ///     字体子族名称。
    /// </summary>
    public string SubFamilyName { get; init; } = string.Empty;

    /// <summary>
    ///     完整字体名称。
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    ///     版本字符串。
    /// </summary>
    public string Version { get; init; } = string.Empty;
}
