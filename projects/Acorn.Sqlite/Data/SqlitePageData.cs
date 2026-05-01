namespace Acorn.Sqlite.Data;

/// <summary>
///     SQLite 数据库文件头数据。
/// </summary>
public sealed class SqliteFileHeader
{
    /// <summary>
    ///     页大小（字节数，必须为 2 的幂，512-65536）。
    /// </summary>
    public int PageSize { get; init; } = SqliteConstants.DefaultPageSize;

    /// <summary>
    ///     文件格式写版本。
    /// </summary>
    public byte WriteVersion { get; init; } = 1;

    /// <summary>
    ///     文件格式读版本。
    /// </summary>
    public byte ReadVersion { get; init; } = 1;

    /// <summary>
    ///     每页未使用空间预留字节数。
    /// </summary>
    public byte ReservedSpace { get; init; }

    /// <summary>
    ///     最大嵌入载荷比例（默认为 64，即 25%）。
    /// </summary>
    public byte MaxEmbeddedPayloadFraction { get; init; } = 64;

    /// <summary>
    ///     最小嵌入载荷比例（默认为 32，即 12.5%）。
    /// </summary>
    public byte MinEmbeddedPayloadFraction { get; init; } = 32;

    /// <summary>
    ///     叶子载荷比例（默认为 32，即 12.5%）。
    /// </summary>
    public byte LeafPayloadFraction { get; init; } = 32;

    /// <summary>
    ///     文件变更计数器。
    /// </summary>
    public long FileChangeCounter { get; init; }

    /// <summary>
    ///     数据库总页数。
    /// </summary>
    public long TotalPages { get; init; }

    /// <summary>
    ///     首页 freelist trunk 页号。
    /// </summary>
    public long FirstFreelistTrunkPage { get; init; }

    /// <summary>
    ///     freelist 页总数。
    /// </summary>
    public long TotalFreelistPages { get; init; }

    /// <summary>
    ///     schema cookie。
    /// </summary>
    public long SchemaCookie { get; init; }

    /// <summary>
    ///     schema 格式号（支持 schema 格式 1-4）。
    /// </summary>
    public long SchemaFormatNumber { get; init; } = 4;

    /// <summary>
    ///     默认页缓存大小。
    /// </summary>
    public long DefaultPageCacheSize { get; init; }

    /// <summary>
    ///     最大根 B-Tree 页号（auto-vacuum 模式下使用）。
    /// </summary>
    public long LargestRootBtreePage { get; init; }

    /// <summary>
    ///     数据库文本编码。
    /// </summary>
    public SqliteConstants.TextEncoding TextEncoding { get; init; } = SqliteConstants.TextEncoding.Utf8;

    /// <summary>
    ///     user_version（用户自定义版本号）。
    /// </summary>
    public long UserVersion { get; init; }

    /// <summary>
    ///     incremental_vacuum 模式标志。
    /// </summary>
    public bool IncrementalVacuumMode { get; init; }

    /// <summary>
    ///     应用 ID。
    /// </summary>
    public long ApplicationId { get; init; }

    /// <summary>
    ///     版本有效号（用于检测文件格式变化）。
    /// </summary>
    public long VersionValidForNumber { get; init; }

    /// <summary>
    ///     SQLite 版本号（编译时版本号 × 1000000）。
    /// </summary>
    public long SqliteVersionNumber { get; init; }
}

/// <summary>
///     SQLite 页数据基类。
/// </summary>
public abstract class SqlitePageData
{
    /// <summary>
    ///     页号（从 1 开始）。
    /// </summary>
    public long PageNumber { get; init; }

    /// <summary>
    ///     页类型。
    /// </summary>
    public SqlitePageType PageType { get; init; }

    /// <summary>
    ///     第一个 freeblock 偏移量。
    /// </summary>
    public int FirstFreeblock { get; init; }

    /// <summary>
    ///     页中 cell 数量。
    /// </summary>
    public int CellCount { get; init; }

    /// <summary>
    ///     cell 内容区域起始偏移量。
    /// </summary>
    public int CellContentStart { get; init; }

    /// <summary>
    ///     页中 fragmented free bytes 数量。
    /// </summary>
    public byte FragmentedFreeBytes { get; init; }
}
