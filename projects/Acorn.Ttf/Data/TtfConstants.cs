namespace Acorn.Ttf.Data;

/// <summary>
///     TrueType/OpenType 字体格式常量。
/// </summary>
public static class TtfConstants
{
    /// <summary>
    ///     TrueType 字体魔数（0x00010000）。
    /// </summary>
    public const uint TrueTypeMagic = 0x00010000;

    /// <summary>
    ///     OpenType CFF 字体魔数（"OTTO"）。
    /// </summary>
    public static ReadOnlySpan<byte> CffMagic => "OTTO"u8.ToArray();

    /// <summary>
    ///     TrueType 集合字体魔数（"ttcf"）。
    /// </summary>
    public static ReadOnlySpan<byte> CollectionMagic => "ttcf"u8.ToArray();

    /// <summary>
    ///     偏移表大小。
    /// </summary>
    public const int OffsetTableSize = 12;

    /// <summary>
    ///     表记录大小。
    /// </summary>
    public const int TableRecordSize = 16;
}

/// <summary>
///     字体表名称常量。
/// </summary>
public static class TtfTableNames
{
    /// <summary>
    ///     字体头部表。
    /// </summary>
    public const string Head = "head";

    /// <summary>
    ///     水平头部表。
    /// </summary>
    public const string HHead = "hhea";

    /// <summary>
    ///     水平度量表。
    /// </summary>
    public const string HMtx = "hmtx";

    /// <summary>
    ///     最大轮廓表。
    /// </summary>
    public const string MaxP = "maxp";

    /// <summary>
    ///     字符到字形映射表。
    /// </summary>
    public const string CMap = "cmap";

    /// <summary>
    ///     命名表。
    /// </summary>
    public const string Name = "name";

    /// <summary>
    ///     OS/2 和 Windows 度量表。
    /// </summary>
    public const string OS2 = "OS/2";

    /// <summary>
    ///     位置表。
    /// </summary>
    public const string Post = "post";

    /// <summary>
    ///     字形数据表。
    /// </summary>
    public const string Glyf = "glyf";

    /// <summary>
    ///     位置索引表。
    /// </summary>
    public const string Loca = "loca";

    /// <summary>
    ///     CFF 轮廓数据。
    /// </summary>
    public const string Cff = "CFF ";
}
