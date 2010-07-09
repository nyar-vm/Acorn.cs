using System.IO.Compression;
using System.Text;
using Acorn.Frame;

namespace Acorn.Zip.Scanner;

/// <summary>
///     ZIP 文件扫描器，基于 <see cref="ByteBuffer" /> 提供对 ZIP 压缩包的快速结构扫描。
/// </summary>
public ref struct ZipScanner
{
    private ByteBuffer _buffer;

    private static ReadOnlySpan<byte> ZipLocalHeader => [0x50, 0x4B, 0x03, 0x04];
    private static ReadOnlySpan<byte> ZipCentralHeader => [0x50, 0x4B, 0x01, 0x02];
    private static ReadOnlySpan<byte> ZipEndOfCentralHeader => [0x50, 0x4B, 0x05, 0x06];

    public ZipScanner(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    public int Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    public int Length => _buffer.Length;

    public bool IsEndOfData => _buffer.IsEnd;

    /// <summary>
    ///     验证 ZIP 文件头。
    /// </summary>
    public bool ValidateHeader()
    {
        return _buffer.MatchMagic(ZipLocalHeader) || _buffer.MatchMagic(ZipEndOfCentralHeader);
    }

    /// <summary>
    ///     扫描 ZIP 文件，提取本地文件头条目统计信息。
    /// </summary>
    /// <remarks>
    ///     通过扫描本地文件头（Local File Header）提取条目信息，
    ///     不解压数据，实现快速探查。
    ///     本地文件头格式：4 字节签名 + 26 字节固定字段 + 变长文件名 + 变长额外字段。
    /// </remarks>
    public ZipStatistics ScanStatistics()
    {
        var stats = new ZipStatistics();

        if (!ValidateHeader())
        {
            return stats;
        }

        while (_buffer.Position + 30 <= _buffer.Length)
        {
            if (!_buffer.MatchMagic(ZipLocalHeader))
            {
                break;
            }

            _buffer.Advance(4);

            var versionNeeded = _buffer.ReadU16LE();
            var flags = _buffer.ReadU16LE();
            var compressionMethod = _buffer.ReadU16LE();
            var lastModTime = _buffer.ReadU16LE();
            var lastModDate = _buffer.ReadU16LE();
            var crc32 = _buffer.ReadU32LE();
            var compressedSize = _buffer.ReadU32LE();
            var uncompressedSize = _buffer.ReadU32LE();
            var fileNameLength = _buffer.ReadU16LE();
            var extraFieldLength = _buffer.ReadU16LE();

            string fileName = string.Empty;

            if (fileNameLength > 0 && _buffer.Position + fileNameLength <= _buffer.Length)
            {
                fileName = _buffer.ReadString(fileNameLength);
            }

            _buffer.Advance(extraFieldLength);

            var isEncrypted = (flags & 0x01) != 0;

            stats.EntryCount++;
            stats.TotalCompressedSize += compressedSize;
            stats.TotalUncompressedSize += uncompressedSize;

            if (isEncrypted)
            {
                stats.EncryptedCount++;
            }

            if (fileNameLength > 0)
            {
                stats.EntryNames.Add(fileName);
            }

            if (_buffer.Position + compressedSize > _buffer.Length)
            {
                break;
            }

            _buffer.Advance((int)compressedSize);
        }

        return stats;
    }
}

/// <summary>
///     ZIP 文件统计信息。
/// </summary>
public sealed class ZipStatistics
{
    public int EntryCount { get; set; }
    public long TotalCompressedSize { get; set; }
    public long TotalUncompressedSize { get; set; }
    public int EncryptedCount { get; set; }
    public List<string> EntryNames { get; set; } = new();
}
