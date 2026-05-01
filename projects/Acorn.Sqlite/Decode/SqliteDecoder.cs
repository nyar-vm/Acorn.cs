using Acorn.Sqlite.Data;

namespace Acorn.Sqlite.Decode;

/// <summary>
///     SQLite 数据库文件解码器，将 .sqlite 二进制格式解码为 C# 数据结构。
/// </summary>
public sealed class SqliteDecoder
{
    /// <summary>
    ///     从 .sqlite 二进制数据中解码文件头信息。
    /// </summary>
    /// <param name="data">.sqlite 二进制数据。</param>
    /// <returns>SQLite 文件头数据。</returns>
    public SqliteFileHeader DecodeHeader(byte[] data)
    {
        var scanner = new Scanner.SqliteScanner(data);

        if (!scanner.IsSqlite())
        {
            throw new InvalidDataException("数据不是有效的 .sqlite 文件格式");
        }

        return scanner.ScanHeader();
    }

    /// <summary>
    ///     从 .sqlite 二进制数据中解码文件头信息。
    /// </summary>
    /// <param name="data">.sqlite 二进制数据。</param>
    /// <returns>SQLite 文件头数据。</returns>
    public SqliteFileHeader DecodeHeader(ReadOnlySpan<byte> data)
    {
        var scanner = new Scanner.SqliteScanner(data);

        if (!scanner.IsSqlite())
        {
            throw new InvalidDataException("数据不是有效的 .sqlite 文件格式");
        }

        return scanner.ScanHeader();
    }
}
