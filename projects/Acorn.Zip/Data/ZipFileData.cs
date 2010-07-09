namespace Acorn.Zip.Data;

/// <summary>
///     ZIP 文件条目数据。
/// </summary>
public sealed class ZipEntryData
{
    /// <summary>
    ///     条目名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     条目大小。
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    ///     压缩后大小。
    /// </summary>
    public long CompressedSize { get; init; }

    /// <summary>
    ///     压缩方法。
    /// </summary>
    public ushort CompressionMethod { get; init; }

    /// <summary>
    ///     条目数据。
    /// </summary>
    public byte[] Data { get; init; } = [];
}

/// <summary>
///     ZIP 文件数据。
/// </summary>
public sealed class ZipFileData
{
    /// <summary>
    ///     ZIP 条目列表。
    /// </summary>
    public IReadOnlyList<ZipEntryData> Entries { get; init; } = [];

    /// <summary>
    ///     条目数量。
    /// </summary>
    public int EntryCount => Entries.Count;
}