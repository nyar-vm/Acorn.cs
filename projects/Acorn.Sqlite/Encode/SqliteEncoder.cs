using Acorn.Frame;
using Acorn.Sqlite.Data;

namespace Acorn.Sqlite.Encode;

/// <summary>
///     SQLite 数据库文件编码器，将 C# 数据结构编码为 .sqlite 二进制格式。
/// </summary>
public sealed class SqliteEncoder
{
    /// <summary>
    ///     将 SQLite 文件头数据编码为 .sqlite 文件头二进制数据。
    /// </summary>
    /// <param name="header">文件头数据。</param>
    /// <returns>100 字节的 .sqlite 文件头数据。</returns>
    public byte[] EncodeHeader(SqliteFileHeader header)
    {
        var writer = new ByteBufferWriter(SqliteConstants.HeaderSize);

        writer.Write(SqliteConstants.MagicNumber);
        WriteInt16BigEndian(ref writer, (short)header.PageSize);
        writer.WriteU8(header.WriteVersion);
        writer.WriteU8(header.ReadVersion);
        writer.WriteU8(header.ReservedSpace);
        writer.WriteU8(header.MaxEmbeddedPayloadFraction);
        writer.WriteU8(header.MinEmbeddedPayloadFraction);
        writer.WriteU8(header.LeafPayloadFraction);
        WriteInt32BigEndian(ref writer, (int)header.FileChangeCounter);
        WriteInt32BigEndian(ref writer, (int)header.TotalPages);
        WriteInt32BigEndian(ref writer, (int)header.FirstFreelistTrunkPage);
        WriteInt32BigEndian(ref writer, (int)header.TotalFreelistPages);
        WriteInt32BigEndian(ref writer, (int)header.SchemaCookie);
        WriteInt32BigEndian(ref writer, (int)header.SchemaFormatNumber);
        WriteInt32BigEndian(ref writer, (int)header.DefaultPageCacheSize);
        WriteInt32BigEndian(ref writer, (int)header.LargestRootBtreePage);
        WriteInt32BigEndian(ref writer, (int)header.TextEncoding);
        WriteInt32BigEndian(ref writer, (int)header.UserVersion);
        WriteInt32BigEndian(ref writer, header.IncrementalVacuumMode ? 1 : 0);
        WriteInt32BigEndian(ref writer, (int)header.ApplicationId);

        writer.Write(new byte[20]);

        WriteInt32BigEndian(ref writer, (int)header.VersionValidForNumber);
        WriteInt32BigEndian(ref writer, (int)header.SqliteVersionNumber);

        return writer.ToArray();
    }

    private static void WriteInt16BigEndian(ref ByteBufferWriter writer, short value)
    {
        writer.WriteU8((byte)((value >> 8) & 0xFF));
        writer.WriteU8((byte)(value & 0xFF));
    }

    private static void WriteInt32BigEndian(ref ByteBufferWriter writer, int value)
    {
        writer.WriteU8((byte)((value >> 24) & 0xFF));
        writer.WriteU8((byte)((value >> 16) & 0xFF));
        writer.WriteU8((byte)((value >> 8) & 0xFF));
        writer.WriteU8((byte)(value & 0xFF));
    }
}
