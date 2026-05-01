namespace Acorn.Sqlite.Data;

/// <summary>
///     SQLite 数据库文件格式常量。
///     参考：https://www.sqlite.org/fileformat.html
/// </summary>
public static class SqliteConstants
{
    /// <summary>
    ///     SQLite 文件魔数（"SQLite format 3\0"）。
    /// </summary>
    public static ReadOnlySpan<byte> MagicNumber => "SQLite format 3\0"u8;

    /// <summary>
    ///     文件头大小（100 字节）。
    /// </summary>
    public const int HeaderSize = 100;

    /// <summary>
    ///     Wal 索引文件头大小。
    /// </summary>
    public const int WalHeaderSize = 32;

    /// <summary>
    ///     日志文件头大小。
    /// </summary>
    public const int JournalHeaderSize = 512;

    /// <summary>
    ///     默认页大小。
    /// </summary>
    public const int DefaultPageSize = 4096;

    /// <summary>
    ///     最小页大小（512）。
    /// </summary>
    public const int MinPageSize = 512;

    /// <summary>
    ///     最大页大小（65536）。
    /// </summary>
    public const int MaxPageSize = 65536;

    /// <summary>
    ///     数据库文本编码。
    /// </summary>
    public enum TextEncoding : uint
    {
        /// <summary>
        ///     UTF-8 编码。
        /// </summary>
        Utf8 = 1,

        /// <summary>
        ///     UTF-16LE 编码。
        /// </summary>
        Utf16Le = 2,

        /// <summary>
        ///     UTF-16BE 编码。
        /// </summary>
        Utf16Be = 3,
    }
}

/// <summary>
///     SQLite 页类型。
/// </summary>
public enum SqlitePageType : byte
{
    /// <summary>
    ///     内部索引 B-Tree 页。
    /// </summary>
    InteriorIndex = 0x02,

    /// <summary>
    ///     内部表 B-Tree 页。
    /// </summary>
    InteriorTable = 0x05,

    /// <summary>
    ///     叶子索引 B-Tree 页。
    /// </summary>
    LeafIndex = 0x0A,

    /// <summary>
    ///     叶子表 B-Tree 页。
    /// </summary>
    LeafTable = 0x0D,
}

/// <summary>
///     SQLite 序列类型（记录格式序列化类型）。
/// </summary>
public enum SqliteSerialType : byte
{
    /// <summary>
    ///     NULL 值。
    /// </summary>
    Null = 0,

    /// <summary>
    ///     8 位整数。
    /// </summary>
    Int8 = 1,

    /// <summary>
    ///     16 位大端整数。
    /// </summary>
    Int16 = 2,

    /// <summary>
    ///     24 位大端整数。
    /// </summary>
    Int24 = 3,

    /// <summary>
    ///     32 位大端整数。
    /// </summary>
    Int32 = 4,

    /// <summary>
    ///     48 位大端整数。
    /// </summary>
    Int48 = 5,

    /// <summary>
    ///     64 位大端整数。
    /// </summary>
    Int64 = 6,

    /// <summary>
    ///     IEEE 754 64 位浮点数。
    /// </summary>
    Float64 = 7,

    /// <summary>
    ///     整数 0（常量，不在记录中存储字节）。
    /// </summary>
    Zero = 8,

    /// <summary>
    ///     整数 1（常量，不在记录中存储字节）。
    /// </summary>
    One = 9,
}

/// <summary>
///     B-Tree 页头部标志。
/// </summary>
[Flags]
public enum SqlitePageFlags : byte
{
    /// <summary>
    ///     无标志。
    /// </summary>
    None = 0,

    /// <summary>
    ///     页包含零长度数据。
    /// </summary>
    ZeroData = 0x01,

    /// <summary>
    ///     页包含可变长度数据。
    /// </summary>
    Varint = 0x02,

    /// <summary>
    ///     叶子页。
    /// </summary>
    Leaf = 0x04,

    /// <summary>
    ///     内部页。
    /// </summary>
    Interior = 0x08,
}
