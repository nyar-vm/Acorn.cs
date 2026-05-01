using Acorn.Sqlite.Data;

namespace Acorn.Sqlite.Scanner;

/// <summary>
///     SQLite 数据库文件扫描器，提供对 .sqlite 文件的快速元信息扫描。
/// </summary>
/// <remarks>
///     .sqlite 文件格式头部为 100 字节，包含魔数、页大小、编码等元信息。
///     扫描器只读取头部元信息，不做完整的页遍历。
/// </remarks>
public ref struct SqliteScanner
{
    private ReadOnlySpan<byte> _data;

    /// <summary>
    ///     初始化 <see cref="SqliteScanner" /> 结构的新实例。
    /// </summary>
    /// <param name="data">要扫描的 .sqlite 字节数据。</param>
    public SqliteScanner(ReadOnlySpan<byte> data)
    {
        _data = data;
    }

    /// <summary>
    ///     快速判断数据是否为 .sqlite 格式。
    /// </summary>
    public bool IsSqlite()
    {
        if (_data.Length < SqliteConstants.HeaderSize)
        {
            return false;
        }

        return _data[..SqliteConstants.MagicNumber.Length].SequenceEqual(SqliteConstants.MagicNumber);
    }

    /// <summary>
    ///     扫描 .sqlite 文件头，提取数据库元信息。
    /// </summary>
    /// <returns>SQLite 文件头信息。</returns>
    public SqliteFileHeader ScanHeader()
    {
        if (_data.Length < SqliteConstants.HeaderSize)
        {
            throw new InvalidDataException($".sqlite 文件数据过短，期望至少 {SqliteConstants.HeaderSize} 字节");
        }

        if (!IsSqlite())
        {
            throw new InvalidDataException(".sqlite 文件魔数不匹配，期望 \"SQLite format 3\\0\"");
        }

        var offset = SqliteConstants.MagicNumber.Length;

        var pageSize = ReadInt16BigEndian(offset);
        offset += 2;

        var writeVersion = _data[offset++];
        var readVersion = _data[offset++];
        var reservedSpace = _data[offset++];
        var maxEmbeddedPayloadFraction = _data[offset++];
        var minEmbeddedPayloadFraction = _data[offset++];
        var leafPayloadFraction = _data[offset++];

        var fileChangeCounter = ReadInt32BigEndian(offset);
        offset += 4;

        var totalPages = ReadInt32BigEndian(offset);
        offset += 4;

        var firstFreelistTrunkPage = ReadInt32BigEndian(offset);
        offset += 4;

        var totalFreelistPages = ReadInt32BigEndian(offset);
        offset += 4;

        var schemaCookie = ReadInt32BigEndian(offset);
        offset += 4;

        var schemaFormatNumber = ReadInt32BigEndian(offset);
        offset += 4;

        var defaultPageCacheSize = ReadInt32BigEndian(offset);
        offset += 4;

        var largestRootBtreePage = ReadInt32BigEndian(offset);
        offset += 4;

        var textEncoding = (SqliteConstants.TextEncoding)ReadInt32BigEndian(offset);
        offset += 4;

        var userVersion = ReadInt32BigEndian(offset);
        offset += 4;

        var incrementalVacuumMode = ReadInt32BigEndian(offset) != 0;
        offset += 4;

        var applicationId = ReadInt32BigEndian(offset);
        offset += 4;

        offset += 20;

        var versionValidForNumber = ReadInt32BigEndian(offset);
        offset += 4;

        var sqliteVersionNumber = ReadInt32BigEndian(offset);

        return new SqliteFileHeader
        {
            PageSize = pageSize,
            WriteVersion = writeVersion,
            ReadVersion = readVersion,
            ReservedSpace = reservedSpace,
            MaxEmbeddedPayloadFraction = maxEmbeddedPayloadFraction,
            MinEmbeddedPayloadFraction = minEmbeddedPayloadFraction,
            LeafPayloadFraction = leafPayloadFraction,
            FileChangeCounter = fileChangeCounter,
            TotalPages = totalPages,
            FirstFreelistTrunkPage = firstFreelistTrunkPage,
            TotalFreelistPages = totalFreelistPages,
            SchemaCookie = schemaCookie,
            SchemaFormatNumber = schemaFormatNumber,
            DefaultPageCacheSize = defaultPageCacheSize,
            LargestRootBtreePage = largestRootBtreePage,
            TextEncoding = textEncoding,
            UserVersion = userVersion,
            IncrementalVacuumMode = incrementalVacuumMode,
            ApplicationId = applicationId,
            VersionValidForNumber = versionValidForNumber,
            SqliteVersionNumber = sqliteVersionNumber,
        };
    }

    private short ReadInt16BigEndian(int offset)
    {
        return (short)((_data[offset] << 8) | _data[offset + 1]);
    }

    private int ReadInt32BigEndian(int offset)
    {
        return (_data[offset] << 24) | (_data[offset + 1] << 16) | (_data[offset + 2] << 8) | _data[offset + 3];
    }
}
